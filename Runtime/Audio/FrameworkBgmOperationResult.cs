using Immersive.Audio.Authoring;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// API status: Experimental. Framework-owned evidence for an optional BGM provider operation.
    /// </summary>
    public readonly struct FrameworkBgmOperationResult
    {
        private FrameworkBgmOperationResult(
            FrameworkBgmOperation operation,
            FrameworkBgmOperationOutcome outcome,
            AudioBgmCueAsset previousConfirmedCue,
            AudioBgmCueAsset requestedCue,
            AudioBgmCueAsset confirmedCue,
            bool requestedExplicitSilence,
            bool confirmedExplicitSilence,
            string reason)
        {
            Operation = operation;
            Outcome = outcome;
            PreviousConfirmedCue = previousConfirmedCue;
            RequestedCue = requestedCue;
            ConfirmedCue = confirmedCue;
            RequestedExplicitSilence = requestedExplicitSilence;
            ConfirmedExplicitSilence = confirmedExplicitSilence;
            Reason = reason;
        }

        public FrameworkBgmOperation Operation { get; }

        public FrameworkBgmOperationOutcome Outcome { get; }

        public AudioBgmCueAsset PreviousConfirmedCue { get; }

        public AudioBgmCueAsset RequestedCue { get; }

        public AudioBgmCueAsset ConfirmedCue { get; }

        public bool RequestedExplicitSilence { get; }

        public bool ConfirmedExplicitSilence { get; }

        public string Reason { get; }

        public bool IsProviderConfirmed => Outcome == FrameworkBgmOperationOutcome.Applied
            || Outcome == FrameworkBgmOperationOutcome.Released;

        internal static FrameworkBgmOperationResult Create(
            FrameworkBgmOperation operation,
            FrameworkBgmOperationOutcome outcome,
            AudioBgmCueAsset previousConfirmedCue,
            AudioBgmCueAsset requestedCue,
            AudioBgmCueAsset confirmedCue,
            bool requestedExplicitSilence,
            bool confirmedExplicitSilence,
            string reason)
        {
            return new FrameworkBgmOperationResult(
                operation,
                outcome,
                previousConfirmedCue,
                requestedCue,
                confirmedCue,
                requestedExplicitSilence,
                confirmedExplicitSilence,
                reason);
        }
    }

    public enum FrameworkBgmOperation
    {
        Apply = 0,
        Release = 1,

        /// <summary>
        /// No explicit provider mutation was requested; keep the confirmed presentation unchanged.
        /// </summary>
        Preserve = 2
    }

    public enum FrameworkBgmOperationOutcome
    {
        Applied = 0,
        Released = 1,
        NoChange = 2,
        OptionalAuthorityUnavailable = 3,
        Rejected = 4
    }
}
