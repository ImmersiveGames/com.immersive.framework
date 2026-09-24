using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class PlayerParticipationReadinessRequirementsTests
    {
        [TestCase(PlayerParticipationRequirementLevel.None, 0)]
        [TestCase(PlayerParticipationRequirementLevel.JoinedSlots, 1)]
        [TestCase(PlayerParticipationRequirementLevel.SelectedActors, 2)]
        [TestCase(PlayerParticipationRequirementLevel.LogicalActorsPrepared, 3)]
        [TestCase(PlayerParticipationRequirementLevel.GameplayReady, 5)]
        public void EveryLevel_ExposesItsCumulativeEvidence(
            PlayerParticipationRequirementLevel level,
            int expectedCount)
        {
            Assert.That(
                PlayerParticipationReadinessRequirements.GetRequiredEvidence(level),
                Has.Count.EqualTo(expectedCount));
        }

        [Test]
        public void GameplayReady_RequiresEveryGameplayEvidence()
        {
            var evidence = PlayerParticipationReadinessRequirements.GetRequiredEvidence(
                PlayerParticipationRequirementLevel.GameplayReady);

            Assert.That(evidence, Does.Contain(PlayerParticipationReadinessEvidence.JoinedSlot));
            Assert.That(evidence, Does.Contain(PlayerParticipationReadinessEvidence.SelectedActor));
            Assert.That(evidence, Does.Contain(PlayerParticipationReadinessEvidence.LogicalActorPrepared));
            Assert.That(evidence, Does.Contain(PlayerParticipationReadinessEvidence.GameplayInputEligibility));
            Assert.That(evidence, Does.Contain(PlayerParticipationReadinessEvidence.GameplayActionEligibility));
        }

        [Test]
        public void GameplayReady_HasNoCameraEligibilityDependency()
        {
            var evidence = PlayerParticipationReadinessRequirements.GetRequiredEvidence(
                PlayerParticipationRequirementLevel.GameplayReady);

            Assert.That(
                evidence,
                Has.None.Matches<PlayerParticipationReadinessEvidence>(
                    value => value.ToString().Contains("Camera")));
        }
    }

    public sealed class ActivityAllJoinedSlotsProjectionTests
    {
        private const string Source = nameof(ActivityAllJoinedSlotsProjectionTests);
        private readonly List<UnityEngine.Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
            {
                if (_created[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_created[index]);
                }
            }

            _created.Clear();
        }

        [Test]
        public void AllJoinedSlots_AllowedZero_ReevaluatesFirstAndSecondLateJoin()
        {
            PlayerSlotProfile p1 = CreateSlot("player.dynamic.p1");
            PlayerSlotProfile p2 = CreateSlot("player.dynamic.p2");
            PlayerParticipationRuntimeContext context = CreateContext(p1, p2);
            ActivityAsset activity = CreateActivity(
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                PlayerParticipationRequirementLevel.GameplayReady);

            AssertProjection(activity, context);
            PlayerSlotRuntimeSnapshot p1Joined = Join(context);
            AssertProjection(activity, context, p1Joined);
            PlayerSlotRuntimeSnapshot p2Joined = Join(context);
            AssertProjection(activity, context, p1Joined, p2Joined);
        }

        [Test]
        public void AllJoinedSlots_RejectedZero_PreservesAuthoredPolicy()
        {
            PlayerSlotProfile p1 = CreateSlot("player.dynamic.zero-rejected");
            PlayerParticipationRuntimeContext context = CreateContext(p1);
            ActivityAsset activity = CreateActivity(
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Rejected,
                PlayerParticipationRequirementLevel.GameplayReady);

            bool resolved = ActivityPlayerParticipationProjectionResolver.TryResolve(
                activity,
                context,
                out _,
                out List<PlayerSlotRuntimeSnapshot> slots,
                out string issue);

            Assert.That(resolved, Is.False);
            Assert.That(slots, Is.Empty);
            Assert.That(issue, Does.Contain("rejects zero projected participants"));
        }

        [Test]
        public void JoinedSlot_PublishesTypedSessionDeltaAtCommittedRevision()
        {
            PlayerSlotProfile p1 = CreateSlot("player.dynamic.change");
            PlayerParticipationRuntimeContext context = CreateContext(p1);
            PlayerSessionChange observed = null;
            context.Changed += change => observed = change;

            PlayerSlotRuntimeSnapshot joined = Join(context);

            Assert.That(observed, Is.Not.Null);
            Assert.That(observed.Kind,
                Is.EqualTo(PlayerSessionChangeKind.SlotAllocationChanged));
            Assert.That(observed.PlayerSlotId, Is.EqualTo(joined.PlayerSlotId));
            Assert.That(observed.CurrentSlot.IsJoined, Is.True);
            Assert.That(observed.SessionRevision,
                Is.EqualTo(context.CreateSnapshot().Revision));
        }

        [Test]
        public void AllJoinedSlots_LeaveAndRejoin_UsesNewSlotOccurrenceRevision()
        {
            PlayerSlotProfile p1 = CreateSlot("player.dynamic.rejoin.p1");
            PlayerSlotProfile p2 = CreateSlot("player.dynamic.rejoin.p2");
            PlayerParticipationRuntimeContext context = CreateContext(p1, p2);
            ActivityAsset activity = CreateActivity(
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                PlayerParticipationRequirementLevel.GameplayReady);
            PlayerSlotRuntimeSnapshot p1A = Join(context);
            PlayerSlotRuntimeSnapshot p2Joined = Join(context);

            SessionPlayerLeaveRuntimeResult leaving =
                context.TryBeginSessionPlayerLeave(
                    p1A.PlayerSlotId,
                    p1A.Revision,
                    Source,
                    "leave-p1-a");
            Assert.That(leaving.Succeeded, Is.True, leaving.Message);
            AssertProjection(activity, context, p2Joined);

            SessionPlayerLeaveRuntimeResult committed =
                context.TryCommitSessionPlayerLeave(
                    leaving.Token,
                    Source,
                    "commit-p1-a-leave");
            Assert.That(committed.Succeeded, Is.True, committed.Message);

            PlayerSlotRuntimeSnapshot p1B = Join(context);
            Assert.That(p1B.PlayerSlotId, Is.EqualTo(p1A.PlayerSlotId));
            Assert.That(p1B.Revision, Is.GreaterThan(p1A.Revision));
            AssertProjection(activity, context, p1B, p2Joined);
        }

        [Test]
        public void NonDynamicProjection_DoesNotExpandFromSessionJoins()
        {
            PlayerSlotProfile p1 = CreateSlot("player.fixed.p1");
            PlayerSlotProfile p2 = CreateSlot("player.fixed.p2");
            PlayerParticipationRuntimeContext context = CreateContext(p1, p2);
            Join(context);
            Join(context);

            ActivityAsset noSlots = CreateActivity(
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                PlayerParticipationRequirementLevel.None);
            ActivityAsset explicitP1 = CreateActivity(
                ActivityParticipationProjectionMode.ExplicitSlots,
                ActivityParticipationZeroParticipantPolicy.Rejected,
                PlayerParticipationRequirementLevel.JoinedSlots,
                p1);

            AssertProjection(noSlots, context);
            AssertProjection(
                explicitP1,
                context,
                FindSlot(context.CreateSnapshot(), p1.PlayerSlotId));
        }

        [Test]
        public void AllJoinedSlots_RepeatedResolutionWithoutDelta_IsIdempotent()
        {
            PlayerSlotProfile p1 = CreateSlot("player.dynamic.idempotent");
            PlayerParticipationRuntimeContext context = CreateContext(p1);
            ActivityAsset activity = CreateActivity(
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                PlayerParticipationRequirementLevel.GameplayReady);
            PlayerSlotRuntimeSnapshot joined = Join(context);
            int sessionRevision = context.CreateSnapshot().Revision;

            List<PlayerSlotRuntimeSnapshot> first = Resolve(activity, context);
            List<PlayerSlotRuntimeSnapshot> second = Resolve(activity, context);

            Assert.That(second, Has.Count.EqualTo(1));
            Assert.That(second[0].PlayerSlotId, Is.EqualTo(first[0].PlayerSlotId));
            Assert.That(second[0].Revision, Is.EqualTo(first[0].Revision));
            Assert.That(second[0].Revision, Is.EqualTo(joined.Revision));
            Assert.That(context.CreateSnapshot().Revision,
                Is.EqualTo(sessionRevision));
        }

        private PlayerParticipationRuntimeContext CreateContext(
            params PlayerSlotProfile[] profiles)
        {
            var slots = new EffectivePlayerSlotProvisioning[profiles.Length];
            for (int index = 0; index < profiles.Length; index++)
            {
                slots[index] = new EffectivePlayerSlotProvisioning(
                    profiles[index],
                    PlayerHostProvisioningMode.ManagerProvisioned);
            }

            var configuration = new EffectivePlayerSessionConfiguration(
                slots,
                true,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerParticipationOperationResult created =
                PlayerParticipationRuntimeContext.TryCreateWithEffectiveConfiguration(
                    configuration,
                    PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                    Source,
                    "create-dynamic-projection-context",
                    out PlayerParticipationRuntimeContext context);
            Assert.That(created.Succeeded, Is.True, created.Message);
            return context;
        }

        private PlayerSlotRuntimeSnapshot Join(
            PlayerParticipationRuntimeContext context)
        {
            PlayerParticipationOperationResult reservation =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "reserve-dynamic-player");
            Assert.That(reservation.Succeeded, Is.True, reservation.Message);
            PlayerParticipationOperationResult joined = context.TryMarkJoined(
                reservation.ReservationToken,
                Source,
                "join-dynamic-player");
            Assert.That(joined.Succeeded, Is.True, joined.Message);
            return joined.Slot;
        }

        private ActivityAsset CreateActivity(
            ActivityParticipationProjectionMode mode,
            ActivityParticipationZeroParticipantPolicy zeroPolicy,
            PlayerParticipationRequirementLevel requirement,
            params PlayerSlotProfile[] explicitSlots)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            _created.Add(activity);
            SetField(activity, "activityId", Guid.NewGuid().ToString("N"));
            SetField(activity, "activityName", "Dynamic Projection Test");
            SetField(activity, "playerParticipationProjectionMode", mode);
            SetField(activity, "playerParticipationZeroParticipantPolicy", zeroPolicy);
            SetField(activity, "playerParticipationRequirementLevel", requirement);
            SetField(
                activity,
                "playerParticipationExplicitSlotProfiles",
                explicitSlots ?? Array.Empty<PlayerSlotProfile>());
            return activity;
        }

        private PlayerSlotProfile CreateSlot(string id)
        {
            PlayerSlotProfile slot =
                ScriptableObject.CreateInstance<PlayerSlotProfile>();
            _created.Add(slot);
            SetField(slot, "playerSlotId", id);
            return slot;
        }

        private static void AssertProjection(
            ActivityAsset activity,
            PlayerParticipationRuntimeContext context,
            params PlayerSlotRuntimeSnapshot[] expected)
        {
            List<PlayerSlotRuntimeSnapshot> actual = Resolve(activity, context);
            Assert.That(actual, Has.Count.EqualTo(expected.Length));
            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That(actual[index].PlayerSlotId,
                    Is.EqualTo(expected[index].PlayerSlotId));
                Assert.That(actual[index].Revision,
                    Is.EqualTo(expected[index].Revision));
            }
        }

        private static List<PlayerSlotRuntimeSnapshot> Resolve(
            ActivityAsset activity,
            PlayerParticipationRuntimeContext context)
        {
            bool resolved = ActivityPlayerParticipationProjectionResolver.TryResolve(
                activity,
                context,
                out _,
                out List<PlayerSlotRuntimeSnapshot> slots,
                out string issue);
            Assert.That(resolved, Is.True, issue);
            return slots;
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId slotId)
        {
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                if (snapshot.Slots[index].PlayerSlotId == slotId)
                {
                    return snapshot.Slots[index];
                }
            }

            throw new InvalidOperationException(
                $"Slot '{slotId.StableText}' was not found.");
        }

        private static void SetField<T>(object target, string name, T value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
