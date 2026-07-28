using NUnit.Framework;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class PlayerParticipationReadinessRequirementsTests
    {
        [TestCase(PlayerParticipationRequirementLevel.None, 0)]
        [TestCase(PlayerParticipationRequirementLevel.JoinedSlots, 1)]
        [TestCase(PlayerParticipationRequirementLevel.SelectedActors, 2)]
        [TestCase(PlayerParticipationRequirementLevel.LogicalActorsPrepared, 3)]
        [TestCase(PlayerParticipationRequirementLevel.GameplayReady, 6)]
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
            Assert.That(evidence, Does.Contain(PlayerParticipationReadinessEvidence.GameplayCameraEligibility));
            Assert.That(evidence, Does.Contain(PlayerParticipationReadinessEvidence.GameplayActionEligibility));
        }

        [Test]
        public void LogicalActorsPrepared_DoesNotRequireGameplayCamera()
        {
            var evidence = PlayerParticipationReadinessRequirements.GetRequiredEvidence(
                PlayerParticipationRequirementLevel.LogicalActorsPrepared);

            Assert.That(
                evidence,
                Does.Not.Contain(PlayerParticipationReadinessEvidence.GameplayCameraEligibility));
        }
    }
}
