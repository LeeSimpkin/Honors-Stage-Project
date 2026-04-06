using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class InteractableNPC : MonoBehaviour
{
    public Transform playerPosition;
    public Transform NPCPosition;
    public TMPro.TextMeshProUGUI interactionPrompt;
    public TMPro.TextMeshProUGUI dialogueText;
    private TextFileManager loadInTextFile;
    public TextFileManager TFM;
    public TextAsset NPCDialogue;
    public TextAsset playerInput;
    private string inputText;
    private bool wasInRange = false;
    private bool isGeneratingDialogue = false;

    private class ProcessResult
    {
        public string Output;
        public string Error;
    }

    // Start is called before the first frame update
    void Start()
    {
        interactionPrompt.SetText("");
    }

    // Update is called once per frame
    void Update()
    {
        bool isInRange = Vector3.Distance(playerPosition.position, NPCPosition.position) < 2f;

        if (isInRange)
        {
            interactionPrompt.SetText("Press E to interact");
            if (Input.GetKeyDown(KeyCode.E) && !isGeneratingDialogue)
            {
                Debug.Log("Interacted with NPC");
                inputText = playerInput != null ? playerInput.text : "hello";
                StartCoroutine(RunOllamaNonBlocking());
            }

            wasInRange = true;
        }
        else
        {
            interactionPrompt.SetText("");
            dialogueText.SetText("");

            // Clear the file when player leaves range
            if (wasInRange)
            {
                string filePath = Path.Combine(Application.dataPath, "Assets\\NPCLLMTool\\NPC\\NPCDialogue.txt");
                File.WriteAllText(filePath, string.Empty);
                wasInRange = false;
            }
        }
    }

    private IEnumerator RunOllamaNonBlocking()
    {
        isGeneratingDialogue = true;
        dialogueText.SetText("...");

        string outputPath = Path.Combine(Application.dataPath, "Assets\\NPCLLMTool\\NPC\\NPCDialogue.txt");
        string prompt = string.IsNullOrWhiteSpace(inputText) ? "hello" : inputText;
        string escapedPrompt = prompt.Replace("\"", "\\\"");

        Task<ProcessResult> task = Task.Run(() =>
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = $" run Phi3 {escapedPrompt} > NPCDialogue.txt",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            };

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
            //    string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return new ProcessResult
                {
                //    Output = output,
                    Error = error
                };
            }
        });

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError(task.Exception);
            dialogueText.SetText("Error generating dialogue.");
            isGeneratingDialogue = false;
            yield break;
        }

        ProcessResult result = task.Result;
        //File.WriteAllText(outputPath, result.Output);
        dialogueText.SetText(outputPath);

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogWarning(result.Error);
        }

        isGeneratingDialogue = false;
    }
}
