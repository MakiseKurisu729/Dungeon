# RESCUE-01 讲稿（AI 初稿，请所有者用自己的话改写）

> 一句话：**救援不再是「走过去自动生效」，而是「你下的一个命令」——它可以被取消，也必须被重新下达。**

---

## 1. 为什么「自动读条」必须改成「指令式」

老版本的救援条件是瞬时的：只要「主选中的队友」和「倒地的队友」距离小于 `rescueRange`（2.5m），`DownedRescue.Update()` 每帧都在检查，条件一满足就 `StartCoroutine` 开读条。

它有三个绕不过去的毛病：

1. **玩家没有表达意图的机会**。队友倒地以后，你只要从他旁边跑过去，系统就当你决定救他——想先去顶怪、先拉开距离，都没法拒绝。
2. **「路过」和「决定救」在数据上完全同形**。两者的状态完全一样（都在 2.5m 内），所以任何「打断」都无从谈起：打断了它下一帧又会自己开，因为条件还没变。老代码里打断只是让协程 `yield break`，而下一帧 `Update` 又重新 `StartCoroutine`——这就是「打断等于没打断」。
3. **和 UI 脱节**。玩家看不到「这个队友可以救」，只能靠撞运气走过去。

改成指令式以后，因果链变成单向的：**点击**这一个动作产生一个**持久的意图**（`CommandRescue` 记下 `_rescuer`），意图可以被两件事撤销（受击、移动指令），撤销之后不会自己回来——因为已经没有「每帧检查条件」这个发动机了。这就是 R-5/R-6 里「不自动重开」能成立的根本原因：不是加了个 if 判断，而是**删掉了自动触发的入口**。

顺带一个设计取舍：既然入口唯一，那「2 秒冷却」就不能做成「不许再下令」，否则玩家的第二次点击会被吞掉、看起来像失灵。所以冷却做成「**读条不许在这些秒内开始**」——`RescueRoutine` 会等冷却到期再开读条，指令不丢、但那一轮不会立刻重新出现交互动作。

## 2. hover 的优先级为什么放在 `CursorManager`，而不是 `SelectionManager`

`CursorManager` 与 `SelectionManager` 是**两件事的两个消费者**：一个回答「画哪个光标」，一个回答「点了之后干什么」。它们各自做一次射线分类，但如果救援的优先级只写在其中一个里，就会出现两种情况之一：

- 只写 `SelectionManager`：光标还是绿圈（`Select`），玩家以为点了是选人，结果人跑过去救了——**反馈和结果不一致**；
- 只写 `CursorManager`：光标变救援图标了，点下去却把倒地者选中了——**承诺了不做的事**。

所以两边都写，而且写的是**同一个条件**：`inter != null && DownedRescue.IsDown && 主选中是活着的玩家单位`。这个条件放在 `CursorManager.ResolveHover` 的返回序列里**必须排在 `if (selectable != null) return Hover.Select;` 之前**——因为倒地单位身上还挂着 `Selectable`（`OnDowned` 只是把选中圈关掉，没删组件），不抢在它前面就永远轮不到救援分支。`SelectionManager.HandleClick` 同理，必须排在 `if (sel != null)` 之前。

为什么不是把条件写进 `SelectionManager`，让 `CursorManager` 去问它？因为 `ResolveHover` 是纯查询（传一个 `Ray` 出去，返回一个枚举），测试可以直接构造射线断言；一旦让它去问「点击会干什么」，它就从查询变成了决策，也就没法在 batch/Play 里单点验证了。

**这个条件的另一半是「开关」**：`Interactable` 是常挂的静态标记，但站着的人 `IsDown == false`，同一套判定会落到原来的 `Select` 分支。所以「只有倒地时才出现救援图标」不是靠动态开关组件实现的，而是靠判定时读状态——比 `enabled` 开关可靠，因为 `GetComponentInParent` 连禁用组件也能拿到。

## 3. 读条循环为什么重发触发器，而不是改 clip 的 Loop

打开 `PlayerBase.controller` 能看到 `Interact` 这个状态的真实结构：它有一条**无条件的** `Interact → Locomotion` 过渡，`exitTime = 0.8077`。也就是说这个动作天生就是「播一次、播到八成、回站姿」的一次性动作。

两条路都能让它「一直播」：

| 做法 | 代价 |
|---|---|
| 把 clip 或状态的 Loop 打开 | 要改 `PlayerBase.controller` —— 高风险资产、所有用 `Interact` 的单位（宝箱、门、将来的机关）一起受影响，而且「读条时长 3s」和「动画多长」会被焊死：以后调 `rescueTime` 就得同步调动画速度 |
| 读条期间按需重发 `Interact` 触发器 | 只改代码：`Interact` 播完自然回 `Locomotion`，下一帧发现「不在 `Interact`」就再压一次，看起来就是循环。读条时长由 `rescueTime` 单独控制，动画长度可以随便换 |

选了后者，但踩了两个坑，都是同一类坑——**触发器是「排队」的，压多了会留下没人消费的残留**：

1. 在「已经进入 `Interact` 的 0.1s 过渡期」里又压一次，这次没人消费，等 `Hit_A` 播完回到 `Locomotion` 时它被消费——于是**打断之后还白播一轮交互动作**。修法：`PushInteract` 里判断「正在进入 `Interact`」就直接返回。
2. `CharacterBrain` 自己还有一条「走到交互落点 → 补播一次 `Interact`」的到达分支。读条是 2.5m 就开始、人还要走到 1.1m 才停，所以这条补播常常发生在读条开始之后；一旦此时已被打断，它就成了另一颗残留。修法：`CommandRescue` 里 `CommandInteract` 之后立刻 `CancelPendingInteract()`——**走位归你，动画归我**，读条动画只由 `DownedRescue` 一个源头推送。

这两条就是 AC-5/AC-6 第一轮红、第二轮绿的全部内容：`2s 窗口内 Interact 进入次数` 从 1 变成 0。

## 4. 两种打断为什么入口不同（受击走事件、移动走指令通知）

它们看起来都是「打断」，但**能观察到的信号来源完全不同**：

- **受击**：伤害可能来自任何方向、任何系统（近战、远程、技能、将来的陷阱），唯一可靠的汇聚点就是 `UnitHealth.OnHit`。这件事和「玩家想干什么」无关，只和「这个单位挨打了」有关，所以订阅事件最稳。注意订阅方是 `DownedRescue`（读条方）而不是 `CharacterBrain`（表现方）——`CharacterBrain` 只负责播 `Hit` 动作，它并不知道自己正在救谁。
- **移动指令**：这不是「发生了什么」，而是「玩家改主意了」。它只在**真正下达移动指令**的那一刻成立，入口是 `CharacterBrain.OnCommandMove`——它只被 `CommandMove` 调用，而受击位移（击退、动画位移）根本不经过它。如果去监听「位置变化」，那么被怪撞一下、或者 NavMesh 的微小位移都会误判成「玩家让他走」，救援会被莫名取消。

所以 `CharacterBrain.OnCommandMove` 末尾加一句 `DownedRescue.NotifyMoveCommand(gameObject)`，由 `DownedRescue` 反向遍历 `AllDowned` 找到「正在由这个人救援」的实例去取消。这里有个顺序陷阱：`CommandRescue` 内部也会走 `CommandInteract → CommandMove → OnCommandMove`，如果先记 `_rescuer` 再跑位，自己刚下的救援指令会立刻被自己这条移动通知取消。所以 `CommandRescue` 里是**先跑位、后记账**。

一句话总结这两条入口的分工：**受击是「世界对我做了什么」，用事件听；移动是「玩家对我做了什么」，在指令处拦。**

---

## 附：本次踩到的两个非代码问题（供答辩时提一句）

1. 工单假设「玩家预制体上已经挂了 `Interactable`」，实际没有（全场景 0 个），而 hover/点击/跑位三处都依赖它——这说明「按文档写代码」不够，得先核对资产现状。已请所有者确认后补上。
2. `interactCursor` 槽是空的，而且 cursor-pack 里只有 4 张图勾了 Read/Write（`CursorManager` 染色要读像素），其中三张已被默认/选择/攻击占用，剩下唯一可用的就是 `gauntlet_point`——「复用现有图标」这句话要落到具体资源上，是要看导入设置的。
