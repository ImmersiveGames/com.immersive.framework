using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Immutable manifest of logical Progression Save slots.
    /// It is metadata only and does not choose, load or write a backend.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save manifest primitive; metadata only.")]
    public readonly struct ProgressionSaveManifest : IEquatable<ProgressionSaveManifest>
    {
        private readonly ProgressionSaveManifestEntry[] _entries;

        public ProgressionSaveManifest(IReadOnlyList<ProgressionSaveManifestEntry> entries, long updatedUtcTicks, string source)
        {
            ValidateTicks(updatedUtcTicks, nameof(updatedUtcTicks));

            ProgressionSaveManifestEntry[] copiedEntries = CopyEntries(entries);
            EnsureUniqueSlots(copiedEntries);

            _entries = copiedEntries;
            UpdatedUtcTicks = updatedUtcTicks;
            Source = Normalize(source);
        }

        public IReadOnlyList<ProgressionSaveManifestEntry> Entries => _entries ?? Array.Empty<ProgressionSaveManifestEntry>();

        public int Count => Entries.Count;

        public long UpdatedUtcTicks { get; }

        public string Source { get; }

        public DateTime UpdatedUtc => new DateTime(UpdatedUtcTicks, DateTimeKind.Utc);

        public bool HasEntries => Count > 0;

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool IsValid => IsValidTicks(UpdatedUtcTicks) && AllEntriesValidAndUnique(Entries);

        public ProgressionSaveManifestEntry[] ToEntryArray()
        {
            IReadOnlyList<ProgressionSaveManifestEntry> source = Entries;
            if (source.Count == 0)
            {
                return Array.Empty<ProgressionSaveManifestEntry>();
            }

            var copy = new ProgressionSaveManifestEntry[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }

        public bool ContainsSlot(ProgressionSaveSlotId slotId)
        {
            return TryGetEntry(slotId, out _);
        }

        public bool TryGetEntry(ProgressionSaveSlotId slotId, out ProgressionSaveManifestEntry entry)
        {
            if (!slotId.IsValid)
            {
                entry = default;
                return false;
            }

            IReadOnlyList<ProgressionSaveManifestEntry> source = Entries;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].SlotId == slotId)
                {
                    entry = source[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public ProgressionSaveManifest WithEntry(ProgressionSaveManifestEntry entry, long updatedUtcTicks, string source)
        {
            if (!entry.IsValid)
            {
                throw new ArgumentException("Progression Save manifest entry must be valid.", nameof(entry));
            }

            ValidateTicks(updatedUtcTicks, nameof(updatedUtcTicks));

            IReadOnlyList<ProgressionSaveManifestEntry> current = Entries;
            bool replaced = false;
            var output = new List<ProgressionSaveManifestEntry>(current.Count + 1);

            for (int i = 0; i < current.Count; i++)
            {
                if (current[i].SlotId == entry.SlotId)
                {
                    output.Add(entry);
                    replaced = true;
                }
                else
                {
                    output.Add(current[i]);
                }
            }

            if (!replaced)
            {
                output.Add(entry);
            }

            return new ProgressionSaveManifest(output, updatedUtcTicks, source);
        }

        public ProgressionSaveManifest WithoutSlot(ProgressionSaveSlotId slotId, long updatedUtcTicks, string source)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException("Progression Save slot id must be valid.", nameof(slotId));
            }

            ValidateTicks(updatedUtcTicks, nameof(updatedUtcTicks));

            IReadOnlyList<ProgressionSaveManifestEntry> current = Entries;
            var output = new List<ProgressionSaveManifestEntry>(current.Count);

            for (int i = 0; i < current.Count; i++)
            {
                if (current[i].SlotId != slotId)
                {
                    output.Add(current[i]);
                }
            }

            return new ProgressionSaveManifest(output, updatedUtcTicks, source);
        }

        public bool Equals(ProgressionSaveManifest other)
        {
            return UpdatedUtcTicks == other.UpdatedUtcTicks
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && SequenceEquals(Entries, other.Entries);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveManifest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = UpdatedUtcTicks.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);

                IReadOnlyList<ProgressionSaveManifestEntry> source = Entries;
                for (int i = 0; i < source.Count; i++)
                {
                    hashCode = hashCode * 397 ^ source[i].GetHashCode();
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = HasSource ? Source : "<none>";
            return $"entries='{Count}' updatedUtcTicks='{UpdatedUtcTicks}' source='{sourceText}'";
        }

        public static ProgressionSaveManifest Empty(long updatedUtcTicks, string source)
        {
            return new ProgressionSaveManifest(Array.Empty<ProgressionSaveManifestEntry>(), updatedUtcTicks, source);
        }

        public static bool operator ==(ProgressionSaveManifest left, ProgressionSaveManifest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveManifest left, ProgressionSaveManifest right)
        {
            return !left.Equals(right);
        }

        private static ProgressionSaveManifestEntry[] CopyEntries(IReadOnlyList<ProgressionSaveManifestEntry> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<ProgressionSaveManifestEntry>();
            }

            var copy = new ProgressionSaveManifestEntry[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Progression Save manifest entries must all be valid.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static void EnsureUniqueSlots(IReadOnlyList<ProgressionSaveManifestEntry> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                for (int j = i + 1; j < source.Count; j++)
                {
                    if (source[i].SlotId == source[j].SlotId)
                    {
                        throw new ArgumentException($"Progression Save manifest has duplicate slot id '{source[i].SlotId.StableText}'.", nameof(source));
                    }
                }
            }
        }

        private static bool AllEntriesValidAndUnique(IReadOnlyList<ProgressionSaveManifestEntry> source)
        {
            if (source == null)
            {
                return false;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    return false;
                }

                for (int j = i + 1; j < source.Count; j++)
                {
                    if (source[i].SlotId == source[j].SlotId)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool SequenceEquals(IReadOnlyList<ProgressionSaveManifestEntry> left, IReadOnlyList<ProgressionSaveManifestEntry> right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateTicks(long ticks, string name)
        {
            if (!IsValidTicks(ticks))
            {
                throw new ArgumentOutOfRangeException(name, ticks, "Progression Save manifest UTC ticks must be a valid positive DateTime tick value.");
            }
        }

        private static bool IsValidTicks(long ticks)
        {
            return ticks > 0 && ticks <= DateTime.MaxValue.Ticks;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
