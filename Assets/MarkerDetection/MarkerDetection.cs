using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArUcoDetectionHoloLensUnity;
using DG.Tweening;

public class MarkerDetection : MonoBehaviour
{
    public ArUcoMarkerDetection marker;
    public MarkerProgressIndicator progressIndicator;

    private void Start()
    {
        // StartDetection();
    }

    public bool Detecting()
    {
        return !marker.MarkerDetected;
    }

    public void StartDetection()
    {
        progressIndicator.StartProgressIndicator();
        
        
        Sequence seq = DOTween.Sequence();
        seq.InsertCallback(0.2f, () => { marker.enabled = true; });

        StartCoroutine(WaitForStopDetection());
    }

    public void StopDetecting()
    {
        marker.enabled = false;
    }

    IEnumerator WaitForStopDetection()
    {
        while (Detecting())
        {
            yield return null;
        }
        yield return new WaitForSeconds(1.0f);
        StopDetecting();
        this.enabled = false;
    }
}
