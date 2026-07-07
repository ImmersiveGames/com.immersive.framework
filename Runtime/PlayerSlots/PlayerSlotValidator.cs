using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.PlayerSlots
{
    /// <summary>
    /// API status: Experimental. Validator for PlayerSlot declarations and passive occupancies.
    /// It produces diagnostics only; it does not own managers, registries, input routing, possession or actor replacement.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45C1 PlayerSlot declaration and occupancy validator.")]
    public static class PlayerSlotValidator
    {
        public static PlayerSlotSet ValidateLoadedSceneDeclarations(string source, string reason)
        {
            PlayerSlotDeclaration[] declarations = Object.FindObjectsByType<PlayerSlotDeclaration>(FindObjectsInactive.Include);
            PlayerSlotOccupancy[] occupancies = Object.FindObjectsByType<PlayerSlotOccupancy>(FindObjectsInactive.Include);
            return ValidateDeclarations(declarations, occupancies, source, reason);
        }

        public static PlayerSlotSet ValidateDeclarations(
            IEnumerable<PlayerSlotDeclaration> declarations,
            IEnumerable<PlayerSlotOccupancy> occupancies,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerSlotValidator));
            var descriptors = new List<PlayerSlotDescriptor>();
            var occupancyDescriptors = new List<PlayerSlotOccupancyDescriptor>();
            var issues = new List<PlayerSlotSetIssue>();

            AppendDeclarations(declarations, normalizedSource, descriptors, issues);
            AppendOccupancies(occupancies, normalizedSource, occupancyDescriptors, issues);

            return PlayerSlotSet.FromDescriptors(descriptors, occupancyDescriptors, issues, normalizedSource, reason);
        }

        public static PlayerSlotSet ValidateDescriptors(
            IEnumerable<PlayerSlotDescriptor> descriptors,
            IEnumerable<PlayerSlotOccupancyDescriptor> occupancies,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerSlotValidator));
            return PlayerSlotSet.FromDescriptors(descriptors, occupancies, normalizedSource, reason);
        }

        private static void AppendDeclarations(
            IEnumerable<PlayerSlotDeclaration> declarations,
            string normalizedSource,
            List<PlayerSlotDescriptor> descriptors,
            List<PlayerSlotSetIssue> issues)
        {
            if (declarations == null)
            {
                return;
            }

            foreach (PlayerSlotDeclaration declaration in declarations)
            {
                if (declaration == null)
                {
                    issues.Add(PlayerSlotSetIssue.BlockingIssue(
                        PlayerSlotSetIssueKind.InvalidDeclaration,
                        string.Empty,
                        string.Empty,
                        normalizedSource,
                        "PlayerSlot declaration reference is null."));
                    continue;
                }

                if (declaration.TryCreateDescriptor(normalizedSource, out PlayerSlotDescriptor descriptor, out PlayerSlotSetIssue issue))
                {
                    descriptors.Add(descriptor);
                    continue;
                }

                issues.Add(issue);
            }
        }

        private static void AppendOccupancies(
            IEnumerable<PlayerSlotOccupancy> occupancies,
            string normalizedSource,
            List<PlayerSlotOccupancyDescriptor> descriptors,
            List<PlayerSlotSetIssue> issues)
        {
            if (occupancies == null)
            {
                return;
            }

            foreach (PlayerSlotOccupancy occupancy in occupancies)
            {
                if (occupancy == null)
                {
                    issues.Add(PlayerSlotSetIssue.BlockingIssue(
                        PlayerSlotSetIssueKind.InvalidDeclaration,
                        string.Empty,
                        string.Empty,
                        normalizedSource,
                        "PlayerSlot occupancy reference is null."));
                    continue;
                }

                if (occupancy.TryCreateDescriptor(normalizedSource, out PlayerSlotOccupancyDescriptor descriptor, out PlayerSlotSetIssue issue))
                {
                    descriptors.Add(descriptor);
                    continue;
                }

                issues.Add(issue);
            }
        }
    }
}
