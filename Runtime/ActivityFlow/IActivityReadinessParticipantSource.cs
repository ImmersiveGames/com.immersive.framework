using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Narrow package-owned source for readiness participants that are scoped to the
    /// FrameworkRuntimeHost rather than discovered from Activity scene content.
    /// </summary>
    internal interface IActivityReadinessParticipantSource
    {
        IReadOnlyList<ActivityReadinessParticipant>
            ResolveActivityReadinessParticipants(ActivityAsset activity);
    }

    internal sealed class EmptyActivityReadinessParticipantSource :
        IActivityReadinessParticipantSource
    {
        internal static readonly EmptyActivityReadinessParticipantSource Instance =
            new EmptyActivityReadinessParticipantSource();

        private EmptyActivityReadinessParticipantSource()
        {
        }

        public IReadOnlyList<ActivityReadinessParticipant>
            ResolveActivityReadinessParticipants(ActivityAsset activity)
        {
            return Array.Empty<ActivityReadinessParticipant>();
        }
    }
}
