using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BleModel : MonoBehaviour
{
    // Change this to match your device.
    public string targetDeviceName = "EnterDeviceNameHere";
    string serviceUuid = "{03b80e5a-ede8-4b33-a751-6ce34ec4c700}";
    string[] characteristicUuids = {
         "{7772e5db-3868-4112-a1a9-f2669d106bf3}", 
    };

    private BLE ble;
    private BLE.BLEScan scan;
    private bool isScanning = false, isConnected = false;
    private string deviceId = null;
    private IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();
    private int devicesCount = 0;

    // BLE Threads 
    private Thread scanningThread, connectionThread, readingThread;

    // Start is called before the first frame update
    void Start()
    {
        ble = new BLE();
        readingThread = new Thread(ReadBleData);
    }

    // Update is called once per frame
    void Update()
    {   
        // The target device was found.
        if (deviceId != null && deviceId != "-1")
        {
            // Target device is connected
            if (ble.isConnected != isConnected)
            {
                // Todo: Change GUI event
                ble.isConnected = isConnected;
            }
        }
        if(discoveredDevices.Count != devicesCount)
        {
            devicesCount = discoveredDevices.Count;
            // Todo: Update GUI event
        }
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    private void OnApplicationQuit()
    {
        CleanUp();
    }

    // Prevent threading issues and free BLE stack.
    // Can cause Unity to freeze and lead
    // to errors when omitted.
    private void CleanUp()
    {
        try
        {
            scan.Cancel();
            ble.Close();
            scanningThread.Abort();
            connectionThread.Abort();
            readingThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Thread or object never initialized.\n" + e);
        }
    }

    public void StartScanHandler()
    {
        devicesCount = 0;
        isScanning = true;
        discoveredDevices.Clear();
        scanningThread = new Thread(ScanBleDevices);
        scanningThread.Start();
    }

    public void ResetHandler()
    {
        // Reset previous discovered devices
        discoveredDevices.Clear();
        deviceId = null;
        CleanUp();
    }

    private void ReadBleData(object obj)
    {
        while (true)
        {
            var packageReceived = BLE.ReadBytes(false);

            var arrayOfMidies = BleToMidiConverter.Convert(packageReceived.buf, packageReceived.size);
            var resultingStr = "";

            // Todo: Refactor this so we get events from BLEModel and some
            //      other module handle midi conversion and midi event firing
            foreach (BleMIDI midi in arrayOfMidies)
            {
                resultingStr += midi.ToString() + "\n";
                if (midi.eventType == BleMIDI.EventType.NoteOn)
                {
                    if (BleMidiBroadcaster.onNoteDown != null)
                    {
                        BleMidiBroadcaster.onNoteDown(midi.note, midi.velocity);
                    }
                }
                else if (midi.eventType == BleMIDI.EventType.NoteOff)
                {
                    if (BleMidiBroadcaster.onNoteUp != null)
                    {
                        BleMidiBroadcaster.onNoteUp(midi.note, midi.velocity);
                    }
                }
            }
            if (!String.IsNullOrEmpty(resultingStr))
            {
                Debug.Log(resultingStr);
            }
            Thread.Sleep(10);
        }
    }

    void ScanBleDevices()
    {
        scan = BLE.ScanDevices();
        Debug.Log("BLE.ScanDevices() started.");
        scan.Found = (_deviceId, deviceName) =>
        {
            Debug.Log("found device with name: " + deviceName);
            discoveredDevices.Add(_deviceId, deviceName);

            if (deviceId == null && deviceName == targetDeviceName)
                deviceId = _deviceId;
        };

        scan.Finished = () =>
        {
            isScanning = false;
            Debug.Log("scan finished");
            if (deviceId == null)
                deviceId = "-1";
        };
        while (deviceId == null)
            Thread.Sleep(500);
        scan.Cancel();
        scanningThread = null;
        isScanning = false;

        if (deviceId == "-1")
        {
            Debug.Log("no device found!");
            return;
        }
        StartConHandler();

    }

    // Start establish BLE connection with
    // target device in dedicated thread.
    public void StartConHandler()
    {
        connectionThread = new Thread(ConnectBleDevice);
        connectionThread.Start();
    }

    void ConnectBleDevice()
    {
        if (deviceId != null)
        {
            try
            {
                ble.Connect(deviceId,
                serviceUuid,
                characteristicUuids);
                readingThread.Start();
            }
            catch (Exception e)
            {
                Debug.Log("Could not establish connection to device with ID " + deviceId + "\n" + e);
            }
        }
        if (ble.isConnected)
            Debug.Log("Connected to: " + targetDeviceName);
    }

}
