using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
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
    private CancellationTokenSource _cts;
    /// <summary>
    /// Fired when dialogue is ready. InteractableNPC subscribes to this — no changes needed there.
    /// </summary>
    public event Action<string> OnDialogueReady;

    [SerializeField] public List<string> forbiddenWords = new List<string>();
    [SerializeField] public string fallbackText = "I have nothing to say.";

    private string ForbiddenWordsFilePath => Path.Combine(Application.dataPath, "NPCLLMTool", "Assets", "TextFiles", "ForbiddenWords.txt");


    [Header("Generation Settings")]
    [SerializeField] private float temperature = 0.7f;
    [SerializeField] private int maxTokens = 30;

    [Header("NPC Personality")]
    [Tooltip("Describe who this NPC is. This is sent as the system prompt to the LLM")]
    [TextArea(4, 10)]
    [SerializeField] private string systemPrompt = "You are a helpful NPC in a fantasy game. Keep your replies brief.";

    readonly System.Diagnostics.Stopwatch _responseTimer = new System.Diagnostics.Stopwatch();

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
        _responseTimer.Restart();
        _responseTimer.Start();

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

        _cts = new CancellationTokenSource();

        Task<string> llmTask = SendRequestAsync(serverUrl, systemPrompt, prompt, _cts.Token);

        yield return new WaitUntil(() => llmTask.IsCompleted);

        if (llmTask.IsFaulted)
        {
            UnityEngine.Debug.LogError("[NPCToLLM] LLM request failed: " +
                llmTask.Exception?.GetBaseException().Message);
            isGeneratingDialogue = false;
            yield break;
        }

        if (llmTask.IsCanceled)
        {
            UnityEngine.Debug.Log("[NPCToLLM] LLM request was cancelled.");
            isGeneratingDialogue = false;
            yield break;
        }

        string replyText = llmTask.Result;

        Filtering checker = new Filtering();
        string finalText = checker.CheckOutput(forbiddenWords, replyText, fallbackText, ForbiddenWordsFilePath);

        string outputPath = GetNpcDialoguePath();
        File.WriteAllText(outputPath, finalText);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        isGeneratingDialogue = false;
        _responseTimer.Stop();
        UnityEngine.Debug.Log("[NPCToLLM] Response time: " + _responseTimer.Elapsed.TotalSeconds.ToString("F2") + " seconds");

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
    private async Task<string> SendRequestAsync(
      string serverUrl,
      string sysPrompt,
      string userMessage,
      CancellationToken ct)
    {
        string endpoint = serverUrl + "/v1/chat/completions";
        string json = BuildRequestJson(sysPrompt, userMessage);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(json);

        using (var httpClient = new HttpClient())
        {
            httpClient.Timeout = TimeSpan.FromSeconds(60);

            using (var content = new ByteArrayContent(bodyBytes))
            {
                content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                HttpResponseMessage response =
                    await httpClient.PostAsync(endpoint, content, ct);

                response.EnsureSuccessStatusCode();

                string responseJson = await response.Content.ReadAsStringAsync();

                ChatResponse parsed = JsonUtility.FromJson<ChatResponse>(responseJson);

                if (parsed == null || parsed.choices == null || parsed.choices.Length == 0)
                    throw new Exception("Response was empty or could not be parsed.");

                return parsed.choices[0].message.content.Trim();
            }
        }
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

    private string BuildRequestJson(string sysPrompt, string userMessage)
    {
        var request = new ChatRequest
        {
            messages = new[]
            {
                new ChatMessage { role = "system", content = sysPrompt },
                new ChatMessage { role = "user",   content = userMessage }
            },
            temperature = this.temperature,
            max_tokens = this.maxTokens,
            stream = false
        };
        return JsonUtility.ToJson(request);
    }

    [Serializable] private class ChatMessage { public string role; public string content; }
    [Serializable] private class ChatRequest { public ChatMessage[] messages; public float temperature; public int max_tokens; public bool stream; }
    [Serializable] private class ChatChoice { public ChatMessage message; }
    [Serializable] private class ChatResponse { public ChatChoice[] choices; }
}