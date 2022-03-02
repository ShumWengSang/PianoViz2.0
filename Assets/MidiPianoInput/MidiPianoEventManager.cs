// #define PIANO_EVENT_LOGGING

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
        private static Queue<NoteEventInfo>[] anticipatedNoteEvents = new Queue<NoteEventInfo>[(int) MidiNote._MIDI_NOTE_COUNT];
        
        [SerializeField] private MidiStreamPlayer midiStreamPlayer;
        
        [SerializeField, Tooltip("Maximum amount of time the user can be either early or late to pressing a key")] 
        private float maxTimeDelta = .5f;

        private void OnEnable()
        {
            BleMidiBroadcaster.onNoteDown += OnKeyboardNotePress;
            BleMidiBroadcaster.onNoteUp += OnKeyboardNoteRelease;
        }

        private void OnDisable()
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
        }

        private void Update()
        {
            // look for notes that the user completely failed to press in time
            foreach (Queue<NoteEventInfo> noteEvents in anticipatedNoteEvents)
            {
                while (noteEvents.Any() && Time.time - maxTimeDelta > noteEvents.Peek().fireTime)
                {
                    // being late for releasing a note will be handled in OnKeyboardNoteRelease instead
                    if (!noteEvents.Peek().anticipatePress)
                        break;
                    
                    MPTKEvent mptkEvent = noteEvents.Peek().mptkEvent;
                    MidiNote note = (MidiNote) mptkEvent.Value;
#if PIANO_EVENT_LOGGING
                    Debug.Log("player failed to press the note '" + note + "' in time");
#endif
                    
                    // delete the note to show that the user failed to play it in time
                    Destroy(noteEvents.Peek().noteVisualization);
                    noteEvents.Dequeue();
                }
            }
        }

        // called to set the time when this note "should" be played by the user
        public static void NotifyUpcomingNote(MPTKEvent mptkEvent, float noteTime, GameObject noteVisualization)
        {
            Assert.AreEqual(MPTKCommand.NoteOn, mptkEvent.Command);
            anticipatedNoteEvents[mptkEvent.Value].Enqueue(new NoteEventInfo(mptkEvent, noteTime, noteVisualization));
        }

        private void OnKeyboardNotePress(MidiNote note, int velocity)
        {
            Queue<NoteEventInfo> anticipated = anticipatedNoteEvents[(int) note];
            if (!anticipated.Any())
            {
#if PIANO_EVENT_LOGGING
                Debug.Log("the note '" + note + "' was played unexpectedly");
#endif
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
                    
                    // delete the note to show that the user played it too early
                    Destroy(nextNote.noteVisualization);
                    anticipated.Dequeue();
                }
            }
            // if we anticipated a release, the user will already be penalized in OnKeyboardNoteRelease
        }

        private void OnKeyboardNoteRelease(MidiNote note, int velocity)
        {
            Queue<NoteEventInfo> anticipated = anticipatedNoteEvents[(int) note];
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
                Destroy(nextNote.noteVisualization);
                anticipated.Dequeue();
            }
        }
    }
}