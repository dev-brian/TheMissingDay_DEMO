using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Panel de rendimiento (3er parcial: profiling). NO hay que ponerlo en ninguna escena: se crea solo.
// F3 = mostrar / ocultar. Usa ProfilerRecorder (la misma fuente de datos que la ventana Profiler de Unity).
// Cada 10 s escribe en consola una línea [PERF] y la guarda en perf.csv (junto a metrics.csv).
// Algunos contadores (draw calls, GC) solo existen en el Editor o en un Development Build: si no, sale "n/a".
public class PerfHud : MonoBehaviour
{
    static readonly ProfilerMarker WireMarker = new ProfilerMarker("TheMissingDay.PerfHud");

    ProfilerRecorder mainThread, usedMemory, gcAlloc, drawCalls, batches;
    bool visible;
    float accTime, minFps = float.MaxValue, maxFrameMs, logTimer, fps, frameMs;
    int accFrames, bodies;
    float bodiesTimer;
    GUIStyle style;
    string text = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindFirstObjectByType<PerfHud>() != null) return;
        var go = new GameObject("PerfHud");
        DontDestroyOnLoad(go);
        go.AddComponent<PerfHud>();
    }

    void OnEnable()
    {
        mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        usedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
        gcAlloc    = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        drawCalls  = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        batches    = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
    }

    void OnDisable()
    {
        mainThread.Dispose(); usedMemory.Dispose(); gcAlloc.Dispose(); drawCalls.Dispose(); batches.Dispose();
    }

    void Update()
    {
        if (TogglePressed()) visible = !visible;

        float dt = Time.unscaledDeltaTime;
        accTime += dt; accFrames++;
        if (dt > 0f) minFps = Mathf.Min(minFps, 1f / dt);
        maxFrameMs = Mathf.Max(maxFrameMs, dt * 1000f);

        bodiesTimer -= dt;
        if (bodiesTimer <= 0f)
        {
            using (WireMarker.Auto())
                bodies = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None).Length;
            bodiesTimer = 1f;
        }

        if (accTime >= 0.5f)
        {
            fps = accFrames / accTime;
            frameMs = accTime * 1000f / accFrames;
            accTime = 0f; accFrames = 0;
            if (visible) text = BuildText();
        }

        logTimer += dt;
        if (logTimer >= 10f)
        {
            Log();
            logTimer = 0f; minFps = float.MaxValue; maxFrameMs = 0f;
        }
    }

    string BuildText()
    {
        var sb = new StringBuilder();
        sb.Append("PROFILING  (F3)\n");
        sb.Append($"FPS: {fps:0}   frame: {frameMs:0.0} ms\n");
        sb.Append($"Main thread: {Ms(mainThread)}\n");
        sb.Append($"Memoria usada: {Mb(usedMemory)}\n");
        sb.Append($"GC/frame: {Kb(gcAlloc)}\n");
        sb.Append($"Draw calls: {Val(drawCalls)}   Batches: {Val(batches)}\n");
        sb.Append($"Rigidbody2D: {bodies}\n");
        sb.Append($"Escena: {SceneManager.GetActiveScene().name}");
        return sb.ToString();
    }

    void Log()
    {
        string scene = SceneManager.GetActiveScene().name;
        float min = minFps == float.MaxValue ? 0f : minFps;
        Debug.Log($"[PERF] Escena: {scene} | FPS: {fps:0} (min {min:0}) | Frame max: {maxFrameMs:0.0} ms | Main: {Ms(mainThread)} | Mem: {Mb(usedMemory)} | Draw calls: {Val(drawCalls)} | Rigidbody2D: {bodies}");
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "perf.csv");
            bool isNew = !File.Exists(path);
            using (var w = new StreamWriter(path, true))
            {
                if (isNew) w.WriteLine("fecha,escena,fps,fps_min,frame_max_ms,memoria_mb,rigidbodies");
                double mem = usedMemory.Valid ? usedMemory.LastValue / (1024.0 * 1024.0) : -1;
                w.WriteLine($"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss},{scene},{fps:0},{min:0},{maxFrameMs:0.0},{mem:0},{bodies}");
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[PERF] No se pudo escribir perf.csv: " + e.Message); }
    }

    static string Ms(ProfilerRecorder r)
    {
        if (!r.Valid || r.Count == 0) return "n/a";
        double sum = 0;
        for (int i = 0; i < r.Count; i++) sum += r.GetSample(i).Value;
        return (sum / r.Count / 1e6).ToString("0.0") + " ms";
    }
    static string Mb(ProfilerRecorder r) => r.Valid ? (r.LastValue / (1024f * 1024f)).ToString("0") + " MB" : "n/a";
    static string Kb(ProfilerRecorder r) => r.Valid ? (r.LastValue / 1024f).ToString("0.0") + " KB" : "n/a";
    static string Val(ProfilerRecorder r) => r.Valid ? r.LastValue.ToString() : "n/a";

    void OnGUI()
    {
        if (!visible) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 16, richText = false };
            style.normal.textColor = new Color32(255, 244, 214, 255);
            style.padding = new RectOffset(10, 10, 8, 8);
        }
        if (string.IsNullOrEmpty(text)) text = BuildText();
        GUI.Box(new Rect(10, 10, 330, 190), text, style);
    }

    static bool TogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F3);
#endif
    }
}
