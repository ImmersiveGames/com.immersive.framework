using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Preferences;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F21D Preferences store contracts and PlayerPrefs adapter.
    /// It writes only namespaced QA keys and deletes them before completion.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F21D Preferences diagnostics smoke; PlayerPrefs adapter only.")]
    internal static class PreferencesQaSmokeRunner
    {
        internal const string SmokeName = "Preferences Store Diagnostics Smoke";
        private const string QaPrefix = "immersive.framework.qa.preferences";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(PreferencesQaSmokeRunner));
            var store = new PlayerPrefsPreferencesStore(QaPrefix);
            var languageKey = PreferenceKey.From("qa.language");
            var volumeKey = PreferenceKey.From("qa.volume");
            var fullscreenKey = PreferenceKey.From("qa.fullscreen");
            var mismatchKey = PreferenceKey.From("qa.type-mismatch");
            var orphanKey = PreferenceKey.From("qa.orphan");
            var missingKey = PreferenceKey.From("qa.missing");

            Cleanup(store, languageKey, volumeKey, fullscreenKey, mismatchKey, orphanKey, missingKey);

            try
            {
                bool contractPassed = ValidateContracts(logger, normalizedSource, store, languageKey);
                bool writeReadPassed = ValidateWriteRead(logger, store, languageKey, volumeKey, fullscreenKey);
                bool missingPassed = ValidateMissing(logger, store, missingKey);
                bool mismatchPassed = ValidateTypeMismatch(logger, store, mismatchKey);
                bool markerGuardPassed = ValidateMarkerGuard(logger, store, orphanKey);
                bool deletePassed = ValidateDelete(logger, store, languageKey, volumeKey, fullscreenKey, mismatchKey, orphanKey, missingKey);
                bool boundaryPassed = ValidateBoundary(logger, store);

                return Task.FromResult(contractPassed
                    && writeReadPassed
                    && missingPassed
                    && mismatchPassed
                    && markerGuardPassed
                    && deletePassed
                    && boundaryPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Preferences Store Diagnostics Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
            finally
            {
                Cleanup(store, languageKey, volumeKey, fullscreenKey, mismatchKey, orphanKey, missingKey);
            }
        }

        private static bool ValidateContracts(
            FrameworkLogger logger,
            string source,
            PlayerPrefsPreferencesStore store,
            PreferenceKey languageKey)
        {
            var stringValue = PreferenceValue.FromString("pt-BR");
            var intValue = PreferenceValue.FromInt(75);
            var floatValue = PreferenceValue.FromFloat(0.75f);
            var boolValue = PreferenceValue.FromBool(true);

            bool passed = languageKey is { IsValid: true, StableText: "Preferences:qa.language" }
                && stringValue.Kind == PreferenceValueKind.String
                && intValue.Kind == PreferenceValueKind.Int
                && floatValue.Kind == PreferenceValueKind.Float
                && boolValue.Kind == PreferenceValueKind.Bool
                && stringValue.StringValue == "pt-BR"
                && intValue.IntValue == 75
                && Math.Abs(floatValue.FloatValue - 0.75f) < 0.0001f
                && boolValue.BoolValue
                && store.KeyPrefix == QaPrefix;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("key", languageKey.StableText),
                    LogFields.Field("domain", languageKey.Domain.ToString()),
                    LogFields.Field("stringKind", stringValue.Kind.ToString()),
                    LogFields.Field("intKind", intValue.Kind.ToString()),
                    LogFields.Field("floatKind", floatValue.Kind.ToString()),
                    LogFields.Field("boolKind", boolValue.Kind.ToString()),
                    LogFields.Field("backend", nameof(PlayerPrefsPreferencesStore))));

            return passed;
        }

        private static bool ValidateWriteRead(
            FrameworkLogger logger,
            IPreferencesStore store,
            PreferenceKey languageKey,
            PreferenceKey volumeKey,
            PreferenceKey fullscreenKey)
        {
            var languageWrite = store.Write(languageKey, PreferenceValue.FromString("pt-BR"));
            var volumeWrite = store.Write(volumeKey, PreferenceValue.FromFloat(0.75f));
            var fullscreenWrite = store.Write(fullscreenKey, PreferenceValue.FromBool(true));
            store.Flush();

            var languageRead = store.Read(languageKey, PreferenceValueKind.String);
            var volumeRead = store.Read(volumeKey, PreferenceValueKind.Float);
            var fullscreenRead = store.Read(fullscreenKey, PreferenceValueKind.Bool);

            bool passed = languageWrite.Written
                && volumeWrite.Written
                && fullscreenWrite.Written
                && languageRead.Found
                && volumeRead.Found
                && fullscreenRead.Found
                && languageRead.Value.StringValue == "pt-BR"
                && Math.Abs(volumeRead.Value.FloatValue - 0.75f) < 0.0001f
                && fullscreenRead.Value.BoolValue;

            LogStep(
                logger,
                "write-read",
                passed,
                LogFields.Of(
                    LogFields.Field("languageWrite", languageWrite.Status.ToString()),
                    LogFields.Field("volumeWrite", volumeWrite.Status.ToString()),
                    LogFields.Field("fullscreenWrite", fullscreenWrite.Status.ToString()),
                    LogFields.Field("languageRead", languageRead.Status.ToString()),
                    LogFields.Field("volumeRead", volumeRead.Status.ToString()),
                    LogFields.Field("fullscreenRead", fullscreenRead.Status.ToString()),
                    LogFields.Field("containsLanguage", store.Contains(languageKey)),
                    LogFields.Field("containsVolume", store.Contains(volumeKey)),
                    LogFields.Field("containsFullscreen", store.Contains(fullscreenKey))));

            return passed;
        }

        private static bool ValidateMissing(
            FrameworkLogger logger,
            IPreferencesStore store,
            PreferenceKey missingKey)
        {
            var read = store.Read(missingKey, PreferenceValueKind.String);
            var delete = store.Delete(missingKey);

            bool passed = read is { Missing: true, HasValue: false }
                && delete.Missing
                && !store.Contains(missingKey);

            LogStep(
                logger,
                "missing",
                passed,
                LogFields.Of(
                    LogFields.Field("readStatus", read.Status.ToString()),
                    LogFields.Field("deleteStatus", delete.Status.ToString()),
                    LogFields.Field("contains", store.Contains(missingKey))));

            return passed;
        }

        private static bool ValidateTypeMismatch(
            FrameworkLogger logger,
            IPreferencesStore store,
            PreferenceKey mismatchKey)
        {
            var write = store.Write(mismatchKey, PreferenceValue.FromInt(1));
            var read = store.Read(mismatchKey, PreferenceValueKind.Bool);

            bool passed = write.Written
                && read is { TypeMismatch: true, HasValue: false }
                && store.Contains(mismatchKey);

            LogStep(
                logger,
                "type-mismatch",
                passed,
                LogFields.Of(
                    LogFields.Field("writeStatus", write.Status.ToString()),
                    LogFields.Field("readStatus", read.Status.ToString()),
                    LogFields.Field("expectedKind", read.ExpectedKind.ToString()),
                    LogFields.Field("hasValue", read.HasValue),
                    LogFields.Field("contains", store.Contains(mismatchKey))));

            return passed;
        }

        private static bool ValidateMarkerGuard(
            FrameworkLogger logger,
            PlayerPrefsPreferencesStore store,
            PreferenceKey orphanKey)
        {
            PlayerPrefs.SetString(store.ToPhysicalValueKey(orphanKey), "orphaned-without-type-marker");
            PlayerPrefs.DeleteKey(store.ToPhysicalTypeKey(orphanKey));

            var read = store.Read(orphanKey, PreferenceValueKind.String);
            bool passed = read is { TypeMismatch: true, HasValue: false } && !store.Contains(orphanKey);

            LogStep(
                logger,
                "playerprefs-type-marker-guard",
                passed,
                LogFields.Of(
                    LogFields.Field("readStatus", read.Status.ToString()),
                    LogFields.Field("contains", store.Contains(orphanKey)),
                    LogFields.Field("valueKeyExists", PlayerPrefs.HasKey(store.ToPhysicalValueKey(orphanKey))),
                    LogFields.Field("typeKeyExists", PlayerPrefs.HasKey(store.ToPhysicalTypeKey(orphanKey)))));

            return passed;
        }

        private static bool ValidateDelete(
            FrameworkLogger logger,
            IPreferencesStore store,
            params PreferenceKey[] keys)
        {
            int deleted = 0;
            int missing = 0;
            foreach (var key in keys)
            {
                var result = store.Delete(key);
                if (result.Deleted)
                {
                    deleted++;
                }
                else if (result.Missing)
                {
                    missing++;
                }
            }

            store.Flush();

            int remaining = 0;
            foreach (var key in keys)
            {
                if (store.Contains(key))
                {
                    remaining++;
                }
            }

            bool passed = remaining == 0 && deleted >= 4 && missing >= 1;

            LogStep(
                logger,
                "delete-cleanup",
                passed,
                LogFields.Of(
                    LogFields.Field("deleted", deleted),
                    LogFields.Field("missing", missing),
                    LogFields.Field("remaining", remaining)));

            return passed;
        }

        private static bool ValidateBoundary(FrameworkLogger logger, PlayerPrefsPreferencesStore store)
        {
            bool passed = store.KeyPrefix == QaPrefix;

            LogStep(
                logger,
                "canonical-boundary",
                passed,
                LogFields.Of(
                    LogFields.Field("namespace", "Immersive.Framework.Preferences"),
                    LogFields.Field("backend", nameof(PlayerPrefsPreferencesStore)),
                    LogFields.Field("snapshot", "none"),
                    LogFields.Field("progressionSlot", "none"),
                    LogFields.Field("json", "none"),
                    LogFields.Field("ui", "none")));

            return passed;
        }

        private static void Cleanup(PlayerPrefsPreferencesStore store, params PreferenceKey[] keys)
        {
            if (store == null || keys == null)
            {
                return;
            }

            foreach (var key in keys)
            {
                if (!key.IsValid)
                {
                    continue;
                }

                PlayerPrefs.DeleteKey(store.ToPhysicalValueKey(key));
                PlayerPrefs.DeleteKey(store.ToPhysicalTypeKey(key));
            }

            PlayerPrefs.Save();
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
                logger.Info("QA Preferences Store Diagnostics Smoke step completed.", allFields);
                return;
            }

            logger.Warning("QA Preferences Store Diagnostics Smoke step failed.", allFields);
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
