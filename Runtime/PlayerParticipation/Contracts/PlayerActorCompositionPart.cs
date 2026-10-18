using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>A prefab-backed keyed part requested from an Actor composition contribution.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-050 D05 prefab-backed keyed Actor composition part.")]
    public sealed class PlayerActorCompositionPart
    {
        private readonly string[] _dependencies;
        private readonly ReadOnlyCollection<string> _dependencyView;

        public PlayerActorCompositionPart(
            string key,
            GameObject prefab,
            IEnumerable<string> dependencies = null)
        {
            Key = key ?? string.Empty;
            Prefab = prefab;
            _dependencies = dependencies == null
                ? Array.Empty<string>()
                : dependencies.ToArray();
            _dependencyView = Array.AsReadOnly(_dependencies);
        }

        public string Key { get; }
        public GameObject Prefab { get; }
        public IReadOnlyList<string> Dependencies => _dependencyView;

        internal bool IsValid => !string.IsNullOrWhiteSpace(Key) && Prefab != null &&
            _dependencies.All(dependency => !string.IsNullOrWhiteSpace(dependency));
    }
}
