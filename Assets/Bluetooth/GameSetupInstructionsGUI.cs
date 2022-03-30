﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using DG.Tweening;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Serialization;

[System.Serializable]
public class BannerObjectHolders
{
    public MeshRenderer BannerBackground;

    public Transform LoadingCircle;

    public SpriteRenderer Icon;

    public TextMeshPro Text;

    public ButtonConfigHelper Button;

    public void SetActive(bool active)
    {
        LoadingCircle.parent.gameObject.SetActive(active);
    }
}

public class GameSetupInstructionsGUI : MonoBehaviour
{
    [SerializeField] private BleModel bleModel;
    [SerializeField] private Ease rotationEase = Ease.InOutCirc;
    [SerializeField] private float rotationDuration = 1f;
    [SerializeField] private GameObject arUcoMarkerGameObject;
    [SerializeField] private Transform arucoMarkerVisual;
    [SerializeField] private AudioSource audioPlayer;
    [SerializeField] private Transform PlaySpace;
    [SerializeField] private BannerObjectHolders MovingBannerObjects;
    [SerializeField] private BannerObjectHolders StaticBannerObjects;
    private Transform PlaySpaceParent;
    private Transform bannerParent;

    [SerializeField] private MarkerDetection markerDetection;


    [NonSerialized] public BleMidiBroadcaster.OnNoteDown AssignLowerCEvent;

    [SerializeField, Header("Falling Down")]
    private Ease fallingDownEase = Ease.InCubic;
    [SerializeField] private Transform KeyboardAndItems;
    [SerializeField] private Transform PositionAdjustments;
    [SerializeField] private Transform SpeedSlider;
    [SerializeField] private Transform NoteSpawner;
    [SerializeField] private Transform outline;
    [SerializeField] private Transform Keyboard;
    [Header("Setup And Teardown")]
    public UnityEvent OnStartup;
    public UnityEvent OnFinished;

    [Header("arUco Marker Detection")]
    public UnityEvent OnStartarUcoSequence;
    public UnityEvent OnEndarUcoSequence;

    [Header("Wait For Position Adjust")]
    public UnityEvent OnWaitForPositionAdjust;
    public UnityEvent AfterWaitForPositionAdjust;
    
    [Header("Wait For Bluetooth to connect")]
    public UnityEvent OnWaitForBluetooth;
    public UnityEvent AfterWaitForBluetooth;
    
    [Header("Wait For Lower C to be pressed")]
    public UnityEvent OnWaitForLowerC;
    public UnityEvent AfterWaitForLowerC;
    
    [Header("Wait For Lower C to be pressed")]
    public UnityEvent OnWaitForUpperC;
    public UnityEvent AfterWaitForUpperC;

    [Header("On thud sound")] public UnityEvent OnKeyboardDropped;

    [Space(10), Header("Sound Effects")]
    [SerializeField] private AudioClip NotificationSound;
    [SerializeField] private AudioClip WelcomeSound;
    [SerializeField] private AudioClip WaitSound;
    [SerializeField] private AudioClip FallSound;
    [SerializeField] private AudioClip ThudSound;
    [SerializeField] private AudioClip BeepingSound;
    [SerializeField] private AudioSource FallingAudioSource;
    
    private static readonly int Property = Shader.PropertyToID("_IridescenceIntensity");

    private MidiNote lowerC;
    private MidiNote upperC;

    // Start is called before the first frame update
    void Start()
    {
        MovingBannerObjects.LoadingCircle.DOBlendableLocalRotateBy(new Vector3(0f, 0f, 360f), rotationDuration, RotateMode.FastBeyond360)
            .SetEase(rotationEase).SetLoops(-1);
        StaticBannerObjects.LoadingCircle.DOBlendableLocalRotateBy(new Vector3(0f, 0f, 360f), rotationDuration, RotateMode.FastBeyond360)
            .SetEase(rotationEase).SetLoops(-1);
        StartCoroutine(Run());
    }

    public IEnumerator Run()
    {
        Startup();
        yield return StartCoroutine(arucoMarkerSequence());
        yield return StartCoroutine(WaitForPositionAdjust());
        yield return StartCoroutine(WaitForBluetooth());
        yield return StartCoroutine(WaitForKeyboardNoteRange());        Finished();
    }
    
    private void Startup()
    {
        PlaySpaceParent = PlaySpace.parent;
        bannerParent = MovingBannerObjects.BannerBackground.transform.parent;
        MovingBannerObjects.SetActive(true);
        StaticBannerObjects.SetActive(false);
        
        MovingBannerObjects.BannerBackground.gameObject.SetActive(true);
        MovingBannerObjects.BannerBackground.material.SetFloat(Property, 0);

        MovingBannerObjects.Text.gameObject.SetActive(true);
        MovingBannerObjects.Text.alpha = 0.0f;
        
        MovingBannerObjects.LoadingCircle.gameObject.SetActive(false);
        
        
        MovingBannerObjects.Icon.gameObject.SetActive(false);
        MovingBannerObjects.Icon.DOFade(1.0f, 0.0f);
        
        arucoMarkerVisual.gameObject.SetActive(false);
        KeyboardAndItems.gameObject.SetActive(false);

        OnStartup.Invoke();
    }

    private void Finished()
    {
        PlaySpace.parent = PlaySpaceParent;
        OnFinished.Invoke();
    }

    public IEnumerator FadeInFadeOutText(string newText, TextMeshPro tmp, bool fadeOut = true)
    {
        MovingBannerObjects.Text.text = newText;
        var bannerFadeIn = DOTween.To(() => tmp.alpha, x => tmp.alpha = x, 1.0f, 2.0f);
        // Fade in text
        yield return bannerFadeIn.WaitForCompletion();
        if (fadeOut)
        {
            var bannerFadeOut = DOTween.To(() => tmp.alpha, x => tmp.alpha = x, 0.0f, 0.6f);
            yield return bannerFadeOut.WaitForCompletion();
        }
    }

    public IEnumerator arucoMarkerSequence()
    {
        bannerParent.GetComponent<FollowCameraScript>().enabled = true;
        yield return null;
        // wait 5 seconds
        yield return new WaitForSeconds(3.0f);

        audioPlayer.clip = NotificationSound;
        audioPlayer.Play();
        // MovingBannerObjects.BannerBackground.gameObject.SetActive(true);
        var quadFadeIn = DOTween.To(() => MovingBannerObjects.BannerBackground.material.GetFloat(Property),
            x => MovingBannerObjects.BannerBackground.material.SetFloat(Property, x), 0.75f, 2.0f);
        // Fade in the blue quad
        yield return quadFadeIn.WaitForCompletion();
        audioPlayer.Play();
        
        yield return StartCoroutine(FadeInFadeOutText("Starting up keyboard tracking", MovingBannerObjects.Text));
        yield return StartCoroutine(FadeInFadeOutText("Please find the marker near the keyboard", MovingBannerObjects.Text, false));
        MovingBannerObjects.LoadingCircle.gameObject.SetActive(true);
        MovingBannerObjects.Icon.gameObject.SetActive(true);

        // Invoke event
        OnStartarUcoSequence.Invoke();
        // Play notification sound
        audioPlayer.clip = NotificationSound;
        audioPlayer.Play();

        // Wait a bit
        yield return new WaitForSeconds(1.0f);
        // Now start aruco detection
        arUcoMarkerGameObject.GetComponent<MarkerDetection>().StartDetection();
        // Add waiting sound
        var sequence = DOTween.Sequence().AppendCallback(() =>
        {
            audioPlayer.clip = WaitSound;
            audioPlayer.Play();
        }).AppendInterval(2.68f).SetLoops(-1);

        // Wait until detection
        {
            bool detected = false;
            int detectedCount = 0;
            void OnConfirmed(Transform trans)
            {
                if (trans)
                {
                    PlaySpace.position = trans.position;
                    PlaySpace.rotation = trans.rotation;
                }
                detected = true;
#if UNITY_EDITOR
                detectedCount = 100;
#endif
            }

            arUcoMarkerGameObject.GetComponent<ArUcoDetectionHoloLensUnity.ArUcoMarkerDetection>().onMarkerDetected.AddListener(OnConfirmed);

            // If not found, set detected count to 0
            // if found, increment detected count
            // correlate detected count to visual
            while (true)
            {
                if (detected)
                {
                    detectedCount++;
                    MovingBannerObjects.LoadingCircle.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, Color.green, detectedCount / 100.0f);
                    if (detectedCount > 10)
                    {
                        markerDetection.StopDetecting();
                        break;
                    }

                    detected = false;
                }

                yield return new WaitForSeconds(0.01f);
            }

            arUcoMarkerGameObject.GetComponent<ArUcoDetectionHoloLensUnity.ArUcoMarkerDetection>().onMarkerDetected.RemoveListener(OnConfirmed);
        }
        sequence.Kill();
        arucoMarkerVisual.gameObject.SetActive(true);
        
        // Play notification sound
        audioPlayer.clip = WelcomeSound;
        audioPlayer.Play();


        // Detected marker, fade out background, icons, and text
        MovingBannerObjects.LoadingCircle.GetComponent<SpriteRenderer>().DOFade(0.0f, 1.0f);
        MovingBannerObjects.Text.DOFade(0.0f, 0.5f);
        MovingBannerObjects.Icon.DOFade(0.0f, 0.5f);
        
        // Fade out + remove the banner
        bannerParent.GetComponent<FollowCameraScript>().enabled = false;
        var bannerFadeOut = DOTween.To(() => MovingBannerObjects.BannerBackground.material.GetFloat(Property),
            x => MovingBannerObjects.BannerBackground.material.SetFloat(Property, x), 0.0f, 0.6f);
        yield return bannerFadeOut.WaitForCompletion();
        yield return new WaitWhile(() => audioPlayer.isPlaying);
        yield return new WaitForSeconds(0.2f);
        
        MovingBannerObjects.BannerBackground.gameObject.SetActive(false);

        // wait for 3 seconds to show marker detection
        audioPlayer.clip = BeepingSound;
        audioPlayer.Play();
        yield return new WaitWhile(() => audioPlayer.isPlaying);

        bannerParent.GetComponent<FollowCameraScript>().enabled = false;

    }

    public IEnumerator WaitForPositionAdjust()
    {
        // Start up, turn off blue banner + objects
        MovingBannerObjects.SetActive(false);
        StaticBannerObjects.SetActive(false);
        StaticBannerObjects.BannerBackground.material.SetFloat(Property, 0);
        StaticBannerObjects.LoadingCircle.gameObject.SetActive(false);
        StaticBannerObjects.Icon.gameObject.SetActive(false);
        StaticBannerObjects.Button.gameObject.SetActive(false);
        StaticBannerObjects.Text.alpha = 0;
        arucoMarkerVisual.gameObject.SetActive(true);
        PositionAdjustments.gameObject.SetActive(false);
        SpeedSlider.gameObject.SetActive(false);
        NoteSpawner.gameObject.SetActive(true);
        outline.gameObject.SetActive(true);
        
        // Make the PlaySpace Fall in
        KeyboardAndItems.gameObject.SetActive(true);
        StaticBannerObjects.SetActive(false);
        
        Vector3 targetPosition = PlaySpace.position;
        
        // Offset the current position, then make it fall
        KeyboardAndItems.position = new Vector3(targetPosition.x, targetPosition.y + 2, targetPosition.z);
        var FallDownTween = KeyboardAndItems.DOMoveY(targetPosition.y, FallSound.length).SetEase(fallingDownEase);
        FallingAudioSource.clip = FallSound;
        FallingAudioSource.Play();
        yield return FallDownTween.WaitForCompletion();
        yield return new WaitWhile(() => FallingAudioSource.isPlaying);

        // Slam
        OnKeyboardDropped?.Invoke();
        audioPlayer.clip = ThudSound;
        audioPlayer.Play();
        
        // Pulse all the keys
        foreach (var child in Keyboard.GetComponentsInChildren<Transform>())
        {
            child.GetComponent<keyboardKeyHighlights>()?.SetModeIrridescentPulse(true);
            yield return null;
        }

        
        yield return new WaitWhile(() => audioPlayer.isPlaying);
        
        // Fade out the marker
        var fadeOutMarker = arucoMarkerVisual.GetComponent<MeshRenderer>().material.DOFade(0.0f, 0.5f);
        yield return fadeOutMarker.WaitForCompletion();
        
        // Play Noise
        audioPlayer.clip = NotificationSound;
        audioPlayer.Play();
        
        // Fade in static blue banner
        StaticBannerObjects.SetActive(true);
        StaticBannerObjects.BannerBackground.gameObject.SetActive(true);
        var bannerFadeIn = DOTween.To(() => StaticBannerObjects.BannerBackground.material.GetFloat(Property),
            x => StaticBannerObjects.BannerBackground.material.SetFloat(Property, x), 0.750f, 2.0f);
        yield return bannerFadeIn.WaitForCompletion();
        
        audioPlayer.clip = NotificationSound;
        audioPlayer.Play();
        
        // Fade in text saying to confirm placement
        StaticBannerObjects.Text.text = "Adjust Placement with sliders.";
        var textFadeIn = DOTween.To(() => StaticBannerObjects.Text.alpha, x => StaticBannerObjects.Text.alpha = x, 1.0f, 2.0f);
        yield return textFadeIn.WaitForCompletion();
        
        audioPlayer.clip = NotificationSound;
        audioPlayer.Play();
        
        // Turn on texture
        
        // Turn on button
        StaticBannerObjects.Button.gameObject.SetActive(true);

        // Now we wait for player to press confirm 
        OnWaitForPositionAdjust.Invoke();
        bool confirmed = false;

        void OnConfirmed()
        {
            confirmed = true;
        }

        StaticBannerObjects.Button.OnClick.AddListener(OnConfirmed);
        
        while(!confirmed)
            yield return null;
        
        // Stop pulse
        foreach (var child in Keyboard.GetComponentsInChildren<Transform>())
        {
            child.GetComponent<keyboardKeyHighlights>()?.SetModeOff();
            yield return null;
        }
        StaticBannerObjects.Button.OnClick.RemoveListener(OnConfirmed);
        StaticBannerObjects.Button.gameObject.SetActive(false);
        outline.gameObject.SetActive(false);
        audioPlayer.clip = WelcomeSound;
        audioPlayer.Play();
        AfterWaitForPositionAdjust.Invoke();
        var textFadeOu = DOTween.To(() => StaticBannerObjects.Text.alpha, x => StaticBannerObjects.Text.alpha = x, 0.0f, 1.0f);
        yield return textFadeOu.WaitForCompletion();
        yield return new WaitWhile(() => audioPlayer.isPlaying);
        

        Debug.Log("Finished Adjusting Position");
    }

    public IEnumerator WaitForBluetooth()
    {
        MovingBannerObjects.SetActive(false);
        StaticBannerObjects.SetActive(true);
        StaticBannerObjects.Button.gameObject.SetActive(false);
        StaticBannerObjects.Text.alpha = 0;
        
        // Fade in text
        StaticBannerObjects.Text.text = "Waiting for Bluetooth signal...";
        var textFadeIn = DOTween.To(() => StaticBannerObjects.Text.alpha, x => StaticBannerObjects.Text.alpha = x, 1.0f, 2.0f);
        yield return textFadeIn.WaitForCompletion();
        
        audioPlayer.clip = NotificationSound;
        audioPlayer.Play();
        
        // Turn on circle + icon
        StaticBannerObjects.LoadingCircle.gameObject.SetActive(true);
        StaticBannerObjects.Icon.gameObject.SetActive(true);
        
        var sequence = DOTween.Sequence().AppendCallback(() =>
        {
            audioPlayer.clip = WaitSound;
            audioPlayer.Play();
        }).AppendInterval(2.68f).SetLoops(-1);
        
        // Wait 5 seconds
        yield return new WaitForSeconds(3.0f);
        
        // Turn on BLE Object to detect bluetooth
        bleModel.gameObject.SetActive(true);
        
        if (!bleModel.IsConnected() && !bleModel.IsScanning())
            bleModel.StartScanHandler();
        

        
        OnWaitForBluetooth.Invoke();
        while(!bleModel.IsConnected())
            yield return null;
        sequence.Kill();
        
        audioPlayer.clip = WelcomeSound;
        audioPlayer.Play();
        var greencircle = StaticBannerObjects.LoadingCircle.GetComponent<SpriteRenderer>().DOColor(Color.green, 1.0f);
        yield return greencircle.WaitForCompletion();
        
        StaticBannerObjects.LoadingCircle.gameObject.SetActive(false);
        StaticBannerObjects.Icon.gameObject.SetActive(false);
        
        AfterWaitForBluetooth.Invoke();
    }

    public IEnumerator WaitForKeyboardNoteRange()
    {
        while (upperC - lowerC != 24)
        {
            yield return StartCoroutine(WaitForLowerC());
            yield return StartCoroutine(WaitForUpperC());

            if (upperC - lowerC != 24)
            {
                // TODO: ADD ERROR SOUND
            }
            
        }
        AssignLowerCEvent.Invoke(lowerC, 0);
        
        var bannerFadeOut = DOTween.To(() => StaticBannerObjects.BannerBackground.material.GetFloat(Property),
            x => StaticBannerObjects.BannerBackground.material.SetFloat(Property, x), 0.0f, 0.6f);
        yield return bannerFadeOut.WaitForCompletion();
        
        StaticBannerObjects.SetActive(false);
    }
    
    public IEnumerator WaitForLowerC()
    {
        StaticBannerObjects.Text.alpha = 0;
        
        // Fade in text
        StaticBannerObjects.Text.text = "Press Left Most C to continue ... ";
        var textFadeIn = DOTween.To(() => StaticBannerObjects.Text.alpha, x => StaticBannerObjects.Text.alpha = x, 1.0f, 2.0f);
        yield return textFadeIn.WaitForCompletion();
        
        OnWaitForLowerC.Invoke();
        bool CPressed = false;

        void OnKeyboardPressed(MidiNote note, int velocity)
        {
            if ((int) note % 12 == 0) // check that the note is a C
            {
                CPressed = true;
                lowerC = note;
            }
        }

        BleMidiBroadcaster.onNoteDown += OnKeyboardPressed;
        
        while(!CPressed)
            yield return null;
        
        BleMidiBroadcaster.onNoteDown -= OnKeyboardPressed;
        AfterWaitForLowerC.Invoke();
        
        var textFadeOut = DOTween.To(() => StaticBannerObjects.Text.alpha, x => StaticBannerObjects.Text.alpha = x, 0.0f, 0.6f);
        yield return textFadeOut.WaitForCompletion();
        
    }
    
    public IEnumerator WaitForUpperC()
    {
        StaticBannerObjects.Text.alpha = 0;
        
        // Fade in text
        StaticBannerObjects.Text.text = "Press The Right Most C to Begin ... ";
        var textFadeIn = DOTween.To(() => StaticBannerObjects.Text.alpha, x => StaticBannerObjects.Text.alpha = x, 1.0f, 2.0f);
        yield return textFadeIn.WaitForCompletion();
        
        OnWaitForUpperC.Invoke();
        bool CPressed = false;

        void OnKeyboardPressed(MidiNote note, int velocity)
        {
            if ((int) note % 12 == 0) // check that the note is a C
            {
                upperC = note;
                CPressed = true;
            }
        }

        BleMidiBroadcaster.onNoteDown += OnKeyboardPressed;
        
        while(!CPressed)
            yield return null;
        
        BleMidiBroadcaster.onNoteDown -= OnKeyboardPressed;
        AfterWaitForUpperC.Invoke();
        
        var textFadeOut = DOTween.To(() => StaticBannerObjects.Text.alpha, x => StaticBannerObjects.Text.alpha = x, 0.0f, 0.6f);
        yield return textFadeOut.WaitForCompletion();
    }
    
    
}
