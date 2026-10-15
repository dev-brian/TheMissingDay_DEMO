using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// C01: primero los datos reales del problema (EF EPI 2025) y después el animatic del equipo.
// Va en un objeto vacío "Cinematica" de la escena C01_Intro. Arrastra el video al campo Animatic.
// ENTER pasa a la siguiente lámina; ESC se salta todo.
public class CinematicIntro : MonoBehaviour
{
    public VideoClip animatic;
    public string nextScene = "N1_MiCuarto";
    public float secondsPerSlide = 6f;

    RectTransform canvas;
    CanvasGroup slideGroup;
    RectTransform slideRoot;
    bool skipAll;

    IEnumerator Start()
    {
        canvas = UIKit.Canvas(transform, "Cinematica", 10);
        UIKit.Stretch(UIKit.Box(canvas, "Negro", Color.black));

        var hint = UIKit.Place(new GameObject("Ayuda", typeof(RectTransform)).GetComponent<RectTransform>(),
                               new Vector2(1, 0), new Vector2(-150, 24), new Vector2(280, 24));
        hint.SetParent(canvas, false);
        UIKit.Text(hint, "ENTER  next     ESC  skip", 13, UIKit.Teal, TextAlignmentOptions.Right);

        // ---- Láminas con datos ----
        yield return Slide(s =>
        {
            Title(s, "In 2025, Mexico ranked <color=#EE8A3A>103rd</color> in English proficiency.", 30, 60);
            Note(s, "En 2025, México quedó en el lugar 103 del índice de dominio del inglés EF EPI.", -20);
        });
        yield return Slide(s =>
        {
            Title(s, "Mexico is <color=#EE8A3A>below</color> the world average", 28, 150);
            Bar(s, "Mexico", 440, 600f, 40, UIKit.Orange);
            Bar(s, "World average", 488, 600f, -20, UIKit.Teal);
            Note(s, "Puntaje EF EPI 2025: México 440, promedio mundial 488 (19 puntos menos que el año anterior).", -110);
        });
        yield return Slide(s =>
        {
            Title(s, "The hardest skills: <color=#EE8A3A>writing</color> and <color=#EE8A3A>speaking</color>", 28, 170);
            Bar(s, "Reading", 455, 600f, 90, UIKit.Teal);
            Bar(s, "Listening", 422, 600f, 40, UIKit.Teal);
            Bar(s, "Speaking", 413, 600f, -10, UIKit.Orange);
            Bar(s, "Writing", 399, 600f, -60, UIKit.Orange);
            Note(s, "Fuente: EF English Proficiency Index 2025, ficha de México.", -140);
        });
        yield return Slide(s =>
        {
            Title(s, "Enedina wants to tell her own stories...\n<size=75%>in English.</size>", 32, 40);
            Note(s, "The Missing Day: aprender inglés (Present Simple) explorando, jugando y descubriendo.", -60);
        });

        // ---- Animatic del equipo ----
        if (animatic != null && !skipAll) yield return PlayVideo();

        SceneManager.LoadScene(nextScene);
    }

    delegate void Fill(RectTransform slide);

    IEnumerator Slide(Fill fill)
    {
        if (skipAll) yield break;
        var go = new GameObject("Lamina", typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        slideRoot = UIKit.Stretch((RectTransform)go.transform);
        slideGroup = go.AddComponent<CanvasGroup>();
        slideGroup.alpha = 0f;
        fill(slideRoot);
        yield return FadeGroup(slideGroup, 0f, 1f, 0.6f);
        yield return null;
        float until = Time.time + secondsPerSlide;
        while (Time.time < until)
        {
            if (EscPressed()) { skipAll = true; break; }
            if (NextPressed()) break;
            yield return null;
        }
        yield return FadeGroup(slideGroup, 1f, 0f, 0.4f);
        Destroy(go);
    }

    IEnumerator PlayVideo()
    {
        var raw = UIKit.Stretch(new GameObject("Video", typeof(RectTransform)).GetComponent<RectTransform>());
        raw.SetParent(canvas, false);
        UIKit.Stretch(raw);
        var image = raw.gameObject.AddComponent<RawImage>();
        var fitter = raw.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
        raw.SetAsFirstSibling();
        raw.SetSiblingIndex(1);   // encima del fondo negro, debajo de la ayuda

        var vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.clip = animatic;
        vp.renderMode = VideoRenderMode.APIOnly;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        bool finished = false;
        vp.loopPointReached += _ => finished = true;
        vp.Prepare();
        while (!vp.isPrepared) yield return null;
        image.texture = vp.texture;
        vp.Play();
        yield return null;
        while (!finished)
        {
            if (EscPressed() || NextPressed()) break;
            image.texture = vp.texture;
            yield return null;
        }
        vp.Stop();
    }

    // ---------- Piezas de las láminas ----------

    static void Title(RectTransform s, string text, float size, float y)
    {
        var rt = UIKit.Place(new GameObject("Titulo", typeof(RectTransform)).GetComponent<RectTransform>(),
                             new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(820, 120));
        rt.SetParent(s, false);
        var t = UIKit.Text(rt, text, size, UIKit.Cream);
        t.fontStyle = FontStyles.Bold;
    }

    static void Note(RectTransform s, string text, float y)
    {
        var rt = UIKit.Place(new GameObject("Nota", typeof(RectTransform)).GetComponent<RectTransform>(),
                             new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(820, 60));
        rt.SetParent(s, false);
        UIKit.Text(rt, text, 17, UIKit.Teal);
    }

    static void Bar(RectTransform s, string label, int value, float max, float y, Color color)
    {
        var row = UIKit.Place(new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>(),
                              new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(760, 36));
        row.SetParent(s, false);
        var l = UIKit.Text(row, label, 18, UIKit.Cream, TextAlignmentOptions.MidlineRight);
        l.rectTransform.offsetMax = new Vector2(-560, 0);
        var bar = UIKit.Box(row, "Barra", color);
        bar.anchorMin = new Vector2(0, 0.15f); bar.anchorMax = new Vector2(0, 0.85f);
        bar.pivot = new Vector2(0, 0.5f);
        bar.anchoredPosition = new Vector2(215, 0);
        bar.sizeDelta = new Vector2(460f * value / max, 0);
        var v = UIKit.Text(row, value.ToString(), 18, UIKit.Gold, TextAlignmentOptions.MidlineLeft);
        v.rectTransform.offsetMin = new Vector2(225 + 460f * value / max, 0);
        v.fontStyle = FontStyles.Bold;
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

    static bool NextPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#endif
    }

    static bool EscPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
