using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Identity;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Passive authored contract for a future Pause visual surface.
    /// It carries the visual prefab reference, Pause content requirement, ContentAnchor owner, RuntimeContent identity/resource data and release policy,
    /// but it does not instantiate, bind, register, release, toggle Pause, change InputMode or alter Time.timeScale.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10C Pause visual surface authored contract; adds ContentAnchor owner for binding request derivation.")]
    public readonly struct PauseVisualSurfaceContract : IEquatable<PauseVisualSurfaceContract>
    {
        public PauseVisualSurfaceContract(
            PauseContentRequirementId surfaceId,
            PauseVisualSurfaceKind surfaceKind,
            GameObject visualPrefab,
            PauseContentRequirement contentRequirement,
            FrameworkIdentityKey anchorOwner,
            RuntimeContentOwner runtimeOwner,
            RuntimeContentId runtimeContentId,
            RuntimeMaterializationResource resource,
            RuntimeReleasePolicy releasePolicy,
            bool resetLocalTransform,
            string source,
            string reason)
        {
            if (!surfaceId.IsValid)
            {
                throw new ArgumentException("Pause visual surface contract requires a valid surface id.", nameof(surfaceId));
            }

            if (!Enum.IsDefined(typeof(PauseVisualSurfaceKind), surfaceKind) || surfaceKind == PauseVisualSurfaceKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(surfaceKind), surfaceKind, "Pause visual surface kind must be explicit.");
            }

            if (visualPrefab == null)
            {
                throw new ArgumentNullException(nameof(visualPrefab), "Pause visual surface contract requires an explicit visual prefab/template.");
            }

            if (!contentRequirement.IsValid)
            {
                throw new ArgumentException("Pause visual surface contract requires a valid Pause content requirement.", nameof(contentRequirement));
            }

            if (!anchorOwner.IsValid)
            {
                throw new ArgumentException("Pause visual surface contract requires a valid Content Anchor owner.", nameof(anchorOwner));
            }

            if (anchorOwner.Domain != ContentAnchorDeclaration.GetOwnerDomain(contentRequirement.AnchorScope))
            {
                throw new ArgumentException("Pause visual surface Content Anchor owner domain must match the requested Content Anchor scope.", nameof(anchorOwner));
            }

            if (!runtimeOwner.IsValid)
            {
                throw new ArgumentException("Pause visual surface contract requires a valid RuntimeContent owner.", nameof(runtimeOwner));
            }

            if (!runtimeContentId.IsValid)
            {
                throw new ArgumentException("Pause visual surface contract requires a valid RuntimeContent id.", nameof(runtimeContentId));
            }

            if (!resource.IsValid)
            {
                throw new ArgumentException("Pause visual surface contract requires a valid runtime materialization resource.", nameof(resource));
            }

            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), releasePolicy) || releasePolicy == RuntimeReleasePolicy.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(releasePolicy), releasePolicy, "Pause visual surface release policy must be explicit.");
            }

            if (contentRequirement.RuntimeScope != runtimeOwner.Scope)
            {
                throw new ArgumentException("Pause visual surface content requirement runtime scope must match the RuntimeContent owner scope.", nameof(contentRequirement));
            }

            if (contentRequirement.Owner != runtimeOwner)
            {
                throw new ArgumentException("Pause visual surface content requirement owner must match the RuntimeContent owner.", nameof(contentRequirement));
            }

            SurfaceId = surfaceId;
            SurfaceKind = surfaceKind;
            VisualPrefab = visualPrefab;
            ContentRequirement = contentRequirement;
            AnchorOwner = anchorOwner;
            RuntimeOwner = runtimeOwner;
            RuntimeContentId = runtimeContentId;
            Resource = resource;
            ReleasePolicy = releasePolicy;
            ResetLocalTransform = resetLocalTransform;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public PauseContentRequirementId SurfaceId { get; }

        public PauseVisualSurfaceKind SurfaceKind { get; }

        public GameObject VisualPrefab { get; }

        public PauseContentRequirement ContentRequirement { get; }

        public FrameworkIdentityKey AnchorOwner { get; }

        public ContentAnchorScope AnchorScope => ContentRequirement.AnchorScope;

        public ContentAnchorKind AnchorKind => ContentRequirement.AnchorKind;

        public ContentAnchorId AnchorId => ContentRequirement.AnchorId;

        public RuntimeContentOwner RuntimeOwner { get; }

        public RuntimeContentScope RuntimeScope => RuntimeOwner.Scope;

        public RuntimeContentId RuntimeContentId { get; }

        public RuntimeContentIdentity RuntimeIdentity => RuntimeContentIdentity.From(RuntimeOwner, RuntimeContentId.StableText);

        public RuntimeMaterializationResource Resource { get; }

        public RuntimeReleasePolicy ReleasePolicy { get; }

        public bool ResetLocalTransform { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool HasVisualPrefab => VisualPrefab != null;

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool IsValid => SurfaceId.IsValid
            && SurfaceKind != PauseVisualSurfaceKind.Unknown
            && HasVisualPrefab
            && ContentRequirement.IsValid
            && AnchorOwner.IsValid
            && RuntimeOwner.IsValid
            && RuntimeContentId.IsValid
            && Resource.IsValid;

        public bool Equals(PauseVisualSurfaceContract other)
        {
            return SurfaceId.Equals(other.SurfaceId)
                && SurfaceKind == other.SurfaceKind
                && ReferenceEquals(VisualPrefab, other.VisualPrefab)
                && ContentRequirement.Equals(other.ContentRequirement)
                && AnchorOwner.Equals(other.AnchorOwner)
                && RuntimeOwner.Equals(other.RuntimeOwner)
                && RuntimeContentId.Equals(other.RuntimeContentId)
                && Resource.Equals(other.Resource)
                && ReleasePolicy == other.ReleasePolicy
                && ResetLocalTransform == other.ResetLocalTransform
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseVisualSurfaceContract other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = SurfaceId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)SurfaceKind;
                hashCode = hashCode * 397 ^ (VisualPrefab != null ? VisualPrefab.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ ContentRequirement.GetHashCode();
                hashCode = hashCode * 397 ^ AnchorOwner.GetHashCode();
                hashCode = hashCode * 397 ^ RuntimeOwner.GetHashCode();
                hashCode = hashCode * 397 ^ RuntimeContentId.GetHashCode();
                hashCode = hashCode * 397 ^ Resource.GetHashCode();
                hashCode = hashCode * 397 ^ (int)ReleasePolicy;
                hashCode = hashCode * 397 ^ ResetLocalTransform.GetHashCode();
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
            string prefabText = VisualPrefab != null ? VisualPrefab.name : "<missing>";
            string sourceText = HasSource ? Source : "<none>";
            string reasonText = HasReason ? Reason : "<none>";
            return $"surface='{SurfaceId.StableText}' kind='{SurfaceKind}' prefab='{prefabText}' pauseRequirement=\"{ContentRequirement.ToDiagnosticString()}\" anchorOwner='{AnchorOwner.StableText}' runtimeIdentity='{RuntimeIdentity.StableText}' resource='{Resource.StableText}' releasePolicy='{ReleasePolicy}' resetLocalTransform='{ResetLocalTransform}' source='{sourceText}' reason='{reasonText}'";
        }

        public static bool operator ==(PauseVisualSurfaceContract left, PauseVisualSurfaceContract right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseVisualSurfaceContract left, PauseVisualSurfaceContract right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
