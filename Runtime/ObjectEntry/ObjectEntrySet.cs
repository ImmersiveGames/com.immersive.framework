using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Immutable passive set of logical object entries.
    /// It is not a manager, registry, service locator, discovery runtime or reset inventory.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry passive set introduced by F13A.")]
    public sealed class ObjectEntrySet
    {
        private static readonly IReadOnlyList<ObjectEntryDescriptor> EmptyEntries = Array.Empty<ObjectEntryDescriptor>();

        public ObjectEntrySet(IEnumerable<ObjectEntryDescriptor> entries)
        {
            IReadOnlyList<ObjectEntryDescriptor> materialized = entries?.ToArray() ?? EmptyEntries;
            IGrouping<ObjectEntryId, ObjectEntryDescriptor> duplicate = materialized.GroupBy(entry => entry.Id).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                throw new ArgumentException($"Object entry set contains duplicate object entry id '{duplicate.Key}'.", nameof(entries));
            }

            Entries = materialized;
        }

        public IReadOnlyList<ObjectEntryDescriptor> Entries { get; }

        public int Count => Entries.Count;

        public int RequiredCount => Entries.Count(entry => entry.Requiredness == ObjectEntryRequiredness.Required);

        public int OptionalCount => Entries.Count(entry => entry.Requiredness == ObjectEntryRequiredness.Optional);

        public bool IsEmpty => Count == 0;

        public bool TryGet(ObjectEntryId id, out ObjectEntryDescriptor descriptor)
        {
            foreach (var entry in Entries)
            {
                if (entry.Id == id)
                {
                    descriptor = entry;
                    return true;
                }
            }

            descriptor = default;
            return false;
        }

        public IReadOnlyList<ObjectEntryDescriptor> GetByScope(ObjectEntryScope scope)
        {
            return Entries.Where(entry => entry.Scope == scope).ToArray();
        }

        public string Summary => $"objectEntries='{Count}' required='{RequiredCount}' optional='{OptionalCount}'";

        public static ObjectEntrySet Empty()
        {
            return new ObjectEntrySet(EmptyEntries);
        }
    }
}
