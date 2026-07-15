using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kind for Unity Input target declaration validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F29A Unity Input target declaration issue vocabulary.")]
    public enum UnityInputTargetSetIssueKind
    {
        None = 0,
        InvalidDeclaration = 10,
        InvalidTargetRole = 20,
        InvalidTargetId = 30,
        MissingRequiredRole = 40,
        DuplicateRequiredRole = 50,
        DuplicateTargetId = 60,
        MissingRequiredPlayerInputEvidence = 70
    }
}
