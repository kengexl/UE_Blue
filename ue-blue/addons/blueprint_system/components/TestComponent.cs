using Godot;

// ============================================================================
// TestComponent.cs — 测试组件
//
// 职责：
//   继承 TypeBComponent（双向组件），添加测试专用属性。
//   用于测试蓝图系统的创建、连接、拖拽等功能。
//
// 导出属性：
//   test_label  — 测试标签
//   test_value  — 测试整数值
// ============================================================================

/// <summary>测试组件 — 带自定义导出属性的双向组件。</summary>
[GlobalClass]
public partial class TestComponent : TypeBComponent
{
    /// <summary>测试标签文本。</summary>
    [Export] public string TestLabel { get; set; } = "Test Node";

    /// <summary>测试整数值。</summary>
    [Export] public int TestValue { get; set; } = 0;

    public TestComponent()
    {
        ComponentName = TestLabel;
    }

    public override void _Ready()
    {
        base._Ready();
        if (TitleLabel != null)
            TitleLabel.Text = ComponentName;
    }

    /// <summary>
    /// 处理逻辑。模拟一个节点的执行过程：接收输入，产生输出。
    /// </summary>
    public void Process()
    {
        GD.Print($"TestComponent[{TestLabel}] processed: value={TestValue}");
    }
}
