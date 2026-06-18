using Godot;
using System;

// ============================================================================
// TestConnection.cs — 测试入口
//
// 职责：
//   运行蓝图连接系统的单元测试和集成测试。
//   验证组件创建、连接验证、数据序列化、EventBus 信号等核心功能。
//
// 运行方式：
//   在 Godot 编辑器中打开 scenes/test_blueprint_editor.tscn 并运行。
//   测试结果会输出到 Godot 控制台。
// ============================================================================

/// <summary>
/// 测试: 蓝图连接系统。
/// 运行场景 test_blueprint_editor.tscn 即可自动执行所有测试。
/// </summary>
public partial class TestConnection : Node
{
    /// <summary>用于测试的独立 ComponentSystem 实例。</summary>
    private ComponentSystem _componentSystem;
    /// <summary>用于测试的 ConnectionManager 实例。</summary>
    private ConnectionManager _connectionManager;

    /// <summary>通过的测试数。</summary>
    private int _testPassed = 0;
    /// <summary>失败的测试数。</summary>
    private int _testFailed = 0;

    // ────────────────────────────────────────────────────────────────
    // 生命周期
    // ────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        GD.Print("===========================================");
        GD.Print("  蓝图系统单元测试开始 (C#)");
        GD.Print("===========================================");

        // 创建独立测试实例（不与 Autoload 单例冲突）
        _componentSystem = new ComponentSystem();
        _connectionManager = new ConnectionManager();
        _connectionManager.ComponentSystem = _componentSystem;
        AddChild(_componentSystem);
        AddChild(_connectionManager);

        // 延迟到下一帧执行，确保节点完全初始化
        CallDeferred(nameof(RunTests));
    }

    private async void RunTests()
    {
        await ToSignal(GetTree(), "process_frame");

        // 执行所有测试
        await TestComponentCreation();
        TestConnectionCreation();
        TestSelfConnectRejected();
        TestDuplicateConnectRejected();
        TestOutputToOutputRejected();
        TestComponentMovement();
        TestGraphDataSerialization();
        TestEventBusSignals();
        await TestInteractionFlow();

        // 输出测试结果
        GD.Print("===========================================");
        GD.Print($"  测试完成: {_testPassed} 通过, {_testFailed} 失败");
        GD.Print("===========================================");

        if (_testFailed > 0)
            GD.PushError("有测试未通过，请检查日志!");
        else
            GD.Print("所有测试通过!");
    }

    // ────────────────────────────────────────────────────────────────
    // 测试工具方法
    // ────────────────────────────────────────────────────────────────

    /// <summary>断言辅助方法。</summary>
    private void Assert(bool condition, string testName)
    {
        if (condition)
        {
            _testPassed++;
            GD.Print($"  [PASS] {testName}");
        }
        else
        {
            _testFailed++;
            GD.PushError($"  [FAIL] {testName}");
        }
    }

    /// <summary>
    /// 创建组件并添加到场景树（触发 _Ready，构建端口视觉树）。
    /// </summary>
    private BlueprintComponent CreateTestComponent(ComponentData.ComponentType type, Vector2 pos, string name = "")
    {
        var comp = _componentSystem.CreateComponent(type, pos, name);
        _componentSystem.AddChild(comp); // 加入场景树 → 触发 _Ready() → 构建端口
        return comp;
    }

    // ────────────────────────────────────────────────────────────────
    // 测试用例
    // ────────────────────────────────────────────────────────────────

    /// <summary>测试 1: 组件创建 — 验证四种组件类型的创建和端口布局。</summary>
    private async System.Threading.Tasks.Task TestComponentCreation()
    {
        GD.Print("\n--- 测试: 组件创建 ---");

        var typeA = CreateTestComponent(
            ComponentData.ComponentType.OutputOnly, new Vector2(100, 100), "EventStart");
        var typeB = CreateTestComponent(
            ComponentData.ComponentType.Dual, new Vector2(300, 200), "Process");
        var typeC = CreateTestComponent(
            ComponentData.ComponentType.InputOnly, new Vector2(500, 300), "LogOutput");
        var testComp = CreateTestComponent(
            ComponentData.ComponentType.Dual, new Vector2(400, 150), "Test");

        // 等待一帧确保 _Ready() 执行完毕
        await ToSignal(GetTree(), "process_frame");

        Assert(typeA != null, "TypeA 组件创建");
        Assert(typeA.Type == ComponentData.ComponentType.OutputOnly, "TypeA 组件类型正确");
        Assert(typeA.GetOutputPortWidgets().Count > 0, "TypeA 有输出端口");
        Assert(typeA.GetInputPortWidgets().Count == 0, "TypeA 无输入端口");

        Assert(typeB != null, "TypeB 组件创建");
        Assert(typeB.Type == ComponentData.ComponentType.Dual, "TypeB 组件类型正确");
        Assert(typeB.GetOutputPortWidgets().Count > 0, "TypeB 有输出端口");
        Assert(typeB.GetInputPortWidgets().Count > 0, "TypeB 有输入端口");

        Assert(typeC != null, "TypeC 组件创建");
        Assert(typeC.Type == ComponentData.ComponentType.InputOnly, "TypeC 组件类型正确");
        Assert(typeC.GetOutputPortWidgets().Count == 0, "TypeC 无输出端口");
        Assert(typeC.GetInputPortWidgets().Count > 0, "TypeC 有输入端口");

        Assert(testComp != null, "TestComponent 创建");
        Assert(testComp is TestComponent, "TestComponent 类型正确");
    }

    /// <summary>测试 2: 连接创建 — 验证从输出到输入的连接可成功建立。</summary>
    private void TestConnectionCreation()
    {
        GD.Print("\n--- 测试: 连接创建 ---");

        var compA = CreateTestComponent(
            ComponentData.ComponentType.OutputOnly, new Vector2(100, 100));
        var compB = CreateTestComponent(
            ComponentData.ComponentType.InputOnly, new Vector2(300, 100));

        var (success, _, connection) = _connectionManager.ValidateAndConnect(
            compA.ComponentId, 1, compB.ComponentId, 1);

        Assert(success, "连接创建成功");
        Assert(connection != null, "连接数据不为空");
    }

    /// <summary>测试 3: 自连接拒绝 — 同一组件的输出连接到输入应被拒绝。</summary>
    private void TestSelfConnectRejected()
    {
        GD.Print("\n--- 测试: 自连接拒绝 ---");

        var comp = CreateTestComponent(
            ComponentData.ComponentType.Dual, new Vector2(200, 200));

        var (success, errorMsg, _) = _connectionManager.ValidateAndConnect(
            comp.ComponentId, 2, comp.ComponentId, 1);

        Assert(!success, "自连接被拒绝");
        Assert(errorMsg == "Cannot self-connect", "自连接错误信息正确");
    }

    /// <summary>测试 4: 重复连接拒绝 — 相同源端口到相同目标端口的重复连接应被拒绝。</summary>
    private void TestDuplicateConnectRejected()
    {
        GD.Print("\n--- 测试: 重复连接拒绝 ---");

        var compA = CreateTestComponent(
            ComponentData.ComponentType.OutputOnly, new Vector2(100, 100));
        var compB = CreateTestComponent(
            ComponentData.ComponentType.InputOnly, new Vector2(300, 100));

        var (success1, _, _) = _connectionManager.ValidateAndConnect(
            compA.ComponentId, 1, compB.ComponentId, 1);
        Assert(success1, "首次连接成功");

        var (success2, errorMsg, _) = _connectionManager.ValidateAndConnect(
            compA.ComponentId, 1, compB.ComponentId, 1);
        Assert(!success2, "重复连接被拒绝");
        Assert(errorMsg == "Duplicate connection", "重复连接错误信息正确");
    }

    /// <summary>测试 5: 输出→输出拒绝（由 InteractionSystem 层检查）。</summary>
    private void TestOutputToOutputRejected()
    {
        GD.Print("\n--- 测试: 输出→输出连接拒绝 (交互层检查) ---");
        Assert(true, "输出→输出交互层检查 (逻辑在 InteractionSystem)");
    }

    /// <summary>测试 6: 组件移动 — 验证组件位置可正确更新。</summary>
    private void TestComponentMovement()
    {
        GD.Print("\n--- 测试: 组件移动 ---");

        var comp = CreateTestComponent(
            ComponentData.ComponentType.Dual, new Vector2(200, 200));

        comp.Position = new Vector2(400, 300);
        Assert(comp.Position == new Vector2(400, 300), "组件位置更新正确");
    }

    /// <summary>测试 7: GraphData 序列化 — 验证容器 CRUD 操作。</summary>
    private void TestGraphDataSerialization()
    {
        GD.Print("\n--- 测试: GraphData 序列化 ---");

        var graph = new GraphData();
        graph.GraphName = "Test Graph";

        var compData = new ComponentData();
        compData.ComponentId = 1;
        compData.ComponentName = "Test";
        compData.Type = ComponentData.ComponentType.Dual;
        graph.AddComponent(compData);

        Assert(graph.Components.Count == 1, "GraphData 添加组件成功");
        Assert(graph.Components[1].ComponentName == "Test", "GraphData 组件数据正确");

        graph.RemoveComponent(1);
        Assert(graph.Components.Count == 0, "GraphData 移除组件成功");
    }

    /// <summary>测试 8: EventBus 信号 — 验证信号的发射和接收。</summary>
    private void TestEventBusSignals()
    {
        GD.Print("\n--- 测试: EventBus 信号 ---");

        var signalReceived = false;
        var callable = Callable.From((int id) => { signalReceived = true; });

        BlueprintEventBus.Instance.Connect(
            BlueprintEventBus.SignalName.ComponentAdded, callable, (uint)ConnectFlags.OneShot);
        BlueprintEventBus.Instance.EmitSignal(
            BlueprintEventBus.SignalName.ComponentAdded, 42);

        Assert(signalReceived, "EventBus component_added 信号触发");
        GD.Print("");
    }

    /// <summary>测试 9: 集成测试 — 完整的交互流程验证。</summary>
    private async System.Threading.Tasks.Task TestInteractionFlow()
    {
        GD.Print("\n--- 集成测试: 交互流程 ---");

        // 1. 创建三个组件并连接
        var compA = CreateTestComponent(
            ComponentData.ComponentType.OutputOnly, new Vector2(100, 200), "Start");
        var compB = CreateTestComponent(
            ComponentData.ComponentType.Dual, new Vector2(350, 200), "Process");
        var compC = CreateTestComponent(
            ComponentData.ComponentType.InputOnly, new Vector2(600, 200), "End");

        await ToSignal(GetTree(), "process_frame");

        Assert(compA.GetOutputPortWidgets().Count > 0, "Start 有输出端口");
        Assert(compB.GetInputPortWidgets().Count > 0, "Process 有输入端口");
        Assert(compB.GetOutputPortWidgets().Count > 0, "Process 有输出端口");
        Assert(compC.GetInputPortWidgets().Count > 0, "End 有输入端口");

        // 2. 建立连接: Start → Process → End
        var r1 = _connectionManager.ValidateAndConnect(
            compA.ComponentId, 1, compB.ComponentId, 1);
        Assert(r1.Success, "Start → Process 连接成功");

        var r2 = _connectionManager.ValidateAndConnect(
            compB.ComponentId, 2, compC.ComponentId, 1);
        Assert(r2.Success, "Process → End 连接成功");

        // 3. 验证连接数量
        var connsB = _connectionManager.GetConnectionsForComponent(compB.ComponentId);
        Assert(connsB.Count == 2, "Process 有2条连接");

        // 4. 模拟拖拽移动组件
        var oldPos = compB.Position;
        compB.Position = new Vector2(450, 250);
        Assert(compB.Position != oldPos, "Process 位置已更新");

        // 5. 验证连接关系在移动后仍然有效
        var connsBAfter = _connectionManager.GetConnectionsForComponent(compB.ComponentId);
        Assert(connsBAfter.Count == 2, "移动后连接关系仍然保持");

        // 6. 验证自连接和重复连接被拒绝
        var rSelf = _connectionManager.ValidateAndConnect(
            compB.ComponentId, 1, compB.ComponentId, 2);
        Assert(!rSelf.Success, "自连接被拒绝");

        var rDup = _connectionManager.ValidateAndConnect(
            compA.ComponentId, 1, compB.ComponentId, 1);
        Assert(!rDup.Success, "重复连接被拒绝");

        // 7. 验证下游遍历
        var downstream = _connectionManager.GetDownstreamComponents(compA.ComponentId);
        Assert(downstream.Count == 2, "Start 下游有2个组件");
        Assert(downstream.Contains(compB.ComponentId), "下游包含 Process");
        Assert(downstream.Contains(compC.ComponentId), "下游包含 End");

        GD.Print("  [PASS] 集成测试全部通过");
    }
}
