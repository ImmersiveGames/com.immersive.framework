using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Non-mutating authoring validation for Actor Profiles and Player selection policies.
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

            ValidateLogicalActorHost(profile, report);

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
                    $"Actor Profile is valid. actorProfileId='{actorProfileId}' kind='{profile.ActorKind}' role='{profile.ActorRole}' logicalHost='{profile.LogicalActorHostPrefab.name}'.",
                    profile);
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateSelectionPolicyProfile(
            PlayerActorSelectionPolicyProfile profile)
        {
            return ValidateSelectionPolicyProfile(profile, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateSelectionPolicyProfile(
            PlayerActorSelectionPolicyProfile profile,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (profile == null)
            {
                report.AddError("Player Actor Selection Policy Profile is missing.", null);
                return report;
            }

            if (string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                report.AddWarning(
                    "Player Actor Selection Policy Profile has no Display Name. Give the reusable policy a designer-facing name.",
                    profile);
            }

            if (!profile.HasDefinedDuplicatePolicy)
            {
                report.AddError(
                    $"Player Actor Selection Policy Profile '{profile.name}' requires an explicit Duplicate Policy.",
                    profile);
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Player Actor Selection Policy Profile is valid. duplicatePolicy='{profile.DuplicatePolicy}'.",
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

            string[] policyGuids = AssetDatabase.FindAssets("t:PlayerActorSelectionPolicyProfile");
            for (int index = 0; index < policyGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(policyGuids[index]);
                PlayerActorSelectionPolicyProfile profile =
                    AssetDatabase.LoadAssetAtPath<PlayerActorSelectionPolicyProfile>(assetPath);
                report.AddRange(ValidateSelectionPolicyProfile(profile, validationMode));
            }

            if (actorProfileGuids.Length == 0)
            {
                report.AddOptionalSkip(
                    "No ActorProfile assets exist yet. Create explicit Actor Profiles before enabling Actor selection requirements.",
                    null);
            }

            if (policyGuids.Length == 0)
            {
                report.AddOptionalSkip(
                    "No Player Actor Selection Policy Profiles exist yet. Create the official policy set before composing Session Actor selection runtime.",
                    null);
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Actor selection authoring validation passed. actorProfiles='{validActorProfiles}' policyProfiles='{policyGuids.Length}'.",
                    null);
            }

            return report;
        }

        private static void ValidateLogicalActorHost(
            ActorProfile profile,
            FrameworkAuthoringValidationReport report)
        {
            GameObject logicalHost = profile.LogicalActorHostPrefab;
            if (logicalHost == null)
            {
                report.AddError(
                    $"ActorProfile '{profile.name}' requires an explicit Logical Actor Host Prefab. No fallback host is inferred.",
                    profile);
                return;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(logicalHost))
            {
                report.AddError(
                    $"ActorProfile '{profile.name}' Logical Actor Host '{logicalHost.name}' is not a prefab asset.",
                    profile);
                return;
            }

            ActorDeclaration[] actorDeclarations =
                logicalHost.GetComponentsInChildren<ActorDeclaration>(true);
            PlayerActorDeclaration[] playerDeclarations =
                logicalHost.GetComponentsInChildren<PlayerActorDeclaration>(true);
            var uniqueDeclarations = new HashSet<Component>();
            for (int index = 0; index < actorDeclarations.Length; index++)
            {
                uniqueDeclarations.Add(actorDeclarations[index]);
            }

            for (int index = 0; index < playerDeclarations.Length; index++)
            {
                uniqueDeclarations.Add(playerDeclarations[index]);
            }

            int declarationCount = uniqueDeclarations.Count;

            if (declarationCount == 0)
            {
                report.AddError(
                    $"Logical Actor Host Prefab '{logicalHost.name}' has no ActorDeclaration or PlayerActorDeclaration.",
                    logicalHost);
                return;
            }

            if (declarationCount > 1)
            {
                report.AddError(
                    $"Logical Actor Host Prefab '{logicalHost.name}' contains '{declarationCount}' Actor declarations. Exactly one declaration is required.",
                    logicalHost);
                return;
            }

            if (profile.ActorKind == ActorKind.Player)
            {
                if (playerDeclarations.Length != 1)
                {
                    report.AddError(
                        $"Player ActorProfile '{profile.name}' requires a Logical Actor Host with one PlayerActorDeclaration.",
                        logicalHost);
                    return;
                }

                PlayerInput[] playerInputs =
                    logicalHost.GetComponentsInChildren<PlayerInput>(true);
                PlayerActorDeclaration declaration = playerDeclarations[0];
                if (playerInputs.Length != 0 || declaration.HasPlayerInputEvidence)
                {
                    report.AddError(
                        $"Player Logical Actor Host '{logicalHost.name}' must not contain PlayerInput evidence. PlayerInput belongs to LocalPlayerHostAuthoring and is bound later by explicit composition.",
                        logicalHost);
                }

                if (profile.ActorRole != declaration.ActorRole)
                {
                    report.AddError(
                        $"ActorProfile '{profile.name}' role '{profile.ActorRole}' does not match its PlayerActorDeclaration role '{declaration.ActorRole}'.",
                        profile);
                }
            }
            else
            {
                if (actorDeclarations.Length != 1 || playerDeclarations.Length != 0)
                {
                    report.AddError(
                        $"Non-Player ActorProfile '{profile.name}' requires a Logical Actor Host with one ActorDeclaration.",
                        logicalHost);
                    return;
                }

                ActorDeclaration declaration = actorDeclarations[0];
                if (profile.ActorKind != declaration.ActorKind)
                {
                    report.AddError(
                        $"ActorProfile '{profile.name}' kind '{profile.ActorKind}' does not match its ActorDeclaration kind '{declaration.ActorKind}'.",
                        profile);
                }

                if (profile.ActorRole != declaration.ActorRole)
                {
                    report.AddError(
                        $"ActorProfile '{profile.name}' role '{profile.ActorRole}' does not match its ActorDeclaration role '{declaration.ActorRole}'.",
                        profile);
                }
            }
        }
    }
}
