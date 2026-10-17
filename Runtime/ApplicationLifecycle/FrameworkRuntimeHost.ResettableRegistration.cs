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
            if (ActorOccurrenceReferences.TryCommitOwner(owner, out diagnostic))
            {
                ActorOccurrenceReferences.RefreshAll();
                return true;
            }
            ActorOccurrenceReferences.TryRollbackOwner(owner, out _);
            return false;
        }
    }
}
