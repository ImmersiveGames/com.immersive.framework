using System;
using Immersive.Framework.ContentAnchor;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    internal enum ContentAnchorAuthoringValidationStatus
    {
        Valid = 0,
        MissingComponent = 10,
        MissingOwner = 20,
        InvalidOwnerIdentity = 30,
        MissingAnchorId = 40,
        InvalidKind = 50,
        InvalidRequiredness = 60,
        InvalidDeclaration = 70
    }

    /// <summary>
    /// Editor-only, non-mutating authoring validation for passive Content Anchors.
    /// It does not query runtime discovery, registration, materialization or binding.
    /// </summary>
    internal readonly struct ContentAnchorAuthoringValidationResult
    {
        internal ContentAnchorAuthoringValidationResult(
            ContentAnchorAuthoringValidationStatus status,
            bool isValid,
            string message,
            string impact,
            string correctiveAction)
        {
            Status = status;
            IsValid = isValid;
            Message = message ?? string.Empty;
            Impact = impact ?? string.Empty;
            CorrectiveAction = correctiveAction ?? string.Empty;
        }

        internal ContentAnchorAuthoringValidationStatus Status { get; }

        internal bool IsValid { get; }

        internal string Message { get; }

        internal string Impact { get; }

        internal string CorrectiveAction { get; }
    }

    /// <summary>
    /// Shared validation used by RouteContentAnchorEditor and
    /// ActivityContentAnchorEditor.
    /// </summary>
    internal static class ContentAnchorAuthoringValidator
    {
        internal static ContentAnchorAuthoringValidationResult Validate(
            RouteContentAnchor anchor)
        {
            if (anchor == null)
            {
                return Invalid(
                    ContentAnchorAuthoringValidationStatus.MissingComponent,
                    "Route Content Anchor is missing.",
                    "No passive Route anchor declaration can be authored.",
                    "Add RouteContentAnchor to the intended scene object.");
            }

            if (anchor.Route == null)
            {
                return Invalid(
                    ContentAnchorAuthoringValidationStatus.MissingOwner,
                    "Owner Route is missing.",
                    "Route-scoped discovery cannot associate this declaration with an owner.",
                    "Assign the RouteAsset that owns this anchor.");
            }

            if (!anchor.Route.HasValidRouteId)
            {
                return Invalid(
                    ContentAnchorAuthoringValidationStatus.InvalidOwnerIdentity,
                    "Owner Route has no valid Route ID.",
                    "The anchor cannot create a canonical owner key.",
                    "Open the Route asset and generate or repair its Route ID.");
            }

            return ValidateShared(
                anchor.HasExplicitAnchorId,
                anchor.AnchorIdText,
                anchor.Kind,
                anchor.Requiredness,
                anchor.TryCreateDeclaration,
                "Route");
        }

        internal static ContentAnchorAuthoringValidationResult Validate(
            ActivityContentAnchor anchor)
        {
            if (anchor == null)
            {
                return Invalid(
                    ContentAnchorAuthoringValidationStatus.MissingComponent,
                    "Activity Content Anchor is missing.",
                    "No passive Activity anchor declaration can be authored.",
                    "Add ActivityContentAnchor to the intended scene object.");
            }

            if (anchor.Activity == null)
            {
                return Invalid(
                    ContentAnchorAuthoringValidationStatus.MissingOwner,
                    "Owner Activity is missing.",
                    "Activity-scoped discovery cannot associate this declaration with an owner.",
                    "Assign the ActivityAsset that owns this anchor.");
            }

            if (!anchor.Activity.HasValidActivityId)
            {
                return Invalid(
                    ContentAnchorAuthoringValidationStatus.InvalidOwnerIdentity,
                    "Owner Activity has no valid Activity ID.",
                    "The anchor cannot create a canonical owner key.",
                    "Open the Activity asset and generate or repair its Activity ID.");
            }

            return ValidateShared(
                anchor.HasExplicitAnchorId,
                anchor.AnchorIdText,
                anchor.Kind,
                anchor.Requiredness,
                anchor.TryCreateDeclaration,
                "Activity");
        }

        private static ContentAnchorAuthoringValidationResult ValidateShared(
            bool hasExplicitAnchorId,
            string anchorIdText,
            ContentAnchorKind kind,
            ContentAnchorRequiredness requiredness,
            TryCreateDeclaration tryCreateDeclaration,
            string ownerKind)
        {
            if (!hasExplicitAnchorId)
            {
                return Invalid(
                    ContentAnchorAuthoringValidationStatus.MissingAnchorId,
                    "Anchor ID is missing.",
                    "The declaration has no stable functional identity and will be rejected.",
                    "Enter an explicit ID or use the suggested identity action.");
            }

            if (!Enum.IsDefined(typeof(ContentAnchorKind), kind) ||
                kind == ContentAnchorKind.Unknown)
            {
                return Invalid(
                    ContentAnchorAuthoringValidationStatus.InvalidKind,
                    "Anchor Kind is not valid.",
                    "The framework cannot determine whether this is a Root, Slot or Point.",
                    "Select Root, Slot or Point.");
            }

            if (!Enum.IsDefined(
                    typeof(ContentAnchorRequiredness),
                    requiredness))
            {
                return Invalid(
                    ContentAnchorAuthoringValidationStatus.InvalidRequiredness,
                    "Anchor Requiredness is not valid.",
                    "Authoring diagnostics cannot classify the declaration as Optional or Required.",
                    "Select Optional or Required.");
            }

            try
            {
                if (!tryCreateDeclaration(out ContentAnchorDeclaration declaration))
                {
                    return Invalid(
                        ContentAnchorAuthoringValidationStatus.InvalidDeclaration,
                        $"{ownerKind} Content Anchor could not create a declaration.",
                        "The authored fields do not satisfy the current Content Anchor contract.",
                        "Review owner, Anchor ID, Kind and Requiredness.");
                }

                return new ContentAnchorAuthoringValidationResult(
                    ContentAnchorAuthoringValidationStatus.Valid,
                    true,
                    $"Ready. Declares '{declaration.AnchorId.StableText}' as " +
                    $"{declaration.Kind} / {declaration.Requiredness}.",
                    "None. This is a structurally valid passive declaration.",
                    "No corrective action is required.");
            }
            catch (Exception exception)
            {
                return Invalid(
                    ContentAnchorAuthoringValidationStatus.InvalidDeclaration,
                    $"{ownerKind} Content Anchor declaration is invalid. " +
                    exception.Message,
                    $"Anchor ID '{anchorIdText}' or its owner identity cannot produce a canonical declaration.",
                    "Review the explicit owner and Anchor ID.");
            }
        }

        private static ContentAnchorAuthoringValidationResult Invalid(
            ContentAnchorAuthoringValidationStatus status,
            string message,
            string impact,
            string correctiveAction)
        {
            return new ContentAnchorAuthoringValidationResult(
                status,
                false,
                message,
                impact,
                correctiveAction);
        }

        private delegate bool TryCreateDeclaration(
            out ContentAnchorDeclaration declaration);
    }
}
