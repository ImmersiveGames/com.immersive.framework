using System;
using System.Runtime.CompilerServices;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using UnityEngine;

namespace Immersive.Framework.RuntimeContent
{
    public enum StableObjectOwnerSelectorKind { Unspecified = 0, Route = 10, Activity = 20 }

    /// <summary>Stable Object Entry ID with an optional exact typed Route or Activity selector.</summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 stable Object Entry identity reference.")]
    public struct StableObjectReference : IEquatable<StableObjectReference>
    {
        [SerializeField] private string objectEntryIdText;
        [SerializeField] private StableObjectOwnerSelectorKind ownerSelectorKind;
        [SerializeField] private RouteAsset routeOwner;
        [SerializeField] private ActivityAsset activityOwner;

        public StableObjectReference(ObjectEntryId objectEntryId)
            : this(objectEntryId, StableObjectOwnerSelectorKind.Unspecified, null, null) { }
        public StableObjectReference(ObjectEntryId objectEntryId, RouteAsset route)
            : this(objectEntryId, StableObjectOwnerSelectorKind.Route, route, null) { }
        public StableObjectReference(ObjectEntryId objectEntryId, ActivityAsset activity)
            : this(objectEntryId, StableObjectOwnerSelectorKind.Activity, null, activity) { }

        private StableObjectReference(ObjectEntryId objectEntryId, StableObjectOwnerSelectorKind kind, RouteAsset route, ActivityAsset activity)
        {
            if (!objectEntryId.IsValid) throw new ArgumentException("Stable Object Reference requires a valid ObjectEntryId.", nameof(objectEntryId));
            if (!Enum.IsDefined(typeof(StableObjectOwnerSelectorKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == StableObjectOwnerSelectorKind.Route && (route == null || !route.HasValidRouteId))
                throw new ArgumentException("Route selector requires a RouteAsset with a valid RouteId.", nameof(route));
            if (kind == StableObjectOwnerSelectorKind.Activity && (activity == null || !activity.HasValidActivityId))
                throw new ArgumentException("Activity selector requires an ActivityAsset with a valid ActivityId.", nameof(activity));
            objectEntryIdText = objectEntryId.Value.Value;
            ownerSelectorKind = kind;
            routeOwner = route;
            activityOwner = activity;
        }

        public StableObjectOwnerSelectorKind OwnerSelectorKind => ownerSelectorKind;
        public RouteAsset RouteOwner => routeOwner;
        public ActivityAsset ActivityOwner => activityOwner;
        public bool HasOwnerSelector => ownerSelectorKind != StableObjectOwnerSelectorKind.Unspecified;
        public ObjectEntryId ObjectEntryId => TryGetObjectEntryId(out ObjectEntryId value) ? value : default;
        public bool IsValid => TryGetObjectEntryId(out _) && TryGetOwnerSelector(out _, out _);

        public bool TryGetObjectEntryId(out ObjectEntryId value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(objectEntryIdText)) return false;
            try
            {
                value = Immersive.Framework.ObjectEntry.ObjectEntryId.From(objectEntryIdText.Trim());
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public bool TryGetOwnerSelector(out FrameworkIdentityKey? ownerIdentity, out RuntimeDefinitionToken? ownerDefinitionToken)
        {
            ownerIdentity = null;
            ownerDefinitionToken = null;
            switch (ownerSelectorKind)
            {
                case StableObjectOwnerSelectorKind.Unspecified: return routeOwner == null && activityOwner == null;
                case StableObjectOwnerSelectorKind.Route:
                    if (routeOwner == null || !routeOwner.HasValidRouteId) return false;
                    ownerIdentity = FrameworkIdentityKey.From(routeOwner.RouteId);
                    ownerDefinitionToken = RuntimeDefinitionToken.FromUnityObject(routeOwner);
                    return ownerDefinitionToken.Value.IsValid;
                case StableObjectOwnerSelectorKind.Activity:
                    if (activityOwner == null || !activityOwner.HasValidActivityId) return false;
                    ownerIdentity = FrameworkIdentityKey.From(activityOwner.ActivityId);
                    ownerDefinitionToken = RuntimeDefinitionToken.FromUnityObject(activityOwner);
                    return ownerDefinitionToken.Value.IsValid;
                default: return false;
            }
        }

        public static StableObjectReference ForEntry(ObjectEntryId id) => new StableObjectReference(id);
        public static StableObjectReference ForRoute(ObjectEntryId id, RouteAsset route) => new StableObjectReference(id, route);
        public static StableObjectReference ForActivity(ObjectEntryId id, ActivityAsset activity) => new StableObjectReference(id, activity);

        public bool Equals(StableObjectReference other) => string.Equals(objectEntryIdText, other.objectEntryIdText, StringComparison.Ordinal)
            && ownerSelectorKind == other.ownerSelectorKind && ReferenceEquals(routeOwner, other.routeOwner)
            && ReferenceEquals(activityOwner, other.activityOwner);
        public override bool Equals(object obj) => obj is StableObjectReference other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(objectEntryIdText ?? string.Empty);
                hash = hash * 397 ^ (int)ownerSelectorKind;
                hash = hash * 397 ^ GetObjectHash(routeOwner);
                return hash * 397 ^ GetObjectHash(activityOwner);
            }
        }
        public override string ToString() => $"objectEntryId='{ObjectEntryId.StableText}' ownerSelector='{ownerSelectorKind}'";
        public static bool operator ==(StableObjectReference left, StableObjectReference right) => left.Equals(right);
        public static bool operator !=(StableObjectReference left, StableObjectReference right) => !left.Equals(right);

        private static int GetObjectHash(UnityEngine.Object value) =>
            ReferenceEquals(value, null) ? 0 : RuntimeHelpers.GetHashCode(value);
    }
}
