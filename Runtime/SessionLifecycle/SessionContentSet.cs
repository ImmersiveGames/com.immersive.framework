using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.SessionLifecycle
{
    /// <summary>
    /// API status: Internal. Immutable Session-owned set of content entries known by the Session scope.
    /// The initial implementation can be empty; it is state data, not a manager, registry or service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Minimal Session content set introduced by F2C.")]
    internal readonly struct SessionContentSet
    {
        private readonly SessionContentEntry[] _entries;

        public SessionContentSet(IReadOnlyList<SessionContentEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                _entries = Array.Empty<SessionContentEntry>();
                return;
            }

            _entries = new SessionContentEntry[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                _entries[i] = entries[i];
            }
        }

        public IReadOnlyList<SessionContentEntry> Entries => _entries ?? Array.Empty<SessionContentEntry>();

        public int Count => _entries?.Length ?? 0;

        public bool IsEmpty => Count == 0;

        public bool HasContent => !IsEmpty;

        public int RegisteredCount => CountOwnership(SessionContentOwnership.Registered);

        public int OwnedCount => CountOwnership(SessionContentOwnership.Owned);

        public int DiagnosticOnlyCount => CountOwnership(SessionContentOwnership.DiagnosticOnly);

        public static SessionContentSet Empty()
        {
            return new SessionContentSet(Array.Empty<SessionContentEntry>());
        }

        public static SessionContentSet From(IReadOnlyList<SessionContentEntry> entries)
        {
            return new SessionContentSet(entries);
        }

        public string ToDiagnosticString()
        {
            if (IsEmpty)
            {
                return "Session Content Set is empty.";
            }

            var builder = new StringBuilder();
            builder.Append($"Session Content Set registered {Count} item(s). registered='{RegisteredCount}' owned='{OwnedCount}' diagnosticOnly='{DiagnosticOnlyCount}' details=[");
            for (int i = 0; i < _entries.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(_entries[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        private int CountOwnership(SessionContentOwnership ownership)
        {
            if (_entries == null || _entries.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Ownership == ownership)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
