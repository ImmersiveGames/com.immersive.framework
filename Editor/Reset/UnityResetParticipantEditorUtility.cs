using System.Collections.Generic;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    internal enum UnityResetParticipantAuthoringContextKind
    {
        None,
        Resettable
    }

    internal readonly struct UnityResetParticipantAuthoringContext
    {
        internal UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind kind, Resettable resettable)
        {
            Kind = kind;
            Resettable = resettable;
        }

        internal UnityResetParticipantAuthoringContextKind Kind { get; }
        internal Resettable Resettable { get; }
    }

    internal static class UnityResetParticipantEditorUtility
    {
        internal static UnityResetParticipantAuthoringContext ResolveContext(UnityResetParticipantBehaviour participant)
        {
            if (participant == null)
            {
                return new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.None, null);
            }

            Resettable resettable = participant.GetComponentInParent<Resettable>(true);
            if (resettable == null)
            {
                return new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.None, null);
            }

            var capabilities = new List<MonoBehaviour>();
            ResettableBoundary.Collect(resettable, capabilities);
            for (int index = 0; index < capabilities.Count; index++)
            {
                if (ReferenceEquals(capabilities[index], participant))
                {
                    return new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.Resettable, resettable);
                }
            }

            return new UnityResetParticipantAuthoringContext(UnityResetParticipantAuthoringContextKind.None, null);
        }

        internal static void DrawExecution(SerializedProperty displayName, SerializedProperty requiredness, SerializedProperty order)
        {
            EditorGUILayout.LabelField("Execution", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(displayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(requiredness, new GUIContent(
                "Requiredness",
                "Required failures block the Subject reset result. Optional failures follow the runtime optional-participant policy."));
            EditorGUILayout.PropertyField(order, new GUIContent("Order", "Lower values execute first."));
        }

        internal static void DrawDiagnostics(
            UnityResetParticipantAuthoringContext context,
            ref bool showDiagnostics)
        {
            showDiagnostics = EditorGUILayout.Foldout(showDiagnostics, "Diagnostics", true);
            if (!showDiagnostics)
            {
                return;
            }

            if (context.Kind != UnityResetParticipantAuthoringContextKind.Resettable)
            {
                EditorGUILayout.HelpBox("This capability is not inside a Resettable boundary.", MessageType.Warning);
                return;
            }

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
        }

        internal static bool ValidateCommon(
            UnityResetParticipantAuthoringContext context,
            SerializedProperty requiredness,
            out string issue)
        {
            if (context.Kind != UnityResetParticipantAuthoringContextKind.Resettable)
            {
                issue = "Capability must be inside a Resettable boundary.";
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
    }
}
