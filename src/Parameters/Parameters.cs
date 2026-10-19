using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Essentials.Plugins
{
    public class TestPatterns
    {
        Dictionary<string, string> Parameters = new Dictionary<string, string>(){
           { "colorbars", "colorBars" }
       };
    }

    // Brightness levels are per processor now: see HeliosBrightnessLevels.
}