using RoR2;
using UnityEngine;

namespace DoormanMod.Survivors.Doorman
{
    public static class DoormanBuffs
    {
        // armor buff gained during roll
        public static BuffDef armorBuff;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Удалите неиспользуемый параметр", Justification = "Public API (BepInEx)")]
        public static void Init(AssetBundle assetBundle)
        {
            armorBuff = Modules.Content.CreateAndAddBuff("DoormanArmorBuff",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                Color.white,
                false,
                false);

        }
    }
}
