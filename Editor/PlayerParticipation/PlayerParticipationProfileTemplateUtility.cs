using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Explicit creation commands for the official Player participation Profile templates.
    /// Created assets remain ordinary editable project assets; no hidden runtime defaults exist.
    /// </summary>
    internal static class PlayerParticipationProfileTemplateUtility
    {
        private const string TemplateFolderName = "PlayerParticipation";

        [MenuItem(
            "Assets/Create/Immersive Framework/Player/Templates/Player Slot Profiles 1-4",
            false,
            210)]
        private static void CreatePlayerSlotProfiles()
        {
            string folder = EnsureTemplateFolder();
            List<Object> created = CreatePlayerSlotProfileSet(folder);
            CompleteCreation(created, "Player Slot Profile templates");
        }

        [MenuItem(
            "Assets/Create/Immersive Framework/Player/Templates/Complete Local Player Profile Set",
            false,
            214)]
        private static void CreateCompleteProfileSet()
        {
            string folder = EnsureTemplateFolder();
            var created = new List<Object>();
            created.AddRange(CreatePlayerSlotProfileSet(folder));
            CompleteCreation(created, "complete local Player Profile template set");
        }

        private static List<Object> CreatePlayerSlotProfileSet(string folder)
        {
            var created = new List<Object>();
            created.Add(CreatePlayerSlotProfile(folder, 1, new Color(0.20f, 0.65f, 1.00f, 1f)));
            created.Add(CreatePlayerSlotProfile(folder, 2, new Color(1.00f, 0.55f, 0.20f, 1f)));
            created.Add(CreatePlayerSlotProfile(folder, 3, new Color(0.35f, 0.85f, 0.40f, 1f)));
            created.Add(CreatePlayerSlotProfile(folder, 4, new Color(0.85f, 0.35f, 0.90f, 1f)));
            return created;
        }

        private static PlayerSlotProfile CreatePlayerSlotProfile(
            string folder,
            int playerNumber,
            Color accentColor)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = $"Player Slot {playerNumber}";

            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("playerSlotId").stringValue = $"player.{playerNumber}";
            serializedProfile.FindProperty("displayName").stringValue = $"Player {playerNumber}";
            serializedProfile.FindProperty("description").stringValue =
                $"Official local participation seat template for Player {playerNumber}.";
            serializedProfile.FindProperty("accentColor").colorValue = accentColor;
            serializedProfile.FindProperty("displayOrder").intValue = playerNumber - 1;
            serializedProfile.FindProperty("defaultActorProfile").objectReferenceValue = null;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/PlayerSlot_Player{playerNumber}.asset");
            AssetDatabase.CreateAsset(profile, assetPath);
            return profile;
        }

        private static string EnsureTemplateFolder()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string parentFolder;

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                parentFolder = "Assets";
            }
            else if (AssetDatabase.IsValidFolder(selectedPath))
            {
                parentFolder = selectedPath;
            }
            else
            {
                parentFolder = Path.GetDirectoryName(selectedPath)?.Replace('\\', '/');
                if (string.IsNullOrWhiteSpace(parentFolder) ||
                    !parentFolder.StartsWith("Assets", System.StringComparison.Ordinal))
                {
                    parentFolder = "Assets";
                }
            }

            string folder = $"{parentFolder}/{TemplateFolderName}";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string guid = AssetDatabase.CreateFolder(parentFolder, TemplateFolderName);
                folder = AssetDatabase.GUIDToAssetPath(guid);
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                throw new System.InvalidOperationException(
                    $"Could not create Player participation template folder under '{parentFolder}'.");
            }

            return folder;
        }

        private static void CompleteCreation(IReadOnlyList<Object> created, string label)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Object firstCreated = null;
            for (int index = 0; index < created.Count; index++)
            {
                if (created[index] != null)
                {
                    firstCreated = created[index];
                    break;
                }
            }

            if (firstCreated != null)
            {
                Selection.activeObject = firstCreated;
                EditorGUIUtility.PingObject(firstCreated);
            }

            FrameworkLogger.Create(typeof(PlayerParticipationProfileTemplateUtility)).Info(
                $"[Immersive.Framework][PlayerParticipation] Created {created.Count} asset(s) for {label}. " +
                "The assets are explicit authoring inputs and are not hidden runtime defaults.");
        }
    }
}
