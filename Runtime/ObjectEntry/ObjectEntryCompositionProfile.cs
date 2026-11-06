using System;
using System.Collections.Generic;
using Immersive.Composition;
using Immersive.Framework.ApiStatus;
using UnityEngine;

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

                if (!ObjectEntryCompositionPartValidation.TryValidate(
                        part.Key,
                        part.Prefab,
                        part.Dependencies,
                        requirePrefabAsset: false,
                        out ContentKey key,
                        out _,
                        out issue))
                {
                    issue = $"Object Entry composition part '{index}' is invalid. {issue}";
                    return false;
                }

                if (byKey.ContainsKey(key))
                {
                    issue = $"Object Entry composition has duplicate part key '{key.Value}'.";
                    return false;
                }

                if (!part.HasDependencies)
                {
                    issue = $"Object Entry composition part '{key.Value}' has no serialized dependencies list.";
                    return false;
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
