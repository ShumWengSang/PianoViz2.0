using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArUcoDetectionHoloLensUnity;
using DG.Tweening;

public class MarkerDetection : MonoBehaviour
{
    public ArUcoMarkerDetection marker;
    public Transform gameObjectToMove;

    private void OnEnable()
    {
        
    }


    private void OnDisable()
    {

    }

    public void StartDetection()
    {
        marker.enabled = true;

        Sequence seq = DOTween.Sequence();
        seq.InsertCallback(10.0f, () => { StopDetecting(); });
    }

    public void StopDetecting()
    {
        marker.enabled = false;
    }
}
