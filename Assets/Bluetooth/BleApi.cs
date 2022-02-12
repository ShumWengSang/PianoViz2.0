﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

public class BleApi
{
    // dll calls
    public enum ScanStatus { PROCESSING, AVAILABLE, FINISHED };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DeviceUpdate
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string id;
        [MarshalAs(UnmanagedType.I1)]
        public bool isConnectable;
        [MarshalAs(UnmanagedType.I1)]
        public bool isConnectableUpdated;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string name;
        [MarshalAs(UnmanagedType.I1)]
        public bool nameUpdated;
    }
#if UNITY_EDITOR
    const string DLLName = "BleWinrtDLLx64";
#elif UNITY_WSA
    const string DLLName = "BleWinrtDLLx86_ble";
#endif
     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "StartDeviceScan")]
    public static extern void StartDeviceScan();

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "PollDevice")]
    public static extern ScanStatus PollDevice(ref DeviceUpdate device, bool block);

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "StopDeviceScan")]
    public static extern void StopDeviceScan();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Service
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string uuid;
    };

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "ScanServices", CharSet = CharSet.Unicode)]
    public static extern void ScanServices(string deviceId);

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "PollService")]
    public static extern ScanStatus PollService(out Service service, bool block);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Characteristic
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string uuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string userDescription;
    };

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "ScanCharacteristics", CharSet = CharSet.Unicode)]
    public static extern void ScanCharacteristics(string deviceId, string serviceId);

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "PollCharacteristic")]
    public static extern ScanStatus PollCharacteristic(out Characteristic characteristic, bool block);

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "SubscribeCharacteristic", CharSet = CharSet.Unicode)]
    public static extern bool SubscribeCharacteristic(string deviceId, string serviceId, string characteristicId, bool block);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct BLEData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] buf;
        [MarshalAs(UnmanagedType.I2)]
        public short size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string deviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string serviceUuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string characteristicUuid;
    };

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "PollData")]
    public static extern bool PollData(out BLEData data, bool block);

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "SendData")]
    public static extern bool SendData(in BLEData data, bool block);

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "Quit")]
    public static extern void Quit();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ErrorMessage
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string msg;
    };

     [DllImport(DLLName, CallingConvention=CallingConvention.Cdecl, EntryPoint = "GetError")]
    public static extern void GetError(out ErrorMessage buf);
}