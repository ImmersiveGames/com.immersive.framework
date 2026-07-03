using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Identity;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.ObjectReset.Unity;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// Development-only runner for Object Reset foundation and Unity adapter smokes.
    /// Foundation smokes validate logical orchestration; Unity adapter smokes validate explicit technical participants such as Transform reset.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F16 Object Reset QA smoke runner with foundation, Transform and GameObject active adapter closure coverage.")]
    internal static class ObjectResetQaSmokeRunner
    {
        internal const string TargetResolutionSmokeName = "Object Reset Target Resolution Smoke";

        internal const string ParticipantContractSmokeName = "Object Reset Participant Contract Smoke";

        internal const string RuntimeExecutorSmokeName = "Object Reset Runtime Executor Smoke";

        internal const string RuntimeHostIntegrationSmokeName = "Object Reset Runtime Host Integration Smoke";

        internal const string TriggerSmokeName = "Object Reset Trigger Smoke";

        internal const string BridgeSmokeName = "Object Reset Bridge Smoke";

        internal const string FoundationClosureSmokeName = "Object Reset Foundation Closure Smoke";

        internal const string UnityParticipantSourceSmokeName = "Object Reset Unity Participant Source Smoke";

        internal const string TransformParticipantSmokeName = "Object Reset Transform Participant Smoke";

        internal const string RequiredGuardrailsSmokeName = "Object Reset Required Guardrails Smoke";

        internal const string UnityAdaptersClosureSmokeName = "Object Reset Unity Adapters Closure Smoke";

        internal const string GameObjectActiveClosureSmokeName = "Object Reset GameObject Active Closure Smoke";

        internal static Task<bool> RunTargetResolutionSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            try
            {
                var activeRouteOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Route,
                    new FrameworkIdentityValue("qa.route.active"));
                var activeActivityOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Activity,
                    new FrameworkIdentityValue("qa.activity.active"));
                var staleActivityOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Activity,
                    new FrameworkIdentityValue("qa.activity.stale"));

                var routeEntry = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-reset.route.target"),
                    ObjectEntryScope.Route,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Route Target",
                    activeRouteOwner);
                var activityEntry = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-reset.activity.target"),
                    ObjectEntryScope.Activity,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Activity Target",
                    activeActivityOwner);

                var snapshot = CreateSnapshot(source, routeEntry, activityEntry);

                var validRequest = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(activityEntry),
                    source,
                    "qa.object-reset.target-resolution.valid");
                var validResult = ObjectResetTargetResolver.ResolveTarget(snapshot, validRequest);

                var missingRequest = ObjectResetRequest.ForTarget(
                    new ObjectResetTarget(
                        ObjectEntryId.From("qa.object-reset.missing"),
                        ObjectEntryScope.Activity,
                        activeActivityOwner),
                    source,
                    "qa.object-reset.target-resolution.missing");
                var missingResult = ObjectResetTargetResolver.ResolveTarget(snapshot, missingRequest);

                var staleRequest = ObjectResetRequest.ForTarget(
                    new ObjectResetTarget(
                        activityEntry.Id,
                        ObjectEntryScope.Activity,
                        staleActivityOwner),
                    source,
                    "qa.object-reset.target-resolution.foreign-or-stale");
                var staleResult = ObjectResetTargetResolver.ResolveTarget(snapshot, staleRequest);

                var invalidResult = ObjectResetTargetResolver.ResolveTarget(snapshot, default);

                bool validResolved = validResult is { Succeeded: true, HasResolvedTarget: true }
                    && validResult.ResolvedTarget.Id == activityEntry.Id
                    && validResult.BlockingIssueCount == 0;
                bool missingRejected = missingResult is { Status: ObjectResetResultStatus.RejectedTargetNotFound, BlockingIssueCount: 1, HasResolvedTarget: false };
                bool staleRejected = staleResult is { Status: ObjectResetResultStatus.RejectedForeignTarget, BlockingIssueCount: 1, HasResolvedTarget: false };
                bool invalidRejected = invalidResult is { Status: ObjectResetResultStatus.RejectedInvalidRequest, BlockingIssueCount: 1, HasResolvedTarget: false };

                if (!validResolved || !missingRejected || !staleRejected || !invalidRejected)
                {
                    logger.Warning(
                        "QA Object Reset Target Resolution Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "target-resolution"),
                            LogFields.Field("validResolved", validResolved),
                            LogFields.Field("missingRejected", missingRejected),
                            LogFields.Field("foreignOrStaleRejected", staleRejected),
                            LogFields.Field("invalidRejected", invalidRejected),
                            LogFields.Field("validStatus", validResult.Status.ToString()),
                            LogFields.Field("missingStatus", missingResult.Status.ToString()),
                            LogFields.Field("foreignOrStaleStatus", staleResult.Status.ToString()),
                            LogFields.Field("invalidStatus", invalidResult.Status.ToString())));
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Reset Target Resolution Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "target-resolution"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot.IsAvailable),
                        LogFields.Field("objectEntries", snapshot.Count),
                        LogFields.Field("validResolved", validResolved),
                        LogFields.Field("missingRejected", missingRejected),
                        LogFields.Field("foreignOrStaleRejected", staleRejected),
                        LogFields.Field("invalidRejected", invalidRejected),
                        LogFields.Field("validStatus", validResult.Status.ToString()),
                        LogFields.Field("missingStatus", missingResult.Status.ToString()),
                        LogFields.Field("foreignOrStaleStatus", staleResult.Status.ToString()),
                        LogFields.Field("invalidStatus", invalidResult.Status.ToString()),
                        LogFields.Field("validBlockingIssues", validResult.BlockingIssueCount),
                        LogFields.Field("missingBlockingIssues", missingResult.BlockingIssueCount),
                        LogFields.Field("foreignOrStaleBlockingIssues", staleResult.BlockingIssueCount),
                        LogFields.Field("invalidBlockingIssues", invalidResult.BlockingIssueCount),
                        LogFields.Field("summary", validResult.ToDiagnosticString())));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Target Resolution Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "target-resolution"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
        }


        internal static Task<bool> RunParticipantContractSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            try
            {
                var activeActivityOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Activity,
                    new FrameworkIdentityValue("qa.activity.active"));
                var staleActivityOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Activity,
                    new FrameworkIdentityValue("qa.activity.stale"));

                var activityEntry = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-reset.participant.target"),
                    ObjectEntryScope.Activity,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Participant Target",
                    activeActivityOwner);

                var snapshot = CreateSnapshot(source, activityEntry);
                var request = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(activityEntry),
                    source,
                    "qa.object-reset.participant-contract.request");
                var resolution = ObjectResetTargetResolver.ResolveTarget(snapshot, request);

                if (!resolution.Succeeded || !resolution.HasResolvedTarget)
                {
                    logger.Warning(
                        "QA Object Reset Participant Contract Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "participant-contract"),
                            LogFields.Field("reason", "target-resolution-failed"),
                            LogFields.Field("targetStatus", resolution.Status.ToString()),
                            LogFields.Field("targetBlockingIssues", resolution.BlockingIssueCount)));
                    return Task.FromResult(false);
                }

                var requiredParticipant = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.participant.required"),
                        request.Target,
                        20,
                        "QA Required Object Reset Participant",
                        source,
                        "qa.object-reset.participant-contract.required"),
                    SyntheticObjectResetParticipantMode.Success);
                var optionalParticipant = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Optional(
                        ObjectResetParticipantId.From("qa.object-reset.participant.optional"),
                        request.Target,
                        10,
                        "QA Optional Object Reset Participant",
                        source,
                        "qa.object-reset.participant-contract.optional"),
                    SyntheticObjectResetParticipantMode.SkippedOptional);
                var failureParticipant = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.participant.required-failure"),
                        request.Target,
                        30,
                        "QA Required Object Reset Failure Participant",
                        source,
                        "qa.object-reset.participant-contract.failure"),
                    SyntheticObjectResetParticipantMode.BlockingFailure);
                var foreignParticipant = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.participant.foreign"),
                        new ObjectResetTarget(
                            activityEntry.Id,
                            ObjectEntryScope.Activity,
                            staleActivityOwner),
                        40,
                        "QA Foreign Object Reset Participant",
                        source,
                        "qa.object-reset.participant-contract.foreign"),
                    SyntheticObjectResetParticipantMode.Success);

                var participantSource = new SyntheticObjectResetParticipantSource(requiredParticipant, optionalParticipant);
                IReadOnlyList<IObjectResetParticipant> resolvedParticipants = participantSource.ResolveObjectResetParticipants(request, resolution.ResolvedTarget);

                var requiredDescriptor = requiredParticipant.GetObjectResetDescriptor();
                var optionalDescriptor = optionalParticipant.GetObjectResetDescriptor();
                var failureDescriptor = failureParticipant.GetObjectResetDescriptor();
                var foreignDescriptor = foreignParticipant.GetObjectResetDescriptor();

                var requiredEntry = new ObjectResetParticipantEntry(requiredParticipant, requiredDescriptor, 0);
                var optionalEntry = new ObjectResetParticipantEntry(optionalParticipant, optionalDescriptor, 1);

                var requiredContext = new ObjectResetContext(request, resolution.ResolvedTarget, requiredDescriptor);
                var optionalContext = new ObjectResetContext(request, resolution.ResolvedTarget, optionalDescriptor);
                var failureContext = new ObjectResetContext(request, resolution.ResolvedTarget, failureDescriptor);

                var successResult = requiredParticipant.ResetObject(requiredContext);
                var skippedResult = optionalParticipant.ResetObject(optionalContext);
                var failureResult = failureParticipant.ResetObject(failureContext);

                bool sourceResolved = resolvedParticipants.Count == 2
                    && ReferenceEquals(resolvedParticipants[0], requiredParticipant)
                    && ReferenceEquals(resolvedParticipants[1], optionalParticipant);
                bool requiredSupportsTarget = requiredDescriptor.SupportsResolvedTarget(request, resolution.ResolvedTarget);
                bool optionalSupportsTarget = optionalDescriptor.SupportsResolvedTarget(request, resolution.ResolvedTarget);
                bool foreignRejected = !foreignDescriptor.SupportsResolvedTarget(request, resolution.ResolvedTarget);
                bool entriesValid = requiredEntry.IsValid
                    && optionalEntry.IsValid
                    && requiredEntry.IsRequired
                    && optionalEntry.IsOptional;
                bool contextsValid = requiredContext.IsValid
                    && optionalContext.IsValid
                    && failureContext.IsValid;
                bool successValid = successResult is { Succeeded: true, BlocksReset: false, IssueCount: 0 };
                bool skippedValid = skippedResult is { WasSkipped: true, BlocksReset: false, IssueCount: 0 };
                bool failureValid = failureResult is { Failed: true, BlocksReset: true, IssueCount: 1 };

                if (!sourceResolved
                    || !requiredSupportsTarget
                    || !optionalSupportsTarget
                    || !foreignRejected
                    || !entriesValid
                    || !contextsValid
                    || !successValid
                    || !skippedValid
                    || !failureValid)
                {
                    logger.Warning(
                        "QA Object Reset Participant Contract Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "participant-contract"),
                            LogFields.Field("sourceResolved", sourceResolved),
                            LogFields.Field("requiredSupportsTarget", requiredSupportsTarget),
                            LogFields.Field("optionalSupportsTarget", optionalSupportsTarget),
                            LogFields.Field("foreignRejected", foreignRejected),
                            LogFields.Field("entriesValid", entriesValid),
                            LogFields.Field("contextsValid", contextsValid),
                            LogFields.Field("successValid", successValid),
                            LogFields.Field("skippedValid", skippedValid),
                            LogFields.Field("failureValid", failureValid)));
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Reset Participant Contract Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "participant-contract"),
                        LogFields.Field("source", source),
                        LogFields.Field("targetResolved", resolution.Succeeded),
                        LogFields.Field("sourceParticipants", resolvedParticipants.Count),
                        LogFields.Field("sourceResolved", sourceResolved),
                        LogFields.Field("requiredSupportsTarget", requiredSupportsTarget),
                        LogFields.Field("optionalSupportsTarget", optionalSupportsTarget),
                        LogFields.Field("foreignRejected", foreignRejected),
                        LogFields.Field("entriesValid", entriesValid),
                        LogFields.Field("contextsValid", contextsValid),
                        LogFields.Field("successStatus", successResult.Status.ToString()),
                        LogFields.Field("skippedStatus", skippedResult.Status.ToString()),
                        LogFields.Field("failureStatus", failureResult.Status.ToString()),
                        LogFields.Field("successBlocksReset", successResult.BlocksReset),
                        LogFields.Field("skippedBlocksReset", skippedResult.BlocksReset),
                        LogFields.Field("failureBlocksReset", failureResult.BlocksReset),
                        LogFields.Field("successIssues", successResult.IssueCount),
                        LogFields.Field("skippedIssues", skippedResult.IssueCount),
                        LogFields.Field("failureIssues", failureResult.IssueCount),
                        LogFields.Field("summary", successResult.ToDiagnosticString())));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Participant Contract Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "participant-contract"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
        }



        internal static Task<bool> RunRuntimeExecutorSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            try
            {
                var activeActivityOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Activity,
                    new FrameworkIdentityValue("qa.activity.active"));
                var activityEntry = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-reset.runtime.target"),
                    ObjectEntryScope.Activity,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Runtime Target",
                    activeActivityOwner);

                var snapshot = CreateSnapshot(source, activityEntry);
                var request = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(activityEntry),
                    source,
                    "qa.object-reset.runtime-executor.request");
                var runtime = new ObjectResetRuntime();

                var requiredSuccess = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.runtime.required"),
                        request.Target,
                        20,
                        "QA Runtime Required Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-executor.required"),
                    SyntheticObjectResetParticipantMode.Success);
                var optionalSkipped = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Optional(
                        ObjectResetParticipantId.From("qa.object-reset.runtime.optional"),
                        request.Target,
                        10,
                        "QA Runtime Optional Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-executor.optional"),
                    SyntheticObjectResetParticipantMode.SkippedOptional);
                var requiredFailure = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.runtime.required-failure"),
                        request.Target,
                        30,
                        "QA Runtime Required Failure Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-executor.required-failure"),
                    SyntheticObjectResetParticipantMode.BlockingFailure);
                var optionalFailure = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Optional(
                        ObjectResetParticipantId.From("qa.object-reset.runtime.optional-failure"),
                        request.Target,
                        40,
                        "QA Runtime Optional Failure Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-executor.optional-failure"),
                    SyntheticObjectResetParticipantMode.NonBlockingFailure);
                var invalidResult = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.runtime.invalid-result"),
                        request.Target,
                        50,
                        "QA Runtime Invalid Result Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-executor.invalid-result"),
                    SyntheticObjectResetParticipantMode.InvalidResult);

                var successSource = new SyntheticObjectResetParticipantSource(requiredSuccess, optionalSkipped);
                var plan = runtime.BuildPlan(snapshot, request, successSource);
                var successResult = runtime.Execute(snapshot, request, successSource);
                var noParticipantsResult = runtime.Execute(snapshot, request, new SyntheticObjectResetParticipantSource());
                var requiredFailureResult = runtime.Execute(snapshot, request, new SyntheticObjectResetParticipantSource(requiredFailure));
                var optionalFailureResult = runtime.Execute(snapshot, request, new SyntheticObjectResetParticipantSource(optionalFailure));
                var invalidResultResult = runtime.Execute(snapshot, request, new SyntheticObjectResetParticipantSource(invalidResult));

                bool planOrdered = plan.ParticipantCount == 2
                    && plan.Participants[0].ParticipantId == optionalSkipped.GetObjectResetDescriptor().ParticipantId
                    && plan.Participants[1].ParticipantId == requiredSuccess.GetObjectResetDescriptor().ParticipantId;
                bool successAggregated = successResult is { Status: ObjectResetResultStatus.Succeeded, ParticipantCount: 2, ParticipantSucceededCount: 1, ParticipantSkippedCount: 1, ParticipantFailedCount: 0, BlockingIssueCount: 0 };
                bool noParticipantsSucceeded = noParticipantsResult is { Status: ObjectResetResultStatus.SucceededNoParticipants, ParticipantCount: 0, BlockingIssueCount: 0 };
                bool requiredFailureBlocked = requiredFailureResult is { Status: ObjectResetResultStatus.Failed, ParticipantFailedCount: 1, ParticipantBlockingFailureCount: 1, BlockingIssueCount: 1 };
                bool optionalFailureWarned = optionalFailureResult is { Status: ObjectResetResultStatus.CompletedWithWarnings, ParticipantFailedCount: 1, ParticipantBlockingFailureCount: 0, BlockingIssueCount: 0, NonBlockingIssueCount: 1 };
                bool invalidResultBlocked = invalidResultResult is { Status: ObjectResetResultStatus.Failed, ParticipantFailedCount: 1, ParticipantBlockingFailureCount: 1, BlockingIssueCount: 1 };

                if (!planOrdered
                    || !successAggregated
                    || !noParticipantsSucceeded
                    || !requiredFailureBlocked
                    || !optionalFailureWarned
                    || !invalidResultBlocked)
                {
                    logger.Warning(
                        "QA Object Reset Runtime Executor Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "runtime-executor"),
                            LogFields.Field("planOrdered", planOrdered),
                            LogFields.Field("successAggregated", successAggregated),
                            LogFields.Field("noParticipantsSucceeded", noParticipantsSucceeded),
                            LogFields.Field("requiredFailureBlocked", requiredFailureBlocked),
                            LogFields.Field("optionalFailureWarned", optionalFailureWarned),
                            LogFields.Field("invalidResultBlocked", invalidResultBlocked),
                            LogFields.Field("successStatus", successResult.Status.ToString()),
                            LogFields.Field("noParticipantsStatus", noParticipantsResult.Status.ToString()),
                            LogFields.Field("requiredFailureStatus", requiredFailureResult.Status.ToString()),
                            LogFields.Field("optionalFailureStatus", optionalFailureResult.Status.ToString()),
                            LogFields.Field("invalidResultStatus", invalidResultResult.Status.ToString())));
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Reset Runtime Executor Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-executor"),
                        LogFields.Field("source", source),
                        LogFields.Field("targetResolved", successResult.HasResolvedTarget),
                        LogFields.Field("planParticipants", plan.ParticipantCount),
                        LogFields.Field("planRequired", plan.RequiredParticipantCount),
                        LogFields.Field("planOptional", plan.OptionalParticipantCount),
                        LogFields.Field("planOrdered", planOrdered),
                        LogFields.Field("successStatus", successResult.Status.ToString()),
                        LogFields.Field("successParticipants", successResult.ParticipantCount),
                        LogFields.Field("successParticipantSucceeded", successResult.ParticipantSucceededCount),
                        LogFields.Field("successParticipantSkipped", successResult.ParticipantSkippedCount),
                        LogFields.Field("successParticipantFailed", successResult.ParticipantFailedCount),
                        LogFields.Field("noParticipantsStatus", noParticipantsResult.Status.ToString()),
                        LogFields.Field("requiredFailureStatus", requiredFailureResult.Status.ToString()),
                        LogFields.Field("requiredFailureBlocks", requiredFailureResult.BlockingIssueCount),
                        LogFields.Field("optionalFailureStatus", optionalFailureResult.Status.ToString()),
                        LogFields.Field("optionalFailureWarnings", optionalFailureResult.NonBlockingIssueCount),
                        LogFields.Field("invalidResultStatus", invalidResultResult.Status.ToString()),
                        LogFields.Field("invalidResultBlockingIssues", invalidResultResult.BlockingIssueCount),
                        LogFields.Field("blockingIssues", successResult.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", successResult.NonBlockingIssueCount),
                        LogFields.Field("summary", successResult.ToDiagnosticString())));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Runtime Executor Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-executor"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
        }

        internal static async Task<bool> RunRuntimeHostIntegrationSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Runtime Host Integration Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "runtime-host-integration"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                string objectEntryId = $"qa.object-reset.host.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetRuntimeHost_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Runtime Host Target",
                    qaActivityOwner: activeActivity);

                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-host-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                if (snapshot == null || !snapshot.TryGet(id, out var descriptor))
                {
                    logger.Warning(
                        "QA Object Reset Runtime Host Integration Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "runtime-host-integration"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                var request = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(descriptor),
                    source,
                    "qa.object-reset.runtime-host.success");
                var requiredSuccess = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.host.required"),
                        request.Target,
                        20,
                        "QA Runtime Host Required Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-host.required"),
                    SyntheticObjectResetParticipantMode.Success);
                var optionalSkipped = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Optional(
                        ObjectResetParticipantId.From("qa.object-reset.host.optional"),
                        request.Target,
                        10,
                        "QA Runtime Host Optional Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-host.optional"),
                    SyntheticObjectResetParticipantMode.SkippedOptional);

                runtimeHost.SetObjectResetParticipantSource(new SyntheticObjectResetParticipantSource(requiredSuccess, optionalSkipped));
                var successResult = await runtimeHost.RequestObjectResetAsync(request);

                runtimeHost.SetObjectResetParticipantSource(null);
                var noParticipantsResult = await runtimeHost.RequestObjectResetAsync(ObjectResetRequest.ForTarget(
                    request.Target,
                    source,
                    "qa.object-reset.runtime-host.no-participants"));

                var missingResult = await runtimeHost.RequestObjectResetAsync(ObjectResetRequest.ForTarget(
                    new ObjectResetTarget(
                        ObjectEntryId.From($"{objectEntryId}.missing"),
                        ObjectEntryScope.Activity,
                        descriptor.OwnerIdentity.Value),
                    source,
                    "qa.object-reset.runtime-host.missing"));

                bool hostSnapshotAvailable = runtimeHost.TryGetObjectEntryRuntimeContextSnapshot(out var hostSnapshot)
                    && hostSnapshot is { IsAvailable: true };
                bool hostResolvedTarget = successResult.HasResolvedTarget
                    && successResult.ResolvedTarget.Id == id;
                bool successValid = successResult is { Status: ObjectResetResultStatus.Succeeded, ParticipantCount: 2, ParticipantSucceededCount: 1, ParticipantSkippedCount: 1, BlockingIssueCount: 0 };
                bool noParticipantsValid = noParticipantsResult is { Status: ObjectResetResultStatus.SucceededNoParticipants, ParticipantCount: 0, BlockingIssueCount: 0 };
                bool missingRejected = missingResult is { Status: ObjectResetResultStatus.RejectedTargetNotFound, BlockingIssueCount: 1 };

                if (!hostSnapshotAvailable
                    || !hostResolvedTarget
                    || !successValid
                    || !noParticipantsValid
                    || !missingRejected)
                {
                    logger.Warning(
                        "QA Object Reset Runtime Host Integration Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "runtime-host-integration"),
                            LogFields.Field("hostSnapshotAvailable", hostSnapshotAvailable),
                            LogFields.Field("hostResolvedTarget", hostResolvedTarget),
                            LogFields.Field("successValid", successValid),
                            LogFields.Field("noParticipantsValid", noParticipantsValid),
                            LogFields.Field("missingRejected", missingRejected),
                            LogFields.Field("successStatus", successResult.Status.ToString()),
                            LogFields.Field("noParticipantsStatus", noParticipantsResult.Status.ToString()),
                            LogFields.Field("missingStatus", missingResult.Status.ToString())));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Runtime Host Integration Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-host-integration"),
                        LogFields.Field("source", source),
                        LogFields.Field("hostSnapshotAvailable", hostSnapshotAvailable),
                        LogFields.Field("snapshotSource", hostSnapshot.ToDiagnosticText(x => x.Source)),
                        LogFields.Field("snapshotRevision", runtimeHost.ObjectEntryRuntimeContextRevision),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("hostResolvedTarget", hostResolvedTarget),
                        LogFields.Field("successStatus", successResult.Status.ToString()),
                        LogFields.Field("successParticipants", successResult.ParticipantCount),
                        LogFields.Field("successParticipantSucceeded", successResult.ParticipantSucceededCount),
                        LogFields.Field("successParticipantSkipped", successResult.ParticipantSkippedCount),
                        LogFields.Field("successBlockingIssues", successResult.BlockingIssueCount),
                        LogFields.Field("noParticipantsStatus", noParticipantsResult.Status.ToString()),
                        LogFields.Field("missingStatus", missingResult.Status.ToString()),
                        LogFields.Field("missingBlockingIssues", missingResult.BlockingIssueCount),
                        LogFields.Field("summary", successResult.ToDiagnosticString())));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Runtime Host Integration Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-host-integration"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-host-smoke-cleanup");
                }
            }
        }


        internal static async Task<bool> RunTriggerSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Trigger Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-trigger"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                string objectEntryId = $"qa.object-reset.trigger.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetTrigger_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Trigger Target",
                    qaActivityOwner: activeActivity);

                var trigger = qaObject.AddComponent<ObjectResetTrigger>();
                trigger.ConfigureForQa(
                    declaration,
                    string.Empty,
                    "qa.object-reset.trigger",
                    qaAllowNoParticipants: true);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-trigger-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                bool targetCollected = snapshot != null && snapshot.TryGet(id, out var descriptor);
                if (!targetCollected)
                {
                    logger.Warning(
                        "QA Object Reset Trigger Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-trigger"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                trigger.RequestObjectReset();
                bool completed = await WaitForObjectResetTriggerAsync(trigger, 32);

                bool requestCompleted = completed
                    && !trigger.IsRequestInFlight
                    && trigger.LastEventPhase == FlowRequestEventPhase.Completed;
                bool resultSucceededNoParticipants = trigger.HasLastResult
                    && trigger.LastResultStatus == ObjectResetResultStatus.SucceededNoParticipants
                    && trigger.LastResultSucceededNoParticipants;
                bool outcomeSucceeded = trigger.LastRequestSucceeded;
                bool participantsValid = trigger.LastParticipantCount == 0
                    && trigger.LastSucceededParticipantCount == 0
                    && trigger.LastSkippedParticipantCount == 0
                    && trigger.LastFailedParticipantCount == 0;
                bool issuesValid = trigger.LastBlockingIssueCount == 0
                    && trigger.LastNonBlockingIssueCount == 0;
                bool idResolved = string.Equals(trigger.ResolvedAuthoringObjectEntryId, objectEntryId, StringComparison.Ordinal);

                if (!requestCompleted
                    || !targetCollected
                    || !resultSucceededNoParticipants
                    || !outcomeSucceeded
                    || !participantsValid
                    || !issuesValid
                    || !idResolved)
                {
                    logger.Warning(
                        "QA Object Reset Trigger Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-trigger"),
                            LogFields.Field("requestCompleted", requestCompleted),
                            LogFields.Field("targetCollected", targetCollected),
                            LogFields.Field("resultSucceededNoParticipants", resultSucceededNoParticipants),
                            LogFields.Field("outcomeSucceeded", outcomeSucceeded),
                            LogFields.Field("participantsValid", participantsValid),
                            LogFields.Field("issuesValid", issuesValid),
                            LogFields.Field("idResolved", idResolved),
                            LogFields.Field("lastStatus", trigger.LastResultStatus.ToString()),
                            LogFields.Field("lastOutcome", trigger.LastOutcome.ToString())));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Trigger Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "object-reset-trigger"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("targetCollected", targetCollected),
                        LogFields.Field("requestCompleted", requestCompleted),
                        LogFields.Field("lastOutcome", trigger.LastOutcome.ToString()),
                        LogFields.Field("resultStatus", trigger.LastResultStatus.ToString()),
                        LogFields.Field("succeededNoParticipants", trigger.LastResultSucceededNoParticipants),
                        LogFields.Field("participants", trigger.LastParticipantCount),
                        LogFields.Field("participantSucceeded", trigger.LastSucceededParticipantCount),
                        LogFields.Field("participantSkipped", trigger.LastSkippedParticipantCount),
                        LogFields.Field("participantFailed", trigger.LastFailedParticipantCount),
                        LogFields.Field("blockingIssues", trigger.LastBlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", trigger.LastNonBlockingIssueCount),
                        LogFields.Field("summary", trigger.LastResultSummary)));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Trigger Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "object-reset-trigger"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-trigger-smoke-cleanup");
                }
            }
        }


        internal static async Task<bool> RunBridgeSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Bridge Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-bridge"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                string objectEntryId = $"qa.object-reset.bridge.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetBridge_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Bridge Target",
                    qaActivityOwner: activeActivity);

                var trigger = qaObject.AddComponent<ObjectResetTrigger>();
                trigger.ConfigureForQa(
                    declaration,
                    string.Empty,
                    "qa.object-reset.bridge",
                    qaAllowNoParticipants: true);

                var bridge = qaObject.AddComponent<ObjectResetTriggerUnityEventBridge>();
                var counters = new BridgeEventCounters();
                AttachBridgeCounters(bridge, counters);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-bridge-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                bool targetCollected = snapshot != null && snapshot.TryGet(id, out _);
                if (!targetCollected)
                {
                    logger.Warning(
                        "QA Object Reset Bridge Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-bridge"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                trigger.RequestObjectReset();
                bool completed = await WaitForObjectResetTriggerAsync(trigger, 32);

                bool requestCompleted = completed
                    && !trigger.IsRequestInFlight
                    && trigger.LastEventPhase == FlowRequestEventPhase.Completed;
                bool resultSucceededNoParticipants = trigger.HasLastResult
                    && trigger.LastResultStatus == ObjectResetResultStatus.SucceededNoParticipants
                    && trigger.LastResultSucceededNoParticipants;
                bool outcomeSucceeded = trigger.LastRequestSucceeded;
                bool countersValid = counters.submitted == 1
                    && counters.succeeded == 1
                    && counters.succeededNoParticipants == 1
                    && counters.succeededWithParticipants == 0
                    && counters.completedWithWarnings == 0
                    && counters.ignored == 0
                    && counters.failed == 0
                    && counters.completed == 1;
                bool issuesValid = trigger.LastBlockingIssueCount == 0
                    && trigger.LastNonBlockingIssueCount == 0;

                if (!requestCompleted
                    || !targetCollected
                    || !resultSucceededNoParticipants
                    || !outcomeSucceeded
                    || !countersValid
                    || !issuesValid)
                {
                    logger.Warning(
                        "QA Object Reset Bridge Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-bridge"),
                            LogFields.Field("requestCompleted", requestCompleted),
                            LogFields.Field("targetCollected", targetCollected),
                            LogFields.Field("resultSucceededNoParticipants", resultSucceededNoParticipants),
                            LogFields.Field("outcomeSucceeded", outcomeSucceeded),
                            LogFields.Field("countersValid", countersValid),
                            LogFields.Field("issuesValid", issuesValid),
                            LogFields.Field("submitted", counters.submitted),
                            LogFields.Field("succeeded", counters.succeeded),
                            LogFields.Field("succeededNoParticipants", counters.succeededNoParticipants),
                            LogFields.Field("completedWithWarnings", counters.completedWithWarnings),
                            LogFields.Field("ignored", counters.ignored),
                            LogFields.Field("failed", counters.failed),
                            LogFields.Field("completed", counters.completed),
                            LogFields.Field("lastStatus", trigger.LastResultStatus.ToString()),
                            LogFields.Field("lastOutcome", trigger.LastOutcome.ToString())));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Bridge Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "object-reset-bridge"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("targetCollected", targetCollected),
                        LogFields.Field("requestCompleted", requestCompleted),
                        LogFields.Field("lastOutcome", trigger.LastOutcome.ToString()),
                        LogFields.Field("resultStatus", trigger.LastResultStatus.ToString()),
                        LogFields.Field("submitted", counters.submitted),
                        LogFields.Field("succeeded", counters.succeeded),
                        LogFields.Field("succeededWithParticipants", counters.succeededWithParticipants),
                        LogFields.Field("succeededNoParticipants", counters.succeededNoParticipants),
                        LogFields.Field("completedWithWarnings", counters.completedWithWarnings),
                        LogFields.Field("ignored", counters.ignored),
                        LogFields.Field("failed", counters.failed),
                        LogFields.Field("completed", counters.completed),
                        LogFields.Field("blockingIssues", trigger.LastBlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", trigger.LastNonBlockingIssueCount),
                        LogFields.Field("summary", trigger.LastResultSummary)));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Bridge Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "object-reset-bridge"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-bridge-smoke-cleanup");
                }
            }
        }


        internal static async Task<bool> RunUnityParticipantSourceSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Unity Participant Source Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "unity-participant-source"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                string objectEntryId = $"qa.object-reset.unity-source.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetUnityParticipantSource_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Unity Source Target",
                    qaActivityOwner: activeActivity);

                var requiredParticipant = qaObject.AddComponent<ObjectResetQaUnityParticipantBehaviour>();
                requiredParticipant.ConfigureForQa(
                    declaration,
                    "qa.object-reset.unity-source.required",
                    ObjectResetParticipantRequiredness.Required,
                    20,
                    "QA Object Reset Unity Source Required Participant",
                    source,
                    "qa.object-reset.unity-source.required");
                requiredParticipant.ConfigureResultForQa(ObjectResetParticipantResultStatus.Succeeded);

                var optionalParticipant = qaObject.AddComponent<ObjectResetQaUnityParticipantBehaviour>();
                optionalParticipant.ConfigureForQa(
                    declaration,
                    "qa.object-reset.unity-source.optional",
                    ObjectResetParticipantRequiredness.Optional,
                    10,
                    "QA Object Reset Unity Source Optional Participant",
                    source,
                    "qa.object-reset.unity-source.optional");
                optionalParticipant.ConfigureResultForQa(ObjectResetParticipantResultStatus.SkippedOptional);

                var unitySource = qaObject.AddComponent<ObjectResetUnityParticipantSource>();
                unitySource.ConfigureForQa(
                    qaRegisterOnEnable: false,
                    requiredParticipant,
                    optionalParticipant);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-unity-participant-source-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                ObjectEntryDescriptor descriptor = default;
                bool targetCollected = snapshot != null && snapshot.TryGet(id, out descriptor);
                if (!targetCollected)
                {
                    logger.Warning(
                        "QA Object Reset Unity Participant Source Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "unity-participant-source"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                var request = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(descriptor),
                    source,
                    "qa.object-reset.unity-source.request");
                IReadOnlyList<IObjectResetParticipant> resolvedParticipants = unitySource.ResolveObjectResetParticipants(request, descriptor);
                bool registered = unitySource.RegisterWithCurrentHost()
                    && runtimeHost.IsObjectResetParticipantSourceRegistered(unitySource);
                var result = await runtimeHost.RequestObjectResetAsync(request);

                bool sourceResolved = resolvedParticipants is { Count: 2 };
                bool resultValid = result is { Status: ObjectResetResultStatus.Succeeded, ParticipantCount: 2, ParticipantSucceededCount: 1, ParticipantSkippedCount: 1, ParticipantFailedCount: 0, BlockingIssueCount: 0, NonBlockingIssueCount: 0 };
                bool targetResolved = result.HasResolvedTarget
                    && result.ResolvedTarget.Id == id
                    && result.ResolvedTarget.HasOwnerIdentity;
                bool cleared = unitySource.ClearRegistration()
                    && !runtimeHost.IsObjectResetParticipantSourceRegistered(unitySource);

                if (!targetCollected
                    || !targetResolved
                    || !sourceResolved
                    || !registered
                    || !resultValid
                    || !cleared)
                {
                    logger.Warning(
                        "QA Object Reset Unity Participant Source Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "unity-participant-source"),
                            LogFields.Field("targetCollected", targetCollected),
                            LogFields.Field("targetResolved", targetResolved),
                            LogFields.Field("sourceResolved", sourceResolved),
                            LogFields.Field("registered", registered),
                            LogFields.Field("cleared", cleared),
                            LogFields.Field("status", result.Status.ToString()),
                            LogFields.Field("participants", result.ParticipantCount),
                            LogFields.Field("blockingIssues", result.BlockingIssueCount),
                            LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount)));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Unity Participant Source Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "unity-participant-source"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("targetCollected", targetCollected),
                        LogFields.Field("targetResolved", targetResolved),
                        LogFields.Field("authoredParticipants", unitySource.AuthoredParticipantCount),
                        LogFields.Field("resolvedParticipants", resolvedParticipants.Count),
                        LogFields.Field("registered", registered),
                        LogFields.Field("cleared", cleared),
                        LogFields.Field("resultStatus", result.Status.ToString()),
                        LogFields.Field("participants", result.ParticipantCount),
                        LogFields.Field("participantSucceeded", result.ParticipantSucceededCount),
                        LogFields.Field("participantSkipped", result.ParticipantSkippedCount),
                        LogFields.Field("participantFailed", result.ParticipantFailedCount),
                        LogFields.Field("blockingIssues", result.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount),
                        LogFields.Field("summary", result.ToDiagnosticString())));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Unity Participant Source Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "unity-participant-source"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-unity-participant-source-smoke-cleanup");
                }
            }
        }


        internal static async Task<bool> RunTransformParticipantSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Transform Participant Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "transform-participant"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                string objectEntryId = $"qa.object-reset.transform.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetTransform_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Transform Target",
                    qaActivityOwner: activeActivity);

                var baselineLocalPosition = new Vector3(1.25f, 2.5f, -3.75f);
                var baselineLocalEulerAngles = new Vector3(10f, 25f, 40f);
                var baselineLocalScale = new Vector3(1.5f, 0.75f, 2.25f);
                var mutatedLocalPosition = new Vector3(-9f, 8f, 7f);
                var mutatedLocalEulerAngles = new Vector3(0f, 90f, 180f);
                var mutatedLocalScale = new Vector3(3f, 3f, 3f);

                var transformParticipant = qaObject.AddComponent<ObjectResetTransformParticipant>();
                transformParticipant.ConfigureForQa(
                    declaration,
                    "qa.object-reset.transform.required",
                    ObjectResetParticipantRequiredness.Required,
                    10,
                    "QA Object Reset Transform Required Participant",
                    source,
                    "qa.object-reset.transform.required");
                transformParticipant.ConfigureTransformBaselineForQa(
                    qaObject.transform,
                    qaBaselineConfigured: true,
                    qaResetLocalPosition: true,
                    qaResetLocalRotation: true,
                    qaResetLocalScale: true,
                    baselineLocalPosition,
                    baselineLocalEulerAngles,
                    baselineLocalScale);

                qaObject.transform.localPosition = mutatedLocalPosition;
                qaObject.transform.localRotation = Quaternion.Euler(mutatedLocalEulerAngles);
                qaObject.transform.localScale = mutatedLocalScale;

                var unitySource = qaObject.AddComponent<ObjectResetUnityParticipantSource>();
                unitySource.ConfigureForQa(
                    qaRegisterOnEnable: false,
                    transformParticipant);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-transform-participant-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                ObjectEntryDescriptor descriptor = default;
                bool targetCollected = snapshot != null && snapshot.TryGet(id, out descriptor);
                if (!targetCollected)
                {
                    logger.Warning(
                        "QA Object Reset Transform Participant Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "transform-participant"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                var request = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(descriptor),
                    source,
                    "qa.object-reset.transform.request");
                IReadOnlyList<IObjectResetParticipant> resolvedParticipants = unitySource.ResolveObjectResetParticipants(request, descriptor);
                bool registered = unitySource.RegisterWithCurrentHost()
                    && runtimeHost.IsObjectResetParticipantSourceRegistered(unitySource);
                var result = await runtimeHost.RequestObjectResetAsync(request);

                bool sourceResolved = resolvedParticipants is { Count: 1 };
                bool targetResolved = result.HasResolvedTarget
                    && result.ResolvedTarget.Id == id
                    && result.ResolvedTarget.HasOwnerIdentity;
                bool transformPositionReset = VectorApproximatelyEqual(qaObject.transform.localPosition, baselineLocalPosition);
                bool transformRotationReset = EulerApproximatelyEqual(qaObject.transform.localEulerAngles, baselineLocalEulerAngles);
                bool transformScaleReset = VectorApproximatelyEqual(qaObject.transform.localScale, baselineLocalScale);
                bool resultValid = result is { Status: ObjectResetResultStatus.Succeeded, ParticipantCount: 1, ParticipantSucceededCount: 1, ParticipantSkippedCount: 0, ParticipantFailedCount: 0, BlockingIssueCount: 0, NonBlockingIssueCount: 0 };
                bool cleared = unitySource.ClearRegistration()
                    && !runtimeHost.IsObjectResetParticipantSourceRegistered(unitySource);

                if (!targetCollected
                    || !targetResolved
                    || !sourceResolved
                    || !registered
                    || !resultValid
                    || !transformPositionReset
                    || !transformRotationReset
                    || !transformScaleReset
                    || !cleared)
                {
                    logger.Warning(
                        "QA Object Reset Transform Participant Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "transform-participant"),
                            LogFields.Field("targetCollected", targetCollected),
                            LogFields.Field("targetResolved", targetResolved),
                            LogFields.Field("sourceResolved", sourceResolved),
                            LogFields.Field("registered", registered),
                            LogFields.Field("cleared", cleared),
                            LogFields.Field("resultStatus", result.Status.ToString()),
                            LogFields.Field("participants", result.ParticipantCount),
                            LogFields.Field("transformPositionReset", transformPositionReset),
                            LogFields.Field("transformRotationReset", transformRotationReset),
                            LogFields.Field("transformScaleReset", transformScaleReset),
                            LogFields.Field("blockingIssues", result.BlockingIssueCount),
                            LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount)));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Transform Participant Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "transform-participant"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("targetCollected", targetCollected),
                        LogFields.Field("targetResolved", targetResolved),
                        LogFields.Field("authoredParticipants", unitySource.AuthoredParticipantCount),
                        LogFields.Field("resolvedParticipants", resolvedParticipants.Count),
                        LogFields.Field("registered", registered),
                        LogFields.Field("cleared", cleared),
                        LogFields.Field("baselineConfigured", transformParticipant.HasBaseline),
                        LogFields.Field("resultStatus", result.Status.ToString()),
                        LogFields.Field("participants", result.ParticipantCount),
                        LogFields.Field("participantSucceeded", result.ParticipantSucceededCount),
                        LogFields.Field("participantSkipped", result.ParticipantSkippedCount),
                        LogFields.Field("participantFailed", result.ParticipantFailedCount),
                        LogFields.Field("transformPositionReset", transformPositionReset),
                        LogFields.Field("transformRotationReset", transformRotationReset),
                        LogFields.Field("transformScaleReset", transformScaleReset),
                        LogFields.Field("blockingIssues", result.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount),
                        LogFields.Field("summary", result.ToDiagnosticString())));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Transform Participant Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "transform-participant"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-transform-participant-smoke-cleanup");
                }
            }
        }



        internal static async Task<bool> RunRequiredGuardrailsSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            GameObject requiredParticipantObject = null;
            GameObject optionalParticipantObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Required Guardrails Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "required-guardrails"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                string objectEntryId = $"qa.object-reset.required-guardrails.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetRequiredGuardrails_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Required Guardrails Target",
                    qaActivityOwner: activeActivity);

                var unitySource = qaObject.AddComponent<ObjectResetUnityParticipantSource>();
                unitySource.ConfigureForQa(qaRegisterOnEnable: false);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-required-guardrails-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                ObjectEntryDescriptor descriptor = default;
                bool targetCollected = snapshot != null && snapshot.TryGet(id, out descriptor);
                if (!targetCollected)
                {
                    logger.Warning(
                        "QA Object Reset Required Guardrails Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "required-guardrails"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                var target = ObjectResetTarget.FromDescriptor(descriptor);
                var requireParticipantsPolicy = new ObjectResetPolicy(
                    requireCurrentSnapshot: true,
                    allowNoParticipants: false);
                var missingAdapterRequest = new ObjectResetRequest(
                    target,
                    requireParticipantsPolicy,
                    source,
                    "qa.object-reset.required-guardrails.missing-adapter");

                bool registered = unitySource.RegisterWithCurrentHost()
                    && runtimeHost.IsObjectResetParticipantSourceRegistered(unitySource);
                var missingAdapterResult = await runtimeHost.RequestObjectResetAsync(missingAdapterRequest);

                requiredParticipantObject = new GameObject("QA_ObjectResetRequiredGuardrails_RequiredMissingBaseline");
                requiredParticipantObject.transform.SetParent(qaObject.transform, false);
                var requiredParticipant = requiredParticipantObject.AddComponent<ObjectResetTransformParticipant>();
                requiredParticipant.ConfigureForQa(
                    declaration,
                    "qa.object-reset.required-guardrails.required-missing-baseline",
                    ObjectResetParticipantRequiredness.Required,
                    10,
                    "QA Object Reset Required Missing Baseline Participant",
                    source,
                    "qa.object-reset.required-guardrails.required-missing-baseline");
                requiredParticipant.ConfigureTransformBaselineForQa(
                    qaObject.transform,
                    qaBaselineConfigured: false,
                    qaResetLocalPosition: true,
                    qaResetLocalRotation: true,
                    qaResetLocalScale: true,
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.one);

                unitySource.ConfigureForQa(qaRegisterOnEnable: false, requiredParticipant);
                var requiredMissingBaselineRequest = ObjectResetRequest.ForTarget(
                    target,
                    source,
                    "qa.object-reset.required-guardrails.required-missing-baseline");
                var requiredMissingBaselineResult = await runtimeHost.RequestObjectResetAsync(requiredMissingBaselineRequest);

                optionalParticipantObject = new GameObject("QA_ObjectResetRequiredGuardrails_OptionalMissingBaseline");
                optionalParticipantObject.transform.SetParent(qaObject.transform, false);
                var optionalParticipant = optionalParticipantObject.AddComponent<ObjectResetTransformParticipant>();
                optionalParticipant.ConfigureForQa(
                    declaration,
                    "qa.object-reset.required-guardrails.optional-missing-baseline",
                    ObjectResetParticipantRequiredness.Optional,
                    20,
                    "QA Object Reset Optional Missing Baseline Participant",
                    source,
                    "qa.object-reset.required-guardrails.optional-missing-baseline");
                optionalParticipant.ConfigureTransformBaselineForQa(
                    qaObject.transform,
                    qaBaselineConfigured: false,
                    qaResetLocalPosition: true,
                    qaResetLocalRotation: true,
                    qaResetLocalScale: true,
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.one);

                unitySource.ConfigureForQa(qaRegisterOnEnable: false, optionalParticipant);
                var optionalMissingBaselineRequest = ObjectResetRequest.ForTarget(
                    target,
                    source,
                    "qa.object-reset.required-guardrails.optional-missing-baseline");
                var optionalMissingBaselineResult = await runtimeHost.RequestObjectResetAsync(optionalMissingBaselineRequest);

                bool missingAdapterBlocked = missingAdapterResult is { Status: ObjectResetResultStatus.Failed, ParticipantCount: 0, BlockingIssueCount: > 0 };
                bool requiredMissingBaselineBlocked = requiredMissingBaselineResult is { Status: ObjectResetResultStatus.Failed, ParticipantCount: 1, ParticipantFailedCount: 1, ParticipantBlockingFailureCount: 1, BlockingIssueCount: > 0 };
                bool optionalMissingBaselineWarned = optionalMissingBaselineResult is { Status: ObjectResetResultStatus.CompletedWithWarnings, ParticipantCount: 1, ParticipantFailedCount: 1, ParticipantBlockingFailureCount: 0, BlockingIssueCount: 0, NonBlockingIssueCount: > 0 };
                bool targetResolved = missingAdapterResult.HasResolvedTarget
                    && requiredMissingBaselineResult.HasResolvedTarget
                    && optionalMissingBaselineResult.HasResolvedTarget
                    && missingAdapterResult.ResolvedTarget.Id == id
                    && requiredMissingBaselineResult.ResolvedTarget.Id == id
                    && optionalMissingBaselineResult.ResolvedTarget.Id == id;
                bool cleared = unitySource.ClearRegistration()
                    && !runtimeHost.IsObjectResetParticipantSourceRegistered(unitySource);

                if (!targetCollected
                    || !targetResolved
                    || !registered
                    || !missingAdapterBlocked
                    || !requiredMissingBaselineBlocked
                    || !optionalMissingBaselineWarned
                    || !cleared)
                {
                    logger.Warning(
                        "QA Object Reset Required Guardrails Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "required-guardrails"),
                            LogFields.Field("targetCollected", targetCollected),
                            LogFields.Field("targetResolved", targetResolved),
                            LogFields.Field("registered", registered),
                            LogFields.Field("missingAdapterBlocked", missingAdapterBlocked),
                            LogFields.Field("requiredMissingBaselineBlocked", requiredMissingBaselineBlocked),
                            LogFields.Field("optionalMissingBaselineWarned", optionalMissingBaselineWarned),
                            LogFields.Field("cleared", cleared),
                            LogFields.Field("missingAdapterStatus", missingAdapterResult.Status.ToString()),
                            LogFields.Field("requiredMissingBaselineStatus", requiredMissingBaselineResult.Status.ToString()),
                            LogFields.Field("optionalMissingBaselineStatus", optionalMissingBaselineResult.Status.ToString()),
                            LogFields.Field("missingAdapterBlockingIssues", missingAdapterResult.BlockingIssueCount),
                            LogFields.Field("requiredMissingBaselineBlockingIssues", requiredMissingBaselineResult.BlockingIssueCount),
                            LogFields.Field("optionalMissingBaselineNonBlockingIssues", optionalMissingBaselineResult.NonBlockingIssueCount)));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Required Guardrails Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "required-guardrails"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("targetCollected", targetCollected),
                        LogFields.Field("targetResolved", targetResolved),
                        LogFields.Field("registered", registered),
                        LogFields.Field("allowNoParticipants", missingAdapterRequest.AllowsNoParticipants),
                        LogFields.Field("missingAdapterStatus", missingAdapterResult.Status.ToString()),
                        LogFields.Field("missingAdapterParticipants", missingAdapterResult.ParticipantCount),
                        LogFields.Field("missingAdapterBlockingIssues", missingAdapterResult.BlockingIssueCount),
                        LogFields.Field("missingAdapterBlocked", missingAdapterBlocked),
                        LogFields.Field("requiredBaselineConfigured", requiredParticipant.HasBaseline),
                        LogFields.Field("requiredMissingBaselineStatus", requiredMissingBaselineResult.Status.ToString()),
                        LogFields.Field("requiredMissingBaselineParticipants", requiredMissingBaselineResult.ParticipantCount),
                        LogFields.Field("requiredMissingBaselineFailed", requiredMissingBaselineResult.ParticipantFailedCount),
                        LogFields.Field("requiredMissingBaselineBlockingIssues", requiredMissingBaselineResult.BlockingIssueCount),
                        LogFields.Field("requiredMissingBaselineBlocked", requiredMissingBaselineBlocked),
                        LogFields.Field("optionalBaselineConfigured", optionalParticipant.HasBaseline),
                        LogFields.Field("optionalMissingBaselineStatus", optionalMissingBaselineResult.Status.ToString()),
                        LogFields.Field("optionalMissingBaselineParticipants", optionalMissingBaselineResult.ParticipantCount),
                        LogFields.Field("optionalMissingBaselineFailed", optionalMissingBaselineResult.ParticipantFailedCount),
                        LogFields.Field("optionalMissingBaselineBlockingIssues", optionalMissingBaselineResult.BlockingIssueCount),
                        LogFields.Field("optionalMissingBaselineNonBlockingIssues", optionalMissingBaselineResult.NonBlockingIssueCount),
                        LogFields.Field("optionalMissingBaselineWarned", optionalMissingBaselineWarned),
                        LogFields.Field("cleared", cleared),
                        LogFields.Field("summary", requiredMissingBaselineResult.ToDiagnosticString())));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Required Guardrails Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "required-guardrails"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-required-guardrails-smoke-cleanup");
                }
            }
        }


        internal static async Task<bool> RunUnityAdaptersClosureSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Unity Adapters Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "unity-adapters-closure"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                string objectEntryId = $"qa.object-reset.unity-adapters.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetUnityAdaptersClosure_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Unity Adapters Closure Target",
                    qaActivityOwner: activeActivity);

                var unitySource = qaObject.AddComponent<ObjectResetUnityParticipantSource>();
                unitySource.ConfigureForQa(qaRegisterOnEnable: false);

                var targetBaselinePosition = new Vector3(1.5f, 2.5f, -3.5f);
                var targetBaselineEuler = new Vector3(10f, 25f, 40f);
                var targetBaselineScale = new Vector3(1.25f, 0.75f, 2f);
                qaObject.transform.localPosition = targetBaselinePosition;
                qaObject.transform.localRotation = Quaternion.Euler(targetBaselineEuler);
                qaObject.transform.localScale = targetBaselineScale;

                var transformParticipant = qaObject.AddComponent<ObjectResetTransformParticipant>();
                transformParticipant.ConfigureForQa(
                    declaration,
                    "qa.object-reset.unity-adapters.transform",
                    ObjectResetParticipantRequiredness.Required,
                    100,
                    "QA Object Reset Unity Adapters Transform Participant",
                    source,
                    "qa.object-reset.unity-adapters.transform");
                transformParticipant.ConfigureTransformBaselineForQa(
                    qaObject.transform,
                    qaBaselineConfigured: true,
                    qaResetLocalPosition: true,
                    qaResetLocalRotation: true,
                    qaResetLocalScale: true,
                    targetBaselinePosition,
                    targetBaselineEuler,
                    targetBaselineScale);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-unity-adapters-closure-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                ObjectEntryDescriptor descriptor = default;
                bool targetCollected = snapshot != null && snapshot.TryGet(id, out descriptor);
                if (!targetCollected)
                {
                    logger.Warning(
                        "QA Object Reset Unity Adapters Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "unity-adapters-closure"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                var target = ObjectResetTarget.FromDescriptor(descriptor);
                bool targetResolved = ObjectResetTargetResolver.ResolveTarget(
                    snapshot,
                    ObjectResetRequest.ForTarget(target, source, "qa.object-reset.unity-adapters.target-resolution")).Succeeded;

                qaObject.transform.localPosition = new Vector3(-8f, 9f, 10f);
                qaObject.transform.localRotation = Quaternion.Euler(70f, 80f, 90f);
                qaObject.transform.localScale = new Vector3(3f, 4f, 5f);

                unitySource.ConfigureForQa(qaRegisterOnEnable: false, transformParticipant);
                bool registered = unitySource.RegisterWithCurrentHost()
                    && runtimeHost.IsObjectResetParticipantSourceRegistered(unitySource);
                var hostResult = await runtimeHost.RequestObjectResetAsync(ObjectResetRequest.ForTarget(
                    target,
                    source,
                    "qa.object-reset.unity-adapters.transform"));

                bool transformPositionReset = VectorApproximatelyEqual(qaObject.transform.localPosition, targetBaselinePosition);
                bool transformRotationReset = EulerApproximatelyEqual(qaObject.transform.localEulerAngles, targetBaselineEuler);
                bool transformScaleReset = VectorApproximatelyEqual(qaObject.transform.localScale, targetBaselineScale);
                bool cleared = unitySource.ClearRegistration()
                    && !runtimeHost.IsObjectResetParticipantSourceRegistered(unitySource);

                var objectResetRuntime = new ObjectResetRuntime();
                var requireParticipantsPolicy = new ObjectResetPolicy(
                    requireCurrentSnapshot: true,
                    allowNoParticipants: false);
                var missingAdapterResult = objectResetRuntime.Execute(
                    snapshot,
                    new ObjectResetRequest(
                        target,
                        requireParticipantsPolicy,
                        source,
                        "qa.object-reset.unity-adapters.missing-adapter"),
                    new SyntheticObjectResetParticipantSource());

                var requiredMissingBaselineObject = new GameObject("QA_ObjectResetUnityAdaptersClosure_RequiredMissingBaseline");
                requiredMissingBaselineObject.transform.SetParent(qaObject.transform, false);
                var requiredMissingBaselineParticipant = requiredMissingBaselineObject.AddComponent<ObjectResetTransformParticipant>();
                requiredMissingBaselineParticipant.ConfigureForQa(
                    declaration,
                    "qa.object-reset.unity-adapters.required-missing-baseline",
                    ObjectResetParticipantRequiredness.Required,
                    100,
                    "QA Object Reset Unity Adapters Required Missing Baseline Participant",
                    source,
                    "qa.object-reset.unity-adapters.required-missing-baseline");
                requiredMissingBaselineParticipant.ConfigureTransformBaselineForQa(
                    qaObject.transform,
                    qaBaselineConfigured: false,
                    qaResetLocalPosition: true,
                    qaResetLocalRotation: true,
                    qaResetLocalScale: true,
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.one);
                var requiredMissingBaselineResult = objectResetRuntime.Execute(
                    snapshot,
                    ObjectResetRequest.ForTarget(
                        target,
                        source,
                        "qa.object-reset.unity-adapters.required-missing-baseline"),
                    new SyntheticObjectResetParticipantSource(requiredMissingBaselineParticipant));

                var optionalMissingBaselineObject = new GameObject("QA_ObjectResetUnityAdaptersClosure_OptionalMissingBaseline");
                optionalMissingBaselineObject.transform.SetParent(qaObject.transform, false);
                var optionalMissingBaselineParticipant = optionalMissingBaselineObject.AddComponent<ObjectResetTransformParticipant>();
                optionalMissingBaselineParticipant.ConfigureForQa(
                    declaration,
                    "qa.object-reset.unity-adapters.optional-missing-baseline",
                    ObjectResetParticipantRequiredness.Optional,
                    100,
                    "QA Object Reset Unity Adapters Optional Missing Baseline Participant",
                    source,
                    "qa.object-reset.unity-adapters.optional-missing-baseline");
                optionalMissingBaselineParticipant.ConfigureTransformBaselineForQa(
                    qaObject.transform,
                    qaBaselineConfigured: false,
                    qaResetLocalPosition: true,
                    qaResetLocalRotation: true,
                    qaResetLocalScale: true,
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.one);
                var optionalMissingBaselineResult = objectResetRuntime.Execute(
                    snapshot,
                    ObjectResetRequest.ForTarget(
                        target,
                        source,
                        "qa.object-reset.unity-adapters.optional-missing-baseline"),
                    new SyntheticObjectResetParticipantSource(optionalMissingBaselineParticipant));

                bool hostSucceeded = hostResult is { Status: ObjectResetResultStatus.Succeeded, ParticipantCount: 1, ParticipantSucceededCount: 1, ParticipantFailedCount: 0, BlockingIssueCount: 0, NonBlockingIssueCount: 0 };
                bool missingAdapterBlocked = missingAdapterResult is { Status: ObjectResetResultStatus.Failed, ParticipantCount: 0, BlockingIssueCount: > 0 };
                bool requiredMissingBaselineBlocked = requiredMissingBaselineResult is { Status: ObjectResetResultStatus.Failed, ParticipantCount: 1, ParticipantFailedCount: 1, ParticipantBlockingFailureCount: 1, BlockingIssueCount: > 0 };
                bool optionalMissingBaselineWarned = optionalMissingBaselineResult is { Status: ObjectResetResultStatus.CompletedWithWarnings, ParticipantCount: 1, ParticipantFailedCount: 1, ParticipantBlockingFailureCount: 0, BlockingIssueCount: 0, NonBlockingIssueCount: > 0 };
                bool transformReset = transformPositionReset && transformRotationReset && transformScaleReset;
                bool closurePassed = targetCollected
                    && targetResolved
                    && registered
                    && cleared
                    && hostSucceeded
                    && transformReset
                    && missingAdapterBlocked
                    && requiredMissingBaselineBlocked
                    && optionalMissingBaselineWarned;

                if (!closurePassed)
                {
                    logger.Warning(
                        "QA Object Reset Unity Adapters Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "unity-adapters-closure"),
                            LogFields.Field("targetCollected", targetCollected),
                            LogFields.Field("targetResolved", targetResolved),
                            LogFields.Field("registered", registered),
                            LogFields.Field("cleared", cleared),
                            LogFields.Field("hostSucceeded", hostSucceeded),
                            LogFields.Field("transformReset", transformReset),
                            LogFields.Field("missingAdapterBlocked", missingAdapterBlocked),
                            LogFields.Field("requiredMissingBaselineBlocked", requiredMissingBaselineBlocked),
                            LogFields.Field("optionalMissingBaselineWarned", optionalMissingBaselineWarned),
                            LogFields.Field("hostStatus", hostResult.Status.ToString()),
                            LogFields.Field("missingAdapterStatus", missingAdapterResult.Status.ToString()),
                            LogFields.Field("requiredMissingBaselineStatus", requiredMissingBaselineResult.Status.ToString()),
                            LogFields.Field("optionalMissingBaselineStatus", optionalMissingBaselineResult.Status.ToString())));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Unity Adapters Closure Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "unity-adapters-closure"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                        LogFields.Field("snapshotSource", snapshot.ToDiagnosticText(x => x.Source)),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("targetCollected", targetCollected),
                        LogFields.Field("targetResolved", targetResolved),
                        LogFields.Field("authoredParticipants", unitySource.AuthoredParticipantCount),
                        LogFields.Field("registered", registered),
                        LogFields.Field("cleared", cleared),
                        LogFields.Field("hostStatus", hostResult.Status.ToString()),
                        LogFields.Field("hostParticipants", hostResult.ParticipantCount),
                        LogFields.Field("hostParticipantSucceeded", hostResult.ParticipantSucceededCount),
                        LogFields.Field("hostParticipantSkipped", hostResult.ParticipantSkippedCount),
                        LogFields.Field("hostParticipantFailed", hostResult.ParticipantFailedCount),
                        LogFields.Field("transformPositionReset", transformPositionReset),
                        LogFields.Field("transformRotationReset", transformRotationReset),
                        LogFields.Field("transformScaleReset", transformScaleReset),
                        LogFields.Field("missingAdapterStatus", missingAdapterResult.Status.ToString()),
                        LogFields.Field("missingAdapterBlocked", missingAdapterBlocked),
                        LogFields.Field("requiredMissingBaselineStatus", requiredMissingBaselineResult.Status.ToString()),
                        LogFields.Field("requiredMissingBaselineBlocked", requiredMissingBaselineBlocked),
                        LogFields.Field("optionalMissingBaselineStatus", optionalMissingBaselineResult.Status.ToString()),
                        LogFields.Field("optionalMissingBaselineWarned", optionalMissingBaselineWarned),
                        LogFields.Field("blockingIssues", hostResult.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", hostResult.NonBlockingIssueCount),
                        LogFields.Field("summary", hostResult.ToDiagnosticString())));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Unity Adapters Closure Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "unity-adapters-closure"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-unity-adapters-closure-smoke-cleanup");
                }
            }
        }


        internal static async Task<bool> RunGameObjectActiveClosureSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject sourceObject = null;
            GameObject inactiveBaselineObject = null;
            GameObject activeBaselineObject = null;
            GameObject guardrailObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset GameObject Active Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "gameobject-active-closure"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                sourceObject = new GameObject("QA_ObjectResetGameObjectActive_Source");
                var unitySource = sourceObject.AddComponent<ObjectResetUnityParticipantSource>();
                unitySource.ConfigureForQa(qaRegisterOnEnable: false);

                string inactiveObjectEntryId = $"qa.object-reset.gameobject-active.inactive-target.{Guid.NewGuid():N}";
                inactiveBaselineObject = new GameObject("QA_ObjectResetGameObjectActive_InactiveBaselineTarget");
                var inactiveDeclaration = inactiveBaselineObject.AddComponent<ObjectEntryDeclaration>();
                inactiveDeclaration.ConfigureForQa(
                    inactiveObjectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset GameObject Active Inactive Baseline Target",
                    qaActivityOwner: activeActivity);
                var inactiveParticipant = inactiveBaselineObject.AddComponent<ObjectResetGameObjectActiveParticipant>();
                inactiveParticipant.ConfigureForQa(
                    inactiveDeclaration,
                    "qa.object-reset.gameobject-active.inactive-baseline",
                    ObjectResetParticipantRequiredness.Required,
                    100,
                    "QA Object Reset GameObject Active Inactive Baseline Participant",
                    source,
                    "qa.object-reset.gameobject-active.inactive-baseline");
                inactiveParticipant.ConfigureActiveBaselineForQa(
                    inactiveBaselineObject,
                    qaBaselineConfigured: true,
                    qaBaselineActiveSelf: false);

                string activeObjectEntryId = $"qa.object-reset.gameobject-active.active-target.{Guid.NewGuid():N}";
                activeBaselineObject = new GameObject("QA_ObjectResetGameObjectActive_ActiveBaselineTarget");
                var activeDeclaration = activeBaselineObject.AddComponent<ObjectEntryDeclaration>();
                activeDeclaration.ConfigureForQa(
                    activeObjectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset GameObject Active Active Baseline Target",
                    qaActivityOwner: activeActivity);
                var activeParticipant = activeBaselineObject.AddComponent<ObjectResetGameObjectActiveParticipant>();
                activeParticipant.ConfigureForQa(
                    activeDeclaration,
                    "qa.object-reset.gameobject-active.active-baseline",
                    ObjectResetParticipantRequiredness.Required,
                    100,
                    "QA Object Reset GameObject Active Active Baseline Participant",
                    source,
                    "qa.object-reset.gameobject-active.active-baseline");
                activeParticipant.ConfigureActiveBaselineForQa(
                    activeBaselineObject,
                    qaBaselineConfigured: true,
                    qaBaselineActiveSelf: true);

                string guardrailObjectEntryId = $"qa.object-reset.gameobject-active.guardrails.{Guid.NewGuid():N}";
                guardrailObject = new GameObject("QA_ObjectResetGameObjectActive_GuardrailTarget");
                var guardrailDeclaration = guardrailObject.AddComponent<ObjectEntryDeclaration>();
                guardrailDeclaration.ConfigureForQa(
                    guardrailObjectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset GameObject Active Guardrail Target",
                    qaActivityOwner: activeActivity);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-gameobject-active-closure-smoke");

                var inactiveId = ObjectEntryId.From(inactiveObjectEntryId);
                var activeId = ObjectEntryId.From(activeObjectEntryId);
                var guardrailId = ObjectEntryId.From(guardrailObjectEntryId);

                ObjectEntryDescriptor inactiveDescriptor = default;
                ObjectEntryDescriptor activeDescriptor = default;
                ObjectEntryDescriptor guardrailDescriptor = default;
                bool inactiveTargetCollected = snapshot != null && snapshot.TryGet(inactiveId, out inactiveDescriptor);
                bool activeTargetCollected = snapshot != null && snapshot.TryGet(activeId, out activeDescriptor);
                bool guardrailTargetCollected = snapshot != null && snapshot.TryGet(guardrailId, out guardrailDescriptor);
                if (!inactiveTargetCollected || !activeTargetCollected || !guardrailTargetCollected)
                {
                    logger.Warning(
                        "QA Object Reset GameObject Active Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "gameobject-active-closure"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                            LogFields.Field("inactiveTargetCollected", inactiveTargetCollected),
                            LogFields.Field("activeTargetCollected", activeTargetCollected),
                            LogFields.Field("guardrailTargetCollected", guardrailTargetCollected)));
                    return false;
                }

                var inactiveTarget = ObjectResetTarget.FromDescriptor(inactiveDescriptor);
                var activeTarget = ObjectResetTarget.FromDescriptor(activeDescriptor);
                var guardrailTarget = ObjectResetTarget.FromDescriptor(guardrailDescriptor);

                bool inactiveTargetResolved = ObjectResetTargetResolver.ResolveTarget(
                    snapshot,
                    ObjectResetRequest.ForTarget(inactiveTarget, source, "qa.object-reset.gameobject-active.inactive-target-resolution")).Succeeded;
                bool activeTargetResolved = ObjectResetTargetResolver.ResolveTarget(
                    snapshot,
                    ObjectResetRequest.ForTarget(activeTarget, source, "qa.object-reset.gameobject-active.active-target-resolution")).Succeeded;
                bool guardrailTargetResolved = ObjectResetTargetResolver.ResolveTarget(
                    snapshot,
                    ObjectResetRequest.ForTarget(guardrailTarget, source, "qa.object-reset.gameobject-active.guardrail-target-resolution")).Succeeded;

                inactiveBaselineObject.SetActive(true);
                activeBaselineObject.SetActive(false);

                unitySource.ConfigureForQa(qaRegisterOnEnable: false, inactiveParticipant, activeParticipant);
                bool registered = unitySource.RegisterWithCurrentHost()
                    && runtimeHost.IsObjectResetParticipantSourceRegistered(unitySource);

                var inactiveBaselineResult = await runtimeHost.RequestObjectResetAsync(ObjectResetRequest.ForTarget(
                    inactiveTarget,
                    source,
                    "qa.object-reset.gameobject-active.inactive-baseline"));
                bool inactiveTargetReset = !inactiveBaselineObject.activeSelf;

                var activeBaselineResult = await runtimeHost.RequestObjectResetAsync(ObjectResetRequest.ForTarget(
                    activeTarget,
                    source,
                    "qa.object-reset.gameobject-active.active-baseline"));
                bool activeTargetReset = activeBaselineObject.activeSelf;

                bool cleared = unitySource.ClearRegistration()
                    && !runtimeHost.IsObjectResetParticipantSourceRegistered(unitySource);

                var objectResetRuntime = new ObjectResetRuntime();
                var requireParticipantsPolicy = new ObjectResetPolicy(
                    requireCurrentSnapshot: true,
                    allowNoParticipants: false);
                var missingAdapterResult = objectResetRuntime.Execute(
                    snapshot,
                    new ObjectResetRequest(
                        guardrailTarget,
                        requireParticipantsPolicy,
                        source,
                        "qa.object-reset.gameobject-active.missing-adapter"),
                    new SyntheticObjectResetParticipantSource());

                var requiredMissingBaselineParticipant = guardrailObject.AddComponent<ObjectResetGameObjectActiveParticipant>();
                requiredMissingBaselineParticipant.ConfigureForQa(
                    guardrailDeclaration,
                    "qa.object-reset.gameobject-active.required-missing-baseline",
                    ObjectResetParticipantRequiredness.Required,
                    100,
                    "QA Object Reset GameObject Active Required Missing Baseline Participant",
                    source,
                    "qa.object-reset.gameobject-active.required-missing-baseline");
                requiredMissingBaselineParticipant.ConfigureActiveBaselineForQa(
                    guardrailObject,
                    qaBaselineConfigured: false,
                    qaBaselineActiveSelf: false);
                var requiredMissingBaselineResult = objectResetRuntime.Execute(
                    snapshot,
                    ObjectResetRequest.ForTarget(
                        guardrailTarget,
                        source,
                        "qa.object-reset.gameobject-active.required-missing-baseline"),
                    new SyntheticObjectResetParticipantSource(requiredMissingBaselineParticipant));

                var optionalMissingBaselineObject = new GameObject("QA_ObjectResetGameObjectActive_OptionalMissingBaseline");
                optionalMissingBaselineObject.transform.SetParent(sourceObject.transform, false);
                var optionalMissingBaselineParticipant = optionalMissingBaselineObject.AddComponent<ObjectResetGameObjectActiveParticipant>();
                optionalMissingBaselineParticipant.ConfigureForQa(
                    guardrailDeclaration,
                    "qa.object-reset.gameobject-active.optional-missing-baseline",
                    ObjectResetParticipantRequiredness.Optional,
                    100,
                    "QA Object Reset GameObject Active Optional Missing Baseline Participant",
                    source,
                    "qa.object-reset.gameobject-active.optional-missing-baseline");
                optionalMissingBaselineParticipant.ConfigureActiveBaselineForQa(
                    guardrailObject,
                    qaBaselineConfigured: false,
                    qaBaselineActiveSelf: false);
                var optionalMissingBaselineResult = objectResetRuntime.Execute(
                    snapshot,
                    ObjectResetRequest.ForTarget(
                        guardrailTarget,
                        source,
                        "qa.object-reset.gameobject-active.optional-missing-baseline"),
                    new SyntheticObjectResetParticipantSource(optionalMissingBaselineParticipant));

                bool inactiveResetSucceeded = inactiveBaselineResult is { Status: ObjectResetResultStatus.Succeeded, ParticipantCount: 1, ParticipantSucceededCount: 1, BlockingIssueCount: 0, NonBlockingIssueCount: 0 }
                    && inactiveTargetReset;
                bool activeResetSucceeded = activeBaselineResult is { Status: ObjectResetResultStatus.Succeeded, ParticipantCount: 1, ParticipantSucceededCount: 1, BlockingIssueCount: 0, NonBlockingIssueCount: 0 }
                    && activeTargetReset;
                bool missingAdapterBlocked = missingAdapterResult is { Status: ObjectResetResultStatus.Failed, ParticipantCount: 0, BlockingIssueCount: > 0 };
                bool requiredMissingBaselineBlocked = requiredMissingBaselineResult is { Status: ObjectResetResultStatus.Failed, ParticipantCount: 1, ParticipantFailedCount: 1, ParticipantBlockingFailureCount: 1, BlockingIssueCount: > 0 };
                bool optionalMissingBaselineWarned = optionalMissingBaselineResult is { Status: ObjectResetResultStatus.CompletedWithWarnings, ParticipantCount: 1, ParticipantFailedCount: 1, ParticipantBlockingFailureCount: 0, BlockingIssueCount: 0, NonBlockingIssueCount: > 0 };

                bool closurePassed = inactiveTargetCollected
                    && activeTargetCollected
                    && guardrailTargetCollected
                    && inactiveTargetResolved
                    && activeTargetResolved
                    && guardrailTargetResolved
                    && registered
                    && cleared
                    && inactiveResetSucceeded
                    && activeResetSucceeded
                    && missingAdapterBlocked
                    && requiredMissingBaselineBlocked
                    && optionalMissingBaselineWarned;

                if (!closurePassed)
                {
                    logger.Warning(
                        "QA Object Reset GameObject Active Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "gameobject-active-closure"),
                            LogFields.Field("inactiveTargetCollected", inactiveTargetCollected),
                            LogFields.Field("activeTargetCollected", activeTargetCollected),
                            LogFields.Field("guardrailTargetCollected", guardrailTargetCollected),
                            LogFields.Field("inactiveTargetResolved", inactiveTargetResolved),
                            LogFields.Field("activeTargetResolved", activeTargetResolved),
                            LogFields.Field("guardrailTargetResolved", guardrailTargetResolved),
                            LogFields.Field("registered", registered),
                            LogFields.Field("cleared", cleared),
                            LogFields.Field("inactiveResetSucceeded", inactiveResetSucceeded),
                            LogFields.Field("activeResetSucceeded", activeResetSucceeded),
                            LogFields.Field("missingAdapterBlocked", missingAdapterBlocked),
                            LogFields.Field("requiredMissingBaselineBlocked", requiredMissingBaselineBlocked),
                            LogFields.Field("optionalMissingBaselineWarned", optionalMissingBaselineWarned),
                            LogFields.Field("inactiveStatus", inactiveBaselineResult.Status.ToString()),
                            LogFields.Field("activeStatus", activeBaselineResult.Status.ToString()),
                            LogFields.Field("missingAdapterStatus", missingAdapterResult.Status.ToString()),
                            LogFields.Field("requiredMissingBaselineStatus", requiredMissingBaselineResult.Status.ToString()),
                            LogFields.Field("optionalMissingBaselineStatus", optionalMissingBaselineResult.Status.ToString())));
                    return false;
                }

                logger.Info(
                    "QA Object Reset GameObject Active Closure Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "gameobject-active-closure"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                        LogFields.Field("snapshotSource", snapshot.ToDiagnosticText(x => x.Source)),
                        LogFields.Field("inactiveObjectEntry", inactiveId.StableText),
                        LogFields.Field("activeObjectEntry", activeId.StableText),
                        LogFields.Field("inactiveTargetCollected", inactiveTargetCollected),
                        LogFields.Field("activeTargetCollected", activeTargetCollected),
                        LogFields.Field("guardrailTargetCollected", guardrailTargetCollected),
                        LogFields.Field("inactiveTargetResolved", inactiveTargetResolved),
                        LogFields.Field("activeTargetResolved", activeTargetResolved),
                        LogFields.Field("guardrailTargetResolved", guardrailTargetResolved),
                        LogFields.Field("authoredParticipants", unitySource.AuthoredParticipantCount),
                        LogFields.Field("registered", registered),
                        LogFields.Field("cleared", cleared),
                        LogFields.Field("inactiveStatus", inactiveBaselineResult.Status.ToString()),
                        LogFields.Field("inactiveParticipants", inactiveBaselineResult.ParticipantCount),
                        LogFields.Field("inactiveParticipantSucceeded", inactiveBaselineResult.ParticipantSucceededCount),
                        LogFields.Field("inactiveActiveSelfReset", inactiveTargetReset),
                        LogFields.Field("activeStatus", activeBaselineResult.Status.ToString()),
                        LogFields.Field("activeParticipants", activeBaselineResult.ParticipantCount),
                        LogFields.Field("activeParticipantSucceeded", activeBaselineResult.ParticipantSucceededCount),
                        LogFields.Field("activeActiveSelfReset", activeTargetReset),
                        LogFields.Field("missingAdapterStatus", missingAdapterResult.Status.ToString()),
                        LogFields.Field("missingAdapterBlocked", missingAdapterBlocked),
                        LogFields.Field("requiredMissingBaselineStatus", requiredMissingBaselineResult.Status.ToString()),
                        LogFields.Field("requiredMissingBaselineBlocked", requiredMissingBaselineBlocked),
                        LogFields.Field("optionalMissingBaselineStatus", optionalMissingBaselineResult.Status.ToString()),
                        LogFields.Field("optionalMissingBaselineWarned", optionalMissingBaselineWarned),
                        LogFields.Field("blockingIssues", inactiveBaselineResult.BlockingIssueCount + activeBaselineResult.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", inactiveBaselineResult.NonBlockingIssueCount + activeBaselineResult.NonBlockingIssueCount),
                        LogFields.Field("summary", inactiveBaselineResult.ToDiagnosticString())));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset GameObject Active Closure Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "gameobject-active-closure"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (sourceObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(sourceObject);
                }

                if (inactiveBaselineObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(inactiveBaselineObject);
                }

                if (activeBaselineObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(activeBaselineObject);
                }

                if (guardrailObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(guardrailObject);
                }

                runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-gameobject-active-closure-smoke-cleanup");
            }
        }

        internal static async Task<bool> RunFoundationClosureSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Foundation Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "foundation-closure"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                string objectEntryId = $"qa.object-reset.foundation.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetFoundation_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Foundation Target",
                    qaActivityOwner: activeActivity);

                var trigger = qaObject.AddComponent<ObjectResetTrigger>();
                trigger.ConfigureForQa(
                    declaration,
                    string.Empty,
                    "qa.object-reset.foundation-closure.trigger",
                    qaAllowNoParticipants: true);

                var bridge = qaObject.AddComponent<ObjectResetTriggerUnityEventBridge>();
                var counters = new BridgeEventCounters();
                AttachBridgeCounters(bridge, counters);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-foundation-closure-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                ObjectEntryDescriptor descriptor = default;
                bool targetCollected = snapshot != null && snapshot.TryGet(id, out descriptor);
                if (!targetCollected)
                {
                    logger.Warning(
                        "QA Object Reset Foundation Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "foundation-closure"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot is { IsAvailable: true }),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                var hostRequest = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(descriptor),
                    source,
                    "qa.object-reset.foundation-closure.host");
                var targetResolution = ObjectResetTargetResolver.ResolveTarget(snapshot, hostRequest);

                var requiredSuccess = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.foundation.required"),
                        hostRequest.Target,
                        20,
                        "QA Object Reset Foundation Required Participant",
                        source,
                        "qa.object-reset.foundation-closure.required"),
                    SyntheticObjectResetParticipantMode.Success);
                var optionalSkipped = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Optional(
                        ObjectResetParticipantId.From("qa.object-reset.foundation.optional"),
                        hostRequest.Target,
                        10,
                        "QA Object Reset Foundation Optional Participant",
                        source,
                        "qa.object-reset.foundation-closure.optional"),
                    SyntheticObjectResetParticipantMode.SkippedOptional);

                runtimeHost.SetObjectResetParticipantSource(new SyntheticObjectResetParticipantSource(requiredSuccess, optionalSkipped));
                var hostResult = await runtimeHost.RequestObjectResetAsync(hostRequest);

                runtimeHost.SetObjectResetParticipantSource(null);
                trigger.RequestObjectReset();
                bool triggerCompleted = await WaitForObjectResetTriggerAsync(trigger, 32);

                bool hostSnapshotAvailable = runtimeHost.TryGetObjectEntryRuntimeContextSnapshot(out var hostSnapshot)
                    && hostSnapshot is { IsAvailable: true };
                bool targetResolved = targetResolution is { Succeeded: true, HasResolvedTarget: true }
                    && targetResolution.ResolvedTarget.Id == id
                    && targetResolution.BlockingIssueCount == 0;
                bool hostResultValid = hostResult is { Status: ObjectResetResultStatus.Succeeded, ParticipantCount: 2, ParticipantSucceededCount: 1, ParticipantSkippedCount: 1, ParticipantFailedCount: 0, BlockingIssueCount: 0, NonBlockingIssueCount: 0 };
                bool triggerResultValid = triggerCompleted
                    && !trigger.IsRequestInFlight
                    && trigger.LastEventPhase == FlowRequestEventPhase.Completed
                    && trigger.LastRequestSucceeded
                    && trigger.LastResultStatus == ObjectResetResultStatus.SucceededNoParticipants
                    && trigger.LastParticipantCount == 0
                    && trigger.LastBlockingIssueCount == 0
                    && trigger.LastNonBlockingIssueCount == 0;
                bool bridgeEventsValid = counters.submitted == 1
                    && counters.succeeded == 1
                    && counters.succeededWithParticipants == 0
                    && counters.succeededNoParticipants == 1
                    && counters.completedWithWarnings == 0
                    && counters.ignored == 0
                    && counters.failed == 0
                    && counters.completed == 1;
                int blockingIssues = hostResult.BlockingIssueCount + trigger.LastBlockingIssueCount + targetResolution.BlockingIssueCount;
                int nonBlockingIssues = hostResult.NonBlockingIssueCount + trigger.LastNonBlockingIssueCount + targetResolution.NonBlockingIssueCount;

                if (!hostSnapshotAvailable
                    || !targetCollected
                    || !targetResolved
                    || !hostResultValid
                    || !triggerResultValid
                    || !bridgeEventsValid
                    || blockingIssues != 0
                    || nonBlockingIssues != 0)
                {
                    logger.Warning(
                        "QA Object Reset Foundation Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "foundation-closure"),
                            LogFields.Field("hostSnapshotAvailable", hostSnapshotAvailable),
                            LogFields.Field("targetCollected", targetCollected),
                            LogFields.Field("targetResolved", targetResolved),
                            LogFields.Field("hostResultValid", hostResultValid),
                            LogFields.Field("triggerResultValid", triggerResultValid),
                            LogFields.Field("bridgeEventsValid", bridgeEventsValid),
                            LogFields.Field("hostStatus", hostResult.Status.ToString()),
                            LogFields.Field("triggerStatus", trigger.LastResultStatus.ToString()),
                            LogFields.Field("blockingIssues", blockingIssues),
                            LogFields.Field("nonBlockingIssues", nonBlockingIssues)));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Foundation Closure Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "foundation-closure"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", hostSnapshotAvailable),
                        LogFields.Field("snapshotSource", hostSnapshot.ToDiagnosticText(x => x.Source)),
                        LogFields.Field("snapshotRevision", runtimeHost.ObjectEntryRuntimeContextRevision),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("targetCollected", targetCollected),
                        LogFields.Field("targetResolved", targetResolved),
                        LogFields.Field("hostStatus", hostResult.Status.ToString()),
                        LogFields.Field("hostParticipants", hostResult.ParticipantCount),
                        LogFields.Field("hostParticipantSucceeded", hostResult.ParticipantSucceededCount),
                        LogFields.Field("hostParticipantSkipped", hostResult.ParticipantSkippedCount),
                        LogFields.Field("hostParticipantFailed", hostResult.ParticipantFailedCount),
                        LogFields.Field("triggerCompleted", triggerCompleted),
                        LogFields.Field("triggerStatus", trigger.LastResultStatus.ToString()),
                        LogFields.Field("bridgeSubmitted", counters.submitted),
                        LogFields.Field("bridgeSucceeded", counters.succeeded),
                        LogFields.Field("bridgeSucceededNoParticipants", counters.succeededNoParticipants),
                        LogFields.Field("bridgeCompleted", counters.completed),
                        LogFields.Field("blockingIssues", blockingIssues),
                        LogFields.Field("nonBlockingIssues", nonBlockingIssues),
                        LogFields.Field("summary", hostResult.ToDiagnosticString())));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Foundation Closure Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "foundation-closure"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-foundation-closure-smoke-cleanup");
                }
            }
        }


        private static async Task<bool> WaitForObjectResetTriggerAsync(
            ObjectResetTrigger trigger,
            int maxYields)
        {
            if (trigger == null)
            {
                return false;
            }

            int iterations = Math.Max(1, maxYields);
            for (int i = 0; i < iterations; i++)
            {
                await Task.Yield();
                if (!trigger.IsRequestInFlight && trigger.HasLastResult)
                {
                    return true;
                }
            }

            return !trigger.IsRequestInFlight && trigger.HasLastResult;
        }

        private static ObjectEntryRuntimeContextSnapshot CreateSnapshot(
            string source,
            params ObjectEntryDescriptor[] descriptors)
        {
            var set = new ObjectEntrySet(descriptors);
            var result = new ObjectEntryDeclarationSourceResult(
                set,
                ObjectEntryResultStatus.Accepted,
                declarationCount: set.Count,
                candidateDescriptorCount: set.Count,
                acceptedDeclarationCount: set.Count,
                rejectedDeclarationCount: 0,
                filteredDeclarationCount: 0);
            return result.ToRuntimeContextSnapshot($"{nameof(ObjectResetQaSmokeRunner)}:{source}");
        }


        private static bool VectorApproximatelyEqual(Vector3 actual, Vector3 expected)
        {
            const float tolerance = 0.001f;
            return Mathf.Abs(actual.x - expected.x) <= tolerance
                && Mathf.Abs(actual.y - expected.y) <= tolerance
                && Mathf.Abs(actual.z - expected.z) <= tolerance;
        }

        private static bool EulerApproximatelyEqual(Vector3 actual, Vector3 expected)
        {
            const float tolerance = 0.001f;
            return Mathf.Abs(Mathf.DeltaAngle(actual.x, expected.x)) <= tolerance
                && Mathf.Abs(Mathf.DeltaAngle(actual.y, expected.y)) <= tolerance
                && Mathf.Abs(Mathf.DeltaAngle(actual.z, expected.z)) <= tolerance;
        }


        private static void AttachBridgeCounters(ObjectResetTriggerUnityEventBridge bridge, BridgeEventCounters counters)
        {
            if (bridge == null || counters == null)
            {
                return;
            }

            bridge.RequestSubmitted.AddListener(() => counters.submitted++);
            bridge.RequestSucceeded.AddListener(() => counters.succeeded++);
            bridge.RequestSucceededWithParticipants.AddListener(() => counters.succeededWithParticipants++);
            bridge.RequestSucceededNoParticipants.AddListener(() => counters.succeededNoParticipants++);
            bridge.RequestCompletedWithWarnings.AddListener(() => counters.completedWithWarnings++);
            bridge.RequestIgnored.AddListener(() => counters.ignored++);
            bridge.RequestFailed.AddListener(() => counters.failed++);
            bridge.RequestCompleted.AddListener(() => counters.completed++);
        }

        private sealed class BridgeEventCounters
        {
            public int submitted;
            public int succeeded;
            public int succeededWithParticipants;
            public int succeededNoParticipants;
            public int completedWithWarnings;
            public int ignored;
            public int failed;
            public int completed;
        }


        private sealed class SyntheticObjectResetParticipant : IObjectResetParticipant
        {
            private readonly ObjectResetParticipantDescriptor _descriptor;
            private readonly SyntheticObjectResetParticipantMode _mode;

            public SyntheticObjectResetParticipant(
                ObjectResetParticipantDescriptor descriptor,
                SyntheticObjectResetParticipantMode mode)
            {
                _descriptor = descriptor;
                _mode = mode;
            }

            public ObjectResetParticipantDescriptor GetObjectResetDescriptor()
            {
                return _descriptor;
            }

            public ObjectResetParticipantResult ResetObject(ObjectResetContext context)
            {
                switch (_mode)
                {
                    case SyntheticObjectResetParticipantMode.Success:
                        return ObjectResetParticipantResult.Success(
                            context,
                            _descriptor.Source,
                            _descriptor.Reason,
                            "Synthetic Object Reset participant succeeded.");
                    case SyntheticObjectResetParticipantMode.SkippedOptional:
                        return ObjectResetParticipantResult.Skipped(
                            context,
                            ObjectResetParticipantResultStatus.SkippedOptional,
                            _descriptor.Source,
                            _descriptor.Reason,
                            "Synthetic Object Reset participant skipped as optional.");
                    case SyntheticObjectResetParticipantMode.BlockingFailure:
                        return ObjectResetParticipantResult.BlockingFailure(
                            context,
                            1,
                            _descriptor.Source,
                            _descriptor.Reason,
                            "Synthetic Object Reset participant failed as required.");
                    case SyntheticObjectResetParticipantMode.NonBlockingFailure:
                        return ObjectResetParticipantResult.NonBlockingFailure(
                            context,
                            1,
                            _descriptor.Source,
                            _descriptor.Reason,
                            "Synthetic Object Reset participant failed without blocking.");
                    case SyntheticObjectResetParticipantMode.InvalidResult:
                        return default(ObjectResetParticipantResult);
                    default:
                        return ObjectResetParticipantResult.Failure(
                            context,
                            1,
                            _descriptor.Source,
                            _descriptor.Reason,
                            "Synthetic Object Reset participant failed.");
                }
            }
        }

        private sealed class SyntheticObjectResetParticipantSource : IObjectResetParticipantSource
        {
            private readonly IReadOnlyList<IObjectResetParticipant> _participants;

            public SyntheticObjectResetParticipantSource(params IObjectResetParticipant[] participants)
            {
                _participants = participants ?? Array.Empty<IObjectResetParticipant>();
            }

            public IReadOnlyList<IObjectResetParticipant> ResolveObjectResetParticipants(
                ObjectResetRequest request,
                ObjectEntryDescriptor resolvedTarget)
            {
                return _participants;
            }
        }

        private enum SyntheticObjectResetParticipantMode
        {
            Success = 0,
            SkippedOptional = 1,
            BlockingFailure = 2,
            NonBlockingFailure = 3,
            InvalidResult = 4
        }
    }
}
#endif
