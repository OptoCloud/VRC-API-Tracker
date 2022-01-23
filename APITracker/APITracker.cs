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

        public override void OnApplicationQuit()
        {
            RateTracker.Stop();
        }

    }
}
