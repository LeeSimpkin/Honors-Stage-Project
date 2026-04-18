using Assets.Scripts;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;

public class LLMTraining : MonoBehaviour
{
    [Header("LLM Options")]
    [Tooltip("List of LLMs available for this NPC.")]
    [SerializeField]
    private List<LLMModelType.LLMModelTypes> availableLLMs = new List<LLMModelType.LLMModelTypes>();

    [Tooltip("Selected LLM index from the available list.")]
    [SerializeField]
    private int selectedLLMIndex;

    [Header("String Inputs Section")]
    [Tooltip("Add your LLM prompts here. Make sure each element has a maximum of one prompt.")]
    [SerializeField]
    private List<string> stringInputs = new List<string>();

    [Header("NPC Name")]
    [Tooltip("Enter the name of the NPC here.")]
    [SerializeField]
    private string npcName;

    
    private LLMModelType.LLMModelTypes SelectedLLM
    {
        get
        {
            if (availableLLMs == null || availableLLMs.Count == 0)
            {
                return LLMModelType.LLMModelTypes.Phi3;
            }

            selectedLLMIndex = Mathf.Clamp(selectedLLMIndex, 0, availableLLMs.Count - 1);
            return availableLLMs[selectedLLMIndex];
        }
    }

    private void Start()
    {
        var description = SelectedLLM.Description();

        foreach (string s in stringInputs)
        {
            UnityEngine.Debug.Log("String Input: " + s);
            Process.Start("CMD.exe", $"/C ollama run {description} \"{s}\"");
        }

        Process.Start("CMD.exe", $"/C ollama save {npcName}");
    }
}


