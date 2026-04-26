using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Assets.Scripts;

/// <summary>
/// Connects an NPC to the local llama-server via LLMHttpClient.
///
/// Replaces the original "ollama run" subprocess approach.
/// All existing public members (OnDialogueReady, forbiddenWords, fallbackText,
/// isGeneratingDialogue, StartProcess) are preserved so that InteractableNPC
/// and any other subscribers require no changes.
///
/// Requires:
///   - LLMServerManager running in the scene (started before this is called)
///   - LLMHttpClient attached to the same GameObject
/// </summary>
public class NPCToLLM : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Public fields — kept identical to the original so nothing else breaks
    // -------------------------------------------------------------------------

    private TextFileManager TFM => TextFileManager.Instance;

    public TextAsset playerInput;
    public TextAsset NPCDialogue;    // Still used to determine the output file path
    public bool isGeneratingDialogue = false;

    /// <summary>
    /// Fired when dialogue is ready. InteractableNPC subscribes to this — no changes needed there.
    /// </summary>
    public event Action<string> OnDialogueReady;

    [SerializeField] public List<string> forbiddenWords = new List<string>();
    [SerializeField] public string fallbackText = "I have nothing to say.";

    // -------------------------------------------------------------------------
    // New fields replacing LLMTraining and the model-selection approach
    // -------------------------------------------------------------------------

    [Header("LLM Options")]
    [Tooltip("The model this NPC uses. Must match a filename in StreamingAssets/LLM/ " +
             "and the model loaded by LLMServerManager.")]
    [SerializeField] private LLMModelType.LLMModelTypes selectedLLM;

    [Header("NPC Personality")]
    [Tooltip("Describe who this NPC is. This is sent as the system prompt to the LLM " +
             "and replaces the Ollama modelfile approach.\n\n" +
             "Example: 'You are a grumpy blacksmith named Aldric in a medieval village. " +
             "Keep replies to two sentences.'")]
    [TextArea(4, 10)]
    [SerializeField] private string systemPrompt = "You are a helpful NPC in a fantasy game. Keep your replies brief.";

    // -------------------------------------------------------------------------
    // Private references — populated in Awake
    // -------------------------------------------------------------------------

    private LLMHttpClient _httpClient;

    private void Awake()
    {
        _httpClient = GetComponent<LLMHttpClient>();

        if (_httpClient == null)
        {
            UnityEngine.Debug.LogError(
                "[NPCToLLM] No LLMHttpClient component found on " + gameObject.name +
                ". Please add LLMHttpClient to the same GameObject.");
        }
    }

    // -------------------------------------------------------------------------
    // Public API — identical signature to original StartProcess()
    // -------------------------------------------------------------------------

    /// <summary>
    /// Begins generating dialogue for this NPC.
    /// Called by InteractableNPC exactly as before — no changes required there.
    /// </summary>
    public void StartProcess()
    {
        if (isGeneratingDialogue)
        {
            UnityEngine.Debug.LogWarning("[NPCToLLM] Already generating dialogue. Request ignored.");
            return;
        }

        if (LLMServerManager.Instance == null || !LLMServerManager.Instance.IsServerReady)
        {
            UnityEngine.Debug.LogError(
                "[NPCToLLM] LLMServerManager is not ready. " +
                "Make sure LLMServerManager.StartServer() has completed before calling StartProcess().");
            return;
        }

        if (_httpClient == null)
        {
            UnityEngine.Debug.LogError("[NPCToLLM] Cannot start: LLMHttpClient is missing.");
            return;
        }

        StartCoroutine(RequestDialogue());
    }

    // -------------------------------------------------------------------------
    // Private implementation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads the player's input, sends it to llama-server, filters the reply,
    /// writes it to disk, and fires OnDialogueReady — same flow as the original.
    /// </summary>
    private IEnumerator RequestDialogue()
    {
        isGeneratingDialogue = true;

        string prompt = GetPromptText();
        string serverUrl = LLMServerManager.Instance.ServerUrl;

        UnityEngine.Debug.Log("[NPCToLLM] Requesting dialogue. Prompt: " + prompt);

        // Result holders — populated by the callbacks below
        string replyText = null;
        string errorText = null;
        bool callbackFired = false;

        // Delegate the HTTP call to LLMHttpClient (keeps HTTP logic out of this class)
        yield return StartCoroutine(_httpClient.SendChatRequest(
            serverUrl,
            systemPrompt,
            prompt,
            onSuccess: reply =>
            {
                replyText = reply;
                callbackFired = true;
            },
            onError: error =>
            {
                errorText = error;
                callbackFired = true;
            }
        ));

        // Wait for the callback to fire (should already be done, but safety guard)
        yield return new WaitUntil(() => callbackFired);

        if (!string.IsNullOrEmpty(errorText))
        {
            UnityEngine.Debug.LogError("[NPCToLLM] Failed to get reply: " + errorText);
            isGeneratingDialogue = false;
            yield break;
        }

        // Run through the existing output checker — unchanged from original
        LLMOutputChecker checker = new LLMOutputChecker();
        string finalText = checker.CheckOutput(forbiddenWords, replyText, fallbackText);

        // Write to disk — preserved from original so any file-reading code still works
        string outputPath = GetNpcDialoguePath();
        File.WriteAllText(outputPath, finalText);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        isGeneratingDialogue = false;

        UnityEngine.Debug.Log("[NPCToLLM] Dialogue ready: " + finalText);

        // Fire the event — InteractableNPC receives this exactly as before
        OnDialogueReady?.Invoke(finalText);
    }

    /// <summary>
    /// Reads the player's prompt text. Preserved from original.
    /// </summary>
    private string GetPromptText()
    {
        if (playerInput != null && !string.IsNullOrWhiteSpace(playerInput.text))
        {
            return playerInput.text;
        }
        return "hello";
    }

    /// <summary>
    /// Returns the path to write dialogue output to. Preserved from original.
    /// </summary>
    public string GetNpcDialoguePath()
    {
#if UNITY_EDITOR
        if (NPCDialogue != null)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(NPCDialogue);
            if (!string.IsNullOrEmpty(assetPath))
            {
                return Path.GetFullPath(assetPath);
            }
        }
#endif
        return Path.Combine(Application.persistentDataPath, "LLMOutput_" + gameObject.name + ".txt");
    }
}