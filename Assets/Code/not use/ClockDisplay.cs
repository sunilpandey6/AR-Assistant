using System;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class ClockDisplay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TextMeshProUGUI timeText;

    // Update is called once per frame
    void Update()
    {
        timeText.text = DateTime.Now.ToString("HH:mm:ss/yyyy");
    }
}
