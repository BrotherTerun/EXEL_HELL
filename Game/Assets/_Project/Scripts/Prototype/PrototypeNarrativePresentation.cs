using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Production presentation glue for authored narrative content. It deliberately owns no gameplay state:
    /// CellMessages are raycast-catching overlays, report captions are labels, and SystemStatus lives in chrome.
    /// </summary>
    [DefaultExecutionOrder(2050)]
    public sealed class PrototypeNarrativePresentation : MonoBehaviour, INarrativeEffectReceiver
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);

        private static readonly char[] AboveMarks =
            { '\u0300', '\u0301', '\u0302', '\u0307', '\u0308', '\u0311', '\u0342', '\u0344' };
        private static readonly char[] BelowMarks =
            { '\u0316', '\u0317', '\u0323', '\u0324', '\u0329', '\u0330', '\u0347', '\u0348' };
        private static readonly char[] OverlayMarks =
            { '\u0334', '\u0335', '\u0336', '\u0338', '\u035C', '\u0360' };

        private ExcelHellPrototype prototype;
        private NarrativeEventRunner runner;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private Canvas canvas;
        private RectTransform submitReserved;
        private RectTransform helpReserved;
        private Text submitLabel;
        private Button legacySubmit;
        private GameObject statusRoot;
        private Text statusText;
        private Coroutine statusRoutine;
        private readonly HashSet<string> manifestedCells = new();
        private readonly List<GameObject> reportCaptions = new();
        private bool presentationBound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeNarrativePresentation>() != null) return;
            var root = new GameObject("[PRESENTATION] Narrative Content");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeNarrativePresentation>();
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            BindRunner();
            if (prototype != null && !presentationBound) TryBuildPresentation();

            if (submitLabel != null) submitLabel.text = "ОТПРАВИТЬ ОТЧЁТ";
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            cells = null;
            views = null;
            canvas = null;
            submitReserved = null;
            helpReserved = null;
            submitLabel = null;
            legacySubmit = null;
            presentationBound = false;
            manifestedCells.Clear();
            ClearReportCaptions();
            DestroyStatus();

            if (prototype == null) return;
            cells = CellsField?.GetValue(prototype) as CellModel[,];
            views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
        }

        private void BindRunner()
        {
            var current = FindFirstObjectByType<NarrativeEventRunner>();
            if (current == runner) return;
            if (runner != null) runner.UnregisterReceiver(this);
            runner = current;
            if (runner != null) runner.RegisterReceiver(this);
        }

        private void TryBuildPresentation()
        {
            if (prototype == null || canvas == null || cells == null || views == null) return;
            submitReserved = FindRect(canvas.transform, "Tasks Reserved");
            helpReserved = FindRect(canvas.transform, "Help Reserved");
            var app = FindRect(canvas.transform, "Spreadsheet App");
            if (submitReserved == null || helpReserved == null || app == null) return;

            legacySubmit = prototype.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.gameObject.name == "ui.submit");
            if (legacySubmit == null) return;

            // Layout only. ProductionHud owns the actual submit callback now, so there is no late binding race.
            submitReserved.sizeDelta = new Vector2(190f, submitReserved.sizeDelta.y);
            helpReserved.anchoredPosition = new Vector2(214f, helpReserved.anchoredPosition.y);
            submitLabel = submitReserved.GetComponentsInChildren<Text>(true).FirstOrDefault();

            BuildSystemStatus(app);
            BuildReportCaptions();
            presentationBound = true;
            Debug.Log("[NARRATIVE/UI] Report captions, system status and CellMessage presenter bound.");
        }

        private void BuildSystemStatus(RectTransform app)
        {
            statusRoot = new GameObject("System Status", typeof(RectTransform));
            statusRoot.transform.SetParent(app, false);
            SetTopLeft(statusRoot.GetComponent<RectTransform>(), 272f, -8f, 680f, 40f);
            statusText = CreateText(statusRoot.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(statusText.rectTransform);
            statusText.font = PrototypeVisualTheme.MonoFont;
            statusText.color = new Color(0.69f, 0.75f, 0.82f, 0.94f);
            statusText.raycastTarget = false;
            statusRoot.SetActive(false);
        }

        private void BuildReportCaptions()
        {
            var level = PrototypeLevelRuntime.Current;
            if (level?.GoalLayout == null) return;
            foreach (var placement in level.GoalLayout)
            {
                if (!InsideBoard(placement.Row, placement.Column)) continue;
                var view = views[placement.Row, placement.Column];
                if (view == null) continue;

                var go = new GameObject("Report Goal Caption", typeof(RectTransform));
                go.transform.SetParent(view.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -4f);
                rect.sizeDelta = new Vector2(-8f, 18f);

                var text = CreateText(go.transform, NarrativeProductionContent.ShortGoalLabel(placement.Goal),
                    11, FontStyle.Bold, TextAnchor.UpperCenter);
                Stretch(text.rectTransform);
                text.font = PrototypeVisualTheme.MonoFont;
                text.color = new Color(0.22f, 0.48f, 0.31f, 0.95f);
                text.raycastTarget = false;
                reportCaptions.Add(go);
            }
        }

        public bool CanReceive(NarrativeEffectType type) =>
            type == NarrativeEffectType.CellMessage || type == NarrativeEffectType.SystemStatus;

        public void Receive(NarrativeEffectTicket ticket)
        {
            if (ticket == null) return;
            var effect = ticket.Request.Effect;
            switch (effect.type)
            {
                case NarrativeEffectType.CellMessage:
                    ShowCellMessage(ticket.Request.EventId, effect);
                    ticket.Complete();
                    break;
                case NarrativeEffectType.SystemStatus:
                    ShowSystemStatus(effect.text, Mathf.Max(0.25f, effect.lifetime.duration));
                    ticket.Complete();
                    break;
                default:
                    ticket.Complete();
                    break;
            }
        }

        private void ShowCellMessage(string eventId, NarrativeEffectDefinition effect)
        {
            if (views == null || cells == null) return;
            var target = ResolveTarget(eventId, effect.row, effect.column);
            if (target == null)
            {
                Debug.LogWarning($"[CELL-MESSAGE] No safe empty cell for event={eventId}; manifestation skipped.");
                return;
            }

            var address = target.Address;
            manifestedCells.Add(address);
            var view = views[target.Row, target.Column];
            var overlay = new GameObject($"Cell Message {eventId}", typeof(RectTransform), typeof(Image), typeof(Button));
            overlay.transform.SetParent(view.transform, false);
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.transform.SetAsLastSibling();

            var accent = ManifestationColor(eventId);
            var image = overlay.GetComponent<Image>();
            image.color = new Color(accent.r, accent.g, accent.b, 0.13f);
            image.raycastTarget = true;
            var text = CreateText(overlay.transform, string.Empty, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 6f);
            text.font = PrototypeVisualTheme.MonoFont;
            text.color = accent;
            text.raycastTarget = false;

            var button = overlay.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                manifestedCells.Remove(address);
                Destroy(overlay);
                Debug.Log($"[CELL-MESSAGE] Dismissed event={eventId} cell={address}; gameplay click consumed.");
            });

            var display = CorruptCellMessage(effect.text ?? string.Empty, eventId, out var mode);
            StartCoroutine(TypeCellMessage(text, display));
            Debug.Log($"[CELL-MESSAGE] Show event={eventId} cell={address} mode={mode} text=\"{effect.text}\" rendered=\"{display}\".");
        }

        private CellModel ResolveTarget(string eventId, int requestedRow, int requestedColumn)
        {
            if (requestedRow >= 0 && requestedColumn >= 0 && InsideBoard(requestedRow, requestedColumn))
            {
                var requested = cells[requestedRow, requestedColumn];
                if (SafeForManifestation(requested)) return requested;
            }

            var candidates = new List<CellModel>();
            foreach (var cell in cells)
                if (SafeForManifestation(cell)) candidates.Add(cell);
            if (candidates.Count == 0) return null;

            candidates.Sort((a, b) => string.CompareOrdinal(a.Address, b.Address));
            var seed = StableHash((PrototypeLevelRuntime.Current?.Id ?? string.Empty) + ":" + (eventId ?? string.Empty));
            return candidates[(int)(seed % (uint)candidates.Count)];
        }

        private bool SafeForManifestation(CellModel cell)
        {
            if (cell == null || cell.State != CellState.Normal || !cell.IsEmpty || cell.IsFormula) return false;
            if (manifestedCells.Contains(cell.Address)) return false;
            var goals = PrototypeLevelRuntime.Current?.GoalLayout;
            if (goals != null && goals.Any(goal => goal.Row == cell.Row && goal.Column == cell.Column)) return false;
            return true;
        }

        private IEnumerator TypeCellMessage(Text target, string fullText)
        {
            if (target == null) yield break;
            target.text = string.Empty;

            // Reveal whole grapheme clusters at once so ZALGO combining marks appear together with their base
            // glyph instead of adding invisible pauses. The cadence is intentionally slow enough to tie the
            // manifestation to the board event that caused it.
            var elements = StringInfo.GetTextElementEnumerator(fullText ?? string.Empty);
            var builder = new StringBuilder(fullText?.Length ?? 0);
            while (elements.MoveNext())
            {
                if (target == null) yield break;
                builder.Append(elements.GetTextElement());
                target.text = builder.ToString();
                yield return new WaitForSecondsRealtime(0.200f);
            }
        }

        private string CorruptCellMessage(string source, string eventId, out CorruptionMode mode)
        {
            mode = CorruptionMode.None;
            if (string.IsNullOrEmpty(source)) return source;

            var levelId = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            var day = levelId.StartsWith("04_") ? 4 : levelId.StartsWith("03_") ? 3 : levelId.StartsWith("02_") ? 2 : 1;
            if (day <= 1) return source;

            var state = StableHash($"corrupt:{levelId}:{eventId}:{source}");
            mode = (state % 3u) switch
            {
                0u => CorruptionMode.Zalgo | CorruptionMode.Symbols,
                1u => CorruptionMode.Zalgo | CorruptionMode.MixedCase,
                _ => CorruptionMode.Symbols | CorruptionMode.MixedCase
            };

            var zalgoChance = day == 2 ? 0.46f : day == 3 ? 0.64f : 0.80f;
            var symbolChance = day == 2 ? 0.20f : day == 3 ? 0.30f : 0.42f;
            var caseChance = day == 2 ? 0.34f : day == 3 ? 0.48f : 0.62f;
            var minMarks = day == 2 ? 2 : day == 3 ? 3 : 4;
            var maxMarks = day == 2 ? 4 : day == 3 ? 6 : 8;

            var builder = new StringBuilder(source.Length * 4);
            foreach (var original in source)
            {
                var rendered = original;
                if (char.IsLetter(original) && mode.HasFlag(CorruptionMode.MixedCase))
                {
                    state = NextHash(state);
                    if (Roll(state) <= caseChance)
                        rendered = char.IsUpper(original) ? char.ToLowerInvariant(original) : char.ToUpperInvariant(original);
                }

                if (char.IsLetter(original) && mode.HasFlag(CorruptionMode.Symbols))
                {
                    state = NextHash(state);
                    if (Roll(state) <= symbolChance && TrySyntaxSubstitute(rendered, out var replacement))
                        rendered = replacement;
                }

                builder.Append(rendered);

                if (!char.IsLetter(original) || !mode.HasFlag(CorruptionMode.Zalgo)) continue;
                state = NextHash(state);
                if (Roll(state) > zalgoChance) continue;

                state = NextHash(state);
                var markCount = minMarks + (int)(state % (uint)(maxMarks - minMarks + 1));
                for (var i = 0; i < markCount; i++)
                {
                    state = NextHash(state);
                    var family = (int)(state % 3u);
                    var familyMarks = family == 0 ? AboveMarks : family == 1 ? BelowMarks : OverlayMarks;
                    state = NextHash(state);
                    builder.Append(familyMarks[(int)(state % (uint)familyMarks.Length)]);
                }
            }

            return builder.ToString();
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
                default:
                    replacement = source;
                    return false;
            }
        }

        private Color ManifestationColor(string eventId)
        {
            var levelId = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (levelId.StartsWith("04_"))
                return (StableHash(eventId ?? string.Empty) & 1u) == 0u
                    ? new Color(0.08f, 0.78f, 0.80f, 1f)
                    : new Color(0.86f, 0.20f, 0.78f, 1f);
            if (levelId.StartsWith("03_")) return new Color(0.82f, 0.16f, 0.64f, 1f);
            return new Color(0.86f, 0.08f, 0.11f, 1f);
        }

        private void ShowSystemStatus(string message, float duration)
        {
            if (statusRoot == null || statusText == null) return;
            if (statusRoutine != null) StopCoroutine(statusRoutine);
            statusText.text = message ?? string.Empty;
            statusRoot.SetActive(true);
            statusRoutine = StartCoroutine(HideStatusAfter(duration));
            Debug.Log($"[SYSTEM-STATUS] {message}");
        }

        private IEnumerator HideStatusAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            if (statusRoot != null) statusRoot.SetActive(false);
            statusRoutine = null;
        }

        private bool InsideBoard(int row, int column) =>
            cells != null && row >= 0 && column >= 0 && row < cells.GetLength(0) && column < cells.GetLength(1);

        private void ClearReportCaptions()
        {
            foreach (var caption in reportCaptions)
                if (caption != null) Destroy(caption);
            reportCaptions.Clear();
        }

        private void DestroyStatus()
        {
            if (statusRoutine != null) StopCoroutine(statusRoutine);
            statusRoutine = null;
            if (statusRoot != null) Destroy(statusRoot);
            statusRoot = null;
            statusText = null;
        }

        private void OnDisable()
        {
            if (runner != null) runner.UnregisterReceiver(this);
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

        private static RectTransform FindRect(Transform root, string objectName)
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.gameObject.name == objectName) return rect;
            return null;
        }

        private static Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = PrototypeVisualTheme.UiFont;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
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

        [Flags]
        private enum CorruptionMode
        {
            None = 0,
            Zalgo = 1,
            Symbols = 2,
            MixedCase = 4
        }
    }
}
