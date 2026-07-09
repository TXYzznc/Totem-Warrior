# gf-x-business-runtime Specification

## ADDED Requirements

### Requirement: GF_X must own the default business startup path

The project SHALL use `Assets/Game/Scene/Launch.unity` as the default Unity startup scene and SHALL enter Totem Warrior business runtime through GF_X Procedure flow.

#### Scenario: GF_X launch reaches business procedure

- **GIVEN** the project is opened or run from default BuildSettings
- **WHEN** GF_X completes built-in preload
- **THEN** `WorkspaceProcedure` SHALL transition to a Totem Warrior business procedure or equivalent GF_X-owned business entry
- **AND** startup SHALL NOT stop at an empty workspace state.

#### Scenario: Legacy scenes are not default runtime entries

- **GIVEN** legacy scenes have been moved to `LegacyProjectArchive/Assets/Scenes`
- **WHEN** BuildSettings are inspected
- **THEN** `Assets/Game/Scene/Launch.unity` SHALL be the only enabled default startup scene.
- **AND** `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/Launch.unity`, and `Assets/Scenes/SampleScene.unity` SHALL NOT be enabled.

#### Scenario: Legacy active asset roots are archived

- **GIVEN** the project workspace is inspected after GF_X migration
- **WHEN** active Unity asset roots are checked
- **THEN** old `Assets/Scenes`, `Assets/Editor`, `Assets/Tools`, `Assets/TutorialInfo`, `Assets/Screenshots`, and `Assets/TestResults` SHALL NOT exist as active Unity asset folders.
- **AND** historical scenes and old editor tools SHALL remain available under `LegacyProjectArchive/Assets`.
- **AND** historical playtest screenshots and test results SHALL remain available under `tools/playtest`.

### Requirement: Legacy business effects must be preserved during refactor

The refactor SHALL preserve existing implemented player-visible and systemic effects unless a later confirmed design decision removes them.

#### Scenario: Existing systems are tracked

- **GIVEN** the old business modules under `Assets/Scripts/Modules`
- **WHEN** the refactor plan is inspected
- **THEN** MainMenu, CharacterSelect, StartupSelect, CombatHUD, Tattoo, Combat, Weapon, Skill, Spawner, MapGen, Camera, Economy, NPC, Event, Bot, Audio, VFX, Save and Settings SHALL be listed with migration status.

### Requirement: Business runtime must expose diagnostics

The business runtime SHALL provide machine-readable diagnostics for startup state, loaded data, loaded UI forms, active GF_X services and recent errors.

#### Scenario: Startup diagnostic report

- **GIVEN** the GF_X business runtime has started
- **WHEN** the diagnostics runner executes
- **THEN** it SHALL include whether business runtime started, which startup procedure is active, which GF_X-native services are active, and whether critical UI/config resources are available.
- **AND** it SHALL report a failure if the GF_X business runtime directly mounts old `GameApp`, `ModuleRunner`, `EventBus`, `UIModule`, or old `DataTableModule` as a runtime dependency.

### Requirement: Input access remains centralized

All player keyboard/mouse gameplay input SHALL go through `InputModule` or its GF_X successor service.

#### Scenario: No direct gameplay input bypass

- **GIVEN** gameplay scripts are scanned
- **WHEN** direct `Input.Get*` use is found outside the input service and test driver boundary
- **THEN** the scan SHALL fail or report a migration warning.

### Requirement: DataTable migration must be explicit

The project SHALL maintain a manifest for all old business JSON data tables and their GF_X migration status.

#### Scenario: Archived legacy JSON table inventory

- **GIVEN** files under `LegacyProjectArchive/Assets/Resources/DataTable/*.json`
- **WHEN** the migration manifest is generated or inspected
- **THEN** every table SHALL be listed with old path, C# type, primary key, owning module, GF_X target path and migration status.
- **AND** `Assets/Resources/DataTable` SHALL NOT exist in the active Unity Resources path.

### Requirement: Automated non-UI verification must run after migration slices

Every migration slice SHALL run non-UI automated verification or document why a verifier is temporarily unavailable.

#### Scenario: Verification evidence

- **GIVEN** a migration slice changes startup, config, UI lifecycle, resources, or gameplay logic
- **WHEN** the slice is marked complete
- **THEN** compile, GF_X diagnostics, AI DataTable validation, and relevant tests or diagnostic scenarios SHALL have recorded evidence.
