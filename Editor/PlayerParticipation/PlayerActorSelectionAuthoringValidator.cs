using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.PlayerParticipation
{
    /// <summary>
    /// Non-mutating authoring validation for Actor Profiles.
    /// </summary>
    internal static class PlayerActorSelectionAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateActorProfile(
            ActorProfile profile,
            bool includeProjectDuplicateScan)
        {
            return ValidateActorProfile(
                profile,
                includeProjectDuplicateScan,
                FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActorProfile(
            ActorProfile profile,
            bool includeProjectDuplicateScan,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (profile == null)
            {
                report.AddError("Actor Profile is missing.", null);
                return report;
            }

            if (!profile.TryGetActorProfileId(out ActorProfileId actorProfileId, out string identityIssue))
            {
                report.AddError(identityIssue, profile);
                return report;
            }

            if (string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                report.AddWarning(
                    $"ActorProfile '{profile.name}' has no Display Name. Identity remains valid, but product presentation is incomplete.",
                    profile);
            }

            if (!profile.HasDefinedActorKind)
            {
                report.AddError(
                    $"ActorProfile '{profile.name}' requires a defined non-Unknown Actor Kind.",
                    profile);
            }

            if (!profile.HasDefinedActorRole)
            {
                report.AddError(
                    $"ActorProfile '{profile.name}' requires a defined non-Unknown Actor Role.",
                    profile);
            }

            ValidateCompositionParts(profile, report);

            if (includeProjectDuplicateScan)
            {
                string[] profileGuids = AssetDatabase.FindAssets("t:ActorProfile");
                for (int index = 0; index < profileGuids.Length; index++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(profileGuids[index]);
                    ActorProfile candidate = AssetDatabase.LoadAssetAtPath<ActorProfile>(assetPath);
                    if (candidate == null || candidate == profile)
                    {
                        continue;
                    }

                    if (candidate.TryGetActorProfileId(out ActorProfileId candidateId, out _) &&
                        candidateId == actorProfileId)
                    {
                        report.AddError(
                            $"ActorProfileId '{actorProfileId}' is also owned by ActorProfile '{candidate.name}' at '{assetPath}'.",
                            profile);
                    }
                }
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Actor Profile is valid. actorProfileId='{actorProfileId}' kind='{profile.ActorKind}' role='{profile.ActorRole}' compositionParts='{profile.CompositionParts.Count}'.",
                    profile);
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateProjectActorSelectionProfiles(
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            string[] actorProfileGuids = AssetDatabase.FindAssets("t:ActorProfile");
            var identityOwners = new Dictionary<ActorProfileId, ActorProfile>();
            int validActorProfiles = 0;

            for (int index = 0; index < actorProfileGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(actorProfileGuids[index]);
                ActorProfile profile = AssetDatabase.LoadAssetAtPath<ActorProfile>(assetPath);
                if (profile == null)
                {
                    report.AddError($"ActorProfile asset at '{assetPath}' could not be loaded.", null);
                    continue;
                }

                report.AddRange(ValidateActorProfile(profile, false, validationMode));
                if (!profile.TryGetActorProfileId(out ActorProfileId actorProfileId, out _))
                {
                    continue;
                }

                if (identityOwners.TryGetValue(actorProfileId, out ActorProfile firstOwner))
                {
                    report.AddError(
                        $"ActorProfileId '{actorProfileId}' is duplicated by Profiles '{firstOwner.name}' and '{profile.name}'. Profile identity must be unique across the project.",
                        profile);
                    continue;
                }

                identityOwners.Add(actorProfileId, profile);
                validActorProfiles++;
            }

            if (actorProfileGuids.Length == 0)
            {
                report.AddOptionalSkip(
                    "No ActorProfile assets exist yet. Create explicit Actor Profiles before enabling Actor selection requirements.",
                    null);
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Actor selection authoring validation passed. actorProfiles='{validActorProfiles}'. Duplicate-selection policy is configured directly by each Game Application.",
                    null);
            }

            return report;
        }

        private static void ValidateCompositionParts(
            ActorProfile profile,
            FrameworkAuthoringValidationReport report)
        {
            if (profile.HasLegacyPresentationPrefab)
            {
                report.AddError(
                    $"ActorProfile '{profile.name}' still has a legacy visual reference. Run Tools > Immersive Framework > Migrate Actor Profile Visual Content.",
                    profile);
                return;
            }

            if (!ActorCompositionProfileContract.TryValidate(profile, out string issue))
            {
                report.AddError(issue, profile);
                return;
            }

            for (int index = 0; index < profile.CompositionParts.Count; index++)
            {
                ActorCompositionPart part = profile.CompositionParts[index];
                if (!PrefabUtility.IsPartOfPrefabAsset(part.Prefab))
                {
                    report.AddError(
                        $"Actor Profile composition key '{part.Key}' requires a Prefab asset; '{part.Prefab.name}' is not a Prefab asset.",
                        part.Prefab);
                }
            }
        }
    }
}
