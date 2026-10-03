using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.SceneLifecycle;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Reset.Composition
{
    /// <summary>
    /// Composes Reset authoring surfaces from explicit Scene Lifecycle roots.
    /// It owns no Reset authority; the FrameworkRuntimeHost supplies narrow canonical ports.
    /// </summary>
    internal sealed class ResetProductBindingSceneLifecycleParticipant : ISceneLifecycleParticipant
    {
        private readonly IResetRegistrationRuntimePort _resetRegistrationRuntime;
        private readonly HashSet<UnityResetSubjectAdapter> _subjectAdapters = new();
        private readonly FrameworkLogger _logger;

        internal ResetProductBindingSceneLifecycleParticipant(
            IResetRegistrationRuntimePort resetRegistrationRuntime)
        {
            this._resetRegistrationRuntime = resetRegistrationRuntime ?? throw new ArgumentNullException(nameof(resetRegistrationRuntime));
            _logger = FrameworkLogger.Create<ResetProductBindingSceneLifecycleParticipant>();
        }

        public SceneCompositionResult OnSceneAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots)
        {
            UnityResetSubjectAdapterBindingResult adapterBinding = UnityResetSubjectAdapterBinding.TryBind(roots, _resetRegistrationRuntime);
            if (!adapterBinding.Succeeded)
            {
                string failure = BuildAvailableDiagnostic(scope, adapterBinding, default);
                _logger.Error("Reset Subject Scene Lifecycle composition rejected.", LogFields.Of(
                    LogFields.Field("operation", "SceneAvailable"),
                    LogFields.Field("scope", scope.Label),
                    LogFields.Field("issue", failure)));
                return SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Available, failure);
            }
            CollectSubjectAdapters(roots);
            RegistrationSummary registration = RefreshSubjectRegistrations("scene-available");

            string diagnostic = BuildAvailableDiagnostic(scope, adapterBinding, registration);

            bool hasAuthoredSurfaces = adapterBinding.AdapterCount > 0;
            if (!hasAuthoredSurfaces)
            {
                _logger.Debug("Reset Scene Lifecycle composition found no authored Reset surfaces.", LogFields.Of(
                    LogFields.Field("operation", "SceneAvailable"),
                    LogFields.Field("scope", scope.Label)));
                return SceneCompositionResult.Completed(
                    scope,
                    SceneCompositionOperation.Available,
                    diagnostic);
            }

            _logger.Info("Reset Scene Lifecycle composition completed.", LogFields.Of(
                LogFields.Field("operation", "SceneAvailable"),
                LogFields.Field("scope", scope.Label),
                LogFields.Field("subjectAdapters", adapterBinding.AdapterCount),
                LogFields.Field("newSubjectAdapters", adapterBinding.BoundCount),
                LogFields.Field("idempotentSubjectAdapters", adapterBinding.IdempotentCount),
                LogFields.Field("deferredSubjectAdapters", registration.DeferredSubjects),
                LogFields.Field("rejectedSubjectAdapters", adapterBinding.RejectedCount),
                LogFields.Field("activeRegisteredSubjects", registration.RegisteredSubjects),
                LogFields.Field("activeRegisteredParticipants", registration.RegisteredParticipants)));
            return SceneCompositionResult.Completed(
                scope,
                SceneCompositionOperation.Available,
                diagnostic);
        }

        public SceneCompositionResult OnSceneReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            List<UnityResetSubjectAdapter> adapters = CollectAdapters(roots);
            int releasedSubjects = 0;
            int releasedParticipants = 0;
            var releaseIssues = new List<string>();
            for (int index = 0; index < adapters.Count; index++)
            {
                UnityResetSubjectAdapter adapter = adapters[index];
                if (adapter.IsRegistered)
                {
                    int participantCount = adapter.RegisteredParticipantCount;
                    if (adapter.ClearRegistration("scope-lifecycle-release:" + reason.NormalizeTextOrFallback("scope-release")))
                    {
                        releasedSubjects++;
                        releasedParticipants += participantCount;
                    }
                    else
                    {
                        releaseIssues.Add($"subjectAdapter='{adapter.name}' registration release failed.");
                    }
                }

                if (adapter.TryUnbindResetRegistrationRuntime(
                        _resetRegistrationRuntime,
                        out string detachIssue))
                {
                    _subjectAdapters.Remove(adapter);
                }
                else
                {
                    releaseIssues.Add($"subjectAdapter='{adapter.name}' detach='{detachIssue}'.");
                }
            }

            string diagnostic = $"Reset Subject Scene Lifecycle release {(releaseIssues.Count == 0 ? "completed" : "failed")}. scope='{scope.Label}' subjectAdapters='{adapters.Count}' registeredSubjectsReleased='{releasedSubjects}' registeredParticipantsReleased='{releasedParticipants}'.";
            if (releaseIssues.Count > 0)
            {
                diagnostic += " " + string.Join(" ", releaseIssues);
                _logger.Error("Reset Scene Lifecycle release failed.", LogFields.Of(
                    LogFields.Field("operation", "ScopeReleasing"),
                    LogFields.Field("scope", scope.Label),
                    LogFields.Field("issue", diagnostic)));
                return SceneCompositionResult.Rejected(
                    scope,
                    SceneCompositionOperation.Releasing,
                    diagnostic);
            }

            if (releasedSubjects == 0 && releasedParticipants == 0)
            {
                _logger.Debug("Reset Scene Lifecycle release completed with no state changes.", LogFields.Of(
                    LogFields.Field("operation", "ScopeReleasing"),
                    LogFields.Field("scope", scope.Label),
                    LogFields.Field("reason", reason.NormalizeTextOrFallback("scope-release"))));
                return SceneCompositionResult.Completed(
                    scope,
                    SceneCompositionOperation.Releasing,
                    diagnostic);
            }

            _logger.Info("Reset Scene Lifecycle release completed.", LogFields.Of(
                LogFields.Field("operation", "ScopeReleasing"),
                LogFields.Field("scope", scope.Label),
                LogFields.Field("reason", reason.NormalizeTextOrFallback("scope-release")),
                LogFields.Field("subjectAdapters", adapters.Count),
                LogFields.Field("registeredSubjectsReleased", releasedSubjects),
                LogFields.Field("registeredParticipantsReleased", releasedParticipants)));
            return SceneCompositionResult.Completed(
                scope,
                SceneCompositionOperation.Releasing,
                diagnostic);
        }

        internal void RefreshSubjectRegistrationsForCurrentOwners(string reason)
        {
            RefreshSubjectRegistrations(reason.NormalizeTextOrFallback("runtime-owner-refresh"));
        }

        private RegistrationSummary RefreshSubjectRegistrations(string reason)
        {
            int registeredSubjects = 0;
            int registeredParticipants = 0;
            int deferredSubjects = 0;
            foreach (UnityResetSubjectAdapter adapter in _subjectAdapters)
            {
                if (adapter == null)
                {
                    continue;
                }

                bool wasRegistered = adapter.IsRegistered;
                if (!adapter.RefreshRegistrationForCurrentOwner(reason))
                {
                    if (adapter.LastRegistrationOutcome == ResetSubjectRegistrationOutcome.DeferredOwnerUnavailable)
                    {
                        deferredSubjects++;
                        _logger.Debug("Reset Subject registration deferred until runtime owner becomes available.", LogFields.Of(
                            LogFields.Field("adapter", adapter.name),
                            LogFields.Field("scope", adapter.Scope),
                            LogFields.Field("refreshReason", reason),
                            LogFields.Field("retryEnabled", true),
                            LogFields.Field("currentOwnerAvailable", false),
                            LogFields.Field("outcome", "DeferredOwnerUnavailable")));
                    }
                    else
                    {
                        _logger.Warning("Reset Subject registration rejected.", LogFields.Of(
                            LogFields.Field("adapter", adapter.name),
                            LogFields.Field("scope", adapter.Scope)));
                    }
                    continue;
                }

                if (!wasRegistered && adapter.IsRegistered)
                {
                    _logger.Info("Reset Subject registration completed.", LogFields.Of(
                        LogFields.Field("adapter", adapter.name),
                        LogFields.Field("subjectId", adapter.SubjectId.StableText),
                        LogFields.Field("scope", adapter.Scope),
                        LogFields.Field("participants", adapter.RegisteredParticipantCount)));
                }

                if (adapter.IsRegistered)
                {
                    registeredSubjects++;
                    registeredParticipants += adapter.RegisteredParticipantCount;
                }
            }

            return new RegistrationSummary(registeredSubjects, registeredParticipants, deferredSubjects);
        }

        private void CollectSubjectAdapters(IReadOnlyList<GameObject> roots)
        {
            foreach (UnityResetSubjectAdapter adapter in CollectAdapters(roots))
            {
                _subjectAdapters.Add(adapter);
            }
        }

        private static List<UnityResetSubjectAdapter> CollectAdapters(IReadOnlyList<GameObject> roots)
        {
            var adapters = new List<UnityResetSubjectAdapter>();
            var unique = new HashSet<UnityResetSubjectAdapter>();
            if (roots == null) return adapters;
            for (int index = 0; index < roots.Count; index++)
            {
                GameObject root = roots[index];
                if (root == null) continue;
                foreach (UnityResetSubjectAdapter adapter in root.GetComponentsInChildren<UnityResetSubjectAdapter>(true))
                    if (adapter != null && unique.Add(adapter)) adapters.Add(adapter);
            }
            return adapters;
        }

        private static string BuildAvailableDiagnostic(SceneCompositionScope scope, UnityResetSubjectAdapterBindingResult adapterBinding, RegistrationSummary registration) =>
            $"Reset Scene Lifecycle composition completed. operation='Available' scope='{scope.Label}' subjectAdapters='{adapterBinding.AdapterCount}' newSubjectAdapters='{adapterBinding.BoundCount}' idempotentSubjectAdapters='{adapterBinding.IdempotentCount}' rejectedSubjectAdapters='{adapterBinding.RejectedCount}' activeRegisteredSubjects='{registration.RegisteredSubjects}' activeRegisteredParticipants='{registration.RegisteredParticipants}'.";

        private readonly struct RegistrationSummary
        {
            internal RegistrationSummary(int registeredSubjects, int registeredParticipants, int deferredSubjects)
            {
                RegisteredSubjects = registeredSubjects;
                RegisteredParticipants = registeredParticipants;
                DeferredSubjects = deferredSubjects;
            }

            internal int RegisteredSubjects { get; }
            internal int RegisteredParticipants { get; }
            internal int DeferredSubjects { get; }
        }
    }
}
