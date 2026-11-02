using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Authoring;
using Immersive.Framework.ObjectEntry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class ObjectEntryAuthoringIndex
    {
        private static ObjectEntryAuthoringIndexSnapshot _snapshot;

        static ObjectEntryAuthoringIndex()
        {
            EditorApplication.projectChanged += Invalidate;
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        internal static ObjectEntryAuthoringIndexSnapshot GetSnapshot()
        {
            return _snapshot ??= BuildSnapshot();
        }

        internal static ObjectEntryAuthoringIndexSnapshot Refresh()
        {
            _snapshot = BuildSnapshot();
            return _snapshot;
        }

        private static void OnSceneSaved(Scene scene) => Invalidate();

        private static void Invalidate() => _snapshot = null;

        private static ObjectEntryAuthoringIndexSnapshot BuildSnapshot()
        {
            var candidates = new Dictionary<ObjectEntryId, ObjectEntryAuthoringCandidate>();
            var issues = new List<string>();
            var configuredScenes = new Dictionary<string, ObjectEntrySceneSources>(StringComparer.Ordinal);

            CollectRouteScenes(configuredScenes, issues);
            CollectActivityScenes(configuredScenes, issues);

            foreach (KeyValuePair<string, ObjectEntrySceneSources> pair in configuredScenes)
            {
                ScanConfiguredScene(pair.Key, pair.Value, candidates, issues);
            }

            ScanPrefabTemplates(candidates, issues);

            List<ObjectEntryAuthoringCandidate> ordered = candidates.Values
                .OrderBy(candidate => candidate.DisplayLabel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.Id.StableText, StringComparer.Ordinal)
                .ToList();

            if (issues.Count > 0)
            {
                const int maxLoggedIssues = 12;
                string[] loggedIssues = issues.Take(maxLoggedIssues).ToArray();
                string omitted = issues.Count > loggedIssues.Length
                    ? $"\n  ... {issues.Count - loggedIssues.Length} additional issue(s) omitted."
                    : string.Empty;
                Debug.LogWarning(
                    $"[Immersive Framework] Object Entry authoring discovery reported {issues.Count} issue(s):\n  - "
                    + string.Join("\n  - ", loggedIssues)
                    + omitted);
            }

            return new ObjectEntryAuthoringIndexSnapshot(ordered, issues);
        }

        private static void CollectRouteScenes(
            Dictionary<string, ObjectEntrySceneSources> scenes,
            List<string> issues)
        {
            string[] routeGuids = AssetDatabase.FindAssets("t:RouteAsset");
            for (int index = 0; index < routeGuids.Length; index++)
            {
                string routePath = AssetDatabase.GUIDToAssetPath(routeGuids[index]);
                RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(routePath);
                if (route == null)
                {
                    issues.Add($"Route asset could not be loaded: '{routePath}'.");
                    continue;
                }

                string ownerKey = routePath;
                string ownerLabel = $"Route '{route.RouteName}' ({routePath})";
                if (route.HasPrimaryScene)
                {
                    AddConfiguredScene(scenes, route.PrimaryScenePath, ownerKey, ownerLabel, issues);
                }

                RouteContentProfileAsset profile = route.RouteContentProfile;
                if (profile == null)
                {
                    continue;
                }

                IReadOnlyList<RouteContentSceneEntry> entries = profile.AdditionalScenes;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    RouteContentSceneEntry entry = entries[entryIndex];
                    if (entry == null || !entry.HasScene)
                    {
                        continue;
                    }

                    AddConfiguredScene(
                        scenes,
                        entry.ScenePath,
                        ownerKey,
                        ownerLabel,
                        issues);
                }
            }
        }

        private static void CollectActivityScenes(
            Dictionary<string, ObjectEntrySceneSources> scenes,
            List<string> issues)
        {
            string[] activityGuids = AssetDatabase.FindAssets("t:ActivityAsset");
            for (int index = 0; index < activityGuids.Length; index++)
            {
                string activityPath = AssetDatabase.GUIDToAssetPath(activityGuids[index]);
                ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(activityPath);
                if (activity == null)
                {
                    issues.Add($"Activity asset could not be loaded: '{activityPath}'.");
                    continue;
                }

                ActivityContentProfileAsset profile = activity.ActivityContentProfile;
                if (profile == null)
                {
                    continue;
                }

                string ownerKey = activityPath;
                string ownerLabel = $"Activity '{activity.ActivityName}' ({activityPath})";
                IReadOnlyList<ActivityContentSceneEntry> entries = profile.Scenes;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    ActivityContentSceneEntry entry = entries[entryIndex];
                    if (entry == null || !entry.HasScene)
                    {
                        continue;
                    }

                    AddConfiguredScene(
                        scenes,
                        entry.ScenePath,
                        ownerKey,
                        ownerLabel,
                        issues);
                }
            }
        }

        private static void AddConfiguredScene(
            Dictionary<string, ObjectEntrySceneSources> scenes,
            string rawScenePath,
            string ownerKey,
            string ownerLabel,
            List<string> issues)
        {
            string scenePath = NormalizeAssetPath(rawScenePath);
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                issues.Add($"Configured content for {ownerLabel} has no scene asset path; scene names are not used to infer identity.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                issues.Add($"Configured scene could not be loaded for {ownerLabel}: '{scenePath}'.");
                return;
            }

            if (!scenes.TryGetValue(scenePath, out ObjectEntrySceneSources sources))
            {
                sources = new ObjectEntrySceneSources();
                scenes.Add(scenePath, sources);
            }

            sources.AddOwner(ownerKey, ownerLabel);
        }

        private static void ScanConfiguredScene(
            string scenePath,
            ObjectEntrySceneSources sources,
            Dictionary<ObjectEntryId, ObjectEntryAuthoringCandidate> candidates,
            List<string> issues)
        {
            Scene loadedScene = FindLoadedScene(scenePath);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                ScanScene(loadedScene, sources, candidates, issues);
                return;
            }

            Scene previewScene = default;
            try
            {
                previewScene = EditorSceneManager.OpenPreviewScene(scenePath);
                if (!previewScene.IsValid())
                {
                    issues.Add($"Scene preview could not be opened: '{scenePath}'.");
                    return;
                }

                ScanScene(previewScene, sources, candidates, issues);
            }
            catch (Exception exception)
            {
                issues.Add($"Scene preview failed for '{scenePath}': {exception.GetType().Name}: {exception.Message}");
            }
            finally
            {
                if (previewScene.IsValid() && EditorSceneManager.IsPreviewScene(previewScene))
                {
                    try
                    {
                        if (!EditorSceneManager.ClosePreviewScene(previewScene))
                        {
                            issues.Add($"Preview scene could not be closed after inspection: '{scenePath}'.");
                        }
                    }
                    catch (Exception exception)
                    {
                        issues.Add($"Preview scene cleanup failed for '{scenePath}': {exception.GetType().Name}: {exception.Message}");
                    }
                }
            }
        }

        private static void ScanScene(
            Scene scene,
            ObjectEntrySceneSources sources,
            Dictionary<ObjectEntryId, ObjectEntryAuthoringCandidate> candidates,
            List<string> issues)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                ObjectEntryDeclaration[] declarations = roots[rootIndex]
                    .GetComponentsInChildren<ObjectEntryDeclaration>(true);
                for (int declarationIndex = 0; declarationIndex < declarations.Length; declarationIndex++)
                {
                    ObjectEntryDeclaration declaration = declarations[declarationIndex];
                    if (declaration == null)
                    {
                        continue;
                    }

                    if (!declaration.TryGetObjectEntryId(out ObjectEntryId id))
                    {
                        issues.Add($"Invalid or missing Object Entry ID in '{scene.path}' at '{GetHierarchyPath(declaration.transform)}'.");
                        continue;
                    }

                    ObjectEntryAuthoringCandidate candidate = GetOrCreateCandidate(candidates, id);
                    string hierarchy = GetHierarchyPath(declaration.transform);
                    for (int sourceIndex = 0; sourceIndex < sources.Owners.Count; sourceIndex++)
                    {
                        ObjectEntryAuthoringOwnerContext owner = sources.Owners[sourceIndex];
                        candidate.AddSceneSource(
                            declaration.gameObject.name,
                            owner.Key,
                            $"{owner.Label} · Scene '{scene.path}' · {hierarchy}");
                    }
                }
            }
        }

        private static void ScanPrefabTemplates(
            Dictionary<ObjectEntryId, ObjectEntryAuthoringCandidate> candidates,
            List<string> issues)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int index = 0; index < prefabGuids.Length; index++)
            {
                string prefabPath = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(prefabGuids[index]));
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (root == null)
                    {
                        issues.Add($"Prefab contents could not be loaded: '{prefabPath}'.");
                        continue;
                    }

                    ObjectEntryDeclaration[] declarations = root.GetComponentsInChildren<ObjectEntryDeclaration>(true);
                    for (int declarationIndex = 0; declarationIndex < declarations.Length; declarationIndex++)
                    {
                        ObjectEntryDeclaration declaration = declarations[declarationIndex];
                        if (declaration == null)
                        {
                            continue;
                        }

                        if (!declaration.TryGetObjectEntryId(out ObjectEntryId id))
                        {
                            issues.Add($"Invalid or missing Object Entry ID in prefab template '{prefabPath}' at '{GetHierarchyPath(declaration.transform)}'.");
                            continue;
                        }

                        ObjectEntryAuthoringCandidate candidate = GetOrCreateCandidate(candidates, id);
                        candidate.AddPrefabTemplate(
                            declaration.gameObject.name,
                            $"Prefab template '{prefabPath}' · {GetHierarchyPath(declaration.transform)}");
                    }
                }
                catch (Exception exception)
                {
                    issues.Add($"Prefab inspection failed for '{prefabPath}': {exception.GetType().Name}: {exception.Message}");
                }
                finally
                {
                    if (root != null)
                    {
                        try
                        {
                            PrefabUtility.UnloadPrefabContents(root);
                        }
                        catch (Exception exception)
                        {
                            issues.Add($"Prefab cleanup failed for '{prefabPath}': {exception.GetType().Name}: {exception.Message}");
                        }
                    }
                }
            }
        }

        private static ObjectEntryAuthoringCandidate GetOrCreateCandidate(
            Dictionary<ObjectEntryId, ObjectEntryAuthoringCandidate> candidates,
            ObjectEntryId id)
        {
            if (!candidates.TryGetValue(id, out ObjectEntryAuthoringCandidate candidate))
            {
                candidate = new ObjectEntryAuthoringCandidate(id);
                candidates.Add(id, candidate);
            }

            return candidate;
        }

        private static Scene FindLoadedScene(string scenePath)
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.IsValid()
                    && scene.isLoaded
                    && string.Equals(NormalizeAssetPath(scene.path), scenePath, StringComparison.Ordinal))
                {
                    return scene;
                }
            }

            return default;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var parts = new List<string>();
            while (transform != null)
            {
                parts.Add(transform.name);
                transform = transform.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim().Replace('\\', '/');
        }

        private sealed class ObjectEntrySceneSources
        {
            private readonly HashSet<string> _ownerKeys = new HashSet<string>(StringComparer.Ordinal);
            private readonly List<ObjectEntryAuthoringOwnerContext> _owners = new List<ObjectEntryAuthoringOwnerContext>();

            internal IReadOnlyList<ObjectEntryAuthoringOwnerContext> Owners => _owners;

            internal void AddOwner(string key, string label)
            {
                if (_ownerKeys.Add(key))
                {
                    _owners.Add(new ObjectEntryAuthoringOwnerContext(key, label));
                }
            }
        }
    }

    internal sealed class ObjectEntryAuthoringIndexSnapshot
    {
        private readonly Dictionary<ObjectEntryId, ObjectEntryAuthoringCandidate> _byId;

        internal ObjectEntryAuthoringIndexSnapshot(
            IReadOnlyList<ObjectEntryAuthoringCandidate> candidates,
            IReadOnlyList<string> issues)
        {
            Candidates = candidates;
            Issues = issues;
            _byId = candidates.ToDictionary(candidate => candidate.Id);
        }

        internal IReadOnlyList<ObjectEntryAuthoringCandidate> Candidates { get; }

        internal IReadOnlyList<string> Issues { get; }

        internal bool TryGetCandidate(ObjectEntryId id, out ObjectEntryAuthoringCandidate candidate) =>
            _byId.TryGetValue(id, out candidate);

        internal string GetSelectionLabel(string rawId)
        {
            if (!TryParse(rawId, out ObjectEntryId id))
            {
                return string.IsNullOrWhiteSpace(rawId)
                    ? "Select an Object Entry"
                    : $"Invalid ID (preserved): {rawId.Trim()}";
            }

            return TryGetCandidate(id, out ObjectEntryAuthoringCandidate candidate)
                ? candidate.DisplayLabel
                : $"Not found (preserved): {id.StableText}";
        }

        internal string GetStatusMessage(string rawId)
        {
            string status;
            if (string.IsNullOrWhiteSpace(rawId))
            {
                status = "No Object Entry is selected.";
            }
            else if (!TryParse(rawId, out ObjectEntryId id))
            {
                status = "Stored Object Entry ID is invalid and has been preserved.";
            }
            else if (!TryGetCandidate(id, out ObjectEntryAuthoringCandidate candidate))
            {
                status = "ID is not indexed; its value is preserved. Check configured Route/Activity scenes and prefab templates.";
            }
            else if (!candidate.HasSceneSource)
            {
                status = "This ID is declared only in prefab templates; no physical occurrence is implied.";
            }
            else if (candidate.HasSameOwnerDuplicate)
            {
                status = "This ID appears more than once under one content owner; review the duplicate declarations.";
            }
            else
            {
                status = $"Found in {candidate.SceneSourceCount} authored source location(s).";
            }

            if (Issues.Count > 0)
            {
                status += $" Discovery reported {Issues.Count} authoring issue(s); details are in the Framework Editor log.";
            }

            return status;
        }

        internal MessageType GetStatusType(string rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId))
            {
                return Issues.Count > 0 ? MessageType.Warning : MessageType.Info;
            }

            if (!TryParse(rawId, out ObjectEntryId id))
            {
                return MessageType.Error;
            }

            if (!TryGetCandidate(id, out ObjectEntryAuthoringCandidate candidate)
                || !candidate.HasSceneSource
                || candidate.HasSameOwnerDuplicate
                || Issues.Count > 0)
            {
                return MessageType.Warning;
            }

            return MessageType.Info;
        }

        internal bool HasIssues => Issues.Count > 0;

        private static bool TryParse(string rawId, out ObjectEntryId id)
        {
            id = default;
            if (string.IsNullOrWhiteSpace(rawId))
            {
                return false;
            }

            try
            {
                id = ObjectEntryId.From(rawId.Trim());
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    internal sealed class ObjectEntryAuthoringCandidate
    {
        private readonly SortedSet<string> _displayNames = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<string> _sceneSources = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<string> _prefabTemplates = new SortedSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _sceneOccurrencesByOwner = new Dictionary<string, int>(StringComparer.Ordinal);

        internal ObjectEntryAuthoringCandidate(ObjectEntryId id)
        {
            Id = id;
        }

        internal ObjectEntryId Id { get; }

        internal bool HasSceneSource => _sceneSources.Count > 0;

        internal int SceneSourceCount => _sceneSources.Count;

        internal bool HasSameOwnerDuplicate => _sceneOccurrencesByOwner.Values.Any(count => count > 1);

        internal string DisplayLabel
        {
            get
            {
                string sourceKind = HasSceneSource ? "Scene entry" : "Prefab template";
                string name = _displayNames.Count == 0
                    ? "<unnamed>"
                    : _displayNames.Count == 1
                        ? _displayNames.Min
                        : $"{_displayNames.Min} (+{_displayNames.Count - 1} names)";
                string source = GetSourceSummary();
                string duplicate = HasSameOwnerDuplicate ? " · duplicate within owner" : string.Empty;
                return $"{sourceKind}: {name} · {Id.StableText} · {source}{duplicate}";
            }
        }

        internal void AddSceneSource(string displayName, string ownerKey, string source)
        {
            AddDisplayName(displayName);
            _sceneSources.Add(source);
            _sceneOccurrencesByOwner.TryGetValue(ownerKey, out int count);
            _sceneOccurrencesByOwner[ownerKey] = count + 1;
        }

        internal void AddPrefabTemplate(string displayName, string source)
        {
            AddDisplayName(displayName);
            _prefabTemplates.Add(source);
        }

        private string GetSourceSummary()
        {
            IEnumerable<string> sources = HasSceneSource ? _sceneSources : _prefabTemplates;
            string[] displayed = sources.Take(2).ToArray();
            string result = string.Join("; ", displayed);
            int total = HasSceneSource ? _sceneSources.Count : _prefabTemplates.Count;
            if (total > displayed.Length)
            {
                result += $"; +{total - displayed.Length} more source(s)";
            }

            if (HasSceneSource && _prefabTemplates.Count > 0)
            {
                result += $"; {_prefabTemplates.Count} prefab template(s)";
            }

            return result;
        }

        private void AddDisplayName(string displayName)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                _displayNames.Add(displayName.Trim());
            }
        }
    }

    internal readonly struct ObjectEntryAuthoringOwnerContext
    {
        internal ObjectEntryAuthoringOwnerContext(string key, string label)
        {
            Key = key ?? string.Empty;
            Label = label ?? string.Empty;
        }

        internal string Key { get; }

        internal string Label { get; }
    }
}
