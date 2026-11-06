using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>Immutable consumer description for one dynamically composed Object Entry part.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendment 003 C2 dynamic Object Entry part input.")]
    public sealed class ObjectEntryCompositionPartDefinition
    {
        private readonly ReadOnlyCollection<string> _dependencies;

        public ObjectEntryCompositionPartDefinition(
            string key,
            GameObject prefab,
            IEnumerable<string> dependencies = null)
        {
            Key = key ?? string.Empty;
            Prefab = prefab;
            _dependencies = Array.AsReadOnly((dependencies ?? Array.Empty<string>()).ToArray());
        }

        public string Key { get; }
        public GameObject Prefab { get; }
        public IReadOnlyList<string> Dependencies => _dependencies;
    }
}
