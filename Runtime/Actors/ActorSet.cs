using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Passive validation set for framework-recognized actor declarations.
    /// It does not own actor lifecycle, materialization, movement, input, reset, snapshot or save behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45A generic actor validation set.")]
    public sealed class ActorSet
    {
        private readonly ActorDescriptor[] _descriptors;
        private readonly ActorSetIssue[] _issues;

        private ActorSet(ActorDescriptor[] descriptors, ActorSetIssue[] issues, string source, string reason)
        {
            _descriptors = descriptors ?? Array.Empty<ActorDescriptor>();
            _issues = issues ?? Array.Empty<ActorSetIssue>();
            Source = source.NormalizeTextOrFallback(nameof(ActorSet));
            Reason = reason.NormalizeText();
        }

        public IReadOnlyList<ActorDescriptor> Descriptors => _descriptors;

        public IReadOnlyList<ActorSetIssue> Issues => _issues;

        public string Source { get; }

        public string Reason { get; }

        public int Count => _descriptors.Length;

        public int IssueCount => _issues.Length;

        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _issues.Length; i++)
                {
                    if (_issues[i].Blocking)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int PlayerActorCount => CountByKind(ActorKind.Player);

        public int NonPlayerActorCount => CountByKind(ActorKind.NonPlayer);

        public int UnknownKindCount => CountByKind(ActorKind.Unknown);

        public int UnknownRoleCount => CountByRole(ActorRole.Unknown);

        public bool Succeeded => BlockingIssueCount == 0;

        public bool Failed => !Succeeded;

        public bool OwnsLifetime => false;

        public bool AppliesInputBehavior => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("actors='").Append(Count).Append("'");
            builder.Append(" playerActors='").Append(PlayerActorCount).Append("'");
            builder.Append(" nonPlayerActors='").Append(NonPlayerActorCount).Append("'");
            builder.Append(" unknownKind='").Append(UnknownKindCount).Append("'");
            builder.Append(" unknownRole='").Append(UnknownRoleCount).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" lifetimeOwnership='").Append(OwnsLifetime).Append("'");
            builder.Append(" inputBehavior='").Append(AppliesInputBehavior).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            for (int i = 0; i < _issues.Length; i++)
            {
                builder.Append(" issue[").Append(i).Append("]='").Append(_issues[i]).Append("'");
            }

            return builder.ToString();
        }

        public static ActorSet FromDescriptors(
            IEnumerable<ActorDescriptor> descriptors,
            string source,
            string reason)
        {
            return FromDescriptors(descriptors, null, source, reason);
        }

        internal static ActorSet FromDescriptors(
            IEnumerable<ActorDescriptor> descriptors,
            IEnumerable<ActorSetIssue> existingIssues,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(ActorSet));
            var descriptorList = new List<ActorDescriptor>();
            var issues = new List<ActorSetIssue>();
            var ids = new HashSet<ActorId>();

            if (existingIssues != null)
            {
                issues.AddRange(existingIssues);
            }

            if (descriptors != null)
            {
                foreach (ActorDescriptor descriptor in descriptors)
                {
                    descriptorList.Add(descriptor);

                    if (descriptor.ActorKind == ActorKind.Unknown)
                    {
                        issues.Add(ActorSetIssue.BlockingIssue(
                            ActorSetIssueKind.UnknownActorKind,
                            descriptor.ActorId.StableText,
                            normalizedSource,
                            "Actor declaration must use an explicit ActorKind."));
                    }

                    if (descriptor.ActorRole == ActorRole.Unknown)
                    {
                        issues.Add(ActorSetIssue.NonBlockingIssue(
                            ActorSetIssueKind.UnknownActorRole,
                            descriptor.ActorId.StableText,
                            normalizedSource,
                            "Actor declaration uses ActorRole.Unknown. This is allowed during preview but should be made explicit."));
                    }

                    if (!ids.Add(descriptor.ActorId))
                    {
                        issues.Add(ActorSetIssue.BlockingIssue(
                            ActorSetIssueKind.DuplicateActorId,
                            descriptor.ActorId.StableText,
                            normalizedSource,
                            "Actor id must be unique in the current validation scope."));
                    }
                }
            }

            return new ActorSet(descriptorList.ToArray(), issues.ToArray(), normalizedSource, reason);
        }

        private int CountByKind(ActorKind kind)
        {
            int count = 0;
            for (int i = 0; i < _descriptors.Length; i++)
            {
                if (_descriptors[i].ActorKind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountByRole(ActorRole role)
        {
            int count = 0;
            for (int i = 0; i < _descriptors.Length; i++)
            {
                if (_descriptors[i].ActorRole == role)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
