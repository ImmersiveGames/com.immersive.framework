using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.ObjectReset;
using UnityEngine;

namespace Immersive.Framework.RuntimeObjects
{
    /// <summary>
    /// API status: Internal. Immutable registry record for one runtime object participating in ObjectEntry and ObjectReset flows.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F44 internal runtime object participation registry record.")]
    internal sealed class RuntimeObjectParticipationRecord
    {
        private static readonly IReadOnlyList<IObjectResetParticipant> EmptyParticipants = Array.Empty<IObjectResetParticipant>();

        internal RuntimeObjectParticipationRecord(
            RuntimeObjectParticipationHandle handle,
            ObjectEntryDescriptor descriptor,
            IReadOnlyList<IObjectResetParticipant> resetParticipants,
            UnityEngine.Object owner,
            string source,
            string reason)
        {
            if (!handle.IsValid)
            {
                throw new ArgumentException("Runtime object participation record requires a valid handle.", nameof(handle));
            }

            if (!descriptor.Id.IsValid)
            {
                throw new ArgumentException("Runtime object participation record requires a valid ObjectEntry descriptor.", nameof(descriptor));
            }

            Handle = handle;
            Descriptor = descriptor;
            ResetParticipants = resetParticipants ?? EmptyParticipants;
            Owner = owner;
            Source = Normalize(source, nameof(RuntimeObjectParticipationRecord));
            Reason = Normalize(reason, "runtime-object.participation");
        }

        internal RuntimeObjectParticipationHandle Handle { get; }

        internal ObjectEntryDescriptor Descriptor { get; }

        internal IReadOnlyList<IObjectResetParticipant> ResetParticipants { get; }

        internal UnityEngine.Object Owner { get; }

        internal string Source { get; }

        internal string Reason { get; }

        internal bool HasLiveOwner => Owner != null;

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
