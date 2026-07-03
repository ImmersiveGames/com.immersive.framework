using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Immutable snapshot of local contributions discovered in currently loaded scene-authored content.
    /// This is not materialization state, not requiredness policy and not a runtime reference inventory.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Local contribution set introduced by F5D, consolidated in F5E, and carrying requiredness metadata from F5F; scoped consumers come later.")]
    internal readonly struct LocalContributionSet
    {
        private readonly LocalContributionHandle[] _handles;

        public LocalContributionSet(IReadOnlyList<LocalContributionHandle> handles)
        {
            if (handles == null || handles.Count == 0)
            {
                _handles = Array.Empty<LocalContributionHandle>();
                return;
            }

            _handles = new LocalContributionHandle[handles.Count];
            for (int i = 0; i < handles.Count; i++)
            {
                if (!handles[i].IsValid)
                {
                    throw new ArgumentException("Local contribution set cannot contain invalid handles.", nameof(handles));
                }

                _handles[i] = handles[i];
            }
        }

        public IReadOnlyList<LocalContributionHandle> Handles => _handles ?? Array.Empty<LocalContributionHandle>();

        public int Count => Handles.Count;

        public bool HasContributions => Count > 0;

        public int SessionCount => CountByScope(FrameworkContentScope.Session);

        public int RouteCount => CountByScope(FrameworkContentScope.Route);

        public int ActivityCount => CountByScope(FrameworkContentScope.Activity);

        public int RouteContentBindingCount => CountBySource(LocalContributionSourceKind.RouteContentBinding);

        public int ActivityLocalVisibilityAdapterCount => CountBySource(LocalContributionSourceKind.ActivityLocalVisibilityAdapter);

        public int RequiredCount => CountByRequiredness(FrameworkContentRequiredness.Required);

        public int OptionalCount => CountByRequiredness(FrameworkContentRequiredness.Optional);

        public bool HasScope(FrameworkContentScope contentScope)
        {
            return CountByScope(contentScope) > 0;
        }

        public int CountByScope(FrameworkContentScope contentScope)
        {
            if (!HasContributions)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<LocalContributionHandle> items = Handles;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Identity.ContentScope == contentScope)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountBySource(LocalContributionSourceKind sourceKind)
        {
            if (!HasContributions || sourceKind == LocalContributionSourceKind.Unknown)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<LocalContributionHandle> items = Handles;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].SourceKind == sourceKind)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountByRequiredness(FrameworkContentRequiredness requiredness)
        {
            if (!HasContributions)
            {
                return 0;
            }

            var normalized = NormalizeRequiredness(requiredness);
            int count = 0;
            IReadOnlyList<LocalContributionHandle> items = Handles;
            for (int i = 0; i < items.Count; i++)
            {
                if (NormalizeRequiredness(items[i].Requiredness) == normalized)
                {
                    count++;
                }
            }

            return count;
        }

        public bool Contains(LocalContentIdentity identity)
        {
            return TryGet(identity, out _);
        }

        public bool TryGet(LocalContentIdentity identity, out LocalContributionHandle handle)
        {
            if (!identity.IsValid || !HasContributions)
            {
                handle = default;
                return false;
            }

            IReadOnlyList<LocalContributionHandle> items = Handles;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Identity.Equals(identity))
                {
                    handle = items[i];
                    return true;
                }
            }

            handle = default;
            return false;
        }

        public IReadOnlyList<LocalContributionHandle> GetByScope(FrameworkContentScope contentScope)
        {
            if (!HasContributions)
            {
                return Array.Empty<LocalContributionHandle>();
            }

            return Filter(handle => handle.Identity.ContentScope == contentScope);
        }

        public IReadOnlyList<LocalContributionHandle> GetBySource(LocalContributionSourceKind sourceKind)
        {
            if (!HasContributions || sourceKind == LocalContributionSourceKind.Unknown)
            {
                return Array.Empty<LocalContributionHandle>();
            }

            return Filter(handle => handle.SourceKind == sourceKind);
        }

        public IReadOnlyList<LocalContributionHandle> GetByRequiredness(FrameworkContentRequiredness requiredness)
        {
            if (!HasContributions)
            {
                return Array.Empty<LocalContributionHandle>();
            }

            var normalized = NormalizeRequiredness(requiredness);
            return Filter(handle => NormalizeRequiredness(handle.Requiredness) == normalized);
        }

        public string DiagnosticMessage
        {
            get
            {
                if (!HasContributions)
                {
                    return "Local Contribution Set is empty.";
                }

                return $"Local Contribution Set discovered {Count} contribution(s). {ToDiagnosticString()}";
            }
        }

        public string ToDiagnosticString(int maxHandles = 8)
        {
            if (!HasContributions)
            {
                return "handles='0'";
            }

            int limit = Math.Max(0, maxHandles);
            int shown = Math.Min(limit, Count);
            var builder = new StringBuilder();
            builder.Append($"handles='{Count}' session='{SessionCount}' route='{RouteCount}' activity='{ActivityCount}' routeBindings='{RouteContentBindingCount}' activityAdapters='{ActivityLocalVisibilityAdapterCount}' required='{RequiredCount}' optional='{OptionalCount}' details=[");

            for (int i = 0; i < shown; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Handles[i].ToDiagnosticString());
            }

            builder.Append("]");
            if (Count > shown)
            {
                builder.Append($" omitted='{Count - shown}'");
            }

            return builder.ToString();
        }

        private static FrameworkContentRequiredness NormalizeRequiredness(FrameworkContentRequiredness requiredness)
        {
            return requiredness == FrameworkContentRequiredness.Required
                ? FrameworkContentRequiredness.Required
                : FrameworkContentRequiredness.Optional;
        }

        private IReadOnlyList<LocalContributionHandle> Filter(Func<LocalContributionHandle, bool> predicate)
        {
            if (predicate == null || !HasContributions)
            {
                return Array.Empty<LocalContributionHandle>();
            }

            var matches = new List<LocalContributionHandle>();
            IReadOnlyList<LocalContributionHandle> items = Handles;
            for (int i = 0; i < items.Count; i++)
            {
                var handle = items[i];
                if (predicate(handle))
                {
                    matches.Add(handle);
                }
            }

            return matches.Count == 0
                ? Array.Empty<LocalContributionHandle>()
                : matches.ToArray();
        }

        public static LocalContributionSet Empty()
        {
            return new LocalContributionSet(Array.Empty<LocalContributionHandle>());
        }

        public static LocalContributionSet FromHandles(IReadOnlyList<LocalContributionHandle> handles)
        {
            return new LocalContributionSet(handles);
        }
    }
}
