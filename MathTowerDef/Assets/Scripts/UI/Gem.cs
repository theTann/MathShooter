using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Gem : MonoBehaviour {
    public static string[] numbers = {"1", "2", "3", "4", "5", "6", "7", "8", "9"};
    [SerializeField] TMP_Text _text;
    [SerializeField] Image _bg;
    Color _originalColor;
    int _currentNumber;

    int _x, _y;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        _originalColor = _bg.color;
    }

    public void initGem(int x, int y) {
        _x = x;
        _y = y;
        randomNumber();
    }

    public (int x, int y) getIndex() {
        return (_x, _y);
    }

    public int getNumber() {
        return _currentNumber;
    }

    public void revertColor() {
        _bg.color = _originalColor;
    }

    public void setSelectColor() {
        _bg.color = Color.red;
    }

    public void randomNumber() {
        _currentNumber = Random.Range(1, 10);
        _text.text = numbers[_currentNumber - 1];
    }

    public void setTempColor() {
        _bg.color = Color.orange;
    }
}
