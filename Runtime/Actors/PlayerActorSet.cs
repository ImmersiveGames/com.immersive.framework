using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Passive validation set for framework-recognized PlayerActor declarations.
    /// It does not own actor lifecycle, input behavior, movement or materialization.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31A PlayerActor validation set.")]
    public sealed class PlayerActorSet
    {
        private readonly PlayerActorDescriptor[] _descriptors;
        private readonly PlayerActorSetIssue[] _issues;

        private PlayerActorSet(PlayerActorDescriptor[] descriptors, PlayerActorSetIssue[] issues, string source, string reason)
        {
            _descriptors = descriptors ?? Array.Empty<PlayerActorDescriptor>();
            _issues = issues ?? Array.Empty<PlayerActorSetIssue>();
            Source = source.NormalizeTextOrFallback(nameof(PlayerActorSet));
            Reason = reason.NormalizeText();
        }

        public IReadOnlyList<PlayerActorDescriptor> Descriptors => _descriptors;

        public IReadOnlyList<PlayerActorSetIssue> Issues => _issues;

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

        public int PlayerInputEvidenceCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _descriptors.Length; i++)
                {
                    if (_descriptors[i].HasPlayerInputEvidence)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool Succeeded => BlockingIssueCount == 0;

        public bool Failed => !Succeeded;

        public bool SwitchesActionMaps => false;

        public bool AppliesInputBehavior => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("playerActors='").Append(Count).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" playerInputEvidence='").Append(PlayerInputEvidenceCount).Append("'");
            builder.Append(" actionMapSwitching='").Append(SwitchesActionMaps).Append("'");
            builder.Append(" inputBehavior='").Append(AppliesInputBehavior).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            for (int i = 0; i < _issues.Length; i++)
            {
                builder.Append(" issue[").Append(i).Append("]='").Append(_issues[i]).Append("'");
            }

            return builder.ToString();
        }

        public static PlayerActorSet FromDescriptors(
            IEnumerable<PlayerActorDescriptor> descriptors,
            string source,
            string reason)
        {
            return FromDescriptors(descriptors, null, source, reason);
        }

        internal static PlayerActorSet FromDescriptors(
            IEnumerable<PlayerActorDescriptor> descriptors,
            IEnumerable<PlayerActorSetIssue> existingIssues,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerActorSet));
            var descriptorList = new List<PlayerActorDescriptor>();
            var issues = new List<PlayerActorSetIssue>();
            var ids = new HashSet<ActorId>();

            if (existingIssues != null)
            {
                issues.AddRange(existingIssues);
            }

            if (descriptors != null)
            {
                foreach (PlayerActorDescriptor descriptor in descriptors)
                {
                    descriptorList.Add(descriptor);

                    if (!descriptor.HasPlayerInputEvidence)
                    {
                        issues.Add(PlayerActorSetIssue.BlockingIssue(
                            PlayerActorSetIssueKind.MissingRequiredPlayerInputEvidence,
                            descriptor.ActorId.StableText,
                            normalizedSource,
                            "PlayerActor requires evidence of Unity's PlayerInput component."));
                    }

                    if (!ids.Add(descriptor.ActorId))
                    {
                        issues.Add(PlayerActorSetIssue.BlockingIssue(
                            PlayerActorSetIssueKind.DuplicatePlayerActorId,
                            descriptor.ActorId.StableText,
                            normalizedSource,
                            "PlayerActor id must be unique in the current validation scope."));
                    }
                }
            }

            return new PlayerActorSet(descriptorList.ToArray(), issues.ToArray(), normalizedSource, reason);
        }
    }
}
