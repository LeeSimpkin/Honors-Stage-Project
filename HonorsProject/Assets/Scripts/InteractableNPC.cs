using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    
    // Start is called before the first frame update
    void Start()
    {
        interactionPrompt.SetText("");
        // Or assign via the Inspector: make `public LoadInTextFile loadInTextFile;` and set it in the editor
    }

    // Update is called once per frame
    void Update()
    {
        dialogueText.SetText(NPCDialogue.text);
        bool isInRange = Vector3.Distance(playerPosition.position, NPCPosition.position) < 2f;
        
        if (isInRange)
        {
            interactionPrompt.SetText("Press E to interact");
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("Interacted with NPC");
                inputText = playerInput.text;
                System.Diagnostics.Process.Start("CMD.exe", "/C ollama run PriateLLM hello > \"Assets\\NPCLLMTool\\NPC\\NPCDialogue.txt\"");
                
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
                string filePath = Path.Combine(Application.dataPath, "NPCLLMTool\\NPC\\NPCDialogue.txt");
                File.WriteAllText(filePath, string.Empty);
                wasInRange = false;
            }
        }
    }

}
