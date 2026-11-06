# M7-0 循环引用清单（文件级 SCC）

> 扫描文件数：78；类型数：91；文件级边数：535。
> 判定口径：A 文件引用 B 文件声明的类型即 A→B；强连通分量（SCC）大小 ≥2 视为循环。
> 近似分析（词边界匹配，已剥离注释与字符串）可能误报同名成员，所有边保留 `文件:行` 与源码片段供人工确认。
> 已做过滤：`小写变量.类型名` 形式的实例成员访问不计为类型引用。

## C1. App / Gameplay / UI 循环（15 个文件）

- `Assets/_Project/Scripts/App/RunFlow.cs`（App）
- `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/World/Interactable.cs`（Gameplay）
- `Assets/_Project/Scripts/UI/EscMenuPanel.cs`（UI）
- `Assets/_Project/Scripts/UI/InventoryUI.cs`（UI）
- `Assets/_Project/Scripts/UI/MainMenuPanel.cs`（UI）
- `Assets/_Project/Scripts/UI/UIManager.cs`（UI）

内部边（`文件:行` → 被引用文件）：

- `Assets/_Project/Scripts/App/RunFlow.cs:46` 引用 `DownedRescue` → `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`  
  `var dr = h.GetComponent<DownedRescue>();`
- `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs:32` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `bool IsAvailable(CharacterBrain unit);`
- `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs:35` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `float Radius(CharacterBrain unit);`
- `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs:38` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `void Interact(CharacterBrain unit);`
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs:28` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `public static float RadiusFor(Component target, CharacterBrain unit)`
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:16` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `static readonly List<IInteractionTarget> KeyTargets = new List<IInteractionTarget>();`
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:18` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `CharacterBrain _brain;`
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:19` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `IInteractionTarget _pending;`
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:22` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `public static InteractionRunner Ensure(CharacterBrain brain)`
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:32` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `public static void RegisterKeyTarget(IInteractionTarget target)`
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:39` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `public static void UnregisterKeyTarget(IInteractionTarget target)`
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:50` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `public void Order(IInteractionTarget target)`
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:98` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `IInteractionTarget NearestKeyTarget()`
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:102` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `IInteractionTarget best = null;`
- `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs:17` 引用 `SelectionManager` → `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`  
  `if (SelectionManager.instance != null)`
- `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs:18` 引用 `SelectionManager` → `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`  
  `SelectionManager.instance.Register(this);`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:22` 引用 `Selectable` → `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`  
  `readonly List<Selectable> _all = new List<Selectable>();`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:23` 引用 `Selectable` → `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`  
  `readonly List<Selectable> _selected = new List<Selectable>();`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:26` 引用 `Selectable` → `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`  
  `public Selectable Primary`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:34` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `CharacterBrain PrimaryBrain`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:39` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `return p != null ? p.GetComponentInParent<CharacterBrain>() : null;`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:64` 引用 `Selectable` → `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`  
  `var s = h.GetComponentInChildren<Selectable>();`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:69` 引用 `Selectable` → `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`  
  `public void Register(Selectable s)`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:80` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `public void ForEachSelectedBrain(System.Action<CharacterBrain> action)`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:84` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `var b = s.GetComponentInParent<CharacterBrain>();`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:117` 引用 `SkillInputController` → `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`  
  `if (Input.GetMouseButtonUp(0) && !_moved && !(SkillInputController.instance != null && SkillInputController.IsTargeting))`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:124` 引用 `InteractionRunner` → `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs`  
  `var runner = InteractionRunner.Ensure(primaryBrain);`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:155` 引用 `Selectable` → `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`  
  `Selectable sel = null;`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:157` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `IInteractionTarget inter = null;`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:163` 引用 `Selectable` → `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`  
  `var s = h.collider.GetComponentInParent<Selectable>();`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:171` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `inter = BestTarget(inter, h.collider.GetComponentInParent<IInteractionTarget>(), primaryBrain);`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:190` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `var brain = s.GetComponent<CharacterBrain>();`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:199` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `var brain = s.GetComponent<CharacterBrain>();`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:211` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `static IInteractionTarget BestTarget(IInteractionTarget best, IInteractionTarget cand, CharacterBrain unit)`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:211` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `static IInteractionTarget BestTarget(IInteractionTarget best, IInteractionTarget cand, CharacterBrain unit)`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:221` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `bool TryCommandRescue(IInteractionTarget target)`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:223` 引用 `InteractionPriority` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `if (target == null || target.Priority != InteractionPriority.Rescue) return false;`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:233` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `var brain = s.GetComponentInParent<CharacterBrain>();`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:241` 引用 `Selectable` → `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`  
  `void SingleSelect(Selectable s)`
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:248` 引用 `Selectable` → `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`  
  `void ToggleSelect(Selectable s)`
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs:49` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`  
  `if (UIManager.instance != null && UIManager.instance.IsInventoryOpen) return;`
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs:75` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `var brain = _pendingCaster.GetComponent<CharacterBrain>();`
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs:122` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `var brain = ss.GetComponent<CharacterBrain>();`
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs:169` 引用 `SelectionManager` → `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`  
  `if (SelectionManager.instance == null || SelectionManager.instance.Primary == null) return null;`
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs:170` 引用 `SelectionManager` → `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`  
  `return SelectionManager.instance.Primary.GetComponentInParent<SkillSystem>();`
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs:23` 引用 `InteractionRunner` → `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs`  
  `InteractionRunner _runner;`
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs:69` 引用 `Interactable` → `Assets/_Project/Scripts/Gameplay/World/Interactable.cs`  
  `public void CommandInteract(Interactable it)`
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs:76` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `public void CommandInteract(IInteractionTarget target)`
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs:79` 引用 `InteractionRunner` → `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs`  
  `if (_runner == null) _runner = InteractionRunner.Ensure(this);`
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs:114` 引用 `DownedRescue` → `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`  
  `DownedRescue.NotifyMoveCommand(gameObject);`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:18` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `public class DownedRescue : MonoBehaviour, IInteractionTarget`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:51` 引用 `Interactable` → `Assets/_Project/Scripts/Gameplay/World/Interactable.cs`  
  `Interactable _interact;`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:73` 引用 `Interactable` → `Assets/_Project/Scripts/Gameplay/World/Interactable.cs`  
  `_interact = GetComponent<Interactable>();`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:74` 引用 `Interactable` → `Assets/_Project/Scripts/Gameplay/World/Interactable.cs`  
  `if (_interact == null) _interact = GetComponentInParent<Interactable>();`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:88` 引用 `Selectable` → `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs`  
  `var sel = GetComponentInChildren<Selectable>();`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:107` 引用 `InteractionPriority` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `public InteractionPriority Priority { get { return InteractionPriority.Rescue; } }`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:111` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `public bool IsAvailable(CharacterBrain unit) { return IsDown; }`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:113` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `public float Radius(CharacterBrain unit) { return InteractionDefinition.RadiusFor(this, unit); }`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:113` 引用 `InteractionDefinition` → `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs`  
  `public float Radius(CharacterBrain unit) { return InteractionDefinition.RadiusFor(this, unit); }`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:116` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `public void Interact(CharacterBrain unit)`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:139` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `var rBrain = rescuer.GetComponent<CharacterBrain>();`
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:142` 引用 `InteractionRunner` → `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs`  
  `InteractionRunner.Ensure(rBrain).Order(this);`
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs:15` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `public class Chest : MonoBehaviour, IInteractionTarget`
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs:29` 引用 `Interactable` → `Assets/_Project/Scripts/Gameplay/World/Interactable.cs`  
  `Interactable _interact;`
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs:37` 引用 `Interactable` → `Assets/_Project/Scripts/Gameplay/World/Interactable.cs`  
  `_interact = GetComponent<Interactable>();`
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs:53` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `public void OpenBy(CharacterBrain unit)`
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs:66` 引用 `InteractionPriority` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `public InteractionPriority Priority { get { return InteractionPriority.Default; } }`
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs:70` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `public bool IsAvailable(CharacterBrain unit) { return !_opened; }`
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs:72` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `public float Radius(CharacterBrain unit) { return InteractionDefinition.RadiusFor(this, unit); }`
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs:72` 引用 `InteractionDefinition` → `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs`  
  `public float Radius(CharacterBrain unit) { return InteractionDefinition.RadiusFor(this, unit); }`
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs:75` 引用 `CharacterBrain` → `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`  
  `public void Interact(CharacterBrain unit) { OpenBy(unit); }`
- `Assets/_Project/Scripts/Gameplay/World/Interactable.cs:14` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `public IInteractionTarget Target()`
- `Assets/_Project/Scripts/Gameplay/World/Interactable.cs:16` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `var target = GetComponent<IInteractionTarget>();`
- `Assets/_Project/Scripts/Gameplay/World/Interactable.cs:17` 引用 `IInteractionTarget` → `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`  
  `return target != null ? target : GetComponentInParent<IInteractionTarget>();`
- `Assets/_Project/Scripts/UI/EscMenuPanel.cs:25` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`  
  `if (UIManager.instance != null) UIManager.instance.ClosePanel(this);`
- `Assets/_Project/Scripts/UI/InventoryUI.cs:29` 引用 `Chest` → `Assets/_Project/Scripts/Gameplay/World/Chest.cs`  
  `Chest _chest;`
- `Assets/_Project/Scripts/UI/InventoryUI.cs:52` 引用 `Chest` → `Assets/_Project/Scripts/Gameplay/World/Chest.cs`  
  `public void SetChestData(Inventory chestInv, Chest chest)`
- `Assets/_Project/Scripts/UI/MainMenuPanel.cs:24` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`  
  `if (UIManager.instance != null) UIManager.instance.ClosePanel(this);`
- `Assets/_Project/Scripts/UI/UIManager.cs:20` 引用 `InventoryUI` → `Assets/_Project/Scripts/UI/InventoryUI.cs`  
  `public InventoryUI inventoryPanel;`
- `Assets/_Project/Scripts/UI/UIManager.cs:26` 引用 `EscMenuPanel` → `Assets/_Project/Scripts/UI/EscMenuPanel.cs`  
  `public EscMenuPanel escMenuPanel;`
- `Assets/_Project/Scripts/UI/UIManager.cs:28` 引用 `MainMenuPanel` → `Assets/_Project/Scripts/UI/MainMenuPanel.cs`  
  `public MainMenuPanel mainMenuPanel;`
- `Assets/_Project/Scripts/UI/UIManager.cs:42` 引用 `Chest` → `Assets/_Project/Scripts/Gameplay/World/Chest.cs`  
  `Chest.Opened += OnChestOpened;`
- `Assets/_Project/Scripts/UI/UIManager.cs:47` 引用 `Chest` → `Assets/_Project/Scripts/Gameplay/World/Chest.cs`  
  `Chest.Opened -= OnChestOpened;`
- `Assets/_Project/Scripts/UI/UIManager.cs:51` 引用 `Chest` → `Assets/_Project/Scripts/Gameplay/World/Chest.cs`  
  `void OnChestOpened(Chest chest)`
- `Assets/_Project/Scripts/UI/UIManager.cs:66` 引用 `RunFlow` → `Assets/_Project/Scripts/App/RunFlow.cs`  
  `bool gameOn = AppFlow.GameStarted && !RunFlow.IsWiped;`
- `Assets/_Project/Scripts/UI/UIManager.cs:107` 引用 `EscMenuPanel` → `Assets/_Project/Scripts/UI/EscMenuPanel.cs`  
  `if (!(panel is EscMenuPanel) && !(panel is MainMenuPanel))`
- `Assets/_Project/Scripts/UI/UIManager.cs:107` 引用 `MainMenuPanel` → `Assets/_Project/Scripts/UI/MainMenuPanel.cs`  
  `if (!(panel is EscMenuPanel) && !(panel is MainMenuPanel))`
- `Assets/_Project/Scripts/UI/UIManager.cs:121` 引用 `Chest` → `Assets/_Project/Scripts/Gameplay/World/Chest.cs`  
  `public void ShowInventory(Inventory chestInv, Chest chest)`
- `Assets/_Project/Scripts/UI/UIManager.cs:124` 引用 `SelectionManager` → `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`  
  `if (chestInv == null && SelectionManager.instance != null && SelectionManager.instance.Primary != null)`
- `Assets/_Project/Scripts/UI/UIManager.cs:125` 引用 `SelectionManager` → `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`  
  `inventoryPanel.BindPlayer(SelectionManager.instance.Primary.GetComponentInParent<Inventory>());`

**建议解法**：

- M7 只需处理其中跨层的 3 条 `Gameplay→UI`（`UIManager`×2、`TransitionFader`×1），其余是同层纠缠。
- ① `SelectionManager ↔ Selectable`：抽 `ISelectable` 或让 `Selectable` 只发选中事件，由 `SelectionManager` 订阅。
- ② `CharacterBrain ↔ InteractionRunner ↔ IInteractionTarget`：Runner 依赖接口而非 `CharacterBrain` 具体类，由 CharacterBrain 注入。
- ③ `Chest ↔ UIManager/InventoryUI`：`Chest.Opened` 已是事件，UI 改为纯订阅方；`Contents` 暴露 `Inventory` 契约而不是 `Chest`。

## C2. Gameplay 循环（4 个文件）

- `Assets/_Project/Scripts/Gameplay/Combat/SkillCastContext.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectBase.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Units/SkillData.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs`（Gameplay）

内部边（`文件:行` → 被引用文件）：

- `Assets/_Project/Scripts/Gameplay/Combat/SkillCastContext.cs:21` 引用 `SkillSystem` → `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs`  
  `public readonly SkillSystem Caster;`
- `Assets/_Project/Scripts/Gameplay/Combat/SkillCastContext.cs:28` 引用 `SkillSystem` → `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs`  
  `SkillSystem caster, UnitHealth casterUnit, float speed)`
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectBase.cs:22` 引用 `SkillCastContext` → `Assets/_Project/Scripts/Gameplay/Combat/SkillCastContext.cs`  
  `public abstract bool CanCast(SkillCastContext ctx);`
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectBase.cs:23` 引用 `SkillCastContext` → `Assets/_Project/Scripts/Gameplay/Combat/SkillCastContext.cs`  
  `public abstract void Execute(SkillCastContext ctx);`
- `Assets/_Project/Scripts/Gameplay/Units/SkillData.cs:19` 引用 `SkillEffectBase` → `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectBase.cs`  
  `public SkillEffectBase[] effects;`
- `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs:19` 引用 `SkillData` → `Assets/_Project/Scripts/Gameplay/Units/SkillData.cs`  
  `public SkillData[] skills = new SkillData[4];`
- `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs:35` 引用 `SkillData` → `Assets/_Project/Scripts/Gameplay/Units/SkillData.cs`  
  `public SkillData GetSkill(int i) { return i >= 0 && i < skills.Length ? skills[i] : null; }`
- `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs:69` 引用 `SkillCastContext` → `Assets/_Project/Scripts/Gameplay/Combat/SkillCastContext.cs`  
  `SkillCastContext ContextFor(int index, SkillEffectBase effect, Transform target)`
- `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs:69` 引用 `SkillEffectBase` → `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectBase.cs`  
  `SkillCastContext ContextFor(int index, SkillEffectBase effect, Transform target)`
- `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs:75` 引用 `SkillCastContext` → `Assets/_Project/Scripts/Gameplay/Combat/SkillCastContext.cs`  
  `return new SkillCastContext(index, trigger, stateName, target, this,`

**建议解法**：

- 抽 `ISkillEffect` / `ISkillCaster` 契约到 Core 或 Gameplay 契约层；`SkillData` 只持效果 ID/接口。
- `SkillCastContext` 不直接持 `SkillSystem` 具体类，改持 `ISkillCaster`。
- M7 单 `Dungeon.Gameplay` 程序集不阻塞；只有再拆 `Units/Combat` 子程序集时才必须先消。

## C3. Gameplay 循环（3 个文件）

- `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`（Gameplay）
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs`（Gameplay）

内部边（`文件:行` → 被引用文件）：

- `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs:15` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public UnitHealth source;      // 来源（可为空）`
- `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs:16` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public UnitHealth target;`
- `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs:29` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public struct UnitDiedEvent { public UnitHealth unit; }`
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:24` 引用 `DamageInfo` → `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`  
  `public event System.Action<DamageInfo> OnHit;`
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:45` 引用 `UnitRegistry` → `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs`  
  `UnitRegistry.Register(this);`
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:48` 引用 `UnitRegistry` → `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs`  
  `void OnEnable() { UnitRegistry.SetActiveState(this, true); }`
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:51` 引用 `UnitRegistry` → `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs`  
  `void OnDisable() { UnitRegistry.SetActiveState(this, gameObject.activeInHierarchy); }`
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:53` 引用 `UnitRegistry` → `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs`  
  `void OnDestroy() { UnitRegistry.Unregister(this); }`
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:69` 引用 `DamageInfo` → `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`  
  `var info = new DamageInfo`
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:92` 引用 `DamageInfo` → `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`  
  `ApplyDamageInfo(new DamageInfo`
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:99` 引用 `DamageInfo` → `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`  
  `void ApplyDamageInfo(DamageInfo info, float final)`
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:107` 引用 `DamageDealtEvent` → `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`  
  `EventBus.Publish(new DamageDealtEvent { info = info });`
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:113` 引用 `UnitDiedEvent` → `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`  
  `EventBus.Publish(new UnitDiedEvent { unit = this });`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:29` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `static readonly List<UnitHealth> _units = new List<UnitHealth>();                  // 注册顺序；含未激活`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:30` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `static readonly Dictionary<UnitHealth, UnitId> _ids = new Dictionary<UnitHealth, UnitId>();`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:31` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `static readonly Dictionary<int, UnitHealth> _byId = new Dictionary<int, UnitHealth>();`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:32` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `static readonly HashSet<UnitHealth> _active = new HashSet<UnitHealth>();`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:35` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `static readonly List<UnitHealth> _viewAll = new List<UnitHealth>();                // 全部注册（含未激活）`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:36` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `static readonly List<UnitHealth> _viewActive = new List<UnitHealth>();             // 仅激活`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:37` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `static readonly List<UnitHealth> _viewPlayers = new List<UnitHealth>();            // 激活 + Player`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:38` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `static readonly List<UnitHealth> _viewMonsters = new List<UnitHealth>();           // 激活 + Monster`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:39` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `static readonly List<UnitHealth> _viewPlayersAny = new List<UnitHealth>();         // 含未激活 + Player`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:40` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `static readonly List<UnitHealth> _viewMonstersAny = new List<UnitHealth>();        // 含未激活 + Monster`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:77` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `var survivors = new List<UnitHealth>(_units.Count);`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:78` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `var survivorIds = new Dictionary<UnitHealth, UnitId>(_units.Count);`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:81` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `UnitHealth u = _units[i];`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:95` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `UnitHealth u = survivors[i];`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:114` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public static void Register(UnitHealth unit)`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:134` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public static void Unregister(UnitHealth unit)`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:142` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `UnitHealth current;`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:150` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public static void SetActiveState(UnitHealth unit, bool active)`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:174` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public static IReadOnlyList<UnitHealth> All(bool includeInactive = false)`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:181` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public static IReadOnlyList<UnitHealth> OfTeam(Team team, bool includeInactive = false)`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:189` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public static void InRadius(Vector3 center, float radius, List<UnitHealth> results,`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:196` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `IReadOnlyList<UnitHealth> source = includeInactive ? _viewAll : _viewActive;`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:200` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `UnitHealth u = source[i];`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:210` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public static UnitHealth Nearest(Vector3 center, float maxRange, Team team,`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:211` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `System.Func<UnitHealth, bool> filter = null)`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:215` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `IReadOnlyList<UnitHealth> source = team == Team.Player ? _viewPlayers : _viewMonsters;`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:216` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `UnitHealth best = null;`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:220` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `UnitHealth u = source[i];`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:230` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public static UnitHealth Find(UnitId id)`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:233` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `UnitHealth unit;`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:239` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `public static bool TryGetId(UnitHealth unit, out UnitId id)`
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs:263` 引用 `UnitHealth` → `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`  
  `UnitHealth u = _units[i];`

**建议解法**：

- `DamageInfo` 的 `UnitHealth` 字段改为 `UnitId`（Core），即可把伤害结果包下沉。
- `UnitRegistry` 归 Gameplay 后，此环变成同层，不阻塞 M7。
- 若将来再拆，注册表改为 `IUnitRegistry` 接口，`UnitHealth` 只依赖接口。

## 层级循环

- App ↔ Gameplay ↔ UI
