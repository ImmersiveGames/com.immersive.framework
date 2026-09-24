using System;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// API status: Internal. Route/Activity request-level Transition diagnostics for logging and smoke validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F24B request-level Transition diagnostics.")]
    internal readonly struct FrameworkTransitionDiagnostics
    {
        public FrameworkTransitionDiagnostics(
            TransitionScope scope,
            TransitionResult beforeResult,
            TransitionResult afterResult)
        {
            Scope = scope;
            BeforeResult = beforeResult;
            AfterResult = afterResult;
        }

        public TransitionScope Scope { get; }

        public TransitionResult BeforeResult { get; }

        public TransitionResult AfterResult { get; }

        public bool HasBefore => BeforeResult.IsValid;

        public bool HasAfter => AfterResult.IsValid;

        public bool HasDiagnostics => Scope != TransitionScope.Unknown && (HasBefore || HasAfter);

        public string ScopeText => HasDiagnostics ? Scope.ToString() : "<none>";

        public string VisualText => HasDiagnostics ? FormatVisualText() : "<none>";

        public string EffectText => HasDiagnostics ? FormatEffectText() : "<none>";

        public string EffectBeforeText => HasBefore ? FormatEffectStatus(BeforeResult) : "<none>";

        public string EffectAfterText => HasAfter ? FormatEffectStatus(AfterResult) : "<none>";

        public int EffectBlockingIssueCount
        {
            get
            {
                int count = 0;
                if (HasBefore)
                {
                    count += BeforeResult.EffectBlockingIssueCount;
                }

                if (HasAfter)
                {
                    count += AfterResult.EffectBlockingIssueCount;
                }

                return count;
            }
        }

        public int EffectAdapterCount => HasDiagnostics
            ? Math.Max(BeforeResult.EffectAdapterCount, AfterResult.EffectAdapterCount)
            : 0;

        public int EffectAdapterEvidenceCount => HasDiagnostics
            ? BeforeResult.EffectAdapterEvidenceCount + AfterResult.EffectAdapterEvidenceCount
            : 0;

        public int AppliedEffectAdapterEvidenceCount => HasDiagnostics
            ? BeforeResult.AppliedEffectAdapterEvidenceCount + AfterResult.AppliedEffectAdapterEvidenceCount
            : 0;

        public int SkippedEffectAdapterEvidenceCount => HasDiagnostics
            ? BeforeResult.SkippedEffectAdapterEvidenceCount + AfterResult.SkippedEffectAdapterEvidenceCount
            : 0;

        public int FailedEffectAdapterEvidenceCount => HasDiagnostics
            ? BeforeResult.FailedEffectAdapterEvidenceCount + AfterResult.FailedEffectAdapterEvidenceCount
            : 0;

        public int EffectAdapterEvidenceBlockingIssueCount => HasDiagnostics
            ? BeforeResult.EffectAdapterEvidenceBlockingIssueCount + AfterResult.EffectAdapterEvidenceBlockingIssueCount
            : 0;

        public string EffectAdapterEvidenceNamesText => HasDiagnostics
            ? FormatAdapterEvidenceNames()
            : "<none>";

        public string EffectAdapterEvidenceStatusesText => HasDiagnostics
            ? FormatAdapterEvidenceStatuses()
            : "<none>";

        public string TransitionText
        {
            get
            {
                if (!HasDiagnostics)
                {
                    return "<none>";
                }

                if (HasBefore && HasAfter && BeforeText == AfterText)
                {
                    return BeforeText;
                }

                if (HasBefore && !HasAfter)
                {
                    return BeforeText;
                }

                if (!HasBefore && HasAfter)
                {
                    return AfterText;
                }

                return $"{BeforeText}/{AfterText}";
            }
        }

        public string BeforeText => FormatResult(BeforeResult);

        public string AfterText => FormatResult(AfterResult);

        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                if (HasBefore)
                {
                    count += BeforeResult.BlockingIssueCount;
                }

                if (HasAfter)
                {
                    count += AfterResult.BlockingIssueCount;
                }

                return count;
            }
        }

        public static FrameworkTransitionDiagnostics Completed(
            TransitionScope scope,
            TransitionResult beforeResult,
            TransitionResult afterResult)
        {
            return new FrameworkTransitionDiagnostics(scope, beforeResult, afterResult);
        }

        private static string FormatResult(TransitionResult result)
        {
            if (!result.IsValid)
            {
                return "<none>";
            }

            return string.IsNullOrWhiteSpace(result.Message)
                ? result.Status.ToString()
                : result.Message;
        }

        private string FormatVisualText()
        {
            if (HasBefore && HasAfter)
            {
                if (string.Equals(BeforeResult.VisualText, AfterResult.VisualText, StringComparison.Ordinal))
                {
                    return BeforeResult.VisualText;
                }

                return $"{BeforeResult.VisualText}/{AfterResult.VisualText}";
            }

            if (HasBefore)
            {
                return BeforeResult.VisualText;
            }

            return HasAfter ? AfterResult.VisualText : "<none>";
        }

        private string FormatEffectText()
        {
            if (HasBefore && HasAfter)
            {
                if (BeforeResult.EffectKind == AfterResult.EffectKind)
                {
                    return FormatEffectKind(BeforeResult);
                }

                return $"{FormatEffectKind(BeforeResult)}/{FormatEffectKind(AfterResult)}";
            }

            if (HasBefore)
            {
                return FormatEffectKind(BeforeResult);
            }

            return HasAfter ? FormatEffectKind(AfterResult) : "<none>";
        }

        private static string FormatEffectKind(TransitionResult result)
        {
            return result.EffectKind != TransitionEffectKind.Unknown
                ? result.EffectKind.ToString()
                : result.VisualText;
        }

        private static string FormatEffectStatus(TransitionResult result)
        {
            return result.EffectStatus.ToString();
        }

        private string FormatAdapterEvidenceNames()
        {
            return FormatAdapterEvidence(item => item.AdapterName);
        }

        private string FormatAdapterEvidenceStatuses()
        {
            return FormatAdapterEvidence(item => item.Status.ToString());
        }

        private string FormatAdapterEvidence(Func<TransitionEffectAdapterEvidence, string> selector)
        {
            var builder = new StringBuilder();
            AppendAdapterEvidence(builder, BeforeResult, selector);
            AppendAdapterEvidence(builder, AfterResult, selector);
            return builder.Length > 0 ? builder.ToString() : "<none>";
        }

        private static void AppendAdapterEvidence(
            StringBuilder builder,
            TransitionResult result,
            Func<TransitionEffectAdapterEvidence, string> selector)
        {
            if (!result.IsValid || result.EffectAdapterEvidenceCount == 0)
            {
                return;
            }

            for (int i = 0; i < result.EffectAdapterEvidence.Count; i++)
            {
                string value = selector(result.EffectAdapterEvidence[i]);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(value);
            }
        }
    }
}
