using UnityEngine;
using System.Collections.Generic; // Required for using List<>
using System;

public class LLMTraining : MonoBehaviour
{
    public enum LLMModelType
    {
        Phi3,
        LLama3_2,
        tinyllama
    }

    [Header("String Inputs Section")]
    [Tooltip("Add your LLM prompts here. Make sure each element has a maximum of one prompt.")]
    [SerializeField]
    private List<string> stringInputs = new List<string>();

    [Header("LLM choice")]
    [Tooltip("Select the LLM you want to use to generate NPC output.")]
    [SerializeField]
    public LLMModelType selectedLLM;

    [Header("NPC Name")]
    [Tooltip("Enter the name of the NPC here.")]
    [SerializeField]
    private string npcName;

    private void Start()
    {
        
        foreach (string s in stringInputs)
        {
            Debug.Log("String Input: " + s);
            System.Diagnostics.Process.Start("CMD.exe", $"C/ ollama run Phi3 {s}" /*+ selectedLLM.ToString()*/);
        }
        System.Diagnostics.Process.Start("CMD.exe", $"C/ /save {npcName}");
    }
}


