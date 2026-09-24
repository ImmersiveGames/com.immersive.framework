using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental concrete adapter API.
    /// ADR018-B certifies this implementation as the official built-in minimum local
    /// Progression Save backend. Its concrete construction/catalog surface remains
    /// Experimental until ADR018-C defines the product composition boundary.
    ///
    /// Core slot persistence and the optional manifest catalog are maintained through
    /// a recoverable single-process transaction intent. The backend does not promise
    /// database-grade transactions or multi-process coordination.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR018-B CERTIFIED built-in minimum JSON backend; concrete construction/catalog API remains Experimental pending ADR018-C product composition.")]
    public sealed class JsonProgressionSaveStore : IProgressionSaveStore, IProgressionSaveCatalog
    {
        internal const int StorageFormatVersion = 1;
        internal const int TransactionFormatVersion = 1;

        internal const string DefaultBackendValue = "json.local";
        internal const string ManifestFileName = "manifest.json";
        internal const string SlotDirectoryName = "slots";

        internal const string TransactionDirectoryName = ".transaction";
        internal const string TransactionIntentFileName = "intent.json";
        internal const string TransactionIntentPendingFileName = "intent.pending.json";
        internal const string TransactionSlotStageFileName = "slot.stage.json";
        internal const string TransactionManifestStageFileName = "manifest.stage.json";

        private const int TransactionOperationWrite = 10;
        private const int TransactionOperationDelete = 20;

        private static readonly object GlobalIoGate = new object();

        private readonly ProgressionSaveBackendId _backendId;
        private readonly string _rootDirectory;
        private readonly string _slotDirectory;
        private readonly string _manifestPath;

        private readonly string _transactionDirectory;
        private readonly string _transactionIntentPath;
        private readonly string _transactionIntentPendingPath;
        private readonly string _transactionSlotStagePath;
        private readonly string _transactionManifestStagePath;

        public JsonProgressionSaveStore(string rootDirectory)
            : this(rootDirectory, ProgressionSaveBackendId.From(DefaultBackendValue))
        {
        }

        public JsonProgressionSaveStore(
            string rootDirectory,
            ProgressionSaveBackendId backendId)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException(
                    "JSON Progression Save store requires an explicit root directory.",
                    nameof(rootDirectory));
            }

            if (!backendId.IsValid)
            {
                throw new ArgumentException(
                    "JSON Progression Save store requires a valid backend id.",
                    nameof(backendId));
            }

            _rootDirectory =
                Path.GetFullPath(rootDirectory.Trim());

            _slotDirectory =
                Path.Combine(
                    _rootDirectory,
                    SlotDirectoryName);

            _manifestPath =
                Path.Combine(
                    _rootDirectory,
                    ManifestFileName);

            _transactionDirectory =
                Path.Combine(
                    _rootDirectory,
                    TransactionDirectoryName);

            _transactionIntentPath =
                Path.Combine(
                    _transactionDirectory,
                    TransactionIntentFileName);

            _transactionIntentPendingPath =
                Path.Combine(
                    _transactionDirectory,
                    TransactionIntentPendingFileName);

            _transactionSlotStagePath =
                Path.Combine(
                    _transactionDirectory,
                    TransactionSlotStageFileName);

            _transactionManifestStagePath =
                Path.Combine(
                    _transactionDirectory,
                    TransactionManifestStageFileName);

            _backendId = backendId;
        }

        public ProgressionSaveBackendId BackendId => _backendId;

        internal string RootDirectory => _rootDirectory;

        internal string SlotDirectory => _slotDirectory;

        internal string ManifestPath => _manifestPath;

        internal string TransactionDirectory => _transactionDirectory;

        internal string TransactionIntentPath => _transactionIntentPath;

        public static JsonProgressionSaveStore CreateDefault(string productName)
        {
            string normalizedProductName =
                productName.NormalizeTextOrFallback("Application");

            return new JsonProgressionSaveStore(
                Path.Combine(
                    Application.persistentDataPath,
                    "ImmersiveFramework",
                    "ProgressionSave",
                    MakeSafePathSegment(normalizedProductName)));
        }

        public ProgressionSaveManifestReadResult ReadManifest()
        {
            lock (GlobalIoGate)
            {
                if (!TryRecoverPendingTransaction(
                        out bool recovered,
                        out string recoveryDiagnostic))
                {
                    return ProgressionSaveManifestReadResult.FailedResult(
                        $"Progression Save JSON recovery blocked manifest read. {recoveryDiagnostic}");
                }

                ProgressionSaveManifestReadResult result =
                    ReadManifestCore();

                return recovered
                    ? AppendRecoveryDiagnostic(
                        result,
                        recoveryDiagnostic)
                    : result;
            }
        }

        public ProgressionSaveReadResult ReadSlot(
            ProgressionSaveSlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException(
                    "Progression Save read requires a valid slot id.",
                    nameof(slotId));
            }

            lock (GlobalIoGate)
            {
                if (!TryRecoverPendingTransaction(
                        out bool recovered,
                        out string recoveryDiagnostic))
                {
                    return ProgressionSaveReadResult.FailedResult(
                        slotId,
                        $"Progression Save JSON recovery blocked slot read. {recoveryDiagnostic}");
                }

                ProgressionSaveReadResult result =
                    ReadSlotCore(slotId);

                return recovered
                    ? AppendRecoveryDiagnostic(
                        result,
                        recoveryDiagnostic)
                    : result;
            }
        }

        public ProgressionSaveWriteResult WriteSlot(
            ProgressionSaveSlotRecord record)
        {
            if (!record.IsValid)
            {
                throw new ArgumentException(
                    "Progression Save write requires a valid slot record.",
                    nameof(record));
            }

            lock (GlobalIoGate)
            {
                if (!TryRecoverPendingTransaction(
                        out bool recoveredBeforeWrite,
                        out string recoveryDiagnostic))
                {
                    return ProgressionSaveWriteResult.FailedResult(
                        record.SlotId,
                        $"Progression Save JSON recovery blocked slot write. {recoveryDiagnostic}");
                }

                ProgressionSaveManifestReadResult manifestRead =
                    ReadManifestCore();

                ProgressionSaveManifest manifest;

                if (manifestRead.HasManifest)
                {
                    manifest =
                        manifestRead.Manifest;
                }
                else if (manifestRead.Status ==
                    ProgressionSaveReadStatus.Missing)
                {
                    manifest =
                        ProgressionSaveManifest.Empty(
                            record.UpdatedUtcTicks,
                            nameof(JsonProgressionSaveStore));
                }
                else
                {
                    return ProgressionSaveWriteResult.FailedResult(
                        record.SlotId,
                        $"Progression Save JSON slot write was rejected before mutation because " +
                        $"manifest status is '{manifestRead.Status}'.");
                }

                ProgressionSaveManifest updatedManifest =
                    manifest.WithEntry(
                        record.ToManifestEntry(),
                        record.UpdatedUtcTicks,
                        nameof(JsonProgressionSaveStore));

                if (!TryPrepareWriteTransaction(
                        record,
                        updatedManifest,
                        out string preparationIssue))
                {
                    return ProgressionSaveWriteResult.FailedResult(
                        record.SlotId,
                        $"Progression Save JSON slot write could not prepare a recoverable transaction. " +
                        preparationIssue);
                }

                if (!TryRecoverPendingTransaction(
                        out bool committed,
                        out string commitDiagnostic))
                {
                    return ProgressionSaveWriteResult.FailedResult(
                        record.SlotId,
                        $"Progression Save JSON slot write did not complete. " +
                        $"A committed transaction was retained for recovery. {commitDiagnostic}");
                }

                string message =
                    "Progression Save slot written through recoverable JSON backend.";

                if (recoveredBeforeWrite)
                {
                    message =
                        CombineMessages(
                            message,
                            $"Previous transaction recovery: {recoveryDiagnostic}");
                }

                if (committed)
                {
                    message =
                        CombineMessages(
                            message,
                            commitDiagnostic);
                }

                return ProgressionSaveWriteResult.SlotWritten(
                    record,
                    message);
            }
        }

        public ProgressionSaveDeleteResult DeleteSlot(
            ProgressionSaveSlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException(
                    "Progression Save delete requires a valid slot id.",
                    nameof(slotId));
            }

            lock (GlobalIoGate)
            {
                if (!TryRecoverPendingTransaction(
                        out bool recoveredBeforeDelete,
                        out string recoveryDiagnostic))
                {
                    return ProgressionSaveDeleteResult.FailedResult(
                        slotId,
                        $"Progression Save JSON recovery blocked slot delete. {recoveryDiagnostic}");
                }

                string slotPath =
                    ToPhysicalSlotPath(slotId);

                bool hadSlotFile =
                    File.Exists(slotPath);

                ProgressionSaveManifestReadResult manifestRead =
                    ReadManifestCore();

                bool manifestHadSlot = false;
                bool hasManifestStage = false;
                ProgressionSaveManifest updatedManifest = default;

                if (manifestRead.HasManifest)
                {
                    manifestHadSlot =
                        manifestRead.Manifest.ContainsSlot(slotId);

                    if (manifestHadSlot)
                    {
                        updatedManifest =
                            manifestRead.Manifest.WithoutSlot(
                                slotId,
                                DateTime.UtcNow.Ticks,
                                nameof(JsonProgressionSaveStore));

                        hasManifestStage = true;
                    }
                }
                else if (manifestRead.Status !=
                    ProgressionSaveReadStatus.Missing)
                {
                    return ProgressionSaveDeleteResult.FailedResult(
                        slotId,
                        $"Progression Save JSON slot delete was rejected before mutation because " +
                        $"manifest status is '{manifestRead.Status}'.");
                }

                if (!hadSlotFile && !manifestHadSlot)
                {
                    return ProgressionSaveDeleteResult.Missing(
                        slotId,
                        "Progression Save slot was already missing.");
                }

                if (!TryPrepareDeleteTransaction(
                        slotId,
                        hasManifestStage,
                        updatedManifest,
                        out string preparationIssue))
                {
                    return ProgressionSaveDeleteResult.FailedResult(
                        slotId,
                        $"Progression Save JSON slot delete could not prepare a recoverable transaction. " +
                        preparationIssue);
                }

                if (!TryRecoverPendingTransaction(
                        out bool committed,
                        out string commitDiagnostic))
                {
                    return ProgressionSaveDeleteResult.FailedResult(
                        slotId,
                        $"Progression Save JSON slot delete did not complete. " +
                        $"A committed transaction was retained for recovery. {commitDiagnostic}");
                }

                string message =
                    "Progression Save slot deleted through recoverable JSON backend.";

                if (recoveredBeforeDelete)
                {
                    message =
                        CombineMessages(
                            message,
                            $"Previous transaction recovery: {recoveryDiagnostic}");
                }

                if (committed)
                {
                    message =
                        CombineMessages(
                            message,
                            commitDiagnostic);
                }

                return ProgressionSaveDeleteResult.Deleted(
                    slotId,
                    message);
            }
        }

        internal string ToPhysicalSlotPath(
            ProgressionSaveSlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException(
                    "Progression Save slot path requires a valid slot id.",
                    nameof(slotId));
            }

            return Path.Combine(
                _slotDirectory,
                ToSlotFileName(slotId));
        }

        internal void DeleteStoreData()
        {
            lock (GlobalIoGate)
            {
                if (Directory.Exists(_rootDirectory))
                {
                    Directory.Delete(
                        _rootDirectory,
                        recursive: true);
                }
            }
        }

        private ProgressionSaveManifestReadResult ReadManifestCore()
        {
            return ReadManifestFile(
                _manifestPath,
                "manifest");
        }

        private ProgressionSaveReadResult ReadSlotCore(
            ProgressionSaveSlotId slotId)
        {
            return ReadSlotFile(
                ToPhysicalSlotPath(slotId),
                slotId,
                "slot");
        }

        private ProgressionSaveManifestReadResult ReadManifestFile(
            string path,
            string diagnosticName)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return ProgressionSaveManifestReadResult.Missing(
                        $"Progression Save JSON {diagnosticName} file is missing.");
                }

                string json =
                    File.ReadAllText(
                        path,
                        Encoding.UTF8);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return ProgressionSaveManifestReadResult.Corrupt(
                        $"Progression Save JSON {diagnosticName} file is empty.");
                }

                ManifestDto dto =
                    JsonUtility.FromJson<ManifestDto>(json);

                if (dto == null ||
                    dto.version != StorageFormatVersion)
                {
                    return ProgressionSaveManifestReadResult.Corrupt(
                        $"Progression Save JSON {diagnosticName} has unsupported or missing storage version.");
                }

                ProgressionSaveManifest manifest =
                    ToManifest(dto);

                if (!manifest.IsValid)
                {
                    return ProgressionSaveManifestReadResult.Corrupt(
                        $"Progression Save JSON {diagnosticName} payload is invalid.");
                }

                return ProgressionSaveManifestReadResult.Found(
                    manifest,
                    $"Progression Save JSON {diagnosticName} read successfully.");
            }
            catch (Exception exception)
            {
                return ProgressionSaveManifestReadResult.Corrupt(
                    $"Progression Save JSON {diagnosticName} could not be read. " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        private ProgressionSaveReadResult ReadSlotFile(
            string path,
            ProgressionSaveSlotId slotId,
            string diagnosticName)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return ProgressionSaveReadResult.Missing(
                        slotId,
                        $"Progression Save JSON {diagnosticName} file is missing.");
                }

                string json =
                    File.ReadAllText(
                        path,
                        Encoding.UTF8);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return ProgressionSaveReadResult.Corrupt(
                        slotId,
                        $"Progression Save JSON {diagnosticName} file is empty.");
                }

                SlotRecordDto dto =
                    JsonUtility.FromJson<SlotRecordDto>(json);

                if (dto == null ||
                    dto.version != StorageFormatVersion)
                {
                    return ProgressionSaveReadResult.Corrupt(
                        slotId,
                        $"Progression Save JSON {diagnosticName} has unsupported or missing storage version.");
                }

                ProgressionSaveSlotRecord record =
                    ToRecord(dto);

                if (!record.IsValid ||
                    record.SlotId != slotId)
                {
                    return ProgressionSaveReadResult.Corrupt(
                        slotId,
                        $"Progression Save JSON {diagnosticName} payload is invalid or belongs to a different slot.");
                }

                return ProgressionSaveReadResult.Found(
                    record,
                    $"Progression Save JSON {diagnosticName} read successfully.");
            }
            catch (Exception exception)
            {
                return ProgressionSaveReadResult.Corrupt(
                    slotId,
                    $"Progression Save JSON {diagnosticName} could not be read. " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        private bool TryPrepareWriteTransaction(
            ProgressionSaveSlotRecord record,
            ProgressionSaveManifest manifest,
            out string issue)
        {
            try
            {
                EnsureDirectories();

                if (Directory.Exists(_transactionDirectory))
                {
                    issue =
                        "A previous JSON transaction directory still exists after recovery.";
                    return false;
                }

                Directory.CreateDirectory(
                    _transactionDirectory);

                File.WriteAllText(
                    _transactionSlotStagePath,
                    SerializeRecord(record),
                    Encoding.UTF8);

                File.WriteAllText(
                    _transactionManifestStagePath,
                    SerializeManifest(manifest),
                    Encoding.UTF8);

                WriteIntentAtomically(
                    new TransactionIntentDto
                    {
                        version = TransactionFormatVersion,
                        operation = TransactionOperationWrite,
                        slotId = record.SlotId.Value.Value,
                        hasSlotStage = true,
                        hasManifestStage = true
                    });

                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                bool committedIntent =
                    File.Exists(_transactionIntentPath);

                if (!committedIntent)
                {
                    TryDiscardUncommittedTransaction(
                        out _);
                }

                issue =
                    $"Transaction preparation failed. committedIntent='{committedIntent}'. " +
                    $"{exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        private bool TryPrepareDeleteTransaction(
            ProgressionSaveSlotId slotId,
            bool hasManifestStage,
            ProgressionSaveManifest manifest,
            out string issue)
        {
            try
            {
                EnsureDirectories();

                if (Directory.Exists(_transactionDirectory))
                {
                    issue =
                        "A previous JSON transaction directory still exists after recovery.";
                    return false;
                }

                Directory.CreateDirectory(
                    _transactionDirectory);

                if (hasManifestStage)
                {
                    if (!manifest.IsValid ||
                        manifest.ContainsSlot(slotId))
                    {
                        issue =
                            "Delete transaction requires a valid staged manifest without the deleted slot.";
                        TryDiscardUncommittedTransaction(
                            out _);
                        return false;
                    }

                    File.WriteAllText(
                        _transactionManifestStagePath,
                        SerializeManifest(manifest),
                        Encoding.UTF8);
                }

                WriteIntentAtomically(
                    new TransactionIntentDto
                    {
                        version = TransactionFormatVersion,
                        operation = TransactionOperationDelete,
                        slotId = slotId.Value.Value,
                        hasSlotStage = false,
                        hasManifestStage = hasManifestStage
                    });

                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                bool committedIntent =
                    File.Exists(_transactionIntentPath);

                if (!committedIntent)
                {
                    TryDiscardUncommittedTransaction(
                        out _);
                }

                issue =
                    $"Delete transaction preparation failed. committedIntent='{committedIntent}'. " +
                    $"{exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        private bool TryRecoverPendingTransaction(
            out bool recovered,
            out string diagnostic)
        {
            recovered = false;
            diagnostic = string.Empty;

            if (!Directory.Exists(_transactionDirectory))
            {
                return true;
            }

            if (!File.Exists(_transactionIntentPath))
            {
                recovered = true;

                bool discarded =
                    TryDiscardUncommittedTransaction(
                        out string cleanupIssue);

                diagnostic = discarded
                    ? "Discarded uncommitted JSON transaction staging because no commit intent existed."
                    : $"Uncommitted JSON transaction staging has no commit intent; cleanup remains pending. {cleanupIssue}";

                return true;
            }

            if (!TryReadTransactionIntent(
                    out TransactionIntentDto intent,
                    out ProgressionSaveSlotId slotId,
                    out string intentIssue))
            {
                diagnostic =
                    $"Committed JSON transaction intent is invalid. Recovery stopped without applying staged data. {intentIssue}";
                return false;
            }

            try
            {
                if (intent.operation ==
                    TransactionOperationWrite)
                {
                    if (!TryValidateWriteTransaction(
                            intent,
                            slotId,
                            out string validationIssue))
                    {
                        diagnostic =
                            $"Committed JSON write transaction is invalid. Recovery stopped before canonical mutation. {validationIssue}";
                        return false;
                    }

                    ApplyStagedFile(
                        _transactionSlotStagePath,
                        ToPhysicalSlotPath(slotId));

                    ApplyStagedFile(
                        _transactionManifestStagePath,
                        _manifestPath);
                }
                else if (intent.operation ==
                    TransactionOperationDelete)
                {
                    if (!TryValidateDeleteTransaction(
                            intent,
                            slotId,
                            out string validationIssue))
                    {
                        diagnostic =
                            $"Committed JSON delete transaction is invalid. Recovery stopped before canonical mutation. {validationIssue}";
                        return false;
                    }

                    string slotPath =
                        ToPhysicalSlotPath(slotId);

                    if (File.Exists(slotPath))
                    {
                        File.Delete(slotPath);
                    }

                    if (intent.hasManifestStage)
                    {
                        ApplyStagedFile(
                            _transactionManifestStagePath,
                            _manifestPath);
                    }
                }
                else
                {
                    diagnostic =
                        $"Committed JSON transaction has unsupported operation '{intent.operation}'.";
                    return false;
                }

                recovered = true;

                bool cleaned =
                    TryDiscardCommittedTransaction(
                        out string cleanupIssue);

                diagnostic =
                    cleaned
                        ? $"Recovered committed JSON transaction operation='{ToOperationText(intent.operation)}' slot='{slotId.StableText}'."
                        : $"Recovered committed JSON transaction operation='{ToOperationText(intent.operation)}' slot='{slotId.StableText}', but transaction cleanup remains pending. {cleanupIssue}";

                return true;
            }
            catch (Exception exception)
            {
                diagnostic =
                    $"Committed JSON transaction recovery failed and remains pending. " +
                    $"{exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        private bool TryValidateWriteTransaction(
            TransactionIntentDto intent,
            ProgressionSaveSlotId slotId,
            out string issue)
        {
            if (!intent.hasSlotStage ||
                !intent.hasManifestStage)
            {
                issue =
                    "Write transaction intent must declare both slot and manifest staging.";
                return false;
            }

            ProgressionSaveReadResult slotRead =
                ReadSlotFile(
                    _transactionSlotStagePath,
                    slotId,
                    "transaction slot stage");

            if (slotRead.Status !=
                    ProgressionSaveReadStatus.Found ||
                !slotRead.HasRecord)
            {
                issue =
                    $"Staged slot status is '{slotRead.Status}'. {slotRead.Message}";
                return false;
            }

            ProgressionSaveManifestReadResult manifestRead =
                ReadManifestFile(
                    _transactionManifestStagePath,
                    "transaction manifest stage");

            if (manifestRead.Status !=
                    ProgressionSaveReadStatus.Found ||
                !manifestRead.HasManifest)
            {
                issue =
                    $"Staged manifest status is '{manifestRead.Status}'. {manifestRead.Message}";
                return false;
            }

            if (!manifestRead.Manifest.TryGetEntry(
                    slotId,
                    out ProgressionSaveManifestEntry entry))
            {
                issue =
                    "Staged manifest does not contain the staged slot.";
                return false;
            }

            ProgressionSaveManifestEntry expectedEntry =
                slotRead.Record.ToManifestEntry();

            if (entry != expectedEntry)
            {
                issue =
                    "Staged manifest entry does not match the staged slot record.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryValidateDeleteTransaction(
            TransactionIntentDto intent,
            ProgressionSaveSlotId slotId,
            out string issue)
        {
            if (intent.hasSlotStage)
            {
                issue =
                    "Delete transaction intent cannot declare a staged slot write.";
                return false;
            }

            if (!intent.hasManifestStage)
            {
                issue = string.Empty;
                return true;
            }

            ProgressionSaveManifestReadResult manifestRead =
                ReadManifestFile(
                    _transactionManifestStagePath,
                    "transaction delete manifest stage");

            if (manifestRead.Status !=
                    ProgressionSaveReadStatus.Found ||
                !manifestRead.HasManifest)
            {
                issue =
                    $"Staged delete manifest status is '{manifestRead.Status}'. {manifestRead.Message}";
                return false;
            }

            if (manifestRead.Manifest.ContainsSlot(slotId))
            {
                issue =
                    "Staged delete manifest still contains the slot selected for deletion.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryReadTransactionIntent(
            out TransactionIntentDto intent,
            out ProgressionSaveSlotId slotId,
            out string issue)
        {
            intent = null;
            slotId = default;

            try
            {
                string json =
                    File.ReadAllText(
                        _transactionIntentPath,
                        Encoding.UTF8);

                if (string.IsNullOrWhiteSpace(json))
                {
                    issue =
                        "Transaction intent file is empty.";
                    return false;
                }

                intent =
                    JsonUtility.FromJson<TransactionIntentDto>(json);

                if (intent == null)
                {
                    issue =
                        "Transaction intent JSON did not produce a payload.";
                    return false;
                }

                if (intent.version !=
                    TransactionFormatVersion)
                {
                    issue =
                        $"Transaction intent version '{intent.version}' is unsupported.";
                    return false;
                }

                if (intent.operation !=
                        TransactionOperationWrite &&
                    intent.operation !=
                        TransactionOperationDelete)
                {
                    issue =
                        $"Transaction intent operation '{intent.operation}' is unsupported.";
                    return false;
                }

                slotId =
                    ProgressionSaveSlotId.From(
                        intent.slotId);

                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                issue =
                    $"Transaction intent could not be read. " +
                    $"{exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        private void WriteIntentAtomically(
            TransactionIntentDto intent)
        {
            if (intent == null)
            {
                throw new ArgumentNullException(
                    nameof(intent));
            }

            string json =
                JsonUtility.ToJson(
                    intent,
                    prettyPrint: true);

            File.WriteAllText(
                _transactionIntentPendingPath,
                json,
                Encoding.UTF8);

            if (File.Exists(_transactionIntentPath))
            {
                File.Delete(
                    _transactionIntentPath);
            }

            File.Move(
                _transactionIntentPendingPath,
                _transactionIntentPath);
        }

        private static void ApplyStagedFile(
            string stagedPath,
            string targetPath)
        {
            if (!File.Exists(stagedPath))
            {
                throw new FileNotFoundException(
                    "Progression Save transaction stage is missing.",
                    stagedPath);
            }

            string targetDirectory =
                Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(
                    targetDirectory);
            }

            string commitPath =
                targetPath + ".commit";

            File.Copy(
                stagedPath,
                commitPath,
                overwrite: true);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(
                commitPath,
                targetPath);
        }

        private bool TryDiscardUncommittedTransaction(
            out string issue)
        {
            try
            {
                if (Directory.Exists(_transactionDirectory))
                {
                    Directory.Delete(
                        _transactionDirectory,
                        recursive: true);
                }

                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                issue =
                    $"Uncommitted transaction cleanup failed. " +
                    $"{exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        private bool TryDiscardCommittedTransaction(
            out string issue)
        {
            try
            {
                if (Directory.Exists(_transactionDirectory))
                {
                    Directory.Delete(
                        _transactionDirectory,
                        recursive: true);
                }

                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                issue =
                    $"Committed transaction cleanup failed. " +
                    $"{exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(
                _rootDirectory);

            Directory.CreateDirectory(
                _slotDirectory);
        }

        private static string SerializeManifest(
            ProgressionSaveManifest manifest)
        {
            return JsonUtility.ToJson(
                FromManifest(manifest),
                prettyPrint: true);
        }

        private static string SerializeRecord(
            ProgressionSaveSlotRecord record)
        {
            return JsonUtility.ToJson(
                FromRecord(record),
                prettyPrint: true);
        }

        private static ProgressionSaveManifestReadResult AppendRecoveryDiagnostic(
            ProgressionSaveManifestReadResult result,
            string recoveryDiagnostic)
        {
            string message =
                CombineMessages(
                    result.Message,
                    recoveryDiagnostic);

            switch (result.Status)
            {
                case ProgressionSaveReadStatus.Found:
                    return ProgressionSaveManifestReadResult.Found(
                        result.Manifest,
                        message);

                case ProgressionSaveReadStatus.Missing:
                    return ProgressionSaveManifestReadResult.Missing(
                        message);

                case ProgressionSaveReadStatus.Rejected:
                    return ProgressionSaveManifestReadResult.Rejected(
                        message);

                case ProgressionSaveReadStatus.Corrupt:
                    return ProgressionSaveManifestReadResult.Corrupt(
                        message);

                case ProgressionSaveReadStatus.BackendUnavailable:
                    return ProgressionSaveManifestReadResult.BackendUnavailable(
                        message);

                case ProgressionSaveReadStatus.Failed:
                    return ProgressionSaveManifestReadResult.FailedResult(
                        message);

                default:
                    throw new InvalidOperationException(
                        $"Progression Save manifest result has unsupported status '{result.Status}'.");
            }
        }

        private static ProgressionSaveReadResult AppendRecoveryDiagnostic(
            ProgressionSaveReadResult result,
            string recoveryDiagnostic)
        {
            string message =
                CombineMessages(
                    result.Message,
                    recoveryDiagnostic);

            switch (result.Status)
            {
                case ProgressionSaveReadStatus.Found:
                    return ProgressionSaveReadResult.Found(
                        result.Record,
                        message);

                case ProgressionSaveReadStatus.Missing:
                    return ProgressionSaveReadResult.Missing(
                        result.SlotId,
                        message);

                case ProgressionSaveReadStatus.Rejected:
                    return ProgressionSaveReadResult.Rejected(
                        result.SlotId,
                        message);

                case ProgressionSaveReadStatus.Corrupt:
                    return ProgressionSaveReadResult.Corrupt(
                        result.SlotId,
                        message);

                case ProgressionSaveReadStatus.BackendUnavailable:
                    return ProgressionSaveReadResult.BackendUnavailable(
                        result.SlotId,
                        message);

                case ProgressionSaveReadStatus.Failed:
                    return ProgressionSaveReadResult.FailedResult(
                        result.SlotId,
                        message);

                default:
                    throw new InvalidOperationException(
                        $"Progression Save read result has unsupported status '{result.Status}'.");
            }
        }

        private static string CombineMessages(
            string first,
            string second)
        {
            string left =
                first.NormalizeText();

            string right =
                second.NormalizeText();

            if (string.IsNullOrWhiteSpace(left))
            {
                return right;
            }

            if (string.IsNullOrWhiteSpace(right))
            {
                return left;
            }

            return $"{left} {right}";
        }

        private static string ToOperationText(
            int operation)
        {
            return operation == TransactionOperationWrite
                ? "Write"
                : operation == TransactionOperationDelete
                    ? "Delete"
                    : $"Unknown({operation})";
        }

        private static ProgressionSaveManifest ToManifest(
            ManifestDto dto)
        {
            ManifestEntryDto[] dtoEntries =
                dto.entries ?? Array.Empty<ManifestEntryDto>();

            var entries =
                new ProgressionSaveManifestEntry[dtoEntries.Length];

            for (int i = 0; i < dtoEntries.Length; i++)
            {
                entries[i] =
                    ToManifestEntry(
                        dtoEntries[i]);
            }

            return new ProgressionSaveManifest(
                entries,
                dto.updatedUtcTicks,
                dto.source);
        }

        private static ManifestDto FromManifest(
            ProgressionSaveManifest manifest)
        {
            IReadOnlyList<ProgressionSaveManifestEntry> entries =
                manifest.Entries;

            var dtoEntries =
                new ManifestEntryDto[entries.Count];

            for (int i = 0; i < entries.Count; i++)
            {
                dtoEntries[i] =
                    FromManifestEntry(
                        entries[i]);
            }

            return new ManifestDto
            {
                version = StorageFormatVersion,
                updatedUtcTicks = manifest.UpdatedUtcTicks,
                source = manifest.Source,
                entries = dtoEntries
            };
        }

        private static ProgressionSaveManifestEntry ToManifestEntry(
            ManifestEntryDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentException(
                    "Progression Save manifest entry JSON is missing.",
                    nameof(dto));
            }

            return new ProgressionSaveManifestEntry(
                ProgressionSaveSlotId.From(dto.slotId),
                ProgressionSaveRecordId.From(dto.recordId),
                dto.displayName,
                dto.createdUtcTicks,
                dto.updatedUtcTicks,
                ToPayloadFormat(dto.payloadFormat),
                dto.payloadByteCount,
                dto.source,
                dto.reason);
        }

        private static ManifestEntryDto FromManifestEntry(
            ProgressionSaveManifestEntry entry)
        {
            return new ManifestEntryDto
            {
                slotId = entry.SlotId.Value.Value,
                recordId = entry.RecordId.Value.Value,
                displayName = entry.DisplayName,
                createdUtcTicks = entry.CreatedUtcTicks,
                updatedUtcTicks = entry.UpdatedUtcTicks,
                payloadFormat = (int)entry.PayloadFormat,
                payloadByteCount = entry.PayloadByteCount,
                source = entry.Source,
                reason = entry.Reason
            };
        }

        private static ProgressionSaveSlotRecord ToRecord(
            SlotRecordDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentException(
                    "Progression Save slot record JSON is missing.",
                    nameof(dto));
            }

            return new ProgressionSaveSlotRecord(
                ProgressionSaveSlotId.From(dto.slotId),
                ProgressionSaveRecordId.From(dto.recordId),
                ToPayload(
                    dto.payloadFormat,
                    dto.payloadBase64,
                    dto.payloadMediaType),
                dto.createdUtcTicks,
                dto.updatedUtcTicks,
                dto.displayName,
                dto.source,
                dto.reason);
        }

        private static SlotRecordDto FromRecord(
            ProgressionSaveSlotRecord record)
        {
            return new SlotRecordDto
            {
                version = StorageFormatVersion,
                slotId = record.SlotId.Value.Value,
                recordId = record.RecordId.Value.Value,
                displayName = record.DisplayName,
                createdUtcTicks = record.CreatedUtcTicks,
                updatedUtcTicks = record.UpdatedUtcTicks,
                payloadFormat = (int)record.Payload.Format,
                payloadMediaType = record.Payload.MediaType,
                payloadBase64 =
                    Convert.ToBase64String(
                        record.Payload.ToByteArray()),
                source = record.Source,
                reason = record.Reason
            };
        }

        private static ProgressionSavePayload ToPayload(
            int payloadFormat,
            string payloadBase64,
            string mediaType)
        {
            ProgressionSavePayloadFormat format =
                ToPayloadFormat(
                    payloadFormat);

            if (format ==
                ProgressionSavePayloadFormat.Empty)
            {
                return ProgressionSavePayload.Empty();
            }

            if (string.IsNullOrEmpty(payloadBase64))
            {
                throw new ArgumentException(
                    "Progression Save non-empty payload JSON is missing base64 bytes.",
                    nameof(payloadBase64));
            }

            return ProgressionSavePayload.FromBytes(
                format,
                Convert.FromBase64String(
                    payloadBase64),
                mediaType);
        }

        private static ProgressionSavePayloadFormat ToPayloadFormat(
            int payloadFormat)
        {
            ProgressionSavePayloadFormat format =
                (ProgressionSavePayloadFormat)payloadFormat;

            if (!Enum.IsDefined(
                    typeof(ProgressionSavePayloadFormat),
                    format) ||
                format ==
                    ProgressionSavePayloadFormat.Unknown)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(payloadFormat),
                    payloadFormat,
                    "Progression Save JSON payload format is invalid.");
            }

            return format;
        }

        private static string ToSlotFileName(
            ProgressionSaveSlotId slotId)
        {
            string stableText =
                slotId.StableText;

            return
                $"{MakeSafePathSegment(stableText)}-" +
                $"{ComputeSha256Hex(stableText).Substring(0, 12)}.json";
        }

        private static string MakeSafePathSegment(
            string value)
        {
            string normalized =
                value.NormalizeTextOrFallback("empty");

            char[] invalid =
                Path.GetInvalidFileNameChars();

            var builder =
                new StringBuilder(
                    normalized.Length);

            for (int i = 0; i < normalized.Length; i++)
            {
                char current =
                    normalized[i];

                bool valid =
                    char.IsLetterOrDigit(current) ||
                    current == '-' ||
                    current == '_' ||
                    current == '.';

                if (valid &&
                    Array.IndexOf(
                        invalid,
                        current) < 0)
                {
                    builder.Append(
                        current);
                }
                else
                {
                    builder.Append('_');
                }
            }

            return builder.Length == 0
                ? "empty"
                : builder.ToString();
        }

        private static string ComputeSha256Hex(
            string value)
        {
            using (SHA256 sha =
                SHA256.Create())
            {
                byte[] bytes =
                    Encoding.UTF8.GetBytes(
                        value ?? string.Empty);

                byte[] hash =
                    sha.ComputeHash(bytes);

                var builder =
                    new StringBuilder(
                        hash.Length * 2);

                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(
                        hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        [Serializable]
        private sealed class TransactionIntentDto
        {
            public int version;
            public int operation;
            public string slotId;
            public bool hasSlotStage;
            public bool hasManifestStage;
        }

        [Serializable]
        private sealed class ManifestDto
        {
            public int version;
            public long updatedUtcTicks;
            public string source;
            public ManifestEntryDto[] entries;
        }

        [Serializable]
        private sealed class ManifestEntryDto
        {
            public string slotId;
            public string recordId;
            public string displayName;
            public long createdUtcTicks;
            public long updatedUtcTicks;
            public int payloadFormat;
            public int payloadByteCount;
            public string source;
            public string reason;
        }

        [Serializable]
        private sealed class SlotRecordDto
        {
            public int version;
            public string slotId;
            public string recordId;
            public string displayName;
            public long createdUtcTicks;
            public long updatedUtcTicks;
            public int payloadFormat;
            public string payloadMediaType;
            public string payloadBase64;
            public string source;
            public string reason;
        }
    }
}
