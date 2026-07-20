using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost : IContentAnchorMaterializationRuntimePort
    {
        RuntimeContentRuntime IContentAnchorMaterializationRuntimePort.ContentRuntime =>
            RuntimeContentRuntime;

        ContentAnchorBindingResult IContentAnchorMaterializationRuntimePort.BindContentAnchor(
            ContentAnchorSet anchorSet,
            ContentAnchorBindingRequest request,
            string source,
            string reason) =>
            BindContentAnchor(anchorSet, request, source, reason);

        bool IContentAnchorMaterializationRuntimePort.UnbindContentAnchor(
            ContentAnchorContentHandle handle) =>
            UnbindContentAnchor(handle);

        ContentAnchorBindingLifecycleResult
            IContentAnchorMaterializationRuntimePort.UnbindContentAnchorRuntimeOwner(
                RuntimeContentOwner owner,
                string source,
                string reason) =>
                UnbindContentAnchorRuntimeOwner(owner, source, reason);
    }
}
