namespace Immersive.Framework.Editor.CameraAuthoring
{
    public readonly struct CameraRigComposerApplyRebuildResult
    {
        private CameraRigComposerApplyRebuildResult(bool succeeded, string status, string blockingIssue, string targetResolutionSummary, string materializationSummary, int createdCount, int repairedCount, int alreadyValidCount, int skippedCount, int blockedCount)
        {
            Succeeded = succeeded;
            Status = status;
            BlockingIssue = blockingIssue;
            TargetResolutionSummary = targetResolutionSummary;
            MaterializationSummary = materializationSummary;
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
        public static CameraRigComposerApplyRebuildResult ValidationSucceeded(string summary) => new(true, "ValidationSucceeded", string.Empty, summary, string.Empty, 0, 0, 0, 0, 0);
        public static CameraRigComposerApplyRebuildResult Failed(string status, string issue, string summary = "") => new(false, status, issue, summary, string.Empty, 0, 0, 0, 0, 1);
        public static CameraRigComposerApplyRebuildResult Applied(bool succeeded, string status, string issue, string targetSummary, string materializationSummary, int created, int repaired, int alreadyValid, int skipped, int blocked) => new(succeeded, status, issue, targetSummary, materializationSummary, created, repaired, alreadyValid, skipped, blocked);
    }
}
