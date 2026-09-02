using NUnit.Framework;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class PlayerPreparedActorReplacementRequestTests
    {
        [Test]
        public void DefaultRequest_IsRejectedWithoutLeakingInternalCorrelations()
        {
            var request = default(PlayerPreparedActorReplacementRequest);

            Assert.That(request.IsValid, Is.False);
            Assert.That(request.HasExpectedSelectionRevision, Is.False);
            Assert.That(request.HasExpectedSessionRevision, Is.False);
        }

        [Test]
        public void NoExpectedRevision_IsTheOnlyPublicOptionalCorrelationSentinel()
        {
            Assert.That(PlayerPreparedActorReplacementRequest.NoExpectedRevision,
                Is.EqualTo(-1));
        }
    }
}
