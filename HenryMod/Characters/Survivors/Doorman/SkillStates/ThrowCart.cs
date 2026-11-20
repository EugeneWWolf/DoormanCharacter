using EntityStates;
using DoormanMod.Survivors.Doorman;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace DoormanMod.Survivors.Doorman.SkillStates
{
    public class ThrowCart : GenericProjectileBaseState
    {
        public static float BaseDuration = 0.4f;

        public static float DamageCoefficient = 12f;

        public override void OnEnter()
        {
            projectilePrefab = DoormanAssets.cartProjectilePrefab;
            baseDuration = BaseDuration;
            damageCoefficient = DamageCoefficient;
            force = 30f;
            base.OnEnter();
        }

        public override void ModifyProjectileInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            base.ModifyProjectileInfo(ref fireProjectileInfo);
            fireProjectileInfo.damageTypeOverride = DamageTypeCombo.GenericSpecial;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }

        public override void PlayAnimation(float duration)
        {

            if (GetModelAnimator())
            {
                PlayAnimation("Gesture, Override", "ThrowBomb", "ThrowBomb.playbackRate", this.duration);
            }
        }
    }
}