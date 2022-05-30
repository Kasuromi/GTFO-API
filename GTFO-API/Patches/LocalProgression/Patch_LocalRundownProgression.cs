using HarmonyLib;
using DropServer;
using GameData;
using Globals;
using System.IO;
using System;
using System.Text;
using System.Collections.Generic;
using CellMenu;

namespace GTFO.API.Patches.LocalProgression
{
    [HarmonyPatch]
    internal class Patch_LocalRundownProgression
    {
        private static readonly string DirPath 
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"GTFO-Modding" ,"LocalProgression");

        private static string RUNDOWN_IDENTIFIER = "";

        // Local progression data holder.
        private static Dictionary<string, LocalExpProgData> LocalProgDict = null;

        private static string LocalProgressionStorePath() => Path.Combine(DirPath, RUNDOWN_IDENTIFIER);

        private static void StoreLocalProgDict()
        {
            if(LocalProgDict == null)
            {
                APILogger.Error(nameof(Patch_LocalRundownProgression), "Critical: LocalProgDict is null!");
                return;
            }

            LocalRundownProgData localRundownProgData = new LocalRundownProgData() { Rundown_Name = RUNDOWN_IDENTIFIER };

            foreach(string ExpKey in LocalProgDict.Keys) 
            {
                LocalExpProgData localExpProgData;
                if(LocalProgDict.TryGetValue(ExpKey, out localExpProgData) == false)
                {
                    continue;
                }
                localRundownProgData.ExpProgs.Add(localExpProgData);
            }

            localRundownProgData.ExpProgs.Sort((p1, p2) => p1.expeditionKey.CompareTo(p2.expeditionKey));

            using (var stream = File.Open(LocalProgressionStorePath(), FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    writer.Write(LocalProgDict.Count);
                    foreach(string expKey in LocalProgDict.Keys)
                    {
                        LocalExpProgData data;
                        LocalProgDict.TryGetValue(expKey, out data);
                        writer.Write(expKey);
                        writer.Write(data.MainCompletionCount);
                        writer.Write(data.SecondaryCompletionCount);
                        writer.Write(data.ThirdCompletionCount);
                        writer.Write(data.AllClearCount);
                    }
                }
            }
        }

        private static void initialize()
        {
            if (!Directory.Exists(DirPath))
            {
                Directory.CreateDirectory(DirPath);
            }

            /*
             * Consider changing `RUNDOWN_IDENTIFIER` to the name of loaded rundown folder.
             */
            RUNDOWN_IDENTIFIER = RundownDataBlock.GetBlock(1).name;
            string filePath = LocalProgressionStorePath();

            APILogger.Verbose(nameof(Patch_LocalRundownProgression), string.Format("Get current rundown name: {0}", RUNDOWN_IDENTIFIER));
            APILogger.Verbose(nameof(Patch_LocalRundownProgression), string.Format("FilePath: {0}", LocalProgressionStorePath()));

            LocalProgDict = new Dictionary<string, LocalExpProgData>();

            if (File.Exists(filePath))
            {
                using (var stream = File.Open(LocalProgressionStorePath(), FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                    {
                        int LocalProgDict_Count = 0;
                        LocalProgDict_Count = reader.ReadInt32();
                        for(int cnt = 0; cnt < LocalProgDict_Count; cnt++)
                        {
                            LocalExpProgData data = new LocalExpProgData();
                            data.expeditionKey = reader.ReadString();
                            data.MainCompletionCount = reader.ReadInt32();
                            data.SecondaryCompletionCount = reader.ReadInt32();
                            data.ThirdCompletionCount = reader.ReadInt32();
                            data.AllClearCount = reader.ReadInt32();
                            LocalProgDict.Add(data.expeditionKey, data);
                        }
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GS_ExpeditionSuccess), nameof(GS_ExpeditionSuccess.Enter))]
        private static void DoChangeState(GS_ExpeditionSuccess __instance)
        {
            if(LocalProgDict == null)
            {
                APILogger.Error(nameof(Patch_LocalRundownProgression), "Critical: localRundownProgData is uninitialized.");
                return;
            }

            // ----------------------
            // store prog data on my side
            // ----------------------
            var CompletedExpedtionData = RundownManager.GetActiveExpeditionData();

            var progData = new LocalExpProgData()
            {
                expeditionKey = RundownManager.GetRundownProgressionExpeditionKey(CompletedExpedtionData.tier, CompletedExpedtionData.expeditionIndex),
                MainCompletionCount = WardenObjectiveManager.CurrentState.main_status == eWardenObjectiveStatus.WardenObjectiveItemSolved ? 1 : 0,
                SecondaryCompletionCount = WardenObjectiveManager.CurrentState.second_status == eWardenObjectiveStatus.WardenObjectiveItemSolved ? 1 : 0,
                ThirdCompletionCount = WardenObjectiveManager.CurrentState.third_status == eWardenObjectiveStatus.WardenObjectiveItemSolved ? 1 : 0
            };

            if (progData.MainCompletionCount == 1 && progData.SecondaryCompletionCount == 1 && progData.ThirdCompletionCount == 1)
            {
                progData.AllClearCount = 1;
            }

            if(LocalProgDict.ContainsKey(progData.expeditionKey))
            {
                LocalExpProgData PreviousProgData;
                if (LocalProgDict.TryGetValue(progData.expeditionKey, out PreviousProgData) == false)
                {
                    return;
                }

                LocalProgDict.Remove(progData.expeditionKey);
                progData.MainCompletionCount += PreviousProgData.MainCompletionCount;
                progData.SecondaryCompletionCount += PreviousProgData.SecondaryCompletionCount;
                progData.ThirdCompletionCount += PreviousProgData.ThirdCompletionCount;
                progData.AllClearCount += PreviousProgData.AllClearCount;
                LocalProgDict.Add(progData.expeditionKey, progData);
            }
            else
            {
                LocalProgDict.Add(progData.expeditionKey, progData);
            }

            StoreLocalProgDict();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.UpdateTierIconsWithProgression))]
        private static void Post_UpdateTierIconsWithProgression(CM_PageRundown_New __instance, 
            RundownManager.RundownProgData progData,
            Il2CppSystem.Collections.Generic.List<CM_ExpeditionIcon_New> tierIcons,
            CM_RundownTierMarker tierMarker,
            bool thisTierUnlocked) {

            /* 
             *  null check 
            */
            //APILogger.Warn(nameof(Patch_LocalRundownProgression), string.Format("CM_PageRundown_New __instance == null ? {0}", __instance == null));
            //APILogger.Warn(nameof(Patch_LocalRundownProgression), string.Format("RundownManager.RundownProgData progData == null ? {0}", progData));
            //APILogger.Warn(nameof(Patch_LocalRundownProgression), string.Format("List<CM_ExpeditionIcon_New> tierIcons == null ? {0}", tierIcons == null));
            //APILogger.Warn(nameof(Patch_LocalRundownProgression), string.Format("CM_RundownTierMarker tierMarker == null ? {0}", tierMarker == null));

            if (LocalProgDict == null)
            {
                initialize();
            }

            // ----------------------------------------
            // original method rewrite
            // ----------------------------------------

            if (tierIcons == null || tierIcons.Count == 0)
            {
                //tierMarker.SetVisible(false);
            }
            else
            {
                int num = 0;
                bool allowFullRundown = Global.AllowFullRundown;
                for (int index = 0; index < tierIcons.Count; ++index)
                {
                    CM_ExpeditionIcon_New tierIcon = tierIcons[index];
                    string progressionExpeditionKey = RundownManager.GetRundownProgressionExpeditionKey(tierIcons[index].Tier, tierIcons[index].ExpIndex);
                    LocalExpProgData localExpProgData;
                    bool flag1 = LocalProgDict.TryGetValue(progressionExpeditionKey, out localExpProgData); // is this one?
                    if (flag1 == false)
                    {
                        continue;
                    }

                    //AddExpProgData(localExpProgData);

                    string mainFinishCount = "0";
                    string secondFinishCount = RundownManager.HasSecondaryLayer(tierIcons[index].DataBlock) ? "0" : "-";
                    string thirdFinishCount = RundownManager.HasThirdLayer(tierIcons[index].DataBlock) ? "0" : "-";
                    string allFinishedCount = RundownManager.HasAllCompletetionPossibility(tierIcons[index].DataBlock) ? "0" : "-";

                    if (localExpProgData.MainCompletionCount > 0)
                        mainFinishCount = localExpProgData.MainCompletionCount.ToString();
                    if (localExpProgData.SecondaryCompletionCount > 0)
                        secondFinishCount = localExpProgData.SecondaryCompletionCount.ToString();
                    if (localExpProgData.ThirdCompletionCount > 0)
                        thirdFinishCount = localExpProgData.ThirdCompletionCount.ToString();
                    if (localExpProgData.AllClearCount > 0)
                        allFinishedCount = localExpProgData.AllClearCount.ToString();

                    bool flag2 = RundownManager.CheckExpeditionUnlocked(tierIcon.DataBlock, tierIcon.Tier, progData);
                    if (allowFullRundown | thisTierUnlocked | flag2)
                    {
                        if (allowFullRundown | flag1)
                        {
                            if (allowFullRundown || localExpProgData.MainCompletionCount > 0)
                            {
                                __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.PlayedAndFinished, mainFinishCount, secondFinishCount, thirdFinishCount, allFinishedCount);
                                ++num;
                            }
                            //else if (prog.Layers.Main.State >= LayerProgressionState.Entered)
                            //    __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.PlayedNotFinished);
                            else
                                __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.NotPlayed);
                        }
                        else
                            __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.NotPlayed);
                    }
                    else if (flag1 && localExpProgData.MainCompletionCount > 0)
                        __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.TierLockedFinishedAnyway, mainFinishCount, secondFinishCount, thirdFinishCount, allFinishedCount);
                    else
                        __instance.SetIconStatus(tierIcon, eExpeditionIconStatus.TierLocked);
                }


                if (allowFullRundown | thisTierUnlocked)
                    tierMarker.SetStatus(eRundownTierMarkerStatus.Unlocked);
                else
                    tierMarker.SetStatus(eRundownTierMarkerStatus.Locked);
            }

        }

        //private static void AddExpProgData(LocalExpProgData expProgData)
        //{
        //    // ----------------------------------------
        //    // Add data to vanilla code rundown progression,
        //    // which can be used to determine tier/level unlock 
        //    // but sucks in displaying the correct completion time.
        //    // ----------------------------------------

        //    Il2CppSystem.Collections.Generic.Dictionary<string, RundownProgression.Expedition> progressionData = RundownManager.RundownProgression.Expeditions;

        //    string expKey = expProgData.expeditionKey;
        //    RundownProgression.Expedition expedition = null;
        //    if (DictionaryHelper.TryGetValue(progressionData, expKey, out expedition) == false || expedition == null)
        //    {
        //        expedition = new RundownProgression.Expedition();
        //        expedition.Layers = new LayerSet<RundownProgression.Expedition.Layer>();
        //        expedition.Layers.Main = new RundownProgression.Expedition.Layer() { CompletionCount = 0 };
        //        expedition.Layers.Secondary = new RundownProgression.Expedition.Layer() { CompletionCount = 0 };
        //        expedition.Layers.Third = new RundownProgression.Expedition.Layer() { CompletionCount = 0 };
        //        expedition.AllLayerCompletionCount = 0;
        //    }
        //    else // already contains. return
        //    {
        //        return;
        //    }

        //    expedition.AllLayerCompletionCount = expProgData.AllClearCount;

        //    var MainLayer = expedition.Layers.GetLayer(ExpeditionLayers.Main);
        //    MainLayer.CompletionCount = expProgData.MainCompletionCount;
        //    expedition.Layers.SetLayer(ExpeditionLayers.Main, MainLayer);

        //    var SecondaryLayer = expedition.Layers.GetLayer(ExpeditionLayers.Secondary);
        //    SecondaryLayer.CompletionCount = expProgData.SecondaryCompletionCount;
        //    expedition.Layers.SetLayer(ExpeditionLayers.Secondary, SecondaryLayer);

        //    var ThirdLayer = expedition.Layers.GetLayer(ExpeditionLayers.Third);
        //    ThirdLayer.CompletionCount = expProgData.ThirdCompletionCount;
        //    expedition.Layers.SetLayer(ExpeditionLayers.Third, ThirdLayer);

        //    // Il2cpp dictionary sucks: actual number is fucking scattered.
        //    // Used only for determining "if the expedition is completed or not" 
        //    if (progressionData.ContainsKey(expKey)) {
        //        progressionData.Remove(expKey);
        //    }
        //    progressionData.Add(expKey, expedition);
        //}

        private static RundownManager.RundownProgData RecomputeRundownProgData()
        {
            RundownManager.RundownProgData rundownProgData = new RundownManager.RundownProgData();
            if (LocalProgDict == null) return rundownProgData;

            RundownDataBlock block = GameDataBlockBase<RundownDataBlock>.GetBlock(1);
            if (block == null)
                return rundownProgData;

            foreach(var localExpData in LocalProgDict.Values)
            {
                if (localExpData.MainCompletionCount > 0) rundownProgData.clearedMain++;
                if (localExpData.SecondaryCompletionCount > 0) rundownProgData.clearedSecondary++;
                if (localExpData.ThirdCompletionCount > 0) rundownProgData.clearedThird++;
                if (localExpData.AllClearCount > 0) rundownProgData.clearedAllClear++;
            }

            int index = 0;
            eRundownTier tier = eRundownTier.TierA;
            foreach(var exp in block.TierA)
            {
                rundownProgData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) rundownProgData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) rundownProgData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) rundownProgData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) rundownProgData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(block, tier, index);
                if (LocalProgDict.ContainsKey(expKey)) rundownProgData.clearedExtra++;

                index++;
            }

            index = 0;
            tier = eRundownTier.TierB;
            foreach (var exp in block.TierB)
            {
                rundownProgData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) rundownProgData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) rundownProgData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) rundownProgData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) rundownProgData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(block, tier, index);
                if (LocalProgDict.ContainsKey(expKey)) rundownProgData.clearedExtra++;

                index++;
            }

            index = 0;
            tier = eRundownTier.TierC;
            foreach (var exp in block.TierC)
            {
                rundownProgData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) rundownProgData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) rundownProgData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) rundownProgData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) rundownProgData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(block, tier, index);
                if (LocalProgDict.ContainsKey(expKey)) rundownProgData.clearedExtra++;

                index++;
            }

            index = 0;
            tier = eRundownTier.TierD;
            foreach (var exp in block.TierD)
            {
                rundownProgData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) rundownProgData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) rundownProgData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) rundownProgData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) rundownProgData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(block, tier, index);
                if (LocalProgDict.ContainsKey(expKey)) rundownProgData.clearedExtra++;

                index++;
            }

            index = 0;
            tier = eRundownTier.TierE;
            foreach (var exp in block.TierE)
            {
                rundownProgData.totalMain++;
                if (RundownManager.HasSecondaryLayer(exp)) rundownProgData.totalSecondary++;
                if (RundownManager.HasThirdLayer(exp)) rundownProgData.totalThird++;
                if (RundownManager.HasAllCompletetionPossibility(exp)) rundownProgData.totalAllClear++;
                if (exp.Descriptive.IsExtraExpedition) rundownProgData.totatlExtra++;
                string expKey = RundownManager.GetUniqueExpeditionKey(block, tier, index);
                if (LocalProgDict.ContainsKey(expKey)) rundownProgData.clearedExtra++;

                index++;
            }

            RundownManager.CheckTierUnlocked(ref rundownProgData, eRundownTier.TierB, block.ReqToReachTierB);
            RundownManager.CheckTierUnlocked(ref rundownProgData, eRundownTier.TierC, block.ReqToReachTierC);
            RundownManager.CheckTierUnlocked(ref rundownProgData, eRundownTier.TierD, block.ReqToReachTierD);
            RundownManager.CheckTierUnlocked(ref rundownProgData, eRundownTier.TierE, block.ReqToReachTierE);
            return rundownProgData;
        }

        // method rewrite.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.UpdateExpeditionIconProgression))]
        private static bool Pre_UpdateExpeditionIconProgression(CM_PageRundown_New __instance)
        {
            /*
             * Mostly mono code copy-paste.
             */

            RundownDataBlock block = GameDataBlockBase<RundownDataBlock>.GetBlock(Global.RundownIdToLoad);
            if (block == null) return true;
            RundownManager.RundownProgData rundownProgData1 = RecomputeRundownProgData();

            // previously commented out. 
            __instance.CheckTier(block.TierA, eRundownTier.TierA, rundownProgData1);
            __instance.CheckTier(block.TierB, eRundownTier.TierB, rundownProgData1);
            __instance.CheckTier(block.TierC, eRundownTier.TierC, rundownProgData1);
            __instance.CheckTier(block.TierD, eRundownTier.TierD, rundownProgData1);
            __instance.CheckTier(block.TierE, eRundownTier.TierE, rundownProgData1);

            UnityEngine.Debug.Log("CM_PageRundown_New.UpdateRundownExpeditionProgression, RundownManager.RundownProgressionReady: " + RundownManager.RundownProgressionReady.ToString());
            RundownProgression rundownProgression = RundownManager.RundownProgression;

            // modified line
            RundownManager.RundownProgData rundownProgData2 = RecomputeRundownProgData();

            if (__instance.m_tierMarkerSectorSummary != null)
            {
                __instance.m_tierMarkerSectorSummary.SetSectorIconTextForMain(rundownProgData2.clearedMain + "<size=50%><color=#FFFFFF33><size=55%>/" + rundownProgData2.totalMain + "</color></size>");
                __instance.m_tierMarkerSectorSummary.SetSectorIconTextForSecondary(rundownProgData2.clearedSecondary + "<size=50%><color=#FFFFFF33><size=55%>/" + rundownProgData2.totalSecondary + "</color></size>");
                __instance.m_tierMarkerSectorSummary.SetSectorIconTextForThird(rundownProgData2.clearedThird + "<size=50%><color=#FFFFFF33><size=55%>/" + rundownProgData2.totalThird + "</color></size>");
                __instance.m_tierMarkerSectorSummary.SetSectorIconTextForAllCleared(rundownProgData2.clearedAllClear + "<size=50%><color=#FFFFFF33><size=55%>/" + rundownProgData2.totalAllClear + "</color></size>");
            }
            if (!(__instance.m_tierMarker1 != null))
                return false;
            __instance.m_tierMarker1.SetProgression(rundownProgData2, new RundownTierProgressionData());
            __instance.UpdateTierIconsWithProgression(rundownProgression, rundownProgData2, __instance.m_expIconsTier1, __instance.m_tierMarker1, true);
            __instance.m_tierMarker2.SetProgression(rundownProgData2, __instance.m_currentRundownData.ReqToReachTierB);
            __instance.UpdateTierIconsWithProgression(rundownProgression, rundownProgData2, __instance.m_expIconsTier2, __instance.m_tierMarker2, rundownProgData2.tierBUnlocked && __instance.m_currentRundownData.UseTierUnlockRequirements);
            __instance.m_tierMarker3.SetProgression(rundownProgData2, __instance.m_currentRundownData.ReqToReachTierC);
            __instance.UpdateTierIconsWithProgression(rundownProgression, rundownProgData2, __instance.m_expIconsTier3, __instance.m_tierMarker3, rundownProgData2.tierCUnlocked && __instance.m_currentRundownData.UseTierUnlockRequirements);
            __instance.m_tierMarker4.SetProgression(rundownProgData2, __instance.m_currentRundownData.ReqToReachTierD);
            __instance.UpdateTierIconsWithProgression(rundownProgression, rundownProgData2, __instance.m_expIconsTier4, __instance.m_tierMarker4, rundownProgData2.tierDUnlocked && __instance.m_currentRundownData.UseTierUnlockRequirements);
            __instance.m_tierMarker5.SetProgression(rundownProgData2, __instance.m_currentRundownData.ReqToReachTierE);
            __instance.UpdateTierIconsWithProgression(rundownProgression, rundownProgData2, __instance.m_expIconsTier5, __instance.m_tierMarker5, rundownProgData2.tierEUnlocked && __instance.m_currentRundownData.UseTierUnlockRequirements);

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Global), nameof(Global.OnApplicationQuit))]
        private static void Post_OnApplicationQuit()
        {
            if(LocalProgDict != null)
            {
                LocalProgDict.Clear();
            }
            LocalProgDict = null;
        }
    }
}
