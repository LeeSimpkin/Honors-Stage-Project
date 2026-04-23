using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Assets.Scripts;
using UnityEngine;

public class NPCToLLM : MonoBehaviour
{
    private TextFileManager TFM => TextFileManager.Instance;
    public TextAsset NPCDialogue;
    public TextAsset playerInput;
    public bool isGeneratingDialogue = false;

    // Fired when dialogue is ready — InteractableNPC subscribes to this
    public event Action<string> OnDialogueReady;

    [SerializeField] public List<string> forbiddenWords = new List<string>();
    [SerializeField] public string fallbackText = "I have nothing to say.";

    [Header("LLM Options")]
    [Tooltip("Choose the LLM used by this NPC.")]
    [SerializeField]
    private LLMModelType.LLMModelTypes selectedLLM;


    public void StartProcess()
    {
        StartCoroutine(RunOllamaNonBlocking());
    }

    // FIX: Read playerInput fresh each time, no stale inputText field
    private string GetPromptText()
    {
        if (playerInput != null && !string.IsNullOrWhiteSpace(playerInput.text))
        {
            return playerInput.text;
        }
        return "hello";
    }

    // FIX: Strip ANSI/VT100 escape sequences Ollama emits when stdout is redirected
    private static string StripAnsiCodes(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        // Matches ESC[ ... m/K/J/H/A/B/C/D sequences and lone backspace-style codes
        return Regex.Replace(input, @"\x1B\[[0-9;]*[A-Za-z]|\x1B\[[0-9]*[A-Za-z]|\x08", string.Empty);
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

            // Tell Ollama it is NOT a TTY so it won't emit colour/cursor codes
            startInfo.EnvironmentVariables["NO_COLOR"] = "1";
            startInfo.EnvironmentVariables["TERM"] = "dumb";

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

        if (result.ExitCode != 0)
            Debug.LogError("Ollama exited with code " + result.ExitCode);

        if (!string.IsNullOrEmpty(result.Error))
            Debug.LogWarning("Ollama stderr: " + result.Error);

        // FIX: Strip ANSI codes before doing anything with the output
        string cleanOutput = StripAnsiCodes(result.Output).Trim();

        // Run the output checker before writing/displaying
        LLMOutputChecker checker = new LLMOutputChecker();
        string finalText = checker.CheckOutput(forbiddenWords, cleanOutput, fallbackText);

        // Write cleaned output to disk
        File.WriteAllText(outputPath, finalText);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        isGeneratingDialogue = false;

        // FIX: Notify InteractableNPC that the text is ready, passing it directly
        OnDialogueReady?.Invoke(finalText);
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