
#define POSITIONING_LOGGING

using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Serialization;

public class PositionAdjustmentSliders : MonoBehaviour
{
    [SerializeField] private Transform transformToSet;
    [SerializeField] private PinchSlider xSlider;
    [SerializeField] private PinchSlider ySlider;
    [SerializeField] private PinchSlider zSlider;
    [SerializeField] private float sensitivity = 10f;

    // Start is called before the first frame update
    void Start()
    {
        xSlider.SliderValue = transformToSet.localPosition.x / sensitivity / 2f + 0.5f;
        ySlider.SliderValue = transformToSet.localPosition.y / sensitivity / 2f + 0.5f;
        zSlider.SliderValue = transformToSet.localPosition.z / sensitivity / 2f + 0.5f;
    }

    // Update is called once per frame
    void Update()
    {
        var newPosition = sensitivity * 2f * new Vector3(
            xSlider.SliderValue - 0.5f,
            ySlider.SliderValue - 0.5f,
            zSlider.SliderValue - 0.5f
        );
        
#if POSITIONING_LOGGING
        if (newPosition != transformToSet.localPosition)
            Debug.Log("moved object to:" + newPosition);
#endif

        transformToSet.localPosition = newPosition;
    }
}
