using UnityEngine;

public class InstructionPoint : MonoBehaviour
{
    [TextArea]
    public string instructionText;

    void OnTriggerEnter(Collider other)
    {
        if (!other.transform.root.CompareTag("Player")) return;

        InstructionManager manager = FindAnyObjectByType<InstructionManager>();
        if (manager != null)
        {
            manager.UpdateInstruction(instructionText);
        }
    }
} 