using System;
using System.Text.RegularExpressions;
using System.Timers;
using System.Diagnostics;

namespace SoundSharp;

public class ClockChannel {
    public double Time = 0;
    public List<ClockEvent> Events = new();
}

public class ClockEvent {
    public double BeatLocation = 0;
    public double Length = 0;
    public int Channel = 0;

    public ClockEvent(double time, double callTime) {
        Length = time;
        BeatLocation = callTime;
    }
}

public class WaitEvent : ClockEvent {
    public WaitEvent(double time, double callTime) : base(time, callTime) { }
}

public class PlayEvent : ClockEvent {
    public PlayEvent(double time, double callTime, string file, string key, int volume = 100) : base(time, callTime) {
        File = file;
        Key = key;
        Volume = volume;
    }

    public string File = "";
    public string Key = "";
    public int Volume = 100;
}

public class BPMEvent : ClockEvent {
    public BPMEvent(double time, double callTime, int bpm) : base(time, callTime) {
        BPM = bpm;
    }

    public int BPM = 0;
}
