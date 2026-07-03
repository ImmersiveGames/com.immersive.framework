namespace Immersive.Framework.ContentAnchor
{
    public enum UnityContentAnchorMaterializationAuthoringValidationStatus
    {
        Unknown = 0,
        Succeeded = 10,
        FailedBridgeMissing = 100,
        FailedBridgeConfiguration = 110,
        FailedBridgeSetMissing = 200,
        FailedBridgeSetConfiguration = 210
    }
}
