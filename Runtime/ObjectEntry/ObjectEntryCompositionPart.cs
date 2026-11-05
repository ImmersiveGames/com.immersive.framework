using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>One authored prefab-backed part; runtime ownership is not stored in the asset.</summary>
    [Serializable]
    internal sealed class ObjectEntryCompositionPart
    {
        [SerializeField] private string key = string.Empty;
        [SerializeField] private GameObject prefab;
        [SerializeField] private List<string> dependencies = new List<string>();

        internal string Key => key ?? string.Empty;
        internal GameObject Prefab => prefab;
        internal bool HasDependencies => dependencies != null;
        internal IReadOnlyList<string> Dependencies => dependencies ?? (IReadOnlyList<string>)Array.Empty<string>();
    }
}
