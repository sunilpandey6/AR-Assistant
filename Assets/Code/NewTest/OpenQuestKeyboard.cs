using TMPro;
using UnityEngine;

public class OpenQuestKeyboard : MonoBehaviour
{
    public TMP_InputField[] inputFields; // Array of input fields

    public void OnSelect(int index) {
        if (index < 0 || index >= inputFields.Length)
            return;

        // Activate the selected input field
        inputFields[index].ActivateInputField();

        // Open the TouchScreenKeyboard
        TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
    }
}
