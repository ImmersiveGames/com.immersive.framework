using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    internal static class ActivityIdAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateProjectAssets(FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            var firstAssetById = new Dictionary<ActivityId, string>();
            string[] guids = AssetDatabase.FindAssets("t:ActivityAsset");

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(path);
                if (activity == null)
                {
                    report.AddError($"Activity asset at '{path}' could not be loaded.", null);
                    continue;
                }

                if (!activity.HasValidActivityId)
                {
                    report.AddError($"Activity ID is missing or invalid. asset='{path}'.", activity);
                    continue;
                }

                ActivityId id = activity.ActivityId;
                if (firstAssetById.TryGetValue(id, out string firstPath))
                {
                    report.AddError($"Duplicate Activity ID '{id.StableText}'. firstAsset='{firstPath}' secondAsset='{path}' context='Project Activity assets'.", activity);
                    continue;
                }

                firstAssetById.Add(id, path);
            }

            return report;
        }
    }
}
