
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;
using UnityEngine.Events;
using MEC;

namespace MidiPlayerTK
{
    /// <summary>
    /// This class, associated with the prefab MidiFileLoader is useful for loading all or a part of the MIDI events from a Midi file.\n 
    /// No sequencer, no synthetizer, no music playing capabilities, just loading and decoding a MIDI file to the more easy class MPTKEvent.\n 
    /// MIDI can be loaded from the MidiDB list (see Unity menu MPTK / Midi Player Setup) or from a folder on the desktop (Pro).
    /// @snippet TestMidiFileLoad.cs Example TheMostSimpleDemoForMidiLoader
    /// </summary>
    public partial class MidiFileLoader : MonoBehaviour
    {
        /// <summary>
        /// Midi name to load. Use the exact name defined in Unity resources folder MidiDB without any path or extension.
        /// Tips: Add Midi files to your project with the Unity menu MPTK or add it directly in the ressource folder and open Midi File Setup to automatically integrate Midi in MPTK.
        /// </summary>
        public string MPTK_MidiName
        {
            get
            {
                //Debug.Log("MPTK_MidiName get " + midiNameToPlay);
                return midiNameToPlay;
            }
            set
            {
                //Debug.Log("MPTK_MidiName set " + value);
                midiIndexToPlay = MidiPlayerGlobal.MPTK_FindMidi(value);
                //Debug.Log("MPTK_MidiName set index= " + midiIndexToPlay);
                midiNameToPlay = value;
            }
        }
        [SerializeField]
        [HideInInspector]
        private string midiNameToPlay;

        /// <summary>
        /// Index Midi. Find the Index of Midi file from the popup in MidiFileLoader inspector.\n
        /// Tips: Add Midi files to your project with the Unity menu MPTK or add it directly in the ressource folder and open Midi File Setup to automatically integrate Midi in MPTK.\n
        /// return -1 if not found
        /// @code
        /// midiFileLoader.MPTK_MidiIndex = 1;
        /// @endcode
        /// </summary>
        /// <param name="index"></param>
        public int MPTK_MidiIndex
        {
            get
            {
                try
                {
                    //int index = MidiPlayerGlobal.MPTK_FindMidi(MPTK_MidiName);
                    //Debug.Log("MPTK_MidiIndex get " + midiIndexToPlay);
                    return midiIndexToPlay;
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }
                return -1;
            }
            set
            {
                try
                {
                    //Debug.Log("MPTK_MidiIndex set " + value);
                    if (value >= 0 && value < MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count)
                    {
                        MPTK_MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[value];
                        // useless, set when set midi name : 
                        midiIndexToPlay = value;
                    }
                    else
                        Debug.LogWarning("MidiFilePlayer - Set MidiIndex value not valid : " + value);
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private int midiIndexToPlay;

        /// <summary>
        /// Log midi events
        /// </summary>
        public bool MPTK_LogEvents;

        /// <summary>
        /// Should keep note off event Events ? 
        /// </summary>
        public bool MPTK_KeepNoteOff;

        /// <summary>
        /// When set to true, meta MIDI event End Track are keep. Default is false.\n
        /// If set to true, the duration of the MIDI taken into account the End Track Event.
        /// </summary>
        public bool MPTK_KeepEndTrack;


        // Should accept change tempo from Midi Events ? 
        // remove after v2.88
        //public bool MPTK_EnableChangeTempo;

        /// <summary>
        /// Initial tempo read in the Midi.
        /// </summary>
        public double MPTK_InitialTempo;

        /// <summary>
        /// Duration of the midi. 
        /// </summary>
        public TimeSpan MPTK_Duration;

        // V2.88 removed Real Duration of the midi calculated with the midi change tempo events find inside the midi file.
        //public TimeSpan MPTK_RealDuration;

        /// <summary>
        /// Duration (milliseconds) of the midi. 
        /// </summary>
        public float MPTK_DurationMS { get { try { if (midiLoaded != null) return midiLoaded.MPTK_DurationMS; } catch (System.Exception ex) { MidiPlayerGlobal.ErrorDetail(ex); } return 0f; } }


        /// <summary>
        /// Last tick position in Midi: Time of the last midi event in sequence expressed in number of "ticks".\n
        /// MPTK_TickLast / MPTK_DeltaTicksPerQuarterNote equal the duration time of a quarter-note regardless the defined tempo.
        /// </summary>
        public long MPTK_TickLast;

        /// <summary>
        /// From TimeSignature event: The numerator counts the number of beats in a measure.\n
        /// For example a numerator of 4 means that each bar contains four beats.\n
        /// This is important to know because usually the first beat of each bar has extra emphasis.\n
        /// https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public int MPTK_NumberBeatsMeasure;

        /// <summary>
        /// From TimeSignature event: number of quarter notes in a beat.\n
        /// Equal 2 Power TimeSigDenominator.\n
        /// https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public int MPTK_NumberQuarterBeat;

        /// <summary>
        /// From TimeSignature event: The numerator counts the number of beats in a measure.\n
        /// For example a numerator of 4 means that each bar contains four beats.\n
        /// This is important to know because usually the first beat of each bar has extra emphasis.\n
        /// In MIDI the denominator value is stored in a special format. i.e. the real denominator = 2 ^ MPTK_TimeSigNumerator\n
        /// https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public int MPTK_TimeSigNumerator;

        /// <summary>
        /// From TimeSignature event: The denominator specifies the number of quarter notes in a beat.\n
        ///   2 represents a quarter-note,\n
        ///   3 represents an eighth-note, etc.\n
        /// https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public int MPTK_TimeSigDenominator;

        /// <summary>
        /// From KeySignature event: Values between -7 and 7 and specifies the key signature in terms of number of flats (if negative) or sharps (if positive)
        /// https://www.recordingblogs.com/wiki/midi-key-signature-meta-message
        /// </summary>
        public int MPTK_KeySigSharpsFlats;

        /// <summary>
        /// From KeySignature event: Specifies the scale of the MIDI file.
        /// @li 0 the scale is major.
        /// @li 1 the scale is minor.
        /// https://www.recordingblogs.com/wiki/midi-key-signature-meta-message
        /// </summary>
        public int MPTK_KeySigMajorMinor;

        /// <summary>
        /// From TimeSignature event: The standard MIDI clock ticks every 24 times every quarter note (crotchet)\n
        /// So a MPTK_TicksInMetronomeClick value of 24 would mean that the metronome clicks once every quarter note.\n
        /// A MPTK_TicksInMetronomeClick value of 6 would mean that the metronome clicks once every 1/8th of a note (quaver).\n
        /// https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public int MPTK_TicksInMetronomeClick;

        /// <summary>
        /// From TimeSignature event: This value specifies the number of 1/32nds of a note happen every MIDI quarter note.\n
        /// It is usually 8 which means that a quarter note happens every quarter note.\n
        /// https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public int MPTK_No32ndNotesInQuarterNote;

        /// <summary>
        /// From the SetTempo event: The tempo is given in micro seconds per quarter beat. \n
        /// To convert this to BPM we needs to use the following equation:BPM = 60,000,000/[tt tt tt]\n
        /// Warning: this value can change during the playing when a change tempo event is find. \n
        /// https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public int MPTK_MicrosecondsPerQuarterNote;

        /// <summary>
        /// From Midi Header: Delta Ticks Per Quarter Note. \n
        /// Represent the duration time in "ticks" which make up a quarter-note. \n
        /// For instance, if 96, then a duration of an eighth-note in the file would be 48.
        /// </summary>
        public int MPTK_DeltaTicksPerQuarterNote;

        /// <summary>
        /// Count of track read in the Midi file.\n
        /// Not to be confused with channel. A track can contains midi events for different channel.
        /// </summary>
        public int MPTK_TrackCount;

        private MidiLoad midiLoaded;

        /// <summary>
        /// Get detailed information about the midi loaded. 
        /// </summary>
        public MidiLoad MPTK_MidiLoaded { get { return midiLoaded; } }

        void Awake()
        {
            //Debug.Log("Awake MidiFileLoader");
        }

        void Start()
        {
            //Debug.Log("Start MidiFileLoader");
        }


        /// <summary>
        /// Load the midi file defined with MPTK_MidiName or MPTK_MidiIndex or from a array of bytes. Look at MPTK_MidiLoaded for detailed information about the MIDI loaded.
        /// </summary>
        /// <param name="midiBytesToLoad">byte arry from a midi stream</param>
        /// <returns>true if loading succeed/returns>
        public bool MPTK_Load(byte[] midiBytesToLoad = null)
        {
            bool result = false;
            try
            {
                // Load description of available soundfont
                //if (MidiPlayerGlobal.ImSFCurrent != null && MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                {
                    if (string.IsNullOrEmpty(MPTK_MidiName))
                        MPTK_MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[0];
                    int selectedMidi = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.FindIndex(s => s == MPTK_MidiName);
                    if (selectedMidi < 0)
                    {
                        Debug.LogWarning("MidiFilePlayer - MidiFile " + MPTK_MidiName + " not found. Try with the first in list.");
                        selectedMidi = 0;
                        MPTK_MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[0];
                    }

                    try
                    {
                        midiLoaded = new MidiLoad();

                        // No midi byte array, try to load from MidiFile from resource
                        if (midiBytesToLoad == null || midiBytesToLoad.Length == 0)
                        {
                            TextAsset mididata = Resources.Load<TextAsset>(System.IO.Path.Combine(MidiPlayerGlobal.MidiFilesDB, MPTK_MidiName));
                            midiBytesToLoad = mididata.bytes;
                        }

                        midiLoaded.KeepNoteOff = MPTK_KeepNoteOff;
                        midiLoaded.MPTK_KeepEndTrack = MPTK_KeepEndTrack;
                        midiLoaded.MPTK_EnableChangeTempo = true;
                        midiLoaded.LogEvents = MPTK_LogEvents;
                        if (!midiLoaded.MPTK_Load(midiBytesToLoad))
                            return false;
                        SetAttributes();
                        result = true;
                    }
                    catch (System.Exception ex)
                    {
                        MidiPlayerGlobal.ErrorDetail(ex);
                    }
                }
                //else
                //    Debug.LogWarning(MidiPlayerGlobal.ErrorNoMidiFile);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return result;
        }

        private void SetAttributes()
        {
            if (midiLoaded != null)
            {
                MPTK_InitialTempo = midiLoaded.MPTK_InitialTempo;
                MPTK_Duration = midiLoaded.MPTK_Duration;
                MPTK_TickLast = midiLoaded.MPTK_TickLast;
                MPTK_NumberBeatsMeasure = midiLoaded.MPTK_NumberBeatsMeasure;
                MPTK_NumberQuarterBeat = midiLoaded.MPTK_NumberQuarterBeat;
                MPTK_TimeSigNumerator = midiLoaded.MPTK_TimeSigNumerator;
                MPTK_TimeSigDenominator = midiLoaded.MPTK_TimeSigDenominator;
                MPTK_KeySigSharpsFlats = midiLoaded.MPTK_KeySigSharpsFlats;
                MPTK_KeySigMajorMinor = midiLoaded.MPTK_KeySigMajorMinor;
                MPTK_TicksInMetronomeClick = midiLoaded.MPTK_TicksInMetronomeClick;
                MPTK_No32ndNotesInQuarterNote = midiLoaded.MPTK_No32ndNotesInQuarterNote;
                MPTK_MicrosecondsPerQuarterNote = midiLoaded.MPTK_MicrosecondsPerQuarterNote;
                MPTK_DeltaTicksPerQuarterNote = midiLoaded.MPTK_DeltaTicksPerQuarterNote;
                MPTK_TrackCount = midiLoaded.MPTK_TrackCount;
            }
        }
        /// <summary>
        /// Read the list of midi events available in the Midi file from a ticks position to an end position into a List of MPTKEvent
        /// @snippet TestMidiFileLoad.cs Example TheMostSimpleDemoForMidiLoader
        /// See full example in TestMidiFileLoad.cs
        /// </summary>
        /// <param name="fromTicks">ticks start, default from start</param>
        /// <param name="toTicks">ticks end, default to the end</param>
        /// <returns></returns>
        public List<MPTKEvent> MPTK_ReadMidiEvents(long fromTicks = 0, long toTicks = long.MaxValue)
        {
            if (midiLoaded == null)
            {
                NoMidiLoaded("MPTK_ReadMidiEvents");
                return null;
            }
            midiLoaded.LogEvents = MPTK_LogEvents;
            midiLoaded.KeepNoteOff = MPTK_KeepNoteOff;
            midiLoaded.MPTK_KeepEndTrack = MPTK_KeepEndTrack;
            midiLoaded.MPTK_EnableChangeTempo = true;
            return midiLoaded.MPTK_ReadMidiEvents(fromTicks, toTicks);
        }

        private void NoMidiLoaded(string action)
        {
            Debug.LogWarning(string.Format("No Midi loaded, {0} canceled", action));
        }
        /// <summary>
        /// Read next Midi from the list of midi defined in MPTK (see Unity menu Midi)
        /// </summary>
        public void MPTK_Next()
        {
            try
            {
                if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                {
                    int selectedMidi = 0;
                    //Debug.Log("Next search " + MPTK_MidiName);
                    if (!string.IsNullOrEmpty(MPTK_MidiName))
                        selectedMidi = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.FindIndex(s => s == MPTK_MidiName);
                    if (selectedMidi >= 0)
                    {
                        selectedMidi++;
                        if (selectedMidi >= MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count)
                            selectedMidi = 0;
                        MPTK_MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[selectedMidi];
                        //Debug.Log("Next found " + MPTK_MidiName);
                    }
                }
                else
                    Debug.LogWarning(MidiPlayerGlobal.ErrorNoMidiFile);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Read previous Midi from the list of midi defined in MPTK (see Unity menu Midi)
        /// </summary>
        public void MPTK_Previous()
        {
            try
            {
                if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                {
                    int selectedMidi = 0;
                    if (!string.IsNullOrEmpty(MPTK_MidiName))
                        selectedMidi = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.FindIndex(s => s == MPTK_MidiName);
                    if (selectedMidi >= 0)
                    {
                        selectedMidi--;
                        if (selectedMidi < 0)
                            selectedMidi = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count - 1;
                        MPTK_MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[selectedMidi];
                    }
                }
                else
                    Debug.LogWarning(MidiPlayerGlobal.ErrorNoMidiFile);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Return note length as https://en.wikipedia.org/wiki/Note_value 
        /// </summary>
        /// <param name="note"></param>
        /// <returns>MPTKEvent.EnumLength</returns>
        public MPTKEvent.EnumLength MPTK_NoteLength(MPTKEvent note)
        {
            if (midiLoaded != null)
                return midiLoaded.NoteLength(note);
            else
                NoMidiLoaded("MPTK_NoteLength");
            return MPTKEvent.EnumLength.Sixteenth;
        }
    }
}

