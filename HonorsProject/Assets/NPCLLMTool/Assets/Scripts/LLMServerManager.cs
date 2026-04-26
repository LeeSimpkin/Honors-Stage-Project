using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// Manages the lifecycle of the llama-server.exe process.
///
/// Setup:
///   Place llama-server.exe and your .gguf model files inside:
///   Assets/StreamingAssets/LLM/
///
///   llama-server.exe can be downloaded from:
///   https://github.com/ggerganov/llama.cpp/releases
///   (look for the windows zip matching your hardware, e.g. llama-b...-bin-win-cuda-cu12.2.0-x64.zip)
///
/// This replaces Bootstrap.cs and removes the Ollama dependency entirely.
/// One instance of this should exist in your scene, shared across all NPCs.
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
    [SerializeField] private float startupTimeoutSeconds = 60f;

    // Other scripts can read this to know when it's safe to send requests
    public bool IsServerReady { get; private set; } = false;

    // The base URL other scripts use to reach the server
    public string ServerUrl => $"http://127.0.0.1:{port}";

    private Process _serverProcess;

    private void Awake()
    {
        // Singleton — one server manager shared across all scenes and NPCs
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Starts the llama-server process using the given .gguf model filename.
    /// Call this from your scene's initialisation code, e.g.:
    ///   StartCoroutine(LLMServerManager.Instance.StartServer("phi3.gguf"));
    /// </summary>
    public IEnumerator StartServer(string modelFileName)
    {
        string llmFolder = Path.Combine(Application.dataPath, "NPCLLMTool", "Build", "llama");
        string serverExePath = Path.Combine(llmFolder, "llama-server.exe");
        string modelPath = Path.Combine(llmFolder, modelFileName);

        if (!File.Exists(serverExePath))
        {
            UnityEngine.Debug.LogError(
                "[LLMServerManager] llama-server.exe not found at: " + serverExePath +
                "\nDownload it from https://github.com/ggerganov/llama.cpp/releases " +
                "and place it in Assets/StreamingAssets/LLM/");
            yield break;
        }

        if (!File.Exists(modelPath))
        {
            UnityEngine.Debug.LogError(
                "[LLMServerManager] Model file not found at: " + modelPath +
                "\nPlace your .gguf model in Assets/StreamingAssets/LLM/");
            yield break;
        }

        // Build the command-line arguments for llama-server
        // -m   : path to the model file
        // --port : port to listen on
        // --host : restrict to localhost only (no external access)
        // -ngl : number of layers to offload to GPU
        // -c   : context size
        string arguments = string.Format(
            "-m \"{0}\" --port {1} --host 127.0.0.1 -ngl {2} -c {3}",
            modelPath, port, gpuLayers, contextSize
        );

        UnityEngine.Debug.Log("[LLMServerManager] Starting llama-server: " + serverExePath);
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
            UnityEngine.Debug.LogError("[LLMServerManager] Failed to start llama-server process.");
            yield break;
        }

        UnityEngine.Debug.Log("[LLMServerManager] Process started. Waiting for server to be ready...");

        // Poll the /health endpoint until the server responds or we time out.
        // llama-server returns { "status": "ok" } once the model is loaded.
        float elapsed = 0f;
        bool ready = false;

        while (elapsed < startupTimeoutSeconds && !ready)
        {
            Task<bool> healthPing = PingHealthEndpoint();
            yield return new WaitUntil(() => healthPing.IsCompleted);

            if (healthPing.Result)
            {
                ready = true;
            }
            else
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }
        }

        if (!ready)
        {
            UnityEngine.Debug.LogError(
                "[LLMServerManager] llama-server did not become ready within " +
                startupTimeoutSeconds + " seconds. Check the model file is valid.");
            yield break;
        }

        IsServerReady = true;
        UnityEngine.Debug.Log("[LLMServerManager] Server is ready at " + ServerUrl);
    }

    /// <summary>
    /// Attempts a GET request to /health. Returns true if the server responds with HTTP 200.
    /// Uses System.Net.Http so it runs off the Unity main thread without blocking it.
    /// </summary>
    private async Task<bool> PingHealthEndpoint()
    {
        try
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.Timeout = System.TimeSpan.FromSeconds(2);
                var response = await httpClient.GetAsync(ServerUrl + "/health");
                return response.IsSuccessStatusCode;
            }
        }
        catch
        {
            // Server not up yet — this is expected during startup
            return false;
        }
    }

    // Kill the server cleanly when the application exits or this object is destroyed
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