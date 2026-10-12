using UnityEditor;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Editor.Reset
{
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Shared Reset Editor helper for recording authored prefab changes.")]
    public static class ResetAuthoringIdentityUtility
    {
        internal static void RecordPrefabModification(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
