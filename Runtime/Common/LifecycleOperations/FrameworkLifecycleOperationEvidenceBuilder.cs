using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.LifecycleOperations
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F44 lifecycle-local operation evidence builder; collects caller-owned evidence only.")]
    internal sealed class FrameworkLifecycleOperationEvidenceBuilder
    {
        private readonly List<FrameworkLifecycleOperationStageEvidence> _stages = new List<FrameworkLifecycleOperationStageEvidence>();
        private bool _explicitNoOp;

        public FrameworkLifecycleOperationEvidenceBuilder(
            FrameworkLifecycleOperationKind operationKind,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(FrameworkLifecycleOperationKind), operationKind) || operationKind == FrameworkLifecycleOperationKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(operationKind), operationKind, "Lifecycle operation evidence builder requires an explicit lifecycle-local operation kind.");
            }

            OperationKind = operationKind;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        public FrameworkLifecycleOperationKind OperationKind { get; }

        public string Source { get; }

        public string Reason { get; }

        public FrameworkLifecycleOperationEvidenceBuilder AddStage(
            FrameworkLifecycleOperationStage stage,
            string statusText,
            string source,
            string reason,
            int issueCount,
            int blockingIssueCount,
            bool sideEffectsApplied,
            bool failed,
            bool skipped,
            string message,
            string originalEvidenceText = "")
        {
            _stages.Add(new FrameworkLifecycleOperationStageEvidence(
                stage,
                statusText,
                source,
                reason,
                issueCount,
                blockingIssueCount,
                sideEffectsApplied,
                failed,
                skipped,
                message,
                originalEvidenceText));
            return this;
        }

        public FrameworkLifecycleOperationEvidenceBuilder MarkExplicitNoOp()
        {
            _explicitNoOp = true;
            return this;
        }

        public FrameworkLifecycleOperationEvidence Build()
        {
            return new FrameworkLifecycleOperationEvidence(OperationKind, Source, Reason, _stages, _explicitNoOp);
        }
    }
}
