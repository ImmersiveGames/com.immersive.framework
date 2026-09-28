using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Materializes contextual Camera Presentations in their declaring Route or
    /// Activity scope and persistent Route/Activity selections directly in the
    /// Session scope. CameraOutputContext remains the sole winner authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-C Route/Activity Camera Presentation lifecycle bridge. CAMERA-037-B/C/D/E persistent Route/Activity selection continuity and replacement.")]
    internal sealed class CameraPresentationLifecycleRuntime : IDisposable
    {
        private readonly CameraPresentationMaterializationRuntime _materializer;
        private readonly CameraOutputSessionTopology _outputTopology;
        private readonly CameraSubjectAvailabilityContext _availability;
        private readonly PlayerCameraPresentationSelectionRuntime
            _playerSelection;
        private readonly Transform _physicalParent;
        private readonly RuntimeScopeContext _sessionScopeContext;
        private readonly FrameworkLogger _logger =
            FrameworkLogger.Create<CameraPresentationLifecycleRuntime>();
        private readonly Dictionary<Object, List<CameraPresentationMaterializationHandle>>
            _activeByOwner =
                new Dictionary<Object, List<CameraPresentationMaterializationHandle>>();
        private readonly Dictionary<CameraOutputId, CameraPresentationMaterializationHandle>
            _selectedByOutput =
                new Dictionary<CameraOutputId, CameraPresentationMaterializationHandle>();

        // Pending-selection state is keyed per owner (Route or Activity) so
        // independent owners can each have a pending persistent selection at
        // the same time, e.g. a Route and its own Startup Activity, as long as
        // they do not target the same Output (see _pendingOutputOwners below).
        // CAMERA-037-B/C/D/E reuse the exact same mechanism for both owner
        // kinds; there is no second winner/selection authority.
        private readonly Dictionary<Object, PendingSelectionEntry>
            _pendingSelectionsByOwner =
                new Dictionary<Object, PendingSelectionEntry>();

        // Tracks which owner currently has a pending (uncommitted) candidate
        // for a given Output, so a second owner targeting the SAME Output
        // while the first owner's candidate is still pending is rejected
        // with a precise diagnostic. IF-ADR-037 defines two different owners
        // targeting the same Output while both selections are pending as an
        // invalid composition; no precedence is inferred between them.
        private readonly Dictionary<CameraOutputId, Object> _pendingOutputOwners =
            new Dictionary<CameraOutputId, Object>();

        private bool _disposed;

        internal CameraPresentationLifecycleRuntime(
            CameraPresentationMaterializationRuntime materializer,
            CameraOutputSessionTopology outputTopology,
            CameraSubjectAvailabilityContext availability,
            PlayerCameraPresentationSelectionRuntime playerSelection,
            Transform physicalParent,
            RuntimeScopeContext sessionScopeContext)
        {
            _materializer = materializer ??
                throw new ArgumentNullException(nameof(materializer));
            _outputTopology = outputTopology ??
                throw new ArgumentNullException(nameof(outputTopology));
            _availability = availability ??
                throw new ArgumentNullException(nameof(availability));
            if (!sessionScopeContext.IsValid ||
                sessionScopeContext.Scope != RuntimeContentScope.Session)
            {
                throw new ArgumentException(
                    "Persistent Camera Presentation selection requires the active Session RuntimeContent scope context.",
                    nameof(sessionScopeContext));
            }

            _playerSelection = playerSelection;
            _physicalParent = physicalParent;
            _sessionScopeContext = sessionScopeContext;
        }

        internal int ActiveOwnerCount => _activeByOwner.Count;

        internal bool TryEnterRoute(
            RouteAsset route,
            RuntimeScopeContext context,
            string source,
            string reason,
            out string issue)
        {
            if (route == null)
            {
                issue = "Route Camera Presentation entry requires a Route.";
                return false;
            }

            if (!TryEnter(
                route,
                "Route",
                route.RouteName,
                route.CameraPresentations,
                context,
                source,
                reason,
                out issue))
            {
                return false;
            }

            if (TryEnterRouteSelection(
                    route,
                    source,
                    reason,
                    out issue))
            {
                return true;
            }

            TryExit(
                route,
                "Route",
                route.RouteName,
                source,
                "camera-route-contextual-presentation-selection-rollback",
                false,
                out _);
            return false;
        }

        /// <summary>
        /// Confirms a pending Session-owned persistent Camera Presentation
        /// selection previously admitted by <see cref="TryEnterRoute"/> or
        /// <see cref="TryEnterActivity"/> for this exact owner. A no-op
        /// (returns true) when that owner has no pending selection, including
        /// when it declared zero selections. Must be called only once the
        /// caller's own Route/Activity entry is effectively committed, and
        /// before that commit becomes irreversible.
        /// </summary>
        internal bool TryCommitSelection(
            Object owner,
            out string issue)
        {
            issue = string.Empty;
            if (owner == null ||
                !_pendingSelectionsByOwner.TryGetValue(
                    owner,
                    out PendingSelectionEntry entry))
            {
                return true;
            }

            if (!TryCommitPendingReplacements(entry, out issue))
            {
                RollbackPendingSelection(
                    owner,
                    entry,
                    nameof(CameraPresentationLifecycleRuntime),
                    "camera-selection-commit-rollback");
                _pendingSelectionsByOwner.Remove(owner);
                return false;
            }

            ClearPendingOutputOwners(owner);
            _pendingSelectionsByOwner.Remove(owner);
            return true;
        }

        /// <summary>
        /// Releases a pending Session-owned persistent Camera Presentation
        /// selection for this exact owner without committing it, preserving
        /// whatever occurrence was effective before <see cref="TryEnterRoute"/>
        /// or <see cref="TryEnterActivity"/> ran. A no-op (returns true) when
        /// that owner has no pending selection.
        /// </summary>
        internal bool TryRollbackSelection(
            Object owner,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (owner == null ||
                !_pendingSelectionsByOwner.TryGetValue(
                    owner,
                    out PendingSelectionEntry entry))
            {
                return true;
            }

            if (!RollbackPendingSelection(owner, entry, source, reason))
            {
                issue =
                    "Pending Camera Presentation selection rollback failed.";
                return false;
            }

            _pendingSelectionsByOwner.Remove(owner);
            return true;
        }

        internal bool TryExitRoute(
            RouteAsset route,
            string source,
            string reason,
            out string issue)
        {
            return TryExit(
                route,
                "Route",
                route != null ? route.RouteName : string.Empty,
                source,
                reason,
                false,
                out issue);
        }

        internal bool TryEnterActivity(
            ActivityAsset activity,
            RuntimeScopeContext context,
            string source,
            string reason,
            out string issue)
        {
            if (activity == null)
            {
                issue = "Activity Camera Presentation entry requires an Activity.";
                return false;
            }

            if (!TryEnter(
                activity,
                "Activity",
                activity.ActivityName,
                activity.CameraPresentations,
                context,
                source,
                reason,
                out issue))
            {
                return false;
            }

            if (TryEnterActivitySelection(
                    activity,
                    source,
                    reason,
                    out issue))
            {
                return true;
            }

            TryExit(
                activity,
                "Activity",
                activity.ActivityName,
                source,
                "camera-activity-contextual-presentation-selection-rollback",
                false,
                out _);
            return false;
        }

        internal bool TryExitActivity(
            ActivityAsset activity,
            string source,
            string reason,
            out string issue)
        {
            return TryExit(
                activity,
                "Activity",
                activity != null ? activity.ActivityName : string.Empty,
                source,
                reason,
                false,
                out issue);
        }

        private bool TryEnterRouteSelection(
            RouteAsset route,
            string source,
            string reason,
            out string issue)
        {
            return TryEnterPersistentSelection(
                route,
                "Route",
                route.RouteName,
                route.CameraPresentationSelections,
                source,
                reason,
                out issue);
        }

        private bool TryEnterActivitySelection(
            ActivityAsset activity,
            string source,
            string reason,
            out string issue)
        {
            return TryEnterPersistentSelection(
                activity,
                "Activity",
                activity.ActivityName,
                activity.CameraPresentationSelections,
                source,
                reason,
                out issue);
        }

        /// <summary>
        /// Shared Route/Activity implementation: admits every declared
        /// persistent Camera Presentation selection as a Session-owned
        /// candidate. A brand-new selection for an Output with no current
        /// persistent selection is admitted and must become the normal
        /// winner immediately. A selection that replaces an existing
        /// persistent selection for the same Output is admitted as a
        /// competing candidate without disturbing the current winner; the
        /// replacement only takes effect at <see cref="TryCommitSelection"/>.
        /// Zero declared selections is always a no-op. Two different owners
        /// (e.g. a Route and its own Startup Activity) may each have a
        /// pending selection at the same time as long as they target
        /// different Outputs; targeting the same Output while another
        /// owner's candidate for it is still pending is rejected.
        /// </summary>
        private bool TryEnterPersistentSelection(
            Object owner,
            string ownerKind,
            string ownerName,
            IReadOnlyList<CameraPresentationDefinition> definitions,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (definitions == null || definitions.Count == 0)
            {
                return true;
            }

            if (_pendingSelectionsByOwner.ContainsKey(owner))
            {
                issue =
                    $"{ownerKind} '{ownerName}' already has a pending Camera Presentation selection.";
                return false;
            }

            if (!TryValidateDefinitions(
                    $"{ownerKind} selection",
                    ownerName,
                    definitions,
                    out issue))
            {
                return false;
            }

            var incomingOutputs = new HashSet<CameraOutputId>();
            for (int index = 0; index < definitions.Count; index++)
            {
                CameraPresentationDefinition definition =
                    definitions[index];
                CameraOutputId outputId =
                    definition.OutputDefinition.OutputId;
                if (!incomingOutputs.Add(outputId))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' declares more than one persistent Camera Presentation selection for Output '{outputId}'.";
                    return false;
                }

                if (_pendingOutputOwners.TryGetValue(
                        outputId,
                        out Object pendingOwner) &&
                    !ReferenceEquals(pendingOwner, owner))
                {
                    ResolvePersistentSelectionOwnerDiagnostic(
                        pendingOwner,
                        out string pendingOwnerKind,
                        out string pendingOwnerName);
                    issue =
                        $"Output '{outputId}' has conflicting pending Camera Presentation selections from {pendingOwnerKind} '{pendingOwnerName}' and {ownerKind} '{ownerName}'. Two different owners cannot select the same Output while both selections are pending.";
                    return false;
                }

                if (!_selectedByOutput.TryGetValue(
                        outputId,
                        out CameraPresentationMaterializationHandle selected))
                {
                    continue;
                }

                if (selected.IsReleased)
                {
                    issue =
                        $"Output '{outputId}' retains a released persistent Camera Presentation selection handle.";
                    return false;
                }
            }

            var created =
                new List<CameraPresentationMaterializationHandle>();
            var replacedCurrents =
                new List<CameraPresentationMaterializationHandle>();
            for (int index = 0; index < definitions.Count; index++)
            {
                CameraPresentationDefinition definition =
                    definitions[index];
                CameraOutputId outputId =
                    definition.OutputDefinition.OutputId;
                _selectedByOutput.TryGetValue(
                    outputId,
                    out CameraPresentationMaterializationHandle current);
                if (current != null &&
                    ReferenceEquals(current.Definition, definition))
                {
                    continue;
                }

                if (!_outputTopology.TryGetOutput(
                        outputId,
                        out CameraOutputAuthoring output,
                        out string outputIssue))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' selected Camera Presentation '{definition.name}' for an unavailable Session Output. {outputIssue}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                CameraPresentationMaterializationResult materialized =
                    _materializer.Materialize(
                        _sessionScopeContext,
                        definition,
                        _physicalParent,
                        source,
                        reason);
                if (!materialized.Succeeded ||
                    materialized.Handle == null)
                {
                    issue =
                        $"{ownerKind} '{ownerName}' persistent Camera Presentation selection '{definition.name}' could not materialize in the Session scope. {materialized.Issue}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                CameraPresentationMaterializationHandle handle =
                    materialized.Handle;
                created.Add(handle);
                replacedCurrents.Add(current);

                if (!CameraCinemachineOutputChannelIsolation
                        .TryConfigurePresentation(
                            output,
                            handle.RigComposer,
                            out string channelIssue))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' persistent Camera Presentation selection '{definition.name}' could not inherit its Session Output channel. {channelIssue}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                if (_playerSelection != null &&
                    !_playerSelection.TryAttach(
                        handle,
                        out _,
                        out string selectionIssue))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' persistent Camera Presentation selection '{definition.name}' Player Subject selection attachment failed. {selectionIssue}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                try
                {
                    handle.PresentationRuntime.AttachOutputSession(output);
                    handle.PresentationRuntime
                        .AttachCameraSubjectAvailability(_availability);
                    handle.PresentationRuntime.SetEnabled(true);
                }
                catch (Exception exception)
                {
                    issue =
                        $"{ownerKind} '{ownerName}' persistent Camera Presentation selection '{definition.name}' activation failed. {exception.Message}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                if (!handle.PresentationRuntime.IsRequestPublished ||
                    !output.Context.Contains(
                        handle.PresentationRuntime.RequestId))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' persistent Camera Presentation selection '{definition.name}' was not admitted on Output '{outputId}'.";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                if (current == null)
                {
                    if (!output.Context.HasWinner ||
                        output.Context.Winner.RequestId !=
                            handle.PresentationRuntime.RequestId)
                    {
                        issue =
                            $"{ownerKind} '{ownerName}' persistent Camera Presentation selection '{definition.name}' did not become the normal winner for Output '{outputId}'.";
                        RollbackCreated(created, source, reason);
                        return false;
                    }
                }
                else if (!output.Context.HasWinner ||
                         current.IsReleased ||
                         !current.PresentationRuntime.IsRequestPublished ||
                         (output.Context.Winner.RequestId !=
                              handle.PresentationRuntime.RequestId &&
                          output.Context.Winner.RequestId !=
                              current.PresentationRuntime.RequestId))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' replacement '{definition.name}' interrupted the normal winner for Output '{outputId}'.";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                _logger.Debug(
                    $"{ownerKind} Camera Presentation selection materialized.",
                    LogFields.Field("owner", ownerName),
                    LogFields.Field("presentation", definition.name),
                    LogFields.Field(
                        "presentationId",
                        definition.PresentationId.Value),
                    LogFields.Field("output", outputId.Value),
                    LogFields.Field(
                        "requestId",
                        handle.PresentationRuntime.RequestId.Value),
                    LogFields.Field(
                        "runtimeScope",
                        handle.ScopeContext.Scope));
            }

            if (created.Count == 0)
            {
                return true;
            }

            var entry = new PendingSelectionEntry();
            for (int index = 0; index < created.Count; index++)
            {
                CameraPresentationMaterializationHandle handle =
                    created[index];
                CameraPresentationMaterializationHandle current =
                    replacedCurrents[index];
                CameraOutputId outputId =
                    handle.Definition.OutputDefinition.OutputId;
                if (current == null)
                {
                    _selectedByOutput.Add(outputId, handle);
                }
                else
                {
                    entry.Replacements.Add(
                        new PendingSelectionReplacement(
                            current,
                            handle));
                }

                entry.Handles.Add(handle);
                _pendingOutputOwners[outputId] = owner;
            }

            _pendingSelectionsByOwner[owner] = entry;
            return true;
        }

        private static void ResolvePersistentSelectionOwnerDiagnostic(
            Object owner,
            out string ownerKind,
            out string ownerName)
        {
            if (owner is RouteAsset route)
            {
                ownerKind = "Route";
                ownerName = route.RouteName;
                return;
            }

            if (owner is ActivityAsset activity)
            {
                ownerKind = "Activity";
                ownerName = activity.ActivityName;
                return;
            }

            ownerKind = owner != null ? owner.GetType().Name : "Owner";
            ownerName = owner != null ? owner.name : "<missing>";
        }

        private bool TryEnter(
            Object owner,
            string ownerKind,
            string ownerName,
            IReadOnlyList<CameraPresentationDefinition> definitions,
            RuntimeScopeContext context,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (_disposed)
            {
                issue = "Camera Presentation lifecycle is already disposed.";
                return false;
            }

            if (owner == null)
            {
                issue = $"{ownerKind} Camera Presentation entry requires an exact authored owner.";
                return false;
            }

            if (definitions == null || definitions.Count == 0)
            {
                return true;
            }

            if (!context.IsValid)
            {
                issue =
                    $"{ownerKind} Camera Presentations require the active RuntimeContent scope context.";
                return false;
            }

            if (_activeByOwner.ContainsKey(owner))
            {
                issue =
                    $"{ownerKind} '{ownerName}' already owns materialized Camera Presentations for the current occurrence.";
                return false;
            }

            if (!TryValidateDefinitions(
                    ownerKind,
                    ownerName,
                    definitions,
                    out issue))
            {
                return false;
            }

            var created =
                new List<CameraPresentationMaterializationHandle>(
                    definitions.Count);

            for (int index = 0; index < definitions.Count; index++)
            {
                CameraPresentationDefinition definition =
                    definitions[index];

                if (!_outputTopology.TryGetOutput(
                        definition.OutputDefinition.OutputId,
                        out CameraOutputAuthoring output,
                        out string outputIssue))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation '{definition.name}' targets an unavailable Session Output. {outputIssue}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                CameraPresentationMaterializationResult materialized =
                    _materializer.Materialize(
                        context,
                        definition,
                        _physicalParent,
                        source,
                        reason);
                if (!materialized.Succeeded ||
                    materialized.Handle == null)
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation '{definition.name}' materialization failed. {materialized.Issue}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                CameraPresentationMaterializationHandle handle =
                    materialized.Handle;
                created.Add(handle);

                if (!CameraCinemachineOutputChannelIsolation
                        .TryConfigurePresentation(
                            output,
                            handle.RigComposer,
                            out string channelIssue))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation '{definition.name}' could not inherit its target Output's Cinemachine channel. {channelIssue}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                if (_playerSelection != null &&
                    !_playerSelection.TryAttach(
                        handle,
                        out _,
                        out string selectionIssue))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation '{definition.name}' Player Subject selection attachment failed. {selectionIssue}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                try
                {
                    handle.PresentationRuntime.AttachOutputSession(output);
                    handle.PresentationRuntime
                        .AttachCameraSubjectAvailability(_availability);
                    handle.PresentationRuntime.SetEnabled(true);
                }
                catch (Exception exception)
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation '{definition.name}' activation failed. {exception.Message}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                _logger.Debug(
                    $"{ownerKind} Camera Presentation materialized.",
                    LogFields.Field("owner", ownerName),
                    LogFields.Field("presentation", definition.name),
                    LogFields.Field(
                        "presentationId",
                        definition.PresentationId.Value),
                    LogFields.Field(
                        "output",
                        definition.OutputDefinition.OutputId.Value),
                    LogFields.Field(
                        "cinemachineOutputChannel",
                        output.CinemachineBrain.ChannelMask),
                    LogFields.Field(
                        "transitionMode",
                        definition.TransitionMode),
                    LogFields.Field(
                        "requestPrecedence",
                        definition.RequestPrecedence),
                    LogFields.Field(
                        "runtimeScope",
                        context.Scope));
            }

            _activeByOwner.Add(owner, created);
            return true;
        }

        private bool TryExit(
            Object owner,
            string ownerKind,
            string ownerName,
            string source,
            string reason,
            bool terminalShutdown,
            out string issue)
        {
            issue = string.Empty;
            if (owner == null)
            {
                return true;
            }

            if (!_activeByOwner.TryGetValue(
                    owner,
                    out List<CameraPresentationMaterializationHandle> handles))
            {
                return true;
            }

            for (int index = handles.Count - 1; index >= 0; index--)
            {
                CameraPresentationMaterializationHandle handle =
                    handles[index];
                CameraPresentationMaterializationResult released =
                    terminalShutdown
                        ? _materializer.ReleaseTerminal(
                            handle,
                            source,
                            reason)
                        : _materializer.Release(
                            handle,
                            source,
                            reason);
                if (!released.Succeeded)
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation release failed. presentation='{handle.Definition?.name ?? "<unknown>"}' issue='{released.Issue}'.";
                    _logger.Warning(
                        $"{ownerKind} Camera Presentation release failed.",
                        LogFields.Field("owner", ownerName),
                        LogFields.Field(
                            "presentation",
                            handle.Definition != null
                                ? handle.Definition.name
                                : "<unknown>"),
                        LogFields.Field("issue", released.Issue));
                    return false;
                }

                _playerSelection?.ForgetReleased(handle);

                _logger.Debug(
                    $"{ownerKind} Camera Presentation released.",
                    LogFields.Field("owner", ownerName),
                    LogFields.Field(
                        "presentation",
                        handle.Definition != null
                            ? handle.Definition.name
                            : "<unknown>"));
            }

            _activeByOwner.Remove(owner);
            return true;
        }

        private static bool TryValidateDefinitions(
            string ownerKind,
            string ownerName,
            IReadOnlyList<CameraPresentationDefinition> definitions,
            out string issue)
        {
            var definitionsSeen =
                new HashSet<CameraPresentationDefinition>();
            var idsSeen =
                new HashSet<CameraPresentationId>();

            for (int index = 0; index < definitions.Count; index++)
            {
                CameraPresentationDefinition definition =
                    definitions[index];
                if (definition == null)
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentations[{index}] is missing.";
                    return false;
                }

                if (!definitionsSeen.Add(definition))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' repeats Camera Presentation definition '{definition.name}'.";
                    return false;
                }

                if (!definition.TryValidate(
                        out string definitionIssue))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation '{definition.name}' is invalid. {definitionIssue}";
                    return false;
                }

                if (!idsSeen.Add(definition.PresentationId))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' contains duplicate CameraPresentationId '{definition.PresentationId}'.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private void RollbackCreated(
            List<CameraPresentationMaterializationHandle> created,
            string source,
            string reason)
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                CameraPresentationMaterializationHandle handle =
                    created[index];
                CameraPresentationMaterializationResult released =
                    _materializer.Release(
                        handle,
                        source,
                        reason);
                if (released.Succeeded)
                {
                    _playerSelection?.ForgetReleased(handle);
                }
            }
        }

        public void Dispose()
        {
            DisposeCore(false);
        }

        internal void DisposeForApplicationQuit()
        {
            DisposeCore(true);
        }

        private void DisposeCore(bool terminalShutdown)
        {
            if (_disposed)
            {
                return;
            }

            Object[] owners =
                new Object[_activeByOwner.Count];
            _activeByOwner.Keys.CopyTo(owners, 0);
            for (int index = owners.Length - 1; index >= 0; index--)
            {
                TryExit(
                    owners[index],
                    "Lifecycle",
                    owners[index] != null
                        ? owners[index].name
                        : string.Empty,
                    nameof(CameraPresentationLifecycleRuntime),
                    terminalShutdown
                        ? "camera-presentation-application-quit"
                        : "camera-presentation-lifecycle-dispose",
                    terminalShutdown,
                    out _);
            }

            CameraOutputId[] selectedOutputs =
                new CameraOutputId[_selectedByOutput.Count];
            _selectedByOutput.Keys.CopyTo(selectedOutputs, 0);
            for (int index = selectedOutputs.Length - 1;
                 index >= 0;
                 index--)
            {
                CameraOutputId outputId = selectedOutputs[index];
                CameraPresentationMaterializationHandle handle =
                    _selectedByOutput[outputId];
                CameraPresentationMaterializationResult released =
                    terminalShutdown
                        ? _materializer.ReleaseTerminal(
                            handle,
                            nameof(CameraPresentationLifecycleRuntime),
                            "camera-presentation-selection-application-quit")
                        : _materializer.Release(
                            handle,
                            nameof(CameraPresentationLifecycleRuntime),
                            "camera-presentation-selection-lifecycle-dispose");
                if (released.Succeeded)
                {
                    _playerSelection?.ForgetReleased(handle);
                    _selectedByOutput.Remove(outputId);
                }
            }

            Object[] pendingOwners =
                new Object[_pendingSelectionsByOwner.Count];
            _pendingSelectionsByOwner.Keys.CopyTo(pendingOwners, 0);
            for (int index = pendingOwners.Length - 1; index >= 0; index--)
            {
                ReleasePendingSelections(
                    _pendingSelectionsByOwner[pendingOwners[index]],
                    terminalShutdown,
                    nameof(CameraPresentationLifecycleRuntime),
                    terminalShutdown
                        ? "camera-presentation-selection-application-quit"
                        : "camera-presentation-selection-lifecycle-dispose");
            }

            _pendingSelectionsByOwner.Clear();
            _pendingOutputOwners.Clear();

            _disposed = true;
        }

        private bool TryCommitPendingReplacements(
            PendingSelectionEntry entry,
            out string issue)
        {
            for (int index = 0;
                 index < entry.Replacements.Count;
                 index++)
            {
                PendingSelectionReplacement replacement =
                    entry.Replacements[index];
                if (!TryCommitReplacement(entry, replacement, out issue))
                {
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool TryCommitReplacement(
            PendingSelectionEntry entry,
            PendingSelectionReplacement replacement,
            out string issue)
        {
            CameraPresentationMaterializationHandle current =
                replacement.Current;
            CameraPresentationMaterializationHandle incoming =
                replacement.Incoming;
            if (current == null ||
                incoming == null ||
                current.IsReleased ||
                incoming.IsReleased ||
                incoming.Definition == null ||
                incoming.Definition.OutputDefinition == null)
            {
                issue =
                    "Camera Presentation replacement requires the current and incoming Session occurrences.";
                return false;
            }

            CameraOutputId outputId =
                incoming.Definition.OutputDefinition.OutputId;
            if (!_outputTopology.TryGetOutput(
                    outputId,
                    out CameraOutputAuthoring output,
                    out string outputIssue))
            {
                issue = outputIssue;
                return false;
            }

            if (!incoming.PresentationRuntime.IsRequestPublished ||
                !output.Context.Contains(
                    incoming.PresentationRuntime.RequestId) ||
                !output.Context.HasWinner)
            {
                issue =
                    $"Incoming Camera Presentation '{incoming.Definition.name}' is not an admitted normal candidate for Output '{outputId}'.";
                return false;
            }

            try
            {
                current.PresentationRuntime.SetEnabled(false);
            }
            catch (Exception exception)
            {
                RestoreCurrentSelectionRequest(current);
                issue =
                    $"Current Camera Presentation request could not be withdrawn. {exception.Message}";
                return false;
            }

            if (!output.Context.HasWinner ||
                output.Context.Winner.RequestId !=
                    incoming.PresentationRuntime.RequestId)
            {
                RestoreCurrentSelectionRequest(current);
                issue =
                    $"Incoming Camera Presentation '{incoming.Definition.name}' did not become the normal winner for Output '{outputId}'.";
                return false;
            }

            // Commit point: the incoming Presentation is the normal winner.
            // The current one is no longer a candidate and may now be released.
            _selectedByOutput[outputId] = incoming;
            entry.Handles.Remove(incoming);

            CameraPresentationMaterializationResult released =
                _materializer.Release(
                    current,
                    nameof(CameraPresentationLifecycleRuntime),
                    "camera-persistent-selection-replaced");
            if (!released.Succeeded)
            {
                issue =
                    $"Committed Camera Presentation replacement could not release the previous occurrence. {released.Issue}";
                return false;
            }

            _playerSelection?.ForgetReleased(current);
            issue = string.Empty;
            return true;
        }

        private static void RestoreCurrentSelectionRequest(
            CameraPresentationMaterializationHandle current)
        {
            if (current == null || current.IsReleased)
            {
                return;
            }

            current.PresentationRuntime.SetEnabled(true);
        }

        private bool RollbackPendingSelection(
            Object owner,
            PendingSelectionEntry entry,
            string source,
            string reason)
        {
            if (!ReleasePendingSelections(entry, false, source, reason))
            {
                return false;
            }

            entry.Replacements.Clear();
            ClearPendingOutputOwners(owner);
            return true;
        }

        private bool ReleasePendingSelections(
            PendingSelectionEntry entry,
            bool terminalShutdown,
            string source,
            string reason)
        {
            for (int index = entry.Handles.Count - 1;
                 index >= 0;
                 index--)
            {
                CameraPresentationMaterializationHandle handle =
                    entry.Handles[index];
                if (handle == null || handle.IsReleased)
                {
                    entry.Handles.RemoveAt(index);
                    continue;
                }

                CameraPresentationMaterializationResult released =
                    terminalShutdown
                        ? _materializer.ReleaseTerminal(
                            handle,
                            source,
                            reason)
                        : _materializer.Release(
                            handle,
                            source,
                            reason);
                if (!released.Succeeded)
                {
                    return false;
                }

                _playerSelection?.ForgetReleased(handle);
                if (handle.Definition != null &&
                    handle.Definition.OutputDefinition != null)
                {
                    CameraOutputId outputId =
                        handle.Definition.OutputDefinition.OutputId;
                    if (_selectedByOutput.TryGetValue(
                            outputId,
                            out CameraPresentationMaterializationHandle selected) &&
                        ReferenceEquals(selected, handle))
                    {
                        _selectedByOutput.Remove(outputId);
                    }
                }

                entry.Handles.RemoveAt(index);
            }

            return true;
        }

        private void ClearPendingOutputOwners(Object owner)
        {
            List<CameraOutputId> toRemove = null;
            foreach (KeyValuePair<CameraOutputId, Object> pair in _pendingOutputOwners)
            {
                if (ReferenceEquals(pair.Value, owner))
                {
                    (toRemove ??= new List<CameraOutputId>()).Add(pair.Key);
                }
            }

            if (toRemove == null)
            {
                return;
            }

            for (int index = 0; index < toRemove.Count; index++)
            {
                _pendingOutputOwners.Remove(toRemove[index]);
            }
        }

        private sealed class PendingSelectionEntry
        {
            internal readonly List<CameraPresentationMaterializationHandle>
                Handles = new List<CameraPresentationMaterializationHandle>();

            internal readonly List<PendingSelectionReplacement> Replacements =
                new List<PendingSelectionReplacement>();
        }

        private readonly struct PendingSelectionReplacement
        {
            internal PendingSelectionReplacement(
                CameraPresentationMaterializationHandle current,
                CameraPresentationMaterializationHandle incoming)
            {
                Current = current;
                Incoming = incoming;
            }

            internal CameraPresentationMaterializationHandle Current { get; }

            internal CameraPresentationMaterializationHandle Incoming { get; }
        }
    }
}
