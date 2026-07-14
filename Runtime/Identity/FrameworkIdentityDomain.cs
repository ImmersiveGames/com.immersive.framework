using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Identity
{
    /// <summary>
    /// API status: Experimental. Canonical coarse-grained domains for framework identity values.
    /// The zero value is invalid for new functional identities and exists only as the default enum value.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal typed identity domain primitive introduced by F1E.")]
    public enum FrameworkIdentityDomain
    {
        /// <summary>
        /// Invalid default value. Do not use for authored or runtime identity keys.
        /// </summary>
        Unspecified = 0,

        Application = 10,
        Session = 20,
        Route = 30,
        Activity = 40,
        Content = 50,
        Local = 60,
        ContentAnchor = 70,
        Runtime = 80,
        Diagnostics = 90,
        Qa = 100,
        CycleReset = 110,
        ObjectEntry = 120,
        ObjectReset = 130,
        Transition = 140,
        TransitionEffect = 150,
        Pause = 160,
        Snapshot = 170,
        Preferences = 180,
        ProgressionSave = 190,
        Loading = 200,
        UnityInput = 210,
        InputMode = 220,
        Actor = 230,
        PlayerSlot = 240,
        ActorProfile = 250
    }
}
