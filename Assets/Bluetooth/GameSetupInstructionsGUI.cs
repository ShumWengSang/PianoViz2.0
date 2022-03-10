using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Microsoft.MixedReality.Toolkit.UI;
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

    [NonSerialized] public BleMidiBroadcaster.OnNoteDown AssignLowerCEvent;

    [Header("Setup And Teardown")]
    public UnityEvent OnStartup;
    public UnityEvent OnFinished;
    
    [Header("Wait For Position Adjust")]
    public UnityEvent OnWaitForPositionAdjust;
    public UnityEvent AfterWaitForPositionAdjust;
    
    [Header("Wait For Bluetooth to connect")]
    public UnityEvent OnWaitForBluetooth;
    public UnityEvent AfterWaitForBluetooth;
    
    [Header("Wait For Lower C to be pressed")]
    public UnityEvent OnWaitForLowerC;
    public UnityEvent AfterWaitForLowerC;

    // Start is called before the first frame update
    void Start()
    {
        loadingCircle.DOBlendableLocalRotateBy(new Vector3(0f, 0f, 360f), rotationDuration, RotateMode.FastBeyond360)
            .SetEase(rotationEase).SetLoops(-1);
        StartCoroutine(Run());
    }

    public IEnumerator Run()
    {
        OnStartup.Invoke();
        yield return StartCoroutine(WaitForPositionAdjust());
        yield return StartCoroutine(WaitForBluetooth());
        yield return StartCoroutine(WaitForLowerC());
        OnFinished.Invoke();
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
