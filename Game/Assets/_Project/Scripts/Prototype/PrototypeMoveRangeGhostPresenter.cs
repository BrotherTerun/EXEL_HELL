using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Presentation-only MOVE ghost that mirrors the complete dragged selection footprint.
    /// The pressed source cell remains the anchor because Formula Cells 2.0 computes DROP offsets from it.
    /// No gameplay state is changed here.
    /// </summary>
    [DefaultExecutionOrder(2450)]
    public sealed class PrototypeMoveRangeGhostPresenter : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo PrototypeViewsField = typeof(ExcelHellPrototype).GetField("views", PrivateInstance);

        private static readonly FieldInfo DraggingMoveField = typeof(PrototypeFormulaCells).GetField("draggingMove", PrivateInstance);
        private static readonly FieldInfo HoverCellField = typeof(PrototypeFormulaCells).GetField("hoverCell", PrivateInstance);
        private static readonly FieldInfo PressCellField = typeof(PrototypeFormulaCells).GetField("pressCell", PrivateInstance);
        private static readonly FieldInfo DragSelectionCellsField = typeof(PrototypeFormulaCells).GetField("dragSelectionCells", PrivateInstance);
        private static readonly FieldInfo GhostRectField = typeof(PrototypeFormulaCells).GetField("dragGhostRect", PrivateInstance);
        private static readonly FieldInfo GhostImageField = typeof(PrototypeFormulaCells).GetField("dragGhostImage", PrivateInstance);
        private static readonly FieldInfo GhostTextField = typeof(PrototypeFormulaCells).GetField("dragGhostText", PrivateInstance);

        private ExcelHellPrototype prototype;
        private PrototypeFormulaCells formulaCells;
        private ExcelHellCellView[,] views;

        private RectTransform previewRoot;
        private readonly List<GameObject> generated = new();
        private string previewSignature = string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeMoveRangeGhostPresenter>() != null) return;

            var root = new GameObject("[PRESENTATION] MOVE Range Ghost");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeMoveRangeGhostPresenter>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active)
            {
                ResetBinding();
                return;
            }

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || views == null) return;

            if (formulaCells == null)
                formulaCells = FindFirstObjectByType<PrototypeFormulaCells>();
            if (formulaCells == null) return;

            var dragging = DraggingMoveField?.GetValue(formulaCells) is bool value && value;
            if (!dragging)
            {
                ClearPreview(true);
                return;
            }

            var ghostRect = GhostRectField?.GetValue(formulaCells) as RectTransform;
            var pressCell = PressCellField?.GetValue(formulaCells) as CellModel;
            var targetCell = HoverCellField?.GetValue(formulaCells) as CellModel;
            var sourceCells = DragSelectionCellsField?.GetValue(formulaCells) as List<CellModel>;

            if (ghostRect == null || pressCell == null || sourceCells == null || sourceCells.Count == 0)
                return;

            SuppressLegacyGhost(ghostRect);

            var signature = BuildSignature(pressCell, sourceCells);
            if (previewRoot == null || signature != previewSignature)
            {
                ClearPreview(false);
                BuildPreview(ghostRect, pressCell, sourceCells);
                previewSignature = signature;
            }

            if (previewRoot == null) return;

            if (targetCell == null || !TryGetViewRect(targetCell, out var targetRect))
            {
                previewRoot.gameObject.SetActive(false);
                return;
            }

            previewRoot.gameObject.SetActive(true);

            // Formula Cells 2.0 computes DROP as target - pressCell. The visual ghost uses the exact same anchor:
            // the pressed cell in the copied range is centered over the currently hovered destination cell.
            ghostRect.pivot = new Vector2(0.5f, 0.5f);
            ghostRect.position = targetRect.TransformPoint(targetRect.rect.center);
            ghostRect.sizeDelta = Vector2.zero;
            ghostRect.localScale = Vector3.one;
            ghostRect.SetAsLastSibling();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            ClearPreview(true);
            prototype = owner;
            formulaCells = null;
            views = prototype != null ? PrototypeViewsField?.GetValue(prototype) as ExcelHellCellView[,] : null;
        }

        private void ResetBinding()
        {
            if (prototype == null && formulaCells == null && previewRoot == null) return;
            ClearPreview(true);
            prototype = null;
            formulaCells = null;
            views = null;
        }

        private void BuildPreview(RectTransform ghostRect, CellModel pressCell, IReadOnlyList<CellModel> sourceCells)
        {
            if (ghostRect == null || pressCell == null || sourceCells == null || sourceCells.Count == 0) return;
            if (!TryGetViewRect(pressCell, out var pressRect)) return;

            var coordinateSpace = ghostRect.parent as RectTransform;
            if (coordinateSpace == null) return;

            var rootGo = new GameObject("MOVE Range Ghost Preview", typeof(RectTransform), typeof(CanvasGroup));
            rootGo.transform.SetParent(ghostRect, false);
            previewRoot = rootGo.GetComponent<RectTransform>();
            previewRoot.anchorMin = previewRoot.anchorMax = new Vector2(0.5f, 0.5f);
            previewRoot.pivot = new Vector2(0.5f, 0.5f);
            previewRoot.anchoredPosition = Vector2.zero;
            previewRoot.sizeDelta = Vector2.zero;
            previewRoot.localScale = Vector3.one;
            generated.Add(rootGo);

            var group = rootGo.GetComponent<CanvasGroup>();
            group.alpha = 0.70f;
            group.interactable = false;
            group.blocksRaycasts = false;
            group.ignoreParentGroups = false;

            var pressCenter = ToLocalCenter(coordinateSpace, pressRect);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            foreach (var cell in sourceCells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column))
            {
                if (cell == null || !TryGetViewRect(cell, out var sourceRect)) continue;

                var center = ToLocalCenter(coordinateSpace, sourceRect);
                var size = ToLocalSize(coordinateSpace, sourceRect);
                var offset = center - pressCenter;

                CreateCellCopy(sourceRect, cell, offset, size, coordinateSpace);

                min.x = Mathf.Min(min.x, offset.x - size.x * 0.5f);
                min.y = Mathf.Min(min.y, offset.y - size.y * 0.5f);
                max.x = Mathf.Max(max.x, offset.x + size.x * 0.5f);
                max.y = Mathf.Max(max.y, offset.y + size.y * 0.5f);
            }

            if (!float.IsInfinity(min.x) && !float.IsInfinity(max.x))
                CreateOuterFrame(min, max);
        }

        private void CreateCellCopy(RectTransform sourceRect, CellModel cell, Vector2 offset, Vector2 size, RectTransform coordinateSpace)
        {
            var sourceView = sourceRect.GetComponent<ExcelHellCellView>();
            if (sourceView == null) return;

            var go = new GameObject($"Ghost {cell.Address}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(previewRoot, false);
            generated.Add(go);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            var sourceImage = sourceView.GetComponent<Image>();
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            if (sourceImage != null)
            {
                image.color = sourceImage.color;
                image.sprite = sourceImage.sprite;
                image.type = sourceImage.type;
                image.preserveAspect = sourceImage.preserveAspect;
            }
            else
            {
                image.color = PrototypeSpreadsheetRedesign.Cell;
            }

            var edgeColor = cell.State == CellState.Destroyed
                ? PrototypeSpreadsheetRedesign.DeletedGrid
                : cell.State == CellState.Corrupted
                    ? PrototypeSpreadsheetRedesign.RefAccent
                    : cell.IsFormula
                        ? PrototypeSpreadsheetRedesign.FormulaText
                        : PrototypeSpreadsheetRedesign.Grid;
            AddBorder(rect, edgeColor, cell.State == CellState.Destroyed ? 1f : 1.5f);

            var sourceCenter = ToLocalCenter(coordinateSpace, sourceRect);
            foreach (var sourceText in sourceView.GetComponentsInChildren<Text>(true))
            {
                if (sourceText == null || !sourceText.gameObject.activeInHierarchy) continue;
                if (string.IsNullOrEmpty(sourceText.text)) continue;
                if (HasAncestorStartingWith(sourceText.transform, "Cell Message")) continue;

                var sourceTextRect = sourceText.rectTransform;
                var textCenter = ToLocalCenter(coordinateSpace, sourceTextRect);
                var textSize = ToLocalSize(coordinateSpace, sourceTextRect);

                var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(rect, false);
                generated.Add(textGo);

                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = textCenter - sourceCenter;
                textRect.sizeDelta = textSize;
                textRect.localScale = Vector3.one;

                var text = textGo.GetComponent<Text>();
                text.raycastTarget = false;
                text.text = sourceText.text;
                text.font = sourceText.font;
                text.fontSize = sourceText.fontSize;
                text.fontStyle = sourceText.fontStyle;
                text.alignment = sourceText.alignment;
                text.color = sourceText.color;
                text.horizontalOverflow = sourceText.horizontalOverflow;
                text.verticalOverflow = sourceText.verticalOverflow;
                text.resizeTextForBestFit = sourceText.resizeTextForBestFit;
                text.resizeTextMinSize = sourceText.resizeTextMinSize;
                text.resizeTextMaxSize = sourceText.resizeTextMaxSize;
                text.lineSpacing = sourceText.lineSpacing;
                text.supportRichText = sourceText.supportRichText;
            }
        }

        private void CreateOuterFrame(Vector2 min, Vector2 max)
        {
            var frameGo = new GameObject("Dragged Range Frame", typeof(RectTransform));
            frameGo.transform.SetParent(previewRoot, false);
            generated.Add(frameGo);

            var frame = frameGo.GetComponent<RectTransform>();
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = (min + max) * 0.5f;
            frame.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
            frame.localScale = Vector3.one;

            AddBorder(frame, PrototypeSpreadsheetRedesign.Selection, 2.5f);
            frame.SetAsLastSibling();
        }

        private static void AddBorder(RectTransform owner, Color color, float thickness)
        {
            CreateEdge(owner, "Top", color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness));
            CreateEdge(owner, "Bottom", color, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness));
            CreateEdge(owner, "Left", color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f));
            CreateEdge(owner, "Right", color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f));
        }

        private static void CreateEdge(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void SuppressLegacyGhost(RectTransform ghostRect)
        {
            var image = GhostImageField?.GetValue(formulaCells) as Image;
            var text = GhostTextField?.GetValue(formulaCells) as Text;
            if (image != null) image.enabled = false;
            if (text != null) text.enabled = false;

            // PrototypeSpreadsheetRedesignPolish runs just before this presenter and may reactivate its old
            // single-cell border. Keep it suppressed while the range preview owns the rendering.
            foreach (Transform child in ghostRect)
                if (child != null && child.name == "Redesign Pixel Border")
                    child.gameObject.SetActive(false);
        }

        private void RestoreLegacyGhost()
        {
            if (formulaCells == null) return;
            var image = GhostImageField?.GetValue(formulaCells) as Image;
            var text = GhostTextField?.GetValue(formulaCells) as Text;
            if (image != null) image.enabled = true;
            if (text != null) text.enabled = true;
        }

        private void ClearPreview(bool restoreLegacy)
        {
            if (restoreLegacy) RestoreLegacyGhost();

            foreach (var go in generated)
                if (go != null) Destroy(go);
            generated.Clear();
            previewRoot = null;
            previewSignature = string.Empty;
        }

        private bool TryGetViewRect(CellModel cell, out RectTransform rect)
        {
            rect = null;
            if (cell == null || views == null) return false;
            if (cell.Row < 0 || cell.Column < 0 || cell.Row >= views.GetLength(0) || cell.Column >= views.GetLength(1)) return false;

            var view = views[cell.Row, cell.Column];
            if (view == null) return false;
            rect = view.GetComponent<RectTransform>();
            return rect != null;
        }

        private static Vector2 ToLocalCenter(RectTransform coordinateSpace, RectTransform source)
        {
            var world = source.TransformPoint(source.rect.center);
            return coordinateSpace.InverseTransformPoint(world);
        }

        private static Vector2 ToLocalSize(RectTransform coordinateSpace, RectTransform source)
        {
            var corners = new Vector3[4];
            source.GetWorldCorners(corners);
            var bottomLeft = (Vector2)coordinateSpace.InverseTransformPoint(corners[0]);
            var topRight = (Vector2)coordinateSpace.InverseTransformPoint(corners[2]);
            return new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y));
        }

        private static bool HasAncestorStartingWith(Transform transform, string prefix)
        {
            var current = transform.parent;
            while (current != null)
            {
                if (current.name.StartsWith(prefix, StringComparison.Ordinal)) return true;
                current = current.parent;
            }
            return false;
        }

        private static string BuildSignature(CellModel pressCell, IEnumerable<CellModel> cells)
        {
            return pressCell.Address + "|" + string.Join(";", cells
                .Where(cell => cell != null)
                .OrderBy(cell => cell.Row)
                .ThenBy(cell => cell.Column)
                .Select(cell => cell.Address));
        }

        private void OnDisable() => ResetBinding();
    }
}
