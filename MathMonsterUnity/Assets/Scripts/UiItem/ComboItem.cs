using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComboItem : MonoBehaviour
{
    [SerializeField] private Image _bg;
    [SerializeField] private Image _check;
    [SerializeField] private TMP_Text _text;

    private void Awake() {
        _check.gameObject.SetActive(false);
        _text.gameObject.SetActive(false);
    }

    public void setPraiseLevel(Sprite praiseImg) {
        _check.sprite = praiseImg;
    }

    public void setCheck(bool check) {
        _check.gameObject.SetActive(check);
    }

    public void setText(string text) {
        if(string.IsNullOrEmpty(text) == true) {
            _text.gameObject.SetActive(false);
            return;
        }
        _text.gameObject.SetActive(true);
        _text.text = text;
    }
}
