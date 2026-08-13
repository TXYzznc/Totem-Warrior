#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemParticipantReadinessDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Participant Readiness Runtime";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            GameObject runtimeObject = null;
            TotemGameRuntime runtime = null;
            if (true)
            {
                try
                {
                    runtimeObject = new GameObject("[TotemReadinessDiagnosticRuntime]");
                    runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                    var provider = new ReadinessInputProvider();
                    runtime.RegisterService(new TotemGameFlowService());
                    runtime.RegisterService(new TotemMatchClockService());
                    runtime.RegisterService(new TotemInputService());
                    runtime.RegisterService(new TotemMapService());
                    runtime.RegisterService(new TotemCombatRelationshipService());
                    runtime.RegisterService(new TotemActorService());
                    runtime.RegisterService(new TotemParticipantReadinessService());
                    runtime.StartRuntime();

                    var flow = runtime.GetService<TotemGameFlowService>();
                    var clock = runtime.GetService<TotemMatchClockService>();
                    var input = runtime.GetService<TotemInputService>();
                    var actor = runtime.GetService<TotemActorService>();
                    var readiness = runtime.GetService<TotemParticipantReadinessService>();
                    input.SetInputProvider(provider);
                    actor.BeginPlayerStartupProtection("Diagnostics");
                    flow.ConfirmLocalFirstPlayable();

                    var player = actor.Player;
                    context.Assert(player != null, "Readiness diagnostic requires the local participant.");
                    context.AssertEqual(TotemParticipantLifecycle.Loading, readiness.GetLifecycle(player), "readiness.initial");
                    context.Assert(player.GameObject != null && !player.GameObject.activeSelf, "Loading participant runtime object must be inactive.");
                    clock.Tick(10f);
                    readiness.Tick(10f);
                    context.AssertEqual(10f, clock.WorldTime, "readiness.worldTimeWhileLoading");
                    context.Assert(!readiness.CanAct(player), "Loading participant must not act.");
                    context.Assert(!readiness.CanBeTargeted(player), "Loading participant must not be targeted.");

                    context.Assert(readiness.NotifyLocalClientReady(player, "Diagnostics.HUDReady"), "Ready must transition Loading to Protected.");
                    context.AssertEqual(TotemParticipantLifecycle.Protected, readiness.GetLifecycle(player), "readiness.protected");
                    context.Assert(player.GameObject != null && player.GameObject.activeSelf, "Protected participant runtime object must be active after HUD readiness.");
                    context.Assert(!readiness.CanAct(player), "Protected participant must not act before release.");

                    provider.Hold(KeyCode.W);
                    input.Tick(0.016f);
                    readiness.Tick(0.016f);
                    context.AssertEqual(TotemParticipantLifecycle.Active, readiness.GetLifecycle(player), "readiness.activeAfterInput");
                    context.Assert(readiness.CanAct(player), "Input intent must release protection before gameplay processing.");
                    context.AssertEqual(10f, clock.WorldTime, "readiness.readyDoesNotResetWorldTime");

                    flow.EnterMainMenu();
                    provider.Clear();
                    actor.BeginPlayerStartupProtection("Diagnostics.Timeout");
                    flow.ConfirmLocalFirstPlayable();
                    player = actor.Player;
                    readiness.Tick(90f);
                    var snapshot = readiness.CaptureSnapshot();
                    context.AssertEqual(TotemParticipantLifecycle.Disconnected, readiness.GetLifecycle(player), "readiness.timeoutLifecycle");
                    context.Assert(player.GameObject != null && !player.GameObject.activeSelf, "Timed-out participant runtime object must remain inactive.");
                    context.AssertEqual(1, snapshot.timeoutCount, "readiness.timeoutCount");
                    context.Pass("World time advances independently while each human participant uses Loading, Protected and timeout states.");
                }
                finally
                {
                    runtime?.ShutdownRuntime();
                    if (runtimeObject != null)
                    {
                        Object.DestroyImmediate(runtimeObject);
                    }
                }
            }
        }

        private sealed class ReadinessInputProvider : ITotemInputProvider
        {
            private readonly HashSet<KeyCode> held = new HashSet<KeyCode>();

            public float UnscaledTime { get; private set; }

            public Vector3 MousePosition => new Vector3(float.NaN, float.NaN, float.NaN);

            public bool GetKey(KeyCode keyCode) => held.Contains(keyCode);

            public bool GetKeyDown(KeyCode keyCode) => false;

            public bool GetMouseButton(int button) => false;

            public bool GetMouseButtonDown(int button) => false;

            public void Hold(KeyCode keyCode)
            {
                held.Add(keyCode);
                UnscaledTime += 0.016f;
            }

            public void Clear()
            {
                held.Clear();
            }
        }
    }
}
#endif
