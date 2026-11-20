using EntityStates;
using EntityStates.Commando.CommandoWeapon;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DoormanMod.Survivors.Doorman.SkillStates
{
    public class Shoot : BaseSkillState
    {
        private const float Range = 256f;
        private float duration;
        private Ray aim;
        private bool fired;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = 0.2f / attackSpeedStat;
            if (isAuthority)
                aim = GetAimRay();

            StartAimMode(2f);
            PlayAnimation("Gesture Additive, Right", "FirePistol, Right");
            if (!string.IsNullOrEmpty(FirePistol2.firePistolSoundString))
                Util.PlaySound(FirePistol2.firePistolSoundString, gameObject);
            if (FirePistol2.muzzleEffectPrefab)
            {
                ChildLocator locator = GetModelChildLocator();
                if (locator)
                {
                    string muzzle = locator.FindChild("MuzzleRight") ? "MuzzleRight" : "Muzzle";
                    if (locator.FindChild(muzzle))
                        EffectManager.SimpleMuzzleFlash(FirePistol2.muzzleEffectPrefab, gameObject, muzzle, false);
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!fired && (isAuthority || NetworkServer.active) && aim.direction.sqrMagnitude > 0.001f)
            {
                fired = true;
                bool hitBell = BellProjectile.TryShoot(gameObject, aim, Range);
                if (isAuthority)
                {
                    AddRecoil(-0.6f, 0.6f, -0.6f, 0.6f);
                    if (!hitBell)
                        FireBullet();
                }
            }

            if (fixedAge >= duration && isAuthority)
                outer.SetNextStateToMain();
        }

        private void FireBullet()
        {
            new BulletAttack
            {
                owner = gameObject,
                weapon = gameObject,
                origin = aim.origin,
                aimVector = aim.direction,
                bulletCount = 1,
                damage = damageStat,
                force = 5f,
                procCoefficient = 1f,
                isCrit = RollCrit(),
                hitMask = LayerIndex.CommonMasks.bullet,
                stopperMask = LayerIndex.CommonMasks.bullet,
                falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                smartCollision = true,
                maxDistance = Range,
                hitEffectPrefab = FirePistol2.hitEffectPrefab,
                tracerEffectPrefab = FirePistol2.tracerEffectPrefab
            }.Fire();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(aim.origin);
            writer.Write(aim.direction);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            aim = new Ray(reader.ReadVector3(), reader.ReadVector3());
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
}
