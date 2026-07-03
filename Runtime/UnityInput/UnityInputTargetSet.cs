using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Immutable validation snapshot for declared Unity Input targets.
    /// It proves target ownership only; it does not read input, switch action maps or mutate PlayerInput.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F29A Unity Input target declaration validation snapshot.")]
    public sealed class UnityInputTargetSet
    {
        private static readonly UnityInputTargetRole[] DefaultRequiredRoles =
        {
            UnityInputTargetRole.GlobalUiPause,
            UnityInputTargetRole.GameplayCommands
        };

        private readonly UnityInputTargetDescriptor[] _targets;
        private readonly UnityInputTargetSetIssue[] _issues;

        private UnityInputTargetSet(UnityInputTargetDescriptor[] targets, UnityInputTargetSetIssue[] issues, string source, string reason)
        {
            _targets = targets ?? Array.Empty<UnityInputTargetDescriptor>();
            _issues = issues ?? Array.Empty<UnityInputTargetSetIssue>();
            Source = source.NormalizeTextOrFallback(nameof(UnityInputTargetSet));
            Reason = reason.NormalizeText();
        }

        public IReadOnlyList<UnityInputTargetDescriptor> Targets => _targets;

        public IReadOnlyList<UnityInputTargetSetIssue> Issues => _issues;

        public string Source { get; }

        public string Reason { get; }

        public int Count => _targets.Length;

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

        public int GlobalUiPauseTargetCount => CountRole(UnityInputTargetRole.GlobalUiPause);

        public int GameplayCommandTargetCount => CountRole(UnityInputTargetRole.GameplayCommands);

        public int PlayerInputReferenceCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _targets.Length; i++)
                {
                    if (_targets[i].HasPlayerInputReference)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int RequiredPlayerInputEvidenceCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _targets.Length; i++)
                {
                    if (_targets[i].RequiresPlayerInputEvidence)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool Succeeded => BlockingIssueCount == 0;

        public bool Failed => !Succeeded;

        public bool AppliesInputBehavior => false;

        public bool SwitchesActionMaps => false;

        public bool TryGetSingle(UnityInputTargetRole role, out UnityInputTargetDescriptor descriptor)
        {
            descriptor = default;
            bool found = false;
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i].Role != role)
                {
                    continue;
                }

                if (found)
                {
                    descriptor = default;
                    return false;
                }

                descriptor = _targets[i];
                found = true;
            }

            return found;
        }

        public int CountRole(UnityInputTargetRole role)
        {
            int count = 0;
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i].Role == role)
                {
                    count++;
                }
            }

            return count;
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("unityInputTargets='").Append(Count).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" globalUiPauseTargets='").Append(GlobalUiPauseTargetCount).Append("'");
            builder.Append(" gameplayCommandTargets='").Append(GameplayCommandTargetCount).Append("'");
            builder.Append(" playerInputReferences='").Append(PlayerInputReferenceCount).Append("'");
            builder.Append(" requiredPlayerInputEvidence='").Append(RequiredPlayerInputEvidenceCount).Append("'");
            builder.Append(" actionMapSwitching='").Append(SwitchesActionMaps).Append("'");
            for (int i = 0; i < _issues.Length; i++)
            {
                builder.Append(" issue[").Append(i).Append("]='").Append(_issues[i]).Append("'");
            }

            return builder.ToString();
        }

        public static UnityInputTargetSet FromDescriptors(
            IEnumerable<UnityInputTargetDescriptor> descriptors,
            string source,
            string reason)
        {
            return FromDescriptors(descriptors, DefaultRequiredRoles, Array.Empty<UnityInputTargetSetIssue>(), source, reason);
        }

        public static UnityInputTargetSet FromDescriptors(
            IEnumerable<UnityInputTargetDescriptor> descriptors,
            IEnumerable<UnityInputTargetRole> requiredRoles,
            IEnumerable<UnityInputTargetSetIssue> existingIssues,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityInputTargetSet));
            var targets = new List<UnityInputTargetDescriptor>();
            if (descriptors != null)
            {
                foreach (UnityInputTargetDescriptor descriptor in descriptors)
                {
                    if (descriptor.IsValid)
                    {
                        targets.Add(descriptor);
                    }
                }
            }

            var issues = new List<UnityInputTargetSetIssue>();
            if (existingIssues != null)
            {
                foreach (UnityInputTargetSetIssue issue in existingIssues)
                {
                    if (issue.Kind != UnityInputTargetSetIssueKind.None)
                    {
                        issues.Add(issue);
                    }
                }
            }

            AddRequiredRoleIssues(targets, requiredRoles, issues, normalizedSource);
            AddDuplicateIdIssues(targets, issues, normalizedSource);
            AddRequiredPlayerInputEvidenceIssues(targets, issues, normalizedSource);

            return new UnityInputTargetSet(targets.ToArray(), issues.ToArray(), normalizedSource, reason);
        }

        private static void AddRequiredRoleIssues(
            IReadOnlyList<UnityInputTargetDescriptor> targets,
            IEnumerable<UnityInputTargetRole> requiredRoles,
            List<UnityInputTargetSetIssue> issues,
            string source)
        {
            IEnumerable<UnityInputTargetRole> roles = requiredRoles ?? DefaultRequiredRoles;
            foreach (UnityInputTargetRole role in roles)
            {
                if (!Enum.IsDefined(typeof(UnityInputTargetRole), role) || role == UnityInputTargetRole.Unknown)
                {
                    issues.Add(UnityInputTargetSetIssue.BlockingIssue(
                        UnityInputTargetSetIssueKind.InvalidTargetRole,
                        role,
                        string.Empty,
                        source,
                        "Required Unity Input target role must be explicit."));
                    continue;
                }

                int count = CountRole(targets, role);
                if (count == 0)
                {
                    issues.Add(UnityInputTargetSetIssue.BlockingIssue(
                        UnityInputTargetSetIssueKind.MissingRequiredRole,
                        role,
                        string.Empty,
                        source,
                        "Required Unity Input target role is missing."));
                    continue;
                }

                if (count > 1)
                {
                    issues.Add(UnityInputTargetSetIssue.BlockingIssue(
                        UnityInputTargetSetIssueKind.DuplicateRequiredRole,
                        role,
                        string.Empty,
                        source,
                        "Required Unity Input target role has duplicate declarations."));
                }
            }
        }


        private static void AddRequiredPlayerInputEvidenceIssues(
            IReadOnlyList<UnityInputTargetDescriptor> targets,
            List<UnityInputTargetSetIssue> issues,
            string source)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                UnityInputTargetDescriptor target = targets[i];
                if (!target.RequiresPlayerInputEvidence || target.HasPlayerInputReference)
                {
                    continue;
                }

                issues.Add(UnityInputTargetSetIssue.BlockingIssue(
                    UnityInputTargetSetIssueKind.MissingRequiredPlayerInputEvidence,
                    target.Role,
                    target.TargetId.StableText,
                    source,
                    "Unity Input target requires PlayerInput evidence, but no PlayerInput component/reference was found."));
            }
        }

        private static void AddDuplicateIdIssues(
            IReadOnlyList<UnityInputTargetDescriptor> targets,
            List<UnityInputTargetSetIssue> issues,
            string source)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var duplicated = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < targets.Count; i++)
            {
                string id = targets[i].TargetId.StableText;
                if (!seen.Add(id) && duplicated.Add(id))
                {
                    issues.Add(UnityInputTargetSetIssue.BlockingIssue(
                        UnityInputTargetSetIssueKind.DuplicateTargetId,
                        targets[i].Role,
                        id,
                        source,
                        "Unity Input target id is declared more than once."));
                }
            }
        }

        private static int CountRole(IReadOnlyList<UnityInputTargetDescriptor> targets, UnityInputTargetRole role)
        {
            int count = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Role == role)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
