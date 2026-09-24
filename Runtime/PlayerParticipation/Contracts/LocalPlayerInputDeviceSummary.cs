using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-025 immutable local Player input device evidence.")]
    public readonly struct LocalPlayerInputDeviceSummary
    {
        private readonly string _layout;
        private readonly string _displayName;

        internal LocalPlayerInputDeviceSummary(
            int deviceId,
            string layout,
            string displayName)
        {
            DeviceId = deviceId;
            _layout = layout;
            _displayName = displayName;
        }

        public int DeviceId { get; }
        public string Layout => _layout ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
    }
}
