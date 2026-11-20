using EntityStates;
using RoR2;
using UnityEngine;
using EntityStates.Commando.CommandoWeapon; // Временные эффекты Commando

namespace DoormanMod.Survivors.Doorman.SkillStates
{
    public class Shoot : BaseSkillState
    {
        public static float BaseDuration = 0.2f; // Задержка между выстрелами (можно менять в EntityState, либо в SkillDef)
        public static float DamageCoefficient = 1f; // Базовый урон (100%)

        private float duration;
        private Ray aimRay;

        // Временные эффекты для теста (замени на свои позже)
        public GameObject hitEffectPrefab = FireBarrage.hitEffectPrefab;
        public GameObject tracerEffectPrefab = FireBarrage.tracerEffectPrefab;

        public override void OnEnter()
        {
            base.OnEnter();

            // Рассчитываем длительность с учётом скорости атаки (attackSpeedStat)
            this.duration = BaseDuration / base.attackSpeedStat;

            // Получаем луч прицела игрока
            this.aimRay = base.GetAimRay();

            // Запускаем режим прицеливания (камера немного приближается)
            base.StartAimMode(aimRay, 2f, false);

            // Проигрываем анимацию (используем анимацию Commando "FirePistol, Right")
            base.PlayAnimation("Gesture Additive, Right", "FirePistol, Right");

            // Воспроизводим звук выстрела (пока от Commando)
            Util.PlaySound(FireBarrage.fireBarrageSoundString, base.gameObject);

            // Добавляем отдачу камеры (по желанию можно убрать)
            base.AddRecoil(-0.6f, 0.6f, -0.6f, 0.6f);

            // Спавним эффект вспышки на дуле оружия
            if (FireBarrage.effectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(FireBarrage.effectPrefab, base.gameObject, "MuzzleRight", false);
            }

            // Самое главное: выпускаем пулю (только на сервере/хосте)
            if (base.isAuthority)
            {
                FireBullet();
            }
        }

        private void FireBullet()
        {
            new BulletAttack
            {
                owner = base.gameObject,
                weapon = base.gameObject,
                origin = this.aimRay.origin,
                aimVector = this.aimRay.direction,
                minSpread = 0f,
                maxSpread = base.characterBody.spreadBloomAngle,
                bulletCount = 1U,
                procCoefficient = 1f,
                damage = base.characterBody.damage * DamageCoefficient,
                force = 3,
                falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                tracerEffectPrefab = this.tracerEffectPrefab,
                muzzleName = "MuzzleRight",
                hitEffectPrefab = this.hitEffectPrefab,
                isCrit = base.RollCrit(),
                HitEffectNormal = false,
                stopperMask = LayerIndex.world.mask,
                smartCollision = true,
                maxDistance = 300f
            }.Fire();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            // Если время действия состояния истекло и мы на сервере — возвращаемся в обычное состояние (Main)
            // Это позволяет стрелять очередями, если кнопка зажата (благодаря настройкам SkillDef)
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}