using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// Typed result of resolving authored application Progression Save composition.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR018-C typed Progression Save application composition result.")]
    public sealed class ProgressionSaveApplicationCompositionResult
    {
        private ProgressionSaveApplicationCompositionResult(
            ProgressionSaveApplicationCompositionStatus status,
            ProgressionSaveProfile profile,
            ProgressionSaveRuntime runtime,
            string message)
        {
            if (!Enum.IsDefined(
                    typeof(ProgressionSaveApplicationCompositionStatus),
                    status) ||
                status ==
                    ProgressionSaveApplicationCompositionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Progression Save application composition status must be explicit.");
            }

            if (status ==
                    ProgressionSaveApplicationCompositionStatus.Ready &&
                (profile == null || runtime == null))
            {
                throw new ArgumentException(
                    "Ready Progression Save composition requires both Profile and Runtime.");
            }

            if (status !=
                    ProgressionSaveApplicationCompositionStatus.Ready &&
                runtime != null)
            {
                throw new ArgumentException(
                    "Non-ready Progression Save composition cannot carry a Runtime.");
            }

            Status = status;
            Profile = profile;
            Runtime = runtime;
            Message = message.NormalizeText();
        }

        public ProgressionSaveApplicationCompositionStatus Status { get; }

        public ProgressionSaveProfile Profile { get; }

        public ProgressionSaveRuntime Runtime { get; }

        public string Message { get; }

        public bool Succeeded =>
            Status is ProgressionSaveApplicationCompositionStatus.Disabled
                or ProgressionSaveApplicationCompositionStatus.Ready;

        public bool Configured =>
            Status ==
            ProgressionSaveApplicationCompositionStatus.Ready;

        public bool Failed =>
            Status ==
            ProgressionSaveApplicationCompositionStatus.Rejected;

        public bool HasRuntime =>
            Runtime != null;

        public bool HasProfile =>
            Profile != null;

        public ProgressionSaveBackendId BackendId =>
            HasRuntime
                ? Runtime.BackendId
                : default;

        internal static ProgressionSaveApplicationCompositionResult Disabled(
            string message)
        {
            return new ProgressionSaveApplicationCompositionResult(
                ProgressionSaveApplicationCompositionStatus.Disabled,
                null,
                null,
                message);
        }

        internal static ProgressionSaveApplicationCompositionResult Ready(
            ProgressionSaveProfile profile,
            ProgressionSaveRuntime runtime,
            string message)
        {
            return new ProgressionSaveApplicationCompositionResult(
                ProgressionSaveApplicationCompositionStatus.Ready,
                profile,
                runtime,
                message);
        }

        internal static ProgressionSaveApplicationCompositionResult Rejected(
            ProgressionSaveProfile profile,
            string message)
        {
            return new ProgressionSaveApplicationCompositionResult(
                ProgressionSaveApplicationCompositionStatus.Rejected,
                profile,
                null,
                message);
        }
    }
}
