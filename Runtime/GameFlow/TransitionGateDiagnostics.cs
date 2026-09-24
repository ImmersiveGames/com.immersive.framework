using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Gate;
using Immersive.Framework.Transition;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// API status: Internal. Request-level Transition Gate diagnostics copied into logs.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F37 request-level Transition Gate diagnostics.")]
    internal readonly struct TransitionGateDiagnostics
    {
        private readonly GateSnapshot _snapshot;

        private TransitionGateDiagnostics(
            TransitionGateMode mode,
            bool applied,
            bool released,
            GateSnapshot snapshot,
            GateEvaluationResult blockingEvaluation)
        {
            Mode = Enum.IsDefined(typeof(TransitionGateMode), mode) ? mode : TransitionGateMode.None;
            Applied = applied;
            Released = released;
            _snapshot = snapshot;
            BlockingEvaluation = blockingEvaluation;
        }

        public TransitionGateMode Mode { get; }

        public bool Applied { get; }

        public bool Released { get; }

        public GateSnapshot Snapshot => _snapshot;

        public GateEvaluationResult BlockingEvaluation { get; }

        public bool HasBlockingEvaluation => BlockingEvaluation.IsValid && !BlockingEvaluation.IsAllowed;

        public int BlockerCount => Snapshot.BlockerCount;

        public int BlockingIssueCount => HasBlockingEvaluation
            ? Math.Max(BlockingEvaluation.BlockingBlockerCount, BlockerCount)
            : 0;

        public bool BlocksInputAcceptance => TransitionGateBlockerPolicy.BlocksInputAcceptance(Mode);

        public bool BlocksInteractionAcceptance => TransitionGateBlockerPolicy.BlocksInteractionAcceptance(Mode);

        public bool BlocksGameplayAction => TransitionGateBlockerPolicy.BlocksGameplayAction(Mode);

        public string ModeText => Mode.ToString();

        public string BlockersText => BlockerCount > 0 ? FormatBlockers() : "<none>";

        public string BlockingIssuesText => HasBlockingEvaluation
            ? BlockingEvaluation.ToDiagnosticString()
            : "<none>";

        public string GateText => Applied
            ? $"mode='{Mode}' applied='{Applied}' released='{Released}' blockers='{BlockerCount}'"
            : $"mode='{Mode}' applied='False' released='{Released}' blockers='0'";

        internal static TransitionGateDiagnostics NotApplied(TransitionGateMode mode)
        {
            return new TransitionGateDiagnostics(mode, false, false, GateSnapshot.Empty(), default);
        }

        internal static TransitionGateDiagnostics AppliedAndReleased(TransitionGateMode mode, GateSnapshot snapshot)
        {
            return new TransitionGateDiagnostics(mode, true, true, snapshot, default);
        }

        internal static TransitionGateDiagnostics Rejected(TransitionGateMode mode, GateSnapshot snapshot, GateEvaluationResult evaluation)
        {
            return new TransitionGateDiagnostics(mode, snapshot.HasBlockers, false, snapshot, evaluation);
        }

        private string FormatBlockers()
        {
            var blockers = Snapshot.Blockers;
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < blockers.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("; ");
                }

                builder.Append(blockers[i].ToDiagnosticString());
            }

            return builder.Length > 0 ? builder.ToString() : "<none>";
        }
    }
}
