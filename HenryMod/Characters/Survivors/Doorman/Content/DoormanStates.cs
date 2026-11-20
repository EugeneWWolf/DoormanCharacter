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

            Modules.Content.AddEntityState(typeof(ThrowBell));
            Modules.Content.AddEntityState(typeof(DoorDash));
            Modules.Content.AddEntityState(typeof(ThrowCart));
            Modules.Content.AddEntityState(typeof(BaronessState));
        }
    }
}
