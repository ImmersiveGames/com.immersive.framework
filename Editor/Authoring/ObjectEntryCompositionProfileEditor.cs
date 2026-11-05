using Immersive.Framework.ObjectEntry;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ObjectEntryCompositionProfile))]
    internal sealed class ObjectEntryCompositionProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var profile = (ObjectEntryCompositionProfile)target;
            if (!profile.TryValidate(out string issue))
                EditorGUILayout.HelpBox(issue, MessageType.Error);
            else
                EditorGUILayout.HelpBox("Profile structure is valid. Runtime application is not connected in B1.", MessageType.Info);
        }
    }
}
