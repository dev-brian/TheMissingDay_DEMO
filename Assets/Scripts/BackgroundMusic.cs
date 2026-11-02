using UnityEngine;

// Música de fondo del juego. No necesitas ponerlo en ninguna escena: se crea solo al iniciar el juego
// (igual que GameSettings) y NO se destruye al cambiar de escena, así que la canción no se reinicia.
//
// Coloca tu canción en:  Assets/Resources/Audio/music.ogg  (o .wav / .mp3)
// El volumen general del panel Options (AudioListener.volume) también controla esta música.
public static class BackgroundMusic
{
    // Volumen de la música respecto al volumen general (0 a 1).
    // Es bajo a propósito para que los efectos (pasos, pistas, preguntas) se escuchen por encima.
    const float MusicVolume = 0.35f;

    static AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (source != null) return;

        var clip = Resources.Load<AudioClip>("Audio/music");
        if (clip == null)
        {
            Debug.LogWarning("BackgroundMusic: no encontré Assets/Resources/Audio/music. La música no sonará.");
            return;
        }

        var go = new GameObject("BackgroundMusic");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = MusicVolume;
        source.Play();
    }
}
