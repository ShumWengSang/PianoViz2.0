
//#define POSITIONING_LOGGING

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
    [SerializeField] private PinchSlider rotationSlider;
    [SerializeField] private float sensitivity = 10f;

    // Start is called before the first frame update
    void Start()
    {
        xSlider.SliderValue = transformToSet.localPosition.x / sensitivity / 2f + 0.5f;
        ySlider.SliderValue = transformToSet.localPosition.y / sensitivity / 2f + 0.5f;
        zSlider.SliderValue = transformToSet.localPosition.z / sensitivity / 2f + 0.5f;
        rotationSlider.SliderValue = transformToSet.localRotation.eulerAngles.y / 360 + 0.5f;
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
            Debug.Log("moved object to: (" + newPosition.x + ", " + newPosition.y + ", " + newPosition.z + ")");
#endif

        transformToSet.localPosition = newPosition;

        Vector3 newRotation = transformToSet.localRotation.eulerAngles;
        newRotation.y = 360f * (rotationSlider.SliderValue - 0.5f);
#if POSITIONING_LOGGING
        if (newRotation.y != transformToSet.eulerAngles.y)
            Debug.Log("rotated object to: newRotation.y");
#endif
        transformToSet.localRotation = Quaternion.Euler(newRotation);
    }
}
