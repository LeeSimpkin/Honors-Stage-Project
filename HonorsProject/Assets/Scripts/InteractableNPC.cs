using System.IO;
using UnityEngine;

public class InteractableNPC : MonoBehaviour
{
    public Transform playerPosition;
    public Transform NPCPosition;
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

        if (npcTollm == null)
        {
            npcTollm = gameObject.AddComponent<NPCToLLM>();
        }

        npcTollm.NPCDialogue = npcDialogueAsset;
        npcTollm.playerInput = playerInputAsset;

        // FIX: Subscribe to the event so we update the UI when Ollama is actually done
        npcTollm.OnDialogueReady += HandleDialogueReady;
    }

    void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks / dangling references
        if (npcTollm != null)
            npcTollm.OnDialogueReady -= HandleDialogueReady;
    }

    // Called by NPCToLLM once Ollama has finished and output is clean
    private void HandleDialogueReady(string dialogue)
    {
        dialogueText.SetText(dialogue);
    }

    void Update()
    {
        bool isInRange = Vector3.Distance(playerPosition.position, NPCPosition.position) < 2f;

        if (isInRange)
        {
            // Show "generating..." while waiting, so the player knows something is happening
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
                    dialogueText.SetText("..."); // placeholder while generating
                    npcTollm.StartProcess();
                }
            }

            wasInRange = true;
        }
        else
        {
            interactionPrompt.SetText("");
            dialogueText.SetText("");

            // FIX: Only clear the output file when NOT generating, to avoid wiping live output
            if (wasInRange && !npcTollm.isGeneratingDialogue)
            {
                File.WriteAllText(npcTollm.GetNpcDialoguePath(), string.Empty);
            }

            wasInRange = false;
        }
    }
}