using System.IO;
using UnityEngine;

public class InteractableNPC : MonoBehaviour
{
    public Transform playerPosition;
    public TMPro.TextMeshProUGUI interactionPrompt;
    public TMPro.TextMeshProUGUI dialogueText;

    private bool wasInRange = false;

    private NPCToLLM npcTollm;
    [SerializeField] private TextAsset npcDialogueAsset;
    [SerializeField] private TextAsset playerInputAsset;

    void Start()
    {
        interactionPrompt.SetText("");
        dialogueText.SetText("");

        npcTollm = GetComponent<NPCToLLM>();
        if (npcTollm == null)
        {
            Debug.LogError("NPCToLLM is required on this NPC but is missing.");
            enabled = false;
            return;
        }

        npcTollm.NPCDialogue = npcDialogueAsset;
        npcTollm.playerInput = playerInputAsset;
        npcTollm.OnDialogueReady += HandleDialogueReady;
    }

    void OnDestroy()
    {
        if (npcTollm != null)
            npcTollm.OnDialogueReady -= HandleDialogueReady;
    }

    private void HandleDialogueReady(string dialogue)
    {
        dialogueText.SetText(dialogue);
    }

    void Update()
    {
        bool isInRange = Vector3.Distance(playerPosition.position, transform.position) < 2f;

        if (isInRange && !PlayerInputSaver.MovementDisabled)
        {
            if (npcTollm.isGeneratingDialogue)
            {
                interactionPrompt.SetText("Generating response...");
            }
            else
            {
                interactionPrompt.SetText("Press E to interact");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("Interacted with NPC");
                    dialogueText.SetText("...");
                    npcTollm.StartProcess();
                }
            }

            wasInRange = true;
        }
        else
        {
            interactionPrompt.SetText("");
            dialogueText.SetText("");

            if (wasInRange && !npcTollm.isGeneratingDialogue)
            {
                File.WriteAllText(npcTollm.GetNpcDialoguePath(), string.Empty);
            }

            wasInRange = false;
        }
    }
}