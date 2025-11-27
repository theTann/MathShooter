using UnityEngine;

public class HpBar : MonoBehaviour {
    [SerializeField] private Transform _bar;

    public void setRatio(float ratio) {
        ratio = Mathf.Clamp01(ratio);
        Vector3 scale = _bar.localScale;
        scale.x = ratio;
        _bar.localScale = scale;
        Vector3 localPos = _bar.localPosition;
        localPos.x = (ratio - 1f) * 0.5f;
        _bar.localPosition = localPos;
    }
}
