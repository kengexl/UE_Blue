using Godot;

// ============================================================================
// TypeAComponent.cs — 输出专用组件
//
// 职责：
//   仅有输出端口，没有输入端口。典型用途：事件起点、定时器触发。
//
// 视觉特征：
//   Header 颜色: 绿色 #4CAF50
//   边框颜色:   #388E3C
// ============================================================================

/// <summary>输出专用组件 — 仅有输出端口。</summary>
public partial class TypeAComponent : BlueprintComponent
{
    public TypeAComponent()
    {
        Type = ComponentData.ComponentType.OutputOnly;
        HeaderColor = new Color("#4CAF50");
        BorderColor = new Color("#388E3C");

        // 预设一个输出端口
        var outExec = new PortDefinition();
        outExec.PortId = 1;
        outExec.PortName = "Out";
        outExec.Direction = PortDefinition.DirectionType.Output;
        outExec.PortType = "exec";

        _inputPorts.Clear();
        _outputPorts.Clear();
        _outputPorts.Add(outExec);
    }

    public override void _Ready() { base._Ready(); }
}
