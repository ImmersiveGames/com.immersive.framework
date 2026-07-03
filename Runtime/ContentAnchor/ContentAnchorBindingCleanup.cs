using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Mechanical ContentAnchor binding cleanup helper shared by Route and Activity lifecycles.
    /// It only decides whether to skip cleanup for a retained owner or invoke the existing binding runtime cleanup.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R helper for explicit ContentAnchor binding cleanup sequencing; no lifecycle ownership.")]
    internal static class ContentAnchorBindingCleanup
    {
        internal static ContentAnchorBindingLifecycleResult CleanupRuntimeOwner(
            RuntimeContentAnchorBinding bindingRuntime,
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            if (bindingRuntime == null)
            {
                throw new ArgumentNullException(nameof(bindingRuntime));
            }

            ValidateOwner(owner);
            return bindingRuntime.UnbindRuntimeOwner(owner, source, reason);
        }

        internal static ContentAnchorBindingLifecycleResult CleanupPreviousRuntimeOwner(
            RuntimeContentAnchorBinding bindingRuntime,
            RuntimeContentOwner previousOwner,
            RuntimeContentOwner nextOwner,
            string source,
            string reason)
        {
            if (bindingRuntime == null)
            {
                throw new ArgumentNullException(nameof(bindingRuntime));
            }

            if (!previousOwner.IsValid)
            {
                return default(ContentAnchorBindingLifecycleResult);
            }

            if (nextOwner.IsValid && previousOwner == nextOwner)
            {
                return default(ContentAnchorBindingLifecycleResult);
            }

            return bindingRuntime.UnbindRuntimeOwner(previousOwner, source, reason);
        }

        private static void ValidateOwner(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime content owner must be valid.", nameof(owner));
            }
        }
    }
}
