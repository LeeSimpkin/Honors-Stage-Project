using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableNPC : MonoBehaviour
{
    public Transform playerPosition;
    public Transform NPCPosition;
    public TMPro.TextMeshProUGUI interactionPrompt;
    public TMPro.TextMeshProUGUI dialogueText;
    private TextFileManager loadInTextFile;
    public TextFileManager TFM;
    public TextAsset textFilePath;
    public TextAsset playerInput;
    private string inputText;
    // Start is called before the first frame update
    void Start()
    {
        interactionPrompt.SetText("");
        // Or assign via the Inspector: make `public LoadInTextFile loadInTextFile;` and set it in the editor
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(playerPosition.position, NPCPosition.position) < 2f)
        {
            interactionPrompt.SetText("Press E to interact");
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("Interacted with NPC");
                inputText = playerInput.text;
                System.Diagnostics.Process.Start("CMD.exe", $"ollama run llama3.2 {inputText}");
                string text = TFM.LoadText(textFilePath);
                dialogueText.SetText(text);
            }
        }
        else
        {
            interactionPrompt.SetText("");
        }
    }

}
