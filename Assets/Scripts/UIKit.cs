using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Piezas para armar interfaces por código (menú, Options, pausa, resultados), con los colores del juego.
public static class UIKit
{
    public static readonly Color Panel = new Color32(26, 18, 32, 235);
    public static readonly Color Gold = new Color32(201, 162, 77, 255);
    public static readonly Color Cream = new Color32(255, 244, 214, 255);
    public static readonly Color Teal = new Color32(124, 184, 174, 255);
    public static readonly Color TealDark = new Color32(47, 95, 98, 255);
    public static readonly Color Orange = new Color32(238, 138, 58, 255);

    public static TMP_FontAsset Font => GameUI.FindFont();

    public static RectTransform Canvas(Transform parent, string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(960, 540);
        s.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        return (RectTransform)go.transform;
    }

    public static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    public static RectTransform Box(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    public static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Framed(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var rt = Place(Box(parent, name, Panel), new Vector2(0.5f, 0.5f), pos, size);
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = Gold;
        o.effectDistance = new Vector2(3, -3);
        return rt;
    }

    public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color,
                                       TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        var f = Font;
        if (f != null) t.font = f;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        Stretch(t.rectTransform);
        return t;
    }

    public static Image Icon(Transform parent, Sprite sprite, Vector2 pos, float size)
    {
        var go = new GameObject("Icono", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.enabled = sprite != null;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);
        return img;
    }

    // Botón con icono a la izquierda (opcional) y texto.
    public static Button Button(Transform parent, string label, Sprite icon, Vector2 pos, Vector2 size, UnityAction onClick)
    {
        var rt = Place(Box(parent, "Boton " + label, TealDark), new Vector2(0.5f, 0.5f), pos, size);
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = Teal;
        o.effectDistance = new Vector2(2, -2);
        var b = rt.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f);
        colors.selectedColor = new Color(1.35f, 1.35f, 1.35f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        b.colors = colors;
        b.onClick.AddListener(onClick);
        if (icon != null) Icon(rt, icon, new Vector2(26, 0), size.y - 12);
        var t = Text(rt, label, size.y * 0.5f, Cream);
        if (icon != null) t.rectTransform.offsetMin = new Vector2(40, 0);
        return b;
    }

    public static Slider Slider(Transform parent, Vector2 pos, Vector2 size, float min, float max, float value,
                                UnityAction<float> onChange)
    {
        var root = Place(new GameObject("Slider", typeof(RectTransform)).GetComponent<RectTransform>(),
                         new Vector2(0.5f, 0.5f), pos, size);
        root.SetParent(parent, false);
        var back = Stretch(Box(root, "Fondo", new Color32(59, 42, 58, 255)));
        back.offsetMin = new Vector2(0, size.y * 0.3f); back.offsetMax = new Vector2(0, -size.y * 0.3f);
        var area = Stretch(new GameObject("Relleno Area", typeof(RectTransform)).GetComponent<RectTransform>());
        area.SetParent(root, false);
        Stretch(area);
        area.offsetMin = new Vector2(0, size.y * 0.3f); area.offsetMax = new Vector2(0, -size.y * 0.3f);
        var fill = Stretch(Box(area, "Relleno", Orange));
        var handleArea = new GameObject("Manija Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(root, false);
        Stretch(handleArea);
        var handle = Box(handleArea, "Manija", Cream);
        handle.sizeDelta = new Vector2(size.y * 0.7f, 0);
        var s = root.gameObject.AddComponent<Slider>();
        s.fillRect = fill;
        s.handleRect = handle;
        s.targetGraphic = handle.GetComponent<Image>();
        s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        s.minValue = min; s.maxValue = max;
        s.value = value;
        s.onValueChanged.AddListener(onChange);
        return s;
    }

    public static Toggle Toggle(Transform parent, Vector2 pos, float size, bool value, UnityAction<bool> onChange)
    {
        var rt = Place(Box(parent, "Casilla", new Color32(59, 42, 58, 255)), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = Teal;
        o.effectDistance = new Vector2(2, -2);
        var check = Stretch(Box(rt, "Palomita", Orange));
        check.offsetMin = new Vector2(5, 5); check.offsetMax = new Vector2(-5, -5);
        var t = rt.gameObject.AddComponent<Toggle>();
        t.targetGraphic = rt.GetComponent<Image>();
        t.graphic = check.GetComponent<Image>();
        t.isOn = value;
        t.onValueChanged.AddListener(onChange);
        return t;
    }

    public static Sprite LoadIcon(string name)
    {
        return Resources.Load<Sprite>("Iconos/" + name);
    }
}
