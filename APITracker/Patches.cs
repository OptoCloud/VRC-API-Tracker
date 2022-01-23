using MelonLoader;
using System;
using System.Reflection;
using System.Text;

namespace APITracker
{
	internal static class Patches
	{
		private static HarmonyLib.HarmonyMethod GetLocalPatch(string name)
		{
			return new HarmonyLib.HarmonyMethod(typeof(Patches).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
		}

		static MelonLogger.Instance LoggerInstance;

		public static void Init(HarmonyLib.Harmony harmonyInstance, MelonLogger.Instance loggerInstance)
		{
			LoggerInstance = loggerInstance;

			harmonyInstance.Patch(typeof(BestHTTP.HTTPManager).GetMethod(nameof(BestHTTP.HTTPManager.SendRequestImpl), BindingFlags.Public | BindingFlags.Static), GetLocalPatch(nameof(Patches.SendRequest)));
			harmonyInstance.Patch(typeof(BestHTTP.HTTPResponse).GetMethod(nameof(BestHTTP.HTTPResponse.ReadChunked), BindingFlags.Public | BindingFlags.Instance), GetLocalPatch(nameof(Patches.ReadChunked)));
			harmonyInstance.Patch(typeof(BestHTTP.HTTPResponse).GetMethod(nameof(BestHTTP.HTTPResponse.ReadRaw), BindingFlags.Public | BindingFlags.Instance), GetLocalPatch(nameof(Patches.ReadRaw)));
		}

		private static void SendRequest(BestHTTP.HTTPRequest request)
		{
			if (request == null) return;

			string uri = request.Uri?.ToString();

			if (uri == null) return;

			RateTracker.RequestTriggered(uri);

			var il2cppData = request.GetEntityBody();

			if (il2cppData == null)
			{
				//LoggerInstance.Msg($"Request: " + uri);
				return;
			}

			byte[] data = new byte[il2cppData.Length];
			il2cppData.CopyTo(data, 0);

			string content;
			try
			{
				content = Encoding.UTF8.GetString(data, 0, data.Length);
			}
			catch (Exception) { return; }
			
			//LoggerInstance.Msg($"Request: {uri}\n{content}");
		}
		private static void ReadChunked(Il2CppSystem.IO.Stream stream)
		{
			//LoggerInstance.Msg("B");
		}
		private static void ReadRaw(Il2CppSystem.IO.Stream stream, long contentLength)
		{
			//LoggerInstance.Msg("C");
		}
	}
}
