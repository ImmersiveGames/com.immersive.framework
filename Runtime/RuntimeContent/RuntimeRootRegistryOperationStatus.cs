using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Internal result vocabulary for runtime root registry operations.
    /// This is diagnostic state only; it does not materialize, destroy or release runtime content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8F internal runtime root registry operation status; adds logical root lifecycle results, no materialization or release execution.")]
    internal enum RuntimeRootRegistryOperationStatus
    {
        /// <summary>
        /// Invalid default value. Registry operations must always return an explicit status.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A logical runtime scope root was created for the requested owner.
        /// </summary>
        RootCreated = 10,

        /// <summary>
        /// A logical runtime scope root for the requested owner already existed.
        /// </summary>
        RootAlreadyExists = 20,

        /// <summary>
        /// A logical runtime scope root was removed from the registry.
        /// </summary>
        RootRemoved = 25,

        /// <summary>
        /// A logical runtime scope root was already absent from the registry.
        /// </summary>
        RootMissing = 26,

        /// <summary>
        /// A runtime content handle was registered in its owner root.
        /// </summary>
        HandleRegistered = 30,

        /// <summary>
        /// The same runtime content handle was already registered in its owner root.
        /// </summary>
        HandleAlreadyRegistered = 40,

        /// <summary>
        /// A runtime content handle was removed from its owner root registry.
        /// </summary>
        HandleUnregistered = 50,

        /// <summary>
        /// The requested runtime content handle was not present in its owner root registry.
        /// </summary>
        HandleMissing = 60,

        /// <summary>
        /// The requested owner root does not exist in the registry.
        /// </summary>
        RejectedMissingRoot = 100,

        /// <summary>
        /// The handle or identity owner does not match the target root owner.
        /// </summary>
        RejectedMismatchedOwner = 110,

        /// <summary>
        /// Another handle with the same runtime content identity is already registered.
        /// </summary>
        RejectedDuplicateHandle = 120,

        /// <summary>
        /// A logical runtime scope root cannot be removed while it still has registered handles.
        /// </summary>
        RejectedRootHasHandles = 130
    }
}
