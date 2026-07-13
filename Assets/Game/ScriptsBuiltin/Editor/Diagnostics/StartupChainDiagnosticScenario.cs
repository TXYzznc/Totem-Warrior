#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class StartupChainDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Launch To Totem Runtime Smoke";

        public override string Category => "Startup";

        public override GFDiagnosticScenarioMode Mode => GFDiagnosticScenarioMode.PlayMode;

        public override void Run(GFDiagnosticScenarioContext context)
        {
            context.TraceInfo("Probe", "Check runtime startup chain.");
            context.Assert(Application.isPlaying, "Startup chain scenario must run in PlayMode.");
            if (!Application.isPlaying)
            {
                return;
            }

            string currentProcedure = GF.Procedure == null || GF.Procedure.CurrentProcedure == null
                ? string.Empty
                : GF.Procedure.CurrentProcedure.GetType().Name;
            context.Detail("currentProcedure", currentProcedure);
            context.Assert(currentProcedure == nameof(TotemGameProcedure), $"Expected current procedure '{nameof(TotemGameProcedure)}', actual '{currentProcedure}'.");

            List<GFTraceEvent> events = GFTrace.GetRecentEvents(500);
            context.Detail("traceEventCount", events.Count);

            var launch = FindLast(events, "Procedure", "Launch.Enter");
            var hotfix = FindLast(events, "Procedure", "LoadHotfixDll.Enter");
            var hotfixEntry = FindLast(events, "Hotfix", "Entry.Start");
            var preload = FindLast(events, "Procedure", "Preload.Enter");
            var preloadCompleted = FindLast(events, "Procedure", "Preload.Completed");
            var workspace = FindLast(events, "Procedure", "Workspace.Enter");
            var totemEnter = FindLast(events, "Procedure", "TotemGame.Enter");
            var totemRuntimeReady = FindLast(events, "Procedure", "TotemGame.RuntimeReady");

            DetailEvent(context, launch, "Procedure.Launch.Enter");
            DetailEvent(context, hotfix, "Procedure.LoadHotfixDll.Enter");
            DetailEvent(context, hotfixEntry, "Hotfix.Entry.Start");
            DetailEvent(context, preload, "Procedure.Preload.Enter");
            DetailEvent(context, preloadCompleted, "Procedure.Preload.Completed");
            DetailEvent(context, workspace, "Procedure.Workspace.Enter");
            DetailEvent(context, totemEnter, "Procedure.TotemGame.Enter");
            DetailEvent(context, totemRuntimeReady, "Procedure.TotemGame.RuntimeReady");

            TotemGameRuntime runtime = TotemGameRuntime.Instance;
            context.Assert(runtime != null, "TotemGameRuntime.Instance is null.");
            if (runtime == null)
            {
                return;
            }

            TotemGameRuntimeSnapshot snapshot = runtime.CaptureSnapshot();
            context.Detail("runtime.started", snapshot.started);
            context.Detail("runtime.currentProcedure", snapshot.currentProcedure);
            context.Detail("runtime.serviceCount", snapshot.serviceCount);
            context.Detail("runtime.readyServiceCount", snapshot.readyServiceCount);
            context.Detail("runtime.failedServiceCount", snapshot.failedServiceCount);
            context.Assert(snapshot.started, "Totem runtime snapshot is not started.");
            context.Assert(snapshot.currentProcedure == nameof(TotemGameProcedure), $"Totem runtime procedure mismatch: {snapshot.currentProcedure}");
            context.Assert(snapshot.servicesReady, "Totem runtime services are not all ready.");
            context.AssertEqual(31, snapshot.serviceCount, "runtime.default.serviceCount");
            context.AssertEqual(snapshot.serviceCount, snapshot.readyServiceCount, "runtime.default.readyServiceCount");
            context.AssertEqual(0, snapshot.failedServiceCount, "runtime.default.failedServiceCount");
            AssertDefaultServices(context, snapshot);

            bool hasCompleteStartupTrace = launch != null &&
                                           hotfix != null &&
                                           hotfixEntry != null &&
                                           preload != null &&
                                           preloadCompleted != null &&
                                           workspace != null &&
                                           totemEnter != null &&
                                           totemRuntimeReady != null;
            context.Detail("startupTrace.windowComplete", hasCompleteStartupTrace);
            if (!hasCompleteStartupTrace)
            {
                context.TraceInfo("Probe", "Startup trace order skipped because early events are outside the recent trace window.");
                return;
            }

            AssertOrder(context, launch, hotfix, "Launch -> LoadHotfixDll");
            AssertOrder(context, hotfix, hotfixEntry, "LoadHotfixDll -> HotfixEntry");
            AssertOrder(context, hotfixEntry, preload, "HotfixEntry -> Preload");
            AssertOrder(context, preload, preloadCompleted, "Preload -> Preload.Completed");
            AssertOrder(context, preloadCompleted, workspace, "Preload.Completed -> Workspace");
            AssertOrder(context, workspace, totemEnter, "Workspace -> TotemGame");
            AssertOrder(context, totemEnter, totemRuntimeReady, "TotemGame -> RuntimeReady");

            int preloadFailures = events.Count(value =>
                value.seq >= preload.seq &&
                value.seq <= workspace.seq &&
                value.system == "Preload" &&
                value.result == GFTrace.ResultFailure);
            context.Detail("preloadFailures", preloadFailures);
            context.Assert(preloadFailures == 0, $"Preload had {preloadFailures} failure trace event(s) before Workspace.");
        }

        private static void AssertDefaultServices(GFDiagnosticScenarioContext context, TotemGameRuntimeSnapshot snapshot)
        {
            string[] required =
            {
                "GameFlow",
                "Input",
                "Data",
                "Asset",
                "Settings",
                "Audio",
                "RunStats",
                "MetaProgress",
                "Map",
                "Actor",
                "Economy",
                "Status",
                "Tattoo",
                "Weapon",
                "Chest",
                "Skill",
                "Zone",
                "Boss",
                "AI",
                "Npc",
                "Choice",
                "Interaction",
                "Camera",
                "VFX",
                "Combat",
                "UI",
            };

            var serviceNames = new HashSet<string>((snapshot.services ?? new TotemRuntimeServiceStatus[0]).Select(item => item.serviceName));
            for (int i = 0; i < required.Length; i++)
            {
                context.Assert(serviceNames.Contains(required[i]), $"Default runtime service missing: {required[i]}");
            }
        }

        private static GFTraceEvent FindLast(List<GFTraceEvent> events, string system, string action)
        {
            return events.LastOrDefault(value => value.system == system && value.action == action);
        }

        private static void DetailEvent(GFDiagnosticScenarioContext context, GFTraceEvent traceEvent, string label)
        {
            context.Detail($"event.{label}", traceEvent != null ? traceEvent.seq.ToString() : "missing");
        }

        private static void AssertOrder(GFDiagnosticScenarioContext context, GFTraceEvent previous, GFTraceEvent next, string label)
        {
            bool ordered = previous.seq < next.seq;
            context.Detail($"order.{label}", $"{previous.seq}->{next.seq}");
            context.Assert(ordered, $"Startup trace order is invalid: {label}.");
        }
    }
}
#endif
