using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.UI;
using MidiPlayerTK;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class FloatEvent : UnityEvent<float>
{
}
public class SongSpeedSliderScript : MonoBehaviour
{
    
    [SerializeField] private MidiFilePlayer midiPlayer;
    [SerializeField] private StepSlider slider;

    [SerializeField] private float minValue = 0.1f;
    [SerializeField] private float maxValue = 1f;

    [SerializeField] public FloatEvent OnValueChange;
    // Update is called once per frame
    void Update()
    {
        midiPlayer.MPTK_Speed = (maxValue - minValue) * slider.SliderValue + minValue;
        OnValueChange.Invoke(midiPlayer.MPTK_Speed);
    }

    public void SetSpeed(float speed)
    {
        slider.SliderValue = speed;
        Update();
    }
}
