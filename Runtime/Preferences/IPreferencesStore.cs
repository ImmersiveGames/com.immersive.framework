using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Preferences
{
    /// <summary>
    /// API status: Experimental. Storage port for user/application Preferences.
    /// This is not a Progression Save backend and must not use progression slots, Snapshot envelopes or gameplay state capture.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21D Preferences storage port; independent from Snapshot and Progression Save.")]
    public interface IPreferencesStore
    {
        bool Contains(PreferenceKey key);

        PreferenceReadResult Read(PreferenceKey key, PreferenceValueKind expectedKind);

        PreferenceWriteResult Write(PreferenceKey key, PreferenceValue value);

        PreferenceWriteResult Delete(PreferenceKey key);

        void Flush();
    }
}
