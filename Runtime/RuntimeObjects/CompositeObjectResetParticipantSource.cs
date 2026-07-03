using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.ObjectReset;

namespace Immersive.Framework.RuntimeObjects
{
    /// <summary>
    /// API status: Internal. Aggregates explicit ObjectReset participant sources without making either source global.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F44 composite ObjectReset participant source for scene-authored and runtime-registered participants.")]
    internal sealed class CompositeObjectResetParticipantSource : IObjectResetParticipantSource
    {
        private readonly IReadOnlyList<IObjectResetParticipantSource> _sources;

        internal CompositeObjectResetParticipantSource(IReadOnlyList<IObjectResetParticipantSource> sources)
        {
            _sources = sources ?? Array.Empty<IObjectResetParticipantSource>();
        }

        public IReadOnlyList<IObjectResetParticipant> ResolveObjectResetParticipants(
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget)
        {
            if (_sources.Count == 0)
            {
                return Array.Empty<IObjectResetParticipant>();
            }

            var participants = new List<IObjectResetParticipant>();
            for (int i = 0; i < _sources.Count; i++)
            {
                var source = _sources[i];
                if (source == null)
                {
                    continue;
                }

                IReadOnlyList<IObjectResetParticipant> resolved = source.ResolveObjectResetParticipants(request, resolvedTarget)
                    ?? Array.Empty<IObjectResetParticipant>();
                for (int participantIndex = 0; participantIndex < resolved.Count; participantIndex++)
                {
                    var participant = resolved[participantIndex];
                    if (participant != null)
                    {
                        participants.Add(participant);
                    }
                }
            }

            return participants;
        }
    }
}
