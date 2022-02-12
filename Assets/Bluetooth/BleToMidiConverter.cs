using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class BleMIDI
{
    static string BleMIDI_Service_ID = "03B80E5A-EDE8-4B33-A751-6CE34EC4C700";
    static string BleMIDI_Data_Characterisic = "7772E5DB-3868-4112-A1A9-F2669D106BF3";

    // 0 to 127
    public byte note;

    // 0 to 127
    public byte velocity = 0;

    // 0 - 127
    public byte pressure;

    public enum EventType
    {
        NoteOn,
        NoteOff,
        Pressure,
        Unknown
    }
    public EventType eventType;

    public override string ToString()
    {
        if (eventType == EventType.Pressure)
            return "";
        string result = "Event: " + eventType.ToString() + " Note: [" + note.ToString() + "], Velocity: [" + velocity.ToString() + "], Pressure: [" + pressure.ToString() +"]";
        return result;
    }
}

public class BleToMidiConverter
{
    public static List<BleMIDI> Convert(byte[] bytes, int size)
    {
        List<BleMIDI> arrayOfBleEvents = new List<BleMIDI>();
        BleMIDI.EventType eventType = BleMIDI.EventType.Unknown;

        int pos = 0;
        while(pos < size - 1)
        {
            // Header
            if (pos == 0)
            {
                // skip header
                pos += 1;
            }
            // Time Stamp
            if(checkSeventhBit(bytes[pos]))
            {
                // skip Time stamp
                pos += 1;
                
                // look for status byte to read an EventType
                if (checkSeventhBit(bytes[pos]))
                {
                    eventType = DetermineType(bytes[pos]);
                    pos += 1;
                }
            }
            
            // eventType should be accurate by this point. read the rest of the data
            BleMIDI res = new BleMIDI();
            res.eventType = eventType;
            pos += BuildMidiEvent(bytes, arrayOfBleEvents, pos, res);
        }

        return arrayOfBleEvents;
    }

    // Shifts by one to the left
    private static byte GetMIDIData(byte dataByte)
    {
        return (byte)(dataByte & ((1 << 7) - (byte)1));
    }

    private static int BuildMidiEvent(byte[] bytes, List<BleMIDI> arrayOfBleEvents, int pos, BleMIDI res)
    {
        arrayOfBleEvents.Add(res);
        switch (res.eventType)
        {
            case BleMIDI.EventType.NoteOn:
                res.note = GetMIDIData(bytes[pos]);
                res.velocity = GetMIDIData(bytes[pos + 1]);
                return 2;
                
            case BleMIDI.EventType.NoteOff:
                res.note = GetMIDIData(bytes[pos]);
                res.velocity = GetMIDIData(bytes[pos + 1]);
                return 2;
            case BleMIDI.EventType.Pressure:
                res.note = GetMIDIData(bytes[pos + 1]);
                res.pressure = GetMIDIData(bytes[pos + 1]);
                return 2;
            default:
                Debug.LogError("Error! Event unknown!");
                return 1;
        }
    }

    private static bool checkSeventhBit(byte b)
    {
        return (b & (1 << 7)) != 0;
    }

    private static BleMIDI.EventType DetermineType(byte midiStatusByte)
    {
        // http://www.somascape.org/midi/tech/mfile.html#mthd
        switch (midiStatusByte)
        {
            case 0x80:
                return BleMIDI.EventType.NoteOff;
            case 0x90:
                return BleMIDI.EventType.NoteOn;
            case 0xA0:
                return BleMIDI.EventType.Pressure;
            default:
                return BleMIDI.EventType.Unknown;
        }
    }
}

public class BleMidiBroadcaster
{
    public delegate void OnNoteDown(int note, int velocity);
    public static OnNoteDown onNoteDown;

    public delegate void OnNoteUp(int note, int velocity);
    public static OnNoteUp onNoteUp;
}