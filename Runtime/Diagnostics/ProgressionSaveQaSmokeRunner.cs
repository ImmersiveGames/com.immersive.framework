using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ProgressionSave;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for the built-in minimum
    /// JSON Progression Save backend.
    ///
    /// Core persistence is exercised through IProgressionSaveStore. Manifest
    /// projection is exercised separately through IProgressionSaveCatalog.
    /// Physical manifest mutation remains an internal backend responsibility.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.DevelopmentTooling,
        "ADR018-A built-in JSON Progression Save diagnostics smoke aligned with core store plus optional catalog capability.")]
    internal static class ProgressionSaveQaSmokeRunner
    {
        internal const string SmokeName =
            "Progression Save JSON Backend Diagnostics Smoke";

        private const string QaRootName =
            "ProgressionSaveJsonBackendSmoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource =
                source.NormalizeTextOrFallback(
                    nameof(ProgressionSaveQaSmokeRunner));

            string rootDirectory =
                Path.Combine(
                    Application.temporaryCachePath,
                    "ImmersiveFramework",
                    "Qa",
                    QaRootName);

            var store =
                new JsonProgressionSaveStore(
                    rootDirectory,
                    ProgressionSaveBackendId.From(
                        "json.qa"));

            IProgressionSaveStore coreStore =
                store;

            IProgressionSaveCatalog catalog =
                store;

            var primarySlot =
                ProgressionSaveSlotId.From(
                    "qa.slot.primary");

            var missingSlot =
                ProgressionSaveSlotId.From(
                    "qa.slot.missing");

            var corruptSlot =
                ProgressionSaveSlotId.From(
                    "qa.slot.corrupt");

            Cleanup(store);

            try
            {
                bool contractsPassed =
                    ValidateContracts(
                        logger,
                        normalizedSource,
                        store,
                        coreStore,
                        catalog,
                        primarySlot);

                bool missingPassed =
                    ValidateMissing(
                        logger,
                        coreStore,
                        catalog,
                        missingSlot);

                bool writeReadPassed =
                    ValidateWriteRead(
                        logger,
                        coreStore,
                        primarySlot,
                        out ProgressionSaveSlotRecord record);

                bool manifestPassed =
                    ValidateManifest(
                        logger,
                        catalog,
                        primarySlot,
                        record);

                bool corruptPassed =
                    ValidateCorruptSlot(
                        logger,
                        store,
                        corruptSlot);

                bool deletePassed =
                    ValidateDelete(
                        logger,
                        store,
                        coreStore,
                        catalog,
                        primarySlot,
                        corruptSlot);

                bool boundaryPassed =
                    ValidateBoundary(
                        logger,
                        store);

                return Task.FromResult(
                    contractsPassed
                    && missingPassed
                    && writeReadPassed
                    && manifestPassed
                    && corruptPassed
                    && deletePassed
                    && boundaryPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Progression Save JSON Backend Diagnostics Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field(
                            "source",
                            normalizedSource),
                        LogFields.Field(
                            "exception",
                            exception.GetType().Name),
                        LogFields.Field(
                            "message",
                            exception.Message)));

                return Task.FromResult(false);
            }
            finally
            {
                Cleanup(store);
            }
        }

        private static bool ValidateContracts(
            FrameworkLogger logger,
            string source,
            JsonProgressionSaveStore store,
            IProgressionSaveStore coreStore,
            IProgressionSaveCatalog catalog,
            ProgressionSaveSlotId primarySlot)
        {
            string physicalPath =
                store.ToPhysicalSlotPath(
                    primarySlot);

            bool passed =
                store.BackendId.IsValid
                && store.BackendId.StableText ==
                    "ProgressionSave:json.qa"
                && ReferenceEquals(
                    store,
                    coreStore)
                && ReferenceEquals(
                    store,
                    catalog)
                && !string.IsNullOrWhiteSpace(
                    store.RootDirectory)
                && !string.IsNullOrWhiteSpace(
                    store.SlotDirectory)
                && !string.IsNullOrWhiteSpace(
                    store.ManifestPath)
                && primarySlot.StableText ==
                    "ProgressionSave:qa.slot.primary"
                && !physicalPath.Contains(
                    primarySlot.StableText)
                && physicalPath.EndsWith(
                    ".json",
                    StringComparison.OrdinalIgnoreCase);

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "source",
                        source),
                    LogFields.Field(
                        "backend",
                        nameof(JsonProgressionSaveStore)),
                    LogFields.Field(
                        "storePort",
                        nameof(IProgressionSaveStore)),
                    LogFields.Field(
                        "catalogPort",
                        nameof(IProgressionSaveCatalog)),
                    LogFields.Field(
                        "backendId",
                        store.BackendId.StableText),
                    LogFields.Field(
                        "storageFormatVersion",
                        JsonProgressionSaveStore.StorageFormatVersion)));

            return passed;
        }

        private static bool ValidateMissing(
            FrameworkLogger logger,
            IProgressionSaveStore store,
            IProgressionSaveCatalog catalog,
            ProgressionSaveSlotId missingSlot)
        {
            ProgressionSaveManifestReadResult manifestRead =
                catalog.ReadManifest();

            ProgressionSaveReadResult slotRead =
                store.ReadSlot(missingSlot);

            ProgressionSaveDeleteResult delete =
                store.DeleteSlot(missingSlot);

            bool passed =
                manifestRead.Status ==
                    ProgressionSaveReadStatus.Missing
                && !manifestRead.HasManifest
                && slotRead.Status ==
                    ProgressionSaveReadStatus.Missing
                && !slotRead.HasRecord
                && delete.Status ==
                    ProgressionSaveDeleteStatus.Missing;

            LogStep(
                logger,
                "missing",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "manifestStatus",
                        manifestRead.Status.ToString()),
                    LogFields.Field(
                        "slotStatus",
                        slotRead.Status.ToString()),
                    LogFields.Field(
                        "deleteStatus",
                        delete.Status.ToString())));

            return passed;
        }

        private static bool ValidateWriteRead(
            FrameworkLogger logger,
            IProgressionSaveStore store,
            ProgressionSaveSlotId primarySlot,
            out ProgressionSaveSlotRecord record)
        {
            record =
                CreateRecord(
                    primarySlot,
                    "qa.record.primary",
                    "QA Primary Slot",
                    "write-read");

            ProgressionSaveWriteResult write =
                store.WriteSlot(record);

            ProgressionSaveReadResult read =
                store.ReadSlot(primarySlot);

            bool stored =
                read.Status ==
                    ProgressionSaveReadStatus.Found
                && read.HasRecord;

            bool passed =
                write.Written
                && stored
                && read.Record == record;

            LogStep(
                logger,
                "write-read",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "writeStatus",
                        write.Status.ToString()),
                    LogFields.Field(
                        "readStatus",
                        read.Status.ToString()),
                    LogFields.Field(
                        "stored",
                        stored),
                    LogFields.Field(
                        "slot",
                        primarySlot.StableText),
                    LogFields.Field(
                        "record",
                        record.RecordId.StableText),
                    LogFields.Field(
                        "payloadBytes",
                        record.Payload.ByteCount)));

            return passed;
        }

        private static bool ValidateManifest(
            FrameworkLogger logger,
            IProgressionSaveCatalog catalog,
            ProgressionSaveSlotId primarySlot,
            ProgressionSaveSlotRecord record)
        {
            ProgressionSaveManifestReadResult manifestRead =
                catalog.ReadManifest();

            ProgressionSaveManifestEntry entry =
                default;

            bool hasEntry =
                manifestRead.HasManifest
                && manifestRead.Manifest.TryGetEntry(
                    primarySlot,
                    out entry);

            bool entryMatchesRecord =
                hasEntry
                && entry.RecordId ==
                    record.RecordId
                && entry.PayloadFormat ==
                    record.Payload.Format
                && entry.PayloadByteCount ==
                    record.Payload.ByteCount;

            bool passed =
                manifestRead.Status ==
                    ProgressionSaveReadStatus.Found
                && manifestRead.HasManifest
                && manifestRead.Manifest.Count == 1
                && hasEntry
                && entryMatchesRecord;

            LogStep(
                logger,
                "catalog-projection",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "catalogPort",
                        nameof(IProgressionSaveCatalog)),
                    LogFields.Field(
                        "manifestStatus",
                        manifestRead.Status.ToString()),
                    LogFields.Field(
                        "entries",
                        manifestRead.HasManifest
                            ? manifestRead.Manifest.Count
                            : 0),
                    LogFields.Field(
                        "hasEntry",
                        hasEntry),
                    LogFields.Field(
                        "entryMatchesRecord",
                        entryMatchesRecord),
                    LogFields.Field(
                        "manifestMutation",
                        "backend-internal")));

            return passed;
        }

        private static bool ValidateCorruptSlot(
            FrameworkLogger logger,
            JsonProgressionSaveStore store,
            ProgressionSaveSlotId corruptSlot)
        {
            Directory.CreateDirectory(
                store.SlotDirectory);

            string corruptPath =
                store.ToPhysicalSlotPath(
                    corruptSlot);

            File.WriteAllText(
                corruptPath,
                "{ not-valid-json",
                Encoding.UTF8);

            ProgressionSaveReadResult read =
                store.ReadSlot(corruptSlot);

            bool physicalArtifactExists =
                File.Exists(corruptPath);

            bool passed =
                read.Status ==
                    ProgressionSaveReadStatus.Corrupt
                && read.Failed
                && !read.HasRecord
                && physicalArtifactExists;

            LogStep(
                logger,
                "corrupt-slot",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "readStatus",
                        read.Status.ToString()),
                    LogFields.Field(
                        "failed",
                        read.Failed),
                    LogFields.Field(
                        "hasRecord",
                        read.HasRecord),
                    LogFields.Field(
                        "physicalArtifactExists",
                        physicalArtifactExists)));

            return passed;
        }

        private static bool ValidateDelete(
            FrameworkLogger logger,
            JsonProgressionSaveStore jsonStore,
            IProgressionSaveStore store,
            IProgressionSaveCatalog catalog,
            ProgressionSaveSlotId primarySlot,
            ProgressionSaveSlotId corruptSlot)
        {
            ProgressionSaveDeleteResult primaryDelete =
                store.DeleteSlot(primarySlot);

            ProgressionSaveDeleteResult corruptDelete =
                store.DeleteSlot(corruptSlot);

            ProgressionSaveReadResult primaryRead =
                store.ReadSlot(primarySlot);

            ProgressionSaveReadResult corruptRead =
                store.ReadSlot(corruptSlot);

            ProgressionSaveManifestReadResult manifestRead =
                catalog.ReadManifest();

            bool manifestHasPrimary =
                manifestRead.HasManifest
                && manifestRead.Manifest.ContainsSlot(
                    primarySlot);

            bool primaryPhysicalMissing =
                !File.Exists(
                    jsonStore.ToPhysicalSlotPath(
                        primarySlot));

            bool corruptPhysicalMissing =
                !File.Exists(
                    jsonStore.ToPhysicalSlotPath(
                        corruptSlot));

            bool passed =
                primaryDelete.Status ==
                    ProgressionSaveDeleteStatus.Deleted
                && corruptDelete.Status ==
                    ProgressionSaveDeleteStatus.Deleted
                && primaryRead.Status ==
                    ProgressionSaveReadStatus.Missing
                && corruptRead.Status ==
                    ProgressionSaveReadStatus.Missing
                && manifestRead.Status ==
                    ProgressionSaveReadStatus.Found
                && manifestRead.HasManifest
                && !manifestHasPrimary
                && primaryPhysicalMissing
                && corruptPhysicalMissing;

            LogStep(
                logger,
                "delete-cleanup",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "primaryDelete",
                        primaryDelete.Status.ToString()),
                    LogFields.Field(
                        "corruptDelete",
                        corruptDelete.Status.ToString()),
                    LogFields.Field(
                        "primaryRead",
                        primaryRead.Status.ToString()),
                    LogFields.Field(
                        "corruptRead",
                        corruptRead.Status.ToString()),
                    LogFields.Field(
                        "manifestStatus",
                        manifestRead.Status.ToString()),
                    LogFields.Field(
                        "manifestHasPrimary",
                        manifestHasPrimary),
                    LogFields.Field(
                        "primaryPhysicalMissing",
                        primaryPhysicalMissing),
                    LogFields.Field(
                        "corruptPhysicalMissing",
                        corruptPhysicalMissing)));

            return passed;
        }

        private static bool ValidateBoundary(
            FrameworkLogger logger,
            JsonProgressionSaveStore store)
        {
            bool passed =
                store.BackendId.StableText ==
                    "ProgressionSave:json.qa"
                && store is IProgressionSaveStore
                && store is IProgressionSaveCatalog;

            LogStep(
                logger,
                "canonical-boundary",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "namespace",
                        "Immersive.Framework.ProgressionSave"),
                    LogFields.Field(
                        "backend",
                        nameof(JsonProgressionSaveStore)),
                    LogFields.Field(
                        "storePort",
                        nameof(IProgressionSaveStore)),
                    LogFields.Field(
                        "catalogPort",
                        nameof(IProgressionSaveCatalog)),
                    LogFields.Field(
                        "manifestMutation",
                        "backend-internal"),
                    LogFields.Field(
                        "snapshot",
                        "none"),
                    LogFields.Field(
                        "preferences",
                        "none"),
                    LogFields.Field(
                        "ui",
                        "none")));

            return passed;
        }

        private static ProgressionSaveSlotRecord CreateRecord(
            ProgressionSaveSlotId slotId,
            string recordValue,
            string displayName,
            string reason)
        {
            long now =
                DateTime.UtcNow.Ticks;

            string payloadJson =
                "{\"level\":2,\"checkpoint\":\"qa\"}";

            var payload =
                ProgressionSavePayload.FromBytes(
                    ProgressionSavePayloadFormat.Structured,
                    Encoding.UTF8.GetBytes(
                        payloadJson),
                    "application/json");

            return new ProgressionSaveSlotRecord(
                slotId,
                ProgressionSaveRecordId.From(
                    recordValue),
                payload,
                now,
                now,
                displayName,
                nameof(ProgressionSaveQaSmokeRunner),
                reason);
        }

        private static void Cleanup(
            JsonProgressionSaveStore store)
        {
            if (store == null)
            {
                return;
            }

            try
            {
                store.DeleteStoreData();
            }
            catch
            {
                // QA cleanup must not mask the smoke result.
            }
        }

        private static void LogStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            LogField[] fields)
        {
            LogField[] allFields =
                AppendFields(
                    LogFields.Of(
                        LogFields.Field(
                            "step",
                            step),
                        LogFields.Field(
                            "passed",
                            passed)),
                    fields);

            if (passed)
            {
                logger.Info(
                    "QA Progression Save JSON Backend Diagnostics Smoke step completed.",
                    allFields);

                return;
            }

            logger.Warning(
                "QA Progression Save JSON Backend Diagnostics Smoke step failed.",
                allFields);
        }

        private static LogField[] AppendFields(
            LogField[] baseFields,
            LogField[] additionalFields)
        {
            if (additionalFields == null
                || additionalFields.Length == 0)
            {
                return baseFields;
            }

            var combined =
                new LogField[
                    baseFields.Length
                    + additionalFields.Length];

            Array.Copy(
                baseFields,
                combined,
                baseFields.Length);

            Array.Copy(
                additionalFields,
                0,
                combined,
                baseFields.Length,
                additionalFields.Length);

            return combined;
        }
    }
}
#endif
