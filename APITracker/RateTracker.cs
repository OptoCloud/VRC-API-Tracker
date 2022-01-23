using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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
			bool updated = false;

			public SpecificRateTracker(int mins, int secs)
			{
				FrameStart = DateTime.Now;
				FrameSize = new TimeSpan(0, mins, secs);
			}

			public void Trigger(DateTime now)
			{
				if (now > FrameStart + FrameSize)
				{
					if (Triggers < minTriggers) { minTriggers = Triggers; updated = true; }
					if (Triggers > maxTriggers) { maxTriggers = Triggers; updated = true; }

					FrameStart = now;
					Triggers = 0;
				}
				else
				{
					Triggers++;
				}
			}

			public string GetStatus()
			{
				if (!updated) return null;
				updated = false;

				return $"Time({FrameSize}) Min({minTriggers}) Max({maxTriggers})";
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

			public bool Log(MelonLogger.Instance loggerInstance)
			{
				uint updates = 0;
				string msg = $"Endpoint {Url}\n";
				foreach (SpecificRateTracker tracker in Trackers)
				{
					string status = tracker.GetStatus();
					if (status != null)
					{
						updates++;
						msg += $"\t\t{status}\n";
					}
				}
				if (updates != 0)
				{
					loggerInstance.Msg(msg);
					return true;
				}
				return false;
			}
		}
		static Dictionary<string, EndPointInfo> endpointTimings = new Dictionary<string, EndPointInfo>();

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
		}

		public static void RequestTriggered(String url)
		{
			var requestTime = DateTime.Now;

			url = Regex.Replace(url.Split('?')[0], @"(\w*_[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12})", "[VRCID]");
			url = Regex.Replace(url, @"(\[VRCID\]:[0-9]*)", "[VRCID]:[INSTANCEID]");
			url = Regex.Replace(url, @"(~region\([a-z]*\))", "~region([REGION])");
			url = Regex.Replace(url, @"([A-Z0-9]{48})", "[NONCE]");

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

		public static void Log(MelonLogger.Instance loggerInstance)
		{
			bool updated = false;
			lock (endpointTimings)
			{
				foreach (var endpoint in endpointTimings)
				{
					updated |= endpoint.Value.Log(loggerInstance);
				}
				if (updated)
				{
					File.WriteAllText("endpointlimits.json", JsonConvert.SerializeObject(endpointTimings));
				}
			}
		}
	}
}
