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
    /// Final release-safe polish for the messenger chat.
    /// - Replaces the unsafe atlas crop with a clean in-theme chat glyph built from UI primitives.
    /// - Prevents accidental consecutive duplicate senders unless the two messages are explicitly one authored thread.
    /// - Adds restrained, readable L3/L4 chat glitches without touching narrative data or gameplay.
    /// </summary>
    [DefaultExecutionOrder(2235)]
    public sealed class PrototypeChatFinalPolish : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Color Cyan = new(0.01f, 0.92f, 0.92f, 1f);
        private static readonly Color Magenta = new(0.97f, 0.11f, 0.95f, 1f);
        private static readonly Color Dim = new(0.08f, 0.13f, 0.16f, 1f);
        private static readonly string[] NamesRu = { "Ирина", "Макс", "Лена", "Антон", "Вика", "Денис", "Катя", "Саша" };
        private static readonly string[] NamesEn = { "Ira", "Max", "Lena", "Anton", "Vika", "Denis", "Katya", "Sasha" };

        private PrototypeProductionHud hud;
        private FieldInfo chatReservedField;
        private FieldInfo departmentMessagesField;
        private FieldInfo chatWindowField;
        private RectTransform chatReserved;
        private GameObject cleanIconRoot;
        private GameObject messengerWindow;
        private float nextSenderPass;
        private float nextGlitchAt;
        private bool glitchRunning;
        private uint rng = 0xC0FFEEu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeChatFinalPolish>() != null) return;
            var root = new GameObject("[PRESENTATION] Chat Final Polish");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeChatFinalPolish>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;

            var current = FindFirstObjectByType<PrototypeProductionHud>();
            if (current != hud) Bind(current);
            if (hud == null) return;

            chatReserved = chatReservedField?.GetValue(hud) as RectTransform;
            messengerWindow = chatWindowField?.GetValue(hud) as GameObject;
            EnsureCleanIcon();

            if (Time.unscaledTime >= nextSenderPass)
            {
                nextSenderPass = Time.unscaledTime + 0.10f;
                FixAccidentalDuplicateSenders();
            }

            var day = CurrentDay();
            if (day < 3 || messengerWindow == null || !messengerWindow.activeSelf || glitchRunning) return;
            if (Time.unscaledTime < nextGlitchAt) return;

            var bubbles = messengerWindow.GetComponentsInChildren<RectTransform>(true)
                .Where(r => r != null && r.gameObject.name == "Message Bubble")
                .ToList();
            if (bubbles.Count == 0)
            {
                ScheduleNextGlitch(day);
                return;
            }

            rng = NextHash(rng);
            var bubble = bubbles[(int)(rng % (uint)bubbles.Count)];
            StartCoroutine(GlitchBubble(bubble, day));
            ScheduleNextGlitch(day);
        }

        private void Bind(PrototypeProductionHud owner)
        {
            hud = owner;
            chatReserved = null;
            messengerWindow = null;
            cleanIconRoot = null;
            nextSenderPass = 0f;
            glitchRunning = false;
            rng = StableHash((PrototypeLevelRuntime.Current?.Id ?? "runtime") + ":chat-final");
            ScheduleNextGlitch(CurrentDay());

            chatReservedField = departmentMessagesField = chatWindowField = null;
            if (hud == null) return;

            var type = typeof(PrototypeProductionHud);
            chatReservedField = type.GetField("chatReserved", PrivateInstance);
            departmentMessagesField = type.GetField("departmentMessages", PrivateInstance);
            chatWindowField = type.GetField("chatWindow", PrivateInstance);
        }

        private void EnsureCleanIcon()
        {
            if (chatReserved == null) return;

            var bad = chatReserved.Find("Chat Messenger Icon");
            if (bad != null) bad.gameObject.SetActive(false);

            if (cleanIconRoot != null && cleanIconRoot.transform.parent == chatReserved)
            {
                TintIcon(messengerWindow != null && messengerWindow.activeSelf ? Cyan : new Color(0.78f, 0.94f, 0.94f, 0.96f));
                return;
            }

            var existing = chatReserved.Find("Chat Clean Icon");
            if (existing != null)
            {
                cleanIconRoot = existing.gameObject;
                TintIcon(new Color(0.78f, 0.94f, 0.94f, 0.96f));
                return;
            }

            cleanIconRoot = new GameObject("Chat Clean Icon", typeof(RectTransform));
            cleanIconRoot.transform.SetParent(chatReserved, false);
            var rootRect = cleanIconRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(30f, 30f);

            var bubble = CreateImage(cleanIconRoot.transform, "Bubble", new Vector2(0f, 2f), new Vector2(24f, 17f), Dim);
            var outline = bubble.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.88f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;

            var tail = CreateImage(cleanIconRoot.transform, "Tail", new Vector2(-7f, -7f), new Vector2(7f, 7f), Dim);
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var tailOutline = tail.gameObject.AddComponent<Outline>();
            tailOutline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.88f);
            tailOutline.effectDistance = new Vector2(1.2f, -1.2f);
            tailOutline.useGraphicAlpha = false;

            for (var i = -1; i <= 1; i++)
            {
                var dot = CreateImage(cleanIconRoot.transform, "Dot", new Vector2(i * 6f, 3f), new Vector2(3f, 3f), Cyan);
                dot.raycastTarget = false;
            }

            TintIcon(new Color(0.78f, 0.94f, 0.94f, 0.96f));
        }

        private void TintIcon(Color color)
        {
            if (cleanIconRoot == null) return;
            foreach (var image in cleanIconRoot.GetComponentsInChildren<Image>(true))
            {
                if (image == null) continue;
                if (image.gameObject.name == "Dot") image.color = color;
            }
            foreach (var outline in cleanIconRoot.GetComponentsInChildren<Outline>(true))
            {
                if (outline == null) continue;
                var c = color;
                c.a = 0.90f;
                outline.effectColor = c;
            }
        }

        private void FixAccidentalDuplicateSenders()
        {
            if (departmentMessagesField?.GetValue(hud) is not IList list || list.Count < 2) return;
            var names = IsRussian() ? NamesRu : NamesEn;

            string previousSender = null;
            string previousGroup = null;

            for (var i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry == null) continue;
                var type = entry.GetType();
                var senderField = type.GetField("Sender", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var messageField = type.GetField("Message", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (senderField == null || messageField == null) return;

                var sender = senderField.GetValue(entry) as string ?? string.Empty;
                var message = messageField.GetValue(entry) as string ?? string.Empty;
                var group = SpeakerGroup(message);
                var sameAuthoredThread = group != null && previousGroup != null && group == previousGroup;

                if (i > 0 && !sameAuthoredThread && sender == previousSender)
                {
                    var currentIndex = Array.IndexOf(names, sender);
                    if (currentIndex < 0) currentIndex = (int)(StableHash(message) % (uint)names.Length);
                    sender = names[(currentIndex + 1 + (i % (names.Length - 1))) % names.Length];
                    senderField.SetValue(entry, sender);
                }

                previousSender = sender;
                previousGroup = group;
            }
        }

        private static string SpeakerGroup(string message)
        {
            if (message == "А Морозов сегодня будет?" || message == "Я ему написал, пока не отвечает.")
                return "morozov-worried";
            if (message == "Я всё-таки не понимаю, где Морозов." ||
                message == "Он же сидит через два стола от меня." ||
                message == "Есть. Он в прошлом месяце мне смену закрывал." ||
                message == "Не нашёл.")
                return "morozov-remembers";
            if (message == "Там никто не сидит." || message == "Не было никакого Морозова.")
                return "morozov-denies";
            if (message == "Нашёл письмо от Морозова." ||
                message == "Обычное. Он мне таблицу присылал." ||
                message == "Не могу открыть.")
                return "morozov-mail";
            if (message == "Какое ещё письмо?" || message == "А отправитель кто?" || message == "...")
                return "morozov-mail-reply";
            return null;
        }

        private IEnumerator GlitchBubble(RectTransform bubble, int day)
        {
            if (bubble == null) yield break;
            glitchRunning = true;

            var basePosition = bubble.anchoredPosition;
            var texts = bubble.GetComponentsInChildren<Text>(true);
            var originalColors = texts.Select(t => t != null ? t.color : Color.white).ToArray();
            var duration = day >= 4 ? 0.22f : 0.13f;
            var amplitude = day >= 4 ? 4.0f : 2.0f;

            var slice = new GameObject("Chat Glitch Slice", typeof(RectTransform), typeof(Image));
            slice.transform.SetParent(bubble, false);
            var sliceRect = slice.GetComponent<RectTransform>();
            sliceRect.anchorMin = new Vector2(0f, 0.5f);
            sliceRect.anchorMax = new Vector2(1f, 0.5f);
            sliceRect.pivot = new Vector2(0.5f, 0.5f);
            sliceRect.anchoredPosition = new Vector2(0f, day >= 4 ? 4f : -2f);
            sliceRect.sizeDelta = new Vector2(0f, day >= 4 ? 3f : 2f);
            var sliceImage = slice.GetComponent<Image>();
            var sliceColor = day >= 4 ? Magenta : Cyan;
            sliceColor.a = day >= 4 ? 0.42f : 0.28f;
            sliceImage.color = sliceColor;
            sliceImage.raycastTarget = false;

            var start = Time.unscaledTime;
            while (bubble != null && Time.unscaledTime - start < duration)
            {
                var t = Time.unscaledTime - start;
                bubble.anchoredPosition = basePosition + new Vector2(Mathf.Sin(t * 80f) * amplitude, 0f);

                for (var i = 0; i < texts.Length; i++)
                {
                    if (texts[i] == null) continue;
                    var isMeta = i < 2;
                    if (!isMeta) continue;
                    texts[i].color = Mathf.Sin(t * 95f) > 0f ? Cyan : (day >= 4 ? Magenta : originalColors[i]);
                }

                yield return null;
            }

            if (bubble != null) bubble.anchoredPosition = basePosition;
            for (var i = 0; i < texts.Length; i++)
                if (texts[i] != null) texts[i].color = originalColors[i];
            if (slice != null) Destroy(slice);

            glitchRunning = false;
        }

        private void ScheduleNextGlitch(int day)
        {
            rng = NextHash(rng);
            var t = (rng & 0xFFFFu) / 65535f;
            nextGlitchAt = Time.unscaledTime + (day >= 4 ? Mathf.Lerp(3.6f, 6.2f, t) : Mathf.Lerp(6.0f, 9.0f, t));
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static bool IsRussian() =>
            string.Equals(ExcelHell.Application.ExcelHellApplication.CurrentLanguageCode, "ru", StringComparison.OrdinalIgnoreCase);

        private static int CurrentDay()
        {
            var id = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (id.StartsWith("04_", StringComparison.OrdinalIgnoreCase)) return 4;
            if (id.StartsWith("03_", StringComparison.OrdinalIgnoreCase)) return 3;
            if (id.StartsWith("02_", StringComparison.OrdinalIgnoreCase)) return 2;
            return 1;
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

        private static uint NextHash(uint value)
        {
            unchecked
            {
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return value == 0u ? 1u : value;
            }
        }
    }
}
