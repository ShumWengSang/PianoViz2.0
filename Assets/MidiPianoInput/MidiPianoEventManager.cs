//#define PIANO_EVENT_LOGGING

using System;
using System.Collections.Generic;
using System.Linq;
using MidiPlayerTK;
using UnityEngine;
using UnityEngine.Assertions;

namespace MidiPianoInput
{
    
    class NoteEventInfo
    {
        public NoteEventInfo(MPTKEvent fireEvent, float time, GameObject noteVisualizationObject)
        {
            mptkEvent = fireEvent;
            fireTime = time;
            fireDuration = fireEvent.Duration * 0.001f;
            anticipatePress = true;
            noteVisualization = noteVisualizationObject;
            mptkEvent.Duration = -1; // make note play until explicitly stopped
        }

        public MPTKEvent mptkEvent;
        public float fireTime;
        public float fireDuration;
        public bool anticipatePress;
        public GameObject noteVisualization;
    }
    
    public class MidiPianoEventManager : MonoBehaviour
    {
        private Queue<NoteEventInfo>[] anticipatedNoteEvents = new Queue<NoteEventInfo>[(int) MidiNote._MIDI_NOTE_COUNT];
        
        [SerializeField] private MidiStreamPlayer midiStreamPlayer;
        
        [SerializeField, Tooltip("Maximum amount of time the user can be either early or late to pressing a key")] 
        private float maxTimeDelta = .5f;

        [SerializeField] private Material successMaterial;
        [SerializeField] private Material failMaterial;

        private bool _gameActive = false;
        public bool gameActive
        {
            get { return _gameActive; }
            set {
                if (value != _gameActive)
                {
                    if (value)
                    {
                        _gameActive = true;
                        OnGameActive();
                    }
                    else
                    {
                        gameActive = false;
                        OnGameInactive();
                    }
                }
            }
        }
        
        
        [SerializeField] private GameSetupInstructionsGUI gameSetup;
        private MidiNote lowerC = 0;

        private void OnGameActive()
        {
            BleMidiBroadcaster.onNoteDown += OnKeyboardNotePress;
            BleMidiBroadcaster.onNoteUp += OnKeyboardNoteRelease;
        }

        private void OnGameInactive()
        {
            BleMidiBroadcaster.onNoteDown -= OnKeyboardNotePress;
            BleMidiBroadcaster.onNoteUp -= OnKeyboardNoteRelease;
        }

        private void Awake()
        {
            for (int i = 0; i < anticipatedNoteEvents.Length; ++i)
            {
                anticipatedNoteEvents[i] = new Queue<NoteEventInfo>();
            }
            
            gameSetup.AssignLowerCEvent += (note, velocity) =>
            {
                lowerC = note;
            };
        }

        private void OnMistake(MidiNote note, int velocity)
        {
            // MPTKEvent mistakeNote = new MPTKEvent()
            // {
            //     Command = MPTKCommand.NoteOn,
            //     Value = (int)note - 12, // play in the wrong octive so it sound extra wrong
            //     Channel = 0,
            //     Duration = 250,
            //     Velocity = velocity,
            //     Delay = 0,
            // };
            //midiStreamPlayer.MPTK_PlayEvent(mistakeNote);
            // TODO: PLAY AN ERROR SOUND WHEN THEY MAKE A MISTAKE
        }

        private void Update()
        {
            if (!gameActive)
                return;
            
            // look for notes that the user completely failed to press in time
            foreach (Queue<NoteEventInfo> anticipated in anticipatedNoteEvents)
            {
                while (anticipated.Any() && Time.time - maxTimeDelta > anticipated.Peek().fireTime)
                {
                    
                    NoteEventInfo nextNote = anticipated.Peek();
                    // being late for releasing a note will be handled in OnKeyboardNoteRelease instead
                    if (!nextNote.anticipatePress)
                        break;
                    
                    MPTKEvent mptkEvent = nextNote.mptkEvent;
                    MidiNote note = (MidiNote) mptkEvent.Value;
#if PIANO_EVENT_LOGGING
                    Debug.Log("player failed to press the note '" + note + "' in time");
#endif
                    
                    // gery out the note to show that the user failed to play it in time
                    if(nextNote.noteVisualization)
                        nextNote.noteVisualization.GetComponent<Renderer>().material = failMaterial;
                    anticipated.Dequeue();
                }
            }
        }

        // called to set the time when this note "should" be played by the user
        public void NotifyUpcomingNote(MPTKEvent mptkEvent, int keyIndex, float noteTime, GameObject noteVisualization)
        {
            Assert.AreEqual(MPTKCommand.NoteOn, mptkEvent.Command);
            anticipatedNoteEvents[keyIndex].Enqueue(new NoteEventInfo(mptkEvent, noteTime, noteVisualization));
        }

        private void OnKeyboardNotePress(MidiNote note, int velocity)
        {
            int noteIndex = note - lowerC;
            Queue<NoteEventInfo> anticipated = anticipatedNoteEvents[noteIndex];
            if (!anticipated.Any())
            {
#if PIANO_EVENT_LOGGING
                Debug.Log("the note '" + note + "' was played unexpectedly");
#endif
                OnMistake(note, velocity);
                return;
            }
            
            NoteEventInfo nextNote = anticipated.Peek();
            if (nextNote.anticipatePress)
            {
                // if the note is pressed at the correct(ish) time, reward the player
                float error = nextNote.fireTime - Time.time;
                if (Mathf.Abs(error) < maxTimeDelta)
                {
#if PIANO_EVENT_LOGGING
                    Debug.Log("<color=green>player successfully pressed '" + note + "' with error: " + error + "</color>");
#endif
                    if(nextNote.noteVisualization)
                        nextNote.noteVisualization.GetComponent<Renderer>().material = successMaterial;
                    midiStreamPlayer.MPTK_PlayEvent(nextNote.mptkEvent);

                    // wait for a release now at the correct time
                    nextNote.anticipatePress = false;
                    nextNote.fireTime += nextNote.fireDuration;
                    nextNote.fireDuration = 0;
                }
                else
                {
#if PIANO_EVENT_LOGGING
                    Debug.Log("player pressed '" + note + "' too early");
#endif
                    OnMistake(note, velocity);

                    if (Math.Abs(nextNoteTime() - nextNote.fireTime) < maxTimeDelta)
                    {
                        // delete the note to show that the user played it too early
                        if(nextNote.noteVisualization)
                            nextNote.noteVisualization.GetComponent<Renderer>().material = failMaterial;
                        anticipated.Dequeue();
                    }
                }
            }
            // if we anticipated a release, the user will already be penalized in OnKeyboardNoteRelease
        }

        private void OnKeyboardNoteRelease(MidiNote note, int velocity)
        {
            int noteIndex = note - lowerC;
            Queue<NoteEventInfo> anticipated = anticipatedNoteEvents[noteIndex];
            if (!anticipated.Any())
            {
#if PIANO_EVENT_LOGGING
                Debug.Log("the note '" + note + "' was released unexpectedly");
#endif
                return;
            }

            NoteEventInfo nextNote = anticipated.Peek();
            if (!nextNote.anticipatePress)
            {
                float error = nextNote.fireTime - Time.time;
                // if the note is pressed at the correct(ish) time, reward the player
                if (error > maxTimeDelta)
                {
#if PIANO_EVENT_LOGGING
                    Debug.Log("player released '" + note + "' too early");
#endif
                }
                else if (error < -maxTimeDelta)
                {
#if PIANO_EVENT_LOGGING
                    Debug.Log("player released '" + note + "' too late");
#endif
                }
                else
                {
#if PIANO_EVENT_LOGGING
                    Debug.Log("<color=green>player successfully released '" + note + "' with delay: " + (nextNote.fireTime - Time.time) + "</color>");
#endif
                }
                
                midiStreamPlayer.MPTK_StopEvent(nextNote.mptkEvent);

                // delete the note to show that the user has played (or a attempted to play) it
                if(nextNote.noteVisualization)
                    nextNote.noteVisualization.GetComponent<Renderer>().material = failMaterial;
                anticipated.Dequeue();
            }
        }

        private float nextNoteTime()
        {
            float minTime = float.PositiveInfinity;
            foreach (Queue<NoteEventInfo> NoteEvents in anticipatedNoteEvents)
            {
                if(NoteEvents.Any())
                    minTime = Mathf.Min(NoteEvents.Peek().fireTime, minTime);
            }

            return minTime;
        }
    }
}