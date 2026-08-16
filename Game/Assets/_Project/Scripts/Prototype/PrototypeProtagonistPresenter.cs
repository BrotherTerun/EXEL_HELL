using System.Collections;
using System.Linq;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Protagonist presentation endpoint. The approved Player sheet supplies authored key poses while this
    /// presenter adds a lightweight pose scheduler and integer micro-motion. NarrativeLayer contracts stay
    /// unchanged, so later animation polish can evolve independently from gameplay and authored narrative.
    /// </summary>
    [DefaultExecutionOrder(1950)]
    public sealed class PrototypeProtagonistPresenter : MonoBehaviour, INarrativeEffectReceiver
    {
        private static readonly int[] NormalPoseSequence = { 0, 1, 0, 0, 2, 0, 3, 0 };
        private static readonly int[] TiredPoseSequence = { 0, 0, 1, 0, 2, 2, 0, 3 };
        private static readonly int[] AlarmedPoseSequence = { 0, 1, 0, 2, 1, 0 };
        private static readonly int[] PsychoticPoseSequence = { 0, 1, 3, 0, 2, 1, 0, 3, 2, 0 };

        private ExcelHellPrototype prototype;
        private NarrativeEventRunner runner;
        private Canvas canvas;
        private RectTransform avatarSlot;
        private Image protagonistImage;
        private RectTransform protagonistRect;
        private GameObject bubble;
        private Text bubbleText;
        private Button bubbleButton;
        private NarrativeEffectTicket activeTicket;
        private Coroutine timeoutRoutine;
        private bool activeShown;

        private Texture2D playerSheet;
        private Sprite[] normalFrames;
        private Sprite[] tiredFrames;
        private Sprite[] alarmedFrames;
        private Sprite[] psychoticFrames;

        private ProtagonistMood currentMood = ProtagonistMood.Normal;
        private int poseSequenceCursor;
        private float nextPoseAt;
        private float nextMicroMotionAt;
        private Vector2 protagonistBasePosition;

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
            TickProtagonistAnimation();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            if (!ReferenceEquals(prototype, null))
                CompleteActive("rebind");

            prototype = owner;
            canvas = null;
            avatarSlot = null;
            protagonistImage = null;
            protagonistRect = null;
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
            if (normalFrames != null && normalFrames.Length > 0) return;

            playerSheet = Resources.Load<Texture2D>("Art/Player");
            if (playerSheet == null)
            {
                // A Sprite (Multiple) importer may expose sub-sprites more readily than the texture asset itself.
                // Pulling the shared texture from any imported sprite keeps runtime cropping independent of the
                // current Sprite Editor slicing metadata.
                var importedSprite = Resources.LoadAll<Sprite>("Art/Player").FirstOrDefault();
                playerSheet = importedSprite != null ? importedSprite.texture : null;
            }

            if (playerSheet == null)
            {
                Debug.LogWarning("[PROTAGONIST/UI] Resources/Art/Player texture not found; protagonist art disabled.");
                return;
            }

            playerSheet.filterMode = FilterMode.Point;

            // Rect coordinates are bottom-left based. Each row keeps one shared baseline and crop height so
            // swapping authored poses does not move the chair/floor contact point. The right side of each row
            // contains sheet/reference material and is intentionally excluded.
            normalFrames = new[]
            {
                Crop("Protagonist_Normal_0", new Rect(185f, 819f, 245f, 195f)),
                Crop("Protagonist_Normal_1", new Rect(425f, 819f, 245f, 195f)),
                Crop("Protagonist_Normal_2", new Rect(675f, 819f, 245f, 195f)),
                Crop("Protagonist_Normal_3", new Rect(915f, 819f, 245f, 195f))
            };

            tiredFrames = new[]
            {
                Crop("Protagonist_Tired_0", new Rect(180f, 614f, 250f, 195f)),
                Crop("Protagonist_Tired_1", new Rect(415f, 614f, 250f, 195f)),
                Crop("Protagonist_Tired_2", new Rect(670f, 614f, 250f, 195f)),
                Crop("Protagonist_Tired_3", new Rect(915f, 614f, 250f, 195f))
            };

            alarmedFrames = new[]
            {
                Crop("Protagonist_Alarmed_0", new Rect(190f, 414f, 250f, 205f)),
                Crop("Protagonist_Alarmed_1", new Rect(455f, 414f, 250f, 205f)),
                Crop("Protagonist_Alarmed_2", new Rect(730f, 414f, 250f, 205f))
            };

            psychoticFrames = new[]
            {
                Crop("Protagonist_Psychotic_0", new Rect(185f, 214f, 255f, 210f)),
                Crop("Protagonist_Psychotic_1", new Rect(430f, 214f, 255f, 210f)),
                Crop("Protagonist_Psychotic_2", new Rect(680f, 214f, 255f, 210f)),
                Crop("Protagonist_Psychotic_3", new Rect(925f, 214f, 255f, 210f))
            };

            Debug.Log($"[PROTAGONIST/ANIM] Visual sheet ready ({playerSheet.width}x{playerSheet.height}); 15 authored poses prepared.");
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
            protagonistRect = go.GetComponent<RectTransform>();
            protagonistRect.anchorMin = protagonistRect.anchorMax = new Vector2(0.5f, 0f);
            protagonistRect.pivot = new Vector2(0.5f, 0f);
            protagonistRect.anchoredPosition = new Vector2(0f, -4f);
            protagonistRect.sizeDelta = new Vector2(322f, 270f);
            protagonistBasePosition = protagonistRect.anchoredPosition;

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
            currentMood = mood;
            ResetAnimationState();
        }

        private void ResetAnimationState()
        {
            poseSequenceCursor = 0;
            nextPoseAt = 0f;
            nextMicroMotionAt = 0f;
            if (protagonistRect != null)
                protagonistRect.anchoredPosition = protagonistBasePosition;
            ApplyPose(CurrentSequence()[0]);
            ScheduleNextPose();
            ScheduleNextMicroMotion();
        }

        private void TickProtagonistAnimation()
        {
            if (protagonistImage == null || protagonistRect == null) return;
            if (CurrentFrames().Length == 0) return;

            var now = Time.unscaledTime;
            if (now >= nextPoseAt)
            {
                var sequence = CurrentSequence();
                poseSequenceCursor = (poseSequenceCursor + 1) % sequence.Length;
                ApplyPose(sequence[poseSequenceCursor]);
                ScheduleNextPose();
            }

            if (now >= nextMicroMotionAt)
            {
                protagonistRect.anchoredPosition = protagonistBasePosition + NextMicroOffset();
                ScheduleNextMicroMotion();
            }
        }

        private void ApplyPose(int frameIndex)
        {
            if (protagonistImage == null) return;
            var frames = CurrentFrames();
            if (frames.Length == 0)
            {
                protagonistImage.enabled = false;
                return;
            }

            frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
            protagonistImage.sprite = frames[frameIndex];
            protagonistImage.enabled = protagonistImage.sprite != null;
        }

        private Sprite[] CurrentFrames()
        {
            return currentMood switch
            {
                ProtagonistMood.Tired => tiredFrames ?? normalFrames ?? System.Array.Empty<Sprite>(),
                ProtagonistMood.Alarmed => alarmedFrames ?? normalFrames ?? System.Array.Empty<Sprite>(),
                ProtagonistMood.Psychotic => psychoticFrames ?? normalFrames ?? System.Array.Empty<Sprite>(),
                _ => normalFrames ?? System.Array.Empty<Sprite>()
            };
        }

        private int[] CurrentSequence()
        {
            return currentMood switch
            {
                ProtagonistMood.Tired => TiredPoseSequence,
                ProtagonistMood.Alarmed => AlarmedPoseSequence,
                ProtagonistMood.Psychotic => PsychoticPoseSequence,
                _ => NormalPoseSequence
            };
        }

        private void ScheduleNextPose()
        {
            var delay = currentMood switch
            {
                ProtagonistMood.Tired => Random.Range(0.95f, 2.15f),
                ProtagonistMood.Alarmed => Random.Range(0.38f, 0.82f),
                ProtagonistMood.Psychotic => Random.Range(0.14f, 0.42f),
                _ => Random.Range(0.72f, 1.65f)
            };
            nextPoseAt = Time.unscaledTime + delay;
        }

        private void ScheduleNextMicroMotion()
        {
            var delay = currentMood switch
            {
                ProtagonistMood.Tired => Random.Range(0.9f, 1.8f),
                ProtagonistMood.Alarmed => Random.Range(0.22f, 0.5f),
                ProtagonistMood.Psychotic => Random.Range(0.07f, 0.18f),
                _ => Random.Range(0.65f, 1.35f)
            };
            nextMicroMotionAt = Time.unscaledTime + delay;
        }

        private Vector2 NextMicroOffset()
        {
            return currentMood switch
            {
                ProtagonistMood.Tired => new Vector2(0f, Random.value < 0.28f ? -1f : 0f),
                ProtagonistMood.Alarmed => new Vector2(Random.Range(-1, 2), Random.value < 0.18f ? 1f : 0f),
                ProtagonistMood.Psychotic => new Vector2(Random.Range(-2, 3), Random.Range(-1, 2)),
                _ => new Vector2(0f, Random.value < 0.22f ? 1f : 0f)
            };
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
            if (protagonistRect != null)
                protagonistRect.anchoredPosition = protagonistBasePosition;
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
