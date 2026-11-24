using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.SceneLifecycle
{
    internal static class SceneLoadIdentity
    {
        internal static bool TryResolveSceneLoadIdentifier(
            string scenePath,
            string sceneName,
            out string sceneIdentifier,
            out string diagnostic)
        {
            scenePath = scenePath.NormalizeText();
            return TryResolveSceneLoadIdentifierFromCandidates(
                scenePath, sceneName, GetCandidatePaths(scenePath, sceneName),
                Application.CanStreamedLevelBeLoaded, out sceneIdentifier, out diagnostic);
        }

        internal static bool TryResolveLegacyPath(
            string sceneName, out string scenePath, out string diagnostic) =>
            TryResolveSceneLoadIdentifierFromCandidates(
                string.Empty, sceneName, GetCandidatePaths(string.Empty, sceneName),
                static _ => true, out scenePath, out diagnostic);

        private static HashSet<string> GetCandidatePaths(string scenePath, string sceneName)
        {
            var candidatePaths = GetEffectiveBuildScenePaths();
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                sceneName = sceneName.NormalizeText();
                for (int index = 0; index < SceneManager.sceneCount; index++)
                {
                    Scene loadedScene = SceneManager.GetSceneAt(index);
                    if (loadedScene.IsValid() && loadedScene.isLoaded &&
                        HasSceneName(loadedScene.path, sceneName))
                    {
                        candidatePaths.Add(loadedScene.path);
                    }
                }
            }

            return candidatePaths;
        }

        internal static bool TryResolveSceneLoadIdentifierFromCandidates(
            string scenePath,
            string sceneName,
            IEnumerable<string> candidatePaths,
            Func<string, bool> canLoadScenePath,
            out string sceneIdentifier,
            out string diagnostic)
        {
            scenePath = scenePath.NormalizeText();
            sceneName = sceneName.NormalizeText();
            var availablePaths = new HashSet<string>(
                candidatePaths ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                if (!scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                    !availablePaths.Contains(scenePath) ||
                    canLoadScenePath == null ||
                    !canLoadScenePath(scenePath))
                {
                    sceneIdentifier = string.Empty;
                    diagnostic =
                        $"Explicit scenePath '{scenePath}' is invalid or unavailable in the effective Player scene set. Name fallback is forbidden.";
                    return false;
                }

                sceneIdentifier = scenePath;
                diagnostic = string.Empty;
                return true;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                sceneIdentifier = string.Empty;
                diagnostic = "Neither scenePath nor legacy sceneName is authored.";
                return false;
            }

            var matchingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string candidatePath in availablePaths)
            {
                if (HasSceneName(candidatePath, sceneName))
                    matchingPaths.Add(candidatePath);
            }

            if (matchingPaths.Count != 1)
            {
                sceneIdentifier = string.Empty;
                diagnostic = matchingPaths.Count == 0
                    ? $"Legacy sceneName '{sceneName}' has no candidate in the effective build or loaded scene set."
                    : $"Legacy sceneName '{sceneName}' is ambiguous across {matchingPaths.Count} scene paths.";
                return false;
            }

            foreach (string candidatePath in matchingPaths)
            {
                if (canLoadScenePath == null || !canLoadScenePath(candidatePath))
                {
                    sceneIdentifier = string.Empty;
                    diagnostic =
                        $"The unique legacy scene candidate '{candidatePath}' cannot be loaded from the effective Player scene set.";
                    return false;
                }

                sceneIdentifier = candidatePath;
                diagnostic = string.Empty;
                return true;
            }

            sceneIdentifier = string.Empty;
            diagnostic = $"Legacy sceneName '{sceneName}' could not be resolved.";
            return false;
        }

        private static HashSet<string> GetEffectiveBuildScenePaths()
        {
            var candidatePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                string candidatePath = SceneUtility.GetScenePathByBuildIndex(index);
                if (!string.IsNullOrWhiteSpace(candidatePath))
                    candidatePaths.Add(candidatePath);
            }

            return candidatePaths;
        }

        internal static bool HasSceneName(string scenePath, string sceneName)
        {
            return !string.IsNullOrWhiteSpace(scenePath) &&
                   string.Equals(
                       Path.GetFileNameWithoutExtension(scenePath),
                       sceneName,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
