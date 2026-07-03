using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Internal. Default participant source for the F11 Cycle Reset foundation.
    /// It intentionally returns no participants until later cuts connect real scoped discovery.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Default empty Cycle Reset participant source; no discovery or gameplay behavior.")]
    internal sealed class EmptyCycleResetParticipantSource : ICycleResetParticipantSource
    {
        internal static readonly EmptyCycleResetParticipantSource Instance = new EmptyCycleResetParticipantSource();

        private EmptyCycleResetParticipantSource()
        {
        }

        public IReadOnlyList<ICycleResetParticipant> ResolveCycleResetParticipants(CycleResetRequest request)
        {
            return Array.Empty<ICycleResetParticipant>();
        }
    }
}
