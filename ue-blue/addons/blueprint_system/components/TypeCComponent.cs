using Godot;

// ============================================================================
// TypeCComponent.cs — 输入专用组件
//
// 职责：
//   仅有输入端口，没有输出端口。典型用途：日志输出、结果返回、断点。
//
// 视觉特征：
//   Header 颜色: 橙红 #FF5722
//   边框颜色:   #D84315
// ============================================================================

/// <summary>输入专用组件 — 仅有输入端口。</summary>
public partial class TypeCComponent : BlueprintComponent
{
    public TypeCComponent()
    {
        Type = ComponentData.ComponentType.InputOnly;
        HeaderColor = new Color("#FF5722");
        BorderColor = new Color("#D84315");

        // 预设一个输入端口
        var inExec = new PortDefinition();
        inExec.PortId = 1;
        inExec.PortName = "In";
        inExec.Direction = PortDefinition.DirectionType.Input;
        inExec.PortType = "exec";

        _inputPorts.Clear();
        _outputPorts.Clear();
        _inputPorts.Add(inExec);
    }

    public override void _Ready() { base._Ready(); }
}
