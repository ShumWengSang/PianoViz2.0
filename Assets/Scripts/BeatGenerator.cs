using System.Collections;
using System.Collections.Generic;
using MidiPlayerTK;
using UnityEngine;
using MPTK;
public class BeatGenerator : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip snap;
    [SerializeField] private AudioClip clap;

    public void PlaySound(int i)
    {
        if (i == 0)
        {
            audioSource.clip = snap;
            audioSource.Play();
        }
        else
        {
            audioSource.clip = clap;
            audioSource.Play();
        }
    }
}
