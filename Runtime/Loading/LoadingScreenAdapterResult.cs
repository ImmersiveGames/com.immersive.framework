using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Explicit result for one loading screen adapter action.
    /// It reports visual adapter outcome only; it does not block or own LoadingOperation, SceneLifecycle, Transition or readiness.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22E Loading Screen adapter result boundary; no lifecycle ownership.")]
    public readonly struct LoadingScreenAdapterResult : IEquatable<LoadingScreenAdapterResult>
    {
        private readonly string[] _issues;

        public LoadingScreenAdapterResult(
            LoadingScreenPresentation presentation,
            LoadingScreenAdapterAction action,
            LoadingScreenAdapterStatus status,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            if (!presentation.IsValid)
            {
                throw new ArgumentException("Loading screen adapter result requires a valid presentation.", nameof(presentation));
            }

            if (!Enum.IsDefined(typeof(LoadingScreenAdapterAction), action) || action == LoadingScreenAdapterAction.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(action), action, "Loading screen adapter action must be explicit.");
            }

            if (!Enum.IsDefined(typeof(LoadingScreenAdapterStatus), status) || status == LoadingScreenAdapterStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Loading screen adapter status must be explicit.");
            }

            Presentation = presentation;
            Action = action;
            Status = status;
            AdapterName = Normalize(adapterName);
            Message = Normalize(message);
            _issues = CopyIssues(issues);
        }

        public LoadingScreenPresentation Presentation { get; }

        public LoadingOperationId OperationId => Presentation.OperationId;

        public LoadingScreenAdapterAction Action { get; }

        public LoadingScreenAdapterStatus Status { get; }

        public string AdapterName { get; }

        public string Message { get; }

        public IReadOnlyList<string> Issues => _issues ?? Array.Empty<string>();

        public int IssueCount => Issues.Count;

        public bool HasIssues => IssueCount > 0;

        public bool Succeeded => Status == LoadingScreenAdapterStatus.Succeeded;

        public bool Skipped => Status == LoadingScreenAdapterStatus.Skipped;

        public bool Failed => Status == LoadingScreenAdapterStatus.Failed;

        public bool Rejected => Status == LoadingScreenAdapterStatus.Rejected;

        public bool Completed => Succeeded || Skipped;

        public bool IsValid => Presentation.IsValid
            && Action != LoadingScreenAdapterAction.Unknown
            && Status != LoadingScreenAdapterStatus.Unknown;

        public bool Equals(LoadingScreenAdapterResult other)
        {
            return Presentation.Equals(other.Presentation)
                && Action == other.Action
                && Status == other.Status
                && string.Equals(AdapterName, other.AdapterName, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(Issues, other.Issues);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingScreenAdapterResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Presentation.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Action;
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(AdapterName ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                for (int i = 0; i < Issues.Count; i++)
                {
                    hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Issues[i] ?? string.Empty);
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string adapterText = AdapterName.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            var builder = new StringBuilder();
            builder.Append($"adapter='{adapterText}' operation='{OperationId.StableText}' action='{Action}' status='{Status}' completed='{Completed}' issues='{IssueCount}' message='{messageText}' presentation=({Presentation.ToDiagnosticString()})");
            if (HasIssues)
            {
                builder.Append(" issues=[");
                for (int i = 0; i < Issues.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(Issues[i]);
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static LoadingScreenAdapterResult SucceededResult(
            LoadingScreenPresentation presentation,
            LoadingScreenAdapterAction action,
            string adapterName,
            string message)
        {
            return new LoadingScreenAdapterResult(presentation, action, LoadingScreenAdapterStatus.Succeeded, adapterName, message, Array.Empty<string>());
        }

        public static LoadingScreenAdapterResult SkippedResult(
            LoadingScreenPresentation presentation,
            LoadingScreenAdapterAction action,
            string adapterName,
            string message)
        {
            return new LoadingScreenAdapterResult(presentation, action, LoadingScreenAdapterStatus.Skipped, adapterName, message, Array.Empty<string>());
        }

        public static LoadingScreenAdapterResult FailedResult(
            LoadingScreenPresentation presentation,
            LoadingScreenAdapterAction action,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new LoadingScreenAdapterResult(presentation, action, LoadingScreenAdapterStatus.Failed, adapterName, message, issues);
        }

        public static LoadingScreenAdapterResult RejectedResult(
            LoadingScreenPresentation presentation,
            LoadingScreenAdapterAction action,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new LoadingScreenAdapterResult(presentation, action, LoadingScreenAdapterStatus.Rejected, adapterName, message, issues);
        }

        public static bool operator ==(LoadingScreenAdapterResult left, LoadingScreenAdapterResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingScreenAdapterResult left, LoadingScreenAdapterResult right)
        {
            return !left.Equals(right);
        }

        private static string[] CopyIssues(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] copy = new string[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = Normalize(source[i]);
            }

            return copy;
        }

        private static bool SequenceEquals(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
