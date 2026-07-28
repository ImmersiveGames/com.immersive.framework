using System;
using Immersive.Framework.Reset;
using UnityEditor;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    public enum ObjectResetTargetAuthoringValidationStatus
    {
        ValidAdapterReference,
        ValidAuthoredSubjectId,
        MissingTarget,
        InvalidAuthoredSubjectId
    }

    /// <summary>Editor-only authoring validation. It intentionally does not query runtime registration.</summary>
    public readonly struct ObjectResetTargetAuthoringValidationResult
    {
        internal ObjectResetTargetAuthoringValidationResult(
            ObjectResetTargetAuthoringValidationStatus status,
            string message,
            bool hasAdapter,
            bool hasDirectId)
        {
            Status = status;
            Message = message ?? string.Empty;
            HasAdapter = hasAdapter;
            HasDirectId = hasDirectId;
        }

        public ObjectResetTargetAuthoringValidationStatus Status { get; }
        public string Message { get; }
        public bool HasAdapter { get; }
        public bool HasDirectId { get; }
        public bool IsValid => Status == ObjectResetTargetAuthoringValidationStatus.ValidAdapterReference || Status == ObjectResetTargetAuthoringValidationStatus.ValidAuthoredSubjectId;
    }

    public static class ObjectResetTargetAuthoringValidator
    {
        public static ObjectResetTargetAuthoringValidationResult Validate(SerializedProperty targetSubject)
        {
            if (targetSubject == null)
            {
                return Missing();
            }

            SerializedProperty adapter = targetSubject.FindPropertyRelative("subjectAdapter");
            SerializedProperty subjectId = targetSubject.FindPropertyRelative("subjectId");
            bool hasAdapter = adapter != null && adapter.objectReferenceValue != null;
            string authoredSubjectId = subjectId != null ? subjectId.stringValue : string.Empty;
            bool hasDirectId = !string.IsNullOrWhiteSpace(authoredSubjectId);

            if (hasAdapter)
            {
                return new ObjectResetTargetAuthoringValidationResult(
                    ObjectResetTargetAuthoringValidationStatus.ValidAdapterReference,
                    hasDirectId
                        ? "Target Subject is configured through the assigned Reset Subject Adapter. When the adapter has a resolved Subject ID it takes precedence; the authored ID remains the current runtime fallback."
                        : "Target Subject is configured through the assigned Reset Subject Adapter. Its runtime Subject ID will be resolved when the adapter is registered.",
                    true,
                    hasDirectId);
            }

            if (!hasDirectId)
            {
                return Missing();
            }

            try
            {
                ResetSubjectId.From(authoredSubjectId);
                return new ObjectResetTargetAuthoringValidationResult(
                    ObjectResetTargetAuthoringValidationStatus.ValidAuthoredSubjectId,
                    "Target Subject is configured through an authored Reset Subject ID.",
                    false,
                    true);
            }
            catch (ArgumentException)
            {
                return new ObjectResetTargetAuthoringValidationResult(
                    ObjectResetTargetAuthoringValidationStatus.InvalidAuthoredSubjectId,
                    "The authored Reset Subject ID is invalid. Enter a non-empty canonical Reset Subject ID or assign a Reset Subject Adapter.",
                    false,
                    true);
            }
        }

        private static ObjectResetTargetAuthoringValidationResult Missing()
        {
            return new ObjectResetTargetAuthoringValidationResult(
                ObjectResetTargetAuthoringValidationStatus.MissingTarget,
                "Assign a Reset Subject Adapter or provide an authored Reset Subject ID.",
                false,
                false);
        }
    }
}
