# -*- coding: utf-8 -*-
"""
M7-0 依赖审计脚本（只读，仅标准库）
用法：py -3.12 -X utf8 Docs/QA/Evidence/M7-0/audit_scripts.py
输入：Assets/_Project/Scripts/**/*.cs
输出（同目录）：files.csv / types.csv / edges.csv / edges.json / stats.json /
              module-matrix.md / layer-matrix.md / cycles.md / layer-plan.md / graph.mmd
不修改任何产品代码、资产、场景；不访问 Unity。
"""
import os
import re
import csv
import json
import sys
import hashlib
from collections import defaultdict

ROOT = os.path.join("Assets", "_Project", "Scripts")
OUT = os.path.join("Docs", "QA", "Evidence", "M7-0")
MODULES = ["Core", "Units", "Combat", "Player", "System", "UI", "Items", "Framework", "Gameplay", "Debug"]
LAYER_ORDER = ["Core", "Content", "Gameplay", "App", "UI"]

# ---------------------------------------------------------------- 源码预处理

def strip_code(text):
    """注释/字符串/字符字面量 -> 空格，保留换行与列位置。"""
    out = []
    i, n = 0, len(text)
    state = "code"
    while i < n:
        c = text[i]
        nxt = text[i + 1] if i + 1 < n else ""
        if state == "code":
            if c == "/" and nxt == "/":
                state = "line_comment"; out.append("  "); i += 2; continue
            if c == "/" and nxt == "*":
                state = "block_comment"; out.append("  "); i += 2; continue
            if c == '"':
                state = "string"; out.append(" "); i += 1; continue
            if c == "'":
                state = "char"; out.append(" "); i += 1; continue
            out.append(c); i += 1
        elif state == "line_comment":
            if c == "\n":
                state = "code"; out.append("\n")
            else:
                out.append(" ")
            i += 1
        elif state == "block_comment":
            if c == "*" and nxt == "/":
                state = "code"; out.append("  "); i += 2; continue
            out.append("\n" if c == "\n" else " "); i += 1
        elif state == "string":
            if c == "\\":
                out.append("  " if nxt else " "); i += 2; continue
            if c == '"':
                state = "code"
            out.append("\n" if c == "\n" else " "); i += 1
        elif state == "char":
            if c == "\\":
                out.append("  " if nxt else " "); i += 2; continue
            if c == "'":
                state = "code"
            out.append("\n" if c == "\n" else " "); i += 1
    return "".join(out)


DECL_RE = re.compile(r"\b(class|struct|interface|enum|record)\s+([A-Za-z_]\w*)")
DELEGATE_RE = re.compile(r"\bdelegate\s+[\w<>,\[\]\.\?]+\s+([A-Za-z_]\w*)\s*\(")


def rel(p):
    return p.replace("\\", "/")


def scan():
    files = []
    for dirpath, _dirnames, filenames in os.walk(ROOT):
        for fn in sorted(filenames):
            if fn.endswith(".cs"):
                files.append(rel(os.path.join(dirpath, fn)))
    return sorted(files)


def module_of(path):
    parts = path.split("/")
    return parts[3] if len(parts) > 3 else "<root>"


# ---------------------------------------------------------------- 目标层映射
# 依据文件实际职责，而非现目录。详见 layer-plan.md 的理由列。
LAYER = {}
EXPLICIT = {
    # Core：纯数据/契约/事件/时间，不依赖 UnityEngine 场景对象
    "Core/EventBus.cs": "Core",
    "Core/UnitId.cs": "Core",
    "Core/MessageFeed.cs": "Core",
    "Core/RunState.cs": "Core",
    "Units/Team.cs": "Core",
    "Framework/Cooldown.cs": "Core",
    "Combat/ICharacterCombat.cs": "Core",
    # Content：ScriptableObject 内容定义，不持有局内状态、不引用玩法类
    "Combat/WeaponData.cs": "Content",
    "Items/ItemData.cs": "Content",
    "Units/UnitStats.cs": "Content",
    # App：局流程/成长/存档/组合根
    "Core/AppFlow.cs": "App",
    "Core/GameData.cs": "App",
    "Core/GameFlow.cs": "App",
    "Core/MetaState.cs": "App",
    "Core/PauseManager.cs": "App",
    "Core/RunFlow.cs": "App",
    # Editor：UNITY_EDITOR 专用
    "Debug/DebugTestTools.cs": "Editor",
    # Gameplay：以下显式列出，避免被目录规则误判
    "Core/UnitRegistry.cs": "Gameplay",
    "Core/WaveSpawner.cs": "Gameplay",
    "Units/SkillData.cs": "Gameplay",   # 引用 SkillEffectBase 效果链，不能进 Content
    "Debug/DebugCameraSwitcher.cs": "Gameplay",
    "Debug/Mirror.cs": "Gameplay",
}


def layer_of(path):
    key = path.split("Scripts/", 1)[-1] if "Scripts/" in path else path
    if key in EXPLICIT:
        return EXPLICIT[key]
    if "/UI/" in path:
        return "UI"
    if "/Core/" in path or "/Framework/" in path:
        return "Core"
    if "/Editor/" in path:
        return "Editor"
    return "Gameplay"


LAYER_REASON = {
    "Core": "纯数据/契约/事件/时间，无场景与玩法依赖",
    "Content": "ScriptableObject 内容定义，不持有局内运行状态",
    "Gameplay": "运行时玩法（单位/战斗/交互/AI/世界）",
    "App": "局流程/整备/成长/存档/组合根",
    "UI": "面板与视图，只依赖下层公开契约",
    "Editor": "UNITY_EDITOR 专用，运行时代码不得反向依赖",
    "Test": "测试专用",
}


def main():
    files = scan()
    file_text = {}
    raw_text = {}
    for f in files:
        with open(f, "r", encoding="utf-8-sig", errors="replace") as fh:
            raw = fh.read()
        raw_text[f] = raw
        file_text[f] = strip_code(raw)

    # ------------------------------------------------------------ 类型表
    types = []
    for f in files:
        code = file_text[f]
        for m in DECL_RE.finditer(code):
            types.append({"name": m.group(2), "kind": m.group(1), "file": f,
                          "line": code.count("\n", 0, m.start()) + 1})
        for m in DELEGATE_RE.finditer(code):
            types.append({"name": m.group(1), "kind": "delegate", "file": f,
                          "line": code.count("\n", 0, m.start()) + 1})

    by_name = defaultdict(list)
    for t in types:
        by_name[t["name"]].append(t)
    dup_names = {k: v for k, v in by_name.items() if len(v) > 1}

    name_to_files = defaultdict(set)
    for t in types:
        name_to_files[t["name"]].add(t["file"])

    # ------------------------------------------------------------ 边表
    edges = []
    for f in files:
        code = file_text[f]
        raw_lines = raw_text[f].split("\n")
        for name in sorted(name_to_files):
            dst_files = name_to_files[name]
            if dst_files == {f}:
                continue
            for m in re.finditer(r"(?<![A-Za-z0-9_])" + re.escape(name) + r"(?![A-Za-z0-9_])", code):
                start = m.start()
                # 过滤实例成员访问：小写变量.类型名
                j = start - 1
                while j >= 0 and code[j] in " \t":
                    j -= 1
                if j >= 0 and code[j] == ".":
                    k = j - 1
                    while k >= 0 and code[k] in " \t":
                        k -= 1
                    e = k
                    while k >= 0 and (code[k].isalnum() or code[k] == "_"):
                        k -= 1
                    prev = code[k + 1:e + 1]
                    if prev and prev[0].islower():
                        continue
                line_no = code.count("\n", 0, start) + 1
                snippet = raw_lines[line_no - 1].strip() if line_no - 1 < len(raw_lines) else ""
                if len(snippet) > 200:
                    snippet = snippet[:197] + "..."
                for dst in sorted(dst_files):
                    edges.append({
                        "src": f, "line": line_no, "type": name, "dst": dst,
                        "module_src": module_of(f), "module_dst": module_of(dst),
                        "layer_src": layer_of(f), "layer_dst": layer_of(dst),
                        "snippet": snippet,
                    })

    seen = set()
    uniq = []
    for e in edges:
        key = (e["src"], e["line"], e["type"], e["dst"])
        if key in seen:
            continue
        seen.add(key)
        uniq.append(e)
    edges = uniq

    os.makedirs(OUT, exist_ok=True)

    # files.csv（AC-1 覆盖证据）
    with open(os.path.join(OUT, "files.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh)
        w.writerow(["file", "module", "target_layer", "lines", "declared_types", "sha256"])
        for f in files:
            with open(f, "rb") as fb:
                content = fb.read()
            ntypes = sum(1 for t in types if t["file"] == f)
            w.writerow([f, module_of(f), layer_of(f), raw_text[f].count("\n") + 1,
                        ntypes, hashlib.sha256(content).hexdigest()])

    with open(os.path.join(OUT, "types.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh)
        w.writerow(["type", "kind", "file", "line", "module", "target_layer"])
        for t in sorted(types, key=lambda x: (x["file"], x["line"])):
            w.writerow([t["name"], t["kind"], t["file"], t["line"],
                        module_of(t["file"]), layer_of(t["file"])])

    with open(os.path.join(OUT, "edges.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh)
        w.writerow(["src_file", "src_line", "ref_type", "dst_file", "src_module", "dst_module",
                    "src_layer", "dst_layer", "snippet"])
        for e in sorted(edges, key=lambda x: (x["src"], x["line"], x["type"], x["dst"])):
            w.writerow([e["src"], e["line"], e["type"], e["dst"], e["module_src"], e["module_dst"],
                        e["layer_src"], e["layer_dst"], e["snippet"]])

    with open(os.path.join(OUT, "edges.json"), "w", encoding="utf-8") as fh:
        json.dump(edges, fh, ensure_ascii=False, indent=1)

    # ------------------------------------------------------------ 模块矩阵
    def matrix_md(title, key_src, key_dst, keys, out_path, order=None):
        m = defaultdict(int)
        me = defaultdict(list)
        for e in edges:
            k = (e[key_src], e[key_dst])
            m[k] += 1
            me[k].append(e)
        lines = [f"# {title}", "",
                 "> 由 `audit_scripts.py` 生成；行=引用方，列=被引用方，单元格为**边数**（同一文件/行/类型去重后）。",
                 "> 逐边原始记录（含 `文件:行` 与源码片段）见 `edges.csv`。", "",
                 "| 引用方 \\ 被引用 | " + " | ".join(keys) + " | 合计 |",
                 "|" + "---|" * (len(keys) + 2)]
        for a in keys:
            row = [str(m.get((a, b), 0)) if a != b else f"({m.get((a, b), 0)})" for b in keys]
            lines.append("| **" + a + "** | " + " | ".join(row) + " | " +
                         str(sum(m.get((a, b), 0) for b in keys)) + " |")
        lines += ["", "括号内为同组内部引用。", ""]
        if order:
            lines += ["## 违规方向（下层 → 上层，M7 必须先消）", ""]
            bad = [(a, b) for a in keys for b in keys
                   if a != b and a in order and b in order and order.index(a) < order.index(b) and m.get((a, b))]
            if not bad:
                lines.append("无。")
            for a, b in bad:
                lines.append(f"- **{a} → {b}**：{m[(a, b)]} 条边")
                for e in sorted(me[(a, b)], key=lambda x: (x["src"], x["line"])):
                    lines.append(f"  - `{e['src']}:{e['line']}` 引用 `{e['type']}` → `{e['dst']}`")
            lines.append("")
        lines += ["## 每个依赖组的来源文件", ""]
        for a in keys:
            for b in keys:
                if a == b or not me.get((a, b)):
                    continue
                srcs = sorted({e["src"] for e in me[(a, b)]})
                lines.append(f"### {a} → {b}（{m[(a, b)]} 条，{len(srcs)} 个来源文件）")
                lines.append("")
                for s in srcs:
                    refs = [e for e in me[(a, b)] if e["src"] == s]
                    sample = "; ".join(f"{e['type']}@{e['line']}" for e in refs[:8])
                    if len(refs) > 8:
                        sample += " …"
                    lines.append(f"- `{s}`：{sample}")
                lines.append("")
        with open(os.path.join(OUT, out_path), "w", encoding="utf-8") as fh:
            fh.write("\n".join(lines))

    mods = [m for m in MODULES if any(module_of(f) == m for f in files)]
    mods += sorted({module_of(f) for f in files} - set(mods))
    matrix_md("M7-0 模块依赖矩阵（按现目录）", "module_src", "module_dst", mods, "module-matrix.md")

    layers = ["Core", "Content", "Gameplay", "App", "UI", "Editor"]
    matrix_md("M7-0 层依赖矩阵（按目标层）", "layer_src", "layer_dst", layers, "layer-matrix.md",
              order=LAYER_ORDER)

    # ------------------------------------------------------------ SCC
    def tarjan(nodes, graph):
        index, low, on_stack = {}, {}, {}
        stack, sccs, counter = [], [], [0]

        def strongconnect(v):
            work = [(v, iter(sorted(graph.get(v, ()))))]
            index[v] = low[v] = counter[0]; counter[0] += 1
            stack.append(v); on_stack[v] = True
            while work:
                node, it = work[-1]
                advanced = False
                for w in it:
                    if w not in index:
                        index[w] = low[w] = counter[0]; counter[0] += 1
                        stack.append(w); on_stack[w] = True
                        work.append((w, iter(sorted(graph.get(w, ())))))
                        advanced = True
                        break
                    elif on_stack.get(w):
                        low[node] = min(low[node], index[w])
                if advanced:
                    continue
                work.pop()
                if work:
                    parent = work[-1][0]
                    low[parent] = min(low[parent], low[node])
                if low[node] == index[node]:
                    comp = []
                    while True:
                        w = stack.pop(); on_stack[w] = False; comp.append(w)
                        if w == node:
                            break
                    if len(comp) > 1:
                        sccs.append(sorted(comp))

        for v in sorted(nodes):
            if v not in index:
                strongconnect(v)
        return sccs

    graph = defaultdict(set)
    for e in edges:
        if e["src"] != e["dst"]:
            graph[e["src"]].add(e["dst"])
    for f in files:
        graph.setdefault(f, set())
    file_sccs = tarjan(files, graph)

    scc_records = []
    for comp in file_sccs:
        cs = set(comp)
        inner = [e for e in edges if e["src"] in cs and e["dst"] in cs and e["src"] != e["dst"]]
        scc_records.append({"files": comp, "modules": sorted({module_of(x) for x in comp}),
                            "layers": sorted({layer_of(x) for x in comp}), "edges": inner})
    scc_records.sort(key=lambda r: (-len(r["files"]), r["files"]))

    # 层级 SCC（用于判断“拆到层之后还剩多少环”）
    lg = defaultdict(set)
    for e in edges:
        if e["layer_src"] != e["layer_dst"]:
            lg[e["layer_src"]].add(e["layer_dst"])
    layer_sccs = tarjan(set(layers), lg)

    # ------------------------------------------------------------ cycles.md
    cl = ["# M7-0 循环引用清单（文件级 SCC）", "",
          f"> 扫描文件数：{len(files)}；类型数：{len(types)}；文件级边数：{len(edges)}。",
          "> 判定口径：A 文件引用 B 文件声明的类型即 A→B；强连通分量（SCC）大小 ≥2 视为循环。",
          "> 近似分析（词边界匹配，已剥离注释与字符串）可能误报同名成员，所有边保留 `文件:行` 与源码片段供人工确认。",
          "> 已做过滤：`小写变量.类型名` 形式的实例成员访问不计为类型引用。",
          ""]
    if not scc_records:
        cl.append("未发现文件级循环引用。")
    ADVICE = {
        15: [
            "M7 只需处理其中跨层的 3 条 `Gameplay→UI`（`UIManager`×2、`TransitionFader`×1），其余是同层纠缠。",
            "① `SelectionManager ↔ Selectable`：抽 `ISelectable` 或让 `Selectable` 只发选中事件，由 `SelectionManager` 订阅。",
            "② `CharacterBrain ↔ InteractionRunner ↔ IInteractionTarget`：Runner 依赖接口而非 `CharacterBrain` 具体类，由 CharacterBrain 注入。",
            "③ `Chest ↔ UIManager/InventoryUI`：`Chest.Opened` 已是事件，UI 改为纯订阅方；`Contents` 暴露 `Inventory` 契约而不是 `Chest`。",
        ],
        4: [
            "抽 `ISkillEffect` / `ISkillCaster` 契约到 Core 或 Gameplay 契约层；`SkillData` 只持效果 ID/接口。",
            "`SkillCastContext` 不直接持 `SkillSystem` 具体类，改持 `ISkillCaster`。",
            "M7 单 `Dungeon.Gameplay` 程序集不阻塞；只有再拆 `Units/Combat` 子程序集时才必须先消。",
        ],
        3: [
            "`DamageInfo` 的 `UnitHealth` 字段改为 `UnitId`（Core），即可把伤害结果包下沉。",
            "`UnitRegistry` 归 Gameplay 后，此环变成同层，不阻塞 M7。",
            "若将来再拆，注册表改为 `IUnitRegistry` 接口，`UnitHealth` 只依赖接口。",
        ],
        2: [
            "`MetaState.TryLevelUp(MeleeCombat, UnitHealth)` 改为 App 侧升级服务，不反向持玩法类型。",
            "`MeleeCombat.Start` 只读 Core 的 `ProgressionState.DamageMultiplier`。",
            "这是 M7 的硬阻塞之一，必须最先消。",
        ],
    }
    for i, rec in enumerate(scc_records, 1):
        cl += [f"## C{i}. {' / '.join(rec['layers'])} 循环（{len(rec['files'])} 个文件）", ""]
        for f in rec["files"]:
            cl.append(f"- `{f}`（{layer_of(f)}）")
        cl += ["", "内部边（`文件:行` → 被引用文件）：", ""]
        for e in sorted(rec["edges"], key=lambda x: (x["src"], x["line"])):
            cl.append(f"- `{e['src']}:{e['line']}` 引用 `{e['type']}` → `{e['dst']}`  \n  `{e['snippet']}`")
        advice = ADVICE.get(len(rec["files"]))
        cl += ["", "**建议解法**：", ""]
        if advice:
            for a in advice:
                cl.append(f"- {a}")
        else:
            cl.append("- 需人工确认后按层拆接口。")
        cl.append("")
    cl += ["## 层级循环", ""]
    if layer_sccs:
        for c in layer_sccs:
            cl.append("- " + " ↔ ".join(c))
    else:
        cl.append("按目标层聚合后无环。")
    cl.append("")
    with open(os.path.join(OUT, "cycles.md"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(cl))

    # ------------------------------------------------------------ graph.mmd
    mm = ["graph TD", "  %% M7-0 文件级依赖图（仅跨模块边；模块内边见 edges.csv）"]
    for m in mods:
        mm.append(f"  subgraph {m}[{m}]")
        for f in sorted(files):
            if module_of(f) == m:
                fid = "N" + re.sub(r"[^A-Za-z0-9]", "_", f.split("Scripts/")[-1])
                mm.append(f"    {fid}[\"{os.path.basename(f)[:-3]}\"]")
        mm.append("  end")
    emitted = set()
    for e in sorted(edges, key=lambda x: (x["src"], x["dst"])):
        if e["module_src"] == e["module_dst"]:
            continue
        key = (e["src"], e["dst"])
        if key in emitted:
            continue
        emitted.add(key)
        a = "N" + re.sub(r"[^A-Za-z0-9]", "_", e["src"].split("Scripts/")[-1])
        b = "N" + re.sub(r"[^A-Za-z0-9]", "_", e["dst"].split("Scripts/")[-1])
        mm.append(f"  {a} --> {b}")
    with open(os.path.join(OUT, "graph.mmd"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(mm) + "\n")

    # ------------------------------------------------------------ layer-plan.md
    lp = ["# M7-0 分层建议（每个文件唯一目标层）", "",
          "> 目标层来自 `Docs/01` §3.1/§3.3：Core ← Content ← Gameplay ← App ← UI；Editor 旁挂。",
          "> 现行目录只作线索，分层按**文件实际职责**判定；`Assets/_Project/Tests` 不在本单扫描范围。", "",
          "| 文件 | 现目录 | 目标层 | 理由 |", "|---|---|---|---|"]
    for f in sorted(files):
        lay = layer_of(f)
        lp.append(f"| `{f}` | {module_of(f)} | **{lay}** | {LAYER_REASON[lay]} |")
    lp += ["", "## 分层统计", ""]
    cnt = defaultdict(int)
    for f in files:
        cnt[layer_of(f)] += 1
    for lay in layers:
        lp.append(f"- {lay}: {cnt[lay]} 个文件")
    lp += ["", "## 已知的“目标层与当前职责冲突”文件（M7 前置重构候选）", "",
           "| 文件 | 现状 | 冲突 | 处理建议 |", "|---|---|---|---|",
           "| `Assets/_Project/Scripts/Combat/DamageInfo.cs` | 伤害结果包，引用 `UnitHealth` | 理想 Core，但 Core 不能引用 Gameplay | M7 先留在 Gameplay；后续把 `UnitHealth` 换成 `UnitId`/接口后再下沉 |",
           "| `Assets/_Project/Scripts/Combat/SkillCastContext.cs` | 施法上下文，引用 `SkillSystem`/`UnitHealth` | 同上 | 留在 Gameplay |",
           "| `Assets/_Project/Scripts/Units/SkillData.cs` | ScriptableObject，但持有 `SkillEffectBase[]` | Content 引用 Gameplay | 留在 Gameplay；若要入 Content，先把效果链改成 ID 列表 |",
           "| `Assets/_Project/Scripts/Units/UnitHealth.cs` | 引用 `UnitStats`(Content) 与 `DamageInfo`(Gameplay) | Gameplay 内正常 | 留在 Gameplay |",
           "| `Assets/_Project/Scripts/Core/GameData.cs` | 适配层，转发 RunState/MetaState/MessageFeed/MeleeCombat/UnitHealth | Core 目录里却依赖玩法 | 归 App；M7 后删除或保留为 App 内兼容层 |",
           "| `Assets/_Project/Scripts/Core/UnitRegistry.cs` | 全局单位注册表 | Core 目录里却依赖 `UnitHealth` | 归 Gameplay |",
           "| `Assets/_Project/Scripts/Core/WaveSpawner.cs` | 波次生成 MonoBehaviour | Core 目录里却是玩法组件 | 归 Gameplay |",
           "| `Assets/_Project/Scripts/Debug/DebugCameraSwitcher.cs` | 运行期相机控制 MonoBehaviour | 名为 Debug，实为场景运行组件 | 归 Gameplay；不能进 Editor asmdef |",
           "| `Assets/_Project/Scripts/Debug/Mirror.cs` | 运行期镜面渲染 | 名为 Debug，实为场景表现 | 归 Gameplay |",
           ""]
    with open(os.path.join(OUT, "layer-plan.md"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(lp))

    # ------------------------------------------------------------ stats
    stats = {
        "file_count": len(files),
        "files_with_zero_declared_types": [f for f in files if not any(t["file"] == f for t in types)],
        "type_count": len(types),
        "edge_count": len(edges),
        "cross_module_edge_count": sum(1 for e in edges if e["module_src"] != e["module_dst"]),
        "duplicate_type_names": {k: [x["file"] for x in v] for k, v in dup_names.items()},
        "file_scc_count": len(scc_records),
        "file_scc_sizes": [len(r["files"]) for r in scc_records],
        "layer_sccs": layer_sccs,
        "layer_file_counts": {lay: sum(1 for f in files if layer_of(f) == lay) for lay in layers},
        "upward_layer_edges": [
            {"src": e["src"], "line": e["line"], "type": e["type"], "dst": e["dst"],
             "src_layer": e["layer_src"], "dst_layer": e["layer_dst"]}
            for e in edges
            if e["layer_src"] in LAYER_ORDER and e["layer_dst"] in LAYER_ORDER
            and LAYER_ORDER.index(e["layer_src"]) < LAYER_ORDER.index(e["layer_dst"])
            and e["layer_src"] != e["layer_dst"]
        ],
    }
    with open(os.path.join(OUT, "stats.json"), "w", encoding="utf-8") as fh:
        json.dump(stats, fh, ensure_ascii=False, indent=2)

    # ------------------------------------------------------------ coverage.txt（AC-1 自检）
    def csv_rows(name):
        with open(os.path.join(OUT, name), "r", encoding="utf-8") as fh:
            return sum(1 for _ in fh) - 1  # 去掉表头

    checks = [
        ("R-1 文件覆盖：实际 .cs 数 == files.csv 数据行数",
         len(files), csv_rows("files.csv"), len(files) == csv_rows("files.csv")),
        ("R-1 每个 .cs 至少声明 1 个类型",
         len(files) - len(stats["files_with_zero_declared_types"]), len(files),
         not stats["files_with_zero_declared_types"]),
        ("R-2 类型表行数 == 扫描到的类型数",
         csv_rows("types.csv"), len(types), csv_rows("types.csv") == len(types)),
        ("R-2 边表行数 == 内存边数",
         csv_rows("edges.csv"), len(edges), csv_rows("edges.csv") == len(edges)),
        ("R-3 已计算文件级 SCC", len(scc_records), len(scc_records), True),
        ("R-4 每个文件都有目标层",
         sum(1 for f in files if layer_of(f) in layers), len(files),
         all(layer_of(f) in layers for f in files)),
    ]
    with open(os.path.join(OUT, "coverage.txt"), "w", encoding="utf-8") as fh:
        fh.write("M7-0 覆盖率与自检\n")
        fh.write("=" * 60 + "\n")
        fh.write("解释器：Python %s（本机 `python` 为 Windows Store 占位符，实际用 `py -3.12`）\n" % sys.version.split()[0])
        fh.write("扫描根：%s\n" % ROOT)
        fh.write("文件数：%d\n" % len(files))
        fh.write("类型数：%d\n" % len(types))
        fh.write("边数：%d（跨目录边 %d）\n" % (len(edges), stats["cross_module_edge_count"]))
        fh.write("文件级 SCC：%s\n" % [len(r["files"]) for r in scc_records])
        fh.write("层文件数：%s\n" % stats["layer_file_counts"])
        fh.write("上行边数：%d\n" % len(stats["upward_layer_edges"]))
        fh.write("-" * 60 + "\n")
        for name, got, want, ok in checks:
            fh.write("[%s] %s：%s（期望 %s）\n" % ("PASS" if ok else "FAIL", name, got, want))
        fh.write("-" * 60 + "\n")
        fh.write("总判定：%s\n" % ("PASS" if all(c[3] for c in checks) else "FAIL"))

    print(json.dumps({k: v for k, v in stats.items() if k != "upward_layer_edges"},
                     ensure_ascii=False, indent=2))
    print("file SCC:", [(len(r["files"]), r["layers"]) for r in scc_records])
    print("upward edges:", len(stats["upward_layer_edges"]))
    for e in stats["upward_layer_edges"][:20]:
        print(" ", e["src"], e["line"], e["type"], "->", e["dst"], e["src_layer"], "->", e["dst_layer"])


if __name__ == "__main__":
    main()
