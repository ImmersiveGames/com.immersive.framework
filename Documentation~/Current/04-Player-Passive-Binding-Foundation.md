# 04 — Player Passive Binding Foundation

PlayerViewBehaviour remains passive view evidence. It is not a camera authority.

PlayerComposer exposes explicit CameraTarget and LookAtTarget values for a CameraRigComposer to consume. It does not own a Unity Camera, CinemachineCamera, output or runtime selection policy.
