using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

// BrainPunch > Build Fight Scene — builds the entire fight scene with one click.
// Run once on an empty scene; re-running adds duplicates, so clear the scene first.
public static class FightSceneBuilder
{
    const float FIGHTER_SEPARATION_Z = 5f;
    const float CAM_Z               = -4f;
    const float CAM_Y               = 3.98f;
    const float CAM_PITCH           = 18.78f;

    [MenuItem("BrainPunch/Build Fight Scene", priority = 1)]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog("Build Fight Scene",
            "Adds all BrainPunch objects to the current scene.\nStart from an empty scene for best results.",
            "Build", "Cancel")) return;

        // ── Managers ─────────────────────────────────────────────────────────
        var managersGO    = new GameObject("Managers");
        var gameManager   = managersGO.AddComponent<GameManager>();
        var roundManager  = managersGO.AddComponent<RoundManager>();
        var combatResolver= managersGO.AddComponent<CombatResolver>();
        var pauseHandler  = managersGO.AddComponent<PauseHandler>();

        // ── Fighters ─────────────────────────────────────────────────────────
        var playerGO = BuildFighter("Player", Vector3.zero,            facingSign:  1f, isPlayer: true);
        var botGO    = BuildFighter("Bot",    new Vector3(0,0,FIGHTER_SEPARATION_Z), facingSign: -1f, isPlayer: false);

        var playerFC = playerGO.GetComponent<FighterController>();
        var botFC    = botGO.GetComponent<FighterController>();

        // Wire FighterController opponents + resolver
        SetField(playerFC, "_opponent",       botFC);
        SetField(playerFC, "_combatResolver", combatResolver);
        SetField(botFC,    "_opponent",       playerFC);
        SetField(botFC,    "_combatResolver", combatResolver);

        // Wire RoundManager
        SetField(roundManager, "_player", playerFC);
        SetField(roundManager, "_bot",    botFC);

        // Wire PauseHandler
        SetField(pauseHandler, "_roundManager", roundManager);

        // Wire BotFighterInput player reference (tag-based at runtime, but set here too)
        playerGO.tag = "Player";

        // ── Camera ───────────────────────────────────────────────────────────
        var cam = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
        cam.tag = "MainCamera";
        if (cam.GetComponent<Camera>() == null) cam.AddComponent<Camera>();
        if (cam.GetComponent<AudioListener>() == null) cam.AddComponent<AudioListener>();

        cam.transform.position = new Vector3(0, CAM_Y, CAM_Z);
        cam.transform.rotation = Quaternion.Euler(CAM_PITCH, 0f, 0f);

        // ── Aim Reticle ──────────────────────────────────────────────────────
        BuildReticle(playerGO.GetComponent<SwivelComponent>(), botGO.transform);

        // ── Canvas / HUD ─────────────────────────────────────────────────────
        var (canvas, hudCtrl, dbgOverlay) = BuildCanvas();

        SetField(hudCtrl, "_player",       playerFC);
        SetField(hudCtrl, "_bot",          botFC);
        SetField(hudCtrl, "_roundManager", roundManager);
        SetField(dbgOverlay, "_player",    playerFC);
        SetField(dbgOverlay, "_bot",       botFC);

        // Ensure EventSystem exists
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Selection.activeGameObject = playerGO;
        Debug.Log("[BrainPunch] Fight scene built. Press Play to test.\n" +
                  "Controls: LShift=Guard | Space=Punch | Mouse=Swivel | F=Focus | R=Recenter | Tab=Debug | Esc=Pause");
    }

    // ── Fighter builder ───────────────────────────────────────────────────────
    static GameObject BuildFighter(string fighterName, Vector3 pos, float facingSign, bool isPlayer)
    {
        var root = new GameObject(fighterName);
        root.transform.position = pos;

        // BodyPivot — this rotates during swivel
        var pivot = new GameObject("BodyPivot");
        pivot.transform.SetParent(root.transform, false);

        // Body (Capsule)
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "BodyVisual";
        body.transform.SetParent(pivot.transform, false);
        body.transform.localPosition = new Vector3(0, 1f, 0);
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
        SetMaterialColor(body, isPlayer ? new Color(0.2f, 0.4f, 0.9f) : new Color(0.9f, 0.3f, 0.2f));

        // Head (Sphere)
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(pivot.transform, false);
        head.transform.localPosition = new Vector3(0, 2.3f, 0);
        head.transform.localScale    = new Vector3(0.6f, 0.6f, 0.6f);
        Object.DestroyImmediate(head.GetComponent<SphereCollider>());
        SetMaterialColor(head, isPlayer ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.5f, 0.3f));

        // Gloves
        CreateGlove("GloveL", pivot.transform, new Vector3(-0.55f, 1.4f, 0.4f), isPlayer);
        CreateGlove("GloveR", pivot.transform, new Vector3( 0.55f, 1.4f, 0.4f), isPlayer);

        // Components
        var health = root.AddComponent<HealthComponent>();
        var punch  = root.AddComponent<PunchComponent>();
        var guard  = root.AddComponent<GuardComponent>();
        var swivel = root.AddComponent<SwivelComponent>();
        var fc     = root.AddComponent<FighterController>();
        root.AddComponent<GloveAnimator>();  // auto-finds GloveL/GloveR under BodyPivot
        root.AddComponent<HitReaction>();    // auto-finds BodyPivot + child Renderers

        SetField(swivel, "_bodyPivot",   pivot.transform);
        SetField(swivel, "_facingSign",  facingSign);

        // Rotate bot to face player
        if (!isPlayer) root.transform.rotation = Quaternion.Euler(0, 180f, 0);

        // Input
        if (isPlayer)
            root.AddComponent<KeyboardFighterInput>();
        else
        {
            root.AddComponent<BotFighterInput>();
            root.AddComponent<BotOrbitMotion>();  // orbit sway + face-player
        }

        return root;
    }

    // ── Reticle builder ───────────────────────────────────────────────────────
    static GameObject BuildReticle(SwivelComponent playerSwivel, Transform botTransform)
    {
        var go = new GameObject("AimReticle");
        var reticle = go.AddComponent<AimReticle>();
        SetField(reticle, "_swivelSource", playerSwivel);
        SetField(reticle, "_target",       botTransform);
        return go;
    }

    [MenuItem("BrainPunch/Add Aim Reticle to Existing Scene", priority = 5)]
    public static void AddReticleToExistingScene()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { EditorUtility.DisplayDialog("Reticle", "No GameObject tagged 'Player' found.", "OK"); return; }

        var fighters = Object.FindObjectsOfType<FighterController>();
        FighterController bot = null;
        foreach (var f in fighters) if (f.gameObject != player) { bot = f; break; }
        if (bot == null) { EditorUtility.DisplayDialog("Reticle", "No bot FighterController found.", "OK"); return; }

        if (Object.FindObjectOfType<AimReticle>() != null)
        {
            if (!EditorUtility.DisplayDialog("Reticle", "An AimReticle already exists. Add another?", "Add", "Cancel")) return;
        }

        BuildReticle(player.GetComponent<SwivelComponent>(), bot.transform);
        Debug.Log("[BrainPunch] Aim reticle added.");
    }

    static void CreateGlove(string name, Transform parent, Vector3 localPos, bool isPlayer)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale    = new Vector3(0.28f, 0.28f, 0.28f);
        Object.DestroyImmediate(g.GetComponent<BoxCollider>());
        SetMaterialColor(g, isPlayer ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.2f, 0.6f, 0.9f));
    }

    // ── Canvas builder ────────────────────────────────────────────────────────
    static (GameObject canvas, HUDController hud, DebugOverlay dbg) BuildCanvas()
    {
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Player HUD (top-left) ────────────────────────
        var playerHP     = MakeSlider(canvasGO.transform, "PlayerHPBar",    new Vector2( 180,-30), new Vector2(300,30), Color.green);
        var playerCharge = MakeSlider(canvasGO.transform, "PlayerChargeBar",new Vector2( 180,-70), new Vector2(300,18), Color.yellow);
        var playerFocus  = MakeSlider(canvasGO.transform, "PlayerFocusBar", new Vector2( 180,-95), new Vector2(300,14), new Color(0.4f,0.2f,1f));
        AnchorTopLeft(playerHP.GetComponent<RectTransform>());
        AnchorTopLeft(playerCharge.GetComponent<RectTransform>());
        AnchorTopLeft(playerFocus.GetComponent<RectTransform>());

        // Guard icon (top-left, below focus bar)
        var guardIconGO = new GameObject("GuardIcon", typeof(RectTransform), typeof(Image));
        guardIconGO.transform.SetParent(canvasGO.transform, false);
        var guardRT = guardIconGO.GetComponent<RectTransform>();
        guardRT.anchorMin = guardRT.anchorMax = new Vector2(0,1);
        guardRT.pivot     = new Vector2(0,1);
        guardRT.anchoredPosition = new Vector2(20, -115);
        guardRT.sizeDelta = new Vector2(50, 20);
        guardIconGO.GetComponent<Image>().color = new Color(0.2f,0.6f,1f,0.8f);
        var guardLabel = MakeText(guardIconGO.transform, "GuardLabel", "GUARD", 12);
        StretchFill(guardLabel.GetComponent<RectTransform>());

        // ── Bot HUD (top-right) ──────────────────────────
        var botHP = MakeSlider(canvasGO.transform, "BotHPBar", new Vector2(-180,-30), new Vector2(300,30), new Color(1f,0.3f,0.2f));
        var botHPRT = botHP.GetComponent<RectTransform>();
        botHPRT.anchorMin = botHPRT.anchorMax = new Vector2(1,1);
        botHPRT.pivot     = new Vector2(1,1);

        // ── Round info (top-center) ──────────────────────
        var roundLabel = MakeText(canvasGO.transform, "RoundLabel",    "Round 1",    28);
        var timerText  = MakeText(canvasGO.transform, "TimerText",     "90",         36);
        CenterTop(roundLabel.GetComponent<RectTransform>(), -30, 160, 40);
        CenterTop(timerText.GetComponent<RectTransform>(),  -75,  80, 45);

        // Round result (center screen, hidden)
        var roundResult = MakeText(canvasGO.transform, "RoundResultText", "", 52);
        var rrRT = roundResult.GetComponent<RectTransform>();
        rrRT.anchorMin = new Vector2(0.5f,0.5f);
        rrRT.anchorMax = new Vector2(0.5f,0.5f);
        rrRT.pivot = new Vector2(0.5f,0.5f);
        rrRT.anchoredPosition = Vector2.zero;
        rrRT.sizeDelta = new Vector2(600,80);
        roundResult.alignment = TextAlignmentOptions.Center;
        roundResult.color = Color.yellow;
        roundResult.gameObject.SetActive(false);

        // ── Debug overlay (full-screen panel, hidden) ────
        var debugPanel = new GameObject("DebugPanel", typeof(RectTransform), typeof(Image));
        debugPanel.transform.SetParent(canvasGO.transform, false);
        var dpRT = debugPanel.GetComponent<RectTransform>();
        StretchFill(dpRT);
        debugPanel.GetComponent<Image>().color = new Color(0,0,0,0.65f);
        var debugText = MakeText(debugPanel.transform, "DebugText", "", 14);
        debugText.alignment = TextAlignmentOptions.TopLeft;
        debugText.color = Color.white;
        var dtRT = debugText.GetComponent<RectTransform>();
        StretchFill(dtRT);
        dtRT.offsetMin = new Vector2(20,20);
        dtRT.offsetMax = new Vector2(-20,-20);
        debugPanel.SetActive(false);

        // ── Attach HUDController ─────────────────────────
        var hud = canvasGO.AddComponent<HUDController>();
        SetField(hud, "_playerHPBar",      playerHP);
        SetField(hud, "_playerChargeBar",  playerCharge);
        SetField(hud, "_playerFocusBar",   playerFocus);
        SetField(hud, "_playerGuardIcon",  guardIconGO.GetComponent<Image>());
        SetField(hud, "_botHPBar",         botHP);
        SetField(hud, "_timerText",        timerText);
        SetField(hud, "_roundLabel",       roundLabel);
        SetField(hud, "_roundResultText",  roundResult);

        // ── Attach DebugOverlay ──────────────────────────
        var dbg = canvasGO.AddComponent<DebugOverlay>();
        SetField(dbg, "_panel", debugPanel);
        SetField(dbg, "_text",  debugText);

        return (canvasGO, hud, dbg);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static Slider MakeSlider(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(go.transform, false);
        StretchFill(bgGO.GetComponent<RectTransform>());
        bgGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        var fillAreaGO = new GameObject("FillArea", typeof(RectTransform));
        fillAreaGO.transform.SetParent(go.transform, false);
        var faRT = fillAreaGO.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = new Vector2(3, 3); faRT.offsetMax = new Vector2(-3, -3);

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        StretchFill(fillGO.GetComponent<RectTransform>());
        fillGO.GetComponent<Image>().color = fillColor;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillGO.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
        slider.interactable = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        return slider;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        return tmp;
    }

    static void AnchorTopLeft(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
    }

    static void CenterTop(RectTransform rt, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetMaterialColor(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        // Creates a material instance — fine for placeholders
        r.material.color = color;
    }

    // Sets a private [SerializeField] field via SerializedObject
    static void SetField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        else Debug.LogWarning($"[SceneBuilder] Field '{fieldName}' not found on {target.GetType().Name}");
    }

    static void SetField(Object target, string fieldName, float value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
