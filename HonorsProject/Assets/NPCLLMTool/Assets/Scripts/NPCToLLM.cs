using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Assets.Scripts;
using Unity.VisualScripting;
using UnityEngine;

public class NPCToLLM : MonoBehaviour
{
    public TextFileManager TFM;
    public TextAsset NPCDialogue;
    public TextAsset playerInput;
    private string inputText;
    public bool isGeneratingDialogue = false;

    [SerializeField] public List<string> forbiddenWords = new List<String>();
    [SerializeField] public string fallbackText = "I have nothing to say.";

    [Header("LLM Options")]
    [Tooltip("Choose the LLM used by this NPC.")]
    [SerializeField]
    private LLMModelType.LLMModelTypes selectedLLM;

    public void StartProcess()
    {
        inputText = GetPromptText();
        StartCoroutine(RunOllamaNonBlocking());
        //RunOllamaBlocking();
    }

    private string GetPromptText()
    {
        if (!string.IsNullOrWhiteSpace(inputText))
        {
            return inputText;
        }

        if (playerInput != null && !string.IsNullOrWhiteSpace(playerInput.text))
        {
            return playerInput.text;
        }

        return "hello";
    }

    private void RunOllamaBlocking()
    {
        string prompt = GetPromptText();
        string selectedModelName = selectedLLM.Description().ToString();
        System.Diagnostics.Process.Start("cmd.exe", $"/C ollama run {selectedModelName} {prompt} > {GetNpcDialoguePath()}");
    }

    private IEnumerator RunOllamaNonBlocking()
    {
        Debug.Log("Called RunOllamaNonBlocking");
        isGeneratingDialogue = true;

        string outputPath = GetNpcDialoguePath();
        string prompt = GetPromptText();
        string selectedModelName = selectedLLM.Description().ToString();
        string escapedPrompt = prompt.Replace("\"", "\\\"");

        Task<ProcessResult> task = Task.Run(() =>
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C ollama run \"" + selectedModelName + "\" \"" + escapedPrompt + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                if (process == null)
                {
                    return new ProcessResult
                    {
                        Output = string.Empty,
                        Error = "Failed to start Ollama process."
                    };
                }

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                process.WaitForExit();
                Task.WaitAll(outputTask, errorTask);

                return new ProcessResult
                {
                    Output = outputTask.Result,
                    Error = errorTask.Result,
                    ExitCode = process.ExitCode
                };
            }
        });

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError(task.Exception);
            isGeneratingDialogue = false;
            yield break;
        }

        Debug.Log("Ollama process completed.");
        ProcessResult result = task.Result;
        File.WriteAllText(outputPath, result.Output);

        if (result.ExitCode != 0)
        {
            Debug.LogError("Ollama exited with code " + result.ExitCode);
        }

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogWarning(result.Error);
        }

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        isGeneratingDialogue = false;
    }

    public string GetNpcDialoguePath()
    {
#if UNITY_EDITOR
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(NPCDialogue);
        if (!string.IsNullOrEmpty(assetPath))
        {
            return Path.GetFullPath(assetPath);
        }
#endif
        return Path.Combine(Application.persistentDataPath, "OllamaOutputs.txt");
    }

    private class ProcessResult
    {
        public string Output;
        public string Error;
        public int ExitCode;
    }
}