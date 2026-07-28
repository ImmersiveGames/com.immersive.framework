using UnityEditor;
using UnityEngine;
using Immersive.Framework.Editor.Common;

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

            subjectId.stringValue = FrameworkAuthoringSuggestionUtility.SuggestIdentity(
                subjectId.serializedObject.targetObject,
                "reset.subject");
            return true;
        }

        public static bool GenerateMissingParticipantId(SerializedProperty participantId)
        {
            if (participantId == null || !string.IsNullOrWhiteSpace(participantId.stringValue))
            {
                return false;
            }

            participantId.stringValue = FrameworkAuthoringSuggestionUtility.SuggestIdentity(
                participantId.serializedObject.targetObject,
                "reset.participant");
            return true;
        }

        internal static void RecordPrefabModification(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
