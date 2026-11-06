using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.RuntimeContent
{
    internal sealed class ObjectEntryCompositionRuntimeBinding
    {
        private sealed class PendingDynamicOperation
        {
            internal PendingDynamicOperation(CancellationTokenSource cancellation)
            {
                Cancellation = cancellation;
                Completion = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            internal CancellationTokenSource Cancellation { get; }
            internal TaskCompletionSource<bool> Completion { get; }
        }

        private readonly StableObjectBinding _target;
        private readonly ObjectEntryCompositionProfile _profile;
        private readonly bool _allowsDynamicContributions;
        private readonly Func<StableObjectBinding, bool> _isTargetCurrent;
        private readonly CompositionComposer _composer;
        private readonly CompositionContribution _baseContribution;
        private readonly ObjectEntryCompositionPartMaterializer _materializer;
        private readonly object _dynamicOperationSync = new();
        private readonly List<PendingDynamicOperation> _pendingDynamicOperations = new();

        private Task<CompositionResult> _closeTask;
        private Task<string> _closeAndReconcileTask;
        private CompositionResult _closeResult;
        private string _closeIssue = string.Empty;
        private bool _closeContextConfirmed;
        private bool _cleanupComplete;
        private bool _writeAccessInvalidated;
        private bool _dynamicOperationsInvalidated;
        private string _dynamicCancellationIssue = string.Empty;
        private bool _active;

        internal ObjectEntryCompositionRuntimeBinding(
            StableObjectBinding target,
            ObjectEntryCompositionProfile profile,
            Func<StableObjectBinding, bool> isTargetCurrent,
            Func<UnityEngine.GameObject, Task<bool>> destroyAndConfirmAsync = null,
            bool allowDynamicContributions = false)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            if (_target.HasAuthoredProfile && profile == null)
                throw new ArgumentException("The authored Object Entry Base Profile was destroyed before its runtime context could be created.", nameof(profile));
            if (!_target.HasAuthoredProfile && !allowDynamicContributions)
                throw new ArgumentException("A profileless Object Entry composition runtime requires explicit dynamic opt-in.", nameof(profile));
            if (_target.HasAuthoredProfile != !ReferenceEquals(profile, null))
                throw new ArgumentException("The Object Entry runtime profile does not match the exact authored configuration.", nameof(profile));

            _profile = profile;
            _allowsDynamicContributions = allowDynamicContributions;
            _isTargetCurrent = isTargetCurrent ?? throw new ArgumentNullException(nameof(isTargetCurrent));
            _composer = new CompositionComposer();
            _baseContribution = profile != null
                ? _composer.CreateContribution(
                    CompositionOwnerId.Create("framework-object-entry-base:" + Guid.NewGuid().ToString("N")),
                    int.MinValue)
                : null;
            _materializer = new ObjectEntryCompositionPartMaterializer(
                IsTargetCurrent,
                destroyAndConfirmAsync);
        }

        internal bool IsActive => _active;

        internal long Revision => _composer.Revision;

        internal bool CleanupComplete => _cleanupComplete;

        internal bool HasOutstandingAcquiredInstances =>
            _materializer.HasOutstandingAcquiredInstances;

        internal bool TryCreateDynamicContribution(
            int priority,
            out CompositionContribution contribution,
            out string issue)
        {
            contribution = null;
            if (!_allowsDynamicContributions)
            {
                issue = "Dynamic Object Entry composition was not explicitly enabled for this exact occurrence.";
                return false;
            }
            if (priority == int.MinValue)
            {
                issue = "Dynamic Object Entry composition priority cannot use the reserved B2 Base priority.";
                return false;
            }
            if (_writeAccessInvalidated || _dynamicOperationsInvalidated ||
                _closeTask != null || _closeContextConfirmed || _cleanupComplete ||
                !_isTargetCurrent(_target))
            {
                issue = "Dynamic Object Entry composition cannot open a contribution after its exact occurrence or context was invalidated.";
                return false;
            }

            try
            {
                contribution = _composer.CreateContribution(
                    CompositionOwnerId.Create("framework-object-entry-contribution:" + Guid.NewGuid().ToString("N")),
                    priority);
                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                issue = $"Dynamic Object Entry composition contribution could not open. {exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        internal bool TryCreateDynamicPartDefinition(
            ObjectEntryCompositionPartDefinition part,
            out PartDefinition definition,
            out string issue)
        {
            definition = null;
            issue = string.Empty;
            if (part == null || !ObjectEntryCompositionPartValidation.TryValidate(
                    part.Key,
                    part.Prefab,
                    part.Dependencies,
                    requirePrefabAsset: true,
                    out ContentKey key,
                    out ContentKey[] dependencies,
                    out issue))
            {
                if (string.IsNullOrEmpty(issue))
                    issue = "A dynamic Object Entry composition part definition is required.";
                return false;
            }

            var descriptor = new ObjectEntryCompositionPartMaterializer.PartBinding(
                _target,
                part.Prefab);
            definition = new PartDefinition(key, descriptor, _materializer, dependencies);
            issue = string.Empty;
            return true;
        }

        internal Task<CompositionResult> SetDynamicPartAsync(
            CompositionContribution contribution,
            PartDefinition definition,
            Func<bool> isConsumerCurrent,
            CancellationToken cancellationToken)
        {
            return ExecuteTrackedDynamicOperationAsync(
                isConsumerCurrent,
                cancellationToken,
                operationToken => contribution.SetPartWhileCurrentAsync(
                    definition, isConsumerCurrent, operationToken));
        }

        internal Task<CompositionResult> RemoveDynamicPartAsync(
            CompositionContribution contribution,
            ContentKey key,
            Func<bool> isConsumerCurrent,
            CancellationToken cancellationToken)
        {
            return ExecuteTrackedDynamicOperationAsync(
                isConsumerCurrent,
                cancellationToken,
                operationToken => contribution.RemovePartWhileCurrentAsync(
                    key, isConsumerCurrent, operationToken));
        }

        internal Task<CompositionResult> CloseDynamicContributionAsync(
            CompositionContribution contribution,
            Func<bool> isConsumerCurrent,
            CancellationToken cancellationToken)
        {
            return ExecuteTrackedDynamicOperationAsync(
                isConsumerCurrent,
                cancellationToken,
                operationToken => contribution.CloseAsync(operationToken));
        }

        private Task<CompositionResult> ExecuteTrackedDynamicOperationAsync(
            Func<bool> isConsumerCurrent,
            CancellationToken consumerCancellationToken,
            Func<CancellationToken, Task<CompositionResult>> startOperation)
        {
            PendingDynamicOperation pending;
            lock (_dynamicOperationSync)
            {
                if (_dynamicOperationsInvalidated || _writeAccessInvalidated ||
                    _closeAndReconcileTask != null || _cleanupComplete)
                {
                    return Task.FromResult<CompositionResult>(null);
                }

                CancellationTokenSource operationCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(consumerCancellationToken);
                pending = new PendingDynamicOperation(operationCancellation);
                _pendingDynamicOperations.Add(pending);
            }

            return RunTrackedDynamicOperationAsync(pending, isConsumerCurrent, startOperation);
        }

        private async Task<CompositionResult> RunTrackedDynamicOperationAsync(
            PendingDynamicOperation pending,
            Func<bool> isConsumerCurrent,
            Func<CancellationToken, Task<CompositionResult>> startOperation)
        {
            try
            {
                return await _materializer.ExecuteConsumerOperationAsync(
                    _target,
                    isConsumerCurrent,
                    () => startOperation(pending.Cancellation.Token));
            }
            finally
            {
                try
                {
                    pending.Cancellation.Dispose();
                }
                finally
                {
                    lock (_dynamicOperationSync)
                    {
                        pending.Completion.TrySetResult(true);
                        _pendingDynamicOperations.Remove(pending);
                    }
                }
            }
        }

        internal static bool IsRecoverableCloseFailure(
            CompositionResult result,
            CompositionContextId expectedContextId,
            bool materializerConfirmedProtectedRootFailure)
        {
            return materializerConfirmedProtectedRootFailure &&
                result != null &&
                expectedContextId.IsValid &&
                result.ContextId == expectedContextId &&
                result.OperationId.IsValid &&
                result.Status == CompositionResultStatus.Faulted &&
                !result.EffectsMayHaveApplied &&
                result.Issues.Count == 1 &&
                result.Issues[0].Code == CompositionIssueCode.DetachFailed &&
                result.Issues[0].Key.IsValid;
        }

        internal void InvalidateWriteAccess()
        {
            lock (_dynamicOperationSync)
            {
                _writeAccessInvalidated = true;
            }
            BlockDynamicOperations();
        }

        internal void BlockDynamicOperations()
        {
            PendingDynamicOperation[] pending;
            lock (_dynamicOperationSync)
            {
                if (_dynamicOperationsInvalidated)
                {
                    return;
                }

                _dynamicOperationsInvalidated = true;
                pending = _pendingDynamicOperations.ToArray();
            }

            for (int index = 0; index < pending.Length; index++)
            {
                try
                {
                    pending[index].Cancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // A disposed source can only belong to an operation already in its completion finally.
                }
                catch (Exception exception)
                {
                    lock (_dynamicOperationSync)
                    {
                        _dynamicCancellationIssue = AppendIssue(
                            _dynamicCancellationIssue,
                            $"Dynamic Object Entry operation cancellation callback threw '{exception.GetType().Name}: {exception.Message}'.");
                    }
                }
            }
        }

        internal async Task<string> ActivateAsync()
        {
            if (_target.HasAuthoredProfile && _profile == null)
            {
                return "Object Entry composition Base activation failed because its authored Profile was destroyed.";
            }

            if (!_target.HasAuthoredProfile)
            {
                return "A profileless Object Entry composition context is opened lazily and has no Base activation.";
            }
            if (_writeAccessInvalidated || !_isTargetCurrent(_target))
            {
                return "Object Entry composition could not activate because the exact admitted binding is no longer current.";
            }

            if (!_profile.TryValidate(out string validationIssue))
            {
                return "Object Entry composition profile became invalid after preflight. " + validationIssue;
            }

            try
            {
                IReadOnlyList<ObjectEntryCompositionPart> authoredParts = _profile.Parts;
                var definitions = new List<PartDefinition>(authoredParts.Count);
                foreach (ObjectEntryCompositionPart part in authoredParts)
                {
                    ContentKey key = ContentKey.Create(part.Key);
                    ContentKey[] dependencies = part.Dependencies.Select(ContentKey.Create).ToArray();
                    var descriptor = new ObjectEntryCompositionPartMaterializer.PartBinding(
                        _target,
                        part.Prefab);
                    definitions.Add(new PartDefinition(key, descriptor, _materializer, dependencies));
                }

                CompositionResult result = await _baseContribution.SetPartsWhileCurrentAsync(
                    definitions,
                    () => IsTargetCurrent(_target));
                if (IsSuccessful(result))
                {
                    _active = true;
                    return string.Empty;
                }

                string issue = BuildIssue(
                    "Object Entry composition initial materialization did not commit.",
                    result);
                string cleanupIssue = await CloseAndReconcileAsync();
                return AppendIssue(issue, cleanupIssue);
            }
            catch (Exception exception)
            {
                string issue =
                    $"Object Entry composition activation threw '{exception.GetType().Name}': {exception.Message}";
                string cleanupIssue = await CloseAndReconcileAsync();
                return AppendIssue(issue, cleanupIssue);
            }
        }

        internal Task<string> CloseAndReconcileAsync()
        {
            TaskCompletionSource<string> completion;
            lock (_dynamicOperationSync)
            {
                if (_closeAndReconcileTask != null)
                {
                    return _closeAndReconcileTask;
                }

                completion = new TaskCompletionSource<string>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                _closeAndReconcileTask = completion.Task;
            }

            InvalidateWriteAccess();
            _active = false;
            _ = CompleteCloseAndReconcileAsync(completion);
            return completion.Task;
        }

        private async Task CompleteCloseAndReconcileAsync(
            TaskCompletionSource<string> completion)
        {
            string issue;
            try
            {
                Task[] pendingOperations;
                lock (_dynamicOperationSync)
                {
                    pendingOperations = _pendingDynamicOperations
                        .Select(operation => operation.Completion.Task)
                        .ToArray();
                }

                await Task.WhenAll(pendingOperations);
                issue = await CloseComposerAndReconcileAsync();

                string cancellationIssue;
                lock (_dynamicOperationSync)
                {
                    cancellationIssue = _dynamicCancellationIssue;
                    _dynamicCancellationIssue = string.Empty;
                }
                issue = AppendIssue(issue, cancellationIssue);
            }
            catch (Exception exception)
            {
                issue = AppendIssue(
                    _closeIssue,
                    $"Object Entry composition drain or reconciliation threw '{exception.GetType().Name}: {exception.Message}'.");
            }

            lock (_dynamicOperationSync)
            {
                if ((!_cleanupComplete || !string.IsNullOrEmpty(issue)) &&
                    ReferenceEquals(_closeAndReconcileTask, completion.Task))
                {
                    _closeAndReconcileTask = null;
                }
            }

            completion.TrySetResult(issue);
        }

        private async Task<string> CloseComposerAndReconcileAsync()
        {
            if (_closeTask == null && !_closeContextConfirmed && string.IsNullOrEmpty(_closeIssue))
            {
                try
                {
                    _closeTask = _composer.CloseAsync();
                }
                catch (Exception exception)
                {
                    _closeIssue =
                        $"Composition Composer close threw '{exception.GetType().Name}': {exception.Message}";
                }
            }

            if (_closeTask != null && _closeResult == null && string.IsNullOrEmpty(_closeIssue))
            {
                try
                {
                    _closeResult = await _closeTask;
                    _closeContextConfirmed = _closeResult != null &&
                        _closeResult.ContextId == _composer.ContextId &&
                        _closeResult.Status is CompositionResultStatus.Committed or
                            CompositionResultStatus.NoChange or
                            CompositionResultStatus.Degraded;
                    if (!_closeContextConfirmed)
                    {
                        bool materializerConfirmedProtectedRootFailure =
                            _materializer.ConfirmsProtectedRootCloseFailure(_closeResult);
                        if (IsRecoverableCloseFailure(
                            _closeResult,
                            _composer.ContextId,
                            materializerConfirmedProtectedRootFailure))
                        {
                            CompositionResult closeConfirmation = await _composer.CloseAsync();
                            _closeContextConfirmed = closeConfirmation != null &&
                                closeConfirmation.ContextId == _composer.ContextId &&
                                closeConfirmation.Status == CompositionResultStatus.NoChange;
                            if (!_closeContextConfirmed)
                            {
                                _closeIssue = BuildIssue(
                                    "Composition Composer did not confirm its previously faulted context as closed.",
                                    closeConfirmation);
                            }
                        }
                        else
                        {
                            _closeIssue = BuildIssue(
                                "Composition Composer close did not confirm a closed context.",
                                _closeResult);
                        }
                    }
                }
                catch (Exception exception)
                {
                    _closeIssue =
                        $"Composition Composer close faulted '{exception.GetType().Name}': {exception.Message}";
                }
            }

            if (!_closeContextConfirmed)
            {
                return _closeIssue.Length == 0
                    ? "Composition Composer close has not confirmed completion."
                    : _closeIssue;
            }

            string reconciliationIssue;
            try
            {
                reconciliationIssue = await _materializer.ReconcileOutstandingAcquiredInstancesAsync();
            }
            catch (Exception exception)
            {
                reconciliationIssue =
                    $"Acquired Object Entry composition resource reconciliation faulted '{exception.GetType().Name}': {exception.Message}";
            }

            bool noPendingAcquiredInstances;
            try
            {
                noPendingAcquiredInstances = !_materializer.HasOutstandingAcquiredInstances;
            }
            catch (Exception exception)
            {
                reconciliationIssue = AppendIssue(
                    reconciliationIssue,
                    $"Acquired Object Entry composition reconciliation state could not be confirmed '{exception.GetType().Name}': {exception.Message}");
                noPendingAcquiredInstances = false;
            }

            if (string.IsNullOrEmpty(reconciliationIssue) && noPendingAcquiredInstances)
            {
                _active = false;
                _closeIssue = string.Empty;
                _cleanupComplete = true;
                return string.Empty;
            }

            if (string.IsNullOrEmpty(reconciliationIssue))
            {
                reconciliationIssue = "Acquired Object Entry composition cleanup is incomplete because physical resources remain pending.";
            }

            _closeIssue = AppendIssue(_closeIssue, reconciliationIssue);
            return _closeIssue;
        }

        private bool IsTargetCurrent(StableObjectBinding binding)
        {
            return !_writeAccessInvalidated && ReferenceEquals(binding, _target) &&
                _isTargetCurrent(binding);
        }

        private static bool IsSuccessful(CompositionResult result)
        {
            return result != null && result.Status is
                CompositionResultStatus.Committed or CompositionResultStatus.NoChange;
        }

        private static string BuildIssue(string prefix, CompositionResult result)
        {
            string issues = result == null
                ? "No Composer result was returned."
                : string.Join(" | ", result.Issues.Select(issue => issue.ToString()));
            return $"{prefix} status='{result?.Status}' issues='{issues}'.";
        }

        private static string AppendIssue(string current, string next)
        {
            return string.IsNullOrEmpty(next)
                ? current
                : string.IsNullOrEmpty(current) ? next : current + " " + next;
        }
    }
}
