using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// 极简 JSON 序列化（仅覆盖断言所需类型），避免依赖第三方库。
/// 由原 <c>SPEC01_Json</c> 原样迁移：<c>float</c> 用 "R" 格式、NaN/Inf → null、
/// <c>Enum</c> → 字符串、<c>IDictionary&lt;string, object&gt;</c>/<c>IEnumerable</c> 递归、控制字符转义。
/// 证据字段格式依赖本类，任何数字/转义口径的变化都会破坏 QA-REFACTOR-01 的 AC-3，禁止改。
/// </summary>
public static class QaJson
{
    public static string Value(object v)
    {
        if (v == null) return "null";
        if (v is string s) return Quote(s);
        if (v is bool b) return b ? "true" : "false";
        if (v is int i) return i.ToString(CultureInfo.InvariantCulture);
        if (v is long l) return l.ToString(CultureInfo.InvariantCulture);
        if (v is short s16) return s16.ToString(CultureInfo.InvariantCulture);
        if (v is byte b8) return b8.ToString(CultureInfo.InvariantCulture);
        if (v is float f) return Number(f);
        if (v is double d) return Number(d);
        if (v is decimal m) return m.ToString(CultureInfo.InvariantCulture);
        if (v is Enum e) return Quote(e.ToString());
        if (v is Vector2 v2) return "{\"x\":" + Number(v2.x) + ",\"y\":" + Number(v2.y) + "}";
        if (v is Vector3 v3) return "{\"x\":" + Number(v3.x) + ",\"y\":" + Number(v3.y) + ",\"z\":" + Number(v3.z) + "}";
        if (v is IDictionary<string, object> dict)
        {
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append(Quote(kv.Key)).Append(":").Append(Value(kv.Value));
            }
            sb.Append("}");
            return sb.ToString();
        }
        if (v is IEnumerable list)
        {
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var item in list)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append(Value(item));
            }
            sb.Append("]");
            return sb.ToString();
        }
        return Quote(v.ToString());
    }

    /// <summary>按 (key, value, key, value …) 顺序构造有序数据字典（证据 data 字段的唯一构造方式）。</summary>
    public static Dictionary<string, object> Dict(params object[] kvs)
    {
        var d = new Dictionary<string, object>();
        for (int i = 0; i + 1 < kvs.Length; i += 2)
            d[(string)kvs[i]] = kvs[i + 1];
        return d;
    }

    /// <summary>坐标字典：{"x","y","z"}，序列化后与 Vector3 的 JSON 形状一致。</summary>
    public static Dictionary<string, object> V3(Vector3 v)
    {
        return new Dictionary<string, object> { { "x", v.x }, { "y", v.y }, { "z", v.z } };
    }

    static string Number(float f)
    {
        if (float.IsNaN(f) || float.IsInfinity(f)) return "null";
        return f.ToString("R", CultureInfo.InvariantCulture);
    }

    static string Number(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) return "null";
        return d.ToString("R", CultureInfo.InvariantCulture);
    }

    static string Quote(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append("\"");
        return sb.ToString();
    }
}
