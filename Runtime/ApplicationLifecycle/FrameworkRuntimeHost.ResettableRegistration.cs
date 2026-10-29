using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        private ResettableOwnerRegistrationRuntime _resettableOwnerRegistration;
        private StableObjectBindingRegistry _stableObjectBindings;
        private ActorOccurrenceReferenceRuntime _actorOccurrenceReferenceRuntime;
        private ActorGroupRuntime _actorGroupRuntime;
        private bool _sessionActorGroupsReleased;

        /// <summary>
        /// IF-ADR-035 RESET-035-B host-owned Resettable registration over the existing ResetRegistry.
        /// No static access: the instance is injected into Route/Activity transactions only.
        /// </summary>
        internal ResettableOwnerRegistrationRuntime ResettableOwnerRegistration =>
            _resettableOwnerRegistration ??= new ResettableOwnerRegistrationRuntime(ResetRegistry);

        internal StableObjectBindingRegistry StableObjectBindings =>
            _stableObjectBindings ??= new StableObjectBindingRegistry();

        internal ActorOccurrenceReferenceRuntime ActorOccurrenceReferences =>
            _actorOccurrenceReferenceRuntime ??= new ActorOccurrenceReferenceRuntime();

        internal ActorGroupRuntime ActorGroups =>
            _actorGroupRuntime ??= new ActorGroupRuntime();

        private void ApplyResettableOwnerRegistration()
        {
            _gameFlowRuntime?.SetResettableOwnerRegistration(
                ResettableOwnerRegistration);
        }

        private void ApplyStableObjectBindings()
        {
            _gameFlowRuntime?.SetStableObjectBindingRegistry(StableObjectBindings);
            PlayerActorPreparationRuntimeHostModule playerPreparation =
                GetComponent<PlayerActorPreparationRuntimeHostModule>();
            if (playerPreparation != null)
            {
                ActorOccurrenceReferences.SetPlayerSource(
                    (IPlayerPreparedActorOccurrenceSource)playerPreparation);
            }
            _gameFlowRuntime?.SetActorOccurrenceReferenceRuntime(ActorOccurrenceReferences);
            _gameFlowRuntime?.SetActorGroupRuntime(ActorGroups);
        }

        private bool TryCommitPersistentActorOccurrencePublications(out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!_runtimeSessionScopeResult.HasOwner || _globalUiSceneRuntime == null)
            {
                diagnostic = "Persistent Actor publication requires the exact Session owner and materialized content roots.";
                return false;
            }
            RuntimeContentOwner owner = _runtimeSessionScopeResult.Owner;
            if (!ActorOccurrenceReferences.TryRegisterOwnerContent(
                    owner,
                    _globalUiSceneRuntime.PersistedRoots,
                    out diagnostic))
                return false;
            if (!ActorGroups.TryRegisterOwnerContent(
                    owner,
                    _globalUiSceneRuntime.PersistedRoots,
                    out diagnostic))
            {
                ActorOccurrenceReferences.TryRollbackOwner(owner, out _);
                return false;
            }
            if (ActorOccurrenceReferences.TryCommitOwner(owner, out diagnostic))
            {
                if (!ActorGroups.TryCommitOwner(owner, out diagnostic))
                {
                    ActorOccurrenceReferences.TryRollbackOwner(owner, out _);
                    ActorGroups.TryRollbackOwner(owner, out _);
                    return false;
                }
                _sessionActorGroupsReleased = false;
                ActorOccurrenceReferences.RefreshAll();
                return true;
            }
            ActorOccurrenceReferences.TryRollbackOwner(owner, out _);
            ActorGroups.TryRollbackOwner(owner, out _);
            return false;
        }

        internal bool TryReleaseSessionActorGroups(out string diagnostic)
        {
            diagnostic = string.Empty;
            if (_sessionActorGroupsReleased || _actorGroupRuntime == null)
            {
                _sessionActorGroupsReleased = true;
                return true;
            }
            if (!_runtimeSessionScopeResult.HasOwner)
            {
                diagnostic = "Actor Group release requires the exact initialized Session owner.";
                return false;
            }

            if (!ActorGroups.TryReleaseOwner(_runtimeSessionScopeResult.Owner, out diagnostic))
                return false;
            _sessionActorGroupsReleased = true;
            return true;
        }
    }
}
