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
    /// Final low-risk animation polish for the jam build.
    ///
    /// This component deliberately does not own or delay gameplay. Instead it observes the already committed
    /// worksheet state after a turn changes and paints short-lived, raycast-free overlays above the existing UI.
    /// If this presenter is disabled or removed, FC2/Narrative/REF behaviour is unchanged.
    /// </summary>
    [DefaultExecutionOrder(2550)]
    public sealed class PrototypeAnimationPolish : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", PrivateInstance);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", PrivateInstance);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", PrivateInstance);
        private static readonly FieldInfo TurnField = typeof(ExcelHellPrototype).GetField("turn", PrivateInstance);
        private static readonly FieldInfo FinishedField = typeof(ExcelHellPrototype).GetField("finished", PrivateInstance);

        private static readonly Color SortAccent = new(0.34f, 0.69f, 0.96f, 1f);

        private readonly List<GameObject> transientObjects = new();

        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private List<ReportGoal> goals;
        private CellSnapshot[,] snapshot;
        private int lastTurn;
        private bool lastFinished;
        private string lastLevelId;
        private float nextButtonScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeAnimationPolish>() != null) return;

            var root = new GameObject("[PRESENTATION] Animation Polish");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeAnimationPolish>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active)
            {
                if (prototype != null) Bind(null);
                return;
            }

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || cells == null || views == null) return;

            var levelId = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (!string.Equals(levelId, lastLevelId, System.StringComparison.OrdinalIgnoreCase))
            {
                // Level application can replace the whole worksheet in one frame. Treat that as a new baseline,
                // not as one gigantic fake MOVE/SORT animation.
                lastLevelId = levelId;
                lastTurn = ReadTurn();
                lastFinished = ReadFinished();
                snapshot = CaptureSnapshot();
                ClearTransient();
                Debug.Log($"[ANIM/POLISH] Baseline captured for level={levelId}.");
            }

            if (Time.unscaledTime >= nextButtonScan)
            {
                nextButtonScan = Time.unscaledTime + 0.75f;
                EnsureButtonMicroFeedback();
            }

            var currentTurn = ReadTurn();
            var currentFinished = ReadFinished();

            if (currentTurn < lastTurn)
            {
                // Reset/restart path: reset our observation baseline and stay out of gameplay's way.
                lastTurn = currentTurn;
                lastFinished = currentFinished;
                snapshot = CaptureSnapshot();
                ClearTransient();
                return;
            }

            if (currentTurn != lastTurn)
            {
                var currentSnapshot = CaptureSnapshot();
                PlayTurnFeedback(snapshot, currentSnapshot);
                snapshot = currentSnapshot;
                lastTurn = currentTurn;
                lastFinished = currentFinished;
            }
            else if (currentFinished && !lastFinished)
            {
                // Successful SUBMIT finishes the level without consuming a turn. Deadline failure happens during
                // CompletePlayerAction and therefore arrives through the turn-change branch above.
                PlayReportAccepted();
                lastFinished = true;
                snapshot = CaptureSnapshot();
            }
            else
            {
                lastFinished = currentFinished;
            }

            transientObjects.RemoveAll(item => item == null);
        }

        private void Bind(ExcelHellPrototype owner)
        {
            ClearTransient();

            prototype = owner;
            cells = null;
            views = null;
            goals = null;
            snapshot = null;
            lastTurn = 0;
            lastFinished = false;
            lastLevelId = null;
            nextButtonScan = 0f;

            if (prototype == null) return;

            cells = CellsField?.GetValue(prototype) as CellModel[,];
            views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            goals = GoalsField?.GetValue(prototype) as List<ReportGoal>;
            lastTurn = ReadTurn();
            lastFinished = ReadFinished();
            lastLevelId = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            snapshot = CaptureSnapshot();
            EnsureButtonMicroFeedback();

            Debug.Log($"[ANIM/POLISH] Bound presentation observer turn={lastTurn} level={lastLevelId}.");
        }

        private int ReadTurn() => TurnField?.GetValue(prototype) is int value ? value : 0;
        private bool ReadFinished() => FinishedField?.GetValue(prototype) is bool value && value;

        private CellSnapshot[,] CaptureSnapshot()
        {
            if (cells == null) return null;
            var result = new CellSnapshot[cells.GetLength(0), cells.GetLength(1)];
            for (var row = 0; row < cells.GetLength(0); row++)
            for (var column = 0; column < cells.GetLength(1); column++)
                result[row, column] = new CellSnapshot(cells[row, column]);
            return result;
        }

        private void PlayTurnFeedback(CellSnapshot[,] previous, CellSnapshot[,] current)
        {
            if (previous == null || current == null || cells == null || views == null) return;
            if (previous.GetLength(0) != current.GetLength(0) || previous.GetLength(1) != current.GetLength(1)) return;

            var contentChanges = new List<Vector2Int>();
            var destroyed = new List<Vector2Int>();
            Vector2Int? sumTarget = null;
            Vector2Int? sortTarget = null;

            for (var row = 0; row < current.GetLength(0); row++)
            for (var column = 0; column < current.GetLength(1); column++)
            {
                var before = previous[row, column];
                var after = current[row, column];
                var coordinate = new Vector2Int(column, row);

                if (before.OccupantId != after.OccupantId || before.Formula != after.Formula)
                    contentChanges.Add(coordinate);

                if (before.State != CellState.Destroyed && after.State == CellState.Destroyed)
                    destroyed.Add(coordinate);

                if (after.Formula == FormulaKind.Sum && after.OccupantKind == ContentKind.Aggregate &&
                    before.OccupantId != after.OccupantId)
                    sumTarget = coordinate;

                if (after.Formula == FormulaKind.Sort &&
                    (after.OccupantKind == ContentKind.FieldKey || after.OccupantKind == ContentKind.RecordKey) &&
                    before.OccupantId != after.OccupantId)
                    sortTarget = coordinate;
            }

            if (sumTarget.HasValue)
            {
                var target = sumTarget.Value;
                PulseCell(target.y, target.x, PrototypeVisualTheme.Mint, 0.34f, 0.34f);

                foreach (var coordinate in contentChanges.Where(c => c != target).Take(7))
                    PulseCell(coordinate.y, coordinate.x, PrototypeVisualTheme.Mint, 0.10f, 0.20f);

                PulseNamedRect("Formula Bar", PrototypeVisualTheme.Mint, 0.15f, 0.25f);
                Debug.Log($"[ANIM/POLISH] SUM feedback target={CellAddress(target)} changes={contentChanges.Count}.");
            }
            else if (sortTarget.HasValue)
            {
                var target = sortTarget.Value;
                PulseCell(target.y, target.x, SortAccent, 0.30f, 0.30f);
                StartCoroutine(CascadeCells(contentChanges.Where(c => c != target).Take(10).ToList(), SortAccent));
                PulseNamedRect("Formula Bar", SortAccent, 0.12f, 0.24f);
                Debug.Log($"[ANIM/POLISH] SORT feedback target={CellAddress(target)} changes={contentChanges.Count}.");
            }
            else if (contentChanges.Count >= 4)
            {
                // Legacy/fallback SORT has no formula target. A broad simultaneous relocation still deserves the
                // same readable sweep, without trying to infer or mutate gameplay intent.
                StartCoroutine(CascadeCells(contentChanges.Take(10).ToList(), SortAccent));
                Debug.Log($"[ANIM/POLISH] Broad relocation feedback changes={contentChanges.Count}.");
            }
            else
            {
                foreach (var coordinate in contentChanges.Take(6))
                    PulseCell(coordinate.y, coordinate.x, PrototypeVisualTheme.Mint, 0.13f, 0.20f);
            }

            // Player DELETE and natural #REF! expiry both become Destroyed through existing gameplay rules. We
            // intentionally give both the same tiny terminal beat rather than guessing which system owned it.
            foreach (var coordinate in destroyed.Take(5))
                PulseCell(coordinate.y, coordinate.x, PrototypeVisualTheme.Danger, 0.24f, 0.30f);

            if (destroyed.Count > 0)
                Debug.Log($"[ANIM/POLISH] DELETE/#REF terminal feedback cells={destroyed.Count}.");
        }

        private IEnumerator CascadeCells(List<Vector2Int> coordinates, Color color)
        {
            if (coordinates == null || coordinates.Count == 0) yield break;

            var ordered = coordinates
                .OrderBy(c => c.y)
                .ThenBy(c => c.x)
                .ToList();

            foreach (var coordinate in ordered)
            {
                PulseCell(coordinate.y, coordinate.x, color, 0.16f, 0.22f);
                yield return new WaitForSecondsRealtime(0.035f);
            }
        }

        private void PlayReportAccepted()
        {
            if (goals == null || goals.Count == 0) return;

            StartCoroutine(ReportAcceptedRoutine());
            Debug.Log($"[ANIM/POLISH] REPORT ACCEPTED feedback goals={goals.Count}.");
        }

        private IEnumerator ReportAcceptedRoutine()
        {
            foreach (var goal in goals)
            {
                if (goal == null) continue;
                PulseCell(goal.TargetRow, goal.TargetColumn, PrototypeVisualTheme.Mint, 0.38f, 0.42f);
                yield return new WaitForSecondsRealtime(0.07f);
            }

            PulseNamedRect("Tasks Reserved", PrototypeVisualTheme.Mint, 0.18f, 0.34f);
            PulseNamedRect("Clock Reserved", PrototypeVisualTheme.Mint, 0.16f, 0.34f);
        }

        private void PulseCell(int row, int column, Color color, float peakAlpha, float duration)
        {
            if (views == null || row < 0 || column < 0 || row >= views.GetLength(0) || column >= views.GetLength(1)) return;
            var view = views[row, column];
            if (view == null) return;
            PulseRect(view.GetComponent<RectTransform>(), color, peakAlpha, duration);
        }

        private void PulseNamedRect(string objectName, Color color, float peakAlpha, float duration)
        {
            if (prototype == null) return;
            var rect = prototype.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(item => item != null && item.gameObject.name == objectName);
            if (rect != null) PulseRect(rect, color, peakAlpha, duration);
        }

        private void PulseRect(RectTransform target, Color color, float peakAlpha, float duration)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;

            var go = new GameObject("Animation Polish Flash", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(target, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect, 2f);
            rect.localScale = new Vector3(0.90f, 0.90f, 1f);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            color.a = 0f;
            image.color = color;

            go.transform.SetAsLastSibling();
            transientObjects.Add(go);
            StartCoroutine(FlashRoutine(go, rect, image, color, Mathf.Clamp01(peakAlpha), Mathf.Max(0.08f, duration)));
        }

        private IEnumerator FlashRoutine(GameObject owner, RectTransform rect, Image image, Color baseColor, float peakAlpha, float duration)
        {
            var elapsed = 0f;
            while (owner != null && rect != null && image != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var envelope = t < 0.28f ? t / 0.28f : 1f - ((t - 0.28f) / 0.72f);
                envelope = Mathf.Clamp01(envelope);

                var color = baseColor;
                color.a = peakAlpha * envelope;
                image.color = color;

                var scale = Mathf.Lerp(0.90f, 1.025f, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t / 0.55f)));
                rect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            if (owner != null)
            {
                transientObjects.Remove(owner);
                Destroy(owner);
            }
        }

        private void EnsureButtonMicroFeedback()
        {
            if (prototype == null) return;

            foreach (var button in prototype.GetComponentsInChildren<Button>(true))
            {
                if (button == null || button.GetComponent<PrototypeButtonMicroFeedback>() != null) continue;
                button.gameObject.AddComponent<PrototypeButtonMicroFeedback>();
            }
        }

        private string CellAddress(Vector2Int coordinate)
        {
            var row = coordinate.y;
            var column = coordinate.x;
            if (cells == null || row < 0 || column < 0 || row >= cells.GetLength(0) || column >= cells.GetLength(1))
                return "?";
            return cells[row, column]?.Address ?? "?";
        }

        private void ClearTransient()
        {
            StopAllCoroutines();
            foreach (var item in transientObjects)
                if (item != null) Destroy(item);
            transientObjects.Clear();
        }

        private void OnDisable() => ClearTransient();

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private readonly struct CellSnapshot
        {
            public readonly CellState State;
            public readonly FormulaKind Formula;
            public readonly string OccupantId;
            public readonly ContentKind? OccupantKind;

            public CellSnapshot(CellModel cell)
            {
                State = cell?.State ?? CellState.Destroyed;
                Formula = cell?.Formula ?? FormulaKind.None;
                OccupantId = cell?.Occupant?.Id;
                OccupantKind = cell?.Occupant != null ? cell.Occupant.Kind : (ContentKind?)null;
            }
        }
    }

    /// <summary>
    /// Shared button micro-motion. It only adjusts localScale and always restores the exact authored scale on
    /// disable, so layout, navigation, callbacks and Button colour transitions remain owned by the existing UI.
    /// </summary>
    public sealed class PrototypeButtonMicroFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform rect;
        private Vector3 baseScale;
        private Vector3 targetScale;
        private bool pointerInside;
        private bool pointerDown;
        private bool initialized;

        private void Awake()
        {
            rect = transform as RectTransform;
            CaptureBaseScale();
        }

        private void OnEnable()
        {
            if (rect == null) rect = transform as RectTransform;
            CaptureBaseScale();
        }

        private void CaptureBaseScale()
        {
            if (rect == null) return;
            baseScale = rect.localScale;
            targetScale = baseScale;
            pointerInside = false;
            pointerDown = false;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized || rect == null) return;
            var response = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
            rect.localScale = Vector3.Lerp(rect.localScale, targetScale, response);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!initialized) CaptureBaseScale();
            pointerInside = true;
            UpdateTarget();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            pointerDown = false;
            UpdateTarget();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            pointerDown = true;
            UpdateTarget();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            pointerDown = false;
            UpdateTarget();
        }

        private void UpdateTarget()
        {
            var factor = pointerDown ? 0.985f : pointerInside ? 1.015f : 1f;
            targetScale = new Vector3(baseScale.x * factor, baseScale.y * factor, baseScale.z);
        }

        private void OnDisable()
        {
            if (rect != null && initialized) rect.localScale = baseScale;
            pointerInside = false;
            pointerDown = false;
        }
    }
}
