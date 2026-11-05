using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.RuntimeContent
{
    internal sealed class ObjectEntryCompositionRuntimeBinding
    {
        private readonly StableObjectBinding _target;
        private readonly ObjectEntryCompositionProfile _profile;
        private readonly Func<StableObjectBinding, bool> _isTargetCurrent;
        private readonly CompositionComposer _composer;
        private readonly CompositionContribution _baseContribution;
        private readonly ObjectEntryCompositionPartMaterializer _materializer;

        private Task<CompositionResult> _closeTask;
        private CompositionResult _closeResult;
        private string _closeIssue = string.Empty;
        private bool _closeContextConfirmed;
        private bool _cleanupComplete;
        private bool _writeAccessInvalidated;
        private bool _active;

        internal ObjectEntryCompositionRuntimeBinding(
            StableObjectBinding target,
            ObjectEntryCompositionProfile profile,
            Func<StableObjectBinding, bool> isTargetCurrent,
            Func<UnityEngine.GameObject, Task<bool>> destroyAndConfirmAsync = null)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _isTargetCurrent = isTargetCurrent ?? throw new ArgumentNullException(nameof(isTargetCurrent));
            _composer = new CompositionComposer();
            _baseContribution = _composer.CreateContribution(
                CompositionOwnerId.Create("framework-object-entry-base:" + Guid.NewGuid().ToString("N")),
                int.MinValue);
            _materializer = new ObjectEntryCompositionPartMaterializer(
                IsTargetCurrent,
                destroyAndConfirmAsync);
        }

        internal bool IsActive => _active;

        internal bool CleanupComplete => _cleanupComplete;

        internal bool HasOutstandingAcquiredInstances =>
            _materializer.HasOutstandingAcquiredInstances;

        internal void InvalidateWriteAccess()
        {
            _writeAccessInvalidated = true;
        }

        internal async Task<string> ActivateAsync()
        {
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
                        part);
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

        internal async Task<string> CloseAndReconcileAsync()
        {
            InvalidateWriteAccess();
            _active = false;
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
                        _closeResult.Status is CompositionResultStatus.Committed or
                            CompositionResultStatus.NoChange or
                            CompositionResultStatus.Degraded;
                    if (!_closeContextConfirmed)
                    {
                        _closeIssue = BuildIssue("Composition Composer close did not confirm a closed context.", _closeResult);
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

            if (string.IsNullOrEmpty(reconciliationIssue))
            {
                _active = false;
                _closeIssue = string.Empty;
                _cleanupComplete = true;
                return string.Empty;
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
