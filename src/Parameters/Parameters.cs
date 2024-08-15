using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace MegapixelHelios.Parameters
{
    public class TestPatterns
    {
        Dictionary<string, string> Parameters = new Dictionary<string, string>(){
           { "colorbars", "colorBars" }
       };
    }

    public static class BrightnessLevel
    {
        public static ushort High { get; set; }
        public static ushort Medium { get; set; }
        public static ushort Low { get; set; }

        static BrightnessLevel()
        {
            High = 75;
            Medium = 50;
            Low = 20;
        }
    }
}