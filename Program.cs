﻿using System;

namespace emud
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = args.Length > 1 ? args[1] : "pong.ch8";
            Emulator emu = new Emulator(file);
        }
    }
}
