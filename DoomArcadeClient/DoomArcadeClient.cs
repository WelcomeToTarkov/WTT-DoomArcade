using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using DoomArcadeClient.Patches;
using DoomArcadeClient.Utils;
using EFT;
using EFT.UI;
using System.IO;
using System.Reflection;
using WTTClientCommonLib.Patches;

namespace DoomArcadeClient
{
    [BepInPlugin("com.wtt.doomarcade", "DoomArcadeClient", "0.0.2")]
    public class DoomArcadeClient : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static CommandProcessor CommandProcessor;
        private static GameWorld _gameWorld;
        private static GameUI _gameUI;
        public static EFT.Player Player;
        internal void Update()
        {
            if (Singleton<GameWorld>.Instantiated && (_gameWorld == null || _gameUI == null || Player == null))
            {
                _gameWorld = Singleton<GameWorld>.Instance;
                _gameUI = MonoBehaviourSingleton<GameUI>.Instance;
                Player = Singleton<GameWorld>.Instance.MainPlayer;
            }
        }
        internal void Start()
        {
            Init();
        }
        void Awake()
        {
            Log = Logger;
            new GetAvailableActionsPatch().Enable();
            new UpdateSingleQuestVisibilityPatch().Enable();
            new HideoutGameSmethod6Patch().Enable();
            new OnGameStarted().Enable();
            var runtimePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DoomArcade.Runtime.dll");
            if (File.Exists(runtimePath))
            {
                var runtimeAsm = Assembly.LoadFrom(runtimePath);
                Logger.LogInfo($"Sideloaded DoomArcade.Runtime: {runtimeAsm.FullName}");
            }
        }

        public void Init()
        {
            if (CommandProcessor == null)
            {
                CommandProcessor = new CommandProcessor();
                CommandProcessor.RegisterCommandProcessor();
            }
        }
    }
}
