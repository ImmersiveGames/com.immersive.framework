using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Reset.Unity;
using UnityEngine;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Authored reference to a ResetSubject by Unity adapter or explicit ResetSubjectId text.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12D authored reset subject reference.")]
    public sealed class ResetSubjectReference : IEquatable<ResetSubjectReference>
    {
        [SerializeField] private UnityResetSubjectAdapter subjectAdapter;
        [SerializeField] private string subjectId;

        public UnityResetSubjectAdapter SubjectAdapter => subjectAdapter;

        public string SubjectIdText => subjectId;

        public bool HasAdapter => subjectAdapter != null;

        public bool HasSubjectIdText => !string.IsNullOrWhiteSpace(subjectId);

        public string ResolvedSubjectIdText => ResolveSubjectIdText();

        public bool TryResolve(out ResetSubjectId resolvedSubjectId, out ResetIssue issue)
        {
            string idText = ResolveSubjectIdText();
            if (string.IsNullOrWhiteSpace(idText))
            {
                resolvedSubjectId = default;
                issue = ResetIssue.Error(
                    ResetIssueKind.InvalidSubject,
                    subjectAdapter != null
                        ? "Reset subject reference has an adapter, but the adapter has no registered ResetSubjectId yet."
                        : "Reset subject reference is missing both adapter and ResetSubjectId text.");
                return false;
            }

            try
            {
                resolvedSubjectId = ResetSubjectId.From(idText);
                issue = default;
                return true;
            }
            catch (ArgumentException exception)
            {
                resolvedSubjectId = default;
                issue = ResetIssue.Error(
                    ResetIssueKind.InvalidSubject,
                    $"Reset subject reference id is invalid. subjectId='{idText}'. message='{exception.Message}'");
                return false;
            }
        }

        public bool Equals(ResetSubjectReference other)
        {
            if (other == null)
            {
                return false;
            }

            return Equals(subjectAdapter, other.subjectAdapter)
                && string.Equals(subjectId.NormalizeText(), other.subjectId.NormalizeText(), StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetSubjectReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = subjectAdapter != null ? subjectAdapter.GetHashCode() : 0;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(subjectId.NormalizeText());
                return hashCode;
            }
        }

        public override string ToString()
        {
            string adapterName = subjectAdapter != null ? subjectAdapter.name : "<none>";
            string resolved = ResolveSubjectIdText().ToDiagnosticText("<none>");
            return $"adapter='{adapterName}' subjectId='{subjectId.ToDiagnosticText("<none>")}' resolvedSubjectId='{resolved}'";
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(UnityResetSubjectAdapter qaSubjectAdapter, string qaSubjectId)
        {
            subjectAdapter = qaSubjectAdapter;
            subjectId = qaSubjectId;
        }
#endif

        private string ResolveSubjectIdText()
        {
            if (subjectAdapter != null)
            {
                ResetSubjectId adapterSubjectId = subjectAdapter.SubjectId;
                if (adapterSubjectId.IsValid)
                {
                    return adapterSubjectId.StableText;
                }
            }

            return subjectId.NormalizeText();
        }
    }
}
