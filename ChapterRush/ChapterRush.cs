using HarmonyLib;
using MelonLoader;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChapterRush
{
    public class ChapterRush : MelonMod
    {
        private static bool chapterRush = false;

        private static readonly int[] chapterLevelAmount = { 10, 10, 10, 3, 10, 10, 10, 10, 10, 2, 10, 2 };
        private static int levelAmount = 0;
        private static int chapterIndex = 0;
        private static readonly LevelRushData[] levelRushDatas = new LevelRushData[12];

        private static readonly Color buttonBackground = new(0.8f, 0.8f, 0.8f, 0.1f);

        private static MelonPreferences_Entry<bool> hellRush;
        private static MelonPreferences_Entry<bool> shuffle;

        public override void OnApplicationLateStart()
        {
            PatchGame();

            MelonPreferences_Category chapterRush = MelonPreferences.CreateCategory("Chapter Rush");
            hellRush = chapterRush.CreateEntry("Hell Rush", false);
            shuffle = chapterRush.CreateEntry("Shuffle", false);
        }

        public static void StartChapterRush(int chapterIndex)
        {
            ChapterRush.chapterIndex = chapterIndex;
            levelAmount = chapterLevelAmount[chapterIndex];
            LevelRush.SetLevelRush(LevelRush.LevelRushType.WhiteRush, !hellRush.Value, false);
            chapterRush = true;

            LevelRushStats currentLevelRush = LevelRush.GetCurrentLevelRush();
            currentLevelRush.RandomizeIndex(levelAmount, shuffle.Value);

            MainMenu.Instance()._screenLevelRush.shuffleToggle.isOn = shuffle.Value;
            LevelRush.PlayCurrentLevelRushMission();
        }

        private void PatchGame()
        {
            HarmonyLib.Harmony harmony = new("de.MOPSKATER.BoofOfMemes");

            BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

            MethodInfo target = typeof(LevelRush).GetMethod("SetLevelRush", flags);
            HarmonyMethod patch = new(typeof(ChapterRush).GetMethod("PostSetLevelRush"));
            harmony.Patch(target, null, patch);

            target = typeof(LevelRush).GetMethod("OnLevelRushComplete", flags);
            patch = new(typeof(ChapterRush).GetMethod("PreOnLevelRushComplete"));
            harmony.Patch(target, patch);

            target = typeof(LevelRush).GetMethod("GetNumLevelsInRush", flags);
            patch = new(typeof(ChapterRush).GetMethod("PreGetNumLevelsInRush"));
            harmony.Patch(target, patch);

            target = typeof(LevelRush).GetMethod("GetLevelRushDataByType", flags);
            patch = new(typeof(ChapterRush).GetMethod("PreGetLevelRushDataByType"));
            harmony.Patch(target, patch);

            target = typeof(LevelRushStats).GetMethod("RandomizeIndex");
            patch = new(typeof(ChapterRush).GetMethod("PostRandomizeIndex"));
            harmony.Patch(target, null, patch);

            target = typeof(LevelRush).GetMethod("GetCurrentLevelRushLevelData", BindingFlags.Public | BindingFlags.Static);
            patch = new(typeof(ChapterRush).GetMethod("PreGetCurrentLevelRushLevelData"));
            harmony.Patch(target, patch);

            target = typeof(MainMenu).GetMethod("SelectCampaign");
            patch = new(typeof(ChapterRush).GetMethod("PostSelectCampaign"));
            harmony.Patch(target, null, patch);

            target = typeof(LeaderboardIntegrationSteam).GetMethod("UploadScore_LevelRush", flags);
            patch = new(typeof(ChapterRush).GetMethod("PreUploadScore_LevelRush"));
            harmony.Patch(target, patch);
        }

        public static void PostSetLevelRush() => chapterRush = false;

        public static void PreOnLevelRushComplete()
        {
            if (!chapterRush) return;

            if (levelRushDatas[chapterIndex] == null)
                levelRushDatas[chapterIndex] = new LevelRushData(LevelRush.LevelRushType.WhiteRush);

            return;
        }

        public static bool PreGetNumLevelsInRush(ref int __result)
        {
            if (!chapterRush || LevelRush.GetCurrentLevelRush().randomizedIndex == null) return true;

            __result = levelAmount;
            return false;
        }

        public static bool PreGetLevelRushDataByType(ref LevelRushData __result)
        {
            if (!chapterRush) return true;

            if (levelRushDatas[chapterIndex] == null)
                levelRushDatas[chapterIndex] = new(LevelRush.LevelRushType.WhiteRush);

            __result = levelRushDatas[chapterIndex];
            return false;
        }

        public static void PostRandomizeIndex()
        {
            if (!chapterRush) return;

            LevelRushStats currentLevelRush = LevelRush.GetCurrentLevelRush();

            int offset = 0;
            for (int i = 0; i < chapterIndex; i++)
                offset += chapterLevelAmount[i];

            for (int i = 0; i < currentLevelRush.randomizedIndex.Length; i++)
                currentLevelRush.randomizedIndex[i] += offset;
        }

        public static bool PreGetCurrentLevelRushLevelData(ref LevelData __result)
        {
            int nextLevel = LevelRush.GetCurrentLevelRush().randomizedIndex[LevelRush.GetCurrentLevelRush().currentLevelIndex];
            if (!chapterRush || nextLevel < 95) return true;

            if (nextLevel == 95)
                __result = Singleton<Game>.Instance.GetGameData().campaigns[0].missionData[11].levels[0];
            if (nextLevel == 96)
                __result = Singleton<Game>.Instance.GetGameData().campaigns[0].missionData[11].levels[1];
            return false;
        }

        public static void PostSelectCampaign(ref string campaignID, ref bool goToMissionScreen)
        {
            if (campaignID != "C_MAINQUEST" || !goToMissionScreen) return;

            MenuScreenMission missionScreen = MainMenu._instance._screenMission;
            for (int i = 0; i < missionScreen.buttonsToLoad.Count; i++)
            {
                MenuButtonHolder buttonHolder = missionScreen.buttonsToLoad[i];
                GameObject rushButton = new("Rush Button", new Type[] { typeof(Image), typeof(RushButton) });
                GameObject rushButtonText = new("Text");

                Transform transform = rushButton.transform;
                transform.SetParent(buttonHolder.transform);
                transform.localPosition = new(-200f, 0, 0);
                transform.localScale = new(.5f, .5f, .5f);

                rushButton.GetComponent<Image>().color = buttonBackground;
                rushButton.GetComponent<RushButton>().Setup(i);

                transform = rushButtonText.transform;
                transform.SetParent(rushButton.transform.transform);
                transform.localPosition = new(125f, 0, 0);
                transform.localScale = new(1.5f, 1.5f, 1.5f);

                rushButtonText.AddComponent<TextMeshProUGUI>().text = "R";
            }
        }

        public static bool PreUploadScore_LevelRush()
        {
            if (chapterRush) return false;
            return true;
        }
    }
}