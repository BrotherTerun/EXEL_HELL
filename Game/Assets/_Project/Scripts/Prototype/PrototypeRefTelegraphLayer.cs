using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Presentation-only #REF! warning language.
    /// The old explicit "REF appears in G4 in N turns" rail is intentionally replaced by a signal living inside
    /// the threatened cell itself: cold digital instability at long range, magenta intrusion at one turn, then a
    /// brief red terminal accent on the actual outbreak. Active spread keeps its own red warning language.
    ///
    /// No gameplay state or anomaly timing is changed here. The layer only reads pendingSpawnIntent/currentIntent.
    /// </summary>
    [DefaultExecutionOrder(1150)]
    public sealed class PrototypeRefTelegraphLayer : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly Color Cyan = new(0.01f, 0.92f, 0.92f, 1f);
        private static readonly Color Blue = new(0.09f, 0.07f, 0.75f, 1f);
        private static readonly Color Magenta = new(0.97f, 0.11f, 0.95f, 1f);
        private static readonly Color Red = new(0.98f, 0.02f, 0.04f, 1f);
        private static readonly Color SpreadFillColor = new(0.96f, 0.08f, 0.10f, 0.24f);

        private static readonly Color FormulaBackgroundColor = new(0.88f, 0.92f, 0.97f, 1f);
        private static readonly Color SelectedBackgroundColor = new(0.65f, 0.84f, 1f, 1f);
        private static readonly Color ReportBackgroundColor = new(0.88f, 0.96f, 0.89f, 1f);
        private static readonly Color KeyBackgroundColor = new(0.91f, 0.94f, 0.98f, 1f);

        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);
        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", Flags);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", Flags);
        private static readonly FieldInfo PendingSpawnField = typeof(ExcelHellPrototype).GetField("pendingSpawnIntent", Flags);
        private static readonly FieldInfo CurrentIntentField = typeof(ExcelHellPrototype).GetField("currentIntent", Flags);

        private sealed class TelegraphVisual
        {
            public RectTransform Root;
            public Image Fill;
            public Image Noise;
            public Image Slice;
            public readonly List<Image> Border = new();
        }

        private readonly Dictionary<CellModel, TelegraphVisual> overlays = new();
        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private List<CellModel> selection;
        private List<ReportGoal> goals;
        private Sprite[] glitchSprites;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeRefTelegraphLayer>() != null) return;
            var helper = new GameObject("[PRESENTATION] REF Telegraph Layer").AddComponent<PrototypeRefTelegraphLayer>();
            DontDestroyOnLoad(helper.gameObject);
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || cells == null || views == null) return;

            foreach (var visual in overlays.Values)
                SetHidden(visual);

            CellModel spawnTarget = null;
            var turnsRemaining = int.MaxValue;
            var pendingValue = PendingSpawnField?.GetValue(prototype);
            if (pendingValue is SpawnIntent pending)
            {
                spawnTarget = cells[pending.Row, pending.Column];
                turnsRemaining = pending.TurnsRemaining;
            }

            CellModel spreadTarget = null;
            var intentValue = CurrentIntentField?.GetValue(prototype);
            if (intentValue is AnomalyIntent intent)
                spreadTarget = cells[intent.TargetRow, intent.TargetColumn];

            if (spreadTarget != null && spreadTarget != spawnTarget && spreadTarget.State == CellState.Normal)
            {
                RestoreUnderlyingBackground(spreadTarget);
                ShowSpread(EnsureOverlay(spreadTarget));
            }

            if (spawnTarget != null && spawnTarget.State == CellState.Normal)
            {
                RestoreUnderlyingBackground(spawnTarget);
                ShowSpawn(EnsureOverlay(spawnTarget), Mathf.Max(1, turnsRemaining));
            }
        }

        private void Bind(ExcelHellPrototype owner)
        {
            foreach (var visual in overlays.Values)
                if (visual?.Root != null) Destroy(visual.Root.gameObject);
            overlays.Clear();

            prototype = owner;
            cells = prototype == null ? null : CellsField?.GetValue(prototype) as CellModel[,];
            views = prototype == null ? null : ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            selection = prototype == null ? null : SelectionField?.GetValue(prototype) as List<CellModel>;
            goals = prototype == null ? null : GoalsField?.GetValue(prototype) as List<ReportGoal>;
            EnsureGlitchSprites();
        }

        private TelegraphVisual EnsureOverlay(CellModel cell)
        {
            if (overlays.TryGetValue(cell, out var existing) && existing?.Root != null) return existing;
            var view = views[cell.Row, cell.Column];
            if (view == null) return null;

            var root = new GameObject("REF Telegraph Overlay", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(view.transform, false);
            var rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            rootRect.SetAsLastSibling();

            var fill = root.GetComponent<Image>();
            fill.color = Color.clear;
            fill.raycastTarget = false;
            fill.enabled = false;

            var noise = CreateLayer(root.transform, "Glitch Noise", Vector2.zero, Vector2.one);
            var slice = CreateLayer(root.transform, "Glitch Slice", new Vector2(0f, 0.40f), new Vector2(1f, 0.60f));

            var visual = new TelegraphVisual
            {
                Root = rootRect,
                Fill = fill,
                Noise = noise,
                Slice = slice
            };

            visual.Border.Add(CreateBorder(root.transform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(0f, 0f)));
            visual.Border.Add(CreateBorder(root.transform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f)));
            visual.Border.Add(CreateBorder(root.transform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(2f, 0f)));
            visual.Border.Add(CreateBorder(root.transform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-2f, 0f), new Vector2(0f, 0f)));

            overlays[cell] = visual;
            return visual;
        }

        private static Image CreateLayer(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static Image CreateBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject($"REF Border {name}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static void SetHidden(TelegraphVisual visual)
        {
            if (visual == null) return;
            if (visual.Fill != null) visual.Fill.enabled = false;
            if (visual.Noise != null) visual.Noise.enabled = false;
            if (visual.Slice != null) visual.Slice.enabled = false;
            foreach (var border in visual.Border)
                if (border != null) border.enabled = false;
        }

        private void ShowSpawn(TelegraphVisual visual, int turnsRemaining)
        {
            if (visual?.Fill == null || visual.Root == null) return;

            var imminent = turnsRemaining <= 1;
            var near = turnsRemaining == 2;
            var primary = imminent ? Magenta : near ? Cyan : Blue;
            var secondary = imminent ? Red : near ? Magenta : Cyan;
            var speed = imminent ? 17f : near ? 10f : 5.5f;
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed);
            var noisePulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (speed * 1.73f) + 1.2f);

            var fill = primary;
            fill.a = imminent ? Mathf.Lerp(0.18f, 0.34f, pulse) : near ? Mathf.Lerp(0.10f, 0.22f, pulse) : Mathf.Lerp(0.045f, 0.10f, pulse);
            visual.Fill.color = fill;
            visual.Fill.enabled = true;

            if (visual.Noise != null)
            {
                visual.Noise.sprite = PickGlitchSprite(turnsRemaining, 0);
                var noise = primary;
                noise.a = imminent ? Mathf.Lerp(0.12f, 0.34f, noisePulse) : near ? Mathf.Lerp(0.07f, 0.22f, noisePulse) : Mathf.Lerp(0.025f, 0.09f, noisePulse);
                visual.Noise.color = noise;
                visual.Noise.enabled = true;
                var nrect = visual.Noise.rectTransform;
                var jitter = imminent ? 3.0f : near ? 1.8f : 0.8f;
                nrect.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 29f) * jitter, Mathf.Cos(Time.unscaledTime * 19f) * jitter * 0.35f);
            }

            if (visual.Slice != null)
            {
                visual.Slice.sprite = PickGlitchSprite(turnsRemaining, 1);
                var slice = secondary;
                slice.a = imminent ? Mathf.Lerp(0.25f, 0.60f, noisePulse) : near ? Mathf.Lerp(0.09f, 0.28f, noisePulse) : Mathf.Lerp(0.02f, 0.10f, noisePulse);
                visual.Slice.color = slice;
                visual.Slice.enabled = imminent || near || noisePulse > 0.82f;

                var y = imminent
                    ? Mathf.Lerp(0.16f, 0.76f, Mathf.PingPong(Time.unscaledTime * 2.7f, 1f))
                    : near
                        ? Mathf.Lerp(0.28f, 0.66f, Mathf.PingPong(Time.unscaledTime * 1.5f, 1f))
                        : 0.46f;
                var height = imminent ? 0.18f : near ? 0.12f : 0.08f;
                var rect = visual.Slice.rectTransform;
                rect.anchorMin = new Vector2(0f, y);
                rect.anchorMax = new Vector2(1f, Mathf.Min(1f, y + height));
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            foreach (var border in visual.Border)
            {
                if (border == null) continue;
                var color = secondary;
                color.a = imminent ? Mathf.Lerp(0.55f, 0.95f, pulse) : near ? Mathf.Lerp(0.30f, 0.70f, pulse) : Mathf.Lerp(0.14f, 0.38f, pulse);
                border.color = color;
                border.enabled = true;
            }

            // Geometry jitter is applied only to the raycast-free overlay root, never to the actual cell/hitbox.
            var offset = imminent ? 2.2f : near ? 1.1f : 0.35f;
            visual.Root.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * speed * 1.31f) * offset, 0f);
            visual.Root.localScale = new Vector3(1f + (imminent ? pulse * 0.012f : near ? pulse * 0.006f : 0f), 1f, 1f);
            visual.Root.SetAsLastSibling();
        }

        private void ShowSpread(TelegraphVisual visual)
        {
            if (visual?.Fill == null) return;
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 11f);
            var fill = SpreadFillColor;
            fill.a = Mathf.Lerp(0.16f, 0.32f, pulse);
            visual.Fill.color = fill;
            visual.Fill.enabled = true;
            visual.Fill.rectTransform.SetAsLastSibling();

            if (visual.Slice != null)
            {
                visual.Slice.sprite = PickGlitchSprite(1, 2);
                var c = Red;
                c.a = Mathf.Lerp(0.14f, 0.38f, pulse);
                visual.Slice.color = c;
                visual.Slice.enabled = true;
            }

            foreach (var border in visual.Border)
                if (border != null) border.enabled = false;
        }

        private Sprite PickGlitchSprite(int turnsRemaining, int salt)
        {
            EnsureGlitchSprites();
            if (glitchSprites == null || glitchSprites.Length == 0) return null;
            var index = Mathf.Abs(turnsRemaining * 3 + salt + Mathf.FloorToInt(Time.unscaledTime * (turnsRemaining <= 1 ? 12f : 5f))) % glitchSprites.Length;
            return glitchSprites[index];
        }

        private void EnsureGlitchSprites()
        {
            if (glitchSprites != null && glitchSprites.Length > 0) return;

            // Preferred asset: Resources/Art/glitch_textures.png, the 2x2 monochrome atlas supplied for the jam.
            // The presenter still has a procedural fallback so the branch remains runnable before the atlas is copied
            // into Resources on a workstation.
            var atlas = Resources.Load<Texture2D>("Art/glitch_textures");
            if (atlas != null && atlas.width >= 4 && atlas.height >= 4)
            {
                var halfW = atlas.width / 2f;
                var halfH = atlas.height / 2f;
                glitchSprites = new[]
                {
                    Sprite.Create(atlas, new Rect(0f, halfH, halfW, halfH), new Vector2(0.5f, 0.5f), 100f),
                    Sprite.Create(atlas, new Rect(halfW, halfH, atlas.width - halfW, halfH), new Vector2(0.5f, 0.5f), 100f),
                    Sprite.Create(atlas, new Rect(0f, 0f, halfW, atlas.height - halfH), new Vector2(0.5f, 0.5f), 100f),
                    Sprite.Create(atlas, new Rect(halfW, 0f, atlas.width - halfW, atlas.height - halfH), new Vector2(0.5f, 0.5f), 100f)
                };
                return;
            }

            glitchSprites = new Sprite[4];
            for (var i = 0; i < glitchSprites.Length; i++)
            {
                var texture = BuildFallbackGlitchTexture(64, 64, i + 1);
                glitchSprites[i] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 64f);
            }
        }

        private static Texture2D BuildFallbackGlitchTexture(int width, int height, int seed)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"Runtime Glitch Mask {seed}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[width * height];
            var state = unchecked((uint)(seed * 2654435761u));
            for (var y = 0; y < height; y++)
            {
                state = NextHash(state);
                var band = 0.10f + ((state & 0xFFu) / 255f) * 0.55f;
                for (var x = 0; x < width; x++)
                {
                    state = NextHash(state);
                    var noise = (state & 0xFFu) / 255f;
                    var on = noise > (0.72f - band * 0.28f) || ((y + seed * 7) % (5 + seed) == 0 && noise > 0.42f);
                    var a = on ? (byte)Mathf.RoundToInt(Mathf.Lerp(70f, 220f, noise)) : (byte)0;
                    pixels[y * width + x] = new Color32(255, 255, 255, a);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

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

        private void RestoreUnderlyingBackground(CellModel cell)
        {
            var view = views[cell.Row, cell.Column];
            var background = view == null ? null : view.GetComponent<Image>();
            if (background == null) return;

            if (cell.IsFormula)
            {
                background.color = FormulaBackgroundColor;
                return;
            }

            if (selection != null && selection.Contains(cell))
            {
                background.color = SelectedBackgroundColor;
                return;
            }

            if (IsReportTarget(cell))
            {
                background.color = ReportBackgroundColor;
                return;
            }

            background.color = cell.Occupant?.Kind == ContentKind.RecordKey || cell.Occupant?.Kind == ContentKind.FieldKey
                ? KeyBackgroundColor
                : Color.white;
        }

        private bool IsReportTarget(CellModel cell)
        {
            if (goals == null || cell == null) return false;
            foreach (var goal in goals)
                if (goal.TargetRow == cell.Row && goal.TargetColumn == cell.Column)
                    return true;
            return false;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
