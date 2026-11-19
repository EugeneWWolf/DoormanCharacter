using RoR2;
using DoormanMod.Modules.Achievements;

namespace DoormanMod.Survivors.Doorman.Achievements
{
    //automatically creates language tokens "ACHIEVMENT_{identifier.ToUpper()}_NAME" and "ACHIEVMENT_{identifier.ToUpper()}_DESCRIPTION" 
    [RegisterAchievement(identifier, unlockableIdentifier, null, 10, null)]
    public class DoormanMasteryAchievement : BaseMasteryAchievement
    {
        public const string identifier = DoormanSurvivor.DOORMAN_PREFIX + "masteryAchievement";
        public const string unlockableIdentifier = DoormanSurvivor.DOORMAN_PREFIX + "masteryUnlockable";

        public override string RequiredCharacterBody => DoormanSurvivor.Instance.BodyName;

        //difficulty coeff 3 is monsoon. 3.5 is typhoon for grandmastery skins
        public override float RequiredDifficultyCoefficient => 3;
    }
}