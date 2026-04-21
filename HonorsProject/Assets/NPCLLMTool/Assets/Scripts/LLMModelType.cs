using System.ComponentModel;
using UnityEngine;

namespace Assets.Scripts
{
    public class LLMModelType
    {
        public enum LLMModelTypes
        {
            [InspectorName("Phi3")]
            [Description("Phi3")]
            Phi3,

            [InspectorName("Llama3.2")]
            [Description("Llama3.2")]
            Llama3_2,

            [InspectorName("TinyLlama")]
            [Description("TinyLlama")]
            TinyLlama
        }
    }
}
