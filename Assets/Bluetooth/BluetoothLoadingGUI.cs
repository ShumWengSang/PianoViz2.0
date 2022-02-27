﻿using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public class BluetoothLoadingGUI : MonoBehaviour
{
    [SerializeField] private BleModel bleModel;
    [SerializeField] private Ease rotationEase = Ease.InOutCirc;
    [SerializeField] private float rotationDuration = 1f;
    public UnityEvent OnConnected;
    
    // Start is called before the first frame update
    void Start()
    {
        if (!bleModel.IsConnected() && !bleModel.IsScanning())
        {
            bleModel.StartScanHandler();
        }

        transform.DOBlendableLocalRotateBy(new Vector3(0f, 0f, 360f), rotationDuration, RotateMode.FastBeyond360)
            .SetEase(rotationEase).SetLoops(-1);
    }

    // Update is called once per frame
    void Update()
    {
        if (bleModel.IsConnected())
        {
            OnConnected.Invoke();
        }
        
    }
}
