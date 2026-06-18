using Godot;
using System.Collections.Generic;

// ============================================================================
// BlueprintNodeFactory.cs — 蓝图节点工厂
//
// 职责：
//   提供简化的 API 来创建自定义蓝图节点。
//   支持指定节点名称、In/Out 端口数量和名称、节点颜色等。
//   所有节点类型封装为统一的 DummyComponent。
//   用法与 UE 蓝图的节点创建流程一致：指定端口 → 连接 → 交互。
//
// 示例（在 BlueprintEditor._Ready 或测试代码中）：
//   var factory = new BlueprintNodeFactory(componentSystem);
//   var node = factory.CreateNode("Multiply", new[]{"A","B"}, new[]{"Result"});
//   editorCanvas.ComponentLayer.AddChild(node);
// ============================================================================

/// <summary>
/// 蓝图节点工厂 — 创建自定义蓝图节点的便捷 API。
/// </summary>
public class BlueprintNodeFactory
{
    private ComponentSystem _componentSystem;

    public BlueprintNodeFactory(ComponentSystem system)
    {
        _componentSystem = system;
    }

    /// <summary>
    /// 创建一个自定义蓝图节点。
    /// </summary>
    /// <param name="nodeName">节点名称，如 "Branch", "Multiply"。</param>
    /// <param name="inputPorts">输入端口名称列表，如 new[]{"A", "B", "Condition"}。</param>
    /// <param name="outputPorts">输出端口名称列表，如 new[]{"Result", "Then"}。</param>
    /// <param name="position">画布上的位置。</param>
    /// <param name="headerColor">标题栏颜色（可选）。</param>
    /// <returns>创建的 BlueprintComponent 实例。</returns>
    public BlueprintComponent CreateNode(
        string nodeName,
        string[] inputPorts,
        string[] outputPorts,
        Vector2 position = default,
        Color? headerColor = null)
    {
        var comp = new DummyComponent();
        comp.ComponentName = nodeName;

        if (headerColor.HasValue)
        {
            comp.HeaderColor = headerColor.Value;
            comp.BorderColor = new Color(
                headerColor.Value.R * 0.7f,
                headerColor.Value.G * 0.7f,
                headerColor.Value.B * 0.7f);
        }

        // 设置端口
        comp.SetInputPorts(inputPorts);
        comp.SetOutputPorts(outputPorts);

        comp.Position = position;

        // 注册到组件系统（获取 ID）+ 不加入场景树（由调用方决定）
        _componentSystem.RegisterComponent(comp);

        return comp;
    }

    /// <summary>
    /// 设置输入端口并注册到 ComponentSystem。
    /// </summary>
    public void SetInputPorts(BlueprintComponent comp, string[] portNames)
    {
        comp.SetInputPorts(portNames);
    }

    /// <summary>
    /// 设置输出端口并注册到 ComponentSystem。
    /// </summary>
    public void SetOutputPorts(BlueprintComponent comp, string[] portNames)
    {
        comp.SetOutputPorts(portNames);
    }

    /// <summary>
    /// 快速连接两个端口。
    /// </summary>
    public static bool Connect(
        ConnectionManager connectionManager,
        BlueprintComponent source, int srcPortId,
        BlueprintComponent target, int tgtPortId)
    {
        var result = connectionManager.ValidateAndConnect(
            source.ComponentId, srcPortId,
            target.ComponentId, tgtPortId);
        return result.Success;
    }
}

// ============================================================================
// DummyComponent.cs — 通用蓝图组件
//
// 职责：
//   一个没有预设端口的空白组件模板。
//   所有端口通过 BlueprintComponent.AddInputPort / AddOutputPort 动态添加。
//   配合 BlueprintNodeFactory 创建任意端口配置的节点。
//
// 视觉特征：
//   Header 颜色: 可通过属性设置（默认蓝色 #2196F3）
// ============================================================================

/// <summary>通用蓝图组件 — 空模板，端口动态添加。</summary>
public partial class DummyComponent : BlueprintComponent
{
    public DummyComponent()
    {
        Type = ComponentData.ComponentType.Dual;
        HeaderColor = new Color("#2196F3");
        BorderColor = new Color("#1976D2");

        _inputPorts.Clear();
        _outputPorts.Clear();
    }

    public override void _Ready() { base._Ready(); }
}

// ============================================================================
// 扩展方法集合 — BlueprintComponent 扩展
// ============================================================================

/// <summary>BlueprintComponent 的扩展方法。</summary>
public static class BlueprintComponentExtensions
{
    /// <summary>
    /// 批量设置输入端口。清空现有端口后按名称列表添加。
    /// </summary>
    public static void SetInputPorts(this BlueprintComponent comp, string[] portNames)
    {
        var names = new (string name, string type)[portNames.Length];
        for (int i = 0; i < portNames.Length; i++)
            names[i] = (portNames[i], "exec");
        comp.SetInputPorts(names);
    }

    /// <summary>
    /// 批量设置输出端口。清空现有端口后按名称列表添加。
    /// </summary>
    public static void SetOutputPorts(this BlueprintComponent comp, string[] portNames)
    {
        var names = new (string name, string type)[portNames.Length];
        for (int i = 0; i < portNames.Length; i++)
            names[i] = (portNames[i], "exec");
        comp.SetOutputPorts(names);
    }

    /// <summary>获取输入端口数量。</summary>
    public static int InputPortCount(this BlueprintComponent comp)
        => comp.GetInputPortDefs().Count;

    /// <summary>获取输出端口数量。</summary>
    public static int OutputPortCount(this BlueprintComponent comp)
        => comp.GetOutputPortDefs().Count;
}
