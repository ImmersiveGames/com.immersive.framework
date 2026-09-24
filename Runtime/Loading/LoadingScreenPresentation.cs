using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Visual-facing presentation data for loading screen adapters.
    /// It wraps a canonical LoadingOperation and display text; it does not own UI, prefab, fade, curtain or lifecycle execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22E Loading Screen presentation boundary; visual adapter data only.")]
    public readonly struct LoadingScreenPresentation : IEquatable<LoadingScreenPresentation>
    {
        public LoadingScreenPresentation(
            LoadingOperation operation,
            bool shouldBeVisible,
            string title,
            string detail,
            string source)
        {
            if (!operation.IsValid)
            {
                throw new ArgumentException("Loading screen presentation requires a valid Loading operation.", nameof(operation));
            }

            Operation = operation;
            ShouldBeVisible = shouldBeVisible;
            Title = Normalize(title);
            Detail = Normalize(detail);
            Source = Normalize(source);
        }

        public LoadingOperation Operation { get; }

        public LoadingOperationId OperationId => Operation.OperationId;

        public LoadingOperationStatus OperationStatus => Operation.Status;

        public LoadingProgress Progress => Operation.Progress;

        public bool ShouldBeVisible { get; }

        public string Title { get; }

        public string Detail { get; }

        public string Source { get; }

        public bool IsValid => Operation.IsValid;

        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

        public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool Equals(LoadingScreenPresentation other)
        {
            return Operation.Equals(other.Operation)
                && ShouldBeVisible == other.ShouldBeVisible
                && string.Equals(Title, other.Title, StringComparison.Ordinal)
                && string.Equals(Detail, other.Detail, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingScreenPresentation other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Operation.GetHashCode();
                hashCode = hashCode * 397 ^ ShouldBeVisible.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Title ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Detail ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
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
            return $"operation='{OperationId.StableText}' status='{OperationStatus}' visible='{ShouldBeVisible}' progress='{Progress.NormalizedValue:0.###}' percent='{Progress.PercentRounded}' title='{titleText}' detail='{detailText}' source='{sourceText}'";
        }

        public static LoadingScreenPresentation FromOperation(
            LoadingOperation operation,
            bool shouldBeVisible,
            string title,
            string detail,
            string source)
        {
            return new LoadingScreenPresentation(operation, shouldBeVisible, title, detail, source);
        }

        public static bool operator ==(LoadingScreenPresentation left, LoadingScreenPresentation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingScreenPresentation left, LoadingScreenPresentation right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
