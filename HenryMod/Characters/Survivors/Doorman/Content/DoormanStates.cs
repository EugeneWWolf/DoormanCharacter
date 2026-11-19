using DoormanMod.Survivors.Doorman.SkillStates;

namespace DoormanMod.Survivors.Doorman
{
    public static class DoormanStates
    {
        public static void Init()
        {
            Modules.Content.AddEntityState(typeof(SlashCombo));

            Modules.Content.AddEntityState(typeof(Shoot));

            Modules.Content.AddEntityState(typeof(Roll));

            Modules.Content.AddEntityState(typeof(ThrowBomb));
        }
    }
}
