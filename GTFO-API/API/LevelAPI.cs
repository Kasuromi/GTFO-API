using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameData;
using GTFO.API.Attributes;
using GTFO.API.Resources;
using SNetwork;

namespace GTFO.API
{
    [API("Level")]
    public static class LevelAPI
    {
        /// <summary>
        /// Status info for the <see cref="LevelAPI"/>
        /// </summary>
        public static ApiStatusInfo Status => APIStatus.Level;

        private static eRundownTier s_LatestExpTier = eRundownTier.Surface;
        private static int s_LatestExpIndex = -1;

        internal static void Setup()
        {
            Status.Created = true;
            Status.Ready = true;

            RundownManager.add_OnExpeditionGameplayStarted((Action)EnterLevel);

#if DEBUG
            OnLevelDataUpdated += (activeExp, expData) => APILogger.Debug(nameof(LevelAPI), $"OnLevelDataUpdated Invoked");
            OnLevelSelected += (tier, index, data) => APILogger.Debug(nameof(LevelAPI), $"OnLevelSelected(tier: {tier}, index: {index}, publicName: {data.Descriptive.PublicName}) Invoked");
            OnBuildStart += () => APILogger.Debug(nameof(LevelAPI), "OnBuildStart Invoked");
            OnBuildDone += () => APILogger.Debug(nameof(LevelAPI), "OnBuildDone Invoked");
            OnEnterLevel += () => APILogger.Debug(nameof(LevelAPI), "OnEnterLevel Invoked");
            OnLevelCleanup += () => APILogger.Debug(nameof(LevelAPI), "OnLevelCleanup Invoked");
#endif
        }

        /// <summary>
        /// Invoked when Level Data has Updated (This includes level change / seed change)
        /// </summary>
        public static event Action<ActiveExpedition, ExpeditionInTierData> OnLevelDataUpdated;

        /// <summary>
        /// Invoked when Level has Selected
        /// </summary>
        public static event Action<eRundownTier, int, ExpeditionInTierData> OnLevelSelected;

        /// <summary>
        /// Invoked when LevelBuild has started
        /// </summary>
        public static event Action OnBuildStart;

        /// <summary>
        /// Invoked when LevelBuild has finished
        /// </summary>
        public static event Action OnBuildDone;

        /// <summary>
        /// Invoked when Enter the level (Player is able to move)
        /// </summary>
        public static event Action OnEnterLevel;

        /// <summary>
        /// Invoked when Level has Cleaned up
        /// </summary>
        public static event Action OnLevelCleanup;

        internal static void ExpeditionUpdated(pActiveExpedition activeExp, ExpeditionInTierData expData)
        {
            OnLevelDataUpdated?.Invoke(ActiveExpedition.CreateFrom(activeExp), expData);

            var tier = activeExp.tier;
            var index = activeExp.expeditionIndex;

            if (tier != s_LatestExpTier || index != s_LatestExpIndex)
            {
                OnLevelSelected?.Invoke(tier, index, expData);
                s_LatestExpTier = tier;
                s_LatestExpIndex = index;
            }
        }
        internal static void BuildStart() => OnBuildStart?.Invoke();
        internal static void BuildDone() => OnBuildDone?.Invoke();
        internal static void EnterLevel() => OnEnterLevel?.Invoke();
        internal static void LevelCleanup() => OnLevelCleanup?.Invoke();
    }

    /// <summary>
    /// Blittable variant of pActiveExpedition
    /// </summary>
    public struct ActiveExpedition
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public SNetStructs.pPlayer player;
        public eRundownKey rundownType;
        public string rundownKey;
        public eRundownTier tier;
        public int expeditionIndex;
        public int hostIDSeed;
        public int sessionSeed;
        //public SNetStructs.pSessionGUID sessionGUID;

        public static ActiveExpedition CreateFrom(pActiveExpedition pActiveExp)
        {
            var data = new ActiveExpedition();
            data.CopyFrom(pActiveExp);
            return data;
        }

        public void CopyFrom(pActiveExpedition pActiveExp)
        {
            player = pActiveExp.player;
            rundownType = pActiveExp.rundownType;
            rundownKey = pActiveExp.rundownKey.data;
            tier = pActiveExp.tier;
            expeditionIndex = pActiveExp.expeditionIndex;
            hostIDSeed = pActiveExp.hostIDSeed;
            sessionSeed = pActiveExp.sessionSeed;
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
