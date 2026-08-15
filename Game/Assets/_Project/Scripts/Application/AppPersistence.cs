using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ExcelHell.Application
{
    [Serializable]
    public sealed class AppProgressData
    {
        public int Version = 1;
        public int CurrentLevelIndex;
        public int HighestUnlockedLevelIndex;
        public bool CampaignCompleted;
        public List<string> NarrativeFlags = new();
        public string SavedAtUtc;
    }

    [Serializable]
    public sealed class AppSettingsData
    {
        public int Version = 2;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 0.9f;
        public bool Fullscreen = true;
        public bool VSync = true;
        public int ResolutionWidth;
        public int ResolutionHeight;
        public string LanguageCode = "ru";
    }

    public static class AppPersistence
    {
        private const string SaveDirectoryName = "Saves";
        private const string ProgressFileName = "excel_hell_progress.json";
        private const string SettingsFileName = "excel_hell_settings.json";

        private static string SaveDirectoryPath => Path.Combine(UnityEngine.Application.persistentDataPath, SaveDirectoryName);
        private static string ProgressPath => Path.Combine(SaveDirectoryPath, ProgressFileName);
        private static string SettingsPath => Path.Combine(SaveDirectoryPath, SettingsFileName);

        public static bool HasProgress
        {
            get
            {
                try { return File.Exists(ProgressPath); }
                catch (Exception exception)
                {
                    Debug.LogWarning($"EXEL HELL: progress existence check failed: {exception.Message}");
                    return false;
                }
            }
        }

        public static AppProgressData LoadProgress()
        {
            try
            {
                return LoadJson(ProgressPath, new AppProgressData());
            }
            catch (Exception exception)
            {
                Debug.LogError($"EXEL HELL: progress save is unreadable. Falling back to a fresh profile. {exception}");
                return new AppProgressData();
            }
        }

        public static void SaveProgress(int currentLevelIndex, int highestUnlockedLevelIndex = -1, bool completed = false,
            IEnumerable<string> narrativeFlags = null)
        {
            var previous = HasProgress ? LoadProgress() : new AppProgressData();
            previous.CurrentLevelIndex = Mathf.Max(0, currentLevelIndex);
            previous.HighestUnlockedLevelIndex = Mathf.Max(
                previous.HighestUnlockedLevelIndex,
                highestUnlockedLevelIndex < 0 ? previous.CurrentLevelIndex : highestUnlockedLevelIndex);
            previous.CampaignCompleted |= completed;
            previous.SavedAtUtc = DateTime.UtcNow.ToString("O");

            if (narrativeFlags != null)
            {
                previous.NarrativeFlags ??= new List<string>();
                foreach (var flag in narrativeFlags)
                    if (!string.IsNullOrWhiteSpace(flag) && !previous.NarrativeFlags.Contains(flag))
                        previous.NarrativeFlags.Add(flag);
            }

            try
            {
                SaveJson(ProgressPath, previous);
            }
            catch (Exception exception)
            {
                Debug.LogError($"EXEL HELL: progress save failed: {exception}");
            }
        }

        public static void DeleteProgress()
        {
            try
            {
                if (File.Exists(ProgressPath)) File.Delete(ProgressPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"EXEL HELL: progress deletion failed: {exception.Message}");
            }
        }

        public static AppSettingsData LoadSettings()
        {
            AppSettingsData settings;
            try
            {
                settings = LoadJson<AppSettingsData>(SettingsPath, null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"EXEL HELL: settings load failed, defaults will be used: {exception.Message}");
                settings = null;
            }

            settings ??= DefaultSettings();
            settings.Version = 2;
            settings.LanguageCode = NormalizeLanguageCode(settings.LanguageCode);
            if (settings.ResolutionWidth <= 0 || settings.ResolutionHeight <= 0)
            {
                settings.ResolutionWidth = Screen.currentResolution.width;
                settings.ResolutionHeight = Screen.currentResolution.height;
            }
            return settings;
        }

        public static void SaveSettings(AppSettingsData settings)
        {
            if (settings == null) return;
            settings.Version = 2;
            settings.LanguageCode = NormalizeLanguageCode(settings.LanguageCode);
            try
            {
                SaveJson(SettingsPath, settings);
            }
            catch (Exception exception)
            {
                Debug.LogError($"EXEL HELL: settings save failed: {exception}");
            }
        }

        public static AppSettingsData DefaultSettings()
        {
            return new AppSettingsData
            {
                ResolutionWidth = Screen.currentResolution.width,
                ResolutionHeight = Screen.currentResolution.height,
                Fullscreen = Screen.fullScreen,
                VSync = QualitySettings.vSyncCount > 0,
                LanguageCode = "ru"
            };
        }

        public static string NormalizeLanguageCode(string value)
        {
            return string.Equals(value, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";
        }

        private static T LoadJson<T>(string path, T fallback) where T : class
        {
            if (!File.Exists(path)) return fallback;
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return fallback;
            return JsonUtility.FromJson<T>(json) ?? fallback;
        }

        private static void SaveJson<T>(string path, T data)
        {
            Directory.CreateDirectory(SaveDirectoryPath);
            var json = JsonUtility.ToJson(data, true);
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(path)) File.Delete(path);
            File.Move(tempPath, path);
        }
    }

    public static class AppSettingsService
    {
        public static AppSettingsData Current { get; private set; }
        public static event Action<string> LanguageChanged = delegate { };

        public static void LoadAndApply()
        {
            Current = AppPersistence.LoadSettings();
            Apply(Current, false);
        }

        public static void Apply(AppSettingsData settings, bool persist = true)
        {
            if (settings == null) return;
            settings.Version = 2;
            settings.LanguageCode = AppPersistence.NormalizeLanguageCode(settings.LanguageCode);
            settings.MasterVolume = Mathf.Clamp01(settings.MasterVolume);
            settings.MusicVolume = Mathf.Clamp01(settings.MusicVolume);
            settings.SfxVolume = Mathf.Clamp01(settings.SfxVolume);
            Current = settings;

            AudioListener.volume = settings.MasterVolume;
            QualitySettings.vSyncCount = settings.VSync ? 1 : 0;

            var mode = settings.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (settings.ResolutionWidth > 0 && settings.ResolutionHeight > 0)
                Screen.SetResolution(settings.ResolutionWidth, settings.ResolutionHeight, mode);

            LanguageChanged(settings.LanguageCode);
            if (persist) AppPersistence.SaveSettings(settings);
        }

        public static void ResetToDefaults()
        {
            Apply(AppPersistence.DefaultSettings());
        }
    }
}
