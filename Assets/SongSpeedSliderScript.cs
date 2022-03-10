using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using MidiPlayerTK;
using UnityEngine;

public class SongSpeedSliderScript : MonoBehaviour
{
    [SerializeField] private MidiFilePlayer midiPlayer;
    [SerializeField] private PinchSlider slider;
    // Update is called once per frame
    void Update()
    {
        midiPlayer.MPTK_Speed = slider.SliderValue;
    }
}
