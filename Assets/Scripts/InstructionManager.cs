using UnityEngine;
using TMPro;

public class InstructionManager : MonoBehaviour
{
    [Header("Assign the UI Text component in the Inspector")]
    public TMP_Text instructionTextUI;

    /// <summary>
    /// Updates the instruction text on the UI.
    /// </summary>
    /// <param name="newInstruction">The new instruction to display.</param>
    public void UpdateInstruction(string newInstruction)
    {
        if (instructionTextUI != null)
        {
            instructionTextUI.text = newInstruction;
        }
    }
} 