using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Las opciones del panel Options: volumen, pantalla completa y contraste.
// Se guardan con PlayerPrefs (quedan guardadas aunque cierres el juego) y se aplican solas en cada escena.
// El contraste usa el post-procesado de URP (Color Adjustments) en un Volume global creado por código.
public static class GameSettings
{
    const string KeyVolume = "opt_volume";
    const string KeyFullscreen = "opt_fullscreen";
    const string KeyContrast = "opt_contrast";

    public static float Volume
    {
        get => PlayerPrefs.GetFloat(KeyVolume, 0.8f);
        set { PlayerPrefs.SetFloat(KeyVolume, value); Apply(); }
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
        set { PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0); Apply(); }
    }

    // De -50 (menos contraste) a 50 (más contraste). 0 = normal.
    public static float Contrast
    {
        get => PlayerPrefs.GetFloat(KeyContrast, 0f);
        set { PlayerPrefs.SetFloat(KeyContrast, value); Apply(); }
    }

    static ColorAdjustments adjustments;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Apply();
        Apply();
    }

    public static void Apply()
    {
        AudioListener.volume = Volume;
        if (Screen.fullScreen != Fullscreen) Screen.fullScreen = Fullscreen;

        if (adjustments == null)
        {
            var go = new GameObject("ContrasteGlobal");
            Object.DontDestroyOnLoad(go);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            adjustments = profile.Add<ColorAdjustments>(true);
            volume.profile = profile;
        }
        adjustments.contrast.overrideState = true;
        adjustments.contrast.value = Contrast;

        // Prende el post-procesado en la cámara de la escena (sin él, el contraste no se ve).
        var cam = Camera.main;
        if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        PlayerPrefs.Save();
    }
}
