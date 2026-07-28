using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.GameFlow.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Diagnostics.GameFlow
{
    [InitializeOnLoad]
    public static class FrameworkGameFlowDiagnosticFaultUtility
    {
        private const string LogPrefix = "[Immersive.Framework][GameFlowDiagnosticFault]";
        private static readonly Dictionary<FrameworkRuntimeHost, ActiveFault> Active = new();

        static FrameworkGameFlowDiagnosticFaultUtility()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static bool TryInstall(
            Component runtimeHost,
            FrameworkGameFlowDiagnosticFaultScenario scenario,
            string caseName,
            out FrameworkGameFlowDiagnosticFaultLease lease,
            out string issue)
        {
            lease = null;
            issue = string.Empty;
            if (runtimeHost is not FrameworkRuntimeHost host)
            {
                issue = "Diagnostic fault installation requires the canonical FrameworkRuntimeHost Component.";
                return false;
            }

            if (Active.ContainsKey(host))
            {
                issue = "FrameworkRuntimeHost already has an active Game Flow diagnostic fault lease.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(caseName))
            {
                issue = "Diagnostic fault case name is required.";
                return false;
            }

            GameFlowDiagnosticFaultCheckpoint checkpoint = GetCheckpoint(scenario);
            string leaseId = Guid.NewGuid().ToString("N");
            lease = new FrameworkGameFlowDiagnosticFaultLease(
                scenario, checkpoint, leaseId, Release);
            var plan = new FixedOneShotPlan(lease, checkpoint, caseName);
            if (!host.TryInstallGameFlowDiagnosticFaultPlan(plan, out issue))
            {
                lease = null;
                return false;
            }

            Active.Add(host, new ActiveFault(host, plan, lease));
            Debug.Log($"{LogPrefix} phase='installed' lease='{leaseId}' scenario='{scenario}' case='{caseName}' checkpoint='{checkpoint}'.");
            return true;
        }

        private static void Release(FrameworkGameFlowDiagnosticFaultLease lease)
        {
            FrameworkRuntimeHost host = null;
            foreach (KeyValuePair<FrameworkRuntimeHost, ActiveFault> pair in Active)
            {
                if (ReferenceEquals(pair.Value.Lease, lease)) { host = pair.Key; break; }
            }

            if (host == null || !Active.TryGetValue(host, out ActiveFault active) ||
                active.Lease.LeaseId != lease.LeaseId)
                return;

            host.ClearGameFlowDiagnosticFaultPlan(active.Plan);
            Active.Remove(host);
            lease.MarkReleased();
            string diagnostic = lease.Consumed
                ? lease.Diagnostic
                : "Lease released without consumption; this is not a successful diagnostic case.";
            Debug.Log($"{LogPrefix} phase='released' lease='{lease.LeaseId}' consumed='{lease.Consumed}' remainingActiveFaults='{Active.Count}' diagnostic='{diagnostic}'.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is not (PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)) return;
            var leases = new List<FrameworkGameFlowDiagnosticFaultLease>();
            foreach (ActiveFault active in Active.Values) leases.Add(active.Lease);
            foreach (FrameworkGameFlowDiagnosticFaultLease lease in leases) lease.Dispose();
        }

        private static GameFlowDiagnosticFaultCheckpoint GetCheckpoint(
            FrameworkGameFlowDiagnosticFaultScenario scenario) => scenario switch
        {
            FrameworkGameFlowDiagnosticFaultScenario.PreparationTokenMismatch => GameFlowDiagnosticFaultCheckpoint.CurrentPreparationTokenValidation,
            FrameworkGameFlowDiagnosticFaultScenario.OwnerMismatch => GameFlowDiagnosticFaultCheckpoint.CurrentOwnershipValidation,
            FrameworkGameFlowDiagnosticFaultScenario.PreCommitFailure => GameFlowDiagnosticFaultCheckpoint.BeforeCandidateStaging,
            FrameworkGameFlowDiagnosticFaultScenario.RuntimeUnavailable => GameFlowDiagnosticFaultCheckpoint.LifecycleRuntimeAvailability,
            FrameworkGameFlowDiagnosticFaultScenario.LoadingRejectedBeforePresentation => GameFlowDiagnosticFaultCheckpoint.BeforeLoadingPresentation,
            FrameworkGameFlowDiagnosticFaultScenario.CommittedTargetNotReady => GameFlowDiagnosticFaultCheckpoint.AfterCommitBeforeTargetReadiness,
            FrameworkGameFlowDiagnosticFaultScenario.CommittedFinalizationFailure => GameFlowDiagnosticFaultCheckpoint.AfterCandidateOwnershipBeforePreviousCleanup,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

        private sealed class ActiveFault
        {
            internal ActiveFault(FrameworkRuntimeHost host, FixedOneShotPlan plan, FrameworkGameFlowDiagnosticFaultLease lease)
            { Host = host; Plan = plan; Lease = lease; }
            internal FrameworkRuntimeHost Host { get; }
            internal FixedOneShotPlan Plan { get; }
            internal FrameworkGameFlowDiagnosticFaultLease Lease { get; }
        }

        private sealed class FixedOneShotPlan : IGameFlowDiagnosticFaultPlan
        {
            private readonly FrameworkGameFlowDiagnosticFaultLease lease;
            private readonly GameFlowDiagnosticFaultCheckpoint checkpoint;
            private readonly string caseName;

            internal FixedOneShotPlan(FrameworkGameFlowDiagnosticFaultLease lease, GameFlowDiagnosticFaultCheckpoint checkpoint, string caseName)
            { this.lease = lease; this.checkpoint = checkpoint; this.caseName = caseName; }

            public GameFlowDiagnosticFaultDecision Evaluate(GameFlowDiagnosticFaultRequest request)
            {
                if (lease.Released || lease.Consumed || request.Checkpoint != checkpoint)
                    return GameFlowDiagnosticFaultDecision.None;

                string diagnostic = $"scenario='{lease.Scenario}' case='{caseName}' checkpoint='{checkpoint}'";
                lease.MarkConsumed(request, diagnostic);
                Debug.Log($"{LogPrefix} phase='consumed' lease='{lease.LeaseId}' scenario='{lease.Scenario}' consumptionCount='{lease.ConsumptionCount}' operation='{request.Operation}' transaction='{request.Transaction}' slot='{request.Slot}'.");
                return GameFlowDiagnosticFaultDecision.Fail(diagnostic);
            }
        }
    }
}
