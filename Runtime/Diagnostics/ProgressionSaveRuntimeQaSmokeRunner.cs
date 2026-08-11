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
    /// API status: Development Tooling. Synthetic smoke for the Progression Save
    /// runtime request path.
    ///
    /// The smoke consumes only the core IProgressionSaveStore contract through
    /// ProgressionSaveRuntime. It does not depend on catalog or manifest maintenance.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.DevelopmentTooling,
        "ADR018-A Progression Save runtime request diagnostics smoke using the core backend contract.")]
    internal static class ProgressionSaveRuntimeQaSmokeRunner
    {
        internal const string SmokeName =
            "Progression Save Runtime Request Smoke";

        private const string QaRootName =
            "ProgressionSaveRuntimeSmoke";

        internal static Task<bool> RunRuntimeRequestSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource =
                source.NormalizeTextOrFallback(
                    nameof(ProgressionSaveRuntimeQaSmokeRunner));

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
                        "json.runtime.qa"));

            var runtime =
                new ProgressionSaveRuntime(store);

            var primarySlot =
                ProgressionSaveSlotId.From(
                    "qa.runtime.slot.primary");

            var autosaveSlot =
                ProgressionSaveSlotId.From(
                    "qa.runtime.slot.autosave");

            var missingSlot =
                ProgressionSaveSlotId.From(
                    "qa.runtime.slot.missing");

            Cleanup(store);

            try
            {
                bool contractsPassed =
                    ValidateContracts(
                        logger,
                        normalizedSource,
                        runtime,
                        primarySlot);

                bool savePassed =
                    ValidateManualSave(
                        logger,
                        runtime,
                        primarySlot,
                        out ProgressionSaveSlotRecord savedRecord);

                bool loadPassed =
                    ValidateLoad(
                        logger,
                        runtime,
                        primarySlot,
                        savedRecord);

                bool autosavePassed =
                    ValidateAutosaveMoment(
                        logger,
                        runtime,
                        autosaveSlot);

                bool missingPassed =
                    ValidateMissingLoad(
                        logger,
                        runtime,
                        missingSlot);

                bool deletePassed =
                    ValidateDelete(
                        logger,
                        runtime,
                        primarySlot,
                        autosaveSlot);

                bool boundaryPassed =
                    ValidateBoundary(
                        logger,
                        runtime);

                return Task.FromResult(
                    contractsPassed
                    && savePassed
                    && loadPassed
                    && autosavePassed
                    && missingPassed
                    && deletePassed
                    && boundaryPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Progression Save Runtime Request Smoke failed with exception.",
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
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId primarySlot)
        {
            var manualMoment =
                ProgressionSaveMoment.Manual(
                    "qa.runtime.moment.manual",
                    source,
                    "qa.manual.save");

            var request =
                CreateSaveRequest(
                    "qa.runtime.request.contracts.save",
                    primarySlot,
                    "qa.runtime.record.contracts",
                    "QA Runtime Contracts",
                    manualMoment,
                    source,
                    "contracts");

            bool passed =
                runtime.BackendId.StableText ==
                    "ProgressionSave:json.runtime.qa"
                && ReferenceEquals(
                    runtime.Store,
                    runtime.Store)
                && request.IsValid
                && request.Kind ==
                    ProgressionSaveRequestKind.Save
                && request.Moment.IsManual
                && request.RequestId.StableText ==
                    "ProgressionSave:qa.runtime.request.contracts.save"
                && primarySlot.StableText ==
                    "ProgressionSave:qa.runtime.slot.primary";

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "source",
                        source),
                    LogFields.Field(
                        "runtime",
                        nameof(ProgressionSaveRuntime)),
                    LogFields.Field(
                        "storePort",
                        nameof(IProgressionSaveStore)),
                    LogFields.Field(
                        "backend",
                        runtime.BackendId.StableText),
                    LogFields.Field(
                        "catalogRequired",
                        false)));

            return passed;
        }

        private static bool ValidateManualSave(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId primarySlot,
            out ProgressionSaveSlotRecord savedRecord)
        {
            var request =
                CreateSaveRequest(
                    "qa.runtime.request.manual.save",
                    primarySlot,
                    "qa.runtime.record.manual",
                    "QA Runtime Manual Save",
                    ProgressionSaveMoment.Manual(
                        "qa.runtime.moment.manual.save",
                        nameof(ProgressionSaveRuntimeQaSmokeRunner),
                        "qa.manual.save"),
                    nameof(ProgressionSaveRuntimeQaSmokeRunner),
                    "manual-save");

            ProgressionSaveRequestResult result =
                runtime.Request(request);

            savedRecord =
                result.HasRecord
                    ? result.Record
                    : default;

            ProgressionSaveReadResult stored =
                runtime.Store.ReadSlot(primarySlot);

            bool storedMatches =
                stored.Status ==
                    ProgressionSaveReadStatus.Found
                && stored.HasRecord
                && savedRecord.IsValid
                && stored.Record == savedRecord;

            bool passed =
                result.Status ==
                    ProgressionSaveRequestStatus.Saved
                && result.Completed
                && !result.Failed
                && result.HasRecord
                && savedRecord.IsValid
                && savedRecord.SlotId ==
                    primarySlot
                && savedRecord.RecordId ==
                    request.RecordId
                && storedMatches;

            LogStep(
                logger,
                "manual-save-request",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "status",
                        result.Status.ToString()),
                    LogFields.Field(
                        "storedStatus",
                        stored.Status.ToString()),
                    LogFields.Field(
                        "storedMatches",
                        storedMatches),
                    LogFields.Field(
                        "slot",
                        primarySlot.StableText),
                    LogFields.Field(
                        "record",
                        result.HasRecord
                            ? result.Record.RecordId.StableText
                            : "<none>")));

            return passed;
        }

        private static bool ValidateLoad(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId primarySlot,
            ProgressionSaveSlotRecord savedRecord)
        {
            var request =
                ProgressionSaveRequest.Load(
                    "qa.runtime.request.load",
                    primarySlot,
                    ProgressionSaveMoment.Manual(
                        "qa.runtime.moment.manual.load",
                        nameof(ProgressionSaveRuntimeQaSmokeRunner),
                        "qa.manual.load"),
                    nameof(ProgressionSaveRuntimeQaSmokeRunner),
                    "manual-load");

            ProgressionSaveRequestResult result =
                runtime.Request(request);

            bool recordMatches =
                result.HasRecord
                && savedRecord.IsValid
                && result.Record == savedRecord;

            bool passed =
                result.Status ==
                    ProgressionSaveRequestStatus.Loaded
                && result.Completed
                && !result.Failed
                && result.HasRecord
                && recordMatches;

            LogStep(
                logger,
                "load-request",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "status",
                        result.Status.ToString()),
                    LogFields.Field(
                        "recordMatches",
                        recordMatches),
                    LogFields.Field(
                        "slot",
                        primarySlot.StableText)));

            return passed;
        }

        private static bool ValidateAutosaveMoment(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId autosaveSlot)
        {
            var request =
                CreateSaveRequest(
                    "qa.runtime.request.autosave.save",
                    autosaveSlot,
                    "qa.runtime.record.autosave",
                    "QA Runtime Autosave Moment",
                    ProgressionSaveMoment.Autosave(
                        "qa.runtime.moment.autosave.save",
                        nameof(ProgressionSaveRuntimeQaSmokeRunner),
                        "qa.autosave.save"),
                    nameof(ProgressionSaveRuntimeQaSmokeRunner),
                    "autosave-moment");

            ProgressionSaveRequestResult save =
                runtime.Request(request);

            ProgressionSaveRequestResult load =
                runtime.Request(
                    ProgressionSaveRequest.Load(
                        "qa.runtime.request.autosave.load",
                        autosaveSlot,
                        ProgressionSaveMoment.Manual(
                            "qa.runtime.moment.autosave.verify",
                            nameof(ProgressionSaveRuntimeQaSmokeRunner),
                            "qa.autosave.verify"),
                        nameof(ProgressionSaveRuntimeQaSmokeRunner),
                        "autosave-verify"));

            bool passed =
                save.Status ==
                    ProgressionSaveRequestStatus.Saved
                && save.Moment.IsAutosave
                && save.HasRecord
                && load.Status ==
                    ProgressionSaveRequestStatus.Loaded
                && load.HasRecord
                && load.Record.RecordId ==
                    request.RecordId;

            LogStep(
                logger,
                "autosave-moment-contract",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "saveStatus",
                        save.Status.ToString()),
                    LogFields.Field(
                        "loadStatus",
                        load.Status.ToString()),
                    LogFields.Field(
                        "isAutosave",
                        save.Moment.IsAutosave),
                    LogFields.Field(
                        "scheduler",
                        "none"),
                    LogFields.Field(
                        "lifecycleHook",
                        "none")));

            return passed;
        }

        private static bool ValidateMissingLoad(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId missingSlot)
        {
            ProgressionSaveRequestResult result =
                runtime.Request(
                    ProgressionSaveRequest.Load(
                        "qa.runtime.request.missing.load",
                        missingSlot,
                        ProgressionSaveMoment.Manual(
                            "qa.runtime.moment.missing.load",
                            nameof(ProgressionSaveRuntimeQaSmokeRunner),
                            "qa.missing.load"),
                        nameof(ProgressionSaveRuntimeQaSmokeRunner),
                        "missing-load"));

            bool passed =
                result.Status ==
                    ProgressionSaveRequestStatus.Missing
                && result.Completed
                && !result.Failed
                && !result.HasRecord;

            LogStep(
                logger,
                "missing-load-request",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "status",
                        result.Status.ToString()),
                    LogFields.Field(
                        "slot",
                        missingSlot.StableText)));

            return passed;
        }

        private static bool ValidateDelete(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime,
            ProgressionSaveSlotId primarySlot,
            ProgressionSaveSlotId autosaveSlot)
        {
            ProgressionSaveRequestResult primaryDelete =
                runtime.Request(
                    ProgressionSaveRequest.Delete(
                        "qa.runtime.request.primary.delete",
                        primarySlot,
                        ProgressionSaveMoment.Manual(
                            "qa.runtime.moment.primary.delete",
                            nameof(ProgressionSaveRuntimeQaSmokeRunner),
                            "qa.primary.delete"),
                        nameof(ProgressionSaveRuntimeQaSmokeRunner),
                        "delete-primary"));

            ProgressionSaveRequestResult autosaveDelete =
                runtime.Request(
                    ProgressionSaveRequest.Delete(
                        "qa.runtime.request.autosave.delete",
                        autosaveSlot,
                        ProgressionSaveMoment.Manual(
                            "qa.runtime.moment.autosave.delete",
                            nameof(ProgressionSaveRuntimeQaSmokeRunner),
                            "qa.autosave.delete"),
                        nameof(ProgressionSaveRuntimeQaSmokeRunner),
                        "delete-autosave"));

            ProgressionSaveRequestResult primaryRead =
                runtime.Request(
                    ProgressionSaveRequest.Load(
                        "qa.runtime.request.primary.verify-delete",
                        primarySlot,
                        ProgressionSaveMoment.Manual(
                            "qa.runtime.moment.primary.verify-delete",
                            nameof(ProgressionSaveRuntimeQaSmokeRunner),
                            "qa.primary.verify-delete"),
                        nameof(ProgressionSaveRuntimeQaSmokeRunner),
                        "verify-delete-primary"));

            ProgressionSaveRequestResult autosaveRead =
                runtime.Request(
                    ProgressionSaveRequest.Load(
                        "qa.runtime.request.autosave.verify-delete",
                        autosaveSlot,
                        ProgressionSaveMoment.Manual(
                            "qa.runtime.moment.autosave.verify-delete",
                            nameof(ProgressionSaveRuntimeQaSmokeRunner),
                            "qa.autosave.verify-delete"),
                        nameof(ProgressionSaveRuntimeQaSmokeRunner),
                        "verify-delete-autosave"));

            bool primaryMissing =
                primaryRead.Status ==
                    ProgressionSaveRequestStatus.Missing;

            bool autosaveMissing =
                autosaveRead.Status ==
                    ProgressionSaveRequestStatus.Missing;

            bool passed =
                primaryDelete.Status ==
                    ProgressionSaveRequestStatus.Deleted
                && autosaveDelete.Status ==
                    ProgressionSaveRequestStatus.Deleted
                && primaryMissing
                && autosaveMissing;

            LogStep(
                logger,
                "delete-request-cleanup",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "primaryDelete",
                        primaryDelete.Status.ToString()),
                    LogFields.Field(
                        "autosaveDelete",
                        autosaveDelete.Status.ToString()),
                    LogFields.Field(
                        "primaryRead",
                        primaryRead.Status.ToString()),
                    LogFields.Field(
                        "autosaveRead",
                        autosaveRead.Status.ToString()),
                    LogFields.Field(
                        "primaryMissing",
                        primaryMissing),
                    LogFields.Field(
                        "autosaveMissing",
                        autosaveMissing)));

            return passed;
        }

        private static bool ValidateBoundary(
            FrameworkLogger logger,
            ProgressionSaveRuntime runtime)
        {
            bool passed =
                runtime.BackendId.StableText ==
                    "ProgressionSave:json.runtime.qa"
                && runtime.Store != null;

            LogStep(
                logger,
                "canonical-boundary",
                passed,
                LogFields.Of(
                    LogFields.Field(
                        "namespace",
                        "Immersive.Framework.ProgressionSave"),
                    LogFields.Field(
                        "runtime",
                        nameof(ProgressionSaveRuntime)),
                    LogFields.Field(
                        "storePort",
                        nameof(IProgressionSaveStore)),
                    LogFields.Field(
                        "catalogRequired",
                        false),
                    LogFields.Field(
                        "snapshotCapture",
                        "none"),
                    LogFields.Field(
                        "routeActivityHook",
                        "none"),
                    LogFields.Field(
                        "ui",
                        "none")));

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
                ProgressionSaveRecordId.From(
                    recordValue),
                CreatePayload(reason),
                displayName,
                moment,
                source,
                reason);
        }

        private static ProgressionSavePayload CreatePayload(
            string reason)
        {
            string payloadJson =
                $"{{\"level\":3,\"reason\":\"{reason}\"}}";

            return ProgressionSavePayload.FromBytes(
                ProgressionSavePayloadFormat.Structured,
                Encoding.UTF8.GetBytes(payloadJson),
                "application/json");
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
                    "QA Progression Save Runtime Request Smoke step completed.",
                    allFields);

                return;
            }

            logger.Warning(
                "QA Progression Save Runtime Request Smoke step failed.",
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
