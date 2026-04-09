using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public class LLMModelType
    {
        public enum LLMModelTypes
        {
            [Description("Phi3")]
            Phi3,
            [Description("Llama3_2")]
            Llama3_2,
            [Description("TinyLlama")]
            TinyLlama
        }
        public override string ToString()
        {
            

        }
    }
}
