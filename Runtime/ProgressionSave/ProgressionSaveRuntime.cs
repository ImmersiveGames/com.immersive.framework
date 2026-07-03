using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Minimal runtime request path for Progression Save.
    /// The runtime executes explicit requests against an injected IProgressionSaveStore. It does not discover participants,
    /// capture Snapshots, schedule autosave, observe Route/Activity lifecycle or own UI.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21G Progression Save explicit runtime request path; store-backed and lifecycle-neutral.")]
    public sealed class ProgressionSaveRuntime
    {
        private readonly IProgressionSaveStore _store;

        public ProgressionSaveRuntime(IProgressionSaveStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            if (!store.BackendId.IsValid)
            {
                throw new ArgumentException("Progression Save runtime requires a store with a valid backend id.", nameof(store));
            }
        }

        public ProgressionSaveBackendId BackendId => _store.BackendId;

        public IProgressionSaveStore Store => _store;

        public ProgressionSaveRequestResult Request(ProgressionSaveRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Progression Save runtime request must be valid.", nameof(request));
            }

            switch (request.Kind)
            {
                case ProgressionSaveRequestKind.Save:
                    return ExecuteSave(request);
                case ProgressionSaveRequestKind.Load:
                    return ExecuteLoad(request);
                case ProgressionSaveRequestKind.Delete:
                    return ExecuteDelete(request);
                default:
                    return ProgressionSaveRequestResult.Rejected(
                        request,
                        BackendId,
                        $"Unsupported Progression Save request kind '{request.Kind}'.");
            }
        }

        private ProgressionSaveRequestResult ExecuteSave(ProgressionSaveRequest request)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            var record = new ProgressionSaveSlotRecord(
                request.SlotId,
                request.RecordId,
                request.Payload,
                nowTicks,
                nowTicks,
                request.DisplayName,
                request.Source,
                request.Reason);

            var write = _store.WriteSlot(record);
            if (write.Written)
            {
                return ProgressionSaveRequestResult.Saved(request, BackendId, record, write.Message);
            }

            if (write.Status == ProgressionSaveWriteStatus.Rejected)
            {
                return ProgressionSaveRequestResult.Rejected(request, BackendId, write.Message);
            }

            if (write.Status == ProgressionSaveWriteStatus.BackendUnavailable)
            {
                return ProgressionSaveRequestResult.BackendUnavailable(request, BackendId, write.Message);
            }

            return ProgressionSaveRequestResult.FailedResult(request, BackendId, write.Message);
        }

        private ProgressionSaveRequestResult ExecuteLoad(ProgressionSaveRequest request)
        {
            var read = _store.ReadSlot(request.SlotId);
            if (read.Status == ProgressionSaveReadStatus.Found)
            {
                return ProgressionSaveRequestResult.Loaded(request, BackendId, read.Record, read.Message);
            }

            if (read.Status == ProgressionSaveReadStatus.Missing)
            {
                return ProgressionSaveRequestResult.Missing(request, BackendId, read.Message);
            }

            if (read.Status == ProgressionSaveReadStatus.Rejected)
            {
                return ProgressionSaveRequestResult.Rejected(request, BackendId, read.Message);
            }

            if (read.Status == ProgressionSaveReadStatus.BackendUnavailable)
            {
                return ProgressionSaveRequestResult.BackendUnavailable(request, BackendId, read.Message);
            }

            if (read.Status == ProgressionSaveReadStatus.Corrupt)
            {
                return ProgressionSaveRequestResult.Corrupt(request, BackendId, read.Message);
            }

            return ProgressionSaveRequestResult.FailedResult(request, BackendId, read.Message);
        }

        private ProgressionSaveRequestResult ExecuteDelete(ProgressionSaveRequest request)
        {
            var delete = _store.DeleteSlot(request.SlotId);
            if (delete.Status == ProgressionSaveDeleteStatus.Deleted)
            {
                return ProgressionSaveRequestResult.Deleted(request, BackendId, delete.Message);
            }

            if (delete.Status == ProgressionSaveDeleteStatus.Missing)
            {
                return ProgressionSaveRequestResult.Missing(request, BackendId, delete.Message);
            }

            if (delete.Status == ProgressionSaveDeleteStatus.Rejected)
            {
                return ProgressionSaveRequestResult.Rejected(request, BackendId, delete.Message);
            }

            if (delete.Status == ProgressionSaveDeleteStatus.BackendUnavailable)
            {
                return ProgressionSaveRequestResult.BackendUnavailable(request, BackendId, delete.Message);
            }

            return ProgressionSaveRequestResult.FailedResult(request, BackendId, delete.Message);
        }
    }
}
