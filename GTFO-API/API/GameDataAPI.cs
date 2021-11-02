using System;
using GTFO.API.Attributes;
using GTFO.API.Resources;

namespace GTFO.API
{
    [API("GameData")]
    public class GameDataAPI
    {
        /// <summary>
        /// Status info for the <see cref="GameDataAPI"/>
        /// </summary>
        public static ApiStatusInfo Status => APIStatus.GameData;

        static GameDataAPI()
        {
            Status.Created = true;
            Status.Ready = true;
        }

        /// <summary>
        /// Invoked when the game data has been fully initialized
        /// </summary>
        public static event Action OnGameDataInitialized;

        internal static void InvokeGameDataInit() => OnGameDataInitialized?.Invoke();
    }
}
