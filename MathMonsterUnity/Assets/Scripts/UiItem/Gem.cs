using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Gem : MonoBehaviour
{
    [SerializeField] TMP_Text _text;
    private Image _gemImage;
    Color _originalColor = Color.white;

    public bool isEnable = false;
    bool _isSelect = false;

    public int value { get; set; }
    public int boardIdx { get; set; }

    private void Awake() {
        _gemImage = GetComponent<Image>();
        _originalColor =_gemImage.color;
    }

    public void init(int number = -1) {
        isEnable = true;
        if(number == -1)
            genRandomNumber();
        else {
            setNumber(number);
        }
    }

    public bool doSelect() {
        if (_isSelect == true)
            return false;

        _isSelect = true;
        _gemImage.color = Color.red;
        return true;
    }

    public void revertColor() {
        _isSelect = false;
        _gemImage.color = _originalColor;
    }

    public void genRandomNumber() {
        setNumber(Random.Range(1, 10));
    }

    public void setNumber(int number) {
        value = number;
        _text.text = value.ToString();
    }
}
