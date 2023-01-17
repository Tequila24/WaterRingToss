using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimpleButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public readonly UnityEvent _downEvent = new UnityEvent();
    public readonly UnityEvent _upEvent = new UnityEvent();

    bool _pushState = false;

    [SerializeField]
    Sprite _activeSprite;
    [SerializeField]
    Sprite _inactiveSprite;

    Image _activeImage;
    Image _inactiveImage;

//================================================================================================================================


    void OnValidate()
    {
        RectTransform selfTransform = this.gameObject.GetComponent<RectTransform>();
        Vector2 selfSize = new Vector2(selfTransform.rect.width, selfTransform.rect.height);

        Transform activeImageTransform = this.transform.Find("ActiveImage");
        if (activeImageTransform != null) 
        {
            _activeImage = activeImageTransform.gameObject.GetComponent<Image>();
            _activeImage.sprite = _activeSprite;
        }

        Transform inactiveImageTransform = this.transform.Find("InactiveImage");
        if (inactiveImageTransform != null) 
        {
            _inactiveImage = inactiveImageTransform.gameObject.GetComponent<Image>();
            _inactiveImage.sprite = _inactiveSprite;
        }

        UpdateVisual();
    }


    public void OnPointerDown(PointerEventData pointerEventData)
    {
        _pushState = true;

        UpdateVisual();

        _downEvent.Invoke();
    }


    public void OnPointerUp(PointerEventData pointerEventData)
    {
        _pushState = false;

        UpdateVisual();

        _upEvent.Invoke();
    }


    public void ForceRelease()
    {
        if (_pushState) {
            _pushState = !_pushState;
        }

        UpdateVisual();
    }


    public void UpdateVisual()
    {
        _activeImage.enabled = _pushState;
        _inactiveImage.enabled = !_pushState;
    }
    
}
