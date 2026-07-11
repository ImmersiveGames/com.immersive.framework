using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Editor.Camera.Cinemachine
{
    /// <summary>
    /// Technical evidence produced by Cinemachine rig materialization.
    /// It is intended for inspector/debug output, QA and later Composer diagnostics.
    /// </summary>
    public sealed class CinemachineRigMaterializationEvidence
    {
        public UnityEngine.Camera UnityCamera { get; internal set; }

        public CinemachineBrain Brain { get; internal set; }

        public CinemachineCamera CinemachineCamera { get; internal set; }

        public Transform FollowTarget { get; internal set; }

        public Transform LookAtTarget { get; internal set; }

    }
}
