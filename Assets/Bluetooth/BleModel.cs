using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BleModel : MonoBehaviour
{
    [SerializeField]
    private string characteristicID = BleMIDI.BleMIDI_Data_Characterisic;
    [SerializeField]
    private string serviceID = BleMIDI.BleMIDI_Service_ID;
    private Thread connectionThread;
    private BleWrapper bleWrapper = new BleWrapper();
    private BleWrapper.BleScan scan;
    [SerializeField]
    private IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();

    [System.Serializable]
    struct DeviceConnector
    {
        public string deviceID;
        public string serviceID;
        public string characteristicUUid;
    }
    
    public void ScanBleDevices()
    {
        scan = BleWrapper.ScanDevices();
        Debug.Log("BLE.ScanDevices() started.");
        scan.Found = (_deviceId, deviceName) =>
        {
            Debug.Log("found device with name: " + deviceName);
            if (!discoveredDevices.ContainsKey(_deviceId))
                discoveredDevices.Add(_deviceId, deviceName);
        };

        scan.Finished = () =>
        {
            Debug.Log("scan finished");
            StartConHandler();
        };
    }

    // Start establish BLE connection with
    // target device in dedicated thread.
    public void StartConHandler()
    {
        DeviceConnector[] deviceConnectors = new DeviceConnector[discoveredDevices.Count];
        int i = 0;
        foreach (var device in discoveredDevices)
        {
            deviceConnectors[i].deviceID = device.Key;
            deviceConnectors[i].characteristicUUid = characteristicID;
            deviceConnectors[i].serviceID = serviceID;
            i++;
        }
        connectionThread = new Thread(ConnectBleDevice);
        connectionThread.Start(deviceConnectors);
    }

    private void ConnectBleDevice( object obj)
    {
        DeviceConnector[] connectors = (DeviceConnector[])obj;
        foreach (var connector in connectors)
        {
            if (connector.deviceID != null)
            {
                try
                {
                    if (bleWrapper.Connect(connector.deviceID,
                            connector.serviceID,
                            connector.characteristicUUid))
                    {
                        Debug.Log("Connection to " + connector.deviceID + " successful");
                    }
                    else
                    {
                        Debug.Log("Warning: Connection to " + connector.deviceID + " unsuccessful");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Could not establish connection to device with ID " + connector.deviceID + "\n" + e);
                }
            }

            if (bleWrapper.IsConnected)
            {
                Debug.Log("Connected to: " + bleWrapper.CurrentlyConnectedDeviceID);
                return;
            }
        }
    }

    private void Update()
    {
        if (bleWrapper.IsConnected)
        {
            BleApi.BLEData res = new BleApi.BLEData();
            while (BleApi.PollData(out res, false))
            {
                var arrayOfMidies = BleToMidiConverter.Convert(res.buf, res.size);

                var resultingStr = "";
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
            }
        }
    }

}
