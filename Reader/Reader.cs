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

        double result = 0;
        if (double.TryParse(value, out result)) return result;
        return null;
    }

    public int? ParseInt(string value) {
        int result = 0;
        if (int.TryParse(value, out result)) return result;
        return null;
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

    public void ExecuteText(string text, int channel = 0) {
        Indent++;
        string indentString = new string(' ', (Indent - 1) * 4);

        Stopwatch debug = new Stopwatch();
        debug.Start();

        double replaceStart = debug.Elapsed.TotalMilliseconds;

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

        double replaceEnd = debug.Elapsed.TotalMilliseconds;
        Console.WriteLine(indentString + "Replaced function contents in " + (replaceEnd - replaceStart) + "ms.");

        // name(args) { content }
        // Capture:
        // (name)((args)) { (everything beyond, until end of line/string)
        Regex functionRegex = new Regex(@"([a-zA-Z0-9]+)\s*\(([^)]*)\)\s*\{");
        
        // name(args)
        // Capture:
        // (name)((args))
        Regex callRegex = new Regex(@"([a-zA-Z0-9]+)\s*\(([^)]*)\)\s*");

        double loopStart = debug.Elapsed.TotalMilliseconds;
        List<double> functionRegexTimes = new List<double>();
        List<double> callRegexTimes = new List<double>();

        foreach (string line in lines) {
            string trimmedLine = line.Trim();
            if (trimmedLine.Length == 0) continue;
            string trimmedLineLower = trimmedLine.ToLower();
            // Console.WriteLine("\"" + line + "\"");

            double functionRegexStart = debug.Elapsed.TotalMilliseconds;

            Match functionMatch = functionRegex.Match(trimmedLine);
            if (functionMatch.Success) {
                string name = functionMatch.Groups[1].Value.ToLower();
                string args = functionMatch.Groups[2].Value;
                string content = line.Substring(line.IndexOf('{') + 1).Trim();

                switch (name) {
                    case "loop":
                        int? loopCount = Session.ParseInt(args);
                        if (loopCount == null) break;
                        for (int i = 0; i < loopCount; i++) ExecuteText(content, channel);
                        break;

                    case "thread":
                        ExecuteText(content, channel + 1);
                        break;

                    case "channel":
                        int? channelN = Session.ParseInt(args);
                        if (channelN == null) break;
                        ExecuteText(content, channelN.Value);
                        break;
                }
            }

            double functionRegexEnd = debug.Elapsed.TotalMilliseconds;
            functionRegexTimes.Add(functionRegexEnd - functionRegexStart);


            double callRegexStart = debug.Elapsed.TotalMilliseconds;

            Match callMatch = callRegex.Match(trimmedLine);
            if (callMatch.Success) {
                string name = callMatch.Groups[1].Value.ToLower();
                string args = callMatch.Groups[2].Value;

                switch (name) {
                    case "bpm":
                        int? bpm = Session.ParseInt(args);
                        if (bpm == null) break;
                        Session.System.AddBPMEvent(channel, bpm.Value);
                        break;

                    case "play":
                        string[] argsArr = args.Split(',');
                        if (argsArr.Length < 2) break;

                        string sample = argsArr[0].Trim();
                        string note = argsArr[1].Trim();

                        double? lengthN = argsArr.Length > 2 ? Session.ParseDouble(argsArr[2].Trim()) : null;
                        double length = lengthN ?? 0;

                        Session.System.AddNoteEvent(channel, sample, note, length);
                        break;

                    case "wait":
                        double? time = Session.ParseDouble(args);
                        if (time == null) break;

                        Session.System.AddWaitEvent(channel, time.Value);
                        break;

                    case "chord":
                        string[] chordArgs = args.Split(',');
                        if (chordArgs.Length < 3) break;
                        string chordFile = chordArgs[0].Trim();
                        double? chordLengthN = chordArgs.Length > 1 ? Session.ParseDouble(chordArgs[1].Trim()) : null;
                        double chordLength = chordLengthN ?? 0;

                        string[] keys = new string[chordArgs.Length - 2];
                        for (int i = 2; i < chordArgs.Length; i++) keys[i - 2] = chordArgs[i].Trim();

                        foreach (string k in keys) Session.System.AddNoteEvent(channel, chordFile, k, 0);
                        Session.System.AddWaitEvent(channel, chordLength);
                        break;

                    case "roll":
                        string[] rollArgs = args.Split(',');
                        if (rollArgs.Length < 3) break;
                        string rollFile = rollArgs[0].Trim();
                        double? rollLengthN = rollArgs.Length > 1 ? Session.ParseDouble(rollArgs[1].Trim()) : null;
                        double rollLength = rollLengthN ?? 0;

                        string[] rollKeys = new string[rollArgs.Length - 2];
                        for (int i = 2; i < rollArgs.Length; i++) rollKeys[i - 2] = rollArgs[i].Trim();

                        foreach (string k in rollKeys) Session.System.AddNoteEvent(channel, rollFile, k, (rollLength / rollKeys.Length));
                        break;
                }

                double callRegexEnd = debug.Elapsed.TotalMilliseconds;
                callRegexTimes.Add(callRegexEnd - callRegexStart);
            }
        }

        double loopEnd = debug.Elapsed.TotalMilliseconds;
        Console.WriteLine(indentString + "Looped through lines in " + (loopEnd - loopStart) + "ms.");

        double functionRegexAvg = functionRegexTimes.Count > 0 ? functionRegexTimes.Average() : 0;
        double callRegexAvg = callRegexTimes.Count > 0 ? callRegexTimes.Average() : 0;
        Console.WriteLine(indentString + "Average function regex time: " + functionRegexAvg + "ms.");
        Console.WriteLine(indentString + "Average call regex time: " + callRegexAvg + "ms.");

        Indent--;
    }
}