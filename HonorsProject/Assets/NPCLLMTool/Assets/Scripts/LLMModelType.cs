using System.ComponentModel;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Defines the LLM models available in the project and maps each one
    /// to the corresponding .gguf filename in StreamingAssets/LLM/.
    /// </summary>
    public class LLMModelType
    {
        public enum LLMModelTypes
        {
            [InspectorName("qwen2.5 (0.5B)")]
            [Description("qwen2.5")]
            Qwen2_5,

            [InspectorName("Llama 3.2 (3B)")]
            [Description("Llama3.2")]
            Llama3_2,

            [InspectorName("TinyLlama (1.1B)")]
            [Description("TinyLlama")]
            TinyLlama
        }

        public static string GetModelFileName(LLMModelTypes modelType)
        {
            switch (modelType)
            {
                case LLMModelTypes.Qwen2_5:
                    return "qwen2.5-0.5b-instruct-fp16.gguf";

                case LLMModelTypes.Llama3_2:
                    return "Llama-3.2-3B-Instruct-Q4_K_M.gguf";

                case LLMModelTypes.TinyLlama:
                    return "tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf";

                default:
                    UnityEngine.Debug.LogWarning(
                        "[LLMModelType] Unknown model type: " + modelType +
                        ". Falling back to TinyLlama.");
                    return "tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf";
            }
        }
    }
}