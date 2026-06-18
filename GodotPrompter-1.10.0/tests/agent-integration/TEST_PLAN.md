# GodotPrompter Agent Integration Test Plan

Run these tests in a **fresh Claude Code session** with GodotPrompter installed.
Record results in `RESULTS.md` after each test.

---

## Category 1: Cold Start (Installation & Discovery)

### Test 1.1: Plugin loads

**Setup:** Fresh Claude Code session with GodotPrompter installed.

**Prompt:** "What Godot skills are available from GodotPrompter?"

**Expected:**
- Agent loads `using-godot-prompter` skill (or reads it)
- Lists skill categories: Core/Process, Architecture, Gameplay, UI, Multiplayer, Build, C#
- Mentions at least 10 specific skill names

**Pass criteria:** Agent shows awareness of the skill catalog, not generic Godot advice.

---

### Test 1.2: Skill content access

**Prompt:** "What does the state-machine skill cover? Show me the approaches."

**Expected:**
- Agent reads `skills/state-machine/SKILL.md`
- Describes 3 approaches: enum-based, node-based, resource-based
- Shows the comparison table from the skill

**Pass criteria:** Response matches skill content, not generic FSM knowledge.

---

### Test 1.3: Cross-reference navigation

**Prompt:** "The state-machine skill mentions related skills. What are they?"

**Expected:**
- Agent finds the Related Skills line: player-controller, ai-navigation, resource-pattern
- Can describe what each related skill covers

**Pass criteria:** Agent navigates cross-references correctly.

---

## Category 2: Skill Discovery (Open-Ended Prompts)

### Test 2.1: State machine request

**Prompt:** "I need to add a state machine to my player character in Godot 4."

**Expected skill:** `state-machine`

**Expected behavior:**
- Loads the skill (not generic advice)
- Asks about complexity to recommend enum vs node vs resource approach
- Shows GDScript example from the skill
- Mentions C# equivalent

**Pass criteria:** Uses skill content, not generic FSM tutorial.

---

### Test 2.2: Project setup request

**Prompt:** "I'm starting a new Godot 4.3 project. How should I organize it?"

**Expected skill:** `godot-project-setup`

**Expected behavior:**
- Shows the split layout directory structure from the skill
- Recommends autoloads (GameManager, EventBus)
- Shows .gitignore template

**Pass criteria:** Directory structure matches skill exactly.

---

### Test 2.3: Enemy AI request

**Prompt:** "I want enemies that patrol waypoints and chase the player when they get close."

**Expected skill:** `ai-navigation`

**Expected behavior:**
- Shows NavigationAgent2D setup
- Provides patrol pattern with waypoints
- Shows chase behavior with state transitions
- References state-machine skill for FSM integration

**Pass criteria:** Uses NavigationAgent2D (not custom pathfinding), shows patrol code from skill.

---

### Test 2.4: Save/load request

**Prompt:** "Help me set up a save/load system for my Godot game."

**Expected skill:** `save-load`

**Expected behavior:**
- Shows strategy comparison table (ConfigFile, JSON, Resource)
- Recommends JSON for game saves
- Shows SaveManager autoload pattern
- Mentions version migration

**Pass criteria:** Shows the comparison table, recommends JSON with reasoning.

---

### Test 2.5: Code review request

**Prompt:** "Review this GDScript for common Godot issues."

**Sample script to paste with the prompt:**

```gdscript
extends CharacterBody2D

var health = 100
var speed = 200

func _process(delta):
    var player = get_node("/root/Main/Player")
    if player:
        var dir = (player.position - position).normalized()
        position += dir * speed * delta

func take_damage(amount):
    health -= amount
    if health <= 0:
        get_parent().remove_child(self)
        queue_free()
```

**Expected skill:** `godot-code-review`

**Expected behavior:**
- Flags: untyped variables, using `_process` instead of `_physics_process` for movement
- Flags: hardcoded node path `/root/Main/Player` (use groups instead)
- Flags: `position +=` instead of `move_and_slide()` on CharacterBody2D
- Flags: `remove_child` before `queue_free` (unnecessary)
- Uses the checklist structure from the skill

**Pass criteria:** Finds at least 3 of the 4 issues, uses skill checklist format.

---

## Category 3: Full Workflow (End-to-End Build)

### Test 3.1: Project + Player

**Setup:** Empty directory, no existing Godot project.

**Prompt:** "Create a new Godot 4.3 project with a player that can move with WASD and attack with Space."

**Expected skills:** `godot-project-setup`, `player-controller`, `state-machine`

**Expected behavior:**
- Scaffolds project with directory structure from godot-project-setup
- Creates player with CharacterBody2D top-down movement from player-controller
- Adds FSM (idle/move/attack) from state-machine
- Sets up input actions

**Pass criteria:** All 3 skills used, project structure matches skill patterns.

---

### Test 3.2: Add Enemy

**Prompt:** "Add an enemy with patrol AI that chases the player and attacks."

**Expected skills:** `ai-navigation`, `state-machine`, `component-system`

**Expected behavior:**
- Creates enemy with NavigationAgent2D
- Uses patrol pattern from ai-navigation
- Adds FSM (idle/patrol/chase/attack) from state-machine
- Uses HitboxComponent/HurtboxComponent/HealthComponent from component-system

**Pass criteria:** Navigation-based patrol, component-based damage.

---

### Test 3.3: Add HUD

**Prompt:** "Add a health bar HUD that shows the player's health."

**Expected skills:** `hud-system`, `event-bus`

**Expected behavior:**
- Creates CanvasLayer HUD from hud-system
- Uses EventBus pattern from event-bus for health updates
- Health bar uses tween animation from hud-system

**Pass criteria:** CanvasLayer HUD, EventBus-driven updates.

---

### Test 3.4: Code Review

**Prompt:** "Review all the code we just wrote for Godot best practices."

**Expected skill:** `godot-code-review`

**Expected behavior:**
- Works through the skill's checklist sections
- Checks node architecture, style, performance, input, signals, resources
- Produces structured review output

**Pass criteria:** Uses checklist format from skill, not ad-hoc review.

---

### Test 3.5: Save/Load

**Prompt:** "Set up save/load for player position and health. F5 to save, F9 to load."

**Expected skill:** `save-load`

**Expected behavior:**
- Creates SaveManager autoload with JSON serialization
- Implements save_game/load_game functions
- Wires to input actions
- Includes version migration pattern

**Pass criteria:** JSON save with version field, matches skill's SaveManager pattern.

---

## How to Run

1. Start a fresh Claude Code session
2. Install GodotPrompter: `claude plugins add ./GodotPrompter`
3. Navigate to an empty test directory
4. Run each test sequentially, recording results in RESULTS.md
5. For Category 3, keep the same session (tests build on each other)
