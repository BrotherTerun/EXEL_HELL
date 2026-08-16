using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Draws the #REF! spawn/move telegraph as one stable translucent layer above the
    /// complete cell presentation. It never participates in raycasts.
    /// </summary>
    [DefaultExecutionOrder(1150)]
    public sealed class PrototypeRefTelegraphLayer : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        // Semi-transparent on purpose: the player must still read the token/formula below it.
        private static readonly Color TelegraphColor = new(1f, 0.58f, 0.12f, 0.34f);
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

        private readonly Dictionary<CellModel, Image> overlays = new();
        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private List<CellModel> selection;
        private List<ReportGoal> goals;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeRefTelegraphLayer>() != null) return;
            var helper = new GameObject("EXEL HELL REF Telegraph Layer").AddComponent<PrototypeRefTelegraphLayer>();
            DontDestroyOnLoad(helper.gameObject);
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || cells == null || views == null) return;

            CellModel target = null;
            var pendingValue = PendingSpawnField?.GetValue(prototype);
            if (pendingValue is SpawnIntent pending)
                target = cells[pending.Row, pending.Column];
            else
            {
                var intentValue = CurrentIntentField?.GetValue(prototype);
                if (intentValue is AnomalyIntent intent)
                    target = cells[intent.TargetRow, intent.TargetColumn];
            }

            foreach (var pair in overlays)
                if (pair.Value != null) pair.Value.enabled = false;

            if (target == null || target.State != CellState.Normal) return;

            // ExcelHellCellView still owns the legacy intent background. Neutralize that one
            // target back to its normal presentation, then draw the unified transparent layer.
            RestoreUnderlyingBackground(target);

            var overlay = EnsureOverlay(target);
            if (overlay == null) return;
            overlay.color = TelegraphColor;
            overlay.enabled = true;

            // Stable ordering: always last. Unlike SetSiblingIndex(relativeIndex), this cannot
            // swap places with Formula 2.0 Interaction on alternating frames.
            overlay.rectTransform.SetAsLastSibling();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            foreach (var overlay in overlays.Values)
                if (overlay != null) Destroy(overlay.gameObject);
            overlays.Clear();

            prototype = owner;
            cells = prototype == null ? null : CellsField?.GetValue(prototype) as CellModel[,];
            views = prototype == null ? null : ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            selection = prototype == null ? null : SelectionField?.GetValue(prototype) as List<CellModel>;
            goals = prototype == null ? null : GoalsField?.GetValue(prototype) as List<ReportGoal>;
        }

        private Image EnsureOverlay(CellModel cell)
        {
            if (overlays.TryGetValue(cell, out var existing) && existing != null) return existing;
            var view = views[cell.Row, cell.Column];
            if (view == null) return null;

            var go = new GameObject("REF Telegraph Overlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(view.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();

            var image = go.GetComponent<Image>();
            image.color = TelegraphColor;
            image.raycastTarget = false;
            image.enabled = false;
            overlays[cell] = image;
            return image;
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
    }
}
