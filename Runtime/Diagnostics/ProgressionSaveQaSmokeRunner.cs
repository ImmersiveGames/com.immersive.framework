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
    /// API status: Development Tooling. Synthetic smoke for F21F JSON Progression Save backend.
    /// It writes only under Unity temporary cache QA storage and removes that storage before completion.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F21F JSON Progression Save backend diagnostics smoke.")]
    internal static class ProgressionSaveQaSmokeRunner
    {
        internal const string SmokeName = "Progression Save JSON Backend Diagnostics Smoke";
        private const string QaRootName = "ProgressionSaveJsonBackendSmoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(ProgressionSaveQaSmokeRunner));
            string rootDirectory = Path.Combine(Application.temporaryCachePath, "ImmersiveFramework", "Qa", QaRootName);
            var store = new JsonProgressionSaveStore(rootDirectory, ProgressionSaveBackendId.From("json.qa"));
            var primarySlot = ProgressionSaveSlotId.From("qa.slot.primary");
            var missingSlot = ProgressionSaveSlotId.From("qa.slot.missing");
            var corruptSlot = ProgressionSaveSlotId.From("qa.slot.corrupt");

            Cleanup(store);

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource, store, primarySlot);
                bool missingPassed = ValidateMissing(logger, store, missingSlot);
                bool writeReadPassed = ValidateWriteRead(logger, store, primarySlot, out var record);
                bool manifestPassed = ValidateManifest(logger, store, primarySlot, record);
                bool corruptPassed = ValidateCorruptSlot(logger, store, corruptSlot);
                bool deletePassed = ValidateDelete(logger, store, primarySlot, corruptSlot);
                bool boundaryPassed = ValidateBoundary(logger, store);

                return Task.FromResult(contractsPassed
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
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
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
            ProgressionSaveSlotId primarySlot)
        {
            string physicalPath = store.ToPhysicalSlotPath(primarySlot);
            bool passed = store.BackendId is { IsValid: true, StableText: "ProgressionSave:json.qa" }
                && !string.IsNullOrWhiteSpace(store.RootDirectory)
                && !string.IsNullOrWhiteSpace(store.SlotDirectory)
                && !string.IsNullOrWhiteSpace(store.ManifestPath)
                && primarySlot.StableText == "ProgressionSave:qa.slot.primary"
                && !physicalPath.Contains(primarySlot.StableText)
                && physicalPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("backend", nameof(JsonProgressionSaveStore)),
                    LogFields.Field("backendId", store.BackendId.StableText),
                    LogFields.Field("slot", primarySlot.StableText),
                    LogFields.Field("physicalPathIsAdapterDetail", !physicalPath.Contains(primarySlot.StableText)),
                    LogFields.Field("storageFormatVersion", JsonProgressionSaveStore.StorageFormatVersion)));

            return passed;
        }

        private static bool ValidateMissing(
            FrameworkLogger logger,
            IProgressionSaveStore store,
            ProgressionSaveSlotId missingSlot)
        {
            var manifestRead = store.ReadManifest();
            var slotRead = store.ReadSlot(missingSlot);
            var delete = store.DeleteSlot(missingSlot);

            bool passed = manifestRead is { Status: ProgressionSaveReadStatus.Missing, HasManifest: false }
                && slotRead is { Status: ProgressionSaveReadStatus.Missing, HasRecord: false }
                && delete.Status == ProgressionSaveDeleteStatus.Missing
                && !store.ContainsSlot(missingSlot);

            LogStep(
                logger,
                "missing",
                passed,
                LogFields.Of(
                    LogFields.Field("manifestStatus", manifestRead.Status.ToString()),
                    LogFields.Field("slotStatus", slotRead.Status.ToString()),
                    LogFields.Field("deleteStatus", delete.Status.ToString()),
                    LogFields.Field("contains", store.ContainsSlot(missingSlot))));

            return passed;
        }

        private static bool ValidateWriteRead(
            FrameworkLogger logger,
            IProgressionSaveStore store,
            ProgressionSaveSlotId primarySlot,
            out ProgressionSaveSlotRecord record)
        {
            record = CreateRecord(primarySlot, "qa.record.primary", "QA Primary Slot", "write-read");
            var write = store.WriteSlot(record);
            var read = store.ReadSlot(primarySlot);
            bool contains = store.ContainsSlot(primarySlot);

            bool passed = write.Written
                && read is { Status: ProgressionSaveReadStatus.Found, HasRecord: true }
                && read.Record == record
                && contains;

            LogStep(
                logger,
                "write-read",
                passed,
                LogFields.Of(
                    LogFields.Field("writeStatus", write.Status.ToString()),
                    LogFields.Field("readStatus", read.Status.ToString()),
                    LogFields.Field("contains", contains),
                    LogFields.Field("slot", primarySlot.StableText),
                    LogFields.Field("record", record.RecordId.StableText),
                    LogFields.Field("payloadFormat", record.Payload.Format.ToString()),
                    LogFields.Field("payloadBytes", record.Payload.ByteCount)));

            return passed;
        }

        private static bool ValidateManifest(
            FrameworkLogger logger,
            IProgressionSaveStore store,
            ProgressionSaveSlotId primarySlot,
            ProgressionSaveSlotRecord record)
        {
            var manifestRead = store.ReadManifest();
            var entry = default(ProgressionSaveManifestEntry);
            bool hasEntry = manifestRead.HasManifest && manifestRead.Manifest.TryGetEntry(primarySlot, out entry);
            bool entryMatchesRecord = hasEntry
                && entry.RecordId == record.RecordId
                && entry.PayloadFormat == record.Payload.Format
                && entry.PayloadByteCount == record.Payload.ByteCount;

            var rewrite = manifestRead.HasManifest
                ? store.WriteManifest(manifestRead.Manifest)
                : ProgressionSaveManifestWriteResult.FailedResult("Manifest was not available for rewrite.");

            bool passed = manifestRead is { Status: ProgressionSaveReadStatus.Found, HasManifest: true, Manifest: { Count: 1 } }
                && hasEntry
                && entryMatchesRecord
                && rewrite.Written;

            LogStep(
                logger,
                "manifest",
                passed,
                LogFields.Of(
                    LogFields.Field("manifestStatus", manifestRead.Status.ToString()),
                    LogFields.Field("hasManifest", manifestRead.HasManifest),
                    LogFields.Field("entries", manifestRead.HasManifest ? manifestRead.Manifest.Count : 0),
                    LogFields.Field("hasEntry", hasEntry),
                    LogFields.Field("entryMatchesRecord", entryMatchesRecord),
                    LogFields.Field("rewriteStatus", rewrite.Status.ToString())));

            return passed;
        }

        private static bool ValidateCorruptSlot(
            FrameworkLogger logger,
            JsonProgressionSaveStore store,
            ProgressionSaveSlotId corruptSlot)
        {
            Directory.CreateDirectory(store.SlotDirectory);
            File.WriteAllText(store.ToPhysicalSlotPath(corruptSlot), "{ not-valid-json", Encoding.UTF8);

            var read = store.ReadSlot(corruptSlot);
            bool passed = read is { Status: ProgressionSaveReadStatus.Corrupt, Failed: true, HasRecord: false }
                && store.ContainsSlot(corruptSlot);

            LogStep(
                logger,
                "corrupt-slot",
                passed,
                LogFields.Of(
                    LogFields.Field("readStatus", read.Status.ToString()),
                    LogFields.Field("failed", read.Failed),
                    LogFields.Field("hasRecord", read.HasRecord),
                    LogFields.Field("contains", store.ContainsSlot(corruptSlot))));

            return passed;
        }

        private static bool ValidateDelete(
            FrameworkLogger logger,
            IProgressionSaveStore store,
            ProgressionSaveSlotId primarySlot,
            ProgressionSaveSlotId corruptSlot)
        {
            var primaryDelete = store.DeleteSlot(primarySlot);
            var corruptDelete = store.DeleteSlot(corruptSlot);
            var primaryRead = store.ReadSlot(primarySlot);
            var manifestRead = store.ReadManifest();
            bool manifestHasPrimary = manifestRead.HasManifest && manifestRead.Manifest.ContainsSlot(primarySlot);

            bool passed = primaryDelete.Status == ProgressionSaveDeleteStatus.Deleted
                && corruptDelete.Status == ProgressionSaveDeleteStatus.Deleted
                && primaryRead.Status == ProgressionSaveReadStatus.Missing
                && !store.ContainsSlot(primarySlot)
                && !store.ContainsSlot(corruptSlot)
                && manifestRead is { Status: ProgressionSaveReadStatus.Found, HasManifest: true }
                && !manifestHasPrimary;

            LogStep(
                logger,
                "delete-cleanup",
                passed,
                LogFields.Of(
                    LogFields.Field("primaryDelete", primaryDelete.Status.ToString()),
                    LogFields.Field("corruptDelete", corruptDelete.Status.ToString()),
                    LogFields.Field("primaryRead", primaryRead.Status.ToString()),
                    LogFields.Field("manifestStatus", manifestRead.Status.ToString()),
                    LogFields.Field("manifestEntries", manifestRead.HasManifest ? manifestRead.Manifest.Count : 0),
                    LogFields.Field("manifestHasPrimary", manifestHasPrimary),
                    LogFields.Field("containsPrimary", store.ContainsSlot(primarySlot)),
                    LogFields.Field("containsCorrupt", store.ContainsSlot(corruptSlot))));

            return passed;
        }

        private static bool ValidateBoundary(FrameworkLogger logger, JsonProgressionSaveStore store)
        {
            bool passed = store.BackendId.StableText == "ProgressionSave:json.qa";

            LogStep(
                logger,
                "canonical-boundary",
                passed,
                LogFields.Of(
                    LogFields.Field("namespace", "Immersive.Framework.ProgressionSave"),
                    LogFields.Field("backend", nameof(JsonProgressionSaveStore)),
                    LogFields.Field("storePort", nameof(IProgressionSaveStore)),
                    LogFields.Field("snapshot", "none"),
                    LogFields.Field("preferences", "none"),
                    LogFields.Field("playerPrefs", "none"),
                    LogFields.Field("runtimeRequestPath", "none"),
                    LogFields.Field("ui", "none")));

            return passed;
        }

        private static ProgressionSaveSlotRecord CreateRecord(
            ProgressionSaveSlotId slotId,
            string recordValue,
            string displayName,
            string reason)
        {
            long now = DateTime.UtcNow.Ticks;
            string payloadJson = "{\"level\":2,\"checkpoint\":\"qa\"}";
            var payload = ProgressionSavePayload.FromBytes(
                ProgressionSavePayloadFormat.Structured,
                Encoding.UTF8.GetBytes(payloadJson),
                "application/json");

            return new ProgressionSaveSlotRecord(
                slotId,
                ProgressionSaveRecordId.From(recordValue),
                payload,
                now,
                now,
                displayName,
                nameof(ProgressionSaveQaSmokeRunner),
                reason);
        }

        private static void Cleanup(JsonProgressionSaveStore store)
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
                // QA cleanup must not mask the actual smoke result.
            }
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            LogField[] allFields = AppendFields(
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("passed", passed)),
                fields);

            if (passed)
            {
                logger.Info("QA Progression Save JSON Backend Diagnostics Smoke step completed.", allFields);
                return;
            }

            logger.Warning("QA Progression Save JSON Backend Diagnostics Smoke step failed.", allFields);
        }

        private static LogField[] AppendFields(LogField[] baseFields, LogField[] additionalFields)
        {
            if (additionalFields == null || additionalFields.Length == 0)
            {
                return baseFields;
            }

            var combined = new LogField[baseFields.Length + additionalFields.Length];
            Array.Copy(baseFields, combined, baseFields.Length);
            Array.Copy(additionalFields, 0, combined, baseFields.Length, additionalFields.Length);
            return combined;
        }
    }
}
#endif
