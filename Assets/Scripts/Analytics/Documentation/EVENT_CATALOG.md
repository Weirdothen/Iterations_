# Copy Paste — Telemetry Event Catalog
**Studio:** Kajo Games  
**Project:** Copy Paste (PC / Steam)  
**SDK:** GameAnalytics Unity SDK 8.3.1  
**Namespace:** `KajoGames.CopyPaste.Analytics`

---

## 1. Overview & Purpose

This catalog is the single source of truth for all telemetry events in **Copy Paste**. It establishes strict event definitions, parameters, trigger locations, and implementation statuses to prevent duplicate tracking, missing data, or schema drift across the engineering and design teams.

### Status Definitions
- **[Implemented]**: Currently active and firing in the game codebase.
- **[Suggested]**: Pre-defined in helper classes or planned for upcoming feature milestones.

---

## 2. Event Catalog Matrix

| Event ID / Hierarchy | GA Type | Category | Parameters / Fields | Trigger Location | Responsible Feature | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `combat:weapon_fired:machine_gun` | Design | Combat | `label` (weapon name) | `MachineGun.Shoot()` | Combat / Shooting | **[Implemented]** |
| `combat:damage_dealt:<weapon>` | Design | Combat | `value` (damage), `target` | Bullet collision handler | Combat / Health | **[Suggested]** |
| `gameplay:player_death:clone_collision` | Design | Gameplay | `value` (retries), `cause`, `level`, `retries` | `PlayerCollision.HandleContact()` | Player / Hazards | **[Implemented]** |
| `gameplay:clone_spawned:clone_<index>` | Design | Gameplay | `value` (duration), `clone_index`, `duration_sec` | `CloningSystem.RecordLoop()` | Core / Cloning | **[Suggested]** |
| `gameplay:clone_replay_finished:clone_<index>` | Design | Gameplay | `label` (clone id) | `ReplaySystem.OnLoopComplete()` | Core / Cloning | **[Suggested]** |
| `gameplay:pickup_collected:<type>` | Design | Gameplay | `value` (count), `current_count`, `total_required` | `PlayerCollision.HandleContact()` | Collectibles | **[Implemented]** |
| `gameplay:all_pickups_collected:<level>` | Design | Gameplay | `value` (total count), `level` | `PlayerCollision.HandleContact()` | Level Flow | **[Implemented]** |
| `gameplay:level_restart:<level>` | Design | Gameplay | `value` (retry count) | `GameManager.RestartLevel()` | Level Flow / Retries | **[Implemented]** |
| `gameplay:gate_opened:<gateId>` | Design | Gameplay | `label` (gate ID) | `Gate.OpenGate()` / `GateButton` | Puzzles / Gates | **[Suggested]** |
| `gameplay:gate_closed:<gateId>` | Design | Gameplay | `label` (gate ID) | `Gate.CloseGate()` / `GateButton` | Puzzles / Gates | **[Suggested]** |
| `progression:level_start:<level>` | Progression (Start) + Design | Progression | `attempt` (number) | `GameManager.HandleSceneLoaded()` | Game Flow / Progression | **[Implemented]** |
| `progression:level_complete:<level>` | Progression (Complete) + Design | Progression | `value` (time sec), `retries`, `score` | `GameManager.HandleWinTriggered()` | Game Flow / Progression | **[Implemented]** |
| `progression:level_fail:<level>` | Progression (Fail) + Design | Progression | `value` (time sec), `reason`, `retry` | `GameManager.HandleLoseTriggered()` | Game Flow / Progression | **[Implemented]** |
| `progression:all_levels_completed` | Design | Progression | `value` (total time sec), `total_retries` | `GameManager.AdvanceAfterWin()` | End Game / Campaign | **[Implemented]** |
| `tutorial:tutorial_start` | Progression (Start) + Design | Tutorial | None | `IntroFlowManager.Start()` | Onboarding / Tutorial | **[Suggested]** |
| `tutorial:tutorial_step:<step>` | Design | Tutorial | `value` (step index) | Tutorial Checkpoint Trigger | Onboarding / Tutorial | **[Suggested]** |
| `tutorial:tutorial_complete` | Progression (Complete) + Design | Tutorial | `value` (duration sec) | Tutorial Exit Trigger | Onboarding / Tutorial | **[Suggested]** |
| `ui:button_click:<screen>:<button>` | Design | UI | `screen`, `button` | `MainMenuManager`, Button click handlers | UI / Menus | **[Implemented]** |
| `ui:panel_opened:<panelName>` | Design | UI | `label` (panel name) | `MainMenuManager.ShowPanel()` | UI / Navigation | **[Implemented]** |
| `ui:panel_closed:<panelName>` | Design | UI | `label` (panel name) | UI modal close events | UI / Navigation | **[Suggested]** |
| `ui:multiplayer_flow:<action>` | Design | UI | `label` (host/join status) | `MainMenuManager.StartAsHost/Client()` | Multiplayer / Relay | **[Implemented]** |
| `settings:volume_changed:<channel>` | Design | Settings | `value` (normalized 0..1) | `VolumeSettings.SetVolume()` | Audio / Settings | **[Implemented]** |
| `settings:setting_changed:<setting>` | Design | Settings | `value` (setting string value) | Settings UI callbacks | Video / Gameplay Settings | **[Suggested]** |
| `economy:resource_source:<curr>:<item>` | Resource (Source) + Design | Economy | `value` (amount), `currency`, `itemType`, `itemId` | Reward / Pickup logic | In-game Economy | **[Suggested]** |
| `economy:resource_sink:<curr>:<item>` | Resource (Sink) + Design | Economy | `value` (amount), `currency`, `itemType`, `itemId` | Shop / Upgrade logic | In-game Economy | **[Suggested]** |
| `economy:item_purchased:<itemId>` | Resource (Sink) + Design | Economy | `value` (cost), `currency` | Shop confirmation | Customization / Skins | **[Suggested]** |
| `economy:pickup_reward:<type>` | Resource (Source) + Design | Economy | `value` (amount), `level` | End-of-level bonus tally | Collectibles / Economy | **[Suggested]** |

---

## 3. Parameter Guidelines

### Reserved Global Dimensions
1. **Build Version**: Managed by `GameAnalytics.SettingsGA.Build` (e.g. `1.0.0`).
2. **User ID / Steam ID**: Set automatically or via `GameAnalyticsManager.SetUserId(steamId)`.
3. **Session ID**: Generated natively by GameAnalytics desktop wrapper.

### Custom Fields (`customFields`)
Custom key-value pairs attached to events are exported in GameAnalytics Raw Data exports. 
- Keys must be concise alphanumeric strings with underscores (e.g. `retries`, `time_sec`, `cause`).
- Values must be primitive types (`string`, `int`, `float`, `bool`).
- Avoid storing high-frequency arrays, large JSON strings, or user personal data.

---

## 4. How to Propose a New Event

1. **Verify Uniqueness**: Check this catalog to ensure the interaction is not already tracked under another name.
2. **Select Domain Helper**: Check if the event fits under `GameplayAnalytics`, `UIAnalytics`, `ProgressionAnalytics`, or `EconomyAnalytics`.
3. **Submit Pull Request**:
   - Add the event implementation using the appropriate helper or `GameAnalyticsManager.Event(...)`.
   - Update this file (`EVENT_CATALOG.md`) changing status to **[Implemented]**.
   - Review with the lead programmer during code review.

