## Context

The generated OasisCity scene currently creates one `ReviewCamera` under `99_Review`. The migrated Unity development toolbox discovers editor panels through `ToolHubItemAttribute` and constructs `IToolHubPanel` instances. The requested workflow must remain editor-only, survive scene saves and builder regeneration, and must not alter runtime camera or input systems.

## Goals / Non-Goals

**Goals:**

- Make every camera in every loaded scene discoverable and inspectable from one toolbox panel.
- Persist camera groups through ordinary scene hierarchy parenting.
- Provide explicit, separate Scene View and Game View actions.
- Make Game View switching temporary and reliably restore the original enabled states.
- Give OasisCity a compact set of representative rendering-review viewpoints.
- Keep grouping mutations Undo-compatible.

**Non-Goals:**

- Replace the runtime `CameraModule` or Cinemachine.
- Add runtime camera hotkeys or player-facing camera selection.
- Persist thumbnails, camera sequences, animation, or render presets.
- Modify render-pipeline settings from the panel.

## Decisions

### Decision: hierarchy-backed groups

Groups are immediate children of a scene-local `CameraGroups` object, and cameras are children of those group objects. The panel derives group names from the hierarchy and reparents with `Undo.SetTransformParent`.

This was chosen over a runtime marker component because grouping is editor metadata, requires no runtime assembly, is visible without the tool, and naturally persists with the scene. Name-prefix parsing was rejected because renaming a camera should not silently change its group.

### Decision: discovery is live and scene-wide

The panel uses `Resources.FindObjectsOfTypeAll<Camera>()`, filters to valid loaded scene objects, includes inactive GameObjects, and sorts by scene, group, then hierarchy path. Results are cached and refreshed on hierarchy/Undo events or an explicit refresh action, avoiding repeated allocations during every GUI event.

Filtered scene/group sections and group popup arrays are also cached. They rebuild only when the hierarchy, selected scene, or search text changes; ordinary repaint/layout events iterate stable lists without LINQ grouping allocations.

### Decision: list rows expose Game View switching

Each camera row reserves a compact lens column and an inline `切换` button. The button invokes the same reversible Game View session as the selected-camera detail action, and the active temporary camera is highlighted as `当前`.

### Decision: Scene View preview copies transform and lens

The Scene View action aligns the active Scene View to the selected camera transform and copies perspective/orthographic size. It does not change camera enabled states and does not mutate the scene.

### Decision: Game View switching is a reversible editor session

On the first Game View switch, the panel snapshots every discovered camera's enabled state. Each switch disables all other scene cameras and enables the selected camera. A visible restore action, panel disable, and play-mode transition restore the snapshot. Repeated switches retain the original snapshot rather than overwriting it.

This was chosen over changing camera depth because multiple cameras can still composite, and over changing tags because `MainCamera` is unrelated to render selection and would affect runtime lookup semantics.

### Decision: OasisCity review viewpoints are builder-owned

`BuildReviewObjects` creates `CameraGroups` plus four purpose-oriented groups: `城市全景`, `街区构图`, `水体与边界`, and `建筑特写`. It creates fourteen review cameras with one overview camera enabled and tagged `MainCamera`. Six building close-ups derive their target and distance deterministically from named entries in `OasisCityLayout.json`, so map rebuilds preserve intentional building coverage.

## Risks / Trade-offs

- [Temporary Game View switch interrupted by assembly reload] → restore from `OnDisable` and `AssemblyReloadEvents.beforeAssemblyReload`; keep a visible restore button.
- [External code changes camera enabled states during a temporary session] → restoration intentionally returns to the pre-switch snapshot; the panel clearly displays that a temporary session is active.
- [Hierarchy groups with duplicate names] → group identity is scene-local and based on the first matching direct child; the panel prevents creation of an empty or duplicate group name.
- [Fixed OasisCity viewpoints drift after a major map-layout change] → keep viewpoints in the deterministic map builder and cover the expected hierarchy with an editor test/validation.

## Migration Plan

1. Add the toolbox panel; discovery requires no scene migration.
2. Update the OasisCity map builder to create the camera hierarchy.
3. Apply the same deterministic hierarchy to the current OasisCity scene through the builder or editor automation.
4. Rollback by reverting the editor panel and builder change; review cameras are isolated under `99_Review/CameraGroups`.

## Open Questions

None. The user selected hierarchy-backed groups and requested both preview modes.
