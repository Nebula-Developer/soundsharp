using System;
using System.Diagnostics;

namespace SoundSharp {
    public static class Program {
        public static void Main(string[] args) {
            ManagedBass.Bass.Init();
            new Reader().ReadFile("test.txt");
        }
    }
}
