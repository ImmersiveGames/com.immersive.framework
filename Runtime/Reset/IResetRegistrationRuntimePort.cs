using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Reset
{
    internal interface IResetRegistrationRuntimePort
    {
        bool TryResolveCurrentResetOwner(
            ResetSubjectScope scope,
            out RuntimeContentOwner owner,
            out string issue);

        ResetRegistryOperationResult RegisterResetSubject(
            ResetSubject subject,
            Object owner,
            string source,
            string reason);

        ResetRegistryOperationResult RegisterRuntimeResetSubject(
            string authoredPrefix,
            ResetSubjectScope scope,
            RuntimeContentOwner owner,
            Object ownerObject,
            string displayName,
            string diagnosticTag,
            string source,
            string reason);

        ResetRegistryOperationResult RegisterResetParticipant(
            ResetRegistrationHandle subjectHandle,
            IResetParticipant participant,
            Object owner,
            string source,
            string reason);

        ResetRegistryOperationResult UnregisterResetRegistration(
            ResetRegistrationHandle handle,
            Object owner,
            string source,
            string reason);
    }
}
