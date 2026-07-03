using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.RuntimeObjects
{
    /// <summary>
    /// API status: Internal. Passive collection diagnostics for runtime object participation entries.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F44 runtime object participation collection result.")]
    internal sealed class RuntimeObjectParticipationCollectionResult
    {
        internal RuntimeObjectParticipationCollectionResult(
            IEnumerable<ObjectEntryDescriptor> descriptors,
            int candidateDescriptorCount,
            int filteredDescriptorCount,
            IEnumerable<ObjectEntryIssue> issues)
        {
            Descriptors = descriptors?.ToArray() ?? System.Array.Empty<ObjectEntryDescriptor>();
            CandidateDescriptorCount = candidateDescriptorCount < 0 ? 0 : candidateDescriptorCount;
            FilteredDescriptorCount = filteredDescriptorCount < 0 ? 0 : filteredDescriptorCount;
            Issues = issues?.ToArray() ?? System.Array.Empty<ObjectEntryIssue>();
        }

        internal IReadOnlyList<ObjectEntryDescriptor> Descriptors { get; }

        internal int CandidateDescriptorCount { get; }

        internal int FilteredDescriptorCount { get; }

        internal IReadOnlyList<ObjectEntryIssue> Issues { get; }
    }
}
