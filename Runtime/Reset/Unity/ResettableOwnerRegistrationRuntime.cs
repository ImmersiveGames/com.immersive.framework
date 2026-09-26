using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// Internal IF-ADR-035 RESET-035-B owner-aware registration of <see cref="Resettable"/> content.
    /// Route/Activity transactions call it with their target <see cref="RuntimeContentOwner"/> and the
    /// explicit materialized roots of that transaction. It never resolves a current owner, never searches
    /// globally and reuses the existing <see cref="ResetRegistry"/> runtime identity generation.
    /// Registration is atomic per call; rollback and release remove every registration of one owner.
    /// </summary>
    internal sealed class ResettableOwnerRegistrationRuntime
    {
        internal const string RuntimeSubjectPrefix = "resettable";
        private const string DefaultSource = nameof(ResettableOwnerRegistrationRuntime);

        private readonly ResetRegistry _registry;
        private readonly Dictionary<RuntimeContentOwner, List<Resettable>> _resettablesByOwner =
            new Dictionary<RuntimeContentOwner, List<Resettable>>();

        internal ResettableOwnerRegistrationRuntime(ResetRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        internal ResetRegistry Registry => _registry;

        internal int GetRegisteredResettableCount(RuntimeContentOwner owner)
        {
            return _resettablesByOwner.TryGetValue(owner, out List<Resettable> resettables)
                ? resettables.Count
                : 0;
        }

        internal bool TryRegisterOwnerContent(
            RuntimeContentOwner owner,
            IReadOnlyList<GameObject> roots,
            string source,
            string reason,
            out string diagnostic)
        {
            string resolvedSource = source.NormalizeTextOrFallback(DefaultSource);
            string resolvedReason = reason.NormalizeTextOrFallback("resettable-owner-registration");
            if (!TryMapOwnerScope(owner, out ResetSubjectScope scope))
            {
                diagnostic =
                    $"resettable-owner-invalid: Resettable registration requires a valid Route or Activity owner. owner='{DescribeOwner(owner)}'.";
                return false;
            }

            if (GetRegisteredResettableCount(owner) > 0)
            {
                diagnostic =
                    $"resettable-owner-already-registered: owner='{owner.StableText}' already has '{GetRegisteredResettableCount(owner)}' Resettable registrations; the previous occurrence must be released first.";
                return false;
            }

            List<Resettable> resettables = CollectResettables(roots);
            if (resettables.Count == 0)
            {
                diagnostic = $"resettables-absent: owner='{owner.StableText}' roots='{roots?.Count ?? 0}'.";
                return true;
            }

            var capabilitiesByResettable = new List<List<MonoBehaviour>>(resettables.Count);
            var issues = new StringBuilder();
            int issueCount = 0;
            for (int index = 0; index < resettables.Count; index++)
            {
                Resettable resettable = resettables[index];
                var capabilities = new List<MonoBehaviour>();
                var legacyAdapters = new List<UnityResetSubjectAdapter>();
                ResettableBoundary.Collect(resettable, capabilities, legacyAdapters);
                capabilitiesByResettable.Add(capabilities);

                if (resettable.IsRegistered)
                {
                    issueCount++;
                    issues.Append(
                        $" resettable='{resettable.DisplayName}' issue='already-registered' owner='{resettable.Owner.StableText}'.");
                }

                if (legacyAdapters.Count > 0)
                {
                    issueCount++;
                    issues.Append(
                        $" resettable='{resettable.DisplayName}' issue='mixed-legacy-adapter' detail='UnityResetSubjectAdapter inside the Resettable boundary ({legacyAdapters.Count}); use either Resettable or the legacy adapter, not both.'.");
                }

                UnityResetSubjectAdapter coveringAncestor =
                    ResettableBoundary.FindCoveringLegacyAncestor(resettable);
                if (coveringAncestor != null)
                {
                    issueCount++;
                    issues.Append(
                        $" resettable='{resettable.DisplayName}' issue='mixed-legacy-adapter' detail='ancestor UnityResetSubjectAdapter \"{coveringAncestor.name}\" uses Children discovery and would also collect this Resettable boundary.'.");
                }
            }

            if (issueCount > 0)
            {
                diagnostic =
                    $"resettable-configuration-invalid: owner='{owner.StableText}' resettables='{resettables.Count}' issues='{issueCount}'." +
                    issues;
                return false;
            }

            var registered = new List<Resettable>(resettables.Count);
            int capabilityTotal = 0;
            for (int index = 0; index < resettables.Count; index++)
            {
                Resettable resettable = resettables[index];
                if (!TryRegisterResettable(
                        resettable,
                        capabilitiesByResettable[index],
                        owner,
                        scope,
                        resolvedSource,
                        resolvedReason,
                        out int capabilityCount,
                        out string registrationIssue))
                {
                    for (int rollbackIndex = registered.Count - 1; rollbackIndex >= 0; rollbackIndex--)
                    {
                        UnregisterResettable(registered[rollbackIndex], resolvedSource, "resettable-registration-atomic-rollback");
                    }

                    diagnostic =
                        $"resettable-registration-failed: owner='{owner.StableText}' resettable='{resettable.DisplayName}'. {registrationIssue} Previously created registrations of this call were removed.";
                    return false;
                }

                registered.Add(resettable);
                capabilityTotal += capabilityCount;
            }

            _resettablesByOwner[owner] = registered;
            diagnostic =
                $"resettables-registered: owner='{owner.StableText}' scope='{scope}' resettables='{registered.Count}' capabilities='{capabilityTotal}'.";
            return true;
        }

        /// <summary>Pre-commit rollback of the target owner registrations prepared by a transaction.</summary>
        internal bool TryRollbackOwner(
            RuntimeContentOwner owner,
            string source,
            string reason,
            out string diagnostic)
        {
            return TryRemoveOwner(owner, source, reason, "rollback", out diagnostic);
        }

        /// <summary>Release of every Resettable registration owned by an exiting Route/Activity owner.</summary>
        internal bool TryReleaseOwner(
            RuntimeContentOwner owner,
            string source,
            string reason,
            out string diagnostic)
        {
            return TryRemoveOwner(owner, source, reason, "release", out diagnostic);
        }

        private bool TryRemoveOwner(
            RuntimeContentOwner owner,
            string source,
            string reason,
            string operation,
            out string diagnostic)
        {
            string resolvedSource = source.NormalizeTextOrFallback(DefaultSource);
            string resolvedReason = reason.NormalizeTextOrFallback("resettable-owner-" + operation);
            if (!owner.IsValid ||
                !_resettablesByOwner.TryGetValue(owner, out List<Resettable> resettables))
            {
                diagnostic = $"resettable-{operation}-not-required: owner='{DescribeOwner(owner)}' has no Resettable registrations.";
                return true;
            }

            int failed = 0;
            for (int index = resettables.Count - 1; index >= 0; index--)
            {
                if (!UnregisterResettable(resettables[index], resolvedSource, resolvedReason))
                {
                    failed++;
                }
            }

            _resettablesByOwner.Remove(owner);
            diagnostic = failed == 0
                ? $"resettable-{operation}-completed: owner='{owner.StableText}' resettables='{resettables.Count}'."
                : $"resettable-{operation}-failed: owner='{owner.StableText}' resettables='{resettables.Count}' failed='{failed}'.";
            return failed == 0;
        }

        private bool TryRegisterResettable(
            Resettable resettable,
            IReadOnlyList<MonoBehaviour> capabilities,
            RuntimeContentOwner owner,
            ResetSubjectScope scope,
            string source,
            string reason,
            out int capabilityCount,
            out string issue)
        {
            capabilityCount = 0;
            ResetRegistryOperationResult subjectResult = _registry.RegisterRuntimeSubject(
                RuntimeSubjectPrefix,
                scope,
                owner,
                resettable,
                resettable.DisplayName,
                "Resettable:RESET-035-B",
                source,
                reason);
            if (!subjectResult.Succeeded)
            {
                issue = "Subject registration rejected. " + FirstIssue(subjectResult);
                return false;
            }

            ResetRegistrationHandle subjectHandle = subjectResult.Handle;
            for (int index = 0; index < capabilities.Count; index++)
            {
                MonoBehaviour capability = capabilities[index];
                var participant = new ResettableCapabilityParticipant(capability, index);
                ResetRegistryOperationResult participantResult = _registry.RegisterParticipant(
                    subjectHandle,
                    participant,
                    capability,
                    source,
                    reason);
                if (!participantResult.Succeeded)
                {
                    _registry.Unregister(subjectHandle, resettable, source, "resettable-capability-registration-failed");
                    issue =
                        $"Capability '{capability.GetType().Name}' registration rejected. " +
                        FirstIssue(participantResult);
                    return false;
                }

                capabilityCount++;
            }

            resettable.MarkRegistered(subjectHandle, subjectResult.Subject, capabilityCount);
            issue = string.Empty;
            return true;
        }

        private bool UnregisterResettable(Resettable resettable, string source, string reason)
        {
            if (ReferenceEquals(resettable, null))
            {
                return true;
            }

            ResetRegistrationHandle handle = resettable.SubjectHandle;
            if (!handle.IsSubject)
            {
                return true;
            }

            ResetRegistryOperationResult result = _registry.Unregister(handle, resettable, source, reason);
            if (resettable != null)
            {
                resettable.ClearRegistrationEvidence();
            }

            return result.Succeeded;
        }

        private static List<Resettable> CollectResettables(IReadOnlyList<GameObject> roots)
        {
            var result = new List<Resettable>();
            if (roots == null)
            {
                return result;
            }

            var seenRoots = new HashSet<GameObject>();
            var seenResettables = new HashSet<Resettable>();
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null || !seenRoots.Add(root))
                {
                    continue;
                }

                Resettable[] candidates = root.GetComponentsInChildren<Resettable>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    Resettable candidate = candidates[index];
                    if (candidate != null && seenResettables.Add(candidate))
                    {
                        result.Add(candidate);
                    }
                }
            }

            return result;
        }

        private static bool TryMapOwnerScope(RuntimeContentOwner owner, out ResetSubjectScope scope)
        {
            scope = ResetSubjectScope.Unknown;
            if (!owner.IsValid)
            {
                return false;
            }

            switch (owner.Scope)
            {
                case RuntimeContentScope.Activity:
                    scope = ResetSubjectScope.Activity;
                    return true;
                case RuntimeContentScope.Route:
                    scope = ResetSubjectScope.Route;
                    return true;
                default:
                    return false;
            }
        }

        private static string FirstIssue(ResetRegistryOperationResult result)
        {
            return result.Issues.Count > 0 ? result.Issues[0].Message : result.Message;
        }

        private static string DescribeOwner(RuntimeContentOwner owner)
        {
            return owner.IsValid ? owner.StableText : "<invalid>";
        }
    }
}
