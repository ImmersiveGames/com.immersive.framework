using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Validator for generic Actor declarations.
    /// It produces diagnostics only; it does not own actors, input, spawning, movement, reset, snapshot or save behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45A generic actor declaration validator.")]
    public static class ActorValidator
    {
        public static ActorSet ValidateDeclarations(
            IEnumerable<ActorDeclaration> actorDeclarations,
            IEnumerable<PlayerActorDeclaration> playerActorDeclarations,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(ActorValidator));
            var descriptors = new List<ActorDescriptor>();
            var issues = new List<ActorSetIssue>();

            AppendActorDeclarations(actorDeclarations, normalizedSource, descriptors, issues);
            AppendPlayerActorDeclarations(playerActorDeclarations, normalizedSource, descriptors, issues);

            return ActorSet.FromDescriptors(descriptors, issues, normalizedSource, reason);
        }

        public static ActorSet ValidateActors(
            IEnumerable<IActor> actors,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(ActorValidator));
            var descriptors = new List<ActorDescriptor>();
            var issues = new List<ActorSetIssue>();

            if (actors != null)
            {
                foreach (IActor actor in actors)
                {
                    if (actor == null)
                    {
                        issues.Add(ActorSetIssue.BlockingIssue(
                            ActorSetIssueKind.InvalidDeclaration,
                            string.Empty,
                            normalizedSource,
                            "Actor reference is null."));
                        continue;
                    }

                    try
                    {
                        MonoBehaviour behaviour = actor as MonoBehaviour;
                        descriptors.Add(new ActorDescriptor(
                            actor.ActorId,
                            actor.ActorKind,
                            actor.ActorRole,
                            actor.ActorDisplayName,
                            behaviour != null && behaviour.gameObject.scene.IsValid() ? behaviour.gameObject.scene.name : string.Empty,
                            behaviour != null ? behaviour.gameObject.name : string.Empty,
                            normalizedSource,
                            string.Empty));
                    }
                    catch (ArgumentOutOfRangeException exception)
                    {
                        issues.Add(ActorSetIssue.BlockingIssue(
                            ActorSetIssueKind.InvalidDeclaration,
                            string.Empty,
                            normalizedSource,
                            exception.Message));
                    }
                    catch (ArgumentException exception)
                    {
                        issues.Add(ActorSetIssue.BlockingIssue(
                            ActorSetIssueKind.InvalidActorId,
                            string.Empty,
                            normalizedSource,
                            exception.Message));
                    }
                    catch (Exception exception)
                    {
                        issues.Add(ActorSetIssue.BlockingIssue(
                            ActorSetIssueKind.InvalidDeclaration,
                            string.Empty,
                            normalizedSource,
                            exception.Message));
                    }
                }
            }

            return ActorSet.FromDescriptors(descriptors, issues, normalizedSource, reason);
        }

        private static void AppendActorDeclarations(
            IEnumerable<ActorDeclaration> actorDeclarations,
            string normalizedSource,
            List<ActorDescriptor> descriptors,
            List<ActorSetIssue> issues)
        {
            if (actorDeclarations == null)
            {
                return;
            }

            foreach (ActorDeclaration declaration in actorDeclarations)
            {
                if (declaration == null)
                {
                    issues.Add(ActorSetIssue.BlockingIssue(
                        ActorSetIssueKind.InvalidDeclaration,
                        string.Empty,
                        normalizedSource,
                        "Actor declaration reference is null."));
                    continue;
                }

                if (declaration.TryCreateDescriptor(normalizedSource, out ActorDescriptor descriptor, out ActorSetIssue issue))
                {
                    descriptors.Add(descriptor);
                    continue;
                }

                issues.Add(issue);
            }
        }

        private static void AppendPlayerActorDeclarations(
            IEnumerable<PlayerActorDeclaration> playerActorDeclarations,
            string normalizedSource,
            List<ActorDescriptor> descriptors,
            List<ActorSetIssue> issues)
        {
            if (playerActorDeclarations == null)
            {
                return;
            }

            foreach (PlayerActorDeclaration declaration in playerActorDeclarations)
            {
                if (declaration == null)
                {
                    issues.Add(ActorSetIssue.BlockingIssue(
                        ActorSetIssueKind.InvalidDeclaration,
                        string.Empty,
                        normalizedSource,
                        "PlayerActor declaration reference is null."));
                    continue;
                }

                if (declaration.TryCreateDescriptor(normalizedSource, out PlayerActorDescriptor playerDescriptor, out PlayerActorSetIssue issue))
                {
                    descriptors.Add(playerDescriptor.ToActorDescriptor());
                    continue;
                }

                issues.Add(ConvertPlayerActorIssue(issue, normalizedSource));
            }
        }

        private static ActorSetIssue ConvertPlayerActorIssue(PlayerActorSetIssue issue, string normalizedSource)
        {
            ActorSetIssueKind kind = issue.Kind == PlayerActorSetIssueKind.InvalidActorId
                ? ActorSetIssueKind.InvalidActorId
                : ActorSetIssueKind.InvalidDeclaration;

            if (issue.Blocking)
            {
                return ActorSetIssue.BlockingIssue(kind, issue.ActorIdText, normalizedSource, issue.Message);
            }

            return ActorSetIssue.NonBlockingIssue(kind, issue.ActorIdText, normalizedSource, issue.Message);
        }
    }
}
