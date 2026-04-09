using System.Collections.Generic;
using UnityEngine;

public class LLMOutputChecker
{

    public TextAsset LLMOutputChecker(List<string> forbiddenWords, TextAsset outputFile, string fallbackText)
    {
        if(outputFile == null)
        {
            Debug.LogWarning("Output file is null. Using fallback text.");
            return new TextAsset(fallbackText);
        }
        else
        {
            string outputText = outputFile.text;
            foreach (string word in forbiddenWords)
            {
                if (outputText.Contains(word))
                {
                    Debug.LogWarning($"Output contains forbidden word: {word}. Using fallback text.");
                    return new TextAsset(fallbackText);
                }
            }
            return outputFile;
        }
    }

}
