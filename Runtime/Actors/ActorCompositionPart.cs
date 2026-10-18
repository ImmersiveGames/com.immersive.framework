using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    [Serializable]
    internal sealed class ActorCompositionPart
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

        internal ActorCompositionPart(
            string key,
            GameObject prefab,
            IEnumerable<string> dependencies = null)
        {
            this.key = key ?? string.Empty;
            this.prefab = prefab;
            this.dependencies = dependencies != null
                ? new List<string>(dependencies)
                : new List<string>();
        }

        private ActorCompositionPart()
        {
        }
    }
}
