using System;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable, read-only evidence for one Player Slot in the current
    /// Manager-Provisioned lifecycle projection.
    /// </summary>
    public sealed class ManagerProvisionedPlayerLifecycleSlotSnapshot
    {
        public ManagerProvisionedPlayerLifecycleSlotSnapshot(
            string playerSlotId,
            string slotState,
            bool hasTechnicalHost,
            string selectedActorProfile,
            bool logicalActorPrepared,
            bool physicalActorMaterialized,
            bool gameplayAdmitted,
            string diagnostic)
        {
            PlayerSlotId = Normalize(playerSlotId);
            SlotState = Normalize(slotState);
            HasTechnicalHost = hasTechnicalHost;
            SelectedActorProfile = Normalize(selectedActorProfile);
            LogicalActorPrepared = logicalActorPrepared;
            PhysicalActorMaterialized = physicalActorMaterialized;
            GameplayAdmitted = gameplayAdmitted;
            Diagnostic = Normalize(diagnostic);
        }

        public string PlayerSlotId { get; }

        public string SlotState { get; }

        public bool HasTechnicalHost { get; }

        public string SelectedActorProfile { get; }

        public bool HasSelectedActor =>
            !string.IsNullOrWhiteSpace(SelectedActorProfile);

        public bool LogicalActorPrepared { get; }

        public bool PhysicalActorMaterialized { get; }

        public bool GameplayAdmitted { get; }

        public string Diagnostic { get; }

        public string ToDiagnosticString()
        {
            return
                $"slot='{PlayerSlotId}' state='{SlotState}' " +
                $"technicalHost='{HasTechnicalHost}' " +
                $"selectedActor='{SelectedActorProfile}' " +
                $"logicalPrepared='{LogicalActorPrepared}' " +
                $"physicalMaterialized='{PhysicalActorMaterialized}' " +
                $"gameplayAdmitted='{GameplayAdmitted}' " +
                $"diagnostic='{Diagnostic}'.";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}
