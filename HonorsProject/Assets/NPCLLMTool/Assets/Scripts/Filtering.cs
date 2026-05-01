using System.Collections.Generic;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class Filtering
{
    public Filtering() { }

    /// <summary>
    /// Checks outputText against forbiddenWords and an optional forbidden words file.
    /// Returns the original text if clean, or fallbackText if a forbidden word is found.
    /// </summary>
    public string CheckOutput(List<string> forbiddenWords, string outputText, string fallbackText, string forbiddenWordsFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(outputText))
        {
            Debug.LogWarning("LLMOutputChecker: Output text is null or empty. Using fallback.");
            return fallbackText;
        }

        List<string> combinedWords = new List<string>();

        if (forbiddenWords != null && forbiddenWords.Count > 0)
        {
            combinedWords.AddRange(forbiddenWords);
        }

        if (!string.IsNullOrWhiteSpace(forbiddenWordsFilePath))
        {
            if (File.Exists(forbiddenWordsFilePath))
            {
                combinedWords.AddRange(ReadForbiddenWordsFromFile(forbiddenWordsFilePath));
            }
            else
            {
                Debug.LogWarning("LLMOutputChecker: Forbidden words file not found at " + forbiddenWordsFilePath);
            }
        }

        if (combinedWords.Count == 0)
        {
            return outputText;
        }

        foreach (string word in combinedWords)
        {
            if (string.IsNullOrWhiteSpace(word)) continue;

            string pattern = @"\b" + Regex.Escape(word.Trim()) + @"\b";

            if (Regex.IsMatch(outputText, pattern, RegexOptions.IgnoreCase))
            {
                Debug.LogWarning($"LLMOutputChecker: Forbidden word \"{word}\" detected. Using fallback text.");
                return fallbackText;
            }
        }

        return outputText;
    }

    private static IEnumerable<string> ReadForbiddenWordsFromFile(string filePath)
    {
        string content = File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(content))
        {
            yield break;
        }

        string[] rawWords = content.Split(new[] { ',', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawWord in rawWords)
        {
            string trimmed = rawWord.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                yield return trimmed;
            }
        }
    }
}