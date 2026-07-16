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
        private readonly List<string> _created = new List<string>();
        private readonly List<string> _repaired = new List<string>();
        private readonly List<string> _alreadyValid = new List<string>();
        private readonly List<string> _skipped = new List<string>();
        private readonly List<string> _blocked = new List<string>();

        public CinemachineRigMaterializationEvidence Evidence { get; } = new CinemachineRigMaterializationEvidence();

        public int CreatedCount => _created.Count;

        public int RepairedCount => _repaired.Count;

        public int AlreadyValidCount => _alreadyValid.Count;

        public int SkippedCount => _skipped.Count;

        public int BlockedCount => _blocked.Count;

        public bool Succeeded => BlockedCount == 0;

        public string FirstBlockingIssue => _blocked.Count > 0 ? _blocked[0] : string.Empty;

        public IReadOnlyList<string> Created => _created;

        public IReadOnlyList<string> Repaired => _repaired;

        public IReadOnlyList<string> AlreadyValid => _alreadyValid;

        public IReadOnlyList<string> Skipped => _skipped;

        public IReadOnlyList<string> Blocked => _blocked;

        public void MarkCreated(string message)
        {
            Add(_created, message);
        }

        public void MarkRepaired(string message)
        {
            Add(_repaired, message);
        }

        public void MarkAlreadyValid(string message)
        {
            Add(_alreadyValid, message);
        }

        public void MarkSkipped(string message)
        {
            Add(_skipped, message);
        }

        public void MarkBlocked(string message)
        {
            Add(_blocked, message);
        }

        public string CreateSummary()
        {
            var builder = new StringBuilder();
            builder.Append("created='").Append(CreatedCount).Append("' ");
            builder.Append("repaired='").Append(RepairedCount).Append("' ");
            builder.Append("alreadyValid='").Append(AlreadyValidCount).Append("' ");
            builder.Append("skipped='").Append(SkippedCount).Append("' ");
            builder.Append("blocked='").Append(BlockedCount).Append("'");

            if (_blocked.Count > 0)
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
