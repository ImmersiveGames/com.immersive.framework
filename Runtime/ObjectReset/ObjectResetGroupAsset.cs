using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Reusable list of logical Object Reset targets.
    /// It stores target ids/policies only; it does not discover, own or reset Unity objects by itself.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Object Reset Group",
        menuName = "Immersive Framework/Object Reset/Object Reset Group")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F39A reusable Object Reset Group authoring asset.")]
    public sealed class ObjectResetGroupAsset : ScriptableObject
    {
        private const string DefaultGroupId = "object-reset-group";
        private const string DefaultReason = "Object Reset Group";

        [Header("Object Reset Group")]
        [SerializeField] private string groupId = DefaultGroupId;
        [SerializeField] private string reason = DefaultReason;
        [SerializeField] private bool allowNoParticipants = true;
        [SerializeField] private bool stopOnFailure = true;

        [Header("Targets")]
        [SerializeField] private List<ObjectResetGroupEntry> entries = new List<ObjectResetGroupEntry>();

        public string GroupId => groupId.NormalizeTextOrFallback(DefaultGroupId);

        public string Reason => reason.NormalizeTextOrFallback(DefaultReason);

        public bool AllowNoParticipants => allowNoParticipants;

        public bool StopOnFailure => stopOnFailure;

        public IReadOnlyList<ObjectResetGroupEntry> Entries => entries != null ? (IReadOnlyList<ObjectResetGroupEntry>)entries : Array.Empty<ObjectResetGroupEntry>();

        public int EntryCount => Entries.Count;
    }
}
