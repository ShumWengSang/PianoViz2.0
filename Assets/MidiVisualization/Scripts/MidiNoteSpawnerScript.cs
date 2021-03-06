using System;
using System.Collections.Generic;
using DG.Tweening;
using Microsoft.MixedReality.Toolkit.UI;
using MidiPianoInput;
using MidiPlayerTK;
using PlaybackControls;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class MidiNoteSpawnerScript : MonoBehaviour
{
    [SerializeField] public MidiFilePlayer midiFilePlayer;
    [SerializeField] private MidiStreamPlayer midiStreamPlayer;

    // transform who's children is the starting pos of all the keys
    [FormerlySerializedAs("startTransform")] [SerializeField] private Transform noteStarts;

    // transform who's children is the ending pos of all the keys
    [FormerlySerializedAs("endTransform")] [SerializeField] private Transform notePaths;

    [SerializeField] private Transform keyboard;

    // transform who's children is all the active notes in que to be played
    [SerializeField] private Transform activeNotes;
    [SerializeField] private Transform activeBars;
    [SerializeField] private GameObject barPrefabStart;
    [SerializeField] private GameObject barPrefabEnd;

    [FormerlySerializedAs("eventManager")] [SerializeField] private MidiPianoEventManager midiPianoEventManager;

    [SerializeField] private Ease tweenEase = Ease.Linear;
    [SerializeField] private float tweenTime = 5;

    [SerializeField] private BeatGenerator beatGenerator;
    
    
    private Transform[] startKeys;
    private Transform[] endKeys;
    private int beatCounter = 0;

    // Start is called before the first frame update
    void Start()
    {
        DOTween.SetTweensCapacity(256, 1024);
        
        PopulateKeyArray(noteStarts, ref startKeys);
        PopulateKeyArray(notePaths, ref endKeys);

        Assert.IsTrue(MidiPlayerGlobal.ImSFCurrent != null);
        Assert.IsTrue(MidiPlayerGlobal.MPTK_SoundFontLoaded);
        Assert.IsTrue((midiFilePlayer != null));
        
         
        midiFilePlayer.enabled = true;
        midiFilePlayer.OnEventNotesMidi.AddListener(HandleMidiEvents);
        midiFilePlayer.OnEventStartPlayMidi.AddListener(OnMusicStart);
        midiFilePlayer.OnEventEndPlayMidi.AddListener(OnMusicEnd);
    }

    private Sequence tempoVisual;
    private void OnMusicEnd(string arg0, EventEndMidiEnum arg1)
    {
        tempoVisual.Kill();
    }
    

    private void OnMusicStart(string midiname)
    {
        Debug.LogFormat($"Start playing midi {midiname}");
        Debug.LogFormat($"Midi tempo of {midiFilePlayer.MPTK_Tempo.ToString()}");
        Debug.LogFormat($"Midi pulse length of {midiFilePlayer.MPTK_PulseLenght.ToString()}");
        Debug.LogFormat($"Midi numerator of {midiFilePlayer.midiLoaded.MPTK_TimeSigNumerator.ToString()}");


        double timeBetweenQuaterNote =
            // midiFilePlayer.MPTK_PulseLenght;
            60 / midiFilePlayer.MPTK_Tempo;

        var preplayMetronome = DOTween.Sequence();
        midiFilePlayer.MPTK_Pause();
        preplayMetronome
            .AppendInterval((float) timeBetweenQuaterNote * 8)
            .AppendCallback(() => { midiFilePlayer.MPTK_UnPause(); })
            .AppendInterval(tweenTime)
            .AppendCallback(() =>
            {
                if (SongToggle.selectedSong.wavAccompaniment)
                {
                    AudioSource audioSource = GetComponent<AudioSource>();
                    audioSource.clip = SongToggle.selectedSong.wavAccompaniment;
                    audioSource.Play();
                }
            });
            
        preplayMetronome.timeScale = midiFilePlayer.MPTK_Speed;

        tempoVisual = DOTween.Sequence();        
        this.beatCounter = 0;
        tempoVisual.AppendCallback(() =>
        {
            // Spawn horizontal bars
            GameObject instance = Instantiate(barPrefabStart, activeBars, true);
            instance.SetActive(true);

            instance.transform.DOMoveY(barPrefabEnd.transform.position.y, tweenTime)
                .SetEase(tweenEase).OnComplete(() =>
                {
                    this.beatCounter++;
                    if (this.beatCounter >= 4)
                    {
                        this.beatCounter = 0;
                    }

                    beatGenerator?.PlaySound(this.beatCounter);
                    Destroy(instance);
                });
        }).AppendInterval((float)timeBetweenQuaterNote).SetLoops(-1);
        tempoVisual.timeScale = midiFilePlayer.MPTK_Speed;
    }

    public void OnChangeTimeScale(float newTimeScale)
    {
        if (tempoVisual != null)
        {
            tempoVisual.timeScale = newTimeScale;
        }
    }

    public void StopSong()
    {
        GetComponent<AudioSource>().Stop();
        midiFilePlayer.MPTK_Stop();
        foreach (Transform activeNote in activeNotes)
        {
            Destroy(activeNote.gameObject);
        }
    }

    public void PlaySong()
    {
        midiFilePlayer.MPTK_Play();
    }
    
    private void PopulateKeyArray(Transform parent, ref Transform[] keys)
    {
        keys = new Transform[parent.childCount];
        for (int i = 0; i < parent.childCount; i++)
            keys[i] = parent.GetChild(i);
    }

    /// <summary>
    /// Call when a group of midi events is ready to plays from the the midi reader.
    /// Playing the events are delayed until they "fall out"
    /// </summary>
    /// <param name="events"></param>
    public void HandleMidiEvents(List<MPTKEvent> events)
    {
        foreach (MPTKEvent mptkEvent in events)
        {
            switch (mptkEvent.Command)
            {
                case MPTKCommand.NoteOn:
                {
                    MidiNote note = (MidiNote) mptkEvent.Value;
                    int noteIndex = note - (MidiNote)SongToggle.selectedSong.startingNote;

                    // note is in key range and chanel matches
                    if (mptkEvent.Channel == SongToggle.selectedSong.channel)
                    {
                        if (noteIndex >= 0 && noteIndex < startKeys.Length)
                        {
                            // instantiate a note
                            GameObject instance = Instantiate(startKeys[noteIndex].gameObject, activeNotes, true);
                            NoteTweenScript script = instance.AddComponent<NoteTweenScript>();
                            script.keyboardKeyHighlights = keyboard.GetChild(noteIndex).GetComponent<keyboardKeyHighlights>();

                            script.DOMoveScale(notePaths.position, tweenTime, mptkEvent.Duration * 0.001f,
                                    tweenEase)
                                //.SetEase(tweenEase)
                                .OnComplete(() => { Destroy(instance); });

                            // fire the event for the note playing once the instance has tweened to the keyboard
                            midiPianoEventManager.NotifyUpcomingNote(mptkEvent, noteIndex, Time.time + tweenTime, instance);
                        }
                        else
                        {
                            Debug.LogWarning("Note out of range: " + note);
                            DOTween.Sequence()
                                .AppendInterval(tweenTime)
                                .AppendCallback(() =>
                                {
                                    if (midiStreamPlayer.MPTK_ChannelPresetGetIndex(0) != 40)
                                    {
                                        bool ret = midiStreamPlayer.MPTK_ChannelPresetChange(0, 40, -1);
                                    }
                                    midiStreamPlayer.MPTK_PlayEvent(mptkEvent);
                                });
                        }
                    }
                    else
                    {
                        //goto default;
                        DOTween.Sequence()
                            .AppendInterval(tweenTime)
                            .AppendCallback(() =>
                            {
                                if (midiStreamPlayer.MPTK_ChannelPresetGetIndex(mptkEvent.Channel) != 40)
                                {
                                    bool ret = midiStreamPlayer.MPTK_ChannelPresetChange(mptkEvent.Channel, 40, -1);
                                }
                                midiStreamPlayer.MPTK_PlayEvent(mptkEvent);
                            });
                    }
                    break;
                }
                default:
                {
                    DOTween.Sequence()
                        .AppendInterval(tweenTime)
                        .AppendCallback(() =>
                        {
                            midiStreamPlayer.MPTK_PlayEvent(mptkEvent);
                        });
                    break;
                }
            }
            
        }
    }
}