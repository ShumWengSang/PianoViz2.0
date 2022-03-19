using System;
using System.Collections.Generic;
using DG.Tweening;
using MidiPianoInput;
using MidiPlayerTK;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

public class MidiNoteSpawnerScript : MonoBehaviour
{
    [SerializeField] private MidiFilePlayer midiFilePlayer;
    [SerializeField] private MidiStreamPlayer midiStreamPlayer;

    // transform who's children is the starting pos of all the keys
    [SerializeField] private Transform startTransform;

    // transform who's children is the ending pos of all the keys
    [SerializeField] private Transform endTransform;

    [SerializeField] private Transform keyboard;

    // transform who's children is all the active notes in que to be played
    [SerializeField] private Transform activeNotes;

    [FormerlySerializedAs("eventManager")] [SerializeField] private MidiPianoEventManager midiPianoEventManager;

    [SerializeField] private MidiNote startingNote;
    [SerializeField] private int midiChanel = 0;

    [SerializeField] private Ease tweenEase = Ease.Linear;
    [SerializeField] private float tweenTime = 5;

    private Transform[] startKeys;
    private Transform[] endKeys;

    // Start is called before the first frame update
    void Start()
    {
        DOTween.SetTweensCapacity(256, 1024);
        
        PopulateKeyArray(startTransform, ref startKeys);
        PopulateKeyArray(endTransform, ref endKeys);

        Assert.IsTrue(MidiPlayerGlobal.ImSFCurrent != null);
        Assert.IsTrue(MidiPlayerGlobal.MPTK_SoundFontLoaded);
        Assert.IsTrue((midiFilePlayer != null));

        midiFilePlayer.enabled = true;
        midiFilePlayer.OnEventNotesMidi.AddListener(HandleMidiEvents);

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
                    int noteIndex = note - startingNote;

                    // note is in key range and chanel matches
                    if (mptkEvent.Channel == midiChanel)
                    {
                        if (noteIndex >= 0 && noteIndex < startKeys.Length)
                        {
                            // instantiate a note
                            GameObject instance = Instantiate(startKeys[noteIndex].gameObject, activeNotes, true);
                            NoteTweenScript script = instance.AddComponent<NoteTweenScript>();
                            script.keyboardKeyHighlights = keyboard.GetChild(noteIndex).GetComponent<keyboardKeyHighlights>();

                            script.DOMoveScale(endKeys[noteIndex].position, tweenTime, mptkEvent.Duration * 0.001f,
                                    tweenEase)
                                //.SetEase(tweenEase)
                                .OnComplete(() => { Destroy(instance); });

                            // fire the event for the note playing once the instance has tweened to the keyboard
                            midiPianoEventManager.NotifyUpcomingNote(mptkEvent, noteIndex, Time.time + tweenTime, instance);
                        }
                        else
                        {
                            Debug.LogWarning("Note out of range: " + note);
                            goto default; // since it's not in range, just play the note normally
                        }
                    }
                    else
                    {
                        //goto default;
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