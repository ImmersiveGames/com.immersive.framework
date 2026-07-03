using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Policy for the Unity loading surface wired through a Game Application.
    /// NoneConfigured keeps loading explicit NoOp. Optional uses the prefab when present and skips explicitly when absent.
    /// Required instantiates the prefab and fails explicitly if the surface is missing or invalid.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface policy for GameApplication authoring.")]
    public enum LoadingSurfacePolicy
    {
        NoneConfigured = 0,
        Optional = 1,
        Required = 2
    }

    /// <summary>
    /// API status: Experimental. Adapter-facing action for a loading surface request.
    /// This is a visual adapter action only; it does not execute SceneLifecycle, RouteLifecycle or ActivityFlow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface adapter action boundary; no lifecycle ownership.")]
    public enum LoadingSurfaceAction
    {
        Unknown = 0,
        Show = 10,
        Update = 20,
        Hide = 30
    }

    /// <summary>
    /// API status: Experimental. Result status for a loading surface adapter action.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface adapter status boundary; no lifecycle ownership.")]
    public enum LoadingSurfaceResultStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededWithWarnings = 20,
        Skipped = 30,
        Failed = 40,
        Rejected = 50
    }

    /// <summary>
    /// API status: Experimental. Visual-facing request data for a loading surface adapter.
    /// It carries display intent and diagnostics only; it does not own scene loading.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface request boundary; visual adapter data only.")]
    public readonly struct LoadingSurfaceRequest : IEquatable<LoadingSurfaceRequest>
    {
        public LoadingSurfaceRequest(
            LoadingSurfaceAction action,
            bool shouldBeVisible,
            LoadingProgress progress,
            bool progressSupported,
            string title,
            string detail,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(LoadingSurfaceAction), action) || action == LoadingSurfaceAction.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(action), action, "Loading surface request action must be explicit.");
            }

            Action = action;
            ShouldBeVisible = shouldBeVisible;
            Progress = progress;
            ProgressSupported = progressSupported;
            Title = Normalize(title);
            Detail = Normalize(detail);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public LoadingSurfaceAction Action { get; }

        public bool ShouldBeVisible { get; }

        public LoadingProgress Progress { get; }

        public bool ProgressSupported { get; }

        public string Title { get; }

        public string Detail { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => Action != LoadingSurfaceAction.Unknown;

        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

        public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool Equals(LoadingSurfaceRequest other)
        {
            return Action == other.Action
                && ShouldBeVisible == other.ShouldBeVisible
                && Progress.Equals(other.Progress)
                && ProgressSupported == other.ProgressSupported
                && string.Equals(Title, other.Title, StringComparison.Ordinal)
                && string.Equals(Detail, other.Detail, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingSurfaceRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Action;
                hashCode = hashCode * 397 ^ ShouldBeVisible.GetHashCode();
                hashCode = hashCode * 397 ^ Progress.GetHashCode();
                hashCode = hashCode * 397 ^ ProgressSupported.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Title ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Detail ?? string.Empty);
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
            string titleText = HasTitle ? Title : "<none>";
            string detailText = HasDetail ? Detail : "<none>";
            string sourceText = HasSource ? Source : "<none>";
            string reasonText = HasReason ? Reason : "<none>";
            return $"action='{Action}' visible='{ShouldBeVisible}' progressSupported='{ProgressSupported}' progress='{Progress.NormalizedValue:0.###}' percent='{Progress.PercentRounded}' title='{titleText}' detail='{detailText}' source='{sourceText}' reason='{reasonText}'";
        }

        public static LoadingSurfaceRequest Show(
            string title,
            string detail,
            string source,
            string reason)
        {
            return Show(title, detail, source, reason, LoadingProgress.Zero, progressSupported: false);
        }

        public static LoadingSurfaceRequest Show(
            string title,
            string detail,
            string source,
            string reason,
            LoadingProgress progress,
            bool progressSupported)
        {
            return Create(
                LoadingSurfaceAction.Show,
                true,
                title,
                detail,
                source,
                reason,
                progress,
                progressSupported);
        }

        public static LoadingSurfaceRequest Update(
            string title,
            string detail,
            string source,
            string reason)
        {
            return Update(title, detail, source, reason, LoadingProgress.Zero, progressSupported: false);
        }

        public static LoadingSurfaceRequest Update(
            string title,
            string detail,
            string source,
            string reason,
            LoadingProgress progress,
            bool progressSupported)
        {
            return Create(
                LoadingSurfaceAction.Update,
                true,
                title,
                detail,
                source,
                reason,
                progress,
                progressSupported);
        }

        public static LoadingSurfaceRequest Hide(
            string title,
            string detail,
            string source,
            string reason)
        {
            return Hide(title, detail, source, reason, LoadingProgress.Zero, progressSupported: false);
        }

        public static LoadingSurfaceRequest Hide(
            string title,
            string detail,
            string source,
            string reason,
            LoadingProgress progress,
            bool progressSupported)
        {
            return Create(
                LoadingSurfaceAction.Hide,
                false,
                title,
                detail,
                source,
                reason,
                progress,
                progressSupported);
        }

        private static LoadingSurfaceRequest Create(
            LoadingSurfaceAction action,
            bool shouldBeVisible,
            string title,
            string detail,
            string source,
            string reason,
            LoadingProgress progress,
            bool progressSupported)
        {
            return new LoadingSurfaceRequest(
                action,
                shouldBeVisible,
                progressSupported ? progress : LoadingProgress.Zero,
                progressSupported,
                title,
                detail,
                source,
                reason);
        }

        public static bool operator ==(LoadingSurfaceRequest left, LoadingSurfaceRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingSurfaceRequest left, LoadingSurfaceRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }

    /// <summary>
    /// API status: Experimental. Domain-specific evidence for one loading surface adapter result.
    /// It is intentionally local to Loading and does not define a shared adapter abstraction.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F43 Loading surface adapter evidence for aggregate diagnostics.")]
    public readonly struct LoadingSurfaceAdapterEvidence : IEquatable<LoadingSurfaceAdapterEvidence>
    {
        public LoadingSurfaceAdapterEvidence(
            string adapterName,
            LoadingSurfaceResultStatus status,
            int issueCount,
            int blockingIssueCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(LoadingSurfaceResultStatus), status) || status == LoadingSurfaceResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Loading surface adapter evidence status must be explicit.");
            }

            AdapterName = Normalize(adapterName);
            Status = status;
            IssueCount = Math.Max(0, issueCount);
            BlockingIssueCount = Math.Max(0, blockingIssueCount);
            Message = Normalize(message);
        }

        public string AdapterName { get; }

        public LoadingSurfaceResultStatus Status { get; }

        public bool Applied => Status is LoadingSurfaceResultStatus.Succeeded or LoadingSurfaceResultStatus.SucceededWithWarnings;

        public bool Skipped => Status == LoadingSurfaceResultStatus.Skipped;

        public bool Failed => Status is LoadingSurfaceResultStatus.Failed or LoadingSurfaceResultStatus.Rejected;

        public int IssueCount { get; }

        public int BlockingIssueCount { get; }

        public string Message { get; }

        public bool HasIssues => IssueCount > 0;

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public bool Equals(LoadingSurfaceAdapterEvidence other)
        {
            return string.Equals(AdapterName, other.AdapterName, StringComparison.Ordinal)
                && Status == other.Status
                && IssueCount == other.IssueCount
                && BlockingIssueCount == other.BlockingIssueCount
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingSurfaceAdapterEvidence other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(AdapterName ?? string.Empty);
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ IssueCount;
                hashCode = hashCode * 397 ^ BlockingIssueCount;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            return $"adapter='{AdapterName.ToDiagnosticText()}' status='{Status}' applied='{Applied}' skipped='{Skipped}' failed='{Failed}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' message='{Message.ToDiagnosticText()}'";
        }

        public static LoadingSurfaceAdapterEvidence FromResult(LoadingSurfaceResult result)
        {
            if (!result.IsValid)
            {
                throw new ArgumentException("Loading surface adapter evidence requires a valid result.", nameof(result));
            }

            return new LoadingSurfaceAdapterEvidence(
                result.AdapterName,
                result.Status,
                result.IssueCount,
                result.BlockingIssueCount,
                result.Message);
        }

        public static bool operator ==(LoadingSurfaceAdapterEvidence left, LoadingSurfaceAdapterEvidence right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingSurfaceAdapterEvidence left, LoadingSurfaceAdapterEvidence right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }

    /// <summary>
    /// API status: Experimental. Explicit result for one loading surface adapter action.
    /// It reports visual adapter outcome only; it does not block or own SceneLifecycle, RouteLifecycle or ActivityFlow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface result boundary; no lifecycle ownership.")]
    public readonly struct LoadingSurfaceResult : IEquatable<LoadingSurfaceResult>
    {
        private readonly string[] _issues;
        private readonly LoadingSurfaceAdapterEvidence[] _adapterEvidence;

        public LoadingSurfaceResult(
            LoadingSurfaceRequest request,
            LoadingSurfaceResultStatus status,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
            : this(request, status, adapterName, message, issues, Array.Empty<LoadingSurfaceAdapterEvidence>())
        {
        }

        public LoadingSurfaceResult(
            LoadingSurfaceRequest request,
            LoadingSurfaceResultStatus status,
            string adapterName,
            string message,
            IReadOnlyList<string> issues,
            IReadOnlyList<LoadingSurfaceAdapterEvidence> adapterEvidence)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Loading surface result requires a valid request.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(LoadingSurfaceResultStatus), status) || status == LoadingSurfaceResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Loading surface result status must be explicit.");
            }

            Request = request;
            Status = status;
            AdapterName = Normalize(adapterName);
            Message = Normalize(message);
            _issues = CopyIssues(issues);
            _adapterEvidence = CopyAdapterEvidence(adapterEvidence);
        }

        public LoadingSurfaceRequest Request { get; }

        public LoadingSurfaceAction Action => Request.Action;

        public bool ShouldBeVisible => Request.ShouldBeVisible;

        public LoadingProgress Progress => Request.Progress;

        public bool ProgressSupported => Request.ProgressSupported;

        public string Title => Request.Title;

        public string Detail => Request.Detail;

        public string Source => Request.Source;

        public string Reason => Request.Reason;

        public LoadingSurfaceResultStatus Status { get; }

        public string AdapterName { get; }

        public string Message { get; }

        public IReadOnlyList<string> Issues => _issues ?? Array.Empty<string>();

        public int IssueCount => Issues.Count;

        public IReadOnlyList<LoadingSurfaceAdapterEvidence> AdapterEvidence => _adapterEvidence ?? Array.Empty<LoadingSurfaceAdapterEvidence>();

        public int AdapterEvidenceCount => AdapterEvidence.Count;

        public int AppliedAdapterEvidenceCount => CountAdapterEvidenceApplied();

        public int SkippedAdapterEvidenceCount => CountAdapterEvidenceSkipped();

        public int FailedAdapterEvidenceCount => CountAdapterEvidenceFailed();

        public int AdapterEvidenceIssueCount => CountAdapterEvidenceIssues();

        public int AdapterEvidenceBlockingIssueCount => CountAdapterEvidenceBlockingIssues();

        public bool HasAdapterEvidence => AdapterEvidenceCount > 0;

        public int BlockingIssueCount => Status is LoadingSurfaceResultStatus.Failed or LoadingSurfaceResultStatus.Rejected
            ? Math.Max(1, IssueCount)
            : 0;

        public bool HasIssues => IssueCount > 0;

        public bool Succeeded => Status == LoadingSurfaceResultStatus.Succeeded;

        public bool SucceededWithWarnings => Status == LoadingSurfaceResultStatus.SucceededWithWarnings;

        public bool Skipped => Status == LoadingSurfaceResultStatus.Skipped;

        public bool Failed => Status == LoadingSurfaceResultStatus.Failed;

        public bool Rejected => Status == LoadingSurfaceResultStatus.Rejected;

        public bool Completed => Succeeded || SucceededWithWarnings || Skipped;

        public bool IsValid => Request.IsValid && Status != LoadingSurfaceResultStatus.Unknown;

        public bool Equals(LoadingSurfaceResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && string.Equals(AdapterName, other.AdapterName, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(Issues, other.Issues)
                && SequenceEquals(AdapterEvidence, other.AdapterEvidence);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingSurfaceResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(AdapterName ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                for (int i = 0; i < Issues.Count; i++)
                {
                    hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Issues[i] ?? string.Empty);
                }

                for (int i = 0; i < AdapterEvidence.Count; i++)
                {
                    hashCode = hashCode * 397 ^ AdapterEvidence[i].GetHashCode();
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
            builder.Append($"adapter='{adapterText}' action='{Action}' status='{Status}' visible='{ShouldBeVisible}' progressSupported='{ProgressSupported}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' adapterEvidence='{AdapterEvidenceCount}' adapterEvidenceApplied='{AppliedAdapterEvidenceCount}' adapterEvidenceSkipped='{SkippedAdapterEvidenceCount}' adapterEvidenceFailed='{FailedAdapterEvidenceCount}' adapterEvidenceBlockingIssues='{AdapterEvidenceBlockingIssueCount}' message='{messageText}' request=({Request.ToDiagnosticString()})");
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

            if (HasAdapterEvidence)
            {
                builder.Append(" adapterEvidence=[");
                for (int i = 0; i < AdapterEvidence.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(AdapterEvidence[i].ToDiagnosticString());
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static LoadingSurfaceResult SucceededResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Succeeded, adapterName, message, Array.Empty<string>());
        }

        public static LoadingSurfaceResult SucceededResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<LoadingSurfaceAdapterEvidence> adapterEvidence)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Succeeded, adapterName, message, Array.Empty<string>(), adapterEvidence);
        }

        public static LoadingSurfaceResult SucceededWithWarningsResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.SucceededWithWarnings, adapterName, message, issues);
        }

        public static LoadingSurfaceResult SucceededWithWarningsResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<string> issues,
            IReadOnlyList<LoadingSurfaceAdapterEvidence> adapterEvidence)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.SucceededWithWarnings, adapterName, message, issues, adapterEvidence);
        }

        public static LoadingSurfaceResult SkippedResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Skipped, adapterName, message, Array.Empty<string>());
        }

        public static LoadingSurfaceResult SkippedResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<LoadingSurfaceAdapterEvidence> adapterEvidence)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Skipped, adapterName, message, Array.Empty<string>(), adapterEvidence);
        }

        public static LoadingSurfaceResult FailedResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Failed, adapterName, message, issues);
        }

        public static LoadingSurfaceResult FailedResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<string> issues,
            IReadOnlyList<LoadingSurfaceAdapterEvidence> adapterEvidence)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Failed, adapterName, message, issues, adapterEvidence);
        }

        public static LoadingSurfaceResult RejectedResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Rejected, adapterName, message, issues);
        }

        public static LoadingSurfaceResult RejectedResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<string> issues,
            IReadOnlyList<LoadingSurfaceAdapterEvidence> adapterEvidence)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Rejected, adapterName, message, issues, adapterEvidence);
        }

        public static bool operator ==(LoadingSurfaceResult left, LoadingSurfaceResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingSurfaceResult left, LoadingSurfaceResult right)
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

        private static LoadingSurfaceAdapterEvidence[] CopyAdapterEvidence(IReadOnlyList<LoadingSurfaceAdapterEvidence> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<LoadingSurfaceAdapterEvidence>();
            }

            var copy = new LoadingSurfaceAdapterEvidence[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (!Enum.IsDefined(typeof(LoadingSurfaceResultStatus), source[i].Status) || source[i].Status == LoadingSurfaceResultStatus.Unknown)
                {
                    throw new ArgumentException("Loading surface adapter evidence status must be explicit.", nameof(source));
                }

                copy[i] = source[i];
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

        private static bool SequenceEquals(IReadOnlyList<LoadingSurfaceAdapterEvidence> left, IReadOnlyList<LoadingSurfaceAdapterEvidence> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private int CountAdapterEvidenceApplied()
        {
            int count = 0;
            for (int i = 0; i < AdapterEvidence.Count; i++)
            {
                if (AdapterEvidence[i].Applied)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountAdapterEvidenceSkipped()
        {
            int count = 0;
            for (int i = 0; i < AdapterEvidence.Count; i++)
            {
                if (AdapterEvidence[i].Skipped)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountAdapterEvidenceFailed()
        {
            int count = 0;
            for (int i = 0; i < AdapterEvidence.Count; i++)
            {
                if (AdapterEvidence[i].Failed)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountAdapterEvidenceIssues()
        {
            int count = 0;
            for (int i = 0; i < AdapterEvidence.Count; i++)
            {
                count += AdapterEvidence[i].IssueCount;
            }

            return count;
        }

        private int CountAdapterEvidenceBlockingIssues()
        {
            int count = 0;
            for (int i = 0; i < AdapterEvidence.Count; i++)
            {
                count += AdapterEvidence[i].BlockingIssueCount;
            }

            return count;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }

    /// <summary>
    /// Adapter-facing contract for executing one loading surface request.
    /// Implementations may perform concrete Unity operations, but they must return explicit results and must not
    /// own SceneLifecycle, RouteLifecycle or ActivityFlow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface adapter contract; no registry or lifecycle ownership.")]
    public interface ILoadingSurfaceAdapter
    {
        /// <summary>Human-readable adapter name for diagnostics.</summary>
        string AdapterName { get; }

        /// <summary>Returns true when this adapter can present the supplied request.</summary>
        bool Supports(LoadingSurfaceRequest request);

        /// <summary>Shows the loading surface for a canonical request.</summary>
        LoadingSurfaceResult Show(LoadingSurfaceRequest request);

        /// <summary>Updates the loading surface from canonical request data.</summary>
        LoadingSurfaceResult Update(LoadingSurfaceRequest request);

        /// <summary>Hides the loading surface for a canonical request.</summary>
        LoadingSurfaceResult Hide(LoadingSurfaceRequest request);
    }

    /// <summary>
    /// Optional marker for loading surface adapters that can present request progress visually.
    /// This does not mean the loading lifecycle has a determinate source yet; it only states that
    /// the adapter can consume progress data when a request carries it.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F26C Loading surface progress presentation receiver contract.")]
    public interface ILoadingSurfaceProgressPresentationAdapter : ILoadingSurfaceAdapter
    {
        /// <summary>Returns true when progress UI references are configured and can receive request progress.</summary>
        bool HasProgressPresentation { get; }
    }

    /// <summary>
    /// Optional Awaitable extension for loading adapters that have a real visual settle boundary.
    /// Implementations still do not own SceneLifecycle, RouteLifecycle, ActivityFlow or GameFlow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D4 Awaitable Loading surface adapter boundary for visible show/hide phases.")]
    public interface IAsyncLoadingSurfaceAdapter : ILoadingSurfaceAdapter
    {
        /// <summary>Shows the loading surface and completes after the visual phase is ready.</summary>
        Awaitable<LoadingSurfaceResult> ShowAsync(LoadingSurfaceRequest request);

        /// <summary>Updates the loading surface and completes after the visual update is applied.</summary>
        Awaitable<LoadingSurfaceResult> UpdateAsync(LoadingSurfaceRequest request);

        /// <summary>Hides the loading surface and completes only after the visual phase is hidden.</summary>
        Awaitable<LoadingSurfaceResult> HideAsync(LoadingSurfaceRequest request);
    }

    /// <summary>
    /// API status: Internal. Explicit no-op loading surface adapter used for NoneConfigured/optional skip cases.
    /// It produces explicit skipped results and does not touch scene state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F24D no-op loading surface adapter; explicit skip only.")]
    internal sealed class NoOpLoadingSurfaceAdapter : ILoadingSurfaceAdapter
    {
        internal static readonly NoOpLoadingSurfaceAdapter Instance = new NoOpLoadingSurfaceAdapter();

        private NoOpLoadingSurfaceAdapter()
        {
        }

        public string AdapterName => "NoOp Loading Surface Adapter";

        public bool Supports(LoadingSurfaceRequest request)
        {
            return request.IsValid;
        }

        public LoadingSurfaceResult Show(LoadingSurfaceRequest request)
        {
            return BuildSkipped(request, "show");
        }

        public LoadingSurfaceResult Update(LoadingSurfaceRequest request)
        {
            return BuildSkipped(request, "update");
        }

        public LoadingSurfaceResult Hide(LoadingSurfaceRequest request)
        {
            return BuildSkipped(request, "hide");
        }

        private LoadingSurfaceResult BuildSkipped(LoadingSurfaceRequest request, string verb)
        {
            if (!request.IsValid)
            {
                return LoadingSurfaceResult.RejectedResult(
                    request,
                    AdapterName,
                    $"No-op loading surface adapter rejected invalid request for {verb}.",
                    new[] { "loading-surface-invalid-request" });
            }

            return LoadingSurfaceResult.SkippedResult(
                request,
                AdapterName,
                $"Loading surface {verb} skipped because no Unity surface is configured.");
        }
    }
}
