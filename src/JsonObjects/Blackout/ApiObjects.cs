using Newtonsoft.Json;
using System;


namespace MegapixelHelios.JsonObjects.Blackout
{
    public class RootDevObject
    {
        [JsonProperty("dev")]
        public DevObject Dev { get; set; }
    }

    public class DevObject
    {
        [JsonProperty("display")]
        public DisplayObject Display { get; set; }
    }

    public class DisplayObject
    {
        [JsonProperty("blackout")]
        public bool Blackout { get; set; }
    }
}