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
        // Example:
        // C4 = 0
        // C#4 = 1
        // D4 = 2
        // C5 = 12
        // C3 = -12

        int octave = 0;
        int note = 0;
        switch (key[0]) {
            case 'c': note = 0; break;
            case 'd': note = 2; break;
            case 'e': note = 4; break;
            case 'f': note = 5; break;
            case 'g': note = 7; break;
            case 'a': note = 9; break;
            case 'b': note = 11; break;
            default: note = 0; break;
        }

        if (key.Length > 2) {
            octave = int.Parse(key.Substring(2));
            if (key[1] == '#') note++;
            else if (key[1] == 'b') note--;
        } else {
            octave = int.Parse(key.Substring(1));
        }

        return (octave - 4) * 12 + note;
    }

    public static void Play(string path, string key, int volume = 100) {
        // Load path and pitch it up 5 semitones
        int stream = Bass.CreateStream("samples/" + path + ".wav", 0, 0, BassFlags.Decode);
        int pitch = BassFx.TempoCreate(stream, BassFlags.Default);
        Bass.ChannelSetAttribute(pitch, ChannelAttribute.Pitch, KeysToSemiRelToC4(key));
        Bass.ChannelSetAttribute(pitch, ChannelAttribute.Volume, volume / 100f);

        // Play the stream
        Bass.ChannelPlay(pitch);
    }
}