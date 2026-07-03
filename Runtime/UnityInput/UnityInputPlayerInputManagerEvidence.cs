using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine.InputSystem;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Passive evidence snapshot for official Unity PlayerInputManager components.
    /// It does not join players, instantiate prefabs, switch action maps or own Unity input.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30C PlayerInputManager component evidence snapshot.")]
    public sealed class UnityInputPlayerInputManagerEvidence
    {
        private readonly UnityInputTargetSetIssue[] _issues;

        private UnityInputPlayerInputManagerEvidence(
            int managerCount,
            UnityInputTargetSetIssue[] issues,
            string source,
            string reason,
            UnityInputPlayerInputManagerScope scope,
            bool required)
        {
            ManagerCount = managerCount < 0 ? 0 : managerCount;
            _issues = issues ?? Array.Empty<UnityInputTargetSetIssue>();
            Source = source.NormalizeTextOrFallback(nameof(UnityInputPlayerInputManagerEvidence));
            Reason = reason.NormalizeText();
            Scope = scope;
            Required = required;
        }

        public int ManagerCount { get; }

        public UnityInputPlayerInputManagerScope Scope { get; }

        public bool Required { get; }

        public bool SessionScoped => Scope == UnityInputPlayerInputManagerScope.Session;

        public IReadOnlyList<UnityInputTargetSetIssue> Issues => _issues;

        public string Source { get; }

        public string Reason { get; }

        public int IssueCount => _issues.Length;

        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _issues.Length; i++)
                {
                    if (_issues[i].Blocking)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool Succeeded => BlockingIssueCount == 0;

        public bool Failed => !Succeeded;

        public bool UsesPlayerInputManager => ManagerCount > 0;

        public bool AppliesInputBehavior => false;

        public bool SwitchesActionMaps => false;

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("playerInputManagers='").Append(ManagerCount).Append("'");
            builder.Append(" scope='").Append(Scope).Append("'");
            builder.Append(" required='").Append(Required).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" actionMapSwitching='").Append(SwitchesActionMaps).Append("'");
            builder.Append(" inputBehavior='").Append(AppliesInputBehavior).Append("'");
            for (int i = 0; i < _issues.Length; i++)
            {
                builder.Append(" issue[").Append(i).Append("]='").Append(_issues[i]).Append("'");
            }

            return builder.ToString();
        }

        public static UnityInputPlayerInputManagerEvidence FromManagers(
            IEnumerable<PlayerInputManager> managers,
            string source,
            string reason)
        {
            int count = 0;
            if (managers != null)
            {
                foreach (PlayerInputManager manager in managers)
                {
                    if (manager != null)
                    {
                        count++;
                    }
                }
            }

            return FromManagerCount(count, source, reason);
        }

        public static UnityInputPlayerInputManagerEvidence FromManagerCount(
            int managerCount,
            string source,
            string reason)
        {
            return FromManagerCount(
                managerCount,
                source,
                reason,
                UnityInputPlayerInputManagerScope.Unknown,
                false);
        }

        public static UnityInputPlayerInputManagerEvidence FromRequiredSessionManagerCount(
            int managerCount,
            string source,
            string reason)
        {
            return FromManagerCount(
                managerCount,
                source,
                reason,
                UnityInputPlayerInputManagerScope.Session,
                true);
        }

        private static UnityInputPlayerInputManagerEvidence FromManagerCount(
            int managerCount,
            string source,
            string reason,
            UnityInputPlayerInputManagerScope scope,
            bool required)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityInputPlayerInputManagerEvidence));
            int count = managerCount < 0 ? 0 : managerCount;
            var issues = new List<UnityInputTargetSetIssue>();

            if (required && count == 0)
            {
                issues.Add(UnityInputTargetSetIssue.BlockingIssue(
                    UnityInputTargetSetIssueKind.MissingRequiredPlayerInputManager,
                    UnityInputTargetRole.Unknown,
                    string.Empty,
                    normalizedSource,
                    "One Session-scoped Unity PlayerInputManager evidence component is required."));
            }

            if (count > 1)
            {
                issues.Add(UnityInputTargetSetIssue.BlockingIssue(
                    UnityInputTargetSetIssueKind.DuplicatePlayerInputManager,
                    UnityInputTargetRole.Unknown,
                    string.Empty,
                    normalizedSource,
                    scope == UnityInputPlayerInputManagerScope.Session
                        ? "Only one Session-scoped Unity PlayerInputManager evidence component is allowed."
                        : "Only one Unity PlayerInputManager evidence component is allowed in the current validation scope."));
            }

            return new UnityInputPlayerInputManagerEvidence(count, issues.ToArray(), normalizedSource, reason, scope, required);
        }
    }
}
