using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Nota: el código de AUDIO (chiptune generado por código) vive en el archivo parcial
// AB900Game.Music.cs (misma clase). Campos de audio compartidos declarados aquí abajo.
public sealed partial class AB900Game : MonoBehaviour
{
    const int SlotCount = 3;
    const int AttackDamage = 16;   // daño base por respuesta correcta (+1 cada 2 niveles vía LevelAtkBonus)
    const int TomeQuestionXp = 15; // XP la PRIMERA vez que se domina cada pregunta de un tomo
    readonly Dictionary<string, object> data = new Dictionary<string, object>();
    readonly Dictionary<string, object> superData = new Dictionary<string, object>();
    readonly Dictionary<string, object> world = new Dictionary<string, object>();
    SaveData save;
    RectTransform root;
    Font font;
    string screen = "title";
    string message = "";
    Battle battle;
    int activeSlot = 1;
    int tomePage = 0;
    string activeTome = "";
    int superTomePage = 0;
    string activeSuperTome = "";
    string termOrigin = "supertome";  // pantalla a la que vuelve la definición de un término
    string sageDom = "";              // dominio del reto del Super Sabio en curso
    List<int> sageQueue;              // índices de bq del dominio, barajados
    int sagePos = 0;                  // posición actual dentro del reto
    int wisdomZi = 0;                 // jefe cuyos conocimientos se repasan
    Action wisdomBack;                // a dónde volver desde el repaso del jefe
    string dunArea = "";              // mazmorra generada actualmente
    List<object> dunLayout;           // layout procedural (filas string)
    List<Dictionary<string, object>> dunThings; // cosas colocadas al azar en la mazmorra
    // --- Torre del Estudio (runtime; el piso vive en save.studyFloor) ---
    readonly HashSet<string> towerSeen = new HashSet<string>();   // cobertura de towerbq por sesión (clave "tbq:i")
    List<int[]> towerEnemyRefs;       // refs planas {zoneIdx, enemyIdx} de TODOS los enemigos (cualquier tipo)
    List<int> studySageQueue;         // índices de towerbq barajados para el Sabio del piso 100
    int studySagePos = 0;             // posición en la prueba del Sabio del Estudio
    List<string> dialogLines;
    int dialogPage = 0;
    string dialogWho = "";
    bool dialogQuiz = false;
    System.Random rng = new System.Random();
    readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    readonly Dictionary<string, string> enemySprites = new Dictionary<string, string>();   // nombre enemigo -> sprite (de zones)
    int stepsSinceBattle = 0;
    readonly List<int[]> chasers = new List<int[]>();   // perseguidores {x, y, ei} (no se guardan)
    int[] bossChaser;                                   // {x,y} el JEFE persigue (2 pasos por el laberinto, sin cruzar muros) tras leer todos los tomos
    Dictionary<string, object> pendingLoot;             // tomo/cofre defendido por un monstruo
    string pendingTomeTile;                              // posición del token de tomo en curso (bloqueo por token)
    Action fiftyFifty;                                   // pergamino: marca 2 opciones incorrectas (solo preguntas single)
    int combo = 0;                     // racha de aciertos en combate: daño = base + combo (no se guarda)
    int towerDmgBonus = 0;             // mejora runtime de daño para la subida actual de la Torre (no se guarda)
    readonly List<int> sabioGuardsDown = new List<int>();   // partes (1..4) cuyo Jefe de Repaso ya cayó en el piso 18 (runtime)
    int walkFrame = 1;                 // ciclo de caminata (no se guarda); 1 = frame neutral
    Image playerTokenImg;              // token del jugador en el mapa actual
    Coroutine idleSettleCo;            // vuelta al frame neutral tras un paso
    AudioSource musicSrc, sfxSrc;
    string currentTrack = "";
    readonly Dictionary<string, AudioClip> musicClips = new Dictionary<string, AudioClip>();
    AudioClip sfxRight, sfxWrong, sfxWin;

    // ---- Simulador de Examen (NPC de ciudad) : pool propio, NO toca el dashboard ----
    List<object> examPool;
    List<Dictionary<string, object>> examSel, examWrong;
    List<Func<bool?>> examEvals;
    int examLastCorrect, examLastTotal;
    List<Dictionary<string, object>> recoveryExamSel;   // examen de recuperación de la Torre del Estudio
    List<Func<bool?>> recoveryExamEvals;
    int[] examGC, examGT;

    // ---- Mando / mapeo de teclas ----
    // Una asignación puede ser un botón (KeyCode) o un eje analógico con signo
    // (cruceta/sticks llegan como ejes en Android, no como botones).
    struct Bind
    {
        public bool isAxis;
        public KeyCode key;
        public int axis;   // índice 0..PadAxisCount-1
        public int sign;   // -1 o +1
        public bool Assigned => isAxis || key != KeyCode.None;
        public static Bind Key(KeyCode k) => new Bind { isAxis = false, key = k };
        public static Bind Axis(int a, int s) => new Bind { isAxis = true, axis = a, sign = s };
        public static Bind None => new Bind { isAxis = false, key = KeyCode.None };
    }

    const int PadAxisCount = 10;
    static string PadAxisName(int i) => "PadAxis" + i;

    readonly Dictionary<string, Bind> pad = new Dictionary<string, Bind>();
    readonly Dictionary<string, bool> bindHeldNow = new Dictionary<string, bool>();
    readonly Dictionary<string, bool> bindDownNow = new Dictionary<string, bool>();
    static readonly string[] PadActions = { "up", "down", "left", "right", "confirm", "back" };
    static readonly string[] PadLabels = { "Arriba  ▲", "Abajo  ▼", "Izquierda  ◀", "Derecha  ▶", "Acción / Confirmar", "Atrás / Cancelar" };
    static readonly Bind[] PadDefaults = { Bind.None, Bind.None, Bind.None, Bind.None, Bind.Key(KeyCode.JoystickButton0), Bind.Key(KeyCode.JoystickButton1) };
    string listeningBind = null;   // acción que espera asignación de tecla
    float listenStart = 0f;
    float[] axisBaseline;          // estado de los ejes al empezar a escuchar
    int heldDirX = 0, heldDirY = 0; // estado para auto-repetición de movimiento
    float moveRepeatAt = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        var go = new GameObject("AB900 Game Runtime");
        DontDestroyOnLoad(go);
        go.AddComponent<AB900Game>();
    }

    void Awake()
    {
        if (FindObjectsOfType<AB900Game>().Length > 1) { Destroy(gameObject); return; }
        Application.targetFrameRate = 60;
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        LoadData();
        LoadBinds();
        ApplyOrientation();
        EnsureEventSystem();
        SetupAudio();
        BuildBossSprites();
        RenderTitleSplash();
    }

    void Update()
    {
        UpdateBindEdges();
        if (listeningBind != null) { CaptureBind(); return; }
        if (screen == "splash") { if (Input.anyKeyDown) RenderTitle(); return; }   // cualquier tecla/botón inicia
        if (screen == "world") HandleWorldInput();
        else HandleMenuInput();
    }

    // Calcula una sola vez por frame el estado (pulsado / flanco) de cada acción,
    // soportando tanto botones como ejes (con detección de flanco para los ejes).
    void UpdateBindEdges()
    {
        for (int i = 0; i < PadActions.Length; i++)
        {
            string a = PadActions[i];
            bool prev = bindHeldNow.TryGetValue(a, out var p) && p;
            bool cur = BindActive(pad[a]);
            bindDownNow[a] = cur && !prev;
            bindHeldNow[a] = cur;
        }
    }

    bool BindActive(Bind b)
    {
        if (b.isAxis) return b.axis >= 0 && b.axis < PadAxisCount && b.sign * Input.GetAxisRaw(PadAxisName(b.axis)) > 0.5f;
        return b.key != KeyCode.None && Input.GetKey(b.key);
    }

    // Movimiento en el mapa: teclado (ejes Horizontal/Vertical incluyen WASD+flechas),
    // stick del mando (mismos ejes) y botones D-pad asignados, con auto-repetición.
    void HandleWorldInput()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        int dx = 0, dy = 0;
        if (h < -0.5f || BindHeld("left")) dx = -1; else if (h > 0.5f || BindHeld("right")) dx = 1;
        if (v > 0.5f || BindHeld("up")) dy = -1; else if (v < -0.5f || BindHeld("down")) dy = 1; // stick arriba (+v) = norte
        if (dx != 0) dy = 0; // un eje a la vez

        if (dx == 0 && dy == 0) { heldDirX = heldDirY = 0; }
        else if (dx != heldDirX || dy != heldDirY)
        {
            heldDirX = dx; heldDirY = dy; moveRepeatAt = Time.unscaledTime + 0.26f; Move(dx, dy);
        }
        else if (Time.unscaledTime >= moveRepeatAt)
        {
            moveRepeatAt = Time.unscaledTime + 0.13f; Move(dx, dy);
        }

        if (Input.GetButtonDown("Submit") || BindDown("confirm")) Action();
    }

    // En menús: el StandaloneInputModule ya maneja stick + Submit(A) + Cancel(B).
    // Aquí añadimos la navegación con los botones D-pad asignados a mano.
    void HandleMenuInput()
    {
        EnsureUiSelection();
        var es = EventSystem.current;
        if (es == null) return;
        var cur = es.currentSelectedGameObject;
        var sel = cur != null ? cur.GetComponent<Selectable>() : null;
        if (sel == null) return;

        Selectable next = null;
        if (BindDown("up")) next = sel.FindSelectableOnUp();
        else if (BindDown("down")) next = sel.FindSelectableOnDown();
        else if (BindDown("left")) next = sel.FindSelectableOnLeft();
        else if (BindDown("right")) next = sel.FindSelectableOnRight();
        if (next != null && next.IsActive() && next.IsInteractable())
            es.SetSelectedGameObject(next.gameObject);

        if (BindDown("confirm")) { var b = sel as Button; if (b != null) b.onClick.Invoke(); }
    }

    bool BindHeld(string a) => bindHeldNow.TryGetValue(a, out var v) && v;
    bool BindDown(string a) => bindDownNow.TryGetValue(a, out var v) && v;

    // Garantiza que siempre haya un botón seleccionado para poder navegar con el mando.
    void EnsureUiSelection()
    {
        var es = EventSystem.current;
        if (es == null || root == null) return;
        var cur = es.currentSelectedGameObject;
        if (cur != null && cur.activeInHierarchy)
        {
            var s = cur.GetComponent<Selectable>();
            if (s != null && s.IsInteractable()) return;
        }
        foreach (var sel in root.GetComponentsInChildren<Selectable>(false))
            if (sel.IsInteractable() && sel.gameObject.activeInHierarchy)
            {
                es.SetSelectedGameObject(sel.gameObject);
                return;
            }
    }

    static string BindToStr(Bind b) => b.isAxis ? ("a" + b.axis + ":" + b.sign) : ("k" + (int)b.key);

    static Bind StrToBind(string s)
    {
        if (string.IsNullOrEmpty(s)) return Bind.None;
        if (s[0] == 'a')
        {
            var parts = s.Substring(1).Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var ax) && int.TryParse(parts[1], out var sg))
                return Bind.Axis(ax, sg);
            return Bind.None;
        }
        if (s[0] == 'k' && int.TryParse(s.Substring(1), out var kv)) return Bind.Key((KeyCode)kv);
        return Bind.None;
    }

    void LoadBinds()
    {
        for (int i = 0; i < PadActions.Length; i++)
            pad[PadActions[i]] = StrToBind(PlayerPrefs.GetString("pad_" + PadActions[i], BindToStr(PadDefaults[i])));
    }

    void SaveBinds()
    {
        for (int i = 0; i < PadActions.Length; i++)
            PlayerPrefs.SetString("pad_" + PadActions[i], BindToStr(pad[PadActions[i]]));
        PlayerPrefs.Save();
    }

    void ResetBinds()
    {
        for (int i = 0; i < PadActions.Length; i++) pad[PadActions[i]] = PadDefaults[i];
        SaveBinds();
    }

    void BeginListen(string act)
    {
        listeningBind = act;
        listenStart = Time.unscaledTime;
        axisBaseline = new float[PadAxisCount];
        for (int i = 0; i < PadAxisCount; i++) axisBaseline[i] = Input.GetAxisRaw(PadAxisName(i));
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        RenderControls();
    }

    // Espera asignación: detecta ejes (cruceta/sticks) comparando contra el baseline
    // capturado al empezar (así los gatillos que descansan en -1 no se asignan solos),
    // y también botones/teclas.
    void CaptureBind()
    {
        if (Time.unscaledTime - listenStart < 0.3f)
        {
            // Durante el asentamiento refrescamos el baseline para no captar el toque/stick inicial.
            for (int i = 0; i < PadAxisCount; i++) axisBaseline[i] = Input.GetAxisRaw(PadAxisName(i));
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) { listeningBind = null; RenderControls(); return; }

        for (int i = 0; i < PadAxisCount; i++)
        {
            float d = Input.GetAxisRaw(PadAxisName(i)) - axisBaseline[i];
            if (Mathf.Abs(d) > 0.6f) { AssignBind(Bind.Axis(i, d > 0 ? 1 : -1)); return; }
        }
        foreach (var kc in BindableCodes)
            if (Input.GetKeyDown(kc)) { AssignBind(Bind.Key(kc)); return; }
    }

    void AssignBind(Bind b)
    {
        pad[listeningBind] = b;
        SaveBinds();
        listeningBind = null;
        PlaySfx(sfxRight);
        RenderControls();
    }

    static readonly KeyCode[] BindableCodes = BuildBindableCodes();
    static KeyCode[] BuildBindableCodes()
    {
        var list = new List<KeyCode>();
        for (int i = 0; i <= 19; i++) list.Add(KeyCode.JoystickButton0 + i);
        list.AddRange(new[] {
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.Space, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Backspace,
            KeyCode.Tab, KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.LeftControl, KeyCode.RightControl
        });
        // Todas las letras A-Z y dígitos 0-9 (teclado y numérico) para dar opciones reales.
        for (var k = KeyCode.A; k <= KeyCode.Z; k++) list.Add(k);
        for (var k = KeyCode.Alpha0; k <= KeyCode.Alpha9; k++) list.Add(k);
        for (var k = KeyCode.Keypad0; k <= KeyCode.Keypad9; k++) list.Add(k);
        return list.ToArray();
    }

    string BindName(Bind b)
    {
        if (b.isAxis) return "Eje " + b.axis + (b.sign > 0 ? "  ＋" : "  －") + "  (cruceta/stick)";
        if (b.key == KeyCode.None) return "(sin asignar)";
        if (b.key >= KeyCode.JoystickButton0 && b.key <= KeyCode.JoystickButton19)
            return "Boton " + (b.key - KeyCode.JoystickButton0);
        return b.key.ToString();
    }

    void RenderControls()
    {
        screen = "controls";
        ClearRoot();
        PlayTrack("title");
        AddBackdrop("Art/External/TitleVista", 0.35f);
        var p = Panel(root, "Controls", new Color(.04f, .06f, .13f, .96f), Anchor.Stretch, new Vector2(40, 28), new Vector2(-40, -28));
        var border = p.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(.45f, .9f, 1f, .5f); border.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 26);
        Label(col, "CONFIGURE CONTROLS", 32, new Color(.45f, .9f, 1f), FontStyle.Bold);
        Label(col, listeningBind != null
            ? "NOW press the key, or move the d-pad/stick or press the gamepad button you want to bind to: " + PadLabels[Array.IndexOf(PadActions, listeningBind)] + "   (ESC to cancel)"
            : "Press CHANGE and then press a KEYBOARD key, move the d-pad/stick or press a gamepad button. The arrow keys, WASD and the left stick ALWAYS move you; the keys you assign here are added on top.",
            17, new Color(.85f, .9f, 1f), FontStyle.Normal);

        for (int i = 0; i < PadActions.Length; i++)
        {
            string act = PadActions[i];
            var row = Panel(col, "row_" + act, new Color(.08f, .12f, .22f, 1f), Anchor.None, Vector2.zero, Vector2.zero);
            var rle = row.gameObject.AddComponent<LayoutElement>(); rle.minHeight = 72; rle.preferredHeight = 72; rle.flexibleWidth = 1;
            Horizontal(row, 12, TextAnchor.MiddleLeft);
            var nameLbl = Label(row, PadLabels[i], 20, Color.white, FontStyle.Bold);
            var le1 = nameLbl.gameObject.AddComponent<LayoutElement>(); le1.flexibleWidth = 1; le1.minWidth = 240;
            var curLbl = Label(row, BindName(pad[act]), 19, new Color(1f, .82f, .22f), FontStyle.Bold);
            var le2 = curLbl.gameObject.AddComponent<LayoutElement>(); le2.minWidth = 180; le2.preferredWidth = 180;
            bool listening = listeningBind == act;
            Button(row, listening ? "WAITING..." : "CHANGE",
                () => BeginListen(act),
                listening ? new Color(.5f, .2f, .2f) : new Color(.16f, .3f, .5f), 250);
        }

        var nav = Panel(col, "cnav", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
        var nle = nav.gameObject.AddComponent<LayoutElement>(); nle.minHeight = 84; nle.preferredHeight = 84; nle.flexibleWidth = 1;
        Horizontal(nav, 12, TextAnchor.MiddleCenter);
        Button(nav, "RESET", () => { ResetBinds(); RenderControls(); }, new Color(.4f, .3f, .12f), 230);
        Button(nav, "BACK", RenderTitle, new Color(.32f, .18f, .42f), 210);
    }

    void LoadData()
    {
        var txt = Resources.Load<TextAsset>("Data/ab900data");
        if (txt == null) throw new Exception("No se encontro Resources/Data/ab900data.json");
        var parsed = MiniJson.Deserialize(txt.text) as Dictionary<string, object>;
        foreach (var kv in parsed) data[kv.Key] = kv.Value;
        var superTxt = Resources.Load<TextAsset>("Data/supertomes");
        if (superTxt != null)
        {
            var parsedSuper = MiniJson.Deserialize(superTxt.text) as Dictionary<string, object>;
            foreach (var kv in parsedSuper) superData[kv.Key] = kv.Value;
        }
        // Banco del Simulador de Examen (NPC de ciudad): pool independiente, NO afecta al dashboard.
        var examTxt = Resources.Load<TextAsset>("Data/examprep");
        if (examTxt != null) examPool = MiniJson.Deserialize(examTxt.text) as List<object>;
        BuildEnemySpriteMap();
    }

    // Mapa nombre-de-enemigo -> sprite, tomado de los campos "spr" de las zonas.
    // Permite enemigos/jefes propios del AB-410 sin codificar cada nombre en SpriteForEnemyName.
    void BuildEnemySpriteMap()
    {
        enemySprites.Clear();
        if (!data.ContainsKey("zones")) return;
        foreach (var z in L(data["zones"]))
        {
            var zone = z as Dictionary<string, object>;
            if (zone == null) continue;
            if (zone.ContainsKey("enemies"))
                foreach (var e in L(zone["enemies"]))
                {
                    var en = e as Dictionary<string, object>;
                    if (en != null && en.ContainsKey("spr")) enemySprites[S(en, "n")] = S(en, "spr");
                }
            if (zone.ContainsKey("boss"))
            {
                var bo = D(zone["boss"]);
                if (bo.ContainsKey("spr")) enemySprites[S(bo, "n")] = S(bo, "spr");
            }
        }
    }

    void NewGame(int slot, string lang)
    {
        activeSlot = slot;
        save = new SaveData
        {
            slot = slot, area = "town", x = 10, y = 10, fx = 0, fy = 1,
            lv = 1, hp = 34, maxhp = 34, mp = 10, maxmp = 10, xp = 0, xpNext = 30,
            lang = lang, qlang = lang,
            readTomes = new List<string>(), goneThings = new List<string>(), cleared = new List<string>(), wrongQ = new List<string>(),
            seenQ = new List<string>(), qstats = new List<QStat>(), medallions = new List<string>(),
            correctQ = new List<string>(), noEnemyZones = new List<string>(),
            items = new List<ItemStack>(), trophies = new List<string>(), towerDone = new List<int>(), sagesDown = new List<int>(), stashLang = ""
        };
        save.studyCheckpoint = 0;
        SaveSlot();
        RenderWorld(T("Nueva partida guardada en ranura " + slot + ".", "New game saved to slot " + slot + "."));
    }

    // Pantalla de idioma: solo aparece al empezar una partida NUEVA en una ranura vacía.
    void RenderLanguagePick(int slot)
    {
        screen = "langpick";
        ClearRoot();
        PlayTrack("title");
        AddBackdrop("Art/External/TitleVista", 0.4f);
        var p = Panel(root, "LangPick", new Color(.04f, .06f, .13f, .94f), Anchor.Stretch, new Vector2(60, 40), new Vector2(-60, -40));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .55f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 14, 28);
        Label(col, "RANURA " + slot + " · NUEVA PARTIDA", 24, new Color(.55f, .8f, 1f), FontStyle.Bold);
        Label(col, "Elige el idioma de la partida\nChoose your game language", 22, Color.white, FontStyle.Bold);
        Button(col, "ESPAÑOL", () => NewGame(slot, "es"), new Color(.16f, .45f, .55f));
        Button(col, "ENGLISH", () => NewGame(slot, "en"), new Color(.2f, .35f, .6f));
        Label(col, "El Mago del Idioma de la ciudad podrá cambiar después el idioma de las preguntas.\nThe Language Mage in town can later switch the question language.", 15, new Color(.8f, .88f, 1f), FontStyle.Italic);
        Button(col, "VOLVER / BACK", RenderTitle, new Color(.32f, .18f, .42f));
    }

    void LoadGame(int slot)
    {
        activeSlot = slot;
        var path = SlotPath(slot);
        if (!File.Exists(path)) { NewGame(slot, "en"); return; }   // English-only: sin pantalla de idioma
        SaveData loaded = null;
        try { loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(path)); } catch { loaded = null; }
        if (loaded == null || string.IsNullOrEmpty(loaded.area)) { NewGame(slot, "en"); return; }   // guardado dañado: nueva partida
        save = loaded;
        chasers.Clear(); pendingLoot = null; stepsSinceBattle = 0; combo = 0; towerDmgBonus = 0;
        if (save.readTomes == null) save.readTomes = new List<string>();
        if (save.goneThings == null) save.goneThings = new List<string>();
        if (save.cleared == null) save.cleared = new List<string>();
        if (save.wrongQ == null) save.wrongQ = new List<string>();
        if (save.seenQ == null) save.seenQ = new List<string>();
        if (save.qstats == null) save.qstats = new List<QStat>();
        if (save.medallions == null) save.medallions = new List<string>();
        if (save.correctQ == null) save.correctQ = new List<string>();
        if (save.noEnemyZones == null) save.noEnemyZones = new List<string>();
        if (save.metEnemies == null) save.metEnemies = new List<string>();
        if (save.readTomeTiles == null) save.readTomeTiles = new List<string>();
        if (save.readSuper == null) save.readSuper = new List<string>();
        if (save.contosoDone == null) save.contosoDone = new List<int>();
        if (save.contosoCleared == null) save.contosoCleared = new List<string>();
        if (save.items == null) save.items = new List<ItemStack>();
        if (save.trophies == null) save.trophies = new List<string>();
        if (save.towerDone == null) save.towerDone = new List<int>();
        if (save.sagesDown == null) save.sagesDown = new List<int>();
        if (save.stashLang == null) save.stashLang = "";
        if (string.IsNullOrEmpty(save.lang)) save.lang = "es";       // partidas antiguas
        if (string.IsNullOrEmpty(save.qlang)) save.qlang = save.lang;
        save.studyCheckpoint = Math.Max(save.studyCheckpoint, StudyCheckpointFor(save.studyFloor));
        if (save.area == "studytower")
        {
            // Cargar dentro de la Torre del Estudio: SIEMPRE despiertas en el vestíbulo y el
            // bibliotecario te ofrece "CONTINUAR DESDE EL PISO N". Guardamos tu piso EXACTO como
            // checkpoint (incluso pisos 1-9, que antes no tenían checkpoint y se perdían), así
            // nunca pierdes la subida. Además, no se regenera ningún laberinto en la carga: si esa
            // generación fallaba, te quedabas atascado en el título sin partida; este camino es seguro.
            if (save.studyFloor >= 1) save.studyCheckpoint = Math.Max(save.studyCheckpoint, save.studyFloor);
            save.studyFloor = 0;
            save.x = TowerStart[0]; save.y = TowerStart[1]; save.fx = 0; save.fy = -1;
            SaveSlot();
        }
        else if (save.studyFloor > 0)
        {
            save.studyFloor = 0; SaveSlot();   // partidas antiguas con un piso colgado fuera de la torre
        }
        if (IsDungeon(save.area)) GenerateDungeon(save.area);   // mazmorra fresca al cargar dentro de una
        RenderWorld(T("Partida cargada de ranura " + slot + ".", "Game loaded from slot " + slot + "."));
    }

    void SaveSlot()
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        // Escritura ATÓMICA: primero a un .tmp y luego se reemplaza el archivo real. Si la app se
        // cierra a media escritura, el guardado real NO queda truncado (antes eso lo corrompía y
        // la pantalla de título no podía mostrar los slots).
        var path = SlotPath(activeSlot);
        var tmp = path + ".tmp";
        var json = JsonUtility.ToJson(save, true);
        try
        {
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
        catch
        {
            // Si el reemplazo atómico falla por lo que sea, cae al guardado directo (mejor que nada).
            File.WriteAllText(path, json);
        }
    }

    string SlotPath(int slot) => Path.Combine(Application.persistentDataPath, "ab900_slot_" + slot + ".json");

    // Pantalla de inicio (splash): muestra la imagen del título y "presiona la pantalla".
    // Al tocar (o pulsar cualquier tecla/botón) pasa al menú, como en los juegos clásicos.
    void RenderTitleSplash()
    {
        screen = "splash";
        ClearRoot();
        PlayTrack("title");
        AddBackdrop("Art/External/TitleVista", 1f);
        Panel(root, "SplashVeil", new Color(0f, 0f, 0f, .26f), Anchor.Stretch, Vector2.zero, Vector2.zero);

        // Botón invisible a pantalla completa: cualquier toque/clic inicia.
        var tap = new GameObject("TapToStart", typeof(Image), typeof(Button));
        tap.transform.SetParent(root, false);
        var trt = tap.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        tap.GetComponent<Image>().color = new Color(0f, 0f, 0f, .004f);   // casi invisible pero capta el toque
        tap.GetComponent<Button>().onClick.AddListener(RenderTitle);

        SplashText("POWER PLATFORM SAGA", 58, new Color(1f, .85f, .28f), .70f, 130);
        SplashText("AB-410 · El reino de Power Platform y Copilot", 23, new Color(.72f, .86f, 1f), .60f, 54);
        var iconBar = Panel(root, "SplashIcons", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
        iconBar.anchorMin = new Vector2(.5f, .45f); iconBar.anchorMax = new Vector2(.5f, .45f);
        iconBar.pivot = new Vector2(.5f, .5f); iconBar.sizeDelta = new Vector2(560, 84); iconBar.anchoredPosition = Vector2.zero;
        Horizontal(iconBar, 14, TextAnchor.MiddleCenter);
        foreach (var n in new[] { "icon_d1", "icon_d2", "icon_d3", "tome", "mon_demon" })
            FramedSprite(iconBar, n, 64, new Color(1f, .82f, .22f, .85f));
        var prompt = SplashText(T("▶  PRESIONA LA PANTALLA", "▶  PRESS THE SCREEN"), 32, Color.white, .17f, 80);
        StartCoroutine(SplashBlink(prompt));
    }

    Text SplashText(string text, int size, Color color, float yFrac, float h)
    {
        var t = Label(root, text, size, color, FontStyle.Bold, Anchor.None);
        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(.5f, yFrac); rt.anchorMax = new Vector2(.5f, yFrac); rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = new Vector2(1180, h); rt.anchoredPosition = Vector2.zero;
        t.alignment = TextAnchor.MiddleCenter;
        var sh = t.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, .85f); sh.effectDistance = new Vector2(3, -3);
        return t;
    }

    System.Collections.IEnumerator SplashBlink(Text prompt)
    {
        bool on = true;
        while (screen == "splash" && prompt != null)
        {
            prompt.color = on ? Color.white : new Color(1f, 1f, 1f, .22f);
            on = !on;
            yield return new WaitForSeconds(.55f);
        }
    }

    void RenderTitle()
    {
        screen = "title";
        ClearRoot();
        PlayTrack("title");
        AddBackdrop("Art/External/TitleVista", 0.5f);
        var panel = Panel(root, "TitlePanel", new Color(0.04f, 0.06f, 0.13f, 0.9f), Anchor.Stretch, new Vector2(46, 30), new Vector2(-46, -30));
        var border = panel.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(1f, .82f, .22f, .55f); border.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(panel, 12, 26);
        Label(col, "POWER PLATFORM SAGA", 36, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, "AB-410 · El reino de Power Platform y Copilot", 20, new Color(.55f, .75f, 1f), FontStyle.Bold);
        // Las RANURAS van ARRIBA del todo para que SIEMPRE se vean sin tener que desplazar
        // (en algunos móviles el resto del contenido empujaba los slots fuera de pantalla).
        for (int i = 1; i <= SlotCount; i++)
        {
            int s = i;
            Button(col, SlotLabel(s), () => LoadGame(s), new Color(.16f, .23f, .48f));
        }
        SpriteRow(col, new[] { "icon_d1", "icon_d2", "icon_d3", "tome", "mon_demon" }, 52, new Color(1f, .82f, .22f, .85f));
        Label(col, "RPG de estudio para el AB-410: Power Platform, Dataverse, Power Apps, Power Pages, Power Automate, AI Builder y Copilot. Pensado para móvil Android.", 18, Color.white, FontStyle.Normal);
        Button(col, "◆  SYLLABUS REVIEW  ◆", RenderReview, new Color(.16f, .45f, .55f));
        Button(col, "🎮  CONFIGURE CONTROLS / TECLAS", RenderControls, new Color(.2f, .42f, .5f));
        Button(col, OrientationLabel(), CycleOrientation, new Color(.22f, .38f, .5f));
        Button(col, "Delete current slot", DeleteActiveSlot, new Color(.42f, .11f, .15f));
        Label(col, "Art: Kenney Tiny Dungeon · OpenGameArt backgrounds · CC0 license", 14, new Color(.78f, .84f, .95f), FontStyle.Normal);
    }

    // ---- Orientación de pantalla, elegible desde el título: AUTO / horizontal / vertical ----
    // "auto" = el móvil decide girando el aparato (Screen.orientation = AutoRotation).
    string OrientationMode() => PlayerPrefs.GetString("orientation", "auto");   // "auto"|"landscape"|"portrait"

    string OrientationLabel()
    {
        switch (OrientationMode())
        {
            case "portrait": return "📱  ORIENTACIÓN: VERTICAL";
            case "landscape": return "🖥  ORIENTACIÓN: HORIZONTAL";
            default: return "🔄  ORIENTACIÓN: AUTO (gira el móvil)";
        }
    }

    // Aplica la orientación guardada: en móvil fija Screen.orientation (AUTO = sensor del
    // teléfono); en escritorio redimensiona la ventana. Llamada al arrancar y al alternar.
    void ApplyOrientation()
    {
        string m = OrientationMode();
        if (Application.isMobilePlatform)
            Screen.orientation = m == "portrait" ? ScreenOrientation.Portrait
                               : m == "landscape" ? ScreenOrientation.LandscapeLeft
                               : ScreenOrientation.AutoRotation;   // auto = gira con el móvil
        else
        {
            bool portrait = m == "portrait";   // sin sensor en escritorio: AUTO se trata como horizontal
            Screen.SetResolution(portrait ? 720 : 1280, portrait ? 1280 : 720, FullScreenMode.Windowed);
        }
    }

    // Cicla AUTO → HORIZONTAL → VERTICAL → AUTO.
    void CycleOrientation()
    {
        string next = OrientationMode() == "auto" ? "landscape" : OrientationMode() == "landscape" ? "portrait" : "auto";
        PlayerPrefs.SetString("orientation", next);
        PlayerPrefs.Save();
        ApplyOrientation();
        RenderTitle();   // re-renderiza para que el CanvasScaler recoloque la UI
    }

    string SlotLabel(int slot)
    {
        var p = SlotPath(slot);
        if (!File.Exists(p)) return "SLOT " + slot + " - NEW GAME";
        // BLINDAJE: si un guardado está dañado (p.ej. la app se cerró a media escritura),
        // NUNCA lanzar excepción aquí. Antes, una excepción abortaba RenderTitle JUSTO después
        // de la descripción y NO se dibujaba NINGÚN slot ni botón (la pantalla parecía "sin slots").
        try
        {
            var s = JsonUtility.FromJson<SaveData>(File.ReadAllText(p));
            if (s == null || string.IsNullOrEmpty(s.area)) return "SLOT " + slot + " - ⚠ DAÑADO (toca para nueva)";
            return "SLOT " + slot + " - LV " + s.lv + " - " + AreaName(s.area);
        }
        catch { return "SLOT " + slot + " - ⚠ DAÑADO (toca para nueva)"; }
    }

    // Borrar partida con SEGURO: hay que acertar una pregunta al azar; si no, puedes HUIR.
    // (Evita borrados accidentales como el del slot por un toque.)
    void DeleteActiveSlot()
    {
        if (!File.Exists(SlotPath(activeSlot))) { RenderTitle(); return; }
        RenderDeleteConfirm();
    }

    void RenderDeleteConfirm(string msg = "")
    {
        screen = "dialog";
        ClearRoot();
        AddBackdrop("Art/External/TitleVista", 0.35f);
        var p = Panel(root, "DelConfirm", new Color(.12f, .04f, .06f, .96f), Anchor.Stretch, new Vector2(30, 26), new Vector2(-30, -26));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .4f, .4f, .7f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 22);
        Label(col, T("⚠ ELIMINAR PARTIDA · RANURA " + activeSlot, "⚠ DELETE SAVE · SLOT " + activeSlot), 26, new Color(1f, .5f, .45f), FontStyle.Bold);
        Label(col, T("Seguro contra borrados sin querer: responde BIEN esta pregunta para eliminar la partida. Si no quieres borrarla, HUYE.",
                     "Anti-accident lock: answer this question CORRECTLY to delete the save. If you don't want to, FLEE."), 17, Color.white, FontStyle.Normal);
        if (!string.IsNullOrEmpty(msg)) Label(col, msg, 16, new Color(1f, .82f, .5f), FontStyle.Bold);
        var all = L(data["bq"]);
        var q = all[rng.Next(all.Count)] as Dictionary<string, object>;
        var eval = BuildQuestionBody(col, q);
        var warn = Label(col, " ", 15, new Color(1f, .65f, .45f), FontStyle.Bold);
        OptButton(col, T("🗑  ELIMINAR (responder)", "🗑  DELETE (answer)"), () =>
        {
            var r = eval();
            if (r == null) { warn.text = T("⚠ Completa la respuesta.", "⚠ Complete the answer."); return; }
            if (r == true)
            {
                var path = SlotPath(activeSlot);
                if (File.Exists(path)) File.Delete(path);
                RenderTitle();
            }
            else
                RenderDeleteConfirm(T("❌ Respuesta incorrecta: la partida NO se eliminó (sigue a salvo). Otra pregunta...",
                                      "❌ Wrong answer: the save was NOT deleted (still safe). Another question..."));
        }, new Color(.5f, .18f, .2f));
        Button(col, T("🏃  HUIR (conservar la partida)", "🏃  FLEE (keep the save)"), RenderTitle, new Color(.2f, .45f, .32f));
    }

    void RenderReview()
    {
        screen = "review";
        ClearRoot();
        PlayTrack("title");
        AddBackdrop("Art/External/TitleVista", 0.32f);
        var p = Panel(root, "Review", new Color(.05f, .07f, .14f, .95f), Anchor.Stretch, new Vector2(16, 16), new Vector2(-16, -16));

        var header = Panel(p, "RevHeader", new Color(.04f, .06f, .12f, 1f), Anchor.TopStretch, new Vector2(0, 0), new Vector2(0, -72));
        Horizontal(header, 12, TextAnchor.MiddleCenter);
        Label(header, "REPASO DEL TEMARIO AB-410", 24, new Color(1f, .82f, .22f), FontStyle.Bold);
        Button(header, "BACK", RenderTitle, new Color(.32f, .18f, .42f), 190);

        var viewport = Panel(p, "RevViewport", new Color(.02f, .03f, .06f, 1f), Anchor.Stretch, new Vector2(8, 8), new Vector2(-8, -80));
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
        var content = Panel(viewport, "RevContent", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
        content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
        scroll.content = content; scroll.viewport = viewport;
        var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 16, 16); v.spacing = 9; v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        SpriteRow(content, new[] { "npc_recepcion", "tome", "mon_slime", "chest", "mon_demon" }, 60, new Color(.55f, .8f, 1f, .8f));
        ReviewLabel(content, "Consejo de oro AB-410: en cada pregunta identifica el COMPONENTE (Power Apps lienzo/modelo, Power Automate, Power Pages, Copilot Studio, AI Builder, Dataverse), el VERBO (crear, automatizar, almacenar, exponer, proteger) y quién controla los DATOS.", 19, new Color(.95f, .85f, .5f), FontStyle.BoldAndItalic);

        var tomes = D(data["tomes"]);
        string lastDom = "";
        foreach (var kv in tomes)
        {
            string id = kv.Key;
            var tome = kv.Value as Dictionary<string, object>;
            string dom = TomeDomain(id);
            if (dom != lastDom)
            {
                FramedSprite(content, DomainIcon(dom), 72, new Color(.55f, .8f, 1f, .85f));
                ReviewLabel(content, DomainTitle(dom), 22, new Color(.55f, .8f, 1f), FontStyle.Bold);
                lastDom = dom;
            }
            ReviewLabel(content, "▶ " + S(tome, "t"), 20, new Color(1f, .82f, .22f), FontStyle.Bold);
            foreach (var pg in L(tome["pages"]))
                ReviewLabel(content, "• " + S(pg), 17, Color.white, FontStyle.Normal);
            var check = D(tome["check"]);
            var opts = L(check["o"]);
            ReviewLabel(content, "✔ " + S(check, "q") + "  →  " + S(opts[I(check["a"])]), 17, new Color(.55f, 1f, .63f), FontStyle.Bold);
        }
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1f;
    }

    // Emblemas propios por dominio (generados por código en BuildDomainIcons):
    // el sprite del protagonista queda reservado SOLO para el jugador en el mapa.
    string DomainIcon(string dom)
    {
        switch (dom)
        {
            case "d1": case "p1": return "icon_d1";
            case "d2": case "p2": return "icon_d2";
            case "d3": case "p3": return "icon_d3";
            case "p4": return "icon_d1";
            default: return "tome";
        }
    }

    string DomainTitle(string dom)
    {
        switch (dom)
        {
            case "d1": case "p1": return "PARTE 1 · Fundamentos de Power Platform con IA (Copilot, agentes, prompts)";
            case "d2": case "p2": return "PARTE 2 · Microsoft Dataverse (tablas, columnas, relaciones, seguridad)";
            case "d3": case "p3": return "PARTE 3 · Power Apps y Power Pages (lienzo, modelo, formularios, sitios)";
            case "p4": return "PARTE 4 · Power Automate y AI Builder (flujos, Dataverse, aprobaciones)";
            default: return "REPASO";
        }
    }

    void ReviewLabel(RectTransform parent, string text, int size, Color color, FontStyle style)
    {
        var t = Label(parent, text, size, color, style);
        t.alignment = TextAnchor.UpperLeft;
    }

    void RenderWorld(string msg = "")
    {
        screen = "world";
        message = msg;
        ClearRoot();
        // Cada mazmorra tiene su propia pista, cada vez más oscura y desafiante.
        PlayTrack(TrackForArea(save.area));
        AddBackdrop(BackdropForArea(save.area), 0.55f);
        var hud = Panel(root, "Hud", new Color(.04f, .055f, .11f, .92f), Anchor.TopStretch, new Vector2(18, -12), new Vector2(-18, -86));
        Horizontal(hud, 12, TextAnchor.MiddleCenter);
        Label(hud, AreaName(save.area), 22, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(hud, "LV " + save.lv, 20, Color.white, FontStyle.Bold);
        Label(hud, "HP " + save.hp + "/" + save.maxhp, 20, new Color(.55f, 1f, .63f), FontStyle.Bold);
        if (save.area == "studytower")   // en la torre la RACHA es moneda y el ataque sube: siempre a la vista
            Label(hud, T("🔥 Racha " + combo + " · ⚔ " + TowerBaseDamage(), "🔥 Streak " + combo + " · ⚔ " + TowerBaseDamage()), 20, new Color(1f, .73f, .32f), FontStyle.Bold);
        else if (combo > 0) Label(hud, "🔥 " + combo, 20, new Color(1f, .73f, .32f), FontStyle.Bold);   // racha actual
        Label(hud, T("Tomos ", "Tomes ") + ReadTomesInArea() + "/" + TotalTomesInArea(), 20, new Color(.6f, .8f, 1f), FontStyle.Bold);
        if (IsDungeon(save.area)) { DomainProgress(save.area, out int dm, out int dt); Label(hud, T("Dominadas ", "Mastered ") + dm + "/" + dt, 20, new Color(.7f, .92f, 1f), FontStyle.Bold); }
        else Label(hud, T("Dominadas ", "Mastered ") + MasteredCount() + "/" + L(data["bq"]).Count, 20, new Color(.7f, .92f, 1f), FontStyle.Bold);
        if (save.wrongQ.Count > 0) Label(hud, T("Dudas ", "Doubts ") + save.wrongQ.Count, 20, new Color(1f, .55f, .35f), FontStyle.Bold);
        if (IsDungeon(save.area) && ZoneAllMastered(save.area))
        {
            bool off = save.noEnemyZones.Contains(save.area);
            Button(hud, off ? T("ENEMIGOS: OFF", "ENEMIES: OFF") : T("ENEMIGOS: ON", "ENEMIES: ON"), () =>
            {
                if (off) save.noEnemyZones.Remove(save.area); else if (!save.noEnemyZones.Contains(save.area)) save.noEnemyZones.Add(save.area);
                SaveSlot();
                RenderWorld(off ? T("Enemigos reactivados en esta zona.", "Enemies re-enabled in this zone.") : T("¡Zona dominada! Enemigos desactivados aquí.", "Zone mastered! Enemies disabled here."));
            }, off ? new Color(.5f, .25f, .25f) : new Color(.22f, .45f, .3f), 210);
        }
        bool dungeonDone = IsDungeon(save.area) && (save.cleared.Contains(save.area) || (TotalTomesInArea() > 0 && ReadTomesInArea() == TotalTomesInArea()));
        if (dungeonDone)
        {
            Button(hud, T("AL HUB ▶", "TO HUB ▶"), () => TryLeaveDungeon(ExitDungeon), new Color(.5f, .22f, .24f), 150);
            Button(hud, T("CIUDAD ▶", "TOWN ▶"), () => TryLeaveDungeon(ExitToTown), new Color(.24f, .35f, .5f), 150);
        }

        var frame = Panel(root, "MapFrame", new Color(.16f, .2f, .34f, 1f), Anchor.Middle, Vector2.zero, Vector2.zero);
        frame.sizeDelta = new Vector2(1196, 480);
        bool seeThrough = IsDungeon(save.area) || save.area == "final" || save.area == "studytower";   // deja ver el fondo del área
        var viewport = Panel(frame, "MapViewport", new Color(.02f, .025f, .05f, seeThrough ? .35f : 1f), Anchor.Stretch, new Vector2(4, 4), new Vector2(-4, -4));
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28;
        var map = Panel(viewport, "MapContent", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
        map.anchorMin = new Vector2(0, 1); map.anchorMax = new Vector2(0, 1); map.pivot = new Vector2(0, 1);
        scroll.content = map; scroll.viewport = viewport;
        var grid = map.gameObject.AddComponent<GridLayoutGroup>();
        var layout = Layout();
        int cols = S(layout[0]).Length, rows = layout.Count;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.cellSize = new Vector2(76, 76);
        grid.spacing = new Vector2(4, 4);
        grid.padding = new RectOffset(12, 12, 12, 12);
        map.sizeDelta = new Vector2(grid.padding.left + grid.padding.right + cols * grid.cellSize.x + (cols - 1) * grid.spacing.x,
                                    grid.padding.top + grid.padding.bottom + rows * grid.cellSize.y + (rows - 1) * grid.spacing.y);
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < S(layout[y]).Length; x++)
        {
            var row = S(layout[y]);
            var cell = Panel(map, "Cell", TileColor(row[x]), Anchor.None, Vector2.zero, Vector2.zero);
            var floorSpr = LoadSprite(FloorSpriteFor(row[x]));
            bool decoTile = row[x] == 'T' || row[x] == 'A' || row[x] == 'C' || row[x] == 'G';
            if (floorSpr != null && decoTile)
            {
                // Adorno con transparencia (árbol/atrezzo remasterizado): suelo o muro
                // base debajo y el adorno encima, para que no quede una caja negra.
                var ci = cell.GetComponent<Image>();
                var tileTint = AreaTileTint(save.area);
                var baseSpr = LoadSprite(row[x] == 'T' ? "floor_town" : "wall");
                if (baseSpr != null) { ci.sprite = baseSpr; ci.type = Image.Type.Simple; ci.preserveAspect = false; ci.color = tileTint; }
                var deco = new GameObject("TileDeco", typeof(Image));
                deco.transform.SetParent(cell, false);
                var drt = deco.GetComponent<RectTransform>();
                drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
                var di = deco.GetComponent<Image>();
                di.sprite = floorSpr; di.type = Image.Type.Simple; di.preserveAspect = true; di.color = tileTint;
            }
            else if (floorSpr != null)
            {
                var ci = cell.GetComponent<Image>();
                ci.sprite = floorSpr; ci.type = Image.Type.Simple; ci.preserveAspect = false;
                var tileTint = AreaTileTint(save.area);   // paleta propia por mazmorra
                if (seeThrough && row[x] == '.') tileTint.a = .62f;   // el camino transparenta el escenario
                ci.color = tileTint;
            }
            else if (seeThrough && row[x] == '.')
            {
                var ci = cell.GetComponent<Image>();
                var fc = ci.color; fc.a = .6f; ci.color = fc;
            }
            var thing = ThingAt(x, y);
            bool bossChaserHere = bossChaser != null && bossChaser[0] == x && bossChaser[1] == y;
            if (x == save.x && y == save.y)
            {
                playerTokenImg = Token(cell, PlayerSpriteName(walkFrame), new Color(.26f, .62f, 1f), "@");
                if (idleSettleCo != null) StopCoroutine(idleSettleCo);
                if (walkFrame % 2 == 0) idleSettleCo = StartCoroutine(PlayerIdleSettle());
            }
            else if (bossChaserHere)
            {
                // El JEFE persiguiéndote: se dibuja con prioridad (incluso sobre cosas) y es el
                // ÚNICO jefe en pantalla — su token estático de la esquina se oculta más abajo.
                var bz = L(data["zones"])[ZoneIndexForArea(save.area)] as Dictionary<string, object>;
                Token(cell, SpriteForEnemyName(S(D(bz["boss"]), "n")), new Color(1f, .3f, .15f), "‼", T("JEFE", "BOSS"), new Color(1f, .7f, .7f, .96f));
            }
            // Mientras el jefe persigue (bossChaser != null), NO dibujamos su token estático de la
            // esquina: así no se ven DOS jefes a la vez. El resto de cosas se dibujan normal.
            else if (thing != null && !(bossChaser != null && S(thing, "kind") == "boss"))
            {
                string cap = null; Color? tint = null;
                if (S(thing, "kind") == "portal")
                {
                    string to = S(thing, "to");
                    bool done = save.cleared.Contains(to);
                    cap = PortalShort(to) + (done ? " ✓" : "");
                    if (done && LoadSprite("portal_done") == null) tint = new Color(.45f, 1f, .55f);   // el portal dorado ya canta victoria
                }
                else if (S(thing, "kind") == "rematch") { cap = T("REVANCHA", "REMATCH"); tint = new Color(.6f, 1f, .72f); }
                else if (S(thing, "kind") == "reviewboss") { cap = T("JEFE REPASO P", "REVIEW BOSS P") + I(thing["part"]); tint = new Color(1f, .72f, .6f); }
                else if (S(thing, "kind") == "hubsage") { cap = T("SABIO", "SAGE"); tint = new Color(1f, .9f, .6f); }
                Token(cell, SpriteForThing(thing), TokenColor(thing), Glyph(TokenKind(thing)), cap, tint);
            }
            else if (IsDungeon(save.area) || save.area == "studytower")
            {
                var ch = ChaserAt(x, y);   // perseguidor fantasma (puede estar sobre un muro)
                if (ch != null)
                {
                    string spr = null;
                    if (save.area == "studytower")
                    {
                        var refs = TowerEnemyRefs();
                        if (refs.Count > 0)
                        {
                            var r = refs[ch[2] % refs.Count];
                            var zone = L(data["zones"])[r[0]] as Dictionary<string, object>;
                            var baseEnemy = L(zone["enemies"])[r[1]] as Dictionary<string, object>;
                            spr = SpriteForEnemyName(S(baseEnemy, "n"));
                        }
                    }
                    else spr = SpriteForEnemyName(S(ZoneEnemy(ch[2]), "n"));
                    Token(cell, spr, new Color(.9f, .2f, .28f), "!", null, new Color(1f, .8f, .8f, .92f));
                }
            }
        }
        Canvas.ForceUpdateCanvases();
        CenterScrollOnPlayer(scroll, grid, cols, rows);

        var controls = Panel(root, "Controls", new Color(.04f, .055f, .11f, .92f), Anchor.BottomStretch, new Vector2(18, 12), new Vector2(-18, 116));
        Horizontal(controls, 12, TextAnchor.MiddleCenter);
        Button(controls, "◀", () => Move(-1, 0), new Color(.12f, .18f, .38f), 92);
        Button(controls, "▲", () => Move(0, -1), new Color(.12f, .18f, .38f), 92);
        Button(controls, "▼", () => Move(0, 1), new Color(.12f, .18f, .38f), 92);
        Button(controls, "▶", () => Move(1, 0), new Color(.12f, .18f, .38f), 92);
        Button(controls, "A", Action, new Color(.95f, .72f, .12f), 110);
        IconButton(controls, "item_bag", () => RenderItems(), new Color(.55f, .4f, .15f), 100);   // sprite real de la mochila junto al botón A (el emoji no se ve en el APK)
        if (save.area == "studytower" && save.studyFloor >= 1 && HasHammer())   // martillo Rompemuros: rompe el muro de enfrente por 1 racha
            Button(controls, T("MURO", "WALL"), UseHammerBreak, new Color(.5f, .34f, .14f), 120);
        Button(controls, "SAVE", () => { SaveSlot(); RenderWorld(T("Partida guardada.", "Game saved.")); }, new Color(.18f, .42f, .25f), 145);
        Button(controls, "MENU", RenderTitle, new Color(.32f, .18f, .42f), 145);
        Label(root, HintText(), 20, new Color(.86f, .9f, 1f), FontStyle.Bold, Anchor.BottomStretch, new Vector2(24, 100), new Vector2(-24, 132));
    }

    // Frames del sheet por direccion: 0 paso, 1 neutral (idle), 2 paso, 3 neutral.
    string PlayerDir() => save.fy < 0 ? "up" : save.fy > 0 ? "down" : save.fx < 0 ? "left" : save.fx > 0 ? "right" : "down";

    string PlayerSpriteName(int frame)
    {
        string name = "player_" + PlayerDir() + "_" + ((frame % 4 + 4) % 4);
        return LoadSprite(name) != null ? name : "player";   // fallback al tile clasico
    }

    IEnumerator PlayerIdleSettle()
    {
        yield return new WaitForSeconds(0.18f);
        if (screen != "world" || playerTokenImg == null || playerTokenImg.sprite == null) yield break;
        var sp = LoadSprite(PlayerSpriteName(1));
        if (sp != null) playerTokenImg.sprite = sp;
    }

    void Move(int dx, int dy)
    {
        save.fx = dx; save.fy = dy;
        int nx = save.x + dx, ny = save.y + dy;
        var layout = Layout();
        if (ny < 0 || nx < 0 || ny >= layout.Count || nx >= S(layout[ny]).Length) return;
        char tile = S(layout[ny])[nx];
        if (tile == '#') { RenderWorld(T("Una pared bloquea el camino.", "A wall blocks the way.")); return; }
        if (tile == '~') { RenderWorld(T("El agua de la fuente te corta el paso.", "The fountain water blocks your path.")); return; }
        if (tile == 'T') { RenderWorld(T("Un árbol bloquea el camino.", "A tree blocks the way.")); return; }
        if (tile == 'A') { RenderWorld(T("Una antorcha de la fortaleza arde en el muro.", "A fortress torch burns on the wall.")); return; }
        if (tile == 'C') { RenderWorld(T("Un cristal de datos bloquea el camino.", "A data crystal blocks the way.")); return; }
        if (tile == 'G') { RenderWorld(T("Un engranaje gigante gira en el muro.", "A giant gear spins in the wall.")); return; }
        save.x = nx; save.y = ny;
        walkFrame++;
        if ((IsDungeon(save.area) || (save.area == "studytower" && save.studyFloor >= 1)) && DungeonStep()) return;
        RenderWorld();
    }

    // --- Estructura anidada AB-410: ciudad -> 4 hubs (p1..p4) -> 17 mazmorras (p#_md#). ---
    // Las áreas se describen en datos: una mazmorra lleva "zi" (índice de zona) y "part"
    // (1..4); un hub lleva "hub":true. Esto evita codificar d1/d2/d3 en el motor.
    Dictionary<string, object> AreaDict(string area)
    {
        if (area == null) return null;
        var areas = D(data["areas"]);
        return areas.ContainsKey(area) ? areas[area] as Dictionary<string, object> : null;
    }

    bool IsDungeon(string area)
    {
        var a = AreaDict(area);
        return a != null && a.ContainsKey("zi");
    }

    bool IsHub(string area)
    {
        var a = AreaDict(area);
        return a != null && a.ContainsKey("hub");
    }

    // Parte (1..4) de una mazmorra o hub.
    int PartOf(string area)
    {
        var a = AreaDict(area);
        if (a != null && a.ContainsKey("part")) return I(a["part"]);
        if (area != null && area.Length >= 2 && area[0] == 'p' && char.IsDigit(area[1])) return area[1] - '0';
        return 1;
    }

    // ---- Configuración del examen (bloque "meta" de los datos; ver build_ab410_data.py). ----
    // Permite adaptar el juego a otra certificación sin tocar el motor: nº de módulos, mapeo
    // módulo->parte y nombre del examen salen de los datos, con fallback a los valores AB-410.
    Dictionary<string, object> Meta() => data != null && data.ContainsKey("meta") ? data["meta"] as Dictionary<string, object> : null;
    int MetaInt(string k, int dflt) { var m = Meta(); return m != null && m.ContainsKey(k) ? I(m[k]) : dflt; }
    string MetaStr(string k, string dflt) { var m = Meta(); return m != null && m.ContainsKey(k) ? S(m[k]) : dflt; }
    bool MetaBool(string k, bool dflt) { var m = Meta(); return m != null && m.ContainsKey(k) ? Convert.ToBoolean(m[k], CultureInfo.InvariantCulture) : dflt; }
    // Torres de estudio (Gran Sabio por módulo + Torre del Estudio de 100 pisos). PL-400 las
    // desactiva por ahora (meta.studyTower=false); se diseñarán después. AB-410 no trae el flag → true.
    bool StudyTowerEnabled() => MetaBool("studyTower", true);
    string ExamShort() => MetaStr("examNameShort", "AB-410");
    string ExamName() => MetaStr("examName", "Power Platform Saga AB-410");

    // Parte a la que pertenece un número de módulo/dominio. Lee meta.parts; fallback heredado.
    int PartOfDom(int dom)
    {
        var m = Meta();
        if (m != null && m.ContainsKey("parts"))
            foreach (var po in L(m["parts"]))
            {
                var pd = po as Dictionary<string, object>;
                if (pd == null || !pd.ContainsKey("modules")) continue;
                foreach (var d in L(pd["modules"])) if (I(d) == dom) return I(pd["part"]);
            }
        if (dom <= 3) return 1;
        if (dom <= 6) return 2;
        if (dom <= 13) return 3;
        return 4;
    }

    // ¿Todas las mazmorras de una parte están limpiadas?
    bool PartComplete(int part)
    {
        foreach (var kv in D(data["areas"]))
        {
            var a = kv.Value as Dictionary<string, object>;
            if (a != null && a.ContainsKey("zi") && a.ContainsKey("part") && I(a["part"]) == part && !save.cleared.Contains(kv.Key))
                return false;
        }
        return true;
    }

    bool AllPartsComplete()
    {
        for (int p = 1; p <= 4; p++) if (!PartComplete(p)) return false;
        return true;
    }

    string TrackForArea(string area)
    {
        if (area == "farm") return "tome";   // la Granja comparte la pista de lectura serena de los tomos
        if (area == "town" || IsHub(area)) return "town";
        if (area == "tower") return "tower";   // METAL desafiante del recorrido de la Torre del Gran Sabio
        if (area == "studytower") return "studytower";   // ROCK heroico de la Torre del Estudio
        // Torre del EXAMEN: la melodía del juego (himno del título) vuelve épica; aquí se juntan
        // los mundos. Mismo tema para el overworld y para todos los combates (ver BattleTrack).
        if (area == "final") return "exam";
        // Cada mundo tiene su propio overworld (1-3 reutilizan d1/d2/d3; 4-6 tienen w4/w5/w6).
        if (IsDungeon(area)) return WorldOverworldTrack(PartOf(area));
        return "town";
    }

    // Overworld propio de cada mundo (parte 1..6).
    string WorldOverworldTrack(int pt)
    {
        switch (pt)
        {
            case 1: return "d1"; case 2: return "d2"; case 3: return "d3";
            case 4: return "w4"; case 5: return "w5"; default: return "w6";
        }
    }

    // Combate propio de cada mundo (parte 1..6); bat1 = pista de combate estándar.
    string WorldBattleTrack(int pt)
    {
        switch (pt)
        {
            case 1: return "bat1"; case 2: return "bat2"; case 3: return "bat3";
            case 4: return "bat4"; case 5: return "bat5"; default: return "bat6";
        }
    }

    // Pista para un combate según el contexto. En la Torre del Examen (guardianes + Rey Demonio)
    // suena la melodía ÉPICA del juego; los jefes de mazmorra conservan su tema; el resto usa
    // el combate PROPIO del mundo donde ocurre la pelea.
    string BattleTrack(bool boss)
    {
        if (battle != null && battle.tower) return "studybattle";
        if (battle != null && battle.thingKey == "gransabio") return "gransabio";
        if (save != null && save.area == "final") return "exam";
        if (boss || (battle != null && battle.thingKey == "sabioguard")) return "boss";
        int pt = (save != null && IsDungeon(save.area)) ? PartOf(save.area) : 1;
        return WorldBattleTrack(pt);
    }

    // Sistema de persecución: a los 5 pasos (al entrar a la mazmorra o tras un combate)
    // aparecen los 3 enemigos de la zona en el mapa y persiguen al jugador con 1 paso
    // por cada paso suyo, atravesando muros y atrezzo. Al tocarlo, combate.
    // Devuelve true si ya se renderizó (combate o aviso de aparición).
    bool DungeonStep()
    {
        if (save.area == "studytower") return TowerDungeonStep();
        // Tras leer TODOS los tomos (jefe desbloqueado y aún vivo): solo te persigue el JEFE,
        // a 2 pasos POR EL LABERINTO (BFS, sin cruzar muros). No se puede esquivar: te obliga a enfrentarlo.
        if (BossChaseActive())
        {
            chasers.Clear();
            if (bossChaser == null) { SpawnBossChaser(); return true; }
            if (bossChaser[0] == save.x && bossChaser[1] == save.y) { StartDungeonBoss(); return true; }
            ChaseAlongPath(bossChaser, 2);
            if (bossChaser[0] == save.x && bossChaser[1] == save.y) { StartDungeonBoss(); return true; }
            return false;
        }
        bossChaser = null;
        if (save.noEnemyZones != null && save.noEnemyZones.Contains(save.area)) { chasers.Clear(); return false; }   // zona pacificada
        foreach (var c in chasers)
            if (c[0] == save.x && c[1] == save.y) { StartChaserBattle(c[2]); return true; }   // el jugador lo pisó
        if (chasers.Count > 0)
        {
            TowerChaseMove();   // BFS por el laberinto: los perseguidores NO atraviesan muros
            foreach (var c in chasers)
                if (c[0] == save.x && c[1] == save.y) { StartChaserBattle(c[2]); return true; }
            return false;
        }
        stepsSinceBattle++;
        if (stepsSinceBattle >= 5) { SpawnChasers(); return true; }
        return false;
    }

    // Rellena los 3 perseguidores SIN renderizar (para poblar un piso de torre al generarlo).
    // Persecución de la TORRE DEL ESTUDIO: los enemigos NO atraviesan muros. Persiguen al jugador
    // por el camino válido (1 paso por paso del jugador) usando un mapa de distancias BFS desde el
    // jugador sobre las casillas de suelo. Si llegas a la escalera, subes de piso.
    bool TowerDungeonStep()
    {
        // ¿El jugador pisó a un perseguidor?
        foreach (var c in chasers) if (c[0] == save.x && c[1] == save.y) { StartChaserBattle(c[2]); return true; }
        if (chasers.Count > 0)
        {
            TowerChaseMove();
            foreach (var c in chasers) if (c[0] == save.x && c[1] == save.y) { StartChaserBattle(c[2]); return true; }
            return false;
        }
        // Sin perseguidores (tras un combate): breve tregua y vuelven 3 nuevos.
        stepsSinceBattle++;
        if (stepsSinceBattle >= 3) { SpawnChasers(); return true; }
        return false;
    }

    // BFS de distancias desde el jugador sobre el suelo ('.'); cada perseguidor avanza a la casilla
    // vecina con menor distancia (persecución por el laberinto, sin cruzar muros).
    void TowerChaseMove()
    {
        var layout = Layout();
        int H = layout.Count, W = H > 0 ? S(layout[0]).Length : 0;
        bool Floor(int x, int y) => y >= 0 && y < H && x >= 0 && x < S(layout[y]).Length && S(layout[y])[x] == '.';
        var dist = new int[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) dist[x, y] = int.MaxValue;
        var q = new Queue<int[]>();
        if (Floor(save.x, save.y)) { dist[save.x, save.y] = 0; q.Enqueue(new[] { save.x, save.y }); }
        int[][] dirs = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var d in dirs)
            {
                int nx = cur[0] + d[0], ny = cur[1] + d[1];
                if (Floor(nx, ny) && dist[nx, ny] == int.MaxValue) { dist[nx, ny] = dist[cur[0], cur[1]] + 1; q.Enqueue(new[] { nx, ny }); }
            }
        }
        foreach (var c in chasers)
        {
            if (c[0] == save.x && c[1] == save.y) continue;
            int best = dist[c[0], c[1]];
            int bx = c[0], by = c[1];
            foreach (var d in dirs)
            {
                int nx = c[0] + d[0], ny = c[1] + d[1];
                if (Floor(nx, ny) && dist[nx, ny] < best) { best = dist[nx, ny]; bx = nx; by = ny; }
            }
            c[0] = bx; c[1] = by;
        }
    }

    // Mueve UNA entidad {x,y} hasta `steps` casillas hacia el jugador por el laberinto (BFS sobre
    // suelo, sin cruzar muros). Se detiene si alcanza al jugador o si queda bloqueada. Lo usa el
    // JEFE perseguidor de mazmorra (misma lógica que los perseguidores de la Torre del Estudio).
    void ChaseAlongPath(int[] pos, int steps)
    {
        var layout = Layout();
        int H = layout.Count, W = H > 0 ? S(layout[0]).Length : 0;
        if (W == 0) return;
        bool Floor(int x, int y) => y >= 0 && y < H && x >= 0 && x < S(layout[y]).Length && S(layout[y])[x] == '.';
        var dist = new int[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) dist[x, y] = int.MaxValue;
        var q = new Queue<int[]>();
        if (Floor(save.x, save.y)) { dist[save.x, save.y] = 0; q.Enqueue(new[] { save.x, save.y }); }
        int[][] dirs = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var d in dirs)
            {
                int nx = cur[0] + d[0], ny = cur[1] + d[1];
                if (Floor(nx, ny) && dist[nx, ny] == int.MaxValue) { dist[nx, ny] = dist[cur[0], cur[1]] + 1; q.Enqueue(new[] { nx, ny }); }
            }
        }
        for (int s = 0; s < steps; s++)
        {
            if (pos[0] == save.x && pos[1] == save.y) return;
            if (pos[0] < 0 || pos[0] >= W || pos[1] < 0 || pos[1] >= H) return;
            int best = dist[pos[0], pos[1]], bx = pos[0], by = pos[1];
            foreach (var d in dirs)
            {
                int nx = pos[0] + d[0], ny = pos[1] + d[1];
                if (Floor(nx, ny) && dist[nx, ny] < best) { best = dist[nx, ny]; bx = nx; by = ny; }
            }
            if (bx == pos[0] && by == pos[1]) return;   // bloqueado (sin camino más corto contiguo)
            pos[0] = bx; pos[1] = by;
        }
    }

    void FillChasers()
    {
        chasers.Clear();
        bool tower = save.area == "studytower";
        int kinds = tower ? TowerEnemyRefs().Count : L((L(data["zones"])[ZoneIndexForArea(save.area)] as Dictionary<string, object>)["enemies"]).Count;
        var layout = Layout();
        var spots = new List<int[]>();
        for (int y = 0; y < layout.Count; y++)
        {
            var row = S(layout[y]);
            for (int x = 0; x < row.Length; x++)
                if (row[x] == '.' && Math.Abs(x - save.x) + Math.Abs(y - save.y) >= 6 && ThingAt(x, y) == null)
                    spots.Add(new[] { x, y });
        }
        for (int i = 0; i < 3 && kinds > 0 && spots.Count > 0; i++)
        {
            var s = spots[rng.Next(spots.Count)];
            spots.Remove(s);
            // Torre del Estudio: cada perseguidor es un enemigo AL AZAR de cualquier zona.
            chasers.Add(new[] { s[0], s[1], tower ? rng.Next(kinds) : i % kinds });
        }
    }

    void SpawnChasers()
    {
        FillChasers();
        bool tower = save.area == "studytower";
        RenderWorld(chasers.Count > 0
            ? (tower
               ? T("¡" + chasers.Count + " monstruos del piso te persiguen por los pasillos!", chasers.Count + " floor monsters chase you through the corridors!")
               : T("¡Te han olido! " + chasers.Count + " monstruos te persiguen por los pasillos...", "They caught your scent! " + chasers.Count + " monsters are hunting you through the corridors..."))
            : "");
    }

    int[] ChaserAt(int x, int y)
    {
        foreach (var c in chasers) if (c[0] == x && c[1] == y) return c;
        return null;
    }

    // ¿Modo "el jefe te persigue"? Tras leer todos los tomos y mientras el jefe siga vivo.
    bool BossChaseActive()
    {
        return IsDungeon(save.area) && save.cleared != null && !save.cleared.Contains(save.area)
            && TotalTomesInArea() > 0 && ReadTomesInArea() == TotalTomesInArea();
    }

    void SpawnBossChaser()
    {
        // El jefe que persigue es EL MISMO de la esquina: arranca desde su celda y se mueve hacia ti.
        int[] pos = null;
        foreach (var t in Things(save.area))
            if (S(t, "kind") == "boss") { pos = new[] { I(t["x"]), I(t["y"]) }; break; }
        if (pos == null)
        {
            // Respaldo: celda de suelo lejana (no debería ocurrir si el jefe sigue vivo).
            var layout = Layout();
            var spots = new List<int[]>();
            for (int y = 0; y < layout.Count; y++)
            {
                var row = S(layout[y]);
                for (int x = 0; x < row.Length; x++)
                    if (row[x] == '.' && Math.Abs(x - save.x) + Math.Abs(y - save.y) >= 6) spots.Add(new[] { x, y });
            }
            pos = spots.Count > 0 ? spots[rng.Next(spots.Count)] : new[] { 1, 1 };
        }
        bossChaser = pos;
        RenderWorld(T("¡El jefe de la esquina despierta y te persigue (atraviesa muros y va a 2 pasos)! No podrás huir...",
                      "The corner boss awakens and hunts you (phases walls, 2 steps)! You can't run..."));
    }

    void StartDungeonBoss()
    {
        foreach (var t in Things(save.area))
            if (S(t, "kind") == "boss") { bossChaser = null; StartBattle(t, true); return; }
        bossChaser = null;
    }

    // Intento de salir de la mazmorra: si el jefe acecha (tomos leídos, jefe vivo), corta la huida.
    void TryLeaveDungeon(Action leave)
    {
        if (BossChaseActive())
        {
            if (bossChaser == null) SpawnBossChaser();
            else RenderWorld(T("¡El jefe te corta la huida! Debes enfrentarlo.", "The boss cuts off your escape! You must face it."));
            return;
        }
        leave();
    }

    Dictionary<string, object> ZoneEnemy(int ei)
    {
        var zone = L(data["zones"])[ZoneIndexForArea(save.area)] as Dictionary<string, object>;
        var enemies = L(zone["enemies"]);
        return enemies[ei % enemies.Count] as Dictionary<string, object>;
    }

    // HP de enemigo escalada con el nivel del jugador: ×(1 + 0.10·LV). LV10 ≈ ×2, LV20 ≈ ×3.
    // (Solo enemigos normales; los jefes conservan su HP de datos.) Así un x3 tiene sentido.
    int ScaledEnemyHp(int baseHp) => Mathf.Max(baseHp, Mathf.RoundToInt(baseHp * (1f + 0.10f * save.lv)));

    void StartChaserBattle(int ei)
    {
        if (save.area == "studytower") { StartStudyChaserBattle(ei); return; }
        var zone = L(data["zones"])[ZoneIndexForArea(save.area)] as Dictionary<string, object>;
        var enemy = ZoneEnemy(ei);
        int hp = ScaledEnemyHp(I(enemy["hp"]));
        battle = new Battle { thingKey = "rnd", enemy = enemy, hp = hp, maxhp = hp, dom = I(zone["dom"]), spr = SpriteForEnemyName(S(enemy, "n")) };
        StartCoroutine(RandomEncounterIntro(T("¡Un " + TS(enemy, "n") + " te ha atrapado!", "A " + TS(enemy, "n") + " caught you!")));
    }

    // ---- Torre del Estudio: enemigos de cualquier zona, escalados por piso ----
    // Lista plana de TODOS los enemigos del juego (cualquier tipo) como refs {zoneIdx, enemyIdx}.
    List<int[]> TowerEnemyRefs()
    {
        if (towerEnemyRefs == null)
        {
            towerEnemyRefs = new List<int[]>();
            var zones = L(data["zones"]);
            for (int z = 0; z < zones.Count; z++)
            {
                var zone = zones[z] as Dictionary<string, object>;
                if (zone == null || !zone.ContainsKey("enemies")) continue;
                var en = L(zone["enemies"]);
                for (int e = 0; e < en.Count; e++) towerEnemyRefs.Add(new[] { z, e });
            }
        }
        return towerEnemyRefs;
    }

    // Copia del enemigo con HP/ATK/XP escalados por el piso (más fuerte cuanto más subes).
    Dictionary<string, object> ScaleStudyEnemy(Dictionary<string, object> e, int floor)
    {
        var c = new Dictionary<string, object>(e);
        c["hp"] = Mathf.RoundToInt(I(e["hp"]) * (1f + 0.06f * floor));   // piso 50 ≈ ×4, piso 99 ≈ ×7
        c["atk"] = Mathf.RoundToInt(Math.Max(3, I(e["atk"])) * (1f + 0.04f * floor));
        c["xp"] = Mathf.RoundToInt(Math.Max(1, I(e["xp"])) * (1f + 0.05f * floor));
        return c;
    }

    // Daño de la Torre del Estudio: PORCENTUAL a la vida MÁXIMA del prota, creciente por piso,
    // pero NUNCA 100% (tope 90%): así un golpe nunca mata de lleno y te centras en sobrevivir.
    float StudyDmgPct(int floor) => Mathf.Min(0.90f, 0.06f + 0.0086f * floor);

    // Daño que recibe el jugador al fallar, según el combate:
    // · Torre del Estudio: % de la vida máxima por piso (tope 90%).
    // · Jefe FINAL (Rey Demonio, dom 0): 20% de la vida máxima.
    // · Resto: el atk del enemigo (datos).
    int EnemyHitDamage()
    {
        if (battle.tower) return Mathf.Clamp(Mathf.RoundToInt(save.maxhp * StudyDmgPct(save.studyFloor)), 4, Mathf.RoundToInt(save.maxhp * 0.90f));
        if (battle.dom == 0 && battle.thingKey.Contains("|boss|")) return Mathf.Max(8, Mathf.RoundToInt(save.maxhp * 0.20f));
        return Math.Max(3, I(battle.enemy["atk"]));
    }

    void StartStudyChaserBattle(int refIdx)
    {
        var refs = TowerEnemyRefs();
        if (refs.Count == 0) { RenderWorld(""); return; }
        var r = refs[refIdx % refs.Count];
        var zone = L(data["zones"])[r[0]] as Dictionary<string, object>;
        var baseEnemy = L(zone["enemies"])[r[1]] as Dictionary<string, object>;
        var enemy = ScaleStudyEnemy(baseEnemy, save.studyFloor);
        int hp = I(enemy["hp"]);
        battle = new Battle { thingKey = "rnd", enemy = enemy, hp = hp, maxhp = hp, dom = I(zone["dom"]), spr = SpriteForEnemyName(S(baseEnemy, "n")), tower = true };
        StartCoroutine(RandomEncounterIntro(T("¡Un " + TS(enemy, "n") + " del piso " + save.studyFloor + " te embiste!", "A floor-" + save.studyFloor + " " + TS(enemy, "n") + " charges you!")));
    }

    void MarkCorrect(string key)
    {
        if (!string.IsNullOrEmpty(key) && !save.correctQ.Contains(key)) save.correctQ.Add(key);
    }

    // Preguntas (de combate) dominadas: respondidas bien al menos una vez, sobre el total.
    int MasteredCount()
    {
        int c = 0;
        foreach (var k in save.correctQ) if (k.StartsWith("bq:")) c++;
        return c;
    }

    // Progreso de un dominio concreto (las preguntas que tiene esa mazmorra).
    void DomainProgress(string area, out int mastered, out int total)
    {
        int dn = DomNum(area);
        var all = L(data["bq"]);
        mastered = 0; total = 0;
        for (int i = 0; i < all.Count; i++)
            if (I((all[i] as Dictionary<string, object>)["d"]) == dn) { total++; if (save.correctQ.Contains("bq:" + i)) mastered++; }
    }

    // ¿Se han respondido bien TODAS las preguntas del dominio de esta zona?
    bool ZoneAllMastered(string area)
    {
        if (!IsDungeon(area)) return false;
        int dn = DomNum(area);
        var all = L(data["bq"]);
        for (int i = 0; i < all.Count; i++)
            if (I((all[i] as Dictionary<string, object>)["d"]) == dn && !save.correctQ.Contains("bq:" + i)) return false;
        return true;
    }

    // Emboscada al abrir un tomo/cofre en mazmorra (40 %): un monstruo de la zona lo
    // defiende. Al vencerlo, el botín se abre solo (ver Victory).
    bool TryAmbush(Dictionary<string, object> thing, bool eligible)
    {
        if (!eligible || !IsDungeon(save.area)) return false;
        if (BossChaseActive()) return false;   // en modo "el jefe te persigue" no hay emboscadas normales
        if (save.noEnemyZones != null && save.noEnemyZones.Contains(save.area)) return false;   // zona pacificada
        if (rng.NextDouble() >= 0.4) return false;
        pendingLoot = thing;
        int zi = ZoneIndexForArea(save.area);
        var zone = L(data["zones"])[zi] as Dictionary<string, object>;
        var enemies = L(zone["enemies"]);
        var enemy = enemies[rng.Next(enemies.Count)] as Dictionary<string, object>;
        battle = new Battle
        {
            thingKey = "defend", enemy = enemy, hp = I(enemy["hp"]), maxhp = I(enemy["hp"]),
            dom = I(zone["dom"]), spr = SpriteForEnemyName(S(enemy, "n"))
        };
        string what = S(thing, "kind") == "chest" ? T("el cofre", "the chest") : T("el tomo", "the tome");
        StartCoroutine(RandomEncounterIntro(T("¡Un " + TS(enemy, "n") + " está defendiendo " + what + "!", "A " + TS(enemy, "n") + " is defending " + what + "!")));
        return true;
    }

    int ZoneIndexForArea(string area)
    {
        var a = AreaDict(area);
        if (a != null && a.ContainsKey("zi")) return I(a["zi"]);
        return 0;
    }

    void Action()
    {
        var thing = ThingHereOrAhead();
        if (thing == null) { RenderWorld(T("No hay nada para interactuar.", "There is nothing to interact with.")); return; }
        var kind = S(thing, "kind");
        if (kind == "portal") EnterPortal(S(thing, "to"));
        else if (kind == "exit") TryLeaveDungeon(ExitDungeon);
        else if (kind == "carriage") CarriageTeleport();
        else if (kind == "inn") { save.hp = save.maxhp; save.mp = save.maxmp; SaveSlot(); RenderWorld(T("Descansaste en la posada.", "You rested at the inn.")); }
        else if (kind == "npc") StartDialog(S(thing, "who"));
        else if (kind == "tome")
        {
            if (IsDungeon(save.area))
            {
                // Cada token = UN tomo, en orden. Un token ya leído no vuelve a abrir tomos.
                string tile = ThingKey(thing);
                if (save.readTomeTiles != null && save.readTomeTiles.Contains(tile))
                { RenderWorld(T("Ya estudiaste este tomo.", "You already studied this tome.")); return; }
                var ordered = AreaTomeIdsOrdered(save.area);
                string openId = ordered.Count > 0 ? ordered[Math.Min(ReadTomesInArea(), ordered.Count - 1)] : S(thing, "id");
                pendingTomeTile = tile;
                if (!TryAmbush(thing, true)) StartTome(openId);
            }
            else
            {
                if (!TryAmbush(thing, !save.readTomes.Contains(S(thing, "id")))) StartTome(S(thing, "id"));
            }
        }
        else if (kind == "supertome") StartSuperTome(S(thing, "id"));
        else if (kind == "sage") RenderSageIntro(S(thing, "dom"));
        else if (kind == "towerentrance") EnterTower();
        else if (kind == "studytower") EnterStudyTower();
        else if (kind == "studynpc") { if (save.studyFloor < 1) RenderStudyTowerIntro(""); else RenderStudyShop(); }
        else if (kind == "studyup") StudyClimb();
        else if (kind == "studyexit") ExitStudyTower();
        else if (kind == "studysage") RenderStudyTowerSageIntro();
        else if (kind == "studybible") RenderBibleIndex();
        else if (kind == "towergem") RenderTowerGemIntro(I(thing["module"]));
        else if (kind == "towerelevator") RenderElevator();
        else if (kind == "towerexit") ExitTower();
        else if (kind == "dashboard") RenderDashboard();
        else if (kind == "examprep") RenderExamIntro();
        else if (kind == "langmage") RenderLangMage();
        else if (kind == "guardian") RenderGuardianIntro(thing);
        else if (kind == "enemy" || kind == "boss") StartBattle(thing, kind == "boss");
        else if (kind == "rematch") RenderRematchOffer(thing);
        else if (kind == "denizen") RenderDenizen(S(thing, "name"));
        else if (kind == "reviewboss") StartFarmPartReview(I(thing["part"]));
        else if (kind == "farmkeeper") RenderFarmKeeper();
        else if (kind == "hubguide") RenderHubGuide(I(thing["part"]));
        else if (kind == "hubsage") RenderHubSage(I(thing["part"]));
        else if (kind == "contoso") RenderContosoIntro(I(thing["part"]));
        else if (kind == "finalcase") RenderContosoIntro(0, SpriteForEnemyName(S(thing, "name")));
        else if (kind == "chest") { if (save.area == "studytower" || !TryAmbush(thing, true)) OpenChest(thing); }
    }

    void RenderRematchOffer(Dictionary<string, object> thing)
    {
        int zi = I(thing["zi"]);
        var zone = L(data["zones"])[zi] as Dictionary<string, object>;
        var bossd = D(zone["boss"]);
        string name = S(bossd, "n");
        screen = "dialog";
        ClearRoot();
        AddBackdrop(BackdropForArea("town"), 0.4f);
        var p = Panel(root, "Rematch", new Color(.05f, .1f, .08f, .94f), Anchor.Stretch, new Vector2(26, 24), new Vector2(-26, -24));
        var col = ScrollColumn(p, 14, 26);
        BigSprite(col, SpriteForEnemyName(name), 132, new Color(.45f, 1f, .6f));
        Label(col, TS(bossd, "n") + T(" (ahora aliado)", " (now an ally)"), 24, new Color(.55f, 1f, .7f), FontStyle.Bold);
        Label(col, T("¡Eh, viejo rival! Ya no soy tu enemigo. Puedo darte un combate amistoso... o entregarte todo lo que sé para tu examen.", "Hey, old rival! I'm no longer your enemy. I can give you a friendly fight... or hand you everything I know for your exam."), 21, Color.white, FontStyle.Normal);
        Button(col, T("¡SÍ, POR LOS VIEJOS TIEMPOS!", "YES, FOR OLD TIMES' SAKE!"), () => StartRematchBattle(zi), new Color(.95f, .72f, .12f));
        Button(col, T("TE ENTREGARÉ MIS CONOCIMIENTOS", "I WILL SHARE MY KNOWLEDGE"), () => RenderBossWisdom(zi, () => RenderRematchOffer(thing)), new Color(.2f, .55f, .85f));
        Button(col, T("Ahora no", "Not now"), () => RenderWorld(""), new Color(.2f, .3f, .5f));
    }

    void StartRematchBattle(int zi)
    {
        var zone = L(data["zones"])[zi] as Dictionary<string, object>;
        var enemy = D(zone["boss"]);
        int hp = Math.Max(40, I(enemy["hp"]) / 2);   // revancha más ágil
        battle = new Battle
        {
            thingKey = "rematch", enemy = enemy, hp = hp, maxhp = hp,
            dom = I(zone["dom"]), spr = SpriteForEnemyName(S(enemy, "n"))
        };
        RenderBattle(T("Combate amistoso con " + TS(enemy, "n") + ". ¡Da lo mejor!", "Friendly battle with " + TS(enemy, "n") + ". Give it your best!"));
    }

    // El jefe-aliado te entrega un resumen de conceptos de su dominio (repaso de examen).
    void RenderBossWisdom(int zi, Action back)
    {
        screen = "wisdom";
        wisdomZi = zi; wisdomBack = back;
        ClearRoot();
        PlayTrack("sage");
        var zone = L(data["zones"])[zi] as Dictionary<string, object>;
        int dom = I(zone["dom"]);
        string name = S(D(zone["boss"]), "n");
        AddBackdrop(BackdropForArea(dom == 0 ? "final" : ("d" + dom)), 0.35f);
        var p = Panel(root, "Wisdom", new Color(.04f, .06f, .12f, .96f), Anchor.Stretch, new Vector2(22, 16), new Vector2(-22, -16));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.45f, .9f, 1f, .55f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 24);
        BigSprite(col, SpriteForEnemyName(name), 110, new Color(.45f, .9f, 1f));
        Label(col, T("Conocimientos de ", "Knowledge of ") + name, 25, new Color(.45f, .9f, 1f), FontStyle.Bold);
        Label(col, T("Resumen para el examen AB-900. Toca un término para ver su definición.", "AB-900 exam summary. Tap a term to see its definition."), 15, new Color(.85f, .9f, 1f), FontStyle.Normal);

        var cards = BossWisdom(dom);
        string allText = "";
        foreach (var c in cards)
        {
            var t = Label(col, "▸ " + c[0], 19, new Color(1f, .82f, .22f), FontStyle.Bold); t.alignment = TextAnchor.UpperLeft;
            var body = Label(col, HighlightTerms(c[1]), 17, Color.white, FontStyle.Normal);
            body.alignment = TextAnchor.UpperLeft; body.supportRichText = true;
            allText += " " + c[1];
        }

        var terms = TermsInText(allText);
        if (terms.Count > 0)
        {
            Label(col, T("Términos para tocar", "Tap a term"), 17, new Color(.55f, 1f, .75f), FontStyle.Bold);
            var chips = Panel(col, "WTermChips", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
            var le = chips.gameObject.AddComponent<LayoutElement>(); le.minHeight = 86; le.preferredHeight = 86; le.flexibleWidth = 1;
            Horizontal(chips, 8, TextAnchor.MiddleCenter);
            foreach (var term in terms)
            {
                string captured = term;
                Button(chips, captured, () => { termOrigin = "wisdom"; RenderTermDefinition(captured); }, TermColor(captured), 190);
            }
        }

        Button(col, T("GRACIAS, MAESTRO", "THANK YOU, MASTER"), () => { if (wisdomBack != null) wisdomBack(); else RenderWorld(""); }, new Color(.95f, .72f, .12f));
    }

    // Tarjetas de repaso por dominio (resumen del temario AB-900). [titulo, cuerpo]
    List<string[]> BossWisdom(int dom)
    {
        if (dom == 1) return new List<string[]>
        {
            new[]{"Confianza cero (Zero Trust)","Es una ESTRATEGIA, no un producto: verificar explícitamente, usar privilegios mínimos y asumir la vulneración. No requiere Azure ni se activa con un interruptor."},
            new[]{"Autenticación vs Autorización","Autenticar = probar quién eres. Autorización = decidir a qué recursos puede acceder una identidad ya autenticada."},
            new[]{"MFA y passwordless","MFA añade un segundo factor. Authenticator usa number matching. Sin contraseña: Windows Hello y FIDO2, resistentes al phishing."},
            new[]{"Acceso condicional","Motor si-entonces de Entra: decide el acceso según identidad, riesgo, dispositivo, ubicación y app. Permite controles de sesión (solo lectura, sin descargas)."},
            new[]{"Entra ID Protection","Detecta riesgo de inicio de sesión y de usuario (p. ej. viaje imposible) y puede bloquear, exigir MFA o forzar cambio de contraseña."},
            new[]{"PIM (acceso privilegiado)","Roles ELEGIBLES que debes ACTIVAR (just-in-time), con límite de tiempo, aprobación y auditoría. Un rol elegible no da permisos hasta activarlo."},
            new[]{"RBAC y privilegio mínimo","Permisos por rol (p. ej. Administrador de contraseñas no lee buzones). Aplica separación de tareas."},
            new[]{"Familia Defender","Defender XDR coordina endpoints, identidades, correo y apps. for Office 365 = phishing y malware. for Identity = Active Directory. for Endpoint = dispositivos. for Cloud Apps = SaaS / shadow IT."},
            new[]{"Identity Secure Score","Sube con recomendaciones: más de un Administrador Global, no expirar contraseñas y exigir MFA a los roles de administrador."},
            new[]{"Roles y app registration","Lector Global = solo lectura de todo el inquilino. Para que una app o servicio externo se autentique en Entra, crea un registro de aplicación (app registration)."},
            new[]{"Servicios core","Licencias: directas o basadas en grupo (los miembros heredan), desde el centro de Microsoft 365. Dominio personalizado: verifícalo con un registro DNS (TXT). Exchange admin center: reglas de flujo de correo y buzones compartidos."},
        };
        if (dom == 2) return new List<string[]>
        {
            new[]{"Microsoft Purview","Solución unificada de gobernanza, protección de la información y cumplimiento: etiquetas, DLP, retención, eDiscovery y riesgos internos."},
            new[]{"Information Protection","Descubre, clasifica, etiqueta y protege datos sensibles en muchas plataformas. Incluye clasificadores entrenables (machine learning)."},
            new[]{"Etiquetas de confidencialidad","Clasifican y protegen (cifrado, marcas) y la protección PERSISTE. Hay que PUBLICARLAS con una directiva para que los usuarios las apliquen. Las de contenedor van a sitios, grupos y Teams."},
            new[]{"DLP (prevención de pérdida de datos)","Impide compartir datos sensibles (PII, financieros) con externos en Exchange, SharePoint, OneDrive y Teams. Puede bloquear, advertir o pedir justificación."},
            new[]{"Data Lifecycle Management","Retención: retener, eliminar, o retener y luego eliminar (p. ej. conservar correos 7 años y borrarlos). NO lo hace Communication Compliance."},
            new[]{"Insider Risk Management","Detecta comportamiento interno riesgoso, como un usuario que se va y exfiltra datos (descargas, copia a almacenamiento personal)."},
            new[]{"Communication Compliance","Detecta texto inapropiado en Teams, correo y prompts de Copilot. Usa OCR en imágenes. No añade descargos ni retiene."},
            new[]{"eDiscovery","Busca, conserva (hold) y EXPORTA copias del contenido para casos legales. (Audit solo da registros de actividad, no los archivos.)"},
            new[]{"DSPM for AI","Postura de seguridad de datos para IA: ve dónde se usa contenido sensible en Copilot y agentes y recomienda protecciones (etiquetas, DLP)."},
            new[]{"Gobernanza de datos","Data explorer: instantánea de elementos clasificados (SSN, tarjetas). Audit: registro unificado de acciones de usuarios y administradores. Data access governance (SharePoint): permisos y vínculos compartidos (oversharing)."},
        };
        if (dom == 3) return new List<string[]>
        {
            new[]{"Microsoft 365 Copilot","Orquestador que combina LLM con tus datos vía Microsoft Graph. Respeta tus permisos, no concede acceso nuevo y NO usa tus prompts ni datos para entrenar los modelos base."},
            new[]{"Microsoft Graph","Conecta correos, chats, reuniones, archivos y relaciones para anclar (grounding) y clasificar las respuestas de Copilot."},
            new[]{"Agentes y Agent Builder","Agentes orientados a tareas. Con Agent Builder defines instrucciones, fuentes de conocimiento y prompts iniciales, y anclas el agente a un sitio o web concretos."},
            new[]{"Copilot Studio","Para agentes personalizados o de motor propio, integrados con sistemas de línea de negocio."},
            new[]{"Researcher vs Analyst","Researcher: razonamiento multipaso sobre datos no estructurados. Analyst: análisis de datos y Excel, tendencias, anomalías y gráficos (usa el code interpreter / Python)."},
            new[]{"Conectores de Copilot","Traen datos externos de línea de negocio para usarlos como fuente de conocimiento de los agentes (se configuran en el centro de Microsoft 365)."},
            new[]{"Licencias de Copilot","Copilot requiere licencia complementaria y Researcher la requiere. Copilot Chat con web no necesita licencia extra. No se asigna Copilot a invitados de otros inquilinos."},
            new[]{"Pago por uso (pay-as-you-go)","Para agentes en Copilot Chat anclados en datos de trabajo. Una billing policy permite ver costos por departamento; el informe de créditos cubre a usuarios SIN licencia de Copilot."},
            new[]{"Oversharing y RSS","Restricted SharePoint Search y SharePoint Advanced Management limitan qué sitios usa Copilot mientras revisas permisos. Si Copilot muestra contenido de más, corrige los permisos del sitio."},
            new[]{"Controles e IA responsable","La generación de imágenes y la búsqueda web de Copilot se gestionan en el centro de Microsoft 365. La IA responsable tiene 6 principios; la responsabilidad implica supervisión humana."},
        };
        // Jefe final: recapitulación de los tres dominios para el examen.
        return new List<string[]>
        {
            new[]{"Los tres dominios","D1 Identidad y seguridad: Entra, MFA, Acceso condicional, PIM, Defender. D2 Purview y datos: etiquetas, DLP, retención, eDiscovery, Insider Risk. D3 Copilot y agentes: Graph, agentes, licencias, oversharing."},
            new[]{"Regla de oro de Copilot","Copilot respeta SIEMPRE los permisos existentes, no entrena con tus datos y se ancla en Microsoft Graph. La mayoría de 've demasiado' se arregla corrigiendo permisos del sitio."},
            new[]{"¿Qué portal usar?","Licencias y Copilot → centro de Microsoft 365. Identidad y acceso → Entra. Cumplimiento y datos → Purview. Amenazas y phishing → Defender. Uso compartido de SharePoint → centro de SharePoint."},
            new[]{"Verbos clave","Impedir compartir → DLP. Conservar o eliminar → Data Lifecycle Management. Exportar contenido → eDiscovery. Detectar exfiltración → Insider Risk. Texto ofensivo → Communication Compliance."},
            new[]{"Identidad esencial","Zero Trust es una estrategia. Rol elegible → ACTIVAR en PIM. App externa → app registration. Bloqueo por riesgo → ID Protection."},
            new[]{"Consejo final","Busca en el enunciado el VERBO y el resultado pedido, y elige el producto o portal exacto. ¡Mucha suerte en el AB-900!"},
        };
    }

    string PortalShort(string to)
    {
        switch (to)
        {
            case "town": return T("CIUDAD", "TOWN");
            case "final": return T("EXAMEN", "EXAM");
            default:
                // p#  -> "PARTE n";  p#_md#  -> "MDn"
                if (to.Length >= 2 && to[0] == 'p' && char.IsDigit(to[1]))
                {
                    int us = to.IndexOf('_');
                    if (us < 0) return T("PARTE ", "PART ") + to.Substring(1);
                    return to.Substring(us + 1).ToUpperInvariant();
                }
                return to.ToUpperInvariant();
        }
    }

    void ExitToTown()
    {
        save.area = "town"; save.x = 10; save.y = 10;
        chasers.Clear(); pendingLoot = null; combo = 0; towerDmgBonus = 0;
        SaveSlot();
        RenderWorld(T("Volviste a la ciudad.", "You returned to town."));
    }

    // Salir de una mazmorra: vuelve al hub de su parte (las mazmorras cuelgan del hub,
    // no de la ciudad). Desde la torre del examen vuelve a la ciudad.
    void ExitDungeon()
    {
        string to = IsDungeon(save.area) ? "p" + PartOf(save.area) : "town";
        var area = AreaDict(to);
        if (area == null || !area.ContainsKey("start")) { ExitToTown(); return; }
        save.area = to;
        var start = L(area["start"]);
        save.x = I(start[0]); save.y = I(start[1]);
        chasers.Clear(); pendingLoot = null; combo = 0; towerDmgBonus = 0;
        SaveSlot();
        RenderWorld(T("Saliste de la mazmorra.", "You left the dungeon."));
    }

    // Carruaje de la mazmorra: te TELETRANSPORTA junto a un tomo SIN LEER (la salida está
    // por el laberinto). Si ya leíste todos, te lleva a uno cualquiera.
    void CarriageTeleport()
    {
        var unread = new List<Dictionary<string, object>>();
        var all = new List<Dictionary<string, object>>();
        foreach (var t in Things(save.area))
        {
            if (S(t, "kind") != "tome" || Gone(t)) continue;
            all.Add(t);
            if (save.readTomeTiles == null || !save.readTomeTiles.Contains(ThingKey(t))) unread.Add(t);
        }
        var pool = unread.Count > 0 ? unread : all;
        if (pool.Count == 0) { RenderWorld(T("El carruaje no encuentra tomos a los que llevarte.", "The carriage finds no tomes to take you to.")); return; }
        var dest = pool[rng.Next(pool.Count)];
        save.x = I(dest["x"]); save.y = I(dest["y"]); save.fx = 0; save.fy = 1;
        chasers.Clear(); bossChaser = null; stepsSinceBattle = 0;   // respiro al llegar
        SaveSlot();
        string mride = unread.Count > 0
            ? T("El carruaje te deja junto a un TOMO SIN LEER. Pulsa A para estudiarlo.", "The carriage drops you by an UNREAD TOME. Press A to study it.")
            : T("Ya leíste todos los tomos: el carruaje te lleva a uno cualquiera.", "All tomes read: the carriage takes you to one anyway.");
        StartCoroutine(PlayCutIn("ride", () => RenderWorld(mride)));
    }

    void EnterPortal(string to)
    {
        if (to == "town") { TownReturnGate(save.area); return; }   // puede aparecer el jefe de repaso de la parte
        if (to == "final" && !AllPartsComplete())
        {
            RenderWorld(T("La Torre del Examen exige completar las 4 partes (todos los módulos).", "The Exam Tower demands all 4 parts (every module) complete."));
            return;
        }
        save.area = to;
        combo = 0; towerDmgBonus = 0;   // la racha no cruza de zona
        // limpia claves 'gone' antiguas de esta área para que las nuevas posiciones no queden ocultas
        save.goneThings.RemoveAll(k => k.StartsWith(to + "|"));
        if (IsDungeon(to))
        {
            GenerateDungeon(to);   // laberinto + posiciones aleatorias; fija save.x/y al inicio
        }
        else
        {
            var area = D(data["areas"])[to] as Dictionary<string, object>;
            var start = L(area["start"]);
            save.x = I(start[0]); save.y = I(start[1]);
        }
        SaveSlot();
        string mportal = T("Entraste a " + AreaName(to) + ".", "You entered " + AreaName(to) + ".") + (IsDungeon(to) ? T(" La mazmorra ha cambiado de forma...", " The dungeon has shifted its shape...") : "");
        StartCoroutine(PlayCutIn("portal", () => RenderWorld(mportal)));
    }

    // Genera un laberinto conectado y coloca tomos, jefe, cofres y salida al azar.
    // Nº de tomos definidos en los datos de un área (para dimensionar la mazmorra).
    int AreaTomeCount(string area)
    {
        int n = 0;
        var ad = D(D(data["areas"])[area]);
        if (ad != null && ad.ContainsKey("things"))
            foreach (var o in L(ad["things"]))
                if (S(o as Dictionary<string, object>, "kind") == "tome") n++;
        return n;
    }

    void GenerateDungeon(string area)
    {
        // El tamaño de la mazmorra escala con SUS tomos: pocas páginas = mapa pequeño (menos
        // caminata para llegar al tomo); muchas = mapa grande (hasta el 21x15 clásico). W/H impares
        // (los exige CarveMaze). 1 tomo -> 11x9 ... 6+ tomos -> 21x15.
        int nt = AreaTomeCount(area);
        int W = Mathf.Clamp(11 + 2 * (nt - 1), 11, 21);
        int H = Mathf.Clamp(9 + 2 * ((nt - 1) / 2), 9, 15);
        var g = CarveMaze(W, H);
        // Atrezzo temático: convierte algunas paredes pegadas al suelo en decoración
        // propia de cada mazmorra (antorchas d1 / cristales d2 / engranajes d3).
        char deco = "ACG"[(PartOf(area) - 1) % 3];   // atrezzo por parte (antorcha/cristal/engranaje)
        int props = Mathf.Clamp(nt + 3, 4, 12);   // menos atrezzo en mazmorras pequeñas
        for (int k = 0; k < 240 && props > 0; k++)
        {
            int dx = 1 + rng.Next(W - 2), dy = 1 + rng.Next(H - 2);
            if (g[dy][dx] != '#') continue;
            if (g[dy - 1][dx] != '.' && g[dy + 1][dx] != '.' && g[dy][dx - 1] != '.' && g[dy][dx + 1] != '.') continue;
            g[dy][dx] = deco; props--;
        }
        dunArea = area;
        dunLayout = new List<object>();
        for (int y = 0; y < H; y++) dunLayout.Add(new string(g[y]));

        int sx = 1, sy = 1;                 // (1,1) siempre es suelo en el laberinto
        save.x = sx; save.y = sy;
        stepsSinceBattle = 0;               // 5 pasos de gracia al entrar
        chasers.Clear(); bossChaser = null; // los perseguidores no cruzan de área

        // celdas de suelo
        var floors = new List<int[]>();
        for (int y = 1; y < H - 1; y++) for (int x = 1; x < W - 1; x++) if (g[y][x] == '.') floors.Add(new[] { x, y });

        // el jefe va en la celda más lejana al inicio
        int[] boss = new[] { sx, sy }; int best = -1;
        foreach (var f in floors)
        {
            if (f[0] == sx && f[1] == sy) continue;
            int dd = Mathf.Abs(f[0] - sx) + Mathf.Abs(f[1] - sy);
            if (dd > best) { best = dd; boss = f; }
        }

        // resto de celdas disponibles, barajadas
        var avail = new List<int[]>();
        foreach (var f in floors) if (!(f[0] == sx && f[1] == sy) && !(f[0] == boss[0] && f[1] == boss[1])) avail.Add(f);
        for (int i = avail.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = avail[i]; avail[i] = avail[j]; avail[j] = t; }

        dunThings = new List<Dictionary<string, object>>();
        int p = 0;
        Dictionary<string, object> Cell(int[] c, params object[] kv)
        {
            var dd = new Dictionary<string, object> { { "x", c[0] }, { "y", c[1] } };
            for (int i = 0; i + 1 < kv.Length; i += 2) dd[(string)kv[i]] = kv[i + 1];
            return dd;
        }

        // tomos definidos en los datos del área
        foreach (var o in L(D(D(data["areas"])[area])["things"]))
        {
            var t = o as Dictionary<string, object>;
            if (S(t, "kind") == "tome" && p < avail.Count)
                dunThings.Add(Cell(avail[p++], "kind", "tome", "id", S(t, "id")));
        }
        // jefe
        dunThings.Add(Cell(boss, "kind", "boss", "zi", ZoneIndexForArea(area)));
        // cofres (3 o 4)
        int chests = 3 + rng.Next(2);
        for (int i = 0; i < chests && p < avail.Count; i++) dunThings.Add(Cell(avail[p++], "kind", "chest"));
        // salida al hub de la parte
        if (p < avail.Count) dunThings.Add(Cell(avail[p++], "kind", "exit"));
        // carruaje: viaje rápido directo a la ciudad desde la mazmorra (siempre disponible)
        if (p < avail.Count) dunThings.Add(Cell(avail[p++], "kind", "carriage"));
    }

    // Laberinto perfecto (recursive backtracker) con algunas paredes abiertas para crear bucles/salas.
    char[][] CarveMaze(int W, int H)
    {
        var g = new char[H][];
        for (int y = 0; y < H; y++) { g[y] = new char[W]; for (int x = 0; x < W; x++) g[y][x] = '#'; }
        var stack = new Stack<int[]>();
        g[1][1] = '.'; stack.Push(new[] { 1, 1 });
        int[][] dirs = { new[] { 0, -2 }, new[] { 0, 2 }, new[] { -2, 0 }, new[] { 2, 0 } };
        while (stack.Count > 0)
        {
            var c = stack.Peek();
            var ds = new List<int[]>(dirs);
            for (int i = ds.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = ds[i]; ds[i] = ds[j]; ds[j] = t; }
            bool moved = false;
            foreach (var d in ds)
            {
                int nx = c[0] + d[0], ny = c[1] + d[1];
                if (nx > 0 && nx < W - 1 && ny > 0 && ny < H - 1 && g[ny][nx] == '#')
                {
                    g[ny][nx] = '.'; g[c[1] + d[1] / 2][c[0] + d[0] / 2] = '.';
                    stack.Push(new[] { nx, ny }); moved = true; break;
                }
            }
            if (!moved) stack.Pop();
        }
        // abre algunas paredes interiores que conecten dos suelos (más espacioso, menos laberíntico)
        int opens = (W * H) / 10;
        for (int k = 0; k < opens; k++)
        {
            int x = 1 + rng.Next(W - 2), y = 1 + rng.Next(H - 2);
            if (g[y][x] != '#') continue;
            if ((g[y - 1][x] == '.' && g[y + 1][x] == '.') || (g[y][x - 1] == '.' && g[y][x + 1] == '.')) g[y][x] = '.';
        }
        return g;
    }

    // ---- Inventario ----
    int ItemCount(string id)
    {
        foreach (var it in save.items) if (it.id == id) return it.n;
        return 0;
    }

    void AddItem(string id, int n)
    {
        foreach (var it in save.items) if (it.id == id) { it.n += n; return; }
        save.items.Add(new ItemStack { id = id, n = n });
    }

    bool ConsumeItem(string id)
    {
        foreach (var it in save.items)
            if (it.id == id && it.n > 0) { it.n--; if (it.n <= 0) save.items.Remove(it); return true; }
        return false;
    }

    string ItemName(string id)
    {
        switch (id)
        {
            case "pocion": return T("Poción de vitalidad", "Vitality potion");
            case "pergamino": return T("Pergamino de conocimiento", "Scroll of knowledge");
            case "salta": return T("Saltamuros", "Wall-jumper");
            case "tomekey": return T("Llave de Tomo", "Tome Key");
            case "hammer": return T("Martillo Rompemuros", "Wall-Breaker Hammer");
            default: return id;
        }
    }

    string ItemDesc(string id)
    {
        switch (id)
        {
            case "pocion": return T("Restaura la mitad de tu vida máxima.", "Restores half of your max HP.");
            case "pergamino": return T("En combate, solo en preguntas de UNA respuesta: descarta (marca en rojo) 2 opciones incorrectas.", "In battle, single-answer questions only: rules out (marks red) 2 wrong options.");
            case "salta": return T("SALTA por encima del muro de piedra que tengas delante (debe haber suelo al otro lado; cada unidad es 1 salto).", "JUMP over the stone wall you are facing (needs floor on the other side; each unit is 1 jump).");
            case "tomekey": return T("Abre un TOMO sin leer de la mazmorra actual (solo en mazmorras con tomos pendientes).", "Opens an UNREAD tome of the current dungeon (only in dungeons with pending tomes).");
            case "hammer": return T("En la Torre del Estudio: +20 de ataque y ROMPE el muro de piedra que tengas delante por solo 1 de racha (botón MURO en el mapa).", "In the Tower of Study: +20 attack and SMASH the stone wall you are facing for just 1 streak (WALL button on the map).");
            default: return "";
        }
    }

    // Icono del objeto: item_<id>; la Llave de Tomo cae al sprite del tomo si falta su PNG.
    string ItemSprite(string id) => (id == "tomekey" && LoadSprite("item_tomekey") == null) ? "tome"
        : (id == "hammer" && LoadSprite("item_hammer") == null) ? "item_salta" : "item_" + id;

    void OpenChest(Dictionary<string, object> thing)
    {
        MarkGone(thing);
        // Torre del Estudio: SIN tomos → pociones, experiencia, saltamuros o racha.
        if (save.area == "studytower")
        {
            int rr = rng.Next(100);
            string ti, bo;
            if (rr < 30)
            {
                AddItem("pocion", 1);
                ti = T("¡Poción de vitalidad!", "Vitality potion!");
                bo = T("Guardada en la mochila (🎒 tienes " + ItemCount("pocion") + ").", "Stored in your bag (🎒 you have " + ItemCount("pocion") + ").");
            }
            else if (rr < 55)
            {
                int gain = 20 + 2 * save.studyFloor;
                GainXp(gain);
                ti = T("¡Reliquia de experiencia!", "Experience relic!");
                bo = T("Ganas " + gain + " XP.  Nivel actual: " + save.lv + ".", "You gain " + gain + " XP.  Current level: " + save.lv + ".");
            }
            else if (rr < 80)
            {
                AddItem("salta", 2);
                ti = T("¡Saltamuros!", "Wall-jumper!");
                bo = T("Botas con 2 saltos (🎒 tienes " + ItemCount("salta") + "). SALTAN muros de PIEDRA.", "Boots with 2 jumps (🎒 you have " + ItemCount("salta") + "). They JUMP over STONE walls.");
            }
            else
            {
                int gain = 2 + rng.Next(4) + save.studyFloor / 25;
                combo += gain;
                ti = T("¡Racha arcana!", "Arcane streak!");
                bo = T("El cofre conserva concentración de la torre. +" + gain + " racha (ahora " + combo + ").",
                       "The chest stores tower focus. +" + gain + " streak (now " + combo + ").");
            }
            SaveSlot();
            RenderChest(ti, bo);
            return;
        }
        int r = rng.Next(100);
        string title, body;
        // Probabilidades: poción 24% · XP 14% · pergamino 20% · LLAVE DE TOMO 18% · SALTAMUROS 24%.
        if (r < 24)
        {
            AddItem("pocion", 1);
            title = T("¡Poción de vitalidad!", "Vitality potion!");
            body = T("Guardada en la mochila (🎒 tienes " + ItemCount("pocion") + "). Úsala cuando la necesites.",
                     "Stored in your bag (🎒 you have " + ItemCount("pocion") + "). Use it when you need it.");
        }
        else if (r < 38)
        {
            GainXp(25);
            title = T("¡Reliquia de experiencia!", "Experience relic!");
            body = T("Ganas 25 XP.  Nivel actual: " + save.lv + ".", "You gain 25 XP.  Current level: " + save.lv + ".");
        }
        else if (r < 58)
        {
            AddItem("pergamino", 1);
            title = T("¡Pergamino de conocimiento!", "Scroll of knowledge!");
            body = T("Guardado en la mochila (🎒 tienes " + ItemCount("pergamino") + "). En preguntas de UNA respuesta descarta 2 opciones incorrectas (en rojo).",
                     "Stored in your bag (🎒 you have " + ItemCount("pergamino") + "). On single-answer questions it rules out 2 wrong options (in red).");
        }
        else if (r < 76)
        {
            AddItem("tomekey", 1);
            title = T("¡Llave de Tomo!", "Tome Key!");
            body = T("Guardada en la mochila (🎒 tienes " + ItemCount("tomekey") + "). Úsala en una mazmorra para ABRIR un tomo sin leer.",
                     "Stored in your bag (🎒 you have " + ItemCount("tomekey") + "). Use it in a dungeon to OPEN an unread tome.");
        }
        else
        {
            AddItem("salta", 2);
            title = T("¡Saltamuros!", "Wall-jumper!");
            body = T("Botas encantadas con 2 saltos (🎒 tienes " + ItemCount("salta") + "). SALTAN por encima de muros de PIEDRA (con suelo al otro lado).",
                     "Enchanted boots with 2 jumps (🎒 you have " + ItemCount("salta") + "). They JUMP over STONE walls (with floor on the other side).");
        }
        SaveSlot();
        RenderChest(title, body);
    }

    void RenderChest(string title, string body)
    {
        screen = "chest";
        ClearRoot();
        AddBackdrop(BackdropForArea(save.area), 0.4f);
        var p = Panel(root, "Chest", new Color(.07f, .06f, .12f, .96f), Anchor.Stretch, new Vector2(30, 24), new Vector2(-30, -24));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .72f, .2f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 14, 26);
        BigSprite(col, "chest", 120, new Color(1f, .72f, .2f));
        Label(col, title, 26, new Color(1f, .82f, .22f), FontStyle.Bold);
        var t = Label(col, body, 18, Color.white, FontStyle.Normal); t.alignment = TextAnchor.UpperLeft;
        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    // ---- Pantalla de inventario y logros ----
    void RenderItems(string msg = "")
    {
        screen = "items";
        ClearRoot();
        AddBackdrop(BackdropForArea(save.area), 0.4f);
        var p = Panel(root, "Items", new Color(.06f, .07f, .14f, .96f), Anchor.Stretch, new Vector2(26, 22), new Vector2(-26, -22));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .72f, .2f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 10, 22);
        BigSprite(col, "item_bag", 80, new Color(1f, .72f, .2f));   // icono real de la mochila (el emoji no se ve en el APK)
        Label(col, T("MOCHILA", "BAG"), 28, new Color(1f, .82f, .22f), FontStyle.Bold);
        if (!string.IsNullOrEmpty(msg)) Label(col, msg, 17, new Color(.7f, 1f, .8f), FontStyle.Bold);
        if (save.items.Count == 0)
            Label(col, T("Vacía. Los cofres de las mazmorras guardan pociones, pergaminos, llaves de tomo y saltamuros. (El Rompemuros es una habilidad, no un objeto.)", "Empty. Dungeon chests hold potions, scrolls, tome keys and wall-jumpers. (Wall Breaker is an ability, not an item.)"), 18, Color.white, FontStyle.Normal);
        foreach (var it in new List<ItemStack>(save.items))
        {
            string id = it.id;
            BigSprite(col, ItemSprite(id), 64, new Color(1f, .72f, .2f));
            Label(col, ItemName(id) + "  ×" + it.n, 21, Color.white, FontStyle.Bold);
            Label(col, ItemDesc(id), 16, new Color(.85f, .9f, 1f), FontStyle.Normal);
            if (id == "tomekey")
                Button(col, T("✦ USAR LLAVE DE TOMO", "✦ USE TOME KEY"), UseTomeKey, new Color(.55f, .4f, .15f));
            else if (id == "pocion")
                Button(col, T("✦ BEBER POCIÓN", "✦ DRINK POTION"), () =>
                {
                    if (!ConsumeItem("pocion")) return;
                    int heal = Mathf.CeilToInt(save.maxhp * 0.5f);
                    save.hp = Mathf.Min(save.maxhp, save.hp + heal);
                    SaveSlot();
                    string mp = T("Recuperas " + heal + " HP. Vida: " + save.hp + "/" + save.maxhp + ".", "You recover " + heal + " HP. Health: " + save.hp + "/" + save.maxhp + ".");
                    StartCoroutine(PlayCutIn("potion", () => RenderItems(mp)));
                }, new Color(.2f, .55f, .35f));
            else if (id == "salta")
                Button(col, T("✦ SALTAR EL MURO DE ENFRENTE", "✦ JUMP THE WALL AHEAD"), UseWallJump, new Color(.2f, .45f, .6f));
        }
        // HABILIDAD Rompemuros: siempre disponible en mazmorra (sin objeto). Sacrifica vida +
        // exige acertar una pregunta de la zona; si aciertas, el muro de enfrente se destruye.
        if (IsDungeon(save.area))
        {
            BigSprite(col, LoadSprite("item_rompe") != null ? "item_rompe" : "item_salta", 60, new Color(.85f, .9f, 1f));
            Label(col, T("ROMPEMUROS (habilidad): destruye el muro de piedra de enfrente por " + WallBreakHpCost + " HP + acertar una pregunta de la zona.",
                         "WALL BREAKER (ability): destroys the stone wall ahead for " + WallBreakHpCost + " HP + a correct zone question."), 15, new Color(.95f, .85f, .7f), FontStyle.Italic);
            Button(col, T("💥 ROMPER MURO DE ENFRENTE  ·  -" + WallBreakHpCost + " HP", "💥 BREAK THE WALL AHEAD  ·  -" + WallBreakHpCost + " HP"), UseWallBreak, new Color(.5f, .18f, .22f));
        }
        Label(col, "—  " + T("LOGROS", "ACHIEVEMENTS") + "  —", 24, new Color(1f, .82f, .22f), FontStyle.Bold);
        if (save.medallions.Count == 0 && save.trophies.Count == 0)
            Label(col, T("Aún sin logros. Los Súper Sabios, el Rey Demonio y el Sabio del Examen otorgan los suyos.", "No achievements yet. The Super Sages, the Demon King and the Exam Sage grant theirs."), 16, new Color(.8f, .85f, 1f), FontStyle.Italic);
        foreach (var m in save.medallions)
        {
            BigSprite(col, "icon_" + m, 56, MasterItemColor(m));
            Label(col, T("Medallón maestro: ", "Master medallion: ") + MasterItemName(m), 18, MasterItemColor(m), FontStyle.Bold);
        }
        foreach (var tr in save.trophies)
        {
            BigSprite(col, tr == "study_tower" && LoadSprite("trophy_studytower") != null ? "trophy_studytower" : "trophy_" + (tr == "examen" || tr == "gran_sabio" || tr == "study_tower" ? "exam" : tr.Replace("demonio", "demon")), 56, new Color(1f, .82f, .22f));
            Label(col, TrophyName(tr), 18, new Color(1f, .82f, .22f), FontStyle.Bold);
        }
        // PODER: La Biblia del Estudio (al dominar la Torre del Estudio). Accesible desde aquí.
        if (save.trophies.Contains("study_tower"))
        {
            Label(col, "—  " + T("PODERES", "POWERS") + "  —", 24, new Color(1f, .82f, .22f), FontStyle.Bold);
            BigSprite(col, bibleSprite, 64, new Color(1f, .82f, .22f));
            Button(col, T("📖  ABRIR LA BIBLIA DEL ESTUDIO", "📖  OPEN THE STUDY BIBLE"), () => RenderBibleIndex(), new Color(.4f, .3f, .1f));
        }
        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    string TrophyName(string tr)
    {
        switch (tr)
        {
            case "examen": return T("🏆 Conquistador del Examen (Sabio del Examen superado)", "🏆 Exam Conqueror (Exam Sage defeated)");
            case "demonio_es": return T("🏆 Vencedor del Rey Demonio · ESPAÑOL", "🏆 Demon King Slayer · SPANISH");
            case "demonio_en": return T("🏆 Vencedor del Rey Demonio · INGLÉS", "🏆 Demon King Slayer · ENGLISH");
            case "gran_sabio": return T("🏅 Medallón del Gran Sabio (Torre del Gran Sabio superada)", "🏅 Great Sage Medallion (Tower of the Great Sage conquered)");
            case "study_tower": return T("🏯 Trofeo de la Torre del Estudio (100 pisos + Sabio del Estudio)", "🏯 Tower of Study Trophy (100 floors + Sage of Study)");
            default: return "🏆 " + tr;
        }
    }

    const int WallBreakHpCost = 25;   // coste de vida de la HABILIDAD Rompemuros (sacrificio)

    // OBJETO Saltamuros: SALTA por encima del muro de piedra de enfrente (teleporta 2 casillas).
    // Necesita suelo al otro lado. Gasta 1 unidad del objeto. (No destruye nada.)
    void UseWallJump()
    {
        var layout = Layout();
        int wx = save.x + save.fx, wy = save.y + save.fy;
        int lx = save.x + save.fx * 2, ly = save.y + save.fy * 2;
        if ((save.fx == 0 && save.fy == 0) || wy < 0 || wx < 0 || wy >= layout.Count || wx >= S(layout[wy]).Length)
        { RenderItems(T("⚠ Mira hacia un muro: muévete una vez en su dirección y reintenta.", "⚠ Face a wall: step once toward it and try again.")); return; }
        char wall = S(layout[wy])[wx];
        if (wall == '~' || wall == 'T' || wall == 'A' || wall == 'C' || wall == 'G')
        { RenderItems(T("⚠ Solo se saltan muros de PIEDRA; eso es de otro material.", "⚠ Only STONE walls can be jumped; that is another material.")); return; }
        if (wall != '#')
        { RenderItems(T("⚠ No tienes un muro de piedra justo delante.", "⚠ You are not facing a stone wall.")); return; }
        if (ly < 0 || lx < 0 || ly >= layout.Count || lx >= S(layout[ly]).Length || "#~TACG".IndexOf(S(layout[ly])[lx]) >= 0)
        { RenderItems(T("⚠ Al otro lado no hay suelo donde caer.", "⚠ There is no floor to land on beyond it.")); return; }
        if (!ConsumeItem("salta")) return;
        save.x = lx; save.y = ly; walkFrame++;
        SaveSlot();
        string mj = T("¡Zas! Saltas el muro de piedra. (Saltamuros: " + ItemCount("salta") + ")", "Whoosh! You jump the stone wall. (Wall-jumpers: " + ItemCount("salta") + ")");
        StartCoroutine(PlayCutIn("jump", () => { if (!(IsDungeon(save.area) && DungeonStep())) RenderWorld(mj); }));
    }

    // HABILIDAD Rompemuros (overworld, sin objeto): sacrifica vida + pregunta de la zona.
    void UseWallBreak() => RenderWallBreak();

    // Índice de una pregunta del MÓDULO de la mazmorra actual (pregunta "de la zona"). -1 si no hay.
    int ZoneQuestionIndex()
    {
        if (!IsDungeon(save.area)) return -1;
        var zone = L(data["zones"])[ZoneIndexForArea(save.area)] as Dictionary<string, object>;
        if (zone == null) return -1;
        int dom = I(zone["dom"]);
        var all = L(data["bq"]); var pool = new List<int>();
        for (int i = 0; i < all.Count; i++) if (I((all[i] as Dictionary<string, object>)["d"]) == dom) pool.Add(i);
        return pool.Count > 0 ? pool[rng.Next(pool.Count)] : -1;
    }

    void BreakWallTile(int wx, int wy)
    {
        if (wy < 0 || wy >= dunLayout.Count) return;
        var rc = S(dunLayout[wy]).ToCharArray();
        if (wx >= 0 && wx < rc.Length) { rc[wx] = '.'; dunLayout[wy] = new string(rc); }
    }

    // HABILIDAD Rompemuros: DESTRUYE el muro de piedra de enfrente (se vuelve caminable).
    // Cuesta VIDA (riesgo: se paga aciertes o no) y exige ACERTAR una pregunta de la ZONA.
    // Solo si aciertas, el muro cae. No usa objeto; está siempre disponible en mazmorra.
    void RenderWallBreak()
    {
        if (!IsDungeon(save.area) || dunLayout == null || dunArea != save.area)
        { RenderItems(T("⚠ Solo puedes romper muros dentro de una mazmorra.", "⚠ You can only break walls inside a dungeon.")); return; }
        int wx = save.x + save.fx, wy = save.y + save.fy;
        if ((save.fx == 0 && save.fy == 0) || wy < 0 || wx < 0 || wy >= dunLayout.Count || wx >= S(dunLayout[wy]).Length)
        { RenderItems(T("⚠ Mira hacia un muro: muévete una vez en su dirección y reintenta.", "⚠ Face a wall: step once toward it and try again.")); return; }
        char wall = S(dunLayout[wy])[wx];
        if (wall == '~' || wall == 'T' || wall == 'A' || wall == 'C' || wall == 'G')
        { RenderItems(T("⚠ Solo se destruyen muros de PIEDRA; eso es de otro material.", "⚠ Only STONE walls can be destroyed; that is another material.")); return; }
        if (wall != '#')
        { RenderItems(T("⚠ No tienes un muro de piedra justo delante.", "⚠ You are not facing a stone wall.")); return; }
        int cost = WallBreakHpCost;
        if (save.hp <= cost)
        { RenderItems(T("⚠ Necesitas más de " + cost + " HP: romper un muro siempre cuesta vida.", "⚠ You need more than " + cost + " HP: breaking a wall always costs life.")); return; }
        int qi = ZoneQuestionIndex();
        if (qi < 0)   // sin preguntas de la zona: rompe pagando solo HP
        {
            save.hp = Math.Max(1, save.hp - cost); BreakWallTile(wx, wy); SaveSlot();
            RenderWorld(T("¡CRAC! Rompes el muro de piedra (-" + cost + " HP).", "Crack! You break the stone wall (-" + cost + " HP).")); return;
        }
        var q = L(data["bq"])[qi] as Dictionary<string, object>;
        string key = "bq:" + qi; MarkSeen(key);
        screen = "dialog";
        ClearRoot();
        AddBackdrop(BackdropForArea(save.area), 0.5f);
        var p = Panel(root, "WallBreak", new Color(.10f, .06f, .06f, .96f), Anchor.Stretch, new Vector2(26, 20), new Vector2(-26, -20));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .6f, .4f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 22);
        BigSprite(col, LoadSprite("item_rompe") != null ? "item_rompe" : "item_salta", 96, new Color(.85f, .9f, 1f));
        Label(col, T("💥 ROMPEMUROS (habilidad)", "💥 WALL BREAKER (ability)"), 25, new Color(1f, .7f, .45f), FontStyle.Bold);
        Label(col, T("Cuesta " + cost + " HP (riesgo: la vida se paga aciertes o no) y exige acertar una pregunta de la ZONA. Solo si aciertas, el muro se destruye.",
                     "Costs " + cost + " HP (risk: life is spent whether you pass or not) and requires a correct ZONE question. The wall is destroyed only if you're right."), 16, new Color(1f, .85f, .7f), FontStyle.Bold);
        var eval = BuildQuestionBody(col, q);
        var warn = Label(col, " ", 15, new Color(1f, .65f, .45f), FontStyle.Bold);
        OptButton(col, T("💥 ROMPER (-" + cost + " HP)", "💥 BREAK (-" + cost + " HP)"), () =>
        {
            var r = eval();
            if (r == null) { warn.text = T("⚠ Completa la respuesta.", "⚠ Complete the answer."); return; }
            save.hp = Math.Max(1, save.hp - cost);   // el riesgo: la vida se paga igual
            if (r == true)
            {
                MarkCorrect(key); save.wrongQ.Remove(key);
                BreakWallTile(wx, wy); PlaySfx(sfxRight); SaveSlot();
                string mb = T("¡CRAC! Aciertas y el muro de piedra se DESTRUYE (-" + cost + " HP). El paso queda abierto. (Vida: " + save.hp + "/" + save.maxhp + ")",
                             "Crack! Correct — the stone wall is DESTROYED (-" + cost + " HP). The way is open. (Health: " + save.hp + "/" + save.maxhp + ")");
                StartCoroutine(PlayCutIn("break", () => RenderWorld(mb)));
            }
            else
            {
                if (!save.wrongQ.Contains(key)) save.wrongQ.Add(key); BumpWrong(key); PlaySfx(sfxWrong); SaveSlot();
                RenderWorld(T("❌ Fallas: pierdes " + cost + " HP y el muro AGUANTA. (Vida: " + save.hp + "/" + save.maxhp + ")",
                             "❌ Wrong: you lose " + cost + " HP and the wall HOLDS. (Health: " + save.hp + "/" + save.maxhp + ")"));
            }
        }, new Color(.5f, .2f, .22f));
        Button(col, T("↩  CANCELAR", "↩  CANCEL"), () => RenderItems(""), new Color(.3f, .2f, .45f));
    }

    // Llave de Tomo: abre un tomo SIN LEER de la mazmorra actual (mismo flujo que pisarlo:
    // lectura + comprobación marca el tomo). Solo sirve en mazmorras con tomos pendientes.
    void UseTomeKey()
    {
        if (!IsDungeon(save.area)) { RenderItems(T("⚠ La Llave de Tomo solo sirve dentro de una mazmorra.", "⚠ The Tome Key only works inside a dungeon.")); return; }
        var unread = new List<Dictionary<string, object>>();
        foreach (var t in Things(save.area))
            if (S(t, "kind") == "tome" && !Gone(t) && (save.readTomeTiles == null || !save.readTomeTiles.Contains(ThingKey(t)))) unread.Add(t);
        if (unread.Count == 0) { RenderItems(T("⚠ Ya leíste todos los tomos de esta mazmorra.", "⚠ You've already read every tome here.")); return; }
        if (!ConsumeItem("tomekey")) return;
        var tome = unread[rng.Next(unread.Count)];
        var ordered = AreaTomeIdsOrdered(save.area);
        string openId = ordered.Count > 0 ? ordered[Math.Min(ReadTomesInArea(), ordered.Count - 1)] : S(tome, "id");
        pendingTomeTile = ThingKey(tome);
        SaveSlot();
        string oid = openId;
        StartCoroutine(PlayCutIn("usekey", () => StartTome(oid)));
    }

    void StartDialog(string who)
    {
        var npc = D(data["npcs"])[who] as Dictionary<string, object>;
        dialogWho = who;
        dialogQuiz = npc.ContainsKey("quiz");
        dialogLines = ToStringList(L(npc["lines"]));
        dialogPage = 0;
        RenderDialog(S(npc, "name"));
    }

    void RenderDialog(string name)
    {
        screen = "dialog";
        ClearRoot();
        AddBackdrop(BackdropForArea(save.area), 0.4f);
        var p = Panel(root, "Dialog", new Color(.05f, .08f, .18f, .94f), Anchor.Stretch, new Vector2(22, 22), new Vector2(-22, -22));
        Vertical(p, 16, TextAnchor.MiddleCenter);
        BigSprite(p, SpriteForNpc(dialogWho), 132, new Color(.35f, .72f, 1f));
        Label(p, name, 26, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(p, dialogLines[dialogPage], 22, Color.white, FontStyle.Normal);
        bool last = dialogPage >= dialogLines.Count - 1;
        Button(p, last ? (dialogQuiz ? T("RESPONDER PREGUNTA", "ANSWER A QUESTION") : T("VOLVER", "BACK")) : T("SIGUIENTE", "NEXT"), () =>
        {
            if (!last) { dialogPage++; RenderDialog(name); }
            else if (dialogQuiz) StartPractice();
            else RenderWorld();
        }, new Color(.95f, .72f, .12f));
    }

    void StartPractice()
    {
        var all = L(data["bq"]);
        int idx = rng.Next(all.Count);
        var q = all[idx] as Dictionary<string, object>;
        string key = "bq:" + idx;
        MarkSeen(key);
        RenderQuestion(q, correct =>
        {
            if (correct)
            {
                save.wrongQ.Remove(key);
                MarkCorrect(key);            // cuenta para "Dominadas"
                GainXp(15);
                PlaySfx(sfxWin);
            }
            else
            {
                if (!save.wrongQ.Contains(key)) save.wrongQ.Add(key);
                BumpWrong(key);
                PlaySfx(sfxWrong);
            }
            SaveSlot();
            RenderPracticeResult(correct, QS(q, "why"));
        });
    }

    void RenderPracticeResult(bool correct, string why)
    {
        screen = "practice";
        ClearRoot();
        AddBackdrop(BackdropForArea(save.area), 0.35f);
        var p = Panel(root, "Practice", new Color(.05f, .08f, .16f, .96f), Anchor.Stretch, new Vector2(30, 24), new Vector2(-30, -24));
        var col = ScrollColumn(p, 14, 26);
        Label(col, correct ? T("¡CORRECTO!  +15 XP", "CORRECT!  +15 XP") : T("INCORRECTO (guardada para repasar)", "INCORRECT (saved for review)"), 26, correct ? new Color(.55f, 1f, .63f) : new Color(1f, .5f, .45f), FontStyle.Bold);
        var t = Label(col, HighlightTerms(why), 18, Color.white, FontStyle.Normal); t.alignment = TextAnchor.UpperLeft; t.supportRichText = true;
        Label(col, T("Dominadas ", "Mastered ") + MasteredCount() + "/" + L(data["bq"]).Count, 18, new Color(.7f, .92f, 1f), FontStyle.Bold);
        Button(col, T("OTRA PREGUNTA", "ANOTHER QUESTION"), StartPractice, new Color(.16f, .45f, .55f));
        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    void StartTome(string id)
    {
        activeTome = id; tomePage = 0; PlaySfx(sfxPage); RenderTome();   // sonido de libro SOLO al abrirlo
    }

    void RenderTome()
    {
        screen = "tome";
        ClearRoot();
        PlayTrack("tome");   // lectura serena (piano suave); la misma pista suena en la Granja y los Súper Tomos
        var tome = D(data["tomes"])[activeTome] as Dictionary<string, object>;
        var pages = L(tome["pages"]);
        string dom = TomeDomain(activeTome);
        AddBackdrop(BackdropForArea(dom), 0.4f);
        var p = Panel(root, "Tome", new Color(.07f, .06f, .16f, .92f), Anchor.Stretch, new Vector2(40, 24), new Vector2(-40, -24));
        var ol = p.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(.55f, .8f, 1f, .5f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 14, 30);
        Label(col, S(tome, "t"), 26, new Color(1f, .82f, .22f), FontStyle.Bold);
        FramedResourceImage(col, TomeImage(tome, tomePage), 200, new Color(1f, .82f, .22f));
        string pageText = S(pages[tomePage]);
        var body = Label(col, HighlightTerms(pageText), 21, Color.white, FontStyle.Normal);
        body.alignment = TextAnchor.UpperLeft;
        body.supportRichText = true;

        var terms = TermsInText(pageText);
        if (terms.Count > 0)
        {
            Label(col, T("Terminos tecnicos de esta pagina", "Technical terms on this page"), 17, new Color(.55f, 1f, .75f), FontStyle.Bold);
            var chips = Panel(col, "TermChips", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
            var le = chips.gameObject.AddComponent<LayoutElement>(); le.minHeight = 86; le.preferredHeight = 86; le.flexibleWidth = 1;
            Horizontal(chips, 8, TextAnchor.MiddleCenter);
            foreach (var term in terms)
            {
                string captured = term;
                Button(chips, captured, () => { termOrigin = "tome"; RenderTermDefinition(captured); }, TermColor(captured), 190);
            }
        }

        Label(col, T("Pagina ", "Page ") + (tomePage + 1) + "/" + pages.Count, 15, new Color(.7f, .8f, 1f), FontStyle.Normal);
        Button(col, tomePage < pages.Count - 1 ? T("SIGUIENTE", "NEXT") : T("COMPROBACIÓN", "KNOWLEDGE CHECK"), () =>
        {
            if (++tomePage < pages.Count) RenderTome();   // pasar página es INSTANTÁNEO (sin animación que ralentice)
            else RenderTomeQuestion();
        }, new Color(.95f, .72f, .12f));
    }

    string TomeDomain(string id)
    {
        // Los tomos AB-410 se identifican p<parte>m<modulo>_<n> (p.ej. "p3m4_2").
        if (id != null && id.Length >= 2 && id[0] == 'p' && char.IsDigit(id[1])) return "p" + id[1];
        return "town";
    }

    // Ilustración del tomo de mazmorra: imágenes REALES del docx asignadas por tema en los
    // datos (campo "imgs" del tomo, rutas Resources/Art/Copilot/imageNN). Alternan por página
    // para dar variedad; cada tomo tiene su propio juego, sin repetir entre tomos.
    string TomeImage(Dictionary<string, object> tome, int page)
    {
        if (tome.ContainsKey("imgs"))
        {
            var imgs = L(tome["imgs"]);
            if (imgs.Count > 0) return S(imgs[page % imgs.Count]);
        }
        return "Art/Copilot/image70";   // fallback genérico (existe)
    }

    // Quiz de comprobacion del tomo: 4 preguntas por tomo (peticion del usuario), todo-o-nada.
    // Hay que acertar TODAS para dominar el tomo; asi se garantiza que se entendio la lectura.
    List<object> TomeChecks(Dictionary<string, object> tome)
    {
        if (tome.ContainsKey("checks")) return L(tome["checks"]);
        return new List<object> { tome["check"] };   // compat partidas/datos antiguos
    }

    void RenderTomeQuestion()
    {
        screen = "tome-quiz";
        ClearRoot();
        PlayTrack("tome");
        var tome = D(data["tomes"])[activeTome] as Dictionary<string, object>;
        var checks = TomeChecks(tome);
        string dom = TomeDomain(activeTome);
        AddBackdrop(BackdropForArea(dom), 0.4f);
        var p = Panel(root, "TomeQuiz", new Color(.05f, .06f, .14f, .95f), Anchor.Stretch, new Vector2(30, 16), new Vector2(-30, -16));
        var col = ScrollColumn(p, 12, 24);
        Label(col, T("COMPROBACIÓN · acierta las " + checks.Count + " para dominar el tomo",
                     "KNOWLEDGE CHECK · answer all " + checks.Count + " to master this tome"), 22, new Color(1f, .85f, .35f), FontStyle.Bold);
        Label(col, S(tome, "t"), 17, new Color(.7f, .9f, 1f), FontStyle.Bold);

        var evals = new List<Func<bool?>>();
        var keys = new List<string>();
        for (int i = 0; i < checks.Count; i++)
        {
            var qq = checks[i] as Dictionary<string, object>;
            string key = "chk:" + activeTome + ":" + i;
            keys.Add(key); MarkSeen(key);
            Label(col, "━━━  " + T("PREGUNTA ", "QUESTION ") + (i + 1) + " / " + checks.Count + "  ━━━", 16, new Color(.6f, .9f, 1f), FontStyle.Bold);
            evals.Add(BuildQuestionBody(col, qq));
        }
        var warn = Label(col, " ", 15, new Color(1f, .65f, .45f), FontStyle.Bold);
        bool done = false;
        OptButton(col, T("✔  COMPROBAR", "✔  CHECK ANSWERS"), () =>
        {
            if (done) return;
            var res = new List<bool?>();
            foreach (var ev in evals) res.Add(ev());
            for (int i = 0; i < res.Count; i++)
                if (res[i] == null) { warn.text = T("⚠ Completa la pregunta " + (i + 1) + ".", "⚠ Complete question " + (i + 1) + "."); return; }
            done = true;
            bool all = true;
            int newlyMastered = 0, lvBefore = save.lv;
            for (int i = 0; i < res.Count; i++)
            {
                if (res[i].Value)
                {
                    if (!save.correctQ.Contains(keys[i])) newlyMastered++;   // XP solo la 1ª vez que se domina
                    MarkCorrect(keys[i]);
                }
                else { all = false; if (!save.wrongQ.Contains(keys[i])) save.wrongQ.Add(keys[i]); BumpWrong(keys[i]); }
            }
            int xpGain = newlyMastered * TomeQuestionXp;   // dominar preguntas SUBE de nivel
            if (xpGain > 0) GainXp(xpGain);
            PlaySfx(all ? sfxRight : sfxWrong);
            if (all)
            {
                if (!save.readTomes.Contains(activeTome)) save.readTomes.Add(activeTome);
                if (!string.IsNullOrEmpty(pendingTomeTile))
                {
                    if (save.readTomeTiles == null) save.readTomeTiles = new List<string>();
                    if (!save.readTomeTiles.Contains(pendingTomeTile)) save.readTomeTiles.Add(pendingTomeTile);
                    pendingTomeTile = null;
                }
                foreach (var k in keys) save.wrongQ.Remove(k);
                SaveSlot();
                if (BossChaseActive() && bossChaser == null) { SpawnBossChaser(); return; }   // último tomo: ¡aparece el jefe!
                string msg = T("¡Tomo dominado!", "Tome mastered!");
                if (xpGain > 0) msg += "  +" + xpGain + " XP";
                if (save.lv > lvBefore) msg += "   " + T("¡SUBES A NIVEL ", "LEVEL UP! Lv ") + save.lv + "!";
                RenderWorld(msg);
            }
            else { SaveSlot(); RenderTomeQuizFail(checks, res, newlyMastered, xpGain, save.lv > lvBefore ? save.lv : 0); }
        }, new Color(.2f, .5f, .35f), 0, 82);
        Button(col, T("VOLVER A LEER", "RE-READ"), () => { tomePage = 0; RenderTome(); }, new Color(.32f, .18f, .42f));
    }

    void RenderTomeQuizFail(List<object> checks, List<bool?> res, int newlyMastered = 0, int xpGain = 0, int leveledTo = 0)
    {
        screen = "tome-quiz-fail";
        ClearRoot();
        AddBackdrop(BackdropForArea(TomeDomain(activeTome)), 0.4f);
        var p = Panel(root, "TomeQuizFail", new Color(.09f, .05f, .1f, .95f), Anchor.Stretch, new Vector2(30, 18), new Vector2(-30, -18));
        var col = ScrollColumn(p, 12, 26);
        int wrong = 0, right = 0; foreach (var r in res) { if (r == false) wrong++; else if (r == true) right++; }
        Label(col, T("Aún no. Repasa y reintenta.", "Not yet. Review and try again."), 26, new Color(1f, .5f, .45f), FontStyle.Bold);
        Label(col, T(wrong + " fallo(s) · necesitas las " + checks.Count + " correctas.",
                     wrong + " wrong · you need all " + checks.Count + " correct."), 17, new Color(1f, .72f, .5f), FontStyle.Bold);
        // Progreso: las que SÍ acertaste quedan dominadas y ya suman XP aunque el tomo no esté completo.
        if (right > 0)
        {
            string dom = "✓ " + right + T(" pregunta(s) DOMINADA(S)", " question(s) MASTERED");
            if (xpGain > 0) dom += "  ·  +" + xpGain + " XP";
            Label(col, dom, 16, new Color(.55f, 1f, .63f), FontStyle.Bold);
            if (leveledTo > 0)
                Label(col, T("¡SUBES A NIVEL " + leveledTo + "!", "LEVEL UP! Lv " + leveledTo), 16, new Color(1f, .85f, .35f), FontStyle.Bold);
        }
        for (int i = 0; i < checks.Count; i++)
        {
            if (res[i] == true) continue;
            var qq = checks[i] as Dictionary<string, object>;
            Label(col, "Q" + (i + 1) + ".  " + QS(qq, "q"), 18, Color.white, FontStyle.Bold);
            Label(col, "→ " + CorrectAnswerText(qq), 17, new Color(.55f, 1f, .63f), FontStyle.Bold);
            var w = Label(col, HighlightTerms(QS(qq, "why")), 15, new Color(.82f, .88f, 1f), FontStyle.Normal);
            w.alignment = TextAnchor.UpperLeft; w.supportRichText = true;
        }
        Button(col, T("REINTENTAR", "TRY AGAIN"), RenderTomeQuestion, new Color(.95f, .72f, .12f));
        Button(col, T("VOLVER A LEER", "RE-READ"), () => { tomePage = 0; RenderTome(); }, new Color(.32f, .18f, .42f));
        Button(col, T("SALIR", "LEAVE"), () => RenderWorld(""), new Color(.3f, .16f, .18f));
    }

    void StartSuperTome(string id)
    {
        activeSuperTome = id;
        superTomePage = 0;
        PlaySfx(sfxPage);   // sonido de libro SOLO al abrirlo
        RenderSuperTome();
    }

    void RenderSuperTome()
    {
        screen = "supertome";
        ClearRoot();
        PlayTrack("tome");   // misma pista de lectura serena que los tomos de mazmorra y la Granja
        var tome = SuperTomeById(activeSuperTome);
        if (tome == null) { RenderWorld(T("No encontre ese Super Tomo.", "Couldn't find that Super Tome.")); return; }
        var pages = L(tome["pages"]);
        superTomePage = Mathf.Clamp(superTomePage, 0, pages.Count - 1);
        var page = pages[superTomePage] as Dictionary<string, object>;
        if (superTomePage == pages.Count - 1 && save != null)   // llegar a la última página = leído (desbloquea Caso Contoso)
        {
            if (save.readSuper == null) save.readSuper = new List<string>();
            if (!save.readSuper.Contains(activeSuperTome)) { save.readSuper.Add(activeSuperTome); SaveSlot(); }
        }

        AddBackdrop(BackdropForArea("town"), 0.35f);
        var p = Panel(root, "SuperTome", new Color(.035f, .045f, .09f, .97f), Anchor.Stretch, new Vector2(24, 18), new Vector2(-24, -18));
        var ol = p.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(.45f, .9f, 1f, .55f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 24);

        Label(col, STS(tome, "title"), 25, new Color(.45f, .9f, 1f), FontStyle.Bold);
        Label(col, T("Desbloqueado al completar ", "Unlocked by clearing ") + AreaName(S(tome, "unlock")) + T(" · Pagina ", " · Page ") + (superTomePage + 1) + "/" + pages.Count, 15, new Color(.8f, .86f, 1f), FontStyle.Normal);
        FramedResourceImage(col, SuperTomeImage(tome, page, superTomePage), 210, new Color(.45f, .9f, 1f));
        var section = Label(col, STS(page, "section"), 21, new Color(1f, .82f, .22f), FontStyle.Bold);
        section.alignment = TextAnchor.UpperLeft;
        var body = Label(col, HighlightTerms(STS(page, "text")), 19, Color.white, FontStyle.Normal);
        body.alignment = TextAnchor.UpperLeft;
        body.supportRichText = true;

        var terms = TermsInText(STS(page, "text"));
        if (terms.Count > 0)
        {
            Label(col, T("Terminos tecnicos de esta pagina", "Technical terms on this page"), 18, new Color(.55f, 1f, .75f), FontStyle.Bold);
            var chips = Panel(col, "TermChips", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
            var le = chips.gameObject.AddComponent<LayoutElement>(); le.minHeight = 86; le.preferredHeight = 86; le.flexibleWidth = 1;
            Horizontal(chips, 8, TextAnchor.MiddleCenter);
            foreach (var term in terms)
            {
                string captured = term;
                Button(chips, captured, () => { termOrigin = "supertome"; RenderTermDefinition(captured); }, TermColor(captured), 190);
            }
        }

        var nav = Panel(col, "SuperNav", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
        var navLe = nav.gameObject.AddComponent<LayoutElement>(); navLe.minHeight = 84; navLe.preferredHeight = 84; navLe.flexibleWidth = 1;
        Horizontal(nav, 10, TextAnchor.MiddleCenter);
        if (superTomePage > 0) Button(nav, T("ANTERIOR", "PREVIOUS"), () => { superTomePage--; RenderSuperTome(); }, new Color(.16f, .26f, .5f), 210);
        if (superTomePage < pages.Count - 1) Button(nav, T("SIGUIENTE", "NEXT"), () => { superTomePage++; RenderSuperTome(); }, new Color(.95f, .72f, .12f), 210);
        Button(nav, T("VOLVER", "BACK"), () => RenderWorld(T("Cerraste el Super Tomo.", "You closed the Super Tome.")), new Color(.32f, .18f, .42f), 180);
    }

    IEnumerator SuperTomePageTurn(int delta)
    {
        screen = "supertome-turn";
        ClearRoot();
        AddBackdrop(BackdropForArea("town"), 0.25f);
        var p = Panel(root, "PageTurn", new Color(.02f, .03f, .07f, .96f), Anchor.Stretch, new Vector2(24, 18), new Vector2(-24, -18));
        var frames = AnimFrames("page");
        if (frames != null)   // animación con el arte del libro (pasa/vuelve la página)
        {
            var first = LoadSprite(frames[0]);
            var go = new GameObject("PageImg", typeof(Image)); go.transform.SetParent(p, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            float h = 400f, w = h * first.rect.width / first.rect.height; rt.sizeDelta = new Vector2(w, h);
            var im = go.GetComponent<Image>(); im.preserveAspect = true; im.raycastTarget = false;
            int[] seq = delta > 0 ? new[] { 0, 1, 2 } : new[] { 2, 1, 0 };
            foreach (int fr in seq) { im.sprite = LoadSprite(frames[Mathf.Min(fr, frames.Count - 1)]); yield return new WaitForSeconds(0.1f); }
        }
        else   // respaldo: efecto de dos páginas de color
        {
            var left = Panel(p, "PageLeft", new Color(.08f, .12f, .2f, 1f), Anchor.Stretch, new Vector2(18, 34), new Vector2(-Screen.width / 2f - 4, -34));
            var right = Panel(p, "PageRight", new Color(.11f, .16f, .28f, 1f), Anchor.Stretch, new Vector2(Screen.width / 2f + 4, 34), new Vector2(-18, -34));
            left.gameObject.AddComponent<Outline>().effectColor = new Color(.45f, .9f, 1f, .45f);
            right.gameObject.AddComponent<Outline>().effectColor = new Color(1f, .82f, .22f, .45f);
            Label(p, delta > 0 ? T("PASANDO PAGINA...", "TURNING PAGE...") : T("VOLVIENDO PAGINA...", "TURNING BACK..."), 24, new Color(1f, .82f, .22f), FontStyle.Bold, Anchor.Stretch, Vector2.zero, Vector2.zero).alignment = TextAnchor.MiddleCenter;
            yield return new WaitForSeconds(0.18f);
        }
        superTomePage += delta;
        RenderSuperTome();
    }

    string SuperTomeImage(Dictionary<string, object> tome, Dictionary<string, object> page, int pageIndex)
    {
        // Cada página trae su imagen real del docx (campo "image"); este es solo el fallback.
        if (page.ContainsKey("image") && !string.IsNullOrEmpty(S(page, "image"))) return S(page, "image");
        string unlock = S(tome, "unlock");
        string[] d1 = { "Art/Copilot/image37", "Art/Copilot/image43", "Art/Copilot/image70", "Art/Copilot/image15" };
        string[] d2 = { "Art/Copilot/image40", "Art/Copilot/image34", "Art/Copilot/image16", "Art/Copilot/image72" };
        string[] d3 = { "Art/Copilot/image61", "Art/Copilot/image52", "Art/Copilot/image35", "Art/Copilot/image5" };
        var set = unlock == "d1" ? d1 : (unlock == "d2" ? d2 : d3);
        return set[pageIndex % set.Length];
    }

    Dictionary<string, object> SuperTomeById(string id)
    {
        if (!superData.ContainsKey("tomes")) return null;
        foreach (var o in L(superData["tomes"]))
        {
            var t = o as Dictionary<string, object>;
            if (S(t, "id") == id) return t;
        }
        return null;
    }

    void RenderTermDefinition(string term)
    {
        screen = "term";
        ClearRoot();
        AddBackdrop(BackdropForArea("town"), 0.32f);
        var p = Panel(root, "TermDefinition", new Color(.045f, .06f, .13f, .97f), Anchor.Stretch, new Vector2(54, 48), new Vector2(-54, -48));
        var col = ScrollColumn(p, 14, 28);
        Label(col, term, 30, TermColor(term), FontStyle.Bold);
        Label(col, TermDefinition(term), 22, Color.white, FontStyle.Normal);
        if (termOrigin == "tome") Button(col, T("VOLVER AL TOMO", "BACK TO TOME"), RenderTome, new Color(.95f, .72f, .12f));
        else if (termOrigin == "wisdom") Button(col, T("VOLVER", "BACK"), () => RenderBossWisdom(wisdomZi, wisdomBack), new Color(.95f, .72f, .12f));
        else Button(col, T("VOLVER AL SUPER TOMO", "BACK TO SUPER TOME"), RenderSuperTome, new Color(.95f, .72f, .12f));
    }

    // ---------------- Reto del Super Sabio ----------------

    // Número de dominio/módulo (1..17) de un área de mazmorra; 0 si no aplica.
    int DomNum(string area)
    {
        var a = AreaDict(area);
        if (a != null && a.ContainsKey("dom")) return I(a["dom"]);
        return 0;
    }

    string MasterItemName(string dom)
    {
        switch (dom)
        {
            case "d1": return T("Medallón Maestro: Zero Trust", "Master Medallion: Zero Trust");
            case "d2": return T("Medallón Maestro: Guardián Purview", "Master Medallion: Purview Guardian");
            default: return T("Medallón Maestro: Arquitecto Copilot", "Master Medallion: Copilot Architect");
        }
    }

    Color MasterItemColor(string dom)
    {
        switch (dom)
        {
            case "d1": return new Color(.35f, .75f, 1f);
            case "d2": return new Color(.55f, 1f, .68f);
            default: return new Color(.82f, .62f, 1f);
        }
    }

    List<int> DomainQuestionIdxs(string dom)
    {
        if (dom == "exam")   // Sabio del Examen: el banco completo
        {
            var allq = L(data["bq"]);
            var ex = new List<int>();
            for (int i = 0; i < allq.Count; i++) ex.Add(i);
            return ex;
        }
        int dn = DomNum(dom);
        var all = L(data["bq"]);
        var idxs = new List<int>();
        for (int i = 0; i < all.Count; i++)
            if (I((all[i] as Dictionary<string, object>)["d"]) == dn) idxs.Add(i);
        return idxs;
    }

    // El Sabio del Examen aparece en la torre tras vencer al Rey Demonio: recorre TODO
    // el banco (63) en rondas de 2 (la última de 1); un fallo expulsa; premio: todos
    // los medallones maestros + trofeo "examen".
    void RenderExamSageIntro()
    {
        screen = "sage";
        sageDom = "exam";
        ClearRoot();
        PlayTrack("sage");
        AddBackdrop(BackdropForArea("final"), 0.45f);
        var p = Panel(root, "ExamSageIntro", new Color(.06f, .05f, .12f, .96f), Anchor.Stretch, new Vector2(30, 24), new Vector2(-30, -24));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .85f, .35f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 26);
        BigSprite(col, "npc_sabio", 132, new Color(1f, .85f, .35f));
        Label(col, T("Sabio del Examen AB-410", "AB-410 Exam Sage"), 26, new Color(1f, .85f, .35f), FontStyle.Bold);
        int n = L(data["bq"]).Count;
        if (save.trophies.Contains("examen"))
            Label(col, T("Ya conquistaste el examen completo. Pocos aspirantes pueden decir lo mismo. ¿Otra vuelta de honor?", "You already conquered the whole exam. Few challengers can say the same. Fancy a victory lap?"), 20, Color.white, FontStyle.Normal);
        else
            Label(col, T("Has vencido al Rey Demonio, pero el examen de verdad soy yo: las " + n + " preguntas del banco, en rondas de DOS a la vez (la última, de una). TODAS correctas. Un solo fallo y vuelves a la ciudad. Premio: TODOS los medallones maestros.",
                         "You defeated the Demon King, but I am the real exam: all " + n + " questions of the bank, in rounds of TWO at a time (the last one alone). ALL correct. A single miss sends you back to town. Reward: ALL master medallions."), 20, Color.white, FontStyle.Normal);
        Button(col, T("ACEPTO EL EXAMEN COMPLETO (" + n + " preguntas)", "I ACCEPT THE FULL EXAM (" + n + " questions)"), () => StartSageChallenge("exam"), new Color(.95f, .72f, .12f));
        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    void RenderSageIntro(string dom)
    {
        if (dom == "exam") { RenderExamSageIntro(); return; }
        screen = "sage";
        sageDom = dom;
        ClearRoot();
        PlayTrack("sage");
        AddBackdrop(BackdropForArea("town"), 0.4f);
        var p = Panel(root, "SageIntro", new Color(.06f, .05f, .12f, .96f), Anchor.Stretch, new Vector2(30, 24), new Vector2(-30, -24));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .85f, .35f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 26);
        BigSprite(col, "sabio_" + dom, 132, new Color(1f, .85f, .35f));
        Label(col, T("Super Sabio de ", "Super Sage of ") + AreaName(dom), 26, new Color(1f, .85f, .35f), FontStyle.Bold);
        int n = DomainQuestionIdxs(dom).Count;
        if (save.medallions.Contains(dom))
        {
            Label(col, T("Ya posees el " + MasterItemName(dom) + ". Tu maestria es indiscutible, joven aspirante.", "You already hold the " + MasterItemName(dom) + ". Your mastery is beyond question, young challenger."), 20, Color.white, FontStyle.Normal);
            DrawMedallion(col, dom, 150);
            Button(col, T("REPETIR EL DESAFIO", "RETRY THE CHALLENGE"), () => StartSageChallenge(dom), new Color(.95f, .72f, .12f));
        }
        else
        {
            Label(col, T("Para ganar el " + MasterItemName(dom) + " debes responder BIEN las " + n + " preguntas del dominio, una tras otra. Un solo fallo y regresas a la ciudad. ¿Estas listo?", "To earn the " + MasterItemName(dom) + " you must answer ALL " + n + " questions of this domain correctly, one after another. A single miss sends you back to town. Are you ready?"), 20, Color.white, FontStyle.Normal);
            Button(col, T("ACEPTO EL DESAFIO (" + n + " preguntas)", "I ACCEPT THE CHALLENGE (" + n + " questions)"), () => StartSageChallenge(dom), new Color(.95f, .72f, .12f));
        }
        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    void StartSageChallenge(string dom)
    {
        sageDom = dom;
        sageQueue = DomainQuestionIdxs(dom);
        for (int i = sageQueue.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = sageQueue[i]; sageQueue[i] = sageQueue[j]; sageQueue[j] = t; }
        sagePos = 0;
        RenderSageChallenge();
    }

    void RenderSageChallenge()
    {
        screen = "sage-challenge";
        uiDom = sageDom == "d1" ? 1 : sageDom == "d2" ? 2 : sageDom == "d3" ? 3 : 0;
        ClearRoot();
        PlayTrack("sage");
        AddBackdrop(BattleBackdrop(), 0.5f);
        var p = Panel(root, "SageChallenge", new Color(.04f, .05f, .12f, .96f), Anchor.Stretch, new Vector2(20, 16), new Vector2(-20, -16));
        var col = ScrollColumn(p, 10, 22);
        Label(col, (sageDom == "exam" ? T("EXAMEN COMPLETO · ", "FULL EXAM · ") : T("RETO DEL SABIO · ", "SAGE CHALLENGE · ")) + (sagePos + 1) + "/" + sageQueue.Count, 22, new Color(1f, .85f, .35f), FontStyle.Bold);
        Label(col, T("100% o vuelves a la ciudad. Sin fallos.", "100% or back to town. No mistakes."), 15, new Color(1f, .7f, .5f), FontStyle.Bold);

        if (sageDom == "exam")
        {
            // Rondas de 2 preguntas (la última de 1) con un único CONFIRMAR.
            int n = Math.Min(2, sageQueue.Count - sagePos);
            var qs = new List<Dictionary<string, object>>();
            var keys = new List<string>();
            var evals = new List<Func<bool?>>();
            for (int i = 0; i < n; i++)
            {
                int qi = sageQueue[sagePos + i];
                var qq = L(data["bq"])[qi] as Dictionary<string, object>;
                qs.Add(qq); keys.Add("bq:" + qi); MarkSeen("bq:" + qi);
                Label(col, "━━━  " + T("PREGUNTA", "QUESTION") + " " + (sagePos + i + 1) + " / " + sageQueue.Count + "  ━━━", 17, new Color(.6f, .9f, 1f), FontStyle.Bold);
                evals.Add(BuildQuestionBody(col, qq));
            }
            var warn = Label(col, " ", 15, new Color(1f, .65f, .45f), FontStyle.Bold);
            bool done = false;
            OptButton(col, T("✔  CONFIRMAR RONDA", "✔  CONFIRM ROUND"), () =>
            {
                if (done) return;
                var res = new List<bool?>();
                foreach (var ev in evals) res.Add(ev());
                for (int i = 0; i < res.Count; i++)
                    if (res[i] == null) { warn.text = T("⚠ Completa la pregunta " + (sagePos + i + 1) + " antes de confirmar.", "⚠ Complete question " + (sagePos + i + 1) + " before confirming."); return; }
                done = true;
                var ok = new List<bool>();
                foreach (var r in res) ok.Add(r.Value);
                ResolveExamRound(qs, keys, ok);
            }, new Color(.78f, .22f, .14f), 0, 82);
            Button(col, T("ABANDONAR", "GIVE UP"), () => RenderWorld(T("Dejaste el examen del sabio.", "You abandoned the sage's exam.")), new Color(.35f, .16f, .18f));
            return;
        }

        int idx = sageQueue[sagePos];
        var q = L(data["bq"])[idx] as Dictionary<string, object>;
        string key = "bq:" + idx;
        MarkSeen(key);
        BuildQuestionWidget(col, q, T("⚔  ¡ATACAR!", "⚔  ATTACK!"), correct => ResolveSage(key, correct, QS(q, "why")));
        Button(col, T("ABANDONAR", "GIVE UP"), () => RenderWorld(T("Dejaste el reto del sabio.", "You abandoned the sage challenge.")), new Color(.35f, .16f, .18f));
    }

    void ResolveExamRound(List<Dictionary<string, object>> qs, List<string> keys, List<bool> ok)
    {
        string failWhy = null;
        for (int i = 0; i < qs.Count; i++)
        {
            if (ok[i]) MarkCorrect(keys[i]);
            else
            {
                if (!save.wrongQ.Contains(keys[i])) save.wrongQ.Add(keys[i]);
                BumpWrong(keys[i]);
                if (failWhy == null) failWhy = QS(qs[i], "why");
            }
        }
        if (failWhy != null)
        {
            PlaySfx(sfxWrong);
            SaveSlot();
            RenderSageFail(failWhy);
            return;
        }
        PlaySfx(sfxRight);
        sagePos += qs.Count;
        SaveSlot();
        if (sagePos >= sageQueue.Count) { RenderSageVictory(); return; }
        RenderSageChallenge();
    }

    void ResolveSage(string key, bool correct, string why)
    {
        if (!correct)
        {
            PlaySfx(sfxWrong);
            if (!save.wrongQ.Contains(key)) save.wrongQ.Add(key);
            BumpWrong(key);
            SaveSlot();
            RenderSageFail(why);
            return;
        }
        PlaySfx(sfxRight);
        MarkCorrect(key);
        sagePos++;
        if (sagePos >= sageQueue.Count) { RenderSageVictory(); return; }
        RenderSageChallenge();
    }

    void RenderSageFail(string why)
    {
        screen = "sage-fail";
        ClearRoot();
        save.hp = Math.Max(1, save.maxhp / 2);
        save.area = "town"; save.x = 10; save.y = 10;
        combo = 0; towerDmgBonus = 0;
        SaveSlot();
        PlayTrack("town");
        AddBackdrop(BackdropForArea("town"), 0.4f);
        var p = Panel(root, "SageFail", new Color(.12f, .04f, .05f, .96f), Anchor.Stretch, new Vector2(34, 28), new Vector2(-34, -28));
        var col = ScrollColumn(p, 14, 28);
        Label(col, T("DESAFIO FALLIDO", "CHALLENGE FAILED"), 30, new Color(1f, .45f, .4f), FontStyle.Bold);
        Label(col, T("Una respuesta incorrecta rompe el reto. El sabio te devuelve a la ciudad. ", "One wrong answer breaks the challenge. The sage sends you back to town. ") + why, 20, Color.white, FontStyle.Normal);
        Label(col, T("Llegaste a " + sagePos + "/" + sageQueue.Count + ". Repasa y vuelve a intentarlo.", "You reached " + sagePos + "/" + sageQueue.Count + ". Review and try again."), 17, new Color(1f, .8f, .5f), FontStyle.Bold);
        Button(col, T("VOLVER A LA CIUDAD", "BACK TO TOWN"), () => RenderWorld(T("El sabio espera tu regreso.", "The sage awaits your return.")), new Color(.95f, .72f, .12f));
    }

    void RenderSageVictory()
    {
        screen = "sage-victory";
        ClearRoot();
        PlaySfx(sfxWin);
        PlayTrack("sage");
        if (sageDom == "exam")
        {
            foreach (var d in new[] { "d1", "d2", "d3" })
                if (!save.medallions.Contains(d)) save.medallions.Add(d);
            if (!save.trophies.Contains("examen")) save.trophies.Add("examen");
            GainXp(300);
            SaveSlot();
            AddBackdrop(BackdropForArea("final"), 0.5f);
            var pe = Panel(root, "ExamVictory", new Color(.05f, .08f, .12f, .96f), Anchor.Stretch, new Vector2(30, 24), new Vector2(-30, -24));
            var ole = pe.gameObject.AddComponent<Outline>(); ole.effectColor = new Color(1f, .85f, .35f, .7f); ole.effectDistance = new Vector2(3, -3);
            var cole = ScrollColumn(pe, 14, 26);
            Label(cole, T("¡EXAMEN AB-410 SUPERADO!", "AB-410 EXAM PASSED!"), 32, new Color(1f, .85f, .35f), FontStyle.Bold);
            Label(cole, T(sageQueue.Count + " preguntas. Cero fallos. El Sabio se inclina ante ti.", sageQueue.Count + " questions. Zero misses. The Sage bows before you."), 20, Color.white, FontStyle.Bold);
            DrawMedallion(cole, "d1", 120); DrawMedallion(cole, "d2", 120); DrawMedallion(cole, "d3", 120);
            Label(cole, T("Obtienes TODOS los medallones maestros y el trofeo Conquistador del Examen. +300 XP.", "You earn ALL master medallions and the Exam Conqueror trophy. +300 XP."), 19, Color.white, FontStyle.Normal);
            Button(cole, T("VOLVER", "BACK"), () => RenderWorld(T("Estás más que listo para el AB-410 real.", "You are more than ready for the real AB-410.")), new Color(.95f, .72f, .12f));
            return;
        }
        if (!save.medallions.Contains(sageDom)) save.medallions.Add(sageDom);
        GainXp(120);
        SaveSlot();
        AddBackdrop(BackdropForArea("town"), 0.45f);
        var p = Panel(root, "SageVictory", new Color(.05f, .08f, .12f, .96f), Anchor.Stretch, new Vector2(30, 24), new Vector2(-30, -24));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .85f, .35f, .7f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 14, 26);
        Label(col, T("¡MAESTRIA LOGRADA!", "MASTERY ACHIEVED!"), 32, new Color(1f, .85f, .35f), FontStyle.Bold);
        DrawMedallion(col, sageDom, 170);
        Label(col, T("Obtuviste el " + MasterItemName(sageDom) + ". 100% del dominio, sin un solo fallo. +120 XP.", "You earned the " + MasterItemName(sageDom) + ". 100% of the domain without a single miss. +120 XP."), 20, Color.white, FontStyle.Normal);
        Button(col, T("VOLVER A LA CIUDAD", "BACK TO TOWN"), () => RenderWorld(T("¡" + MasterItemName(sageDom) + " conseguido!", MasterItemName(sageDom) + " obtained!")), new Color(.95f, .72f, .12f));
    }

    void DrawMedallion(RectTransform parent, string dom, int size)
    {
        var holder = Panel(parent, "Medallion", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
        var le = holder.gameObject.AddComponent<LayoutElement>(); le.minHeight = size + 16; le.preferredHeight = size + 16; le.flexibleWidth = 1;
        Horizontal(holder, 8, TextAnchor.MiddleCenter);
        FramedSprite(holder, DomainIcon(dom), size, MasterItemColor(dom));
    }

    // ---------------- Widget de pregunta estilo examen Microsoft ----------------
    // Tipos (campo "t" del JSON; si falta = "single", compatible con los checks de tomo):
    //   single = radio (elige 1) · multi = checkbox "elige k" (las k primeras opciones
    //   del JSON son las correctas) · yesno = Sí/No por afirmación · drop = desplegables
    //   que completan frases. La respuesta se confirma con un botón submit (¡ATACAR!).
    // ---- Tema visual por dominio/mazmorra (1 azul acero · 2 verde · 3 violeta · 0 final rojo).
    // Tiñe paneles, botones de opción, acentos y bordes para que el combate "cambie" según la zona.
    int uiDom = 1;   // dominio activo; lo fijan RenderBattle / RenderQuestion / retos de sabio.
    static readonly Color[] AccentByDom =
    {
        new Color(1f,   .52f, .42f),   // 0 final / neutro: rojo ascua
        new Color(.45f, .78f, 1f),     // 1 azul acero
        new Color(.42f, 1f,   .62f),   // 2 verde musgo
        new Color(.80f, .56f, 1f),     // 3 violeta tecno
    };
    Color Accent(int dom) => AccentByDom[(dom >= 1 && dom <= 3) ? dom : 0];
    Color AccentC => Accent(uiDom);
    Color ThemePanelBg { get { var a = AccentC; return new Color(a.r * .06f + .03f, a.g * .06f + .03f, a.b * .06f + .05f, .95f); } }
    Color ThemeChipBg { get { var a = AccentC; return new Color(a.r * .18f + .04f, a.g * .18f + .04f, a.b * .18f + .07f, 1f); } }
    // Colores de los botones de respuesta, derivados del acento del dominio activo.
    Color OptBase { get { var a = AccentC; return new Color(a.r * .17f + .06f, a.g * .17f + .06f, a.b * .17f + .11f, 1f); } }
    Color OptSel { get { var a = AccentC; return new Color(a.r * .62f + .07f, a.g * .62f + .07f, a.b * .62f + .07f, 1f); } }
    Color OptDark { get { var a = AccentC; return new Color(a.r * .10f + .04f, a.g * .10f + .04f, a.b * .10f + .08f, 1f); } }

    static string QType(Dictionary<string, object> q) => q.ContainsKey("t") ? S(q["t"]) : "single";

    void BuildQuestionWidget(RectTransform col, Dictionary<string, object> q, string attackLabel, Action<bool> onSubmit)
    {
        var evaluate = BuildQuestionBody(col, q);
        var warn = Label(col, " ", 15, new Color(1f, .65f, .45f), FontStyle.Bold);
        bool done = false;
        OptButton(col, attackLabel, () =>
        {
            if (done) return;
            var res = evaluate();
            if (res == null) { warn.text = IncompleteWarn(q); return; }
            done = true;
            onSubmit(res.Value);
        }, new Color(.78f, .22f, .14f), 0, 82);
    }

    string IncompleteWarn(Dictionary<string, object> q)
    {
        string t = QType(q);
        return t == "multi" ? T("⚠ Marca exactamente " + I(q["k"]) + " casillas antes de atacar.", "⚠ Check exactly " + I(q["k"]) + " boxes before attacking.")
             : t == "yesno" ? T("⚠ Marca Sí o No en todas las afirmaciones.", "⚠ Select Yes or No for every statement.")
             : t == "drop" ? T("⚠ Elige una opción en cada desplegable.", "⚠ Pick an option in every drop-down.")
             : T("⚠ Elige una opción antes de atacar.", "⚠ Choose an option before attacking.");
    }

    // Pinta pista + enunciado + área de respuesta de una pregunta y devuelve su evaluador
    // (null = incompleta, true/false = correcta o no). El botón de envío lo pone el llamador,
    // así el combate en trío puede juntar 3 preguntas bajo un único ¡ATACAR!.
    Func<bool?> BuildQuestionBody(RectTransform col, Dictionary<string, object> q)
    {
        string t = QType(q);
        string hint = t == "multi" ? T("VARIAS RESPUESTAS · marca " + I(q["k"]) + " casillas", "MULTIPLE ANSWERS · check " + I(q["k"]) + " boxes")
                    : t == "yesno" ? T("MARCA SÍ O NO EN CADA AFIRMACIÓN", "SELECT YES OR NO FOR EACH STATEMENT")
                    : t == "drop" ? T("COMPLETA LA FRASE CON LOS DESPLEGABLES", "COMPLETE THE SENTENCE WITH THE DROP-DOWNS")
                    : T("ELIGE UNA OPCIÓN", "CHOOSE ONE OPTION");
        Chip(col, "◆ " + hint, new Color(0f, 0f, 0f, .32f), AccentC, 14, 34);
        var qt = Label(col, QS(q, "q"), 19, Color.white, FontStyle.Bold);
        qt.alignment = TextAnchor.UpperLeft;

        fiftyFifty = null;      // solo las preguntas single habilitan el pergamino (50/50)
        Func<bool?> evaluate;   // null = respuesta incompleta

        if (t == "yesno")
        {
            var rows = L(q["rows"]);
            int n = rows.Count;
            var sel = new int[n]; for (int i = 0; i < n; i++) sel[i] = -1;
            var yesBtns = new GameObject[n];
            var noBtns = new GameObject[n];
            string yesTxt = T("SÍ", "YES"), noTxt = T("NO", "NO");
            for (int r = 0; r < n; r++)
            {
                int rr = r;
                var rd = rows[r] as Dictionary<string, object>;
                var st = Label(col, (r + 1) + ". " + QS(rd, "s"), 17, new Color(.92f, .95f, 1f), FontStyle.Normal);
                st.alignment = TextAnchor.UpperLeft;
                var rowP = Panel(col, "YN" + r, Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
                var le = rowP.gameObject.AddComponent<LayoutElement>(); le.minHeight = 74; le.flexibleWidth = 1;
                Horizontal(rowP, 12, TextAnchor.MiddleCenter);
                yesBtns[rr] = OptButton(rowP, yesTxt, () =>
                {
                    sel[rr] = 1;
                    RestyleBtn(yesBtns[rr], "●  " + yesTxt, OptSel);
                    RestyleBtn(noBtns[rr], noTxt, OptBase);
                }, OptBase, 180, 58);
                noBtns[rr] = OptButton(rowP, noTxt, () =>
                {
                    sel[rr] = 0;
                    RestyleBtn(noBtns[rr], "●  " + noTxt, OptSel);
                    RestyleBtn(yesBtns[rr], yesTxt, OptBase);
                }, OptBase, 180, 58);
            }
            evaluate = () =>
            {
                for (int i = 0; i < n; i++) if (sel[i] < 0) return null;
                for (int i = 0; i < n; i++)
                    if (B((rows[i] as Dictionary<string, object>)["v"]) != (sel[i] == 1)) return false;
                return true;
            };
        }
        else if (t == "drop")
        {
            var drops = L(q["drops"]);
            int n = drops.Count;
            var sel = new int[n]; for (int i = 0; i < n; i++) sel[i] = -1;
            var comboBtns = new GameObject[n];
            var optLists = new List<GameObject>[n];
            for (int dI = 0; dI < n; dI++)
            {
                int di = dI;
                var dd = drops[dI] as Dictionary<string, object>;
                string pre = QS(dd, "pre");
                if (pre.Length > 0)
                {
                    var pl = Label(col, pre, 17, new Color(.92f, .95f, 1f), FontStyle.Normal);
                    pl.alignment = TextAnchor.UpperLeft;
                }
                optLists[di] = new List<GameObject>();
                comboBtns[di] = OptButton(col, T("▼   — elegir opción —", "▼   — choose an option —"), () =>
                {
                    bool show = !optLists[di][0].activeSelf;
                    for (int o = 0; o < n; o++) foreach (var g in optLists[o]) g.SetActive(false);
                    if (show) foreach (var g in optLists[di]) g.SetActive(true);
                }, OptDark, 0, 66);
                var dopts = QL(dd, "o");
                // Barajamos el ORDEN VISIBLE de las opciones del desplegable (la correcta no debe
                // caer siempre primera). 'sel' guarda el índice ORIGINAL, así que la evaluación
                // contra 'a' sigue siendo válida sin tocar los datos.
                int oc = dopts.Count;
                var perm = new int[oc]; for (int k = 0; k < oc; k++) perm[k] = k;
                for (int k = oc - 1; k > 0; k--) { int j = rng.Next(k + 1); int tmp = perm[k]; perm[k] = perm[j]; perm[j] = tmp; }
                for (int oI = 0; oI < oc; oI++)
                {
                    int origIdx = perm[oI];   // índice real en o[] (la selección se guarda con este)
                    string optText = S(dopts[origIdx]);
                    var g = OptButton(col, "‣ " + optText, () =>
                    {
                        sel[di] = origIdx;
                        RestyleBtn(comboBtns[di], "▼   " + optText, OptSel);
                        foreach (var gg in optLists[di]) gg.SetActive(false);
                    }, OptBase, 0, 58);
                    g.SetActive(false);
                    optLists[di].Add(g);
                }
                string post = QS(dd, "post");
                if (post.Length > 0)
                {
                    var pl2 = Label(col, post, 17, new Color(.92f, .95f, 1f), FontStyle.Normal);
                    pl2.alignment = TextAnchor.UpperLeft;
                }
            }
            evaluate = () =>
            {
                for (int i = 0; i < n; i++) if (sel[i] < 0) return null;
                for (int i = 0; i < n; i++)
                    if (sel[i] != I((drops[i] as Dictionary<string, object>)["a"])) return false;
                return true;
            };
        }
        else  // single / multi: opciones barajadas
        {
            var opts = QL(q, "o");
            bool multi = t == "multi";
            int k = multi ? I(q["k"]) : 1;
            var order = new List<int>();
            for (int i = 0; i < opts.Count; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int tmp = order[i]; order[i] = order[j]; order[j] = tmp; }
            var selected = new HashSet<int>();           // índices de pantalla
            var btns = new GameObject[opts.Count];
            for (int i = 0; i < order.Count; i++)
            {
                int di = i;
                string txt = S(opts[order[i]]);
                btns[di] = OptButton(col, (multi ? "☐  " : "○  ") + txt, () =>
                {
                    if (multi)
                    {
                        if (selected.Contains(di)) { selected.Remove(di); RestyleBtn(btns[di], "☐  " + txt, OptBase); }
                        else { selected.Add(di); RestyleBtn(btns[di], "☑  " + txt, OptSel); }
                    }
                    else
                    {
                        selected.Clear(); selected.Add(di);
                        for (int b = 0; b < btns.Length; b++)
                            RestyleBtn(btns[b], (b == di ? "●  " : "○  ") + S(opts[order[b]]), b == di ? OptSel : OptBase);
                    }
                }, OptBase, 0, 66);
            }
            if (!multi)
            {
                int aIdx = I(q["a"]);
                fiftyFifty = () =>
                {
                    var wrong = new List<int>();
                    for (int di = 0; di < order.Count; di++) if (order[di] != aIdx) wrong.Add(di);
                    for (int i = wrong.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int tmp = wrong[i]; wrong[i] = wrong[j]; wrong[j] = tmp; }
                    int marked = 0;
                    foreach (int di in wrong)
                    {
                        if (marked >= 2) break;
                        selected.Remove(di);
                        var b = btns[di];
                        b.GetComponent<Image>().color = new Color(.6f, .12f, .12f);
                        var bt = b.GetComponent<Button>(); if (bt != null) bt.interactable = false;
                        var tx = b.GetComponentInChildren<Text>(); tx.text = "✗  " + S(opts[order[di]]); tx.color = new Color(1f, .75f, .75f);
                        marked++;
                    }
                };
            }
            evaluate = () =>
            {
                if (multi && selected.Count != k) return null;
                if (!multi && selected.Count == 0) return null;
                if (multi)
                {
                    foreach (int di in selected) if (order[di] >= k) return false;   // correctas = k primeras del JSON
                    return true;
                }
                foreach (int di in selected) return order[di] == I(q["a"]);
                return false;
            };
        }

        return evaluate;
    }

    GameObject OptButton(RectTransform parent, string text, Action action, Color color, int width = 0, int minH = 74)
    {
        var go = new GameObject("Opt", typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(() => action());
        var le = go.AddComponent<LayoutElement>(); le.minHeight = minH; le.flexibleWidth = width == 0 ? 1 : 0; if (width > 0) le.minWidth = width;
        int fs = text.Length > 28 ? 13 : (text.Length > 18 ? 16 : 20);
        Label(go.GetComponent<RectTransform>(), text, fs, Color.white, FontStyle.Bold, Anchor.Stretch, new Vector2(8, 6), new Vector2(-8, -6));
        return go;
    }

    void RestyleBtn(GameObject go, string text, Color color)
    {
        go.GetComponent<Image>().color = color;
        var t = go.GetComponentInChildren<Text>();
        t.text = text;
        t.fontSize = text.Length > 28 ? 13 : (text.Length > 18 ? 16 : 20);
    }

    // Texto de la respuesta correcta de una pregunta, para el "Pergamino de sabiduría".
    string CorrectAnswerText(Dictionary<string, object> q)
    {
        string t = QType(q);
        if (t == "multi")
        {
            int k = I(q["k"]); var o = QL(q, "o");
            var parts = new List<string>();
            for (int i = 0; i < k && i < o.Count; i++) parts.Add(S(o[i]));
            return string.Join("  +  ", parts);
        }
        if (t == "yesno")
        {
            var parts = new List<string>();
            foreach (var r in L(q["rows"]))
            {
                var rd = r as Dictionary<string, object>;
                parts.Add((B(rd["v"]) ? T("SÍ → ", "YES → ") : T("NO → ", "NO → ")) + QS(rd, "s"));
            }
            return string.Join("\n", parts);
        }
        if (t == "drop")
        {
            var parts = new List<string>();
            foreach (var dr in L(q["drops"]))
            {
                var dd = dr as Dictionary<string, object>;
                parts.Add(S(QL(dd, "o")[I(dd["a"])]));
            }
            return string.Join("  ·  ", parts);
        }
        return S(QL(q, "o")[I(q["a"])]);
    }

    // ---------------- Simulador de Examen AB-410 (NPC de ciudad) ----------------
    // Pool propio de 100 preguntas tipo caso; cada intento sortea 40 ESTRATIFICADAS por el
    // peso oficial del examen (Fundamentos 25-30% · Crear apps 25-30% · Lógica/IA 40-45%).
    // NO toca seenQ/wrongQ/qstats: es independiente del dashboard.
    void RenderExamIntro()
    {
        screen = "examintro";
        ClearRoot();
        PlayTrack("town");
        AddBackdrop(BackdropForArea("town"), 0.35f);
        var p = Panel(root, "ExamIntro", new Color(.05f, .05f, .10f, .96f), Anchor.Stretch, new Vector2(20, 16), new Vector2(-20, -16));
        var col = ScrollColumn(p, 14, 26);
        Label(col, T("SIMULADOR DE EXAMEN AB-410", "AB-410 EXAM SIMULATOR"), 28, new Color(1f, .82f, .3f), FontStyle.Bold);
        FramedSprite(col, "npc_examinadora", 110, new Color(1f, .82f, .3f));
        Label(col, T("Te haré 40 preguntas tipo CASO sorteadas de un banco de 100, repartidas según el PESO REAL del examen:",
                     "I'll ask you 40 random CASE questions from a pool of 100, weighted by the REAL exam:"), 17, Color.white, FontStyle.Normal);
        Label(col, T("• Fundamentos: 11   • Crear apps: 11   • Lógica y automatización: 18", "• Foundation: 11   • Create apps: 11   • Logic & automation: 18"), 16, new Color(.8f, .9f, 1f), FontStyle.Normal);
        Label(col, T("Al terminar verás tu PUNTUACIÓN estilo Microsoft (0-1000, aprobado 700) y el desglose por área. Este simulador NO afecta a tu progreso ni al panel de errores.",
                     "At the end you'll get a Microsoft-style SCORE (0-1000, pass 700) and a per-area breakdown. This simulator does NOT affect your progress or the dashboard."), 15, new Color(.85f, .88f, .95f), FontStyle.Normal);
        Button(col, T("📝  COMENZAR EXAMEN (40 preguntas)", "📝  START EXAM (40 questions)"), StartExam, new Color(.2f, .7f, .35f));
        Button(col, T("↩  AHORA NO", "↩  NOT NOW"), () => RenderWorld(""), new Color(.3f, .2f, .45f));
    }

    List<Dictionary<string, object>> SampleExam(List<Dictionary<string, object>> pool, int n)
    {
        var copy = new List<Dictionary<string, object>>(pool);
        for (int i = copy.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = copy[i]; copy[i] = copy[j]; copy[j] = t; }
        if (n > copy.Count) n = copy.Count;
        return copy.GetRange(0, n);
    }

    void StartExam()
    {
        if (examPool == null || examPool.Count == 0) { RenderWorld(T("El examen no está disponible.", "The exam is unavailable.")); return; }
        var byG = new Dictionary<int, List<Dictionary<string, object>>> { { 1, new List<Dictionary<string, object>>() }, { 2, new List<Dictionary<string, object>>() }, { 3, new List<Dictionary<string, object>>() } };
        foreach (var o in examPool)
        {
            var q = o as Dictionary<string, object>; if (q == null) continue;
            int g = q.ContainsKey("g") ? I(q["g"]) : 3; if (!byG.ContainsKey(g)) byG[g] = new List<Dictionary<string, object>>();
            byG[g].Add(q);
        }
        examSel = new List<Dictionary<string, object>>();
        int[] want = { 0, 11, 11, 18 };
        for (int g = 1; g <= 3; g++) examSel.AddRange(SampleExam(byG[g], want[g]));
        for (int i = examSel.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = examSel[i]; examSel[i] = examSel[j]; examSel[j] = t; }
        RenderExam();
    }

    void RenderExam()
    {
        screen = "exam";
        ClearRoot();
        var p = Panel(root, "Exam", new Color(.04f, .05f, .09f, .98f), Anchor.Stretch, new Vector2(16, 12), new Vector2(-16, -12));
        var col = ScrollColumn(p, 16, 24);
        Label(col, T("EXAMEN AB-410 · 40 PREGUNTAS", "AB-410 EXAM · 40 QUESTIONS"), 24, new Color(1f, .82f, .3f), FontStyle.Bold);
        Label(col, T("Marca tus respuestas y pulsa ENTREGAR al final. Desplázate para verlas todas.", "Answer all, then press SUBMIT at the bottom. Scroll to see them all."), 15, new Color(.82f, .88f, .95f), FontStyle.Normal);
        examEvals = new List<Func<bool?>>();
        for (int i = 0; i < examSel.Count; i++)
        {
            Chip(col, "— " + T("Pregunta", "Question") + " " + (i + 1) + " / " + examSel.Count + " —", new Color(0f, 0f, 0f, .32f), new Color(1f, .82f, .3f), 14, 34);
            examEvals.Add(BuildQuestionBody(col, examSel[i]));
        }
        Button(col, T("✔  ENTREGAR EXAMEN", "✔  SUBMIT EXAM"), GradeExam, new Color(.2f, .7f, .35f));
        Button(col, T("↩  ABANDONAR", "↩  QUIT"), () => RenderWorld(""), new Color(.4f, .2f, .25f));
    }

    void GradeExam()
    {
        examGC = new int[4]; examGT = new int[4]; examWrong = new List<Dictionary<string, object>>();
        int correct = 0;
        for (int i = 0; i < examEvals.Count; i++)
        {
            int g = examSel[i].ContainsKey("g") ? I(examSel[i]["g"]) : 3; if (g < 1 || g > 3) g = 3;
            examGT[g]++;
            bool ok = examEvals[i]() == true;
            if (ok) { correct++; examGC[g]++; } else examWrong.Add(examSel[i]);
        }
        examLastCorrect = correct; examLastTotal = examEvals.Count;
        RenderExamResult();
    }

    void RenderExamResult()
    {
        screen = "examresult";
        ClearRoot();
        AddBackdrop(BackdropForArea("town"), 0.35f);
        var p = Panel(root, "ExamResult", new Color(.05f, .06f, .12f, .97f), Anchor.Stretch, new Vector2(20, 16), new Vector2(-20, -16));
        var col = ScrollColumn(p, 14, 26);
        float pct = examLastTotal > 0 ? (float)examLastCorrect / examLastTotal : 0f;
        int scaled = Mathf.RoundToInt(pct * 1000f);
        bool pass = scaled >= 700;
        Label(col, pass ? T("✅  APROBADO", "✅  PASS") : T("❌  SUSPENDIDO", "❌  FAIL"), 32, pass ? new Color(.4f, .9f, .5f) : new Color(1f, .45f, .4f), FontStyle.Bold);
        Label(col, T("Puntuación estimada: ", "Estimated score: ") + scaled + " / 1000   (" + T("aprobado", "pass") + " = 700)", 20, Color.white, FontStyle.Bold);
        Label(col, T("Aciertos: ", "Correct: ") + examLastCorrect + " / " + examLastTotal + "   (" + Mathf.RoundToInt(pct * 100f) + "%)", 17, new Color(.85f, .9f, 1f), FontStyle.Normal);
        string[] gn = { "", T("Fundamentos (25-30%)", "Foundation (25-30%)"), T("Crear apps inteligentes (25-30%)", "Create apps (25-30%)"), T("Lógica y automatización (40-45%)", "Logic & automation (40-45%)") };
        Label(col, T("Desglose por área del examen:", "Breakdown by exam area:"), 18, new Color(1f, .82f, .3f), FontStyle.Bold);
        for (int g = 1; g <= 3; g++)
        {
            int gp = examGT[g] > 0 ? Mathf.RoundToInt(100f * examGC[g] / examGT[g]) : 0;
            Label(col, "• " + gn[g] + ":  " + examGC[g] + "/" + examGT[g] + "  (" + gp + "%)", 16, gp >= 70 ? new Color(.7f, .95f, .75f) : new Color(1f, .8f, .55f), FontStyle.Normal);
        }
        Label(col, T("Nota: la puntuación es una ESTIMACIÓN; el examen real escala distinto. Úsalo para detectar huecos.",
                     "Note: the score is an ESTIMATE; the real exam scales differently. Use it to spot gaps."), 14, new Color(.8f, .82f, .9f), FontStyle.Italic);
        if (examWrong != null && examWrong.Count > 0)
            Button(col, T("🔎  REVISAR FALLOS (", "🔎  REVIEW MISSES (") + examWrong.Count + ")", RenderExamReview, new Color(.3f, .45f, .8f));
        Button(col, T("🔁  REPETIR (otras 40)", "🔁  RETRY (another 40)"), StartExam, new Color(.2f, .7f, .35f));
        Button(col, T("↩  VOLVER A LA CIUDAD", "↩  BACK TO TOWN"), () => RenderWorld(""), new Color(.3f, .2f, .45f));
    }

    void RenderExamReview()
    {
        screen = "examreview";
        ClearRoot();
        var p = Panel(root, "ExamReview", new Color(.04f, .05f, .09f, .98f), Anchor.Stretch, new Vector2(18, 14), new Vector2(-18, -14));
        var col = ScrollColumn(p, 14, 24);
        Label(col, T("REVISIÓN DE FALLOS", "MISSED QUESTIONS"), 24, new Color(1f, .55f, .45f), FontStyle.Bold);
        if (examWrong == null || examWrong.Count == 0)
            Label(col, T("¡Sin fallos! Pleno.", "No misses! Perfect."), 18, new Color(.7f, .95f, .75f), FontStyle.Bold);
        for (int i = 0; examWrong != null && i < examWrong.Count; i++)
        {
            var q = examWrong[i];
            Chip(col, "— " + (i + 1) + " —", new Color(0f, 0f, 0f, .3f), new Color(1f, .55f, .45f), 13, 30);
            var qt = Label(col, QS(q, "q"), 16, Color.white, FontStyle.Bold); qt.alignment = TextAnchor.UpperLeft;
            var ct = Label(col, T("Correcta: ", "Correct: ") + CorrectAnswerText(q), 15, new Color(.7f, .95f, .75f), FontStyle.Bold); ct.alignment = TextAnchor.UpperLeft;
            var wt = Label(col, QS(q, "why"), 14, new Color(.82f, .86f, .95f), FontStyle.Normal); wt.alignment = TextAnchor.UpperLeft;
        }
        Button(col, T("↩  VOLVER AL RESULTADO", "↩  BACK TO RESULT"), RenderExamResult, new Color(.3f, .2f, .45f));
    }

    // ---------------- Dashboard: dónde fallas más ----------------
    void RenderDashboard()
    {
        screen = "dashboard";
        ClearRoot();
        PlayTrack("town");
        AddBackdrop(BackdropForArea("town"), 0.35f);
        var p = Panel(root, "Dashboard", new Color(.04f, .06f, .12f, .96f), Anchor.Stretch, new Vector2(20, 16), new Vector2(-20, -16));
        var col = ScrollColumn(p, 12, 24);
        Label(col, T("DONDE FALLAS MAS", "WHERE YOU MISS MOST"), 28, new Color(.6f, .9f, 1f), FontStyle.Bold);
        FramedSprite(col, "npc_dashboard", 110, new Color(.6f, .9f, 1f));

        int totalWrong = 0; foreach (var s in save.qstats) totalWrong += s.w;
        Label(col, T("Preguntas distintas vistas: " + save.seenQ.Count + " · Fallos acumulados: " + totalWrong, "Distinct questions seen: " + save.seenQ.Count + " · Total misses: " + totalWrong), 17, Color.white, FontStyle.Normal);

        var all = L(data["bq"]);
        for (int part = 1; part <= 4; part++)
        {
            string dom = "p" + part;
            int totalDom = 0, seenDom = 0, wrongDom = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (PartOfDom(I((all[i] as Dictionary<string, object>)["d"])) != part) continue;
                totalDom++;
                string k = "bq:" + i;
                if (save.seenQ.Contains(k)) seenDom++;
                wrongDom += WrongCount(k);
            }
            Label(col, DomainTitle(dom), 18, new Color(.55f, .8f, 1f), FontStyle.Bold);
            HpBar(col, T("Cobertura ", "Coverage ") + seenDom + "/" + totalDom, seenDom, Math.Max(1, totalDom), new Color(.35f, .75f, 1f));
            Label(col, T("Fallos en esta parte: ", "Misses in this part: ") + wrongDom, 15, new Color(1f, .7f, .6f), FontStyle.Bold);
        }

        var sorted = new List<QStat>(save.qstats);
        sorted.Sort((x, y) => y.w.CompareTo(x.w));
        Label(col, T("Tus preguntas mas falladas", "Your most missed questions"), 20, new Color(1f, .82f, .22f), FontStyle.Bold);
        int shown = 0;
        foreach (var s in sorted)
        {
            if (s.w <= 0) continue;
            var q = QuestionByKey(s.k);
            if (q == null) continue;
            Label(col, "✗" + s.w + "  " + QS(q, "q"), 16, new Color(1f, .7f, .6f), FontStyle.Bold);
            Label(col, T("✔ Respuesta: ", "✔ Answer: ") + CorrectAnswerText(q), 16, new Color(.55f, 1f, .72f), FontStyle.Bold);
            Label(col, "→ " + QS(q, "why"), 15, new Color(.8f, .9f, 1f), FontStyle.Normal);
            if (++shown >= 8) break;
        }
        if (shown == 0) Label(col, T("¡Aun no tienes fallos registrados! Sigue jugando para descubrir tus puntos debiles.", "No misses recorded yet! Keep playing to uncover your weak spots."), 17, new Color(.55f, 1f, .68f), FontStyle.Normal);

        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    void StartBattle(Dictionary<string, object> thing, bool boss)
    {
        if (boss && save.area != "final" && ReadTomesInArea() < TotalTomesInArea())
        {
            RenderWorld(T("El jefe esta sellado hasta leer todos los tomos.", "The boss is sealed until you read every tome."));
            return;
        }
        if (S(thing, "kind") == "guardian")
        {
            int gpart = thing.ContainsKey("part") ? I(thing["part"]) : I(thing["dom"]);
            string gspr = thing.ContainsKey("spr") ? S(thing, "spr") : "guard_d" + (((gpart - 1) % 3) + 1);
            battle = new Battle { thingKey = ThingKey(thing), enemy = thing, hp = I(thing["hp"]), maxhp = I(thing["hp"]), dom = 0, part = gpart, spr = gspr, qn = 3 };
            RenderBattle(T("¡Un Guardián del Examen te cierra el paso! Domina TODA su parte.", "An Exam Guardian blocks your path! Master its WHOLE part."));
            return;
        }
        if (boss && save.area == "final" && GuardiansLeft() > 0)
        {
            RenderWorld(T("Los Guardianes del Examen sellan la sala: aún quedan " + GuardiansLeft() + " en pie.", "The Exam Guardians seal the chamber: " + GuardiansLeft() + " still stand."));
            return;
        }
        var zi = I(thing["zi"]);
        var zone = L(data["zones"])[zi] as Dictionary<string, object>;
        var enemy = boss ? D(zone["boss"]) : L(zone["enemies"])[I(thing["ei"])] as Dictionary<string, object>;
        int ehp = boss ? I(enemy["hp"]) : ScaledEnemyHp(I(enemy["hp"]));   // los enemigos escalan con tu nivel
        battle = new Battle { thingKey = ThingKey(thing), enemy = enemy, hp = ehp, maxhp = ehp, dom = I(zone["dom"]), spr = SpriteForEnemyName(S(enemy, "n")), qn = !boss ? 1 : (save.area == "final" ? 3 : 2) };
        RenderBattle(T("Combate iniciado. Responde para atacar.", "Battle started. Answer to attack."));
    }

    int GuardiansLeft()
    {
        int n = 0;
        foreach (var t in Things(save.area)) if (S(t, "kind") == "guardian" && !Gone(t)) n++;
        return n;
    }

    void RenderBattle(string log)
    {
        screen = "battle";
        ClearRoot();
        bool boss = battle.thingKey.Contains("|boss|") || battle.thingKey.Contains("|guardian|") || battle.thingKey == "rematch";
        uiDom = battle != null ? battle.dom : 1;   // tema de la mazmorra/dominio del combate
        var accent = AccentC;
        // El Gran Sabio (piso 18) tiene su propia pista épica/aterradora; sus 4 guardianes
        // usan la pista de jefe; el resto, jefe normal o combate estándar.
        PlayTrack(BattleTrack(boss));
        AddBackdrop(BattleBackdrop(), 0.62f);
        var p = Panel(root, "Battle", ThemePanelBg, Anchor.Stretch, new Vector2(16, 12), new Vector2(-16, -12));
        var pol = p.gameObject.AddComponent<Outline>();
        pol.effectColor = new Color(accent.r, accent.g, accent.b, .6f); pol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 9, 16);

        // --- Banner del enemigo: retrato enmarcado + nombre + chip de dominio ---
        FramedSprite(col, battle.spr, boss ? 128 : 104, accent);
        Label(col, TS(battle.enemy, "n"), boss ? 26 : 23, accent, FontStyle.Bold);
        Chip(col, "▦  " + DomainBattleTag(battle.dom), ThemeChipBg, accent, 14, 36);
        HpBar(col, "☠ " + T("Enemigo", "Enemy"), battle.hp, battle.maxhp, new Color(.88f, .26f, .30f));
        HpBar(col, "❤ " + T("Tú", "You"), save.hp, save.maxhp, new Color(.34f, .84f, .45f));
        if (combo > 0)
        {
            int nextHit = BattleBaseDamage() + combo;
            Chip(col, T("🔥 Racha " + combo + " · próximo golpe " + nextHit + " HP", "🔥 Streak " + combo + " · next hit " + nextHit + " HP"), new Color(.36f, .2f, .05f, 1f), new Color(1f, .73f, .32f), 15, 38);
        }
        if (battle.locked)
            Chip(col, T("★ FASE FINAL: remata con tus falladas (" + ScopedWrongCount() + ")", "★ FINAL PHASE: finish it with your missed (" + ScopedWrongCount() + ")"), new Color(.34f, .26f, .05f, 1f), new Color(1f, .85f, .32f), 15, 40);
        if (!string.IsNullOrEmpty(log) && log.Trim().Length > 0)
        {
            var lg = Label(col, log, 15, new Color(.86f, .92f, 1f), FontStyle.Italic);
            lg.alignment = TextAnchor.UpperCenter;
        }
        // Ronda MÚLTIPLE: jefes (2/3), súper ataque del jefe final (7-10) y/o HABILIDAD del
        // jugador (multiplica las preguntas de la ronda y el daño). La fase "locked" va de 1 en 1.
        if ((battle.qn > 1 || battle.skill) && !battle.locked)
        {
            // SÚPER ATAQUE del Rey Demonio (dom 0) con racha >= 2: lanza 7-10 preguntas base.
            bool superAtk = battle.dom == 0 && battle.thingKey.Contains("|boss|") && combo >= 2;
            int baseQn = superAtk ? Math.Min(10, 7 + rng.Next(4)) : (battle.varQn ? 4 + rng.Next(2) : battle.qn);
            int skillN = battle.skill ? battle.skillN : 1;
            int qn = Math.Min(30, baseQn * skillN);   // la habilidad multiplica las PREGUNTAS (cap: 30)
            // Multiplicador propio del JEFE FINAL por su nº de preguntas base: 7-8 → ×2, 9-10 → ×3.
            int bossMult = baseQn >= 9 ? 3 : (baseQn >= 7 ? 2 : 1);
            // Daño: la skill aporta su nivel fijo (×N) y SE MULTIPLICA con el del jefe final.
            int dmgMult = (battle.skill ? skillN : 1) * bossMult;
            battle.dmgMult = dmgMult;
            if (battle.skill)
                Chip(col, T("✦ " + SkillName(battle.skillIdx) + ": responde las " + qn + " · daño ×" + dmgMult + " · +" + (2 * skillN) + " racha",
                            "✦ " + SkillName(battle.skillIdx) + ": answer all " + qn + " · ×" + dmgMult + " dmg · +" + (2 * skillN) + " streak"),
                     new Color(.13f, .07f, .28f, 1f), new Color(.78f, .6f, 1f), 16, 48);
            else if (superAtk)
                Chip(col, T("☠ ¡SÚPER ATAQUE DEL EXAMEN! Responde las " + qn + " a la vez", "☠ EXAM SUPER ATTACK! Answer all " + qn + " at once"), new Color(.4f, .05f, .08f, 1f), new Color(1f, .5f, .4f), 16, 44);
            else
                Chip(col, T("⚜ RONDA DE " + qn + " · solo el pleno " + qn + "/" + qn + " hace daño", "⚜ ROUND OF " + qn + " · only a perfect " + qn + "/" + qn + " deals damage"), ThemeChipBg, accent, 15, 40);
            List<string> keys;
            var qs = battle.tower ? PickTowerQuestions(qn, out keys) : PickBattleQuestions(qn, out keys);
            var evals = new List<Func<bool?>>();
            for (int i = 0; i < qs.Count; i++)
            {
                Chip(col, "━  " + T("PREGUNTA", "QUESTION") + " " + (i + 1) + " / " + qs.Count + "  ━", new Color(0f, 0f, 0f, .35f), accent, 14, 32);
                evals.Add(BuildQuestionBody(col, qs[i]));
            }
            var warn = Label(col, " ", 15, new Color(1f, .65f, .45f), FontStyle.Bold);
            bool done = false;
            AttackButton(col, T("⚔  ¡ATACAR!", "⚔  ATTACK!"), () =>
            {
                if (done) return;
                var res = new List<bool?>();
                foreach (var ev in evals) res.Add(ev());
                for (int i = 0; i < res.Count; i++)
                    if (res[i] == null) { warn.text = T("⚠ Completa la pregunta " + (i + 1) + " antes de atacar.", "⚠ Complete question " + (i + 1) + " before attacking."); return; }
                done = true;
                var ok = new List<bool>();
                foreach (var r in res) ok.Add(r.Value);
                ResolveTrio(qs, keys, ok);
            });
            AddScrollButton(col, qs);
        }
        else
        {
            string key; var q = battle.tower ? PickTowerQuestion(out key) : PickBattleQuestion(out key);
            var evaluate = BuildQuestionBody(col, q);
            var warn = Label(col, " ", 15, new Color(1f, .65f, .45f), FontStyle.Bold);
            bool done = false;
            AttackButton(col, T("⚔  ¡ATACAR!", "⚔  ATTACK!"), () =>
            {
                if (done) return;
                var res = evaluate();
                if (res == null) { warn.text = IncompleteWarn(q); return; }
                done = true;
                ResolveBattle(q, key, res.Value);
            });
            AddScrollButton(col, new List<Dictionary<string, object>> { q });
        }
        // HABILIDADES (ahora también contra jefes): activar desde el menú o cancelar la ronda.
        if (!battle.locked)
        {
            if (battle.skill)
                Button(col, T("✕  Cancelar habilidad", "✕  Cancel skill"), () =>
                {
                    battle.skill = false; battle.skillN = 1;
                    RenderBattle(T("Cancelas la habilidad.", "You cancel the skill."));
                }, new Color(.28f, .2f, .34f));
            else if (SkillsUnlocked() > 0)
                Button(col, T("🎇  HABILIDADES (" + SkillsUnlocked() + ")", "🎇  SKILLS (" + SkillsUnlocked() + ")"), () => RenderSkills(), new Color(.30f, .18f, .42f));
        }
        // Torre del Estudio: se pueden usar POCIONES en combate (cura 50% de la vida máxima).
        if (battle.tower && ItemCount("pocion") > 0)
            Button(col, T("🧪  Usar poción (+50% vida) · tienes " + ItemCount("pocion"), "🧪  Use potion (+50% HP) · you have " + ItemCount("pocion")), () =>
            {
                if (ConsumeItem("pocion")) { save.hp = Math.Min(save.maxhp, save.hp + save.maxhp / 2); PlaySfx(sfxPotion); SaveSlot(); RenderBattle(T("Bebes una poción. Recuperas vida.", "You drink a potion. HP restored.")); }
            }, new Color(.24f, .5f, .3f));
        Button(col, T("HUIR", "FLEE"), () =>
        {
            chasers.Clear(); pendingLoot = null;   // respiro breve: volverán al siguiente paso si toca
            RenderWorld(T("Escapaste del combate.", "You fled the battle."));
        }, new Color(.32f, .15f, .17f));
    }

    // Pergamino de conocimiento: revela la respuesta correcta de la ronda actual (1 uso).
    void AddScrollButton(RectTransform col, List<Dictionary<string, object>> qs)
    {
        if (ItemCount("pergamino") <= 0) return;
        // El Pergamino SOLO funciona en preguntas de respuesta ÚNICA (single): descarta 2 opciones.
        if (qs.Count != 1 || QType(qs[0]) != "single" || fiftyFifty == null) return;
        var act = fiftyFifty;
        bool used = false;
        OptButton(col, "📜  " + T("PERGAMINO: descarta 2 incorrectas (" + ItemCount("pergamino") + ")", "SCROLL: rule out 2 wrong (" + ItemCount("pergamino") + ")"), () =>
        {
            if (used || !ConsumeItem("pergamino")) return;
            used = true;
            SaveSlot();
            act();   // marca en rojo 2 opciones incorrectas
        }, new Color(.24f, .42f, .3f), 0, 58);
    }

    IEnumerator RandomEncounterIntro(string log)
    {
        screen = "encounter";
        ClearRoot();
        PlayTrack(BattleTrack(false));
        AddBackdrop(BattleBackdrop(), 0.72f);
        var p = Panel(root, "EncounterAlert", new Color(.02f, .025f, .06f, .97f), Anchor.Stretch, new Vector2(18, 14), new Vector2(-18, -14));
        var col = ScrollColumn(p, 10, 22);
        Label(col, "!", 70, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, T("ENCUENTRO REPENTINO", "SUDDEN ENCOUNTER"), 24, new Color(1f, .82f, .22f), FontStyle.Bold);
        BigSprite(col, battle.spr, 136, new Color(.9f, .2f, .28f));
        Label(col, log, 21, Color.white, FontStyle.Bold);
        Label(col, T("PREPARATE", "GET READY"), 18, new Color(.65f, .86f, 1f), FontStyle.Bold);

        var flash = Panel(root, "EncounterFlash", new Color(1f, 1f, 1f, .18f), Anchor.Stretch, Vector2.zero, Vector2.zero);
        for (int i = 0; i < 6; i++)
        {
            flash.gameObject.SetActive(i % 2 == 0);
            yield return new WaitForSeconds(0.09f);
        }
        yield return new WaitForSeconds(0.25f);
        RenderBattle(log + T(" Responde para atacar.", " Answer to attack."));
    }

    void ResolveBattle(Dictionary<string, object> q, string key, bool correct)
    {
        if (screen == "battle-anim") return;            // evita doble respuesta durante la animación
        StartCoroutine(AnswerSeq(q, key, correct));
    }

    IEnumerator AnswerSeq(Dictionary<string, object> q, string key, bool correct)
    {
        screen = "battle-anim";
        bool isBoss = battle.thingKey.Contains("|boss|");
        if (correct)
        {
            PlaySfx(sfxRight);
            if (!battle.tower) { save.wrongQ.Remove(key); MarkCorrect(key); }   // refuerzo (la Torre no toca estadísticas del bq)
            int hit = BattleBaseDamage() + combo;   // racha: cada acierto pega 1 más
            combo++;
            yield return SlashAnim(ConceptOf(q));
            if (battle.locked)
            {
                if (ScopedWrongCount() == 0) { yield return DefeatedAnim(); Victory(); yield break; }
                SaveSlot();
                RenderBattle(T("¡Correcto! Te quedan " + ScopedWrongCount() + " preguntas por dominar.", "Correct! " + ScopedWrongCount() + " questions left to master."));
                yield break;
            }
            battle.hp -= hit;
            if (battle.hp <= 0)
            {
                if (isBoss && ScopedWrongCount() > 0)
                {
                    battle.hp = 1; battle.locked = true; SaveSlot();
                    RenderBattle(T("El jefe resiste con 1 HP. Responde bien tus " + ScopedWrongCount() + " preguntas falladas para rematarlo.", "The boss holds on with 1 HP. Answer your " + ScopedWrongCount() + " missed questions to finish it."));
                    yield break;
                }
                yield return DefeatedAnim();
                Victory();
                yield break;
            }
            SaveSlot();
            RenderBattle(T("¡Corte certero! -" + hit + " HP (racha " + combo + "). ", "Clean hit! -" + hit + " HP (streak " + combo + "). ") + QS(q, "why"));
        }
        else
        {
            PlaySfx(sfxWrong);
            combo = 0;   // la racha se rompe
            if (!battle.tower) { if (!save.wrongQ.Contains(key)) save.wrongQ.Add(key); BumpWrong(key); }
            int dmg = EnemyHitDamage();
            yield return EnemyAttackAnim(dmg);
            save.hp -= dmg;
            if (save.hp <= 0)
            {
                save.hp = Math.Max(1, save.maxhp / 2);
                save.area = "town"; save.x = 7; save.y = 8;
                chasers.Clear(); pendingLoot = null; combo = 0; towerDmgBonus = 0;
                SaveSlot();
                RenderWorld(T("Derrota. Guardé la pregunta para repasar. Despertaste en la ciudad con media vida.", "Defeat. I saved the question for review. You woke up in town with half your HP."));
                yield break;
            }
            SaveSlot();
            RenderBattle(T("Incorrecto (guardada para repasar). ", "Incorrect (saved for review). ") + QS(q, "why"));
        }
    }

    void ResolveTrio(List<Dictionary<string, object>> qs, List<string> keys, List<bool> ok)
    {
        if (screen == "battle-anim") return;            // evita doble respuesta durante la animación
        StartCoroutine(TrioAnswerSeq(qs, keys, ok));
    }

    // Ronda triple de guardianes/jefe final: las 3 preguntas puntúan en las estadísticas,
    // pero solo el pleno 3/3 hace daño (triple). La fase "locked" sigue siendo de 1 en 1.
    IEnumerator TrioAnswerSeq(List<Dictionary<string, object>> qs, List<string> keys, List<bool> ok)
    {
        screen = "battle-anim";
        // Si la ronda venía de una HABILIDAD del jugador, capturamos sus datos y la
        // consumimos: tras resolverla se vuelve a pregunta normal (hay que reactivarla).
        bool wasSkill = battle.skill;
        int skillN = battle.skillN;
        int skillIdx = battle.skillIdx;
        int mult = battle.dmgMult;   // ya calculado en RenderBattle (skill × jefe final)
        int skillCost = wasSkill ? SkillSpend(skillIdx) : 0;   // GASTA racha (×2→0, ×3→1, ×4→2…) antes de acumular
        if (wasSkill) { battle.skill = false; battle.skillN = 1; combo = Math.Max(0, combo - skillCost); }
        int wrong = 0;
        for (int i = 0; i < qs.Count; i++)
        {
            if (ok[i])
            {
                if (!battle.tower) { save.wrongQ.Remove(keys[i]); MarkCorrect(keys[i]); }
            }
            else
            {
                if (!battle.tower) { if (!save.wrongQ.Contains(keys[i])) save.wrongQ.Add(keys[i]); BumpWrong(keys[i]); }
                wrong++;
            }
        }
        if (wrong == 0)
        {
            PlaySfx(sfxRight);
            // Daño: la skill aporta su nivel fijo (×N) y se multiplica con el del jefe final
            // (7-8 → ×2, 9-10 → ×3). 'mult' ya viene calculado de RenderBattle (battle.dmgMult).
            int hit = (BattleBaseDamage() + combo) * mult;
            if (wasSkill) combo += 2 * skillN;   // BONUS de racha por usar habilidad (×2 → +4, ×3 → +6...)
            else combo++;                        // ronda normal/super: la racha sube de 1 en 1
            if (wasSkill) yield return SkillAnim(skillIdx, qs.Count);
            else yield return SlashAnim(ConceptOf(qs[0]));
            battle.hp -= hit;
            if (battle.hp <= 0)
            {
                if (ScopedWrongCount() > 0)
                {
                    battle.hp = 1; battle.locked = true; SaveSlot();
                    RenderBattle(T("Resiste con 1 HP. Responde bien tus " + ScopedWrongCount() + " preguntas falladas para rematarlo.", "It holds on with 1 HP. Answer your " + ScopedWrongCount() + " missed questions to finish it."));
                    yield break;
                }
                yield return DefeatedAnim();
                Victory();
                yield break;
            }
            SaveSlot();
            string multTxt = mult > 1 ? (" x" + mult) : "";
            if (wasSkill)
                RenderBattle(T("✦ ¡" + SkillName(skillIdx) + "! Las " + qs.Count + " correctas: -" + hit + " HP (×" + mult + ", racha " + combo + ").", "✦ " + SkillName(skillIdx) + "! All " + qs.Count + " correct: -" + hit + " HP (×" + mult + ", streak " + combo + ")."));
            else
                RenderBattle(T("¡PLENO! Las " + qs.Count + " respuestas correctas: -" + hit + " HP" + multTxt + " (racha " + combo + ").", "PERFECT! All " + qs.Count + " answers correct: -" + hit + " HP" + multTxt + " (streak " + combo + ")."));
        }
        else
        {
            PlaySfx(sfxWrong);
            combo = 0;   // la racha se rompe
            int dmg = EnemyHitDamage();
            yield return EnemyAttackAnim(dmg);
            save.hp -= dmg;
            if (save.hp <= 0)
            {
                save.hp = Math.Max(1, save.maxhp / 2);
                save.area = "town"; save.x = 7; save.y = 8;
                chasers.Clear(); pendingLoot = null; combo = 0; towerDmgBonus = 0;
                SaveSlot();
                RenderWorld(T("Derrota. Guardé tus preguntas falladas para repasar. Despertaste en la ciudad con media vida.", "Defeat. I saved your missed questions for review. You woke up in town with half your HP."));
                yield break;
            }
            SaveSlot();
            RenderBattle(T("Has fallado " + wrong + " de " + qs.Count + " (guardadas para repasar). Exige el pleno para recibir daño.", "You missed " + wrong + " of " + qs.Count + " (saved for review). It only takes damage from a perfect round."));
        }
    }

    // Conceptos de cada parte para el texto del ataque (en vez de los heredados).
    static readonly string[][] PART_CONCEPTS = {
        new[] { "IA", "Copilot", "Power Platform", "Agentes", "Prompts", "el Planificador" },
        new[] { "Dataverse", "Tablas", "Columnas", "Relaciones", "Seguridad", "Roles" },
        new[] { "Power Apps", "el Lienzo", "Modelo", "Formularios", "Power Pages", "el ALM" },
        new[] { "Power Automate", "Flujos", "el Conector", "Aprobaciones", "AI Builder" },
    };

    // Texto del ataque: aleatorio entre el TIPO de pregunta y un concepto de la PARTE repasada.
    string ConceptOf(Dictionary<string, object> q)
    {
        if (rng.Next(100) < 40)
        {
            switch (S(q, "t"))
            {
                case "multi": return T("Selección Múltiple", "Multiple Choice");
                case "yesno": return T("Sí o No", "Yes or No");
                case "drop": return T("Desplegables", "Dropdowns");
                default: return T("Opción Única", "Single Choice");
            }
        }
        var pool = PART_CONCEPTS[Mathf.Clamp(PartOfDom(q.ContainsKey("d") ? I(q["d"]) : 0) - 1, 0, 3)];
        return pool[rng.Next(pool.Length)];
    }

    // Animación de ataque básico: el PROTAGONISTA ejecuta el golpe (cut-in sobrio).
    // 5 efectos al azar (corte/magia/fuego/hielo/rayo) con su sprite attack_<fx>.
    IEnumerator SlashAnim(string concept)
    {
        int fx = rng.Next(5);   // 0 corte · 1 magia · 2 fuego · 3 hielo · 4 electricidad
        Color c; string verbEs, verbEn;
        switch (fx)
        {
            case 1: c = new Color(.72f, .45f, 1f); verbEs = "Magia"; verbEn = "Magic"; break;
            case 2: c = new Color(1f, .5f, .15f); verbEs = "Fuego"; verbEn = "Fire"; break;
            case 3: c = new Color(.5f, .85f, 1f); verbEs = "Hielo"; verbEn = "Ice"; break;
            case 4: c = new Color(1f, .95f, .3f); verbEs = "Rayo"; verbEn = "Bolt"; break;
            default: c = new Color(1f, 1f, 1f); verbEs = "Corte"; verbEn = "Slash"; break;
        }
        string baseKey = "attack_" + fx;
        var frames = AnimFrames(baseKey);   // [base_0, base_1, base_2] (animado) o null
        string single = LoadSprite(baseKey) != null ? baseKey
                       : (fx == 0 && LoadSprite("attack_0b") != null ? "attack_0b" : null);
        Sprite first = frames != null ? LoadSprite(frames[0]) : (single != null ? LoadSprite(single) : null);
        var ov = Panel(root, "Atk", new Color(0f, 0f, 0f, 0f), Anchor.Stretch, Vector2.zero, Vector2.zero);
        var flash = Panel(ov, "AtkFlash", new Color(c.r, c.g, c.b, .16f), Anchor.Stretch, Vector2.zero, Vector2.zero);  // destello suave (sobrio)
        Image hero = null;
        if (first != null)
        {
            var go = new GameObject("AtkHero", typeof(Image));
            go.transform.SetParent(ov, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            float h = 340f, w = h * first.rect.width / first.rect.height;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, 30f);
            hero = go.GetComponent<Image>(); hero.sprite = first; hero.preserveAspect = true; hero.raycastTarget = false;
        }
        else   // respaldo: barras de color como antes
        {
            int rays = fx == 0 ? 2 : (fx == 4 ? 7 : 5);
            for (int i = 0; i < rays; i++) SlashBar(flash, i * (180f / rays) + (fx == 4 ? rng.Next(20) : 0), 0, c);
        }
        var t = Label(ov, T("¡" + verbEs + " de " + concept + "!", concept + " " + verbEn + "!"), 26,
                      new Color(Mathf.Min(1f, c.r + .3f), Mathf.Min(1f, c.g + .3f), Mathf.Min(1f, c.b + .3f)), FontStyle.Bold,
                      Anchor.BottomStretch, new Vector2(20f, 36f), new Vector2(-20f, 86f));
        t.alignment = TextAnchor.MiddleCenter;
        var tol = t.gameObject.AddComponent<Outline>(); tol.effectColor = new Color(0f, 0f, 0f, .9f); tol.effectDistance = new Vector2(2f, -2f);
        if (hero != null && frames != null)   // carga → golpe → remate
        {
            int[] seq = { 0, 1, 1, 2 };
            foreach (int fr in seq) { var s = LoadSprite(frames[Mathf.Min(fr, frames.Count - 1)]); if (s != null) hero.sprite = s; yield return new WaitForSeconds(0.09f); }
            yield return new WaitForSeconds(0.22f);
        }
        else
        {
            for (int i = 0; i < 4; i++) { flash.gameObject.SetActive(i % 2 == 0); yield return new WaitForSeconds(0.06f); }
            flash.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.4f);
        }
        Destroy(ov.gameObject);
    }

    // Frames de animación de un cut-in (base_0, base_1, …). null si no hay base_0.
    List<string> AnimFrames(string baseKey)
    {
        if (LoadSprite(baseKey + "_0") == null) return null;
        var l = new List<string>();
        for (int i = 0; i < 8 && LoadSprite(baseKey + "_" + i) != null; i++) l.Add(baseKey + "_" + i);
        return l;
    }

    // Reproduce una animación de acción (carga→acción→remate) sobre un velo oscuro y luego
    // ejecuta onDone. Si no hay frames base_0/1/2, llama a onDone directamente (sin animación).
    IEnumerator PlayCutIn(string baseKey, Action onDone, float heroH = 360f)
    {
        PlaySfxForCutIn(baseKey);   // sonido característico de la animación (suena aunque falte el arte)
        var frames = AnimFrames(baseKey);
        var first = frames != null ? LoadSprite(frames[0]) : null;
        if (first == null) { if (onDone != null) onDone(); yield break; }
        var ov = Panel(root, "CutIn", new Color(0f, 0f, 0f, .55f), Anchor.Stretch, Vector2.zero, Vector2.zero);
        var go = new GameObject("CutHero", typeof(Image)); go.transform.SetParent(ov, false);
        var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
        float w = heroH * first.rect.width / first.rect.height; rt.sizeDelta = new Vector2(w, heroH);
        var im = go.GetComponent<Image>(); im.sprite = first; im.preserveAspect = true; im.raycastTarget = false;
        int[] seq = { 0, 0, 1, 1, 1, 2, 2 };
        foreach (int fr in seq) { var s = LoadSprite(frames[Mathf.Min(fr, frames.Count - 1)]); if (s != null) im.sprite = s; yield return new WaitForSeconds(0.085f); }
        yield return new WaitForSeconds(0.28f);
        Destroy(ov.gameObject);
        if (onDone != null) onDone();
    }

    void SlashBar(RectTransform parent, float angle, float yoff, Color? color = null)
    {
        var go = new GameObject("Bar", typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = new Vector2(1100, 14);
        rt.anchoredPosition = new Vector2(0, yoff);
        rt.localEulerAngles = new Vector3(0, 0, angle);
        var col = color ?? new Color(1f, 1f, 1f, .92f);
        if (col.a >= 1f) col.a = .92f;
        go.GetComponent<Image>().color = col;
    }

    // ===================== HABILIDADES DE COMBATE =====================
    // Una skill permanente cada 5 niveles. Transforma la pregunta del enemigo en N
    // preguntas; el pleno N/N hace daño ×N con animación propia. Solo vs ENEMIGOS.
    // idx 0..4 → N = idx+2 · desbloqueo en nivel (idx+1)*5 · fx de animación.
    static readonly string[] SKILL_ES = { "Doble Filo", "Tríada Rúnica", "Tormenta Tetra", "Quinta Sentencia", "Hexágono Astral" };
    static readonly string[] SKILL_EN = { "Double Edge", "Rune Triad", "Tetra Storm", "Fifth Verdict", "Astral Hexagon" };
    static readonly int[] SKILL_FX = { 0, 1, 4, 2, 3 };   // 0 corte · 1 magia · 4 rayo · 2 fuego · 3 hielo

    int SkillsUnlocked() => Mathf.Clamp(save.lv / 4, 0, SKILL_ES.Length);   // una cada 4 niveles (todas a lv20)
    int SkillQuestions(int idx) => idx + 2;
    int SkillCost(int idx) => idx + 1;                       // racha NECESARIA para invocarla: ×2→1, ×3→2…
    int SkillSpend(int idx) => Math.Max(0, SkillCost(idx) - 1);   // racha que GASTA (1 menos que el requisito): ×2→0, ×3→1…
    int SkillUnlockLevel(int idx) => (idx + 1) * 4;         // lv 4/8/12/16/20
    string SkillName(int idx) => T(SKILL_ES[Mathf.Clamp(idx, 0, SKILL_ES.Length - 1)], SKILL_EN[Mathf.Clamp(idx, 0, SKILL_EN.Length - 1)]);

    Color SkillColor(int fx)
    {
        switch (fx)
        {
            case 1: return new Color(.72f, .45f, 1f);   // magia
            case 2: return new Color(1f, .5f, .15f);    // fuego
            case 3: return new Color(.5f, .85f, 1f);    // hielo
            case 4: return new Color(1f, .95f, .3f);    // rayo
            default: return new Color(1f, 1f, 1f);      // corte
        }
    }

    // Menú de habilidades (durante un combate contra un enemigo normal).
    void RenderSkills()
    {
        if (battle == null) { RenderWorld(""); return; }
        screen = "skills";
        ClearRoot();
        var accent = AccentC;
        AddBackdrop(BattleBackdrop(), 0.62f);
        var p = Panel(root, "Skills", ThemePanelBg, Anchor.Stretch, new Vector2(16, 12), new Vector2(-16, -12));
        var col = ScrollColumn(p, 10, 16);
        Label(col, T("🎇  HABILIDADES", "🎇  SKILLS"), 28, accent, FontStyle.Bold);
        Chip(col, T("Contra enemigos Y jefes · necesitan racha pero GASTAN poca (×2→0, ×3→1…) y luego la acumulan (+2N) · ¡acierta TODAS!",
                    "Vs enemies AND bosses · they need streak but SPEND little (×2→0, ×3→1…) then add it back (+2N) · answer ALL!"), ThemeChipBg, accent, 14, 60);
        Chip(col, T("🔥 Racha disponible: " + combo, "🔥 Streak available: " + combo), new Color(.36f, .2f, .05f, 1f), new Color(1f, .73f, .32f), 15, 38);
        int unlocked = SkillsUnlocked();
        for (int i = 0; i < SKILL_ES.Length; i++)
        {
            if (i < unlocked)
            {
                int idx = i, n = SkillQuestions(i), cost = SkillCost(i), spend = SkillSpend(i);
                var c = SkillColor(SKILL_FX[i]);
                if (combo >= cost)
                    Button(col, T("✦ " + SkillName(i) + "  ·  ×" + n + " pregs/daño  ·  -" + spend + " racha → +" + (2 * n),
                                    "✦ " + SkillName(i) + "  ·  ×" + n + " q/dmg  ·  -" + spend + " streak → +" + (2 * n)), () =>
                    {
                        battle.skill = true; battle.skillN = n; battle.skillIdx = idx;
                        RenderBattle(T("Invocas «" + SkillName(idx) + "».", "You invoke \"" + SkillName(idx) + "\"."));
                    }, new Color(c.r * .3f + .12f, c.g * .3f + .1f, c.b * .3f + .14f));
                else
                    Chip(col, T("✦ " + SkillName(i) + " — necesitas " + cost + " de racha (tienes " + combo + ")",
                                "✦ " + SkillName(i) + " — needs " + cost + " streak (you have " + combo + ")"),
                         new Color(0f, 0f, 0f, .35f), new Color(.85f, .7f, .5f), 14, 40);
            }
            else
            {
                Chip(col, T("🔒 " + SkillName(i) + " — nivel " + SkillUnlockLevel(i) + " (×" + SkillQuestions(i) + " · necesita " + SkillCost(i) + " · gasta " + SkillSpend(i) + " racha)",
                            "🔒 " + SkillName(i) + " — level " + SkillUnlockLevel(i) + " (×" + SkillQuestions(i) + " · needs " + SkillCost(i) + " · spends " + SkillSpend(i) + " streak)"),
                     new Color(0f, 0f, 0f, .35f), new Color(.7f, .7f, .75f), 14, 40);
            }
        }
        Button(col, T("↩  VOLVER AL COMBATE", "↩  BACK TO BATTLE"), () => RenderBattle(""), new Color(.30f, .18f, .22f));
    }

    // Animación de una habilidad: destello del color del efecto + ráfaga escalada con N,
    // con el nombre del ataque y el "×N" en grande.
    IEnumerator SkillAnim(int idx, int n)
    {
        PlaySkillSfx(idx);   // firma sonora propia de cada habilidad
        int fx = SKILL_FX[Mathf.Clamp(idx, 0, SKILL_FX.Length - 1)];
        Color c = SkillColor(fx);
        // Contenedor transparente; dentro: destello de color (parpadea) + el HÉROE (fijo) + texto.
        var ov = Panel(root, "Skill", new Color(0f, 0f, 0f, 0f), Anchor.Stretch, Vector2.zero, Vector2.zero);
        var flash = Panel(ov, "SkillFlash", new Color(c.r, c.g, c.b, .30f), Anchor.Stretch, Vector2.zero, Vector2.zero);

        // Cut-in del protagonista ejecutando la técnica. Animado (skill_<idx>_0/1/2) si existe;
        // si no, el PNG único skill_<idx>; si no, barras de color.
        var frames = AnimFrames("skill_" + idx);
        Sprite sp = frames != null ? LoadSprite(frames[0]) : LoadSprite("skill_" + idx);
        Image hero = null;
        if (sp != null)
        {
            var go = new GameObject("SkillHero", typeof(Image));
            go.transform.SetParent(ov, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            float h = 470f, w = h * sp.rect.width / sp.rect.height;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, 30f);
            hero = go.GetComponent<Image>();
            hero.sprite = sp; hero.preserveAspect = true; hero.raycastTarget = false;
        }
        else
        {
            int rays = 5 + n * 2;
            for (int i = 0; i < rays; i++)
                SlashBar(flash, i * (180f / rays) + rng.Next(16), (i % 2 == 0 ? 1 : -1) * (i * 6), c);
        }

        // Nombre + ×N abajo (no tapa al héroe). BLANCO con contorno/sombra oscuros: legible.
        var t = Label(ov, "✦ " + SkillName(idx) + "  ×" + n + " ✦", 40, Color.white, FontStyle.Bold,
                      Anchor.BottomStretch, new Vector2(20f, 40f), new Vector2(-20f, 96f));
        t.alignment = TextAnchor.MiddleCenter;
        var tol = t.gameObject.AddComponent<Outline>();
        tol.effectColor = new Color(0f, 0f, 0f, .95f); tol.effectDistance = new Vector2(2.5f, -2.5f);
        var tsh = t.gameObject.AddComponent<Shadow>();
        tsh.effectColor = new Color(0f, 0f, 0f, .8f); tsh.effectDistance = new Vector2(0f, -3f);

        if (hero != null && frames != null)   // carga → invocación → estallido
        {
            int[] seq = { 0, 0, 1, 1, 1, 2, 2 };
            foreach (int fr in seq) { var s = LoadSprite(frames[Mathf.Min(fr, frames.Count - 1)]); if (s != null) hero.sprite = s; yield return new WaitForSeconds(0.085f); }
            yield return new WaitForSeconds(0.35f);
        }
        else
        {
            for (int i = 0; i < 6; i++) { flash.gameObject.SetActive(i % 2 == 0); yield return new WaitForSeconds(0.07f); }
            flash.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.6f);
        }
        Destroy(ov.gameObject);
    }

    IEnumerator EnemyAttackAnim(int dmg)
    {
        var ov = Panel(root, "EnemyAtk", new Color(.85f, .1f, .1f, 0f), Anchor.Stretch, Vector2.zero, Vector2.zero);
        var img = ov.GetComponent<Image>();
        // El PROTAGONISTA encaja el golpe: anima hurt_0..2 si existen (centrado).
        Image hero = null;
        if (LoadSprite("hurt_0") != null)
        {
            var go = new GameObject("HurtHero", typeof(Image));
            go.transform.SetParent(ov, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            var sp0 = LoadSprite("hurt_0");
            float hh = 340f, ww = hh * sp0.rect.width / sp0.rect.height;
            rt.sizeDelta = new Vector2(ww, hh);
            rt.anchoredPosition = new Vector2(0f, 30f);
            hero = go.GetComponent<Image>(); hero.preserveAspect = true; hero.raycastTarget = false;
        }
        var t = Label(ov, T(TS(battle.enemy, "n") + " contraataca:  -" + dmg + " HP", TS(battle.enemy, "n") + " strikes back:  -" + dmg + " HP"), 26, new Color(1f, .88f, .88f), FontStyle.Bold, Anchor.BottomStretch, new Vector2(20f, 36f), new Vector2(-20f, 86f));
        t.alignment = TextAnchor.MiddleCenter;
        var tol = t.gameObject.AddComponent<Outline>(); tol.effectColor = new Color(0f, 0f, 0f, .9f); tol.effectDistance = new Vector2(2f, -2f);
        int[] frames = { 0, 1, 2, 1 };   // secuencia de daño
        for (int i = 0; i < 5; i++)
        {
            img.color = new Color(.85f, .1f, .1f, i % 2 == 0 ? .42f : 0f);
            if (hero != null) { var s = LoadSprite("hurt_" + frames[i % frames.Length]); if (s != null) hero.sprite = s; }
            yield return new WaitForSeconds(0.09f);
        }
        if (hero != null) { var s2 = LoadSprite("hurt_2"); if (s2 != null) hero.sprite = s2; }
        yield return new WaitForSeconds(0.3f);
        Destroy(ov.gameObject);
    }

    IEnumerator DefeatedAnim()
    {
        var ov = Panel(root, "Defeated", new Color(0, 0, 0, .62f), Anchor.Stretch, Vector2.zero, Vector2.zero);
        var t = Label(ov, T("¡LO HAS DERROTADO!", "ENEMY DEFEATED!"), 40, new Color(1f, .85f, .3f), FontStyle.Bold, Anchor.Stretch, Vector2.zero, Vector2.zero);
        t.alignment = TextAnchor.MiddleCenter;
        for (int i = 0; i < 6; i++) { t.gameObject.SetActive(i % 2 == 0); yield return new WaitForSeconds(0.11f); }
        t.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.55f);
        Destroy(ov.gameObject);
    }

    void Victory()
    {
        PlaySfx(sfxWin);
        stepsSinceBattle = 0;   // tras derrotar a cualquier monstruo, 5 pasos de gracia
        chasers.Clear(); bossChaser = null;   // los perseguidores se disuelven al ganar el combate
        // Registrar el enemigo vencido para la Granja (bestiario): aparece allí para conversar.
        if (save.metEnemies == null) save.metEnemies = new List<string>();
        string metName = S(battle.enemy, "n");
        if (!string.IsNullOrEmpty(metName) && !save.metEnemies.Contains(metName)) save.metEnemies.Add(metName);
        bool wasBoss = battle.thingKey.Contains("|boss|");
        if (battle.thingKey != "rnd" && battle.thingKey != "rematch" && battle.thingKey != "defend" && battle.thingKey != "tower" && battle.thingKey != "gransabio" && battle.thingKey != "sabioguard" && battle.thingKey != "hubsage") MarkGoneByKey(battle.thingKey);
        GainXp(I(battle.enemy["xp"]));
        if (wasBoss && !save.cleared.Contains(save.area)) save.cleared.Add(save.area);
        if (wasBoss && save.area == "final")
        {
            string tr = QEN ? "demonio_en" : "demonio_es";   // logro por idioma del examen
            if (!save.trophies.Contains(tr)) save.trophies.Add(tr);
        }
        SaveSlot();
        if (battle.thingKey == "defend" && pendingLoot != null)
        {
            var loot = pendingLoot; pendingLoot = null;
            if (S(loot, "kind") == "chest") OpenChest(loot);
            else if (IsDungeon(save.area))
            {
                var ordered = AreaTomeIdsOrdered(save.area);
                StartTome(ordered.Count > 0 ? ordered[Math.Min(ReadTomesInArea(), ordered.Count - 1)] : S(loot, "id"));
            }
            else StartTome(S(loot, "id"));
            return;
        }
        if (battle.thingKey == "rematch") { RenderWorld(T("¡Buen combate, viejo amigo! Ganaste " + I(battle.enemy["xp"]) + " XP.", "Good fight, old friend! You earned " + I(battle.enemy["xp"]) + " XP.")); return; }
        if (battle.thingKey.Contains("|guardian|"))
        {
            int left = GuardiansLeft();
            RenderWorld(left > 0
                ? T("El Guardián cae. Quedan " + left + " guardianes protegiendo la Sala del Examen.", "The Guardian falls. " + left + " guardians still protect the Exam Chamber.")
                : T("¡El último Guardián cae! La Sala del Examen está abierta: te espera EL EXAMEN AB-410.", "The last Guardian falls! The Exam Chamber is open: THE AB-410 EXAM awaits."));
            return;
        }
        if (battle.thingKey == "hubsage") { HubSageWon(); return; }
        if (battle.thingKey == "sabioguard") { SabioGuardWon(); return; }
        if (battle.thingKey == "gransabio") { GranSabioWon(); return; }
        if (wasBoss && IsDungeon(save.area)) { RenderBossCleared(); return; }
        RenderWorld(T("Victoria. ", "Victory! ") + TS(battle.enemy, "lore"));
    }

    void RenderBossCleared()
    {
        screen = "dialog";
        ClearRoot();
        AddBackdrop(BackdropForArea(save.area), 0.45f);
        var p = Panel(root, "BossCleared", new Color(.05f, .08f, .12f, .96f), Anchor.Stretch, new Vector2(28, 22), new Vector2(-28, -22));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .7f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 14, 26);
        BigSprite(col, battle.spr, 120, new Color(1f, .82f, .22f));
        Label(col, T("¡DOMINIO SUPERADO!", "DOMAIN CLEARED!"), 28, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, S(battle.enemy, "lore"), 19, Color.white, FontStyle.Normal);
        Label(col, T("Has vencido al jefe. ¿Seguir explorando o volver al selector de módulos?", "You defeated the boss. Keep exploring, or back to the module selector?"), 19, new Color(.85f, .92f, 1f), FontStyle.Bold);
        Button(col, T("SEGUIR EXPLORANDO", "KEEP EXPLORING"), () => RenderWorld(T("Sigues explorando la mazmorra...", "You keep exploring the dungeon...")), new Color(.2f, .55f, .35f));
        Button(col, T("AL SELECTOR DE MÓDULOS ▶", "TO MODULE SELECTOR ▶"), ExitDungeon, new Color(.95f, .72f, .12f));
    }

    // ===================== TORRE DEL GRAN SABIO (endgame) =====================
    // Torre FÍSICA: N pisos (uno por módulo) que se recorren caminando. Cada piso tiene una
    // GEMA (repasa TODO el módulo, repite los fallos hasta el pleno) y, al superarla, una
    // ESCALERA al piso siguiente. Tras el último módulo, la escalera lleva al piso del Gran
    // Sabio. Toda la lógica de recorrido está en "TORRE FÍSICA DEL GRAN SABIO" (cerca de Layout).
    int TowerFloors => MetaInt("moduleCount", 17);   // un piso por módulo (de la config del examen)
    Dictionary<string, object> TowerZone(int dom) => (dom >= 1 && dom <= L(data["zones"]).Count) ? L(data["zones"])[dom - 1] as Dictionary<string, object> : null;

    // Medalla del módulo (1..N): usa el PNG "medal_<módulo>" si existe (arte propio); si no,
    // cae a la gema. Las medallas se OBTIENEN al superar la gema del piso (= save.towerDone).
    string MedalSprite(int module) => LoadSprite("medal_" + module) != null ? "medal_" + module : "tower_gem";
    bool HasMedal(int module) => save.towerDone != null && save.towerDone.Contains(module);

    // Piso 18: el Gran Sabio está sellado por 4 GUARDIANES (los Jefes de Repaso P1..P4).
    // Cada uno pregunta 4-5 a la vez. Vence a los 4 para desafiar al Gran Sabio.
    void RenderGranSabioFloor(string msg = "")
    {
        screen = "sabiofloor";
        ClearRoot();
        AddBackdrop(BattleBackdrop(), 0.55f);
        var p = Panel(root, "GranSabioFloor", new Color(.06f, .05f, .12f, .96f), Anchor.Stretch, new Vector2(20, 16), new Vector2(-20, -16));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .7f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 10, 20);
        BigSprite(col, "gran_sabio", 112, new Color(1f, .82f, .3f));
        Label(col, T("♜ PISO 18 · EL GRAN SABIO", "♜ FLOOR 18 · THE GREAT SAGE"), 25, new Color(1f, .82f, .22f), FontStyle.Bold);
        Chip(col, T("Lo sellan 4 GUARDIANES (Jefes de Repaso P1-P4): preguntan 4-5 a la vez. Vence a los 4. Caídos: " + sabioGuardsDown.Count + "/4.",
                    "Sealed by 4 GUARDIANS (Review Bosses P1-P4): they ask 4-5 at once. Beat all 4. Down: " + sabioGuardsDown.Count + "/4."), ThemeChipBg, AccentC, 14, 56);
        if (!string.IsNullOrEmpty(msg)) Label(col, msg, 16, new Color(.7f, 1f, .8f), FontStyle.Bold);
        for (int part = 1; part <= 4; part++)
        {
            int prt = part;
            if (sabioGuardsDown.Contains(prt))
                Chip(col, "✔  " + T("Jefe de Repaso P", "Review Boss P") + prt, new Color(.1f, .25f, .13f, 1f), new Color(.6f, 1f, .72f), 15, 42);
            else
                Button(col, "⚔  " + T("Guardián: Jefe de Repaso P", "Guardian: Review Boss P") + prt + T(" (4-5 preguntas)", " (4-5 questions)"), () => StartSabioGuardFight(prt), new Color(.5f, .2f, .24f));
        }
        if (sabioGuardsDown.Count >= 4)
            Button(col, T("☼  ¡RETAR AL GRAN SABIO!", "☼  CHALLENGE THE GREAT SAGE!"), () => StartGranSabioFight(), new Color(.95f, .72f, .12f));
        Button(col, T("↩  BAJAR A LA TORRE", "↩  DOWN TO THE TOWER"), () => { save.area = "tower"; save.x = TowerStart[0]; save.y = TowerStart[1]; RenderWorld(""); }, new Color(.30f, .18f, .22f));
    }

    void StartSabioGuardFight(int part)
    {
        var enemy = new Dictionary<string, object> {
            { "n", "Jefe de Repaso P" + part }, { "n_en", "Review Boss P" + part },
            { "hp", 150 }, { "atk", 13 }, { "xp", 80 },
            { "lore", "Un guardián del Gran Sabio." }, { "lore_en", "A guardian of the Great Sage." }
        };
        battle = new Battle { thingKey = "sabioguard", enemy = enemy, hp = 150, maxhp = 150, dom = 0, part = part, spr = "boss_d" + (((part - 1) % 3) + 1), qn = 4, varQn = true };
        RenderBattle(T("Jefe de Repaso P" + part + ": «Gracias a ti obtuvimos nuestro máximo poder.» 4-5 preguntas a la vez (de toda la Parte " + part + ").",
                       "Review Boss P" + part + ": \"Thanks to you we obtained our maximum power.\" 4-5 questions at once (from all of Part " + part + ")."));
    }

    void SabioGuardWon()
    {
        int part = battle.part;
        if (!sabioGuardsDown.Contains(part)) sabioGuardsDown.Add(part);
        RenderGranSabioFloor(sabioGuardsDown.Count >= 4
            ? T("¡Los 4 guardianes han caído! El Gran Sabio ya puede ser desafiado.", "All 4 guardians fell! The Great Sage can now be challenged.")
            : T("¡Jefe de Repaso P" + part + " vencido! Quedan " + (4 - sabioGuardsDown.Count) + " guardianes.", "Review Boss P" + part + " defeated! " + (4 - sabioGuardsDown.Count) + " guardians left."));
    }

    void StartGranSabioFight()
    {
        var enemy = new Dictionary<string, object> {
            { "n", "Gran Sabio AB-410" }, { "n_en", "Great Sage AB-410" },
            { "hp", 1200 }, { "atk", 14 }, { "xp", 300 },
            { "lore", "El Gran Sabio reconoce tu maestría." }, { "lore_en", "The Great Sage acknowledges your mastery." }
        };
        battle = new Battle { thingKey = "gransabio", enemy = enemy, hp = 1200, maxhp = 1200, dom = 0, spr = "gran_sabio", qn = 3 };
        RenderBattle(T("El Gran Sabio: «Te he usado para adquirir el poder definitivo. Gracias por derrotar al Rey Demonio.» Rondas de 3.",
                       "The Great Sage: \"I have used you to gain the ultimate power. Thank you for defeating the Demon King.\" Rounds of 3."));
    }

    void GranSabioWon()
    {
        if (!save.trophies.Contains("gran_sabio")) save.trophies.Add("gran_sabio");
        GainXp(300);
        SaveSlot();
        screen = "dialog"; ClearRoot();
        AddBackdrop(BackdropForArea("final"), 0.45f);
        var p = Panel(root, "GranSabioWin", new Color(.05f, .08f, .12f, .96f), Anchor.Stretch, new Vector2(28, 22), new Vector2(-28, -22));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .8f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 14, 26);
        BigSprite(col, "trophy_exam", 120, new Color(1f, .82f, .22f));
        Label(col, T("🏆 ¡TORRE DEL GRAN SABIO SUPERADA!", "🏆 TOWER OF THE GREAT SAGE CONQUERED!"), 25, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, T("Recibes el MEDALLÓN DEL GRAN SABIO, el máximo honor de AB-410. +300 XP.",
                     "You receive the GREAT SAGE MEDALLION, the highest AB-410 honor. +300 XP."), 18, Color.white, FontStyle.Normal);
        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.2f, .55f, .35f));
    }

    // ¿Esta pregunta entra en el combate actual? Los guardianes (battle.part>0)
    // preguntan de TODOS los MD de su parte; el jefe final (dom 0) de cualquiera.
    bool QInBattleScope(Dictionary<string, object> q)
    {
        if (q == null || !q.ContainsKey("d")) return false;   // checks de tomo (sin dominio) no cuentan
        if (battle.part > 0) return PartOfDom(I(q["d"])) == battle.part;
        return battle.dom == 0 || I(q["d"]) == battle.dom;
    }

    // Preguntas FALLADAS dentro del ámbito del combate actual (la fase de remate del jefe
    // solo exige limpiar las de SU módulo/parte, no las de otras partes).
    int ScopedWrongCount()
    {
        int n = 0;
        foreach (var k in save.wrongQ) { var q = QuestionByKey(k); if (q != null && QInBattleScope(q)) n++; }
        return n;
    }

    string ScopedWrongKey()
    {
        foreach (var k in save.wrongQ) { var q = QuestionByKey(k); if (q != null && QInBattleScope(q)) return k; }
        return null;
    }

    Dictionary<string, object> PickBattleQuestion(out string key)
    {
        if (battle.locked)
        {
            string sk = ScopedWrongKey();   // solo falladas del ámbito de este jefe
            if (sk != null) { key = sk; var d = QuestionByKey(sk); if (d != null) return d; }
        }
        var all = L(data["bq"]);
        var idxs = new List<int>();
        for (int i = 0; i < all.Count; i++)
        {
            var q = all[i] as Dictionary<string, object>;
            if (QInBattleScope(q)) idxs.Add(i);
        }
        // Selección ponderada: prioriza preguntas NO vistas (cobertura) y las falladas
        // (refuerzo), escalando con el número de veces que se han fallado.
        var weights = new List<int>();
        int total = 0;
        foreach (int i in idxs)
        {
            int w = 1;
            string k = "bq:" + i;
            if (!save.seenQ.Contains(k)) w += 100;   // prioriza fuerte la COBERTURA: evita repetir hasta verlas todas
            w += 3 * WrongCount(k);
            weights.Add(w);
            total += w;
        }
        int roll = rng.Next(total);
        int pick = idxs[idxs.Count - 1];
        for (int j = 0; j < idxs.Count; j++) { roll -= weights[j]; if (roll < 0) { pick = idxs[j]; break; } }
        key = "bq:" + pick;
        MarkSeen(key);
        return all[pick] as Dictionary<string, object>;
    }

    // n preguntas DISTINTAS para la ronda triple, con la misma ponderación
    // (+6 no vista, +3 por fallo acumulado) que PickBattleQuestion.
    List<Dictionary<string, object>> PickBattleQuestions(int n, out List<string> keys)
    {
        var all = L(data["bq"]);
        var idxs = new List<int>();
        for (int i = 0; i < all.Count; i++)
        {
            var q = all[i] as Dictionary<string, object>;
            if (QInBattleScope(q)) idxs.Add(i);
        }
        var picked = new List<int>();
        for (int p = 0; p < n && idxs.Count > 0; p++)
        {
            var weights = new List<int>();
            int total = 0;
            foreach (int i in idxs)
            {
                int w = 1;
                string k = "bq:" + i;
                if (!save.seenQ.Contains(k)) w += 100;   // prioriza fuerte la COBERTURA: evita repetir hasta verlas todas
                w += 3 * WrongCount(k);
                weights.Add(w);
                total += w;
            }
            int roll = rng.Next(total);
            int pick = idxs[idxs.Count - 1];
            for (int j = 0; j < idxs.Count; j++) { roll -= weights[j]; if (roll < 0) { pick = idxs[j]; break; } }
            picked.Add(pick);
            idxs.Remove(pick);
        }
        keys = new List<string>();
        var qs = new List<Dictionary<string, object>>();
        foreach (int i in picked)
        {
            keys.Add("bq:" + i);
            MarkSeen("bq:" + i);
            qs.Add(all[i] as Dictionary<string, object>);
        }
        return qs;
    }

    // Banco EXCLUSIVO de la Torre del Estudio (preguntas de máxima dificultad, AI-first).
    List<object> TowerBank() => data != null && data.ContainsKey("towerbq") ? L(data["towerbq"]) : new List<object>();

    // Pregunta de la Torre: pondera la COBERTURA por sesión (towerSeen) y NO toca el bq.
    Dictionary<string, object> PickTowerQuestion(out string key)
    {
        var all = TowerBank();
        if (all.Count == 0) return PickBattleQuestion(out key);   // respaldo si faltara el banco
        var weights = new List<int>();
        int total = 0;
        for (int i = 0; i < all.Count; i++)
        {
            int w = 1;
            if (!towerSeen.Contains("tbq:" + i)) w += 100;   // prioriza no repetir hasta verlas todas
            weights.Add(w); total += w;
        }
        int roll = rng.Next(total);
        int pick = all.Count - 1;
        for (int j = 0; j < all.Count; j++) { roll -= weights[j]; if (roll < 0) { pick = j; break; } }
        key = "tbq:" + pick;
        towerSeen.Add(key);
        return all[pick] as Dictionary<string, object>;
    }

    // n preguntas DISTINTAS de la Torre, ponderadas por cobertura de la sesión (towerSeen).
    List<Dictionary<string, object>> PickTowerQuestions(int n, out List<string> keys)
    {
        var all = TowerBank();
        if (all.Count == 0) return PickBattleQuestions(n, out keys);   // respaldo si faltara el banco
        var idxs = new List<int>();
        for (int i = 0; i < all.Count; i++) idxs.Add(i);
        var picked = new List<int>();
        for (int p = 0; p < n && idxs.Count > 0; p++)
        {
            var weights = new List<int>();
            int total = 0;
            foreach (int i in idxs)
            {
                int w = 1;
                if (!towerSeen.Contains("tbq:" + i)) w += 100;   // prioriza no repetir hasta verlas todas
                weights.Add(w); total += w;
            }
            int roll = rng.Next(total);
            int pick = idxs[idxs.Count - 1];
            for (int j = 0; j < idxs.Count; j++) { roll -= weights[j]; if (roll < 0) { pick = idxs[j]; break; } }
            picked.Add(pick);
            idxs.Remove(pick);
        }
        keys = new List<string>();
        var qs = new List<Dictionary<string, object>>();
        foreach (int i in picked)
        {
            string key = "tbq:" + i;
            keys.Add(key);
            towerSeen.Add(key);
            qs.Add(all[i] as Dictionary<string, object>);
        }
        return qs;
    }

    // ---- Estadísticas de preguntas (cobertura + fallos) ----
    int WrongCount(string key)
    {
        foreach (var s in save.qstats) if (s.k == key) return s.w;
        return 0;
    }

    void BumpWrong(string key)
    {
        foreach (var s in save.qstats) if (s.k == key) { s.w++; return; }
        save.qstats.Add(new QStat { k = key, w = 1 });
    }

    void MarkSeen(string key)
    {
        if (!save.seenQ.Contains(key)) save.seenQ.Add(key);
    }

    Dictionary<string, object> QuestionByKey(string key)
    {
        if (key.StartsWith("bq:"))
        {
            int i;
            if (int.TryParse(key.Substring(3), out i))
            {
                var all = L(data["bq"]);
                if (i >= 0 && i < all.Count) return all[i] as Dictionary<string, object>;
            }
        }
        else if (key.StartsWith("chk:"))
        {
            string id = key.Substring(4);
            var tomes = D(data["tomes"]);
            if (tomes.ContainsKey(id)) return (tomes[id] as Dictionary<string, object>)["check"] as Dictionary<string, object>;
        }
        return null;
    }

    void RenderQuestion(Dictionary<string, object> q, Action<bool> onAnswer)
    {
        uiDom = q.ContainsKey("d") ? I(q["d"]) : 1;   // tema según el dominio de la pregunta
        ClearRoot();
        var p = Panel(root, "Question", ThemePanelBg, Anchor.Stretch, new Vector2(24, 20), new Vector2(-24, -20));
        var qol = p.gameObject.AddComponent<Outline>(); var qa = AccentC;
        qol.effectColor = new Color(qa.r, qa.g, qa.b, .5f); qol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 24);
        BuildQuestionWidget(col, q, T("✔  CONFIRMAR RESPUESTA", "✔  SUBMIT ANSWER"), onAnswer);
    }

    void GainXp(int xp)
    {
        save.xp += xp;
        while (save.xp >= save.xpNext)
        {
            save.xp -= save.xpNext; save.lv++; save.xpNext += 22;
            save.maxhp += 10; save.hp = save.maxhp;   // al subir de nivel solo aumenta el HP
        }
    }

    int ReadTomesInArea()
    {
        int n = 0;
        foreach (var t in Things(save.area))
            if (S(t, "kind") == "tome" && save.readTomes.Contains(S(t, "id"))) n++;
        return n;
    }

    // Ids de los tomos de un área EN ORDEN (según los datos del área).
    List<string> AreaTomeIdsOrdered(string area)
    {
        var res = new List<string>();
        var a = AreaDict(area);
        if (a == null || !a.ContainsKey("things")) return res;
        foreach (var o in L(a["things"]))
        {
            var t = o as Dictionary<string, object>;
            if (t != null && S(t, "kind") == "tome") res.Add(S(t, "id"));
        }
        return res;
    }

    // El siguiente tomo NO leído en orden (para leer la información ordenada 1,2,3...).
    string NextUnreadTome(string area)
    {
        foreach (var id in AreaTomeIdsOrdered(area))
            if (!save.readTomes.Contains(id)) return id;
        return null;
    }

    int TotalTomesInArea()
    {
        int n = 0;
        foreach (var t in Things(save.area)) if (S(t, "kind") == "tome") n++;
        return n;
    }

    List<object> Layout()
    {
        if (IsDungeon(save.area) && dunLayout != null && dunArea == save.area) return dunLayout;
        if (save.area == "town") return L(data["town"]);
        if (save.area == "farm") return L(data["farmmap"]);
        if (save.area == "tower") return TowerRoomLayout();
        if (save.area == "studytower") return save.studyFloor >= 1 && dunLayout != null && dunArea == "studytower" ? dunLayout : TowerRoomLayout();
        if (IsHub(save.area)) return L(data["hub"]);
        return L(data["dun"]);   // torre del examen u otras áreas estáticas
    }

    // ============================ TORRE DEL ESTUDIO (100 pisos aleatorios) ============================
    // Escalera SIEMPRE accesible desde la ciudad (incluso recién empezada la partida). Un vestíbulo
    // (piso 0) con un NPC que explica las reglas; subiendo, 99 pisos GENERADOS AL AZAR como las
    // mazmorras (cofres de solo poción/XP/saltamuros, SIN tomos, enemigos de cualquier tipo cada vez
    // más fuertes) y, en el piso 100, el Sabio del Estudio. "Exit to city" disponible en todo momento.
    const int StudyTopFloor = 100;

    bool StudyTowerMastered() => save != null && save.trophies != null && save.trophies.Contains("study_tower");

    int StudyCheckpointFor(int floor)
    {
        if (floor >= StudyTopFloor) return 90;
        if (floor < 10) return 0;
        return Mathf.Clamp((floor / 10) * 10, 10, 90);
    }

    void UpdateStudyCheckpoint()
    {
        save.studyCheckpoint = Math.Max(save.studyCheckpoint, StudyCheckpointFor(save.studyFloor));
    }

    void EnterStudyTower()
    {
        save.area = "studytower";
        towerSeen.Clear();
        chasers.Clear(); bossChaser = null; pendingLoot = null; combo = 0; towerDmgBonus = 0; stepsSinceBattle = 0;
        save.studyFloor = 0;   // siempre entras por el vestíbulo; el bibliotecario ofrece el checkpoint
        save.x = TowerStart[0]; save.y = TowerStart[1]; save.fx = 0; save.fy = -1;
        SaveSlot();
        RenderStudyTowerIntro(T("Subes la escalera y entras en la Torre del Estudio.", "You climb the stairs into the Tower of Study."));
    }

    // Vestíbulo (piso 0): NPC guía + escalera de subida + salida. Reutiliza el cuarto estático.
    List<Dictionary<string, object>> StudyLobbyThings()
    {
        var o = new List<Dictionary<string, object>>();
        o.Add(new Dictionary<string, object> { { "kind", "studynpc" }, { "x", TowerGemPos[0] }, { "y", TowerGemPos[1] } });
        o.Add(new Dictionary<string, object> { { "kind", "studyup" }, { "x", TowerStairsPos[0] }, { "y", TowerStairsPos[1] } });
        o.Add(new Dictionary<string, object> { { "kind", "studyexit" }, { "x", TowerExitPos[0] }, { "y", TowerExitPos[1] } });
        return o;
    }

    // Genera un piso aleatorio (1..100): laberinto + cofres + escalera de subida (o el Sabio en el 100).
    void GenerateStudyFloor(int floor)
    {
        const int W = 21, H = 15;
        save.goneThings.RemoveAll(k => k.StartsWith("studytower|"));   // cofres frescos en cada piso
        var g = CarveMaze(W, H);
        dunArea = "studytower";
        dunLayout = new List<object>();
        for (int y = 0; y < H; y++) dunLayout.Add(new string(g[y]));
        var floors = new List<int[]>();
        for (int y = 1; y < H - 1; y++) for (int x = 1; x < W - 1; x++) if (g[y][x] == '.') floors.Add(new[] { x, y });
        // La torre no fuerza recorrido largo: protagonista y objetivo pueden aparecer en
        // cualquier celda caminable, siempre que no sea la misma.
        int[] start = floors[rng.Next(floors.Count)];
        save.x = start[0]; save.y = start[1]; save.fx = 0; save.fy = -1;
        stepsSinceBattle = 0; chasers.Clear(); bossChaser = null;

        int[] far = floors[rng.Next(floors.Count)];
        if (floors.Count > 1)
            while (far[0] == save.x && far[1] == save.y) far = floors[rng.Next(floors.Count)];
        var avail = new List<int[]>();
        foreach (var f in floors) if (!(f[0] == save.x && f[1] == save.y) && !(f[0] == far[0] && f[1] == far[1])) avail.Add(f);
        for (int i = avail.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = avail[i]; avail[i] = avail[j]; avail[j] = t; }

        dunThings = new List<Dictionary<string, object>>();
        int p = 0;
        Dictionary<string, object> Cell(int[] c, params object[] kv)
        {
            var dd = new Dictionary<string, object> { { "x", c[0] }, { "y", c[1] } };
            for (int i = 0; i + 1 < kv.Length; i += 2) dd[(string)kv[i]] = kv[i + 1];
            return dd;
        }
        // Objetivo del piso: la escalera de subida (o el Sabio en la cúspide), en celda caminable aleatoria.
        if (floor >= StudyTopFloor) dunThings.Add(Cell(far, "kind", "studysage"));
        else dunThings.Add(Cell(far, "kind", "studyup"));
        // Cofres (3-4): solo poción / XP / saltamuros.
        int chests = 3 + rng.Next(2);
        for (int i = 0; i < chests && p < avail.Count; i++) dunThings.Add(Cell(avail[p++], "kind", "chest"));
        // El bibliotecario amistoso: vende pociones a cambio de racha (sin bloquear el paso).
        if (p < avail.Count) dunThings.Add(Cell(avail[p++], "kind", "studynpc"));
        // Salida a la ciudad (siempre disponible).
        if (p < avail.Count) dunThings.Add(Cell(avail[p++], "kind", "studyexit"));
        // Cada piso TIENE 3 enemigos que te persiguen (sin atravesar muros). Se colocan ya.
        FillChasers();
    }

    // Sube un piso (desde el vestíbulo o desde la escalera de un piso).
    void StudyClimb()
    {
        if (save.studyFloor >= StudyTopFloor) { RenderWorld(T("Ya estás en la cúspide.", "You are already at the summit.")); return; }
        save.studyFloor++;
        GenerateStudyFloor(save.studyFloor);
        UpdateStudyCheckpoint();
        stepsSinceBattle = 0;   // la RACHA se mantiene entre pisos (solo la rompe un fallo)
        SaveSlot();
        string m = save.studyFloor >= StudyTopFloor
            ? T("Piso 100. El Sabio del Estudio te espera...", "Floor 100. The Sage of Study awaits...")
            : T("Subes al piso " + save.studyFloor + ". Los enemigos son más fuertes aquí.", "You climb to floor " + save.studyFloor + ". The enemies are stronger here.");
        StartCoroutine(PlayCutIn(LoadSprite("climb_0") != null ? "climb" : "elevup", () => RenderWorld(m)));
    }

    void ExitStudyTower()
    {
        save.area = "town";
        var area = AreaDict("town");
        if (area != null && area.ContainsKey("start")) { var st = L(area["start"]); save.x = I(st[0]); save.y = I(st[1]); }
        else { save.x = 10; save.y = 10; }
        UpdateStudyCheckpoint();
        save.studyFloor = 0;
        chasers.Clear(); pendingLoot = null; combo = 0; towerDmgBonus = 0;
        SaveSlot();
        RenderWorld(save.studyCheckpoint > 0
            ? T("Sales de la Torre del Estudio. Checkpoint guardado: piso " + save.studyCheckpoint + ".",
                "You leave the Tower of Study. Checkpoint saved: floor " + save.studyCheckpoint + ".")
            : T("Sales de la Torre del Estudio.", "You leave the Tower of Study."));
    }

    // NPC del vestíbulo: explica las reglas de la torre.
    void RenderStudyTowerIntro(string msg = "")
    {
        screen = "studyintro";
        ClearRoot();
        PlayTrack("tower");
        AddBackdrop(BackdropForArea("studytower"), 0.45f);
        var p = Panel(root, "StudyIntro", new Color(.05f, .06f, .13f, .96f), Anchor.Stretch, new Vector2(30, 24), new Vector2(-30, -24));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.5f, .8f, 1f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 26);
        BigSprite(col, LoadSprite("npc_studyguide_1") != null ? "npc_studyguide_1" : "npc_sabio", 128, new Color(.6f, .85f, 1f));
        Label(col, T("Bibliotecario de la Torre del Estudio", "Librarian of the Tower of Study"), 25, new Color(.6f, .85f, 1f), FontStyle.Bold);
        if (!string.IsNullOrEmpty(msg)) Label(col, msg, 17, new Color(.85f, .92f, 1f), FontStyle.Italic);
        Label(col, T("Te acompañaré. Tengo una magia que puede hacerte más poderoso dentro de esta torre.",
                     "I will accompany you. I have magic that can make you stronger inside this tower."), 18, new Color(.7f, .95f, 1f), FontStyle.Bold);
        if (save.studyCheckpoint > 0)
            Label(col, T("Puedes continuar desde el checkpoint del piso " + save.studyCheckpoint + ". Al volver desde un checkpoint ya perdiste toda la racha y los bonus temporales de la subida.",
                         "You can continue from the floor " + save.studyCheckpoint + " checkpoint. Returning from a checkpoint means your streak and temporary climb bonuses are already gone."), 17, new Color(1f, .82f, .45f), FontStyle.Bold);
        if (StudyTowerMastered())
            Label(col, T("Como conquistaste la torre, la Biblia fija tu daño base en 50 para todo el juego.",
                         "Because you conquered the tower, the Bible fixes your base damage at 50 for the whole game."), 17, new Color(1f, .86f, .45f), FontStyle.Bold);
        Label(col, T("Son 100 pisos al azar. En cada piso hay 3 monstruos que te persiguen por los pasillos (NO atraviesan muros): corre hasta la ESCALERA para subir. Cuanto más alto, más fuertes (y más XP), y pegan un % de tu vida (hasta 90% en lo más alto).",
                     "100 random floors. Each floor has 3 monsters that chase you through the corridors (they do NOT pass through walls): run to the STAIRS to climb. Higher = stronger (and more XP), and they hit for a % of your HP (up to 90% near the top)."), 18, Color.white, FontStyle.Normal);
        Label(col, T("Los cofres dan poción/XP/saltamuros (no hay tomos). Puedes usar POCIONES en combate. Tu RACHA se mantiene entre pisos: gástala conmigo por pociones o daño. En el piso 100 está el Sabio. Si cierras el juego, vuelves aquí y CONTINÚAS desde tu piso exacto. Si vuelves a la ciudad, retomas desde el checkpoint (10,20,...,90).",
                     "Chests give potion/XP/wall-jumper (no tomes). You can use POTIONS in battle. Your STREAK carries between floors: spend it with me for potions or damage. Floor 100 holds the Sage. If you close the game, you wake here and CONTINUE from your exact floor. If you go back to the city, you resume from the checkpoint (10,20,...,90)."), 18, new Color(1f, .85f, .5f), FontStyle.Normal);
        if (save.studyCheckpoint > 0)
            Button(col, T("⮝  CONTINUAR DESDE PISO " + save.studyCheckpoint, "⮝  CONTINUE FROM FLOOR " + save.studyCheckpoint), () =>
            {
                combo = 0; towerDmgBonus = 0;
                save.studyFloor = save.studyCheckpoint;
                GenerateStudyFloor(save.studyFloor);
                SaveSlot();
                RenderWorld(T("Retomas la subida desde el checkpoint del piso " + save.studyFloor + ". Tus bonus temporales se perdieron.",
                              "You resume the climb from the floor " + save.studyFloor + " checkpoint. Your temporary bonuses are gone."));
            }, new Color(.2f, .55f, .7f));
        // Examen de RECUPERACIÓN: si ya subiste al piso 20+, el Bibliotecario te deja recuperar tu
        // poder con un examen de 50 preguntas. 34 aciertos = 50 de daño base; los extra van a la racha.
        if (RecoveryExamAvailable())
        {
            Label(col, T("Como llegaste al piso " + save.studyCheckpoint + ", puedo devolverte tu poder con un EXAMEN de " + RecoveryExamQ + " preguntas: cada acierto sube tu daño base de torre (máx 50 con " + (50 - AttackDamage) + " aciertos) y los aciertos extra se vuelven RACHA.",
                         "Since you reached floor " + save.studyCheckpoint + ", I can restore your power with a " + RecoveryExamQ + "-question EXAM: each correct answer raises your tower base damage (max 50 at " + (50 - AttackDamage) + " correct) and extra correct answers become STREAK."), 17, new Color(.8f, 1f, .7f), FontStyle.Bold);
            Button(col, T("📝  EXAMEN DE RECUPERACIÓN (" + RecoveryExamQ + " preguntas)", "📝  RECOVERY EXAM (" + RecoveryExamQ + " questions)"), StartRecoveryExam, new Color(.42f, .34f, .12f));
        }
        Button(col, T("⮝  SUBIR AL PISO 1", "⮝  CLIMB TO FLOOR 1"), () => StudyClimb(), new Color(.2f, .55f, .7f));
        Button(col, T("🧪  COMERCIAR (racha ↔ objetos)", "🧪  TRADE (streak ↔ items)"), () => RenderStudyShop(), new Color(.3f, .45f, .3f));
        Button(col, T("🚪  VOLVER A LA CIUDAD", "🚪  EXIT TO CITY"), () => ExitStudyTower(), new Color(.32f, .18f, .42f));
    }

    // ===================== EXAMEN DE RECUPERACIÓN (Torre del Estudio) =====================
    // Tras "reiniciar" la torre (caer al vestíbulo) y habiendo subido al piso 20+, el Bibliotecario
    // ofrece un examen de 50 preguntas de la torre. Cada acierto = +1 daño base de torre (tope 50;
    // con AttackDamage 16, 34 aciertos llegan al máximo). Los aciertos por encima del tope se
    // convierten en RACHA. Así puedes recuperar el poder que perdiste al reiniciar la subida.
    const int RecoveryExamQ = 50;
    const int RecoveryFloorReq = 20;
    int RecoveryAtkCap => 50 - AttackDamage;   // aciertos que llevan el daño base al máximo (34)

    bool RecoveryExamAvailable() => save != null && save.studyCheckpoint >= RecoveryFloorReq && !StudyTowerMastered();

    void StartRecoveryExam()
    {
        var bank = TowerBank();
        var copy = new List<Dictionary<string, object>>();
        foreach (var o in bank) { var q = o as Dictionary<string, object>; if (q != null) copy.Add(q); }
        if (copy.Count == 0) { RenderStudyTowerIntro(T("El examen no está disponible.", "The exam is unavailable.")); return; }
        for (int i = copy.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = copy[i]; copy[i] = copy[j]; copy[j] = t; }
        recoveryExamSel = copy.GetRange(0, Math.Min(RecoveryExamQ, copy.Count));
        RenderRecoveryExam();
    }

    void RenderRecoveryExam()
    {
        screen = "recoveryexam";
        ClearRoot();
        AddBackdrop(BackdropForArea("studytower"), 0.5f);
        var p = Panel(root, "RecoveryExam", new Color(.04f, .05f, .09f, .98f), Anchor.Stretch, new Vector2(16, 12), new Vector2(-16, -12));
        var col = ScrollColumn(p, 16, 24);
        Label(col, T("EXAMEN DE RECUPERACIÓN · " + recoveryExamSel.Count + " PREGUNTAS", "RECOVERY EXAM · " + recoveryExamSel.Count + " QUESTIONS"), 24, new Color(1f, .82f, .3f), FontStyle.Bold);
        Label(col, T("Cada acierto sube tu DAÑO BASE de torre (máx 50 con " + RecoveryAtkCap + " aciertos); los aciertos extra se vuelven RACHA. Desplázate para verlas todas y pulsa ENTREGAR.",
                     "Each correct answer raises your tower BASE DAMAGE (max 50 at " + RecoveryAtkCap + " correct); extra correct answers become STREAK. Scroll to see them all, then press SUBMIT."), 15, new Color(.82f, .88f, .95f), FontStyle.Normal);
        recoveryExamEvals = new List<Func<bool?>>();
        for (int i = 0; i < recoveryExamSel.Count; i++)
        {
            Chip(col, "— " + T("Pregunta", "Question") + " " + (i + 1) + " / " + recoveryExamSel.Count + " —", new Color(0f, 0f, 0f, .32f), new Color(1f, .82f, .3f), 14, 34);
            recoveryExamEvals.Add(BuildQuestionBody(col, recoveryExamSel[i]));
        }
        Button(col, T("✔  ENTREGAR EXAMEN", "✔  SUBMIT EXAM"), GradeRecoveryExam, new Color(.2f, .7f, .35f));
        Button(col, T("↩  ABANDONAR", "↩  QUIT"), () => RenderStudyTowerIntro(""), new Color(.4f, .2f, .25f));
    }

    void GradeRecoveryExam()
    {
        int correct = 0;
        for (int i = 0; i < recoveryExamEvals.Count; i++)
            if (recoveryExamEvals[i]() == true) correct++;
        int toAtk = Math.Min(RecoveryAtkCap, correct);        // aciertos que suben el daño base (tope 34 → 50)
        int extra = Math.Max(0, correct - RecoveryAtkCap);    // el resto va a la racha
        towerDmgBonus = Math.Max(towerDmgBonus, toAtk);       // nunca reduce lo que ya tuvieras
        combo += extra;
        PlaySfx(correct >= RecoveryAtkCap ? sfxWin : sfxRight);
        SaveSlot();
        RenderRecoveryExamResult(correct, extra);
    }

    void RenderRecoveryExamResult(int correct, int extra)
    {
        screen = "recoveryexamresult";
        ClearRoot();
        AddBackdrop(BackdropForArea("studytower"), 0.45f);
        var p = Panel(root, "RecoveryResult", new Color(.05f, .06f, .12f, .97f), Anchor.Stretch, new Vector2(22, 18), new Vector2(-22, -18));
        var col = ScrollColumn(p, 14, 26);
        Label(col, T("RESULTADO DEL EXAMEN", "EXAM RESULT"), 30, new Color(1f, .82f, .3f), FontStyle.Bold);
        Label(col, T("Aciertos: ", "Correct: ") + correct + " / " + recoveryExamSel.Count, 21, Color.white, FontStyle.Bold);
        Label(col, T("⚔ Daño base de torre: ", "⚔ Tower base damage: ") + TowerBaseDamage() + (TowerBaseDamage() >= 50 ? T("  (¡MÁXIMO!)", "  (MAX!)") : ""), 20, new Color(1f, .86f, .45f), FontStyle.Bold);
        if (extra > 0)
            Label(col, T("🔥 Racha extra ganada: +" + extra + "  (racha actual: " + combo + ")", "🔥 Extra streak gained: +" + extra + "  (current streak: " + combo + ")"), 19, new Color(1f, .73f, .32f), FontStyle.Bold);
        else
            Label(col, T("Consigue más de " + RecoveryAtkCap + " aciertos para convertir el resto en RACHA.", "Score more than " + RecoveryAtkCap + " correct to turn the rest into STREAK."), 16, new Color(.85f, .9f, 1f), FontStyle.Italic);
        Button(col, T("↩  VOLVER CON EL BIBLIOTECARIO", "↩  BACK TO THE LIBRARIAN"), () => RenderStudyTowerIntro(T("Tu poder regresa contigo.", "Your power returns with you.")), new Color(.2f, .55f, .35f));
    }

    const int StudyPotionCost = 5;   // racha por poción en el bibliotecario
    const int StudyJumpCost = 4;     // racha por 2 saltamuros
    const int StudyPotionSell = StudyPotionCost / 2;   // recompra: mitad del valor
    const int StudyJumpSell = StudyJumpCost / 4;       // compra 2 por 4; recompra 1 por 1
    const int HammerCost = 50;       // racha por el Martillo Rompemuros (solo si ya llegaste a 50 de ataque)
    const int HammerAtkBonus = 20;   // ataque extra del martillo dentro de la torre

    bool HasHammer() => save != null && save.items != null && ItemCount("hammer") > 0;
    int LevelAtkBonus() => save != null ? save.lv / 2 : 0;   // +1 de ataque por cada 2 niveles subidos
    int PlayerBaseDamage() => StudyTowerMastered() ? 50 : AttackDamage + LevelAtkBonus();
    int TowerBaseDamage()
    {
        int b = StudyTowerMastered() ? 50 : Math.Min(50, AttackDamage + towerDmgBonus);
        if (HasHammer()) b += HammerAtkBonus;   // el martillo suma +20 ataque DENTRO de la torre (hasta 70)
        return b;
    }
    int BattleBaseDamage() => battle != null && battle.tower ? TowerBaseDamage() : PlayerBaseDamage();

    // Martillo Rompemuros: rompe el muro de piedra de enfrente por 1 de racha (solo en pisos de la torre).
    void UseHammerBreak()
    {
        if (save.area != "studytower" || save.studyFloor < 1 || dunLayout == null || dunArea != "studytower")
        { RenderWorld(T("⚠ El martillo solo rompe muros dentro de un piso de la torre.", "⚠ The hammer only breaks walls inside a tower floor.")); return; }
        if (!HasHammer()) { RenderWorld(T("⚠ No tienes el Martillo Rompemuros.", "⚠ You don't have the Wall-Breaker Hammer.")); return; }
        if (combo < 1) { RenderWorld(T("⚠ Necesitas al menos 1 de racha para usar el martillo.", "⚠ You need at least 1 streak to use the hammer.")); return; }
        int wx = save.x + save.fx, wy = save.y + save.fy;
        if ((save.fx == 0 && save.fy == 0) || wy < 0 || wx < 0 || wy >= dunLayout.Count || wx >= S(dunLayout[wy]).Length)
        { RenderWorld(T("⚠ Mira hacia un muro: muévete una vez en su dirección y reintenta.", "⚠ Face a wall: step once toward it and retry.")); return; }
        if (S(dunLayout[wy])[wx] != '#')
        { RenderWorld(T("⚠ No tienes un muro de piedra justo delante.", "⚠ You are not facing a stone wall.")); return; }
        combo -= 1;
        BreakWallTile(wx, wy);
        PlaySfx(sfxBreak);
        SaveSlot();
        StartCoroutine(PlayCutIn("break", () => RenderWorld(T("¡CRAC! El martillo rompe el muro. (-1 racha · racha " + combo + ")", "Crack! The hammer smashes the wall. (-1 streak · streak " + combo + ")"))));
    }

    int TowerUpgradeCost()
    {
        if (StudyTowerMastered()) return -1;
        int baseDmg = AttackDamage + towerDmgBonus;
        if (baseDmg >= 50) return -1;
        if (baseDmg < 20) return 2;
        if (baseDmg < 30) return 3;
        if (baseDmg < 40) return 5;
        return 10;
    }

    void RenderStudyShop()
    {
        screen = "studyshop";
        ClearRoot();
        PlayTrack(TrackForArea(save.area));
        AddBackdrop(BackdropForArea("studytower"), 0.42f);
        var p = Panel(root, "StudyShop", new Color(.06f, .08f, .1f, .96f), Anchor.Stretch, new Vector2(28, 22), new Vector2(-28, -22));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.5f, .85f, .6f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 24);
        BigSprite(col, LoadSprite("npc_studyguide_0") != null ? "npc_studyguide_0" : "npc_sabio", 120, new Color(.7f, .95f, .8f));
        Label(col, T("El Bibliotecario", "The Librarian"), 25, new Color(.7f, .95f, .8f), FontStyle.Bold);
        Label(col, T("Te acompaño en cada piso. Mi magia puede hacerte más poderoso solo dentro de esta torre.",
                     "I travel with you on every floor. My magic can make you stronger only inside this tower."), 16, new Color(.7f, .95f, 1f), FontStyle.Bold);
        Label(col, T("Tu racha también potencia tus ataques. Puedes convertirla en daño permanente de esta subida, comprar objetos o venderme objetos por la mitad de su valor.",
                     "Your streak also powers your attacks. You can convert it into permanent damage for this climb, buy items, or sell items back to me for half value."), 16, new Color(.85f, .92f, 1f), FontStyle.Normal);
        Label(col, T("🔥 Racha: ", "🔥 Streak: ") + combo + T("   ·   ⚔ Daño base torre: ", "   ·   ⚔ Tower base damage: ") + TowerBaseDamage() + T("   ·   🧪 Pociones: ", "   ·   🧪 Potions: ") + ItemCount("pocion") + T("   ·   🧱 Saltamuros: ", "   ·   🧱 Wall-jumpers: ") + ItemCount("salta"), 18, new Color(1f, .85f, .4f), FontStyle.Bold);
        int towerCost = TowerUpgradeCost();
        if (towerCost > 0 && combo >= towerCost)
            Button(col, T("⚔  +1 daño permanente en la torre  (−" + towerCost + " racha)", "⚔  +1 permanent tower damage  (−" + towerCost + " streak)"), () =>
            {
                int cost = TowerUpgradeCost();
                if (cost <= 0 || combo < cost) { RenderStudyShop(); return; }
                combo -= cost; towerDmgBonus++; PlaySfx(sfxRight); SaveSlot(); RenderStudyShop();
            }, new Color(.48f, .34f, .12f));
        else if (towerCost == -1)
            Label(col, StudyTowerMastered() ? T("Poder de conquista activo: daño máximo permanente en todo el juego (50).", "Conquest power active: permanent max damage everywhere (50).") : T("Daño máximo alcanzado (50)", "Max damage reached (50)"), 16, new Color(1f, .86f, .45f), FontStyle.Bold);
        // Martillo Rompemuros: SOLO se vende si ya llegaste a 50 de ataque en la torre. +20 ataque y
        // rompe muros por 1 de racha. Cuesta 50 de racha y es permanente (objeto en la mochila).
        if (HasHammer())
            Label(col, T("🔨 Tienes el Martillo Rompemuros: +" + HammerAtkBonus + " ataque en la torre y rompes muros por 1 de racha (botón MURO).",
                         "🔨 You own the Wall-Breaker Hammer: +" + HammerAtkBonus + " attack in the tower and you smash walls for 1 streak (WALL button)."), 16, new Color(1f, .82f, .5f), FontStyle.Bold);
        else if (TowerBaseDamage() >= 50)
        {
            if (combo >= HammerCost)
                Button(col, T("🔨  Comprar Martillo Rompemuros  (−" + HammerCost + " racha)", "🔨  Buy Wall-Breaker Hammer  (−" + HammerCost + " streak)"), () =>
                {
                    if (combo < HammerCost || HasHammer()) { RenderStudyShop(); return; }
                    combo -= HammerCost; AddItem("hammer", 1); PlaySfx(sfxWin); SaveSlot();
                    RenderStudyShop();
                }, new Color(.5f, .34f, .14f));
            else
                Label(col, T("🔨 Martillo Rompemuros: necesitas " + HammerCost + " de racha (tienes " + combo + ").", "🔨 Wall-Breaker Hammer: needs " + HammerCost + " streak (you have " + combo + ")."), 16, new Color(.9f, .8f, .6f), FontStyle.Italic);
        }
        if (combo >= StudyPotionCost)
            Button(col, T("🧪  Comprar 1 poción  (−" + StudyPotionCost + " racha)", "🧪  Buy 1 potion  (−" + StudyPotionCost + " streak)"), () =>
            {
                combo -= StudyPotionCost; AddItem("pocion", 1); PlaySfx(sfxPotion); SaveSlot(); RenderStudyShop();
            }, new Color(.24f, .5f, .3f));
        if (ItemCount("pocion") > 0)
            Button(col, T("↩  Vender 1 poción  (+" + StudyPotionSell + " racha)", "↩  Sell 1 potion  (+" + StudyPotionSell + " streak)"), () =>
            {
                if (!ConsumeItem("pocion")) return;
                combo += StudyPotionSell; PlaySfx(sfxRight); SaveSlot(); RenderStudyShop();
            }, new Color(.22f, .38f, .28f));
        if (combo >= StudyJumpCost)
            Button(col, T("🧱  Comprar 2 saltamuros  (−" + StudyJumpCost + " racha)", "🧱  Buy 2 wall-jumpers  (−" + StudyJumpCost + " streak)"), () =>
            {
                combo -= StudyJumpCost; AddItem("salta", 2); PlaySfx(sfxJump); SaveSlot(); RenderStudyShop();
            }, new Color(.26f, .42f, .5f));
        if (ItemCount("salta") > 0)
            Button(col, T("↩  Vender 1 saltamuros  (+" + StudyJumpSell + " racha)", "↩  Sell 1 wall-jumper  (+" + StudyJumpSell + " streak)"), () =>
            {
                if (!ConsumeItem("salta")) return;
                combo += StudyJumpSell; PlaySfx(sfxRight); SaveSlot(); RenderStudyShop();
            }, new Color(.22f, .34f, .42f));
        if (combo < StudyJumpCost)
            Label(col, T("Necesitas al menos " + StudyJumpCost + " de racha para comerciar.", "You need at least " + StudyJumpCost + " streak to trade."), 16, new Color(1f, .7f, .6f), FontStyle.Italic);
        Button(col, T("CERRAR", "CLOSE"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    // ---- Sabio del Estudio (piso 100): prueba con TODO el banco towerbq, rondas de 2, un fallo = caes ----
    void RenderStudyTowerSageIntro()
    {
        screen = "studysage";
        ClearRoot();
        PlayTrack("sage");
        AddBackdrop(BackdropForArea("studytower"), 0.45f);
        var p = Panel(root, "StudySageIntro", new Color(.06f, .05f, .12f, .96f), Anchor.Stretch, new Vector2(30, 24), new Vector2(-30, -24));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .85f, .35f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 26);
        BigSprite(col, LoadSprite("npc_studysage") != null ? "npc_studysage" : "npc_sabio", 132, new Color(1f, .85f, .35f));
        Label(col, T("Sabio del Estudio", "Sage of Study"), 26, new Color(1f, .85f, .35f), FontStyle.Bold);
        int n = TowerBank().Count;
        if (save.trophies != null && save.trophies.Contains("study_tower"))
            Label(col, T("Ya dominaste la torre entera. ¿Otra vuelta de honor por las " + n + " preguntas?", "You already mastered the whole tower. Another honor lap through all " + n + " questions?"), 19, Color.white, FontStyle.Normal);
        else
            Label(col, T("Has subido 100 pisos. Ahora responde las " + n + " preguntas de la torre, en rondas de DOS. TODAS correctas: un solo fallo y caes al vestíbulo. Premio: el Trofeo de la Torre del Estudio.",
                         "You climbed 100 floors. Now answer all " + n + " tower questions, in rounds of TWO. ALL correct: one miss and you fall back to the lobby. Reward: the Tower of Study Trophy."), 19, Color.white, FontStyle.Normal);
        Button(col, T("ACEPTO LA PRUEBA (" + n + " preguntas)", "I ACCEPT THE TRIAL (" + n + " questions)"), () => StartStudyTowerSage(), new Color(.95f, .72f, .12f));
        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    void StartStudyTowerSage()
    {
        studySageQueue = new List<int>();
        int n = TowerBank().Count;
        for (int i = 0; i < n; i++) studySageQueue.Add(i);
        for (int i = studySageQueue.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = studySageQueue[i]; studySageQueue[i] = studySageQueue[j]; studySageQueue[j] = t; }
        studySagePos = 0;
        RenderStudyTowerSageRound();
    }

    void RenderStudyTowerSageRound()
    {
        screen = "studysage";
        ClearRoot();
        PlayTrack("sage");
        AddBackdrop(BattleBackdrop(), 0.5f);
        var p = Panel(root, "StudySageRound", new Color(.04f, .05f, .12f, .96f), Anchor.Stretch, new Vector2(20, 16), new Vector2(-20, -16));
        var col = ScrollColumn(p, 10, 22);
        Label(col, T("PRUEBA DEL SABIO · ", "SAGE'S TRIAL · ") + (studySagePos + 1) + "/" + studySageQueue.Count, 22, new Color(1f, .85f, .35f), FontStyle.Bold);
        Label(col, T("100% o vuelves al vestíbulo. Sin fallos.", "100% or back to the lobby. No mistakes."), 15, new Color(1f, .7f, .5f), FontStyle.Bold);
        var all = TowerBank();
        int n = Math.Min(2, studySageQueue.Count - studySagePos);
        var qs = new List<Dictionary<string, object>>();
        var evals = new List<Func<bool?>>();
        for (int i = 0; i < n; i++)
        {
            int qi = studySageQueue[studySagePos + i];
            var qq = all[qi] as Dictionary<string, object>;
            qs.Add(qq);
            Label(col, "━━━  " + T("PREGUNTA", "QUESTION") + " " + (studySagePos + i + 1) + " / " + studySageQueue.Count + "  ━━━", 17, new Color(.6f, .9f, 1f), FontStyle.Bold);
            evals.Add(BuildQuestionBody(col, qq));
        }
        var warn = Label(col, " ", 15, new Color(1f, .65f, .45f), FontStyle.Bold);
        bool done = false;
        OptButton(col, T("✔  CONFIRMAR RONDA", "✔  CONFIRM ROUND"), () =>
        {
            if (done) return;
            var res = new List<bool?>();
            foreach (var ev in evals) res.Add(ev());
            for (int i = 0; i < res.Count; i++)
                if (res[i] == null) { warn.text = T("⚠ Completa la pregunta " + (studySagePos + i + 1) + " antes de confirmar.", "⚠ Complete question " + (studySagePos + i + 1) + " before confirming."); return; }
            done = true;
            var ok = new List<bool>();
            foreach (var r in res) ok.Add(r.Value);
            ResolveStudyTowerRound(qs, ok);
        }, new Color(.78f, .22f, .14f), 0, 82);
        Button(col, T("ABANDONAR", "GIVE UP"), () => RenderWorld(T("Dejaste la prueba del Sabio.", "You left the Sage's trial.")), new Color(.35f, .16f, .18f));
    }

    void ResolveStudyTowerRound(List<Dictionary<string, object>> qs, List<bool> ok)
    {
        string failWhy = null;
        for (int i = 0; i < qs.Count; i++)
            if (!ok[i] && failWhy == null) failWhy = QS(qs[i], "why");
        if (failWhy != null) { PlaySfx(sfxWrong); RenderStudyTowerSageFail(failWhy); return; }
        PlaySfx(sfxRight);
        studySagePos += qs.Count;
        if (studySagePos >= studySageQueue.Count) { RenderStudyTowerSageVictory(); return; }
        RenderStudyTowerSageRound();
    }

    void RenderStudyTowerSageFail(string why)
    {
        screen = "studysage-fail";
        ClearRoot();
        save.hp = Math.Max(1, save.maxhp / 2);
        save.studyFloor = 0;                 // caes al vestíbulo: hay que volver a subir
        save.area = "studytower";
        save.x = TowerStart[0]; save.y = TowerStart[1]; save.fx = 0; save.fy = -1;
        combo = 0; towerDmgBonus = 0;
        SaveSlot();
        PlayTrack("tower");
        AddBackdrop(BackdropForArea("studytower"), 0.45f);
        var p = Panel(root, "StudySageFail", new Color(.12f, .04f, .05f, .96f), Anchor.Stretch, new Vector2(34, 28), new Vector2(-34, -28));
        var col = ScrollColumn(p, 14, 28);
        Label(col, T("PRUEBA FALLIDA", "TRIAL FAILED"), 30, new Color(1f, .45f, .4f), FontStyle.Bold);
        Label(col, T("Una respuesta incorrecta rompe la prueba. El Sabio te devuelve al vestíbulo. ", "One wrong answer breaks the trial. The Sage sends you back to the lobby. ") + why, 19, Color.white, FontStyle.Normal);
        Label(col, T("Llegaste a " + studySagePos + "/" + studySageQueue.Count + ". Repasa y vuelve a subir.", "You reached " + studySagePos + "/" + studySageQueue.Count + ". Review and climb again."), 17, new Color(1f, .8f, .5f), FontStyle.Bold);
        Button(col, T("CONTINUAR", "CONTINUE"), () => RenderStudyTowerIntro(""), new Color(.95f, .72f, .12f));
    }

    void RenderStudyTowerSageVictory()
    {
        screen = "studysage-victory";
        ClearRoot();
        PlaySfx(sfxWin);
        PlayTrack("sage");
        if (save.trophies != null && !save.trophies.Contains("study_tower")) save.trophies.Add("study_tower");
        GainXp(300);
        SaveSlot();
        AddBackdrop(BackdropForArea("studytower"), 0.5f);
        var p = Panel(root, "StudySageVictory", new Color(.05f, .08f, .06f, .97f), Anchor.Stretch, new Vector2(32, 26), new Vector2(-32, -26));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .7f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 14, 28);
        BigSprite(col, LoadSprite("trophy_studytower") != null ? "trophy_studytower" : (LoadSprite("trophy_exam") != null ? "trophy_exam" : "npc_sabio"), 120, new Color(1f, .82f, .22f));
        Label(col, T("¡TORRE DEL ESTUDIO DOMINADA!", "TOWER OF STUDY MASTERED!"), 28, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, T("Cien pisos y todas las preguntas de máxima dificultad. Recibes el TROFEO DE LA TORRE DEL ESTUDIO. +300 XP.",
                     "A hundred floors and every maximum-difficulty question. You receive the TOWER OF STUDY TROPHY. +300 XP."), 19, Color.white, FontStyle.Normal);
        Label(col, T("✦ NUEVA HABILIDAD: La Biblia fija tu daño base en 50 para todo el juego.",
                     "✦ NEW ABILITY: The Bible sets your base damage to 50 for the whole game."), 18, new Color(1f, .86f, .45f), FontStyle.Bold);
        Label(col, T("✦ NUEVO PODER: La BIBLIA DEL ESTUDIO. Consulta TODOS los tomos cuando quieras, por Parte y Módulo, desde la ciudad o tu mochila (🎒).",
                     "✦ NEW POWER: The STUDY BIBLE. Consult ALL tomes anytime, by Part and Module, from the city or your bag (🎒)."), 18, new Color(.7f, .95f, 1f), FontStyle.Bold);
        Button(col, T("VOLVER A LA CIUDAD", "EXIT TO CITY"), () => ExitStudyTower(), new Color(.2f, .55f, .35f));
        Button(col, T("QUEDARSE EN LA TORRE", "STAY IN THE TOWER"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    // ============================ LA BIBLIA DEL ESTUDIO (recompensa de la torre) ============================
    // Poder desbloqueado al dominar la Torre del Estudio (trofeo "study_tower"): consultar TODOS los
    // tomos del juego como una "biblia" navegable, con SECCIONES seleccionables por módulo. Accesible
    // desde la ciudad (tile) y desde el inventario (🎒). Usa el sprite del Súper Tomo (tomo dorado).

    // Etiqueta de prefijo de tomo de un módulo (p.ej. dom 4 -> "p2m1"), según meta.parts.
    string ModuleTag(int dom)
    {
        var m = Meta();
        if (m != null && m.ContainsKey("parts"))
            foreach (var po in L(m["parts"]))
            {
                var pd = po as Dictionary<string, object>;
                if (pd == null || !pd.ContainsKey("modules")) continue;
                var mods = L(pd["modules"]);
                for (int k = 0; k < mods.Count; k++) if (I(mods[k]) == dom) return "p" + I(pd["part"]) + "m" + (k + 1);
            }
        return "p" + PartOfDom(dom) + "m" + dom;
    }

    string ModuleName(int dom)
    {
        var zones = L(data["zones"]);
        if (dom >= 1 && dom <= zones.Count) { var z = zones[dom - 1] as Dictionary<string, object>; if (z != null && z.ContainsKey("name")) return S(z, "name"); }
        return "MD" + dom;
    }

    // Tomos (ordenados) de un módulo, leyendo data["tomes"] por prefijo "p#m#_".
    List<string> ModuleTomeIds(int dom)
    {
        var outIds = new List<string>();
        var tomes = D(data["tomes"]);
        if (tomes == null) return outIds;
        string tag = ModuleTag(dom) + "_";
        foreach (var kv in tomes) if (kv.Key.StartsWith(tag)) outIds.Add(kv.Key);
        outIds.Sort((a, b) =>
        {
            int ia, ib;
            int.TryParse(a.Substring(a.LastIndexOf('_') + 1), out ia);
            int.TryParse(b.Substring(b.LastIndexOf('_') + 1), out ib);
            return ia.CompareTo(ib);
        });
        return outIds;
    }

    string bibleSprite => LoadSprite("supertome") != null ? "supertome" : "tome_read";

    void RenderBibleIndex()
    {
        screen = "bible";
        ClearRoot();
        PlayTrack("tome");
        AddBackdrop(BackdropForArea("town"), 0.4f);
        var p = Panel(root, "Bible", new Color(.07f, .06f, .12f, .96f), Anchor.Stretch, new Vector2(26, 22), new Vector2(-26, -22));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 10, 20);
        BigSprite(col, bibleSprite, 96, new Color(1f, .82f, .22f));
        Label(col, T("📖 La Biblia del Estudio", "📖 The Study Bible"), 26, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, T("El poder de la Torre del Estudio: consulta TODOS los tomos cuando quieras. Elige una PARTE.",
                     "The power of the Tower of Study: consult ALL tomes anytime. Choose a PART."), 16, new Color(.85f, .9f, 1f), FontStyle.Normal);
        int pc = MetaInt("partCount", 4);
        for (int part = 1; part <= pc; part++)
        {
            int pp = part;
            int mods = PartModules(part).Count;
            Button(col, "P" + part + " · " + PartName(part) + "  (" + mods + T(" módulos)", " modules)"), () => RenderBiblePart(pp), new Color(.22f, .3f, .5f));
        }
        Button(col, T("CERRAR", "CLOSE"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    string PartName(int part)
    {
        var m = Meta();
        if (m != null && m.ContainsKey("parts"))
            foreach (var po in L(m["parts"])) { var pd = po as Dictionary<string, object>; if (pd != null && I(pd["part"]) == part) return S(pd, "name"); }
        return T("Parte ", "Part ") + part;
    }

    List<int> PartModules(int part)
    {
        var outl = new List<int>();
        var m = Meta();
        if (m != null && m.ContainsKey("parts"))
            foreach (var po in L(m["parts"]))
            {
                var pd = po as Dictionary<string, object>;
                if (pd != null && I(pd["part"]) == part && pd.ContainsKey("modules"))
                    foreach (var d in L(pd["modules"])) outl.Add(I(d));
            }
        return outl;
    }

    void RenderBiblePart(int part)
    {
        screen = "bible-part";
        ClearRoot();
        PlayTrack("tome");
        AddBackdrop(BackdropForArea("town"), 0.4f);
        var p = Panel(root, "BiblePart", new Color(.07f, .06f, .12f, .96f), Anchor.Stretch, new Vector2(26, 22), new Vector2(-26, -22));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 10, 20);
        BigSprite(col, bibleSprite, 72, new Color(1f, .82f, .22f));
        Label(col, "P" + part + " · " + PartName(part), 24, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, T("Selecciona un módulo.", "Select a module."), 15, new Color(.85f, .9f, 1f), FontStyle.Normal);
        var mods = PartModules(part);
        for (int i = 0; i < mods.Count; i++)
        {
            int dd = mods[i];
            int secs = ModuleTomeIds(dd).Count;
            Button(col, "MD" + (i + 1) + " · " + ModuleName(dd) + "  (" + secs + T(" tomos)", " tomes)"), () => RenderBibleModule(dd), new Color(.2f, .42f, .4f));
        }
        Button(col, T("◀  VOLVER A LAS PARTES", "◀  BACK TO PARTS"), () => RenderBibleIndex(), new Color(.32f, .18f, .42f));
    }

    void RenderBibleModule(int dom)
    {
        screen = "bible-mod";
        ClearRoot();
        PlayTrack("tome");
        AddBackdrop(BackdropForArea("town"), 0.4f);
        var p = Panel(root, "BibleModule", new Color(.07f, .06f, .12f, .96f), Anchor.Stretch, new Vector2(26, 22), new Vector2(-26, -22));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 10, 20);
        BigSprite(col, bibleSprite, 72, new Color(1f, .82f, .22f));
        Label(col, ModuleName(dom), 24, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, T("Elige un tomo, o lee el módulo completo de una vez.", "Pick a tome, or read the whole module at once."), 15, new Color(.85f, .9f, 1f), FontStyle.Normal);
        var ids = ModuleTomeIds(dom);
        var tomes = D(data["tomes"]);
        if (ids.Count == 0) Label(col, T("Este módulo no tiene tomos.", "This module has no tomes."), 16, new Color(1f, .8f, .6f), FontStyle.Italic);
        else Button(col, T("📚  LEER TODO EL MÓDULO (compendio)", "📚  READ THE WHOLE MODULE (compendium)"), () => RenderBibleCompendium(dom), new Color(.4f, .3f, .1f));
        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];
            var tome = tomes.ContainsKey(id) ? tomes[id] as Dictionary<string, object> : null;
            string ttl = tome != null && tome.ContainsKey("t") ? S(tome, "t") : id;
            Button(col, (i + 1) + ".  " + ttl, () => RenderBibleSection(id, dom), new Color(.2f, .42f, .4f));
        }
        Button(col, T("◀  VOLVER A LOS MÓDULOS", "◀  BACK TO MODULES"), () => RenderBiblePart(PartOfDom(dom)), new Color(.32f, .18f, .42f));
    }

    // Compendio: TODOS los tomos del módulo concatenados en una sola lectura.
    void RenderBibleCompendium(int dom)
    {
        screen = "bible-comp";
        ClearRoot();
        PlayTrack("tome");
        AddBackdrop(BackdropForArea("town"), 0.4f);
        var p = Panel(root, "BibleCompendium", new Color(.06f, .06f, .12f, .97f), Anchor.Stretch, new Vector2(24, 20), new Vector2(-24, -20));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 10, 16);
        Label(col, "📚 " + ModuleName(dom), 24, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, T("Compendio completo del módulo.", "Full module compendium."), 14, new Color(.6f, .85f, 1f), FontStyle.Bold);
        var tomes = D(data["tomes"]);
        var ids = ModuleTomeIds(dom);
        foreach (var id in ids)
        {
            var tome = tomes != null && tomes.ContainsKey(id) ? tomes[id] as Dictionary<string, object> : null;
            if (tome == null) continue;
            Label(col, "━━  " + (tome.ContainsKey("t") ? S(tome, "t") : id) + "  ━━", 19, new Color(.85f, .95f, 1f), FontStyle.Bold);
            if (tome.ContainsKey("pages"))
                foreach (var pg in L(tome["pages"]))
                {
                    var lab = Label(col, S(pg), 17, Color.white, FontStyle.Normal);
                    lab.alignment = TextAnchor.UpperLeft;
                }
        }
        Button(col, T("◀  VOLVER AL MÓDULO", "◀  BACK TO MODULE"), () => RenderBibleModule(dom), new Color(.32f, .18f, .42f));
    }

    void RenderBibleSection(string id, int dom)
    {
        screen = "bible-sec";
        ClearRoot();
        PlayTrack("tome");
        AddBackdrop(BackdropForArea("town"), 0.4f);
        var p = Panel(root, "BibleSection", new Color(.06f, .06f, .12f, .97f), Anchor.Stretch, new Vector2(24, 20), new Vector2(-24, -20));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 10, 18);
        var tomes = D(data["tomes"]);
        var tome = tomes != null && tomes.ContainsKey(id) ? tomes[id] as Dictionary<string, object> : null;
        Label(col, tome != null && tome.ContainsKey("t") ? S(tome, "t") : id, 24, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, ModuleName(dom), 14, new Color(.6f, .85f, 1f), FontStyle.Bold);
        if (tome != null && tome.ContainsKey("pages"))
            foreach (var pg in L(tome["pages"]))
            {
                var lab = Label(col, S(pg), 17, Color.white, FontStyle.Normal);
                lab.alignment = TextAnchor.UpperLeft;
            }
        Button(col, T("◀  VOLVER A LAS SECCIONES", "◀  BACK TO SECTIONS"), () => RenderBibleModule(dom), new Color(.32f, .18f, .42f));
    }

    // ============================ TORRE FÍSICA DEL GRAN SABIO ============================
    // Endgame tras el Rey Demonio: una torre que se RECORRE caminando, un piso por módulo.
    // Cada piso tiene una GEMA (repasa TODAS las preguntas del módulo, repite los fallos hasta
    // el pleno); al superarla aparece la ESCALERA al piso siguiente. Reutiliza el motor de
    // mapa (Move/Things/Action/RenderWorld) y el de repaso (reviewQueue/RenderPartReview).

    // Cuarto reutilizado para todos los pisos (andamiaje del motor, no es contenido de examen).
    // 13×11: muro en el borde, suelo dentro. Gema centro, escalera arriba, jugador/salida abajo.
    static readonly string[] TowerRoomRows = {
        "#############",
        "#...........#",
        "#...........#",
        "#...........#",
        "#...........#",
        "#...........#",
        "#...........#",
        "#...........#",
        "#...........#",
        "#...........#",
        "#############",
    };
    List<object> towerRoomCache;
    List<object> TowerRoomLayout()
    {
        if (towerRoomCache == null) { towerRoomCache = new List<object>(); foreach (var r in TowerRoomRows) towerRoomCache.Add(r); }
        return towerRoomCache;
    }
    // Posiciones fijas dentro del cuarto.
    static readonly int[] TowerStart = { 6, 9 };   // entrada del jugador (abajo-centro)
    static readonly int[] TowerGemPos = { 6, 5 };  // pedestal de la gema (centro)
    static readonly int[] TowerStairsPos = { 6, 1 };// hueco de la escalera (arriba-centro)
    static readonly int[] TowerExitPos = { 1, 9 };  // salida de la torre (abajo-izquierda)

    List<Dictionary<string, object>> TowerThings()
    {
        var outList = new List<Dictionary<string, object>>();
        int fl = save.towerFloor < 1 ? 1 : save.towerFloor;
        outList.Add(new Dictionary<string, object> { { "kind", "towergem" }, { "module", fl }, { "x", TowerGemPos[0] }, { "y", TowerGemPos[1] } });
        // El ASCENSOR está SIEMPRE presente (al norte); su botón de SUBIR se habilita solo
        // cuando la gema de este piso está superada. También permite BAJAR a pisos ya hechos.
        outList.Add(new Dictionary<string, object> { { "kind", "towerelevator" }, { "x", TowerStairsPos[0] }, { "y", TowerStairsPos[1] } });
        outList.Add(new Dictionary<string, object> { { "kind", "towerexit" }, { "x", TowerExitPos[0] }, { "y", TowerExitPos[1] } });
        return outList;
    }

    // Entra a la torre física: arranca en el piso más bajo cuya gema aún no esté superada.
    void EnterTower()
    {
        int n = TowerFloors;
        int fl = 1;
        while (fl < n && save.towerDone != null && save.towerDone.Contains(fl)) fl++;
        // Si TODAS las gemas están hechas, el piso útil es el último (su escalera lleva al Gran Sabio).
        save.towerFloor = fl;
        save.area = "tower";
        save.x = TowerStart[0]; save.y = TowerStart[1]; save.fx = 0; save.fy = -1;
        chasers.Clear(); bossChaser = null; pendingLoot = null; combo = 0; towerDmgBonus = 0;
        SaveSlot();
        RenderWorld(T("Entras a la Torre del Gran Sabio. Cada piso guarda una GEMA de repaso.",
                      "You enter the Tower of the Great Sage. Each floor holds a review GEM."));
    }

    void StartTowerGemReview(int module)
    {
        reviewTower = true; reviewFarm = false; reviewModule = module;
        reviewPart = PartOfDom(module);
        reviewQueue = new List<int>();
        var all = L(data["bq"]);
        for (int i = 0; i < all.Count; i++)
            if (I((all[i] as Dictionary<string, object>)["d"]) == module) reviewQueue.Add(i);
        for (int i = reviewQueue.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = reviewQueue[i]; reviewQueue[i] = reviewQueue[j]; reviewQueue[j] = t; }
        if (reviewQueue.Count == 0)   // módulo sin preguntas: la gema se da por superada
        {
            if (!save.towerDone.Contains(module)) save.towerDone.Add(module);
            SaveSlot();
            RenderWorld(T("La gema brilla: este módulo no tiene preguntas. Recibes su medalla y el ascensor puede subir.",
                          "The gem glows: this module has no questions. You get its medal and the elevator can go up."));
            return;
        }
        RenderPartReview();
    }

    // ASCENSOR de la Torre: panel con el piso actual y botones de SUBIR (si la gema está
    // superada) / BAJAR (a pisos ya hechos). Tras el último módulo, SUBIR lleva al Gran Sabio.
    void RenderElevator(string msg = "")
    {
        bool gemDone = save.towerDone != null && save.towerDone.Contains(save.towerFloor);
        var zone = TowerZone(save.towerFloor);
        string nm = zone != null ? TS(zone, "name") : ("MD" + save.towerFloor);
        screen = "dialog";
        ClearRoot();
        AddBackdrop(BackdropForArea("final"), 0.5f);
        var p = Panel(root, "Elevator", new Color(.05f, .06f, .12f, .96f), Anchor.Stretch, new Vector2(26, 22), new Vector2(-26, -22));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .7f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 24);
        BigSprite(col, "tower_stairs", 96, new Color(1f, .9f, .5f));
        Label(col, T("🛗  ASCENSOR · PISO ", "🛗  ELEVATOR · FLOOR ") + save.towerFloor + " / " + TowerFloors, 24, new Color(1f, .82f, .22f), FontStyle.Bold);
        Label(col, nm, 18, new Color(.85f, .9f, 1f), FontStyle.Italic);
        if (!string.IsNullOrEmpty(msg)) Label(col, msg, 16, new Color(.7f, 1f, .8f), FontStyle.Bold);
        Chip(col, gemDone
            ? T("Gema de este piso: ✔ superada.", "This floor's gem: ✔ mastered.")
            : T("Gema de este piso: pendiente. Supérala para poder SUBIR.", "This floor's gem: pending. Master it to go UP."),
            ThemeChipBg, AccentC, 15, 44);
        int earned = 0; for (int m = 1; m <= TowerFloors; m++) if (HasMedal(m)) earned++;
        Chip(col, T("🏅 Medallas de módulo: " + earned + " / " + TowerFloors, "🏅 Module medals: " + earned + " / " + TowerFloors),
            new Color(.36f, .2f, .05f, 1f), new Color(1f, .82f, .35f), 15, 38);
        // SUBIR
        if (gemDone)
        {
            if (save.towerFloor < TowerFloors)
                Button(col, T("▲  SUBIR al piso ", "▲  GO UP to floor ") + (save.towerFloor + 1), () => ElevatorTo(save.towerFloor + 1), new Color(.2f, .55f, .35f));
            else
                Button(col, T("▲  SUBIR a la CÚSPIDE (Gran Sabio)", "▲  GO UP to the SUMMIT (Great Sage)"), () => { sabioGuardsDown.Clear(); StartCoroutine(PlayCutIn("elevup", () => RenderGranSabioFloor(T("El ascensor te deja en la cúspide. Te aguarda el Gran Sabio.", "The elevator leaves you at the summit. The Great Sage awaits.")))); }, new Color(.95f, .72f, .12f));
        }
        // BAJAR (a pisos ya visitados)
        if (save.towerFloor > 1)
            Button(col, T("▼  BAJAR al piso ", "▼  GO DOWN to floor ") + (save.towerFloor - 1), () => ElevatorTo(save.towerFloor - 1), new Color(.3f, .35f, .55f));
        Button(col, T("↩  CERRAR", "↩  CLOSE"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    void ElevatorTo(int floor)
    {
        bool up = floor > save.towerFloor;   // animación de subida solo al subir
        save.towerFloor = Mathf.Clamp(floor, 1, TowerFloors);
        save.x = TowerStart[0]; save.y = TowerStart[1]; save.fx = 0; save.fy = -1;
        SaveSlot();
        string m = T("🛗 El ascensor se detiene en el piso " + save.towerFloor + ".", "🛗 The elevator stops at floor " + save.towerFloor + ".");
        if (up) StartCoroutine(PlayCutIn("elevup", () => RenderWorld(m)));
        else RenderWorld(m);
    }

    void ExitTower()
    {
        save.area = "final";
        var area = AreaDict("final");
        if (area != null && area.ContainsKey("start")) { var st = L(area["start"]); save.x = I(st[0]); save.y = I(st[1]); }
        else { save.x = 8; save.y = 12; }
        SaveSlot();
        RenderWorld(T("Bajas de la Torre del Gran Sabio.", "You descend from the Tower of the Great Sage."));
    }

    // Intro de la gema: explica el reto del piso y arranca el repaso del módulo.
    void RenderTowerGemIntro(int module)
    {
        bool done = save.towerDone != null && save.towerDone.Contains(module);
        var zone = TowerZone(module);
        string nm = zone != null ? TS(zone, "name") : ("MD" + module);
        int qn = 0; var all = L(data["bq"]);
        foreach (var o in all) if (I((o as Dictionary<string, object>)["d"]) == module) qn++;
        screen = "dialog";
        ClearRoot();
        AddBackdrop(BackdropForArea("final"), 0.5f);
        var p = Panel(root, "TowerGem", new Color(.06f, .05f, .12f, .96f), Anchor.Stretch, new Vector2(26, 22), new Vector2(-26, -22));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .7f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 24);
        BigSprite(col, "tower_gem", 110, new Color(.6f, 1f, .9f));
        Label(col, "♦ " + T("PISO ", "FLOOR ") + save.towerFloor + " · " + nm, 24, new Color(1f, .82f, .22f), FontStyle.Bold);
        if (done)
            Label(col, T("Ya tienes la MEDALLA de este módulo. El ascensor ya puede subir al piso siguiente.",
                         "You already own this module's MEDAL. The elevator can go up to the next floor."), 19, new Color(.7f, 1f, .8f), FontStyle.Bold);
        Label(col, T("La gema te hará repasar las " + qn + " preguntas de este módulo. Las que falles VOLVERÁN hasta que las aciertes todas. Al lograrlo recibes la MEDALLA del módulo y el ascensor puede subir.",
                     "The gem makes you review this module's " + qn + " questions. Missed ones RETURN until you get them all. On success you earn the module's MEDAL and the elevator can go up."), 18, Color.white, FontStyle.Normal);
        Button(col, done ? T("⟳  REPASAR DE NUEVO", "⟳  REVIEW AGAIN") : T("♦  TOCAR LA GEMA", "♦  TOUCH THE GEM"), () => StartTowerGemReview(module), new Color(.2f, .55f, .7f));
        Button(col, T("↩  VOLVER", "↩  BACK"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    Dictionary<string, object> ThingAt(int x, int y)
    {
        foreach (var t in Things(save.area))
            if (Gone(t) || I(t["x"]) != x || I(t["y"]) != y) continue;
            else return t;
        return null;
    }

    string TokenKind(Dictionary<string, object> t)
    {
        var kind = S(t, "kind");
        if (kind == "tome" && (IsDungeon(save.area) ? (save.readTomeTiles != null && save.readTomeTiles.Contains(ThingKey(t))) : save.readTomes.Contains(S(t, "id")))) return "tomeRead";
        return kind;
    }

    string TokenLabel(Dictionary<string, object> t)
    {
        switch (S(t, "kind"))
        {
            case "portal": return "PORTAL";
            case "exit": return T("SALIR", "EXIT");
            case "carriage": return T("A UN TOMO", "TO A TOME");
            case "npc": return "NPC";
            case "inn": return "INN";
            case "tome": return save.readTomes.Contains(S(t, "id")) ? "OK" : T("TOMO", "TOME");
            case "supertome": return "SUPER";
            case "sage": return T("SABIO", "SAGE");
            case "towerentrance": return T("TORRE", "TOWER");
            case "studytower": return T("ESTUDIO", "STUDY");
            case "studyup": return T("SUBIR", "UP");
            case "studyexit": return T("SALIR", "EXIT");
            case "studynpc": return T("GUÍA", "GUIDE");
            case "studysage": return T("SABIO", "SAGE");
            case "studybible": return T("BIBLIA", "BIBLE");
            case "towergem": return T("GEMA", "GEM");
            case "towerelevator": return T("ASCENSOR", "ELEVATOR");
            case "towerexit": return T("SALIR", "EXIT");
            case "reviewboss": return "REPASO P" + I(t["part"]);
            case "hubsage": return T("SABIO", "SAGE");
            case "dashboard": return "STATS";
            case "examprep": return T("EXAMEN", "EXAM");
            case "langmage": return T("IDIOMA", "LANG");
            case "enemy": return "RISK";
            case "boss": return "BOSS";
            case "guardian": return T("GUARDIÁN", "GUARDIAN");
            case "chest": return "MP";
            default: return "";
        }
    }

    Color TokenColor(Dictionary<string, object> t)
    {
        switch (S(t, "kind"))
        {
            case "portal": return new Color(.22f, .85f, 1f);
            case "exit": return new Color(.9f, .9f, .9f);
            case "carriage": return new Color(.55f, .8f, 1f);
            case "npc": return new Color(.35f, .72f, 1f);
            case "inn": return new Color(.25f, .85f, .45f);
            case "tome": return new Color(1f, .78f, .2f);
            case "supertome": return new Color(.45f, .9f, 1f);
            case "sage": return new Color(1f, .85f, .35f);
            case "towerentrance": return new Color(1f, .82f, .3f);
            case "studytower": return new Color(.5f, .85f, 1f);
            case "studyup": return new Color(.6f, .9f, 1f);
            case "studyexit": return new Color(.9f, .9f, .9f);
            case "studynpc": return new Color(.6f, .85f, 1f);
            case "studysage": return new Color(1f, .85f, .35f);
            case "studybible": return new Color(1f, .82f, .22f);
            case "towergem": return new Color(.5f, 1f, .85f);
            case "towerelevator": return new Color(1f, .9f, .5f);
            case "towerexit": return new Color(.9f, .9f, .9f);
            case "reviewboss": return new Color(1f, .55f, .45f);
            case "hubsage": return new Color(1f, .85f, .35f);
            case "dashboard": return new Color(.6f, .9f, 1f);
            case "examprep": return new Color(1f, .82f, .3f);
            case "langmage": return new Color(.7f, .5f, 1f);
            case "enemy": return new Color(.9f, .2f, .28f);
            case "boss": return new Color(1f, .35f, .18f);
            case "guardian": return new Color(1f, .6f, .2f);
            case "chest": return new Color(1f, .62f, .12f);
            case "rematch": return new Color(.45f, 1f, .6f);
            case "denizen": return new Color(.55f, 1f, .72f);
            case "hubguide": return new Color(.6f, .8f, 1f);
            case "contoso": return new Color(1f, .85f, .35f);
            case "farmkeeper": return new Color(.6f, 1f, .78f);
            case "finalcase": return new Color(1f, .55f, .45f);
            default: return Color.white;
        }
    }

    Dictionary<string, object> ThingHereOrAhead()
    {
        foreach (var t in Things(save.area))
            if (!Gone(t) && I(t["x"]) == save.x && I(t["y"]) == save.y) return t;
        int ax = save.x + save.fx, ay = save.y + save.fy;
        foreach (var t in Things(save.area))
            if (!Gone(t) && I(t["x"]) == ax && I(t["y"]) == ay) return t;
        return null;
    }

    string HintText()
    {
        var t = ThingHereOrAhead();
        if (t == null) return string.IsNullOrEmpty(message) ? "Muevete y pulsa A para interactuar." : message;
        return "A: " + S(t, "kind").ToUpperInvariant() + (string.IsNullOrEmpty(message) ? "" : " · " + message);
    }

    List<Dictionary<string, object>> Things(string area)
    {
        if (IsDungeon(area) && dunThings != null && dunArea == area) return dunThings;
        if (area == "tower") return TowerThings();
        if (area == "studytower") return save.studyFloor >= 1 && dunThings != null && dunArea == "studytower" ? dunThings : StudyLobbyThings();
        var a = D(data["areas"])[area] as Dictionary<string, object>;
        var outList = new List<Dictionary<string, object>>();
        foreach (var t in L(a["things"])) outList.Add(t as Dictionary<string, object>);
        // Simulador de Examen: NPC fijo en la ciudad, accesible en cualquier momento.
        if (area == "town" && examPool != null && examPool.Count > 0)
            outList.Add(new Dictionary<string, object> { { "kind", "examprep" }, { "x", 13 }, { "y", 11 } });
        // Mago del Idioma: alterna español/inglés de las preguntas (qlang).
        if (area == "town")
            outList.Add(new Dictionary<string, object> { { "kind", "langmage" }, { "x", 7 }, { "y", 11 } });
        // Torre del Estudio: escalera SIEMPRE disponible (desde el inicio de la partida).
        if (area == "town" && StudyTowerEnabled())
            outList.Add(new Dictionary<string, object> { { "kind", "studytower" }, { "x", 4 }, { "y", 4 } });
        // La Biblia del Estudio: recompensa por dominar la Torre del Estudio (consulta TODOS los tomos).
        if (area == "town" && StudyTowerEnabled() && save != null && save.trophies != null && save.trophies.Contains("study_tower"))
            outList.Add(new Dictionary<string, object> { { "kind", "studybible" }, { "x", 16 }, { "y", 4 } });
        if (IsHub(area) && save != null && save.cleared != null)
        {
            // En el selector de módulos: un NPC guía anima mientras quedan puertas o jefe de
            // repaso pendiente. Solo cuando la parte está dominada (jefe vencido) desaparece el
            // NPC y aparece el Super Tomo de la parte.
            int part = PartOf(area);
            // El Sabio del hub: SIEMPRE presente (útil por sus consejos), te saluda amistoso y da
            // consejos por módulo. Puedes retarlo cuantas veces quieras; no se marcha del hub.
            outList.Add(new Dictionary<string, object> { { "kind", "hubsage" }, { "part", part }, { "x", 4 }, { "y", 5 } });
            if (PartComplete(part) && PartUnmasteredCount(part) == 0)
            {
                AddSuperTome(outList, "st_p" + part, part, 7, 5);
                // Caso Contoso: tras LEER el Super Tomo, el guardián vuelve con el desafío (4 niveles).
                // El NPC se queda SIEMPRE para poder seguir pidiendo el examen y practicar.
                if (save.readSuper != null && save.readSuper.Contains("st_p" + part))
                    outList.Add(new Dictionary<string, object> { { "kind", "contoso" }, { "part", part }, { "x", 10 }, { "y", 5 } });
            }
            else
                outList.Add(new Dictionary<string, object> { { "kind", "hubguide" }, { "part", part }, { "x", 7 }, { "y", 5 } });
        }
        // Sabio del Examen: aparece en la torre tras vencer al Rey Demonio.
        if (area == "final" && save != null && save.cleared != null && save.cleared.Contains("final"))
        {
            outList.Add(new Dictionary<string, object> { { "kind", "sage" }, { "dom", "exam" }, { "x", 8 }, { "y", 10 } });
            // Torre del Gran Sabio: torre FÍSICA de N pisos (uno por módulo) + el Gran Sabio.
            if (StudyTowerEnabled())
                outList.Add(new Dictionary<string, object> { { "kind", "towerentrance" }, { "x", 4 }, { "y", 11 } });
            // Caso Contoso FINAL: un enemigo (aparente al azar) ofrece el reto definitivo de 10
            // preguntas de TODO el examen. Desaparece al superarlo.
            if (save.metEnemies != null && save.metEnemies.Count > 0 && ContosoCase(0) != null)
            {
                string nm = save.metEnemies[(save.metEnemies.Count * 7 + 3) % save.metEnemies.Count];
                outList.Add(new Dictionary<string, object> { { "kind", "finalcase" }, { "name", nm }, { "x", 12 }, { "y", 11 } });
            }
        }
        // Granja: cada enemigo/jefe vencido aparece para conversar; los jefes ofrecen revancha.
        if (area == "farm" && save != null && save.metEnemies != null)
        {
            outList.Add(new Dictionary<string, object> { { "kind", "farmkeeper" }, { "x", 10 }, { "y", 2 } });
            // JEFE DE REPASO por parte (P1..P4): el Sabio solo aparece en la granja para la revancha
            // cuando lo DERROTAS en el piso 18 (Gran Sabio, trofeo "gran_sabio"). Repasa TODA su parte.
            int[] rbx = { 3, 8, 14, 19 };
            if (save.trophies != null && save.trophies.Contains("gran_sabio"))
                for (int prt = 1; prt <= 4; prt++)
                    outList.Add(new Dictionary<string, object> { { "kind", "reviewboss" }, { "part", prt }, { "x", rbx[prt - 1] }, { "y", 1 } });
            // Habitantes: enemigos/jefes vencidos. Rejilla 11×8 = 88 huecos en la granja 25×19,
            // evitando granjero (10,2), portal (17,12) y spawn (10,12).
            int[] xs = { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22 };
            int[] ys = { 3, 5, 7, 9, 11, 13, 15, 17 };
            int idx = 0;
            foreach (var name in save.metEnemies)
            {
                if (idx >= xs.Length * ys.Length) break;
                int x = xs[idx % xs.Length], y = ys[idx / xs.Length];
                idx++;
                int bz = BossZoneOf(name);
                if (bz >= 0)
                    outList.Add(new Dictionary<string, object> { { "kind", "rematch" }, { "zi", bz }, { "x", x }, { "y", y } });
                else
                    outList.Add(new Dictionary<string, object> { { "kind", "denizen" }, { "name", name }, { "x", x }, { "y", y } });
            }
        }
        return outList;
    }

    // Todos los nombres coleccionables (enemigos + jefes de zonas + guardianes de la torre).
    HashSet<string> AllCollectibleNames()
    {
        var set = new HashSet<string>();
        foreach (var z in L(data["zones"]))
        {
            var zo = z as Dictionary<string, object>;
            if (zo == null) continue;
            if (zo.ContainsKey("enemies"))
                foreach (var e in L(zo["enemies"])) { var en = e as Dictionary<string, object>; if (en != null) set.Add(S(en, "n")); }
            if (zo.ContainsKey("boss")) set.Add(S(D(zo["boss"]), "n"));
        }
        var fa = AreaDict("final");
        if (fa != null && fa.ContainsKey("things"))
            foreach (var o in L(fa["things"])) { var t = o as Dictionary<string, object>; if (t != null && S(t, "kind") == "guardian") set.Add(S(t, "n")); }
        return set;
    }

    // NPC granjero: cuántos monstruos faltan por coleccionar (vencer).
    void RenderFarmKeeper()
    {
        var all = AllCollectibleNames();
        int total = all.Count;
        int got = 0;
        if (save.metEnemies != null) foreach (var n in save.metEnemies) if (all.Contains(n)) got++;
        int left = Math.Max(0, total - got);
        screen = "dialog"; ClearRoot(); PlayTrack("tome");
        AddBackdrop(BackdropForArea("farm"), 0.4f);
        var p = Panel(root, "FarmKeeper", new Color(.05f, .1f, .08f, .95f), Anchor.Stretch, new Vector2(28, 24), new Vector2(-28, -24));
        var col = ScrollColumn(p, 14, 26);
        BigSprite(col, "npc_recepcion", 120, new Color(.7f, 1f, .8f));
        Label(col, T("Granjero de Monstruos", "Monster Rancher"), 24, new Color(.7f, 1f, .8f), FontStyle.Bold);
        Label(col, T("Has reunido " + got + " de " + total + " criaturas.", "You've gathered " + got + " of " + total + " creatures."), 22, Color.white, FontStyle.Bold);
        HpBar(col, T("Colección", "Collection"), got, Math.Max(1, total), new Color(.4f, .9f, .6f));
        Label(col, left > 0
            ? T("Te faltan " + left + " monstruos por vencer. ¡Síguelos cazando para llenar la granja!", left + " monsters left to defeat. Keep hunting to fill the farm!")
            : T("¡Los has reunido a TODOS! Eres el maestro de la granja.", "You gathered them ALL! You are the master of the farm."), 19, left > 0 ? new Color(1f, .85f, .6f) : new Color(.6f, 1f, .75f), FontStyle.Normal);
        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.2f, .3f, .5f));
    }

    // Índice de zona cuyo JEFE se llama 'name' (-1 si no es un jefe).
    int BossZoneOf(string name)
    {
        var zs = L(data["zones"]);
        for (int i = 0; i < zs.Count; i++)
        {
            var zo = zs[i] as Dictionary<string, object>;
            if (zo != null && zo.ContainsKey("boss") && S(D(zo["boss"]), "n") == name) return i;
        }
        return -1;
    }

    // Lore (resumen del tópico) de un enemigo por su nombre.
    string EnemyLoreByName(string name)
    {
        foreach (var z in L(data["zones"]))
        {
            var zo = z as Dictionary<string, object>;
            if (zo == null) continue;
            if (zo.ContainsKey("enemies"))
                foreach (var e in L(zo["enemies"]))
                {
                    var en = e as Dictionary<string, object>;
                    if (en != null && S(en, "n") == name) return TS(en, "lore");
                }
            if (zo.ContainsKey("boss") && S(D(zo["boss"]), "n") == name) return TS(D(zo["boss"]), "lore");
        }
        return "";
    }

    // Pantalla al hablar con un morador de la granja (enemigo vencido): da un resumen del tópico.
    void RenderDenizen(string name)
    {
        screen = "dialog";
        ClearRoot();
        PlayTrack("tome");
        AddBackdrop(BackdropForArea("farm"), 0.4f);
        var p = Panel(root, "Denizen", new Color(.05f, .1f, .08f, .94f), Anchor.Stretch, new Vector2(26, 24), new Vector2(-26, -24));
        var col = ScrollColumn(p, 14, 26);
        BigSprite(col, SpriteForEnemyName(name), 132, new Color(.6f, 1f, .72f));
        Label(col, name + T(" (ahora vive en la granja)", " (now lives on the farm)"), 24, new Color(.7f, 1f, .8f), FontStyle.Bold);
        Label(col, "\"" + EnemyLoreByName(name) + "\"", 20, Color.white, FontStyle.Normal);
        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.2f, .3f, .5f));
    }

    // ---- Jefe de repaso por parte (al volver a la ciudad) ----
    List<int> reviewQueue = new List<int>();
    int reviewPart;
    bool reviewFarm;        // repaso desde la GRANJA: pregunta TODA la parte y solo acumula fallos
    int reviewFarmWrong;    // fallos acumulados en el repaso de granja actual
    bool reviewTower;       // repaso desde la GEMA de un piso de la Torre física (un módulo, hay que dominarlo)
    int reviewModule;       // módulo (dom 1..N) que repasa la gema actual

    int PartUnmasteredCount(int part)
    {
        var all = L(data["bq"]); int n = 0;
        for (int i = 0; i < all.Count; i++)
            if (PartOfDom(I((all[i] as Dictionary<string, object>)["d"])) == part && !save.correctQ.Contains("bq:" + i)) n++;
        return n;
    }

    // Puertas (módulos) de la parte aún no superadas.
    int DoorsLeft(int part)
    {
        int n = 0;
        foreach (var kv in D(data["areas"]))
        {
            var a = kv.Value as Dictionary<string, object>;
            if (a != null && a.ContainsKey("zi") && a.ContainsKey("part") && I(a["part"]) == part && !save.cleared.Contains(kv.Key)) n++;
        }
        return n;
    }

    // Un consejo: la explicación de una pregunta de la parte aún no dominada.
    string PartTip(int part)
    {
        var all = L(data["bq"]);
        var pool = new List<int>();
        for (int i = 0; i < all.Count; i++)
            if (PartOfDom(I((all[i] as Dictionary<string, object>)["d"])) == part && !save.correctQ.Contains("bq:" + i)) pool.Add(i);
        if (pool.Count == 0) return "";
        return QS(all[pool[rng.Next(pool.Count)]] as Dictionary<string, object>, "why");
    }

    // Sprite del guardián de una parte (uno por parte; el de cada hub y el de la torre coinciden).
    string GuardSpr(int part) => "guard_d" + Math.Max(1, Math.Min(part, 4));   // P1..P4 -> guard_d1..d4 (sin repetir)

    string PartTheme(int part)
    {
        switch (part)
        {
            case 1: return T("la IA y Copilot", "AI and Copilot");
            case 2: return T("Microsoft Dataverse", "Microsoft Dataverse");
            case 3: return T("Power Apps y Power Pages", "Power Apps and Power Pages");
            default: return T("Power Automate y AI Builder", "Power Automate and AI Builder");
        }
    }

    // Intro del Guardián del Examen: te reconoce del reino que vigiló antes de pelear.
    void RenderGuardianIntro(Dictionary<string, object> thing)
    {
        int part = thing.ContainsKey("part") ? I(thing["part"]) : 1;
        screen = "dialog"; ClearRoot(); PlayTrack("boss");
        AddBackdrop(BackdropForArea("final"), 0.5f);
        var p = Panel(root, "GuardianIntro", new Color(.1f, .05f, .07f, .96f), Anchor.Stretch, new Vector2(28, 24), new Vector2(-28, -24));
        var col = ScrollColumn(p, 14, 26);
        BigSprite(col, GuardSpr(part), 132, new Color(1f, .6f, .45f));
        Label(col, TS(thing, "n"), 24, new Color(1f, .7f, .5f), FontStyle.Bold);
        Label(col, T("Te estuve observando en la Parte " + part + " (" + PartTheme(part) + "). Sabía que nos enfrentaríamos aquí... ¡Prepárate!",
                     "I watched you in Part " + part + " (" + PartTheme(part) + "). I knew we would clash here... Get ready!"), 21, Color.white, FontStyle.Normal);
        Button(col, T("⚔  ¡PELEAR!", "⚔  FIGHT!"), () => StartBattle(thing, false), new Color(.85f, .3f, .25f));
        Button(col, T("Aún no", "Not yet"), () => RenderWorld(""), new Color(.3f, .2f, .45f));
    }

    // ---- Desafío Caso Contoso (opcional, por parte; aparece tras leer el Súper Tomo) ----
    List<object> contosoQs; int contosoPart, contosoPos, contosoWrong, contosoLevel; string contosoSpr;

    Dictionary<string, object> ContosoCase(int part)
    {
        if (!data.ContainsKey("contoso")) return null;
        foreach (var o in L(data["contoso"]))
        {
            var c = o as Dictionary<string, object>;
            if (c != null && I(c["part"]) == part) return c;
        }
        return null;
    }

    // Niveles de un caso: usa "levels" si existe; si no, envuelve "qs" como un único nivel Fácil.
    List<object> ContosoLevels(Dictionary<string, object> cs)
    {
        if (cs.ContainsKey("levels")) return L(cs["levels"]);
        var one = new Dictionary<string, object> { { "n", "Fácil" }, { "qs", cs.ContainsKey("qs") ? cs["qs"] : new List<object>() } };
        return new List<object> { one };
    }

    // ¿La parte (reino) tiene superado TODO su Caso Contoso (el último nivel)? -> portal con checklist.
    bool ContosoDone(int part)
    {
        var cs = ContosoCase(part);
        if (cs == null || save.contosoCleared == null) return false;
        int n = ContosoLevels(cs).Count;
        return n > 0 && save.contosoCleared.Contains(part + "-" + n);
    }

    void StartContosoLevel(int part, int li, string spr)
    {
        var levels = ContosoLevels(ContosoCase(part));
        if (li < 0 || li >= levels.Count) return;
        contosoPart = part; contosoLevel = li; contosoSpr = spr;
        contosoQs = L((levels[li] as Dictionary<string, object>)["qs"]);
        contosoPos = 0; contosoWrong = 0;
        RenderContosoQuestion();
    }

    void RenderContosoIntro(int part, string sprOverride = null)
    {
        var cs = ContosoCase(part);
        if (cs == null) { RenderWorld(T("El caso aún no está disponible.", "The case isn't available yet.")); return; }
        screen = "dialog"; ClearRoot(); PlayTrack("sage");
        AddBackdrop(BackdropForArea(part >= 1 && part <= 4 ? "p" + part : "final"), 0.45f);
        var p = Panel(root, "ContosoIntro", new Color(.05f, .07f, .13f, .96f), Anchor.Stretch, new Vector2(28, 22), new Vector2(-28, -22));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .85f, .35f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 24);
        BigSprite(col, sprOverride ?? (part >= 1 && part <= 4 ? GuardSpr(part) : "npc_sabio"), 116, new Color(1f, .85f, .35f));
        Label(col, T("Desafío Caso Contoso · ", "Contoso Case Challenge · ") + S(cs, "titulo"), 24, new Color(1f, .85f, .35f), FontStyle.Bold);
        Label(col, T("Lee el caso y luego propón soluciones. Acierta TODAS para superarlo (es opcional).", "Read the case, then propose solutions. Get them ALL right to pass (optional)."), 16, new Color(1f, .8f, .6f), FontStyle.Bold);
        var caso = Label(col, S(cs, "caso"), 19, Color.white, FontStyle.Normal); caso.alignment = TextAnchor.UpperLeft;
        Label(col, T("Elige un nivel de dificultad. Acierta TODAS para superarlo. Puedes practicar las veces que quieras (no desaparezco).",
                     "Pick a difficulty. Get them ALL right to pass. Practice as much as you like (I won't leave)."), 15, new Color(1f, .8f, .6f), FontStyle.Normal);
        var levels = ContosoLevels(cs);
        string sp = sprOverride ?? (part >= 1 && part <= 4 ? GuardSpr(part) : "npc_sabio");
        string[] defNames = { "Fácil", "Medio", "Difícil", "Experto" };
        for (int i = 0; i < levels.Count; i++)
        {
            int li = i; int lvNum = i + 1;
            var lv = levels[i] as Dictionary<string, object>;
            string nm = lv.ContainsKey("n") ? S(lv, "n") : (i < defNames.Length ? defNames[i] : "Nivel " + lvNum);
            int lq = L(lv["qs"]).Count;
            bool done = save.contosoCleared.Contains(part + "-" + lvNum);
            bool unlocked = lvNum == 1 || save.contosoCleared.Contains(part + "-" + (lvNum - 1));
            string lbl = "Nv." + lvNum + " · " + nm + " (" + lq + ")" + (done ? "  ✓" : "");
            if (unlocked)
                Button(col, lbl, () => StartContosoLevel(part, li, sp), done ? new Color(.2f, .5f, .35f) : new Color(.95f, .72f, .12f));
            else
                Label(col, "🔒 " + lbl + T(" — supera el nivel anterior", " — clear the previous level"), 16, new Color(.7f, .7f, .8f), FontStyle.Bold);
        }
        Button(col, T("AHORA NO", "NOT NOW"), () => RenderWorld(""), new Color(.3f, .2f, .45f));
    }

    void RenderContosoQuestion()
    {
        if (contosoQs == null) { RenderWorld(""); return; }
        if (contosoPos >= contosoQs.Count)
        {
            screen = "dialog"; ClearRoot(); PlayTrack("town");
            AddBackdrop(BackdropForArea(contosoPart >= 1 && contosoPart <= 4 ? "p" + contosoPart : "final"), 0.45f);
            var pe = Panel(root, "ContosoEnd", new Color(.05f, .08f, .12f, .96f), Anchor.Stretch, new Vector2(28, 24), new Vector2(-28, -24));
            var ce = ScrollColumn(pe, 14, 26);
            if (contosoWrong == 0)
            {
                int lvNum = contosoLevel + 1;
                string ck = contosoPart + "-" + lvNum;
                if (save.contosoCleared == null) save.contosoCleared = new List<string>();
                if (!save.contosoCleared.Contains(ck)) save.contosoCleared.Add(ck);
                if (save.contosoDone == null) save.contosoDone = new List<int>();
                if (!save.contosoDone.Contains(contosoPart)) save.contosoDone.Add(contosoPart);
                int xp = (40 * lvNum) + (contosoPart == 0 ? 120 : 0);
                GainXp(xp); PlaySfx(sfxWin); SaveSlot();
                BigSprite(ce, contosoSpr ?? (contosoPart >= 1 && contosoPart <= 4 ? GuardSpr(contosoPart) : "boss_final"), 110, new Color(.55f, 1f, .7f));
                Label(ce, T("¡NIVEL " + lvNum + " RESUELTO!", "LEVEL " + lvNum + " SOLVED!"), 28, new Color(.6f, 1f, .75f), FontStyle.Bold);
                Label(ce, T("Diste con la solución. +" + xp + " XP. El siguiente nivel está disponible; vuelve a hablarme para practicar.",
                            "You nailed it. +" + xp + " XP. The next level is unlocked; talk to me again to practice."), 20, Color.white, FontStyle.Normal);
            }
            else
            {
                Label(ce, T("EL CASO NECESITA MÁS TRABAJO", "THE CASE NEEDS MORE WORK"), 26, new Color(1f, .6f, .45f), FontStyle.Bold);
                Label(ce, T("Fallaste " + contosoWrong + " solución(es). Repasa el Súper Tomo y reinténtalo cuando quieras.", "You missed " + contosoWrong + " solution(s). Review the Super Tome and retry whenever you like."), 20, Color.white, FontStyle.Normal);
            }
            Button(ce, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.3f, .2f, .45f));
            return;
        }
        var q = contosoQs[contosoPos] as Dictionary<string, object>;
        RenderQuestion(q, correct => { if (!correct) contosoWrong++; contosoPos++; RenderContosoQuestion(); });
    }

    // El SABIO del hub: te saluda SIEMPRE con amabilidad (para que el giro final sea más chocante)
    // y da un consejo por cada módulo de la parte (página por módulo, scrolleable). Puedes retarlo;
    // al derrotarlo desaparece del hub y pasa a la granja para la revancha.
    void RenderHubSage(int part)
    {
        screen = "dialog"; ClearRoot(); PlayTrack("town");
        AddBackdrop(BackdropForArea("p" + part), 0.4f);
        var p = Panel(root, "HubSage", new Color(.05f, .06f, .14f, .96f), Anchor.Stretch, new Vector2(24, 18), new Vector2(-24, -18));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, .82f, .22f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 12, 24);
        BigSprite(col, "npc_sabio", 110, new Color(1f, .82f, .35f));
        Label(col, T("El Sabio · Reino " + part, "The Sage · Realm " + part), 24, new Color(1f, .85f, .35f), FontStyle.Bold);
        Label(col, T("¡Hola, querido amigo! Qué alegría verte. Déjame ayudarte a dominar cada módulo de este reino.",
                     "Hello, dear friend! What a joy to see you. Let me help you master every module of this realm."), 18, new Color(.9f, .95f, 1f), FontStyle.Italic);
        // Una página por módulo de la parte: nombre del módulo + consejo (la idea clave del módulo).
        int n = 0;
        foreach (var z in L(data["zones"]))
        {
            var zo = z as Dictionary<string, object>;
            if (zo == null || !zo.ContainsKey("dom") || PartOfDom(I(zo["dom"])) != part) continue;
            n++;
            Chip(col, "✦ " + T("Módulo ", "Module ") + n + ": " + TS(zo, "name"), new Color(0f, 0f, 0f, .3f), new Color(1f, .82f, .35f), 15, 42);
            string tip = zo.ContainsKey("boss") ? TS(D(zo["boss"]), "lore") : TS(zo, "sub");
            var tl = Label(col, T("Consejo: ", "Tip: ") + tip, 16, new Color(.85f, .92f, 1f), FontStyle.Normal);
            tl.alignment = TextAnchor.UpperLeft;
        }
        Button(col, T("⚔  RETAR AL SABIO (preguntas del reino)", "⚔  CHALLENGE THE SAGE (realm questions)"), () => StartHubSageFight(part), new Color(.5f, .2f, .24f));
        Button(col, T("GRACIAS, SABIO", "THANK YOU, SAGE"), () => RenderWorld(""), new Color(.3f, .2f, .45f));
    }

    void StartHubSageFight(int part)
    {
        var enemy = new Dictionary<string, object> {
            { "n", "El Sabio del Reino " + part }, { "n_en", "The Sage of Realm " + part },
            { "hp", 120 }, { "atk", 11 }, { "xp", 70 },
            { "lore", "El Sabio sonríe con calidez incluso en combate." }, { "lore_en", "The Sage smiles warmly even in battle." }
        };
        battle = new Battle { thingKey = "hubsage", enemy = enemy, hp = 120, maxhp = 120, dom = 0, part = part, spr = "npc_sabio", qn = 2 };
        RenderBattle(T("El Sabio: «¡Será un placer medirme contigo, amigo! Preguntas de todo el Reino " + part + ".»",
                       "The Sage: \"It will be my pleasure to test you, friend! Questions from all of Realm " + part + ".\""));
    }

    void HubSageWon()
    {
        int part = battle.part;
        if (save.sagesDown == null) save.sagesDown = new List<int>();
        if (!save.sagesDown.Contains(part)) save.sagesDown.Add(part);
        SaveSlot();
        RenderWorld(T("El Sabio del Reino " + part + ": «¡Magnífico, amigo! Sabía que llegarías lejos... más de lo que imaginas. Vuelve cuando quieras: aquí seguiré con mis consejos.»",
                      "The Sage of Realm " + part + ": \"Magnificent, friend! I knew you'd go far... farther than you imagine. Come back anytime: I'll be here with my advice.\""));
    }

    // NPC del selector de módulos: anima ("te faltan N puertas") o, si el jefe de repaso está
    // pendiente, avisa de su poder (preguntas por dominar) y da un consejo.
    void RenderHubGuide(int part)
    {
        screen = "dialog"; ClearRoot(); PlayTrack("town");
        AddBackdrop(BackdropForArea("p" + part), 0.4f);
        var p = Panel(root, "HubGuide", new Color(.05f, .08f, .14f, .95f), Anchor.Stretch, new Vector2(28, 24), new Vector2(-28, -24));
        var col = ScrollColumn(p, 14, 26);
        BigSprite(col, GuardSpr(part), 120, new Color(.7f, .85f, 1f));
        if (PartComplete(part) && PartUnmasteredCount(part) > 0)
        {
            int pwr = PartUnmasteredCount(part);
            Label(col, T("Guardián del Reino", "Realm Warden"), 24, new Color(.8f, .9f, 1f), FontStyle.Bold);
            Label(col, T("No pude contenerlo, lo siento... es muy fuerte. Solo te dejará llegar a la ciudad cuando lo domines.", "I couldn't hold it back, sorry... it's too strong. It will only let you reach town once you master it."), 20, Color.white, FontStyle.Normal);
            Label(col, T("Lo poderoso que es ahora mismo: " + pwr + " preguntas por dominar.", "How powerful it is right now: " + pwr + " questions left to master."), 19, new Color(1f, .7f, .5f), FontStyle.Bold);
            string tip = PartTip(part);
            if (tip != "") Label(col, T("Un consejo: ", "A tip: ") + tip, 18, new Color(.7f, 1f, .8f), FontStyle.Normal);
        }
        else
        {
            int n = DoorsLeft(part);
            Label(col, T("Guía del Reino", "Realm Guide"), 24, new Color(.8f, .9f, 1f), FontStyle.Bold);
            Label(col, n > 0
                ? T("¡Vas muy bien! Te faltan " + n + (n == 1 ? " puerta" : " puertas") + " en este reino.", "You're doing great! " + n + (n == 1 ? " door" : " doors") + " left in this realm.")
                : T("¡Has cruzado todas las puertas! Prepárate para el reto final del reino.", "You've cleared every door! Get ready for the realm's final challenge."), 20, Color.white, FontStyle.Normal);
        }
        Button(col, T("VOLVER", "BACK"), () => RenderWorld(""), new Color(.3f, .2f, .45f));
    }

    // Decide qué pasa al intentar volver a la ciudad desde un hub o mazmorra: si esa parte está
    // completa y aún quedan preguntas sin dominar, aparece el jefe de repaso; si no, vas a la ciudad.
    void TownReturnGate(string fromArea)
    {
        int part = (IsHub(fromArea) || IsDungeon(fromArea)) ? PartOf(fromArea) : 0;
        if (part >= 1 && part <= 4 && PartComplete(part) && PartUnmasteredCount(part) > 0) { StartPartReview(part); return; }
        ExitToTown();
    }

    void StartPartReview(int part)
    {
        reviewFarm = false; reviewTower = false;   // repaso del HUB: solo las preguntas sin dominar, hay que dominarlas
        reviewPart = part;
        reviewQueue = new List<int>();
        var all = L(data["bq"]);
        for (int i = 0; i < all.Count; i++)
            if (PartOfDom(I((all[i] as Dictionary<string, object>)["d"])) == part && !save.correctQ.Contains("bq:" + i)) reviewQueue.Add(i);
        for (int i = reviewQueue.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = reviewQueue[i]; reviewQueue[i] = reviewQueue[j]; reviewQueue[j] = t; }
        RenderPartReview();
    }

    // Repaso desde la GRANJA: el jefe de repaso te pregunta TODOS los módulos de su parte
    // (no solo lo no dominado) en una pasada, y SOLO acumula las que fallas (a la lista de Dudas).
    void StartFarmPartReview(int part)
    {
        reviewFarm = true; reviewTower = false; reviewFarmWrong = 0;
        reviewPart = part;
        reviewQueue = new List<int>();
        var all = L(data["bq"]);
        for (int i = 0; i < all.Count; i++)
            if (PartOfDom(I((all[i] as Dictionary<string, object>)["d"])) == part) reviewQueue.Add(i);
        for (int i = reviewQueue.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = reviewQueue[i]; reviewQueue[i] = reviewQueue[j]; reviewQueue[j] = t; }
        RenderPartReview();
    }

    void RenderPartReview()
    {
        if (reviewQueue.Count == 0)
        {
            if (reviewTower)
            {
                if (!save.towerDone.Contains(reviewModule)) save.towerDone.Add(reviewModule);
                reviewTower = false;
                PlaySfx(sfxWin); GainXp(60);
                save.area = "tower"; save.x = TowerStart[0]; save.y = TowerStart[1]; save.fx = 0; save.fy = -1;
                SaveSlot();
                screen = "dialog"; ClearRoot(); PlayTrack("final");
                AddBackdrop(BackdropForArea("final"), 0.5f);
                var pgt = Panel(root, "TowerGemWin", new Color(.05f, .08f, .12f, .96f), Anchor.Stretch, new Vector2(28, 24), new Vector2(-28, -24));
                var olg = pgt.gameObject.AddComponent<Outline>(); olg.effectColor = new Color(1f, .82f, .22f, .8f); olg.effectDistance = new Vector2(3, -3);
                var cgt = ScrollColumn(pgt, 14, 26);
                var mz = TowerZone(reviewModule);
                string mnm = mz != null ? TS(mz, "name") : ("MD" + reviewModule);
                BigSprite(cgt, MedalSprite(reviewModule), 132, new Color(1f, .85f, .35f));
                Label(cgt, T("🏅 ¡MEDALLA DEL MÓDULO!", "🏅 MODULE MEDAL!"), 25, new Color(1f, .82f, .22f), FontStyle.Bold);
                Label(cgt, T("Piso " + save.towerFloor + " · " + mnm, "Floor " + save.towerFloor + " · " + mnm), 19, new Color(.85f, .92f, 1f), FontStyle.Bold);
                Label(cgt, T("Dominaste la gema: respondiste bien TODAS las preguntas del módulo. Recibes su MEDALLA y el ascensor ya puede SUBIR. +60 XP.",
                             "You mastered the gem: answered ALL the module's questions. You receive its MEDAL and the elevator can now go UP. +60 XP."), 18, Color.white, FontStyle.Normal);
                Button(cgt, T("AL ASCENSOR ▲", "TO THE ELEVATOR ▲"), () => RenderWorld(T("Usa el ascensor (al norte) para subir de piso.", "Use the elevator (to the north) to go up a floor.")), new Color(.95f, .72f, .12f));
                return;
            }
            if (reviewFarm)
            {
                PlaySfx(sfxWin); GainXp(40); SaveSlot();
                screen = "dialog"; ClearRoot(); PlayTrack("tome");
                AddBackdrop(BackdropForArea("farm"), 0.45f);
                var pf = Panel(root, "FarmReviewWin", new Color(.05f, .1f, .08f, .95f), Anchor.Stretch, new Vector2(28, 24), new Vector2(-28, -24));
                var cf = ScrollColumn(pf, 14, 26);
                Label(cf, T("REPASO DE LA PARTE " + reviewPart + " COMPLETO", "PART " + reviewPart + " REVIEW COMPLETE"), 25, new Color(1f, .85f, .35f), FontStyle.Bold);
                Label(cf, reviewFarmWrong > 0
                    ? T("Fallaste " + reviewFarmWrong + " preguntas: las guardé en tus Dudas para repasar. +40 XP.", "You missed " + reviewFarmWrong + " questions: saved to your Doubts for review. +40 XP.")
                    : T("¡Sin fallos! Dominas toda la Parte " + reviewPart + ". +40 XP.", "Flawless! You master all of Part " + reviewPart + ". +40 XP."), 20, Color.white, FontStyle.Normal);
                Button(cf, T("VOLVER A LA GRANJA", "BACK TO THE FARM"), () => RenderWorld(""), new Color(.2f, .55f, .35f));
                return;
            }
            PlaySfx(sfxWin); GainXp(80); SaveSlot();
            screen = "dialog"; ClearRoot(); PlayTrack("town");
            AddBackdrop(PartBackdrop(reviewPart), 0.5f);
            var pv = Panel(root, "ReviewWin", new Color(.05f, .1f, .08f, .95f), Anchor.Stretch, new Vector2(28, 24), new Vector2(-28, -24));
            var cv = ScrollColumn(pv, 14, 26);
            Label(cv, T("¡PARTE " + reviewPart + " DOMINADA!", "PART " + reviewPart + " MASTERED!"), 28, new Color(1f, .85f, .35f), FontStyle.Bold);
            Label(cv, T("Respondiste bien todas sus preguntas. El jefe te deja pasar. +80 XP.", "You answered all its questions. The boss lets you pass. +80 XP."), 20, Color.white, FontStyle.Normal);
            Button(cv, T("IR A LA CIUDAD ▶", "GO TO TOWN ▶"), ExitToTown, new Color(.95f, .72f, .12f));
            return;
        }
        screen = "review-boss";
        uiDom = reviewPart <= 3 ? reviewPart : 3;
        ClearRoot();
        PlayTrack("boss");
        AddBackdrop(PartBackdrop(reviewPart), 0.55f);
        var p = Panel(root, "ReviewBoss", new Color(.08f, .04f, .06f, .96f), Anchor.Stretch, new Vector2(20, 16), new Vector2(-20, -16));
        var col = ScrollColumn(p, 10, 22);
        if (reviewTower)
        {
            var tz = TowerZone(reviewModule);
            BigSprite(col, "tower_gem", 104, new Color(.6f, 1f, .9f));
            Label(col, "♦ " + T("GEMA · PISO ", "GEM · FLOOR ") + save.towerFloor + " · " + (tz != null ? TS(tz, "name") : ("MD" + reviewModule)), 22, new Color(1f, .82f, .4f), FontStyle.Bold);
            Label(col, T("Domina TODO el módulo para ganar su MEDALLA y habilitar el ascensor. Quedan " + reviewQueue.Count + ". Las que aciertes quedan dominadas; las que falles, volverán.",
                         "Master the WHOLE module to earn its MEDAL and enable the elevator. " + reviewQueue.Count + " left. Correct ones are mastered; missed ones return."), 16, new Color(.8f, 1f, .9f), FontStyle.Bold);
        }
        else
        {
        BigSprite(col, "boss_d" + (((reviewPart - 1) % 3) + 1), 104, new Color(1f, .55f, .45f));
        Label(col, (reviewFarm ? T("JEFE REPASO P", "REVIEW BOSS P") + reviewPart + T(" · GRANJA", " · FARM") : T("JEFE DE REPASO · PARTE ", "REVIEW BOSS · PART ") + reviewPart), 22, new Color(1f, .6f, .45f), FontStyle.Bold);
        Label(col, reviewFarm
            ? T("Repaso de TODA la Parte " + reviewPart + ". Quedan " + reviewQueue.Count + ". Las que FALLES se guardan en tus Dudas.",
                "Full Part " + reviewPart + " review. " + reviewQueue.Count + " left. MISSED ones go to your Doubts.")
            : T("Domina sus preguntas para pasar. Te quedan " + reviewQueue.Count + ". Las que aciertes quedan dominadas; las que falles, volverán.",
                "Master its questions to pass. " + reviewQueue.Count + " left. Correct ones are mastered; missed ones return."), 16, new Color(1f, .8f, .6f), FontStyle.Bold);
        }
        int n = Math.Min(reviewTower ? 15 : 2, reviewQueue.Count);   // la GEMA pregunta de a 15 (y el resto)
        var evals = new List<Func<bool?>>();
        var idxs = new List<int>();
        for (int i = 0; i < n; i++)
        {
            int qi = reviewQueue[i]; idxs.Add(qi);
            var qq = L(data["bq"])[qi] as Dictionary<string, object>;
            MarkSeen("bq:" + qi);
            Label(col, "━━━  " + T("PREGUNTA", "QUESTION") + " " + (i + 1) + "  ━━━", 17, new Color(1f, .8f, .55f), FontStyle.Bold);
            evals.Add(BuildQuestionBody(col, qq));
        }
        var warn = Label(col, " ", 15, new Color(1f, .65f, .45f), FontStyle.Bold);
        bool done = false;
        OptButton(col, T("⚔  RESPONDER", "⚔  ANSWER"), () =>
        {
            if (done) return;
            var res = new List<bool?>();
            foreach (var ev in evals) res.Add(ev());
            for (int i = 0; i < res.Count; i++)
                if (res[i] == null) { warn.text = T("⚠ Completa la pregunta " + (i + 1) + ".", "⚠ Complete question " + (i + 1) + "."); return; }
            done = true;
            var toBack = new List<int>();
            for (int i = 0; i < n; i++)
            {
                int qi = idxs[i]; string k = "bq:" + qi;
                if (res[i] == true) { MarkCorrect(k); save.wrongQ.Remove(k); }
                else { BumpWrong(k); if (!save.wrongQ.Contains(k)) save.wrongQ.Add(k); toBack.Add(qi); }
            }
            reviewQueue.RemoveRange(0, n);
            if (!reviewFarm) foreach (var qi in toBack) reviewQueue.Add(qi);   // granja: una sola pasada
            else reviewFarmWrong += toBack.Count;                              // granja: cuenta los fallos
            SaveSlot();
            RenderPartReview();
        }, new Color(.85f, .3f, .25f));
        Button(col, reviewTower ? T("DEJAR LA GEMA", "LEAVE THE GEM") : reviewFarm ? T("VOLVER A LA GRANJA", "BACK TO THE FARM") : T("RETIRARSE AL HUB", "RETREAT TO HUB"), () =>
        {
            if (reviewTower)   // la gema no guarda progreso parcial: hay que dominarla de una pasada
            {
                reviewTower = false;
                save.area = "tower"; save.x = TowerStart[0]; save.y = TowerStart[1]; save.fx = 0; save.fy = -1;
                SaveSlot(); RenderWorld(T("Sueltas la gema. Tendrás que repasar el módulo entero de nuevo.", "You release the gem. You'll have to review the whole module again."));
                return;
            }
            if (reviewFarm) { RenderWorld(""); return; }   // ya estás en la granja
            save.area = "p" + reviewPart;
            var st = L(AreaDict(save.area)["start"]); save.x = I(st[0]); save.y = I(st[1]);
            SaveSlot(); RenderWorld(T("Te retiras. El jefe te esperará a la salida.", "You retreat. The boss will wait at the exit."));
        }, new Color(.3f, .2f, .45f));
    }

    void AddSage(List<Dictionary<string, object>> list, string dom, int x, int y)
    {
        if (!save.cleared.Contains(dom)) return;   // el sabio solo aparece tras vencer al jefe
        list.Add(new Dictionary<string, object> { { "kind", "sage" }, { "dom", dom }, { "x", x }, { "y", y } });
    }

    void AddRematch(List<Dictionary<string, object>> list, string areaId, int zi, int x, int y)
    {
        if (!save.cleared.Contains(areaId)) return;
        list.Add(new Dictionary<string, object> { { "kind", "rematch" }, { "zi", zi }, { "x", x }, { "y", y } });
    }

    void AddSuperTome(List<Dictionary<string, object>> list, string id, int unlockPart, int x, int y)
    {
        if (superData.Count == 0 || !PartComplete(unlockPart)) return;
        list.Add(new Dictionary<string, object> { { "kind", "supertome" }, { "id", id }, { "x", x }, { "y", y } });
    }

    bool Gone(Dictionary<string, object> t) => save.goneThings.Contains(ThingKey(t));
    void MarkGone(Dictionary<string, object> t) => MarkGoneByKey(ThingKey(t));
    void MarkGoneByKey(string key) { if (!save.goneThings.Contains(key)) save.goneThings.Add(key); }
    string ThingKey(Dictionary<string, object> t) => save.area + "|" + S(t, "kind") + "|" + I(t["x"]) + "|" + I(t["y"]);

    string AreaName(string area)
    {
        var a = AreaDict(area);
        if (a != null && a.ContainsKey("name")) return S(a, "name");
        switch (area)
        {
            case "town": return "Ciudad de Power Platform";
            case "final": return T("Torre del Examen ", "Exam Tower ") + ExamShort();
            case "tower": return T("Torre del Gran Sabio", "Tower of the Great Sage");
            case "studytower": return save.studyFloor >= 1 ? T("Torre del Estudio · Piso " + save.studyFloor, "Tower of Study · Floor " + save.studyFloor) : T("Torre del Estudio", "Tower of Study");
            default: return area;
        }
    }

    // Tinte de ambiente por zona: multiplica los sprites de suelo/pared para que cada
    // mazmorra tenga su propia paleta (fría, musgosa, violeta, ardiente).
    Color AreaTileTint(string area)
    {
        if (area == "final") return new Color(1f, .74f, .7f);   // Torre: rojo ascua
        if (IsDungeon(area))
        {
            switch (PartOf(area))
            {
                case 1: return new Color(.76f, .88f, 1f);   // P1 IA/Copilot: azul acero
                case 2: return new Color(.78f, 1f, .82f);   // P2 Dataverse: verde datos
                case 3: return new Color(.95f, .8f, 1f);    // P3 Power Apps/Pages: violeta
                default: return new Color(1f, .86f, .68f);  // P4 Power Automate: ámbar
            }
        }
        return Color.white;   // ciudad y hubs sin tinte
    }

    Color TileColor(char c)
    {
        if (c == '#') return new Color(.08f, .1f, .18f, .98f);
        if (c == '~') return new Color(.16f, .45f, .72f, .98f);   // agua de fuente, más viva
        if (c == ',') return new Color(.08f, .26f, .14f, .98f);
        if (c == 'T') return new Color(.07f, .22f, .12f, .98f);   // césped bajo el árbol
        if (c == '+') return new Color(.28f, .19f, .11f, .98f);
        return new Color(.12f, .13f, .17f, .98f);
    }

    string BackdropForArea(string area)
    {
        if (area == "town") return "Art/External/TownPlaza";
        if (area == "farm") return "Art/External/MonsterFarm";
        if (area == "tower") return "Art/External/TowerSage";   // arte propio de la Torre del Gran Sabio
        if (area == "studytower") return Resources.Load<Texture2D>("Art/External/TowerStudy") != null ? "Art/External/TowerStudy" : "Art/External/TowerSage";   // fondo de biblioteca propio
        if (area == "final") return "Art/External/BattleThrone";
        return PartBackdrop(IsDungeon(area) || IsHub(area) ? PartOf(area) : 1);
    }

    // Fondo por parte (1..4). Reutiliza los 4 fondos heredados hasta tener arte propio.
    string PartBackdrop(int part)
    {
        switch (part)
        {
            case 1: return "Art/External/BattleBastion";
            case 2: return "Art/External/BattleCrypt";
            case 3: return "Art/External/BattleFactory";
            default: return "Art/External/BattleThrone";
        }
    }

    string BattleBackdrop()
    {
        if (battle == null) return BackdropForArea(save.area);
        if (battle.dom == 0) return "Art/External/BattleThrone";   // Rey/examen: cualquier dominio
        return PartBackdrop(PartOfDom(battle.dom));
    }

    void ClearRoot()
    {
        if (root != null) Destroy(root.gameObject);
        var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = canvas.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        root = canvas.GetComponent<RectTransform>();
    }

    void AddBackdrop(string resource, float alpha)
    {
        var tex = Resources.Load<Texture2D>(resource);
        if (tex == null) return;
        var go = new GameObject("Backdrop", typeof(Image));
        go.transform.SetParent(root, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
        img.preserveAspect = false;
        img.color = new Color(1f, 1f, 1f, alpha);
    }

    // ---------------- Sprites de jefes generados por código (únicos) ----------------
    const int BS = 48;

    static Color32 BC(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

    void BuildBossSprites()
    {
        CacheGenerated("boss_d1", DrawDragon);       // Dragón de Entra
        CacheGenerated("boss_d2", DrawHydra);        // Hidra de Purview
        CacheGenerated("boss_d3", DrawCore);         // El Núcleo de Copilot
        CacheGenerated("boss_final", DrawDemonKing); // Rey Demonio AB-900 (jefe final)
        CacheGenerated("guard_d1", DrawSentinelGuard); // Centinela de la Identidad
        CacheGenerated("guard_d2", DrawWardenGuard);   // Custodio del Cumplimiento
        CacheGenerated("guard_d3", DrawVigilGuard);    // Vigía de los Agentes
        CacheGenerated("icon_d1", DrawShieldIcon);   // Emblema Zero Trust (escudo)
        CacheGenerated("icon_d2", DrawGemIcon);      // Emblema Purview (gema)
        CacheGenerated("icon_d3", DrawSparkIcon);    // Emblema Copilot (chispa)
        CacheGenerated("deco_d1", DrawTorchDeco);    // Atrezzo: antorcha (Bastión)
        CacheGenerated("deco_d2", DrawCrystalDeco);  // Atrezzo: cristales (Cripta)
        CacheGenerated("deco_d3", DrawGearDeco);     // Atrezzo: engranaje (Fábrica)
        CacheGenerated("npc_langmage", DrawLangMage);// Mago del Idioma (túnica bicolor)
        CacheGenerated("tower_gem", DrawTowerGem);   // Torre física: gema de repaso del piso
        CacheGenerated("tower_stairs", DrawTowerStairs); // Torre física: escalera al piso siguiente
    }

    // Gema de repaso de la Torre: diamante cian con núcleo dorado y destello.
    void DrawTowerGem(Color32[] b)
    {
        var edge = BC(18, 96, 120); var mid = BC(54, 196, 224); var lite = BC(170, 246, 255); var core = BC(255, 232, 150);
        for (int y = 6; y <= 42; y++)
        {
            int half = y <= 24 ? (y - 6) + 2 : 20 - (y - 24);
            if (half < 1) break;
            for (int x = 24 - half; x <= 24 + half; x++)
                BPx(b, x, y, (x == 24 - half || x == 24 + half) ? edge : (x < 24 ? (y < 24 ? lite : mid) : (y < 24 ? mid : edge)));
        }
        BLine(b, 24, 6, 24, 42, 0, edge);                // arista central
        BLine(b, 6, 24, 42, 24, 0, edge);                // cintura
        BDisc(b, 24, 24, 4, core);                       // núcleo dorado
        BPx(b, 15, 15, BC(255, 255, 255)); BPx(b, 16, 16, BC(255, 255, 255));   // destello
    }

    // Escalera ascendente: peldaños de piedra clara que suben hacia el norte.
    void DrawTowerStairs(Color32[] b)
    {
        var stone = BC(150, 150, 168); var litef = BC(206, 206, 224); var dark = BC(78, 78, 96);
        for (int s = 0; s < 5; s++)
        {
            int y0 = 38 - s * 6, y1 = y0 - 5;
            int x0 = 8 + s * 3, x1 = 40 - s * 3;
            for (int y = y1; y <= y0; y++)
                for (int x = x0; x <= x1; x++)
                    BPx(b, x, y, y == y1 ? litef : (x == x0 || x == x1 ? dark : stone));
        }
        BPx(b, 24, 6, BC(255, 232, 150)); BPx(b, 23, 7, BC(255, 232, 150)); BPx(b, 25, 7, BC(255, 232, 150)); // brillo en la cúspide
    }

    // El PNG remasterizado de Resources/Art/Tiles manda; el dibujo por código es el fallback.
    void CacheGenerated(string key, Action<Color32[]> draw)
    {
        if (LoadSprite(key) == null) spriteCache[key] = MakeBoss(draw);
    }

    // Mago del Idioma: túnica mitad dorada (ES) mitad azul (EN), sombrero estrellado y báculo.
    void DrawLangMage(Color32[] b)
    {
        var skin = BC(255, 213, 170);
        var robeL = BC(228, 166, 42);   // mitad dorada: español
        var robeR = BC(58, 108, 218);   // mitad azul: inglés
        var hat = BC(96, 62, 168);
        for (int y = 22; y <= 44; y++)
        {
            int half = 4 + (y - 22) / 2;
            for (int x = 24 - half; x <= 24 + half; x++) BPx(b, x, y, x < 24 ? robeL : robeR);
        }
        BDisc(b, 24, 17, 6, skin);                               // cara
        BPx(b, 22, 16, BC(30, 30, 40)); BPx(b, 26, 16, BC(30, 30, 40));
        BHorn(b, 24, 12, 0, 10, hat);                            // cono del sombrero
        BRect(b, 14, 11, 34, 13, hat);                           // ala
        BPx(b, 26, 5, BC(255, 240, 150));                        // estrella
        BRect(b, 38, 18, 39, 44, BC(110, 78, 48));               // báculo
        BDisc(b, 38, 15, 3, BC(140, 240, 200));                  // orbe
    }

    void RenderLangMage()
    {
        screen = "dialog";
        ClearRoot();
        AddBackdrop(BackdropForArea("town"), 0.4f);
        var p = Panel(root, "LangMage", new Color(.08f, .05f, .14f, .95f), Anchor.Stretch, new Vector2(26, 24), new Vector2(-26, -24));
        var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.7f, .5f, 1f, .6f); ol.effectDistance = new Vector2(3, -3);
        var col = ScrollColumn(p, 14, 26);
        BigSprite(col, "npc_langmage", 132, new Color(.7f, .5f, 1f));
        Label(col, T("Mago del Idioma", "Language Mage"), 26, new Color(1f, .82f, .22f), FontStyle.Bold);
        bool qen = QEN;
        Label(col, T(
            "Mis hechizos cruzan todas las lenguas. Ahora mismo las PREGUNTAS están en " + (qen ? "INGLÉS (el idioma original del examen)" : "ESPAÑOL") + ". ¿Quieres que las invierta?",
            "My spells cross every tongue. Right now the QUESTIONS are in " + (qen ? "ENGLISH (the exam's original language)" : "SPANISH") + ". Shall I flip them?"),
            20, Color.white, FontStyle.Normal);
        bool resuming = save.stashLang == (qen ? "es" : "en");
        Label(col, T(
            "⚠ CADA IDIOMA TIENE SU PROPIA AVENTURA: si cambio el idioma, guardo tu progreso actual (mazmorras, tomos, medallones, estadísticas) y " + (resuming ? "RETOMAS tu aventura en el otro idioma donde la dejaste" : "EMPIEZAS una aventura nueva en el otro idioma") + ". Conservas nivel, vida, objetos y trofeos. Despertarás en la ciudad.",
            "⚠ EACH LANGUAGE HAS ITS OWN ADVENTURE: if I switch the language, I store your current progress (dungeons, tomes, medallions, stats) and you " + (resuming ? "RESUME your adventure in the other language where you left it" : "START a fresh adventure in the other language") + ". You keep level, HP, items and trophies. You will wake up in town."),
            16, new Color(1f, .8f, .5f), FontStyle.Bold);
        Button(col, T(qen ? "✦ CAMBIAR A ESPAÑOL" : "✦ CAMBIAR A INGLÉS", qen ? "✦ SWITCH TO SPANISH" : "✦ SWITCH TO ENGLISH"), () =>
        {
            SwapLangProgress(qen ? "es" : "en");
            RenderWorld(T(
                "El mago agita su báculo: tu aventura continúa en " + (save.qlang == "en" ? "INGLÉS" : "ESPAÑOL") + ".",
                "The mage waves his staff: your adventure continues in " + (save.qlang == "en" ? "ENGLISH" : "SPANISH") + "."));
        }, new Color(.55f, .3f, .8f));
        Button(col, T("AHORA NO", "NOT NOW"), () => RenderWorld(""), new Color(.32f, .18f, .42f));
    }

    // Intercambia el progreso de mundo/estudio entre idiomas. Se conservan nivel, vida,
    // XP, objetos, trofeos, idioma de UI y mando. Siempre se despierta en la ciudad.
    void SwapLangProgress(string newLang)
    {
        var cur = new Progress
        {
            area = save.area, x = save.x, y = save.y,
            readTomes = save.readTomes, goneThings = save.goneThings, cleared = save.cleared,
            wrongQ = save.wrongQ, seenQ = save.seenQ, correctQ = save.correctQ,
            noEnemyZones = save.noEnemyZones, medallions = save.medallions, qstats = save.qstats
        };
        var nxt = (save.stash != null && save.stashLang == newLang) ? save.stash : null;
        save.readTomes = nxt?.readTomes ?? new List<string>();
        save.goneThings = nxt?.goneThings ?? new List<string>();
        save.cleared = nxt?.cleared ?? new List<string>();
        save.wrongQ = nxt?.wrongQ ?? new List<string>();
        save.seenQ = nxt?.seenQ ?? new List<string>();
        save.correctQ = nxt?.correctQ ?? new List<string>();
        save.noEnemyZones = nxt?.noEnemyZones ?? new List<string>();
        save.medallions = nxt?.medallions ?? new List<string>();
        save.qstats = nxt?.qstats ?? new List<QStat>();
        save.stash = cur;
        save.stashLang = save.qlang;
        save.qlang = newLang;
        save.area = "town"; save.x = 10; save.y = 10;
        chasers.Clear(); pendingLoot = null; stepsSinceBattle = 0; combo = 0; towerDmgBonus = 0;
        SaveSlot();
    }

    // Muro con antorcha encendida: ambiente de fortaleza fría (d1).
    void DrawTorchDeco(Color32[] b)
    {
        BRect(b, 0, 0, BS - 1, BS - 1, BC(24, 32, 56));
        BRect(b, 0, 0, BS - 1, 3, BC(38, 50, 84));
        BRect(b, 0, BS - 4, BS - 1, BS - 1, BC(16, 22, 40));
        BRect(b, 22, 22, 25, 41, BC(96, 66, 40));            // mango
        BRect(b, 20, 20, 27, 23, BC(70, 78, 96));            // soporte de hierro
        BDisc(b, 24, 15, 6, BC(255, 150, 30));               // llama
        BDisc(b, 24, 13, 3, BC(255, 235, 140));              // núcleo
        BPx(b, 17, 8, BC(255, 200, 80)); BPx(b, 31, 7, BC(255, 200, 80)); BPx(b, 24, 4, BC(255, 220, 110));
    }

    // Roca con cristales de datos: ambiente de cripta musgosa (d2).
    void DrawCrystalDeco(Color32[] b)
    {
        BRect(b, 0, 0, BS - 1, BS - 1, BC(20, 40, 32));
        BRect(b, 0, 0, BS - 1, 3, BC(30, 58, 44));
        BRect(b, 0, BS - 4, BS - 1, BS - 1, BC(12, 26, 20));
        BHorn(b, 16, 42, 0, 20, BC(70, 220, 150));           // cristal grande
        BHorn(b, 28, 44, 0, 13, BC(110, 240, 180));          // cristal mediano
        BHorn(b, 37, 42, 0, 9, BC(50, 180, 120));            // cristal pequeño
        BPx(b, 14, 26, BC(235, 255, 245)); BPx(b, 27, 34, BC(235, 255, 245));
    }

    // Muro con engranaje girando: ambiente de fábrica violeta (d3).
    void DrawGearDeco(Color32[] b)
    {
        var wallc = BC(38, 28, 54);
        BRect(b, 0, 0, BS - 1, BS - 1, wallc);
        BRect(b, 0, 0, BS - 1, 3, BC(56, 42, 78));
        BRect(b, 0, BS - 4, BS - 1, BS - 1, BC(26, 18, 38));
        var gear = BC(150, 120, 190); var lite = BC(196, 168, 230);
        BDisc(b, 24, 24, 4, gear);                           // dientes cardinales
        BDisc(b, 24, 8, 4, gear); BDisc(b, 24, 40, 4, gear); BDisc(b, 8, 24, 4, gear); BDisc(b, 40, 24, 4, gear);
        BDisc(b, 13, 13, 3, gear); BDisc(b, 35, 13, 3, gear); BDisc(b, 13, 35, 3, gear); BDisc(b, 35, 35, 3, gear);
        BDisc(b, 24, 24, 12, gear);                          // cuerpo
        BDisc(b, 20, 20, 4, lite);                           // brillo
        BDisc(b, 24, 24, 5, wallc);                          // agujero central
    }

    // Escudo azul con cerradura: identidad y Confianza Cero (dominio 1).
    void DrawShieldIcon(Color32[] b)
    {
        var edge = BC(16, 44, 96); var mid = BC(52, 124, 214); var lite = BC(126, 192, 255); var dark = BC(8, 22, 56);
        for (int y = 5; y <= 42; y++)
        {
            int half = y <= 24 ? 17 : 17 - (y - 24);
            if (half < 1) break;
            for (int x = 24 - half; x <= 24 + half; x++)
                BPx(b, x, y, (x == 24 - half || x == 24 + half || y == 5 || half == 1) ? edge : (x < 24 ? lite : mid));
        }
        BDisc(b, 24, 19, 4, dark);                       // cerradura
        BRect(b, 23, 21, 25, 29, dark);
        BPx(b, 18, 10, BC(255, 255, 255)); BPx(b, 19, 11, BC(255, 255, 255));   // brillo
    }

    // Gema verde facetada: datos y Purview (dominio 2).
    void DrawGemIcon(Color32[] b)
    {
        var edge = BC(10, 70, 44); var mid = BC(38, 168, 108); var lite = BC(120, 236, 178); var dark = BC(20, 110, 72);
        for (int y = 8; y <= 40; y++)
        {
            int half = y <= 22 ? (y - 8) + 3 : 17 - (y - 22);
            if (half < 1) break;
            for (int x = 24 - half; x <= 24 + half; x++)
                BPx(b, x, y, (x == 24 - half || x == 24 + half) ? edge : (x < 24 ? (y < 22 ? lite : mid) : (y < 22 ? mid : dark)));
        }
        BLine(b, 24, 8, 24, 40, 0, edge);                // arista central
        BLine(b, 7, 22, 41, 22, 0, edge);                // cintura de la gema
        BPx(b, 16, 14, BC(255, 255, 255)); BPx(b, 17, 15, BC(255, 255, 255));   // destello
    }

    // Chispa/estrella púrpura con núcleo: Copilot y agentes (dominio 3).
    void DrawSparkIcon(Color32[] b)
    {
        var mid = BC(150, 92, 230); var lite = BC(208, 168, 255); var core = BC(255, 244, 160);
        BHorn(b, 24, 20, 0, 16, mid);                    // punta superior
        for (int k = 0; k < 16; k++)                     // punta inferior (espejo)
        {
            int w = (16 - k + 1) / 2;
            for (int t = -w; t <= w; t++) BPx(b, 24 + t, 28 + k, mid);
        }
        for (int k = 0; k < 16; k++)                     // puntas laterales
        {
            int w = (16 - k + 1) / 2;
            for (int t = -w; t <= w; t++) { BPx(b, 20 - k, 24 + t, mid); BPx(b, 28 + k, 24 + t, mid); }
        }
        BDisc(b, 24, 24, 7, lite);
        BDisc(b, 24, 24, 4, core);
        BPx(b, 36, 10, lite); BPx(b, 11, 36, lite); BPx(b, 38, 36, lite);        // chispitas
    }

    Sprite MakeBoss(Action<Color32[]> draw)
    {
        var buf = new Color32[BS * BS];
        for (int i = 0; i < buf.Length; i++) buf[i] = new Color32(0, 0, 0, 0);
        draw(buf);
        var tex = new Texture2D(BS, BS, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels32(buf);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, BS, BS), new Vector2(.5f, .5f), 16f);
    }

    // Coordenadas con origen ARRIBA-IZQUIERDA (y=0 arriba); BPx convierte a la textura.
    void BPx(Color32[] b, int x, int y, Color32 c) { if (x < 0 || x >= BS || y < 0 || y >= BS) return; b[(BS - 1 - y) * BS + x] = c; }
    void BDisc(Color32[] b, int cx, int cy, int r, Color32 c) { for (int y = cy - r; y <= cy + r; y++) for (int x = cx - r; x <= cx + r; x++) { int dx = x - cx, dy = y - cy; if (dx * dx + dy * dy <= r * r) BPx(b, x, y, c); } }
    void BRect(Color32[] b, int x0, int y0, int x1, int y1, Color32 c) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) BPx(b, x, y, c); }
    void BLine(Color32[] b, int x0, int y0, int x1, int y1, int th, Color32 c)
    {
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx - dy;
        while (true) { BDisc(b, x0, y0, th, c); if (x0 == x1 && y0 == y1) break; int e2 = 2 * err; if (e2 > -dy) { err -= dy; x0 += sx; } if (e2 < dx) { err += dx; y0 += sy; } }
    }
    // Cuerno/pico que crece hacia ARRIBA (y decreciente), estrechándose.
    void BHorn(Color32[] b, int baseX, int baseY, int dir, int len, Color32 c)
    {
        for (int k = 0; k < len; k++) { int w = (len - k + 1) / 2; int x = baseX + dir * k; int y = baseY - k; for (int t = -w; t <= w; t++) BPx(b, x + t, y, c); }
    }

    void DrawDragon(Color32[] b)
    {
        Color32 scale = BC(58, 123, 213), dark = BC(27, 58, 102), horn = BC(159, 195, 255), eye = BC(255, 210, 61), pupil = BC(20, 20, 30), mouth = BC(122, 16, 32), white = BC(255, 250, 235);
        BDisc(b, 24, 27, 15, dark); BDisc(b, 24, 27, 14, scale);
        BRect(b, 17, 31, 31, 40, scale); BDisc(b, 24, 39, 7, scale);
        BPx(b, 20, 37, dark); BPx(b, 28, 37, dark);
        BHorn(b, 14, 16, -1, 12, horn); BHorn(b, 34, 16, 1, 12, horn);
        BRect(b, 13, 21, 35, 22, dark);
        BDisc(b, 18, 26, 3, eye); BDisc(b, 30, 26, 3, eye); BDisc(b, 18, 26, 1, pupil); BDisc(b, 30, 26, 1, pupil);
        BRect(b, 18, 41, 30, 43, mouth);
        BPx(b, 20, 41, white); BPx(b, 20, 42, white); BPx(b, 28, 41, white); BPx(b, 28, 42, white);
    }

    void DrawHydra(Color32[] b)
    {
        Color32 body = BC(57, 179, 107), dark = BC(23, 107, 58), eye = BC(255, 224, 138);
        BDisc(b, 24, 40, 9, dark); BDisc(b, 24, 40, 8, body);
        BLine(b, 24, 40, 12, 20, 2, body); BLine(b, 24, 40, 24, 14, 2, body); BLine(b, 24, 40, 36, 20, 2, body);
        int[][] heads = { new[] { 12, 18 }, new[] { 24, 12 }, new[] { 36, 18 } };
        foreach (var h in heads)
        {
            BDisc(b, h[0], h[1], 5, dark); BDisc(b, h[0], h[1], 4, body);
            BPx(b, h[0] - 1, h[1] - 1, eye); BPx(b, h[0] + 1, h[1] - 1, eye);
        }
    }

    void DrawCore(Color32[] b)
    {
        Color32 core = BC(160, 107, 255), glow = BC(223, 202, 255), ring = BC(107, 58, 194), dark = BC(40, 20, 70), node = BC(201, 166, 255);
        BDisc(b, 24, 24, 15, ring); BDisc(b, 24, 24, 12, dark); BDisc(b, 24, 24, 9, core); BDisc(b, 24, 24, 5, glow);
        int[][] nodes = { new[] { 24, 8 }, new[] { 24, 40 }, new[] { 8, 24 }, new[] { 40, 24 } };
        foreach (var n in nodes) { BLine(b, 24, 24, n[0], n[1], 0, node); BDisc(b, n[0], n[1], 3, node); BDisc(b, n[0], n[1], 1, glow); }
    }

    void DrawDemonKing(Color32[] b)
    {
        Color32 face = BC(150, 28, 36), dark = BC(42, 14, 18), crown = BC(255, 201, 46), eye = BC(255, 77, 46), eyeg = BC(255, 190, 120), fang = BC(255, 242, 208), gem = BC(255, 90, 90), brow = BC(90, 16, 20);
        BDisc(b, 24, 28, 15, dark); BDisc(b, 24, 28, 14, face);
        BHorn(b, 11, 19, -1, 14, dark); BHorn(b, 37, 19, 1, 14, dark);
        BRect(b, 12, 12, 36, 17, crown);
        BHorn(b, 15, 12, 0, 6, crown); BHorn(b, 24, 11, 0, 7, crown); BHorn(b, 33, 12, 0, 6, crown);
        BPx(b, 18, 15, gem); BPx(b, 24, 14, gem); BPx(b, 30, 15, gem);
        BRect(b, 14, 24, 21, 25, brow); BRect(b, 27, 24, 34, 25, brow);
        BDisc(b, 18, 29, 3, eye); BDisc(b, 30, 29, 3, eye); BDisc(b, 18, 29, 1, eyeg); BDisc(b, 30, 29, 1, eyeg);
        BRect(b, 16, 37, 32, 42, dark);
        for (int fx = 17; fx <= 31; fx += 3) { BPx(b, fx, 37, fang); BPx(b, fx, 38, fang); }
        BPx(b, 20, 42, fang); BPx(b, 28, 42, fang);
    }

    // Centinela de la Identidad (guardián d1): armadura azul acero, gran escudo y visor luminoso.
    void DrawSentinelGuard(Color32[] b)
    {
        var armor = BC(52, 124, 214); var dark = BC(16, 44, 96); var lite = BC(126, 192, 255); var visor = BC(140, 240, 255);
        BRect(b, 16, 20, 32, 40, armor);                              // torso
        BRect(b, 16, 20, 32, 22, lite);                               // hombreras
        BDisc(b, 24, 13, 7, dark); BDisc(b, 24, 13, 6, armor);        // yelmo
        BRect(b, 19, 12, 29, 14, visor);                              // visor luminoso
        BHorn(b, 24, 6, 0, 5, lite);                                  // cimera
        BRect(b, 6, 18, 14, 36, dark);                                // escudo frontal
        BRect(b, 7, 19, 13, 35, armor);
        BRect(b, 9, 22, 11, 32, lite);                                // franja vertical
        BRect(b, 8, 26, 12, 28, lite);                                // cruz
        BRect(b, 36, 14, 38, 42, BC(110, 78, 48));                    // lanza
        BHorn(b, 37, 13, 0, 6, lite);                                 // punta
        BRect(b, 18, 41, 22, 45, dark); BRect(b, 26, 41, 30, 45, dark); // piernas
    }

    // Custodio del Cumplimiento (guardián d2): túnica verde con capucha, gema y balanza.
    void DrawWardenGuard(Color32[] b)
    {
        var robe = BC(38, 168, 108); var dark = BC(10, 70, 44); var lite = BC(120, 236, 178);
        for (int y = 18; y <= 44; y++)                                // túnica acampanada
        {
            int half = 5 + (y - 18) / 3;
            for (int x = 24 - half; x <= 24 + half; x++) BPx(b, x, y, x == 24 - half || x == 24 + half ? dark : robe);
        }
        BDisc(b, 24, 14, 7, dark);                                    // capucha
        BDisc(b, 24, 15, 5, BC(18, 26, 30));                          // cara en sombra
        BPx(b, 22, 15, lite); BPx(b, 26, 15, lite);                   // ojos brillantes
        BDisc(b, 24, 28, 4, dark); BDisc(b, 24, 28, 3, lite);         // gema pectoral
        BPx(b, 24, 27, BC(255, 255, 255));                            // destello
        BRect(b, 8, 20, 9, 38, BC(110, 78, 48));                      // báculo
        BRect(b, 3, 20, 14, 21, dark);                                // travesaño de balanza
        BPx(b, 4, 22, dark); BPx(b, 4, 23, dark); BPx(b, 13, 22, dark); BPx(b, 13, 23, dark); // cadenas
        BDisc(b, 4, 26, 2, lite); BDisc(b, 13, 26, 2, lite);          // platillos
    }

    // Vigía de los Agentes (guardián d3): vigía mecánico púrpura de ojo único con chispas.
    void DrawVigilGuard(Color32[] b)
    {
        var body = BC(150, 92, 230); var dark = BC(70, 36, 130); var lite = BC(208, 168, 255); var core = BC(255, 244, 160);
        BRect(b, 14, 22, 34, 40, dark); BRect(b, 15, 23, 33, 39, body); // torso blindado
        BRect(b, 14, 30, 34, 31, dark);                                 // junta central
        BDisc(b, 24, 13, 9, dark); BDisc(b, 24, 13, 8, body);           // cabeza
        BDisc(b, 24, 13, 4, BC(30, 20, 50)); BDisc(b, 24, 13, 2, core); // ojo único
        BPx(b, 24, 12, BC(255, 255, 255));                              // brillo
        BRect(b, 10, 24, 13, 26, lite); BRect(b, 35, 24, 38, 26, lite); // hombros flotantes
        BHorn(b, 16, 6, -1, 5, lite); BHorn(b, 32, 6, 1, 5, lite);      // antenas
        BPx(b, 8, 16, core); BPx(b, 40, 18, core); BPx(b, 6, 30, core); // chispas
        BPx(b, 42, 34, core); BPx(b, 9, 40, core);
        BRect(b, 18, 41, 22, 45, dark); BRect(b, 26, 41, 30, 45, dark); // patas
    }

    Sprite LoadSprite(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (spriteCache.TryGetValue(name, out var cached)) return cached;
        var tex = Resources.Load<Texture2D>("Art/Tiles/" + name);
        Sprite sp = null;
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 16f);
        }
        spriteCache[name] = sp;
        return sp;
    }

    string SpriteForThing(Dictionary<string, object> t)
    {
        switch (S(t, "kind"))
        {
            case "portal":
                {
                    string pto = S(t, "to");
                    if (pto.Length == 2 && pto[0] == 'p' && char.IsDigit(pto[1]) && ContosoDone(pto[1] - '0') && LoadSprite("portal_check") != null) return "portal_check";
                    // Superado -> portal dorado de victoria (si existe).
                    if (save.cleared.Contains(pto) && LoadSprite("portal_done") != null) return "portal_done";
                    // Cada portal tiene SU color: portal_p1..p6 por mundo destino, portal_final para el examen.
                    string pc = pto == "final" ? "portal_final"
                              : (pto.Length >= 2 && pto[0] == 'p' && char.IsDigit(pto[1])) ? "portal_p" + (pto[1] - '0')
                              : null;
                    if (pc != null && LoadSprite(pc) != null) return pc;
                    return "portal";
                }
            case "exit": return "exit";
            case "carriage": return LoadSprite("carriage") != null ? "carriage" : "exit";
            case "inn": return "inn";
            case "chest": return "chest";
            case "npc": return SpriteForNpc(S(t, "who"));
            case "sage": return S(t, "dom") == "exam" ? "npc_sabio" : "sabio_" + S(t, "dom");   // cada Super Sabio tiene su sprite
            case "towerentrance": return "tower_stairs";   // entrada a la Torre física del Gran Sabio
            case "studytower": return LoadSprite("tower_studystairs") != null ? "tower_studystairs" : "tower_stairs";   // escalera a la Torre del Estudio
            case "studyup": return LoadSprite("tower_studystairs") != null ? "tower_studystairs" : "tower_stairs";
            case "studyexit": return "exit";
            // NPC guía ANIMADO: avanza 1 frame por cada paso del protagonista (usa walkFrame).
            case "studynpc": return LoadSprite("npc_studyguide_0") != null ? "npc_studyguide_" + (((walkFrame % 3) + 3) % 3) : "npc_sabio";
            case "studysage": return LoadSprite("npc_studysage") != null ? "npc_studysage" : "npc_sabio";
            case "studybible": return bibleSprite;   // tomo dorado (Súper Tomo)
            case "towergem": return "tower_gem";
            case "towerelevator": return "tower_stairs";
            case "towerexit": return "exit";
            case "hubsage": return "npc_sabio";   // el Sabio (mismo personaje que el Gran Sabio)
            case "reviewboss": return "boss_d" + (((I(t["part"]) - 1) % 3) + 1);   // jefe de repaso de la parte
            case "dashboard": return "npc_dashboard";
            case "examprep": return "npc_examinadora";     // examinador del simulador
            case "langmage": return "npc_langmage";       // generado por código
            case "tome": return (IsDungeon(save.area) ? (save.readTomeTiles != null && save.readTomeTiles.Contains(ThingKey(t))) : save.readTomes.Contains(S(t, "id"))) ? "tome_read" : "tome";
            case "supertome": return LoadSprite("supertome") != null ? "supertome" : "tome_read";
            case "boss": return SpriteForEnemyName(BossName(t));
            case "guardian": return t.ContainsKey("spr") ? S(t, "spr") : "guard_d" + (t.ContainsKey("part") ? (((I(t["part"]) - 1) % 3) + 1) : I(t["dom"]));
            case "rematch": return SpriteForEnemyName(BossName(t));
            case "denizen": return SpriteForEnemyName(S(t, "name"));
            case "hubguide": return GuardSpr(I(t["part"]));
            case "contoso": return GuardSpr(I(t["part"]));
            case "farmkeeper": return "npc_recepcion";
            case "finalcase": return SpriteForEnemyName(S(t, "name"));
            case "enemy": return SpriteForEnemyName(EnemyName(t));
            default: return "";
        }
    }

    string BossName(Dictionary<string, object> t)
    {
        var zone = L(data["zones"])[I(t["zi"])] as Dictionary<string, object>;
        return S(D(zone["boss"]), "n");
    }

    string EnemyName(Dictionary<string, object> t)
    {
        var zone = L(data["zones"])[I(t["zi"])] as Dictionary<string, object>;
        return S(L(zone["enemies"])[I(t["ei"])] as Dictionary<string, object>, "n");
    }

    string SpriteForNpc(string who)
    {
        switch (who)
        {
            case "recepcion": return "npc_recepcion";
            case "veterano": return "npc_veterano";
            case "purview": return "npc_purview";
            case "examinadora": return "npc_examinadora";
            case "sabio": return "npc_sabio";
            default: return "npc_recepcion";
        }
    }

    string SpriteForEnemyName(string n)
    {
        if (n != null && enemySprites.TryGetValue(n, out var mapped) && !string.IsNullOrEmpty(mapped)) return mapped;
        switch (n)
        {
            // D1 · Responsible AI, privacy & content exclusions
            case "Hallucination Slime": return "mon_slime";
            case "Bias Golem": return "mon_ogre";
            case "Privacy Leak Wraith": return "mon_ghost";
            // D2 · Copilot features
            case "Toxic Prompt Crab": return "mon_crab";
            case "License Bot": return "mon_demon";
            case "Rogue Agent": return "mon_spider";
            // D3 · Prompt engineering, data flow & productivity
            case "Zero-Shot Eye": return "mon_eye";
            case "Context Glitch Goblin": return "mon_goblin";
            case "Flaky Test Spectre": return "mon_spectre";
            // Bosses
            case "The Bias Warden": return "boss_d1";
            case "The Proxy Core": return "boss_d2";
            case "The Context Window": return "boss_d3";
            case "THE GH-300 EXAM": return "boss_final";
            // Final-tower guardians
            case "Sentinel of Trust": return "guard_d1";
            case "Warden of Features": return "guard_d2";
            case "Vigil of Prompts": return "guard_d3";
            default: return "mon_slime";
        }
    }

    string FloorSpriteFor(char c)
    {
        if (c == '#') return "wall";
        if (c == '+') return "floor_town";   // la antigua floor_path era una "alfombrilla" rara a tamaño celda
        if (c == ',') return "floor_town";
        if (c == 'T') return "tree";     // árbol decorativo (bloquea el paso)
        if (c == 'A') return "deco_d1";  // antorcha de fortaleza (generada por código)
        if (c == 'C') return "deco_d2";  // cristales de datos
        if (c == 'G') return "deco_d3";  // engranaje de la fábrica
        if (c == '~') return null;       // agua: sin sprite, usa color
        return "floor";                  // '.' y por defecto
    }

    RectTransform Panel(RectTransform parent, string name, Color color, Anchor anchor, Vector2 minOff, Vector2 maxOff)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        ApplyAnchor(rt, anchor, minOff, maxOff);
        go.GetComponent<Image>().color = color;
        return rt;
    }

    Image Token(RectTransform parent, string spriteName, Color accent, string glyph, string caption = null, Color? tint = null)
    {
        var sprite = LoadSprite(spriteName);
        var frame = Panel(parent, "Token", Color.clear, Anchor.Stretch, new Vector2(3, 3), new Vector2(-3, -3));
        var le = frame.gameObject.AddComponent<LayoutElement>();
        le.minWidth = 88; le.minHeight = 88; le.preferredWidth = 88; le.preferredHeight = 88;
        le.flexibleWidth = 0; le.flexibleHeight = 0;
        var img = frame.GetComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = tint ?? Color.white;
        }
        else
        {
            img.color = new Color(accent.r * .34f + .03f, accent.g * .34f + .03f, accent.b * .34f + .03f, 1f);
            var outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = accent; outline.effectDistance = new Vector2(2.5f, -2.5f);
            var g = Label(frame, glyph, 34, Color.white, FontStyle.Bold, Anchor.Stretch, new Vector2(2, 2), new Vector2(-2, -2));
            g.alignment = TextAnchor.MiddleCenter;
        }
        if (!string.IsNullOrEmpty(caption))
        {
            var strip = Panel(frame, "Cap", new Color(0f, 0f, 0f, .62f), Anchor.BottomStretch, new Vector2(0, 0), new Vector2(0, 20));
            var l = Label(strip, caption, 13, Color.white, FontStyle.Bold, Anchor.Stretch, Vector2.zero, Vector2.zero);
            l.alignment = TextAnchor.MiddleCenter;
        }
        return img;
    }

    void BigSprite(RectTransform parent, string name, int size, Color fallback)
    {
        var go = Panel(parent, "Big", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
        var le = go.gameObject.AddComponent<LayoutElement>();
        le.minHeight = size; le.preferredHeight = size; le.flexibleWidth = 1;
        var img = go.GetComponent<Image>();
        var sp = LoadSprite(name);
        if (sp != null) { img.sprite = sp; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white; }
        else img.color = fallback;
    }

    void FramedResourceImage(RectTransform parent, string resource, int size, Color border)
    {
        var card = Panel(parent, "ResourceFrame", new Color(.08f, .1f, .18f, 1f), Anchor.None, Vector2.zero, Vector2.zero);
        var le = card.gameObject.AddComponent<LayoutElement>();
        le.minHeight = size; le.preferredHeight = size; le.flexibleWidth = 1;
        var ol = card.gameObject.AddComponent<Outline>(); ol.effectColor = border; ol.effectDistance = new Vector2(3, -3);
        var inner = Panel(card, "ResourceImg", Color.clear, Anchor.Stretch, new Vector2(8, 8), new Vector2(-8, -8));
        var img = inner.GetComponent<Image>();
        var tex = Resources.Load<Texture2D>(resource);
        if (tex != null)
        {
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
            img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white;
        }
        else img.color = border;
    }

    // Etiqueta compacta con fondo y borde: encabezados/estados del combate (dominio, racha, fase).
    void Chip(RectTransform parent, string text, Color bg, Color fg, int size = 15, int h = 40)
    {
        var box = Panel(parent, "Chip", bg, Anchor.None, Vector2.zero, Vector2.zero);
        var le = box.gameObject.AddComponent<LayoutElement>(); le.minHeight = h; le.preferredHeight = h; le.flexibleWidth = 1;
        var ol = box.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(fg.r, fg.g, fg.b, .45f); ol.effectDistance = new Vector2(1.5f, -1.5f);
        var l = Label(box, text, size, fg, FontStyle.Bold, Anchor.Stretch, new Vector2(12, 2), new Vector2(-12, -2));
        l.alignment = TextAnchor.MiddleCenter;
    }

    // Botón de ATACAR/CONFIRMAR grande con resplandor del acento del dominio activo.
    GameObject AttackButton(RectTransform col, string label, Action act)
    {
        var go = OptButton(col, label, act, new Color(.84f, .25f, .16f), 0, 90);
        var a = AccentC;
        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(a.r, a.g, a.b, .85f); ol.effectDistance = new Vector2(2.5f, -2.5f);
        return go;
    }

    // Etiqueta temática del dominio en combate.
    string DomainBattleTag(int dom)
    {
        if (dom <= 0) return T("FINAL · El Examen AB-410", "FINAL · The AB-410 Exam");
        switch (PartOfDom(dom))
        {
            case 1: return T("PARTE 1 · Power Platform con IA", "PART 1 · Power Platform with AI");
            case 2: return T("PARTE 2 · Microsoft Dataverse", "PART 2 · Microsoft Dataverse");
            case 3: return T("PARTE 3 · Power Apps y Power Pages", "PART 3 · Power Apps & Power Pages");
            default: return T("PARTE 4 · Power Automate y AI Builder", "PART 4 · Power Automate & AI Builder");
        }
    }

    void HpBar(RectTransform parent, string caption, int cur, int max, Color fillColor)
    {
        var box = Panel(parent, "HpBox", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
        var le = box.gameObject.AddComponent<LayoutElement>(); le.minHeight = 40; le.preferredHeight = 40; le.flexibleWidth = 1;
        var bg = Panel(box, "HpBg", new Color(0f, 0f, 0f, .62f), Anchor.Stretch, new Vector2(0, 4), new Vector2(0, -4));
        var bol = bg.gameObject.AddComponent<Outline>();
        bol.effectColor = new Color(fillColor.r, fillColor.g, fillColor.b, .55f); bol.effectDistance = new Vector2(1.5f, -1.5f);
        float frac = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
        var track = Panel(bg, "HpTrack", new Color(fillColor.r * .22f, fillColor.g * .22f, fillColor.b * .22f, .5f), Anchor.Stretch, new Vector2(3, 3), new Vector2(-3, -3));
        var fill = Panel(track, "HpFill", fillColor, Anchor.None, Vector2.zero, Vector2.zero);
        fill.anchorMin = new Vector2(0, 0); fill.anchorMax = new Vector2(frac, 1);
        fill.offsetMin = Vector2.zero; fill.offsetMax = Vector2.zero;
        var l = Label(bg, caption + "   " + cur + " / " + max, 16, Color.white, FontStyle.Bold, Anchor.Stretch, Vector2.zero, Vector2.zero);
        l.alignment = TextAnchor.MiddleCenter;
    }

    RectTransform ScrollColumn(RectTransform parent, int spacing, int pad)
    {
        var viewport = Panel(parent, "Viewport", Color.clear, Anchor.Stretch, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 42;
        var content = Panel(viewport, "Content", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
        content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
        scroll.content = content; scroll.viewport = viewport;
        var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(pad, pad, pad, pad); v.spacing = spacing; v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        var fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return content;
    }

    void FramedSprite(RectTransform parent, string name, int size, Color border)
    {
        var card = Panel(parent, "Frame", new Color(.10f, .12f, .2f, 1f), Anchor.None, Vector2.zero, Vector2.zero);
        var le = card.gameObject.AddComponent<LayoutElement>();
        le.minHeight = size; le.preferredHeight = size; le.minWidth = size; le.preferredWidth = size;
        le.flexibleWidth = 0; le.flexibleHeight = 0;
        var ol = card.gameObject.AddComponent<Outline>(); ol.effectColor = border; ol.effectDistance = new Vector2(3, -3);
        var inner = Panel(card, "Img", Color.clear, Anchor.Stretch, new Vector2(7, 7), new Vector2(-7, -7));
        var img = inner.GetComponent<Image>();
        var sp = LoadSprite(name);
        if (sp != null) { img.sprite = sp; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white; }
        else img.color = border;
    }

    void SpriteRow(RectTransform parent, string[] names, int size, Color border)
    {
        var row = Panel(parent, "SpriteRow", Color.clear, Anchor.None, Vector2.zero, Vector2.zero);
        var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = size + 8; le.preferredHeight = size + 8; le.flexibleWidth = 1;
        Horizontal(row, 14, TextAnchor.MiddleCenter);
        foreach (var n in names) FramedSprite(row, n, size, border);
    }

    string Glyph(string kind)
    {
        switch (kind)
        {
            case "player": return "@";
            case "portal": return "O";
            case "exit": return "<";
            case "carriage": return "▣";
            case "npc": return "?";
            case "inn": return "+";
            case "tome": return "i";
            case "tomeRead": return "OK";
            case "supertome": return "S";
            case "sage": return "✦";
            case "towerentrance": return "♜";
            case "towergem": return "♦";
            case "towerelevator": return "▲";
            case "towerexit": return "<";
            case "reviewboss": return "‼";
            case "hubsage": return "✦";
            case "dashboard": return "▦";
            case "langmage": return "Ñ";
            case "enemy": return "!";
            case "boss": return "X";
            case "guardian": return "‼";
            case "rematch": return "★";
            case "denizen": return "☺";
            case "hubguide": return "?";
            case "contoso": return "★";
            case "farmkeeper": return "?";
            case "finalcase": return "★";
            case "chest": return "$";
            default: return "*";
        }
    }

    void CenterScrollOnPlayer(ScrollRect scroll, GridLayoutGroup grid, int cols, int rows)
    {
        float cw = grid.cellSize.x + grid.spacing.x;
        float ch = grid.cellSize.y + grid.spacing.y;
        float contentW = grid.padding.left + grid.padding.right + cols * grid.cellSize.x + (cols - 1) * grid.spacing.x;
        float contentH = grid.padding.top + grid.padding.bottom + rows * grid.cellSize.y + (rows - 1) * grid.spacing.y;
        float viewW = scroll.viewport.rect.width;
        float viewH = scroll.viewport.rect.height;
        float pcx = grid.padding.left + save.x * cw + grid.cellSize.x / 2f;
        float pcyTop = grid.padding.top + save.y * ch + grid.cellSize.y / 2f;
        if (contentW > viewW)
            scroll.horizontalNormalizedPosition = Mathf.Clamp01((pcx - viewW / 2f) / (contentW - viewW));
        if (contentH > viewH)
            scroll.verticalNormalizedPosition = Mathf.Clamp01(1f - (pcyTop - viewH / 2f) / (contentH - viewH));
    }

    Text Label(RectTransform parent, string text, int size, Color color, FontStyle style, Anchor anchor = Anchor.None, Vector2 min = default, Vector2 max = default)
    {
        var go = new GameObject("Text", typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (anchor != Anchor.None) ApplyAnchor(rt, anchor, min, max);
        var t = go.GetComponent<Text>();
        t.text = text; t.font = font; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.supportRichText = true;
        t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        var le = go.AddComponent<LayoutElement>(); le.minHeight = Math.Max(48, size * 2); le.flexibleWidth = 1;
        return t;
    }

    string HighlightTerms(string text)
    {
        foreach (var term in TermOrder())
        {
            var color = ColorUtility.ToHtmlStringRGB(TermColor(term));
            text = Regex.Replace(text, @"(?<![\w])" + Regex.Escape(term) + @"(?![\w])", "<b><color=#" + color + ">$0</color></b>", RegexOptions.IgnoreCase);
        }
        return text;
    }

    List<string> TermsInText(string text)
    {
        var found = new List<string>();
        foreach (var term in TermOrder())
        {
            if (Regex.IsMatch(text, @"(?<![\w])" + Regex.Escape(term) + @"(?![\w])", RegexOptions.IgnoreCase))
                found.Add(term);
            if (found.Count >= 8) break;
        }
        return found;
    }

    string[] TermOrder()
    {
        return new[]
        {
            "MFA", "BYOD", "BYOR", "PIM", "DLP", "EDR", "XDR", "SIEM", "SOAR", "JIT", "JEA", "RBAC", "SSO", "SaaS",
            "EDM", "DSPM", "eDiscovery", "Copilot", "Graph", "Purview", "Intune", "Entra", "Defender",
            "Sentinel", "SharePoint", "OneDrive", "Teams", "Zero Trust", "Confianza cero", "Conditional Access",
            "Acceso condicional", "Sensitivity labels", "Etiquetas de confidencialidad", "Insider Risk",
            "Communication Compliance", "Data Lifecycle Management", "Restricted SharePoint Search", "RSS",
            "DAG", "SAM", "Authenticator", "Passwordless", "FIDO2", "Windows Hello"
        };
    }

    Color TermColor(string term)
    {
        string t = term.ToLowerInvariant();
        if (t.Contains("mfa") || t.Contains("auth") || t.Contains("fido") || t.Contains("hello") || t.Contains("password")) return new Color(.35f, .75f, 1f);
        if (t.Contains("purview") || t.Contains("dlp") || t.Contains("edm") || t.Contains("label") || t.Contains("confidencial")) return new Color(.55f, 1f, .68f);
        if (t.Contains("copilot") || t.Contains("graph") || t.Contains("agent")) return new Color(.82f, .62f, 1f);
        if (t.Contains("defender") || t.Contains("xdr") || t.Contains("edr") || t.Contains("sentinel") || t.Contains("siem") || t.Contains("soar")) return new Color(1f, .55f, .48f);
        if (t.Contains("pim") || t.Contains("jit") || t.Contains("rbac") || t.Contains("entra") || t.Contains("access")) return new Color(1f, .82f, .22f);
        return new Color(.55f, .9f, 1f);
    }

    string TermDefinition(string term)
    {
        switch (term.ToLowerInvariant())
        {
            case "mfa": return "Autenticacion multifactor: exige dos o mas pruebas de identidad, por ejemplo contraseña y Microsoft Authenticator.";
            case "byod": return "Bring Your Own Device: uso de dispositivos personales para acceder a recursos corporativos. Se controla con cumplimiento, Intune y acceso condicional.";
            case "byor": return "Bring Your Own Resource. En este temario suele confundirse con BYOD; si ves movilidad/dispositivo personal, piensa en BYOD.";
            case "pim": return "Privileged Identity Management: acceso privilegiado temporal, just-in-time. Un rol elegible debe activarse antes de usarlo.";
            case "dlp": return "Data Loss Prevention: directivas que detectan y bloquean o auditan movimientos de datos sensibles.";
            case "edr": return "Endpoint Detection and Response: deteccion, investigacion y respuesta en dispositivos.";
            case "xdr": return "Extended Detection and Response: correlaciona señales entre correo, identidad, endpoints, apps y datos.";
            case "siem": return "Security Information and Event Management: recopila y CENTRALIZA logs, eventos y alertas de toda la organizacion, los correlaciona y detecta amenazas para investigar. Microsoft Sentinel es el SIEM de Microsoft.";
            case "soar": return "Security Orchestration, Automation and Response: AUTOMATIZA la respuesta a las alertas con playbooks (aislar un equipo, bloquear una cuenta, notificar). Microsoft Sentinel suma SOAR al SIEM; el SIEM detecta y el SOAR actua.";
            case "jit": return "Just-In-Time: concede privilegios solo durante el tiempo necesario.";
            case "jea": return "Just-Enough-Access: concede solo los permisos exactos necesarios para una tarea.";
            case "rbac": return "Role-Based Access Control: permisos asignados por roles.";
            case "sso": return "Single Sign-On: iniciar sesion una vez para acceder a varias aplicaciones.";
            case "saas": return "Software as a Service: aplicacion en la nube administrada por el proveedor.";
            case "edm": return "Exact Data Match: compara datos detectados contra registros propios para reducir falsos positivos.";
            case "dspm": return "Data Security Posture Management: visibilidad y recomendaciones sobre postura de seguridad de datos, incluido uso de IA.";
            case "ediscovery": return "Herramienta legal/compliance para buscar, conservar y exportar copias de contenido.";
            case "copilot": return "Asistente de IA de Microsoft 365 que usa Microsoft Graph y respeta permisos del usuario.";
            case "graph": return "Microsoft Graph: API e indice que conecta datos, actividad, relaciones y permisos de Microsoft 365.";
            case "purview": return "Familia de soluciones para gobernanza, cumplimiento, proteccion de datos, DLP, etiquetas, eDiscovery y riesgos internos.";
            case "intune": return "Administracion de endpoints y apps: cumplimiento, configuracion, proteccion de aplicaciones y dispositivos.";
            case "entra": return "Plataforma de identidad y acceso de Microsoft: usuarios, grupos, MFA, acceso condicional, PIM e identidades de apps.";
            case "defender": return "Familia de seguridad para amenazas: Office 365, Endpoint, Identity, Cloud Apps y XDR.";
            case "sentinel": return "SIEM/SOAR nativo de nube para recopilar señales, correlacionar incidentes y automatizar respuesta.";
            case "sharepoint": return "Servicio de sitios y archivos compartidos; Teams usa SharePoint para sus archivos.";
            case "onedrive": return "Almacenamiento personal de archivos de usuario dentro de Microsoft 365.";
            case "teams": return "Colaboracion con chat, reuniones, canales y archivos respaldados por SharePoint.";
            case "zero trust": return "Modelo de seguridad: verificar explicitamente, aplicar minimo privilegio y asumir vulneracion.";
            case "confianza cero": return "Nombre en espanol de Zero Trust: nunca confiar por defecto, verificar cada solicitud.";
            case "conditional access":
            case "acceso condicional": return "Motor de reglas si-entonces de Entra que decide acceso segun usuario, riesgo, dispositivo, ubicacion y app.";
            case "sensitivity labels":
            case "etiquetas de confidencialidad": return "Etiquetas de Purview que clasifican y protegen contenido con cifrado, marcas o restricciones.";
            case "insider risk": return "Purview Insider Risk Management: detecta comportamiento riesgoso, como exfiltracion o actividad de empleados que se van.";
            case "communication compliance": return "Supervisa comunicaciones en Teams, correo y Copilot para detectar lenguaje ofensivo, riesgos o fugas.";
            case "data lifecycle management": return "Retiene y elimina contenido por politicas o etiquetas de retencion.";
            case "restricted sharepoint search":
            case "rss": return "Limita que contenido de SharePoint puede usar Copilot como fuente mientras revisas permisos.";
            case "dag": return "Data Access Governance: informes y controles para revisar permisos y uso compartido en SharePoint.";
            case "sam": return "SharePoint Advanced Management: controles avanzados como RSS, acceso restringido a sitios y ciclo de vida.";
            case "authenticator": return "App Microsoft Authenticator para MFA, number matching y metodos sin contraseña.";
            case "passwordless": return "Autenticacion sin contraseña, por ejemplo Windows Hello o llaves FIDO2.";
            case "fido2": return "Estandar de llaves de seguridad resistentes al phishing para autenticacion fuerte.";
            case "windows hello": return "Metodo passwordless de Windows con PIN, biometria o claves protegidas por dispositivo.";
            default: return "Termino tecnico del temario AB-900. Revisa el contexto del Super Tomo para asociarlo con el portal, producto o verbo correcto.";
        }
    }

    void Button(RectTransform parent, string text, Action action, Color color, int width = 0)
    {
        var go = new GameObject("Button", typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(() => action());
        var le = go.AddComponent<LayoutElement>(); le.minHeight = 74; le.flexibleWidth = width == 0 ? 1 : 0; if (width > 0) le.minWidth = width;
        int fs = text.Length > 28 ? 13 : (text.Length > 18 ? 16 : 20);
        Label(go.GetComponent<RectTransform>(), text, fs, Color.white, FontStyle.Bold, Anchor.Stretch, new Vector2(8, 6), new Vector2(-8, -6));
    }

    // Botón con un SPRITE centrado en vez de texto (para iconos que el emoji no muestra en el APK).
    void IconButton(RectTransform parent, string sprite, Action action, Color color, int width = 0)
    {
        var go = new GameObject("IconButton", typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(() => action());
        var le = go.AddComponent<LayoutElement>(); le.minHeight = 74; le.flexibleWidth = width == 0 ? 1 : 0; if (width > 0) le.minWidth = width;
        var icon = Panel(go.GetComponent<RectTransform>(), "Icon", Color.clear, Anchor.Stretch, new Vector2(12, 10), new Vector2(-12, -10));
        var img = icon.GetComponent<Image>();
        var sp = LoadSprite(sprite);
        if (sp != null) { img.sprite = sp; img.preserveAspect = true; img.color = Color.white; }
        else img.color = new Color(1f, .82f, .22f);
    }

    void Vertical(RectTransform rt, int spacing, TextAnchor align)
    {
        var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 18, 18); v.spacing = spacing; v.childAlignment = align;
        v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
    }

    void Horizontal(RectTransform rt, int spacing, TextAnchor align)
    {
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(12, 12, 8, 8); h.spacing = spacing; h.childAlignment = align;
        h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = true;
    }

    void ApplyAnchor(RectTransform rt, Anchor a, Vector2 min, Vector2 max)
    {
        if (a == Anchor.Stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = min; rt.offsetMax = max; }
        else if (a == Anchor.TopStretch) { rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one; rt.offsetMin = min; rt.offsetMax = max; }
        else if (a == Anchor.BottomStretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1, 0); rt.offsetMin = min; rt.offsetMax = max; }
        else if (a == Anchor.Middle) { rt.anchorMin = rt.anchorMax = new Vector2(.5f, .55f); rt.anchoredPosition = min; }
    }

    // ---- Audio (chiptune por código): movido al archivo parcial AB900Game.Music.cs ----

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static Dictionary<string, object> D(object o) => o as Dictionary<string, object>;
    static List<object> L(object o) => o as List<object>;
    static string S(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) ? S(v) : "";
    static string S(object o) => Convert.ToString(o, CultureInfo.InvariantCulture);
    static int I(object o) => Convert.ToInt32(o, CultureInfo.InvariantCulture);
    static bool B(object o) => o is bool b && b;

    // ---- Idioma ----
    // GitHub Copilot RPG es English-only: EN y QEN son siempre true, así toda la UI
    // (T()) sale en inglés y QS/QL/TS/STS leen el campo base (los datos van en inglés,
    // sin campos *_en). El andamiaje bilingüe queda inerte por si se reactivara.
    bool EN => true;
    bool QEN => save != null && save.qlang == "en";   // el Mago del Idioma alterna qlang; las preguntas con _en salen en inglés
    string T(string es, string en) => EN ? en : es;
    string QS(Dictionary<string, object> d, string k) => QEN && d.ContainsKey(k + "_en") ? S(d, k + "_en") : S(d, k);
    List<object> QL(Dictionary<string, object> d, string k) => QEN && d.ContainsKey(k + "_en") ? L(d[k + "_en"]) : L(d[k]);
    string TS(Dictionary<string, object> d, string k) => EN && d.ContainsKey(k + "_en") ? S(d, k + "_en") : S(d, k);
    string STS(Dictionary<string, object> d, string k) => QS(d, k);   // Súper Tomos: siguen el idioma de las PREGUNTAS (qlang)
    static List<string> ToStringList(List<object> src) { var l = new List<string>(); foreach (var x in src) l.Add(S(x)); return l; }

    enum Anchor { None, Stretch, TopStretch, BottomStretch, Middle }

    [Serializable] public class SaveData
    {
        public int slot, x, y, fx, fy, lv, hp, maxhp, mp, maxmp, xp, xpNext;
        public string area;
        public string lang;              // idioma de la partida: "es" | "en"
        public string qlang;             // idioma de las PREGUNTAS (el Mago del Idioma lo alterna)
        public List<string> readTomes, goneThings, cleared, wrongQ;
        public List<string> seenQ;       // preguntas ya mostradas (cobertura)
        public List<QStat> qstats;       // fallos acumulados por pregunta (ponderación + dashboard)
        public List<string> medallions;  // retos de sabio superados: "d1","d2","d3"
        public List<string> correctQ;    // preguntas respondidas bien al menos una vez
        public List<string> noEnemyZones;// zonas con enemigos desactivados
        public List<string> metEnemies;  // enemigos/jefes vencidos -> aparecen en la Granja
        public List<string> readTomeTiles;// tomos de mazmorra ya leídos, por posición (ThingKey)
        public List<string> readSuper;   // ids de Super Tomos ya leídos (desbloquea Caso Contoso)
        public List<int> contosoDone;    // (heredado) partes con el Caso Contoso superado
        public List<string> contosoCleared;// niveles de Caso Contoso superados: "<parte>-<nivel>" (1..4)
        public List<ItemStack> items;    // inventario (compartido entre idiomas)
        public List<string> trophies;    // logros globales: "examen", "demonio_es", "demonio_en", "gran_sabio"
        public List<int> towerDone;      // Torre del Gran Sabio: módulos (dom 1..17) cuya GEMA ya superaste
        public int towerFloor;           // Torre física: piso actual mientras la recorres (1..moduleCount; 0 = sin empezar)
        public int studyFloor;           // Torre del Estudio: piso aleatorio actual (0 = vestíbulo; 1..99 mazmorra; 100 = Sabio)
        public int studyCheckpoint;      // Torre del Estudio: checkpoint persistente (0,10,20,...,90)
        public List<int> sagesDown;      // partes (1..4) cuyo Sabio del hub ya derrotaste (pasa a la granja)
        public Progress stash;           // progreso del OTRO idioma (Mago del Idioma)
        public string stashLang;         // idioma del stash; "" = sin stash
    }

    [Serializable] public class QStat { public string k; public int w; }

    [Serializable] public class ItemStack { public string id; public int n; }

    // Progreso de mundo/estudio que se intercambia al cambiar de idioma de preguntas.
    [Serializable] public class Progress
    {
        public string area;
        public int x, y;
        public List<string> readTomes, goneThings, cleared, wrongQ, seenQ, correctQ, noEnemyZones, medallions;
        public List<QStat> qstats;
    }

    class Battle
    {
        public string thingKey;
        public Dictionary<string, object> enemy;
        public int hp, maxhp, dom;
        public int part = 0;   // >0 = combate de PARTE (guardián): preguntas de TODOS los MD de esa parte
        public string spr;
        public bool locked;
        public int qn = 1;   // preguntas por ronda: 1 normal, 2 jefes de mazmorra, 3 guardianes/jefe final
        // --- Habilidad de combate activa (ahora también contra jefes) ---
        public bool skill;       // ronda transformada por una habilidad
        public int skillN = 1;   // preguntas BASE de la skill (2..6); el total = qn natural × skillN
        public int skillIdx;     // índice de la habilidad (para nombre/animación)
        public int dmgMult = 1;  // multiplicador de daño de la ronda (calculado en RenderBattle)
        public bool varQn;       // ronda variable de 4-5 preguntas (guardianes del Gran Sabio)
        public bool tower;       // combate de la Torre del Estudio: usa el banco towerbq y no toca estadísticas del bq
    }
}

public static class MiniJson
{
    public static object Deserialize(string json) => new Parser(json).ParseValue();
    sealed class Parser
    {
        readonly string json; int i;
        public Parser(string json) { this.json = json; }
        public object ParseValue()
        {
            Eat();
            if (json[i] == '{') return ParseObject();
            if (json[i] == '[') return ParseArray();
            if (json[i] == '"') return ParseString();
            if (char.IsDigit(json[i]) || json[i] == '-') return ParseNumber();
            if (Match("true")) return true;
            if (Match("false")) return false;
            if (Match("null")) return null;
            throw new Exception("JSON invalido en " + i);
        }
        Dictionary<string, object> ParseObject()
        {
            var d = new Dictionary<string, object>(); i++; Eat();
            while (json[i] != '}')
            {
                var k = ParseString(); Eat(); i++; d[k] = ParseValue(); Eat();
                if (json[i] == ',') { i++; Eat(); }
            }
            i++; return d;
        }
        List<object> ParseArray()
        {
            var l = new List<object>(); i++; Eat();
            while (json[i] != ']')
            {
                l.Add(ParseValue()); Eat();
                if (json[i] == ',') { i++; Eat(); }
            }
            i++; return l;
        }
        string ParseString()
        {
            var s = new System.Text.StringBuilder(); i++;
            while (json[i] != '"')
            {
                if (json[i] == '\\')
                {
                    i++; char c = json[i];
                    if (c == 'n') s.Append('\n'); else if (c == 'r') s.Append('\r'); else if (c == 't') s.Append('\t');
                    else if (c == 'u') { var hex = json.Substring(i + 1, 4); s.Append((char)Convert.ToInt32(hex, 16)); i += 4; }
                    else s.Append(c);
                }
                else s.Append(json[i]);
                i++;
            }
            i++; return s.ToString();
        }
        object ParseNumber()
        {
            int s = i;
            while (i < json.Length && "-+0123456789.eE".IndexOf(json[i]) >= 0) i++;
            var n = json.Substring(s, i - s);
            if (n.IndexOf('.') >= 0 || n.IndexOf('e') >= 0 || n.IndexOf('E') >= 0) return double.Parse(n, CultureInfo.InvariantCulture);
            return int.Parse(n, CultureInfo.InvariantCulture);
        }
        bool Match(string s)
        {
            if (string.Compare(json, i, s, 0, s.Length, StringComparison.Ordinal) != 0) return false;
            i += s.Length; return true;
        }
        void Eat() { while (i < json.Length && char.IsWhiteSpace(json[i])) i++; }
    }
}
