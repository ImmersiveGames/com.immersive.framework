using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    internal enum UnityResetParticipantAuthoringContextKind
    {
        None,
        Resettable,
        Adapter,
        Ambiguous
    }

    internal readonly struct UnityResetParticipantAuthoringContext
    {
        internal UnityResetParticipantAuthoringContext(
            UnityResetParticipantAuthoringContextKind kind,
            Resettable resettable,
            UnityResetSubjectAdapter adapter)
        {
            Kind = kind;
            Resettable = resettable;
            Adapter = adapter;
        }

        internal UnityResetParticipantAuthoringContextKind Kind { get; }
        internal Resettable Resettable { get; }
        internal UnityResetSubjectAdapter Adapter { get; }
        internal bool UsesResettable => Kind == UnityResetParticipantAuthoringContextKind.Resettable;
        internal bool UsesAdapter => Kind == UnityResetParticipantAuthoringContextKind.Adapter;
    }

    internal static class UnityResetParticipantEditorUtility
    {
        internal static UnityResetParticipantAuthoringContext ResolveContext(
            UnityResetParticipantBehaviour participant)
        {
            if (participant == null)
                return new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.None, null, null);

            Resettable resettable = participant.GetComponentInParent<Resettable>(true);
            if (resettable != null)
            {
                var capabilities = new List<MonoBehaviour>();
                var adapters = new List<UnityResetSubjectAdapter>();
                ResettableBoundary.Collect(resettable, capabilities, adapters);
                if (!capabilities.Any(component => ReferenceEquals(component, participant)))
                    return new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.None, null, null);

                UnityResetSubjectAdapter coveringAncestor = ResettableBoundary.FindCoveringLegacyAncestor(resettable);
                UnityResetSubjectAdapter conflictingAdapter = adapters.FirstOrDefault() ?? coveringAncestor;
                return adapters.Count > 0 || coveringAncestor != null
                    ? new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.Ambiguous, resettable, conflictingAdapter)
                    : new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.Resettable, resettable, null);
            }

            var matchingAdapters = new List<UnityResetSubjectAdapter>();
            Transform current = participant.transform;
            while (current != null)
            {
                UnityResetSubjectAdapter adapter = current.GetComponent<UnityResetSubjectAdapter>();
                if (adapter != null && AdapterCollectsParticipant(adapter, participant))
                    matchingAdapters.Add(adapter);
                current = current.parent;
            }

            if (matchingAdapters.Count == 1)
                return new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.Adapter, null, matchingAdapters[0]);
            if (matchingAdapters.Count > 1)
                return new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.Ambiguous, null, matchingAdapters[0]);

            return new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.None, null, null);
        }

        internal static void DrawExecution(
            SerializedProperty displayName,
            SerializedProperty requiredness,
            SerializedProperty order)
        {
            EditorGUILayout.LabelField("Execution", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(displayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(requiredness, new GUIContent(
                "Requiredness",
                "Required failures block the Subject reset result. Optional failures follow the runtime optional-participant policy."));
            EditorGUILayout.PropertyField(order, new GUIContent("Order", "Lower values execute first."));
        }

        internal static void DrawIdentityAndDiagnostics(
            UnityResetParticipantAuthoringContext context,
            SerializedProperty participantId,
            SerializedProperty source,
            SerializedProperty reason,
            SerializedProperty requiredness,
            ref bool showAdvanced,
            ref bool showDiagnostics)
        {
            if (context.UsesAdapter)
            {
                showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
                if (showAdvanced)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(participantId, new GUIContent("Participant ID"));
                    EditorGUILayout.PropertyField(source, new GUIContent("Source"));
                    EditorGUILayout.PropertyField(reason, new GUIContent("Reason"));
                }
            }

            showDiagnostics = EditorGUILayout.Foldout(showDiagnostics, "Diagnostics", true);
            if (!showDiagnostics) return;

            switch (context.Kind)
            {
                case UnityResetParticipantAuthoringContextKind.Resettable:
                    EditorGUILayout.LabelField("Parent Resettable", context.Resettable.DisplayName);
                    EditorGUILayout.LabelField(
                        "Registration",
                        Application.isPlaying
                            ? (context.Resettable.IsRegistered
                                ? $"Registered ({context.Resettable.RegisteredCapabilityCount} capabilities)"
                                : "Not registered")
                            : "Runtime-dependent");
                    EditorGUILayout.LabelField("Capability Identity", "Runtime-generated / deterministic");
                    EditorGUILayout.HelpBox(
                        "The capability ID is generated from its deterministic position inside this Resettable boundary.",
                        MessageType.Info);
                    break;
                case UnityResetParticipantAuthoringContextKind.Adapter:
                    EditorGUILayout.LabelField("Parent Subject", context.Adapter.name);
                    EditorGUILayout.LabelField(
                        "Registration",
                        Application.isPlaying
                            ? (context.Adapter.IsRegistered ? "Registered" : "Not registered")
                            : "Runtime-dependent");
                    EditorGUILayout.LabelField("Serialized ID", participantId.stringValue);
                    EditorGUILayout.LabelField("Identity", string.IsNullOrWhiteSpace(participantId.stringValue) ? "Missing" : "Present");
                    break;
                case UnityResetParticipantAuthoringContextKind.Ambiguous:
                    EditorGUILayout.HelpBox(
                        context.Resettable != null && context.Adapter != null
                            ? $"Resettable '{context.Resettable.DisplayName}' overlaps UnityResetSubjectAdapter '{context.Adapter.name}'. Runtime registration rejects mixed boundaries."
                            : "More than one UnityResetSubjectAdapter discovers this participant. Resolve the overlapping adapter boundaries.",
                        MessageType.Error);
                    break;
                default:
                    EditorGUILayout.HelpBox(
                        "No Resettable or UnityResetSubjectAdapter boundary discovers this participant.",
                        MessageType.Warning);
                    break;
            }
        }

        internal static bool ValidateCommon(
            UnityResetParticipantAuthoringContext context,
            SerializedProperty participantId,
            SerializedProperty requiredness,
            out string issue)
        {
            if (context.Kind == UnityResetParticipantAuthoringContextKind.Ambiguous)
            {
                issue = "Reset participant authoring context is ambiguous. Runtime rejects overlapping Resettable and adapter boundaries.";
                return false;
            }

            if (context.Kind == UnityResetParticipantAuthoringContextKind.None)
            {
                issue = "Participant is not discovered by a Resettable or UnityResetSubjectAdapter boundary.";
                return false;
            }

            if (context.UsesAdapter)
                return ValidateLegacyParticipant(participantId, requiredness, out issue);

            if (requiredness.intValue == (int)ResetParticipantRequiredness.Unknown)
            {
                issue = "Requiredness must be explicit.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        internal static bool ValidateLegacyParticipant(
            SerializedProperty participantId,
            SerializedProperty requiredness,
            out string issue)
        {
            if (string.IsNullOrWhiteSpace(participantId.stringValue))
            {
                issue = "Participant ID is missing. Use Generate Missing ID.";
                return false;
            }

            if (requiredness.intValue == (int)ResetParticipantRequiredness.Unknown)
            {
                issue = "Requiredness must be explicit.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool AdapterCollectsParticipant(
            UnityResetSubjectAdapter adapter,
            UnityResetParticipantBehaviour participant)
        {
            switch (adapter.ParticipantDiscovery)
            {
                case UnityResetParticipantDiscoveryMode.SameGameObject:
                    return adapter.gameObject == participant.gameObject
                        && adapter.GetComponents<UnityResetParticipantBehaviour>()
                            .Any(component => ReferenceEquals(component, participant));
                case UnityResetParticipantDiscoveryMode.Children:
                    var serializedAdapter = new SerializedObject(adapter);
                    SerializedProperty includeInactive = serializedAdapter.FindProperty("includeInactiveParticipants");
                    bool shouldIncludeInactive = includeInactive != null && includeInactive.boolValue;
                    if (!shouldIncludeInactive && !participant.gameObject.activeInHierarchy)
                        return false;
                    return adapter.GetComponentsInChildren<UnityResetParticipantBehaviour>(shouldIncludeInactive)
                        .Any(component => ReferenceEquals(component, participant));
                default:
                    return false;
            }
        }
    }
}
