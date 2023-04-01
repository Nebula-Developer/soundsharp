using System;
using System.Diagnostics;

namespace SoundSharp {
    public static class Program {
        public static void Main(string[] args) {
            Console.WriteLine("Initializing audio...");
            ManagedBass.Bass.Init();
            Console.WriteLine("Audio initialized.");
            Reader read = new Reader();
            Console.WriteLine("Reading file...");
            read.ReadFile("test.txt");
            Console.WriteLine("File read.");
            Console.WriteLine("Starting events...");
            read.Session.System.ExecuteEvents();
        }
    }
}
