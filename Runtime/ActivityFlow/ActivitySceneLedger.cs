using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25H explicit ledger for Activity-owned Unity scenes.")]
    internal sealed class ActivitySceneLedger
    {
        private readonly List<ActivitySceneLedgerEntry> _entries = new List<ActivitySceneLedgerEntry>();

        internal int EntryCount => _entries.Count;

        internal int LoadedCount => CountByStatus(ActivitySceneLedgerEntryStatus.Loaded);

        internal int ReleasedCount => CountByStatus(ActivitySceneLedgerEntryStatus.Released);

        internal int StaleCount => CountByStatus(ActivitySceneLedgerEntryStatus.Stale);

        internal void RegisterLoaded(
            string routeInstanceId,
            RouteAsset route,
            ActivityAsset activity,
            ActivitySceneCompositionResultEntry entry)
        {
            if (activity == null || entry is { Loaded: false, AlreadyLoaded: false })
            {
                return;
            }

            var ledgerEntry = new ActivitySceneLedgerEntry(
                routeInstanceId,
                route,
                activity,
                entry.PlanEntry,
                ActivitySceneLedgerOwnership.Activity,
                ActivitySceneLedgerEntryStatus.Loaded);

            Upsert(ledgerEntry);
        }

        internal bool TryGetLoaded(
            ActivityAsset activity,
            ActivitySceneCompositionPlanEntry planEntry,
            string routeInstanceId,
            out ActivitySceneLedgerEntry entry)
        {
            entry = default;
            string normalizedRouteInstanceId = Normalize(routeInstanceId);
            if (activity == null || !planEntry.ContentIdentity.IsValid || string.IsNullOrWhiteSpace(normalizedRouteInstanceId))
            {
                return false;
            }

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var existing = _entries[i];
                if (!existing.IsLoaded)
                {
                    continue;
                }

                if (IsSameEntry(existing, normalizedRouteInstanceId, activity, planEntry.ContentIdentity.StableText))
                {
                    entry = existing;
                    return true;
                }
            }

            return false;
        }

        internal List<ActivitySceneLedgerEntry> CollectLoadedForActivityRouteInstance(ActivityAsset activity, string routeInstanceId)
        {
            var entries = new List<ActivitySceneLedgerEntry>();
            string normalizedRouteInstanceId = Normalize(routeInstanceId);
            if (activity == null || string.IsNullOrWhiteSpace(normalizedRouteInstanceId))
            {
                return entries;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry is { IsLoaded: true, Ownership: ActivitySceneLedgerOwnership.Activity }
                    && ReferenceEquals(entry.Activity, activity)
                    && string.Equals(entry.RouteInstanceId, normalizedRouteInstanceId, StringComparison.Ordinal))
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        internal List<ActivitySceneLedgerEntry> CollectLoadedForRouteInstance(string routeInstanceId)
        {
            var entries = new List<ActivitySceneLedgerEntry>();
            string normalizedRouteInstanceId = Normalize(routeInstanceId);
            if (string.IsNullOrWhiteSpace(normalizedRouteInstanceId))
            {
                return entries;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry is { IsLoaded: true, Ownership: ActivitySceneLedgerOwnership.Activity }
                    && string.Equals(entry.RouteInstanceId, normalizedRouteInstanceId, StringComparison.Ordinal))
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        internal void MarkReleased(ActivitySceneLedgerEntry entry)
        {
            UpdateStatus(entry, ActivitySceneLedgerEntryStatus.Released);
        }

        internal void MarkStale(ActivitySceneLedgerEntry entry)
        {
            UpdateStatus(entry, ActivitySceneLedgerEntryStatus.Stale);
        }

        private void Upsert(ActivitySceneLedgerEntry entry)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var existing = _entries[i];
                if (IsSameEntry(existing, entry.RouteInstanceId, entry.Activity, entry.ContentIdentity))
                {
                    _entries[i] = entry;
                    return;
                }
            }

            _entries.Add(entry);
        }

        private void UpdateStatus(ActivitySceneLedgerEntry entry, ActivitySceneLedgerEntryStatus status)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var existing = _entries[i];
                if (IsSameEntry(existing, entry.RouteInstanceId, entry.Activity, entry.ContentIdentity))
                {
                    _entries[i] = existing.WithStatus(status);
                    return;
                }
            }
        }

        private static bool IsSameEntry(
            ActivitySceneLedgerEntry existing,
            string routeInstanceId,
            ActivityAsset activity,
            string contentIdentity)
        {
            return string.Equals(existing.RouteInstanceId, Normalize(routeInstanceId), StringComparison.Ordinal)
                && ReferenceEquals(existing.Activity, activity)
                && string.Equals(existing.ContentIdentity, Normalize(contentIdentity), StringComparison.Ordinal);
        }

        private int CountByStatus(ActivitySceneLedgerEntryStatus status)
        {
            int count = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Status == status)
                {
                    count++;
                }
            }

            return count;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
