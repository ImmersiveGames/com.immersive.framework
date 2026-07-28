using System;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    public static class ResetAuthoringIdentityUtility
    {
        public static bool GenerateMissingSubjectId(
            SerializedProperty idGeneration,
            SerializedProperty subjectId)
        {
            if (idGeneration == null || subjectId == null ||
                idGeneration.intValue != 10 ||
                !string.IsNullOrWhiteSpace(subjectId.stringValue))
            {
                return false;
            }

            subjectId.stringValue = "reset.subject." + Guid.NewGuid().ToString("N");
            return true;
        }

        public static bool GenerateMissingParticipantId(SerializedProperty participantId)
        {
            if (participantId == null || !string.IsNullOrWhiteSpace(participantId.stringValue))
            {
                return false;
            }

            participantId.stringValue = "reset.participant." + Guid.NewGuid().ToString("N");
            return true;
        }

        internal static void RecordPrefabModification(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
