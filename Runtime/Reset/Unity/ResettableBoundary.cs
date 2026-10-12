using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// Internal IF-ADR-035 typed boundary collection for one <see cref="Resettable"/>.
    /// Traverses descendants deterministically and stops at nested Resettable boundaries.
    /// Hierarchy is a composition boundary only: no names, paths or global search are used.
    /// </summary>
    internal static class ResettableBoundary
    {
        internal static void Collect(Resettable resettable, List<MonoBehaviour> capabilities)
        {
            if (resettable == null || capabilities == null)
            {
                return;
            }

            Visit(resettable.transform, true, capabilities);
        }

        private static void Visit(Transform node, bool isBoundaryRoot, List<MonoBehaviour> capabilities)
        {
            if (node == null)
            {
                return;
            }

            if (!isBoundaryRoot && node.GetComponent<Resettable>() != null)
            {
                return;
            }

            MonoBehaviour[] components = node.GetComponents<MonoBehaviour>();
            for (int index = 0; index < components.Length; index++)
            {
                MonoBehaviour component = components[index];
                if (component != null && IsCapability(component))
                {
                    capabilities.Add(component);
                }
            }

            for (int childIndex = 0; childIndex < node.childCount; childIndex++)
            {
                Visit(node.GetChild(childIndex), false, capabilities);
            }
        }

        private static bool IsCapability(MonoBehaviour component)
        {
            return component is UnityResetParticipantBehaviour || component is IUnityResettable;
        }
    }
}
