using System;
using System.Text.RegularExpressions;
using System.Timers;
using System.Diagnostics;

namespace SoundSharp;

public class Session {
    public Dictionary<string, string> Variables = new Dictionary<string, string>();
    public ClockSystem System = new ClockSystem();

    public double? ParseDouble(string value) {
        if (value.IndexOf('/') != -1) {
            string[] split = value.Split('/');
            double numerator = 0;
            double denominator = 0;
            if (double.TryParse(split[0], out numerator) && double.TryParse(split[1], out denominator))
                return numerator / denominator;
        }

        int result = 0;
        if (int.TryParse(value, out result)) return result;
        return null;
    }

    public int? ParseInt(string value) {
        int result = 0;
        if (int.TryParse(value, out result)) return result;
        return null;
    }
}

public class ClockEvent {
    public double CallTime = 0;
    public double Time = 0;
    public int Channel = 0;
    
    public enum Type {
        Wait,
        Play
    }

    public Type EventType = Type.Wait;
    public string? File = null;
    public string? Key = null;
}

public class ClockChannel {
    public double Time = 0;
    public List<ClockEvent> Events = new();
}

public class ClockSystem {
    public int BPM = 120;
    public List<ClockChannel> Channels = new();

    public void AddEvent(int channel, double time, string? file = null, string? key = null) {
        if (Channels.Count <= channel) {
            for (int i = Channels.Count; i <= channel; i++) Channels.Add(new ClockChannel());
        }

        ClockChannel clockChannel = Channels[channel];

        clockChannel.Events.Add(new ClockEvent() {
            CallTime = clockChannel.Time,
            EventType = file == null ? ClockEvent.Type.Wait : ClockEvent.Type.Play,
            Channel = channel,
            Time = time,
            File = file,
            Key = key
        });

        clockChannel.Events.Sort((a, b) => a.CallTime.CompareTo(b.CallTime));
        clockChannel.Time += time;
    }

    public void ExecuteEvents() {
        List<ClockEvent> events = new();
        foreach (ClockChannel channel in Channels) events.AddRange(channel.Events);
        events.Sort((a, b) => a.CallTime.CompareTo(b.CallTime));

        double BPMTime = 60000 / BPM;

        Stopwatch stopwatch = new();
        stopwatch.Start();

        double endTime = (events[events.Count - 1].CallTime * BPMTime) + (BPMTime * 4);
        double currentTime = 0;
        // int currentEvent = 0;

        // Console.WriteLine(endTime);

        // while (currentTime < endTime) {
        //     currentTime = stopwatch.ElapsedMilliseconds;
        //     if (currentEvent < events.Count && currentTime >= events[currentEvent].CallTime * BPMTime) {
        //         ClockEvent clockEvent = events[currentEvent];
        //         Console.SetCursorPosition(0, 0);
        //         if (clockEvent.EventType == ClockEvent.Type.Wait) {
        //             Console.Write($"Waited {clockEvent.Time}ms" + new string(' ', 20));
        //         } else if (clockEvent.EventType == ClockEvent.Type.Play && clockEvent.File != null) {
        //             Console.Write($"Played {clockEvent.File} at {clockEvent.Key}" + new string(' ', 20));
        //             Audio.Play(clockEvent.File, clockEvent.Key ?? "");
        //         }

        //         currentEvent++;
        //     }
        //     Thread.Sleep(1);
        // }
        int[] currentEvent = new int[Channels.Count];

        while (currentTime < endTime) {
            currentTime = stopwatch.ElapsedMilliseconds;
            for (int i = 0; i < Channels.Count; i++) {
                ClockChannel channel = Channels[i];
                if (currentEvent[i] < channel.Events.Count && currentTime >= channel.Events[currentEvent[i]].CallTime * BPMTime) {
                    ClockEvent clockEvent = channel.Events[currentEvent[i]];
                    // Console.SetCursorPosition(0, 0);
                    if (clockEvent.EventType == ClockEvent.Type.Wait) {
                        Console.WriteLine($"Waited {clockEvent.Time * BPMTime}ms");
                    } else if (clockEvent.EventType == ClockEvent.Type.Play && clockEvent.File != null) {
                        Console.WriteLine($"Played {clockEvent.File} at {clockEvent.Key}" + " - " + channel.Events[currentEvent[i]].CallTime * BPMTime);
                        Audio.Play(clockEvent.File, clockEvent.Key ?? "C4");
                    }

                    currentEvent[i]++;
                }
            }
            Thread.Sleep(1);
        }
    }
}

public class Reader {
    // { (absolutely everything) }
    public static string FunctionContentsRegex = @"\{(?:[^{}]|(?<c>\{)|(?<-c>\}))*\}(?(c)(?!))";

    public Session Session = new Session();
    
    public void ReadFile(string path) {
        if (!File.Exists(path)) {
            Console.WriteLine("File does not exist.");
            return;
        }

        string text = File.ReadAllText(path);
        ExecuteText(text);
    }

    public int Indent = 0;

    public void ExecuteText(string text) {
        Indent++;
        string indentString = new string(' ', (Indent - 1) * 4);

        List<string> functionContentArr = new List<string>();
        Regex functionContentsRegex = new Regex(FunctionContentsRegex);

        MatchCollection functionContentsMatches = functionContentsRegex.Matches(text);
        foreach (Match match in functionContentsMatches) functionContentArr.Add(match.Value);

        text = functionContentsRegex.Replace(text, "(__FUNCTION_CONTENTS__)}");
        string[] lines = text.Split(';', '}');
        
        for (int i = 0; i < lines.Length; i++) {
            if (lines[i].IndexOf("(__FUNCTION_CONTENTS__)") != -1) {
                lines[i] = lines[i].Replace("(__FUNCTION_CONTENTS__)", functionContentArr[0]);
                functionContentArr.RemoveAt(0);
            }
        }

        // name(args) { content }
        // Capture:
        // (name)((args)) { (everything beyond, until end of line/string)
        Regex functionRegex = new Regex(@"([a-zA-Z0-9]+)\s*\(([^)]*)\)\s*\{");

        foreach (string line in lines) {
            string trimmedLine = line.Trim();
            if (trimmedLine.Length == 0) continue;
            string trimmedLineLower = trimmedLine.ToLower();
            Console.WriteLine("\"" + line + "\"");

            Match functionMatch = functionRegex.Match(trimmedLine);
            if (functionMatch.Success) {
                string name = functionMatch.Groups[1].Value.ToLower();
                string args = functionMatch.Groups[2].Value;
                string content = line.Substring(line.IndexOf('{') + 1).Trim();

                switch (name) {
                    case "loop":
                        int? loopCount = Session.ParseInt(args);
                        if (loopCount == null) break;
                        Console.WriteLine(indentString + $"Looping {loopCount} times...");
                        for (int i = 0; i < loopCount; i++) ExecuteText(content);
                        break;

                    case "thread":
                        Console.WriteLine(indentString + "Starting thread...");
                        ExecuteText(content);
                        break;
                }
            }
        }

        // ClockSystem clockSystem = Session.System;
        // clockSystem.BPM = 70;

        // // Billie Jean
        // string[] notes = new[] {
        //     "E4", "B3", "D4", "E4", "D4", "B3", "A3", "B3"
        // };


        // for (int i = 0; i < notes.Length * (4 / 2); i++) {
        //     clockSystem.AddEvent(1, 1, "click.wav", (i % 4 == 0 ? "C5" : "C4"));
        // }

        // for (int i = 0; i < notes.Length * (4 / 1); i++) {
        //     clockSystem.AddEvent(0, 0.5, "piano.wav", notes[i % notes.Length]);
        // }
        
        // clockSystem.ExecuteEvents();

        Indent--;
    }
}