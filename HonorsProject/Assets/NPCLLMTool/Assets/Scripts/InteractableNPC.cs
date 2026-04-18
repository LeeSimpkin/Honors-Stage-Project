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
    private bool wasInRange = false;
    private NPCToLLM npcTollm;

    private class ProcessResult
    {
        public string Output;
        public string Error;
    }

    void Start()
    {
        interactionPrompt.SetText("");
        npcTollm = gameObject.AddComponent<NPCToLLM>();
    }

    void Update()
    {
        bool isInRange = Vector3.Distance(playerPosition.position, NPCPosition.position) < 2f;

        if (isInRange)
        {
            interactionPrompt.SetText("Press E to interact");
            if (Input.GetKeyDown(KeyCode.E) && !npcTollm.isGeneratingDialogue)
            {
                Debug.Log("Interacted with NPC");
                npcTollm.StartProcess();
            }

            wasInRange = true;
        }
        else
        {
            interactionPrompt.SetText("");
            dialogueText.SetText("");

            if (wasInRange)
            {
                File.WriteAllText(npcTollm.GetNpcDialoguePath(), string.Empty);
                wasInRange = false; 
            }

        }
    }
}



