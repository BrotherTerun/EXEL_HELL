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

        // Spawn = warning amber + strong border. Spread = danger red, no border.
        private static readonly Color SpawnFillColor = new(1f, 0.68f, 0.12f, 0.28f);
        private static readonly Color SpawnOutlineColor = new(1f, 0.88f, 0.28f, 0.95f);
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
            public Image Image;
            public Outline Outline;
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

            // Active spread may coexist with a countdown to a new outbreak. Show both.
            // If both resolve to the same cell, spawn warning wins because it is the rarer event.
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
                if (visual?.Image != null) Destroy(visual.Image.gameObject);
            overlays.Clear();

            prototype = owner;
            cells = prototype == null ? null : CellsField?.GetValue(prototype) as CellModel[,];
            views = prototype == null ? null : ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            selection = prototype == null ? null : SelectionField?.GetValue(prototype) as List<CellModel>;
            goals = prototype == null ? null : GoalsField?.GetValue(prototype) as List<ReportGoal>;
        }

        private TelegraphVisual EnsureOverlay(CellModel cell)
        {
            if (overlays.TryGetValue(cell, out var existing) && existing?.Image != null) return existing;
            var view = views[cell.Row, cell.Column];
            if (view == null) return null;

            var go = new GameObject("REF Telegraph Overlay", typeof(RectTransform), typeof(Image), typeof(Outline));
            go.transform.SetParent(view.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;

            var outline = go.GetComponent<Outline>();
            outline.useGraphicAlpha = false;
            outline.effectColor = SpawnOutlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.enabled = false;

            var visual = new TelegraphVisual { Image = image, Outline = outline };
            overlays[cell] = visual;
            return visual;
        }

        private static void SetHidden(TelegraphVisual visual)
        {
            if (visual?.Image != null) visual.Image.enabled = false;
            if (visual?.Outline != null) visual.Outline.enabled = false;
        }

        private static void ShowSpawn(TelegraphVisual visual)
        {
            if (visual?.Image == null) return;
            visual.Image.color = SpawnFillColor;
            visual.Image.enabled = true;
            visual.Image.rectTransform.SetAsLastSibling();
            if (visual.Outline == null) return;
            visual.Outline.effectColor = SpawnOutlineColor;
            visual.Outline.effectDistance = new Vector2(3f, -3f);
            visual.Outline.enabled = true;
        }

        private static void ShowSpread(TelegraphVisual visual)
        {
            if (visual?.Image == null) return;
            visual.Image.color = SpreadFillColor;
            visual.Image.enabled = true;
            visual.Image.rectTransform.SetAsLastSibling();
            if (visual.Outline != null) visual.Outline.enabled = false;
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
