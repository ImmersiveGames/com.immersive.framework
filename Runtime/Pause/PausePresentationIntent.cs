using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Intent-level presentation data for Pause.
    /// This does not show, update or hide UI and does not require an overlay adapter.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23E Pause presentation intent; no overlay adapter or UI materialization.")]
    public readonly struct PausePresentationIntent : IEquatable<PausePresentationIntent>
    {
        public PausePresentationIntent(
            PauseSnapshot snapshot,
            bool shouldBeVisible,
            PauseContentRequirement contentRequirement,
            string title,
            string detail,
            string source)
        {
            if (!snapshot.IsValid)
            {
                throw new ArgumentException("Pause presentation intent requires a valid Pause snapshot.", nameof(snapshot));
            }

            Snapshot = snapshot;
            ShouldBeVisible = shouldBeVisible;
            ContentRequirement = contentRequirement;
            Title = Normalize(title);
            Detail = Normalize(detail);
            Source = Normalize(source);
        }

        public PauseSnapshot Snapshot { get; }

        public bool ShouldBeVisible { get; }

        public PauseContentRequirement ContentRequirement { get; }

        public string Title { get; }

        public string Detail { get; }

        public string Source { get; }

        public bool IsValid => Snapshot.IsValid;

        public bool HasContentRequirement => ContentRequirement.IsValid;

        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

        public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool Equals(PausePresentationIntent other)
        {
            return Snapshot.Equals(other.Snapshot)
                && ShouldBeVisible == other.ShouldBeVisible
                && ContentRequirement.Equals(other.ContentRequirement)
                && string.Equals(Title, other.Title, StringComparison.Ordinal)
                && string.Equals(Detail, other.Detail, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PausePresentationIntent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Snapshot.GetHashCode();
                hashCode = hashCode * 397 ^ ShouldBeVisible.GetHashCode();
                hashCode = hashCode * 397 ^ ContentRequirement.GetHashCode();
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
            return $"state='{Snapshot.State}' visible='{ShouldBeVisible}' hasContentRequirement='{HasContentRequirement}' title='{titleText}' detail='{detailText}' source='{sourceText}'";
        }

        public static PausePresentationIntent FromSnapshot(
            PauseSnapshot snapshot,
            bool shouldBeVisible,
            string title,
            string detail,
            string source)
        {
            return new PausePresentationIntent(snapshot, shouldBeVisible, default, title, detail, source);
        }

        public static PausePresentationIntent FromSnapshot(
            PauseSnapshot snapshot,
            bool shouldBeVisible,
            PauseContentRequirement contentRequirement,
            string title,
            string detail,
            string source)
        {
            return new PausePresentationIntent(snapshot, shouldBeVisible, contentRequirement, title, detail, source);
        }

        public static bool operator ==(PausePresentationIntent left, PausePresentationIntent right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PausePresentationIntent left, PausePresentationIntent right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
