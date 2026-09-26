using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Designer-facing Activity declaration that product Pause binding is required for the
    /// officially admitted Local Player. This component is passive: it does not resolve a Player,
    /// bind input, create Pause runtime, or mutate Pause state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Pause/Pause Activity Authoring")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Stable,
        "Stable Activity authoring contract for required product Pause binding.")]
    public sealed class ActivityPauseAuthoring : MonoBehaviour
    {
        [Header("Pause")]
        [SerializeField]
        [Tooltip("This Activity requires product Pause binding for its officially admitted Local Player. The Activity lifecycle materializes and releases the binding.")]
        private PauseActivityBindingRequiredness requiredness =
            PauseActivityBindingRequiredness.Required;

        public PauseActivityBindingRequiredness Requiredness => requiredness;

        public bool TryCreateIntent(
            out PauseActivityBindingIntent intent,
            out string diagnostic)
        {
            intent = default;
            if (!Enum.IsDefined(typeof(PauseActivityBindingRequiredness), requiredness) ||
                requiredness == PauseActivityBindingRequiredness.Unknown)
            {
                diagnostic =
                    $"invalid-authoring: Pause Activity Binding requires explicit Requiredness. Current value is '{requiredness}'.";
                return false;
            }

            try
            {
                intent = new PauseActivityBindingIntent(
                    requiredness,
                    nameof(ActivityPauseAuthoring));
                diagnostic =
                    "intent-created: Pause Activity Binding authoring created a required product Pause intent.";
                return true;
            }
            catch (ArgumentException exception)
            {
                diagnostic = $"invalid-authoring: {exception.Message.NormalizeText()}";
                return false;
            }
        }
    }
}