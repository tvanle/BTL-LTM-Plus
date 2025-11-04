using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        this.button = this.GetComponent<Button>();
        this.button.onClick.AddListener(this.OnButtonClick);
    }

    private void OnDestroy()
    {
        if (this.button != null)
        {
            this.button.onClick.RemoveListener(this.OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }
}
