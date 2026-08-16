using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Gives completed CELL MESSAGE manifestations the same living typography language as #REF!.
    /// Color semantics are shared with the glitch pass: cyan/blue = digital instability, magenta = intrusion,
    /// red = terminal danger. The slow typewriter remains readable; corruption begins only after the line settles.
    /// </summary>
    [DefaultExecutionOrder(2100)]
    public sealed class PrototypeCellMessageIdleGlitchPresenter : MonoBehaviour
    {
        private static readonly Color Cyan = new(0.01f, 0.92f, 0.92f, 1f);
        private static readonly Color Blue = new(0.09f, 0.07f, 0.75f, 1f);
        private static readonly Color Magenta = new(0.97f, 0.11f, 0.95f, 1f);
        private static readonly Color Red = new(0.98f, 0.02f, 0.04f, 1f);

        private readonly Dictionary<int, MessageState> states = new();
        private readonly List<int> dead = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeCellMessageIdleGlitchPresenter>() != null) return;
            var root = new GameObject("[PRESENTATION] CELL Message Idle Glitch");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeCellMessageIdleGlitchPresenter>();
        }

        private void LateUpdate()
        {
            Discover();
            dead.Clear();

            foreach (var pair in states)
            {
                var state = pair.Value;
                if (state.Text == null || state.Rect == null || state.Root == null)
                {
                    dead.Add(pair.Key);
                    continue;
                }

                ObserveTypewriter(state);
                ApplyColorLanguage(state);
                if (!state.IdleReady) continue;

                if (Time.unscaledTime >= state.NextPulseAt)
                {
                    Pulse(state);
                    state.NextPulseAt = Time.unscaledTime + NextInterval(state);
                }

                ApplyJitter(state);
            }

            foreach (var id in dead) states.Remove(id);
        }

        private void Discover()
        {
            foreach (var text in FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (text == null || text.transform.parent == null) continue;
                var root = text.transform.parent.gameObject;
                if (!root.name.StartsWith("Cell Message ", StringComparison.Ordinal)) continue;

                var id = root.GetInstanceID();
                if (states.ContainsKey(id)) continue;

                var rect = text.rectTransform;
                states[id] = new MessageState
                {
                    Root = root,
                    Text = text,
                    Rect = rect,
                    BasePosition = rect.anchoredPosition,
                    BaseColor = text.color,
                    LastObservedText = text.text ?? string.Empty,
                    SettledAt = Time.unscaledTime,
                    Seed = StableHash(root.name + ":" + id),
                    NextPulseAt = float.PositiveInfinity
                };
            }
        }

        private static void ObserveTypewriter(MessageState state)
        {
            var current = state.Text.text ?? string.Empty;
            if (!state.IdleReady)
            {
                if (!string.Equals(current, state.LastObservedText, StringComparison.Ordinal))
                {
                    state.LastObservedText = current;
                    state.SettledAt = Time.unscaledTime;
                    return;
                }

                if (current.Length > 0 && Time.unscaledTime - state.SettledAt >= 0.46f)
                {
                    state.BaseText = StripCombining(current);
                    state.IdleReady = true;
                    state.NextPulseAt = Time.unscaledTime + 0.28f;
                }
            }
        }

        private static void ApplyColorLanguage(MessageState state)
        {
            if (state?.Text == null) return;
            var day = CurrentDay();
            if (day <= 1)
            {
                state.Text.color = state.BaseColor;
                return;
            }

            if (day == 2)
            {
                var c = Cyan;
                c.a = Mathf.Max(0.88f, state.BaseColor.a);
                state.Text.color = Color.Lerp(state.BaseColor, c, 0.42f);
                return;
            }

            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (day >= 4 ? 4.2f : 2.8f) + (state.Seed & 7u));
            if (day == 3)
            {
                var c = Color.Lerp(Blue, Magenta, pulse * 0.72f);
                c.a = Mathf.Max(0.90f, state.BaseColor.a);
                state.Text.color = c;
                return;
            }

            // L4 is primarily magenta intrusion. Red is a short accent, not the default message color,
            // preserving red as the terminal/danger signal used by #REF!/destruction.
            var redAccent = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * 1.9f + (state.Seed & 3u))), 10f);
            var l4 = Color.Lerp(Magenta, Red, redAccent * 0.72f);
            l4.a = Mathf.Max(0.92f, state.BaseColor.a);
            state.Text.color = l4;
        }

        private static void Pulse(MessageState state)
        {
            state.Seed = NextHash(state.Seed);
            state.Phase++;
            var day = CurrentDay();
            var mode = (int)(state.Seed % 3u);
            state.Text.text = BuildVariant(state.BaseText, state.Seed, state.Phase, day, mode);
        }

        private static string BuildVariant(string source, uint seed, int phase, int day, int mode)
        {
            var state = seed ^ unchecked((uint)(phase * 2654435761u));
            var builder = new StringBuilder(Mathf.Max(16, source.Length * 4));
            var zalgo = mode != 2;
            var symbols = mode != 1;
            var mixedCase = mode != 0;

            var zalgoChance = day >= 4 ? 0.82f : day == 3 ? 0.66f : 0.48f;
            var symbolChance = day >= 4 ? 0.38f : day == 3 ? 0.28f : 0.18f;
            var caseChance = day >= 4 ? 0.60f : day == 3 ? 0.46f : 0.32f;
            var minMarks = day >= 4 ? 4 : day == 3 ? 3 : 2;
            var maxMarks = day >= 4 ? 9 : day == 3 ? 7 : 5;

            foreach (var original in source)
            {
                var rendered = original;

                if (char.IsLetter(original) && mixedCase)
                {
                    state = NextHash(state);
                    if (Roll(state) <= caseChance)
                        rendered = char.IsUpper(rendered) ? char.ToLowerInvariant(rendered) : char.ToUpperInvariant(rendered);
                }

                if (char.IsLetter(original) && symbols)
                {
                    state = NextHash(state);
                    if (Roll(state) <= symbolChance && TrySyntaxSubstitute(rendered, out var substitute))
                        rendered = substitute;
                }

                builder.Append(rendered);
                if (!char.IsLetter(original) || !zalgo) continue;

                state = NextHash(state);
                if (Roll(state) > zalgoChance) continue;
                state = NextHash(state);
                var count = minMarks + (int)(state % (uint)(maxMarks - minMarks + 1));
                for (var i = 0; i < count; i++)
                {
                    state = NextHash(state);
                    builder.Append(PickCombiningMark(state, i));
                }
            }

            return builder.ToString();
        }

        private static void ApplyJitter(MessageState state)
        {
            var t = Time.unscaledTime * (5.4f + (state.Seed & 3u));
            var strength = CurrentDay() >= 4 ? 2.4f : CurrentDay() == 3 ? 1.8f : 1.25f;
            var x = Mathf.Sin(t * 1.17f) * strength;
            var y = Mathf.Cos(t * 1.61f) * strength * 0.55f;
            state.Rect.anchoredPosition = state.BasePosition + new Vector2(x, y);
        }

        private static float NextInterval(MessageState state)
        {
            state.Seed = NextHash(state.Seed);
            var t = (state.Seed & 0xFFFFu) / 65535f;
            return Mathf.Lerp(0.42f, 0.68f, t);
        }

        private static int CurrentDay()
        {
            var id = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (id.StartsWith("04_", StringComparison.OrdinalIgnoreCase)) return 4;
            if (id.StartsWith("03_", StringComparison.OrdinalIgnoreCase)) return 3;
            if (id.StartsWith("02_", StringComparison.OrdinalIgnoreCase)) return 2;
            return 1;
        }

        private static string StripCombining(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark ||
                    category == UnicodeCategory.SpacingCombiningMark ||
                    category == UnicodeCategory.EnclosingMark)
                    continue;
                builder.Append(c);
            }
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static char PickCombiningMark(uint state, int index)
        {
            const string above = "\u0300\u0301\u0302\u0307\u0308\u0311\u0342\u0344";
            const string below = "\u0316\u0317\u0323\u0324\u0329\u0330\u0347\u0348";
            const string overlay = "\u0334\u0335\u0336\u0338\u035C\u0360";
            var pool = index % 3 == 0 ? above : index % 3 == 1 ? below : overlay;
            return pool[(int)(state % (uint)pool.Length)];
        }

        private static bool TrySyntaxSubstitute(char source, out char replacement)
        {
            switch (char.ToUpperInvariant(source))
            {
                case 'А': replacement = '@'; return true;
                case 'Б': replacement = '6'; return true;
                case 'В': replacement = '8'; return true;
                case 'Г': replacement = '7'; return true;
                case 'Е': replacement = '3'; return true;
                case 'Ж': replacement = '*'; return true;
                case 'З': replacement = '3'; return true;
                case 'К': replacement = '<'; return true;
                case 'Н': replacement = '#'; return true;
                case 'О': replacement = '0'; return true;
                case 'Р': replacement = '?'; return true;
                case 'С': replacement = '('; return true;
                case 'Т': replacement = '+'; return true;
                case 'Х': replacement = '%'; return true;
                case 'Ч': replacement = '4'; return true;
                default: replacement = source; return false;
            }
        }

        private static float Roll(uint value) => (value & 0xFFFFu) / 65535f;

        private static uint NextHash(uint value)
        {
            unchecked
            {
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return value;
            }
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var c in value ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return hash;
            }
        }

        private sealed class MessageState
        {
            public GameObject Root;
            public Text Text;
            public RectTransform Rect;
            public Vector2 BasePosition;
            public Color BaseColor;
            public string LastObservedText;
            public string BaseText;
            public float SettledAt;
            public float NextPulseAt;
            public uint Seed;
            public int Phase;
            public bool IdleReady;
        }
    }
}
