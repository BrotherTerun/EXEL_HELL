using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Draws #REF! telegraphs as stable translucent layers above complete cell presentation.
    /// New outbreak spawn and active spread use deliberately different visual language.
    /// </summary>
    [DefaultExecutionOrder(1150)]
    public sealed class PrototypeRefTelegraphLayer : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        // Spawn = warning amber + explicit border. Spread = danger red, no border.
        // Border is built from four thin Images: Unity Outline duplicates the whole graphic
        // and makes a translucent full-cell overlay appear almost opaque.
        private static readonly Color SpawnFillColor = new(1f, 0.68f, 0.12f, 0.22f);
        private static readonly Color SpawnBorderColor = new(1f, 0.88f, 0.28f, 0.95f);
        private static readonly Color SpreadFillColor = new(0.96f, 0.22f, 0.10f, 0.30f);

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
            public Image Fill;
            public readonly List<Image> Border = new();
        }

        private readonly Dictionary<CellModel, TelegraphVisual> overlays = new();
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

            foreach (var visual in overlays.Values)
                SetHidden(visual);

            CellModel spawnTarget = null;
            var pendingValue = PendingSpawnField?.GetValue(prototype);
            if (pendingValue is SpawnIntent pending)
                spawnTarget = cells[pending.Row, pending.Column];

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
                ShowSpawn(EnsureOverlay(spawnTarget));
            }
        }

        private void Bind(ExcelHellPrototype owner)
        {
            foreach (var visual in overlays.Values)
                if (visual?.Fill != null) Destroy(visual.Fill.gameObject);
            overlays.Clear();

            prototype = owner;
            cells = prototype == null ? null : CellsField?.GetValue(prototype) as CellModel[,];
            views = prototype == null ? null : ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            selection = prototype == null ? null : SelectionField?.GetValue(prototype) as List<CellModel>;
            goals = prototype == null ? null : GoalsField?.GetValue(prototype) as List<ReportGoal>;
        }

        private TelegraphVisual EnsureOverlay(CellModel cell)
        {
            if (overlays.TryGetValue(cell, out var existing) && existing?.Fill != null) return existing;
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

            var visual = new TelegraphVisual { Fill = fill };
            visual.Border.Add(CreateBorder(root.transform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 0f)));
            visual.Border.Add(CreateBorder(root.transform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 3f)));
            visual.Border.Add(CreateBorder(root.transform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(3f, 0f)));
            visual.Border.Add(CreateBorder(root.transform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-3f, 0f), new Vector2(0f, 0f)));

            overlays[cell] = visual;
            return visual;
        }

        private static Image CreateBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject($"Spawn Border {name}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = go.GetComponent<Image>();
            image.color = SpawnBorderColor;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static void SetHidden(TelegraphVisual visual)
        {
            if (visual?.Fill != null) visual.Fill.enabled = false;
            if (visual == null) return;
            foreach (var border in visual.Border)
                if (border != null) border.enabled = false;
        }

        private static void ShowSpawn(TelegraphVisual visual)
        {
            if (visual?.Fill == null) return;
            visual.Fill.color = SpawnFillColor;
            visual.Fill.enabled = true;
            visual.Fill.rectTransform.SetAsLastSibling();
            foreach (var border in visual.Border)
            {
                if (border == null) continue;
                border.color = SpawnBorderColor;
                border.enabled = true;
            }
        }

        private static void ShowSpread(TelegraphVisual visual)
        {
            if (visual?.Fill == null) return;
            visual.Fill.color = SpreadFillColor;
            visual.Fill.enabled = true;
            visual.Fill.rectTransform.SetAsLastSibling();
            foreach (var border in visual.Border)
                if (border != null) border.enabled = false;
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
