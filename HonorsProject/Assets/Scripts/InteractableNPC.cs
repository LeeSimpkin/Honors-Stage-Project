using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class InteractableNPC : MonoBehaviour
{
    public Transform playerPosition;
    public Transform NPCPosition;
    public TMPro.TextMeshProUGUI interactionPrompt;
    public TMPro.TextMeshProUGUI dialogueText;
    public TextFileManager TFM;
    public TextAsset NPCDialogue;
    public TextAsset playerInput;
    private string inputText;
    private bool wasInRange = false;
    private bool isGeneratingDialogue = false;

    [SerializeField] public List<string> forbiddenWords = new List<String>();
    [SerializeField] public string fallbackText = "I have nothing to say.";

    private class ProcessResult
    {
        public string Output;
        public string Error;
    }

    void Start()
    {
        interactionPrompt.SetText("");
    }

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

            if (wasInRange)
            {
                File.WriteAllText(GetNpcDialoguePath(), string.Empty);
                wasInRange = false;
            }
        }
    }

    private IEnumerator RunOllamaNonBlocking()
    {
        isGeneratingDialogue = true;
        dialogueText.SetText("...");

        string outputPath = GetNpcDialoguePath();
        string prompt = string.IsNullOrWhiteSpace(inputText) ? "hello" : inputText;
        string escapedPrompt = prompt.Replace("\"", "\\\"");

        Task<ProcessResult> task = Task.Run(() =>
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "run Phi3 \"" + escapedPrompt + "\"",
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
            dialogueText.SetText("Error generating dialogue.");
            isGeneratingDialogue = false;
            yield break;
        }

        ProcessResult result = task.Result;
        File.WriteAllText(outputPath, result.Output);
        dialogueText.SetText(result.Output);

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogWarning(result.Error);
        }

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        isGeneratingDialogue = false;
    }

    private string GetNpcDialoguePath()
    {
#if UNITY_EDITOR
        if (NPCDialogue != null)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(NPCDialogue);
            if (!string.IsNullOrEmpty(assetPath))
            {
                return Path.GetFullPath(assetPath);
            }
        }
#endif
        return Path.Combine(Application.persistentDataPath, "NPCDialogue.txt");
    }
}
