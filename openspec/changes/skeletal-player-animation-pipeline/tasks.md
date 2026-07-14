## 1. Contracts and legacy protection

- [x] 1.1 Record the isolated Transform-cutout design and the no-deletion requirement in the OpenSpec change.
- [x] 1.2 Correct the legacy M02 import tool to target the standard controller path.
- [x] 1.3 Add an automated legacy-resource guard that verifies the original frame controller and clips were not changed by skeletal imports.

## 2. Layered character art

- [ ] 2.1 Produce M02-consistent, transparent, four-direction skeletal part source art with joint overlap margins.
- [ ] 2.2 Import and configure each direction's required parts under the isolated Skeletal Sprite directory.
- [ ] 2.3 Validate art proportions, bare-skin tattoo regions and part completeness for all directions.

## 3. Skeletal runtime assets

- [x] 3.1 Implement the stable M02 bone hierarchy and six tattoo anchors with fixed local mask bounds.
- [x] 3.2 Create a skeletal preview Prefab that uses only layered part Sprites and Transform bones.
- [ ] 3.3 Create the isolated skeletal Animator Controller and initial four-direction Idle/Walk clips.
- [ ] 3.4 Add low-amplitude Active, Hit, Roll, Sprint and Death clips on the same skeleton.

## 4. Validation and handoff

- [x] 4.1 Add an editor validation/import utility for the skeletal preview and legacy protection contract.
- [ ] 4.2 Compile and validate the preview hierarchy, controller parameters, four directions and six tattoo anchors in Unity.
- [ ] 4.3 Leave the production Player, SmartAI and LightAI Prefabs on the legacy frame controller; document the explicit future switch point.
