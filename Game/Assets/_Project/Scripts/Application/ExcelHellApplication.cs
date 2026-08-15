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
    public sealed class ExcelHellApplication : MonoBehaviour
    {
        private const int UiSortingOrder = 32000;
        private const float SettingsSaveDebounceSeconds = 0.3f;
        private static ExcelHellApplication instance;

        private readonly Stack<GameObject> screenHistory = new();
        private readonly List<(int Width, int Height)> resolutions = new();

        private Canvas canvas;
        private Font font;
        private GameObject mainScreen;
        private GameObject pauseScreen;
        private GameObject settingsScreen;
        private GameObject loadScreen;
        private GameObject helpScreen;
        private GameObject resolutionPopup;
        private GameObject currentScreen;

        private Button continueButton;
        private Text titleText;
        private Text subtitleText;
        private Text newGameText;
        private Text continueText;
        private Text loadButtonText;
        private Text settingsButtonText;
        private Text quitText;
        private Text pauseTitleText;
        private Text resumeText;
        private Text saveText;
        private Text pauseLoadText;
        private Text pauseSettingsText;
        private Text helpButtonText;
        private Text resetLevelText;
        private Text mainMenuText;
        private Text settingsTitleText;
        private Text masterLabel;
        private Text musicLabel;
        private Text sfxLabel;
        private Text fullscreenText;
        private Text vsyncText;
        private Text resolutionText;
        private Text languageText;
        private Text defaultsText;
        private Text settingsBackText;
        private Text loadTitleText;
        private Text loadInfoText;
        private Text loadSlotText;
        private Text deleteSaveText;
        private Text backFromLoadText;
        private Text helpTitleText;
        private Text helpBodyText;
        private Text backFromHelpText;

        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;

        private AppSettingsData editingSettings;
        private bool settingsDirty;
        private float settingsDirtyAt;

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
            RefreshLocalizedText();
            ShowMainMenu();
        }

        private void Update()
        {
            PersistSettingsIfDue();

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            if (resolutionPopup != null && resolutionPopup.activeSelf)
            {
                resolutionPopup.SetActive(false);
                return;
            }

            if (settingsScreen != null && settingsScreen.activeSelf)
            {
                ApplySettingsAndReturn();
                return;
            }

            if ((loadScreen != null && loadScreen.activeSelf) || (helpScreen != null && helpScreen.activeSelf))
            {
                CloseSubscreen();
                return;
            }

            if (GameplayActive)
                SetPaused(!Paused);
        }

        private void OnDestroy()
        {
            PersistSettingsDraft();
            if (instance == this) instance = null;
        }

        public static bool CanPrototypeBootstrap() => instance == null || GameplayActive;

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

        public static void OpenGameplayMenu()
        {
            if (instance == null || !GameplayActive) return;
            instance.SetPaused(true);
        }

        public static string CurrentLanguageCode =>
            AppSettingsService.Current?.LanguageCode ?? "ru";

        private bool IsRussian => string.Equals(
            editingSettings?.LanguageCode ?? AppSettingsService.Current?.LanguageCode ?? "ru",
            "ru", StringComparison.OrdinalIgnoreCase);

        private string L(string ru, string en) => IsRussian ? ru : en;

        private void ShowMainMenu()
        {
            Time.timeScale = 1f;
            Paused = false;
            GameplayActive = false;
            PersistSettingsDraft();
            editingSettings = null;
            DestroyPrototypeIfPresent();
            HideAllScreens();
            mainScreen.SetActive(true);
            mainScreen.transform.SetAsLastSibling();
            currentScreen = mainScreen;
            screenHistory.Clear();
            RefreshLocalizedText();
            RefreshContinueState();
            EnsureShellOnTop();
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
            PersistSettingsDraft();
            editingSettings = null;
            HideAllScreens();
            currentScreen = null;
            screenHistory.Clear();
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
            screenHistory.Clear();

            if (!paused)
            {
                currentScreen = null;
                return;
            }

            pauseScreen.SetActive(true);
            pauseScreen.transform.SetAsLastSibling();
            currentScreen = pauseScreen;
            RefreshLocalizedText();
            EnsureShellOnTop();
            Canvas.ForceUpdateCanvases();
        }

        private void SaveCheckpoint()
        {
            AppPersistence.SaveProgress(PrototypeLevelRuntime.CurrentIndex, PrototypeLevelRuntime.CurrentIndex);
            SetPaused(false);
        }

        private void ResetCurrentLevel()
        {
            if (!GameplayActive) return;
            Time.timeScale = 1f;
            Paused = false;
            HideAllScreens();
            currentScreen = null;
            screenHistory.Clear();
            DestroyPrototypeIfPresent();
            new GameObject("EXEL HELL Prototype").AddComponent<ExcelHellPrototype>();
        }

        private void OpenLoadScreen()
        {
            PushScreen(loadScreen);
            RefreshLoadInfo();
            RefreshLocalizedText();
        }

        private void OpenHelpScreen()
        {
            PushScreen(helpScreen);
            RefreshLocalizedText();
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

        private void OpenSettings()
        {
            editingSettings = CloneSettings(AppSettingsService.Current ?? AppPersistence.DefaultSettings());
            settingsDirty = false;
            SyncSettingsControls();
            RefreshLocalizedText();
            PushScreen(settingsScreen);
        }

        private void ApplySettingsAndReturn()
        {
            if (editingSettings != null)
            {
                PullSliderValues();
                AppSettingsService.Apply(editingSettings, true);
                settingsDirty = false;
                editingSettings = null;
            }

            if (resolutionPopup != null) resolutionPopup.SetActive(false);
            CloseSubscreen();
        }

        private void ResetSettings()
        {
            editingSettings = AppPersistence.DefaultSettings();
            SyncSettingsControls();
            RefreshLocalizedText();
            MarkSettingsDirty();
        }

        private void CloseSubscreen()
        {
            if (resolutionPopup != null) resolutionPopup.SetActive(false);

            if (screenHistory.Count > 0)
            {
                var previous = screenHistory.Pop();
                HideAllScreens();
                previous.SetActive(true);
                previous.transform.SetAsLastSibling();
                currentScreen = previous;
                RefreshLocalizedText();
                EnsureShellOnTop();
                return;
            }

            if (GameplayActive && Paused)
            {
                HideAllScreens();
                pauseScreen.SetActive(true);
                pauseScreen.transform.SetAsLastSibling();
                currentScreen = pauseScreen;
                EnsureShellOnTop();
                return;
            }

            ShowMainMenu();
        }

        private void PushScreen(GameObject target)
        {
            if (currentScreen != null && currentScreen.activeSelf)
                screenHistory.Push(currentScreen);

            HideAllScreens();
            target.SetActive(true);
            target.transform.SetAsLastSibling();
            currentScreen = target;
            EnsureShellOnTop();
        }

        private void HideAllScreens()
        {
            if (mainScreen != null) mainScreen.SetActive(false);
            if (pauseScreen != null) pauseScreen.SetActive(false);
            if (settingsScreen != null) settingsScreen.SetActive(false);
            if (loadScreen != null) loadScreen.SetActive(false);
            if (helpScreen != null) helpScreen.SetActive(false);
            if (resolutionPopup != null) resolutionPopup.SetActive(false);
        }

        private void RefreshContinueState()
        {
            if (continueButton == null) return;
            var hasProgress = AppPersistence.HasProgress;
            continueButton.interactable = hasProgress;
            if (continueText != null)
                continueText.text = hasProgress
                    ? L("ПРОДОЛЖИТЬ", "CONTINUE")
                    : L("ПРОДОЛЖИТЬ — НЕТ СОХРАНЕНИЯ", "CONTINUE — NO SAVE");
        }

        private void RefreshLoadInfo()
        {
            if (loadInfoText == null) return;
            if (!AppPersistence.HasProgress)
            {
                loadInfoText.text = L("Сохранений нет", "No save data");
                return;
            }

            var data = AppPersistence.LoadProgress();
            var level = PrototypeLevelCatalog.Get(data.CurrentLevelIndex);
            var levelName = IsRussian ? level.NameRu : level.NameEn;
            var timestamp = string.IsNullOrEmpty(data.SavedAtUtc) ? "—" : data.SavedAtUtc;
            loadInfoText.text = IsRussian
                ? $"Слот 01\nУровень {data.CurrentLevelIndex + 1}: {levelName}\nОткрыто уровней: {data.HighestUnlockedLevelIndex + 1}\nСохранено: {timestamp}"
                : $"Slot 01\nLevel {data.CurrentLevelIndex + 1}: {levelName}\nUnlocked levels: {data.HighestUnlockedLevelIndex + 1}\nSaved: {timestamp}";
        }

        private void BuildUi()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("Application Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiSortingOrder;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            mainScreen = CreateScreen("Main Menu", new Color(0.035f, 0.04f, 0.045f, 1f));
            pauseScreen = CreateScreen("Pause Menu", new Color(0.02f, 0.025f, 0.03f, 0.96f));
            settingsScreen = CreateScreen("Settings", new Color(0.025f, 0.03f, 0.035f, 0.98f));
            loadScreen = CreateScreen("Load Game", new Color(0.025f, 0.03f, 0.035f, 0.98f));
            helpScreen = CreateScreen("Help", new Color(0.025f, 0.03f, 0.035f, 0.98f));

            BuildMainMenu();
            BuildPauseMenu();
            BuildSettingsMenu();
            BuildLoadMenu();
            BuildHelpMenu();
        }

        private void BuildMainMenu()
        {
            var panel = CreatePanel(mainScreen.transform, 610f, 600f);
            titleText = CreateLabel(panel, "EXEL HELL", 56, FontStyle.Bold, 70f);
            subtitleText = CreateLabel(panel, "", 18, FontStyle.Normal, 30f);
            AddSpacer(panel, 12f);
            CreateButton(panel, "", NewGame, out newGameText, 54f);
            continueButton = CreateButton(panel, "", ContinueGame, out continueText, 54f);
            CreateButton(panel, "", OpenLoadScreen, out loadButtonText, 54f);
            CreateButton(panel, "", OpenSettings, out settingsButtonText, 54f);
            CreateButton(panel, "", QuitGame, out quitText, 54f);
        }

        private void BuildPauseMenu()
        {
            var panel = CreatePanel(pauseScreen.transform, 590f, 620f);
            pauseTitleText = CreateLabel(panel, "", 40, FontStyle.Bold, 52f);
            CreateButton(panel, "", () => SetPaused(false), out resumeText, 46f);
            CreateButton(panel, "", SaveCheckpoint, out saveText, 46f);
            CreateButton(panel, "", OpenLoadScreen, out pauseLoadText, 46f);
            CreateButton(panel, "", OpenSettings, out pauseSettingsText, 46f);
            CreateButton(panel, "", OpenHelpScreen, out helpButtonText, 46f);
            CreateButton(panel, "", ResetCurrentLevel, out resetLevelText, 46f);
            CreateButton(panel, "", ShowMainMenu, out mainMenuText, 46f);
        }

        private void BuildLoadMenu()
        {
            var panel = CreatePanel(loadScreen.transform, 720f, 500f);
            loadTitleText = CreateLabel(panel, "", 38, FontStyle.Bold, 56f);
            loadInfoText = CreateLabel(panel, "", 19, FontStyle.Normal, 145f);
            CreateButton(panel, "", LoadSelectedSave, out loadSlotText, 50f);
            CreateButton(panel, "", DeleteSelectedSave, out deleteSaveText, 50f);
            CreateButton(panel, "", CloseSubscreen, out backFromLoadText, 50f);
        }

        private void BuildHelpMenu()
        {
            var panel = CreatePanel(helpScreen.transform, 900f, 700f);
            helpTitleText = CreateLabel(panel, "", 38, FontStyle.Bold, 56f);
            helpBodyText = CreateLabel(panel, "", 18, FontStyle.Normal, 475f);
            helpBodyText.alignment = TextAnchor.UpperLeft;
            CreateButton(panel, "", CloseSubscreen, out backFromHelpText, 48f);
        }

        private void BuildSettingsMenu()
        {
            var panel = CreatePanel(settingsScreen.transform, 800f, 690f);
            settingsTitleText = CreateLabel(panel, "", 38, FontStyle.Bold, 52f);

            masterSlider = CreateSliderRow(panel, out masterLabel, OnMasterChanged);
            musicSlider = CreateSliderRow(panel, out musicLabel, OnMusicChanged);
            sfxSlider = CreateSliderRow(panel, out sfxLabel, OnSfxChanged);

            CreateButton(panel, "", ToggleFullscreen, out fullscreenText, 44f);
            CreateButton(panel, "", ToggleVSync, out vsyncText, 44f);
            CreateButton(panel, "", ToggleResolutionPopup, out resolutionText, 44f);
            CreateButton(panel, "", ToggleEditingLanguage, out languageText, 44f);

            AddSpacer(panel, 4f);
            CreateButton(panel, "", ResetSettings, out defaultsText, 44f);
            CreateButton(panel, "", ApplySettingsAndReturn, out settingsBackText, 44f);

            BuildResolutionPopup();
        }

        private Slider CreateSliderRow(Transform parent, out Text label, UnityEngine.Events.UnityAction<float> callback)
        {
            var row = new GameObject("Slider Row", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 58f;
            rowLayout.preferredHeight = 58f;
            rowLayout.flexibleHeight = 0f;

            label = CreateFreeText(row.transform, 17, FontStyle.Bold, TextAnchor.MiddleLeft);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.47f, 1f);
            labelRect.offsetMin = new Vector2(4, 4);
            labelRect.offsetMax = new Vector2(-8, -4);

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.offsetMin = new Vector2(0, -10);
            sliderRect.offsetMax = new Vector2(-4, 10);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderGo.transform, false);
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.17f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>());
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.52f, 0.82f, 0.88f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>());
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            handle.GetComponent<Image>().color = new Color(0.9f, 0.94f, 0.96f, 1f);

            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.onValueChanged.AddListener(callback);
            return slider;
        }

        private void BuildResolutionPopup()
        {
            resolutionPopup = new GameObject("Resolution Popup", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            resolutionPopup.transform.SetParent(settingsScreen.transform, false);
            var rect = resolutionPopup.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500f, 500f);
            resolutionPopup.GetComponent<Image>().color = new Color(0.055f, 0.065f, 0.075f, 1f);

            var layout = resolutionPopup.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            foreach (var resolution in resolutions.Take(9))
            {
                var captured = resolution;
                CreateButton(resolutionPopup.transform, $"{captured.Width} × {captured.Height}", () => SelectResolution(captured), 44f);
            }

            resolutionPopup.SetActive(false);
        }

        private void ToggleResolutionPopup()
        {
            if (resolutionPopup == null) return;
            resolutionPopup.SetActive(!resolutionPopup.activeSelf);
            if (resolutionPopup.activeSelf)
            {
                resolutionPopup.transform.SetAsLastSibling();
                EnsureShellOnTop();
            }
        }

        private void SelectResolution((int Width, int Height) resolution)
        {
            if (editingSettings == null) return;
            editingSettings.ResolutionWidth = resolution.Width;
            editingSettings.ResolutionHeight = resolution.Height;
            resolutionPopup.SetActive(false);
            RefreshSettingsLabels();
            MarkSettingsDirty();
        }

        private void ToggleEditingLanguage()
        {
            if (editingSettings == null) return;
            editingSettings.LanguageCode = IsRussian ? "en" : "ru";
            RefreshLocalizedText();
            RefreshSettingsLabels();
            RefreshLoadInfo();
            MarkSettingsDirty();
        }

        private void ToggleFullscreen()
        {
            if (editingSettings == null) return;
            editingSettings.Fullscreen = !editingSettings.Fullscreen;
            RefreshSettingsLabels();
            MarkSettingsDirty();
        }

        private void ToggleVSync()
        {
            if (editingSettings == null) return;
            editingSettings.VSync = !editingSettings.VSync;
            RefreshSettingsLabels();
            MarkSettingsDirty();
        }

        private void OnMasterChanged(float value)
        {
            if (editingSettings == null) return;
            editingSettings.MasterVolume = value;
            AudioListener.volume = value;
            RefreshSettingsLabels();
            MarkSettingsDirty();
        }

        private void OnMusicChanged(float value)
        {
            if (editingSettings == null) return;
            editingSettings.MusicVolume = value;
            RefreshSettingsLabels();
            MarkSettingsDirty();
        }

        private void OnSfxChanged(float value)
        {
            if (editingSettings == null) return;
            editingSettings.SfxVolume = value;
            RefreshSettingsLabels();
            MarkSettingsDirty();
        }

        private void PullSliderValues()
        {
            if (editingSettings == null) return;
            editingSettings.MasterVolume = masterSlider.value;
            editingSettings.MusicVolume = musicSlider.value;
            editingSettings.SfxVolume = sfxSlider.value;
        }

        private void SyncSettingsControls()
        {
            if (editingSettings == null) return;
            masterSlider.SetValueWithoutNotify(editingSettings.MasterVolume);
            musicSlider.SetValueWithoutNotify(editingSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(editingSettings.SfxVolume);
            RefreshSettingsLabels();
        }

        private void MarkSettingsDirty()
        {
            if (editingSettings == null) return;
            settingsDirty = true;
            settingsDirtyAt = Time.realtimeSinceStartup;
        }

        private void PersistSettingsIfDue()
        {
            if (!settingsDirty || editingSettings == null) return;
            if (Time.realtimeSinceStartup - settingsDirtyAt < SettingsSaveDebounceSeconds) return;
            PersistSettingsDraft();
        }

        private void PersistSettingsDraft()
        {
            if (!settingsDirty || editingSettings == null) return;
            PullSliderValues();
            AppPersistence.SaveSettings(editingSettings);
            settingsDirty = false;
        }

        private void RefreshSettingsLabels()
        {
            if (editingSettings == null) return;
            if (masterLabel != null) masterLabel.text = $"{L("Общая громкость", "Master volume")}: {Mathf.RoundToInt(editingSettings.MasterVolume * 100f)}%";
            if (musicLabel != null) musicLabel.text = $"{L("Музыка", "Music")}: {Mathf.RoundToInt(editingSettings.MusicVolume * 100f)}%";
            if (sfxLabel != null) sfxLabel.text = $"{L("Эффекты", "Effects")}: {Mathf.RoundToInt(editingSettings.SfxVolume * 100f)}%";
            if (fullscreenText != null) fullscreenText.text = $"{L("Полный экран", "Fullscreen")}: {(editingSettings.Fullscreen ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF"))}";
            if (vsyncText != null) vsyncText.text = $"VSync: {(editingSettings.VSync ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF"))}";
            if (resolutionText != null) resolutionText.text = $"{L("Разрешение", "Resolution")}: {editingSettings.ResolutionWidth} × {editingSettings.ResolutionHeight}  ▼";
            if (languageText != null) languageText.text = $"{L("Язык", "Language")}: {(IsRussian ? "Русский" : "English")}";
        }

        private void RefreshLocalizedText()
        {
            if (subtitleText != null) subtitleText.text = L("КОРПОРАТИВНЫЙ ТАБЛИЧНЫЙ КЛИЕНТ", "CORPORATE SPREADSHEET CLIENT");
            if (newGameText != null) newGameText.text = L("НОВАЯ ИГРА", "NEW GAME");
            if (loadButtonText != null) loadButtonText.text = L("ЗАГРУЗИТЬ", "LOAD");
            if (settingsButtonText != null) settingsButtonText.text = L("НАСТРОЙКИ", "SETTINGS");
            if (quitText != null) quitText.text = L("ВЫХОД", "QUIT");
            if (pauseTitleText != null) pauseTitleText.text = L("МЕНЮ", "MENU");
            if (resumeText != null) resumeText.text = L("ПРОДОЛЖИТЬ", "RESUME");
            if (saveText != null) saveText.text = L("СОХРАНИТЬ", "SAVE");
            if (pauseLoadText != null) pauseLoadText.text = L("ЗАГРУЗИТЬ", "LOAD");
            if (pauseSettingsText != null) pauseSettingsText.text = L("НАСТРОЙКИ", "SETTINGS");
            if (helpButtonText != null) helpButtonText.text = L("СПРАВКА", "HELP");
            if (resetLevelText != null) resetLevelText.text = L("СБРОСИТЬ УРОВЕНЬ", "RESET LEVEL");
            if (mainMenuText != null) mainMenuText.text = L("ГЛАВНОЕ МЕНЮ", "MAIN MENU");
            if (settingsTitleText != null) settingsTitleText.text = L("НАСТРОЙКИ", "SETTINGS");
            if (defaultsText != null) defaultsText.text = L("ПО УМОЛЧАНИЮ", "DEFAULTS");
            if (settingsBackText != null) settingsBackText.text = L("НАЗАД", "BACK");
            if (loadTitleText != null) loadTitleText.text = L("ЗАГРУЗКА", "LOAD GAME");
            if (loadSlotText != null) loadSlotText.text = L("ЗАГРУЗИТЬ СЛОТ", "LOAD SLOT");
            if (deleteSaveText != null) deleteSaveText.text = L("УДАЛИТЬ СОХРАНЕНИЕ", "DELETE SAVE");
            if (backFromLoadText != null) backFromLoadText.text = L("НАЗАД", "BACK");
            if (helpTitleText != null) helpTitleText.text = L("СПРАВКА", "HELP");
            if (backFromHelpText != null) backFromHelpText.text = L("НАЗАД", "BACK");

            if (helpBodyText != null)
            {
                helpBodyText.text = L(
                    "Основные действия:\n\n• SORT собирает данные вокруг выбранного ключа.\n• SUM работает с непрерывным диапазоном минимум из двух чисел.\n• CUT / PASTE позволяют перестраивать лист.\n• DELETE удаляет клетку и может локализовать #REF!.\n• SUBMIT проверяет заполненный отчёт.\n\n#REF! заранее показывает следующую цель. Планируйте действия так, чтобы сохранить данные, необходимые для отчёта.\n\nEsc или кнопка МЕНЮ открывают системное меню.",
                    "Core actions:\n\n• SORT assembles data around the selected key.\n• SUM works on a contiguous range containing at least two numbers.\n• CUT / PASTE restructure the worksheet.\n• DELETE destroys a cell and can quarantine #REF!.\n• SUBMIT checks the completed report.\n\n#REF! telegraphs its next target. Plan around the threat and preserve report-critical data.\n\nEsc or the MENU button opens the system menu.");
            }

            RefreshContinueState();
            RefreshSettingsLabels();
        }

        private void BuildResolutionList()
        {
            resolutions.Clear();
            foreach (var resolution in Screen.resolutions)
            {
                var pair = (resolution.width, resolution.height);
                if (!resolutions.Contains(pair)) resolutions.Add(pair);
            }

            var current = (Screen.currentResolution.width, Screen.currentResolution.height);
            if (!resolutions.Contains(current)) resolutions.Add(current);

            resolutions.Sort((a, b) =>
            {
                var area = (b.Width * b.Height).CompareTo(a.Width * a.Height);
                return area != 0 ? area : b.Width.CompareTo(a.Width);
            });
        }

        private GameObject CreateScreen(string name, Color background)
        {
            var screen = new GameObject(name, typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(canvas.transform, false);
            Stretch(screen.GetComponent<RectTransform>());
            screen.GetComponent<Image>().color = background;
            screen.SetActive(false);
            return screen;
        }

        private Transform CreatePanel(Transform parent, float width, float height)
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.095f, 0.105f, 0.98f);

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 24);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            return panel.transform;
        }

        private Text CreateLabel(Transform parent, string value, int size, FontStyle style, float height)
        {
            var text = CreateFreeText(parent, size, style, TextAnchor.MiddleCenter);
            text.text = value;
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
            return text;
        }

        private Text CreateFreeText(Transform parent, int size, FontStyle style, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = new Color(0.92f, 0.95f, 0.96f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button CreateButton(Transform parent, string label, Action callback, float height = 54f)
        {
            return CreateButton(parent, label, callback, out _, height);
        }

        private Button CreateButton(Transform parent, string label, Action callback, out Text labelText, float height = 54f)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;

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

            labelText = CreateFreeText(go.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(labelText.rectTransform);
            labelText.rectTransform.offsetMin = new Vector2(12, 4);
            labelText.rectTransform.offsetMax = new Vector2(-12, -4);
            labelText.text = label;
            return button;
        }

        private static void AddSpacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void EnsureShellOnTop()
        {
            if (canvas == null) return;
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiSortingOrder;
            canvas.gameObject.SetActive(true);
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
                ResolutionHeight = source.ResolutionHeight,
                LanguageCode = source.LanguageCode
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
            UnityEngine.Application.Quit();
#endif
        }
    }
}
