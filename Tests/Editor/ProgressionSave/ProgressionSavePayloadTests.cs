using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.ProgressionSave;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.ProgressionSave.Tests
{
    public sealed class ProgressionSavePayloadTests
    {
        [Test]
        public void Constructor_CopiesInputAndToByteArrayReturnsIndependentCopy()
        {
            byte[] input = { 1, 2, 3 };
            var payload = new ProgressionSavePayload(
                ProgressionSavePayloadFormat.Binary,
                input,
                "application/octet-stream");

            input[0] = 99;
            byte[] exposedCopy = payload.ToByteArray();
            exposedCopy[1] = 88;

            CollectionAssert.AreEqual(
                new byte[] { 1, 2, 3 },
                payload.Bytes);
        }

        [Test]
        public void Bytes_ExposesNoMutableBackingArrayOrWritableView()
        {
            var payload = CreatePayload(1, 2, 3);

            Assert.IsFalse(payload.Bytes is byte[]);
            Assert.IsTrue(payload.Bytes is IList<byte>);

            var writableView = (IList<byte>)payload.Bytes;
            Assert.Throws<NotSupportedException>(
                () => writableView[0] = 99);
            Assert.Throws<NotSupportedException>(
                () => writableView.Add(4));
            Assert.Throws<NotSupportedException>(
                () => writableView.Clear());

            CollectionAssert.AreEqual(
                new byte[] { 1, 2, 3 },
                payload.Bytes);
        }

        [Test]
        public void EqualityAndHashCode_RemainStableAfterCallerMutatesCopies()
        {
            var left = CreatePayload(1, 2, 3);
            var right = CreatePayload(1, 2, 3);
            int originalHash = left.GetHashCode();

            byte[] copy = left.ToByteArray();
            copy[0] = 99;
            var readOnlyView = (IList<byte>)left.Bytes;
            Assert.Throws<NotSupportedException>(
                () => readOnlyView[0] = 88);

            Assert.AreEqual(right, left);
            Assert.AreEqual(originalHash, left.GetHashCode());
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Test]
        public void StoreContract_PreservesPayloadAcrossWriteReadAndDelete()
        {
            byte[] input = { 4, 5, 6 };
            var payload = new ProgressionSavePayload(
                ProgressionSavePayloadFormat.Binary,
                input,
                "application/octet-stream");
            var slotId = new ProgressionSaveSlotId("payload-integrity.slot");
            long createdTicks = DateTime.UtcNow.Ticks;
            var record = new ProgressionSaveSlotRecord(
                slotId,
                new ProgressionSaveRecordId("payload-integrity.record"),
                payload,
                createdTicks,
                createdTicks + 1,
                "Payload integrity",
                "test",
                "round-trip");
            var store = new InMemoryProgressionSaveStore();

            ProgressionSaveWriteResult writeResult = store.WriteSlot(record);
            input[0] = 99;
            Assert.IsTrue(writeResult.Written);

            ProgressionSaveReadResult readResult = store.ReadSlot(slotId);
            Assert.AreEqual(ProgressionSaveReadStatus.Found, readResult.Status);
            Assert.AreEqual(payload, readResult.Record.Payload);
            CollectionAssert.AreEqual(
                new byte[] { 4, 5, 6 },
                readResult.Record.Payload.Bytes);

            var storedPayloadView = (IList<byte>)readResult.Record.Payload.Bytes;
            Assert.Throws<NotSupportedException>(
                () => storedPayloadView[0] = 88);

            ProgressionSaveDeleteResult deleteResult = store.DeleteSlot(slotId);
            Assert.AreEqual(ProgressionSaveDeleteStatus.Deleted, deleteResult.Status);
            Assert.AreEqual(
                ProgressionSaveReadStatus.Missing,
                store.ReadSlot(slotId).Status);
        }

        [Test]
        public void StoreContract_RoundTripsEmptyAndTextPayloads()
        {
            var payloads = new[]
            {
                ProgressionSavePayload.Empty(),
                ProgressionSavePayload.FromText("saved text", "text/plain")
            };
            var store = new InMemoryProgressionSaveStore();

            for (int index = 0; index < payloads.Length; index++)
            {
                var slotId = new ProgressionSaveSlotId(
                    $"payload-integrity.format-{index}");
                long createdTicks = DateTime.UtcNow.Ticks;
                var record = new ProgressionSaveSlotRecord(
                    slotId,
                    new ProgressionSaveRecordId(
                        $"payload-integrity.format-record-{index}"),
                    payloads[index],
                    createdTicks,
                    createdTicks + 1,
                    "Payload format conformance",
                    "test",
                    "round-trip");

                Assert.IsTrue(store.WriteSlot(record).Written);
                ProgressionSaveReadResult readResult = store.ReadSlot(slotId);
                Assert.AreEqual(ProgressionSaveReadStatus.Found, readResult.Status);
                Assert.AreEqual(payloads[index], readResult.Record.Payload);
                Assert.AreEqual(payloads[index].ByteCount, readResult.Record.Payload.ByteCount);

                Assert.AreEqual(
                    ProgressionSaveDeleteStatus.Deleted,
                    store.DeleteSlot(slotId).Status);
                Assert.AreEqual(
                    ProgressionSaveReadStatus.Missing,
                    store.ReadSlot(slotId).Status);
            }
        }

        [Test]
        public void JsonStore_ImplementsStableReadWriteDeleteContractWithExactPayload()
        {
            string root = CreateTemporaryStoreRoot();
            try
            {
                var store = new JsonProgressionSaveStore(root);
                var slotId = new ProgressionSaveSlotId("json-contract.slot");
                byte[] sourceBytes = { 17, 28, 39, 40 };
                var payload = ProgressionSavePayload.FromBytes(
                    ProgressionSavePayloadFormat.Binary,
                    sourceBytes,
                    "application/octet-stream");
                long createdTicks = DateTime.UtcNow.Ticks;
                var record = new ProgressionSaveSlotRecord(
                    slotId,
                    new ProgressionSaveRecordId("json-contract.record"),
                    payload,
                    createdTicks,
                    createdTicks + 1,
                    "JSON contract",
                    "NUnit",
                    "round-trip");

                ProgressionSaveReadResult missing = store.ReadSlot(slotId);
                Assert.AreEqual(ProgressionSaveReadStatus.Missing, missing.Status);
                Assert.IsFalse(missing.HasRecord);

                ProgressionSaveWriteResult written = store.WriteSlot(record);
                sourceBytes[0] = 255;
                Assert.AreEqual(ProgressionSaveWriteStatus.Written, written.Status);
                Assert.AreEqual(slotId, written.SlotId);
                Assert.AreEqual(record.RecordId, written.RecordId);

                ProgressionSaveReadResult found = store.ReadSlot(slotId);
                Assert.AreEqual(ProgressionSaveReadStatus.Found, found.Status);
                Assert.AreEqual(record, found.Record);
                Assert.AreEqual(payload, found.Record.Payload);
                CollectionAssert.AreEqual(
                    new byte[] { 17, 28, 39, 40 },
                    found.Record.Payload.Bytes);

                ProgressionSaveDeleteResult deleted = store.DeleteSlot(slotId);
                Assert.AreEqual(ProgressionSaveDeleteStatus.Deleted, deleted.Status);
                Assert.AreEqual(slotId, deleted.SlotId);
                Assert.AreEqual(
                    ProgressionSaveReadStatus.Missing,
                    store.ReadSlot(slotId).Status);
            }
            finally
            {
                DeleteTemporaryStoreDirectory(root);
            }
        }

        [Test]
        public void JsonStore_WriteFailureDoesNotClaimTheSlotWasWritten()
        {
            string blockerFile = Path.Combine(
                Application.temporaryCachePath,
                "IF-ProgressionSave-BackendBlock-" + Guid.NewGuid().ToString("N"));
            try
            {
                File.WriteAllText(blockerFile, "not a directory");
                var store = new JsonProgressionSaveStore(
                    Path.Combine(blockerFile, "store"));
                ProgressionSaveSlotRecord record = CreateRecord(
                    CreatePayload(3, 1, 4));

                ProgressionSaveWriteResult result = store.WriteSlot(record);

                Assert.AreEqual(ProgressionSaveWriteStatus.Failed, result.Status);
                Assert.IsFalse(result.Written);
                Assert.AreEqual(record.SlotId, result.SlotId);
                Assert.IsFalse(result.HasRecord);
            }
            finally
            {
                if (File.Exists(blockerFile))
                {
                    File.Delete(blockerFile);
                }
            }
        }

        [Test]
        public void ReadResults_PreserveDistinctOutcomeCodesAndFoundPayload()
        {
            ProgressionSaveSlotRecord record = CreateRecord(
                CreatePayload(7, 8, 9));

            ProgressionSaveReadResult found =
                ProgressionSaveReadResult.Found(record, "Found.");
            Assert.AreEqual(ProgressionSaveReadStatus.Found, found.Status);
            Assert.IsTrue(found.HasRecord);
            Assert.AreEqual(record, found.Record);
            Assert.AreEqual(record.RecordId, found.Record.RecordId);
            Assert.AreEqual(record.Payload, found.Record.Payload);
            CollectionAssert.AreEqual(
                new byte[] { 7, 8, 9 },
                found.Record.Payload.Bytes);

            AssertReadStatus(
                ProgressionSaveReadResult.Missing(record.SlotId, "Missing."),
                ProgressionSaveReadStatus.Missing);
            AssertReadStatus(
                ProgressionSaveReadResult.Rejected(record.SlotId, "Rejected."),
                ProgressionSaveReadStatus.Rejected);
            AssertReadStatus(
                ProgressionSaveReadResult.BackendUnavailable(record.SlotId, "Unavailable."),
                ProgressionSaveReadStatus.BackendUnavailable);
            AssertReadStatus(
                ProgressionSaveReadResult.FailedResult(record.SlotId, "Failed."),
                ProgressionSaveReadStatus.Failed);
            AssertReadStatus(
                ProgressionSaveReadResult.Corrupt(record.SlotId, "Corrupt."),
                ProgressionSaveReadStatus.Corrupt);
        }

        [Test]
        public void WriteResults_PreserveDistinctOutcomeCodesAndWrittenIdentity()
        {
            ProgressionSaveSlotRecord record = CreateRecord(
                CreatePayload(10, 11));

            ProgressionSaveWriteResult written =
                ProgressionSaveWriteResult.SlotWritten(record, "Written.");
            Assert.AreEqual(ProgressionSaveWriteStatus.Written, written.Status);
            Assert.AreEqual(record.SlotId, written.SlotId);
            Assert.AreEqual(record.RecordId, written.RecordId);

            AssertWriteStatus(
                ProgressionSaveWriteResult.Rejected(record.SlotId, "Rejected."),
                ProgressionSaveWriteStatus.Rejected);
            AssertWriteStatus(
                ProgressionSaveWriteResult.BackendUnavailable(record.SlotId, "Unavailable."),
                ProgressionSaveWriteStatus.BackendUnavailable);
            AssertWriteStatus(
                ProgressionSaveWriteResult.FailedResult(record.SlotId, "Failed."),
                ProgressionSaveWriteStatus.Failed);
        }

        [Test]
        public void DeleteResults_PreserveDistinctOutcomeCodesAndSlotIdentity()
        {
            var slotId = new ProgressionSaveSlotId("payload-integrity.delete-result");

            AssertDeleteStatus(
                ProgressionSaveDeleteResult.Deleted(slotId, "Deleted."),
                ProgressionSaveDeleteStatus.Deleted);
            AssertDeleteStatus(
                ProgressionSaveDeleteResult.Missing(slotId, "Missing."),
                ProgressionSaveDeleteStatus.Missing);
            AssertDeleteStatus(
                ProgressionSaveDeleteResult.Rejected(slotId, "Rejected."),
                ProgressionSaveDeleteStatus.Rejected);
            AssertDeleteStatus(
                ProgressionSaveDeleteResult.BackendUnavailable(slotId, "Unavailable."),
                ProgressionSaveDeleteStatus.BackendUnavailable);
            AssertDeleteStatus(
                ProgressionSaveDeleteResult.FailedResult(slotId, "Failed."),
                ProgressionSaveDeleteStatus.Failed);
        }

        private static ProgressionSavePayload CreatePayload(params byte[] bytes)
        {
            return new ProgressionSavePayload(
                ProgressionSavePayloadFormat.Binary,
                bytes,
                "application/octet-stream");
        }

        private static string CreateTemporaryStoreRoot()
        {
            return Path.Combine(
                Application.temporaryCachePath,
                "IF-ProgressionSave-Contract-" + Guid.NewGuid().ToString("N"));
        }

        private static void DeleteTemporaryStoreDirectory(string path)
        {
            string temporaryRoot = Path.GetFullPath(
                    Application.temporaryCachePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string resolvedPath = Path.GetFullPath(path);
            if (!resolvedPath.StartsWith(
                    temporaryRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Progression Save contract test cleanup escaped Unity's temporary cache directory.");
            }

            if (Directory.Exists(resolvedPath))
            {
                Directory.Delete(resolvedPath, recursive: true);
            }
        }

        private static ProgressionSaveSlotRecord CreateRecord(
            ProgressionSavePayload payload)
        {
            long createdTicks = DateTime.UtcNow.Ticks;
            return new ProgressionSaveSlotRecord(
                new ProgressionSaveSlotId("payload-integrity.result.slot"),
                new ProgressionSaveRecordId("payload-integrity.result.record"),
                payload,
                createdTicks,
                createdTicks + 1,
                "Result conformance",
                "test",
                "result-codes");
        }

        private static void AssertReadStatus(
            ProgressionSaveReadResult result,
            ProgressionSaveReadStatus expectedStatus)
        {
            Assert.AreEqual(expectedStatus, result.Status);
            Assert.IsFalse(result.HasRecord);
            Assert.IsFalse(result.Record.IsValid);
        }

        private static void AssertWriteStatus(
            ProgressionSaveWriteResult result,
            ProgressionSaveWriteStatus expectedStatus)
        {
            Assert.AreEqual(expectedStatus, result.Status);
            Assert.IsFalse(result.Written);
            Assert.IsFalse(result.HasRecord);
        }

        private static void AssertDeleteStatus(
            ProgressionSaveDeleteResult result,
            ProgressionSaveDeleteStatus expectedStatus)
        {
            Assert.AreEqual(expectedStatus, result.Status);
            Assert.AreEqual("payload-integrity.delete-result", result.SlotId.Value.Value);
        }

        private sealed class InMemoryProgressionSaveStore : IProgressionSaveStore
        {
            private readonly Dictionary<ProgressionSaveSlotId, ProgressionSaveSlotRecord>
                _records = new Dictionary<ProgressionSaveSlotId, ProgressionSaveSlotRecord>();

            public ProgressionSaveBackendId BackendId =>
                new ProgressionSaveBackendId("payload-integrity.memory");

            public ProgressionSaveReadResult ReadSlot(ProgressionSaveSlotId slotId)
            {
                return _records.TryGetValue(slotId, out ProgressionSaveSlotRecord record)
                    ? ProgressionSaveReadResult.Found(record, "Found.")
                    : ProgressionSaveReadResult.Missing(slotId, "Missing.");
            }

            public ProgressionSaveWriteResult WriteSlot(ProgressionSaveSlotRecord record)
            {
                _records[record.SlotId] = record;
                return ProgressionSaveWriteResult.SlotWritten(record, "Written.");
            }

            public ProgressionSaveDeleteResult DeleteSlot(ProgressionSaveSlotId slotId)
            {
                return _records.Remove(slotId)
                    ? ProgressionSaveDeleteResult.Deleted(slotId, "Deleted.")
                    : ProgressionSaveDeleteResult.Missing(slotId, "Missing.");
            }
        }
    }
}
