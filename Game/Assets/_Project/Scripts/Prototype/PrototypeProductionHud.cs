using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExcelHell.Application;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Production presentation for the compact gameplay shell. The legacy sidebar remains hidden and
    /// continues owning battle-tested callbacks; this HUD mirrors its data and forwards explicit clicks.
    /// </summary>
    [DefaultExecutionOrder(1900)]
    public sealed class PrototypeProductionHud : MonoBehaviour, INarrativeEffectReceiver
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo TurnField = typeof(ExcelHellPrototype).GetField("turn", Flags);
        private static readonly FieldInfo GoalsTextField = typeof(ExcelHellPrototype).GetField("goalsText", Flags);
        private static readonly FieldInfo HelpTextField = typeof(ExcelHellPrototype).GetField("helpText", Flags);

        private readonly List<string> bossMessages = new();
        private readonly List<string> departmentMessages = new();

        private ExcelHellPrototype prototype;
        private NarrativeEventRunner runner;
        private PrototypeLevelFlow levelFlow;
        private Canvas canvas;

        private RectTransform tasksReserved;
        private RectTransform helpReserved;
        private RectTransform clockReserved;
        private RectTransform chatReserved;
        private RectTransform menuReserved;
        private RectTransform deleteReserved;

        private Text tasksButtonText;
        private Text helpButtonText;
        private Text clockText;
        private Text chatButtonText;
        private Text menuButtonText;
        private Text deleteButtonText;
        private Text goalsSource;
        private Text helpSource;
        private Button legacySubmitButton;
        private Button legacyDeleteButton;

        private GameObject tasksWindow;
        private Text tasksBody;
        private Text tasksHeader;
        private Text submitLabel;
        private GameObject helpWindow;
        private Text helpBody;
        private Text helpHeader;

        private GameObject chatBadge;
        private Text chatBadgeText;
        private GameObject chatWindow;
        private Text chatHeader;
        private Text chatBody;
        private Button bossTab;
        private Button departmentTab;
        private Text bossTabText;
        private Text departmentTabText;

        private GameObject toastRoot;
        private Text toastText;
        private Coroutine toastRoutine;

        private GameObject completionModal;
        private Text completionTitle;
        private Text completionBody;
        private Text completionButtonText;
        private Button completionButton;

        private ChatChannel activeChannel = ChatChannel.Boss;
        private int bossUnread;
        private int departmentUnread;
        private bool bound;

        private bool IsRussian => string.Equals(ExcelHellApplication.CurrentLanguageCode, "ru", StringComparison.OrdinalIgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeProductionHud>() != null) return;
            var root = new GameObject("[PRESENTATION] Production HUD");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeProductionHud>();
        }

        private void Update()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (!bound) TryBuild();

            BindNarrativeRunner();
            RefreshClock();
            RefreshMirroredContent();
            RefreshLocalizedLabels();
            RefreshCompletionState();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            if (runner != null) runner.UnregisterReceiver(this);
            prototype = owner;
            runner = null;
            levelFlow = null;
            canvas = null;
            tasksReserved = helpReserved = clockReserved = chatReserved = menuReserved = deleteReserved = null;
            tasksButtonText = helpButtonText = clockText = chatButtonText = menuButtonText = deleteButtonText = null;
            goalsSource = helpSource = null;
            legacySubmitButton = legacyDeleteButton = null;
            bound = false;
            bossMessages.Clear();
            departmentMessages.Clear();
            bossUnread = 0;
            departmentUnread = 0;
            activeChannel = ChatChannel.Boss;
            DestroyOwnedUi();

            if (prototype != null)
                canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
        }

        private void TryBuild()
        {
            if (prototype == null || canvas == null) return;

            tasksReserved = FindRect(canvas.transform, "Tasks Reserved");
            helpReserved = FindRect(canvas.transform, "Help Reserved");
            clockReserved = FindRect(canvas.transform, "Clock Reserved");
            chatReserved = FindRect(canvas.transform, "Chat Reserved");
            menuReserved = FindRect(canvas.transform, "Menu Reserved");
            deleteReserved = FindRect(canvas.transform, "Delete Reserved");
            if (tasksReserved == null || helpReserved == null || clockReserved == null || chatReserved == null ||
                menuReserved == null || deleteReserved == null) return;

            goalsSource = GoalsTextField?.GetValue(prototype) as Text;
            helpSource = HelpTextField?.GetValue(prototype) as Text;
            legacySubmitButton = FindLegacyButton("ui.submit");
            legacyDeleteButton = FindLegacyButton("ui.delete");

            SetupReservedButton(tasksReserved, ToggleTasks, out tasksButtonText);
            SetupReservedButton(helpReserved, ToggleHelp, out helpButtonText);
            SetupReservedButton(chatReserved, ToggleChat, out chatButtonText);
            SetupReservedButton(menuReserved, ExcelHellApplication.OpenGameplayMenu, out menuButtonText);
            SetupReservedButton(deleteReserved, DeleteSelection, out deleteButtonText);

            ReplacePlaceholder(clockReserved, out clockText);
            clockText.fontSize = 22;
            clockText.alignment = TextAnchor.MiddleCenter;
            clockText.color = new Color(0.75f, 0.93f, 0.83f, 1f);

            BuildChatBadge();
            BuildTasksWindow();
            BuildHelpWindow();
            BuildChatWindow();
            BuildToast();
            BuildCompletionModal();
            bound = true;

            Debug.Log("[UI-HUD] Compact topbar, task/help drawers, chat and completion presentation bound.");
        }

        private void SetupReservedButton(RectTransform rect, Action callback, out Text label)
        {
            var image = rect.GetComponent<Image>();
            if (image != null) image.raycastTarget = true;
            var button = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback());
            ReplacePlaceholder(rect, out label);
            label.raycastTarget = false;
        }

        private void BuildChatBadge()
        {
            chatBadge = new GameObject("Chat Badge", typeof(RectTransform), typeof(Image));
            chatBadge.transform.SetParent(chatReserved, false);
            SetTopLeft(chatBadge.GetComponent<RectTransform>(), 36f, -1f, 20f, 20f);
            var image = chatBadge.GetComponent<Image>();
            image.color = new Color(0.78f, 0.18f, 0.20f, 1f);
            image.raycastTarget = false;
            chatBadgeText = CreateText(chatBadge.transform, string.Empty, 11, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(chatBadgeText.rectTransform, 1f);
            chatBadgeText.color = Color.white;
            chatBadgeText.raycastTarget = false;
            chatBadge.SetActive(false);
        }

        private void BuildTasksWindow()
        {
            tasksWindow = CreateWindow("Tasks Window", 40f, -82f, 570f, 330f);
            tasksHeader = CreateText(tasksWindow.transform, string.Empty, 19, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetTopLeft(tasksHeader.rectTransform, 18f, -12f, 430f, 34f);

            var close = CreateButton(tasksWindow.transform, "×", 500f, -10f, 48f, 38f, CloseTasks);
            close.GetComponentInChildren<Text>().fontSize = 22;

            tasksBody = CreateText(tasksWindow.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft);
            SetTopLeft(tasksBody.rectTransform, 18f, -60f, 534f, 190f);
            tasksBody.color = new Color(0.92f, 0.93f, 0.95f, 1f);
            tasksBody.verticalOverflow = VerticalWrapMode.Truncate;

            var submit = CreateButton(tasksWindow.transform, string.Empty, 18f, -270f, 534f, 44f, SubmitReport);
            submitLabel = submit.GetComponentInChildren<Text>();
            tasksWindow.SetActive(false);
        }

        private void BuildHelpWindow()
        {
            helpWindow = CreateWindow("Help Window", 188f, -82f, 650f, 410f);
            helpHeader = CreateText(helpWindow.transform, string.Empty, 19, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetTopLeft(helpHeader.rectTransform, 18f, -12f, 520f, 34f);

            var close = CreateButton(helpWindow.transform, "×", 580f, -10f, 48f, 38f, CloseHelp);
            close.GetComponentInChildren<Text>().fontSize = 22;

            helpBody = CreateText(helpWindow.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.UpperLeft);
            SetTopLeft(helpBody.rectTransform, 18f, -60f, 614f, 330f);
            helpBody.color = new Color(0.92f, 0.93f, 0.95f, 1f);
            helpBody.verticalOverflow = VerticalWrapMode.Truncate;
            helpWindow.SetActive(false);
        }

        private void BuildChatWindow()
        {
            chatWindow = CreateWindow("Chat Window", 1040f, -88f, 520f, 620f);
            chatHeader = CreateText(chatWindow.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetTopLeft(chatHeader.rectTransform, 18f, -12f, 360f, 34f);

            var close = CreateButton(chatWindow.transform, "×", 450f, -10f, 48f, 38f, CloseChat);
            close.GetComponentInChildren<Text>().fontSize = 22;

            bossTab = CreateButton(chatWindow.transform, string.Empty, 18f, -60f, 228f, 44f, () => SelectChannel(ChatChannel.Boss));
            bossTabText = bossTab.GetComponentInChildren<Text>();
            departmentTab = CreateButton(chatWindow.transform, string.Empty, 256f, -60f, 246f, 44f, () => SelectChannel(ChatChannel.Department));
            departmentTabText = departmentTab.GetComponentInChildren<Text>();

            var bodyPanel = new GameObject("Chat Body", typeof(RectTransform), typeof(Image));
            bodyPanel.transform.SetParent(chatWindow.transform, false);
            SetTopLeft(bodyPanel.GetComponent<RectTransform>(), 18f, -116f, 484f, 480f);
            bodyPanel.GetComponent<Image>().color = new Color(0.15f, 0.17f, 0.20f, 1f);

            chatBody = CreateText(bodyPanel.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft);
            Stretch(chatBody.rectTransform, 16f);
            chatBody.color = new Color(0.91f, 0.925f, 0.945f, 1f);
            chatBody.verticalOverflow = VerticalWrapMode.Truncate;

            chatWindow.SetActive(false);
            RefreshChatWindow();
        }

        private void BuildToast()
        {
            toastRoot = new GameObject("Narrative Toast", typeof(RectTransform), typeof(Image));
            toastRoot.transform.SetParent(canvas.transform, false);
            SetTopLeft(toastRoot.GetComponent<RectTransform>(), 1010f, -78f, 550f, 72f);
            toastRoot.GetComponent<Image>().color = new Color(0.08f, 0.095f, 0.12f, 0.97f);
            toastText = CreateText(toastRoot.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(toastText.rectTransform, 14f);
            toastText.color = Color.white;
            toastRoot.SetActive(false);
        }

        private void BuildCompletionModal()
        {
            completionModal = CreateWindow("Completion Modal", 440f, -300f, 720f, 230f);
            completionTitle = CreateText(completionModal.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetTopLeft(completionTitle.rectTransform, 30f, -24f, 660f, 48f);
            completionBody = CreateText(completionModal.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleCenter);
            SetTopLeft(completionBody.rectTransform, 40f, -82f, 640f, 48f);
            completionButton = CreateButton(completionModal.transform, string.Empty, 190f, -156f, 340f, 48f, OnCompletionButton);
            completionButtonText = completionButton.GetComponentInChildren<Text>();
            completionModal.SetActive(false);
        }

        private GameObject CreateWindow(string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            SetTopLeft(go.GetComponent<RectTransform>(), x, y, width, height);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.105f, 0.12f, 0.145f, 0.985f);
            image.raycastTarget = true;
            return go;
        }

        private void BindNarrativeRunner()
        {
            var currentRunner = FindFirstObjectByType<NarrativeEventRunner>();
            if (currentRunner == runner) return;
            if (runner != null) runner.UnregisterReceiver(this);
            runner = currentRunner;
            if (runner != null) runner.RegisterReceiver(this);
        }

        private void RefreshClock()
        {
            if (!bound || clockText == null || prototype == null) return;
            if (TurnField?.GetValue(prototype) is not int turn) return;

            var maxTurns = Mathf.Max(1, PrototypeLevelRuntime.Current?.MaxTurns ?? 1);
            var clampedTurn = Mathf.Clamp(turn, 0, maxTurns);
            var minutes = Mathf.RoundToInt(540f * clampedTurn / maxTurns);
            var total = 9 * 60 + minutes;
            clockText.text = $"{total / 60:00}:{total % 60:00}";
        }

        private void RefreshMirroredContent()
        {
            if (!bound) return;
            if (tasksBody != null) tasksBody.text = goalsSource != null ? goalsSource.text : string.Empty;
            if (helpBody != null) helpBody.text = helpSource != null ? helpSource.text : string.Empty;
        }

        private void RefreshLocalizedLabels()
        {
            if (!bound) return;
            if (tasksButtonText != null) tasksButtonText.text = IsRussian ? "ЗАДАЧИ" : "TASKS";
            if (helpButtonText != null) helpButtonText.text = "?";
            if (chatButtonText != null) chatButtonText.text = "✉";
            if (menuButtonText != null) menuButtonText.text = IsRussian ? "МЕНЮ" : "MENU";
            if (deleteButtonText != null) deleteButtonText.text = "DEL";
            if (tasksHeader != null) tasksHeader.text = IsRussian ? "ЗАДАЧИ ОТЧЁТА" : "REPORT TASKS";
            if (submitLabel != null) submitLabel.text = IsRussian ? "ОТПРАВИТЬ ОТЧЁТ" : "SUBMIT REPORT";
            if (helpHeader != null) helpHeader.text = IsRussian ? "СПРАВКА" : "HELP";
            if (chatHeader != null) chatHeader.text = IsRussian ? "СООБЩЕНИЯ" : "MESSAGES";
            RefreshChatButton();
            RefreshChatWindow();
        }

        private void RefreshCompletionState()
        {
            if (!bound || completionModal == null) return;
            levelFlow ??= FindFirstObjectByType<PrototypeLevelFlow>();
            var accepted = levelFlow != null && levelFlow.ReportAcceptedForPresentation;
            if (!accepted)
            {
                if (completionModal.activeSelf) completionModal.SetActive(false);
                return;
            }

            if (!completionModal.activeSelf)
            {
                CloseTasks();
                CloseHelp();
                CloseChat();
                completionModal.SetActive(true);
            }

            var last = levelFlow.IsLastLevel;
            completionTitle.text = IsRussian
                ? (last ? "СМЕНА ЗАВЕРШЕНА" : "РАБОЧИЙ ДЕНЬ ЗАВЕРШЁН")
                : (last ? "SHIFT COMPLETE" : "WORKDAY COMPLETE");
            completionBody.text = IsRussian
                ? (last ? "Все отчёты сданы." : "Отчёт принят. Можно переходить к следующему заданию.")
                : (last ? "All reports submitted." : "Report accepted. Continue to the next assignment.");
            completionButtonText.text = IsRussian
                ? (last ? "МЕНЮ" : "ПРОДОЛЖИТЬ")
                : (last ? "MENU" : "CONTINUE");
        }

        private void OnCompletionButton()
        {
            if (levelFlow == null) return;
            if (levelFlow.IsLastLevel) ExcelHellApplication.OpenGameplayMenu();
            else levelFlow.AdvanceFromPresentation();
        }

        private void ToggleTasks()
        {
            if (tasksWindow == null) return;
            var show = !tasksWindow.activeSelf;
            CloseHelp();
            CloseChat();
            tasksWindow.SetActive(show);
        }

        private void CloseTasks()
        {
            if (tasksWindow != null) tasksWindow.SetActive(false);
        }

        private void ToggleHelp()
        {
            if (helpWindow == null) return;
            var show = !helpWindow.activeSelf;
            CloseTasks();
            CloseChat();
            helpWindow.SetActive(show);
        }

        private void CloseHelp()
        {
            if (helpWindow != null) helpWindow.SetActive(false);
        }

        private void SubmitReport()
        {
            CloseTasks();
            legacySubmitButton?.onClick.Invoke();
        }

        private void DeleteSelection() => legacyDeleteButton?.onClick.Invoke();

        public bool CanReceive(NarrativeEffectType type) =>
            type == NarrativeEffectType.BossChatMessage ||
            type == NarrativeEffectType.DepartmentChatMessage ||
            type == NarrativeEffectType.Toast;

        public void Receive(NarrativeEffectTicket ticket)
        {
            var effect = ticket.Request.Effect;
            switch (effect.type)
            {
                case NarrativeEffectType.BossChatMessage:
                    ReceiveChat(ChatChannel.Boss, effect.text);
                    ticket.Complete();
                    break;
                case NarrativeEffectType.DepartmentChatMessage:
                    ReceiveChat(ChatChannel.Department, effect.text);
                    ticket.Complete();
                    break;
                case NarrativeEffectType.Toast:
                    ShowToast(effect.text, effect.lifetime.duration > 0f ? effect.lifetime.duration : 2.5f);
                    ticket.Complete();
                    break;
                default:
                    ticket.Complete();
                    break;
            }
        }

        private void ReceiveChat(ChatChannel channel, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            var target = channel == ChatChannel.Boss ? bossMessages : departmentMessages;
            target.Add(message.Trim());

            var readingNow = chatWindow != null && chatWindow.activeSelf && activeChannel == channel;
            if (!readingNow)
            {
                if (channel == ChatChannel.Boss) bossUnread++;
                else departmentUnread++;
            }

            var prefix = channel == ChatChannel.Boss ? (IsRussian ? "НАЧАЛЬНИК" : "BOSS") : (IsRussian ? "ОТДЕЛ" : "DEPARTMENT");
            ShowToast($"{prefix}: {message}", 3f, () => OpenChat(channel));
            RefreshChatButton();
            RefreshChatWindow();
        }

        private void ToggleChat()
        {
            if (chatWindow == null) return;
            if (chatWindow.activeSelf) CloseChat();
            else OpenChat(activeChannel);
        }

        private void OpenChat(ChatChannel channel)
        {
            CloseTasks();
            CloseHelp();
            activeChannel = channel;
            chatWindow.SetActive(true);
            MarkCurrentRead();
            RefreshChatButton();
            RefreshChatWindow();
        }

        private void CloseChat()
        {
            if (chatWindow != null) chatWindow.SetActive(false);
            RefreshChatButton();
        }

        private void SelectChannel(ChatChannel channel)
        {
            activeChannel = channel;
            MarkCurrentRead();
            RefreshChatButton();
            RefreshChatWindow();
        }

        private void MarkCurrentRead()
        {
            if (activeChannel == ChatChannel.Boss) bossUnread = 0;
            else departmentUnread = 0;
        }

        private void RefreshChatButton()
        {
            if (chatButtonText != null) chatButtonText.text = "✉";
            var unread = bossUnread + departmentUnread;
            if (chatBadge != null) chatBadge.SetActive(unread > 0);
            if (chatBadgeText != null) chatBadgeText.text = unread > 9 ? "9+" : unread.ToString();
        }

        private void RefreshChatWindow()
        {
            if (chatWindow == null) return;
            if (bossTabText != null)
            {
                var label = IsRussian ? "НАЧАЛЬНИК" : "BOSS";
                bossTabText.text = bossUnread > 0 ? $"{label}  • {bossUnread}" : label;
            }
            if (departmentTabText != null)
            {
                var label = IsRussian ? "ОТДЕЛ" : "DEPARTMENT";
                departmentTabText.text = departmentUnread > 0 ? $"{label}  • {departmentUnread}" : label;
            }

            if (bossTab != null) bossTab.GetComponent<Image>().color = activeChannel == ChatChannel.Boss
                ? new Color(0.34f, 0.39f, 0.47f, 1f) : new Color(0.22f, 0.25f, 0.30f, 1f);
            if (departmentTab != null) departmentTab.GetComponent<Image>().color = activeChannel == ChatChannel.Department
                ? new Color(0.34f, 0.39f, 0.47f, 1f) : new Color(0.22f, 0.25f, 0.30f, 1f);

            if (chatBody == null) return;
            var source = activeChannel == ChatChannel.Boss ? bossMessages : departmentMessages;
            chatBody.text = source.Count == 0
                ? (IsRussian ? "Нет новых сообщений." : "No new messages.")
                : string.Join("\n\n", source.Select(message => $"› {message}"));
        }

        private void ShowToast(string message, float duration, Action onClick = null)
        {
            if (toastRoot == null || toastText == null || string.IsNullOrWhiteSpace(message)) return;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastText.text = message;
            toastRoot.SetActive(true);

            var button = toastRoot.GetComponent<Button>() ?? toastRoot.AddComponent<Button>();
            button.targetGraphic = toastRoot.GetComponent<Image>();
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() =>
            {
                onClick();
                HideToast();
            });
            toastRoutine = StartCoroutine(HideToastAfter(Mathf.Max(0.25f, duration)));
        }

        private IEnumerator HideToastAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            HideToast();
        }

        private void HideToast()
        {
            if (toastRoot != null) toastRoot.SetActive(false);
            toastRoutine = null;
        }

        private Button FindLegacyButton(string name) => prototype.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button => button.gameObject.name == name);

        private void DestroyOwnedUi()
        {
            if (tasksWindow != null) Destroy(tasksWindow);
            if (helpWindow != null) Destroy(helpWindow);
            if (chatWindow != null) Destroy(chatWindow);
            if (toastRoot != null) Destroy(toastRoot);
            if (completionModal != null) Destroy(completionModal);
            tasksWindow = helpWindow = chatWindow = toastRoot = completionModal = null;
            chatBadge = null;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = null;
        }

        private static RectTransform FindRect(Transform root, string objectName)
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.gameObject.name == objectName) return rect;
            return null;
        }

        private static void ReplacePlaceholder(RectTransform parent, out Text label)
        {
            label = parent.GetComponentsInChildren<Text>(true).FirstOrDefault();
            if (label != null) return;
            label = CreateText(parent, string.Empty, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 6f);
        }

        private static Button CreateButton(Transform parent, string label, float x, float y, float width, float height, Action callback)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            SetTopLeft(go.GetComponent<RectTransform>(), x, y, width, height);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.25f, 0.30f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => callback());
            var text = CreateText(go.transform, label, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 4f);
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }

        private static Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            rect.localScale = Vector3.one;
        }

        private enum ChatChannel
        {
            Boss,
            Department
        }
    }
}
