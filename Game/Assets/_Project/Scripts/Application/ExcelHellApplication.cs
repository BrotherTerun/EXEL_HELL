using System;
using System.Collections.Generic;
using System.Linq;
using ExcelHell.Prototype;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ExcelHell.Application
{
    /// <summary>
    /// Runtime application shell for the jam build. It deliberately lives outside the prototype
    /// scene so the graybox remains replaceable while menu/settings/save flow stays stable.
    /// </summary>
    public sealed class ExcelHellApplication : MonoBehaviour
    {
        private const int UiSortingOrder = 5000;
        private static ExcelHellApplication instance;

        private readonly Stack<GameObject> screenHistory = new();
        private readonly List<(int Width, int Height)> resolutions = new();

        private Canvas canvas;
        private Font font;
        private GameObject mainScreen;
        private GameObject pauseScreen;
        private GameObject settingsScreen;
        private GameObject loadScreen;
        private GameObject currentScreen;
        private Text continueText;
        private Button continueButton;
        private Text loadInfoText;
        private Text fullscreenText;
        private Text vsyncText;
        private Text resolutionText;
        private Text masterText;
        private Text musicText;
        private Text sfxText;
        private AppSettingsData editingSettings;
        private int resolutionIndex;
        private bool settingsOpenedFromGameplay;

        public static bool ShellAvailable => instance != null;
        public static bool GameplayActive { get; private set; }
        public static bool Paused { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null || FindFirstObjectByType<ExcelHellApplication>() != null) return;
            var root = new GameObject("EXEL HELL Application");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<ExcelHellApplication>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            GameplayActive = false;
            Paused = false;
            EnsureEventSystem();
            BuildResolutionList();
            BuildUi();
            AppSettingsService.LoadAndApply();
            ShowMainMenu();
        }

        private void Update()
        {
            if (!GameplayActive) return;
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            if (settingsScreen.activeSelf)
            {
                CloseSettings();
                return;
            }

            if (loadScreen.activeSelf)
            {
                CloseSubscreen();
                return;
            }

            SetPaused(!Paused);
        }

        public static bool CanPrototypeBootstrap()
        {
            // Preserve old behaviour if this script is removed/disabled on another branch.
            return instance == null || GameplayActive;
        }

        public static void NotifyLevelAdvanced(int levelIndex)
        {
            AppPersistence.SaveProgress(levelIndex, levelIndex);
        }

        public static void NotifyCampaignCompleted(int levelIndex)
        {
            AppPersistence.SaveProgress(levelIndex, levelIndex, true);
        }

        public static void AddNarrativeFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return;
            var level = PrototypeLevelRuntime.CurrentIndex;
            AppPersistence.SaveProgress(level, level, false, new[] { flag });
        }

        private void ShowMainMenu()
        {
            Time.timeScale = 1f;
            Paused = false;
            GameplayActive = false;
            DestroyPrototypeIfPresent();
            HideAllScreens();
            mainScreen.SetActive(true);
            currentScreen = mainScreen;
            screenHistory.Clear();
            RefreshContinueState();
        }

        private void NewGame()
        {
            AppPersistence.DeleteProgress();
            AppPersistence.SaveProgress(0, 0);
            StartGameplay(0);
        }

        private void ContinueGame()
        {
            if (!AppPersistence.HasProgress) return;
            var progress = AppPersistence.LoadProgress();
            StartGameplay(progress.CurrentLevelIndex);
        }

        private void StartGameplay(int levelIndex)
        {
            Time.timeScale = 1f;
            Paused = false;
            GameplayActive = true;
            HideAllScreens();
            PrototypeLevelRuntime.SetCurrentIndex(levelIndex);
            DestroyPrototypeIfPresent();
            new GameObject("EXEL HELL Prototype").AddComponent<ExcelHellPrototype>();
        }

        private void SetPaused(bool paused)
        {
            if (!GameplayActive) return;
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            HideAllScreens();
            if (paused)
            {
                pauseScreen.SetActive(true);
                currentScreen = pauseScreen;
            }
        }

        private void SaveCheckpoint()
        {
            AppPersistence.SaveProgress(PrototypeLevelRuntime.CurrentIndex, PrototypeLevelRuntime.CurrentIndex);
            SetPaused(false);
        }

        private void OpenLoadScreen(bool fromGameplay)
        {
            settingsOpenedFromGameplay = fromGameplay;
            PushScreen(loadScreen);
            RefreshLoadInfo();
        }

        private void LoadSelectedSave()
        {
            if (!AppPersistence.HasProgress) return;
            var progress = AppPersistence.LoadProgress();
            StartGameplay(progress.CurrentLevelIndex);
        }

        private void DeleteSelectedSave()
        {
            AppPersistence.DeleteProgress();
            RefreshLoadInfo();
            RefreshContinueState();
        }

        private void OpenSettings(bool fromGameplay)
        {
            settingsOpenedFromGameplay = fromGameplay;
            editingSettings = CloneSettings(AppSettingsService.Current ?? AppPersistence.DefaultSettings());
            SyncResolutionIndex();
            RefreshSettingsLabels();
            PushScreen(settingsScreen);
        }

        private void ApplyAndCloseSettings()
        {
            AppSettingsService.Apply(editingSettings, true);
            CloseSettings();
        }

        private void ResetSettings()
        {
            editingSettings = AppPersistence.DefaultSettings();
            SyncResolutionIndex();
            RefreshSettingsLabels();
        }

        private void CloseSettings()
        {
            if (settingsOpenedFromGameplay)
            {
                HideAllScreens();
                pauseScreen.SetActive(true);
                currentScreen = pauseScreen;
                Paused = true;
                Time.timeScale = 0f;
                screenHistory.Clear();
            }
            else
            {
                HideAllScreens();
                mainScreen.SetActive(true);
                currentScreen = mainScreen;
                screenHistory.Clear();
            }
        }

        private void CloseSubscreen()
        {
            if (screenHistory.Count > 0)
            {
                var previous = screenHistory.Pop();
                HideAllScreens();
                previous.SetActive(true);
                currentScreen = previous;
                return;
            }
            CloseSettings();
        }

        private void PushScreen(GameObject target)
        {
            if (currentScreen != null && currentScreen.activeSelf) screenHistory.Push(currentScreen);
            HideAllScreens();
            target.SetActive(true);
            currentScreen = target;
        }

        private void HideAllScreens()
        {
            if (mainScreen != null) mainScreen.SetActive(false);
            if (pauseScreen != null) pauseScreen.SetActive(false);
            if (settingsScreen != null) settingsScreen.SetActive(false);
            if (loadScreen != null) loadScreen.SetActive(false);
        }

        private void RefreshContinueState()
        {
            if (continueButton == null) return;
            var hasSave = AppPersistence.HasProgress;
            continueButton.interactable = hasSave;
            if (continueText != null) continueText.text = hasSave ? "ПРОДОЛЖИТЬ / CONTINUE" : "ПРОДОЛЖИТЬ / NO SAVE";
        }

        private void RefreshLoadInfo()
        {
            if (loadInfoText == null) return;
            if (!AppPersistence.HasProgress)
            {
                loadInfoText.text = "СОХРАНЕНИЙ НЕТ / NO SAVE DATA";
                return;
            }

            var data = AppPersistence.LoadProgress();
            var level = PrototypeLevelCatalog.Get(data.CurrentLevelIndex);
            var timestamp = string.IsNullOrEmpty(data.SavedAtUtc) ? "—" : data.SavedAtUtc;
            loadInfoText.text = $"СЛОТ 01\nУровень {data.CurrentLevelIndex + 1}: {level.NameRu}\nUnlocked: {data.HighestUnlockedLevelIndex + 1}\nSaved: {timestamp}";
        }

        private void BuildUi()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("Application Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = UiSortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            mainScreen = CreateScreen("Main Menu", new Color(0.035f, 0.04f, 0.045f, 1f));
            pauseScreen = CreateScreen("Pause Menu", new Color(0.02f, 0.025f, 0.03f, 0.94f));
            settingsScreen = CreateScreen("Settings", new Color(0.025f, 0.03f, 0.035f, 0.98f));
            loadScreen = CreateScreen("Load Game", new Color(0.025f, 0.03f, 0.035f, 0.98f));

            BuildMainMenu();
            BuildPauseMenu();
            BuildSettingsMenu();
            BuildLoadMenu();
        }

        private void BuildMainMenu()
        {
            var panel = CreatePanel(mainScreen.transform, 610f, 720f);
            CreateLabel(panel, "EXEL HELL", 58, FontStyle.Bold, 82f);
            CreateLabel(panel, "CORPORATE SPREADSHEET CLIENT // BUILD SHELL", 18, FontStyle.Normal, 42f);
            AddSpacer(panel, 34f);
            CreateButton(panel, "НОВАЯ ИГРА / NEW GAME", NewGame);
            continueButton = CreateButton(panel, "ПРОДОЛЖИТЬ / CONTINUE", ContinueGame, out continueText);
            CreateButton(panel, "ЗАГРУЗИТЬ / LOAD", () => OpenLoadScreen(false));
            CreateButton(panel, "НАСТРОЙКИ / SETTINGS", () => OpenSettings(false));
            CreateButton(panel, "ВЫХОД / QUIT", QuitGame);
            AddSpacer(panel, 22f);
            CreateLabel(panel, "Псевдо-Excel tactical puzzle // application shell", 15, FontStyle.Italic, 32f);
        }

        private void BuildPauseMenu()
        {
            var panel = CreatePanel(pauseScreen.transform, 560f, 650f);
            CreateLabel(panel, "ПАУЗА / PAUSED", 42, FontStyle.Bold, 76f);
            AddSpacer(panel, 20f);
            CreateButton(panel, "ПРОДОЛЖИТЬ / RESUME", () => SetPaused(false));
            CreateButton(panel, "СОХРАНИТЬ CHECKPOINT / SAVE", SaveCheckpoint);
            CreateButton(panel, "ЗАГРУЗИТЬ / LOAD", () => OpenLoadScreen(true));
            CreateButton(panel, "НАСТРОЙКИ / SETTINGS", () => OpenSettings(true));
            CreateButton(panel, "В ГЛАВНОЕ МЕНЮ / MAIN MENU", ShowMainMenu);
        }

        private void BuildLoadMenu()
        {
            var panel = CreatePanel(loadScreen.transform, 720f, 610f);
            CreateLabel(panel, "ЗАГРУЗКА / LOAD GAME", 40, FontStyle.Bold, 76f);
            loadInfoText = CreateLabel(panel, "", 20, FontStyle.Normal, 170f);
            CreateButton(panel, "ЗАГРУЗИТЬ СЛОТ / LOAD SLOT", LoadSelectedSave);
            CreateButton(panel, "УДАЛИТЬ СОХРАНЕНИЕ / DELETE SAVE", DeleteSelectedSave);
            CreateButton(panel, "НАЗАД / BACK", CloseSubscreen);
        }

        private void BuildSettingsMenu()
        {
            var panel = CreatePanel(settingsScreen.transform, 760f, 850f);
            CreateLabel(panel, "НАСТРОЙКИ / SETTINGS", 40, FontStyle.Bold, 72f);
            AddSpacer(panel, 8f);
            CreateButton(panel, "", () => AdjustVolume("master", 0.1f), out masterText);
            CreateButton(panel, "", () => AdjustVolume("music", 0.1f), out musicText);
            CreateButton(panel, "", () => AdjustVolume("sfx", 0.1f), out sfxText);
            CreateButton(panel, "", ToggleFullscreen, out fullscreenText);
            CreateButton(panel, "", ToggleVSync, out vsyncText);
            CreateButton(panel, "", CycleResolution, out resolutionText);
            AddSpacer(panel, 12f);
            CreateLabel(panel, "Нажатие на громкость: +10%; после 100% → 0%. Music/SFX сохраняются как отдельные каналы и будут подключены к AudioMixer при добавлении аудио.", 16, FontStyle.Normal, 82f);
            CreateButton(panel, "ПРИМЕНИТЬ / APPLY", ApplyAndCloseSettings);
            CreateButton(panel, "СБРОСИТЬ / DEFAULTS", ResetSettings);
            CreateButton(panel, "ОТМЕНА / CANCEL", CloseSettings);
        }

        private void AdjustVolume(string channel, float step)
        {
            if (editingSettings == null) return;
            float Cycle(float value)
            {
                value += step;
                return value > 1.001f ? 0f : Mathf.Round(value * 10f) / 10f;
            }

            switch (channel)
            {
                case "master": editingSettings.MasterVolume = Cycle(editingSettings.MasterVolume); break;
                case "music": editingSettings.MusicVolume = Cycle(editingSettings.MusicVolume); break;
                case "sfx": editingSettings.SfxVolume = Cycle(editingSettings.SfxVolume); break;
            }
            RefreshSettingsLabels();
        }

        private void ToggleFullscreen()
        {
            editingSettings.Fullscreen = !editingSettings.Fullscreen;
            RefreshSettingsLabels();
        }

        private void ToggleVSync()
        {
            editingSettings.VSync = !editingSettings.VSync;
            RefreshSettingsLabels();
        }

        private void CycleResolution()
        {
            if (resolutions.Count == 0) return;
            resolutionIndex = (resolutionIndex + 1) % resolutions.Count;
            editingSettings.ResolutionWidth = resolutions[resolutionIndex].Width;
            editingSettings.ResolutionHeight = resolutions[resolutionIndex].Height;
            RefreshSettingsLabels();
        }

        private void RefreshSettingsLabels()
        {
            if (editingSettings == null) return;
            if (masterText != null) masterText.text = $"ОБЩАЯ ГРОМКОСТЬ / MASTER: {Mathf.RoundToInt(editingSettings.MasterVolume * 100f)}%";
            if (musicText != null) musicText.text = $"МУЗЫКА / MUSIC: {Mathf.RoundToInt(editingSettings.MusicVolume * 100f)}%";
            if (sfxText != null) sfxText.text = $"ЭФФЕКТЫ / SFX: {Mathf.RoundToInt(editingSettings.SfxVolume * 100f)}%";
            if (fullscreenText != null) fullscreenText.text = $"ПОЛНЫЙ ЭКРАН / FULLSCREEN: {(editingSettings.Fullscreen ? "ON" : "OFF")}";
            if (vsyncText != null) vsyncText.text = $"VSYNC: {(editingSettings.VSync ? "ON" : "OFF")}";
            if (resolutionText != null) resolutionText.text = $"РАЗРЕШЕНИЕ / RESOLUTION: {editingSettings.ResolutionWidth} × {editingSettings.ResolutionHeight}";
        }

        private void BuildResolutionList()
        {
            resolutions.Clear();
            foreach (var resolution in Screen.resolutions)
            {
                var pair = (resolution.width, resolution.height);
                if (!resolutions.Contains(pair)) resolutions.Add(pair);
            }
            if (resolutions.Count == 0) resolutions.Add((Screen.currentResolution.width, Screen.currentResolution.height));
        }

        private void SyncResolutionIndex()
        {
            if (editingSettings == null || resolutions.Count == 0) return;
            var found = resolutions.FindIndex(r => r.Width == editingSettings.ResolutionWidth && r.Height == editingSettings.ResolutionHeight);
            resolutionIndex = found >= 0 ? found : resolutions.Count - 1;
            var resolution = resolutions[resolutionIndex];
            editingSettings.ResolutionWidth = resolution.Width;
            editingSettings.ResolutionHeight = resolution.Height;
        }

        private GameObject CreateScreen(string name, Color background)
        {
            var screen = new GameObject(name, typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(canvas.transform, false);
            var rect = screen.GetComponent<RectTransform>();
            Stretch(rect);
            screen.GetComponent<Image>().color = background;
            screen.SetActive(false);
            return screen;
        }

        private Transform CreatePanel(Transform parent, float width, float height)
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.095f, 0.105f, 0.98f);
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 34, 34);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            return panel.transform;
        }

        private Text CreateLabel(Transform parent, string value, int size, FontStyle style, float height)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.92f, 0.95f, 0.96f, 1f);
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private Button CreateButton(Transform parent, string label, Action callback)
        {
            return CreateButton(parent, label, callback, out _);
        }

        private Button CreateButton(Transform parent, string label, Action callback, out Text labelText)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 62f;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.14f, 0.18f, 0.2f, 1f);
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.62f, 0.85f, 0.9f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            button.colors = colors;
            button.onClick.AddListener(() => callback?.Invoke());

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            Stretch(textRect);
            textRect.offsetMin = new Vector2(12, 4);
            textRect.offsetMax = new Vector2(-12, -4);
            labelText = textGo.GetComponent<Text>();
            labelText.font = font;
            labelText.fontSize = 20;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.text = label;
            return button;
        }

        private static void AddSpacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            DontDestroyOnLoad(eventSystem);
        }

        private static AppSettingsData CloneSettings(AppSettingsData source)
        {
            return new AppSettingsData
            {
                Version = source.Version,
                MasterVolume = source.MasterVolume,
                MusicVolume = source.MusicVolume,
                SfxVolume = source.SfxVolume,
                Fullscreen = source.Fullscreen,
                VSync = source.VSync,
                ResolutionWidth = source.ResolutionWidth,
                ResolutionHeight = source.ResolutionHeight
            };
        }

        private static void DestroyPrototypeIfPresent()
        {
            var prototype = FindFirstObjectByType<ExcelHellPrototype>();
            if (prototype != null) Destroy(prototype.gameObject);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("EXEL HELL: Quit requested (ignored in editor).");
#else
            Application.Quit();
#endif
        }
    }
}