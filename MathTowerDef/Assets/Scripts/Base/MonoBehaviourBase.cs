using System.Reflection;
using UnityEngine;

// 사용하지 않는 이벤트 메소드 정의로 속도 저하 주의해야함.

public class MonoBehaviourBase : MonoBehaviour {
    // ========== 초기화 및 생명주기 ==========
    protected virtual void Awake() { }
    protected virtual void OnEnable() { }
    protected virtual void Start() { }

    // 성능 민감한 부분은 일단 virtual을 사용하지 말아본다.
    //protected virtual void Update() { }
    //protected virtual void LateUpdate() { }
    //protected virtual void FixedUpdate() { }

    protected virtual void OnDisable() { }
    protected virtual void OnDestroy() { }

    // ========== 충돌 및 물리 ==========
    protected virtual void OnCollisionEnter(Collision collision) { }

    // 성능 민감한 부분은 일단 virtual을 사용하지 말아본다.
    // protected virtual void OnCollisionStay(Collision collision) { }

    protected virtual void OnCollisionExit(Collision collision) { }

    protected virtual void OnTriggerEnter(Collider other) { }

    // 성능 민감한 부분은 일단 virtual을 사용하지 말아본다.
    // protected virtual void OnTriggerStay(Collider other) { }

    protected virtual void OnTriggerExit(Collider other) { }

    // 2D 충돌
    protected virtual void OnCollisionEnter2D(Collision2D collision) { }

    // 성능 민감한 부분은 일단 virtual을 사용하지 말아본다.
    // protected virtual void OnCollisionStay2D(Collision2D collision) { }

    protected virtual void OnCollisionExit2D(Collision2D collision) { }

    protected virtual void OnTriggerEnter2D(Collider2D other) { }

    // 성능 민감한 부분은 일단 virtual을 사용하지 말아본다.
    // protected virtual void OnTriggerStay2D(Collider2D other) { }

    protected virtual void OnTriggerExit2D(Collider2D other) { }

    // ========== 렌더링 및 카메라 ==========
    //protected virtual void OnPreRender() { }
    //protected virtual void OnPostRender() { }
    //protected virtual void OnRenderObject() { }
    //protected virtual void OnWillRenderObject() { }

    //protected virtual void OnBecameVisible() { }
    //protected virtual void OnBecameInvisible() { }

    // ========== 애니메이션 ==========
    //protected virtual void OnAnimatorMove() { }
    //protected virtual void OnAnimatorIK(int layerIndex) { }

    // ========== GUI / 입력 ==========
    // protected virtual void OnGUI() { }

    // ========== 애플리케이션 상태 ==========
    protected virtual void OnApplicationFocus(bool hasFocus) { }
    protected virtual void OnApplicationPause(bool pauseStatus) { }
    protected virtual void OnApplicationQuit() { }

    // ========== 오디오 ==========
    // protected virtual void OnAudioFilterRead(float[] data, int channels) { }

    // ========== 기타 ==========
    protected virtual void Reset() { }

//#if UNITY_EDITOR
//    protected virtual void OnValidate() {
//        AssignAttribute.setFields(this);
//    }
//#endif

    // ========== 드래그 등 마우스 ==========
    protected virtual void OnMouseEnter() { }
    protected virtual void OnMouseOver() { }
    protected virtual void OnMouseExit() { }
    protected virtual void OnMouseDown() { }
    protected virtual void OnMouseUp() { }
    protected virtual void OnMouseDrag() { }
}
