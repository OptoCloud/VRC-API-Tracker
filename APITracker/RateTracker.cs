using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace APITracker
{
	internal static class RateTracker
	{
		class SpecificRateTracker
		{
			[JsonIgnore]
			DateTime FrameStart;
			[JsonProperty(PropertyName = "frame_size")]
			TimeSpan FrameSize;
			[JsonIgnore]
			uint Triggers = 0;

			[JsonProperty(PropertyName = "min")]
			uint minTriggers = uint.MaxValue;
			[JsonProperty(PropertyName = "max")]
			uint maxTriggers = uint.MinValue;
			[JsonIgnore]
			bool changed = false;

			public SpecificRateTracker(int mins, int secs)
			{
				FrameStart = DateTime.Now;
				FrameSize = new TimeSpan(0, mins, secs);
			}

            public bool Update(DateTime now)
            {
                if (now > FrameStart + FrameSize)
                {
                    if (Triggers < minTriggers) { minTriggers = Triggers; changed = true; }
                    if (Triggers > maxTriggers) { maxTriggers = Triggers; changed = true; }

                    FrameStart = now;
                    Triggers = 0;
                }
                return changed;
            }

            public void Trigger(DateTime now)
            {
                Update(now);
                Triggers++;
            }
		}
		class EndPointInfo
		{
			public string Url { get; private set; }
			public SpecificRateTracker[] Trackers { get; private set; }

			public EndPointInfo(string url)
			{
				Url = url;
				Trackers = new SpecificRateTracker[]
				{
					new SpecificRateTracker(0,1),
					new SpecificRateTracker(0,5),
					new SpecificRateTracker(0,10),
					new SpecificRateTracker(0,20),
					new SpecificRateTracker(0,30),
					new SpecificRateTracker(0,40),
					new SpecificRateTracker(0,50),
					new SpecificRateTracker(1,0),
					new SpecificRateTracker(2,0),
					new SpecificRateTracker(3,0),
					new SpecificRateTracker(4,0),
					new SpecificRateTracker(5,0),
					new SpecificRateTracker(10,0),
					new SpecificRateTracker(20,0),
					new SpecificRateTracker(30,0)
				};
			}

			public void Trigger(DateTime now)
			{
				foreach (SpecificRateTracker tracker in Trackers) {
					tracker.Trigger(now);
				} 
			}

			public bool Update(DateTime now)
			{
                bool updated = false;
				foreach (SpecificRateTracker tracker in Trackers)
				{
                    updated |= tracker.Update(now);
				}
                return updated;
			}
		}
		static Dictionary<string, EndPointInfo> endpointTimings = new Dictionary<string, EndPointInfo>();
        static bool run;
        static Thread updateThread;
		public static void Init()
		{
			try
			{
				endpointTimings = JsonConvert.DeserializeObject<Dictionary<string, EndPointInfo>>(File.ReadAllText("endpointlimits.json"));
			}
			catch (Exception)
			{
				endpointTimings = new Dictionary<string, EndPointInfo>();
			}
            run = true;
            updateThread = new Thread(Update);
            updateThread.Start();
		}

		public static void RequestTriggered(String url)
		{
			var requestTime = DateTime.Now;

			url = Regex.Replace(
                    Regex.Replace(
                        Regex.Replace(
                            Regex.Replace(
                                Regex.Replace(
                                    url.Split('?')[0],

                                    @"([0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})",
                                    "[GUID]"),

                                @"(\w+_\[GUID\])",
                                "[VRCID]"),

                            @"(\[VRCID\]:[^~/]+)",
                            "[VRCID]:[INSTANCEID]"),

                        @"(~region\([^\)]+\))",
                        "~region([REGION])"),

                    @"(~nonce\([^\)]+\))",
                    "~nonce([NONCE])");

			lock (endpointTimings)
			{
				if (!endpointTimings.TryGetValue(url, out EndPointInfo endPointInfo))
				{
					endPointInfo = new EndPointInfo(url);
					endpointTimings.Add(url, endPointInfo);
				}

				endPointInfo.Trigger(requestTime);
			}
		}

        public static void Stop()
        {
            run = false;
            updateThread.Join();
        }

		private static void Update()
        {
            while (run)
            {
                var updateTime = DateTime.Now;

                bool updated = false;
                lock (endpointTimings)
                {
                    foreach (var endpoint in endpointTimings)
                    {
                        updated |= endpoint.Value.Update(updateTime);
                    }
                    if (updated)
                    {
                        File.WriteAllText("endpointlimits.json", JsonConvert.SerializeObject(endpointTimings, Formatting.Indented));
                    }
                }
                Thread.Sleep(10);
            }
		}
	}
}
