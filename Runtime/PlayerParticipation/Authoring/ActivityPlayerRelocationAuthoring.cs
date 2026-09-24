using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Explicit ActivityId + PlayerSlotId to world-anchor mapping. The component may
    /// live in the Route primary scene, Route content, or current Activity content.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Activity Player Relocation")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-021 Model B Activity-owned explicit Player relocation authoring surface.")]
    public sealed class ActivityPlayerRelocationAuthoring : MonoBehaviour
    {
        [Serializable]
        public sealed class Binding
        {
            [SerializeField]
            [Tooltip("Exact Activity identity. Bindings for other Activities are ignored, not treated as duplicates.")]
            private ActivityAsset activity;

            [SerializeField]
            [Tooltip("Exact configured Player Slot. No fallback Slot is allowed.")]
            private PlayerSlotProfile playerSlotProfile;

            [SerializeField]
            [Tooltip("Exact world-pose anchor used for Activity contextual relocation position and rotation only.")]
            private Transform relocationAnchor;

            public ActivityAsset Activity => activity;
            public PlayerSlotProfile PlayerSlotProfile => playerSlotProfile;
            public Transform RelocationAnchor => relocationAnchor;

            public bool TryGetActivityId(out ActivityId activityId, out string issue)
            {
                activityId = default;
                if (activity == null || !activity.HasValidActivityId)
                {
                    issue = "Activity Player relocation binding requires an Activity with a valid stable Activity ID.";
                    return false;
                }

                activityId = activity.ActivityId;
                issue = string.Empty;
                return true;
            }

            public bool TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out string issue)
            {
                playerSlotId = default;
                if (playerSlotProfile == null)
                {
                    issue = "Activity Player relocation binding requires an explicit Player Slot Profile.";
                    return false;
                }

                return playerSlotProfile.TryGetPlayerSlotId(out playerSlotId, out issue);
            }
        }

        [SerializeField]
        private List<Binding> bindings = new List<Binding>();

        public IReadOnlyList<Binding> Bindings =>
            bindings ?? (IReadOnlyList<Binding>)Array.Empty<Binding>();

        public bool TryValidateBindings(out string issue)
        {
            issue = string.Empty;
            var identities = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<Binding> currentBindings = Bindings;
            for (int index = 0; index < currentBindings.Count; index++)
            {
                Binding binding = currentBindings[index];
                if (binding == null ||
                    !binding.TryGetActivityId(out ActivityId activityId, out issue) ||
                    !binding.TryGetPlayerSlotId(out PlayerSlotId slotId, out issue) ||
                    binding.RelocationAnchor == null)
                {
                    issue = string.IsNullOrEmpty(issue)
                        ? $"Activity Player relocation binding[{index}] is incomplete."
                        : $"Activity Player relocation binding[{index}] is invalid. {issue}";
                    return false;
                }

                string identity = activityId.StableText + "|" + slotId.StableText;
                if (!identities.Add(identity))
                {
                    issue = $"Activity Player relocation has duplicate Activity '{activityId.StableText}' + Slot '{slotId.StableText}' bindings on '{name}'.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }
    }
}
