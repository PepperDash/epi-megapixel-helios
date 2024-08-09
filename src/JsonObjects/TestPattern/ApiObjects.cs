using Newtonsoft.Json;
using System;

namespace MegapixelHelios.JsonObjects.TestPattern
{
    public class RootDevObject
    {
        [JsonProperty("dev")]
        public DevObject Dev { get; set; }
    }

    public class DevObject
    {
        [JsonProperty("ingest")]
        public IngestObject Ingest { get; set; }
    }

    public class IngestObject
    {
        [JsonProperty("testPattern")]
        public TestPatternObject TestPattern { get; set; }
    }

    public class TestPatternObject
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
    }
}