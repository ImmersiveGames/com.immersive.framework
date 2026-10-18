using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Reusable authored definition for one Base content part on an Actor Profile.
    /// Runtime instances and ownership remain with the Actor composition binding.
    /// </summary>
    internal sealed class ActorCompositionPartAsset : ScriptableObject
    {
        [SerializeField] private string key = "visual";
        [SerializeField] private GameObject prefab;
        [SerializeField] private List<string> dependencies = new List<string>();

        internal string Key => key ?? string.Empty;

        internal GameObject Prefab => prefab;

        internal bool HasDependenciesList => dependencies != null;

        internal IReadOnlyList<string> Dependencies
        {
            get
            {
                if (dependencies == null)
                {
                    return Array.Empty<string>();
                }

                return dependencies;
            }
        }

        internal void Initialize(
            string partKey,
            GameObject partPrefab,
            IEnumerable<string> dependencyKeys = null)
        {
            key = partKey ?? string.Empty;
            prefab = partPrefab;
            dependencies = dependencyKeys != null
                ? new List<string>(dependencyKeys)
                : new List<string>();
        }
    }
}
