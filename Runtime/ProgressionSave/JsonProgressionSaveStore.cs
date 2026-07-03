using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Initial local JSON backend for the Progression Save store port.
    /// JSON and file paths are adapter details; framework consumers must depend on IProgressionSaveStore.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21F JSON Progression Save backend adapter; not the canonical Progression Save contract.")]
    public sealed class JsonProgressionSaveStore : IProgressionSaveStore
    {
        internal const int StorageFormatVersion = 1;
        internal const string DefaultBackendValue = "json.local";
        internal const string ManifestFileName = "manifest.json";
        internal const string SlotDirectoryName = "slots";

        private readonly ProgressionSaveBackendId _backendId;
        private readonly string _rootDirectory;
        private readonly string _slotDirectory;
        private readonly string _manifestPath;

        public JsonProgressionSaveStore(string rootDirectory)
            : this(rootDirectory, ProgressionSaveBackendId.From(DefaultBackendValue))
        {
        }

        public JsonProgressionSaveStore(string rootDirectory, ProgressionSaveBackendId backendId)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("JSON Progression Save store requires an explicit root directory.", nameof(rootDirectory));
            }

            if (!backendId.IsValid)
            {
                throw new ArgumentException("JSON Progression Save store requires a valid backend id.", nameof(backendId));
            }

            _rootDirectory = Path.GetFullPath(rootDirectory.Trim());
            _slotDirectory = Path.Combine(_rootDirectory, SlotDirectoryName);
            _manifestPath = Path.Combine(_rootDirectory, ManifestFileName);
            _backendId = backendId;
        }

        public ProgressionSaveBackendId BackendId => _backendId;

        internal string RootDirectory => _rootDirectory;

        internal string SlotDirectory => _slotDirectory;

        internal string ManifestPath => _manifestPath;

        public static JsonProgressionSaveStore CreateDefault(string productName)
        {
            string normalizedProductName = productName.NormalizeTextOrFallback("Application");
            return new JsonProgressionSaveStore(
                Path.Combine(Application.persistentDataPath, "ImmersiveFramework", "ProgressionSave", MakeSafePathSegment(normalizedProductName)));
        }

        public ProgressionSaveManifestReadResult ReadManifest()
        {
            try
            {
                if (!File.Exists(_manifestPath))
                {
                    return ProgressionSaveManifestReadResult.Missing("Progression Save manifest file is missing.");
                }

                string json = File.ReadAllText(_manifestPath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return ProgressionSaveManifestReadResult.Corrupt("Progression Save manifest file is empty.");
                }

                var dto = JsonUtility.FromJson<ManifestDto>(json);
                if (dto == null || dto.version != StorageFormatVersion)
                {
                    return ProgressionSaveManifestReadResult.Corrupt("Progression Save manifest has unsupported or missing storage version.");
                }

                var manifest = ToManifest(dto);
                if (!manifest.IsValid)
                {
                    return ProgressionSaveManifestReadResult.Corrupt("Progression Save manifest payload is invalid.");
                }

                return ProgressionSaveManifestReadResult.Found(manifest, "Progression Save manifest read from JSON backend.");
            }
            catch (Exception exception)
            {
                return ProgressionSaveManifestReadResult.Corrupt($"Progression Save manifest JSON could not be read. {exception.GetType().Name}: {exception.Message}");
            }
        }

        public ProgressionSaveManifestWriteResult WriteManifest(ProgressionSaveManifest manifest)
        {
            if (!manifest.IsValid)
            {
                return ProgressionSaveManifestWriteResult.Rejected("Progression Save manifest write rejected because the manifest is invalid.");
            }

            try
            {
                EnsureDirectories();
                var dto = FromManifest(manifest);
                string json = JsonUtility.ToJson(dto, prettyPrint: true);
                File.WriteAllText(_manifestPath, json, Encoding.UTF8);
                return ProgressionSaveManifestWriteResult.WrittenResult("Progression Save manifest written through JSON backend.");
            }
            catch (Exception exception)
            {
                return ProgressionSaveManifestWriteResult.FailedResult($"Progression Save manifest JSON could not be written. {exception.GetType().Name}: {exception.Message}");
            }
        }

        public bool ContainsSlot(ProgressionSaveSlotId slotId)
        {
            return slotId.IsValid && File.Exists(ToPhysicalSlotPath(slotId));
        }

        public ProgressionSaveReadResult ReadSlot(ProgressionSaveSlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException("Progression Save read requires a valid slot id.", nameof(slotId));
            }

            string path = ToPhysicalSlotPath(slotId);
            try
            {
                if (!File.Exists(path))
                {
                    return ProgressionSaveReadResult.Missing(slotId, "Progression Save slot file is missing.");
                }

                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return ProgressionSaveReadResult.Corrupt(slotId, "Progression Save slot file is empty.");
                }

                var dto = JsonUtility.FromJson<SlotRecordDto>(json);
                if (dto == null || dto.version != StorageFormatVersion)
                {
                    return ProgressionSaveReadResult.Corrupt(slotId, "Progression Save slot has unsupported or missing storage version.");
                }

                var record = ToRecord(dto);
                if (!record.IsValid || record.SlotId != slotId)
                {
                    return ProgressionSaveReadResult.Corrupt(slotId, "Progression Save slot payload is invalid or belongs to a different slot.");
                }

                return ProgressionSaveReadResult.Found(record, "Progression Save slot read from JSON backend.");
            }
            catch (Exception exception)
            {
                return ProgressionSaveReadResult.Corrupt(slotId, $"Progression Save slot JSON could not be read. {exception.GetType().Name}: {exception.Message}");
            }
        }

        public ProgressionSaveWriteResult WriteSlot(ProgressionSaveSlotRecord record)
        {
            if (!record.IsValid)
            {
                throw new ArgumentException("Progression Save write requires a valid slot record.", nameof(record));
            }

            try
            {
                EnsureDirectories();

                var dto = FromRecord(record);
                string json = JsonUtility.ToJson(dto, prettyPrint: true);
                File.WriteAllText(ToPhysicalSlotPath(record.SlotId), json, Encoding.UTF8);

                var manifestRead = ReadManifest();
                ProgressionSaveManifest manifest;
                if (manifestRead.HasManifest)
                {
                    manifest = manifestRead.Manifest;
                }
                else if (manifestRead.Status == ProgressionSaveReadStatus.Missing)
                {
                    manifest = ProgressionSaveManifest.Empty(record.UpdatedUtcTicks, nameof(JsonProgressionSaveStore));
                }
                else
                {
                    return ProgressionSaveWriteResult.FailedResult(record.SlotId, $"Progression Save slot was written, but manifest could not be updated because manifest status is '{manifestRead.Status}'.");
                }

                var updatedManifest = manifest.WithEntry(record.ToManifestEntry(), record.UpdatedUtcTicks, nameof(JsonProgressionSaveStore));
                var manifestWrite = WriteManifest(updatedManifest);
                if (!manifestWrite.Written)
                {
                    return ProgressionSaveWriteResult.FailedResult(record.SlotId, $"Progression Save slot was written, but manifest write failed with status '{manifestWrite.Status}'.");
                }

                return ProgressionSaveWriteResult.SlotWritten(record, "Progression Save slot written through JSON backend.");
            }
            catch (Exception exception)
            {
                return ProgressionSaveWriteResult.FailedResult(record.SlotId, $"Progression Save slot JSON could not be written. {exception.GetType().Name}: {exception.Message}");
            }
        }

        public ProgressionSaveDeleteResult DeleteSlot(ProgressionSaveSlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException("Progression Save delete requires a valid slot id.", nameof(slotId));
            }

            try
            {
                string path = ToPhysicalSlotPath(slotId);
                bool hadSlotFile = File.Exists(path);
                if (hadSlotFile)
                {
                    File.Delete(path);
                }

                bool manifestHadSlot = false;
                var manifestRead = ReadManifest();
                if (manifestRead.HasManifest)
                {
                    manifestHadSlot = manifestRead.Manifest.ContainsSlot(slotId);
                    if (manifestHadSlot)
                    {
                        var updatedManifest = manifestRead.Manifest.WithoutSlot(
                            slotId,
                            DateTime.UtcNow.Ticks,
                            nameof(JsonProgressionSaveStore));
                        var manifestWrite = WriteManifest(updatedManifest);
                        if (!manifestWrite.Written)
                        {
                            return ProgressionSaveDeleteResult.FailedResult(slotId, $"Progression Save slot file was deleted, but manifest update failed with status '{manifestWrite.Status}'.");
                        }
                    }
                }
                else if (manifestRead.Status != ProgressionSaveReadStatus.Missing)
                {
                    return ProgressionSaveDeleteResult.FailedResult(slotId, $"Progression Save slot delete could not validate manifest because manifest status is '{manifestRead.Status}'.");
                }

                if (!hadSlotFile && !manifestHadSlot)
                {
                    return ProgressionSaveDeleteResult.Missing(slotId, "Progression Save slot was already missing.");
                }

                return ProgressionSaveDeleteResult.Deleted(slotId, "Progression Save slot deleted from JSON backend.");
            }
            catch (Exception exception)
            {
                return ProgressionSaveDeleteResult.FailedResult(slotId, $"Progression Save slot JSON could not be deleted. {exception.GetType().Name}: {exception.Message}");
            }
        }

        internal string ToPhysicalSlotPath(ProgressionSaveSlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException("Progression Save slot path requires a valid slot id.", nameof(slotId));
            }

            return Path.Combine(_slotDirectory, ToSlotFileName(slotId));
        }

        internal void DeleteStoreData()
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(_rootDirectory);
            Directory.CreateDirectory(_slotDirectory);
        }

        private static ProgressionSaveManifest ToManifest(ManifestDto dto)
        {
            ManifestEntryDto[] dtoEntries = dto.entries ?? Array.Empty<ManifestEntryDto>();
            var entries = new ProgressionSaveManifestEntry[dtoEntries.Length];
            for (int i = 0; i < dtoEntries.Length; i++)
            {
                entries[i] = ToManifestEntry(dtoEntries[i]);
            }

            return new ProgressionSaveManifest(entries, dto.updatedUtcTicks, dto.source);
        }

        private static ManifestDto FromManifest(ProgressionSaveManifest manifest)
        {
            IReadOnlyList<ProgressionSaveManifestEntry> entries = manifest.Entries;
            var dtoEntries = new ManifestEntryDto[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                dtoEntries[i] = FromManifestEntry(entries[i]);
            }

            return new ManifestDto
            {
                version = StorageFormatVersion,
                updatedUtcTicks = manifest.UpdatedUtcTicks,
                source = manifest.Source,
                entries = dtoEntries
            };
        }

        private static ProgressionSaveManifestEntry ToManifestEntry(ManifestEntryDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentException("Progression Save manifest entry JSON is missing.", nameof(dto));
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

        private static ManifestEntryDto FromManifestEntry(ProgressionSaveManifestEntry entry)
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

        private static ProgressionSaveSlotRecord ToRecord(SlotRecordDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentException("Progression Save slot record JSON is missing.", nameof(dto));
            }

            return new ProgressionSaveSlotRecord(
                ProgressionSaveSlotId.From(dto.slotId),
                ProgressionSaveRecordId.From(dto.recordId),
                ToPayload(dto.payloadFormat, dto.payloadBase64, dto.payloadMediaType),
                dto.createdUtcTicks,
                dto.updatedUtcTicks,
                dto.displayName,
                dto.source,
                dto.reason);
        }

        private static SlotRecordDto FromRecord(ProgressionSaveSlotRecord record)
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
                payloadBase64 = Convert.ToBase64String(record.Payload.ToByteArray()),
                source = record.Source,
                reason = record.Reason
            };
        }

        private static ProgressionSavePayload ToPayload(int payloadFormat, string payloadBase64, string mediaType)
        {
            var format = ToPayloadFormat(payloadFormat);
            if (format == ProgressionSavePayloadFormat.Empty)
            {
                return ProgressionSavePayload.Empty();
            }

            if (string.IsNullOrEmpty(payloadBase64))
            {
                throw new ArgumentException("Progression Save non-empty payload JSON is missing base64 bytes.", nameof(payloadBase64));
            }

            return ProgressionSavePayload.FromBytes(format, Convert.FromBase64String(payloadBase64), mediaType);
        }

        private static ProgressionSavePayloadFormat ToPayloadFormat(int payloadFormat)
        {
            var format = (ProgressionSavePayloadFormat)payloadFormat;
            if (!Enum.IsDefined(typeof(ProgressionSavePayloadFormat), format) || format == ProgressionSavePayloadFormat.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(payloadFormat), payloadFormat, "Progression Save JSON payload format is invalid.");
            }

            return format;
        }

        private static string ToSlotFileName(ProgressionSaveSlotId slotId)
        {
            string stableText = slotId.StableText;
            return $"{MakeSafePathSegment(stableText)}-{ComputeSha256Hex(stableText).Substring(0, 12)}.json";
        }

        private static string MakeSafePathSegment(string value)
        {
            string normalized = value.NormalizeTextOrFallback("empty");
            char[] invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(normalized.Length);
            for (int i = 0; i < normalized.Length; i++)
            {
                char current = normalized[i];
                bool valid = char.IsLetterOrDigit(current) || current == '-' || current == '_' || current == '.';
                if (valid && Array.IndexOf(invalid, current) < 0)
                {
                    builder.Append(current);
                }
                else
                {
                    builder.Append('_');
                }
            }

            return builder.Length == 0 ? "empty" : builder.ToString();
        }

        private static string ComputeSha256Hex(string value)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
                byte[] hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
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
