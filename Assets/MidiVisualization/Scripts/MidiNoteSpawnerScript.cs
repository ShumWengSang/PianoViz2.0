using System.Collections.Generic;
using DG.Tweening;
using MidiPlayerTK;
using UnityEngine;
using UnityEngine.Assertions;

public class MidiNoteSpawnerScript : MonoBehaviour
{
    [SerializeField] private MidiFilePlayer midiFilePlayer;
    [SerializeField] private MidiStreamPlayer midiStreamPlayer;

    // transform who's children is the starting pos of all the keys
    [SerializeField] private Transform startTransform;

    // transform who's children is the ending pos of all the keys
    [SerializeField] private Transform endTransform;

    // transform who's children is all the active notes in que to be played
    [SerializeField] private Transform activeNotes;

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

        // If call is already set from the inspector there is no need to set another listeneer
        if (!midiFilePlayer.OnEventNotesMidi.HasEvent())
        {
            midiFilePlayer.OnEventNotesMidi.AddListener(HandleMidiEvents);
        }
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

                            script.DOMoveScale(endKeys[noteIndex].position, tweenTime, mptkEvent.Duration * 0.001f,
                                    tweenEase)
                                //.SetEase(tweenEase)
                                .OnComplete(() => { Destroy(instance); });
                        }
                        else
                        {
                            Debug.LogWarning("Note out of range: " + note);
                        }
                    }
                    break;
                }
            }
            DOTween.Sequence()
                .AppendInterval(tweenTime)
                .AppendCallback(() =>
                {
                    midiStreamPlayer.MPTK_PlayEvent(mptkEvent);
                });
        }
    }
}