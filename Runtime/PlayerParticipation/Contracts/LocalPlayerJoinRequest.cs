
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Product-authorized intent to provision one local Player through Unity PlayerInputManager.
    /// Slot allocation, operation identity, playerIndex and Actor selection are not caller inputs.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3G.2 manual local Player join request.")]
    public readonly struct LocalPlayerJoinRequest
    {
        public LocalPlayerJoinRequest(
            string source,
            string reason,
            InputDevice pairWithDevice = null,
            string controlScheme = null)
        {
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            PairWithDevice = pairWithDevice;
            ControlScheme = controlScheme.NormalizeText();
        }

        public string Source { get; }

        public string Reason { get; }

        public InputDevice PairWithDevice { get; }

        public string ControlScheme { get; }

        public bool HasDeviceHint => PairWithDevice != null;

        public bool HasControlSchemeHint => !string.IsNullOrEmpty(ControlScheme);

        public bool IsValid => TryValidate(out _);

        public bool TryValidate(out string issue)
        {
            if (string.IsNullOrEmpty(Source))
            {
                issue = "Local Player join request requires a non-empty source.";
                return false;
            }

            if (string.IsNullOrEmpty(Reason))
            {
                issue = "Local Player join request requires a non-empty reason.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        public string ToDiagnosticString()
        {
            return $"source='{Source}' reason='{Reason}' deviceHint='{HasDeviceHint}' " +
                $"controlScheme='{ControlScheme}'";
        }
    }
}
