using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    [DefaultExecutionOrder(2400)]
    public sealed class PrototypeAudioDirector : MonoBehaviour
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
        const string S = "SFX/EXCEL_HELL_Audio_Kit_v1/";
        const string M = "Soundtrak/EXCEL_HELL_dynamic_music_pack/";

        static readonly FieldInfo CellsF = typeof(ExcelHellPrototype).GetField("cells", F);
        static readonly FieldInfo TurnF = typeof(ExcelHellPrototype).GetField("turn", F);
        static readonly FieldInfo StatusF = typeof(ExcelHellPrototype).GetField("statusText", F);
        static readonly FieldInfo PendingF = typeof(ExcelHellPrototype).GetField("pendingSpawnIntent", F);
        static readonly FieldInfo ExpressionF = typeof(PrototypeFormulaCells).GetField("lastExpression", F);
        static readonly FieldInfo HelpF = typeof(PrototypeProductionHud).GetField("helpWindow", F);
        static readonly FieldInfo ChatF = typeof(PrototypeProductionHud).GetField("chatWindow", F);
        static readonly FieldInfo BossF = typeof(PrototypeProductionHud).GetField("bossMessages", F);
        static readonly FieldInfo DeptF = typeof(PrototypeProductionHud).GetField("departmentMessages", F);
        static readonly FieldInfo BubbleF = typeof(PrototypeProtagonistPresenter).GetField("bubble", F);
        static readonly FieldInfo BubbleTextF = typeof(PrototypeProtagonistPresenter).GetField("bubbleText", F);

        public static PrototypeAudioDirector Instance { get; private set; }

        AudioSource normal, psychosis, ambience, sfx, stinger;
        AudioClip uiHover, uiClick, uiOpen, uiClose, pickup, drop, invalid, delete;
        AudioClip sum, sort, refTelegraph, refMove, refDestroy, chatPing, typewriter, accepted, finalStinger;

        ExcelHellPrototype prototype;
        PrototypeFormulaCells formulas;
        PrototypeProductionHud hud;
        PrototypeProtagonistPresenter protagonist;
        PrototypeLevelFlow flow;

        CellState[,] states;
        readonly HashSet<int> buttonTaps = new();
        readonly HashSet<int> cellTaps = new();

        int lastLevel = -1, lastTurn, bossCount, deptCount, hudId;
        string lastStatus = "", lastExpression = "", lastBubble = "";
        bool lastPending, helpOpen, chatOpen, wasAccepted, bubbleOpen;
        float normalTarget, psychosisTarget, nextBind;
        Coroutine typing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeAudioDirector>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("[PRESENTATION] Audio Director");
            DontDestroyOnLoad(go);
            go.AddComponent<PrototypeAudioDirector>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            uiHover = Load(S+"ui_hover"); uiClick = Load(S+"ui_click");
            uiOpen = Load(S+"ui_open"); uiClose = Load(S+"ui_close");
            pickup = Load(S+"cell_pickup"); drop = Load(S+"cell_drop"); invalid = Load(S+"invalid");
            delete = Load(S+"delete"); sum = Load(S+"sum"); sort = Load(S+"sort");
            refTelegraph = Load(S+"ref_telegraph"); refMove = Load(S+"ref_move"); refDestroy = Load(S+"ref_destroy");
            chatPing = Load(S+"chat_ping"); typewriter = Load(S+"typewriter"); accepted = Load(S+"report_accepted");
            finalStinger = Load(M+"excel_hell_final_stinger");

            normal = Source(Load(M+"excel_hell_normal_office_75bpm"), true, 0);
            psychosis = Source(Load(M+"excel_hell_psychosis_75bpm"), true, 0);
            ambience = Source(Load(S+"office_ambience_loop"), true, .13f);
            sfx = Source(null, false, 1);
            stinger = Source(null, false, .65f);

            var dsp = AudioSettings.dspTime + .12;
            if (normal.clip != null) normal.PlayScheduled(dsp);
            if (psychosis.clip != null) psychosis.PlayScheduled(dsp);
            if (ambience.clip != null) ambience.Play();
            Mix(true);
            Debug.Log("[AUDIO] broad release pass enabled.");
        }

        void Update()
        {
            if (PrototypeAuthoringMode.Active) { MuteBeds(true); return; }

            var p = FindFirstObjectByType<ExcelHellPrototype>();
            if (p != prototype) Bind(p);
            MuteBeds(prototype == null);

            formulas ??= FindFirstObjectByType<PrototypeFormulaCells>();
            hud ??= FindFirstObjectByType<PrototypeProductionHud>();
            protagonist ??= FindFirstObjectByType<PrototypeProtagonistPresenter>();
            flow ??= FindFirstObjectByType<PrototypeLevelFlow>();

            Mix(false);
            FadeMix();

            if (prototype != null)
            {
                ObserveCells();
                ObserveTelegraph();
                ObserveStatus();
                ObserveFormula();
                ObserveAccepted();
            }

            ObserveHud();
            ObserveBubble();

            if (Time.unscaledTime >= nextBind)
            {
                nextBind = Time.unscaledTime + .35f;
                BindTaps();
            }
        }

        void Bind(ExcelHellPrototype p)
        {
            prototype = p; formulas = null; hud = null; protagonist = null; flow = null;
            states = null; buttonTaps.Clear(); cellTaps.Clear();
            lastStatus = lastExpression = lastBubble = "";
            lastPending = helpOpen = chatOpen = wasAccepted = bubbleOpen = false;
            bossCount = deptCount = hudId = 0;
            if (prototype == null) return;

            lastTurn = Turn;
            var t = StatusF?.GetValue(prototype) as Text;
            lastStatus = t != null ? t.text ?? "" : "";
            lastPending = PendingF?.GetValue(prototype) != null;
            Snapshot();
            Debug.Log($"[AUDIO] bound level {PrototypeLevelRuntime.CurrentIndex + 1}.");
        }

        void Snapshot()
        {
            var cells = CellsF?.GetValue(prototype) as CellModel[,];
            if (cells == null) return;
            states = new CellState[cells.GetLength(0), cells.GetLength(1)];
            for (var r=0;r<cells.GetLength(0);r++)
            for (var c=0;c<cells.GetLength(1);c++) states[r,c]=cells[r,c].State;
        }

        void ObserveCells()
        {
            var cells = CellsF?.GetValue(prototype) as CellModel[,];
            if (cells == null) return;
            if (states == null || states.GetLength(0)!=cells.GetLength(0) || states.GetLength(1)!=cells.GetLength(1))
            { Snapshot(); return; }

            for (var r=0;r<cells.GetLength(0);r++)
            for (var c=0;c<cells.GetLength(1);c++)
            {
                var before=states[r,c]; var after=cells[r,c].State;
                if (before==after) continue;
                if (after==CellState.Corrupted && before!=CellState.Corrupted) Play(refMove,.78f);
                else if (after==CellState.Destroyed)
                    Play(before==CellState.Corrupted ? refDestroy : delete, before==CellState.Corrupted ? .88f : .72f);
                states[r,c]=after;
            }
        }

        void ObserveTelegraph()
        {
            var now = PendingF?.GetValue(prototype) != null;
            if (now && !lastPending) Play(refTelegraph,.68f);
            lastPending=now;
        }

        void ObserveStatus()
        {
            var t=StatusF?.GetValue(prototype) as Text;
            var now=t!=null ? t.text ?? "" : "";
            if (now==lastStatus) return;
            if (LooksInvalid(now)) Play(invalid,.68f);
            lastStatus=now;
            lastTurn=Turn;
        }

        void ObserveFormula()
        {
            formulas ??= FindFirstObjectByType<PrototypeFormulaCells>();
            if (formulas==null) return;
            var now=ExpressionF?.GetValue(formulas) as string ?? "";
            if (now==lastExpression) return;
            if (now.StartsWith("=SUM(",StringComparison.OrdinalIgnoreCase)) Play(sum,.76f);
            else if (now.StartsWith("=SORT(",StringComparison.OrdinalIgnoreCase)) Play(sort,.76f);
            lastExpression=now;
        }

        void ObserveAccepted()
        {
            flow ??= FindFirstObjectByType<PrototypeLevelFlow>();
            var ok=flow!=null && flow.ReportAcceptedForPresentation;
            if (ok && !wasAccepted)
            {
                Play(accepted,.82f);
                if (PrototypeLevelRuntime.IsLast && finalStinger!=null)
                {
                    stinger.clip=finalStinger; stinger.Play();
                }
            }
            wasAccepted=ok;
        }

        void ObserveHud()
        {
            hud ??= FindFirstObjectByType<PrototypeProductionHud>();
            if (hud==null) return;

            var id=hud.GetInstanceID();
            if (id!=hudId)
            {
                hudId=id;
                bossCount=Count(BossF?.GetValue(hud)); deptCount=Count(DeptF?.GetValue(hud));
                helpOpen=Active(HelpF?.GetValue(hud)); chatOpen=Active(ChatF?.GetValue(hud));
            }

            var bc=Count(BossF?.GetValue(hud)); var dc=Count(DeptF?.GetValue(hud));
            if (bc>bossCount || dc>deptCount) Play(chatPing,.62f);
            bossCount=bc; deptCount=dc;

            var h=Active(HelpF?.GetValue(hud)); var ch=Active(ChatF?.GetValue(hud));
            if (h!=helpOpen) Play(h?uiOpen:uiClose,.50f);
            if (ch!=chatOpen) Play(ch?uiOpen:uiClose,.50f);
            helpOpen=h; chatOpen=ch;
        }

        void ObserveBubble()
        {
            protagonist ??= FindFirstObjectByType<PrototypeProtagonistPresenter>();
            if (protagonist==null) return;
            var go=BubbleF?.GetValue(protagonist) as GameObject;
            var t=BubbleTextF?.GetValue(protagonist) as Text;
            var open=go!=null && go.activeSelf;
            var text=t!=null ? t.text ?? "" : "";
            if (open && (!bubbleOpen || text!=lastBubble)) StartTyping(text);
            bubbleOpen=open; lastBubble=text;
        }

        void BindTaps()
        {
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (b==null || !b.gameObject.activeInHierarchy || !buttonTaps.Add(b.GetInstanceID())) continue;
                var tap=b.GetComponent<PrototypeAudioUiTap>() ?? b.gameObject.AddComponent<PrototypeAudioUiTap>();
                tap.Bind(this);
            }
            foreach (var o in FindObjectsByType<FormulaCellOverlay>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (o==null || !o.gameObject.activeInHierarchy || !cellTaps.Add(o.GetInstanceID())) continue;
                var tap=o.GetComponent<PrototypeAudioCellTap>() ?? o.gameObject.AddComponent<PrototypeAudioCellTap>();
                tap.Bind(this);
            }
        }

        void Mix(bool force)
        {
            var level=PrototypeLevelRuntime.CurrentIndex;
            if (!force && level==lastLevel) return;
            lastLevel=level;
            normalTarget=level switch { <=0=>.34f, 1=>.30f, 2=>.20f, _=>.09f };
            psychosisTarget=level switch { <=0=>0f, 1=>.07f, 2=>.20f, _=>.34f };
            if (force) { normal.volume=normalTarget; psychosis.volume=psychosisTarget; }
        }

        void FadeMix()
        {
            var step=.18f*Time.unscaledDeltaTime;
            normal.volume=Mathf.MoveTowards(normal.volume,normalTarget,step);
            psychosis.volume=Mathf.MoveTowards(psychosis.volume,psychosisTarget,step);
        }

        void MuteBeds(bool value) { normal.mute=value; psychosis.mute=value; ambience.mute=value; }

        public int Turn => prototype!=null && TurnF?.GetValue(prototype) is int n ? n : 0;
        public string Expression
        {
            get
            {
                formulas ??= FindFirstObjectByType<PrototypeFormulaCells>();
                return formulas!=null ? ExpressionF?.GetValue(formulas) as string ?? "" : "";
            }
        }

        public void UiHover()=>Play(uiHover,.22f,.98f,1.02f);
        public void UiClick()=>Play(uiClick,.46f,.98f,1.02f);
        public void Pickup()=>Play(pickup,.54f,.98f,1.02f);
        public void ResolveDrop(int before,string expressionBefore)
        {
            var e=Expression;
            var formulaChanged=e!=expressionBefore && (e.StartsWith("=SUM(",StringComparison.OrdinalIgnoreCase) || e.StartsWith("=SORT(",StringComparison.OrdinalIgnoreCase));
            if (!formulaChanged && Turn>before) Play(drop,.60f,.98f,1.02f);
        }

        void StartTyping(string text)
        {
            if (typewriter==null || string.IsNullOrWhiteSpace(text)) return;
            if (typing!=null) StopCoroutine(typing);
            typing=StartCoroutine(TypeBurst(text));
        }

        IEnumerator TypeBurst(string text)
        {
            var ticks=Mathf.Clamp(text.Length/12,4,12);
            for(var i=0;i<ticks;i++){ Play(typewriter,.18f,.96f,1.04f); yield return new WaitForSecondsRealtime(.045f); }
            typing=null;
        }

        void Play(AudioClip clip,float volume,float minPitch=1,float maxPitch=1)
        {
            if (clip==null || sfx==null) return;
            sfx.pitch=Mathf.Approximately(minPitch,maxPitch)?minPitch:UnityEngine.Random.Range(minPitch,maxPitch);
            sfx.PlayOneShot(clip,volume); sfx.pitch=1;
        }

        AudioSource Source(AudioClip clip,bool loop,float volume)
        {
            var a=gameObject.AddComponent<AudioSource>();
            a.clip=clip; a.loop=loop; a.playOnAwake=false; a.spatialBlend=0; a.volume=volume;
            return a;
        }

        static AudioClip Load(string path)
        {
            var c=Resources.Load<AudioClip>(path);
            if(c==null) Debug.LogWarning($"[AUDIO] missing {path}");
            return c;
        }

        static bool Active(object v)=>v is GameObject g && g.activeSelf;
        static int Count(object v)=>v is ICollection c ? c.Count : 0;

        static bool LooksInvalid(string value)
        {
            if(string.IsNullOrWhiteSpace(value)) return false;
            var x=value.ToLowerInvariant();
            string[] words={"#spill","нельзя","нуж","занят","недоступ","выходит за","не переносится","только числа","отклон",
                            "cannot","need","occupied","unavailable","would leave","only numbers","invalid","rejected","not allowed"};
            return words.Any(x.Contains);
        }

        void OnDestroy(){ if(Instance==this) Instance=null; }
    }

    public sealed class PrototypeAudioUiTap : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        PrototypeAudioDirector d;
        public void Bind(PrototypeAudioDirector value)=>d=value;
        public void OnPointerEnter(PointerEventData e)=>d?.UiHover();
        public void OnPointerClick(PointerEventData e){ if(e.button==PointerEventData.InputButton.Left)d?.UiClick(); }
    }

    public sealed class PrototypeAudioCellTap : MonoBehaviour, IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler
    {
        const float Threshold=7f;
        PrototypeAudioDirector d;
        bool down,drag;
        Vector2 pos;
        int turn;
        string expression;

        public void Bind(PrototypeAudioDirector value)=>d=value;

        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left || d==null)return;
            var k=Keyboard.current;
            if(k!=null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed))return;
            down=true; drag=false; pos=e.position; turn=d.Turn; expression=d.Expression;
        }

        public void OnPointerMove(PointerEventData e)
        {
            if(!down || drag || d==null || Vector2.Distance(pos,e.position)<Threshold)return;
            drag=true; d.Pickup();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if(!down)return;
            down=false;
            if(drag && d!=null)StartCoroutine(AfterFrame());
        }

        IEnumerator AfterFrame(){ yield return null; d?.ResolveDrop(turn,expression); drag=false; }
    }
}
