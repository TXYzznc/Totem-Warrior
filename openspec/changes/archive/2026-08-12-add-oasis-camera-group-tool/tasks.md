## 1. Camera group toolbox

- [x] 1.1 Implement cached loaded-scene camera discovery and hierarchy-backed group metadata.
- [x] 1.2 Implement camera data UI, filtering, selection, group creation, and Undo-supported regrouping.
- [x] 1.3 Implement separate Scene View preview and reversible Game View temporary switching.

## 2. OasisCity review viewpoints

- [x] 2.1 Update the OasisCity builder to deterministically generate purpose-based camera groups and review cameras.
- [x] 2.2 Apply and save the generated camera hierarchy in the current OasisCity scene.

## 3. Verification

- [x] 3.1 Add EditMode coverage for discovery, grouping, temporary switching, restoration, and generated camera hierarchy.
- [x] 3.2 Compile editor assemblies, run targeted tests, and run relevant project diagnostics.
- [x] 3.3 Verify the OpenSpec implementation against proposal, design, specs, and completed tasks.

## 4. Inline switching, layout, and close-ups

- [x] 4.1 Add a per-row Game View switch and active-camera indication without requiring selection.
- [x] 4.2 Cache filtered/grouped list data and selected-scene group options to reduce IMGUI repaint allocations.
- [x] 4.3 Add a deterministic building-close-up group with six representative OasisCity viewpoints.
- [x] 4.4 Apply the expanded hierarchy to OasisCity and verify compilation, tests, scene health, and OpenSpec.
