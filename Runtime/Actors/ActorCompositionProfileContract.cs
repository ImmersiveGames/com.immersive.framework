using System.Collections.Generic;
using Immersive.Composition;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Actors
{
    internal static class ActorCompositionProfileContract
    {
        internal static bool TryValidate(ActorProfile profile, out string issue)
        {
            issue = string.Empty;
            if (profile == null)
            {
                issue = "Actor Profile is missing.";
                return false;
            }

            if (!profile.HasCompositionPartsList)
            {
                issue = $"Actor Profile '{profile.name}' has no serialized Base composition list. Repair or reimport the asset before materialization.";
                return false;
            }

            IReadOnlyList<ActorCompositionPart> parts = profile.CompositionParts;
            var keys = new Dictionary<ContentKey, ActorCompositionPart>();
            for (int index = 0; index < parts.Count; index++)
            {
                ActorCompositionPart part = parts[index];
                if (part == null)
                {
                    issue = $"Actor Profile '{profile.name}' composition entry '{index}' is missing.";
                    return false;
                }

                if (!ContentKey.TryCreate(part.Key, out ContentKey key))
                {
                    issue = $"Actor Profile '{profile.name}' composition entry '{index}' has invalid key '{part.Key}'.";
                    return false;
                }

                if (keys.ContainsKey(key))
                {
                    issue = $"Actor Profile '{profile.name}' has duplicate composition key '{key.Value}'.";
                    return false;
                }

                if (part.Prefab == null)
                {
                    issue = $"Actor Profile '{profile.name}' composition key '{key.Value}' requires an explicit prefab.";
                    return false;
                }

                if (!part.HasDependenciesList)
                {
                    issue = $"Actor Profile composition key '{key.Value}' has no serialized dependency list.";
                    return false;
                }

                if (part.Prefab.GetComponentInChildren<ActorDeclaration>(true) != null ||
                    part.Prefab.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null ||
                    part.Prefab.GetComponentInChildren<PlayerInput>(true) != null)
                {
                    issue = $"Actor Profile composition prefab '{part.Prefab.name}' must not contain PlayerInput, Framework Actor declarations or Player Actor Runtime Host infrastructure.";
                    return false;
                }

                var dependencyKeys = new HashSet<ContentKey>();
                for (int dependencyIndex = 0; dependencyIndex < part.Dependencies.Count; dependencyIndex++)
                {
                    string dependencyText = part.Dependencies[dependencyIndex];
                    if (!ContentKey.TryCreate(dependencyText, out ContentKey dependency))
                    {
                        issue = $"Actor Profile composition key '{key.Value}' has invalid dependency key '{dependencyText}'.";
                        return false;
                    }

                    if (!dependencyKeys.Add(dependency))
                    {
                        issue = $"Actor Profile composition key '{key.Value}' repeats dependency '{dependency.Value}'.";
                        return false;
                    }

                    if (dependency == key)
                    {
                        issue = $"Actor Profile composition key '{key.Value}' cannot depend on itself.";
                        return false;
                    }
                }

                keys.Add(key, part);
            }

            foreach (KeyValuePair<ContentKey, ActorCompositionPart> pair in keys)
            {
                for (int index = 0; index < pair.Value.Dependencies.Count; index++)
                {
                    string dependencyText = pair.Value.Dependencies[index];
                    ContentKey dependency = ContentKey.Create(dependencyText);
                    if (!keys.ContainsKey(dependency))
                    {
                        issue = $"Actor Profile composition key '{pair.Key.Value}' requires missing key '{dependency.Value}'.";
                        return false;
                    }
                }
            }

            if (TryFindCycle(keys, out ContentKey cycleKey))
            {
                issue = $"Actor Profile composition dependencies contain a cycle at key '{cycleKey.Value}'.";
                return false;
            }

            return true;
        }

        private static bool TryFindCycle(
            IReadOnlyDictionary<ContentKey, ActorCompositionPart> parts,
            out ContentKey cycleKey)
        {
            var visiting = new HashSet<ContentKey>();
            var visited = new HashSet<ContentKey>();
            foreach (ContentKey key in parts.Keys)
            {
                if (Visit(key, parts, visiting, visited, out cycleKey))
                {
                    return true;
                }
            }

            cycleKey = default;
            return false;
        }

        private static bool Visit(
            ContentKey key,
            IReadOnlyDictionary<ContentKey, ActorCompositionPart> parts,
            ISet<ContentKey> visiting,
            ISet<ContentKey> visited,
            out ContentKey cycleKey)
        {
            if (visited.Contains(key))
            {
                cycleKey = default;
                return false;
            }

            if (!visiting.Add(key))
            {
                cycleKey = key;
                return true;
            }

            ActorCompositionPart part = parts[key];
            for (int index = 0; index < part.Dependencies.Count; index++)
            {
                ContentKey dependency = ContentKey.Create(part.Dependencies[index]);
                if (Visit(dependency, parts, visiting, visited, out cycleKey))
                {
                    return true;
                }
            }

            visiting.Remove(key);
            visited.Add(key);
            cycleKey = default;
            return false;
        }
    }
}
