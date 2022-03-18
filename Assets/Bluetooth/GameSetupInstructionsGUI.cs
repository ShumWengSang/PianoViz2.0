using System;
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

public class GameSetupInstructionsGUI : MonoBehaviour
{
    [SerializeField] private BleModel bleModel;
    [SerializeField] private Ease rotationEase = Ease.InOutCirc;
    [SerializeField] private float rotationDuration = 1f;
    [SerializeField] private Transform loadingCircle;
    [SerializeField] private ButtonConfigHelper confirmButton;
    [SerializeField] private MeshRenderer bannerMesh;
    [SerializeField] private TextMeshPro bannerText;
    [SerializeField] private GameObject arUcoMarkerGameObject;
    [SerializeField] private Transform arucoMarkerVisual;
    [SerializeField] private SpriteRenderer iconVisual;
    [SerializeField] private AudioSource audioPlayer;
    [SerializeField] private Transform PlaySpace;
    private Transform PlaySpaceParent;
    private Transform bannerParent;

    [Header("Play Space Items"), SerializeField] private Transform PositionAdjustment;

    [NonSerialized] public BleMidiBroadcaster.OnNoteDown AssignLowerCEvent;


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

    [Space(10), Header("Sound Effects")]
    [SerializeField] private AudioClip NotificationSound;
    [SerializeField] private AudioClip WelcomeSound;
    [SerializeField] private AudioClip WaitSound;
    
    private static readonly int Property = Shader.PropertyToID("_IridescenceIntensity");

    // Start is called before the first frame update
    void Start()
    {
        loadingCircle.DOBlendableLocalRotateBy(new Vector3(0f, 0f, 360f), rotationDuration, RotateMode.FastBeyond360)
            .SetEase(rotationEase).SetLoops(-1);
        StartCoroutine(Run());
    }

    public IEnumerator Run()
    {
        Startup();
        yield return StartCoroutine(arucoMarkerSequence());
        // yield return StartCoroutine(WaitForPositionAdjust());
        // yield return StartCoroutine(WaitForBluetooth());
        // yield return StartCoroutine(WaitForLowerC());
        Finished();
    }

    private void Startup()
    {
        PlaySpaceParent = PlaySpace.parent;
        bannerParent = bannerMesh.transform.parent;

        
        bannerMesh.gameObject.SetActive(true);
        bannerMesh.material.SetFloat(Property, 0);

        bannerText.gameObject.SetActive(true);
        bannerText.alpha = 0.0f;
        
        loadingCircle.gameObject.SetActive(false);
        
        
        iconVisual.gameObject.SetActive(false);
        iconVisual.DOFade(1.0f, 0.0f);
        
        arucoMarkerVisual.gameObject.SetActive(false);

        OnStartup.Invoke();
    }

    private void Finished()
    {
        PlaySpace.parent = PlaySpaceParent;
        OnFinished.Invoke();
    }

    public IEnumerator arucoMarkerSequence()
    {
        bannerParent.GetComponent<FollowCameraScript>().enabled = true;
        yield return null;
        // wait 5 seconds
        yield return new WaitForSeconds(3.0f);

        audioPlayer.clip = NotificationSound;
        audioPlayer.Play();
        // bannerMesh.gameObject.SetActive(true);
        var quadFadeIn = DOTween.To(() => bannerMesh.material.GetFloat(Property),
            x => bannerMesh.material.SetFloat(Property, x), 0.75f, 2.0f);
        // Fade in the blue quad
        yield return quadFadeIn.WaitForCompletion();
        audioPlayer.Play();

        var bannerFadeIn = DOTween.To(() => bannerText.alpha, x => bannerText.alpha = x, 1.0f, 2.0f);
        // Fade in text
        yield return bannerFadeIn.WaitForCompletion();

        loadingCircle.gameObject.SetActive(true);
        iconVisual.gameObject.SetActive(true);

        // Invoke event
        OnStartarUcoSequence.Invoke();
        // Play notification sound
        audioPlayer.clip = NotificationSound;
        audioPlayer.Play();

        // Wait a bit
        yield return new WaitForSeconds(3.0f);
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

            void OnConfirmed(Transform trans)
            {
                if (trans)
                {
                    PlaySpace.position = trans.position;
                    PlaySpace.rotation = trans.rotation;
                }
                detected = true;
            }

            arUcoMarkerGameObject.GetComponent<ArUcoDetectionHoloLensUnity.ArUcoMarkerDetection>().onMarkerDetected.AddListener(OnConfirmed);

            while (!detected)
                yield return null;

            arUcoMarkerGameObject.GetComponent<ArUcoDetectionHoloLensUnity.ArUcoMarkerDetection>().onMarkerDetected.RemoveListener(OnConfirmed);
        }
        sequence.Kill();
        arucoMarkerVisual.gameObject.SetActive(true);
        
        // Play notification sound
        audioPlayer.clip = WelcomeSound;
        audioPlayer.Play();


        // Make marker visual


        // Detected marker, fade out background, icons, and text
        loadingCircle.GetComponent<SpriteRenderer>().DOFade(0.0f, 1.0f);
        bannerText.DOFade(0.0f, 0.5f);
        iconVisual.DOFade(0.0f, 0.5f);
        
        // Fade out + remove the banner
        bannerParent.GetComponent<FollowCameraScript>().enabled = false;
        var bannerFadeOut = DOTween.To(() => bannerMesh.material.GetFloat(Property),
            x => bannerMesh.material.SetFloat(Property, x), 0.0f, 0.6f);
        yield return bannerFadeOut.WaitForCompletion();
        yield return new WaitWhile(() => audioPlayer.isPlaying);
        bannerMesh.gameObject.SetActive(false);

        // wait for 3 seconds to show marker detection
        yield return new WaitForSeconds(3.0f);
        // Fade out the marker

        var fadeOutMarker = arucoMarkerVisual.GetComponent<MeshRenderer>().material.DOFade(0.0f, 0.5f);
        yield return fadeOutMarker.WaitForCompletion();

        // Play another sound
        audioPlayer.clip = NotificationSound;
        audioPlayer.Play();
        // Return when sound finishes playing
        yield return new WaitWhile(() => audioPlayer.isPlaying);
        
        bannerParent.GetComponent<FollowCameraScript>().enabled = false;
        PositionAdjustment.gameObject.SetActive(true);
    }

    public IEnumerator WaitForPositionAdjust()
    {
        OnWaitForPositionAdjust.Invoke();
        bool confirmed = false;

        void OnConfirmed()
        {
            confirmed = true;
        }

        confirmButton.OnClick.AddListener(OnConfirmed);
        
        while(!confirmed)
            yield return null;
        
        confirmButton.OnClick.RemoveListener(OnConfirmed);
        AfterWaitForPositionAdjust.Invoke();
    }

    public IEnumerator WaitForBluetooth()
    {
        if (!bleModel.IsConnected() && !bleModel.IsScanning())
            bleModel.StartScanHandler();
        
        OnWaitForBluetooth.Invoke();
        while(!bleModel.IsConnected())
            yield return null;
        AfterWaitForBluetooth.Invoke();
    }
    
    public IEnumerator WaitForLowerC()
    {
        OnWaitForLowerC.Invoke();
        bool CPressed = false;

        void OnKeyboardPressed(MidiNote note, int velocity)
        {
            if ((int) note % 12 == 0) // check that the note is a C
            {
                CPressed = true;
                AssignLowerCEvent.Invoke(note, velocity);
            }
        }

        BleMidiBroadcaster.onNoteDown += OnKeyboardPressed;
        
        while(!CPressed)
            yield return null;
        
        BleMidiBroadcaster.onNoteDown -= OnKeyboardPressed;
        AfterWaitForLowerC.Invoke();
    }
    
    
}
