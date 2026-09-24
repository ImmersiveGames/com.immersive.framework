using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Do not treat this as a stable materialization or contribution contract before F1/F5/F8.
    /// Immutable set of content handles owned by one lifecycle scope and owner.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public readonly struct FrameworkContentSet
    {
        private readonly FrameworkContentHandle[] _handles;

        public FrameworkContentSet(
            FrameworkContentScope scope,
            string ownerId,
            string ownerName,
            IReadOnlyList<FrameworkContentHandle> handles)
        {
            Scope = scope;
            OwnerId = Normalize(ownerId);
            OwnerName = Normalize(ownerName);

            if (handles == null || handles.Count == 0)
            {
                _handles = Array.Empty<FrameworkContentHandle>();
            }
            else
            {
                _handles = new FrameworkContentHandle[handles.Count];
                for (int i = 0; i < handles.Count; i++)
                {
                    _handles[i] = handles[i];
                }
            }
        }

        public FrameworkContentScope Scope { get; }

        public string OwnerId { get; }

        public string OwnerName { get; }

        public IReadOnlyList<FrameworkContentHandle> Handles => _handles ?? Array.Empty<FrameworkContentHandle>();

        public int Count => _handles?.Length ?? 0;

        public bool IsEmpty => Count == 0;

        public bool HasContent => !IsEmpty;

        public static FrameworkContentSet Empty(
            FrameworkContentScope scope,
            string ownerId,
            string ownerName)
        {
            return new FrameworkContentSet(scope, ownerId, ownerName, Array.Empty<FrameworkContentHandle>());
        }

        public static FrameworkContentSet Single(
            FrameworkContentScope scope,
            string ownerId,
            string ownerName,
            FrameworkContentHandle handle)
        {
            return new FrameworkContentSet(scope, ownerId, ownerName, new[] { handle });
        }

        public string ToDiagnosticString()
        {
            if (IsEmpty)
            {
                return $"scope='{Scope}' owner='{OwnerName}' handles='0'";
            }

            var builder = new StringBuilder();
            builder.Append($"scope='{Scope}' owner='{OwnerName}' handles='{Count}' details=[");
            for (int i = 0; i < _handles.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(_handles[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
