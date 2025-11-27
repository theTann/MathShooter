using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LongTouchDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler {
    public float longPressThreshold = 1.0f; // 롱터치 시간 (초)
    private bool isPointerDown = false;
    private float pointerDownTimer = 0f;

    public System.Action<LongTouchDetector> onLongPress; // 외부에서 이벤트 등록 가능
    public System.Action<LongTouchDetector> onTap;

    public int idx { get; set; }

    private void Update() {
        if (isPointerDown) {
            pointerDownTimer += Time.deltaTime;

            if (pointerDownTimer >= longPressThreshold) {
                isPointerDown = false;
                pointerDownTimer = 0f;
                onLongPress?.Invoke(this);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        isPointerDown = true;
        pointerDownTimer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData) {
        if(isPointerDown == true) {
            onTap?.Invoke(this);
        }

        Reset();
    }

    public void OnPointerExit(PointerEventData eventData) {
        Reset();
    }

    private void Reset() {
        isPointerDown = false;
        pointerDownTimer = 0f;
    }
}
