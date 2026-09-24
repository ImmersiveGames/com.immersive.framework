using Immersive.Framework.Reset;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost : IResetRegistrationRuntimePort
    {
        bool IResetRegistrationRuntimePort.TryResolveCurrentResetOwner(
            ResetSubjectScope scope,
            out RuntimeContentOwner owner,
            out string issue)
        {
            return TryResolveCurrentResetOwner(
                scope,
                out owner,
                out issue);
        }

        ResetRegistryOperationResult IResetRegistrationRuntimePort.RegisterResetSubject(
            ResetSubject subject,
            Object owner,
            string source,
            string reason)
        {
            return RegisterResetSubject(
                subject,
                owner,
                source,
                reason);
        }

        ResetRegistryOperationResult IResetRegistrationRuntimePort.RegisterRuntimeResetSubject(
            string authoredPrefix,
            ResetSubjectScope scope,
            RuntimeContentOwner owner,
            Object ownerObject,
            string displayName,
            string diagnosticTag,
            string source,
            string reason)
        {
            return RegisterRuntimeResetSubject(
                authoredPrefix,
                scope,
                owner,
                ownerObject,
                displayName,
                diagnosticTag,
                source,
                reason);
        }

        ResetRegistryOperationResult IResetRegistrationRuntimePort.RegisterResetParticipant(
            ResetRegistrationHandle subjectHandle,
            IResetParticipant participant,
            Object owner,
            string source,
            string reason)
        {
            return RegisterResetParticipant(
                subjectHandle,
                participant,
                owner,
                source,
                reason);
        }

        ResetRegistryOperationResult IResetRegistrationRuntimePort.UnregisterResetRegistration(
            ResetRegistrationHandle handle,
            Object owner,
            string source,
            string reason)
        {
            return UnregisterResetRegistration(
                handle,
                owner,
                source,
                reason);
        }
    }
}
