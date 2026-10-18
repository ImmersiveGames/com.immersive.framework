using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>Prefab-backed or explicitly adopted keyed content for one published Actor occurrence.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-050 D07 owner-bound NonPlayer composition part.")]
    public sealed class ActorOccurrenceCompositionPart
    {
        private readonly string[] _dependencies;
        private readonly ReadOnlyCollection<string> _dependencyView;

        public ActorOccurrenceCompositionPart(
            string key,
            GameObject prefab,
            IEnumerable<string> dependencies = null)
        {
            Key = key ?? string.Empty;
            Prefab = prefab;
            _dependencies = dependencies == null ? Array.Empty<string>() : dependencies.ToArray();
            _dependencyView = Array.AsReadOnly(_dependencies);
        }

        public string Key { get; }
        public GameObject Prefab { get; }
        public GameObject AdoptedInstance { get; }
        public IReadOnlyList<string> Dependencies => _dependencyView;

        internal bool IsAdopted => AdoptedInstance != null;
        internal bool IsValid => !string.IsNullOrWhiteSpace(Key) &&
            ((Prefab != null && AdoptedInstance == null) || (Prefab == null && AdoptedInstance != null)) &&
            _dependencies.All(dependency => !string.IsNullOrWhiteSpace(dependency));

        private ActorOccurrenceCompositionPart(
            string key,
            GameObject adoptedInstance,
            IEnumerable<string> dependencies,
            bool adoptExisting)
            : this(key, null, dependencies)
        {
            if (!adoptExisting)
                throw new ArgumentException("Adopted Actor composition parts must explicitly select an existing instance.", nameof(adoptExisting));
            AdoptedInstance = adoptedInstance;
        }

        public static ActorOccurrenceCompositionPart AdoptExisting(
            string key,
            GameObject instance,
            IEnumerable<string> dependencies = null)
        {
            return new ActorOccurrenceCompositionPart(key, instance, dependencies, true);
        }
    }
}
