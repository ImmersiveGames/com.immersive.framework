using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Route-local Slot-to-anchor composition. It is discovered only in the current
    /// Route primary scene and Route-owned content scenes.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Route Player Spatial Entry")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-021 Model B Route-owned Player spatial entry authoring surface.")]
    public sealed class RoutePlayerSpatialEntryAuthoring : MonoBehaviour
    {
        [Serializable]
        public sealed class Binding
        {
            [SerializeField]
            [Tooltip("Exact configured Player Slot. No fallback Slot is allowed.")]
            private PlayerSlotProfile playerSlotProfile;

            [SerializeField]
            [Tooltip("Exact world-pose anchor used for Route spatial entry position and rotation only.")]
            private Transform placementAnchor;

            public PlayerSlotProfile PlayerSlotProfile => playerSlotProfile;
            public Transform PlacementAnchor => placementAnchor;

            public bool TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out string issue)
            {
                if (playerSlotProfile == null)
                {
                    playerSlotId = default;
                    issue = "Route spatial entry binding requires an explicit Player Slot Profile.";
                    return false;
                }

                return playerSlotProfile.TryGetPlayerSlotId(out playerSlotId, out issue);
            }
        }

        [SerializeField]
        private List<Binding> bindings = new List<Binding>();

        public IReadOnlyList<Binding> Bindings =>
            bindings ?? (IReadOnlyList<Binding>)Array.Empty<Binding>();
    }
}
