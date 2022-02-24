using System.Collections.Generic;
using MidiPlayerTK;
using UnityEngine;

public static class ExtensionMethods
{
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

}

class MyNoteCallback : MonoBehaviour
{
    private MidiStreamPlayer midiStreamPlayer;
    // Note to event
    private Dictionary<int, MPTKEvent> _currentPlayingEvents = new Dictionary<int , MPTKEvent>(100);
    public int channel = 0;

    private void OnEnable()
    {
        BleMidiBroadcaster.onNoteDown += OnNoteDown;
        BleMidiBroadcaster.onNoteUp += OnNoteUp;
        midiStreamPlayer = FindObjectOfType<MidiStreamPlayer>();
    }

    private void OnDisable()
    {
        BleMidiBroadcaster.onNoteDown -= OnNoteDown;
        BleMidiBroadcaster.onNoteUp -= OnNoteUp;
    }

    private void OnNoteDown(MidiNote note, int velocity)
    {
        var notePlaying = new MPTKEvent()
        {
            Command = MPTKCommand.NoteOn,
            Value = (int)note,  //C5
            Channel = channel,
            Duration = -1,
            Velocity = velocity,
            Delay = 0,
        };
        midiStreamPlayer.MPTK_PlayEvent(notePlaying);
        _currentPlayingEvents[(int)note] = notePlaying;
    }

    private void OnNoteUp(MidiNote note, int velocity)
    {
        if (_currentPlayingEvents.ContainsKey((int)note))
        {
            midiStreamPlayer.MPTK_StopEvent(_currentPlayingEvents[(int)note]);
            _currentPlayingEvents.Remove((int)note);
        }
    }


    //            var notePlaying = new MPTKEvent()
    //            {
    //                Command = MPTKCommand.NoteOn,
    //                Value = note.noteNumber,  //C5
    //                Channel = channel,
    //                Duration = 10000,
    //                Velocity = (int)velocity.Remap(0, 1, 0, 127),
    //                Delay = 0,
    //            };
    //            midiStreamPlayer.MPTK_PlayEvent(notePlaying);
    //            _currentPlayingEvents[note] = notePlaying;
    //        };

    //        midiDevice.onWillNoteOff += (note) => {
    //            Debug.Log(string.Format(
    //                "Note Off #{0} ({1}) ch:{2} dev:'{3}'",
    //                note.noteNumber,
    //                note.shortDisplayName,
    //                (note.device as Minis.MidiDevice)?.channel,
    //                note.device.description.product
    //            ));

    //            if (_currentPlayingEvents.ContainsKey(note))
    //            {
    //                midiStreamPlayer.MPTK_StopEvent(_currentPlayingEvents[note]);
    //                _currentPlayingEvents.Remove(note);
    //            }
    //        };
}