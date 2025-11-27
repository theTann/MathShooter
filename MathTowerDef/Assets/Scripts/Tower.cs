using UnityEngine;
using TMPro;

public class Tower : MonoBehaviour {
    //static readonly int RadiusID = Shader.PropertyToID("_Radius");
    //static readonly int ColorID = Shader.PropertyToID("_Color");

    //// attack range related.
    //[SerializeField] Mesh quadMesh;
    //[SerializeField] Material ringMat;
    //[SerializeField] Color color = new(0.2f, 0.7f, 1f, 0.5f);
    [SerializeField] private float _radius = 1f;
    //MaterialPropertyBlock mpb;
    //Matrix4x4 matrix;
    //// end of attack range related.

    [SerializeField] TMP_Text _numberText;
    [SerializeField] Transform _attackRadius;

    //void LateUpdate() {
    //    // 쿼드를 지면에 살짝 띄워 Z-fight 방지
    //    var pos = transform.position + Vector3.up * 0.02f;
    //    // 쿼드 스케일은 반경*2 정도(셰이더가 실제 링을 잘라줌)
    //    float s = radius * 2.2f;
    //    matrix = Matrix4x4.TRS(pos, Quaternion.Euler(90, 0, 0), new Vector3(s, s, 1));
    //    Graphics.DrawMesh(quadMesh, matrix, ringMat, gameObject.layer, null, 0, mpb);
    //}

    //void OnEnable() {
    //    mpb = new MaterialPropertyBlock();
    //    mpb.SetColor(ColorID, color);
    //    mpb.SetFloat(RadiusID, radius);
    //}

    public void setRadius(float r) {
        _radius = r;
        _attackRadius.localScale = new Vector3(_radius, _radius, _radius);
    }

    public void setNumber(int number) {
        _numberText.text = number.ToString();
    }

    private void Update() {
        setRadius(_radius);
    }
}
