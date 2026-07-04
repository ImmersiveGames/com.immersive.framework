using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for the preview.12A ResetRegistry core.
    /// It validates the new Reset module in isolation and does not use ObjectEntry, ObjectResetTrigger,
    /// This validates ResetRegistry without old reset target resolution, ActivityRestartTrigger or runtime participation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "preview.12A ResetRegistry synthetic smoke; Reset core only.")]
    internal static class ResetRegistryQaSmokeRunner
    {
        internal const string SmokeName = "Reset Registry Synthetic Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(ResetRegistryQaSmokeRunner));

            bool subjectRegistrationPassed = ValidateSubjectRegistration(logger, normalizedSource);
            bool runtimeIdGenerationPassed = ValidateRuntimeIdGeneration(logger, normalizedSource);
            bool participantRegistrationPassed = ValidateParticipantRegistration(logger, normalizedSource);
            bool unregisterPassed = ValidateUnregister(logger, normalizedSource);
            bool duplicateRejectionPassed = ValidateDuplicateSubjectRejection(logger, normalizedSource);
            bool staleCleanupPassed = ValidateStaleOwnerCleanup(logger, normalizedSource);
            bool invalidInputsPassed = ValidateInvalidInputs(logger, normalizedSource);

            bool passed = subjectRegistrationPassed
                && runtimeIdGenerationPassed
                && participantRegistrationPassed
                && unregisterPassed
                && duplicateRejectionPassed
                && staleCleanupPassed
                && invalidInputsPassed;

            logger.Info(
                "QA Reset Registry Synthetic Smoke completed.",
                LogFields.Of(
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("source", normalizedSource),
                    LogFields.Field("subjectRegistration", subjectRegistrationPassed),
                    LogFields.Field("runtimeIdGeneration", runtimeIdGenerationPassed),
                    LogFields.Field("participantRegistration", participantRegistrationPassed),
                    LogFields.Field("unregister", unregisterPassed),
                    LogFields.Field("duplicateRejection", duplicateRejectionPassed),
                    LogFields.Field("staleCleanup", staleCleanupPassed),
                    LogFields.Field("invalidInputs", invalidInputsPassed)));

            return Task.FromResult(passed);
        }

        private static bool ValidateSubjectRegistration(FrameworkLogger logger, string source)
        {
            var registry = new ResetRegistry();
            object ownerObject = new object();
            var routeOwner = RuntimeContentOwner.Route("qa.reset.route", "QA Reset Route");
            var subject = ResetSubject.SceneRoute(
                "qa.reset.subject.scene.route",
                routeOwner,
                "QA Scene Route Subject",
                "Smoke:subject-registration");

            var result = registry.RegisterSubject(subject, ownerObject, source, "qa.reset-registry.subject-registration");
            bool byIdFound = registry.TryGetSubject(subject.SubjectId, out ResetSubject byId) && byId.SubjectId == subject.SubjectId;
            bool byScopeFound = registry.GetSubjectsByScope(ResetSubjectScope.Route).Count == 1;
            bool byOwnerFound = registry.GetSubjectsByOwner(routeOwner).Count == 1;

            bool passed = result.Succeeded
                && registry.SubjectCount == 1
                && registry.ParticipantCount == 0
                && byIdFound
                && byScopeFound
                && byOwnerFound;

            LogRegistryStep(
                logger,
                "subject-registration",
                passed,
                registry,
                result.Status.ToString(),
                result.IssueCount,
                "scene-authored subject can register and be queried by id, scope and owner");
            return passed;
        }

        private static bool ValidateRuntimeIdGeneration(FrameworkLogger logger, string source)
        {
            var registry = new ResetRegistry();
            object ownerObject = new object();
            var activityOwner = RuntimeContentOwner.Activity("qa.reset.activity", "QA Reset Activity");

            var first = registry.RegisterRuntimeSubject(
                "qa.reset.runtime.box",
                ResetSubjectScope.Runtime,
                activityOwner,
                ownerObject,
                "QA Runtime Box A",
                "Smoke:runtime-box",
                source,
                "qa.reset-registry.runtime-first");

            var second = registry.RegisterRuntimeSubject(
                "qa.reset.runtime.box",
                ResetSubjectScope.Runtime,
                activityOwner,
                ownerObject,
                "QA Runtime Box B",
                "Smoke:runtime-box",
                source,
                "qa.reset-registry.runtime-second");

            bool idsAreMonotonic = string.Equals(first.Subject.SubjectId.StableText, "qa.reset.runtime.box#1", StringComparison.Ordinal)
                && string.Equals(second.Subject.SubjectId.StableText, "qa.reset.runtime.box#2", StringComparison.Ordinal);
            bool idsAreDistinct = first.Subject.SubjectId != second.Subject.SubjectId;
            bool runtimeScopeFound = registry.GetSubjectsByScope(ResetSubjectScope.Runtime).Count == 2;
            bool runtimeOriginFound = registry.GetSubjectsByOrigin(ResetSubjectOrigin.RuntimeRegistered).Count == 2;

            bool passed = first.Succeeded
                && second.Succeeded
                && registry.SubjectCount == 2
                && idsAreMonotonic
                && idsAreDistinct
                && runtimeScopeFound
                && runtimeOriginFound;

            LogRegistryStep(
                logger,
                "runtime-id-generation",
                passed,
                registry,
                $"{first.Status}/{second.Status}",
                first.IssueCount + second.IssueCount,
                $"first='{GetSubjectId(first)}' second='{GetSubjectId(second)}'");
            return passed;
        }

        private static bool ValidateParticipantRegistration(FrameworkLogger logger, string source)
        {
            var registry = new ResetRegistry();
            object ownerObject = new object();
            var activityOwner = RuntimeContentOwner.Activity("qa.reset.activity", "QA Reset Activity");
            var subject = ResetSubject.SceneActivity(
                "qa.reset.subject.scene.activity",
                activityOwner,
                "QA Scene Activity Subject",
                "Smoke:participant-registration");

            var subjectResult = registry.RegisterSubject(subject, ownerObject, source, "qa.reset-registry.participant-subject");
            var participant = new SyntheticResetParticipant(
                "transform",
                ResetParticipantRequiredness.Required,
                0,
                SyntheticResetParticipantMode.Success);
            var participantResult = registry.RegisterParticipant(
                subjectResult.Handle,
                participant,
                ownerObject,
                source,
                "qa.reset-registry.participant-registration");

            bool participantFound = registry.TryGetParticipant(
                participantResult.Handle,
                out ResetParticipantDescriptor descriptor,
                out IResetParticipant registeredParticipant)
                && descriptor.ParticipantId == ResetParticipantId.From("transform")
                && ReferenceEquals(participant, registeredParticipant);

            bool bySubjectHandleFound = registry.GetParticipants(subjectResult.Handle).Count == 1;
            bool bySubjectIdFound = registry.GetParticipants(subject.SubjectId).Count == 1;
            bool participantResetPassed = false;
            if (participantFound)
            {
                var resetResult = registeredParticipant.Reset(new ResetContext(subject, descriptor, source, "qa.reset-registry.participant-reset"));
                participantResetPassed = resetResult.Succeeded && !resetResult.BlocksReset;
            }

            bool passed = subjectResult.Succeeded
                && participantResult.Succeeded
                && registry.SubjectCount == 1
                && registry.ParticipantCount == 1
                && participantFound
                && bySubjectHandleFound
                && bySubjectIdFound
                && participantResetPassed;

            LogRegistryStep(
                logger,
                "participant-registration",
                passed,
                registry,
                $"{subjectResult.Status}/{participantResult.Status}",
                subjectResult.IssueCount + participantResult.IssueCount,
                "participant registers under a subject and remains synchronous");
            return passed;
        }

        private static bool ValidateUnregister(FrameworkLogger logger, string source)
        {
            var registry = new ResetRegistry();
            object ownerObject = new object();
            var routeOwner = RuntimeContentOwner.Route("qa.reset.route", "QA Reset Route");
            var subject = ResetSubject.SceneRoute(
                "qa.reset.subject.unregister",
                routeOwner,
                "QA Unregister Subject",
                "Smoke:unregister");

            var subjectResult = registry.RegisterSubject(subject, ownerObject, source, "qa.reset-registry.unregister-subject");
            var participant = new SyntheticResetParticipant("transform", ResetParticipantRequiredness.Required, 0, SyntheticResetParticipantMode.Success);
            var participantResult = registry.RegisterParticipant(subjectResult.Handle, participant, ownerObject, source, "qa.reset-registry.unregister-participant");

            var participantUnregister = registry.Unregister(participantResult.Handle, ownerObject, source, "qa.reset-registry.unregister-participant");
            bool participantRemoved = registry.ParticipantCount == 0 && registry.GetParticipants(subjectResult.Handle).Count == 0;

            var subjectUnregister = registry.Unregister(subjectResult.Handle, ownerObject, source, "qa.reset-registry.unregister-subject");
            bool subjectRemoved = registry.SubjectCount == 0 && !registry.TryGetSubject(subject.SubjectId, out _);

            var secondSubjectUnregister = registry.Unregister(subjectResult.Handle, ownerObject, source, "qa.reset-registry.unregister-subject-again");

            bool passed = subjectResult.Succeeded
                && participantResult.Succeeded
                && participantUnregister.Succeeded
                && participantRemoved
                && subjectUnregister.Succeeded
                && subjectRemoved
                && secondSubjectUnregister.Status == ResetRegistryOperationStatus.AlreadyUnregistered;

            LogRegistryStep(
                logger,
                "unregister",
                passed,
                registry,
                $"{participantUnregister.Status}/{subjectUnregister.Status}/{secondSubjectUnregister.Status}",
                participantUnregister.IssueCount + subjectUnregister.IssueCount + secondSubjectUnregister.IssueCount,
                "participant and subject unregister are deterministic and idempotent");
            return passed;
        }

        private static bool ValidateDuplicateSubjectRejection(FrameworkLogger logger, string source)
        {
            var registry = new ResetRegistry();
            object ownerObject = new object();
            var routeOwner = RuntimeContentOwner.Route("qa.reset.route", "QA Reset Route");
            var first = ResetSubject.SceneRoute(
                "qa.reset.subject.duplicate",
                routeOwner,
                "QA Duplicate Subject A",
                "Smoke:duplicate-a");
            var duplicate = ResetSubject.SceneRoute(
                "qa.reset.subject.duplicate",
                routeOwner,
                "QA Duplicate Subject B",
                "Smoke:duplicate-b");

            var firstResult = registry.RegisterSubject(first, ownerObject, source, "qa.reset-registry.duplicate-first");
            var duplicateResult = registry.RegisterSubject(duplicate, ownerObject, source, "qa.reset-registry.duplicate-second");

            bool passed = firstResult.Succeeded
                && duplicateResult.Failed
                && duplicateResult.Status == ResetRegistryOperationStatus.RejectedDuplicateSubjectId
                && duplicateResult.BlockingIssueCount == 1
                && registry.SubjectCount == 1;

            LogRegistryStep(
                logger,
                "duplicate-subject-rejection",
                passed,
                registry,
                $"{firstResult.Status}/{duplicateResult.Status}",
                firstResult.IssueCount + duplicateResult.IssueCount,
                "duplicate scene-authored subject id in the same context is rejected");
            return passed;
        }

        private static bool ValidateStaleOwnerCleanup(FrameworkLogger logger, string source)
        {
#if UNITY_EDITOR
            var registry = new ResetRegistry();
            var ownerObject = new GameObject("QA_ResetRegistry_StaleOwner");
            var routeOwner = RuntimeContentOwner.Route("qa.reset.route", "QA Reset Route");
            var subject = ResetSubject.SceneRoute(
                "qa.reset.subject.stale",
                routeOwner,
                "QA Stale Subject",
                "Smoke:stale-owner");

            var subjectResult = registry.RegisterSubject(subject, ownerObject, source, "qa.reset-registry.stale-subject");
            var participant = new SyntheticResetParticipant("transform", ResetParticipantRequiredness.Required, 0, SyntheticResetParticipantMode.Success);
            var participantResult = registry.RegisterParticipant(subjectResult.Handle, participant, ownerObject, source, "qa.reset-registry.stale-participant");

            Object.DestroyImmediate(ownerObject);
            var cleanup = registry.CleanupStaleOwners();

            bool passed = subjectResult.Succeeded
                && participantResult.Succeeded
                && cleanup.RemovedSubjects == 1
                && cleanup.RemovedParticipants == 1
                && registry.SubjectCount == 0
                && registry.ParticipantCount == 0;

            LogRegistryStep(
                logger,
                "stale-owner-cleanup",
                passed,
                registry,
                cleanup.RemovedAny ? "Removed" : "NoOp",
                cleanup.IssueCount,
                $"removedSubjects='{cleanup.RemovedSubjects}' removedParticipants='{cleanup.RemovedParticipants}'");
            return passed;
#else
            logger.Info(
                "QA Reset Registry Synthetic Smoke step skipped.",
                LogFields.Of(
                    LogFields.Field("step", "stale-owner-cleanup"),
                    LogFields.Field("status", "SkippedEditorOnly"),
                    LogFields.Field("source", source),
                    LogFields.Field("reason", "DestroyImmediate is editor-only; runtime adapter smoke will cover live Unity objects in later cuts.")));
            return true;
#endif
        }

        private static bool ValidateInvalidInputs(FrameworkLogger logger, string source)
        {
            var registry = new ResetRegistry();
            object ownerObject = new object();
            bool invalidPrefixRejected = !registry.TryGenerateRuntimeSubjectId(" ", out _, out ResetIssue invalidPrefixIssue)
                && invalidPrefixIssue.Kind == ResetIssueKind.InvalidSubject;

            var invalidUnregister = registry.Unregister(default, ownerObject, source, "qa.reset-registry.invalid-unregister");

            var missingSubjectParticipant = registry.RegisterParticipant(
                new ResetRegistrationHandle(ResetRegistrationKind.Subject, 404),
                new SyntheticResetParticipant("orphan", ResetParticipantRequiredness.Required, 0, SyntheticResetParticipantMode.Success),
                ownerObject,
                source,
                "qa.reset-registry.orphan-participant");

            bool passed = invalidPrefixRejected
                && invalidUnregister.Status == ResetRegistryOperationStatus.RejectedInvalidHandle
                && missingSubjectParticipant.Status == ResetRegistryOperationStatus.RejectedSubjectNotFound;

            LogRegistryStep(
                logger,
                "invalid-inputs",
                passed,
                registry,
                $"{invalidUnregister.Status}/{missingSubjectParticipant.Status}",
                invalidUnregister.IssueCount + missingSubjectParticipant.IssueCount,
                "invalid prefix, invalid handle and orphan participant are rejected");
            return passed;
        }

        private static void LogRegistryStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            ResetRegistry registry,
            string operationStatus,
            int issues,
            string detail)
        {
            var fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                LogFields.Field("subjects", registry.SubjectCount),
                LogFields.Field("participants", registry.ParticipantCount),
                LogFields.Field("operationStatus", operationStatus.NormalizeTextOrFallback("<none>")),
                LogFields.Field("issues", issues),
                LogFields.Field("detail", detail.NormalizeTextOrFallback("<none>")));

            if (passed)
            {
                logger.Info("QA Reset Registry Synthetic Smoke step completed.", fields);
            }
            else
            {
                logger.Warning("QA Reset Registry Synthetic Smoke step failed.", fields);
            }
        }

        private static string GetSubjectId(ResetRegistryOperationResult result)
        {
            return result.Subject.IsValid ? result.Subject.SubjectId.StableText : "<none>";
        }

        private enum SyntheticResetParticipantMode
        {
            Success = 0,
            Skipped = 10,
            Failed = 20
        }

        private sealed class SyntheticResetParticipant : IResetParticipant
        {
            private readonly ResetParticipantId _participantId;
            private readonly ResetParticipantRequiredness _requiredness;
            private readonly int _order;
            private readonly SyntheticResetParticipantMode _mode;

            internal SyntheticResetParticipant(
                string participantId,
                ResetParticipantRequiredness requiredness,
                int order,
                SyntheticResetParticipantMode mode)
            {
                _participantId = ResetParticipantId.From(participantId);
                _requiredness = requiredness;
                _order = order;
                _mode = mode;
            }

            public bool TryCreateResetParticipantDescriptor(
                ResetSubject subject,
                out ResetParticipantDescriptor descriptor,
                out ResetIssue issue)
            {
                if (!subject.IsValid)
                {
                    descriptor = default;
                    issue = ResetIssue.Error(ResetIssueKind.InvalidSubject, "Synthetic reset participant requires a valid subject.");
                    return false;
                }

                descriptor = new ResetParticipantDescriptor(
                    _participantId,
                    subject.SubjectId,
                    _requiredness,
                    _order,
                    $"QA Synthetic Reset Participant {_participantId.StableText}",
                    nameof(SyntheticResetParticipant),
                    "qa.reset-registry.synthetic-participant");
                issue = default;
                return true;
            }

            public ResetParticipantResult Reset(ResetContext context)
            {
                switch (_mode)
                {
                    case SyntheticResetParticipantMode.Success:
                        return ResetParticipantResult.CreateSucceeded(
                            context.Participant,
                            context.Source,
                            context.Reason,
                            "Synthetic participant reset succeeded.");
                    case SyntheticResetParticipantMode.Skipped:
                        return ResetParticipantResult.CreateSkipped(
                            context.Participant,
                            1,
                            context.Source,
                            context.Reason,
                            "Synthetic participant reset skipped.");
                    case SyntheticResetParticipantMode.Failed:
                        return ResetParticipantResult.CreateFailed(
                            context.Participant,
                            1,
                            context.Source,
                            context.Reason,
                            "Synthetic participant reset failed.");
                    default:
                        return ResetParticipantResult.CreateFailed(
                            context.Participant,
                            1,
                            context.Source,
                            context.Reason,
                            "Synthetic participant reset mode is unsupported.");
                }
            }
        }
    }
}
#endif
