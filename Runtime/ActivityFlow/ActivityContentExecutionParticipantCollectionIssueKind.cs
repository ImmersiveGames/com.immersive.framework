using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kind for passive Activity Content Execution participant collection construction.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10E Activity Content Execution participant collection issue kind; no discovery runtime or Unity side effects.")]
    public enum ActivityContentExecutionParticipantCollectionIssueKind
    {
        Unknown = 0,
        NullParticipant = 10,
        InvalidDescriptor = 20,
        DuplicateContentId = 30
    }
}
