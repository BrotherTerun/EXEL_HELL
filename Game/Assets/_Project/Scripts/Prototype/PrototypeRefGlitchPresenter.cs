using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Presentation-only corruption for visible #REF! cells. It never mutates CellModel or anomaly timing.
    /// Every corrupted cell chooses one visual composition for its lifetime, then jitters within that preset.
    /// </summary>
    [DefaultExecutionOrder(2090)]
    public sealed class PrototypeRefGlitchPresenter : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);
        private static readonly FieldInfo CellLabelField = typeof(ExcelHellCellView).GetField("label", Flags);

        private static readonly char[] AboveMarks =
            { '\u0300', '\u0301', '\u0302', '\u0307', '\u0308', '\u0311', '\u0342', '\u0344' };
        private static readonly char[] BelowMarks =
            { '\u0316', '\u0317', '\u0323', '\u0324', '\u0329', '\u0330', '\u0347', '\u0348' };
        private static readonly char[] OverlayMarks =
            { '\u0334', '\u0335', '\u0336', '\u0338', '\u035C', '\u0360' };

        private readonly Dictionary<string, RefVisualState> active = new();
        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeRefGlitchPresenter>() != null) return;
            var root = new GameObject("[PRESENTATION] REF Glitch");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeRefGlitchPresenter>();
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || cells == null || views == null) return;

            var stillCorrupted = new HashSet<string>();
            foreach (var cell in cells)
            {
                if (cell == null || cell.State != CellState.Corrupted) continue;
                stillCorrupted.Add(cell.Address);

                if (!active.TryGetValue(cell.Address, out var state))
                {
                    var view = views[cell.Row, cell.Column];
                    if (view == null) continue;
                    var label = CellLabelField?.GetValue(view) as Text;
                    if (label == null) continue;

                    state = CreateState(cell, label);
                    active[cell.Address] = state;
                    Debug.Log($"[REF/VISUAL] {cell.Address} preset={state.Preset} seed={state.Seed}.");
                }

                Apply(state);
            }

            var removed = new List<string>();
            foreach (var pair in active)
                if (!stillCorrupted.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (var address in removed)
            {
                Restore(active[address]);
                active.Remove(address);
            }
        }

        private void Bind(ExcelHellPrototype owner)
        {
            foreach (var state in active.Values) Restore(state);
            active.Clear();
            prototype = owner;
            cells = null;
            views = null;
            if (prototype == null) return;
            cells = CellsField?.GetValue(prototype) as CellModel[,];
            views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
        }

        private static RefVisualState CreateState(CellModel cell, Text label)
        {
            // Random per anomaly instance, stable while that red cell is alive.
            var seed = unchecked((uint)UnityEngine.Random.Range(1, int.MaxValue));
            var preset = (RefPreset)(seed % 5u);
            var rect = label.rectTransform;
            return new RefVisualState
            {
                Address = cell.Address,
                Label = label,
                Rect = rect,
                BasePosition = rect.anchoredPosition,
                BaseScale = rect.localScale,
                BaseFontSize = label.fontSize,
                BaseStyle = label.fontStyle,
                Seed = seed,
                Preset = preset,
                NextPulseAt = Time.unscaledTime,
                Phase = 0
            };
        }

        private static void Apply(RefVisualState state)
        {
            if (state?.Label == null || state.Rect == null) return;
            if (Time.unscaledTime >= state.NextPulseAt)
            {
                state.Phase++;
                state.Seed = NextHash(state.Seed);
                state.NextPulseAt = Time.unscaledTime + PulseInterval(state.Preset, state.Seed);
                state.RenderedText = BuildRefText(state.Preset, state.Seed, state.Phase);
            }

            state.Label.font = PrototypeVisualTheme.MonoFont;
            state.Label.fontStyle = FontStyle.Bold;
            state.Label.text = string.IsNullOrEmpty(state.RenderedText) ? BuildRefText(state.Preset, state.Seed, state.Phase) : state.RenderedText;

            var jump = Jump(state.Preset, state.Seed, state.Phase);
            state.Rect.anchoredPosition = state.BasePosition + jump.Position;
            state.Rect.localScale = Vector3.Scale(state.BaseScale, jump.Scale);
            state.Label.fontSize = jump.FontSize;
        }

        private static RefJump Jump(RefPreset preset, uint seed, int phase)
        {
            var signedA = ((seed & 0xFFu) / 255f) * 2f - 1f;
            var signedB = (((seed >> 8) & 0xFFu) / 255f) * 2f - 1f;
            var flip = (phase & 1) == 0 ? 1f : -1f;

            return preset switch
            {
                RefPreset.DenseVertical => new RefJump(new Vector2(0f, signedB * 3.5f),
                    new Vector3(1f, 1f + 0.05f * flip, 1f), 27 + (phase % 3)),
                RefPreset.BrokenCase => new RefJump(new Vector2(signedA * 2f, signedB * 2f),
                    Vector3.one, 24 + (phase % 4)),
                RefPreset.WidePulse => new RefJump(new Vector2(signedA * 1.5f, 0f),
                    new Vector3(1.02f + 0.06f * Mathf.Abs(signedA), 0.98f + 0.05f * flip, 1f), 26 + (phase % 2) * 3),
                RefPreset.Twitch => new RefJump(new Vector2(signedA * 4.5f, signedB * 4.5f),
                    new Vector3(1f + 0.04f * flip, 1f - 0.04f * flip, 1f), 23 + (phase % 5)),
                _ => new RefJump(new Vector2(signedA * 2.5f, signedB * 1.5f),
                    new Vector3(0.98f + 0.04f * flip, 1.02f - 0.03f * flip, 1f), 25 + (phase % 3))
            };
        }

        private static float PulseInterval(RefPreset preset, uint seed)
        {
            var t = (seed & 0xFFFFu) / 65535f;
            return preset switch
            {
                RefPreset.Twitch => Mathf.Lerp(0.055f, 0.11f, t),
                RefPreset.BrokenCase => Mathf.Lerp(0.10f, 0.19f, t),
                RefPreset.DenseVertical => Mathf.Lerp(0.13f, 0.23f, t),
                RefPreset.WidePulse => Mathf.Lerp(0.16f, 0.28f, t),
                _ => Mathf.Lerp(0.09f, 0.17f, t)
            };
        }

        private static string BuildRefText(RefPreset preset, uint seed, int phase)
        {
            var mixedCase = preset == RefPreset.BrokenCase || preset == RefPreset.Twitch;
            var marksMin = preset == RefPreset.DenseVertical ? 5 : preset == RefPreset.WidePulse ? 2 : 3;
            var marksMax = preset == RefPreset.DenseVertical ? 10 : preset == RefPreset.WidePulse ? 5 : 7;
            var chance = preset == RefPreset.WidePulse ? 0.58f : preset == RefPreset.BrokenCase ? 0.70f : 0.86f;
            var state = seed ^ unchecked((uint)(phase * 2654435761u));
            const string source = "#REF!";
            var builder = new StringBuilder(32);

            foreach (var original in source)
            {
                var c = original;
                if (mixedCase && char.IsLetter(c))
                {
                    state = NextHash(state);
                    if ((state & 1u) == 0u) c = char.ToLowerInvariant(c);
                }
                builder.Append(c);
                if (!char.IsLetter(original)) continue;

                state = NextHash(state);
                if (Roll(state) > chance) continue;
                state = NextHash(state);
                var marks = marksMin + (int)(state % (uint)(marksMax - marksMin + 1));
                for (var i = 0; i < marks; i++)
                {
                    state = NextHash(state);
                    var family = preset == RefPreset.DenseVertical
                        ? (i % 2 == 0 ? 0 : 1)
                        : (int)(state % 3u);
                    var pool = family == 0 ? AboveMarks : family == 1 ? BelowMarks : OverlayMarks;
                    state = NextHash(state);
                    builder.Append(pool[(int)(state % (uint)pool.Length)]);
                }
            }

            return builder.ToString();
        }

        private static void Restore(RefVisualState state)
        {
            if (state?.Label == null || state.Rect == null) return;
            state.Rect.anchoredPosition = state.BasePosition;
            state.Rect.localScale = state.BaseScale;
            state.Label.fontSize = state.BaseFontSize;
            state.Label.fontStyle = state.BaseStyle;
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

        private sealed class RefVisualState
        {
            public string Address;
            public Text Label;
            public RectTransform Rect;
            public Vector2 BasePosition;
            public Vector3 BaseScale;
            public int BaseFontSize;
            public FontStyle BaseStyle;
            public uint Seed;
            public RefPreset Preset;
            public float NextPulseAt;
            public int Phase;
            public string RenderedText;
        }

        private readonly struct RefJump
        {
            public readonly Vector2 Position;
            public readonly Vector3 Scale;
            public readonly int FontSize;

            public RefJump(Vector2 position, Vector3 scale, int fontSize)
            {
                Position = position;
                Scale = scale;
                FontSize = fontSize;
            }
        }

        private enum RefPreset
        {
            DenseVertical,
            BrokenCase,
            WidePulse,
            Twitch,
            Scramble
        }
    }
}
