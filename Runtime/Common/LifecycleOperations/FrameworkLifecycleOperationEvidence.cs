using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.LifecycleOperations
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F44 lifecycle-local operation evidence projection; does not decide domain success.")]
    internal readonly struct FrameworkLifecycleOperationEvidence : IEquatable<FrameworkLifecycleOperationEvidence>
    {
        private readonly FrameworkLifecycleOperationStageEvidence[] _stages;

        public FrameworkLifecycleOperationEvidence(
            FrameworkLifecycleOperationKind operationKind,
            string source,
            string reason,
            IReadOnlyList<FrameworkLifecycleOperationStageEvidence> stages,
            bool explicitNoOp = false)
        {
            if (!Enum.IsDefined(typeof(FrameworkLifecycleOperationKind), operationKind) || operationKind == FrameworkLifecycleOperationKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(operationKind), operationKind, "Lifecycle operation evidence requires an explicit lifecycle-local operation kind.");
            }

            if ((stages == null || stages.Count == 0) && !explicitNoOp)
            {
                throw new ArgumentException("Lifecycle operation evidence requires stage evidence unless it is explicitly no-op.", nameof(stages));
            }

            OperationKind = operationKind;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            _stages = CopyStages(stages);
            ExplicitNoOp = explicitNoOp;
        }

        public FrameworkLifecycleOperationKind OperationKind { get; }

        public string OperationKindText => OperationKind.ToString();

        public string Source { get; }

        public string Reason { get; }

        public IReadOnlyList<FrameworkLifecycleOperationStageEvidence> Stages => _stages ?? Array.Empty<FrameworkLifecycleOperationStageEvidence>();

        public int StageCount => Stages.Count;

        public int IssueCount => CountIssues();

        public int BlockingIssueCount => CountBlockingIssues();

        public int SideEffectCount => CountSideEffects();

        public int FailedStageCount => CountFailedStages();

        public int SkippedStageCount => CountSkippedStages();

        public bool ExplicitNoOp { get; }

        public string StageNamesText => BuildStageNamesText();

        public string StageStatusesText => BuildStageStatusesText();

        public string DiagnosticText => FrameworkLifecycleOperationDiagnostics.BuildDiagnosticString(this);

        public bool Equals(FrameworkLifecycleOperationEvidence other)
        {
            return OperationKind == other.OperationKind
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && ExplicitNoOp == other.ExplicitNoOp
                && SequenceEquals(Stages, other.Stages);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkLifecycleOperationEvidence other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)OperationKind;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ ExplicitNoOp.GetHashCode();
                for (int i = 0; i < Stages.Count; i++)
                {
                    hashCode = hashCode * 397 ^ Stages[i].GetHashCode();
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return DiagnosticText;
        }

        private static FrameworkLifecycleOperationStageEvidence[] CopyStages(IReadOnlyList<FrameworkLifecycleOperationStageEvidence> stages)
        {
            if (stages == null || stages.Count == 0)
            {
                return Array.Empty<FrameworkLifecycleOperationStageEvidence>();
            }

            var copy = new FrameworkLifecycleOperationStageEvidence[stages.Count];
            for (int i = 0; i < stages.Count; i++)
            {
                copy[i] = stages[i];
            }

            return copy;
        }

        private int CountIssues()
        {
            int count = 0;
            for (int i = 0; i < Stages.Count; i++)
            {
                count += Stages[i].IssueCount;
            }

            return count;
        }

        private int CountBlockingIssues()
        {
            int count = 0;
            for (int i = 0; i < Stages.Count; i++)
            {
                count += Stages[i].BlockingIssueCount;
            }

            return count;
        }

        private int CountSideEffects()
        {
            int count = 0;
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].SideEffectsApplied)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountFailedStages()
        {
            int count = 0;
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].Failed)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountSkippedStages()
        {
            int count = 0;
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].Skipped)
                {
                    count++;
                }
            }

            return count;
        }

        private string BuildStageNamesText()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < Stages.Count; i++)
            {
                FrameworkLifecycleOperationDiagnostics.AppendSeparated(builder, Stages[i].StageText);
            }

            return builder.Length > 0 ? builder.ToString() : "None";
        }

        private string BuildStageStatusesText()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < Stages.Count; i++)
            {
                FrameworkLifecycleOperationDiagnostics.AppendSeparated(builder, $"{Stages[i].StageText}:{Stages[i].StatusText}");
            }

            return builder.Length > 0 ? builder.ToString() : "None";
        }

        private static bool SequenceEquals(
            IReadOnlyList<FrameworkLifecycleOperationStageEvidence> left,
            IReadOnlyList<FrameworkLifecycleOperationStageEvidence> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
