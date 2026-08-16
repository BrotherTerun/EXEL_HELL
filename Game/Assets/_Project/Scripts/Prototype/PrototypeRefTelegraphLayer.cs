using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Keeps #REF! spawn/move telegraph visually above FormulaCell background tint.
    /// Formula Cells 2.0 repaints the base cell Image in LateUpdate, so formula targets need
    /// a dedicated render layer between the base background and the interaction/text overlay.
    /// </summary>
    [DefaultExecutionOrder(1150)]
    public sealed class PrototypeRefTelegraphLayer : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Color TelegraphColor = new(1f, 0.73f, 0.34f, 1f);

        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);
        private static readonly FieldInfo PendingSpawnField = typeof(ExcelHellPrototype).GetField("pendingSpawnIntent", Flags);
        private static readonly FieldInfo CurrentIntentField = typeof(ExcelHellPrototype).GetField("currentIntent", Flags);

        private readonly Dictionary<CellModel, Image> overlays = new();
        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;

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

            // Normal cells already render telegraph correctly through ExcelHellCellView.
            // The extra layer is needed only when FormulaCells would otherwise repaint it.
            if (target == null || !target.IsFormula || target.State != CellState.Normal) return;

            var overlay = EnsureOverlay(target);
            if (overlay == null) return;
            overlay.color = TelegraphColor;
            overlay.enabled = true;
            PlaceBelowFormulaInteraction(overlay.rectTransform);
        }

        private void Bind(ExcelHellPrototype owner)
        {
            foreach (var overlay in overlays.Values)
                if (overlay != null) Destroy(overlay.gameObject);
            overlays.Clear();

            prototype = owner;
            cells = prototype == null ? null : CellsField?.GetValue(prototype) as CellModel[,];
            views = prototype == null ? null : ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
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

            var image = go.GetComponent<Image>();
            image.color = TelegraphColor;
            image.raycastTarget = false;
            image.enabled = false;
            overlays[cell] = image;
            PlaceBelowFormulaInteraction(rect);
            return image;
        }

        private static void PlaceBelowFormulaInteraction(RectTransform telegraph)
        {
            if (telegraph == null || telegraph.parent == null) return;
            var parent = telegraph.parent;
            Transform formulaInteraction = null;
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == "Formula 2.0 Interaction")
                {
                    formulaInteraction = child;
                    break;
                }
            }

            if (formulaInteraction == null)
            {
                telegraph.SetAsLastSibling();
                return;
            }

            telegraph.SetSiblingIndex(formulaInteraction.GetSiblingIndex());
        }
    }
}
