using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Passive scene-authored declaration for a logical object entry.
    /// It does not bind a Unity GameObject as a runtime object, reset anything, spawn anything or create Player/Actor semantics.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Entry/Object Entry Declaration")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F13C passive scene-authored Object Entry declaration; F13J adds explicit Route/Activity owner authoring.")]
    public sealed class ObjectEntryDeclaration : MonoBehaviour
    {
        [Header("Object Entry")]
        [SerializeField] private string objectEntryId;
        [SerializeField] private ObjectEntryScope scope = ObjectEntryScope.Activity;
        [SerializeField] private RouteAsset routeOwner;
        [SerializeField] private ActivityAsset activityOwner;
        [SerializeField] private ObjectEntryRequiredness requiredness = ObjectEntryRequiredness.Required;
        [SerializeField] private string displayName;

        public string ObjectEntryIdText => objectEntryId;

        public ObjectEntryScope Scope => scope;

        public RouteAsset RouteOwner => routeOwner;

        public ActivityAsset ActivityOwner => activityOwner;

        public ObjectEntryRequiredness Requiredness => requiredness;

        public string DisplayName => displayName;

        public bool HasObjectEntryId => !string.IsNullOrWhiteSpace(objectEntryId);

        public bool HasRequiredAuthoredOwner
        {
            get
            {
                switch (scope)
                {
                    case ObjectEntryScope.Session:
                        return true;
                    case ObjectEntryScope.Route:
                        return routeOwner != null;
                    case ObjectEntryScope.Activity:
                        return activityOwner != null;
                    default:
                        return false;
                }
            }
        }

        public bool TryCreateDescriptor(out ObjectEntryDescriptor descriptor, out string issue)
        {
            descriptor = default;
            issue = string.Empty;

            if (string.IsNullOrWhiteSpace(objectEntryId))
            {
                issue = "Missing Object Entry Id.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ObjectEntryScope), scope) || scope == ObjectEntryScope.Unspecified)
            {
                issue = "Object Entry Scope must be explicit.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ObjectEntryRequiredness), requiredness) || requiredness == ObjectEntryRequiredness.Unspecified)
            {
                issue = "Object Entry Requiredness must be explicit.";
                return false;
            }

            if (scope == ObjectEntryScope.Route
                && routeOwner != null
                && string.IsNullOrWhiteSpace(routeOwner.PrimaryScenePath))
            {
                issue = "Route Owner requires a declared Primary Scene before it can provide a typed owner identity.";
                return false;
            }

            try
            {
                descriptor = new ObjectEntryDescriptor(
                    ObjectEntryId.From(objectEntryId.Trim()),
                    scope,
                    ObjectEntrySourceKind.SceneAuthored,
                    requiredness,
                    ResolveDisplayName(),
                    TryCreateAuthoredOwnerIdentity(out var authoredOwnerIdentity)
                        ? authoredOwnerIdentity
                        : (FrameworkIdentityKey?)null);
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                issue = exception.Message;
                return false;
            }
        }

        public ObjectEntryDescriptor CreateDescriptor()
        {
            if (TryCreateDescriptor(out var descriptor, out string issue))
            {
                return descriptor;
            }

            throw new InvalidOperationException(issue);
        }

        internal bool TryCreateScopedDescriptor(
            FrameworkIdentityKey ownerIdentity,
            out ObjectEntryDescriptor descriptor,
            out string issue)
        {
            descriptor = default;
            issue = string.Empty;

            if (!ownerIdentity.IsValid)
            {
                issue = "Object Entry owner identity is missing.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ObjectEntryScope), scope) || scope == ObjectEntryScope.Unspecified)
            {
                issue = "Object Entry Scope must be explicit.";
                return false;
            }

            if (ownerIdentity.Domain != ObjectEntryDescriptor.GetExpectedOwnerDomain(scope))
            {
                issue = $"Object Entry owner domain '{ownerIdentity.Domain}' does not match scope '{scope}'.";
                return false;
            }

            if (!TryCreateDescriptor(out var unscopedDescriptor, out issue))
            {
                return false;
            }

            try
            {
                descriptor = new ObjectEntryDescriptor(
                    unscopedDescriptor.Id,
                    unscopedDescriptor.Scope,
                    unscopedDescriptor.SourceKind,
                    unscopedDescriptor.Requiredness,
                    unscopedDescriptor.DisplayName,
                    ownerIdentity);
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                issue = exception.Message;
                return false;
            }
        }

        internal bool MatchesRouteOwner(RouteAsset route)
        {
            return route != null && routeOwner != null && ReferenceEquals(routeOwner, route);
        }

        internal bool MatchesActivityOwner(ActivityAsset activity)
        {
            return activity != null && activityOwner != null && ReferenceEquals(activityOwner, activity);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            string qaObjectEntryId,
            ObjectEntryScope qaScope,
            ObjectEntryRequiredness qaRequiredness,
            string qaDisplayName,
            RouteAsset qaRouteOwner = null,
            ActivityAsset qaActivityOwner = null)
        {
            objectEntryId = qaObjectEntryId;
            scope = qaScope;
            requiredness = qaRequiredness;
            displayName = qaDisplayName;
            routeOwner = qaRouteOwner;
            activityOwner = qaActivityOwner;
        }
#endif

        private bool TryCreateAuthoredOwnerIdentity(out FrameworkIdentityKey ownerIdentity)
        {
            switch (scope)
            {
                case ObjectEntryScope.Route when routeOwner != null && !string.IsNullOrWhiteSpace(routeOwner.PrimaryScenePath):
                    ownerIdentity = FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, routeOwner.PrimaryScenePath);
                    return true;
                case ObjectEntryScope.Activity when activityOwner != null && !string.IsNullOrWhiteSpace(activityOwner.ActivityName):
                    ownerIdentity = FrameworkIdentityKey.From(FrameworkIdentityDomain.Activity, activityOwner.ActivityName);
                    return true;
                default:
                    ownerIdentity = default;
                    return false;
            }
        }

        private string ResolveDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            return gameObject != null && !string.IsNullOrWhiteSpace(gameObject.name)
                ? gameObject.name.Trim()
                : objectEntryId.Trim();
        }
    }
}
