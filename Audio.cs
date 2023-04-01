using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ManagedBass;
using ManagedBass.Fx;

namespace SoundSharp;

public static class Audio {
    public static int KeysToSemiRelToC4(string key) {
        key = key.ToLower();
        int distance = 0;
        int octave = 0;
        char note = key[0];
        if (key.Length > 2) octave = int.Parse(key[2].ToString());
        else if (key.Length > 1) octave = int.Parse(key[1].ToString());

        switch (note) {
            case 'c': distance = 0; break;
            case 'd': distance = 2; break;
            case 'e': distance = 4; break;
            case 'f': distance = 5; break;
            case 'g': distance = 7; break;
            case 'a': distance = 9; break;
            case 'b': distance = 11; break;
        }

        if (key.Length > 1) {
            if (key[1] == '#') distance++;
            else if (key[1] == 'b') distance--;
        }

        return (octave - 4) * 12 + distance;
    }

    public static void Play(string path, string key) {
        // Load path and pitch it up 5 semitones
        int stream = Bass.CreateStream(path, 0, 0, BassFlags.Decode);
        int pitch = BassFx.TempoCreate(stream, BassFlags.Default);
        Bass.ChannelSetAttribute(pitch, ChannelAttribute.Pitch, KeysToSemiRelToC4(key));

        // Play the stream
        Bass.ChannelPlay(pitch);
    }
}