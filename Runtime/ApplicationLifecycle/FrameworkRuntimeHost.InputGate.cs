using Immersive.Framework.Gate;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost : IInputGateRuntimePort
    {
        GateSnapshot IInputGateRuntimePort.CurrentGateSnapshot => CurrentGateSnapshot;
    }
}
