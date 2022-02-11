#define DEBUG_LOGEVENT 
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using MPTK.NAudio.Midi;
using System;
using System.IO;
using System.Linq;

namespace MidiPlayerTK
{
    /// <summary>
    /// Base class for loading a Midi file.\n
    /// Internal class used by MidiFilePlayer, MidiListPlayer, MidiFileWrite2, MidiFileLoader.\n
    /// <b>It is not recommended to instanciate directly this class.</b> 
    /// It is better to use the prefab and class MidiFileLoader if you want to only load a Midi file.
    public partial class MidiLoad
    {
        //! @cond NODOC
        public MidiFile midifile;
        public bool EndMidiEvent;
        //public double QuarterPerMinuteValue;
        public string SequenceTrackName = "";
        public string ProgramName = "";
        public string TrackInstrumentName = "";
        public string TextEvent = "";
        public string Copyright = "";

        /// <summary>
        /// Lenght in millisecond of a tick. Obviously depends on the current tempo and the ticks per quarter.
        /// </summary>
        public double MPTK_PulseLenght;

        //! @endcond

        public List<TrackMidiEvent> MPTK_MidiEvents;

        /// <summary>
        /// Initial tempo found in the Midi.
        /// </summary>
        public double MPTK_InitialTempo;

        /// <summary>
        /// Current tempo played by the internal Midi sequencer.
        /// </summary>
        public double MPTK_CurrentTempo { get { return fluid_player_get_bpm(); } }

        /// <summary>
        /// Real duration expressed in TimeSpan of the full midi from the first event (tick=0) to the last event.\n
        /// If #MPTK_KeepEndTrack is false, the MIDI events End Track are not considered to calculate this time.\n
        /// The tempo changes are taken into account if #MPTK_EnableChangeTempo is set to true before loading the MIDI.
        /// </summary>
        public TimeSpan MPTK_Duration;

        /// <summary>
        /// Real duration expressed in milliseconds of the full midi from the first event (tick=0) to the last event.\n
        /// If #MPTK_KeepEndTrack is false, the MIDI events End Track are not considered to calculate this time.\n
        /// The tempo changes are taken into account if #MPTK_EnableChangeTempo is set to true before loading the MIDI.
        /// </summary>
        public float MPTK_DurationMS;

        /// <summary>
        /// Tick position of the last MIDI event found.
        /// </summary>
        public long MPTK_TickLast;

        /// <summary>
        /// Set or get the current tick position when the MIDI sequencer is playing the MIDI. \n
        /// Midi tick is an easy way to identify a position in a song independently of the time which could vary with tempo change. \n
        /// The count of ticks for a quarter is constant all along a Midi: see properties #MPTK_DeltaTicksPerQuarterNote. \n
        /// Example: with a time signature of 4/4 the ticks length of a bar is 4 * #MPTK_DeltaTicksPerQuarterNote.\n
        /// Warning: if you want to set the start position, set #MPTK_TickCurrent inside the processing of the event OnEventStartPlayMidi \n
        /// because MidiFilePlayer#MPTK_Play() reset the start position to 0.\n
        /// Other possibility to change the position in the Midi is to use the property MidiFilePlayer#MPTK_Position: set or get the position in milliseconds \n
        /// but tempo change event will impact also this time.
        /// More info here https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public long MPTK_TickCurrent;

        /// <summary>
        /// Current MIDI event read when the MIDI sequencer is playing the MIDI. See #MPTK_TickCurrent.
        /// </summary>
        public MPTKEvent MPTK_LastEventPlayed;

        /// <summary>
        /// Tick position for the first note-on found.\n
        /// Most MIDI don't start playing a note immediately. There is often a delay.\n
        /// Use this attribute to known the tick position where the will start to play a sound.\n
        /// See also #MPTK_PositionFirstNote
        /// </summary>
        public long MPTK_TickFirstNote;

        /// <summary>
        /// Tick position for the last note-on found.\n
        /// There is often other MIDI events after the last note-on: for example event track-end.\n
        /// Use this attribute to known the tick position time when all sound will be stop.\n
        /// See also the #MPTK_PositionLastNote which provides the last tich of the MIDI.
        /// </summary>
        public long MPTK_TickLastNote;

        /// <summary>
        /// Real time position in millisecond for the first note-on found.\n
        /// Most MIDI don't start playing a note immediately. There is often a delay.\n
        /// Use this attribute to known the real time wich it will start.\n
        /// See also #MPTK_TickFirstNote
        /// </summary>
        public double MPTK_PositionFirstNote;

        /// <summary>
        /// Real time position in millisecond for the last note-on found in the MIDI.\n
        /// There is often other MIDI events after the last note-on: for example event track-end.\n
        /// Use this attribute to known the real time when all sound will be stop.\n
        /// See also the #MPTK_DurationMS which provides the full time of all MIDI events including track-end, control at the beginning and at the end, ....\n
        /// See also #MPTK_TickLastNote
        /// </summary>
        public double MPTK_PositionLastNote;

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
        /// In MIDI the denominator value is stored in a special format. i.e. the real denominator = 2 ^ #MPTK_TimeSigNumerator\n
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
        /// @li  0 the scale is major.
        /// @li  1 the scale is minor.
        /// https://www.recordingblogs.com/wiki/midi-key-signature-meta-message
        /// </summary>
        public int MPTK_KeySigMajorMinor;

        /// <summary>
        /// From TimeSignature event: The standard MIDI clock ticks every 24 times every quarter note (crotchet)\n
        /// So a #MPTK_TicksInMetronomeClick value of 24 would mean that the metronome clicks once every quarter note.\n
        /// A #MPTK_TicksInMetronomeClick value of 6 would mean that the metronome clicks once every 1/8th of a note (quaver).\n
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
        /// Read from the SetTempo event: The tempo is given in micro seconds per quarter beat. 
        /// To convert this to BPM we needs to use the following equation:BPM = 60,000,000/[tt tt tt]
        /// Warning: this value can change during the playing when a change tempo event is find. \n
        /// https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public int MPTK_MicrosecondsPerQuarterNote;

        /// <summary>
        /// Read from MIDI Header: Delta Ticks Per Quarter Note. 
        /// Represent the duration time in "ticks" which make up a quarter-note. 
        /// For instance, if 96, then a duration of an eighth-note in the file would be 48 ticks.
        /// Also named Division.
        /// </summary>
        public int MPTK_DeltaTicksPerQuarterNote;

        /// <summary>
        /// Count of track in the MIDI file
        /// </summary>
        public int MPTK_TrackCount;

        /// <summary>
        /// Time takne expressed in millisecond for loading the MIDI file.
        /// </summary>
        public float MPTK_LoadTime;

        public bool LogEvents;

        public bool KeepNoteOff;

        /// <summary>
        /// When set to true, meta MIDI event End Track are keep. Default is false.\n
        /// If set to true, the duration of the MIDI taken into account the End Track Event.
        /// </summary>
        public bool MPTK_KeepEndTrack;

        /// <summary>
        /// Should accept change tempo from MIDI Events ? 
        /// </summary>
        public bool MPTK_EnableChangeTempo;

        public bool ReadyToStarted;
        public bool ReadyToPlay;

        private long Quantization;
        //private long CurrentTick;
        private double Speed = 1d;
        //private double LastTimeFromStartMS;

        private int seek_ticks;           /* new position in tempo ticks (midi ticks) for seeking */
        private int next_event;
        private int start_ticks;          /* the number of tempo ticks passed at the last tempo change */
        private int cur_ticks;            /* the number of tempo ticks passed */
        //private int begin_msec;           /* the time (msec) of the beginning of the file */
        private int start_msec;           /* the start time of the last tempo change */
        private int cur_msec;             /* the current time */
        private int miditempo;            /* as indicated by MIDI SetTempo: n 24th of a usec per midi-clock. bravo! */
        //private double deltatime;         /* milliseconds per midi tick. depends on set-tempo */

        public MidiLoad()
        {
            //ReadyToStarted = false;
            ReadyToPlay = false;
        }

        private void Init()
        {
            MPTK_PulseLenght = 0;
            MPTK_MidiEvents = null;
            MPTK_Duration = TimeSpan.Zero;
            MPTK_DurationMS = 0f;
            MPTK_TickLast = 0;
            MPTK_TickCurrent = 0;
            MPTK_LastEventPlayed = null;
            MPTK_TickFirstNote = -1;
            MPTK_TickLastNote = -1;
            MPTK_PositionFirstNote = 0;
            MPTK_PositionLastNote = 0;
            MPTK_NumberBeatsMeasure = 0;
            MPTK_NumberQuarterBeat = 0;
            MPTK_TimeSigNumerator = 0;
            MPTK_TimeSigDenominator = 0;
            MPTK_KeySigSharpsFlats = 0;
            MPTK_KeySigMajorMinor = 0;
            MPTK_TicksInMetronomeClick = 0;
            MPTK_No32ndNotesInQuarterNote = 0;
            MPTK_MicrosecondsPerQuarterNote = 0;
            MPTK_DeltaTicksPerQuarterNote = 0;
            MPTK_TrackCount = 0;
        }

        /// <summary>
        /// Load Midi from midi MPTK referential (Unity resource). \n
        /// The index of the Midi file can be found in the windo "Midi File Setup". Display with menu MPTK / Midi File Setup
        /// @code
        /// public MidiLoad MidiLoaded;
        /// // .....
        /// MidiLoaded = new MidiLoad();
        /// MidiLoaded.MPTK_Load(14) // index for "Beattles - Michelle"
        /// Debug.Log("Duration:" + MidiLoaded.MPTK_Duration);
        /// @endcode
        /// </summary>
        /// <param name="index"></param>
        /// <param name="strict">If true will error on non-paired note events, default:false</param>
        /// <returns>true if loaded</returns>
        public bool MPTK_Load(int index, bool strict = false)
        {
            Init();
            bool ok = true;
            try
            {
                if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                {
                    if (index >= 0 && index < MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count)
                    {
                        DateTime start = DateTime.Now;
                        string midiname = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[index];
                        TextAsset mididata = Resources.Load<TextAsset>(Path.Combine(MidiPlayerGlobal.MidiFilesDB, midiname));
                        midifile = new MidiFile(mididata.bytes, strict);
                        if (midifile != null)
                        {
                            List<TrackMidiEvent> tmEvents = ConvertFromMidiFileToTrackMidiEvent();
                            if (tmEvents != null)
                            {
                                AnalyseTrackMidiEvent(tmEvents);
                                MPTK_MidiEvents = tmEvents;
                            }
                        }
                        MPTK_LoadTime = (float)(DateTime.Now - start).TotalMilliseconds;
                        if (LogEvents) Debug.Log($"MPTK_LoadTime:\t\t{MPTK_LoadTime} millisecond");
                    }
                    else
                    {
                        Debug.LogWarningFormat("MidiLoad - index {0} out of range ", index);
                        ok = false;
                    }
                }
                else
                {
                    Debug.LogWarningFormat("MidiLoad - index:{0} - {1}", index, MidiPlayerGlobal.ErrorNoMidiFile);
                    ok = false;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
                ok = false;
            }
            return ok;
        }

        /// <summary>
        /// Load Midi from a local file
        /// </summary>
        /// <param name="filename">Midi path and filename to load</param>
        /// <param name="strict">if true struct respect of the midi norm is checked</param>
        /// <returns></returns>
        public bool MPTK_LoadFile(string filename, bool strict = false)
        {
            bool ok = true;
            try
            {
                using (Stream sfFile = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    byte[] data = new byte[sfFile.Length];
                    sfFile.Read(data, 0, (int)sfFile.Length);
                    ok = MPTK_Load(data);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
                ok = false;
            }
            return ok;
        }

        /// <summary>
        /// Load Midi from an array of bytes
        /// </summary>
        /// <param name="datamidi">byte arry midi</param>
        /// <param name="strict">If true will error on non-paired note events, default:false</param>
        /// <returns>true if loaded</returns>
        public bool MPTK_Load(byte[] datamidi, bool strict = false)
        {
            Init();
            bool ok = true;
            try
            {
                DateTime start = DateTime.Now;
                midifile = new MidiFile(datamidi, strict);
                if (midifile != null)
                {
                    List<TrackMidiEvent> tmEvents = ConvertFromMidiFileToTrackMidiEvent();
                    if (tmEvents != null)
                    {
                        AnalyseTrackMidiEvent(tmEvents);
                        MPTK_MidiEvents = tmEvents;
                    }
                }
                else
                    ok = false;
                MPTK_LoadTime = (float)(DateTime.Now - start).TotalMilliseconds;
                if (LogEvents) Debug.Log($"MPTK_LoadTime:\t\t{MPTK_LoadTime} millisecond");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
                ok = false;
            }
            return ok;
        }

        /// <summary>
        /// Load Midi from a Midi file from Unity resources. The Midi file must be present in Unity MidiDB ressource folder.
        /// @code
        /// public MidiLoad MidiLoaded;
        /// // .....
        /// MidiLoaded = new MidiLoad();
        /// MidiLoaded.MPTK_Load("Beattles - Michelle")
        /// Debug.Log("Duration:" + MidiLoaded.MPTK_Duration);
        /// @endcode
        /// </summary>
        /// <param name="midiname">Midi file name without path and extension</param>
        /// <param name="strict">if true, check strict compliance with the Midi norm</param>
        /// <returns>true if loaded</returns>
        public bool MPTK_Load(string midiname, bool strict = false)
        {
            try
            {
                //TextAsset mididata = Resources.Load<TextAsset>(MidiPlayerGlobal.MidiFilesDB + "/" + midiname);
                TextAsset mididata = Resources.Load<TextAsset>(Path.Combine(MidiPlayerGlobal.MidiFilesDB, midiname));
                if (mididata != null && mididata.bytes != null && mididata.bytes.Length > 0)
                    return MPTK_Load(mididata.bytes, strict);
                else
                    Debug.LogWarningFormat("Midi {0} not loaded from folder {1}", midiname, MidiPlayerGlobal.MidiFilesDB);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return false;
        }

        /// <summary>
        /// Read the list of midi events available in the Midi from a ticks position to an end position.
        /// </summary>
        /// <param name="fromTicks">ticks start, default 0</param>
        /// <param name="toTicks">ticks end, default end of Midi file</param>
        /// <returns></returns>
        public List<MPTKEvent> MPTK_ReadMidiEvents(long fromTicks = 0, long toTicks = long.MaxValue)
        {
            List<MPTKEvent> midievents = new List<MPTKEvent>();
            try
            {
                if (midifile != null)
                {
                    foreach (TrackMidiEvent trackEvent in MPTK_MidiEvents)
                    {
                        if (Quantization != 0)
                            trackEvent.AbsoluteQuantize = ((trackEvent.Event.AbsoluteTime + Quantization / 2) / Quantization) * Quantization;
                        else
                            trackEvent.AbsoluteQuantize = trackEvent.Event.AbsoluteTime;

                        //Debug.Log("ReadMidiEvents - timeFromStartMS:" + Convert.ToInt32(timeFromStartMS) + " LastTimeFromStartMS:" + Convert.ToInt32(LastTimeFromStartMS) + " CurrentPulse:" + CurrentPulse + " AbsoluteQuantize:" + trackEvent.AbsoluteQuantize);

                        if (trackEvent.AbsoluteQuantize >= fromTicks && trackEvent.AbsoluteQuantize <= toTicks)
                        {
                            MPTKEvent mptkEvent = ConvertTrackEventToMPTKEvent(trackEvent);
                            if (mptkEvent != null)
                                midievents.Add(mptkEvent);
                        }
                        MPTK_TickCurrent = trackEvent.AbsoluteQuantize;
                        MPTK_TickLast = trackEvent.AbsoluteQuantize;

                        if (trackEvent.AbsoluteQuantize > toTicks)
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return midievents;
        }

        /// <summary>
        /// Convert the tick duration to a real time duration in millisecond regarding the current tempo.
        /// </summary>
        /// <param name="tick">duration in ticks</param>
        /// <returns>duration in milliseconds</returns>
        public double MPTK_ConvertTickToTime(long tick)
        {
            return tick * MPTK_PulseLenght;
        }

        /// <summary>
        /// Convert a real time duration in millisecond to a number of tick regarding the current tempo.
        /// </summary>
        /// <param name="time">duration in milliseconds</param>
        /// <returns>duration in ticks</returns>
        public long MPTK_ConvertTimeToTick(double time)
        {
            if (MPTK_PulseLenght != 0d)
                return Convert.ToInt64((time / MPTK_PulseLenght) + 0.5d);
            else
                return 0;
        }

        /// <summary>
        /// Search for a Midi event from a time position expressed in millisecond.\n
        /// So time=12.3 and time=12.9 will find the same event.\n
        /// </summary>
        /// <param name="time">position in milliseconds</param>
        /// <returns>MPTKEvent or null</returns>
        public MPTKEvent MPTK_SearchEventFromTime(double time)
        {
            if (time < 0d || time >= MPTK_DurationMS)
                return null;
            else
            {
                //foreach (TrackMidiEvent trackEvent in MPTK_MidiEvents)
                //    if (trackEvent.RealTime >= time)
                //    {
                //        MPTKEvent mptkEvent = ConvertTrackEventToMPTKEvent(trackEvent);
                //        if (mptkEvent != null)
                //            return mptkEvent;
                //    }
                int low = 0;
                int high = MPTK_MidiEvents.Count - 1;
                int found = -1;
                long lTime = (long)time;
                while (found < 0)
                {
                    int middle = low + high / 2;
                    long middleTime = (long)MPTK_MidiEvents[middle].RealTime;
                    if (lTime < middleTime)
                        high = middle;
                    else if (lTime > middleTime)
                        low = middle;
                    else
                        found = middle;
                    if (low == high)
                        found = low;
                }

                if (found >= 0)
                {
                    MPTKEvent mptkEvent = ConvertTrackEventToMPTKEvent(MPTK_MidiEvents[found]);
                    if (mptkEvent != null)
                        return mptkEvent;
                }

            }
            return null;
        }

        /// <summary>
        /// Search a tick position in the current midi from a position in millisecond.\n
        /// Warning: this method loop on the whole midi to find the position. \n
        /// Could be CPU costly but this method take care of the tempo change in the Midi.\n
        /// Use MPTK_ConvertTimeToTick if there is no tempo change in the midi. 
        /// </summary>
        /// <param name="time">position in milliseconds</param>
        /// <returns>position in ticks</returns>
        public long MPTK_SearchTickFromTime(double time)
        {
            if (time <= 0d)
                return 0L;
            else if (time >= MPTK_DurationMS)
                return MPTK_TickLast;
            else
            {
                foreach (TrackMidiEvent trackEvent in MPTK_MidiEvents)
                    if (trackEvent.RealTime >= time)
                        return trackEvent.Event.AbsoluteTime;
            }
            return 0;
        }

        // No doc until end of file
        //! @cond NODOC

        /// <summary>
        /// Build OS path to the midi file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        static public string BuildOSPath(string filename)
        {
            try
            {
                string pathMidiFolder = Path.Combine(Application.dataPath, MidiPlayerGlobal.PathToMidiFile);
                string pathfilename = Path.Combine(pathMidiFolder, filename + MidiPlayerGlobal.ExtensionMidiFile);
                return pathfilename;
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return null;
        }

        class TempoMap
        {
            public int track;
            public long fromTick;
            public double cumul;
            public double ratio;
            public int MicrosecondsPerQuarterNote;
        }
        List<TempoMap> tempoMap = new List<TempoMap>();

        private List<TrackMidiEvent> ConvertFromMidiFileToTrackMidiEvent()
        {
            try
            {
                MPTK_TickCurrent = 0;
                ClearMetaText();
                int indexTracks;

                try
                {
                    tempoMap = new List<TempoMap>();

                    indexTracks = 0;
                    foreach (IList<MidiEvent> track in midifile.Events)
                    {
                        foreach (MidiEvent nAudioMidievent in track)
                        {
                            if (nAudioMidievent.CommandCode == MidiCommandCode.MetaEvent)
                            {
                                MetaEvent meta = (MetaEvent)nAudioMidievent;

                                if (meta.MetaEventType == MetaEventType.SetTempo)
                                {
                                    if (!MPTK_EnableChangeTempo && tempoMap.Count >= 1)
                                    {
                                        // Keep only the first tempo change
                                    }
                                    else
                                    {
                                        double ratio = (double)((TempoEvent)meta).MicrosecondsPerQuarterNote / (double)midifile.DeltaTicksPerQuarterNote / 1000d;
                                        double cumul = 0;
                                        if (tempoMap.Count > 0)
                                        {
                                            cumul = tempoMap[tempoMap.Count - 1].cumul + meta.DeltaTime * tempoMap[tempoMap.Count - 1].ratio;
                                        }

                                        tempoMap.Add(new TempoMap()
                                        {
                                            track = indexTracks,
                                            fromTick = nAudioMidievent.AbsoluteTime,
                                            ratio = ratio,
                                            cumul = cumul,
                                            MicrosecondsPerQuarterNote = ((TempoEvent)meta).MicrosecondsPerQuarterNote
                                        });
                                    }
                                }
                                else if (meta.MetaEventType == MetaEventType.TimeSignature)
                                    AnalyzeTimeSignature(meta);
                                else if (meta.MetaEventType == MetaEventType.KeySignature)
                                    AnalyzeKeySignature(meta);
                            }
                        }
                        indexTracks++;
                    }

                    if (tempoMap.Count == 0)
                        // No tempo defined, set to 120 by default (500 000 microseconds)
                        tempoMap.Add(new TempoMap()
                        {
                            track = 0,
                            fromTick = 0,
                            MicrosecondsPerQuarterNote = MPTK_BPM2MPQN(120),
                            ratio = (double)MPTK_BPM2MPQN(120) / (double)midifile.DeltaTicksPerQuarterNote / 1000d,

                        });
                    else
                    if (tempoMap.Count > 1)
                        tempoMap = tempoMap.OrderBy(o => o.fromTick).ToList();

                    //foreach (TempoMap tempo in tempoMap) Debug.Log($"track:{tempo.track} fromTick:{tempo.fromTick} ratio:{tempo.ratio} cumul:{tempo.cumul}");

                    // Set initial tempo 
                    int indexTempo = 0;
                    MPTK_MicrosecondsPerQuarterNote = tempoMap[indexTempo].MicrosecondsPerQuarterNote;
                    MPTK_InitialTempo = MPTK_MPQN2BPM(MPTK_MicrosecondsPerQuarterNote);
                    fluid_player_set_midi_tempo(MPTK_MicrosecondsPerQuarterNote);

                    indexTracks = 0;
                    List<TrackMidiEvent> tmEvents = new List<TrackMidiEvent>();
                    foreach (IList<MidiEvent> nAudioTrack in midifile.Events)
                    {
                        indexTempo = 0;
                        foreach (MidiEvent nAudioMidievent in nAudioTrack)
                        {
                            // Check next entry tempo
                            while (indexTempo < tempoMap.Count - 1 && tempoMap[indexTempo + 1].fromTick < nAudioMidievent.AbsoluteTime)
                                indexTempo++;

                            double newTime = tempoMap[indexTempo].cumul + (nAudioMidievent.AbsoluteTime - tempoMap[indexTempo].fromTick) * tempoMap[indexTempo].ratio;
                            //Debug.Log($"Calcul real time Track:{indexTracks}  CommandCode:{nAudioMidievent.CommandCode} indexTempo:{indexTempo} AbsoluteTime:{nAudioMidievent.AbsoluteTime} DeltaTime:{nAudioMidievent.DeltaTime} RealTime:{newTime / 1000f:F2} Ratio:{(float)tempoMap[indexTempo].MicrosecondsPerQuarterNote / (float)midifile.DeltaTicksPerQuarterNote / 1000f}");

                            // V2.89.0 able to remove End Track event
                            if (nAudioMidievent.CommandCode == MidiCommandCode.MetaEvent && ((MetaEvent)nAudioMidievent).MetaEventType == MetaEventType.EndTrack && !MPTK_KeepEndTrack)
                            {
                                //Debug.Log($"Dont keep EndTrack:{indexTracks}  CommandCode:{nAudioMidievent.CommandCode} indexTempo:{indexTempo} AbsoluteTime:{nAudioMidievent.AbsoluteTime} DeltaTime:{nAudioMidievent.DeltaTime} RealTime:{newTime / 1000f:F2} Ratio:{(float)tempoMap[indexTempo].MicrosecondsPerQuarterNote / (float)midifile.DeltaTicksPerQuarterNote / 1000f}");
                            }
                            else
                            {
                                tmEvents.Add(new TrackMidiEvent()
                                {
                                    IndexTrack = indexTracks, // Start from 0
                                    RealTime = (float)newTime,
                                    Event = nAudioMidievent//.Clone()
                                });
                            }
                        }
                        indexTracks++; // will be the count of tracks
                    }

                    if (tmEvents.Count == 0)
                        throw new Exception("No midi event found");

                    //DebugMidiSorted(events);

                    return tmEvents.OrderBy(o => o.Event.AbsoluteTime).ToList();

                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return null;
        }

        private void AnalyseTrackMidiEvent(List<TrackMidiEvent> tmEvents)
        {
            MPTK_TickFirstNote = -1;
            MPTK_TickLastNote = -1;
            MPTK_TrackCount = 0;
            for (int indexEvent = 0; indexEvent < tmEvents.Count; indexEvent++)
            {
                TrackMidiEvent midievent = tmEvents[indexEvent];
                midievent.IndexEvent = indexEvent;
                if (MPTK_TrackCount <= midievent.IndexTrack)
                    MPTK_TrackCount = midievent.IndexTrack + 1;
                if (midievent.Event.CommandCode == MidiCommandCode.NoteOn)
                {
                    if (MPTK_TickFirstNote == -1)
                    {
                        MPTK_TickFirstNote = midievent.Event.AbsoluteTime;
                        MPTK_PositionFirstNote = midievent.RealTime;
                    }
                    MPTK_TickLastNote = midievent.Event.AbsoluteTime;
                    MPTK_PositionLastNote = midievent.RealTime;
                }

                //if (MPTK_TickFirstNote == -1 && midievent.Event.CommandCode == MidiCommandCode.NoteOn)
                //{
                //    MPTK_TickFirstNote = midievent.Event.AbsoluteTime;
                //    MPTK_PositionFirstNote = midievent.RealTime;
                //    //break;
                //}
            }

            TrackMidiEvent lastEvent = tmEvents[tmEvents.Count - 1];
            MPTK_TickLast = lastEvent.Event.AbsoluteTime;
            MPTK_DurationMS = lastEvent.RealTime;
            MPTK_Duration = TimeSpan.FromMilliseconds(MPTK_DurationMS);
            MPTK_DeltaTicksPerQuarterNote = midifile.DeltaTicksPerQuarterNote;

            if (LogEvents)
            {
                Debug.Log($"MPTK_DeltaTicksPerQuarterNote:\t{MPTK_DeltaTicksPerQuarterNote} ticks");
                Debug.Log($"MPTK_TrackCount:\t\t{MPTK_TrackCount}");
                Debug.Log($"MPTK_InitialTempo:\t\t{MPTK_InitialTempo} bpm\t\tTempo change:\t\t{tempoMap.Count}");
                Debug.Log($"MPTK_DurationMS:\t\t{MPTK_DurationMS / 1000f} seconds \tMPTK_Duration:\t\t{MPTK_Duration}");
                Debug.Log($"MPTK_TickFirstNote:\t\t{MPTK_TickFirstNote} ticks \t\tMPTK_PositionFirstNote:\t{MPTK_PositionFirstNote / 1000f:F2} second {TimeSpan.FromMilliseconds(MPTK_PositionFirstNote)} ");
                Debug.Log($"MPTK_TickLastNote:\t\t{MPTK_TickLastNote} ticks \t\tMPTK_PositionLastNote:\t{MPTK_PositionLastNote / 1000f:F2} second {TimeSpan.FromMilliseconds(MPTK_PositionLastNote)}");
                Debug.Log($"MPTK_TickLast:\t\t{MPTK_TickLast} ticks");
                Debug.Log($"MPTK_MidiEvents:\t\t{tmEvents.Count} events");
            }

        }
        public void ClearMetaText()
        {
            SequenceTrackName = "";
            ProgramName = "";
            TrackInstrumentName = "";
            TextEvent = "";
            Copyright = "";
        }

        /// <summary>
        /// Convert BPM to duration of a quarter in microsecond
        /// </summary>
        /// <param name="bpm">m</param>
        /// <returns></returns>
        public static int MPTK_BPM2MPQN(int bpm)
        {
            return 60000000 / bpm;
        }

        /// <summary>
        /// Convert duration of a quarter in microsecond to Beats Per Minute
        /// </summary>
        /// <param name="microsecondsPerQuaterNote"></param>
        /// <returns></returns>
        public static int MPTK_MPQN2BPM(int microsecondsPerQuaterNote)
        {
            return 60000000 / microsecondsPerQuaterNote;
        }

        /// <summary>
        /// Change speed to play. 1=normal speed
        /// </summary>
        /// <param name="speed"></param>
        public void ChangeSpeed(float speed)
        {
            try
            {
                if (speed > 0)
                {
                    //Debug.Log($"ChangeSpeed from {Speed} to {speed}");
                    //double lastSpeed = Speed;
                    Speed = speed;
                    fluid_player_set_midi_tempo(miditempo);
                    // V2.88 : duration is no longer linked to speed
                    //MPTK_DurationMS = (float)((double)MPTK_DurationMS * lastSpeed / Speed);
                    //MPTK_Duration = TimeSpan.FromMilliseconds(MPTK_DurationMS);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        public void ChangeQuantization(int level = 4)
        {
            try
            {
                if (level <= 0)
                    Quantization = 0;
                else
                    Quantization = midifile.DeltaTicksPerQuarterNote / level;
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        public void StartMidi()
        {
            // Debug.Log("StartMidi core");
            //begin_msec = 0;
            start_msec = 0;
            start_ticks = 0;
            cur_ticks = 0;
            seek_ticks = -1;
            cur_msec = 0;
            next_event = 0;
            EndMidiEvent = false;
        }

        /**
         * Set the tempo of a MIDI player.
         * @param player MIDI player instance
         * @param tempo Tempo to set playback speed from param1
         * @return Always returns 0
         *
         */
        private void fluid_player_set_midi_tempo(int tempo)
        {
            if (midifile == null || midifile.DeltaTicksPerQuarterNote <= 0)
                Debug.LogWarning("Can't set tempo while midi is not loaded");
            else
            {
                miditempo = tempo;
                //Debug.Log($"Avant tempo change tempo:{tempo} pulse:{MPTK_PulseLenght} start:{start_msec} ms start:{start_ticks} ticks");
                MPTK_PulseLenght = (double)tempo / midifile.DeltaTicksPerQuarterNote / 1000f / Speed; /* in milliseconds */
                //Debug.Log($"Apres tempo change tempo:{tempo} pulse:{MPTK_PulseLenght} start:{start_msec} ms start:{start_ticks} ticks");
                start_msec = cur_msec;
                start_ticks = cur_ticks;
            }
        }

        /**
        * Seek in the currently playing file.
        * @param player MIDI player instance
        * @param ticks the position to seek to in the current file
        * @return #FLUID_FAILED if ticks is negative or after the latest tick of the file,
        *   #FLUID_OK otherwise
        * @since 2.0.0
        *
        * The actual seek is performed during the player_callback.
        */
        public void fluid_player_seek(int ticks)
        {
            //Debug.Log("fluid_player_seek:" + ticks);
            // Include tick un parameter
            if (ticks > 0)
                seek_ticks = ticks - 1;
            else
                seek_ticks = 0;
        }


        /**
      * Set the tempo of a MIDI player in beats per minute.
      * @param player MIDI player instance
      * @param bpm Tempo in beats per minute
      * @return Always returns #FLUID_OK
      */
        private void fluid_player_set_bpm(int bpm)
        {
            fluid_player_set_midi_tempo(60000000 / bpm);
        }

        /**
         * Get the tempo of a MIDI player in beats per minute.
         * @param player MIDI player instance
         * @return MIDI player tempo in BPM
         * @since 1.1.7
         */

        private int fluid_player_get_bpm()
        {
            return 60000000 / miditempo;
        }


        // see previous version for comment and debugide
        public List<MPTKEvent> fluid_player_callback(int msec, int idSession)
        {
            List<MPTKEvent> midiEvents = null;

            try
            {
                if (midifile != null && next_event >= 0)
                {
                    cur_msec = msec;
                    cur_ticks = start_ticks + (int)(((double)(cur_msec - start_msec) / MPTK_PulseLenght) + 0.5d);
                    int ticks = cur_ticks;

                    if (seek_ticks >= 0)
                        ticks = seek_ticks;
                    if (MPTK_TickCurrent > ticks)
                        next_event = 0;

                    while (true)
                    {
                        if (next_event >= MPTK_MidiEvents.Count)
                        {
                            next_event = -1;
                            break;
                        }

                        TrackMidiEvent trackEvent = MPTK_MidiEvents[next_event];
                        trackEvent.AbsoluteQuantize = Quantization != 0 ? ((trackEvent.Event.AbsoluteTime + Quantization / 2) / Quantization) * Quantization : trackEvent.Event.AbsoluteTime;

                        if (trackEvent.AbsoluteQuantize >= ticks) // V2.872 replace > by >=
                        {
                            break;
                        }
                        if (seek_ticks >= 0 &&
                           (trackEvent.Event.CommandCode == MidiCommandCode.NoteOn ||
                            trackEvent.Event.CommandCode == MidiCommandCode.NoteOff ||
                            (trackEvent.Event.CommandCode == MidiCommandCode.MetaEvent && ((MetaEvent)trackEvent.Event).MetaEventType != MetaEventType.SetTempo)))
                        {
                        }
                        else
                        {
                            // This midi event list will be used in another threads, it's mandatory to create a new one at each iteration
                            if (midiEvents == null) midiEvents = new List<MPTKEvent>();
                            MPTKEvent mptkEvent = ConvertTrackEventToMPTKEvent(trackEvent);
                            if (mptkEvent != null)
                            {
                                mptkEvent.IdSession = idSession;
                                midiEvents.Add(mptkEvent);
                            }
                        }
                        next_event++;
                    }
                    if (seek_ticks >= 0)
                    {
                        start_ticks = seek_ticks;
                        cur_ticks = seek_ticks;
                        start_msec = msec;
                        seek_ticks = -1;
                    }
                    if (next_event < 0)
                        EndMidiEvent = true;
                }

                if (midiEvents != null && midiEvents.Count > 0)
                {
                    MPTK_LastEventPlayed = midiEvents[midiEvents.Count - 1];
                    //Debug.Log($"{MPTK_LastEventPlayed.Command} {MPTK_LastEventPlayed.RealTime}");
                    MPTK_TickCurrent = MPTK_LastEventPlayed.Tick;// trackEvent.Event.AbsoluteTime;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }

            return midiEvents;
        }

        private MPTKEvent ConvertTrackEventToMPTKEvent(TrackMidiEvent trackEvent)
        {
            MPTKEvent mptkEvent = null;
            switch (trackEvent.Event.CommandCode)
            {
                case MidiCommandCode.NoteOn:
                    //if (((NoteOnEvent)trackEvent.Event).OffEvent != null)
                    {

                        NoteOnEvent noteon = (NoteOnEvent)trackEvent.Event;
                        //Debug.Log(string.Format("Track:{0} NoteNumber:{1,3:000} AbsoluteTime:{2,6:000000} NoteLength:{3,6:000000} OffDeltaTime:{4,6:000000} ", track, noteon.NoteNumber, noteon.AbsoluteTime, noteon.NoteLength, noteon.OffEvent.DeltaTime));
                        if (noteon.OffEvent != null)
                        // V2.88   if (noteon.Velocity != 0)
                        {
                            mptkEvent = new MPTKEvent()
                            {
                                Track = trackEvent.IndexTrack,
                                Index = trackEvent.IndexEvent,
                                Tick = trackEvent.AbsoluteQuantize,
                                RealTime = trackEvent.RealTime,
                                Command = MPTKCommand.NoteOn,
                                Value = noteon.NoteNumber,
                                Channel = trackEvent.Event.Channel - 1,
                                Velocity = noteon.Velocity,
                                Duration = noteon.OffEvent != null ? Convert.ToInt64(noteon.NoteLength * MPTK_PulseLenght) : -1,
                                Length = noteon.NoteLength,
                            };
                            if (LogEvents && seek_ticks < 0)
                            {
                                Debug.Log(BuildInfoTrack(trackEvent) + $"NoteOn {mptkEvent.Value:000} {noteon.NoteName,-4}\tDuration:{noteon.NoteLength,5} ticks {mptkEvent.Duration:000} ms {NoteLength(mptkEvent)}\tVelocity:{noteon.Velocity,3}");
                            }
                        }
                        else // It's a noteoff
                        {
                            // V2.88 if (KeepNoteOff)
                            if (KeepNoteOff)// && noteon.OffEvent == null && noteon.NoteLength!=0)
                            {
                                mptkEvent = new MPTKEvent()
                                {
                                    Track = trackEvent.IndexTrack,
                                    Index = trackEvent.IndexEvent,
                                    Tick = trackEvent.AbsoluteQuantize,
                                    RealTime = trackEvent.RealTime,
                                    Command = MPTKCommand.NoteOff,
                                    Value = noteon.NoteNumber,
                                    Channel = trackEvent.Event.Channel - 1,
                                    Velocity = noteon.Velocity,
                                    Duration = Convert.ToInt64(noteon.NoteLength * MPTK_PulseLenght),
                                    Length = noteon.NoteLength,
                                };

                                if (LogEvents && seek_ticks < 0)
                                {
                                    Debug.Log(BuildInfoTrack(trackEvent) + $"NoteOff {mptkEvent.Value:000}\t{noteon.NoteName,-4}\tFrom NoteOn");
                                }
                            }
                        }
                    }
                    break;

                case MidiCommandCode.NoteOff:
                    if (KeepNoteOff)
                    {
                        NoteEvent noteoff = (NoteEvent)trackEvent.Event;
                        //Debug.Log(string.Format("Track:{0} NoteNumber:{1,3:000} AbsoluteTime:{2,6:000000} NoteLength:{3,6:000000} OffDeltaTime:{4,6:000000} ", track, noteon.NoteNumber, noteon.AbsoluteTime, noteon.NoteLength, noteon.OffEvent.DeltaTime));
                        mptkEvent = new MPTKEvent()
                        {
                            Track = trackEvent.IndexTrack,
                            Index = trackEvent.IndexEvent,
                            Tick = trackEvent.AbsoluteQuantize,
                            RealTime = trackEvent.RealTime,
                            Command = MPTKCommand.NoteOff,
                            Value = noteoff.NoteNumber,
                            Channel = trackEvent.Event.Channel - 1,
                            Velocity = noteoff.Velocity,
                            Duration = 0,
                            Length = 0,
                        };

                        if (LogEvents && seek_ticks < 0)
                        {
                            Debug.Log(BuildInfoTrack(trackEvent) + $"NoteOff {mptkEvent.Value:000}\t{noteoff.NoteName,-4}\tFrom file");
                        }
                    }
                    break;

                case MidiCommandCode.PitchWheelChange:
                    PitchWheelChangeEvent pitch = (PitchWheelChangeEvent)trackEvent.Event;
                    mptkEvent = new MPTKEvent()
                    {
                        Track = trackEvent.IndexTrack,
                        Index = trackEvent.IndexEvent,
                        Tick = trackEvent.AbsoluteQuantize,
                        RealTime = trackEvent.RealTime,
                        Command = MPTKCommand.PitchWheelChange,
                        Channel = trackEvent.Event.Channel - 1,
                        Value = pitch.Pitch,  // Pitch Wheel Value 0 is minimum, 0x2000 (8192) is default, 0x3FFF (16383) is maximum
                    };
                    if (LogEvents && seek_ticks < 0)
                        Debug.Log(BuildInfoTrack(trackEvent) + string.Format("PitchWheelChange {0}", pitch.Pitch));
                    break;

                case MidiCommandCode.ControlChange:
                    ControlChangeEvent controlchange = (ControlChangeEvent)trackEvent.Event;
                    mptkEvent = new MPTKEvent()
                    {
                        Track = trackEvent.IndexTrack,
                        Index = trackEvent.IndexEvent,
                        Tick = trackEvent.AbsoluteQuantize,
                        RealTime = trackEvent.RealTime,
                        Command = MPTKCommand.ControlChange,
                        Channel = trackEvent.Event.Channel - 1,
                        Controller = (MPTKController)controlchange.Controller,
                        Value = controlchange.ControllerValue,

                    };

                    //if ((MPTKController)controlchange.Controller != MPTKController.Sustain)

                    // Other midi event
                    if (LogEvents && seek_ticks < 0)
                        Debug.Log(BuildInfoTrack(trackEvent) + $"Control 0x{mptkEvent.Controller:X}/{mptkEvent.Controller} {mptkEvent.Value}");

                    break;

                case MidiCommandCode.PatchChange:
                    PatchChangeEvent change = (PatchChangeEvent)trackEvent.Event;
                    mptkEvent = new MPTKEvent()
                    {
                        Track = trackEvent.IndexTrack,
                        Index = trackEvent.IndexEvent,
                        Tick = trackEvent.AbsoluteQuantize,
                        RealTime = trackEvent.RealTime,
                        Command = MPTKCommand.PatchChange,
                        Channel = trackEvent.Event.Channel - 1,
                        Value = change.Patch,
                    };
                    if (LogEvents && seek_ticks < 0)
                        Debug.Log(BuildInfoTrack(trackEvent) + string.Format("Patch   {0,3:000} {1}", change.Patch, PatchChangeEvent.GetPatchName(change.Patch)));
                    break;

                case MidiCommandCode.MetaEvent:
                    MetaEvent meta = (MetaEvent)trackEvent.Event;
                    mptkEvent = new MPTKEvent()
                    {
                        Track = trackEvent.IndexTrack,
                        Index = trackEvent.IndexEvent,
                        Tick = trackEvent.AbsoluteQuantize,
                        RealTime = trackEvent.RealTime,
                        Command = MPTKCommand.MetaEvent,
                        Channel = trackEvent.Event.Channel - 1,
                        Meta = (MPTKMeta)meta.MetaEventType,
                    };

                    switch (meta.MetaEventType)
                    {
                        case MetaEventType.EndTrack:
                            mptkEvent.Info = "End Track";
                            break;

                        case MetaEventType.KeySignature:
                            AnalyzeKeySignature(meta, trackEvent, mptkEvent);
                            break;

                        case MetaEventType.TimeSignature:
                            AnalyzeTimeSignature(meta, trackEvent, mptkEvent);
                            break;

                        case MetaEventType.SetTempo:
                            if (MPTK_EnableChangeTempo)
                            {
                                TempoEvent tempo = (TempoEvent)meta;
                                // Tempo change will be done in MidiFilePlayer
                                mptkEvent.Duration = (long)tempo.Tempo;
                                mptkEvent.Value = tempo.MicrosecondsPerQuarterNote;
                                MPTK_MicrosecondsPerQuarterNote = tempo.MicrosecondsPerQuarterNote;
                                fluid_player_set_midi_tempo(tempo.MicrosecondsPerQuarterNote);

                                // Force exit loop
                                if (LogEvents && seek_ticks < 0)
                                    Debug.Log(BuildInfoTrack(trackEvent) + string.Format("Meta     {0,-15} 'Tempo:{1:F2} MicrosecondsPerQuarterNote:{2}'", meta.MetaEventType, tempo.Tempo, tempo.MicrosecondsPerQuarterNote));
                            }
                            break;

                        case MetaEventType.SequenceTrackName:
                            mptkEvent.Info = ((TextEvent)meta).Text;
                            if (!string.IsNullOrEmpty(SequenceTrackName)) SequenceTrackName += "\n";
                            SequenceTrackName += string.Format("T{0,2:00} {1}", trackEvent.IndexTrack, mptkEvent.Info);
                            break;

                        case MetaEventType.ProgramName:
                            mptkEvent.Info = ((TextEvent)meta).Text;
                            ProgramName += mptkEvent.Info + " ";
                            break;

                        case MetaEventType.TrackInstrumentName:
                            mptkEvent.Info = ((TextEvent)meta).Text;
                            if (!string.IsNullOrEmpty(TrackInstrumentName)) TrackInstrumentName += "\n";
                            TrackInstrumentName += string.Format("T{0,2:00} {1}", trackEvent.IndexTrack, mptkEvent.Info);
                            break;

                        case MetaEventType.TextEvent:
                            mptkEvent.Info = ((TextEvent)meta).Text;
                            TextEvent += mptkEvent.Info + " ";
                            break;

                        case MetaEventType.Copyright:
                            mptkEvent.Info = ((TextEvent)meta).Text;
                            Copyright += mptkEvent.Info + " ";
                            break;

                        case MetaEventType.Lyric: // lyric
                            mptkEvent.Info = ((TextEvent)meta).Text;
                            TextEvent += mptkEvent.Info + " ";
                            break;

                        case MetaEventType.Marker: // marker
                            mptkEvent.Info = ((TextEvent)meta).Text;
                            TextEvent += mptkEvent.Info + " ";
                            break;

                        case MetaEventType.CuePoint: // cue point
                        case MetaEventType.DeviceName:
                            break;
                    }

                    if (LogEvents && !string.IsNullOrEmpty(mptkEvent.Info) && seek_ticks < 0)
                        Debug.Log(BuildInfoTrack(trackEvent) + string.Format("Meta     {0,-15} '{1}'", mptkEvent.Meta, mptkEvent.Info));

                    //Debug.Log(BuildInfoTrack(trackEvent) + string.Format("Meta {0} {1}", meta.MetaEventType, meta.ToString()));
                    break;

                default:
                    // Other midi event
                    if (LogEvents && seek_ticks < 0)
                        Debug.Log(BuildInfoTrack(trackEvent) + string.Format("Other    {0,-15} Not handle by MPTK", trackEvent.Event.CommandCode));
                    break;
            }
            return mptkEvent;
        }

        //private static string BuildNoteName(MPTKEvent midievent)
        //{
        //    return (midievent.Channel != 9) ?
        //        $"{HelperNoteLabel.LabelFromMidi(midievent.Value)}":
        //        "Drum";

        //    //String.Format("{0}{1}", NoteNames[midievent.Value % 12], midievent.Value / 12) :
        //}

        /// <summary>
        /// https://en.wikipedia.org/wiki/Note_value
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        public MPTKEvent.EnumLength NoteLength(MPTKEvent note)
        {
            if (midifile != null)
            {
                if (note.Length >= midifile.DeltaTicksPerQuarterNote * 4)
                    return MPTKEvent.EnumLength.Whole;
                else if (note.Length >= midifile.DeltaTicksPerQuarterNote * 2)
                    return MPTKEvent.EnumLength.Half;
                else if (note.Length >= midifile.DeltaTicksPerQuarterNote)
                    return MPTKEvent.EnumLength.Quarter;
                else if (note.Length >= midifile.DeltaTicksPerQuarterNote / 2)
                    return MPTKEvent.EnumLength.Eighth;
            }
            return MPTKEvent.EnumLength.Sixteenth;
        }

        // https://www.recordingblogs.com/wiki/midi-key-signature-meta-message
        private void AnalyzeKeySignature(MetaEvent meta, TrackMidiEvent trackEvent = null, MPTKEvent mptkEvent = null)
        {
            KeySignatureEvent keysig = (KeySignatureEvent)meta;
            MPTK_KeySigSharpsFlats = keysig.SharpsFlats;
            MPTK_KeySigMajorMinor = keysig.MajorMinor;

            if (mptkEvent != null)
            {
                mptkEvent.Value = MPTK_KeySigSharpsFlats;
                mptkEvent.Duration = MPTK_KeySigMajorMinor;
                mptkEvent.Info = $"SharpsFlats:{MPTK_KeySigSharpsFlats} MajorMinor:{MPTK_KeySigMajorMinor}";
            }

            //if (LogEvents && trackEvent != null)
            //    Debug.Log(BuildInfoTrack(trackEvent) + string.Format("Meta     {0,-15} Numerator:{1} Denominator:{2}", meta.MetaEventType, MPTK_KeySigSharpsFlats, MPTK_KeySigMajorMinor));
        }

        private void AnalyzeTimeSignature(MetaEvent meta, TrackMidiEvent trackEvent = null, MPTKEvent mptkEvent = null)
        {
            TimeSignatureEvent timesig = (TimeSignatureEvent)meta;

            // Numerator: counts the number of beats in a measure. 
            // For example a numerator of 4 means that each bar contains four beats. 
            MPTK_TimeSigNumerator = timesig.Numerator;
            // Denominator: number of quarter notes in a beat.0=ronde, 1=blanche, 2=quarter, 3=eighth, etc. 
            MPTK_TimeSigDenominator = timesig.Denominator;
            MPTK_NumberBeatsMeasure = timesig.Numerator;
            MPTK_NumberQuarterBeat = System.Convert.ToInt32(Mathf.Pow(2f, timesig.Denominator));
            MPTK_TicksInMetronomeClick = timesig.TicksInMetronomeClick;
            MPTK_No32ndNotesInQuarterNote = timesig.No32ndNotesInQuarterNote;

            if (mptkEvent != null)
            {
                mptkEvent.Value = timesig.Numerator;
                mptkEvent.Duration = timesig.Denominator;
                mptkEvent.Info = $"Numerator:{timesig.Numerator} Denominator:{timesig.Denominator}";
            }

            //if (LogEvents && trackEvent != null)
            //    Debug.Log(BuildInfoTrack(trackEvent) + string.Format("Meta     {0,-15} Numerator:{1} Denominator:{2}", meta.MetaEventType, timesig.Numerator, timesig.Denominator));
        }

        private string BuildInfoTrack(TrackMidiEvent e)
        {
#if !DEBUG_LOGEVENT
            return string.Format("[A:{0,5:00000} Q:{1,5:00000} P:{2,5:00000}] [T:{3,2:00} C:{4,2:00}] ",
                e.Event.AbsoluteTime, e.AbsoluteQuantize, cur_ticks, e.IndexTrack, e.Event.Channel);
#else
            //return string.Format("[A:{0,5:00000} D:{1,4:0000} Q:{2,5:00000} R:{6:F2} CurrentTick:{3,5:00000}] [Track:{4,2:00} Channel:{5,2:00}] ",
            //    e.Event.AbsoluteTime, e.Event.DeltaTime, e.AbsoluteQuantize, cur_ticks, e.IndexTrack, e.Event.Channel - 1 /*2.84*/,e.RealTime/1000f);
            // {DateTime.Now:h:mm:ss:fff} 
            // {cur_msec / 1000f:F2}
            //{e.IndexEvent:00000}
            //return "";
            return $"[I:{e.IndexEvent:00000} A:{e.Event.AbsoluteTime:00000} D:{e.Event.DeltaTime:0000} R:{e.RealTime / 1000f:F2}] [Track:{e.IndexTrack:00} Channel:{e.Event.Channel - 1,00}] ";
#endif
        }

        public void DebugTrack()
        {
            int itrck = 0;
            foreach (IList<MidiEvent> track in midifile.Events)
            {
                itrck++;
                foreach (MidiEvent midievent in track)
                {
                    string info = string.Format("Track:{0} Channel:{1,2:00} Command:{2} AbsoluteTime:{3:0000000} ", itrck, midievent.Channel - 1/*2.84*/, midievent.CommandCode, midievent.AbsoluteTime);
                    if (midievent.CommandCode == MidiCommandCode.NoteOn)
                    {
                        NoteOnEvent noteon = (NoteOnEvent)midievent;
                        if (noteon.OffEvent == null)
                            info += string.Format(" OffEvent null");
                        else
                            info += string.Format(" OffEvent.DeltaTimeChannel:{0:0000.00} ", noteon.OffEvent.DeltaTime);
                    }
                    Debug.Log(info);
                }
            }
        }
        public void DebugMidiSorted(List<TrackMidiEvent> midiSorted)
        {
            foreach (TrackMidiEvent midievent in midiSorted)
            {
                string info = string.Format("Track:{0} Channel:{1,2:00} Command:{2} AbsoluteTime:{3:0000000} DeltaTime:{4:0000000} ", midievent.IndexTrack, midievent.Event.Channel - 1/*2.84*/, midievent.Event.CommandCode, midievent.Event.AbsoluteTime, midievent.Event.DeltaTime);
                switch (midievent.Event.CommandCode)
                {
                    case MidiCommandCode.NoteOn:
                        NoteOnEvent noteon = (NoteOnEvent)midievent.Event;
                        if (noteon.Velocity == 0)
                            info += string.Format(" Velocity 0");
                        if (noteon.OffEvent == null)
                            info += string.Format(" OffEvent null");
                        else
                            info += string.Format(" OffEvent.DeltaTimeChannel:{0:0000.00} ", noteon.OffEvent.DeltaTime);
                        break;
                }
                Debug.Log(info);
            }
        }
        //! @endcond
    }
}

