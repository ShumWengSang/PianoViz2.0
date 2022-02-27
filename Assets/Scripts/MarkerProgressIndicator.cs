using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using System.Threading.Tasks;

public class MarkerProgressIndicator : MonoBehaviour
{
    [SerializeField]
    private GameObject progressIndicatorRotatingOrbsGo = null;

    private IProgressIndicator progressIndicatorRotatingOrbs;

    [SerializeField]
    private MarkerDetection markerDetection;

    private void OnEnable()
    {
        progressIndicatorRotatingOrbs = progressIndicatorRotatingOrbsGo.GetComponent<IProgressIndicator>();
    }

    public void StartProgressIndicator()
    {
        OpenProgressIndicator(progressIndicatorRotatingOrbs);
    }

    private async void OpenProgressIndicator(IProgressIndicator indicator)
    {
        await indicator.OpenAsync();
        indicator.Message = "Looking for arUco marker ... ";

        await Task.Delay(500);
        while (markerDetection.Detecting())
        {
            await Task.Yield();
        }

        await indicator.CloseAsync();
    }
}
