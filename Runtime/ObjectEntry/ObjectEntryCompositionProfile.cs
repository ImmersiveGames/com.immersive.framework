using System;
using System.Collections.Generic;
using Immersive.Composition;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// Optional reusable authoring data for physical Object Entry composition.
    /// The asset never owns a physical occurrence or starts composition on its own.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ObjectEntryCompositionProfile",
        menuName = "Immersive Framework/Object Entry/Composition Profile")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendment 002 optional Object Entry composition profile authoring.")]
    public sealed class ObjectEntryCompositionProfile : ScriptableObject
    {
        [SerializeField] private List<ObjectEntryCompositionPart> parts = new List<ObjectEntryCompositionPart>();

        internal IReadOnlyList<ObjectEntryCompositionPart> Parts =>
            parts ?? (IReadOnlyList<ObjectEntryCompositionPart>)Array.Empty<ObjectEntryCompositionPart>();

        public bool TryValidate(out string issue)
        {
            issue = string.Empty;
            if (parts == null || parts.Count == 0)
            {
                issue = "Object Entry composition profile requires at least one explicitly configured part.";
                return false;
            }

            var byKey = new Dictionary<ContentKey, ObjectEntryCompositionPart>();
            for (int index = 0; index < parts.Count; index++)
            {
                ObjectEntryCompositionPart part = parts[index];
                if (part == null)
                {
                    issue = $"Object Entry composition part '{index}' is missing.";
                    return false;
                }

                if (!ContentKey.TryCreate(part.Key, out ContentKey key))
                {
                    issue = $"Object Entry composition part '{index}' has invalid key '{part.Key}'.";
                    return false;
                }

                if (byKey.ContainsKey(key))
                {
                    issue = $"Object Entry composition has duplicate part key '{key.Value}'.";
                    return false;
                }

                if (part.Prefab == null)
                {
                    issue = $"Object Entry composition part '{key.Value}' requires a prefab.";
                    return false;
                }

                // A composed child must not smuggle in a new unadmitted identity or Actor/Player authority.
                if (part.Prefab.GetComponentInChildren<ObjectEntryDeclaration>(true) != null ||
                    part.Prefab.GetComponentInChildren<ObjectEntryCompositionAuthoring>(true) != null ||
                    part.Prefab.GetComponentInChildren<ActorDeclaration>(true) != null ||
                    part.Prefab.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null ||
                    part.Prefab.GetComponentInChildren<PlayerInput>(true) != null)
                {
                    issue = $"Object Entry composition prefab for '{key.Value}' contains a forbidden identity, composition authoring, Actor host or PlayerInput component.";
                    return false;
                }

                if (!part.HasDependencies)
                {
                    issue = $"Object Entry composition part '{key.Value}' has no serialized dependencies list.";
                    return false;
                }

                var seenDependencies = new HashSet<ContentKey>();
                foreach (string dependencyText in part.Dependencies)
                {
                    if (!ContentKey.TryCreate(dependencyText, out ContentKey dependency))
                    {
                        issue = $"Object Entry composition part '{key.Value}' has invalid dependency '{dependencyText}'.";
                        return false;
                    }

                    if (dependency == key || !seenDependencies.Add(dependency))
                    {
                        issue = $"Object Entry composition part '{key.Value}' has a self or duplicate dependency '{dependency.Value}'.";
                        return false;
                    }
                }

                byKey.Add(key, part);
            }

            foreach (KeyValuePair<ContentKey, ObjectEntryCompositionPart> pair in byKey)
            {
                foreach (string dependencyText in pair.Value.Dependencies)
                {
                    ContentKey dependency = ContentKey.Create(dependencyText);
                    if (!byKey.ContainsKey(dependency))
                    {
                        issue = $"Object Entry composition part '{pair.Key.Value}' requires missing dependency '{dependency.Value}'.";
                        return false;
                    }
                }
            }

            var visiting = new HashSet<ContentKey>();
            var visited = new HashSet<ContentKey>();
            foreach (ContentKey key in byKey.Keys)
            {
                if (!HasCycle(key, byKey, visiting, visited, out ContentKey offendingKey)) continue;
                issue = $"Object Entry composition dependencies contain a cycle at key '{offendingKey.Value}'.";
                return false;
            }

            return true;
        }

        private static bool HasCycle(
            ContentKey key,
            IReadOnlyDictionary<ContentKey, ObjectEntryCompositionPart> byKey,
            ISet<ContentKey> visiting,
            ISet<ContentKey> visited,
            out ContentKey offendingKey)
        {
            if (visited.Contains(key))
            {
                offendingKey = default;
                return false;
            }

            if (!visiting.Add(key))
            {
                offendingKey = key;
                return true;
            }

            foreach (string dependencyText in byKey[key].Dependencies)
            {
                if (HasCycle(ContentKey.Create(dependencyText), byKey, visiting, visited, out offendingKey))
                    return true;
            }

            visiting.Remove(key);
            visited.Add(key);
            offendingKey = default;
            return false;
        }
    }
}
