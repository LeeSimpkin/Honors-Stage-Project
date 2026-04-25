using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts;
using Unity.VisualScripting;

public class LLMTraining : MonoBehaviour
{
    [Header("LLM Options")]
    [Tooltip("List of LLMs available for this NPC.")]
    [SerializeField]
    private List<LLMModelType.LLMModelTypes> availableLLMs = new List<LLMModelType.LLMModelTypes>();

    [Tooltip("Selected LLM index from the available list.")]
    [SerializeField]
    private int selectedLLMIndex;

    [Header("String Inputs Section")]
    [Tooltip("Add your LLM prompts here. Make sure each element has a maximum of one prompt.")]
    [SerializeField]
    private List<string> stringInputs = new List<string>();

    [Header("NPC Name")]
    [Tooltip("Enter the name of the NPC here.")]
    [SerializeField]
    private string npcName;

    private string runtimeModelName;
    public bool IsTrainingComplete { get; private set; }

    public string RuntimeModelName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(runtimeModelName))
            {
                return runtimeModelName;
            }

            return SelectedLLM.Description().ToString();
        }
    }

    private LLMModelType.LLMModelTypes SelectedLLM
    {
        get
        {
            if (availableLLMs == null || availableLLMs.Count == 0)
            {
                return LLMModelType.LLMModelTypes.Phi3;
            }

            selectedLLMIndex = Mathf.Clamp(selectedLLMIndex, 0, availableLLMs.Count - 1);
            return availableLLMs[selectedLLMIndex];
        }
    }

    private void Start()
    {
        StartCoroutine(CreateRuntimeModel());
    }

    private IEnumerator CreateRuntimeModel()
    {
        string baseModelName = SelectedLLM.Description().ToString();
        string targetModelName = BuildTargetModelName(baseModelName);
        runtimeModelName = targetModelName;

        string modelfilePath = Path.Combine(Application.persistentDataPath, targetModelName + ".modelfile");
        string modelfileContent = BuildModelfileContent(baseModelName);

        File.WriteAllText(modelfilePath, modelfileContent);

        UnityEngine.Debug.Log("Creating runtime Ollama model: " + targetModelName);

        Task<ProcessResult> task = Task.Run(() =>
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C ollama create \"" + targetModelName + "\" -f \"" + modelfilePath + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    return new ProcessResult
                    {
                        Output = string.Empty,
                        Error = "Failed to start Ollama create process.",
                        ExitCode = -1
                    };
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return new ProcessResult
                {
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode
                };
            }
        });

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            UnityEngine.Debug.LogError("Model training failed: " + task.Exception);
            IsTrainingComplete = false;
            runtimeModelName = baseModelName;
            yield break;
        }

        ProcessResult result = task.Result;

        if (result.ExitCode != 0)
        {
            UnityEngine.Debug.LogError("Ollama create failed: " + result.Error);
            IsTrainingComplete = false;
            runtimeModelName = baseModelName;
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            UnityEngine.Debug.LogWarning("Ollama create stderr: " + result.Error);
        }

        IsTrainingComplete = true;
        UnityEngine.Debug.Log("Runtime Ollama model ready: " + runtimeModelName);
    }

    private string BuildModelfileContent(string baseModelName)
    {
        StringBuilder contextBuilder = new StringBuilder();

        foreach (string input in stringInputs.Where(i => !string.IsNullOrWhiteSpace(i)))
        {
            contextBuilder.AppendLine(input.Trim());
        }

        string context = contextBuilder.ToString().Trim();
        string safeContext = context.Replace("\"\"\"", "\"\" ");

        StringBuilder modelfileBuilder = new StringBuilder();
        modelfileBuilder.AppendLine("FROM " + baseModelName);

        if (!string.IsNullOrWhiteSpace(safeContext))
        {
            modelfileBuilder.AppendLine("SYSTEM \"\"\"");
            modelfileBuilder.AppendLine(safeContext);
            modelfileBuilder.AppendLine("\"\"\"");
        }

        return modelfileBuilder.ToString();
    }

    private string BuildTargetModelName(string baseModelName)
    {
        string rawName = string.IsNullOrWhiteSpace(npcName) ? (baseModelName + "-npc") : npcName;
        string lower = rawName.ToLowerInvariant();

        StringBuilder sb = new StringBuilder();
        foreach (char c in lower)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
            {
                sb.Append(c);
            }
            else if (char.IsWhiteSpace(c))
            {
                sb.Append('-');
            }
        }

        if (sb.Length == 0)
        {
            sb.Append(baseModelName.ToLowerInvariant().Replace('.', '-')).Append("-npc");
        }

        return sb.ToString();
    }

    private class ProcessResult
    {
        public string Output;
        public string Error;
        public int ExitCode;
    }
}


