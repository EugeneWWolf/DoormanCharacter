using DoormanMod.Survivors.Doorman.Achievements;
using RoR2;
using UnityEngine;

namespace DoormanMod.Survivors.Doorman
{
    public static class DoormanUnlockables
    {
        public static UnlockableDef characterUnlockableDef = null;
        public static UnlockableDef masterySkinUnlockableDef = null;

        public static void Init()
        {
            masterySkinUnlockableDef = Modules.Content.CreateAndAddUnlockbleDef(
                DoormanMasteryAchievement.unlockableIdentifier,
                Modules.Tokens.GetAchievementNameToken(DoormanMasteryAchievement.identifier),
                DoormanSurvivor.Instance.AssetBundle.LoadAsset<Sprite>("texMasteryAchievement"));
        }
    }
}
