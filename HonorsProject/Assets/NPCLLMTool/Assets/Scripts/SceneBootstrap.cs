using UnityEngine;
using System.Collections;
using Assets.Scripts;

/// <summary>
/// Scene-level initialisation. Replaces Bootstrap.cs.
///
/// Attach this to a GameObject in your scene alongside LLMServerManager.
/// It reads the selected model type, resolves the .gguf filename,
/// and starts the llama-server before any NPC can request dialogue.
///
/// NPCs should check LLMServerManager.Instance.IsServerReady before
/// calling StartProcess(), or subscribe to OnServerReady below.
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    [Header("Model Selection")]
    [Tooltip("The LLM model to load at startup. " +
             "The matching .gguf file must exist in Assets/StreamingAssets/LLM/")]
    [SerializeField] private LLMModelType.LLMModelTypes selectedModel = LLMModelType.LLMModelTypes.TinyLlama;

    /// <summary>
    /// Fired once the server is ready. NPCs or UI can subscribe to this
    /// instead of polling IsServerReady themselves.
    /// </summary>
    public static event System.Action OnServerReady;

    private void Start()
    {
        StartCoroutine(Initialise());
    }

    private IEnumerator Initialise()
    {
        if (LLMServerManager.Instance == null)
        {
            Debug.LogError(
                "[SceneBootstrap] LLMServerManager not found in scene. " +
                "Add a GameObject with LLMServerManager attached.");
            yield break;
        }

        // Resolve the enum to the actual .gguf filename
        string modelFile = LLMModelType.GetModelFileName(selectedModel);
        Debug.Log("[SceneBootstrap] Starting server with model: " + modelFile);

        // Start the server and wait for it to be ready
        yield return StartCoroutine(LLMServerManager.Instance.StartServer(modelFile));

        if (!LLMServerManager.Instance.IsServerReady)
        {
            Debug.LogError("[SceneBootstrap] Server failed to start. NPCs will not be able to generate dialogue.");
            yield break;
        }

        Debug.Log("[SceneBootstrap] Server ready. NPCs may now generate dialogue.");
        OnServerReady?.Invoke();
    }
}