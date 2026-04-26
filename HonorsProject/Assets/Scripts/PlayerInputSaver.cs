using System.IO;
using UnityEngine;
using TMPro;

public class PlayerInputSaver : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public TMP_Text statusText;

    [Header("File Settings")]
    public TextAsset fileName;
    public bool appendToFile = true;

    public static bool MovementDisabled = false;

    private string FilePath => Path.Combine(Application.dataPath, "NPCLLMTool", "Assets", "TextFiles", "PlayerInput.txt");

    void Start()
    {
        // Subscribe to focus/unfocus events on the input field
        inputField.onSelect.AddListener(OnInputSelected);
        inputField.onDeselect.AddListener(OnInputDeselected);
    }

    void OnDestroy()
    {
        // Always clean up listeners
        inputField.onSelect.RemoveListener(OnInputSelected);
        inputField.onDeselect.RemoveListener(OnInputDeselected);
    }

    private void OnInputSelected(string value)
    {
        MovementDisabled = true;
    }

    private void OnInputDeselected(string value)
    {
        MovementDisabled = false;
    }

    public void SaveInput()
    {
        string playerText = inputField.text.Trim();

        if (string.IsNullOrEmpty(playerText))
        {
            ShowStatus("Nothing to save!", Color.yellow);
            return;
        }

        try
        {
            if (appendToFile)
                File.AppendAllText(FilePath, playerText + System.Environment.NewLine);
            else
                File.WriteAllText(FilePath, playerText);

            ShowStatus($"Saved to: {FilePath}", Color.green);
            inputField.text = "";
        }
        catch (System.Exception e)
        {
            ShowStatus($"Error saving: {e.Message}", Color.red);
            Debug.LogError(e);
        }
    }

    private void ShowStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log(message);
    }
}