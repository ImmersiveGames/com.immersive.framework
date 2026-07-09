using System.Collections.Generic;
using System.Text;

namespace Immersive.Framework.Editor.Camera.Cinemachine
{
    /// <summary>
    /// Diagnostic report for idempotent Cinemachine rig materialization.
    /// The report separates created, repaired, already valid, skipped and blocked outcomes.
    /// </summary>
    public sealed class CinemachineRigMaterializationReport
    {
        private readonly List<string> created = new List<string>();
        private readonly List<string> repaired = new List<string>();
        private readonly List<string> alreadyValid = new List<string>();
        private readonly List<string> skipped = new List<string>();
        private readonly List<string> blocked = new List<string>();

        public CinemachineRigMaterializationEvidence Evidence { get; } = new CinemachineRigMaterializationEvidence();

        public int CreatedCount => created.Count;

        public int RepairedCount => repaired.Count;

        public int AlreadyValidCount => alreadyValid.Count;

        public int SkippedCount => skipped.Count;

        public int BlockedCount => blocked.Count;

        public bool Succeeded => BlockedCount == 0;

        public string FirstBlockingIssue => blocked.Count > 0 ? blocked[0] : string.Empty;

        public IReadOnlyList<string> Created => created;

        public IReadOnlyList<string> Repaired => repaired;

        public IReadOnlyList<string> AlreadyValid => alreadyValid;

        public IReadOnlyList<string> Skipped => skipped;

        public IReadOnlyList<string> Blocked => blocked;

        public void MarkCreated(string message)
        {
            Add(created, message);
        }

        public void MarkRepaired(string message)
        {
            Add(repaired, message);
        }

        public void MarkAlreadyValid(string message)
        {
            Add(alreadyValid, message);
        }

        public void MarkSkipped(string message)
        {
            Add(skipped, message);
        }

        public void MarkBlocked(string message)
        {
            Add(blocked, message);
        }

        public string CreateSummary()
        {
            var builder = new StringBuilder();
            builder.Append("created='").Append(CreatedCount).Append("' ");
            builder.Append("repaired='").Append(RepairedCount).Append("' ");
            builder.Append("alreadyValid='").Append(AlreadyValidCount).Append("' ");
            builder.Append("skipped='").Append(SkippedCount).Append("' ");
            builder.Append("blocked='").Append(BlockedCount).Append("'");

            if (blocked.Count > 0)
            {
                builder.Append(" firstBlockingIssue='").Append(FirstBlockingIssue).Append("'");
            }

            return builder.ToString();
        }

        private static void Add(List<string> target, string message)
        {
            target.Add(string.IsNullOrWhiteSpace(message) ? "unspecified" : message.Trim());
        }
    }
}
