using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Activity-local explicit initial-placement composition. Each binding maps one exact
    /// Player Slot Profile to one exact Transform anchor. Runtime discovery is restricted by
    /// ActivityFlow to the canonical ActivityOwnedScenes scope.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Activity Player Initial Placement")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-021 Activity-local Player Slot to initial placement authoring surface.")]
    public sealed class ActivityPlayerInitialPlacementAuthoring : MonoBehaviour
    {
        [Serializable]
        public sealed class Binding
        {
            [SerializeField]
            [Tooltip("Exact configured Player Slot. No fallback Slot is allowed.")]
            private PlayerSlotProfile playerSlotProfile;

            [SerializeField]
            [Tooltip("Exact world-pose anchor used for initial position and rotation only.")]
            private Transform placementAnchor;

            public PlayerSlotProfile PlayerSlotProfile => playerSlotProfile;
            public Transform PlacementAnchor => placementAnchor;

            public bool TryGetPlayerSlotId(
                out PlayerSlotId playerSlotId,
                out string issue)
            {
                if (playerSlotProfile == null)
                {
                    playerSlotId = default;
                    issue = "Initial placement binding requires an explicit Player Slot Profile.";
                    return false;
                }

                return playerSlotProfile.TryGetPlayerSlotId(
                    out playerSlotId,
                    out issue);
            }
        }

        [SerializeField]
        private List<Binding> bindings = new List<Binding>();

        public IReadOnlyList<Binding> Bindings =>
            bindings ?? (IReadOnlyList<Binding>)Array.Empty<Binding>();
    }
}
