using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.UnityInput;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class PlayerSessionAccessGovernanceTests
    {
        private PlayerSlotProfile _slot;
        private PlayerParticipationRuntimeContext _participation;

        [SetUp]
        public void SetUp()
        {
            _slot = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            Assert.That(PlayerParticipationRuntimeContext.TryCreate(
                new[] { _slot },
                initialJoiningOpen: true,
                source: nameof(PlayerSessionAccessGovernanceTests),
                reason: "test setup",
                out _participation).Succeeded, Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            if (_slot != null)
                UnityEngine.Object.DestroyImmediate(_slot);
        }

        [Test]
        public void ScopedAccessHelper_IsProtectedForDerivedImplementationsOnly()
        {
            MethodInfo method = typeof(PlayerSessionScopedAccessConsumer).GetMethod(
                "TryGetAccess",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.IsFamily, Is.True);
            Assert.That(method.IsPublic, Is.False);
            Assert.That(method.IsAssembly, Is.False);
        }

        [Test]
        public void SessionLeaveOwner_RejectsPriorOccurrenceRevisionAfterSameSlotRejoins()
        {
            PlayerParticipationOperationResult firstJoin = JoinAvailableSlot();
            PlayerSlotRuntimeSnapshot occurrenceA = firstJoin.Slot;
            SessionPlayerLeaveRuntimeResult leaveA =
                _participation.TryBeginSessionPlayerLeave(
                    occurrenceA.PlayerSlotId,
                    occurrenceA.Revision,
                    "test",
                    "leave-occurrence-a");
            Assert.That(leaveA.Status,
                Is.EqualTo(SessionPlayerLeaveRuntimeStatus.SucceededLeaving));

            SessionPlayerLeaveRuntimeResult commitA =
                _participation.TryCommitSessionPlayerLeave(
                    leaveA.Token,
                    "test",
                    "commit-occurrence-a");
            Assert.That(commitA.Status,
                Is.EqualTo(SessionPlayerLeaveRuntimeStatus.SucceededCommitted));

            PlayerParticipationOperationResult occurrenceB = JoinAvailableSlot();
            Assert.That(occurrenceB.Slot.PlayerOccurrenceId,
                Is.Not.EqualTo(occurrenceA.PlayerOccurrenceId));

            SessionPlayerLeaveRuntimeResult staleLeave =
                _participation.TryBeginSessionPlayerLeave(
                    occurrenceA.PlayerSlotId,
                    occurrenceA.Revision,
                    "test",
                    "reject-stale-occurrence-a");
            PlayerParticipationSnapshot afterStaleLeave = _participation.CreateSnapshot();

            Assert.That(staleLeave.Status,
                Is.EqualTo(SessionPlayerLeaveRuntimeStatus.RejectedForeignOrStaleOccurrence));
            Assert.That(staleLeave.StateChanged, Is.False);
            Assert.That(afterStaleLeave.JoinedCount, Is.EqualTo(1));
            Assert.That(afterStaleLeave.Slots[0].IsJoined, Is.True);
            Assert.That(afterStaleLeave.Slots[0].PlayerOccurrenceId,
                Is.EqualTo(occurrenceB.Slot.PlayerOccurrenceId));
            Assert.That(afterStaleLeave.Slots[0].Revision,
                Is.EqualTo(occurrenceB.Slot.Revision));
        }

        [Test]
        public void AvailabilityOwner_MultipleTokensRejectForeignAndStaleWithoutEarlyRestore()
        {
            PlayerParticipationOperationResult reservation =
                _participation.TryReserveNextAvailableSlot("test", "reserve");
            Assert.That(reservation.Succeeded, Is.True, reservation.ToDiagnosticString());
            PlayerParticipationOperationResult joined = _participation.TryMarkJoined(
                reservation.ReservationToken,
                "test",
                "join");
            Assert.That(joined.Succeeded, Is.True, joined.ToDiagnosticString());
            PlayerSlotId slotId = joined.Slot.PlayerSlotId;
            string sessionContextId = _participation.CreateSnapshot().ContextId;

            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var gameplayMap = new InputActionMap("Gameplay");
            gameplayMap.AddAction("Move", InputActionType.Value);
            actions.AddActionMap(gameplayMap);
            gameplayMap.Enable();

            var playerObject = new GameObject("Availability owner PlayerInput test");
            playerObject.SetActive(false);
            PlayerInput playerInput = playerObject.AddComponent<PlayerInput>();
            playerInput.actions = actions;
            UnityPlayerInputGateAdapter adapter =
                playerObject.AddComponent<UnityPlayerInputGateAdapter>();
            SetField(adapter, "playerInput", playerInput);
            SetField(
                adapter,
                "gameplayActionMap",
                PlayerInputActionMapReference.From(gameplayMap));

            var moduleObject = new GameObject("Availability owner runtime test");
            moduleObject.SetActive(false);
            PlayerGameplayRuntimeHostModule module =
                moduleObject.AddComponent<PlayerGameplayRuntimeHostModule>();
            FrameworkRuntimeHost runtimeHost =
                moduleObject.AddComponent<FrameworkRuntimeHost>();
            Assert.That(runtimeHost.TryBeginStartup(), Is.True);
            runtimeHost.CompleteStartup(succeeded: true);
            PlayerActorPreparationRuntimeHostModule preparation =
                moduleObject.AddComponent<PlayerActorPreparationRuntimeHostModule>();
            PlayerGameplayOccupancyRuntimeContext occupancy =
                CreateUninitialized<PlayerGameplayOccupancyRuntimeContext>();
            PlayerGameplayAdmissionRuntimeContext admission =
                CreateUninitialized<PlayerGameplayAdmissionRuntimeContext>();
            PlayerGameplayCurrentContextRuntime current =
                CreateUninitialized<PlayerGameplayCurrentContextRuntime>();
            PlayerGameplayInputBindingRuntimeContext inputContext =
                CreateInputContext(sessionContextId, slotId, adapter);

            SetField(module, "_runtimeHost", runtimeHost);
            SetField(module, "_participationContext", _participation);
            SetField(module, "_preparationModule", preparation);
            SetField(module, "_occupancyContext", occupancy);
            SetField(module, "_inputContext", inputContext);
            SetField(module, "_admissionContext", admission);
            SetField(module, "_currentGameplayContext", current);

            try
            {
                PlayerGameplayAvailabilityBlockResult first =
                    module.RequestBlockRuntimeGameplay(slotId, "first", "first-token");
                PlayerGameplayAvailabilityBlockResult second =
                    module.RequestBlockRuntimeGameplay(slotId, "second", "second-token");

                Assert.That(first.Status,
                    Is.EqualTo(PlayerGameplayAvailabilityBlockStatus.SucceededBlocked),
                    first.Message);
                Assert.That(second.Status,
                    Is.EqualTo(PlayerGameplayAvailabilityBlockStatus.SucceededBlocked),
                    second.Message);
                Assert.That(first.BlockToken.IsValid, Is.True);
                Assert.That(second.BlockToken.IsValid, Is.True);
                Assert.That(first.BlockToken, Is.Not.EqualTo(second.BlockToken));
                Assert.That(gameplayMap.enabled, Is.False,
                    "The first owner token must project the physical gameplay block.");

                var foreignToken = new PlayerGameplayAvailabilityBlockToken(
                    "foreign-session",
                    first.BlockToken.PlayerOccurrenceId,
                    901);
                PlayerGameplayAvailabilityBlockResult foreign =
                    module.RequestReleaseRuntimeGameplay(foreignToken, "test", "foreign-token");
                Assert.That(foreign.Status,
                    Is.EqualTo(PlayerGameplayAvailabilityBlockStatus.RejectedForeignOrStaleToken));

                var staleToken = new PlayerGameplayAvailabilityBlockToken(
                    sessionContextId,
                    first.BlockToken.PlayerOccurrenceId,
                    902);
                PlayerGameplayAvailabilityBlockResult stale =
                    module.RequestReleaseRuntimeGameplay(staleToken, "test", "stale-token");
                Assert.That(stale.Status,
                    Is.EqualTo(PlayerGameplayAvailabilityBlockStatus.RejectedForeignOrStaleToken));
                Assert.That(gameplayMap.enabled, Is.False,
                    "Rejected token releases must preserve the active owner blocks.");

                PlayerGameplayAvailabilityBlockResult firstRelease =
                    module.RequestReleaseRuntimeGameplay(first.BlockToken, "test", "release-first");
                Assert.That(firstRelease.Status,
                    Is.EqualTo(PlayerGameplayAvailabilityBlockStatus.SucceededReleased),
                    firstRelease.Message);
                Assert.That(gameplayMap.enabled, Is.False,
                    "Releasing one of two exact tokens must not restore gameplay prematurely.");

                PlayerGameplayAvailabilityBlockResult repeatedFirstRelease =
                    module.RequestReleaseRuntimeGameplay(first.BlockToken, "test", "repeat-first");
                Assert.That(repeatedFirstRelease.Status,
                    Is.EqualTo(PlayerGameplayAvailabilityBlockStatus.RejectedForeignOrStaleToken));
                Assert.That(gameplayMap.enabled, Is.False);

                PlayerGameplayAvailabilityBlockResult finalRelease =
                    module.RequestReleaseRuntimeGameplay(second.BlockToken, "test", "release-second");
                Assert.That(finalRelease.Status,
                    Is.EqualTo(PlayerGameplayAvailabilityBlockStatus.SucceededReleased),
                    finalRelease.Message);
                Assert.That(gameplayMap.enabled, Is.True,
                    "The final exact token release must restore the original gameplay posture.");

                PlayerGameplayAvailabilityBlockResult staleAfterRelease =
                    module.RequestReleaseRuntimeGameplay(second.BlockToken, "test", "stale-after-release");
                Assert.That(staleAfterRelease.Status,
                    Is.EqualTo(PlayerGameplayAvailabilityBlockStatus.RejectedForeignOrStaleToken));
                Assert.That(gameplayMap.enabled, Is.True);
            }
            finally
            {
                gameplayMap.Enable();
                UnityEngine.Object.DestroyImmediate(moduleObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(actions);
            }
        }

        private static PlayerGameplayInputBindingRuntimeContext CreateInputContext(
            string sessionContextId,
            PlayerSlotId slotId,
            UnityPlayerInputGateAdapter adapter)
        {
            Type contextType = typeof(PlayerGameplayInputBindingRuntimeContext);
            var context = (PlayerGameplayInputBindingRuntimeContext)Activator.CreateInstance(
                contextType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { sessionContextId, null, null, new[] { slotId } },
                null);
            Assert.That(context, Is.Not.Null);
            var adapters = (IDictionary<PlayerSlotId, UnityPlayerInputGateAdapter>)contextType
                .GetField("_gateAdaptersBySlot", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(context);
            adapters.Add(slotId, adapter);
            return context;
        }

        private PlayerParticipationOperationResult JoinAvailableSlot()
        {
            PlayerParticipationOperationResult reservation =
                _participation.TryReserveNextAvailableSlot("test", "reserve");
            Assert.That(reservation.Succeeded, Is.True, reservation.ToDiagnosticString());
            PlayerParticipationOperationResult joined = _participation.TryMarkJoined(
                reservation.ReservationToken,
                "test",
                "join");
            Assert.That(joined.Succeeded, Is.True, joined.ToDiagnosticString());
            return joined;
        }

        private static T CreateUninitialized<T>() where T : class =>
            (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));

        private static void SetField<T>(object target, string name, T value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{name}' was not found on '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }
    }
}
