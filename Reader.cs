using System;
using System.Text.RegularExpressions;

namespace SoundSharp;

public class Session {
    public int BPM = 120;
    public Dictionary<string, string> Variables = new Dictionary<string, string>();
    public double BPMDuration => (60.0 * 1000.0) / (double)BPM;

    public double GetDuration(double duration) {
        return BPMDuration * (double)duration;
    }

    public void Sleep(double duration) {
        Thread.Sleep((int)GetDuration(duration));
    }

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

    public void Play(string note, string key = "c4", double duration = -1) {
        Console.WriteLine($"Playing sample {note} at key {key} for {duration} beats.");
    }
}

public class Reader {
    // { (absolutely everything) }
    public static string FunctionContentsRegex = @"\{(?:[^{}]|(?<c>\{)|(?<-c>\}))*\}(?(c)(?!))";

    public Session Session = new Session();

    public Reader() {
        Session.BPM = 120;
    }
    
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
        List<string> functionContentArr = new List<string>();
        Regex functionContentsRegex = new Regex(FunctionContentsRegex);

        MatchCollection functionContentsMatches = functionContentsRegex.Matches(text);
        foreach (Match match in functionContentsMatches) {
            functionContentArr.Add(match.Value);
        }

        text = functionContentsRegex.Replace(text, "(__FUNCTION_CONTENTS__)}");
        string[] lines = text.Split(';', '}');
        
        for (int i = 0; i < lines.Length; i++) {
            if (lines[i].IndexOf("(__FUNCTION_CONTENTS__)") != -1) {
                lines[i] = lines[i].Replace("(__FUNCTION_CONTENTS__)", functionContentArr[0]);
                functionContentArr.RemoveAt(0);
            }
        }

        Regex variableAssignmentRegex = new Regex(@"(\w+)\s*=\s*(\w+)");
        Regex variableUsageRegex = new Regex(@"$(\w+)");

        Regex functionIncCallRegex = new Regex(@"(\w+)\s*\(([^)]*)\)\s*{");
        Regex callRegex = new Regex(@"(\w+)\s*\(([^)]*)\)");

        foreach (string line in lines) {
            string trimmedLine = line.Trim();
            if (trimmedLine.Length == 0) continue;
            string trimmedLineLower = trimmedLine.ToLower();

            Match variableAssignmentMatch = variableAssignmentRegex.Match(trimmedLine);
            if (variableAssignmentMatch.Success) {
                string variableName = variableAssignmentMatch.Groups[1].Value;
                string variableValue = variableAssignmentMatch.Groups[2].Value;
                Session.Variables[variableName] = variableValue;
                continue;
            }

            Match variableUsageMatch = variableUsageRegex.Match(trimmedLine);
            if (variableUsageMatch.Success) {
                string variableName = variableUsageMatch.Groups[1].Value;
                if (Session.Variables.ContainsKey(variableName)) {
                    Console.WriteLine(Session.Variables[variableName]);
                }
                continue;
            }

            Match functionIncCallMatch = functionIncCallRegex.Match(trimmedLineLower);
            if (functionIncCallMatch.Success) {
                string functionName = functionIncCallMatch.Groups[1].Value;
                string functionArgs = functionIncCallMatch.Groups[2].Value;
                string functionContents = trimmedLineLower.Substring(
                    functionIncCallMatch.Groups[0].Length,
                    trimmedLineLower.Length - functionIncCallMatch.Groups[0].Length
                );

                switch (functionName) {
                    case "loop":
                        int? loopCount = Session.ParseInt(functionArgs);
                        if (loopCount != null) {
                            for (int i = 0; i < loopCount.Value; i++) {
                                ExecuteText(functionContents);
                            }
                        }
                        break;
                }
            }

            Match callMatch = callRegex.Match(trimmedLineLower);
            if (callMatch.Success) {
                string functionName = callMatch.Groups[1].Value;
                string functionArgs = callMatch.Groups[2].Value;
                string[] functionArgsArr = functionArgs.Split(',').Select(x => x.Trim()).ToArray();

                switch (functionName) {
                    case "wait":
                        double? sleepDuration = Session.ParseDouble(functionArgs);
                        if (sleepDuration != null) {
                            Console.WriteLine($"Sleeping for {sleepDuration.Value} beats.");
                            Session.Sleep(sleepDuration.Value);
                        }
                        break;

                    case "play":
                        string note = functionArgsArr[0];
                        string key = functionArgsArr.Length > 1 ? functionArgsArr[1] : "c4";
                        double duration = (functionArgsArr.Length > 2 ? Session.ParseDouble(functionArgsArr[2]) : -1) ?? -1;
                        Session.Play(note, key, duration);
                        break;
                }
            }
        }

        Indent--;
    }
}