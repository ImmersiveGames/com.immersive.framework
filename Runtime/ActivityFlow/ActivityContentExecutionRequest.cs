using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive request describing one logical Activity Content Execution operation.
    /// It carries Activity, runtime owner context, content identity, phase and requiredness only; it does not invoke Unity objects or gameplay consumers.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10B Activity Content Execution request contract; no execution runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionRequest : IEquatable<ActivityContentExecutionRequest>
    {
        private ActivityContentExecutionRequest(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            ActivityContentExecutionRequiredness requiredness,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionPhase), phase)
                || phase == ActivityContentExecutionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Activity content execution phase must be explicit.");
            }

            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity), "Activity content execution request requires an Activity.");
            }

            if (!context.IsValid)
            {
                throw new ArgumentException("Activity content execution request requires a valid runtime scope context.", nameof(context));
            }

            if (context.Scope != RuntimeContentScope.Activity)
            {
                throw new ArgumentException("Activity content execution request context must be Activity scoped.", nameof(context));
            }

            if (!contentId.IsValid)
            {
                throw new ArgumentException("Activity content execution request requires a valid RuntimeContentId.", nameof(contentId));
            }

            if (!Enum.IsDefined(typeof(ActivityContentExecutionRequiredness), requiredness)
                || requiredness == ActivityContentExecutionRequiredness.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Activity content execution requiredness must be explicit.");
            }

            Phase = phase;
            Activity = activity;
            PreviousActivity = previousActivity;
            NextActivity = nextActivity;
            Context = context;
            ContentId = contentId;
            Requiredness = requiredness;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        public ActivityContentExecutionPhase Phase { get; }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }

        public ActivityAsset NextActivity { get; }

        private RuntimeScopeContext Context { get; }

        public RuntimeContentOwner Owner => Context.Owner;

        public RuntimeContentScope Scope => Context.Scope;

        public RuntimeContentId ContentId { get; }

        public RuntimeContentIdentity Identity => Context.CreateIdentity(ContentId);

        public ActivityContentExecutionRequiredness Requiredness { get; }

        private string Source { get; }

        private string Reason { get; }

        public bool IsRequired => Requiredness == ActivityContentExecutionRequiredness.Required;

        public bool IsValid => Activity != null
            && Context.IsValid
            && Scope == RuntimeContentScope.Activity
            && ContentId.IsValid
            && Phase != ActivityContentExecutionPhase.Unknown
            && Requiredness != ActivityContentExecutionRequiredness.Unknown;

        private string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;

        private string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        private string NextActivityName => NextActivity != null ? NextActivity.ActivityName : string.Empty;

        public bool Equals(ActivityContentExecutionRequest other)
        {
            return Phase == other.Phase
                && ReferenceEquals(Activity, other.Activity)
                && ReferenceEquals(PreviousActivity, other.PreviousActivity)
                && ReferenceEquals(NextActivity, other.NextActivity)
                && Context.Equals(other.Context)
                && ContentId.Equals(other.ContentId)
                && Requiredness == other.Requiredness
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Phase;
                hashCode = hashCode * 397 ^ (Activity != null ? Activity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (PreviousActivity != null ? PreviousActivity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (NextActivity != null ? NextActivity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ Context.GetHashCode();
                hashCode = hashCode * 397 ^ ContentId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Requiredness;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"phase='{Phase}' activity='{ActivityName}' previousActivity='{PreviousActivityName}' nextActivity='{NextActivityName}' identity='{Identity.StableText}' owner='{Owner.StableText}' contentId='{ContentId.StableText}' requiredness='{Requiredness}' source='{sourceText}' reason='{reasonText}'";
        }

        public static ActivityContentExecutionRequest Enter(
            ActivityAsset activity,
            ActivityAsset previousActivity,
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            ActivityContentExecutionRequiredness requiredness,
            string source,
            string reason)
        {
            return new ActivityContentExecutionRequest(
                ActivityContentExecutionPhase.Enter,
                activity,
                previousActivity,
                activity,
                context,
                contentId,
                requiredness,
                source,
                reason);
        }

        public static ActivityContentExecutionRequest Exit(
            ActivityAsset activity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            ActivityContentExecutionRequiredness requiredness,
            string source,
            string reason)
        {
            return new ActivityContentExecutionRequest(
                ActivityContentExecutionPhase.Exit,
                activity,
                activity,
                nextActivity,
                context,
                contentId,
                requiredness,
                source,
                reason);
        }

        public static bool operator ==(ActivityContentExecutionRequest left, ActivityContentExecutionRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActivityContentExecutionRequest left, ActivityContentExecutionRequest right)
        {
            return !left.Equals(right);
        }
    }
}
