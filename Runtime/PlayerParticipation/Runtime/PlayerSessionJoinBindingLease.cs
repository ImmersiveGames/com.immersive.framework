using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "I2 immutable identity captured by a scoped Player Session Join binding.")]
    internal readonly struct PlayerSessionJoinBindingIdentity
    {
        internal PlayerSessionJoinBindingIdentity(
            FrameworkRuntimeHost runtimeHost,
            PlayerParticipationRuntimeContext participationContext,
            PlayerSessionScopedAccessRuntimeHostModule bindingModule,
            PlayerSessionScopedBindingRecipient recipient,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            int routeOccurrenceSequence,
            int activityTransitionSequence,
            LocalPlayerProvisioningRuntimeHostModule manager)
        {
            RuntimeHost = runtimeHost;
            ParticipationContext = participationContext;
            BindingModule = bindingModule;
            Recipient = recipient;
            Scope = scope;
            Owner = owner;
            RouteOccurrenceSequence = routeOccurrenceSequence;
            ActivityTransitionSequence = activityTransitionSequence;
            Manager = manager;
        }

        internal FrameworkRuntimeHost RuntimeHost { get; }
        internal PlayerParticipationRuntimeContext ParticipationContext { get; }
        internal PlayerSessionScopedAccessRuntimeHostModule BindingModule { get; }
        internal PlayerSessionScopedBindingRecipient Recipient { get; }
        internal LocalPlayerProvisioningConsumerScope Scope { get; }
        internal RuntimeContentOwner Owner { get; }
        internal int RouteOccurrenceSequence { get; }
        internal int ActivityTransitionSequence { get; }
        internal LocalPlayerProvisioningRuntimeHostModule Manager { get; }

        internal bool MatchesCurrent(PlayerSessionJoinBindingIdentity current)
        {
            return ReferenceEquals(RuntimeHost, current.RuntimeHost) &&
                ReferenceEquals(ParticipationContext, current.ParticipationContext) &&
                ReferenceEquals(BindingModule, current.BindingModule) &&
                Recipient != null && Recipient.HasSameDestination(current.Recipient) &&
                Scope == current.Scope &&
                Owner == current.Owner &&
                RouteOccurrenceSequence == current.RouteOccurrenceSequence &&
                ActivityTransitionSequence == current.ActivityTransitionSequence &&
                ReferenceEquals(Manager, current.Manager);
        }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "I2 occurrence-bound Player Session Join recipient lease.")]
    internal enum PlayerSessionJoinBindingLeaseValidation
    {
        Current = 10,
        RejectedConfiguration = 20,
        RejectedUnavailable = 30
    }

    /// <summary>
    /// Captures one recipient binding against the exact Route/Activity occurrence and
    /// revalidates that same lifecycle authority at the point of use.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "I2 internal occurrence-bound scoped Join binding lease.")]
    internal sealed class PlayerSessionJoinBindingLease : IDisposable
    {
        private readonly PlayerSessionJoinBindingIdentity _identity;
        private bool _disposed;

        private PlayerSessionJoinBindingLease(
            PlayerSessionJoinBindingIdentity identity)
        {
            _identity = identity;
        }

        internal Component Recipient => _identity.Recipient != null ? _identity.Recipient.Component : null;
        internal LocalPlayerProvisioningConsumerScope Scope => _identity.Scope;
        internal RuntimeContentOwner Owner => _identity.Owner;
        internal FrameworkRuntimeHost RuntimeHost => _identity.RuntimeHost;
        internal PlayerParticipationRuntimeContext ParticipationContext => _identity.ParticipationContext;
        internal LocalPlayerProvisioningRuntimeHostModule Manager => _identity.Manager;
        internal int RouteOccurrenceSequence => _identity.RouteOccurrenceSequence;
        internal int ActivityTransitionSequence => _identity.ActivityTransitionSequence;
        internal bool IsDisposed => _disposed;

        internal static PlayerSessionJoinBindingLeaseValidation TryCapture(
            FrameworkRuntimeHost runtimeHost,
            PlayerSessionScopedAccessRuntimeHostModule bindingModule,
            PlayerParticipationRuntimeContext participationContext,
            PlayerSessionScopedBindingRecipient recipient,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            LocalPlayerProvisioningRuntimeHostModule manager,
            out PlayerSessionJoinBindingLease lease)
        {
            lease = null;
            if (!IsValidScopeOwnerPair(scope, owner))
                return PlayerSessionJoinBindingLeaseValidation.RejectedConfiguration;

            if (recipient == null)
                return PlayerSessionJoinBindingLeaseValidation.RejectedUnavailable;

            if (recipient.Component == null)
                return PlayerSessionJoinBindingLeaseValidation.RejectedUnavailable;

            if (recipient.Scope != scope || !recipient.TryValidateScope(out _))
            {
                return PlayerSessionJoinBindingLeaseValidation.RejectedConfiguration;
            }

            if (recipient.Component == null ||
                !TryResolveCurrentIdentity(
                    runtimeHost,
                    bindingModule,
                    participationContext,
                    recipient.Component,
                    scope,
                    owner,
                    out int routeOccurrenceSequence,
                    out int activityTransitionSequence,
                    out LocalPlayerProvisioningRuntimeHostModule currentManager) ||
                !ReferenceEquals(manager, currentManager))
            {
                return PlayerSessionJoinBindingLeaseValidation.RejectedUnavailable;
            }

            lease = new PlayerSessionJoinBindingLease(new PlayerSessionJoinBindingIdentity(
                runtimeHost,
                participationContext,
                bindingModule,
                recipient,
                scope,
                owner,
                routeOccurrenceSequence,
                activityTransitionSequence,
                manager));
            return PlayerSessionJoinBindingLeaseValidation.Current;
        }

        internal PlayerSessionJoinBindingLeaseValidation ValidateCurrent(out string issue)
        {
            if (_disposed)
            {
                issue = "Player Session Join binding lease was released.";
                return PlayerSessionJoinBindingLeaseValidation.RejectedUnavailable;
            }

            if (!IsValidScopeOwnerPair(_identity.Scope, _identity.Owner))
            {
                issue = "Player Session Join binding lease has an invalid authored scope or owner configuration.";
                return PlayerSessionJoinBindingLeaseValidation.RejectedConfiguration;
            }

            if (_identity.Recipient == null || _identity.Recipient.Component == null)
            {
                issue = "Player Session Join binding recipient was destroyed or released.";
                return PlayerSessionJoinBindingLeaseValidation.RejectedUnavailable;
            }

            if (_identity.Recipient.Scope != _identity.Scope)
            {
                issue = "Player Session Join binding recipient scope no longer matches its lease.";
                return PlayerSessionJoinBindingLeaseValidation.RejectedConfiguration;
            }

            if (!_identity.Recipient.TryValidateScope(out issue))
            {
                issue = string.IsNullOrWhiteSpace(issue)
                    ? "Player Session Join binding recipient scope no longer matches its lease."
                    : issue;
                return PlayerSessionJoinBindingLeaseValidation.RejectedConfiguration;
            }

            if (_identity.Recipient.Component == null ||
                !TryResolveCurrentIdentity(
                    _identity.RuntimeHost,
                    _identity.BindingModule,
                    _identity.ParticipationContext,
                    _identity.Recipient.Component,
                    _identity.Scope,
                    _identity.Owner,
                    out int routeOccurrenceSequence,
                    out int activityTransitionSequence,
                    out LocalPlayerProvisioningRuntimeHostModule currentManager) ||
                !_identity.MatchesCurrent(new PlayerSessionJoinBindingIdentity(
                    _identity.RuntimeHost,
                    _identity.ParticipationContext,
                    _identity.BindingModule,
                    _identity.Recipient,
                    _identity.Scope,
                    _identity.Owner,
                    routeOccurrenceSequence,
                    activityTransitionSequence,
                    currentManager)))
            {
                issue = "Player Session Join binding lease is unavailable because its recipient, owner or lifecycle occurrence is no longer current.";
                return PlayerSessionJoinBindingLeaseValidation.RejectedUnavailable;
            }

            if (_identity.Manager == null || !_identity.Manager.IsReady)
            {
                issue = "Manager-Provisioned Player Join is unavailable because its runtime is missing or not ready.";
                return PlayerSessionJoinBindingLeaseValidation.RejectedUnavailable;
            }

            issue = string.Empty;
            return PlayerSessionJoinBindingLeaseValidation.Current;
        }

        internal bool HasSameBindingIdentity(PlayerSessionJoinBindingLease other)
        {
            return other != null &&
                !_disposed &&
                !other._disposed &&
                _identity.MatchesCurrent(other._identity);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        private static bool IsValidScopeOwnerPair(
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner)
        {
            if (!scope.IsDefinedScope() || !owner.IsValid)
                return false;

            return scope == LocalPlayerProvisioningConsumerScope.Route
                ? owner.Scope == RuntimeContentScope.Route
                : owner.Scope == RuntimeContentScope.Activity;
        }

        private static bool TryResolveCurrentIdentity(
            FrameworkRuntimeHost runtimeHost,
            PlayerSessionScopedAccessRuntimeHostModule bindingModule,
            PlayerParticipationRuntimeContext participationContext,
            Component recipient,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            out int routeOccurrenceSequence,
            out int activityTransitionSequence,
            out LocalPlayerProvisioningRuntimeHostModule manager)
        {
            routeOccurrenceSequence = 0;
            activityTransitionSequence = 0;
            manager = null;

            if (runtimeHost == null || !runtimeHost.isActiveAndEnabled ||
                bindingModule == null || !bindingModule.isActiveAndEnabled ||
                !ReferenceEquals(
                    runtimeHost.GetComponent<PlayerSessionScopedAccessRuntimeHostModule>(),
                    bindingModule) ||
                participationContext == null || recipient == null ||
                !runtimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext currentParticipation) ||
                !ReferenceEquals(currentParticipation, participationContext))
            {
                return false;
            }

            var flow = runtimeHost.CurrentGameFlowRuntime;
            RouteLifecycleRuntime routeLifecycle = flow?.CurrentRouteLifecycleRuntime;
            RouteAsset currentRoute = flow?.CurrentRoute;
            if (routeLifecycle == null || currentRoute == null)
                return false;

            routeOccurrenceSequence = routeLifecycle.CurrentRouteOccurrenceSequence;
            if (routeOccurrenceSequence <= 0)
                return false;

            if (scope == LocalPlayerProvisioningConsumerScope.Route)
            {
                RuntimeContentOwner currentOwner = RuntimeContentOwner.Route(
                    currentRoute.RouteId.StableText,
                    currentRoute.RouteName,
                    RuntimeDefinitionToken.FromUnityObject(currentRoute));
                if (owner != currentOwner ||
                    !routeLifecycle.TryCreateCurrentRouteContentDiscoveryScope(
                        currentRoute,
                        out RouteContentDiscoveryScope routeScope) ||
                    !ContainsRecipient(SceneCompositionComponentQuery.GetComponents<Component>(routeScope), recipient))
                {
                    return false;
                }
            }
            else if (scope == LocalPlayerProvisioningConsumerScope.Activity)
            {
                ActivityAsset currentActivity = flow.CurrentActivity;
                ActivityFlowRuntime activityFlow = routeLifecycle.CurrentActivityFlowRuntime;
                ActivityReadinessOccurrence occurrence = flow.CurrentOccurrence;
                if (currentActivity == null || activityFlow == null ||
                    !occurrence.Matches(currentActivity, occurrence.TransitionSequence))
                {
                    return false;
                }

                RuntimeContentOwner currentOwner = RuntimeContentOwner.Activity(
                    currentActivity.ActivityId.StableText,
                    currentActivity.ActivityName,
                    RuntimeDefinitionToken.FromUnityObject(currentActivity));
                if (owner != currentOwner ||
                    !activityFlow.TryCreateCurrentActivityContentDiscoveryScope(
                        currentActivity,
                        out ActivityContentDiscoveryScope activityScope) ||
                    !ContainsRecipient(
                        SceneCompositionComponentQuery.GetComponents<Component>(activityScope, currentActivity),
                        recipient))
                {
                    return false;
                }

                activityTransitionSequence = occurrence.TransitionSequence;
            }
            else
            {
                return false;
            }

            manager = runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>();
            return true;
        }

        private static bool ContainsRecipient(
            IReadOnlyList<Component> candidates,
            Component recipient)
        {
            if (candidates == null || recipient == null)
                return false;

            for (int index = 0; index < candidates.Count; index++)
            {
                if (ReferenceEquals(candidates[index], recipient))
                    return true;
            }

            return false;
        }
    }
}
