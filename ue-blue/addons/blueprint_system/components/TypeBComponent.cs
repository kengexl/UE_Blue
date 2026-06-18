using Godot;

// ============================================================================
// TypeBComponent.cs — 双向组件（同时有输入和输出端口）
//
// 职责：
//   同时具备输入和输出端口。典型用途：函数调用、计算节点、条件分支。
//
// 视觉特征：
//   Header 颜色: 蓝色 #2196F3
//   边框颜色:   #1976D2
// ============================================================================

/// <summary>双向组件 — 同时有输入和输出端口。</summary>
public partial class TypeBComponent : BlueprintComponent
{
    public TypeBComponent()
    {
        Type = ComponentData.ComponentType.Dual;
        HeaderColor = new Color("#2196F3");
        BorderColor = new Color("#1976D2");

        // 预设输入端口
        var inExec = new PortDefinition();
        inExec.PortId = 1;
        inExec.PortName = "In";
        inExec.Direction = PortDefinition.DirectionType.Input;
        inExec.PortType = "exec";

        // 预设输出端口
        var outExec = new PortDefinition();
        outExec.PortId = 2;
        outExec.PortName = "Out";
        outExec.Direction = PortDefinition.DirectionType.Output;
        outExec.PortType = "exec";

        _inputPorts.Clear();
        _outputPorts.Clear();
        _inputPorts.Add(inExec);
        _outputPorts.Add(outExec);
    }

    public override void _Ready() { base._Ready(); }
}
