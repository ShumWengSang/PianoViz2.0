using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BleWrapper
{
    //////// Public Data Structures
    
    /// <summary>
    /// Use this in conjunction with ScanDevices() by subscribing to events within this object
    /// </summary>
    public class BleScan
    {
        public delegate void FoundDel(string deviceId, string deviceName);
        public delegate void FinishedDel();
        public FoundDel Found;
        public FinishedDel Finished;
        internal bool cancelled = false;

        public void Cancel()
        {
            cancelled = true;
            BleApi.StopDeviceScan();
        }

        public void Reset()
        {
            Found = null;
            Finished = null;
        }
        
    }
    //////// Public Properties
    
    /// <summary>
    /// Returns if the current BLE is connected
    /// </summary>
    public bool IsConnected
    {
        get;
        set;
    } = false;

    /// <summary>
    /// Returns the device id that the BLE is currently connected to
    /// </summary>
    public string CurrentlyConnectedDeviceID
    {
        get;
        set;
    }
    
    //////// Private Variables
    private static Thread scanThread;
    private static BleScan currentScan = new BleScan();
    
    //////// Public Static Methods
    
    /// <summary>
    /// Starts an async thread the scans for BLE devices
    /// </summary>
    /// <returns>
    /// A BleScan object which user can subscribe to Found Device and Finished events.
    /// </returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static BleScan ScanDevices()
    {
        if (scanThread == Thread.CurrentThread)
            throw new InvalidOperationException("Please use another thread to scan");
        else if (scanThread != null)
            throw new InvalidOperationException("Scan already running!");
        currentScan.Reset();

        scanThread = new Thread(() =>
        {
            BleApi.StartDeviceScan();
            BleApi.DeviceUpdate res = new BleApi.DeviceUpdate();
            
            // Device ID is a string
            List<string> deviceIds = new List<string>();
            Dictionary<string, string> deviceName = new Dictionary<string, string>();
            Dictionary<string, bool> deviceIsConnectable = new Dictionary<string, bool>();
            
            while (BleApi.PollDevice(ref res, true) != BleApi.ScanStatus.FINISHED)
            {
                if (!deviceIds.Contains(res.id))
                {
                    deviceIds.Add(res.id);
                    deviceName[res.id] = "";
                    deviceIsConnectable[res.id] = false;
                }
                if (res.nameUpdated)
                    deviceName[res.id] = res.name;
                if (res.isConnectableUpdated)
                    deviceIsConnectable[res.id] = res.isConnectable;
                // connectable device
                if (deviceName[res.id] != "" && deviceIsConnectable[res.id] == true)
                    currentScan.Found?.Invoke(res.id, deviceName[res.id]);
                // check if scan was cancelled in callback
                if (currentScan.cancelled)
                    break;
            }
            
            currentScan.Finished?.Invoke();
            scanThread = null;
        });
        
        scanThread.Start();
        return currentScan;
    }
    
    /// <summary>
    /// Still in work: Function puts out the service id and characteristic of device ID in debug.Log()
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="serviceUuid"></param>
    public static void RetrieveProfile(string deviceId, string serviceUuid)
    {
        BleApi.ScanServices(deviceId);
        // Get services
        
        BleApi.Service service = new BleApi.Service();
        while (BleApi.PollService(out service, true) != BleApi.ScanStatus.FINISHED)
            Debug.Log("service found: " + service.uuid);
        // wait to avoid error
        Thread.Sleep(200);
        
        // Get characteristics
        BleApi.ScanCharacteristics(deviceId, serviceUuid);
        BleApi.Characteristic c = new BleApi.Characteristic();
        while (BleApi.PollCharacteristic(out c, true) != BleApi.ScanStatus.FINISHED)
            Debug.Log("characteristic found: " + c.uuid + ", user description: " + c.userDescription);

        return;
    }
    
    /// <summary>
    /// Shortcut to GetError() from C++ side
    /// </summary>
    /// <returns></returns>
    public static string GetError()
    {
        BleApi.ErrorMessage buf;
        BleApi.GetError(out buf);
        return buf.msg;
    }
    
    /// <summary>
    /// Read bytes from connected BLE target
    /// </summary>
    /// <param name="blocking"></param>
    /// <returns>
    /// Returns the byte[] data if something is read; else [0]
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static byte[] ReadBytes(bool blocking = true)
    {
        BleApi.BLEData packageReceived;
        bool result = BleApi.PollData(out packageReceived, blocking);

        if (result)
        {
            // Debug.Log("Size: " + packageReceived.size);
            // Debug.Log("From: " + packageReceived.deviceId);

            if (packageReceived.size > 512)
                throw new ArgumentOutOfRangeException("Package too large.");

            return packageReceived.buf;
        } else
        {
            return new byte[] { 0x0 };
        }
    }
    
    
    //////// Public Methods
    
    /// <summary>
    ///  Given a deviceID, and its BLE service and characterisric id, connect to device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="serviceUuid"></param>
    /// <param name="characteristicUuid"></param>
    /// <returns>
    /// True when successful, else false
    /// </returns>
    /// <exception cref="Exception">
    /// Throws exception when connection failed with message describing error
    /// </exception>
    public bool Connect(string deviceId, string serviceUuid, string characteristicUuid)
    {
        if (IsConnected)
            return false;
        Debug.Log("retrieving ble profile...");
        RetrieveProfile(deviceId, serviceUuid);
        if (GetError() != "Ok")
            throw new Exception("Connection failed: " + GetError());
        Debug.Log("subscribing to characteristics...");
        bool result = BleApi.SubscribeCharacteristic(deviceId, serviceUuid, characteristicUuid, false);
        if (GetError() != "Ok" || !result)
            throw new Exception("Connection failed: " + GetError());
        IsConnected = true;
        CurrentlyConnectedDeviceID = deviceId;
        return true;
    }
    
    /// <summary>
    /// Cleans up the internal thread and cleanly destroys resources
    /// </summary>
    public void Close()
    {
        BleApi.Quit();
        IsConnected = false;
    }
    
    ~BleWrapper()
    {
        Close();
    }
}
