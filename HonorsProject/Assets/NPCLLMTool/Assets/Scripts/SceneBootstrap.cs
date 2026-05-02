using UnityEngine;
using System.Collections;
using Assets.Scripts;

/// <summary>
/// Scene-level initialisation. 
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    [Header("Model Selection")]
    [Tooltip("The LLM model to load at startup. " +
             "The matching .gguf file must exist in Assets/StreamingAssets/LLM/")]
    [SerializeField] private LLMModelType.LLMModelTypes selectedModel = LLMModelType.LLMModelTypes.TinyLlama;

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

        string modelFile = LLMModelType.GetModelFileName(selectedModel);
        Debug.Log("[SceneBootstrap] Starting server with model: " + modelFile);

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