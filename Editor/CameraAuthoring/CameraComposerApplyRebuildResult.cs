namespace Immersive.Framework.Editor.CameraAuthoring
{
    /// <summary>
    /// Editor-only result returned by CameraComposer validation and Apply/Rebuild tooling.
    /// </summary>
    public readonly struct CameraComposerApplyRebuildResult
    {
        private CameraComposerApplyRebuildResult(
            bool succeeded,
            string status,
            string blockingIssue,
            string targetResolutionSummary,
            string materializationSummary,
            int createdCount,
            int repairedCount,
            int alreadyValidCount,
            int skippedCount,
            int blockedCount)
        {
            Succeeded = succeeded;
            Status = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
            BlockingIssue = string.IsNullOrWhiteSpace(blockingIssue) ? string.Empty : blockingIssue.Trim();
            TargetResolutionSummary = string.IsNullOrWhiteSpace(targetResolutionSummary) ? string.Empty : targetResolutionSummary.Trim();
            MaterializationSummary = string.IsNullOrWhiteSpace(materializationSummary) ? string.Empty : materializationSummary.Trim();
            CreatedCount = createdCount;
            RepairedCount = repairedCount;
            AlreadyValidCount = alreadyValidCount;
            SkippedCount = skippedCount;
            BlockedCount = blockedCount;
        }

        public bool Succeeded { get; }

        public string Status { get; }

        public string BlockingIssue { get; }

        public string TargetResolutionSummary { get; }

        public string MaterializationSummary { get; }

        public int CreatedCount { get; }

        public int RepairedCount { get; }

        public int AlreadyValidCount { get; }

        public int SkippedCount { get; }

        public int BlockedCount { get; }

        public static CameraComposerApplyRebuildResult ValidationSucceeded(string targetResolutionSummary)
        {
            return new CameraComposerApplyRebuildResult(
                true,
                "ValidationSucceeded",
                string.Empty,
                targetResolutionSummary,
                string.Empty,
                0,
                0,
                0,
                0,
                0);
        }

        public static CameraComposerApplyRebuildResult Failed(string status, string blockingIssue, string targetResolutionSummary = "")
        {
            return new CameraComposerApplyRebuildResult(
                false,
                status,
                blockingIssue,
                targetResolutionSummary,
                string.Empty,
                0,
                0,
                0,
                0,
                1);
        }

        public static CameraComposerApplyRebuildResult Applied(
            bool succeeded,
            string status,
            string blockingIssue,
            string targetResolutionSummary,
            string materializationSummary,
            int createdCount,
            int repairedCount,
            int alreadyValidCount,
            int skippedCount,
            int blockedCount)
        {
            return new CameraComposerApplyRebuildResult(
                succeeded,
                status,
                blockingIssue,
                targetResolutionSummary,
                materializationSummary,
                createdCount,
                repairedCount,
                alreadyValidCount,
                skippedCount,
                blockedCount);
        }
    }
}
