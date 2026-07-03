using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Passive declaration wrapper for a semantic reference point.
    /// This type does not move transforms, bind cameras, spawn actors or resolve gameplay consumers.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Content Anchor point declaration wrapper introduced by F7C.")]
    public readonly struct ContentAnchorPoint : IEquatable<ContentAnchorPoint>
    {
        public ContentAnchorPoint(ContentAnchorDeclaration declaration)
        {
            if (!declaration.IsValid)
            {
                throw new ArgumentException("Content Anchor point requires a valid declaration.", nameof(declaration));
            }

            if (declaration.Kind != ContentAnchorKind.Point)
            {
                throw new ArgumentOutOfRangeException(nameof(declaration), declaration.Kind, "Content Anchor point requires a Point declaration kind.");
            }

            Declaration = declaration;
        }

        public ContentAnchorDeclaration Declaration { get; }

        public ContentAnchorId AnchorId => Declaration.AnchorId;

        public ContentAnchorScope Scope => Declaration.Scope;

        public ContentAnchorRequiredness Requiredness => Declaration.Requiredness;

        public string StableText => Declaration.StableText;

        public bool IsValid => Declaration is { IsValid: true, Kind: ContentAnchorKind.Point };

        public bool Equals(ContentAnchorPoint other)
        {
            return Declaration.Equals(other.Declaration);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Declaration.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ContentAnchorPoint FromDeclaration(ContentAnchorDeclaration declaration)
        {
            return new ContentAnchorPoint(declaration);
        }

        public static bool operator ==(ContentAnchorPoint left, ContentAnchorPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentAnchorPoint left, ContentAnchorPoint right)
        {
            return !left.Equals(right);
        }
    }
}
