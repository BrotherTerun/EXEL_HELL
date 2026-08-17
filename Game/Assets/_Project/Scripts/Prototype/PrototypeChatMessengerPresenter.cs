using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Presentation-only messenger skin for ProductionHud chat.
    /// Keeps the existing history/unread/channel/narrative machinery intact and replaces only the chat visuals:
    /// angular message bubbles, sender + timestamp hierarchy, Inter body typography and an authored chat icon.
    /// </summary>
    [DefaultExecutionOrder(2185)]
    public sealed class PrototypeChatMessengerPresenter : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly Color WindowBg = new(0.055f, 0.070f, 0.090f, 0.992f);
        private static readonly Color ViewportBg = new(0.075f, 0.090f, 0.112f, 1f);
        private static readonly Color Chrome = new(0.115f, 0.140f, 0.175f, 1f);
        private static readonly Color ChromeActive = new(0.170f, 0.220f, 0.270f, 1f);
        private static readonly Color Border = new(0.31f, 0.43f, 0.51f, 0.68f);
        private static readonly Color TextMain = new(0.91f, 0.93f, 0.95f, 1f);
        private static readonly Color TextMuted = new(0.49f, 0.58f, 0.66f, 1f);
        private static readonly Color Cyan = new(0.01f, 0.92f, 0.92f, 1f);
        private static readonly Color DepartmentBubble = new(0.095f, 0.145f, 0.165f, 0.985f);
        private static readonly Color DepartmentEdge = new(0.12f, 0.47f, 0.49f, 0.82f);
        private static readonly Color BossBubble = new(0.155f, 0.135f, 0.165f, 0.985f);
        private static readonly Color BossEdge = new(0.55f, 0.31f, 0.39f, 0.82f);
        private static readonly Color BossName = new(0.91f, 0.64f, 0.70f, 1f);
        private static readonly Color DeptName = new(0.39f, 0.84f, 0.84f, 1f);

        private PrototypeProductionHud hud;
        private FieldInfo chatWindowField;
        private FieldInfo chatReservedField;
        private FieldInfo chatButtonTextField;
        private FieldInfo bossMessagesField;
        private FieldInfo departmentMessagesField;
        private FieldInfo activeChannelField;
        private FieldInfo bossTabField;
        private FieldInfo departmentTabField;
        private FieldInfo chatHeaderField;

        private GameObject boundWindow;
        private RectTransform chatReserved;
        private GameObject messengerViewport;
        private RectTransform messengerContent;
        private ScrollRect messengerScroll;
        private Image chatIcon;
        private Sprite chatIconSprite;
        private Font interFont;
        private Font monoFont;

        private string lastChannel = string.Empty;
        private uint lastSignature;
        private bool wasOpen;
        private float nextRefreshAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeChatMessengerPresenter>() != null) return;
            var root = new GameObject("[PRESENTATION] Chat Messenger");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeChatMessengerPresenter>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;

            var currentHud = FindFirstObjectByType<PrototypeProductionHud>();
            if (currentHud != hud)
                BindHud(currentHud);
            if (hud == null) return;

            var currentWindow = chatWindowField?.GetValue(hud) as GameObject;
            if (currentWindow != boundWindow)
                BindWindow(currentWindow);
            if (boundWindow == null) return;

            MaintainButtonIcon();
            MaintainWindowSkin();

            var open = boundWindow.activeSelf;
            if (Time.unscaledTime >= nextRefreshAt || open != wasOpen)
            {
                nextRefreshAt = Time.unscaledTime + 0.12f;
                RefreshConversation(open && !wasOpen);
            }
            wasOpen = open;
        }

        private void BindHud(PrototypeProductionHud owner)
        {
            hud = owner;
            boundWindow = null;
            chatReserved = null;
            messengerViewport = null;
            messengerContent = null;
            messengerScroll = null;
            chatIcon = null;
            chatIconSprite = null;
            lastChannel = string.Empty;
            lastSignature = 0u;
            wasOpen = false;

            chatWindowField = chatReservedField = chatButtonTextField = null;
            bossMessagesField = departmentMessagesField = activeChannelField = null;
            bossTabField = departmentTabField = chatHeaderField = null;

            if (hud == null) return;

            var type = typeof(PrototypeProductionHud);
            chatWindowField = type.GetField("chatWindow", PrivateInstance);
            chatReservedField = type.GetField("chatReserved", PrivateInstance);
            chatButtonTextField = type.GetField("chatButtonText", PrivateInstance);
            bossMessagesField = type.GetField("bossMessages", PrivateInstance);
            departmentMessagesField = type.GetField("departmentMessages", PrivateInstance);
            activeChannelField = type.GetField("activeChannel", PrivateInstance);
            bossTabField = type.GetField("bossTab", PrivateInstance);
            departmentTabField = type.GetField("departmentTab", PrivateInstance);
            chatHeaderField = type.GetField("chatHeader", PrivateInstance);

            interFont = Resources.Load<Font>("Fonts/Inter-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            monoFont = Resources.Load<Font>("Fonts/IBMPlexMono-Regular") ?? interFont;
        }

        private void BindWindow(GameObject window)
        {
            boundWindow = window;
            messengerViewport = null;
            messengerContent = null;
            messengerScroll = null;
            lastChannel = string.Empty;
            lastSignature = 0u;
            wasOpen = false;

            chatReserved = chatReservedField?.GetValue(hud) as RectTransform;
            if (boundWindow == null) return;

            // Hide the old single Text history. ProductionHud still writes to it, but it is no longer rendered.
            var legacyBody = boundWindow.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect != null && rect.gameObject.name == "Chat Body");
            if (legacyBody != null)
                legacyBody.gameObject.SetActive(false);

            BuildMessengerView();
            BuildButtonIcon();
            MaintainWindowSkin();
            Debug.Log("[CHAT/UI] Messenger presentation bound: bubbles + Inter + authored icon.");
        }

        private void BuildMessengerView()
        {
            if (boundWindow == null) return;

            var viewport = new GameObject("Messenger View", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(boundWindow.transform, false);
            SetTopLeft(viewport.GetComponent<RectTransform>(), 16f, -116f, 488f, 486f);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = ViewportBg;
            viewportImage.raycastTarget = true;
            AddOutline(viewport, new Color(Border.r, Border.g, Border.b, 0.42f), new Vector2(1f, -1f));

            var content = new GameObject("Messenger Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            messengerContent = content.GetComponent<RectTransform>();
            messengerContent.anchorMin = new Vector2(0f, 1f);
            messengerContent.anchorMax = new Vector2(1f, 1f);
            messengerContent.pivot = new Vector2(0.5f, 1f);
            messengerContent.anchoredPosition = Vector2.zero;
            messengerContent.sizeDelta = Vector2.zero;

            var vertical = content.GetComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(12, 12, 12, 14);
            vertical.spacing = 9f;
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            messengerScroll = viewport.GetComponent<ScrollRect>();
            messengerScroll.viewport = viewport.GetComponent<RectTransform>();
            messengerScroll.content = messengerContent;
            messengerScroll.horizontal = false;
            messengerScroll.vertical = true;
            messengerScroll.movementType = ScrollRect.MovementType.Clamped;
            messengerScroll.scrollSensitivity = 32f;

            messengerViewport = viewport;
        }

        private void BuildButtonIcon()
        {
            if (chatReserved == null) return;

            var existing = chatReserved.Find("Chat Messenger Icon");
            if (existing != null)
            {
                chatIcon = existing.GetComponent<Image>();
                return;
            }

            var icon = new GameObject("Chat Messenger Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(chatReserved, false);
            var rect = icon.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(30f, 30f);

            chatIcon = icon.GetComponent<Image>();
            chatIcon.raycastTarget = false;
            chatIcon.preserveAspect = true;
            chatIcon.color = new Color(0.78f, 0.94f, 0.94f, 0.96f);
            chatIcon.sprite = LoadChatIconSprite();
        }

        private Sprite LoadChatIconSprite()
        {
            if (chatIconSprite != null) return chatIconSprite;
            var atlas = Resources.Load<Texture2D>("Art/icons");
            if (atlas == null || atlas.width < 4 || atlas.height < 4) return null;

            // Authored icon sheet is a 2x2 square atlas. The speech/message glyph is the lower-left tile.
            // FullRect avoids any dependency on readable texture pixels.
            var halfW = atlas.width * 0.5f;
            var halfH = atlas.height * 0.5f;
            var inset = Mathf.Min(atlas.width, atlas.height) * 0.018f;
            var rect = new Rect(inset, inset, halfW - inset * 2f, halfH - inset * 2f);
            chatIconSprite = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            chatIconSprite.name = "Chat Icon Runtime Crop";
            return chatIconSprite;
        }

        private void MaintainButtonIcon()
        {
            chatReserved = chatReservedField?.GetValue(hud) as RectTransform;
            if (chatReserved == null) return;

            var legacyLabel = chatButtonTextField?.GetValue(hud) as Text;
            if (legacyLabel != null)
                legacyLabel.enabled = false;

            if (chatIcon == null || chatIcon.transform.parent != chatReserved)
                BuildButtonIcon();
            if (chatIcon != null)
            {
                chatIcon.enabled = true;
                chatIcon.sprite ??= LoadChatIconSprite();
                chatIcon.color = boundWindow != null && boundWindow.activeSelf
                    ? Cyan
                    : new Color(0.78f, 0.94f, 0.94f, 0.96f);
            }
        }

        private void MaintainWindowSkin()
        {
            if (boundWindow == null) return;

            var windowRect = boundWindow.GetComponent<RectTransform>();
            if (windowRect != null)
                SetTopLeft(windowRect, 1000f, -84f, 560f, 630f);

            var windowImage = boundWindow.GetComponent<Image>();
            if (windowImage != null)
                windowImage.color = WindowBg;
            EnsureOutline(boundWindow, Border, new Vector2(2f, -2f));

            var header = chatHeaderField?.GetValue(hud) as Text;
            if (header != null)
            {
                header.font = interFont;
                header.fontSize = 20;
                header.fontStyle = FontStyle.Bold;
                header.color = TextMain;
            }

            var channel = CurrentChannel();
            RestyleTab(bossTabField?.GetValue(hud) as Button, channel == "Boss");
            RestyleTab(departmentTabField?.GetValue(hud) as Button, channel == "Department");

            foreach (var button in boundWindow.GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;
                var label = button.GetComponentInChildren<Text>(true);
                if (label == null) continue;
                label.font = interFont;
                if (label.text == "×")
                {
                    label.fontSize = 20;
                    label.color = TextMuted;
                    var img = button.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.09f, 0.11f, 0.14f, 1f);
                }
            }
        }

        private void RestyleTab(Button button, bool active)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = active ? ChromeActive : Chrome;
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.font = interFont;
                text.fontSize = 13;
                text.fontStyle = FontStyle.Bold;
                text.color = active ? TextMain : TextMuted;
            }
        }

        private void RefreshConversation(bool forceBottom)
        {
            if (messengerContent == null || hud == null) return;

            var channel = CurrentChannel();
            var source = CurrentMessages(channel);
            var signature = ComputeSignature(source, channel);
            if (!forceBottom && string.Equals(channel, lastChannel, StringComparison.Ordinal) && signature == lastSignature)
                return;

            lastChannel = channel;
            lastSignature = signature;
            RebuildConversation(source, channel == "Boss");

            if (messengerScroll != null && (forceBottom || boundWindow.activeSelf))
                StartCoroutine(SnapBottomNextFrame());
        }

        private string CurrentChannel()
        {
            var value = activeChannelField?.GetValue(hud);
            return value?.ToString() ?? "Boss";
        }

        private IList CurrentMessages(string channel)
        {
            var field = channel == "Department" ? departmentMessagesField : bossMessagesField;
            return field?.GetValue(hud) as IList;
        }

        private void RebuildConversation(IList source, bool bossChannel)
        {
            for (var i = messengerContent.childCount - 1; i >= 0; i--)
                Destroy(messengerContent.GetChild(i).gameObject);

            if (source == null || source.Count == 0)
            {
                CreateEmptyState();
                return;
            }

            string lastLevel = null;
            foreach (var entry in source)
            {
                if (entry == null) continue;
                var view = ReadEntry(entry);
                if (!string.Equals(lastLevel, view.LevelId, StringComparison.OrdinalIgnoreCase))
                {
                    CreateDayDivider(view.Day);
                    lastLevel = view.LevelId;
                }
                CreateMessageBubble(view, bossChannel);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(messengerContent);
        }

        private void CreateEmptyState()
        {
            var row = new GameObject("Empty Chat", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(messengerContent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 90f;
            var text = CreateText(row.transform,
                IsRussian() ? "Здесь пока тихо." : "Nothing here yet.",
                14, FontStyle.Normal, TextAnchor.MiddleCenter, interFont, TextMuted);
            Stretch(text.rectTransform, 8f);
        }

        private void CreateDayDivider(int day)
        {
            var row = new GameObject("Day Divider", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(messengerContent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 30f;

            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(row.transform, false);
            var lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.08f, 0.5f);
            lineRect.anchorMax = new Vector2(0.92f, 0.5f);
            lineRect.sizeDelta = new Vector2(0f, 1f);
            line.GetComponent<Image>().color = new Color(Border.r, Border.g, Border.b, 0.28f);
            line.GetComponent<Image>().raycastTarget = false;

            var label = CreateText(row.transform,
                IsRussian() ? $"ДЕНЬ {day}" : $"DAY {day}",
                11, FontStyle.Bold, TextAnchor.MiddleCenter, monoFont, TextMuted);
            label.rectTransform.anchorMin = new Vector2(0.38f, 0f);
            label.rectTransform.anchorMax = new Vector2(0.62f, 1f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateMessageBubble(ChatEntryView entry, bool bossChannel)
        {
            const float contentWidth = 464f;
            var bubbleWidth = bossChannel ? 394f : 382f;
            var bodyWidth = bubbleWidth - 24f;

            var row = new GameObject("Message Row", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(messengerContent, false);

            var bubble = new GameObject("Message Bubble", typeof(RectTransform), typeof(Image));
            bubble.transform.SetParent(row.transform, false);
            var bubbleRect = bubble.GetComponent<RectTransform>();
            bubbleRect.anchorMin = bubbleRect.anchorMax = bossChannel ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            bubbleRect.pivot = bossChannel ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            bubbleRect.anchoredPosition = bossChannel ? new Vector2(-2f, 0f) : new Vector2(2f, 0f);

            var bubbleImage = bubble.GetComponent<Image>();
            bubbleImage.color = bossChannel ? BossBubble : DepartmentBubble;
            bubbleImage.raycastTarget = false;
            AddOutline(bubble, bossChannel ? BossEdge : DepartmentEdge, new Vector2(1f, -1f));

            var sender = CreateText(bubble.transform, entry.Sender, 12, FontStyle.Bold, TextAnchor.UpperLeft,
                interFont, bossChannel ? BossName : DeptName);
            SetTopLeft(sender.rectTransform, 12f, -9f, bubbleWidth - 96f, 18f);

            var time = CreateText(bubble.transform, entry.Time, 10, FontStyle.Normal, TextAnchor.UpperRight,
                monoFont, TextMuted);
            SetTopLeft(time.rectTransform, bubbleWidth - 78f, -10f, 66f, 17f);

            var body = CreateText(bubble.transform, entry.Message, 15, FontStyle.Normal, TextAnchor.UpperLeft,
                interFont, TextMain);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            SetTopLeft(body.rectTransform, 12f, -32f, bodyWidth, 500f);

            var bodyHeight = MeasureTextHeight(body, entry.Message, bodyWidth);
            body.rectTransform.sizeDelta = new Vector2(bodyWidth, bodyHeight);
            var bubbleHeight = Mathf.Max(64f, bodyHeight + 48f);
            bubbleRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
            row.GetComponent<LayoutElement>().preferredHeight = bubbleHeight + 2f;

            var accent = new GameObject("Message Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(bubble.transform, false);
            var accentRect = accent.GetComponent<RectTransform>();
            if (bossChannel)
            {
                accentRect.anchorMin = new Vector2(1f, 0f);
                accentRect.anchorMax = new Vector2(1f, 1f);
                accentRect.pivot = new Vector2(1f, 0.5f);
                accentRect.anchoredPosition = Vector2.zero;
                accentRect.sizeDelta = new Vector2(3f, 0f);
            }
            else
            {
                accentRect.anchorMin = new Vector2(0f, 0f);
                accentRect.anchorMax = new Vector2(0f, 1f);
                accentRect.pivot = new Vector2(0f, 0.5f);
                accentRect.anchoredPosition = Vector2.zero;
                accentRect.sizeDelta = new Vector2(3f, 0f);
            }
            accent.GetComponent<Image>().color = bossChannel ? BossName : DeptName;
            accent.GetComponent<Image>().raycastTarget = false;

            // Small square notch keeps the messenger silhouette without introducing rounded/mobile UI styling.
            var notch = new GameObject("Bubble Notch", typeof(RectTransform), typeof(Image));
            notch.transform.SetParent(bubble.transform, false);
            var notchRect = notch.GetComponent<RectTransform>();
            notchRect.anchorMin = notchRect.anchorMax = bossChannel ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            notchRect.pivot = bossChannel ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            notchRect.anchoredPosition = bossChannel ? new Vector2(4f, 7f) : new Vector2(-4f, 7f);
            notchRect.sizeDelta = new Vector2(8f, 8f);
            notch.GetComponent<Image>().color = bossChannel ? BossBubble : DepartmentBubble;
            notch.GetComponent<Image>().raycastTarget = false;

            // Keep row width deterministic even before the layout system's first pass.
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(contentWidth, bubbleHeight + 2f);
        }

        private static float MeasureTextHeight(Text text, string value, float width)
        {
            if (text == null) return 24f;
            var settings = text.GetGenerationSettings(new Vector2(width, 1000f));
            settings.updateBounds = true;
            var raw = text.cachedTextGeneratorForLayout.GetPreferredHeight(value ?? string.Empty, settings);
            return Mathf.Max(22f, raw / Mathf.Max(0.001f, text.pixelsPerUnit));
        }

        private IEnumerator SnapBottomNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (messengerContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(messengerContent);
            if (messengerScroll != null)
                messengerScroll.verticalNormalizedPosition = 0f;
        }

        private uint ComputeSignature(IList source, string channel)
        {
            unchecked
            {
                uint hash = StableHash(channel + ":" + (IsRussian() ? "ru" : "en"));
                if (source == null) return hash;
                foreach (var entry in source)
                {
                    if (entry == null) continue;
                    var view = ReadEntry(entry);
                    hash = Mix(hash, view.LevelId);
                    hash = Mix(hash, view.Time);
                    hash = Mix(hash, view.Sender);
                    hash = Mix(hash, view.Message);
                }
                return hash;
            }
        }

        private static ChatEntryView ReadEntry(object entry)
        {
            var type = entry.GetType();
            return new ChatEntryView
            {
                LevelId = ReadString(type, entry, "LevelId"),
                Day = ReadInt(type, entry, "Day"),
                Time = ReadString(type, entry, "Time"),
                Sender = ReadString(type, entry, "Sender"),
                Message = ReadString(type, entry, "Message")
            };
        }

        private static string ReadString(Type type, object target, string field) =>
            type.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target) as string ?? string.Empty;

        private static int ReadInt(Type type, object target, string field) =>
            type.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target) is int value ? value : 1;

        private static bool IsRussian() =>
            string.Equals(ExcelHell.Application.ExcelHellApplication.CurrentLanguageCode, "ru", StringComparison.OrdinalIgnoreCase);

        private static Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor anchor,
            Font font, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value ?? string.Empty;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            if (go == null) return;
            var outline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = false;
        }

        private static void EnsureOutline(GameObject go, Color color, Vector2 distance)
        {
            if (go == null) return;
            var outline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = false;
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

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (var c in value ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }
                return hash == 0u ? 1u : hash;
            }
        }

        private static uint Mix(uint hash, string value)
        {
            unchecked
            {
                foreach (var c in value ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        private sealed class ChatEntryView
        {
            public string LevelId;
            public int Day;
            public string Time;
            public string Sender;
            public string Message;
        }
    }
}
