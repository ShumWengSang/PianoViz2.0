using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
using System;
using System.Collections.ObjectModel;
using MEC;

namespace MidiPlayerTK
{
    /// <summary>
    /// Singleton class to manage all global features of MPTK.
    /// More information here: https://paxstellar.fr/midiplayerglobal/
    /// </summary>
    [HelpURL("https://paxstellar.fr/midiplayerglobal/")]
    public partial class MidiPlayerGlobal : MonoBehaviour
    {
        private static MidiPlayerGlobal instance;
        private MidiPlayerGlobal()
        {
        }
        public static MidiPlayerGlobal Instance { get { return instance; } }

        public const string SoundfontsDB = "SoundfontsDB";
        public const string MidiFilesDB = "MidiDB";
        public const string SongFilesDB = "SongDB";
        public const string ExtensionMidiFile = ".bytes";
        public const string ExtensionSoundFileDot = ".txt";
        public const string ExtensionSoundFileFileData = "_data.bytes";
        public const string FilenameMidiSet = "MidiSet";
        public const string PathSF2 = "SoundFont";
        public const string PathToWave = "wave";
        public const string ErrorNoSoundFont = "No SoundFont found. Choose a SoundFont from the Unity menu 'MPTK' (Alt-f) or load in live a SoundFont with the API [PRO].";
        public const string ErrorNoMidiFile = "No Midi defined. Add Midi file from the Unity menu 'MPTK' or Alt-m";
        public const string HelpDefSoundFont = "Add or Select SoundFont from the Unity menu 'MPTK' (Alt-f)";


        [HideInInspector]
        public string PathToResources;

        /// <summary>
        /// This path could change depending your project. Change the path before any actions in MPTK. 
        /// DEPRECATED, WILL BE REMOVED.
        /// </summary>
        //static public string MPTK_PathToResources = "MidiPlayer/Resources/";
        static public string MPTK_PathToResources //= "MPTK/Resources/";
        {
            get
            {
                string path;
                //if (Instance == null)
                //    Debug.Log("MPTK_PathToResources: Instance==null");
                //else if (string.IsNullOrEmpty(Instance.PathToResources))
                //    Debug.Log("MPTK_PathToResources: PathToResources==null");

                if (instance != null && !string.IsNullOrEmpty(instance.PathToResources))
                {
                    path = instance.PathToResources + "/Resources/";
                    //Debug.Log($"MPTK_PathToResources: {path}");
                }
                else
                {
                    path = "MidiPlayer/Resources/";
                    //Debug.Log($"MPTK_PathToResources (default): {path}");
                }
                return path;
            }
        }


        // Initialized with InitPath()
        static public string PathToSoundfonts;
        static public string PathToMidiFile;
        static public string PathToMidiSet;


        /// <summary>
        /// Loading time for the current SoundFont
        /// </summary>
        public static TimeSpan MPTK_TimeToLoadSoundFont
        {
            get
            {
                return timeToLoadSoundFont;
            }
        }

        /// <summary>
        /// Loading time for the samples 
        /// </summary>
        public static TimeSpan MPTK_TimeToLoadWave
        {
            get
            {
                return timeToLoadWave;
            }
        }

        /// <summary>
        /// Count of preset loaded
        /// </summary>
        public static int MPTK_CountPresetLoaded
        {
            get
            {
                int count = 0;
                if (ImSFCurrent != null)
                {
                    if (ImSFCurrent.DefaultBankNumber >= 0 && ImSFCurrent.DefaultBankNumber < ImSFCurrent.Banks.Length)
                        count = ImSFCurrent.Banks[ImSFCurrent.DefaultBankNumber].PatchCount;
                    if (ImSFCurrent.DrumKitBankNumber >= 0 && ImSFCurrent.DrumKitBankNumber < ImSFCurrent.Banks.Length)
                        count += ImSFCurrent.Banks[ImSFCurrent.DrumKitBankNumber].PatchCount;
                }
                return count;
            }
        }

        /// <summary>
        /// Number of samples loaded
        /// </summary>
        public static int MPTK_CountWaveLoaded;

        /// <summary>
        /// If true load soundfont when startup
        /// </summary>
        public static bool MPTK_LoadSoundFontAtStartup
        {
            get { return instance != null ? instance.LoadSoundFontAtStartup : false; }
            set
            {
                if (instance != null)
                    instance.LoadSoundFontAtStartup = value;
                else
                    Debug.LogWarning("MPTK_LoadWaveAtStartup: no MidiPlayerGlobal instance found");
            }
        }

        /// <summary>
        /// If true load all waves when application is started else load when need when playing (default)
        /// </summary>
        public static bool MPTK_LoadWaveAtStartup
        {
            get { return instance != null ? instance.LoadWaveAtStartup : false; }
            set
            {
                if (instance != null)
                    instance.LoadWaveAtStartup = value;
                else
                    Debug.LogWarning("MPTK_LoadWaveAtStartup: no MidiPlayerGlobal instance found");
            }
        }

        /// <summary>
        /// Find index of a Midi by name. Use the exact name defined in Unity resources folder MidiDB without any path or extension.\n
        /// Tips: Add Midi files to your project with the Unity menu MPTK or add it directly in the ressource folder and open Midi File Setup to automatically integrate Midi in MPTK.
        /// </summary>
        /// <param name="name">name of the midi without path nor extension</param>
        /// <returns>-1 if not found else return the index of the midi.</returns>
        public static int MPTK_FindMidi(string name)
        {
            int index = -1;
            try
            {
                if (!string.IsNullOrEmpty(name))
                    if (CurrentMidiSet != null && CurrentMidiSet.MidiFiles != null)
                        index = CurrentMidiSet.MidiFiles.FindIndex(s => s == name);

            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return index;
        }

        /// <summary>
        /// Calculate distance with the AudioListener.
        /// </summary>
        /// <param name="trf">Transform of the object to calculate the distance.</param>
        /// <returns></returns>
        public static float MPTK_DistanceToListener(Transform trf)
        {
            float distance = 0f;
            try
            {
                if (AudioListener != null)
                {
                    distance = Vector3.Distance(AudioListener.transform.position, trf.position);
                    //Debug.Log("Camera:" + AudioListener.name + " " + distance);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }

            return distance;
        }

        /// <summary>
        /// Event triggered at the end of the loading of the soundfont.\n
        /// Setting this callback function by script (AddListener) is not recommended. It's better to set callback function from the inspector.
        /// See here:https://mptkapi.paxstellar.com/d7/dc4/class_midi_player_t_k_1_1_midi_player_global.html#a64995b20027b35286c143f9f25a1cb6d
        /// @image html OnEventGlobal.png
        /// </summary>
        public static UnityEvent OnEventPresetLoaded
        {
            get { return Instance != null ? Instance.InstanceOnEventPresetLoaded : null; }
            set { Instance.InstanceOnEventPresetLoaded = value; }
        }

        /// <summary>
        /// True if soundfont is loaded
        /// </summary>
        public static bool MPTK_SoundFontLoaded = false;

        //! @cond NODOC

        public bool LoadSoundFontAtStartup = true;
        public bool LoadWaveAtStartup;
        public static TimeSpan timeToLoadSoundFont = TimeSpan.Zero;
        public static TimeSpan timeToLoadWave = TimeSpan.Zero;

        /// <summary>
        /// Current SoundFont loaded
        /// </summary>
        public static ImSoundFont ImSFCurrent;

        /// <summary>
        /// Event triggered when Soundfont is loaded
        /// </summary>
        public UnityEvent InstanceOnEventPresetLoaded = new UnityEvent();

        /// <summary>
        /// Current Midi Set loaded
        /// </summary>
        public static MidiSet CurrentMidiSet;

        public static string WavePath;

        private static AudioListener AudioListener;
        private static bool Initialized = false;

        // create path. Useful only in editor mode in conjonction of Application.dataPath. (for OS point of view)
        public static void InitPath()
        {
            //if (Instance != null)
            //{
            //    if (string.IsNullOrEmpty(MPTK_PathToResources))
            //        Debug.Log("MPTK_PathToResources not defined");
            //    else
            //    {
            //Debug.Log("InitPath Instance.PathToResources " + Instance.PathToResources);

            PathToSoundfonts = MPTK_PathToResources + SoundfontsDB;
            PathToMidiFile = MPTK_PathToResources + MidiFilesDB;
            PathToMidiSet = MPTK_PathToResources + FilenameMidiSet + ExtensionSoundFileDot;
            //PathToSoundfonts = Instance.PathToResources + "/Resources/" + SoundfontsDB;
            //PathToMidiFile = Instance.PathToResources + "/Resources/" + MidiFilesDB;
            //PathToMidiSet = Instance.PathToResources + "/Resources/" + FilenameMidiSet + ExtensionSoundFileDot;
            //    }
            //}
        }
        //public static void InitPath()
        //{
        //    if (string.IsNullOrEmpty(MPTK_PathToResources))
        //        Debug.Log("MPTK_PathToResources not defined");
        //    else
        //    {
        //        PathToSoundfonts = MPTK_PathToResources + SoundfontsDB;
        //        PathToMidiFile = MPTK_PathToResources + MidiFilesDB;
        //        PathToMidiSet = MPTK_PathToResources + FilenameMidiSet + ExtensionSoundFileDot;
        //    }
        //}

        void Awake()
        {
            HelperNoteLabel.Init();

            //Debug.Log("Awake MidiPlayerGlobal");
            if (instance != null && instance != this)
                Destroy(gameObject);    // remove previous instance
            else
            {
                //DontDestroyOnLoad(gameObject);
                instance = this;
                Routine.RunCoroutine(instance.InitThread(), Segment.RealtimeUpdate);
            }

            InitPath();
        }

        void OnApplicationQuit()
        {
            //Debug.Log("MPTK ending after" + Time.time + " seconds");
        }

        //! @endcond

        /// <summary>
        /// List of Soundfont(s) available. With the MPTK PRO, add or remove SoundFonts from the Unity editor menu "MPTK / SoundFont Setup" or Alt-F
        /// </summary>
        public static List<string> MPTK_ListSoundFont
        {
            get
            {
                if (CurrentMidiSet != null && CurrentMidiSet.SoundFonts != null)
                {
                    List<string> sfNames = new List<string>();
                    foreach (SoundFontInfo sfi in CurrentMidiSet.SoundFonts)
                        sfNames.Add(sfi.Name);
                    return sfNames;
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// List of MIDI files available. Add or remove MIDI from the Unity editor menu "MPTK / Midi File Setup" or Alt-M
        /// </summary>
        public static List<MPTKListItem> MPTK_ListMidi;

        /// <summary>
        /// List of presets (instrument) for the default or selected bank.\n
        /// The default bank can be changed with #MPTK_SelectBankInstrument or with the popup "SoundFont Setup Alt-F" in the Unity editor.
        /// </summary>
        public static List<MPTKListItem> MPTK_ListPreset;

        public static string MPTK_PresetName(int patch)
        {

            if (MPTK_ListPreset != null && patch >= 0 && patch < MPTK_ListPreset.Count && MPTK_ListPreset[patch] != null)
                return MPTK_ListPreset[patch].Label;
            else
                return "";
        }

        /// <summary>
        /// Get the list of banks available.\n
        /// The default bank can be changed with #MPTK_SelectBankInstrument or #MPTK_SelectBankDrum or with the menu "MPTK / SoundFont" or Alt-F in the Unity editor.
        /// </summary>
        public static List<MPTKListItem> MPTK_ListBank;

        /// <summary>
        /// List of drum set for the default or selected bank.\n
        /// The default bank can be changed with #MPTK_SelectBankDrum or with the menu "MPTK / SoundFont" or Alt-F in the Unity editor.
        /// </summary>
        public static List<MPTKListItem> MPTK_ListPresetDrum;

        // <summary>
        // Get the list of presets available
        // </summary>
        // NOT USED removed with 2.89.0 public static List<MPTKListItem> MPTK_ListDrum;

        /// <summary>
        /// Call by the first MidiPlayer awake
        /// </summary>
        private IEnumerator<float> InitThread()
        {
            if (!Initialized)
            {
                //Debug.Log("MidiPlayerGlobal InitThread");
                Initialized = true;
                ImSFCurrent = null;

                try
                {
                    AudioListener = Component.FindObjectOfType<AudioListener>();
                    if (AudioListener == null)
                    {
                        Debug.LogWarning("No audio listener found. Add one and only one AudioListener component to your hierarchy.");
                        //return;
                    }
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }

                try
                {
                    AudioListener[] listeners = Component.FindObjectsOfType<AudioListener>();
                    if (listeners != null && listeners.Length > 1)
                    {
                        Debug.LogWarning("More than one audio listener found. Some unexpected behaviors could happen.");
                    }
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }

                try
                {
                    LoadMidiSetFromRsc();
                    DicAudioWave.Init();
                    DicAudioClip.Init();
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }

                if (CurrentMidiSet == null)
                {
                    Debug.LogWarning(MidiPlayerGlobal.ErrorNoMidiFile);
                    yield return Routine.WaitForOneFrame;
                }
                else if (CurrentMidiSet.ActiveSounFontInfo == null)
                {
                    Debug.Log(MidiPlayerGlobal.ErrorNoSoundFont);
                    yield return Routine.WaitForOneFrame;
                }

                BuildMidiList();

                if (MPTK_LoadSoundFontAtStartup)
                    LoadCurrentSF();
            }
        }

        /// <summary>
        /// Stop all MIDI Synthesizers.
        /// </summary>
        public static void MPTK_Stop()
        {
            MidiSynth[] synths = FindObjectsOfType<MidiSynth>();
            if (synths != null)
            {
                foreach (MidiSynth synth in synths)
                {
                    if (synth is MidiFilePlayer)
                    {
                        MidiFilePlayer player = (MidiFilePlayer)synth;
                        if (player.MPTK_IsPlaying)
                        {
                            player.MPTK_Stop(); // stop and clear all sound
                        }
                    }
                    synth.MPTK_StopSynth();
                }
            }
        }

        /// <summary>
        /// Stop all MIDI Synthesizers and exit application
        /// </summary>
        public static void MPTK_Quit()
        {
            MPTK_Stop();
            Application.Quit();
        }

        private static float startupdate = float.MinValue;

        /// <summary>
        /// Check if SoudFont is loaded. Add a default wait time because Unity AudioSource need a delay to be ready to play. 
        /// </summary>
        /// <param name="delay">Additional waiting time (second), default is 0.5 second. Could be equal to 0.</param>
        /// <returns></returns>
        public static bool MPTK_IsReady(float delay = 0.5f)
        {
            if (startupdate < 0f)
                startupdate = Time.realtimeSinceStartup;
            if (!MPTK_SoundFontLoaded)
                return false;
            //Debug.Log(Time.realtimeSinceStartup);
            if (Time.realtimeSinceStartup - startupdate < 0.5f)
                return false;
            return true;
        }


        /// <summary>
        /// The default instrument and drum banks are defined with the popup "SoundFont Setup Alt-F" in the Unity editor.\n
        /// This method change the default instrument drum bank and build presets available for it. See #MPTK_ListPreset.\n
        /// Note 1: this call doesn't change the current MIDI bank used to play an instrument, only the content of #MPTK_ListPreset.\n
        /// Note 2: to apply the bank to all channels, the synth must be restarted: call MidiFilePlayer.MPTK_InitSynth.\n
        /// Note 3: to change the current bank, rather use MidiFilePlayer.MPTK_ChannelPresetChange\n
        /// </summary>
        /// <param name="nbank">Number of the SoundFont Bank to load for instrument.</param>
        /// <returns>true if bank has been found else false.</returns>
        public static bool MPTK_SelectBankInstrument(int nbank)
        {
            if (nbank >= 0 && nbank < ImSFCurrent.Banks.Length)
                if (ImSFCurrent.Banks[nbank] != null)
                {
                    ImSFCurrent.DefaultBankNumber = nbank;
                    BuildPresetList(true);
                    return true;
                }
                else
                    Debug.LogWarningFormat("MPTK_SelectBankInstrument: bank {0} is not defined", nbank);
            else
                Debug.LogWarningFormat("MPTK_SelectBankInstrument: bank {0} outside of range", nbank);
            return false;
        }

        /// <summary>
        /// The default instrument and drum banks are defined with the popup "SoundFont Setup Alt-F" in the Unity editor.\n
        /// This method change the default instrument drum bank and build presets available for it. See #MPTK_ListPreset.\n
        /// Note 1: this call doesn't change the current MIDI bank used to play an instrument, only the content of #MPTK_ListPreset.\n
        /// Note 2: to apply the bank to all channels, the synth must be restarted: call MidiFilePlayer.MPTK_InitSynth.\n
        /// Note 3: to change the current bank, rather use MidiFilePlayer.MPTK_ChannelPresetChange\n
        /// </summary>
        /// <param name="nbank">Number of the SoundFont Bank to load for drum.</param>
        /// <returns>true if bank has been found else false.</returns>
        public static void MPTK_SelectBankDrum(int nbank)
        {
            if (nbank >= 0 && nbank < ImSFCurrent.Banks.Length)
                if (ImSFCurrent.Banks[nbank] != null)
                {
                    ImSFCurrent.DrumKitBankNumber = nbank;
                    BuildPresetList(false);
                    //BuildDrumList();
                }
                else
                    Debug.LogWarningFormat("MPTK_SelectBankDrum: bank {0} is not defined", nbank);
            else
                Debug.LogWarningFormat("MPTK_SelectBankDrum: bank {0} outside of range", nbank);
        }

        /// <summary>
        /// Return the nape of the preset (patch) from a bank and patch number.
        /// </summary>
        /// <param name="bank"></param>
        /// <param name="patch"></param>
        /// <returns>Name of the preset or empty if not found.</returns>
        public static string MPTK_GetPatchName(int bank, int patch)
        {
            string name = "";
            if (ImSFCurrent != null && ImSFCurrent.Banks != null)
            {
                if (bank >= 0 && bank < ImSFCurrent.Banks.Length && ImSFCurrent.Banks[bank] != null)
                {
                    if (ImSFCurrent.Banks[bank].defpresets != null)
                    {
                        if (patch >= 0 && patch < ImSFCurrent.Banks[bank].defpresets.Length && ImSFCurrent.Banks[bank].defpresets[patch] != null)
                            name = ImSFCurrent.Banks[bank].defpresets[patch].Name;
                    }
                }
            }
            return name;
        }

        //! @cond NODOC

        /// <summary>
        /// Loading of a SoundFont when playing using a thread
        /// </summary>
        /// <param name="restartPlayer"></param>
        /// <returns></returns>
        private static IEnumerator<float> LoadSoundFontThread(bool restartPlayer = true)
        {
            //if (MidiPlayerGlobal.ImSFCurrent != null)
            {
                //Debug.Log("Load MidiPlayerGlobal.ImSFCurrent: " + MidiPlayerGlobal.ImSFCurrent.SoundFontName);
                //Debug.Log("Load CurrentMidiSet.ActiveSounFontInfo: " + CurrentMidiSet.ActiveSounFontInfo.Name);

                MidiSynth[] synths = FindObjectsOfType<MidiSynth>();
                List<MidiFilePlayer> playerToRestart = new List<MidiFilePlayer>();
                MPTK_SoundFontLoaded = false;
                if (Application.isPlaying)
                {
                    if (synths != null)
                    {
                        foreach (MidiSynth synth in synths)
                        {
                            if (synth is MidiFilePlayer)
                            {
                                MidiFilePlayer player = (MidiFilePlayer)synth;
                                if (player.MPTK_IsPlaying)
                                {
                                    playerToRestart.Add(player);
                                    player.MPTK_Stop(); // stop and clear all sound
                                }
                            }
                            //synth.MPTK_ClearAllSound();
                            yield return Routine.WaitUntilDone(Routine.RunCoroutine(synth.ThreadWaitAllStop(), Segment.RealtimeUpdate), false);
                            synth.MPTK_StopSynth();
                        }
                    }
                    DicAudioClip.Init();
                    DicAudioWave.Init();
                }
                LoadCurrentSF();

                //Debug.Log("   Time To Load SoundFont: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3).ToString() + " second");
                //Debug.Log("   Time To Load Waves: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3).ToString() + " second");

                if (synths != null)
                {
                    foreach (MidiSynth synth in synths)
                    {
                        synth.MPTK_InitSynth();
                        if (synth is MidiFilePlayer)
                            synth.MPTK_StartSequencerMidi();
                    }
                    if (restartPlayer)
                        foreach (MidiFilePlayer player in playerToRestart)
                            player.MPTK_RePlay();
                }
            }
        }

        /// <summary>
        /// Load a SF when editor
        /// </summary>
        private static void LoadSoundFont()
        {
            if (MidiPlayerGlobal.ImSFCurrent != null)
            {
                Debug.Log("Load MidiPlayerGlobal.ImSFCurrent: " + MidiPlayerGlobal.ImSFCurrent.SoundFontName);
                Debug.Log("Load CurrentMidiSet.ActiveSounFontInfo: " + CurrentMidiSet.ActiveSounFontInfo.Name);

                LoadCurrentSF();
                Debug.Log("Time To Load SoundFont: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3).ToString() + " second");
                if (Application.isPlaying)
                    Debug.Log("Time To Load Samples: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3).ToString() + " second");
            }
        }

        /// <summary>
        /// Core function to load a SF when playing or from editor from the Unity asset
        /// </summary>
        public static void LoadCurrentSF()
        {
            MPTK_SoundFontLoaded = false;
            // Load simplfied soundfont
            try
            {
                DateTime start = DateTime.Now;
                if (CurrentMidiSet == null)
                {
                    Debug.Log(MidiPlayerGlobal.ErrorNoSoundFont);
                }
                else
                {
                    SoundFontInfo sfi = CurrentMidiSet.ActiveSounFontInfo;
                    if (sfi == null)
                        Debug.Log(MidiPlayerGlobal.ErrorNoSoundFont);
                    else
                    {
                        //Debug.Log("Start loading " + sfi.Name);

                        // Path to the soundfonts directory for this SF, start from resource folder
                        string pathToImSF = Path.Combine(SoundfontsDB + "/", sfi.Name);

                        WavePath = Path.Combine(pathToImSF + "/", PathToWave);
                        // Load all presets defined in the sf
                        ImSFCurrent = ImSoundFont.LoadMPTKSoundFont(pathToImSF, sfi.Name);

                        // Add
                        if (ImSFCurrent == null)
                        {
                            Debug.LogWarning("Error loading " + sfi.Name ?? "name not defined");
                        }
                        else
                        {
                            BuildBankList();
                            BuildPresetList(true);
                            BuildPresetList(false);
                            //BuildDrumList();
                            timeToLoadSoundFont = DateTime.Now - start;

                            //Debug.Log("End loading SoundFont " + timeToLoadSoundFont.TotalSeconds + " seconds");

                        }

                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }

            if (ImSFCurrent == null)
            {
                Debug.LogWarning("SoundFont not loaded.");
                return;
            }

            // Load samples only in run mode
            if (Application.isPlaying)
            {
                try
                {
                    MPTK_CountWaveLoaded = 0;
                    System.Diagnostics.Stopwatch watchLoadWave = new System.Diagnostics.Stopwatch(); // High resolution time
                    watchLoadWave.Start();
                    // Load audio clip is used only in the legacy mode (non core mode)
                    if (MPTK_LoadWaveAtStartup)
                    {
                        LoadAudioClip();
                    }
                    // Load sample for core mode
                    // Attntion, on ne peut pas utiliser AudioClip et Resources endehors du main thread unity. IL faut donc charger tous les echantillons avant.
                    LoadWave();
                    timeToLoadWave = watchLoadWave.Elapsed;
                    //Debug.Log("End loading Waves " + timeToLoadWave.TotalSeconds + " seconds" + " count:" + MPTK_CountWaveLoaded);
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }
            }
            if (ImSFCurrent != null)
                MPTK_SoundFontLoaded = true;

            if (OnEventPresetLoaded != null) OnEventPresetLoaded.Invoke();
        }

        /// <summary>
        /// Load samples associated to a patch for High SF
        /// </summary>
        private static void LoadAudioClip()
        {
            try
            {
                float start = Time.realtimeSinceStartup;
                //Debug.LogWarning("LoadAudioClip - Load Sample for legacy mode");
                //int count = 0;
                if (ImSFCurrent != null)
                {
                    foreach (HiSample smpl in ImSFCurrent.HiSf.Samples)
                    {
                        if (smpl.Name != null)
                        {
                            if (!DicAudioClip.Exist(smpl.Name))
                            {
                                string path = WavePath + "/" + Path.GetFileNameWithoutExtension(smpl.Name);// + ".wav";
                                AudioClip ac = Resources.Load<AudioClip>(path);
                                if (ac != null)
                                {
                                    //Debug.Log("Sample load " + path);
                                    DicAudioClip.Add(smpl.Name, ac);

                                    MPTK_CountWaveLoaded++;
                                }
                                //else Debug.LogWarning("Sample " + smpl.WaveFile + " not found");
                            }
                        }
                    }
                }
                else Debug.Log("SoundFont not loaded ");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private static void LoadWave()
        {
            try
            {
                float start = Time.realtimeSinceStartup;
                //Debug.Log(">>> Load Sample");
                //int count = 0;
                if (ImSFCurrent != null)
                {
                    foreach (HiSample hiSample in ImSFCurrent.HiSf.Samples)
                    {
                        if (hiSample.Name != null)
                        {
                            if (!DicAudioWave.Exist(hiSample.Name))
                            {
                                LoadWave(hiSample);
                                MPTK_CountWaveLoaded++;
                            }
                        }
                    }
                }
                else Debug.Log("SoundFont not loaded ");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        public static void LoadWave(HiSample smpl)
        {
            //Debug.Log("-------------------- " + smpl.Name);
            string path = WavePath + "/" + Path.GetFileNameWithoutExtension(smpl.Name);// + ".wav";
            AudioClip ac = Resources.Load<AudioClip>(path);
            if (ac != null)
            {
                float[] data = new float[ac.samples * ac.channels];
                if (ac.GetData(data, 0))
                {
                    //Debug.Log(smpl.Name + " " +ac.samples * ac.channels);
                    smpl.Data = data;
                    DicAudioWave.Add(smpl);
                    //MPTK_CountWaveLoaded++;
                }
            }
            //else Debug.LogWarning("Sample " + smpl.Name + " not found");
        }

        /// <summary>
        /// Build list of presets found in the SoundFont
        /// </summary>
        static public void BuildBankList()
        {
            MPTK_ListBank = new List<MPTKListItem>();
            try
            {
                //Debug.Log(">>> Load Preset - b:" + ibank + " p:" + ipatch);
                if (ImSFCurrent != null && CurrentMidiSet != null)
                {
                    foreach (ImBank bank in ImSFCurrent.Banks)
                    {
                        if (bank != null)
                            MPTK_ListBank.Add(new MPTKListItem() { Index = bank.BankNumber, Label = "Bank " + bank.BankNumber });
                        else
                            MPTK_ListBank.Add(null);
                    }
                }
                else
                {
                    Debug.Log(MidiPlayerGlobal.ErrorNoSoundFont);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        /// Build list of presets found in the default bank of the current SoundFont
        /// </summary>
        static public void BuildPresetList(bool forInstrument)
        {
            List<MPTKListItem> presets = new List<MPTKListItem>();
            try
            {
                //Debug.Log(">>> Load Preset - b:" + ibank + " p:" + ipatch);
                if (ImSFCurrent != null)
                {
                    if ((forInstrument && ImSFCurrent.DefaultBankNumber >= 0 && ImSFCurrent.DefaultBankNumber < ImSFCurrent.Banks.Length) ||
                       (!forInstrument && ImSFCurrent.DrumKitBankNumber >= 0 && ImSFCurrent.DrumKitBankNumber < ImSFCurrent.Banks.Length))
                    {
                        int ibank = forInstrument ? ImSFCurrent.DefaultBankNumber : ImSFCurrent.DrumKitBankNumber;
                        if (ImSFCurrent.Banks[ibank] != null)
                        {
                            ImSFCurrent.Banks[ibank].PatchCount = 0;
                            for (int ipreset = 0; ipreset < ImSFCurrent.Banks[ibank].defpresets.Length; ipreset++)
                            {
                                HiPreset p = ImSFCurrent.Banks[ibank].defpresets[ipreset];
                                if (p != null)
                                {
                                    presets.Add(new MPTKListItem() { Index = p.Num, Label = p.Num + " - " + p.Name, Position = presets.Count });
                                    ImSFCurrent.Banks[ibank].PatchCount++;
                                }
                                //else
                                //    presets.Add(null);
                            }
                        }
                        else
                        {
                            Debug.LogWarningFormat("Default bank {0} for {1} is not defined.", ibank, forInstrument ? "Instrument" : "Drum");
                        }
                    }
                    else
                    {
                        Debug.LogWarningFormat("No default bank defined for {0}.", forInstrument ? "Instrument" : "Drum");
                    }

                    // Global count
                    //ImSFCurrent.PatchCount = 0;
                    foreach (ImBank bank in ImSFCurrent.Banks)
                    {
                        if (bank != null)
                        {
                            bank.PatchCount = 0;
                            foreach (HiPreset preset in bank.defpresets)
                            {
                                if (preset != null)
                                {
                                    // Bank count
                                    bank.PatchCount++;
                                }
                            }
                            //ImSFCurrent.PatchCount += bank.PatchCount;
                        }
                    }
                }
                else
                {
                    Debug.Log(MidiPlayerGlobal.ErrorNoSoundFont);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            if (forInstrument)
                MPTK_ListPreset = presets;
            else
                MPTK_ListPresetDrum = presets;
        }

        public static void BuildMidiList()
        {
            MPTK_ListMidi = new List<MPTKListItem>();
            if (CurrentMidiSet != null && CurrentMidiSet.MidiFiles != null)
                foreach (string name in CurrentMidiSet.MidiFiles)
                    MPTK_ListMidi.Add(new MPTKListItem() { Index = MPTK_ListMidi.Count, Label = name, Position = MPTK_ListMidi.Count });
            //Debug.Log("Midi file list loaded: " + MPTK_ListMidi.Count);
        }

        private static void LoadMidiSetFromRsc()
        {
            try
            {
                TextAsset sf = Resources.Load<TextAsset>(MidiPlayerGlobal.FilenameMidiSet);
                if (sf == null)
                    Debug.LogWarning(MidiPlayerGlobal.ErrorNoMidiFile);
                else
                {
                    //UnityEngine.Debug.Log(sf.text);
                    CurrentMidiSet = MidiSet.LoadRsc(sf.text);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        public void EndLoadingSF()
        {
            Debug.Log("End loading SF, MPTK is ready to play");

            //Debug.Log("List of presets available");
            //int i = 0;
            //foreach (string preset in MidiPlayerGlobal.MPTK_ListPreset)
            //    Debug.Log("   " + string.Format("[{0,3:000}] - {1}", i++, preset));
            //i = 0;
            //Debug.Log("List of drums available");
            //foreach (string drum in MidiPlayerGlobal.MPTK_ListDrum)
            //    Debug.Log("   " + string.Format("[{0,3:000}] - {1}", i++, drum));

            Debug.Log("Load statistique");
            Debug.Log("   Time To Load SoundFont: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Time To Load Samples: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Presets Loaded: " + MidiPlayerGlobal.MPTK_CountPresetLoaded);
            Debug.Log("   Samples Loaded: " + MidiPlayerGlobal.MPTK_CountWaveLoaded);
        }

        public static void ErrorDetail(System.Exception ex)
        {
            Debug.LogWarning("Maestro Error " + ex.Message);
            //Debug.LogWarning("   " + ex.TargetSite ?? "");
            //Debug.LogWarning("   InnerException:" + ex.InnerException ?? "");

            // V2.89.0 Remove the full error stack because Unity 2019 do the job!
            //var st = new System.Diagnostics.StackTrace(ex, true);
            //if (st != null)
            //{
            //    var frames = st.GetFrames();
            //    if (frames != null)
            //    {
            //        foreach (var frame in frames)
            //        {
            //            if (frame.GetFileLineNumber() < 1)
            //                continue;
            //            Debug.LogWarning("   " + frame.GetFileName() + " " + frame.GetMethod().Name + " " + frame.GetFileLineNumber());
            //        }
            //    }
            //    else
            //        Debug.LogWarning("   " + ex.StackTrace ?? "");
            //}
            //else
            //    Debug.LogWarning("   " + ex.StackTrace ?? "");
        }
        //! @endcond
    }
}
