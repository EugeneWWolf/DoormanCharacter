using EntityStates;
using RoR2.Projectile;
using UnityEngine;

namespace DoormanMod.Survivors.Doorman.SkillStates
{
    public class ThrowCart : BaseSkillState
    {
        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = 0.35f / attackSpeedStat;
            if (!isAuthority)
                return;

            Ray aim = GetAimRay();
            Vector3 forward = Vector3.ProjectOnPlane(aim.direction, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = characterDirection.forward;

            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                projectilePrefab = DoormanAssets.CartProjectile,
                owner = gameObject,
                position = characterBody.footPosition + Vector3.up * 0.65f,
                rotation = Quaternion.LookRotation(forward),
                damage = damageStat * 6f,
                crit = RollCrit()
            });
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= duration)
                outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }
}
