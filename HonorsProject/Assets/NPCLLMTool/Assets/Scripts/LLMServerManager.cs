using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using System.Text;

/// <summary>
/// Manages the lifecycle of the llama-server.exe process.
/// </summary>
public class LLMServerManager : MonoBehaviour
{
    public static LLMServerManager Instance { get; private set; }

    [Header("Server Configuration")]
    [Tooltip("Port the llama-server will listen on. Change if 8080 is already in use.")]
    [SerializeField] private int port = 8080;

    [Tooltip("Number of model layers to offload to GPU. 0 = CPU only. 999 = offload as many as possible.")]
    [SerializeField] private int gpuLayers = 0;

    [Tooltip("Context window size in tokens. Higher = more memory. 2048 is a safe default.")]
    [SerializeField] private int contextSize = 2048;

    [Tooltip("How many seconds to wait for the server to become ready before giving up.")]
    [SerializeField] private float startupTimeoutSeconds = 120f;

    public bool IsServerReady { get; private set; } = false;
    public string ServerUrl => $"http://127.0.0.1:{port}";

    private Process _serverProcess;

    private readonly StringBuilder _pendingLogs = new StringBuilder();
    private readonly object _logLock = new object();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator StartServer(string modelFileName)
    {
        string llmFolder = Path.Combine(Application.dataPath, "NPCLLMTool", "Build", "llama");
        string serverExePath = Path.Combine(llmFolder, "llama-server.exe");
        string modelPath = Path.Combine(llmFolder, modelFileName);

        UnityEngine.Debug.Log("[LLMServerManager] LLM folder:  " + llmFolder);
        UnityEngine.Debug.Log("[LLMServerManager] Server path: " + serverExePath);
        UnityEngine.Debug.Log("[LLMServerManager] Model path:  " + modelPath);

        if (!File.Exists(serverExePath))
        {
            UnityEngine.Debug.LogError(
                "[LLMServerManager] llama-server.exe not found.\n" +
                "Expected at: " + serverExePath);
            yield break;
        }

        if (!File.Exists(modelPath))
        {
            UnityEngine.Debug.LogError(
                "[LLMServerManager] Model file not found.\n" +
                "Expected at: " + modelPath + "\n" +
                "Check the filename in LLMModelType.GetModelFileName() matches exactly.");
            yield break;
        }

        // Log model file size — a 0-byte or very small file means a bad download
        long modelBytes = new FileInfo(modelPath).Length;
        UnityEngine.Debug.Log(
            "[LLMServerManager] Model file size: " + (modelBytes / 1024 / 1024) + " MB — " +
            (modelBytes > 100_000 ? "looks valid." : "WARNING: file may be corrupt or incomplete."));

        string arguments = string.Format(
            "-m \"{0}\" --port {1} --host 127.0.0.1 -ngl {2} -c {3}",
            modelPath, port, gpuLayers, contextSize);

        UnityEngine.Debug.Log("[LLMServerManager] Arguments: " + arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = serverExePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _serverProcess = Process.Start(startInfo);

        if (_serverProcess == null)
        {
            UnityEngine.Debug.LogError("[LLMServerManager] Process.Start returned null — could not launch llama-server.exe.");
            yield break;
        }

        _serverProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                lock (_logLock) { _pendingLogs.AppendLine("[llama stdout] " + e.Data); }
        };

        _serverProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                lock (_logLock) { _pendingLogs.AppendLine("[llama stderr] " + e.Data); }
        };

        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        UnityEngine.Debug.Log("[LLMServerManager] Process launched (PID " + _serverProcess.Id + "). Polling /health...");

        float elapsed = 0f;
        bool ready = false;

        while (elapsed < startupTimeoutSeconds && !ready)
        {
            FlushPendingLogs();

            if (_serverProcess.HasExited)
            {
                FlushPendingLogs();
                UnityEngine.Debug.LogError(
                    "[LLMServerManager] llama-server.exe crashed on startup (exit code " +
                    _serverProcess.ExitCode + ").\n" +
                    "Read the [llama stderr] lines above for the exact error.\n\n" +
                    "Common causes:\n" +
                    "  1. Wrong build flavour — e.g. a CUDA build on a machine without an Nvidia GPU.\n" +
                    "     Download the AVX2 build instead for CPU-only.\n" +
                    "  2. Missing Visual C++ runtime — install vc_redist.x64.exe from Microsoft.\n" +
                    "  3. Corrupted .gguf file — re-download the model.\n" +
                    "  4. Port " + port + " already in use — change the Port field in the Inspector.");
                yield break;
            }

            Task<bool> ping = PingHealthEndpoint();
            yield return new WaitUntil(() => ping.IsCompleted);

            if (ping.Result)
            {
                ready = true;
            }
            else
            {
                yield return new WaitForSeconds(2f);
                elapsed += 2f;

                if (Mathf.RoundToInt(elapsed) % 10 == 0)
                    UnityEngine.Debug.Log("[LLMServerManager] Still loading... " + elapsed + "s / " + startupTimeoutSeconds + "s");
            }
        }

        FlushPendingLogs();

        if (!ready)
        {
            UnityEngine.Debug.LogError(
                "[LLMServerManager] Timed out after " + startupTimeoutSeconds + "s.\n" +
                "The server process is running but never responded on port " + port + ".\n" +
                "Try increasing Startup Timeout Seconds in the Inspector if the model is large.");
            yield break;
        }

        IsServerReady = true;
        UnityEngine.Debug.Log("[LLMServerManager] Server is ready at " + ServerUrl);
    }

    private void Update()
    {
        if (_serverProcess != null && !IsServerReady)
            FlushPendingLogs();
    }

    /// <summary>
    /// Moves any log lines buffered on background threads into Unity's console.
    /// </summary>
    private void FlushPendingLogs()
    {
        string toLog;
        lock (_logLock)
        {
            if (_pendingLogs.Length == 0) return;
            toLog = _pendingLogs.ToString();
            _pendingLogs.Clear();
        }
        UnityEngine.Debug.Log(toLog);
    }

    private async Task<bool> PingHealthEndpoint()
    {
        try
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.Timeout = System.TimeSpan.FromSeconds(3);
                var response = await httpClient.GetAsync(ServerUrl + "/health");
                return response.IsSuccessStatusCode;
            }
        }
        catch
        {
            return false;
        }
    }

    private void OnApplicationQuit() => ShutdownServer();
    private void OnDestroy() => ShutdownServer();

    private void ShutdownServer()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            UnityEngine.Debug.Log("[LLMServerManager] Shutting down llama-server.");
            _serverProcess.Kill();
            _serverProcess.Dispose();
            _serverProcess = null;
        }

        IsServerReady = false;
    }
}