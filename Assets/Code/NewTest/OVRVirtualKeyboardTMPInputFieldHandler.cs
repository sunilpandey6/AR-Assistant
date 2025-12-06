using System;
using UnityEngine;
using TMPro;
using Meta.XR.Util;
using GLTFast.Schema;

public class OVRVirtualKeyboardTMPInputFieldHandler : OVRVirtualKeyboard.AbstractTextHandler
{
    [Header("Assign your TMP Input Field here")]
    [SerializeField]
    private TMP_InputField inputField;

    /// <summary>
    /// Set/Get the TMP_InputField at runtime
    /// </summary>
    public TMP_InputField InputField {
        get => inputField;
        set {
            if (value == inputField) return;

            if (inputField != null)
                inputField.onValueChanged.RemoveListener(OnValueChanged);

            inputField = value;

            if (inputField != null)
                inputField.onValueChanged.AddListener(OnValueChanged);

            OnTextChanged?.Invoke(Text);
        }
    }

    public override Action<string> OnTextChanged { get; set; }

    public override string Text => inputField ? inputField.text : string.Empty;

    // Submit on Enter only if not multi-line
    public override bool SubmitOnEnter => inputField && inputField.lineType != TMP_InputField.LineType.MultiLineNewline;

    public override bool IsFocused => inputField && inputField.isFocused;

    public override void Submit() {
        if (!inputField) return;

        // TMP_InputField events
        inputField.onSubmit.Invoke(inputField.text);
        inputField.onEndEdit.Invoke(inputField.text);
    }

    public override void AppendText(string s) {
        if (!inputField) return;

        inputField.text += s;
        inputField.caretPosition = inputField.text.Length; // move caret to end
    }

    public override void ApplyBackspace() {
        if (!inputField || string.IsNullOrEmpty(inputField.text)) return;

        inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        inputField.caretPosition = inputField.text.Length;
    }

    public override void MoveTextEnd() {
        if (!inputField) return;

        inputField.caretPosition = inputField.text.Length;
    }

    private void Start() {
        if (inputField != null)
            inputField.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(string newText) {
        OnTextChanged?.Invoke(newText);
    }
}
