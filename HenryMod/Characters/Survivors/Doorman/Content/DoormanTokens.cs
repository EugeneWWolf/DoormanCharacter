using DoormanMod.Modules;

namespace DoormanMod.Survivors.Doorman
{
    public static class DoormanTokens
    {
        public static void Init()
        {
            string prefix = DoormanSurvivor.DOORMAN_PREFIX;
            Language.Add(prefix + "NAME", "Doorman");
            Language.Add(prefix + "SUBTITLE", "Hotel Attendant");
            Language.Add(prefix + "DESCRIPTION", "Use your pistol and hotel equipment to control the fight.");
            Language.Add(prefix + "OUTRO_FLAVOR", "..and so he left, the doors closing behind him.");
            Language.Add(prefix + "OUTRO_FAILURE", "..and so the doors closed for the last time.");
            AddSkill("SHOOT", "Pistol", "Fire a pistol. Hold to keep shooting.");
            AddSkill("BELL", "Bell", "Throw a bell that slows to a stop. Shoot your own bell to detonate it in the marked 16 m sphere.");
            AddSkill("DOOR", "Door", "Dash forward and open two connected doors for 8 seconds. Step away and walk back into either door to cross.");
            AddSkill("CART", "Luggage Cart", "Send a cart forward. It carries enemies and stuns them if it hits a wall.");
            AddSkill("ULTIMATE", "Baroness", "Aim at a visible enemy to banish it for three seconds, then deal damage.");
        }

        private static void AddSkill(string id, string name, string description)
        {
            string prefix = DoormanSurvivor.DOORMAN_PREFIX + id;
            Language.Add(prefix + "_NAME", name);
            Language.Add(prefix + "_DESCRIPTION", description);
        }
    }
}
