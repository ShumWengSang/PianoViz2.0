using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;

/// <summary>
/// Load a MIDI and play in loop between two bar position. 
/// Icing on the cake: filtering notes played by channel. 
/// 
/// As usual wit a MVP demo, focus is on the essentials: no value check, no error catch, limited functions ...
/// 
/// </summary>
public class MidiLoop : MonoBehaviour
{
    /// <summary>
    /// MPTK component able to play a Midi file from your list of Midi file. This PreFab must be present in your scene.
    /// </summary>
    public MidiFilePlayer midiFilePlayer;

    /// <summary>
    /// Bar to start playing. Change value in the Inspector.
    /// </summary>
    public int StartBar;

    /// <summary>
    /// Bar where to loop playing. Change value in the Inspector.
    /// </summary>
    public int LoopBar;

    /// <summary>
    /// Play only notes from this channel. -1 for playing all channels. Change value in the Inspector.
    /// </summary>
    public int ChannelSelected;

    // Start is called before the first frame update
    void Start()
    {
        // Find existing MidiFilePlayer in the scene hierarchy
        // ---------------------------------------------------

        midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
        if (midiFilePlayer == null)
        {
            Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
            return;
        }

        // Set Listeners 
        // -------------

        // triggered when MIDI starts playing (Indeed, will be triggered at every loop)
        midiFilePlayer.OnEventStartPlayMidi.AddListener(StartPlay);

        // triggered every time MIDI events are read from the MIDI File and are ready to be played with the MIDI synth.
        midiFilePlayer.OnEventNotesMidi.AddListener(MidiReadEvents);
    }

    /// <summary>
    /// Start playing MIDI: MIDI File is loaded, Midi Synth is initialized, but so far any MIDI event has been read.
    /// This is the right time to defined some specific behaviors. 
    /// </summary>
    /// <param name="name"></param>
    public void StartPlay(string name)
    {
        Debug.Log($"Start playing {name}");
        Debug.Log($"   Delta Ticks Per Quarter: {midiFilePlayer.MPTK_MidiLoaded.MPTK_DeltaTicksPerQuarterNote}");
        Debug.Log($"   Number Beats Measure   : { midiFilePlayer.MPTK_MidiLoaded.MPTK_NumberBeatsMeasure}");
        Debug.Log($"   Number Quarter Beats   : { midiFilePlayer.MPTK_MidiLoaded.MPTK_NumberQuarterBeat}");

        // Enable or disable MIDI channel
        for (int channel = 0; channel < 16; channel++)
            midiFilePlayer.MPTK_ChannelEnableSet(channel, channel == ChannelSelected || ChannelSelected == -1 ? true : false);

        // Set start position
        midiFilePlayer.MPTK_TickCurrent = ConvertBarToTick(StartBar);
    }

    /// <summary>
    /// Triggered by the listener when midi notes are available from MidiFilePlayer. 
    /// </summary>
    public void MidiReadEvents(List<MPTKEvent> midiEvents)
    {
        if (midiFilePlayer.MPTK_TickCurrent > ConvertBarToTick(LoopBar))
        {
            //// Uncomment if you have the Pro version for a delayed start
            //// ---------------------------------------------------------
            //midiFilePlayer.MPTK_Stop(); // avoid delayed stop because MPTK will continue to trigger this function and MPTK_Stop will be continously call.
            //// Delayed start for 2 seconds with a volume rampup of 1 second.
            //midiFilePlayer.MPTK_Play(1000f, 2000f);

            // Replay the MIDI: StartPlay() will be triggered and MPTK_TickCurrent will be set from StartBar value (converted to tick).
            // For a delayed start: comment below and uncomment further up if you want to test delayed start (only with the Pro Version).
            midiFilePlayer.MPTK_RePlay();
        }
    }

    /// <summary>
    /// Convert a bar number (musical score concept) to a tick position (MIDI concept).
    /// <br><b>Tested only with 4/4 signature! 
    /// If someone brave wants to test ? I will be very happy :-)</b></br>
    /// </summary>
    /// <param name="bar"></param>
    /// <returns></returns>
    long ConvertBarToTick(int bar)
    {
        return (long)(bar * midiFilePlayer.MPTK_MidiLoaded.MPTK_NumberQuarterBeat * midiFilePlayer.MPTK_MidiLoaded.MPTK_DeltaTicksPerQuarterNote);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
