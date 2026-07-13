#if UNITY_EDITOR
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemActorVisualDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Actor Visual Runtime";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckVisualHelperContracts(context);
            CheckActorServiceVisualAttachment(context);
            context.Pass("Totem actor visual runtime contract is ready.");
        }

        private static void CheckVisualHelperContracts(GFDiagnosticScenarioContext context)
        {
            var root = new GameObject("[TotemActorVisualHelperDiagnostic]");
            try
            {
                var spriteGo = new GameObject("BodySprite");
                spriteGo.transform.SetParent(root.transform, false);
                var spriteRenderer = spriteGo.AddComponent<SpriteRenderer>();
                root.transform.position = new Vector3(0f, 0.5f, 5f);

                var blue = new Color32(0x34, 0xA6, 0xFF, 0xFF);
                var red = new Color32(0xFF, 0x59, 0x40, 0xFF);
                context.Assert(TotemActorVisualHelper.AttachFactionRing(root, blue), "Actor visual helper should attach a faction ring.");
                context.Assert(TotemActorVisualHelper.AttachFactionRing(root, red), "Actor visual helper should reuse an existing faction ring.");

                var attachment = TotemActorVisualHelper.AttachActorVisuals(root, TotemActorKind.Player);
                context.Assert(attachment.shadowAttached, "Actor visual helper should attach a shadow.");
                context.Assert(attachment.depthSorterAttached, "Actor visual helper should attach a depth sorter when sprites exist.");
                context.Assert(attachment.billboardAttached, "Actor visual helper should attach billboard correction when sprites exist.");
                context.AssertEqual(2, attachment.spriteRendererCount, "actorVisual.helper.spriteRendererCountBeforeShadow");

                var shadow = root.transform.Find(TotemActorVisualHelper.ShadowName);
                context.Assert(shadow != null, "Actor shadow child should exist.");
                var shadowRenderer = shadow?.GetComponent<SpriteRenderer>();
                context.Assert(shadowRenderer != null && shadowRenderer.sprite != null, "Actor shadow should have a generated sprite.");

                var factionRing = root.transform.Find(TotemActorVisualHelper.FactionRingName);
                var factionRingRenderer = factionRing?.GetComponent<SpriteRenderer>();
                context.Assert(factionRingRenderer != null && factionRingRenderer.sprite != null, "Actor faction ring should have a generated sprite.");
                AssertColor(context, red, factionRingRenderer?.color ?? Color.clear, "actorVisual.factionRing.reusedColor");
                context.AssertEqual(1, root.GetComponentsInChildren<Transform>(true).Count(item => item.name == TotemActorVisualHelper.FactionRingName), "actorVisual.factionRing.count");

                var sorter = root.GetComponent<TotemActorDepthSorter>();
                sorter.RefreshRenderers();
                sorter.ForceRecalculate();
                context.AssertEqual(9500, spriteRenderer.sortingOrder, "actorVisual.depth.bodyOrder");
                if (factionRingRenderer != null)
                {
                    context.AssertEqual(9499, factionRingRenderer.sortingOrder, "actorVisual.depth.factionRingOrder");
                }
                context.AssertEqual(9498, shadowRenderer.sortingOrder, "actorVisual.depth.shadowOrder");

                var billboard = root.GetComponent<TotemActorBillboard>();
                billboard.ApplyTilt(55f);
                AssertNear(context, 55f, root.transform.localEulerAngles.x, "actorVisual.billboard.tilt");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CheckActorServiceVisualAttachment(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemActorVisualRuntimeDiagnostic]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                runtime.RegisterService(new TotemGameFlowService());
                runtime.RegisterService(new TotemDataService());
                runtime.RegisterService(new TotemAssetService());
                runtime.RegisterService(new TotemMapService());
                runtime.RegisterService(new TotemActorService());
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                context.Assert(actor.Player?.GameObject != null, "ActorService should spawn a player GameObject.");
                var playerObject = actor.Player.GameObject;
                context.Assert(playerObject.transform.Find(TotemActorVisualHelper.ShadowName) != null, "Spawned player should have a GF_X shadow child.");
                context.Assert(playerObject.transform.Find(TotemActorVisualHelper.FactionRingName) != null, "Spawned player should have a player faction ring.");
                context.Assert(playerObject.GetComponent<TotemActorDepthSorter>() != null, "Spawned player should have GF_X depth sorter.");
                context.Assert(playerObject.GetComponent<TotemActorBillboard>() != null, "Spawned player should have GF_X billboard correction.");

                int actorObjectsWithShadow = actor.Actors.Count(item => item.GameObject != null && item.GameObject.transform.Find(TotemActorVisualHelper.ShadowName) != null);
                context.AssertEqual(actor.Actors.Count, actorObjectsWithShadow, "actorVisual.runtime.shadowCount");
                int actorObjectsWithFactionRing = actor.Actors.Count(item => item.GameObject != null && item.GameObject.transform.Find(TotemActorVisualHelper.FactionRingName) != null);
                context.AssertEqual(actor.Actors.Count - 1, actorObjectsWithFactionRing, "actorVisual.runtime.factionRingCount");
                context.Assert(actor.Boss?.GameObject?.transform.Find(TotemActorVisualHelper.FactionRingName) == null, "Spawned Boss should not have a player or AI faction ring.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string name)
        {
            context.Detail($"{name}.actual", actual);
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, $"{name}: expected={expected}, actual={actual}");
        }

        private static void AssertColor(GFDiagnosticScenarioContext context, Color expected, Color actual, string name)
        {
            context.Detail($"{name}.actual", ColorUtility.ToHtmlStringRGBA(actual));
            context.Assert(Mathf.Abs(expected.r - actual.r) <= 0.001f
                && Mathf.Abs(expected.g - actual.g) <= 0.001f
                && Mathf.Abs(expected.b - actual.b) <= 0.001f
                && Mathf.Abs(expected.a - actual.a) <= 0.001f,
                $"{name}: expected={ColorUtility.ToHtmlStringRGBA(expected)}, actual={ColorUtility.ToHtmlStringRGBA(actual)}");
        }
    }
}
#endif
