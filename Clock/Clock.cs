using System;
using System.Text.RegularExpressions;
using System.Timers;
using System.Diagnostics;

namespace SoundSharp;

public class ClockSystem {
    public int BPM = 120;
    public List<ClockChannel> Channels = new();

    public void AddNoteEvent(int channel, string sample, string note, double length) {
        while (Channels.Count <= channel) Channels.Add(new ClockChannel());
        Channels[channel].Events.Add(new PlayEvent(length, Channels[channel].Time, sample, note));
        Channels[channel].Time += length;
        // Console.WriteLine("Adding note event - " + sample + " - " + note + " - " + length + " - " + Channels[channel].Time + " - c" + channel);
    }

    public void AddWaitEvent(int channel, double length) {
        while (Channels.Count <= channel) Channels.Add(new ClockChannel());
        Channels[channel].Events.Add(new WaitEvent(length, Channels[channel].Time));
        Channels[channel].Time += length;
    }

    public void AddBPMEvent(int channel, int bpm) {
        while (Channels.Count <= channel) Channels.Add(new ClockChannel());
        Channels[channel].Events.Add(new BPMEvent(0, Channels[channel].Time, bpm));
    }
    
    public void ExecuteEvents() {
        List<ClockEvent> events = new();
        foreach (ClockChannel channel in Channels) {
            List<ClockEvent> newEvents = channel.Events;
            events.AddRange(newEvents);
            events.Sort((a, b) => a.BeatLocation.CompareTo(b.BeatLocation));
            channel.Events = newEvents;
        }

        double BPMTime = 60000 / BPM;

        Stopwatch stopwatch = new();
        stopwatch.Start();

        double endTime = (events[events.Count - 1].BeatLocation * BPMTime) + (BPMTime * 4);
        double currentTime = 0;

        int[] currentEvent = new int[Channels.Count];

        while (currentTime < endTime) {
            currentTime = stopwatch.ElapsedMilliseconds;

            for (int i = 0; i < Channels.Count; i++) {
                if (currentEvent[i] >= Channels[i].Events.Count) continue;
                ClockEvent clockEvent = Channels[i].Events[currentEvent[i]];
                bool isAtEvent = currentTime >= (clockEvent.BeatLocation * BPMTime);
                if (!isAtEvent) continue;
                else currentEvent[i]++;

                if (clockEvent is PlayEvent playEvent) {
                    Audio.Play(playEvent.File, playEvent.Key, playEvent.Volume);
                } else if (clockEvent is BPMEvent bpmEvent) {
                    BPM = bpmEvent.BPM;
                    BPMTime = 60000 / BPM;
                }
            }
            
            Thread.Sleep(1);
        }
    }
}
