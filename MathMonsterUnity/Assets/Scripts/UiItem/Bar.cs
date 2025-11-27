using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Bar : MonoBehaviour {
    [SerializeField] Slider _slider;
    [SerializeField] TMP_Text _text;

    float _max;
    float _cur;

    public void setRatio(float ratio, bool setText = true) {
        _slider.value = ratio;
        if(setText == true) {
            _text.text = $"{ratio}/1.0f";
        }
    }

    public void setText(string text) {
        _text.text = text;
    }

    public void setMaxVal(float max) {
        _max = max;
    }

    public void setCurVal(float cur, bool setText = true) {
        _cur = cur;
        float ratio = _cur / _max;
        _slider.value = ratio;

        if(setText == true) {
            refreshText();
        }
    }

    void refreshText() {
        float displayHp = _cur;
        
        if(_cur <= 1.0f) {
            displayHp = 1.0f;
            if(_cur < 0.0f)
                displayHp = 0.0f;
        }

        _text.text = $"{displayHp:F0}/{_max:F0}";

    }
}
