using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using MPTK;
using MidiPlayerTK;
using UnityEngine.SceneManagement;

public class SplashSceneController : MonoBehaviour
{
    public GameObject digiSplash;
    public GameObject pianoSplash;
    public MidiStreamPlayer midiStreamPlayer;

    public float fadeDuration = 0.5f;
    public float soundInterval = 1.0f;
    [Tooltip("Millisecond, -1 for infinite")]
    public int soundDuration = 400;
    [Tooltip("0 to 127")]
    public int soundVelocity = 50;
    private Material digiMat;
    private Material pianoMat;

    public bool nextScene = false;
    // Start is called before the first frame update
    public void Start()
    {
        digiMat = digiSplash.GetComponent<MeshRenderer>().material;
        pianoMat = pianoSplash.GetComponent<MeshRenderer>().material;

        SoundSequence();
        SplashSequence();


    }

    private void SoundSequence()
    {
        Sequence soundSequence = DOTween.Sequence();
        MPTKEvent soundevent = new MPTKEvent()
        {
            Command = MPTKCommand.NoteOn, // midi command
            Value = (int)MidiNote.C4, // from 0 to 127, 48 for C4, 60 for C5, ...
            Channel = 0, // from 0 to 15, 9 reserved for drum
            Duration = soundDuration, // note duration in millisecond, -1 to play undefinitely, MPTK_StopChord to stop
            Velocity = soundVelocity, // from 0 to 127, sound can vary depending on the velocity
            Delay = 0, // delay in millisecond before playing the note
        };
        MPTKEvent soundevent1 = new MPTKEvent()
        {
            Command = MPTKCommand.NoteOn, // midi command
            Value = 50, // from 0 to 127, 48 for C4, 60 for C5, ...
            Channel = 0, // from 0 to 15, 9 reserved for drum
            Duration = soundDuration, // note duration in millisecond, -1 to play undefinitely, MPTK_StopChord to stop
            Velocity = soundVelocity, // from 0 to 127, sound can vary depending on the velocity
            Delay = 0, // delay in millisecond before playing the note
        };
        MPTKEvent soundevent2 = new MPTKEvent()
        {
            Command = MPTKCommand.NoteOn, // midi command
            Value = 52, // from 0 to 127, 48 for C4, 60 for C5, ...
            Channel = 0, // from 0 to 15, 9 reserved for drum
            Duration = soundDuration, // note duration in millisecond, -1 to play undefinitely, MPTK_StopChord to stop
            Velocity = soundVelocity, // from 0 to 127, sound can vary depending on the velocity
            Delay = 0, // delay in millisecond before playing the note
        };


        // First have sounds play
        soundSequence.AppendInterval(soundInterval);
        soundSequence.AppendCallback(() =>
        {
            midiStreamPlayer.MPTK_PlayEvent(soundevent);
        });
        soundSequence.AppendInterval(soundInterval);

        soundSequence.AppendCallback(() =>
        {
            midiStreamPlayer.MPTK_PlayEvent(soundevent1);
        });
        soundSequence.AppendInterval(soundInterval);

        soundSequence.AppendCallback(() =>
        {
            midiStreamPlayer.MPTK_PlayEvent(soundevent2);
        });
    }

    private void SplashSequence()
    {
        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(3.0f);
        seq.Append(digiMat.DOBlendableColor(Color.clear, fadeDuration));
        seq.Append(pianoMat.DOBlendableColor(Color.white, fadeDuration));
        seq.AppendInterval(3.0f);
        seq.Append(pianoMat.DOBlendableColor(Color.clear, fadeDuration));
        seq.AppendInterval(1.0f);
        seq.AppendCallback( () =>
        {
#if UNITY_EDITOR
            if (nextScene)
            {
                SceneManager.LoadScene("OurGameScene");
            }
#else
            SceneManager.LoadScene("OurGameScene");
#endif
        });
    }
}
