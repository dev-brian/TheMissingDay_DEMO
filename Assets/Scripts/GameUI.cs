using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

// Toda la interfaz del juego, armada por código para que no tengas que construirla a mano:
//   - HUD: misión (arriba a la izquierda), puntos, barra de energía y contador de pistas (arriba a la derecha)
//   - Letrero "Press E" abajo al centro
//   - Caja de diálogo con efecto de máquina de escribir (Espacio, Enter o E para avanzar)
//   - Panel de preguntas de inglés con 3 opciones (clic o teclas 1, 2 y 3) y retroalimentación inmediata
//   - Banners grandes (LEVEL 1, MISSION COMPLETED, ACHIEVEMENT UNLOCKED), avisos cortos y fundido a negro
// No hace falta ponerlo en la escena: se crea solo. Si quieres cambiar la fuente o los sonidos,
// ponlo tú en un objeto vacío llamado GameUI y llena sus campos en el Inspector.
public class GameUI : MonoBehaviour
{
    [Serializable]
    public class Question
    {
        [Tooltip("Nombre corto para las métricas, por ejemplo N1_Camara.")]
        public string id = "N1_Pregunta";
        public string instruction = "Complete the sentence:";
        [TextArea] public string text = "Enedina _____ movies.";
        public string[] options = { "love", "loves", "loving" };
        [Tooltip("0 = primera opción, 1 = segunda, 2 = tercera.")]
        public int correct = 1;
        [TextArea] public string feedbackCorrect = "Correct! Enedina loves movies.";
        [TextArea] public string feedbackWrong = "Try again! Remember the third-person singular rule.";
    }

    static GameUI instance;
    public static GameUI I
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameUI>();
                if (instance == null) instance = new GameObject("GameUI").AddComponent<GameUI>();
            }
            return instance;
        }
    }

    [Header("Fuente (vacío = la de TextMeshPro por defecto)")]
    public TMP_FontAsset font;

    [Header("Sonidos (opcionales)")]
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip pickupClip;
    public AudioClip typeClip;

    [Header("Colores (paleta del juego)")]
    public Color panelColor = new Color32(26, 18, 32, 230);
    public Color borderColor = new Color32(201, 162, 77, 255);     // dorado del collar
    public Color textColor = new Color32(255, 244, 214, 255);
    public Color accentColor = new Color32(124, 184, 174, 255);   // verde azulado de la chamarra
    public Color goodColor = new Color32(138, 184, 90, 255);
    public Color badColor = new Color32(224, 90, 90, 255);
    public Color energyColor = new Color32(238, 138, 58, 255);

    public bool Busy => lockCount > 0;

    // La fuente que usan todas las pantallas (la de GameUI si tiene una, si no la de TextMeshPro).
    public static TMP_FontAsset FindFont()
    {
        var g = instance != null ? instance : FindFirstObjectByType<GameUI>();
        if (g != null && g.font != null) return g.font;
        return TMP_Settings.defaultFontAsset;
    }

    int lockCount;
    GameSession session;
    Canvas canvas;
    AudioSource sfx;
    TextMeshProUGUI objectiveText, scoreText, counterText, promptText, toastText;
    Image energyFill;
    GameObject dialogueBox, questionPanel;
    TextMeshProUGUI speakerText, dialogueText, continueHint;
    TextMeshProUGUI qInstruction, qText, qFeedback;
    readonly Button[] optionButtons = new Button[3];
    readonly TextMeshProUGUI[] optionLabels = new TextMeshProUGUI[3];
    CanvasGroup bannerGroup, fader;
    TextMeshProUGUI bannerBig, bannerSmall;
    Coroutine toastRoutine;
    int chosen = -1;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (font == null) font = TMP_Settings.defaultFontAsset;
        sfx = gameObject.AddComponent<AudioSource>();
        EnsureEventSystem();
        Build();
        session = GameSession.I;
        session.Changed += RefreshHud;
        if (GetComponent<PauseMenu>() == null) gameObject.AddComponent<PauseMenu>();
        if (GetComponent<ClueJournal>() == null) gameObject.AddComponent<ClueJournal>();
        RefreshHud();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (session != null) session.Changed -= RefreshHud;
    }

    IEnumerator Start()
    {
        yield return Fade(1f, 0f, 0.6f);
    }

    // ---------- Bloquear a Enedina mientras hay diálogo o pregunta ----------

    public void Lock()
    {
        lockCount++;
        SetPlayer(false);
    }

    public void Unlock()
    {
        lockCount = Mathf.Max(0, lockCount - 1);
        if (lockCount == 0) SetPlayer(true);
    }

    static void SetPlayer(bool on)
    {
        var p = FindFirstObjectByType<PlayerPlatformer>();
        if (p != null) p.enabled = on;
    }

    // ---------- HUD ----------

    public void SetObjective(string text)
    {
        objectiveText.text = string.IsNullOrEmpty(text) ? "" : "<color=#C9A24D>MISSION</color>  " + text;
        objectiveText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    public void SetCounter(string label, int found, int total)
    {
        counterText.text = total > 0 ? $"{label}: {found}/{total}" : "";
    }

    public void RefreshHud()
    {
        if (scoreText == null) return;
        var s = GameSession.I;
        scoreText.text = $"<color=#C9A24D>POINTS</color> {s.Score}";
        energyFill.fillAmount = (float)s.Energy / GameSession.MaxEnergy;
    }

    public void ShowPrompt(string text)
    {
        promptText.text = text;
        promptText.gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        promptText.gameObject.SetActive(false);
    }

    public void Toast(string text, bool good = true)
    {
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(text, good));
    }

    IEnumerator ToastRoutine(string text, bool good)
    {
        toastText.text = text;
        toastText.color = good ? goodColor : badColor;
        toastText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.8f);
        toastText.gameObject.SetActive(false);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip != null) sfx.PlayOneShot(clip);
    }

    public void PlayPickup() => PlaySfx(pickupClip);

    // ---------- Diálogos ----------

    // Muestra varias líneas, una por una. Espacio, Enter o E para avanzar.
    public IEnumerator ShowLines(string speaker, string[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;
        Lock();
        dialogueBox.SetActive(true);
        speakerText.text = speaker;
        yield return null;   // que la tecla que abrió el diálogo no salte la primera línea
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            dialogueText.text = line;
            dialogueText.maxVisibleCharacters = 0;
            continueHint.gameObject.SetActive(false);
            int total = line.Length;
            for (int i = 0; i <= total; i++)
            {
                dialogueText.maxVisibleCharacters = i;
                if (i % 3 == 0) PlaySfx(typeClip);
                if (AdvancePressed()) { dialogueText.maxVisibleCharacters = total; break; }
                yield return new WaitForSeconds(0.025f);
            }
            dialogueText.maxVisibleCharacters = 99999;
            continueHint.gameObject.SetActive(true);
            yield return null;
            while (!AdvancePressed()) yield return null;
            yield return null;
        }
        dialogueBox.SetActive(false);
        Unlock();
    }

    // ---------- Preguntas de inglés ----------

    // Abre la pregunta y no termina hasta que se responde bien.
    // Correcta: +20 puntos y +1 energía. Incorrecta: -5 puntos y -1 energía, y se reintenta.
    public IEnumerator Ask(Question q)
    {
        Lock();
        var session = GameSession.I;
        questionPanel.SetActive(true);
        qInstruction.text = q.instruction;
        qText.text = q.text;
        qFeedback.text = "Click an answer or press 1, 2 or 3";
        qFeedback.color = accentColor;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool has = q.options != null && i < q.options.Length;
            optionButtons[i].gameObject.SetActive(has);
            optionButtons[i].interactable = true;
            if (has) optionLabels[i].text = $"{i + 1})  {q.options[i]}";
        }

        int attempts = 0;
        float t0 = Time.time;
        bool solved = false;
        yield return null;
        while (!solved)
        {
            chosen = -1;
            while (chosen < 0)
            {
                int k = NumberPressed();
                if (k >= 0 && k < q.options.Length && optionButtons[k].interactable) chosen = k;
                yield return null;
            }
            attempts++;
            bool ok = chosen == q.correct;
            MetricsLogger.LogAnswer(q.id, attempts, q.options[chosen], ok);
            if (ok)
            {
                solved = true;
                session.CorrectAnswers++;
                session.AddScore(20);
                session.AddEnergy(1);
                qFeedback.color = goodColor;
                qFeedback.text = q.feedbackCorrect + "   <size=70%>+20</size>";
                PlaySfx(correctClip);
                MetricsLogger.Log(q.id, attempts, true, Time.time - t0);
                foreach (var b in optionButtons) b.interactable = false;
                yield return new WaitForSeconds(0.4f);
                float until = Time.time + 2.5f;
                while (Time.time < until && !AdvancePressed()) yield return null;
            }
            else
            {
                session.WrongAnswers++;
                session.AddScore(-5);
                session.AddEnergy(-1);
                optionButtons[chosen].interactable = false;
                qFeedback.color = badColor;
                qFeedback.text = q.feedbackWrong + "   <size=70%>-5</size>";
                if (session.Energy <= 0)
                {
                    qFeedback.text += "\nEnedina is tired... Let's try again!";
                    session.AddEnergy(GameSession.StartEnergy);
                }
                PlaySfx(wrongClip);
                yield return new WaitForSeconds(0.3f);
            }
        }
        questionPanel.SetActive(false);
        Unlock();
    }

    // ---------- Banners, fundidos y cambio de escena ----------

    public IEnumerator Banner(string big, string small, float seconds = 2.2f)
    {
        bannerBig.text = big;
        bannerSmall.text = small;
        yield return FadeGroup(bannerGroup, 0f, 1f, 0.3f);
        float until = Time.time + seconds;
        while (Time.time < until && !AdvancePressed()) yield return null;
        yield return FadeGroup(bannerGroup, 1f, 0f, 0.3f);
    }

    public IEnumerator Fade(float from, float to, float seconds)
    {
        yield return FadeGroup(fader, from, to, seconds);
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        Lock();
        yield return Fade(0f, 1f, 0.6f);
        SceneManager.LoadScene(sceneName);
    }

    static IEnumerator FadeGroup(CanvasGroup g, float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            g.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        g.alpha = to;
    }

    // ---------- Construcción de la interfaz ----------

    void Build()
    {
        var cgo = new GameObject("Canvas", typeof(RectTransform));
        cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        scaler.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();
        var root = cgo.transform;

        // Misión (arriba a la izquierda)
        var obj = Panel(root, "Mision", new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), new Vector2(430, 40));
        objectiveText = Text(obj, "Texto", 17, TextAlignmentOptions.MidlineLeft, new Vector2(12, 4));
        obj.gameObject.SetActive(false);

        // Puntos, energía y contador (arriba a la derecha)
        var hud = Panel(root, "HUD", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), new Vector2(230, 92));
        scoreText = Text(hud, "Puntos", 17, TextAlignmentOptions.TopLeft, new Vector2(12, 6));
        var label = Text(hud, "EnergiaTexto", 13, TextAlignmentOptions.MidlineLeft, new Vector2(12, 6));
        label.text = "<color=#EE8A3A>ENERGY</color>";
        Place(label.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -32), new Vector2(70, 20));
        var barBack = Box(hud, "BarraFondo", new Color32(59, 42, 58, 255));
        Place(barBack, new Vector2(0, 1), new Vector2(0, 1), new Vector2(82, -35), new Vector2(136, 14));
        var fillRect = Box(barBack, "BarraRelleno", energyColor);
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2); fillRect.offsetMax = new Vector2(-2, -2);
        energyFill = fillRect.GetComponent<Image>();
        energyFill.sprite = WhiteSprite();
        energyFill.type = Image.Type.Filled;
        energyFill.fillMethod = Image.FillMethod.Horizontal;
        counterText = Text(hud, "Contador", 17, TextAlignmentOptions.BottomLeft, new Vector2(12, 8));
        counterText.color = accentColor;

        // Letrero de interacción
        promptText = Text(root, "Prompt", 18, TextAlignmentOptions.Center, Vector2.zero);
        Place(promptText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(500, 30));
        promptText.text = "Press E";
        promptText.fontStyle = FontStyles.Bold;
        promptText.gameObject.SetActive(false);

        // Aviso corto
        toastText = Text(root, "Aviso", 22, TextAlignmentOptions.Center, Vector2.zero);
        Place(toastText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(700, 40));
        toastText.fontStyle = FontStyles.Bold;
        toastText.gameObject.SetActive(false);

        // Caja de diálogo
        var dlg = Panel(root, "Dialogo", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(760, 120));
        dialogueBox = dlg.gameObject;
        speakerText = Text(dlg, "Nombre", 16, TextAlignmentOptions.TopLeft, new Vector2(18, 10));
        speakerText.color = borderColor;
        speakerText.fontStyle = FontStyles.Bold;
        dialogueText = Text(dlg, "Texto", 21, TextAlignmentOptions.TopLeft, new Vector2(18, 10));
        dialogueText.rectTransform.offsetMax = new Vector2(-18, -34);
        continueHint = Text(dlg, "Seguir", 13, TextAlignmentOptions.BottomRight, new Vector2(14, 8));
        continueHint.text = "SPACE  >";
        continueHint.color = accentColor;
        dialogueBox.SetActive(false);

        // Panel de preguntas
        var qp = Panel(root, "Pregunta", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(620, 330));
        questionPanel = qp.gameObject;
        var title = Text(qp, "Titulo", 15, TextAlignmentOptions.Top, new Vector2(16, 12));
        title.text = "<color=#C9A24D>ENGLISH DETECTIVE</color>";
        qInstruction = Text(qp, "Instruccion", 16, TextAlignmentOptions.Top, new Vector2(16, 38));
        qInstruction.color = accentColor;
        qText = Text(qp, "Frase", 26, TextAlignmentOptions.Top, new Vector2(16, 66));
        qText.fontStyle = FontStyles.Bold;
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var bRect = Box(qp, "Opcion" + (i + 1), new Color32(47, 95, 98, 255));
            Place(bRect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -132 - i * 46), new Vector2(360, 38));
            var outline = bRect.gameObject.AddComponent<Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(2, -2);
            var b = bRect.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            b.colors = colors;
            b.onClick.AddListener(() => chosen = index);
            optionButtons[i] = b;
            optionLabels[i] = Text(bRect, "Texto", 20, TextAlignmentOptions.Center, Vector2.zero);
        }
        qFeedback = Text(qp, "Retro", 17, TextAlignmentOptions.Bottom, new Vector2(16, 14));
        questionPanel.SetActive(false);

        // Banner grande
        var bgo = new GameObject("Banner", typeof(RectTransform));
        bgo.transform.SetParent(root, false);
        var brt = (RectTransform)bgo.transform;
        brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
        brt.sizeDelta = new Vector2(0, 150); brt.anchoredPosition = new Vector2(0, 40);
        bgo.AddComponent<Image>().color = new Color32(26, 18, 32, 200);
        bannerGroup = bgo.AddComponent<CanvasGroup>();
        bannerGroup.alpha = 0f;
        bannerGroup.blocksRaycasts = false;
        bannerBig = Text(brt, "Grande", 44, TextAlignmentOptions.Center, Vector2.zero);
        bannerBig.rectTransform.offsetMin = new Vector2(0, 45);
        bannerBig.color = borderColor;
        bannerBig.fontStyle = FontStyles.Bold;
        bannerSmall = Text(brt, "Chico", 20, TextAlignmentOptions.Center, Vector2.zero);
        bannerSmall.rectTransform.offsetMax = new Vector2(0, -70);

        // Fundido a negro (encima de todo)
        var fRect = Box(root, "Fundido", Color.black);
        fRect.anchorMin = Vector2.zero; fRect.anchorMax = Vector2.one;
        fRect.offsetMin = Vector2.zero; fRect.offsetMax = Vector2.zero;
        fader = fRect.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 1f;
        fader.blocksRaycasts = false;
    }

    RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = Box(parent, name, panelColor);
        Place(rt, anchor, pivot, pos, size);
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = borderColor;
        o.effectDistance = new Vector2(2, -2);
        return rt;
    }

    static RectTransform Box(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    // Texto que llena a su padre con un margen.
    TextMeshProUGUI Text(Transform parent, string name, float size, TextAlignmentOptions align, Vector2 margin)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = textColor;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = margin; rt.offsetMax = -margin;
        return t;
    }

    static Sprite whiteSprite;
    static Sprite WhiteSprite()
    {
        if (whiteSprite == null)
        {
            var tex = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return whiteSprite;
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    // ---------- Teclado ----------

    public static bool AdvancePressed()
    {
        if (PauseMenu.Paused) return false;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame
                             || k.numpadEnterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
               || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E);
#endif
    }

    public static bool InteractPressed()
    {
        if (PauseMenu.Paused) return false;
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    static int NumberPressed()
    {
        if (PauseMenu.Paused) return -1;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return -1;
        if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) return 0;
        if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) return 1;
        if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) return 2;
#else
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 2;
#endif
        return -1;
    }
}
