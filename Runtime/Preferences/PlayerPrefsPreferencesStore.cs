using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Preferences
{
    /// <summary>
    /// API status: Experimental. PlayerPrefs-backed adapter for Preferences.
    /// It writes a type marker beside each value so mismatched or unmarked keys do not fall back silently.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21D PlayerPrefs adapter for Preferences only; not Progression Save.")]
    public sealed class PlayerPrefsPreferencesStore : IPreferencesStore
    {
        public const string DefaultKeyPrefix = "immersive.framework.preferences";
        private const string TypeMarkerSegment = "__type__";

        private readonly string _keyPrefix;

        public PlayerPrefsPreferencesStore()
            : this(DefaultKeyPrefix)
        {
        }

        public PlayerPrefsPreferencesStore(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                throw new ArgumentException("PlayerPrefs Preferences key prefix cannot be null, empty or whitespace.", nameof(keyPrefix));
            }

            _keyPrefix = keyPrefix.Trim().TrimEnd('.');
        }

        public string KeyPrefix => _keyPrefix;

        public bool Contains(PreferenceKey key)
        {
            EnsureKey(key);
            return PlayerPrefs.HasKey(ToPhysicalValueKey(key)) && PlayerPrefs.HasKey(ToPhysicalTypeKey(key));
        }

        public PreferenceReadResult Read(PreferenceKey key, PreferenceValueKind expectedKind)
        {
            EnsureKey(key);
            ValidateKind(expectedKind);

            string valueKey = ToPhysicalValueKey(key);
            string typeKey = ToPhysicalTypeKey(key);
            bool hasValue = PlayerPrefs.HasKey(valueKey);
            bool hasType = PlayerPrefs.HasKey(typeKey);

            if (!hasValue && !hasType)
            {
                return PreferenceReadResult.MissingResult(key, expectedKind, "Preference key was not found.");
            }

            if (!hasValue || !hasType)
            {
                return PreferenceReadResult.TypeMismatchResult(key, expectedKind, "Preference key has incomplete PlayerPrefs value/type marker pair.");
            }

            string storedKindText = PlayerPrefs.GetString(typeKey, string.Empty);
            if (!TryParseKind(storedKindText, out var storedKind))
            {
                return PreferenceReadResult.TypeMismatchResult(key, expectedKind, "Preference key has an invalid PlayerPrefs type marker.");
            }

            if (storedKind != expectedKind)
            {
                return PreferenceReadResult.TypeMismatchResult(key, expectedKind, $"Preference key stored kind '{storedKind}' does not match expected kind '{expectedKind}'.");
            }

            try
            {
                return PreferenceReadResult.FoundResult(key, ReadValue(valueKey, expectedKind), "Preference key read from PlayerPrefs.");
            }
            catch (Exception exception)
            {
                return PreferenceReadResult.FailedResult(key, expectedKind, exception.Message);
            }
        }

        public PreferenceWriteResult Write(PreferenceKey key, PreferenceValue value)
        {
            EnsureKey(key);
            if (!value.IsValid)
            {
                throw new ArgumentException("Preference write requires a valid value.", nameof(value));
            }

            string valueKey = ToPhysicalValueKey(key);
            string typeKey = ToPhysicalTypeKey(key);

            try
            {
                WriteValue(valueKey, value);
                PlayerPrefs.SetString(typeKey, value.Kind.ToString());
                return PreferenceWriteResult.WrittenResult(key, value.Kind, "Preference key written to PlayerPrefs.");
            }
            catch (Exception exception)
            {
                return PreferenceWriteResult.FailedResult(key, value.Kind, exception.Message);
            }
        }

        public PreferenceWriteResult Delete(PreferenceKey key)
        {
            EnsureKey(key);
            string valueKey = ToPhysicalValueKey(key);
            string typeKey = ToPhysicalTypeKey(key);
            bool hasValue = PlayerPrefs.HasKey(valueKey);
            bool hasType = PlayerPrefs.HasKey(typeKey);

            if (!hasValue && !hasType)
            {
                return PreferenceWriteResult.MissingResult(key, "Preference key was already absent.");
            }

            try
            {
                PlayerPrefs.DeleteKey(valueKey);
                PlayerPrefs.DeleteKey(typeKey);
                return PreferenceWriteResult.DeletedResult(key, "Preference key deleted from PlayerPrefs.");
            }
            catch (Exception exception)
            {
                return PreferenceWriteResult.FailedResult(key, PreferenceValueKind.Unknown, exception.Message);
            }
        }

        public void Flush()
        {
            PlayerPrefs.Save();
        }

        public string ToPhysicalValueKey(PreferenceKey key)
        {
            EnsureKey(key);
            return $"{_keyPrefix}.{key.Value.Value}";
        }

        public string ToPhysicalTypeKey(PreferenceKey key)
        {
            EnsureKey(key);
            return $"{_keyPrefix}.{TypeMarkerSegment}.{key.Value.Value}";
        }

        private static PreferenceValue ReadValue(string valueKey, PreferenceValueKind kind)
        {
            switch (kind)
            {
                case PreferenceValueKind.String:
                    return PreferenceValue.FromString(PlayerPrefs.GetString(valueKey));
                case PreferenceValueKind.Int:
                    return PreferenceValue.FromInt(PlayerPrefs.GetInt(valueKey));
                case PreferenceValueKind.Float:
                    return PreferenceValue.FromFloat(PlayerPrefs.GetFloat(valueKey));
                case PreferenceValueKind.Bool:
                    return PreferenceValue.FromBool(PlayerPrefs.GetInt(valueKey) != 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Preference value kind must be explicit.");
            }
        }

        private static void WriteValue(string valueKey, PreferenceValue value)
        {
            switch (value.Kind)
            {
                case PreferenceValueKind.String:
                    PlayerPrefs.SetString(valueKey, value.StringValue);
                    return;
                case PreferenceValueKind.Int:
                    PlayerPrefs.SetInt(valueKey, value.IntValue);
                    return;
                case PreferenceValueKind.Float:
                    PlayerPrefs.SetFloat(valueKey, value.FloatValue);
                    return;
                case PreferenceValueKind.Bool:
                    PlayerPrefs.SetInt(valueKey, value.BoolValue ? 1 : 0);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Kind, "Preference value kind must be explicit.");
            }
        }

        private static void EnsureKey(PreferenceKey key)
        {
            if (!key.IsValid)
            {
                throw new ArgumentException("Preference key must be valid.", nameof(key));
            }
        }

        private static void ValidateKind(PreferenceValueKind kind)
        {
            if (!Enum.IsDefined(typeof(PreferenceValueKind), kind) || kind == PreferenceValueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Preference value kind must be explicit.");
            }
        }

        private static bool TryParseKind(string value, out PreferenceValueKind kind)
        {
            if (Enum.TryParse(value, ignoreCase: false, out kind))
            {
                return Enum.IsDefined(typeof(PreferenceValueKind), kind) && kind != PreferenceValueKind.Unknown;
            }

            kind = PreferenceValueKind.Unknown;
            return false;
        }
    }
}
