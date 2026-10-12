using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "IF-ADR-046 I3 trigger-to-canonical Join port.")]
    internal interface IPlayerSessionJoinPort : IDisposable
    {
        LocalPlayerJoinOutcome RequestJoin(LocalPlayerJoinRequest request);
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "IF-ADR-046 I3 canonical Manager-Provisioned Join adapter.")]
    internal sealed class ManagerPlayerSessionJoinPort : IPlayerSessionJoinPort
    {
        private readonly PlayerSessionJoinBindingLease _lease;
        private bool _disposed;

        internal ManagerPlayerSessionJoinPort(PlayerSessionJoinBindingLease lease)
        {
            _lease = lease ?? throw new ArgumentNullException(nameof(lease));
        }

        public LocalPlayerJoinOutcome RequestJoin(LocalPlayerJoinRequest request)
        {
            if (_disposed)
                return LocalPlayerJoinOutcomeProjection.RejectedUnavailable();

            try
            {
                LocalPlayerProvisioningRuntimeHostModule manager = _lease.Manager;
                if (manager == null || !manager.IsReady)
                    return LocalPlayerJoinOutcomeProjection.RejectedUnavailable();

                PlayerSessionJoinBindingLeaseValidation validation =
                    _lease.ValidateCurrent(out _);
                if (validation == PlayerSessionJoinBindingLeaseValidation.RejectedConfiguration)
                    return LocalPlayerJoinOutcomeProjection.RejectedConfiguration();
                if (validation != PlayerSessionJoinBindingLeaseValidation.Current)
                    return LocalPlayerJoinOutcomeProjection.RejectedUnavailable();

                LocalPlayerJoinResult result = manager.RegisterJoinWithActorPreparation(
                    manager.TryJoin(request));
                return LocalPlayerJoinOutcomeProjection.Project(result);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return LocalPlayerJoinOutcomeProjection.FailedUnknown();
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
