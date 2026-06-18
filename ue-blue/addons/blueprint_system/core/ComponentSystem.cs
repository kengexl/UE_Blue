using Godot;
using System.Collections.Generic;

// ============================================================================
// ComponentSystem.cs — 组件系统控制器（Autoload 单例）
//
// 职责：
//   管理所有蓝图组件的注册、查询和生命周期。
//   使用自增 ID 分配组件唯一标识。
//   提供工厂方法按类型创建组件实例。
//
// 注册方式：
//   通过 project.godot 的 [autoload] 节注册为单例，
//   运行时通过 ComponentSystem.Instance 访问。
// ============================================================================

/// <summary>
/// 组件系统控制器 — Autoload 单例。
/// 管理蓝图组件的注册、查找、创建和销毁。
/// </summary>
public partial class ComponentSystem : Node
{
    // ────────────────────────────────────────────────────────────────
    // 单例
    // ────────────────────────────────────────────────────────────────

    /// <summary>全局单例引用。</summary>
    public static ComponentSystem Instance { get; private set; }

    public override void _EnterTree() { Instance = this; }
    public override void _ExitTree() { if (Instance == this) Instance = null; }

    // ────────────────────────────────────────────────────────────────
    // 信号
    // ────────────────────────────────────────────────────────────────

    /// <summary>组件注册时发射。</summary>
    [Signal] public delegate void ComponentRegisteredEventHandler(int componentId);
    /// <summary>组件注销时发射。</summary>
    [Signal] public delegate void ComponentUnregisteredEventHandler(int componentId);

    // ────────────────────────────────────────────────────────────────
    // 内部状态
    // ────────────────────────────────────────────────────────────────

    /// <summary>已注册的组件字典，以 componentId 为键。</summary>
    private Dictionary<int, BlueprintComponent> _components = new();
    /// <summary>下一个可用的组件 ID（自增）。</summary>
    private int _nextComponentId = 1;

    /// <summary>
    /// Test 节点自动命名计数器（持续递增，不重置）。
    /// 格式: test(1), test(2), test(3), ...
    /// </summary>
    private static int _testNodeCounter = 1;

    // ────────────────────────────────────────────────────────────────
    // CRUD API
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 注册组件。自动分配唯一 ID 并触发 ComponentRegistered 信号。
    /// </summary>
    public bool RegisterComponent(BlueprintComponent component)
    {
        component.ComponentId = _nextComponentId;
        _nextComponentId++;
        _components[component.ComponentId] = component;
        EmitSignal(SignalName.ComponentRegistered, component.ComponentId);
        return true;
    }

    /// <summary>
    /// 注销组件。从场景树移除并触发 ComponentUnregistered 信号。
    /// </summary>
    public void UnregisterComponent(int componentId)
    {
        if (_components.TryGetValue(componentId, out var comp))
        {
            comp.QueueFree();
            _components.Remove(componentId);
            EmitSignal(SignalName.ComponentUnregistered, componentId);
        }
    }

    /// <summary>根据 ID 获取组件。</summary>
    public BlueprintComponent GetComponent(int componentId)
    {
        _components.TryGetValue(componentId, out var comp);
        return comp;
    }

    /// <summary>获取所有已注册的组件。</summary>
    public List<BlueprintComponent> GetAllComponents()
    {
        return new List<BlueprintComponent>(_components.Values);
    }

    /// <summary>
    /// 工厂方法：按类型创建组件并注册到系统。
    /// Test 组件自动命名 test(1), test(2), ...（持续递增）。
    /// </summary>
    /// <param name="type">组件类型（OutputOnly/Dual/InputOnly）。</param>
    /// <param name="position">画布上的初始位置。</param>
    /// <param name="name">可选的自定义名称。留空则自动生成。</param>
    /// <returns>创建的组件实例。</returns>
    public BlueprintComponent CreateComponent(ComponentData.ComponentType type, Vector2 position, string name = "")
    {
        BlueprintComponent comp = type switch
        {
            ComponentData.ComponentType.OutputOnly => new TypeAComponent(),
            ComponentData.ComponentType.Dual => new TestComponent(),
            ComponentData.ComponentType.InputOnly => new TypeCComponent(),
            _ => new TestComponent()
        };

        // 自动命名：Test 节点用 test(序号)，其他不覆盖
        if (!string.IsNullOrEmpty(name))
        {
            comp.ComponentName = name;
        }
        else if (comp is TestComponent || type == ComponentData.ComponentType.Dual)
        {
            comp.ComponentName = $"test({_testNodeCounter})";
            _testNodeCounter++;
        }

        comp.Position = position;
        RegisterComponent(comp);
        return comp;
    }
}
