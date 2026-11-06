using System.Collections.Generic;
using Immersive.Composition;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.ObjectEntry
{
    internal static class ObjectEntryCompositionPartValidation
    {
        internal static bool TryValidate(
            string keyText,
            GameObject prefab,
            IEnumerable<string> dependencyTexts,
            bool requirePrefabAsset,
            out ContentKey key,
            out ContentKey[] dependencies,
            out string issue)
        {
            key = default;
            dependencies = null;
            issue = string.Empty;
            if (!ContentKey.TryCreate(keyText, out key))
            {
                issue = $"Object Entry composition part has invalid key '{keyText}'.";
                return false;
            }

            if (prefab == null)
            {
                issue = $"Object Entry composition part '{key.Value}' requires a prefab.";
                return false;
            }

            if (requirePrefabAsset && prefab.scene.IsValid())
            {
                issue = $"Object Entry composition part '{key.Value}' requires a prefab asset, not a scene object.";
                return false;
            }

            if (prefab.GetComponentInChildren<ObjectEntryDeclaration>(true) != null ||
                prefab.GetComponentInChildren<ObjectEntryCompositionAuthoring>(true) != null ||
                prefab.GetComponentInChildren<ActorDeclaration>(true) != null ||
                prefab.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null ||
                prefab.GetComponentInChildren<PlayerInput>(true) != null)
            {
                issue = $"Object Entry composition prefab for '{key.Value}' contains a forbidden identity, composition authoring, Actor host or PlayerInput component.";
                return false;
            }

            var validatedDependencies = new List<ContentKey>();
            var seenDependencies = new HashSet<ContentKey>();
            if (dependencyTexts != null)
            {
                foreach (string dependencyText in dependencyTexts)
                {
                    if (!ContentKey.TryCreate(dependencyText, out ContentKey dependency))
                    {
                        issue = $"Object Entry composition part '{key.Value}' has invalid dependency '{dependencyText}'.";
                        return false;
                    }

                    if (dependency == key)
                    {
                        issue = $"Object Entry composition part '{key.Value}' cannot depend on itself.";
                        return false;
                    }

                    if (!seenDependencies.Add(dependency))
                    {
                        issue = $"Object Entry composition part '{key.Value}' has duplicate dependency '{dependency.Value}'.";
                        return false;
                    }

                    validatedDependencies.Add(dependency);
                }
            }

            dependencies = validatedDependencies.ToArray();
            return true;
        }
    }
}
