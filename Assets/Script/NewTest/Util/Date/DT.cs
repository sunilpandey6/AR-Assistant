using TMPro;
using UnityEngine;

public class DT : MonoBehaviour
{
    public TextMeshProUGUI dateTimeText;

    void Update() {
        System.DateTime now = System.DateTime.Now;

        string time = now.ToString("HH:mm");
        string date = now.ToString("ddd, MMM dd");

        dateTimeText.text = time + "\n" + date;
    }
}
