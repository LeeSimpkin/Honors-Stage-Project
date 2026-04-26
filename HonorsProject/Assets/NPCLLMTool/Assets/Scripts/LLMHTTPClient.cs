using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

/// <summary>
/// Sends chat completion requests to the local llama-server instance
/// and returns the model's reply.
///
/// llama-server exposes an OpenAI-compatible REST API, so requests follow the
/// standard chat completion format:
///   POST /v1/chat/completions
///   { "messages": [ {"role": "system", "content": "..."}, {"role": "user", "content": "..."} ] }
///
/// This replaces the "ollama run" subprocess call in the original NPCToLLM.cs.
/// Attach this component to the same GameObject as NPCToLLM.
/// </summary>
public class LLMHttpClient : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("Controls randomness of replies. Lower = more predictable. Range: 0.0 - 1.0")]
    [SerializeField] private float temperature = 0.7f;

    [Tooltip("Maximum number of tokens (roughly words) the model will generate in one reply.")]
    [SerializeField] private int maxTokens = 256;

    // -----------------------------------------------------------------
    // JSON serialisation classes
    // These must be [Serializable] for Unity's JsonUtility to handle them.
    // They mirror the shape of the llama-server API request and response.
    // -----------------------------------------------------------------

    [Serializable]
    private class ChatMessage
    {
        public string role;     // "system", "user", or "assistant"
        public string content;
    }

    [Serializable]
    private class ChatRequest
    {
        public ChatMessage[] messages;
        public float temperature;
        public int max_tokens;
        public bool stream;     // false = wait for the full reply before returning
    }

    // The response from llama-server wraps the reply inside choices[0].message.content
    // JsonUtility requires a wrapper class to deserialise an array at the top level.

    [Serializable]
    private class ChatChoice
    {
        public ChatMessage message;
    }

    [Serializable]
    private class ChatResponse
    {
        public ChatChoice[] choices;
    }

    /// <summary>
    /// Sends a system prompt and user message to llama-server.
    /// Results are returned via callbacks to keep this non-blocking.
    ///
    /// serverUrl   - the base URL of the server, e.g. "http://127.0.0.1:8080"
    /// systemPrompt - the NPC's personality/role instructions
    /// userMessage  - the player's input text
    /// onSuccess    - called with the model's reply string on success
    /// onError      - called with an error description string on failure
    /// </summary>
    public IEnumerator SendChatRequest(
        string serverUrl,
        string systemPrompt,
        string userMessage,
        Action<string> onSuccess,
        Action<string> onError)
    {
        string endpoint = serverUrl + "/v1/chat/completions";

        // Build the request body.
        // The system message tells the model who it is.
        // The user message is the player's prompt.
        ChatRequest requestBody = new ChatRequest
        {
            messages = new ChatMessage[]
            {
                new ChatMessage { role = "system", content = systemPrompt },
                new ChatMessage { role = "user",   content = userMessage  }
            },
            temperature = this.temperature,
            max_tokens = this.maxTokens,
            stream = false
        };

        string json = JsonUtility.ToJson(requestBody);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(json);

        UnityEngine.Debug.Log("[LLMHttpClient] Sending request to: " + endpoint);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            // Handle network-level errors (server unreachable, timeout, etc.)
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = "[LLMHttpClient] Request failed: " + request.error;
                UnityEngine.Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
                yield break;
            }

            string responseJson = request.downloadHandler.text;
            UnityEngine.Debug.Log("[LLMHttpClient] Response received. Parsing...");

            // Parse the response JSON into our ChatResponse class
            ChatResponse response = JsonUtility.FromJson<ChatResponse>(responseJson);

            if (response == null || response.choices == null || response.choices.Length == 0)
            {
                string errorMessage = "[LLMHttpClient] Response was empty or could not be parsed.";
                UnityEngine.Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
                yield break;
            }

            // Extract the actual reply text from choices[0].message.content
            string replyText = response.choices[0].message.content.Trim();
            UnityEngine.Debug.Log("[LLMHttpClient] Reply: " + replyText);

            onSuccess?.Invoke(replyText);
        }
    }
}