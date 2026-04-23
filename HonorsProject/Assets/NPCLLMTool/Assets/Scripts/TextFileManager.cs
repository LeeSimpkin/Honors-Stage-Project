using UnityEngine;
using System;

public class TextFileManager : MonoBehaviour
{
    public static TextFileManager Instance { get; private set; }

    public TMPro.TextMeshProUGUI dialogueText;
    public TextAsset textFileReference;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public string LoadText(TextAsset filePath)
    {
        if (filePath != null)
        {
            return filePath.text;
        }

        Debug.LogError("Failed to load text file at path: " + filePath);
        return "Failed to find text file";
    }
}
