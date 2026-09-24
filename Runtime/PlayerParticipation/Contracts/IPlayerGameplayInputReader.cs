using Immersive.Framework.ApiStatus;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Actor-local read surface for the current Activity gameplay input occurrence.
    /// Authored InputActionReference values are treated only as stable action identity;
    /// reads are resolved against the exact live PlayerInput.actions instance owned by
    /// the current Player gameplay input binding.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-GAMEPLAY-INPUT-CONSUMER-01 generic Actor-local gameplay input read surface.")]
    public interface IPlayerGameplayInputReader
    {
        bool HasCurrentGameplayBinding { get; }
        bool GameplayReady { get; }
        int BindingRevision { get; }
        PlayerGameplayInputBindingToken CurrentBindingToken { get; }
        string Diagnostic { get; }

        bool TryReadValue<TValue>(
            InputActionReference authoredAction,
            out TValue value)
            where TValue : struct;

        bool TryIsPressed(
            InputActionReference authoredAction,
            out bool isPressed);

        bool TryWasPressedThisFrame(
            InputActionReference authoredAction,
            out bool wasPressed);

        bool TryWasReleasedThisFrame(
            InputActionReference authoredAction,
            out bool wasReleased);
    }
}
