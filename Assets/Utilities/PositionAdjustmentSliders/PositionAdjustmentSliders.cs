
#define POSITIONING_LOGGING

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Serialization;


public class PositionAdjustmentSliders : MonoBehaviour
{
    [SerializeField] private Transform transformToSet;
    [SerializeField] private PinchSlider xSlider;
    [SerializeField] private PinchSlider ySlider;
    [SerializeField] private PinchSlider zSlider;
    [SerializeField] private PinchSlider rotationSlider;
    [SerializeField] private float sensitivity = .2f;
    [SerializeField] private float rotationalSensitivity = .2f;

    private Vector3 positionOffset;
    private float rotationOffset;

    private Vector3 sliderValues =>
        sensitivity * 2f * new Vector3(xSlider.SliderValue - 0.5f, ySlider.SliderValue - 0.5f, zSlider.SliderValue - 0.5f);

    private float rotationSliderValue => (rotationSlider.SliderValue - 0.5f) * 180f * rotationalSensitivity;

    // Start is called before the first frame update
    void Start()
    {
        positionOffset = transformToSet.localPosition - sliderValues;
        rotationOffset = transformToSet.localRotation.eulerAngles.y - rotationSliderValue;
    }

    // Update is called once per frame
    void Update()
    {
        var newPosition = sliderValues + positionOffset;

#if POSITIONING_LOGGING
        if (newPosition != transformToSet.localPosition)
            Debug.Log("moved object to: (" + newPosition.x + ", " + newPosition.y + ", " + newPosition.z + ")");
#endif

        transformToSet.localPosition = newPosition;

        Vector3 newRotation = transformToSet.localRotation.eulerAngles;
        newRotation.y = rotationSliderValue + rotationOffset;
#if POSITIONING_LOGGING
        if (Math.Abs(newRotation.y - transformToSet.localRotation.eulerAngles.y) > 0.0001f)
            Debug.Log("rotated object to: " + newRotation.y);
#endif
        transformToSet.localRotation = Quaternion.Euler(newRotation);
    }
}
