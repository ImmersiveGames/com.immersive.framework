namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Describes whether a target role participates in validation/materialization.
    /// </summary>
    public enum CameraTargetRequirement
    {
        NotUsed = 0,
        Optional = 10,
        Required = 20
    }
}
