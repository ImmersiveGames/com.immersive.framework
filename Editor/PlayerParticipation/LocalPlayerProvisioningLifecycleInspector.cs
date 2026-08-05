using System;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Designer-first, read-only lifecycle panel rendered immediately after
    /// the standard Inspector header. The existing authoring editor remains
    /// responsible for configuration and validation.
    /// </summary>
    [InitializeOnLoad]
    internal static class
        LocalPlayerProvisioningLifecycleInspector
    {
        private const double RepaintIntervalSeconds = 0.25d;
        private const string AdvancedStatePrefix =
            "Immersive.Framework.PlayerParticipation.LifecycleInspector.";

        private static double nextRepaintTime;

        static LocalPlayerProvisioningLifecycleInspector()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI +=
                DrawLifecycleHeader;
            EditorApplication.update +=
                RepaintActiveProvisioningInspectors;
        }

        private static void DrawLifecycleHeader(
            UnityEditor.Editor editor)
        {
            if (!Application.isPlaying ||
                editor == null ||
                editor.targets == null ||
                editor.targets.Length != 1 ||
                !(editor.target is
                    LocalPlayerProvisioningAuthoring authoring))
            {
                return;
            }

            ManagerProvisionedPlayerLifecycleSnapshot snapshot =
                authoring.ManagerProvisionedLifecycleSnapshot;

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Manager-Provisioned Player Lifecycle",
                    EditorStyles.boldLabel);

                EditorGUILayout.HelpBox(
                    BuildHeadline(snapshot),
                    ResolveMessageType(snapshot));

                DrawDesignerSummary(snapshot);
                DrawSlots(snapshot);
                DrawAdvanced(authoring, snapshot);
            }
        }

        private static void DrawDesignerSummary(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Status",
                    snapshot.Status.ToString());

                EditorGUILayout.TextField(
                    "Activity",
                    DisplayOr(
                        snapshot.ActivityName,
                        "No active Activity evidence"));

                EditorGUILayout.IntField(
                    "Occurrence",
                    snapshot.ActivityOccurrence);

                EditorGUILayout.TextField(
                    "Entry Policy",
                    DisplayOr(
                        snapshot.EntryPolicy,
                        "Not observed"));

                EditorGUILayout.TextField(
                    "Readiness",
                    DisplayOr(
                        snapshot.ReadinessStatus,
                        "Not observed"));

                EditorGUILayout.TextField(
                    "Player Gate Contribution",
                    BuildGateLabel(snapshot));

                EditorGUILayout.Toggle(
                    "Joining Open",
                    snapshot.JoiningOpen);

                EditorGUILayout.IntField(
                    "Technical Hosts",
                    snapshot.HostCount);
            }
        }

        private static void DrawSlots(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField(
                "Projected Player Slots",
                EditorStyles.miniBoldLabel);

            if (snapshot.SlotCount == 0)
            {
                EditorGUILayout.LabelField(
                    "No Player Slot evidence.");
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                for (int index = 0;
                     index < snapshot.Slots.Count;
                     index++)
                {
                    ManagerProvisionedPlayerLifecycleSlotSnapshot
                        slot = snapshot.Slots[index];

                    EditorGUILayout.TextField(
                        DisplayOr(
                            slot.PlayerSlotId,
                            $"Slot {index + 1}"),
                        BuildSlotStage(slot));
                }
            }
        }

        private static void DrawAdvanced(
            LocalPlayerProvisioningAuthoring authoring,
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            string stateKey =
                AdvancedStatePrefix +
                authoring.GetEntityId().ToString();

            bool showAdvanced =
                SessionState.GetBool(stateKey, false);

            bool nextShowAdvanced =
                EditorGUILayout.Foldout(
                    showAdvanced,
                    "Advanced / Debug",
                    true);

            if (nextShowAdvanced != showAdvanced)
            {
                SessionState.SetBool(
                    stateKey,
                    nextShowAdvanced);
            }

            if (!nextShowAdvanced)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Gate Evidence Scope",
                    snapshot.GateEvidenceScope.ToString());

                EditorGUILayout.Toggle(
                    "Has Gate Evidence",
                    snapshot.HasGateEvidence);

                EditorGUILayout.Toggle(
                    "Gate Held",
                    snapshot.GateHeld);

                EditorGUILayout.IntField(
                    "Session Revision",
                    snapshot.SessionRevision);

                EditorGUILayout.IntField(
                    "Requested Revision",
                    snapshot.RequestedSessionRevision);

                EditorGUILayout.IntField(
                    "Applied Revision",
                    snapshot.AppliedSessionRevision);

                EditorGUILayout.TextField(
                    "Readiness Reason",
                    DisplayOr(
                        snapshot.ReadinessReason,
                        "None"));
            }

            if (!string.IsNullOrWhiteSpace(
                    snapshot.Diagnostic))
            {
                EditorGUILayout.LabelField(
                    "Lifecycle Diagnostic",
                    EditorStyles.miniBoldLabel);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextArea(
                        snapshot.Diagnostic,
                        GUILayout.MinHeight(52f));
                }
            }

            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                ManagerProvisionedPlayerLifecycleSlotSnapshot
                    slot = snapshot.Slots[index];

                if (string.IsNullOrWhiteSpace(
                        slot.Diagnostic))
                {
                    continue;
                }

                EditorGUILayout.LabelField(
                    DisplayOr(
                        slot.PlayerSlotId,
                        $"Slot {index + 1}") +
                    " Diagnostic",
                    EditorStyles.miniBoldLabel);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextArea(
                        slot.Diagnostic,
                        GUILayout.MinHeight(38f));
                }
            }
        }

        private static string BuildHeadline(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.IsAvailable)
            {
                return snapshot != null &&
                       !string.IsNullOrWhiteSpace(
                           snapshot.Diagnostic)
                    ? snapshot.Diagnostic
                    : "Manager-Provisioned Player lifecycle evidence is unavailable.";
            }

            switch (snapshot.Status)
            {
                case ManagerProvisionedPlayerLifecycleStatus
                    .WaitingForActivity:
                    return
                        "Waiting for an Activity occurrence before Player lifecycle preparation can begin.";

                case ManagerProvisionedPlayerLifecycleStatus
                    .WaitingForJoin:
                    return
                        "Activity is known. Waiting for a Manager-Provisioned Player to join.";

                case ManagerProvisionedPlayerLifecycleStatus
                    .WaitingForActorSelection:
                    return
                        "A joined Player is waiting for Logical Actor selection.";

                case ManagerProvisionedPlayerLifecycleStatus
                    .PreparingLogicalActor:
                    return
                        "Preparing the selected Logical Actor for the current Activity.";

                case ManagerProvisionedPlayerLifecycleStatus
                    .MaterializingPhysicalActor:
                    return
                        "Materializing the Physical Actor for the current Activity.";

                case ManagerProvisionedPlayerLifecycleStatus
                    .PreparingGameplayAdmission:
                    return snapshot.GateHeld
                        ? "The official Player readiness contribution is still pending."
                        : "Waiting for gameplay admission or stable lifecycle reconciliation.";

                case ManagerProvisionedPlayerLifecycleStatus.Ready:
                    return
                        "Manager-Provisioned Player lifecycle is ready for gameplay.";

                case ManagerProvisionedPlayerLifecycleStatus.Failed:
                    return
                        "Manager-Provisioned Player lifecycle failed. Inspect Advanced / Debug evidence.";

                case ManagerProvisionedPlayerLifecycleStatus.Released:
                    return
                        "Activity-owned Player lifecycle evidence was released.";

                default:
                    return
                        "Manager-Provisioned Player lifecycle evidence is unavailable.";
            }
        }

        private static MessageType ResolveMessageType(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            if (snapshot == null ||
                !snapshot.IsAvailable)
            {
                return MessageType.Warning;
            }

            return snapshot.IsFailure
                ? MessageType.Error
                : MessageType.Info;
        }

        private static string BuildGateLabel(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            if (!snapshot.HasGateEvidence)
            {
                return "Unavailable";
            }

            string state =
                snapshot.GateHeld
                    ? "Pending"
                    : "Released";

            return
                $"{state} — Player contribution only; not aggregate Activity gate";
        }

        private static string BuildSlotStage(
            ManagerProvisionedPlayerLifecycleSlotSnapshot slot)
        {
            if (!slot.HasTechnicalHost)
            {
                return "No technical Host";
            }

            if (!slot.HasSelectedActor)
            {
                return "Waiting for Actor selection";
            }

            if (!slot.LogicalActorPrepared)
            {
                return "Preparing Logical Actor";
            }

            if (!slot.PhysicalActorMaterialized)
            {
                return "Materializing Physical Actor";
            }

            if (!slot.GameplayAdmitted)
            {
                return "Waiting for gameplay admission";
            }

            return "Ready";
        }

        private static string DisplayOr(
            string value,
            string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value;
        }

        private static void
            RepaintActiveProvisioningInspectors()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            double now =
                EditorApplication.timeSinceStartup;
            if (now < nextRepaintTime)
            {
                return;
            }

            nextRepaintTime =
                now + RepaintIntervalSeconds;

            ActiveEditorTracker tracker =
                ActiveEditorTracker.sharedTracker;
            if (tracker == null ||
                tracker.activeEditors == null)
            {
                return;
            }

            UnityEditor.Editor[] activeEditors =
                tracker.activeEditors;

            for (int index = 0;
                 index < activeEditors.Length;
                 index++)
            {
                UnityEditor.Editor activeEditor =
                    activeEditors[index];

                if (activeEditor != null &&
                    activeEditor.target is
                        LocalPlayerProvisioningAuthoring)
                {
                    activeEditor.Repaint();
                }
            }
        }
    }
}
