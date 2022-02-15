using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

public class BLE
{
    public class Impl
    {
        // dll calls
        public enum ScanStatus
        {
            PROCESSING,
            AVAILABLE,
            FINISHED
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DeviceUpdate
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string id;

            [MarshalAs(UnmanagedType.I1)] public bool isConnectable;
            [MarshalAs(UnmanagedType.I1)] public bool isConnectableUpdated;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string name;

            [MarshalAs(UnmanagedType.I1)] public bool nameUpdated;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Service
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string uuid;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Characteristic
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string uuid;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string userDescription;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BLEData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] buf;

            [MarshalAs(UnmanagedType.I2)] public short size;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string deviceId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string serviceUuid;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string characteristicUuid;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ErrorMessage
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string msg;
        };
#if UNITY_EDITOR
        const string DLLName = "BleWinrtDLLx64";
#elif UNITY_WSA
    const string DLLName = "BleWinrtDLLx86_ble";
#endif

#if UNITY_ANDROID
    public static void StartDeviceScan()
    {
        Debug.LogError("Bluetooth functionality not available for Android!");
    }

    public static ScanStatus PollDevice(ref DeviceUpdate device, bool block)
    {
        return ScanStatus.FINISHED;
    }

    public static void StopDeviceScan()
    {
    }

    public static void ScanServices(string deviceId)
    {
    }

    public static ScanStatus PollService(out Service service, bool block)
    {
        service = new Service();
        return ScanStatus.FINISHED;
    }


    public static void ScanCharacteristics(string deviceId, string serviceId)
    {
    }

    public static ScanStatus PollCharacteristic(out Characteristic characteristic, bool block)
    {
        characteristic = new Characteristic();
        return ScanStatus.FINISHED;
    }

    public static bool SubscribeCharacteristic(string deviceId, string serviceId, string characteristicId, bool block)
    {
        return false;
    }

    public static bool PollData(out BLEData data, bool block)
    {
        data = new BLEData();
        return false;
    }

    public static bool SendData(in BLEData data, bool block)
    {
        return false;
    }

    public static void Quit()
    {
    }

    public static void GetError(out ErrorMessage buf)
    {
        buf = new ErrorMessage();
    }
#else
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StartDeviceScan")]
        public static extern void StartDeviceScan();

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PollDevice")]
        public static extern ScanStatus PollDevice(ref DeviceUpdate device, bool block);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopDeviceScan")]
        public static extern void StopDeviceScan();

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ScanServices", CharSet =
    CharSet.Unicode)]
        public static extern void ScanServices(string deviceId);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PollService")]
        public static extern ScanStatus PollService(out Service service, bool block);


        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ScanCharacteristics", CharSet =
    CharSet.Unicode)]
        public static extern void ScanCharacteristics(string deviceId, string serviceId);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PollCharacteristic")]
        public static extern ScanStatus PollCharacteristic(out Characteristic characteristic, bool block);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SubscribeCharacteristic", CharSet =
    CharSet.Unicode)]
        public static extern bool SubscribeCharacteristic(string deviceId, string serviceId, string characteristicId, bool block);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PollData")]
        public static extern bool PollData(out BLEData data, bool block);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SendData")]
        public static extern bool SendData(in BLEData data, bool block);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Quit")]
        public static extern void Quit();

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetError")]
        public static extern void GetError(out ErrorMessage buf);
#endif
    }

    public static Thread scanThread;
    public static BLEScan currentScan = new BLEScan();
    public bool isConnected = false;

    public class BLEScan
    {
        public delegate void FoundDel(string deviceId, string deviceName);
        public delegate void FinishedDel();
        public FoundDel Found;
        public FinishedDel Finished;
        internal bool cancelled = false;

        public void Cancel()
        {
            cancelled = true;
            Impl.StopDeviceScan();
        }
    }

    // don't block the thread in the Found or Finished callback; it would disturb cancelling the scan
    public static BLEScan ScanDevices()
    {
        if (scanThread == Thread.CurrentThread)
            throw new InvalidOperationException("a new scan can not be started from a callback of the previous scan");
        else if (scanThread != null)
            throw new InvalidOperationException("the old scan is still running");
        currentScan.Found = null;
        currentScan.Finished = null;
        scanThread = new Thread(() =>
        {
            Impl.StartDeviceScan();
            Impl.DeviceUpdate res = new Impl.DeviceUpdate();
            List<string> deviceIds = new List<string>();
            Dictionary<string, string> deviceNames = new Dictionary<string, string>();
            //Impl.ScanStatus status;
            while (Impl.PollDevice(ref res, true) != Impl.ScanStatus.FINISHED)
            {
                if (res.nameUpdated)
                {
                    deviceIds.Add(res.id);
                    deviceNames.Add(res.id, res.name);
                }
                // connectable device
                if (deviceIds.Contains(res.id) && res.isConnectable)
                    currentScan.Found?.Invoke(res.id, deviceNames[res.id]);
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

    public static void RetrieveProfile(string deviceId, string serviceUuid)
    {
        Impl.ScanServices(deviceId);
        Impl.Service service = new Impl.Service();
        while (Impl.PollService(out service, true) != Impl.ScanStatus.FINISHED)
            Debug.Log("service found: " + service.uuid);
        // wait some delay to prevent error
        Thread.Sleep(200);
        Impl.ScanCharacteristics(deviceId, serviceUuid);
        Impl.Characteristic c = new Impl.Characteristic();
        while (Impl.PollCharacteristic(out c, true) != Impl.ScanStatus.FINISHED)
            Debug.Log("characteristic found: " + c.uuid + ", user description: " + c.userDescription);
    }

    public static bool Subscribe(string deviceId, string serviceUuids, string[] characteristicUuids)
    {
        foreach (string characteristicUuid in characteristicUuids)
        {
            bool res = Impl.SubscribeCharacteristic(deviceId, serviceUuids, characteristicUuid, true);
            if (!res)
                return false;
        }
        return true;
    }

    public bool Connect(string deviceId, string serviceUuid, string[] characteristicUuids)
    {
        if (isConnected)
            return false;
        Debug.Log("retrieving ble profile...");
        RetrieveProfile(deviceId, serviceUuid);
        if (GetError() != "Ok")
            throw new Exception("Connection failed: " + GetError());
        Debug.Log("subscribing to characteristics...");
        bool result = Subscribe(deviceId, serviceUuid, characteristicUuids);
        if (GetError() != "Ok" || !result)
            throw new Exception("Connection failed: " + GetError());
        isConnected = true;
        return true;
    }

    public static bool WritePackage(string deviceId, string serviceUuid, string characteristicUuid, byte[] data)
    {
        Impl.BLEData packageSend;
        packageSend.buf = new byte[512];
        packageSend.size = (short)data.Length;
        packageSend.deviceId = deviceId;
        packageSend.serviceUuid = serviceUuid;
        packageSend.characteristicUuid = characteristicUuid;
        return Impl.SendData(packageSend, false);
    }

    public static void ReadPackage()
    {
        Impl.BLEData packageReceived;
        bool result = Impl.PollData(out packageReceived, true);
        if (result)
        {
            if (packageReceived.size > 512)
                throw new ArgumentOutOfRangeException("Please keep your ble package at a size of maximum 512, cf. spec!\n"
                    + "This is to prevent package splitting and minimize latency.");
            Debug.Log("received package from characteristic: " + packageReceived.characteristicUuid
                + " and size " + packageReceived.size + " use packageReceived.buf to access the data.");
        }
    }

    public static Impl.BLEData ReadBytes(bool blocking)
    {
        Impl.BLEData packageReceived;
        bool result = Impl.PollData(out packageReceived, blocking);

        if (result)
        {
            Debug.Log("Size: " + packageReceived.size);
            Debug.Log("From: " + packageReceived.deviceId);

            if (packageReceived.size > 512)
                throw new ArgumentOutOfRangeException("Package too large.");

            return packageReceived;
        }
        else
        {
            return packageReceived;
        }
    }

    public void Close()
    {
        Impl.Quit();
        isConnected = false;
    }

    public static string GetError()
    {
        Impl.ErrorMessage buf;
        Impl.GetError(out buf);
        return buf.msg;
    }

    ~BLE()
    {
        Close();
    }
}