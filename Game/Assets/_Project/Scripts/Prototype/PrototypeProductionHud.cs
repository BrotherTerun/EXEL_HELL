using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Production HUD built on top of the final UI shell. Owns presentation only:
    /// workday clock, read-only boss/department chat, unread indicators and transient toasts.
    /// </summary>
    [DefaultExecutionOrder(1900)]
    public sealed class PrototypeProductionHud : MonoBehaviour, INarrativeEffectReceiver
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo TurnField = typeof(ExcelHellPrototype).GetField("turn", Flags);

        private readonly List<string> bossMessages = new();
        private readonly List<string> departmentMessages = new();

        private ExcelHellPrototype prototype;
        private NarrativeEventRunner runner;
        private Canvas canvas;
        private RectTransform clockReserved;
        private RectTransform chatReserved;
        private Text clockText;
        private Text chatButtonText;
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
        private ChatChannel activeChannel = ChatChannel.Boss;
        private int bossUnread;
        private int departmentUnread;
        private bool bound;

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
        }

        private void Bind(ExcelHellPrototype owner)
        {
            if (runner != null) runner.UnregisterReceiver(this);
            prototype = owner;
            runner = null;
            canvas = null;
            clockReserved = null;
            chatReserved = null;
            clockText = null;
            chatButtonText = null;
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

            clockReserved = FindRect(canvas.transform, "Clock Reserved");
            chatReserved = FindRect(canvas.transform, "Chat Reserved");
            if (clockReserved == null || chatReserved == null) return;

            ReplacePlaceholder(clockReserved, out clockText);
            clockText.text = "09:00";
            clockText.fontSize = 28;
            clockText.alignment = TextAnchor.MiddleCenter;
            clockText.color = new Color(0.75f, 0.93f, 0.83f, 1f);

            BuildChatButton();
            BuildChatWindow();
            BuildToast();
            HideLegacyTurnCounter();
            bound = true;

            Debug.Log("[UI-HUD] Workday clock + chat shell bound.");
        }

        private void BuildChatButton()
        {
            foreach (var image in chatReserved.GetComponents<Image>()) image.raycastTarget = true;
            var button = chatReserved.GetComponent<Button>() ?? chatReserved.gameObject.AddComponent<Button>();
            button.targetGraphic = chatReserved.GetComponent<Image>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ToggleChat);

            ReplacePlaceholder(chatReserved, out chatButtonText);
            chatButtonText.fontSize = 15;
            chatButtonText.alignment = TextAnchor.MiddleCenter;
            RefreshChatButton();
        }

        private void BuildChatWindow()
        {
            chatWindow = new GameObject("Chat Window", typeof(RectTransform), typeof(Image));
            chatWindow.transform.SetParent(canvas.transform, false);
            var rect = chatWindow.GetComponent<RectTransform>();
            SetTopLeft(rect, 996f, -274f, 548f, 516f);
            chatWindow.GetComponent<Image>().color = new Color(0.105f, 0.12f, 0.145f, 0.985f);

            chatHeader = CreateText(chatWindow.transform, "СООБЩЕНИЯ", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetTopLeft(chatHeader.rectTransform, 20f, -14f, 360f, 34f);
            chatHeader.color = Color.white;

            var close = CreateButton(chatWindow.transform, "×", 474f, -10f, 52f, 40f, CloseChat);
            var closeLabel = close.GetComponentInChildren<Text>();
            if (closeLabel != null) closeLabel.fontSize = 24;

            bossTab = CreateButton(chatWindow.transform, string.Empty, 18f, -62f, 242f, 44f, () => SelectChannel(ChatChannel.Boss));
            bossTabText = bossTab.GetComponentInChildren<Text>();
            departmentTab = CreateButton(chatWindow.transform, string.Empty, 270f, -62f, 258f, 44f, () => SelectChannel(ChatChannel.Department));
            departmentTabText = departmentTab.GetComponentInChildren<Text>();

            var bodyPanel = new GameObject("Chat Body", typeof(RectTransform), typeof(Image));
            bodyPanel.transform.SetParent(chatWindow.transform, false);
            SetTopLeft(bodyPanel.GetComponent<RectTransform>(), 18f, -118f, 510f, 376f);
            bodyPanel.GetComponent<Image>().color = new Color(0.15f, 0.17f, 0.20f, 1f);

            chatBody = CreateText(bodyPanel.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft);
            Stretch(chatBody.rectTransform, 16f);
            chatBody.color = new Color(0.91f, 0.925f, 0.945f, 1f);
            chatBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            chatBody.verticalOverflow = VerticalWrapMode.Truncate;

            chatWindow.SetActive(false);
            RefreshChatWindow();
        }

        private void BuildToast()
        {
            toastRoot = new GameObject("Narrative Toast", typeof(RectTransform), typeof(Image));
            toastRoot.transform.SetParent(canvas.transform, false);
            SetTopLeft(toastRoot.GetComponent<RectTransform>(), 1038f, -210f, 464f, 72f);
            toastRoot.GetComponent<Image>().color = new Color(0.08f, 0.095f, 0.12f, 0.97f);

            toastText = CreateText(toastRoot.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(toastText.rectTransform, 14f);
            toastText.color = Color.white;
            toastRoot.SetActive(false);
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
            var hour = total / 60;
            var minute = total % 60;
            clockText.text = $"{hour:00}:{minute:00}";
        }

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

            var prefix = channel == ChatChannel.Boss ? "НАЧАЛЬНИК" : "ОТДЕЛ";
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
            activeChannel = channel;
            if (chatWindow != null) chatWindow.SetActive(true);
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
            if (chatButtonText == null) return;
            var unread = bossUnread + departmentUnread;
            chatButtonText.text = unread > 0 ? $"СООБЩЕНИЯ  • {unread}" : "СООБЩЕНИЯ";
        }

        private void RefreshChatWindow()
        {
            if (chatWindow == null) return;
            if (bossTabText != null) bossTabText.text = bossUnread > 0 ? $"НАЧАЛЬНИК  • {bossUnread}" : "НАЧАЛЬНИК";
            if (departmentTabText != null) departmentTabText.text = departmentUnread > 0 ? $"ОТДЕЛ  • {departmentUnread}" : "ОТДЕЛ";

            if (bossTab != null) bossTab.GetComponent<Image>().color = activeChannel == ChatChannel.Boss
                ? new Color(0.34f, 0.39f, 0.47f, 1f) : new Color(0.22f, 0.25f, 0.30f, 1f);
            if (departmentTab != null) departmentTab.GetComponent<Image>().color = activeChannel == ChatChannel.Department
                ? new Color(0.34f, 0.39f, 0.47f, 1f) : new Color(0.22f, 0.25f, 0.30f, 1f);

            if (chatBody == null) return;
            var source = activeChannel == ChatChannel.Boss ? bossMessages : departmentMessages;
            chatBody.text = source.Count == 0
                ? "Нет новых сообщений."
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

        private void HideLegacyTurnCounter()
        {
            // The old turn counter remains mechanically owned by gameplay but is no longer player-facing.
            if (canvas == null) return;
            foreach (var text in canvas.GetComponentsInChildren<Text>(true))
            {
                if (text == clockText || text == chatButtonText) continue;
                if (text.fontSize == 20 && text.rectTransform.anchoredPosition.x > 1000f)
                {
                    text.gameObject.SetActive(false);
                    break;
                }
            }
        }

        private void DestroyOwnedUi()
        {
            if (chatWindow != null) Destroy(chatWindow);
            if (toastRoot != null) Destroy(toastRoot);
            chatWindow = null;
            toastRoot = null;
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
            Stretch(label.rectTransform, 8f);
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
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private enum ChatChannel
        {
            Boss,
            Department
        }
    }
}
