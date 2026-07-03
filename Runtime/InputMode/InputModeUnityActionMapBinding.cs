
using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Passive binding between a logical InputMode and a project-owned Unity action map name.
    /// It is not action-map switching and does not mutate PlayerInput.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32B InputMode to Unity action map binding.")]
    public readonly struct InputModeUnityActionMapBinding : IEquatable<InputModeUnityActionMapBinding>
    {
        public InputModeUnityActionMapBinding(InputModeKind inputMode, UnityInputActionMapName actionMapName, bool actionMapRequired)
        {
            InputMode = inputMode;
            ActionMapName = actionMapName;
            ActionMapRequired = actionMapRequired;
        }

        public InputModeKind InputMode { get; }

        public UnityInputActionMapName ActionMapName { get; }

        public bool ActionMapRequired { get; }

        public bool IsValid => Enum.IsDefined(typeof(InputModeKind), InputMode)
            && InputMode != InputModeKind.Unknown
            && (!ActionMapRequired || ActionMapName.IsValid);

        public bool Equals(InputModeUnityActionMapBinding other)
        {
            return InputMode == other.InputMode
                && ActionMapName == other.ActionMapName
                && ActionMapRequired == other.ActionMapRequired;
        }

        public override bool Equals(object obj)
        {
            return obj is InputModeUnityActionMapBinding other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)InputMode;
                hash = (hash * 397) ^ ActionMapName.GetHashCode();
                hash = (hash * 397) ^ ActionMapRequired.GetHashCode();
                return hash;
            }
        }
    }
}
