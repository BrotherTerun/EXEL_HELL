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
    /// L3-L4 spreadsheet psychosis. Every manifestation is a presentation duplicate layered above the worksheet;
    /// gameplay cells, ContentToken values, formula ownership, selection and turn economy are never modified.
    /// Some manifestations are dismissible by clicking the phantom itself, some decay on their own.
    /// </summary>
    [DefaultExecutionOrder(2180)]
    public sealed class PrototypeSpreadsheetPsychosisPresenter : MonoBehaviour
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
        private uint rng = 0xC0FFEEu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeSpreadsheetPsychosisPresenter>() != null) return;
            var root = new GameObject("[PRESENTATION] Spreadsheet Psychosis");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeSpreadsheetPsychosisPresenter>();
        }

        private void LateUpdate()
        {
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
                rng = StableHash(levelId + ":psychosis");
                return;
            }

            var day = CurrentDay();
            if (day < 3) return;

            var turn = ReadTurn();
            if (turn == lastTurn) return;
            lastTurn = turn;

            var cap = day >= 4 ? 2 : 1;
            if (active.Count >= cap) return;

            rng = NextHash(rng ^ unchecked((uint)(turn * 2654435761u)));
            var roll = Roll(rng);
            var threshold = day >= 4 ? 0.72f : 0.48f;
            if (roll > threshold) return;

            var kindCount = day >= 4 ? 5 : 3;
            rng = NextHash(rng);
            var kind = (ManifestationKind)(rng % (uint)kindCount);
            Spawn(kind, day);
        }

        private void Bind(ExcelHellPrototype owner)
        {
            ClearAll();
            prototype = owner;
            cells = prototype == null ? null : CellsField?.GetValue(prototype) as CellModel[,];
            views = prototype == null ? null : ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            lastTurn = prototype == null ? -1 : ReadTurn();
            lastLevelId = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
        }

        private int ReadTurn() => TurnField?.GetValue(prototype) is int value ? value : 0;

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
                case ManifestationKind.ColumnEcho:
                    SpawnColumnEcho(day);
                    break;
                case ManifestationKind.CellEscape:
                    SpawnCellEscape(day);
                    break;
                case ManifestationKind.GridTear:
                    SpawnGridTear(day);
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

            var root = CreateOverlayRoot(view.transform, "Psychosis False Key", true);
            var background = root.GetComponent<Image>();
            var bg = day >= 4 ? Magenta : Cyan;
            bg.a = day >= 4 ? 0.10f : 0.07f;
            background.color = bg;

            var ghost = CreateText(root.transform, WrongKeyLabel(cell), original);
            ghost.color = day >= 4 ? Magenta : Blue;
            ghost.fontStyle = FontStyle.Bold;
            ghost.rectTransform.anchoredPosition = new Vector2(2f, day >= 4 ? 1f : 0f);

            var second = CreateText(root.transform, WrongKeyLabel(cell), original);
            var secondColor = Cyan;
            secondColor.a = 0.50f;
            second.color = secondColor;
            second.rectTransform.anchoredPosition = new Vector2(-2f, -1f);

            RegisterDismiss(root, day >= 4 ? 4.8f : 3.8f);
            Debug.Log($"[PSYCHOSIS] FalseKey at {cell.Address} day={day}.");
        }

        private void SpawnGhostSelection(int day)
        {
            var candidates = cells.Cast<CellModel>().Where(c => c != null && c.State == CellState.Normal).ToList();
            if (candidates.Count == 0) return;
            var cell = Pick(candidates);
            var view = views[cell.Row, cell.Column];
            if (view == null) return;

            var root = CreateOverlayRoot(view.transform, "Psychosis Ghost Selection", false);
            var image = root.GetComponent<Image>();
            var c = day >= 4 ? Magenta : Cyan;
            c.a = day >= 4 ? 0.15f : 0.10f;
            image.color = c;
            CreateEdge(root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), Vector2.zero, c);
            CreateEdge(root.transform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f), c);
            CreateEdge(root.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(2f, 0f), c);
            CreateEdge(root.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-2f, 0f), Vector2.zero, c);

            RegisterTimed(root, day >= 4 ? 2.8f : 2.2f);
            StartCoroutine(GhostSelectionRoutine(root.GetComponent<RectTransform>(), day));
            Debug.Log($"[PSYCHOSIS] GhostSelection at {cell.Address} day={day}.");
        }

        private void SpawnColumnEcho(int day)
        {
            if (views.GetLength(1) == 0) return;
            rng = NextHash(rng);
            var column = (int)(rng % (uint)views.GetLength(1));
            var offset = day >= 4 ? 10f : 6f;
            var group = new GameObject("Psychosis Column Echo Group");
            group.transform.SetParent(transform, false);
            active.Add(group);

            var count = 0;
            for (var row = 0; row < views.GetLength(0) && count < 5; row++)
            {
                var cell = cells[row, column];
                var view = views[row, column];
                if (cell == null || view == null || cell.State != CellState.Normal) continue;
                var sourceText = CellLabelField?.GetValue(view) as Text;

                var root = CreateOverlayRoot(view.transform, "Psychosis Column Echo", false);
                root.transform.SetParent(view.transform, false);
                var image = root.GetComponent<Image>();
                var c = count % 2 == 0 ? Cyan : Magenta;
                c.a = day >= 4 ? 0.08f : 0.05f;
                image.color = c;
                root.GetComponent<RectTransform>().anchoredPosition = new Vector2((count % 2 == 0 ? 1f : -1f) * offset, 0f);

                if (sourceText != null && !string.IsNullOrEmpty(sourceText.text))
                {
                    var ghost = CreateText(root.transform, sourceText.text, sourceText);
                    var tc = count % 2 == 0 ? Cyan : Magenta;
                    tc.a = 0.52f;
                    ghost.color = tc;
                }

                root.transform.SetParent(group.transform, true);
                count++;
            }

            if (count == 0)
            {
                active.Remove(group);
                Destroy(group);
                return;
            }

            RegisterTimed(group, day >= 4 ? 3.4f : 2.4f);
            Debug.Log($"[PSYCHOSIS] ColumnEcho column={ExcelHellPrototype.ColumnName(column)} day={day} cells={count}.");
        }

        private void SpawnCellEscape(int day)
        {
            var candidates = cells.Cast<CellModel>()
                .Where(c => c != null && c.State == CellState.Normal && c.Occupant != null)
                .ToList();
            if (candidates.Count == 0) return;

            var cell = Pick(candidates);
            var view = views[cell.Row, cell.Column];
            var source = view == null ? null : CellLabelField?.GetValue(view) as Text;
            if (view == null || source == null) return;

            var root = CreateOverlayRoot(view.transform, "Psychosis Cell Escape", true);
            var image = root.GetComponent<Image>();
            var bg = Magenta;
            bg.a = 0.08f;
            image.color = bg;
            var text = CreateText(root.transform, source.text, source);
            text.color = Magenta;

            RegisterDismiss(root, 4.4f);
            StartCoroutine(CellEscapeRoutine(root.GetComponent<RectTransform>(), text));
            Debug.Log($"[PSYCHOSIS] CellEscape at {cell.Address} day={day}.");
        }

        private void SpawnGridTear(int day)
        {
            var spreadsheet = FindSpreadsheetRect();
            if (spreadsheet == null) return;

            var root = new GameObject("Psychosis Grid Tear", typeof(RectTransform));
            root.transform.SetParent(spreadsheet, false);
            var rect = root.GetComponent<RectTransform>();
            Stretch(rect);
            rect.SetAsLastSibling();
            active.Add(root);

            var bars = day >= 4 ? 6 : 4;
            for (var i = 0; i < bars; i++)
            {
                rng = NextHash(rng);
                var y = 0.10f + Roll(rng) * 0.80f;
                rng = NextHash(rng);
                var h = Mathf.Lerp(0.004f, 0.018f, Roll(rng));
                var color = i % 3 == 0 ? Red : i % 2 == 0 ? Magenta : Cyan;
                color.a = day >= 4 ? 0.42f : 0.24f;
                var bar = CreateEdge(root.transform, new Vector2(0f, y), new Vector2(1f, Mathf.Min(1f, y + h)), Vector2.zero, Vector2.zero, color);
                bar.raycastTarget = false;
                var barRect = bar.rectTransform;
                barRect.anchoredPosition = new Vector2((i % 2 == 0 ? 1f : -1f) * (day >= 4 ? 7f : 3f), 0f);
            }

            RegisterTimed(root, day >= 4 ? 1.6f : 1.1f);
            StartCoroutine(GridTearRoutine(rect, day));
            Debug.Log($"[PSYCHOSIS] GridTear day={day}.");
        }

        private IEnumerator GhostSelectionRoutine(RectTransform rect, int day)
        {
            if (rect == null) yield break;
            var start = Time.unscaledTime;
            var duration = day >= 4 ? 2.8f : 2.2f;
            while (rect != null && Time.unscaledTime - start < duration)
            {
                var t = Time.unscaledTime - start;
                rect.anchoredPosition = new Vector2(Mathf.Sin(t * 17f) * (day >= 4 ? 2f : 1f), Mathf.Cos(t * 13f) * 0.6f);
                yield return null;
            }
        }

        private IEnumerator CellEscapeRoutine(RectTransform rect, Text text)
        {
            if (rect == null) yield break;
            var start = Time.unscaledTime;
            while (rect != null && Time.unscaledTime - start < 4.4f)
            {
                var t = Time.unscaledTime - start;
                rect.anchoredPosition = new Vector2(Mathf.Lerp(0f, 20f, Mathf.Clamp01(t / 3.5f)) + Mathf.Sin(t * 23f) * 2.5f,
                    Mathf.Sin(t * 5.7f) * 2f);
                if (text != null)
                {
                    var c = Magenta;
                    c.a = 0.72f + Mathf.Sin(t * 19f) * 0.18f;
                    text.color = c;
                }
                yield return null;
            }
        }

        private IEnumerator GridTearRoutine(RectTransform rect, int day)
        {
            if (rect == null) yield break;
            var start = Time.unscaledTime;
            var duration = day >= 4 ? 1.6f : 1.1f;
            while (rect != null && Time.unscaledTime - start < duration)
            {
                var t = Time.unscaledTime - start;
                rect.anchoredPosition = new Vector2(Mathf.Sin(t * 38f) * (day >= 4 ? 2.4f : 1.2f), 0f);
                yield return null;
            }
        }

        private GameObject CreateOverlayRoot(Transform parent, string name, bool capturesClick)
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
            text.raycastTarget = false;
            Stretch(text.rectTransform, 2f);
            return text;
        }

        private static Image CreateEdge(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject("Psychosis Edge", typeof(RectTransform), typeof(Image));
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

        private void RegisterDismiss(GameObject root, float timeout)
        {
            if (root == null) return;
            var dismiss = root.AddComponent<PrototypePsychosisDismissTarget>();
            dismiss.Configure(() => Dismiss(root));
            StartCoroutine(DestroyAfter(root, timeout));
        }

        private void RegisterTimed(GameObject root, float timeout)
        {
            if (root == null) return;
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
                return hash == 0 ? 1u : hash;
            }
        }

        private enum ManifestationKind
        {
            FalseKey,
            GhostSelection,
            ColumnEcho,
            CellEscape,
            GridTear
        }
    }

    public sealed class PrototypePsychosisDismissTarget : MonoBehaviour, IPointerClickHandler
    {
        private Action onDismiss;

        public void Configure(Action callback) => onDismiss = callback;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            eventData.Use();
            var callback = onDismiss;
            onDismiss = null;
            callback?.Invoke();
        }
    }
}
