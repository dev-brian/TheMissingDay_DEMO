using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Música por escena + efectos de sonido conectados solos (3er parcial: diseño sonoro).
// NO hay que ponerlo en ninguna escena: se crea solo al iniciar el juego y vive entre escenas.
//
// Qué hace:
//  1. Toca la pista de cada escena (tabla Pistas, abajo) en loop desde Assets/Resources/Audio/.
//  2. Sincroniza acciones con la música:
//     - baja la música (ducking) mientras hay diálogo o pregunta de inglés (GameUI.Busy),
//     - los "stingers" (MISSION COMPLETED, ACHIEVEMENT, victoria) entran en el siguiente tiempo
//       del compás (cuantizados al BPM de la pista) y la música se apaga mientras suenan.
//  3. Llena los AudioClip que estén vacíos: GameUI (correct, wrong, pickup, type) y el salto de Enedina.
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer I { get; private set; }

    // Escena -> (pista, BPM). Pista vacía = silencio (la cinemática trae su propio audio).
    static readonly Dictionary<string, (string clip, float bpm)> Pistas = new Dictionary<string, (string, float)>
    {
        { "Menu",             ("musica_menu",    80f) },
        { "C01_Intro",        ("",               80f) },
        { "N1_MiCuarto",      ("musica_nivel",  100f) },
        { "N2_SalaAbuela",    ("musica_nivel",  100f) },
        { "N3_EstudioAbuela", ("musica_estudio", 92f) },
        { "Resultados",       ("musica_menu",    80f) },
    };

    [Range(0f, 1f)] public float musicVolume = 0.45f;
    [Range(0f, 1f)] public float duckedVolume = 0.15f;   // con diálogo o pregunta en pantalla

    AudioSource music, sfx;
    readonly AudioSource[] scheduled = new AudioSource[2];
    int nextScheduled;
    string currentClip = "";
    float bpm = 100f;
    double musicStartDsp;
    double stingerUntilDsp;
    GameUI ui;
    float nextSearch;
    bool wired;
    string lastScene = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (I != null) return;
        var go = new GameObject("MusicPlayer");
        DontDestroyOnLoad(go);
        go.AddComponent<MusicPlayer>();
    }

    void Awake()
    {
        I = this;
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;
        music.volume = 0f;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        for (int i = 0; i < scheduled.Length; i++)
        {
            scheduled[i] = gameObject.AddComponent<AudioSource>();
            scheduled[i].playOnAwake = false;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ui = null;
        wired = false;
        nextSearch = 0f;

        // Pasar de un nivel a otro por la puerta: sonido de puerta.
        if (lastScene.StartsWith("N") && scene.name.StartsWith("N")) PlaySfx("door");
        lastScene = scene.name;

        if (!Pistas.TryGetValue(scene.name, out var p)) return;   // escena no listada: se queda la música que suena
        bpm = p.bpm;

        if (scene.name == "Resultados")
        {
            PlayTrack("");               // silencio, fanfarria de ganador y luego la música del menú
            float len = Stinger("victoria");
            PlayTrack(p.clip, len + 0.3f);
            return;
        }
        PlayTrack(p.clip);
    }

    void PlayTrack(string clipName, float delay = 0f)
    {
        if (clipName == currentClip && music.isPlaying && delay <= 0f) return;
        currentClip = clipName;
        music.Stop();
        if (string.IsNullOrEmpty(clipName)) return;
        var clip = Load(clipName);
        if (clip == null) return;
        music.clip = clip;
        musicStartDsp = AudioSettings.dspTime + 0.1 + delay;
        music.PlayScheduled(musicStartDsp);
    }

    void Update()
    {
        // Buscar el GameUI de la escena (sin crearlo) y conectar los sonidos que falten.
        if (!wired && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.5f;
            WireScene();
        }

        float target = musicVolume;
        if (AudioSettings.dspTime < stingerUntilDsp) target = 0.03f;
        else if (ui != null && ui.Busy) target = duckedVolume;
        music.volume = Mathf.MoveTowards(music.volume, target, Time.unscaledDeltaTime * 1.5f);
    }

    void WireScene()
    {
        if (ui == null) ui = FindFirstObjectByType<GameUI>();
        if (ui != null)
        {
            if (ui.correctClip == null) ui.correctClip = Load("correct");
            if (ui.wrongClip == null) ui.wrongClip = Load("wrong");
            if (ui.pickupClip == null) ui.pickupClip = Load("pickup");
            if (ui.typeClip == null) ui.typeClip = Load("type");
        }

        var jump = Load("jump");
        foreach (var p in FindObjectsByType<PlayerPlatformer>(FindObjectsSortMode.None))
        {
            if (p.jumpClip == null) p.jumpClip = jump;
            if (p.audioSource == null) p.audioSource = AddSource(p.gameObject);
        }
        foreach (var p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
        {
            if (p.jumpClip == null) p.jumpClip = jump;
            if (p.audioSource == null) p.audioSource = AddSource(p.gameObject);
        }

        // En escenas sin HUD (Menu, Resultados) no hay GameUI: no seguir buscando.
        bool levelScene = SceneManager.GetActiveScene().name.StartsWith("N");
        wired = ui != null || !levelScene;
    }

    static AudioSource AddSource(GameObject go)
    {
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        return s;
    }

    // ---------- API para otros scripts ----------

    // Efecto inmediato (no espera al compás).
    public static void PlaySfx(string name)
    {
        if (I == null) return;
        var clip = Load(name);
        if (clip != null) I.sfx.PlayOneShot(clip);
    }

    // Stinger sincronizado: entra en la siguiente corchea de la música y la apaga mientras suena.
    // Devuelve cuánto dura (segundos) para que quien lo llame pueda esperar si quiere.
    public static float Stinger(string name)
    {
        if (I == null) return 0f;
        var clip = Load(name);
        if (clip == null) return 0f;
        double at = I.NextBeat(0.5);
        var src = I.scheduled[I.nextScheduled];
        I.nextScheduled = (I.nextScheduled + 1) % I.scheduled.Length;
        src.clip = clip;
        src.PlayScheduled(at);
        I.stingerUntilDsp = at + clip.length;
        return (float)(at - AudioSettings.dspTime) + clip.length;
    }

    // Siguiente subdivisión del compás (0.5 = corchea) según el BPM de la pista que suena.
    double NextBeat(double fractionOfBeat)
    {
        double now = AudioSettings.dspTime + 0.02;
        if (!music.isPlaying || music.clip == null) return now;
        double step = 60.0 / bpm * fractionOfBeat;
        double t = now - musicStartDsp;
        if (t < 0) return musicStartDsp;
        return musicStartDsp + System.Math.Ceiling(t / step) * step;
    }

    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static AudioClip Load(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (cache.TryGetValue(name, out var c) && c != null) return c;
        c = Resources.Load<AudioClip>("Audio/" + name);
        if (c == null) Debug.LogWarning("[AUDIO] Falta Assets/Resources/Audio/" + name + ".wav");
        cache[name] = c;
        return c;
    }
}
