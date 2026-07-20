using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    internal interface IContentAnchorMaterializationRuntimePort
    {
        RuntimeContentRuntime ContentRuntime { get; }

        ContentAnchorBindingResult BindContentAnchor(
            ContentAnchorSet anchorSet,
            ContentAnchorBindingRequest request,
            string source,
            string reason);

        bool UnbindContentAnchor(ContentAnchorContentHandle handle);

        ContentAnchorBindingLifecycleResult UnbindContentAnchorRuntimeOwner(
            RuntimeContentOwner owner,
            string source,
            string reason);
    }
}
