using System;
using TMPro;
using UnityEngine;

// El panel Options (lo usan el menú principal y la pausa): volumen, pantalla completa y contraste.
public static class OptionsPanel
{
    public static GameObject Build(Transform canvas, Action onBack)
    {
        var panel = UIKit.Framed(canvas, "Options", new Vector2(0, 0), new Vector2(520, 340));

        var title = UIKit.Text(panel, "OPTIONS", 34, UIKit.Gold, TextAlignmentOptions.Top);
        title.rectTransform.offsetMax = new Vector2(0, -18);
        title.fontStyle = FontStyles.Bold;

        Row(panel, "Volume", "volumen", 70);
        UIKit.Slider(panel, new Vector2(90, 70), new Vector2(220, 26), 0f, 1f, GameSettings.Volume,
                     v => GameSettings.Volume = v);

        Row(panel, "Fullscreen", "pantalla", 10);
        UIKit.Toggle(panel, new Vector2(0, 10), 30, GameSettings.Fullscreen, on => GameSettings.Fullscreen = on);

        Row(panel, "Contrast", "contraste", -50);
        UIKit.Slider(panel, new Vector2(90, -50), new Vector2(220, 26), -50f, 50f, GameSettings.Contrast,
                     v => GameSettings.Contrast = v);

        UIKit.Button(panel, "Back", UIKit.LoadIcon("regresar"), new Vector2(0, -125), new Vector2(200, 44), () => onBack());
        return panel.gameObject;
    }

    static void Row(RectTransform panel, string label, string icon, float y)
    {
        var row = UIKit.Place(new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>(),
                              new Vector2(0.5f, 0.5f), new Vector2(-150, y), new Vector2(180, 36));
        row.SetParent(panel, false);
        UIKit.Icon(row, UIKit.LoadIcon(icon), new Vector2(14, 0), 28);
        var t = UIKit.Text(row, label, 20, UIKit.Cream, TextAlignmentOptions.MidlineLeft);
        t.rectTransform.offsetMin = new Vector2(38, 0);
    }
}
