using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;

namespace DemoMVP
{
    /// <summary>
    /// This demo is able to load all events from a MIDI file only by script.\n
    /// There is nothing to create in the Unity editor, just add this script to a GameObject in your scene and run!
    /// </summary>
    public class TheSimplestMidiLoader : MonoBehaviour
    {
        MidiFileLoader midiFileLoader;

        private void Awake()
        {
            Debug.Log("Awake: dynamically add MidiFileLoader component");

            // MidiPlayerGlobal is a singleton: only one instance can be created. 
            if (MidiPlayerGlobal.Instance == null)
                gameObject.AddComponent<MidiPlayerGlobal>();
            
            // When running, this component will be added to this gameObject
            midiFileLoader = gameObject.AddComponent<MidiFileLoader>();
        }

        public void Start()
        {
            Debug.Log("Start: select a MIDI file and load MIDI events. Any sequencer and synth are instanciated");

            // Select a MIDI from the MIDI DB (with exact name)
            midiFileLoader.MPTK_MidiName = "Bach - Fugue";
            // Load the MIDI file
            if (midiFileLoader.MPTK_Load())
            {
                // Read all MIDI events
                List<MPTKEvent> sequence = midiFileLoader.MPTK_ReadMidiEvents();
                Debug.Log($"Loading '{midiFileLoader.MPTK_MidiName}', MIDI events count:{sequence.Count}");
            }
            else
                Debug.Log($"Loading '{midiFileLoader.MPTK_MidiName}' - Error");
        }
    }
}