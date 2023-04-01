using System;
using System.Diagnostics;

namespace SoundSharp {
    public static class Program {
        public static void Main(string[] args) {
            Stopwatch debug = new();
            debug.Start();

            Console.WriteLine("Initializing audio...");
            double initTime = debug.ElapsedMilliseconds;
            ManagedBass.Bass.Init();
            Console.WriteLine("Audio initialized in " + (debug.ElapsedMilliseconds - initTime) + "ms.");

            double readerTime = debug.ElapsedMilliseconds;
            Reader read = new Reader();
            Console.WriteLine("Reading file...");
            read.ReadFile("test.txt");
            Console.WriteLine("File read in " + (debug.ElapsedMilliseconds - readerTime) + "ms.");
            
            Console.WriteLine("Starting events...");
            read.Session.System.ExecuteEvents();
        }
    }
}
