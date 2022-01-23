using MelonLoader;

namespace APITracker
{
	public class APITracker : MelonMod
	{
		public override void OnApplicationStart()
		{
			RateTracker.Init();
			Patches.Init(HarmonyInstance, LoggerInstance);
		}

		uint updates = 0;
		public override void OnLateUpdate()
		{
			if (updates++ >= 600)
			{
				updates = 0;
				RateTracker.Log(LoggerInstance);
			}
		}
	}
}
