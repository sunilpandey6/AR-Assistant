using UnityEngine;

public class AppListShow : MonoBehaviour
{
    [SerializeField] private GameObject panel;
   public void  ShowAppPanel () {
        panel.SetActive(!panel.activeSelf);
    }
    public void CloseIt() {
        panel.SetActive(false);
    }
}
