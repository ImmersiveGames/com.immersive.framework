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
    /// API status: Development Tooling. Synthetic smoke for F21G Progression Save runtime request path.
    /// It uses a temporary JSON store only as an adapter behind IProgressionSaveStore.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F21G Progression Save runtime request diagnostics smoke.")]
    internal static class ProgressionSaveRuntimeQaSmokeRunner
    {
        internal const string SmokeName = "Progression Save Runtime Request Smoke";
        private const string QaRootName = "ProgressionSaveRuntimeSmoke";

        internal static Task<bool> RunRuntimeRequestSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(ProgressionSaveRuntimeQaSmokeRunner));
            string rootDirectory = Path.Combine(Application.temporaryCachePath, "ImmersiveFramework", "Qa", QaRootName);
            var store = new JsonProgressionSaveStore(rootDirectory, ProgressionSaveBackendId.From("json.runtime.qa"));
            var runtime = new ProgressionSaveRuntime(store);
            var primarySlot = ProgressionSaveSlotId.From("qa.runtime.slot.primary");
            var autosaveSlot = ProgressionSaveSlotId.From("qa.runtime.slot.autosave");
            var missingSlot = ProgressionSaveSlotId.From("qa.runtime.slot.missing");

            Cleanup(store);

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource, runtime, primarySlot);
                bool savePassed = ValidateManualSave(logger, runtime, primarySlot, out var savedRecord);
                bool loadPassed = ValidateLoad(logger, runtime, primarySlot, savedRecord);
                bool autosaveMomentPassed = ValidateAutosaveMoment(logger, runtime, autosaveSlot);
                bool missingPassed = ValidateMissingLoad(logger, runtime, missingSlot);
                bool deletePassed = ValidateDelete(logger, runtime, primarySlot, autosaveSlot);
                bool boundaryPassed = ValidateBoundary(logger, runtime);

                return Task.FromResult(contractsPassed
                    && savePassed
                    && loadPassed
                    && autosaveMomentPassed
                    && missingPassed
                    && deletePassed
                    && boundaryPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Progression Save Runtime Request Smoke failed with exception.",
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
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId primarySlot)
        {
            var manualMoment = ProgressionSaveMoment.Manual("qa.runtime.moment.manual", source, "qa.manual.save");
            var autosaveMoment = ProgressionSaveMoment.Autosave("qa.runtime.moment.autosave", source, "qa.autosave.save");
            var request = CreateSaveRequest(
                "qa.runtime.request.contracts.save",
                primarySlot,
                "qa.runtime.record.contracts",
                "QA Runtime Contracts",
                manualMoment,
                source,
                "contracts");

            bool passed = runtime.BackendId.StableText == "ProgressionSave:json.runtime.qa"
                && request is { IsValid: true, Kind: ProgressionSaveRequestKind.Save, Moment: { IsManual: true } }
                && manualMoment.IsManual
                && autosaveMoment.IsAutosave
                && request.RequestId.StableText == "ProgressionSave:qa.runtime.request.contracts.save"
                && primarySlot.StableText == "ProgressionSave:qa.runtime.slot.primary";

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("runtime", nameof(ProgressionSaveRuntime)),
                    LogFields.Field("storePort", nameof(IProgressionSaveStore)),
                    LogFields.Field("backend", runtime.BackendId.StableText),
                    LogFields.Field("request", request.RequestId.StableText),
                    LogFields.Field("slot", primarySlot.StableText),
                    LogFields.Field("manualMoment", manualMoment.Kind.ToString()),
                    LogFields.Field("autosaveMoment", autosaveMoment.Kind.ToString())));

            return passed;
        }

        private static bool ValidateManualSave(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId primarySlot,
            out ProgressionSaveSlotRecord savedRecord)
        {
            var request = CreateSaveRequest(
                "qa.runtime.request.manual.save",
                primarySlot,
                "qa.runtime.record.manual",
                "QA Runtime Manual Save",
                ProgressionSaveMoment.Manual("qa.runtime.moment.manual.save", nameof(ProgressionSaveRuntimeQaSmokeRunner), "qa.manual.save"),
                nameof(ProgressionSaveRuntimeQaSmokeRunner),
                "manual-save");

            var result = runtime.Request(request);
            savedRecord = result.HasRecord ? result.Record : default;
            bool contains = runtime.Store.ContainsSlot(primarySlot);

            bool passed = result is { Status: ProgressionSaveRequestStatus.Saved, Completed: true, Failed: false, HasRecord: true }
                && savedRecord.IsValid
                && savedRecord.SlotId == primarySlot
                && savedRecord.RecordId == request.RecordId
                && contains;

            LogStep(
                logger,
                "manual-save-request",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("completed", result.Completed),
                    LogFields.Field("failed", result.Failed),
                    LogFields.Field("hasRecord", result.HasRecord),
                    LogFields.Field("momentKind", result.Moment.Kind.ToString()),
                    LogFields.Field("contains", contains),
                    LogFields.Field("slot", primarySlot.StableText),
                    LogFields.Field("record", result.HasRecord ? result.Record.RecordId.StableText : "<none>")));

            return passed;
        }

        private static bool ValidateLoad(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId primarySlot,
            ProgressionSaveSlotRecord savedRecord)
        {
            var request = ProgressionSaveRequest.Load(
                "qa.runtime.request.load",
                primarySlot,
                ProgressionSaveMoment.Manual("qa.runtime.moment.manual.load", nameof(ProgressionSaveRuntimeQaSmokeRunner), "qa.manual.load"),
                nameof(ProgressionSaveRuntimeQaSmokeRunner),
                "manual-load");

            var result = runtime.Request(request);
            bool recordMatches = result.HasRecord && savedRecord.IsValid && result.Record == savedRecord;

            bool passed = result is { Status: ProgressionSaveRequestStatus.Loaded, Completed: true, Failed: false, HasRecord: true }
                && recordMatches;

            LogStep(
                logger,
                "load-request",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("completed", result.Completed),
                    LogFields.Field("failed", result.Failed),
                    LogFields.Field("hasRecord", result.HasRecord),
                    LogFields.Field("recordMatches", recordMatches),
                    LogFields.Field("slot", primarySlot.StableText)));

            return passed;
        }

        private static bool ValidateAutosaveMoment(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId autosaveSlot)
        {
            var request = CreateSaveRequest(
                "qa.runtime.request.autosave.save",
                autosaveSlot,
                "qa.runtime.record.autosave",
                "QA Runtime Autosave Moment",
                ProgressionSaveMoment.Autosave("qa.runtime.moment.autosave.save", nameof(ProgressionSaveRuntimeQaSmokeRunner), "qa.autosave.save"),
                nameof(ProgressionSaveRuntimeQaSmokeRunner),
                "autosave-moment");

            var result = runtime.Request(request);
            var readBack = runtime.Request(ProgressionSaveRequest.Load(
                "qa.runtime.request.autosave.load",
                autosaveSlot,
                ProgressionSaveMoment.Manual("qa.runtime.moment.autosave.verify", nameof(ProgressionSaveRuntimeQaSmokeRunner), "qa.autosave.verify"),
                nameof(ProgressionSaveRuntimeQaSmokeRunner),
                "autosave-verify"));

            bool passed = result is { Status: ProgressionSaveRequestStatus.Saved, Moment: { IsAutosave: true }, HasRecord: true }
                && readBack is { Status: ProgressionSaveRequestStatus.Loaded, HasRecord: true }
                && readBack.Record.RecordId == request.RecordId;

            LogStep(
                logger,
                "autosave-moment-contract",
                passed,
                LogFields.Of(
                    LogFields.Field("saveStatus", result.Status.ToString()),
                    LogFields.Field("momentKind", result.Moment.Kind.ToString()),
                    LogFields.Field("isAutosave", result.Moment.IsAutosave),
                    LogFields.Field("loadStatus", readBack.Status.ToString()),
                    LogFields.Field("hasRecord", readBack.HasRecord),
                    LogFields.Field("scheduler", "none"),
                    LogFields.Field("lifecycleHook", "none")));

            return passed;
        }

        private static bool ValidateMissingLoad(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId missingSlot)
        {
            var request = ProgressionSaveRequest.Load(
                "qa.runtime.request.missing.load",
                missingSlot,
                ProgressionSaveMoment.Manual("qa.runtime.moment.missing.load", nameof(ProgressionSaveRuntimeQaSmokeRunner), "qa.missing.load"),
                nameof(ProgressionSaveRuntimeQaSmokeRunner),
                "missing-load");

            var result = runtime.Request(request);
            bool passed = result is { Status: ProgressionSaveRequestStatus.Missing, Completed: true, Failed: false, HasRecord: false };

            LogStep(
                logger,
                "missing-load-request",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("completed", result.Completed),
                    LogFields.Field("failed", result.Failed),
                    LogFields.Field("hasRecord", result.HasRecord),
                    LogFields.Field("slot", missingSlot.StableText)));

            return passed;
        }

        private static bool ValidateDelete(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId primarySlot,
            ProgressionSaveSlotId autosaveSlot)
        {
            var primaryDelete = runtime.Request(ProgressionSaveRequest.Delete(
                "qa.runtime.request.primary.delete",
                primarySlot,
                ProgressionSaveMoment.Manual("qa.runtime.moment.primary.delete", nameof(ProgressionSaveRuntimeQaSmokeRunner), "qa.primary.delete"),
                nameof(ProgressionSaveRuntimeQaSmokeRunner),
                "delete-primary"));
            var autosaveDelete = runtime.Request(ProgressionSaveRequest.Delete(
                "qa.runtime.request.autosave.delete",
                autosaveSlot,
                ProgressionSaveMoment.Manual("qa.runtime.moment.autosave.delete", nameof(ProgressionSaveRuntimeQaSmokeRunner), "qa.autosave.delete"),
                nameof(ProgressionSaveRuntimeQaSmokeRunner),
                "delete-autosave"));

            var primaryRead = runtime.Request(ProgressionSaveRequest.Load(
                "qa.runtime.request.primary.verify-delete",
                primarySlot,
                ProgressionSaveMoment.Manual("qa.runtime.moment.primary.verify-delete", nameof(ProgressionSaveRuntimeQaSmokeRunner), "qa.primary.verify-delete"),
                nameof(ProgressionSaveRuntimeQaSmokeRunner),
                "verify-delete-primary"));
            var autosaveRead = runtime.Request(ProgressionSaveRequest.Load(
                "qa.runtime.request.autosave.verify-delete",
                autosaveSlot,
                ProgressionSaveMoment.Manual("qa.runtime.moment.autosave.verify-delete", nameof(ProgressionSaveRuntimeQaSmokeRunner), "qa.autosave.verify-delete"),
                nameof(ProgressionSaveRuntimeQaSmokeRunner),
                "verify-delete-autosave"));

            bool passed = primaryDelete.Status == ProgressionSaveRequestStatus.Deleted
                && autosaveDelete.Status == ProgressionSaveRequestStatus.Deleted
                && primaryRead.Status == ProgressionSaveRequestStatus.Missing
                && autosaveRead.Status == ProgressionSaveRequestStatus.Missing
                && !runtime.Store.ContainsSlot(primarySlot)
                && !runtime.Store.ContainsSlot(autosaveSlot);

            LogStep(
                logger,
                "delete-request-cleanup",
                passed,
                LogFields.Of(
                    LogFields.Field("primaryDelete", primaryDelete.Status.ToString()),
                    LogFields.Field("autosaveDelete", autosaveDelete.Status.ToString()),
                    LogFields.Field("primaryRead", primaryRead.Status.ToString()),
                    LogFields.Field("autosaveRead", autosaveRead.Status.ToString()),
                    LogFields.Field("containsPrimary", runtime.Store.ContainsSlot(primarySlot)),
                    LogFields.Field("containsAutosave", runtime.Store.ContainsSlot(autosaveSlot))));

            return passed;
        }

        private static bool ValidateBoundary(FrameworkLogger logger, ProgressionSaveRuntime runtime)
        {
            bool passed = runtime.BackendId.StableText == "ProgressionSave:json.runtime.qa";

            LogStep(
                logger,
                "canonical-boundary",
                passed,
                LogFields.Of(
                    LogFields.Field("namespace", "Immersive.Framework.ProgressionSave"),
                    LogFields.Field("runtime", nameof(ProgressionSaveRuntime)),
                    LogFields.Field("storePort", nameof(IProgressionSaveStore)),
                    LogFields.Field("backend", nameof(JsonProgressionSaveStore)),
                    LogFields.Field("snapshotCapture", "none"),
                    LogFields.Field("preferences", "none"),
                    LogFields.Field("playerPrefs", "none"),
                    LogFields.Field("routeActivityHook", "none"),
                    LogFields.Field("autosaveScheduler", "none"),
                    LogFields.Field("ui", "none")));

            return passed;
        }

        private static ProgressionSaveRequest CreateSaveRequest(
            string requestValue,
            ProgressionSaveSlotId slotId,
            string recordValue,
            string displayName,
            ProgressionSaveMoment moment,
            string source,
            string reason)
        {
            return ProgressionSaveRequest.Save(
                requestValue,
                slotId,
                ProgressionSaveRecordId.From(recordValue),
                CreatePayload(reason),
                displayName,
                moment,
                source,
                reason);
        }

        private static ProgressionSavePayload CreatePayload(string reason)
        {
            string payloadJson = $"{{\"level\":3,\"reason\":\"{reason}\"}}";
            return ProgressionSavePayload.FromBytes(
                ProgressionSavePayloadFormat.Structured,
                Encoding.UTF8.GetBytes(payloadJson),
                "application/json");
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
                logger.Info("QA Progression Save Runtime Request Smoke step completed.", allFields);
                return;
            }

            logger.Warning("QA Progression Save Runtime Request Smoke step failed.", allFields);
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
