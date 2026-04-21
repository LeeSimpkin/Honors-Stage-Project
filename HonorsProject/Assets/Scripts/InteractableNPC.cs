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
    [SerializeField] private NPCToLLM npcTollm;
    [SerializeField] private TextAsset npcDialogueAsset;
    [SerializeField] private TextAsset playerInputAsset;

    private class ProcessResult
    {
        public string Output;
        public string Error;
    }

    void Start()
    {
        interactionPrompt.SetText("");

        if (npcTollm == null)
        {
            npcTollm = gameObject.AddComponent<NPCToLLM>();
        }

        npcTollm.NPCDialogue = npcDialogueAsset;
        npcTollm.playerInput = playerInputAsset;
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
                dialogueText.SetText(npcDialogueAsset.text);
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





