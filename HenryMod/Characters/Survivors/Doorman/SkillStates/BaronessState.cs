using EntityStates;
using RoR2;
using UnityEngine;

namespace DoormanMod.Survivors.Doorman.SkillStates
{
    public class BaronessState : BaseSkillState
    {
        public static float Duration = 3f;
        public static float DamageCoefficient = 10f;

        private CharacterBody targetBody;
        private Vector3 originalPosition;

        public override void OnEnter()
        {
            base.OnEnter();

            // Находим цель (например, ближайшего врага в радиусе)
            var enemies = Physics.OverlapSphere(transform.position, 10f, LayerIndex.entityPrecise.mask);
            foreach (var enemy in enemies)
            {
                var body = enemy.GetComponent<CharacterBody>();
                if (body != null && body.teamComponent.teamIndex != characterBody.teamComponent.teamIndex)
                {
                    targetBody = body;
                    originalPosition = body.transform.position;
                    break;
                }
            }

            if (targetBody != null)
            {
                // "Исчезновение": отключаем рендерер и обездвиживаем
                foreach (var renderer in targetBody.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = false;
                }
                targetBody.characterMotor.enabled = false;
                targetBody.transform.position = new Vector3(0, -1000, 0); // временно убираем
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= Duration)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            if (targetBody != null)
            {
                // Возвращаем и наносим урон
                targetBody.transform.position = originalPosition;
                foreach (var renderer in targetBody.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = true;
                }
                targetBody.characterMotor.enabled = true;

                targetBody.healthComponent.TakeDamage(new DamageInfo
                {
                    attacker = characterBody.gameObject,
                    damage = characterBody.damage * DamageCoefficient,
                    position = originalPosition,
                    procCoefficient = 1f,
                });
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}