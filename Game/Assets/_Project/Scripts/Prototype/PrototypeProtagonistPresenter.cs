using System.Collections;
using System.Linq;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Protagonist presentation endpoint. Visual pass v1 crops the approved character sheet at runtime into
    /// four stable mood sprites; the later animation pass can replace those static frames without changing
    /// NarrativeLayer contracts.
    /// </summary>
    [DefaultExecutionOrder(1950)]
    public sealed class PrototypeProtagonistPresenter : MonoBehaviour, INarrativeEffectReceiver
    {
        private ExcelHellPrototype prototype;
        private NarrativeEventRunner runner;
        private Canvas canvas;
        private RectTransform avatarSlot;
        private Image protagonistImage;
        private GameObject bubble;
        private Text bubbleText;
        private Button bubbleButton;
        private NarrativeEffectTicket activeTicket;
        private Coroutine timeoutRoutine;
        private bool activeShown;

        private Texture2D playerSheet;
        private Sprite normalSprite;
        private Sprite tiredSprite;
        private Sprite alarmedSprite;
        private Sprite psychoticSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeProtagonistPresenter>() != null) return;
            var root = new GameObject("[PRESENTATION] Protagonist Presenter");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeProtagonistPresenter>();
        }

        private void Update()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            BindRunner();
            if (prototype != null && bubble == null) TryBuild();
            if (activeTicket != null && !activeShown) TryShowActive();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            if (!ReferenceEquals(prototype, null))
                CompleteActive("rebind");

            prototype = owner;
            canvas = null;
            avatarSlot = null;
            protagonistImage = null;
            DestroyBubble();
            if (prototype != null)
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

        private void TryBuild()
        {
            if (canvas == null) return;
            avatarSlot = canvas.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect.gameObject.name == "Avatar Reserved");
            if (avatarSlot == null) return;

            EnsureMoodSprites();
            BuildProtagonistImage();
            BuildBubble();
            SetMood(ProtagonistMood.Normal);
        }

        private void EnsureMoodSprites()
        {
            if (normalSprite != null) return;
            playerSheet = Resources.Load<Texture2D>("Art/Player");
            if (playerSheet == null)
            {
                Debug.LogWarning("[PROTAGONIST/UI] Resources/Art/Player texture not found; protagonist art disabled.");
                return;
            }

            playerSheet.filterMode = FilterMode.Point;

            // Rect coordinates are bottom-left based. These are the first clean authored frame from each row
            // of the 1536x1024 master sheet. All four include chair + desk, so they share a visual ground line.
            normalSprite = Crop("Protagonist_Normal", new Rect(185f, 819f, 245f, 195f));
            tiredSprite = Crop("Protagonist_Tired", new Rect(180f, 614f, 250f, 195f));
            alarmedSprite = Crop("Protagonist_Alarmed", new Rect(190f, 414f, 250f, 205f));
            psychoticSprite = Crop("Protagonist_Psychotic", new Rect(185f, 214f, 255f, 210f));
        }

        private Sprite Crop(string spriteName, Rect rect)
        {
            if (playerSheet == null) return null;
            var clamped = new Rect(
                Mathf.Clamp(rect.x, 0f, playerSheet.width - 1f),
                Mathf.Clamp(rect.y, 0f, playerSheet.height - 1f),
                Mathf.Min(rect.width, playerSheet.width - rect.x),
                Mathf.Min(rect.height, playerSheet.height - rect.y));
            var sprite = Sprite.Create(playerSheet, clamped, new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = spriteName;
            return sprite;
        }

        private void BuildProtagonistImage()
        {
            var go = new GameObject("Protagonist Sprite", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(avatarSlot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(322f, 270f);

            protagonistImage = go.GetComponent<Image>();
            protagonistImage.preserveAspect = true;
            protagonistImage.raycastTarget = false;
            protagonistImage.color = Color.white;
        }

        private void BuildBubble()
        {
            bubble = new GameObject("Protagonist Line", typeof(RectTransform), typeof(Image), typeof(Button));
            bubble.transform.SetParent(canvas.transform, false);
            SetTopLeft(bubble.GetComponent<RectTransform>(), 1000f, -410f, 560f, 112f);

            var image = bubble.GetComponent<Image>();
            image.color = new Color(0.075f, 0.095f, 0.13f, 0.975f);
            image.raycastTarget = true;

            bubbleButton = bubble.GetComponent<Button>();
            bubbleButton.targetGraphic = image;
            bubbleButton.onClick.AddListener(OnBubbleClicked);

            bubbleText = CreateText(bubble.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(bubbleText.rectTransform, 18f);
            bubbleText.color = PrototypeVisualTheme.Text;
            bubbleText.raycastTarget = false;
            bubble.SetActive(false);
        }

        public bool CanReceive(NarrativeEffectType type) => type == NarrativeEffectType.ProtagonistLine;

        public void Receive(NarrativeEffectTicket ticket)
        {
            if (ticket == null) return;
            CompleteActive("replaced");
            activeTicket = ticket;
            activeShown = false;

            if (!TryShowActive())
                Debug.Log($"[PROTAGONIST/UI] Pending event={ticket.Request.EventId}; waiting for visual slot.");
        }

        private bool TryShowActive()
        {
            if (activeTicket == null) return false;
            if (bubble == null) TryBuild();
            if (bubble == null || bubbleText == null) return false;

            var effect = activeTicket.Request.Effect;
            bubbleText.text = effect.text ?? string.Empty;
            SetMood(effect.mood);
            bubble.SetActive(true);
            bubble.transform.SetAsLastSibling();
            activeShown = true;

            Debug.Log($"[PROTAGONIST/UI] Show event={activeTicket.Request.EventId} mood={effect.mood} " +
                      $"dismiss={effect.lifetime.dismissMode} duration={effect.lifetime.duration:0.##} text=\"{effect.text}\"");

            if (effect.lifetime.dismissMode == NarrativeDismissMode.Timed ||
                effect.lifetime.dismissMode == NarrativeDismissMode.TimedOrClick)
            {
                timeoutRoutine = StartCoroutine(DismissAfter(Mathf.Max(0.05f, effect.lifetime.duration)));
            }
            return true;
        }

        public void SetMood(ProtagonistMood mood)
        {
            if (protagonistImage == null) return;
            protagonistImage.sprite = mood switch
            {
                ProtagonistMood.Tired => tiredSprite ?? normalSprite,
                ProtagonistMood.Alarmed => alarmedSprite ?? normalSprite,
                ProtagonistMood.Psychotic => psychoticSprite ?? normalSprite,
                _ => normalSprite
            };
            protagonistImage.enabled = protagonistImage.sprite != null;
        }

        private void OnBubbleClicked()
        {
            if (activeTicket == null) return;
            var mode = activeTicket.Request.Effect.lifetime.dismissMode;
            if (mode == NarrativeDismissMode.OnClick || mode == NarrativeDismissMode.TimedOrClick)
                CompleteActive("click");
        }

        private IEnumerator DismissAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            CompleteActive("timeout");
        }

        private void CompleteActive(string reason)
        {
            if (timeoutRoutine != null)
            {
                StopCoroutine(timeoutRoutine);
                timeoutRoutine = null;
            }

            if (bubble != null) bubble.SetActive(false);
            if (activeTicket != null)
            {
                var eventId = activeTicket.Request.EventId;
                activeTicket.Complete();
                activeTicket = null;
                Debug.Log($"[PROTAGONIST/UI] Hide event={eventId} reason={reason}.");
            }
            activeShown = false;
        }

        private void DestroyBubble()
        {
            if (bubble != null) Destroy(bubble);
            bubble = null;
            bubbleText = null;
            bubbleButton = null;
            activeShown = false;
        }

        private void OnDisable()
        {
            CompleteActive("disabled");
            if (runner != null) runner.UnregisterReceiver(this);
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
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
