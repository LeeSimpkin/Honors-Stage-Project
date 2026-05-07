using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

/// <summary>
/// Sends chat completion requests to the local llama-server instance
/// and returns the model's reply.
/// </summary>
public class LLMHttpClient : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("Controls randomness of replies. Lower = more predictable. Range: 0.0 - 1.0")]
    [SerializeField] private float temperature = 0.7f;

    [Tooltip("Maximum number of tokens (roughly words) the model will generate in one reply.")]
    [SerializeField] private int maxTokens = 30;


    [Serializable]
    private class ChatMessage
    {
        public string role;    
        public string content;
    }

    [Serializable]
    private class ChatRequest
    {
        public ChatMessage[] messages;
        public float temperature;
        public int max_tokens;
        public bool stream;
    }


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

  
    public IEnumerator SendChatRequest(
        string serverUrl,
        string systemPrompt,
        string userMessage,
        Action<string> onSuccess,
        Action<string> onError)
    {
        string endpoint = serverUrl + "/v1/chat/completions";

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

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = "[LLMHttpClient] Request failed: " + request.error;
                UnityEngine.Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
                yield break;
            }

            string responseJson = request.downloadHandler.text;
            UnityEngine.Debug.Log("[LLMHttpClient] Response received. Parsing...");

            ChatResponse response = JsonUtility.FromJson<ChatResponse>(responseJson);

            if (response == null || response.choices == null || response.choices.Length == 0)
            {
                string errorMessage = "[LLMHttpClient] Response was empty or could not be parsed.";
                UnityEngine.Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
                yield break;
            }

            string replyText = response.choices[0].message.content.Trim();
            UnityEngine.Debug.Log("[LLMHttpClient] Reply: " + replyText);

            onSuccess?.Invoke(replyText);
        }
    }
}