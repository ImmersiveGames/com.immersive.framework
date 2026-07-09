namespace Immersive.Framework.PlayerBinding
{
    public enum UnityPlayerInputActivationFailureKind
    {
        None = 0,
        MissingUnityPlayerInputBridgeTarget = 1,
        MissingUnityPlayerInputBridge = 2,
        InvalidUnityPlayerInputBridge = 3,
        MissingUnityPlayerInputActivationTarget = 4,
        MissingUnityPlayerInput = 5,
        MissingExpectedPlayerSlot = 6,
        PlayerSlotMismatch = 7,
        MissingActionMapName = 8,
        MissingActionAsset = 9,
        MissingActionMap = 10,
        ActionMapSwitchFailed = 11,
        TargetRejectedActivation = 12,
        MissingExistingActivation = 13,
        TargetPlayerSlotMismatch = 14,
        UnexpectedException = 15
    }
}
