using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Connects an NPC to the local llama-server via LLMHttpClient.
///
/// </summary>
public class NPCToLLM : MonoBehaviour
{

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

    private string ForbiddenWordsFilePath => Path.Combine(Application.dataPath, "NPCLLMTool", "Assets", "TextFiles", "ForbiddenWords.txt");


    [Header("NPC Personality")]
    [Tooltip("Describe who this NPC is. This is sent as the system prompt to the LLM")]
    [TextArea(4, 10)]
    [SerializeField] private string systemPrompt = "You are a helpful NPC in a fantasy game. Keep your replies brief.";


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

    /// <summary>
    /// Begins generating dialogue for this NPC.
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

        // Wait for the callback
        yield return new WaitUntil(() => callbackFired);

        if (!string.IsNullOrEmpty(errorText))
        {
            UnityEngine.Debug.LogError("[NPCToLLM] Failed to get reply: " + errorText);
            isGeneratingDialogue = false;
            yield break;
        }

        // Run through the output checker
        Filtering checker = new Filtering();
        string finalText = checker.CheckOutput(forbiddenWords, replyText, fallbackText, ForbiddenWordsFilePath);

        // Write to disk — preserved from original so any file-reading code still works
        string outputPath = GetNpcDialoguePath();
        File.WriteAllText(outputPath, finalText);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        isGeneratingDialogue = false;

        UnityEngine.Debug.Log("[NPCToLLM] Dialogue ready: " + finalText);

        OnDialogueReady?.Invoke(finalText);
    }

    /// <summary>
    /// Reads the player's prompt text. 
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
    /// Returns the path to write dialogue output to.
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