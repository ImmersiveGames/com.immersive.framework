using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// Internal IF-ADR-035 typed boundary collection for one <see cref="Resettable"/>.
    /// Traverses the Resettable hierarchy depth-first in child order, stops at nested Resettables,
    /// and reports legacy <see cref="UnityResetSubjectAdapter"/> components that would share the boundary.
    /// Hierarchy is a composition boundary only: no names, paths or global search are used.
    /// </summary>
    internal static class ResettableBoundary
    {
        internal static void Collect(
            Resettable resettable,
            List<MonoBehaviour> capabilities,
            List<UnityResetSubjectAdapter> legacyAdapters)
        {
            if (resettable == null || capabilities == null || legacyAdapters == null)
            {
                return;
            }

            Visit(resettable.transform, true, capabilities, legacyAdapters);
        }

        /// <summary>
        /// Returns the nearest ancestor legacy adapter whose Children discovery would also collect
        /// capabilities inside this Resettable boundary, or null.
        /// </summary>
        internal static UnityResetSubjectAdapter FindCoveringLegacyAncestor(Resettable resettable)
        {
            if (resettable == null)
            {
                return null;
            }

            Transform current = resettable.transform.parent;
            while (current != null)
            {
                UnityResetSubjectAdapter adapter = current.GetComponent<UnityResetSubjectAdapter>();
                if (adapter != null &&
                    adapter.ParticipantDiscovery == UnityResetParticipantDiscoveryMode.Children)
                {
                    return adapter;
                }

                current = current.parent;
            }

            return null;
        }

        internal static bool IsCapability(MonoBehaviour component)
        {
            return component is UnityResetParticipantBehaviour || component is IUnityResettable;
        }

        private static void Visit(
            Transform node,
            bool isBoundaryRoot,
            List<MonoBehaviour> capabilities,
            List<UnityResetSubjectAdapter> legacyAdapters)
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
                if (component == null)
                {
                    continue;
                }

                if (component is UnityResetSubjectAdapter adapter)
                {
                    legacyAdapters.Add(adapter);
                    continue;
                }

                if (IsCapability(component))
                {
                    capabilities.Add(component);
                }
            }

            for (int childIndex = 0; childIndex < node.childCount; childIndex++)
            {
                Visit(node.GetChild(childIndex), false, capabilities, legacyAdapters);
            }
        }
    }
}
