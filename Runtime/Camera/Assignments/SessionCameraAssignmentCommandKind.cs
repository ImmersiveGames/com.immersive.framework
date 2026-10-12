using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-039 command selection used by the Experimental Session Camera Assignment Trigger.")]
    public enum SessionCameraAssignmentCommandKind
    {
        Undefined = 0,
        Activate = 10,
        Replace = 20,
        Clear = 30
    }
}
