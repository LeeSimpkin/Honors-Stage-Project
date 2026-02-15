using UnityEngine;
using System.IO;
using System;
using System;

public class TextFileManager : MonoBehaviour
{
    // public field kept for backward compatibility if you want to assign a Stream externally
    [SteralizeField] public Stream textFile;
    private string text;

    // filename relative to the Assets folder. Change if you move the file.
    public string fileName = "OllamaOutputs";

    // Start is called before the first frame update
    void Start()
    {
        // Keep Start simple — LoadText is lazy and will attempt to read the file on demand.
    }
    void Update()
    {
        // For demonstration, we can call LoadText here or from any other method when needed.

        string loadedText = LoadText();
        Debug.Log($"Loaded Text: {loadedText}");

    }

    public string LoadText()
    {
        // If already loaded return cached text
        if (!string.IsNullOrEmpty(text))
            return text;

        // If a Stream was explicitly assigned, prefer reading from it
        if (textFile != null)
        {
            try
            {
                using (var reader = new StreamReader(textFile))
                {
                    text = reader.ReadToEnd();
                }
                Debug.Log("Loaded text from assigned Stream.");
                return text;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read from assigned Stream: {e.Message}");
                return text = "";
            }
        }

        // Try reading the file from the project Assets folder (works in Editor and standalone when file is present)
        string filePath = Path.Combine(Application.dataPath, fileName);
        if (File.Exists(filePath))
        {
            try
            {
                text = File.ReadAllText(filePath);
                Debug.Log($"Loaded text from {filePath}");
                return text;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read file at {filePath}: {e.Message}");
                return text = "";
            }
        }

        Debug.Log("No text file assigned or file not found.");
        return text = "";
    }
}
