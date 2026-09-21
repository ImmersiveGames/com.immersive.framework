using System;
using System.Collections.Generic;
using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    public static class CameraDefinitionIdentityEditorUtility
    {
        public static bool HasCollision(ScriptableObject definition) => FindCollision(definition) != null;

        public static void GenerateMissingId(ScriptableObject definition)
        {
            var serialized = SerializedDefinition(definition);
            if (!string.IsNullOrEmpty(serialized.FindProperty("stableId").stringValue))
                throw new InvalidOperationException("Identity already exists. Use explicit collision repair.");
            WriteNewId(serialized);
        }

        public static void RepairCollision(ScriptableObject definition)
        {
            var serialized = SerializedDefinition(definition);
            if (FindCollision(definition) == null)
                throw new InvalidOperationException("The selected definition has no stable ID collision.");
            WriteNewId(serialized);
        }

        public static string Validate(ScriptableObject definition)
        {
            SerializedDefinition(definition);
            bool valid = HasValidId(definition);
            if (!valid)
                return "Stable ID is missing or invalid. Generate identity explicitly for a new definition.";

            var collision = FindCollision(definition);
            return collision == null ? null :
                "Stable ID collision with " + AssetDatabase.GetAssetPath(collision) +
                ". Repair the selected duplicate explicitly.";
        }

        private static SerializedObject SerializedDefinition(ScriptableObject definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!(definition is CameraOutputDefinition) &&
                !(definition is CameraPresentationDefinition))
            {
                throw new ArgumentException(
                    "Expected a supported Camera definition.",
                    nameof(definition));
            }

            return new SerializedObject(definition);
        }

        private static bool HasValidId(ScriptableObject definition)
        {
            return definition switch
            {
                CameraOutputDefinition output => output.HasValidId,
                CameraPresentationDefinition presentation => presentation.HasValidId,
                _ => false
            };
        }

        private static IEnumerable<ScriptableObject> Definitions(Type type)
        {
            var paths = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + type.Name))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!paths.Add(path)) continue;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset != null && asset.GetType() == type)
                        yield return (ScriptableObject)asset;
            }
        }

        private static ScriptableObject FindCollision(ScriptableObject definition)
        {
            string id = SerializedDefinition(definition).FindProperty("stableId").stringValue;
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var other in Definitions(definition.GetType()))
                if (!ReferenceEquals(other, definition) &&
                    SerializedDefinition(other).FindProperty("stableId").stringValue == id)
                    return other;
            return null;
        }

        private static void WriteNewId(SerializedObject serialized)
        {
            var occupied = new HashSet<string>();
            foreach (var other in Definitions(serialized.targetObject.GetType()))
                occupied.Add(SerializedDefinition(other).FindProperty("stableId").stringValue);
            string id;
            do { id = CameraAuthoringIdUtility.GenerateIdText(); } while (occupied.Contains(id));
            Undo.RecordObject(serialized.targetObject, "Generate Camera Definition Identity");
            serialized.FindProperty("stableId").stringValue = id;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(serialized.targetObject);
        }
    }
}
