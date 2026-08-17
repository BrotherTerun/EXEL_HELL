using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Safe v2 presentation pass for L3-L4 spreadsheet psychosis.
    /// It disables the first-pass presenter and replaces it with stronger, readable manifestations while leaving
    /// CellModel, ContentToken, formula ownership, selection and turn economy untouched.
    /// </summary>
    [DefaultExecutionOrder(2160)]
    public sealed class PrototypeSpreadsheetPsychosisPolish : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);
        private static readonly FieldInfo TurnField = typeof(ExcelHellPrototype).GetField("turn", Flags);
        private static readonly FieldInfo CellLabelField = typeof(ExcelHellCellView).GetField("label", Flags);

        private static readonly Color Cyan = new(0.01f, 0.92f, 0.92f, 1f);
        private static readonly Color Blue = new(0.09f, 0.07f, 0.75f, 1f);
        private static readonly Color Magenta = new(0.97f, 0.11f, 0.95f, 1f);
        private static readonly Color Red = new(0.98f, 0.02f, 0.04f, 1f);

        private readonly List<GameObject> active = new();
        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private int lastTurn = -1;
        private string lastLevelId;
        private uint rng = 0x7A11CEu;
        private float nextAmbientSpawnAt;
        private ManifestationKind lastKind = (ManifestationKind)(-1);
        private Sprite[] glitchSprites;
        private bool legacyDisabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeSpreadsheetPsychosisPolish>() != null) return;
            var root = new GameObject("[PRESENTATION] Spreadsheet Psychosis v2");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeSpreadsheetPsychosisPolish>();
        }

        private void LateUpdate()
        {
            DisableLegacyPresenter();

            if (PrototypeAuthoringMode.Active)
            {
                Bind(null);
                return;
            }

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || cells == null || views == null) return;

            active.RemoveAll(go => go == null);

            var levelId = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (!string.Equals(levelId, lastLevelId, StringComparison.OrdinalIgnoreCase))
            {
                ClearAll();
                lastLevelId = levelId;
                lastTurn = ReadTurn();
                rng = StableHash(levelId + ":psychosis-v2");
                nextAmbientSpawnAt = Time.unscaledTime + 1.1f;
                lastKind = (ManifestationKind)(-1);
                return;
            }

            var day = CurrentDay();
            if (day < 3) return;

            var turn = ReadTurn();
            if (day == 3)
            {
                if (turn == lastTurn) return;
                lastTurn = turn;
                if (active.Count >= 1) return;

                rng = NextHash(rng ^ unchecked((uint)(turn * 2654435761u)));
                if (Roll(rng) <= 0.54f)
                    SpawnRandom(day);
                return;
            }

            // L4 should feel continuously hostile instead of occasionally bugged.
            if (turn != lastTurn)
            {
                lastTurn = turn;
                rng = NextHash(rng ^ unchecked((uint)(turn * 2654435761u)));
                if (active.Count < 3 && Roll(rng) <= 0.90f)
                    SpawnRandom(day);
            }

            if (Time.unscaledTime >= nextAmbientSpawnAt && active.Count < 2)
            {
                SpawnRandom(day);
                rng = NextHash(rng);
                nextAmbientSpawnAt = Time.unscaledTime + Mathf.Lerp(1.7f, 2.6f, Roll(rng));
            }
        }

        private void DisableLegacyPresenter()
        {
            if (legacyDisabled) return;
            var legacy = FindFirstObjectByType<PrototypeSpreadsheetPsychosisPresenter>();
            if (legacy == null) return;

            legacy.enabled = false;
            legacyDisabled = true;

            // Hot-reload safety: clean any first-pass visuals that may have existed before this patch took over.
            var stale = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => r != null && IsLegacyVisualName(r.gameObject.name))
                .Select(r => r.gameObject)
                .Distinct()
                .ToList();
            foreach (var go in stale)
                if (go != null) Destroy(go);

            Debug.Log("[PSYCHOSIS/V2] Legacy presenter disabled; presentation-only polish active.");
        }

        private static bool IsLegacyVisualName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name == "Psychosis False Key" ||
                   name == "Psychosis Ghost Selection" ||
                   name == "Psychosis Column Echo Group" ||
                   name == "Psychosis Column Echo" ||
                   name == "Psychosis Cell Escape" ||
                   name == "Psychosis Grid Tear";
        }

        private void Bind(ExcelHellPrototype owner)
        {
            ClearAll();
            prototype = owner;
            cells = prototype == null ? null : CellsField?.GetValue(prototype) as CellModel[,];
            views = prototype == null ? null : ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            lastTurn = prototype == null ? -1 : ReadTurn();
            lastLevelId = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            nextAmbientSpawnAt = Time.unscaledTime + 1.1f;
            lastKind = (ManifestationKind)(-1);
            EnsureGlitchSprites();
        }

        private int ReadTurn() => TurnField?.GetValue(prototype) is int value ? value : 0;

        private void SpawnRandom(int day)
        {
            var kindCount = day >= 4 ? 6 : 3;
            ManifestationKind kind = ManifestationKind.FalseKey;

            for (var attempt = 0; attempt < 4; attempt++)
            {
                rng = NextHash(rng);
                kind = (ManifestationKind)(rng % (uint)kindCount);
                if (kind != lastKind || kindCount <= 1) break;
            }

            lastKind = kind;
            Spawn(kind, day);
        }

        private void Spawn(ManifestationKind kind, int day)
        {
            switch (kind)
            {
                case ManifestationKind.FalseKey:
                    SpawnFalseKey(day);
                    break;
                case ManifestationKind.GhostSelection:
                    SpawnGhostSelection(day);
                    break;
                case ManifestationKind.ColumnDrift:
                    SpawnColumnDrift(day);
                    break;
                case ManifestationKind.CellEscape:
                    SpawnCellEscape(day);
                    break;
                case ManifestationKind.GridTear:
                    SpawnGridTear(day);
                    break;
                case ManifestationKind.RowDrift:
                    SpawnRowDrift(day);
                    break;
            }
        }

        private void SpawnFalseKey(int day)
        {
            var candidates = cells.Cast<CellModel>()
                .Where(c => c != null && c.State == CellState.Normal &&
                            (c.Occupant?.Kind == ContentKind.RecordKey || c.Occupant?.Kind == ContentKind.FieldKey))
                .ToList();
            if (candidates.Count == 0) return;

            var cell = Pick(candidates);
            var view = views[cell.Row, cell.Column];
            var original = view == null ? null : CellLabelField?.GetValue(view) as Text;
            if (original == null) return;

            var root = CreateCellOverlayRoot(view.transform, "Psychosis v2 False Key", true);
            var background = root.GetComponent<Image>();
            var bg = day >= 4 ? Magenta : Cyan;
            bg.a = day >= 4 ? 0.16f : 0.10f;
            background.color = bg;

            var ghost = CreateText(root.transform, WrongKeyLabel(cell), original);
            ghost.color = day >= 4 ? Magenta : Cyan;
            ghost.fontStyle = FontStyle.Bold;
            ghost.fontSize = Mathf.Max(original.fontSize + (day >= 4 ? 2 : 1), original.fontSize);
            ghost.rectTransform.anchoredPosition = new Vector2(2f, 1f);

            var second = CreateText(root.transform, WrongKeyLabel(cell), original);
            var secondColor = day >= 4 ? Cyan : Blue;
            secondColor.a = 0.72f;
            second.color = secondColor;
            second.fontStyle = FontStyle.Bold;
            second.rectTransform.anchoredPosition = new Vector2(-3f, -1f);

            RegisterDismiss(root, day >= 4 ? 5.2f : 4.0f);
            Debug.Log($"[PSYCHOSIS/V2] FalseKey at {cell.Address} day={day}.");
        }

        private void SpawnGhostSelection(int day)
        {
            var spreadsheet = FindSpreadsheetRect();
            if (spreadsheet == null) return;

            var normal = cells.Cast<CellModel>().Where(c => c != null && c.State == CellState.Normal).ToList();
            if (normal.Count == 0) return;
            var anchor = Pick(normal);

            rng = NextHash(rng);
            var width = day >= 4 ? 2 + (int)(rng % 2u) : 2;
            rng = NextHash(rng);
            var height = day >= 4 && (rng & 1u) == 0u ? 2 : 1;

            var range = new List<CellModel>();
            for (var row = anchor.Row; row < Mathf.Min(cells.GetLength(0), anchor.Row + height); row++)
            for (var column = anchor.Column; column < Mathf.Min(cells.GetLength(1), anchor.Column + width); column++)
                if (cells[row, column] != null && views[row, column] != null)
                    range.Add(cells[row, column]);
            if (range.Count == 0) return;

            var bounds = CombinedRect(spreadsheet, range);
            if (bounds.width <= 0f || bounds.height <= 0f) return;

            var root = CreateSheetRoot(spreadsheet, "Psychosis v2 Ghost Selection");
            var selection = CreateImageRect(root.transform, "Ghost Range", bounds, Color.clear, false);
            var fill = day >= 4 ? Magenta : Cyan;
            fill.a = day >= 4 ? 0.18f : 0.12f;
            selection.color = fill;

            var edge = day >= 4 ? Magenta : Cyan;
            edge.a = 0.95f;
            AddRectEdges(selection.rectTransform, edge, day >= 4 ? 4f : 3f);

            var handle = new GameObject("Ghost Selection Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(selection.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(1f, 0f);
            handleRect.anchorMax = new Vector2(1f, 0f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = new Vector2(-2f, 2f);
            handleRect.sizeDelta = new Vector2(day >= 4 ? 9f : 7f, day >= 4 ? 9f : 7f);
            handle.GetComponent<Image>().color = edge;
            handle.GetComponent<Image>().raycastTarget = false;

            StartCoroutine(GhostSelectionRoutine(root, selection.rectTransform, day));
            Debug.Log($"[PSYCHOSIS/V2] GhostSelection range={range.First().Address}:{range.Last().Address} day={day}.");
        }

        private void SpawnColumnDrift(int day)
        {
            var spreadsheet = FindSpreadsheetRect();
            if (spreadsheet == null || views.GetLength(1) == 0) return;

            rng = NextHash(rng);
            var column = (int)(rng % (uint)views.GetLength(1));
            var selected = new List<CellModel>();
            for (var row = 0; row < views.GetLength(0); row++)
            {
                var cell = cells[row, column];
                if (cell != null && cell.State == CellState.Normal && views[row, column] != null)
                    selected.Add(cell);
            }
            if (selected.Count == 0) return;

            rng = NextHash(rng);
            var sign = (rng & 1u) == 0u ? -1f : 1f;
            rng = NextHash(rng);
            var distance = day >= 4 ? Mathf.Lerp(42f, 66f, Roll(rng)) : Mathf.Lerp(18f, 30f, Roll(rng));
            var target = new Vector2(sign * distance, 0f);

            var root = CreateSheetRoot(spreadsheet, "Psychosis v2 Column Drift");
            var pieces = BuildDriftPieces(root.transform, spreadsheet, selected, target, day, true);
            if (pieces.Count == 0)
            {
                Dismiss(root);
                return;
            }

            StartCoroutine(DriftRoutine(root, pieces, day >= 4 ? 4.2f : 3.0f, day));
            Debug.Log($"[PSYCHOSIS/V2] ColumnDrift column={ExcelHellPrototype.ColumnName(column)} offset={target.x:0} day={day}.");
        }

        private void SpawnRowDrift(int day)
        {
            var spreadsheet = FindSpreadsheetRect();
            if (spreadsheet == null || views.GetLength(0) == 0) return;

            rng = NextHash(rng);
            var row = (int)(rng % (uint)views.GetLength(0));
            var selected = new List<CellModel>();
            for (var column = 0; column < views.GetLength(1); column++)
            {
                var cell = cells[row, column];
                if (cell != null && cell.State == CellState.Normal && views[row, column] != null)
                    selected.Add(cell);
            }
            if (selected.Count == 0) return;

            rng = NextHash(rng);
            var sign = (rng & 1u) == 0u ? -1f : 1f;
            rng = NextHash(rng);
            var x = sign * Mathf.Lerp(48f, 78f, Roll(rng));
            rng = NextHash(rng);
            var y = ((rng & 1u) == 0u ? -1f : 1f) * Mathf.Lerp(4f, 12f, Roll(rng));

            var root = CreateSheetRoot(spreadsheet, "Psychosis v2 Row Drift");
            var pieces = BuildDriftPieces(root.transform, spreadsheet, selected, new Vector2(x, y), day, true);
            if (pieces.Count == 0)
            {
                Dismiss(root);
                return;
            }

            StartCoroutine(DriftRoutine(root, pieces, 3.8f, day));
            Debug.Log($"[PSYCHOSIS/V2] RowDrift row={row + 1} offset=({x:0},{y:0}) day={day}.");
        }

        private void SpawnCellEscape(int day)
        {
            var spreadsheet = FindSpreadsheetRect();
            if (spreadsheet == null) return;

            var candidates = cells.Cast<CellModel>()
                .Where(c => c != null && c.State == CellState.Normal && c.Occupant != null && views[c.Row, c.Column] != null)
                .ToList();
            if (candidates.Count == 0) return;

            var cell = Pick(candidates);
            rng = NextHash(rng);
            var sign = (rng & 1u) == 0u ? -1f : 1f;
            rng = NextHash(rng);
            var x = sign * Mathf.Lerp(62f, 104f, Roll(rng));
            rng = NextHash(rng);
            var y = ((rng & 1u) == 0u ? -1f : 1f) * Mathf.Lerp(9f, 22f, Roll(rng));

            var root = CreateSheetRoot(spreadsheet, "Psychosis v2 Cell Escape");
            var pieces = BuildDriftPieces(root.transform, spreadsheet, new[] { cell }, new Vector2(x, y), day, true);
            if (pieces.Count == 0)
            {
                Dismiss(root);
                return;
            }

            var main = pieces.FirstOrDefault(p => !p.Spectral);
            if (main?.Rect != null)
            {
                var graphic = main.Rect.GetComponent<Image>();
                if (graphic != null) graphic.raycastTarget = true;
                var dismiss = main.Rect.gameObject.AddComponent<PrototypePsychosisDismissTarget>();
                dismiss.Configure(() => Dismiss(root));
            }

            StartCoroutine(EscapeRoutine(root, pieces, 4.8f));
            Debug.Log($"[PSYCHOSIS/V2] CellEscape at {cell.Address} offset=({x:0},{y:0}).");
        }

        private void SpawnGridTear(int day)
        {
            var spreadsheet = FindSpreadsheetRect();
            if (spreadsheet == null) return;

            var root = CreateSheetRoot(spreadsheet, "Psychosis v2 Grid Tear");
            var pieces = new List<DriftPiece>();
            var bandCount = 2;

            for (var band = 0; band < bandCount; band++)
            {
                rng = NextHash(rng);
                var row = (int)(rng % (uint)views.GetLength(0));
                var selected = new List<CellModel>();
                for (var column = 0; column < views.GetLength(1); column++)
                {
                    var cell = cells[row, column];
                    if (cell != null && cell.State == CellState.Normal && views[row, column] != null)
                        selected.Add(cell);
                }
                if (selected.Count == 0) continue;

                rng = NextHash(rng);
                var sign = ((band + (int)(rng & 1u)) & 1) == 0 ? -1f : 1f;
                rng = NextHash(rng);
                var distance = Mathf.Lerp(72f, 126f, Roll(rng));
                var bandPieces = BuildDriftPieces(root.transform, spreadsheet, selected, new Vector2(sign * distance, 0f), day, false);
                pieces.AddRange(bandPieces);

                var bandRect = CombinedRect(spreadsheet, selected);
                if (bandRect.width > 0f)
                {
                    var stripRect = new Rect(bandRect.xMin, bandRect.center.y - Mathf.Min(12f, bandRect.height * 0.22f),
                        bandRect.width, Mathf.Min(24f, bandRect.height * 0.44f));
                    var stripColor = band == 0 ? Magenta : Cyan;
                    stripColor.a = 0.34f;
                    var strip = CreateImageRect(root.transform, "Grid Tear Glitch Strip", stripRect, stripColor, false);
                    strip.sprite = PickGlitchSprite(band);
                    strip.type = Image.Type.Simple;
                    strip.preserveAspect = false;
                }
            }

            if (pieces.Count == 0)
            {
                Dismiss(root);
                return;
            }

            StartCoroutine(GridTearRoutine(root, pieces, 1.55f));
            Debug.Log($"[PSYCHOSIS/V2] GridTear bands={bandCount} pieces={pieces.Count}.");
        }

        private List<DriftPiece> BuildDriftPieces(Transform root, RectTransform spreadsheet, IEnumerable<CellModel> selected,
            Vector2 target, int day, bool addSpectralEcho)
        {
            var pieces = new List<DriftPiece>();
            var index = 0;

            foreach (var cell in selected)
            {
                var view = views[cell.Row, cell.Column];
                if (view == null) continue;
                var viewRect = view.GetComponent<RectTransform>();
                var sourceImage = view.GetComponent<Image>();
                var sourceText = CellLabelField?.GetValue(view) as Text;
                if (viewRect == null || sourceImage == null) continue;

                var rect = RectInAncestor(spreadsheet, viewRect);
                if (rect.width <= 0f || rect.height <= 0f) continue;

                // Cover the real visual, not the actual cell. The hitbox/gameplay object remains untouched below.
                var maskColor = sourceImage.color;
                maskColor.a = 1f;
                var mask = CreateImageRect(root, "Psychosis Blank Original", rect, maskColor, false);
                AddRectEdges(mask.rectTransform, new Color(0.18f, 0.24f, 0.31f, 0.52f), 1f);

                var accent = index % 2 == 0 ? Magenta : Cyan;
                var main = CreateCellReplica(root, rect, sourceImage, sourceText, accent, false);
                pieces.Add(new DriftPiece(main.rectTransform, main.rectTransform.anchoredPosition, target, false, index * 0.71f));

                if (addSpectralEcho)
                {
                    var echoAccent = index % 2 == 0 ? Cyan : Magenta;
                    var echo = CreateCellReplica(root, rect, sourceImage, sourceText, echoAccent, true);
                    var echoTarget = target * 0.82f + new Vector2(index % 2 == 0 ? -6f : 6f, index % 3 - 1);
                    pieces.Add(new DriftPiece(echo.rectTransform, echo.rectTransform.anchoredPosition, echoTarget, true, index * 1.13f));
                }

                index++;
            }

            return pieces;
        }

        private Image CreateCellReplica(Transform parent, Rect rect, Image sourceImage, Text sourceText, Color accent, bool spectral)
        {
            var bg = sourceImage.color;
            if (spectral)
            {
                bg = Color.Lerp(bg, accent, 0.62f);
                bg.a = 0.34f;
            }
            else
            {
                bg.a = 0.98f;
            }

            var image = CreateImageRect(parent, spectral ? "Psychosis Spectral Cell" : "Psychosis Drifted Cell", rect, bg, false);
            var edge = accent;
            edge.a = spectral ? 0.60f : 0.82f;
            AddRectEdges(image.rectTransform, edge, spectral ? 2f : 2.5f);

            if (sourceText != null && !string.IsNullOrEmpty(sourceText.text))
            {
                var text = CreateText(image.transform, sourceText.text, sourceText);
                if (spectral)
                {
                    var c = accent;
                    c.a = 0.82f;
                    text.color = c;
                }
                else
                {
                    text.color = sourceText.color;
                    text.fontStyle = sourceText.fontStyle;
                }
            }

            return image;
        }

        private IEnumerator GhostSelectionRoutine(GameObject root, RectTransform rect, int day)
        {
            var start = Time.unscaledTime;
            var duration = day >= 4 ? 3.8f : 3.0f;
            var basePosition = rect == null ? Vector2.zero : rect.anchoredPosition;
            while (root != null && rect != null && Time.unscaledTime - start < duration)
            {
                var t = Time.unscaledTime - start;
                var jump = day >= 4 ? 3.2f : 1.7f;
                rect.anchoredPosition = basePosition + new Vector2(Mathf.Sin(t * 15f) * jump, Mathf.Cos(t * 11f) * jump * 0.36f);
                yield return null;
            }
            Dismiss(root);
        }

        private IEnumerator DriftRoutine(GameObject root, List<DriftPiece> pieces, float duration, int day)
        {
            var start = Time.unscaledTime;
            while (root != null && Time.unscaledTime - start < duration)
            {
                var t = Mathf.Clamp01((Time.unscaledTime - start) / duration);
                var envelope = t < 0.12f ? Mathf.SmoothStep(0f, 1f, t / 0.12f) :
                    t > 0.86f ? Mathf.SmoothStep(1f, 0f, (t - 0.86f) / 0.14f) : 1f;

                foreach (var piece in pieces)
                {
                    if (piece?.Rect == null) continue;
                    var jitter = piece.Spectral ? 1.6f : day >= 4 ? 2.8f : 1.3f;
                    var phase = Time.unscaledTime * (piece.Spectral ? 23f : 13f) + piece.Phase;
                    var noise = new Vector2(Mathf.Sin(phase) * jitter, Mathf.Cos(phase * 0.73f) * jitter * 0.34f);
                    piece.Rect.anchoredPosition = piece.Base + piece.Target * envelope + noise * envelope;
                }
                yield return null;
            }
            Dismiss(root);
        }

        private IEnumerator EscapeRoutine(GameObject root, List<DriftPiece> pieces, float duration)
        {
            var start = Time.unscaledTime;
            while (root != null && Time.unscaledTime - start < duration)
            {
                var t = Mathf.Clamp01((Time.unscaledTime - start) / duration);
                var travel = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t / 0.72f));
                foreach (var piece in pieces)
                {
                    if (piece?.Rect == null) continue;
                    var phase = Time.unscaledTime * (piece.Spectral ? 29f : 19f) + piece.Phase;
                    var jitter = piece.Spectral ? 4.5f : 3f;
                    piece.Rect.anchoredPosition = piece.Base + piece.Target * travel +
                        new Vector2(Mathf.Sin(phase) * jitter, Mathf.Cos(phase * 0.67f) * jitter * 0.55f);
                }
                yield return null;
            }
            Dismiss(root);
        }

        private IEnumerator GridTearRoutine(GameObject root, List<DriftPiece> pieces, float duration)
        {
            var start = Time.unscaledTime;
            while (root != null && Time.unscaledTime - start < duration)
            {
                var t = Mathf.Clamp01((Time.unscaledTime - start) / duration);
                var gate = t < 0.08f ? t / 0.08f : t > 0.88f ? 1f - ((t - 0.88f) / 0.12f) : 1f;
                gate = Mathf.Clamp01(gate);
                var stutter = 0.78f + 0.22f * (Mathf.Floor(Time.unscaledTime * 18f) % 2f);

                foreach (var piece in pieces)
                {
                    if (piece?.Rect == null) continue;
                    var phase = Time.unscaledTime * 41f + piece.Phase;
                    var jitter = new Vector2(Mathf.Sin(phase) * 7f, Mathf.Cos(phase * 0.31f) * 1.8f);
                    piece.Rect.anchoredPosition = piece.Base + piece.Target * gate * stutter + jitter * gate;
                }
                yield return null;
            }
            Dismiss(root);
        }

        private GameObject CreateCellOverlayRoot(Transform parent, string name, bool capturesClick)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            Stretch(rect);
            rect.SetAsLastSibling();
            var image = root.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = capturesClick;
            active.Add(root);
            return root;
        }

        private GameObject CreateSheetRoot(RectTransform spreadsheet, string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(spreadsheet, false);
            var rect = root.GetComponent<RectTransform>();
            rect.pivot = spreadsheet.pivot;
            Stretch(rect);
            rect.SetAsLastSibling();
            active.Add(root);
            return root;
        }

        private static Image CreateImageRect(Transform parent, string name, Rect localRect, Color color, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            var parentRect = parent as RectTransform;
            var anchor = parentRect == null ? new Vector2(0.5f, 0.5f) : parentRect.pivot;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = localRect.center;
            rect.sizeDelta = localRect.size;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Text CreateText(Transform parent, string value, Text source)
        {
            var go = new GameObject("Psychosis Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = source.font;
            text.fontSize = source.fontSize;
            text.fontStyle = source.fontStyle;
            text.alignment = source.alignment;
            text.horizontalOverflow = source.horizontalOverflow;
            text.verticalOverflow = source.verticalOverflow;
            text.text = value;
            text.color = source.color;
            text.raycastTarget = false;
            Stretch(text.rectTransform, 2f);
            return text;
        }

        private static void AddRectEdges(RectTransform parent, Color color, float thickness)
        {
            CreateEdge(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -thickness), Vector2.zero, color);
            CreateEdge(parent, "Bottom", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, thickness), color);
            CreateEdge(parent, "Left", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(thickness, 0f), color);
            CreateEdge(parent, "Right", new Vector2(1f, 0f), Vector2.one, new Vector2(-thickness, 0f), Vector2.zero, color);
        }

        private static Image CreateEdge(Transform parent, string suffix, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject("Psychosis Edge " + suffix, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Rect CombinedRect(RectTransform spreadsheet, IEnumerable<CellModel> selected)
        {
            var initialized = false;
            var min = Vector2.zero;
            var max = Vector2.zero;

            foreach (var cell in selected)
            {
                var view = views[cell.Row, cell.Column];
                var rect = view == null ? null : view.GetComponent<RectTransform>();
                if (rect == null) continue;
                var local = RectInAncestor(spreadsheet, rect);
                if (!initialized)
                {
                    min = local.min;
                    max = local.max;
                    initialized = true;
                }
                else
                {
                    min = Vector2.Min(min, local.min);
                    max = Vector2.Max(max, local.max);
                }
            }

            return initialized ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : Rect.zero;
        }

        private static Rect RectInAncestor(RectTransform ancestor, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                var local = ancestor.InverseTransformPoint(corner);
                min = Vector2.Min(min, new Vector2(local.x, local.y));
                max = Vector2.Max(max, new Vector2(local.x, local.y));
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void RegisterDismiss(GameObject root, float timeout)
        {
            if (root == null) return;
            var dismiss = root.AddComponent<PrototypePsychosisDismissTarget>();
            dismiss.Configure(() => Dismiss(root));
            StartCoroutine(DestroyAfter(root, timeout));
        }

        private IEnumerator DestroyAfter(GameObject root, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            Dismiss(root);
        }

        private void Dismiss(GameObject root)
        {
            if (root == null) return;
            active.Remove(root);
            Destroy(root);
        }

        private RectTransform FindSpreadsheetRect()
        {
            if (prototype == null) return null;
            return prototype.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(r => r != null && r.gameObject.name == "Spreadsheet");
        }

        private Sprite PickGlitchSprite(int salt)
        {
            EnsureGlitchSprites();
            if (glitchSprites == null || glitchSprites.Length == 0) return null;
            rng = NextHash(rng ^ unchecked((uint)(salt * 2246822519u)));
            return glitchSprites[(int)(rng % (uint)glitchSprites.Length)];
        }

        private void EnsureGlitchSprites()
        {
            if (glitchSprites != null && glitchSprites.Length > 0) return;
            var atlas = Resources.Load<Texture2D>("Art/glitch_textures");
            if (atlas == null || atlas.width < 4 || atlas.height < 4) return;

            var halfW = atlas.width / 2f;
            var halfH = atlas.height / 2f;
            glitchSprites = new[]
            {
                Sprite.Create(atlas, new Rect(0f, halfH, halfW, halfH), new Vector2(0.5f, 0.5f), 100f),
                Sprite.Create(atlas, new Rect(halfW, halfH, atlas.width - halfW, halfH), new Vector2(0.5f, 0.5f), 100f),
                Sprite.Create(atlas, new Rect(0f, 0f, halfW, atlas.height - halfH), new Vector2(0.5f, 0.5f), 100f),
                Sprite.Create(atlas, new Rect(halfW, 0f, atlas.width - halfW, atlas.height - halfH), new Vector2(0.5f, 0.5f), 100f)
            };
        }

        private T Pick<T>(IReadOnlyList<T> values)
        {
            rng = NextHash(rng);
            return values[(int)(rng % (uint)values.Count)];
        }

        private static string WrongKeyLabel(CellModel cell)
        {
            if (cell?.Occupant?.Kind == ContentKind.FieldKey)
            {
                return cell.Occupant.FieldId switch
                {
                    "hours" => "BONUS",
                    "salary" => "OVERTIME",
                    "overtime" => "SALARY",
                    "bonus" => "HOURS",
                    _ => "#FIELD?"
                };
            }

            if (cell?.Occupant?.Kind == ContentKind.RecordKey)
            {
                return cell.Occupant.RecordId switch
                {
                    "ivanov" => "PETROV",
                    "petrov" => "SIDOROV",
                    "sidorov" => "VOLKOVA",
                    "volkova" => "KIM",
                    "kim" => "IVANOV",
                    _ => "#EMPLOYEE?"
                };
            }

            return "#KEY?";
        }

        private static int CurrentDay()
        {
            var id = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (id.StartsWith("04_", StringComparison.OrdinalIgnoreCase)) return 4;
            if (id.StartsWith("03_", StringComparison.OrdinalIgnoreCase)) return 3;
            if (id.StartsWith("02_", StringComparison.OrdinalIgnoreCase)) return 2;
            return 1;
        }

        private void ClearAll()
        {
            foreach (var go in active)
                if (go != null) Destroy(go);
            active.Clear();
        }

        private void OnDisable() => ClearAll();

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static float Roll(uint value) => (value & 0xFFFFu) / 65535f;

        private static uint NextHash(uint value)
        {
            unchecked
            {
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return value == 0 ? 1u : value;
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
                return hash == 0 ? 1u : hash;
            }
        }

        private sealed class DriftPiece
        {
            public readonly RectTransform Rect;
            public readonly Vector2 Base;
            public readonly Vector2 Target;
            public readonly bool Spectral;
            public readonly float Phase;

            public DriftPiece(RectTransform rect, Vector2 basePosition, Vector2 target, bool spectral, float phase)
            {
                Rect = rect;
                Base = basePosition;
                Target = target;
                Spectral = spectral;
                Phase = phase;
            }
        }

        private enum ManifestationKind
        {
            FalseKey,
            GhostSelection,
            ColumnDrift,
            CellEscape,
            GridTear,
            RowDrift
        }
    }
}
