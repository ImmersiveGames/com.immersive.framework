using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    public sealed class LocalPlayerProvisioningValidationResult
    {
        internal LocalPlayerProvisioningValidationResult(
            LocalPlayerProvisioningAuthoring authoring,
            int surfaceCount,
            bool required,
            LocalPlayerProvisioningIssue[] issues,
            string source,
            string reason)
        {
            Authoring = authoring;
            SurfaceCount = Math.Max(0, surfaceCount);
            Required = required;
            Issues = issues ?? Array.Empty<LocalPlayerProvisioningIssue>();
            Source = source.NormalizeTextOrFallback(nameof(LocalPlayerProvisioningValidationResult));
            Reason = reason.NormalizeText();
        }

        public LocalPlayerProvisioningAuthoring Authoring { get; }
        public int SurfaceCount { get; }
        public bool Required { get; }
        public IReadOnlyList<LocalPlayerProvisioningIssue> Issues { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool Available => Authoring != null && SurfaceCount == 1;
        public bool Succeeded
        {
            get
            {
                for (int index = 0; index < Issues.Count; index++)
                {
                    if (Issues[index].Blocking) return false;
                }

                return true;
            }
        }
        public bool Failed => !Succeeded;
        public int IssueCount => Issues.Count;
        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < Issues.Count; index++)
                {
                    if (Issues[index].Blocking) count++;
                }

                return count;
            }
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("surfaces='").Append(SurfaceCount).Append("'");
            builder.Append(" required='").Append(Required).Append("'");
            builder.Append(" available='").Append(Available).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            for (int index = 0; index < Issues.Count; index++)
                builder.Append(" issue[").Append(index).Append("]='").Append(Issues[index]).Append("'");
            return builder.ToString();
        }
    }
}
