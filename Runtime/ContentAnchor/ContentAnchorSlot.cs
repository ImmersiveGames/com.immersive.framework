using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Passive declaration wrapper for a placement/mount slot.
    /// This type does not instantiate prefabs, bind consumers or reserve runtime ownership.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Content Anchor slot declaration wrapper introduced by F7C.")]
    public readonly struct ContentAnchorSlot : IEquatable<ContentAnchorSlot>
    {
        public ContentAnchorSlot(ContentAnchorDeclaration declaration)
        {
            if (!declaration.IsValid)
            {
                throw new ArgumentException("Content Anchor slot requires a valid declaration.", nameof(declaration));
            }

            if (declaration.Kind != ContentAnchorKind.Slot)
            {
                throw new ArgumentOutOfRangeException(nameof(declaration), declaration.Kind, "Content Anchor slot requires a Slot declaration kind.");
            }

            Declaration = declaration;
        }

        public ContentAnchorDeclaration Declaration { get; }

        public ContentAnchorId AnchorId => Declaration.AnchorId;

        public ContentAnchorScope Scope => Declaration.Scope;

        public ContentAnchorRequiredness Requiredness => Declaration.Requiredness;

        public string StableText => Declaration.StableText;

        public bool IsValid => Declaration is { IsValid: true, Kind: ContentAnchorKind.Slot };

        public bool Equals(ContentAnchorSlot other)
        {
            return Declaration.Equals(other.Declaration);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorSlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Declaration.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ContentAnchorSlot FromDeclaration(ContentAnchorDeclaration declaration)
        {
            return new ContentAnchorSlot(declaration);
        }

        public static bool operator ==(ContentAnchorSlot left, ContentAnchorSlot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentAnchorSlot left, ContentAnchorSlot right)
        {
            return !left.Equals(right);
        }
    }
}
