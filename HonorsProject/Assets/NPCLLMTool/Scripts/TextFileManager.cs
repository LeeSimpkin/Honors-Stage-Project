using UnityEngine;
using System.IO;
using System;

public class TextFileManager : MonoBehaviour
{
    public TMPro.TextMeshProUGUI dialogueText;
    public TextAsset textFileReference; 



    public TextFileManager()
    {
        // Default constructor
    }
    void Start()
    {

    }
    void Update()
    {

    }

    public string LoadText(TextAsset filePath)
    {
        if (filePath != null)
        {
            return filePath.text;
        }
        else
        {
            Debug.LogError("Failed to load text file at path: " + filePath);
            return "Failed to find text file";
        }
    }
}
