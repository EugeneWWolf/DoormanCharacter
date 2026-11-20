using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace DoormanMod.Survivors.Doorman.SkillStates
{
    public class ThrowBell : GenericProjectileBaseState
    {
        public static float BaseDuration = 0.5f;
        public static float DamageCoefficient = 8f;

        public override void OnEnter()
        {
            projectilePrefab = DoormanAssets.bellProjectilePrefab;
            baseDuration = BaseDuration;
            damageCoefficient = DamageCoefficient;
            force = 40f;
            recoilAmplitude = 0.05f;
            base.OnEnter();
        }

        public override void ModifyProjectileInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            base.ModifyProjectileInfo(ref fireProjectileInfo);
            fireProjectileInfo.damageTypeOverride = DamageTypeCombo.GenericSpecial;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}