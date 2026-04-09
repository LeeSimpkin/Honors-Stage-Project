using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

    public NPCToLLM()
    {
        TFM = new TextFileManager();
        inputText = playerInput != null ? playerInput.text : "hello";
    }

    public void StartProcess()
    {
        StartCoroutine(RunOllamaNonBlocking());
    }

    private IEnumerator RunOllamaNonBlocking()
    {
        Debug.Log("Called RunOllamaNonBlocking");
        isGeneratingDialogue = true;

        string outputPath = GetNpcDialoguePath();
        string prompt = string.IsNullOrWhiteSpace(inputText) ? "hello" : inputText;
        //string escapedPrompt = prompt.Replace("\"", "\\\"");

        Task<ProcessResult> task = Task.Run(() =>
        {
            Debug.Log("Sending prompt to Ollama: " + prompt);
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "run Phi3 \"" + prompt + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            };

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return new ProcessResult
                {
                    Output = output,
                    Error = error
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
    }
}