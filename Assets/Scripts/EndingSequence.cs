using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// El final de la demo (Nivel 3), armado por código como GameUI:
//   1) El guion completo aparece a pantalla completa, como la carta de Plantas contra Zombies
//   2) Enedina descubre el nombre de ELENA y lo comenta
//   3) La pantalla se oscurece y solo queda el nombre ELENA
//   4) "A secret that the family has kept hidden for years is about to come to light..."
//   5) TO BE CONTINUED... / YAVIGA · THE MISSING DAY / END OF DEMO
// Va en un objeto vacío "Final". En el LevelManager del Nivel 3, en On Completed, arrastra este objeto
// y elige EndingSequence > Play.
public class EndingSequence : MonoBehaviour
{
    [Header("El guion (puedes cambiarlo)")]
    public string scriptTitle = "THE STAR OF TLAXCALA";
    [TextArea(8, 20)]
    public string scriptBody =
        "SCENE 1. A small town in Tlaxcala. Morning.\n\n" +
        "A young woman walks to the plaza. She has an old camera.\n\n" +
        "WOMAN: Today is a special day.\n\n" +
        "She opens a small box. Inside, there is a golden necklace with a star.\n\n" +
        "WOMAN: This star is my secret. Nobody knows it.";
    public string starName = "ELENA";

    [Header("Diálogo de Enedina")]
    [TextArea] public string[] enedinaLines =
    {
        "Wait... Elena? That famous actress from Tlaxcala?",
        "I wonder why her name is here.",
        "Could this be one of her movies? There must be a story behind this."
    };

    [Header("Textos finales")]
    [TextArea] public string secretText = "A SECRET THAT THE FAMILY HAS KEPT HIDDEN FOR YEARS IS ABOUT TO COME TO LIGHT...";
    [Tooltip("Escena al terminar (por ejemplo Resultados o Menu). Vacío = se queda en la pantalla final.")]
    public string nextScene = "Resultados";

    Color paper = new Color32(246, 226, 196, 255);
    Color ink = new Color32(59, 42, 58, 255);
    Color gold = new Color32(201, 162, 77, 255);
    Color red = new Color32(168, 42, 58, 255);

    CanvasGroup pageGroup, blackGroup, nameGroup, secretGroup, endGroup;
    TMP_FontAsset font;
    bool playing;

    public void Play()
    {
        if (playing) return;
        playing = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        var ui = GameUI.I;
        font = ui.font != null ? ui.font : TMP_Settings.defaultFontAsset;
        Build();
        ui.Lock();
        ui.SetObjective("");

        // 1) El guion a pantalla completa
        yield return FadeGroup(pageGroup, 0f, 1f, 0.8f);
        yield return new WaitForSeconds(1.5f);

        // 2) Enedina descubre el nombre
        yield return ui.ShowLines("ENEDINA", enedinaLines);

        // 3) Oscurece; solo queda ELENA
        StartCoroutine(FadeGroup(nameGroup, 0f, 1f, 2.5f));
        yield return FadeGroup(blackGroup, 0f, 1f, 2.5f);
        yield return new WaitForSeconds(2f);
        yield return FadeGroup(nameGroup, 1f, 0f, 1.2f);

        // 4) El secreto
        yield return FadeGroup(secretGroup, 0f, 1f, 1.5f);
        yield return new WaitForSeconds(3.5f);
        yield return FadeGroup(secretGroup, 1f, 0f, 1f);

        // 5) Continuará
        yield return FadeGroup(endGroup, 0f, 1f, 1.5f);
        MetricsLogger.Log("Demo_completa", 1, true, Time.time - GameSession.I.StartTime);
        yield return new WaitForSeconds(1f);
        while (!GameUI.AdvancePressed()) yield return null;

        if (!string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene))
            SceneManager.LoadScene(nextScene);
    }

    // ---------- Construcción ----------

    void Build()
    {
        // Debajo del diálogo de GameUI (orden 50): la hoja del guion
        var below = MakeCanvas("FinalGuion", 40);
        var dim = Box(below, "Oscuro", new Color(0.05f, 0.03f, 0.08f, 0.85f), true);
        pageGroup = dim.gameObject.AddComponent<CanvasGroup>();
        pageGroup.alpha = 0f;
        var page = Box(dim, "Hoja", paper, false);
        page.anchorMin = page.anchorMax = new Vector2(0.5f, 0.5f);
        page.sizeDelta = new Vector2(520, 440);
        page.anchoredPosition = new Vector2(0, 40);
        page.localRotation = Quaternion.Euler(0, 0, -1.5f);
        var o = page.gameObject.AddComponent<Outline>();
        o.effectColor = new Color32(196, 154, 124, 255);
        o.effectDistance = new Vector2(3, -3);
        Text(page, scriptTitle, 26, ink, TextAlignmentOptions.Top, new Vector4(24, 18, 24, 0), FontStyles.Bold);
        Text(page, "A movie script", 14, new Color32(140, 106, 92, 255), TextAlignmentOptions.Top, new Vector4(24, 52, 24, 0), FontStyles.Italic);
        Text(page, scriptBody, 16, ink, TextAlignmentOptions.TopLeft, new Vector4(30, 90, 30, 70), FontStyles.Normal);
        Text(page, "Starring: <color=#A82A3A><b>" + starName + "</b></color>", 22, ink, TextAlignmentOptions.Bottom,
             new Vector4(24, 0, 24, 22), FontStyles.Normal);

        // Encima de todo: negro, el nombre y los textos finales
        var above = MakeCanvas("FinalNegro", 60);
        var black = Box(above, "Negro", Color.black, true);
        blackGroup = black.gameObject.AddComponent<CanvasGroup>();
        blackGroup.alpha = 0f;
        blackGroup.blocksRaycasts = false;

        nameGroup = Group(above, "Nombre");
        Text((RectTransform)nameGroup.transform, starName, 72, red, TextAlignmentOptions.Center, Vector4.zero, FontStyles.Bold);

        secretGroup = Group(above, "Secreto");
        Text((RectTransform)secretGroup.transform, secretText, 26, paper, TextAlignmentOptions.Center,
             new Vector4(120, 0, 120, 0), FontStyles.Bold);

        endGroup = Group(above, "Continuara");
        Text((RectTransform)endGroup.transform, "TO BE CONTINUED...", 46, gold, TextAlignmentOptions.Center,
             new Vector4(0, 0, 0, 90), FontStyles.Bold);
        Text((RectTransform)endGroup.transform, "YAVIGA  -  THE MISSING DAY\n<size=70%>END OF DEMO</size>", 24, paper,
             TextAlignmentOptions.Center, new Vector4(0, 120, 0, 0), FontStyles.Normal);
        Text((RectTransform)endGroup.transform, "Press SPACE", 14, new Color32(124, 184, 174, 255),
             TextAlignmentOptions.Bottom, new Vector4(0, 0, 0, 24), FontStyles.Normal);
    }

    RectTransform MakeCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(960, 540);
        s.matchWidthOrHeight = 0.5f;
        return (RectTransform)go.transform;
    }

    static RectTransform Box(Transform parent, string name, Color color, bool stretch)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        if (stretch)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        return rt;
    }

    static CanvasGroup Group(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var g = go.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        g.blocksRaycasts = false;
        return g;
    }

    // margin = (izquierda, arriba, derecha, abajo)
    TextMeshProUGUI Text(RectTransform parent, string text, float size, Color color, TextAlignmentOptions align,
                         Vector4 margin, FontStyles style)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin.x, margin.w);
        rt.offsetMax = new Vector2(-margin.z, -margin.y);
        return t;
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
}
