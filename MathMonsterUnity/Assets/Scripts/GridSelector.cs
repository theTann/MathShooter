using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GridSelector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler {
    RectTransform _rectTransform;
    List<Gem> _gemPool = new List<Gem>();

    void IDragHandler.OnDrag(PointerEventData eventData) {
        Debug.Log($"Drag!");
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
        Debug.Log($"PointerDown!");
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform,
            eventData.position,
            eventData.pressEventCamera,  // UI 카메라 (Canvas가 Screen Space - Camera일 때 필요)
            out Vector2 localPos
        );
        Debug.Log($"ScreenPosition : {eventData.position}, Local Position: {localPos}");
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
        Debug.Log($"PointerUp!");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        float fixedWidth = _rectTransform.rect.width;
        float gemCount = 9;
        _rectTransform.sizeDelta = new Vector2(fixedWidth, 500);
        float gemSize = fixedWidth / gemCount;
        Debug.Log($"fixedWidth : {fixedWidth}. gem size : {gemSize}");
    }

    private void createGem(int createCount) {
        Addressables.InstantiateAsync("", )
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
