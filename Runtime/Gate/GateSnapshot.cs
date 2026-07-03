using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Gate
{
    /// <summary>
    /// API status: Experimental. Passive immutable snapshot of active Gate blockers.
    /// The snapshot can evaluate admission but does not own registration, lifecycle, queueing or execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F17B passive Gate snapshot primitive; no runtime registry or flow integration.")]
    public readonly struct GateSnapshot
    {
        private readonly GateBlocker[] _blockers;

        public GateSnapshot(IReadOnlyList<GateBlocker> blockers)
        {
            if (FrameworkCollectionCopy.IsNullOrEmpty(blockers))
            {
                _blockers = Array.Empty<GateBlocker>();
                return;
            }

            _blockers = FrameworkCollectionCopy.ToArrayOrEmpty(blockers);
            for (int i = 0; i < _blockers.Length; i++)
            {
                if (!_blockers[i].IsValid)
                {
                    throw new ArgumentException("Gate snapshot cannot contain invalid blockers.", nameof(blockers));
                }
            }
        }

        public IReadOnlyList<GateBlocker> Blockers => FrameworkCollectionCopy.IsNullOrEmpty(_blockers) ? Array.Empty<GateBlocker>() : _blockers;

        public int BlockerCount => Blockers.Count;

        public bool IsEmpty => BlockerCount == 0;

        public bool HasBlockers => BlockerCount > 0;

        public int CountByScope(GateScope scope)
        {
            if (scope == GateScope.Unknown || !HasBlockers)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<GateBlocker> items = Blockers;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Scope == scope)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountByDomain(GateDomain domain)
        {
            if (domain == GateDomain.Unknown || !HasBlockers)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<GateBlocker> items = Blockers;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Domain == domain)
                {
                    count++;
                }
            }

            return count;
        }

        public bool IsBlocked(GateScope scope, GateDomain domain)
        {
            return IsBlocked(scope, domain, default);
        }

        public bool IsBlocked(GateScope scope, GateDomain domain, FrameworkIdentityKey owner)
        {
            if (scope == GateScope.Unknown || domain == GateDomain.Unknown || !HasBlockers)
            {
                return false;
            }

            IReadOnlyList<GateBlocker> items = Blockers;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Blocks(scope, domain, owner))
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<GateBlocker> GetBlockingBlockers(GateScope scope, GateDomain domain)
        {
            return GetBlockingBlockers(scope, domain, default);
        }

        public IReadOnlyList<GateBlocker> GetBlockingBlockers(
            GateScope scope,
            GateDomain domain,
            FrameworkIdentityKey owner)
        {
            if (scope == GateScope.Unknown || domain == GateDomain.Unknown || !HasBlockers)
            {
                return Array.Empty<GateBlocker>();
            }

            var matches = new List<GateBlocker>();
            IReadOnlyList<GateBlocker> items = Blockers;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Blocks(scope, domain, owner))
                {
                    matches.Add(items[i]);
                }
            }

            return matches.Count == 0 ? Array.Empty<GateBlocker>() : matches.ToArray();
        }

        public GateEvaluationResult Evaluate(
            GateScope scope,
            GateDomain domain,
            FrameworkIdentityKey owner,
            string subject,
            string source,
            string reason,
            string policySource)
        {
            var safeScope = Enum.IsDefined(typeof(GateScope), scope) ? scope : GateScope.Unknown;
            var safeDomain = Enum.IsDefined(typeof(GateDomain), domain) ? domain : GateDomain.Unknown;

            if (safeScope == GateScope.Unknown)
            {
                var decision = GateDecision.Rejected(
                    GateDecisionStatus.RejectedInvalidScope,
                    safeScope,
                    safeDomain,
                    owner,
                    subject,
                    source,
                    "Gate evaluation rejected because scope is missing or invalid.",
                    policySource);

                return GateEvaluationResult.Rejected(
                    decision,
                    new[] { "Gate scope must be explicit." });
            }

            if (safeDomain == GateDomain.Unknown)
            {
                var decision = GateDecision.Rejected(
                    GateDecisionStatus.RejectedInvalidDomain,
                    safeScope,
                    safeDomain,
                    owner,
                    subject,
                    source,
                    "Gate evaluation rejected because domain is missing or invalid.",
                    policySource);

                return GateEvaluationResult.Rejected(
                    decision,
                    new[] { "Gate domain must be explicit." });
            }

            IReadOnlyList<GateBlocker> blocking = GetBlockingBlockers(safeScope, safeDomain, owner);
            if (blocking.Count == 0)
            {
                var decision = GateDecision.Allowed(
                    safeScope,
                    safeDomain,
                    owner,
                    subject,
                    source,
                    reason,
                    policySource);

                return GateEvaluationResult.Allowed(
                    decision,
                    new[] { "No matching Gate blockers were active." });
            }

            var blockedDecision = GateDecision.Blocked(
                safeScope,
                safeDomain,
                owner,
                subject,
                source,
                reason,
                policySource);

            return GateEvaluationResult.Blocked(
                blockedDecision,
                blocking,
                new[] { "One or more matching Gate blockers are active." });
        }

        public static GateSnapshot Empty()
        {
            return new GateSnapshot(Array.Empty<GateBlocker>());
        }
    }
}
