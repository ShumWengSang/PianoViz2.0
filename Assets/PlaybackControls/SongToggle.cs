using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.MixedReality.Toolkit.UI;
using MidiPlayerTK;
using TMPro;
using UnityEngine;

namespace PlaybackControls
{
    public enum startingMidiNote
    {
        C0 = 000,
        C1 = 012,
        C2 = 024,
        C3 = 036,
        C4 = 048,
        C5 = 060,
        C6 = 072,
        C7 = 084,
        C8 = 096,
        C9 = 108,
        C10 = 120,
    }

    [Serializable]
    public class SongSelection
    {
        [ReadOnly(true)] public string songName;
        [SerializeField] public startingMidiNote startingNote;
        [SerializeField] public int channel;
    }
    
    [ExecuteInEditMode]
    public class SongToggle : MonoBehaviour
    {
        [NonSerialized] private static SongToggle self;
        [SerializeField] private MidiNoteSpawnerScript notespawner;
        [SerializeField] private List<SongSelection> songList = new List<SongSelection>();
        [SerializeField] private bool refresh = false;
        private int selectedMidiIndex = -1;

        private TextMeshPro songTitle;
        private ButtonConfigHelper TogglePlayButton;

        public static SongSelection selectedSong => self.songList[self.selectedMidiIndex];

        private void Awake()
        {
            songTitle = transform.Find("SongTitle").GetComponent<TextMeshPro>();
            TogglePlayButton = transform.Find("PlayStop").GetComponent<ButtonConfigHelper>();
            self = this;
        }

        private void Update()
        {
            if (notespawner.midiFilePlayer.MPTK_MidiIndex != selectedMidiIndex)
            {
                selectedMidiIndex = notespawner.midiFilePlayer.MPTK_MidiIndex;
                songTitle.text = "• " + songList[selectedMidiIndex].songName + " •";
            }
            TogglePlayButton.SetSpriteIcon(TogglePlayButton.IconSet.SpriteIcons[(notespawner.midiFilePlayer.MPTK_IsPlaying? 7 : 1)]);
        }

        private void OnValidate()
        {
            refresh = false;
            if (MidiPlayerGlobal.CurrentMidiSet == null)
            {
                MidiPlayerGlobal.InitPath();
                try
                {
                    MidiPlayerGlobal.CurrentMidiSet = MidiSet.Load(Application.dataPath + "/" + MidiPlayerGlobal.PathToMidiSet);
                    if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles == null)
                        MidiPlayerGlobal.CurrentMidiSet.MidiFiles = new List<string>();
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }
            }
            
            
            var midiList = MidiPlayerGlobal.CurrentMidiSet.MidiFiles;
            
            List<SongSelection> newSongList = new List<SongSelection>();
            foreach (string fileName in midiList)
            {
                newSongList.Add(new SongSelection
                {
                    songName = fileName,
                    startingNote = startingMidiNote.C5,
                    channel = 0
                });
            }

            foreach (SongSelection song in songList)
            {
                int index = MidiPlayerGlobal.MPTK_FindMidi(song.songName);
                if (index != -1)
                {
                    newSongList[index].startingNote = song.startingNote;
                    newSongList[index].channel = song.channel;
                }
                
            }

            songList = newSongList;
        }


        public void NextSong()
        {
            notespawner.StopSong();
            if ( notespawner.midiFilePlayer.MPTK_MidiIndex == songList.Count - 1)
                notespawner.midiFilePlayer.MPTK_MidiIndex = 0;
            else
                ++notespawner.midiFilePlayer.MPTK_MidiIndex;
        }

        public void PrevSong()
        {
            notespawner.StopSong();
            if ( notespawner.midiFilePlayer.MPTK_MidiIndex == 0)
                notespawner.midiFilePlayer.MPTK_MidiIndex = songList.Count - 1;
            else
                --notespawner.midiFilePlayer.MPTK_MidiIndex;
        }

        public void TogglePlay()
        {
            if (notespawner.midiFilePlayer.MPTK_IsPlaying)
            {
                notespawner.StopSong();
            }
            else
            {
                notespawner.PlaySong();
            }
        }
    }
}