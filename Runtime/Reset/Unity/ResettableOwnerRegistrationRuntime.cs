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
            if (!ResetCompositionResolver.TryResolveMemberships(
                    roots,
                    resettables,
                    out Dictionary<Resettable, ResetMembership> membershipByResettable,
                    out string compositionDiagnostic))
            {
                diagnostic =
                    $"reset-composition-resolution-failed: owner='{owner.StableText}'. {compositionDiagnostic}";
                return false;
            }

            if (resettables.Count == 0)
            {
                diagnostic = $"resettables-absent: owner='{owner.StableText}' roots='{roots?.Count ?? 0}'. {compositionDiagnostic}".TrimEnd();
                return true;
            }

            var capabilitiesByResettable = new List<List<MonoBehaviour>>(resettables.Count);
            var issues = new StringBuilder();
            int issueCount = 0;
            for (int index = 0; index < resettables.Count; index++)
            {
                Resettable resettable = resettables[index];
                var capabilities = new List<MonoBehaviour>();
                ResettableBoundary.Collect(resettable, capabilities);
                capabilitiesByResettable.Add(capabilities);

                if (resettable.IsRegistered)
                {
                    issueCount++;
                    issues.Append(
                        $" resettable='{resettable.DisplayName}' issue='already-registered' owner='{resettable.Owner.StableText}'.");
                }

                ResetMembership resolvedMembership = membershipByResettable[resettable];
                if (!ResetSubject.IsMembershipCompatibleWithOwner(resolvedMembership, owner))
                {
                    issueCount++;
                    issues.Append(
                        $" resettable='{resettable.DisplayName}' issue='membership-owner-incompatible' membership='{resolvedMembership}' owner='{owner.StableText}'. Activity-owned content cannot have Route reset membership.");
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
                        membershipByResettable[resettable],
                        resolvedSource,
                        resolvedReason,
                        out int capabilityCount,
                        out string registrationIssue))
                {
                    var rollbackFailures = new List<Resettable>();
                    var rollbackDiagnostics = new List<string>();
                    if (resettable.IsRegistered)
                    {
                        rollbackFailures.Add(resettable);
                        rollbackDiagnostics.Add($"handle='{resettable.SubjectHandle}' status='compensation-failed' message='{registrationIssue}'");
                    }
                    for (int rollbackIndex = registered.Count - 1; rollbackIndex >= 0; rollbackIndex--)
                    {
                        Resettable rollbackTarget = registered[rollbackIndex];
                        if (!UnregisterResettable(
                                rollbackTarget,
                                resolvedSource,
                                "resettable-registration-atomic-rollback",
                                out ResetRegistryOperationResult rollbackResult))
                        {
                            rollbackFailures.Add(rollbackTarget);
                            rollbackDiagnostics.Add($"handle='{rollbackTarget.SubjectHandle}' status='{rollbackResult.Status}' message='{rollbackResult.Message}'");
                        }
                    }

                    if (rollbackFailures.Count > 0)
                    {
                        rollbackFailures.Reverse();
                        _resettablesByOwner[owner] = rollbackFailures;
                    }

                    diagnostic =
                        $"resettable-registration-failed: owner='{owner.StableText}' resettable='{resettable.DisplayName}'. {registrationIssue} " +
                        (rollbackFailures.Count == 0
                            ? "Previously created registrations of this call were removed."
                            : $"Atomic rollback incomplete; retry evidence retained for '{rollbackFailures.Count}' registrations. {string.Join("; ", rollbackDiagnostics)}.");
                    return false;
                }

                registered.Add(resettable);
                capabilityTotal += capabilityCount;
            }

            _resettablesByOwner[owner] = registered;
            diagnostic =
                $"resettables-registered: owner='{owner.StableText}' scope='{scope}' resettables='{registered.Count}' capabilities='{capabilityTotal}'. {compositionDiagnostic}".TrimEnd();
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

            var failedResettables = new List<Resettable>();
            var failedHandles = new List<string>();
            for (int index = resettables.Count - 1; index >= 0; index--)
            {
                Resettable resettable = resettables[index];
                if (!UnregisterResettable(resettable, resolvedSource, resolvedReason, out ResetRegistryOperationResult result))
                {
                    failedResettables.Add(resettable);
                    failedHandles.Add($"handle='{resettable.SubjectHandle}' status='{result.Status}' message='{result.Message}'");
                }
            }

            if (failedResettables.Count == 0)
            {
                _resettablesByOwner.Remove(owner);
            }
            else
            {
                failedResettables.Reverse();
                _resettablesByOwner[owner] = failedResettables;
            }

            diagnostic = failedResettables.Count == 0
                ? $"resettable-{operation}-completed: owner='{owner.StableText}' resettables='{resettables.Count}'."
                : $"resettable-{operation}-failed: owner='{owner.StableText}' resettables='{resettables.Count}' failed='{failedResettables.Count}'. Retry evidence retained. {string.Join("; ", failedHandles)}.";
            return failedResettables.Count == 0;
        }

        private bool TryRegisterResettable(
            Resettable resettable,
            IReadOnlyList<MonoBehaviour> capabilities,
            RuntimeContentOwner owner,
            ResetSubjectScope scope,
            ResetMembership membership,
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
                reason,
                membership);
            if (!subjectResult.Succeeded)
            {
                issue = "Subject registration rejected. " + FirstIssue(subjectResult);
                return false;
            }

            ResetRegistrationHandle subjectHandle = subjectResult.Handle;
            resettable.MarkRegistered(subjectHandle, subjectResult.Subject, 0);
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
                    resettable.MarkRegistered(subjectHandle, subjectResult.Subject, capabilityCount);
                    bool compensationSucceeded = UnregisterResettable(
                        resettable,
                        source,
                        "resettable-capability-registration-failed",
                        out ResetRegistryOperationResult compensationResult);
                    issue =
                        $"Capability '{capability.GetType().Name}' registration rejected. " +
                        FirstIssue(participantResult) +
                        (compensationSucceeded
                            ? " Subject compensation succeeded."
                            : $" Subject compensation failed; receipt retained. status='{compensationResult.Status}' message='{compensationResult.Message}'.");
                    return false;
                }

                capabilityCount++;
            }

            resettable.MarkRegistered(subjectHandle, subjectResult.Subject, capabilityCount);
            issue = string.Empty;
            return true;
        }

        private bool UnregisterResettable(
            Resettable resettable,
            string source,
            string reason,
            out ResetRegistryOperationResult result)
        {
            if (ReferenceEquals(resettable, null))
            {
                result = default;
                return true;
            }

            ResetRegistrationHandle handle = resettable.SubjectHandle;
            if (!handle.IsSubject)
            {
                result = default;
                return true;
            }

            result = _registry.Unregister(handle, resettable, source, reason);
            if (result.Succeeded)
            {
                resettable.ClearRegistrationEvidence();
                return true;
            }

            return false;
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
