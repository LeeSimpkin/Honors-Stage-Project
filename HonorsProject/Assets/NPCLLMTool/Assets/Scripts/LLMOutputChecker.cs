using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class LLMOutputChecker
{
    public LLMOutputChecker() { }

    /// <summary>
    /// Checks outputText against forbiddenWords.
    /// Returns the original text if clean, or fallbackText if a forbidden word is found.
    /// Case-insensitive, whole-word matching (so "hell" won't block "hello").
    /// </summary>
    public string CheckOutput(List<string> forbiddenWords, string outputText, string fallbackText)
    {
        if (string.IsNullOrWhiteSpace(outputText))
        {
            Debug.LogWarning("LLMOutputChecker: Output text is null or empty. Using fallback.");
            return fallbackText;
        }

        if (forbiddenWords == null || forbiddenWords.Count == 0)
        {
            return outputText;
        }

        foreach (string word in forbiddenWords)
        {
            if (string.IsNullOrWhiteSpace(word)) continue;

            // \b = word boundary — prevents "hell" matching "hello"
            // RegexOptions.IgnoreCase — case-insensitive
            string pattern = @"\b" + Regex.Escape(word.Trim()) + @"\b";

            if (Regex.IsMatch(outputText, pattern, RegexOptions.IgnoreCase))
            {
                Debug.LogWarning($"LLMOutputChecker: Forbidden word \"{word}\" detected. Using fallback text.");
                return fallbackText;
            }
        }

        return outputText;
    }
}