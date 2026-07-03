using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Passive declaration wrapper for a semantic content root/container anchor.
    /// This type is not a runtime root registry and it does not create or own GameObjects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Content Anchor root declaration wrapper introduced by F7C.")]
    public readonly struct ContentAnchorRoot : IEquatable<ContentAnchorRoot>
    {
        public ContentAnchorRoot(ContentAnchorDeclaration declaration)
        {
            if (!declaration.IsValid)
            {
                throw new ArgumentException("Content Anchor root requires a valid declaration.", nameof(declaration));
            }

            if (declaration.Kind != ContentAnchorKind.Root)
            {
                throw new ArgumentOutOfRangeException(nameof(declaration), declaration.Kind, "Content Anchor root requires a Root declaration kind.");
            }

            Declaration = declaration;
        }

        public ContentAnchorDeclaration Declaration { get; }

        public ContentAnchorId AnchorId => Declaration.AnchorId;

        public ContentAnchorScope Scope => Declaration.Scope;

        public ContentAnchorRequiredness Requiredness => Declaration.Requiredness;

        public string StableText => Declaration.StableText;

        public bool IsValid => Declaration is { IsValid: true, Kind: ContentAnchorKind.Root };

        public bool Equals(ContentAnchorRoot other)
        {
            return Declaration.Equals(other.Declaration);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorRoot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Declaration.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ContentAnchorRoot FromDeclaration(ContentAnchorDeclaration declaration)
        {
            return new ContentAnchorRoot(declaration);
        }

        public static bool operator ==(ContentAnchorRoot left, ContentAnchorRoot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentAnchorRoot left, ContentAnchorRoot right)
        {
            return !left.Equals(right);
        }
    }
}
