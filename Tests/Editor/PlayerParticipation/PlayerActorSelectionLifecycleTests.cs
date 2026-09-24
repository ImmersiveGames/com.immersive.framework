using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation.Editor.Tests
{
    public sealed class PlayerActorSelectionLifecycleTests
    {
        private const string Source = nameof(PlayerActorSelectionLifecycleTests);
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _createdObjects.Count - 1; index >= 0; index--)
            {
                if (_createdObjects[index] != null)
                {
                    Object.DestroyImmediate(_createdObjects[index]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void SelectActor_JoinedSlotWithoutSelection_CommitsExactlyOneRevision()
        {
            ActorProfile actor = CreateActor("actor.select");
            PlayerSlotProfile slotProfile = CreateSlot("player.select", null);
            PlayerParticipationRuntimeContext context = CreateContext(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                slotProfile);
            PlayerSlotId slotId = JoinNextAvailableSlot(context);
            PlayerParticipationSnapshot before = context.CreateSnapshot();
            PlayerSlotRuntimeSnapshot beforeSlot = GetSlot(context, slotId);

            PlayerActorSelectionResult result = context.TrySelectActorProfile(
                Request(slotId, actor));

            PlayerSlotRuntimeSnapshot afterSlot = GetSlot(context, slotId);
            Assert.That(result.Status, Is.EqualTo(PlayerActorSelectionStatus.SucceededSelected));
            Assert.That(result.StateChanged, Is.True);
            Assert.That(afterSlot.SelectedActorProfile, Is.SameAs(actor));
            Assert.That(afterSlot.SelectionRevision,
                Is.EqualTo(beforeSlot.SelectionRevision + 1));
            Assert.That(afterSlot.Revision, Is.EqualTo(beforeSlot.Revision + 1));
            Assert.That(context.CreateSnapshot().Revision, Is.EqualTo(before.Revision + 1));
        }

        [Test]
        public void SelectDefaultActor_ResolveConfiguredDefault_SelectsConfiguredProfile()
        {
            ActorProfile defaultActor = CreateActor("actor.default");
            PlayerSlotProfile slotProfile = CreateSlot("player.default", defaultActor);
            PlayerParticipationRuntimeContext context = CreateContext(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                slotProfile);
            PlayerSlotId slotId = JoinNextAvailableSlot(context);

            PlayerActorSelectionResult result = context.TrySelectDefaultActor(
                slotId,
                PlayerActorSelectionRequest.NoExpectedRevision,
                Source,
                "select-default");

            Assert.That(result.Status, Is.EqualTo(PlayerActorSelectionStatus.SucceededSelected));
            Assert.That(result.SelectedActorProfile, Is.SameAs(defaultActor));
            Assert.That(GetSlot(context, slotId).SelectedActorProfile, Is.SameAs(defaultActor));
        }

        [Test]
        public void SelectDefaultActor_LeaveUnresolved_RejectsWithoutChangingRevision()
        {
            ActorProfile defaultActor = CreateActor("actor.unresolved-default");
            PlayerSlotProfile slotProfile = CreateSlot("player.unresolved", defaultActor);
            PlayerParticipationRuntimeContext context = CreateContext(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                PlayerActorResolutionPolicy.LeaveUnresolved,
                slotProfile);
            PlayerSlotId slotId = JoinNextAvailableSlot(context);
            PlayerParticipationSnapshot before = context.CreateSnapshot();
            PlayerSlotRuntimeSnapshot beforeSlot = GetSlot(context, slotId);

            PlayerActorSelectionResult result = context.TrySelectDefaultActor(
                slotId,
                PlayerActorSelectionRequest.NoExpectedRevision,
                Source,
                "select-default-disabled");

            Assert.That(result.Status,
                Is.EqualTo(PlayerActorSelectionStatus.RejectedDefaultResolutionDisabled));
            AssertUnchanged(context, slotId, before, beforeSlot);
        }

        [Test]
        public void SelectDefaultActor_MissingConfiguredDefault_RejectsWithoutFallback()
        {
            PlayerSlotProfile slotProfile = CreateSlot("player.no-default", null);
            PlayerParticipationRuntimeContext context = CreateContext(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                slotProfile);
            PlayerSlotId slotId = JoinNextAvailableSlot(context);
            PlayerParticipationSnapshot before = context.CreateSnapshot();
            PlayerSlotRuntimeSnapshot beforeSlot = GetSlot(context, slotId);

            PlayerActorSelectionResult result = context.TrySelectDefaultActor(
                slotId,
                PlayerActorSelectionRequest.NoExpectedRevision,
                Source,
                "missing-default");

            Assert.That(result.Status,
                Is.EqualTo(PlayerActorSelectionStatus.RejectedActorProfileMissing));
            AssertUnchanged(context, slotId, before, beforeSlot);
        }

        [Test]
        public void SelectActor_InvalidActorProfile_RejectsWithoutStateCorruption()
        {
            ActorProfile invalidActor = CreateActor("actor.invalid");
            SetString(invalidActor, "actorProfileId", string.Empty);
            PlayerSlotProfile slotProfile = CreateSlot("player.invalid-actor", null);
            PlayerParticipationRuntimeContext context = CreateContext(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                slotProfile);
            PlayerSlotId slotId = JoinNextAvailableSlot(context);
            PlayerParticipationSnapshot before = context.CreateSnapshot();
            PlayerSlotRuntimeSnapshot beforeSlot = GetSlot(context, slotId);

            PlayerActorSelectionResult result = context.TrySelectActorProfile(
                Request(slotId, invalidActor));

            Assert.That(result.Status,
                Is.EqualTo(PlayerActorSelectionStatus.RejectedActorProfileInvalid));
            AssertUnchanged(context, slotId, before, beforeSlot);
        }

        [Test]
        public void SelectActor_SameProfileIsIdempotent_AndDifferentProfileRequiresReplace()
        {
            ActorProfile actorA = CreateActor("actor.a");
            ActorProfile actorB = CreateActor("actor.b");
            PlayerSlotProfile slotProfile = CreateSlot("player.idempotent", null);
            PlayerParticipationRuntimeContext context = CreateContext(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                slotProfile);
            PlayerSlotId slotId = JoinNextAvailableSlot(context);
            context.TrySelectActorProfile(Request(slotId, actorA));
            PlayerParticipationSnapshot before = context.CreateSnapshot();
            PlayerSlotRuntimeSnapshot beforeSlot = GetSlot(context, slotId);

            PlayerActorSelectionResult same = context.TrySelectActorProfile(
                Request(slotId, actorA));
            PlayerActorSelectionResult different = context.TrySelectActorProfile(
                Request(slotId, actorB));

            Assert.That(same.Status, Is.EqualTo(PlayerActorSelectionStatus.SucceededSelected));
            Assert.That(same.StateChanged, Is.False);
            Assert.That(different.Status,
                Is.EqualTo(PlayerActorSelectionStatus.RejectedInvalidRequest));
            AssertUnchanged(context, slotId, before, beforeSlot);
        }

        [Test]
        public void ReplaceAndClear_BeforePreparation_CommitExactlyOneRevision()
        {
            ActorProfile actorA = CreateActor("actor.replace.a");
            ActorProfile actorB = CreateActor("actor.replace.b");
            PlayerSlotProfile slotProfile = CreateSlot("player.replace", null);
            PlayerParticipationRuntimeContext context = CreateContext(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                slotProfile);
            PlayerSlotId slotId = JoinNextAvailableSlot(context);
            context.TrySelectActorProfile(Request(slotId, actorA));
            PlayerParticipationSnapshot beforeReplace = context.CreateSnapshot();
            PlayerSlotRuntimeSnapshot beforeReplaceSlot = GetSlot(context, slotId);

            PlayerActorSelectionResult replace = context.TryReplaceActorSelection(
                Request(slotId, actorB));
            PlayerSlotRuntimeSnapshot afterReplaceSlot = GetSlot(context, slotId);
            PlayerActorSelectionResult clear = context.TryClearActorSelection(
                new PlayerActorSelectionRequest(
                    slotId,
                    null,
                    Source,
                    "clear",
                    PlayerActorSelectionRequest.NoExpectedRevision));
            PlayerSlotRuntimeSnapshot afterClearSlot = GetSlot(context, slotId);
            PlayerActorSelectionResult clearAgain = context.TryClearActorSelection(
                new PlayerActorSelectionRequest(
                    slotId,
                    null,
                    Source,
                    "clear-idempotent",
                    PlayerActorSelectionRequest.NoExpectedRevision));

            Assert.That(replace.Status, Is.EqualTo(PlayerActorSelectionStatus.SucceededReplaced));
            Assert.That(afterReplaceSlot.SelectedActorProfile, Is.SameAs(actorB));
            Assert.That(afterReplaceSlot.SelectionRevision,
                Is.EqualTo(beforeReplaceSlot.SelectionRevision + 1));
            Assert.That(context.CreateSnapshot().Revision,
                Is.EqualTo(beforeReplace.Revision + 2));
            Assert.That(clear.Status, Is.EqualTo(PlayerActorSelectionStatus.SucceededCleared));
            Assert.That(afterClearSlot.HasSelectedActor, Is.False);
            Assert.That(afterClearSlot.SelectionRevision,
                Is.EqualTo(afterReplaceSlot.SelectionRevision + 1));
            Assert.That(clearAgain.Status,
                Is.EqualTo(PlayerActorSelectionStatus.SucceededCleared));
            Assert.That(clearAgain.StateChanged, Is.False);
            Assert.That(context.CreateSnapshot().Revision,
                Is.EqualTo(beforeReplace.Revision + 2));
        }

        [Test]
        public void AllSelectionMutations_NotJoined_RejectWithoutStateCorruption()
        {
            ActorProfile actor = CreateActor("actor.not-joined");
            PlayerSlotProfile slotProfile = CreateSlot("player.not-joined", actor);
            PlayerParticipationRuntimeContext context = CreateContext(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                slotProfile);
            PlayerSlotId slotId = slotProfile.PlayerSlotId;
            PlayerParticipationSnapshot before = context.CreateSnapshot();
            PlayerSlotRuntimeSnapshot beforeSlot = GetSlot(context, slotId);

            PlayerActorSelectionResult select = context.TrySelectActorProfile(Request(slotId, actor));
            PlayerActorSelectionResult selectDefault = context.TrySelectDefaultActor(
                slotId,
                PlayerActorSelectionRequest.NoExpectedRevision,
                Source,
                "default-not-joined");
            PlayerActorSelectionResult replace = context.TryReplaceActorSelection(Request(slotId, actor));
            PlayerActorSelectionResult clear = context.TryClearActorSelection(
                new PlayerActorSelectionRequest(
                    slotId,
                    null,
                    Source,
                    "clear-not-joined",
                    PlayerActorSelectionRequest.NoExpectedRevision));

            AssertRejectedSlotNotJoined(select);
            AssertRejectedSlotNotJoined(selectDefault);
            AssertRejectedSlotNotJoined(replace);
            AssertRejectedSlotNotJoined(clear);
            AssertUnchanged(context, slotId, before, beforeSlot);
        }

        [Test]
        public void SelectActor_StaleExpectedRevision_RejectsWithoutStateCorruption()
        {
            ActorProfile actorA = CreateActor("actor.stale.a");
            ActorProfile actorB = CreateActor("actor.stale.b");
            PlayerSlotProfile slotProfile = CreateSlot("player.stale", null);
            PlayerParticipationRuntimeContext context = CreateContext(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                slotProfile);
            PlayerSlotId slotId = JoinNextAvailableSlot(context);
            context.TrySelectActorProfile(Request(slotId, actorA));
            PlayerParticipationSnapshot before = context.CreateSnapshot();
            PlayerSlotRuntimeSnapshot beforeSlot = GetSlot(context, slotId);

            PlayerActorSelectionResult result = context.TryReplaceActorSelection(
                new PlayerActorSelectionRequest(
                    slotId,
                    actorB,
                    Source,
                    "stale",
                    beforeSlot.SelectionRevision - 1));

            Assert.That(result.Status,
                Is.EqualTo(PlayerActorSelectionStatus.RejectedStaleSelectionRevision));
            AssertUnchanged(context, slotId, before, beforeSlot);
        }

        [Test]
        public void SelectActor_UniqueAcrossJoinedSlots_RejectsDuplicateWithoutStateCorruption()
        {
            ActorProfile actor = CreateActor("actor.unique");
            PlayerSlotProfile firstProfile = CreateSlot("player.unique.1", null);
            PlayerSlotProfile secondProfile = CreateSlot("player.unique.2", null);
            PlayerParticipationRuntimeContext context = CreateContext(
                PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                firstProfile,
                secondProfile);
            PlayerSlotId firstSlotId = JoinNextAvailableSlot(context);
            PlayerSlotId secondSlotId = JoinNextAvailableSlot(context);
            context.TrySelectActorProfile(Request(firstSlotId, actor));
            PlayerParticipationSnapshot before = context.CreateSnapshot();
            PlayerSlotRuntimeSnapshot beforeSecondSlot = GetSlot(context, secondSlotId);

            PlayerActorSelectionResult result = context.TrySelectActorProfile(
                Request(secondSlotId, actor));

            Assert.That(result.Status,
                Is.EqualTo(PlayerActorSelectionStatus.RejectedDuplicateActorSelection));
            Assert.That(result.ConflictingPlayerSlotId, Is.EqualTo(firstSlotId));
            AssertUnchanged(context, secondSlotId, before, beforeSecondSlot);
        }

        private PlayerParticipationRuntimeContext CreateContext(
            PlayerActorSelectionDuplicatePolicy duplicatePolicy,
            PlayerActorResolutionPolicy resolutionPolicy,
            params PlayerSlotProfile[] slotProfiles)
        {
            var slots = new EffectivePlayerSlotProvisioning[slotProfiles.Length];
            for (int index = 0; index < slotProfiles.Length; index++)
            {
                slots[index] = new EffectivePlayerSlotProvisioning(
                    slotProfiles[index],
                    PlayerHostProvisioningMode.ManagerProvisioned);
            }

            var configuration = new EffectivePlayerSessionConfiguration(
                slots,
                true,
                PlayerHostProvisioningMode.ManagerProvisioned,
                resolutionPolicy);
            PlayerParticipationOperationResult result =
                PlayerParticipationRuntimeContext.TryCreateWithEffectiveConfiguration(
                    configuration,
                    duplicatePolicy,
                    Source,
                    "create-test-context",
                    out PlayerParticipationRuntimeContext context);
            Assert.That(result.Succeeded, Is.True, result.Message);
            return context;
        }

        private PlayerSlotId JoinNextAvailableSlot(PlayerParticipationRuntimeContext context)
        {
            PlayerParticipationOperationResult reserve = context.TryReserveNextAvailableSlot(
                PlayerHostProvisioningMode.ManagerProvisioned,
                Source,
                "reserve-test-slot");
            Assert.That(reserve.Succeeded, Is.True, reserve.Message);
            PlayerParticipationOperationResult joined = context.TryMarkJoined(
                reserve.ReservationToken,
                Source,
                "join-test-slot");
            Assert.That(joined.Succeeded, Is.True, joined.Message);
            return reserve.ReservationToken.PlayerSlotId;
        }

        private ActorProfile CreateActor(string actorProfileId)
        {
            ActorProfile actor = ScriptableObject.CreateInstance<ActorProfile>();
            actor.name = actorProfileId;
            SetString(actor, "actorProfileId", actorProfileId);
            _createdObjects.Add(actor);
            return actor;
        }

        private PlayerSlotProfile CreateSlot(string playerSlotId, ActorProfile defaultActor)
        {
            PlayerSlotProfile slot = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            slot.name = playerSlotId;
            SetString(slot, "playerSlotId", playerSlotId);
            SetObject(slot, "defaultActorProfile", defaultActor);
            _createdObjects.Add(slot);
            return slot;
        }

        private static PlayerActorSelectionRequest Request(
            PlayerSlotId playerSlotId,
            ActorProfile actorProfile)
        {
            return new PlayerActorSelectionRequest(
                playerSlotId,
                actorProfile,
                Source,
                "selection",
                PlayerActorSelectionRequest.NoExpectedRevision);
        }

        private static PlayerSlotRuntimeSnapshot GetSlot(
            PlayerParticipationRuntimeContext context,
            PlayerSlotId playerSlotId)
        {
            Assert.That(context.TryGetActorSelection(playerSlotId, out PlayerSlotRuntimeSnapshot slot),
                Is.True);
            return slot;
        }

        private static void AssertRejectedSlotNotJoined(PlayerActorSelectionResult result)
        {
            Assert.That(result.Status,
                Is.EqualTo(PlayerActorSelectionStatus.RejectedSlotNotJoined));
            Assert.That(result.StateChanged, Is.False);
        }

        private static void AssertUnchanged(
            PlayerParticipationRuntimeContext context,
            PlayerSlotId playerSlotId,
            PlayerParticipationSnapshot expectedSnapshot,
            PlayerSlotRuntimeSnapshot expectedSlot)
        {
            PlayerParticipationSnapshot current = context.CreateSnapshot();
            PlayerSlotRuntimeSnapshot currentSlot = GetSlot(context, playerSlotId);
            Assert.That(current.Revision, Is.EqualTo(expectedSnapshot.Revision));
            Assert.That(currentSlot.Revision, Is.EqualTo(expectedSlot.Revision));
            Assert.That(currentSlot.SelectionRevision,
                Is.EqualTo(expectedSlot.SelectionRevision));
            Assert.That(currentSlot.SelectedActorProfile,
                Is.SameAs(expectedSlot.SelectedActorProfile));
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
