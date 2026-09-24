using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    public readonly struct ActivityVisibilityEvaluation
    {
        public ActivityVisibilityEvaluation(
            bool isValid,
            ActivityAsset activeActivity,
            ActivityAsset matchedActivity,
            bool hasMatch,
            bool desiredVisibility,
            string diagnosticReason)
        {
            IsValid = isValid;
            ActiveActivity = activeActivity;
            MatchedActivity = matchedActivity;
            HasMatch = hasMatch;
            DesiredVisibility = desiredVisibility;
            DiagnosticReason = diagnosticReason ?? string.Empty;
        }

        public bool IsValid { get; }

        public ActivityAsset ActiveActivity { get; }

        public ActivityAsset MatchedActivity { get; }

        public bool HasMatch { get; }

        public bool DesiredVisibility { get; }

        public string DiagnosticReason { get; }

        public bool HasActiveActivity => ActiveActivity != null;

        public bool IsActivityVisible => IsValid && HasActiveActivity && DesiredVisibility;
    }
}
