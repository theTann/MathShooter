using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class UIBezierFly : MonoBehaviour {
    public RectTransform targetUI; // 도착 지점
    private float _duration = 1.0f;   // 이동 시간
    public Vector2 offsetRange = new Vector2(100f, 100f); // 랜덤 곡률 오프셋 범위

    private RectTransform _rectTransform;
    private Vector3 _startPos, _controlPos, _endPos;
    Action<UIBezierFly> _onComplete;

    public void init(Vector3 startPos, RectTransform targetUI, float duration, Action<UIBezierFly> onComplete) {
        _rectTransform = GetComponent<RectTransform>();

        _startPos = startPos;
        _endPos = targetUI.position;
        _duration = duration;
        _onComplete = onComplete;

        // 곡률을 위한 제어점 랜덤 설정
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-offsetRange.x, offsetRange.x),
            UnityEngine.Random.Range(-offsetRange.y, offsetRange.y),
            0f
        );
        _controlPos = (_startPos + _endPos) * 0.5f + randomOffset;
        StartCoroutine(AnimateToTarget());
    }

    IEnumerator AnimateToTarget() {
        float time = 0f;
        while (time < _duration) {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _duration);

            // Quadratic Bezier 계산
            Vector3 pos = Mathf.Pow(1 - t, 2) * _startPos +
                          2 * (1 - t) * t * _controlPos +
                          Mathf.Pow(t, 2) * _endPos;

            _rectTransform.position = pos;
            yield return null;
        }

        // 최종 도착 위치 보정
        _rectTransform.position = _endPos;
        _rectTransform.position = _startPos;
        _onComplete?.Invoke(this);
    }
}
