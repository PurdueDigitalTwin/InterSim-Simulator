using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

public class ExperimentRunner : MonoBehaviour
{
    [Header("Hook your generator")]
    public TrafficGenerator traffic;  // assign in Inspector

    [Header("Density sweep (vehicles/hour/lane)")]
    public float[] vphplSettings = new float[] { 0f, 200f, 400f, 800f, 1200f, 1600f, 2000f };

    [Header("Timing (seconds)")]
    public float warmupSeconds = 12f;   // enough to let vehicles distribute
    public float recordSeconds = 60f;   // collect stable stats
    public float sampleInterval = 0.0f; // 0 = every frame; else sample every X sec

    [Header("Run options")]
    public bool clearBetweenRuns = true;      // Clear all vehicles before each run
    public bool restartGeneratorEachRun = true;
    public bool disableVSync = true;          // recommended for stable numbers
    public int targetFrameRate = -1;          // -1 = uncapped
    public int randomSeed = 12345;            // fixed seed for reproducibility (optional)
    public bool quitWhenDoneInPlayer = true;  // auto-quit after sweep in Player builds

    [Header("Output")]
    public string filePrefix = "Experiment_VPHPL";
    public bool logPerSampleDebug = false;

    // ---- internals ----
    struct Sample {
        public float dtMs, fps, cpuMs, gpuMs, drawCalls, tris, ramMB, vehCount;
    }

    ProfilerRecorder cpuFrameTime, gpuFrameTime, drawCalls, triangles, gcAllocRate;

    StringBuilder csv;
    string csvPath;

    void Awake()
    {
        if (disableVSync) QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
        if (randomSeed != 0) UnityEngine.Random.InitState(randomSeed);
    }

    void Start()
    {
        if (traffic == null)
        {
            traffic = FindAnyObjectByType<TrafficGenerator>();
            if (traffic == null)
            {
                Debug.LogError("[ExperimentRunner] No TrafficGenerator found/assigned.");
                enabled = false;
                return;
            }
        }

        // Ensure generator is initialized and running
        if (!traffic.isGenerating)
            traffic.StartTrafficGeneration();

        // CSV header
        var ts = DateTime.Now.ToString("yyyyMMdd_HHmm");
        // Store CSV under "SimulationLogs" in the project root
        var logDir = Path.Combine(Application.dataPath, "../SimulationLogs");
        Directory.CreateDirectory(logDir);
        csvPath = Path.Combine(logDir, $"{filePrefix}_{ts}.csv");
        csv = new StringBuilder();
        csv.AppendLine("map,requested_vphpl,global_mult,run_idx,avg_fps,p95_frame_ms,avg_cpu_frame_ms,avg_gpu_frame_ms,avg_draw_calls,avg_triangles,avg_ram_mb,avg_total_vehicles,samples");

        Debug.Log("csvPath: " + csvPath);

        // Recorders (best effort; fields left blank if unsupported)
        TryOpen(ref cpuFrameTime, ProfilerCategory.Internal, "CPU Total Frame Time");
        if (!cpuFrameTime.Valid) TryOpen(ref cpuFrameTime, ProfilerCategory.Internal, "Main Thread");
        TryOpen(ref gpuFrameTime, ProfilerCategory.Render, "GPU Frame Time");
        TryOpen(ref drawCalls,   ProfilerCategory.Render, "Draw Calls Count");
        TryOpen(ref triangles,   ProfilerCategory.Render, "Triangles Count");
        TryOpen(ref gcAllocRate, ProfilerCategory.Memory, "GC Allocated In Frame");

        // Kick off sweep
        StartCoroutine(RunSweep());
    }

    void OnDestroy()
    {
        cpuFrameTime.Dispose();
        gpuFrameTime.Dispose();
        drawCalls.Dispose();
        triangles.Dispose();
        gcAllocRate.Dispose();

        FlushCsv();
    }

    void ReopenGpuRecorder()
    {
        try { gpuFrameTime.Dispose(); } catch {}
        try {
            gpuFrameTime = ProfilerRecorder.StartNew(
                ProfilerCategory.Render, "GPU Frame Time", capacity: 2048);
        } catch {}
    }

    System.Collections.IEnumerator PrimeGpuTiming(float seconds)
    {
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < seconds)
        {
            UnityEngine.FrameTimingManager.CaptureFrameTimings();
            yield return null;
        }
    }

    System.Collections.IEnumerator RunSweep()
    {
        string mapName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        int runIdx = 0;

        foreach (var vphpl in vphplSettings)
        {
            runIdx++;

            // Prepare next run
            if (restartGeneratorEachRun)
            {
                traffic.StopTrafficGeneration();
            }
            if (clearBetweenRuns)
            {
                traffic.ClearAllVehicles();
            }

            // Set density knob
            traffic.baseVehiclesPerHourPerLane = Mathf.Max(0f, vphpl);

            if (restartGeneratorEachRun)
            {
                traffic.StartTrafficGeneration();
            }

            ReopenGpuRecorder();
            yield return PrimeGpuTiming(1.0f); // ~1s prime

            // WARMUP
            yield return CollectForSeconds(warmupSeconds, record:false);

            // RECORD
            List<Sample> samples = new List<Sample>();
            yield return CollectForSeconds(recordSeconds, record:true, samples);

            // Aggregate and log one row
            WriteAggregateRow(mapName, vphpl, traffic.globalDensityMultiplier, runIdx, samples);
            FlushCsv();

            Debug.Log($"[ExperimentRunner] {mapName} VPHPL={vphpl} : {samples.Count} samples recorded.");
        }

        Debug.Log($"[ExperimentRunner] Completed sweep. CSV: {csvPath}");

#if !UNITY_EDITOR
        if (quitWhenDoneInPlayer) Application.Quit();
#endif
    }

    static double TryFrameTimingGPUms()
    {
        // CaptureFrameTimings() should be called earlier this frame
        var ft = new UnityEngine.FrameTiming[1];
        uint n = UnityEngine.FrameTimingManager.GetLatestTimings(1, ft);
        if (n > 0 && ft[0].gpuFrameTime > 0.0) return ft[0].gpuFrameTime; // already ms
        return double.NaN;
    }

    System.Collections.IEnumerator CollectForSeconds(float seconds, bool record, List<Sample> samples)
    {
        float t0 = Time.realtimeSinceStartup;
        float nextSampleAt = 0f;

        while (Time.realtimeSinceStartup - t0 < seconds)
        {
            UnityEngine.FrameTimingManager.CaptureFrameTimings();

            yield return null;

            if (!record) continue;
            if (sampleInterval > 0f && Time.realtimeSinceStartup < nextSampleAt) continue;

            float dt = Time.unscaledDeltaTime;
            float dtMs = dt * 1000f;
            float fps = dt > 1e-6f ? (1f/dt) : 0f;

            float cpuMs = ReadMs(cpuFrameTime);
            float gpuMs = ReadMs(gpuFrameTime);
            if (float.IsNaN(gpuMs) || gpuMs <= 0f)
            {
                double ft = TryFrameTimingGPUms();
                if (!double.IsNaN(ft)) gpuMs = (float)ft;
            }

            float dc    = ReadAvg(drawCalls);
            float tri   = ReadAvg(triangles);
            float ramMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f*1024f);

            // Average the generator's vehicle count over the window
            float vehCount = 0f;
            try {
                var stats = traffic.GetTrafficStats();
                vehCount = stats != null ? stats.totalVehicles : 0f;
            } catch {}

            var s = new Sample {
                dtMs = dtMs, fps = fps,
                cpuMs = cpuMs, gpuMs = gpuMs,
                drawCalls = dc, tris = tri,
                ramMB = ramMB, vehCount = vehCount
            };
            if (samples != null) samples.Add(s);

            if (logPerSampleDebug)
                Debug.Log($"samp fps={fps:F1} dt={dtMs:F2} cpu={cpuMs:F2} gpu={gpuMs:F2} dc={dc:F0} tri={tri:F0} ram={ramMB:F1}MB veh={vehCount:F0}");

            nextSampleAt = Time.realtimeSinceStartup + Mathf.Max(0f, sampleInterval);
        }
    }

    System.Collections.IEnumerator CollectForSeconds(float seconds, bool record=false)
    {
        yield return CollectForSeconds(seconds, record, null);
    }

    static float AvgValid(List<float> vals)
    {
        var good = vals.Where(v => !float.IsNaN(v) && v > 0f).ToArray();
        if (good.Length == 0) return float.NaN;
        return good.Average();
    }

    void WriteAggregateRow(string mapName, float vphpl, float globalMult, int runIdx, List<Sample> s)
    {
        if (s == null || s.Count == 0)
        {
            csv.AppendLine($"{mapName},{vphpl},{globalMult},{runIdx},,,,,,,,0");
            return;
        }

        float Avg(Func<Sample,float> f) => s.Average(f);
        float P(float q, Func<Sample,float> f) {
            var arr = s.Select(f).OrderBy(x=>x).ToArray();
            int idx = Mathf.Clamp(Mathf.RoundToInt((arr.Length-1)*q), 0, arr.Length-1);
            return arr[idx];
        }

        float avgFps = Avg(x=>x.fps);
        float p95Dt  = P(0.95f, x=>x.dtMs);
        float avgCpu = Has(cpuFrameTime) ? Avg(x=>x.cpuMs) : float.NaN;
        float avgGpu = AvgValid(s.Select(x => x.gpuMs).ToList());
        float avgDc  = Has(drawCalls)    ? Avg(x=>x.drawCalls) : float.NaN;
        float avgTri = Has(triangles)    ? Avg(x=>x.tris) : float.NaN;
        float avgRam = Avg(x=>x.ramMB);
        float avgVeh = Avg(x=>x.vehCount);

        csv.AppendLine(string.Join(",",
            mapName,
            vphpl.ToString("F0"),
            globalMult.ToString("F2"),
            runIdx.ToString(),
            avgFps.ToString("F2"),
            p95Dt.ToString("F2"),
            FloatOrBlank(avgCpu),
            FloatOrBlank(avgGpu),
            FloatOrBlank(avgDc),
            FloatOrBlank(avgTri),
            avgRam.ToString("F1"),
            avgVeh.ToString("F1"),
            s.Count.ToString()
        ));
    }

    // ---- ProfilerRecorder helpers ----
    static void TryOpen(ref ProfilerRecorder r, ProfilerCategory cat, string name)
    {
        try {
            if (!r.Valid) r = ProfilerRecorder.StartNew(cat, name, capacity: 300);
        } catch { /* stat not available on this platform/unity */ }
    }
    static bool Has(ProfilerRecorder r) => r.Valid && r.Capacity > 0 && r.Count > 0;
    static float ReadMs(ProfilerRecorder r)
    {
        if (!Has(r)) return float.NaN;
        long ns = r.LastValue; // time stats are usually in ns
        return ns > 0 ? (ns / 1_000_000f) : float.NaN;
    }
    static float ReadAvg(ProfilerRecorder r)
    {
        if (!Has(r)) return float.NaN;
        var buf = new System.Collections.Generic.List<ProfilerRecorderSample>(r.Count);
        r.CopyTo(buf);
        long sum = 0;
        for (int i=0;i<buf.Count;i++) sum += buf[i].Value;
        return (float)sum / Mathf.Max(1, buf.Count);
    }
    static string FloatOrBlank(float v) => float.IsNaN(v) ? "" : v.ToString("F2");

    void FlushCsv()
    {
        try {
            File.AppendAllText(csvPath, csv.ToString());
            csv.Clear();
        } catch (Exception e) {
            Debug.LogWarning($"[ExperimentRunner] CSV write failed: {e.Message}");
        }
    }
}
