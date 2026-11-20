using EntityStates;
using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace DoormanMod.Survivors.Doorman.SkillStates
{
    internal sealed class BaronessSkillDef : SkillDef
    {
        public override bool CanExecute(GenericSkill slot)
        {
            if (!base.CanExecute(slot) || !slot.characterBody)
                return false;

            InputBankTest input = slot.characterBody.inputBank;
            return input && BaronessState.FindTarget(slot.characterBody, input.GetAimRay());
        }
    }

    public class BaronessState : BaseSkillState
    {
        private const float Duration = 3f;
        private const float Range = 22f;
        private const float AttackDuration = 0.35f;

        public override void OnEnter()
        {
            base.OnEnter();
            if (!NetworkServer.active)
                return;

            HealthComponent target = FindTarget(characterBody, GetAimRay());
            if (!target)
                return;

            target.body.AddBuff(DoormanAssets.Banished);
            SetStateOnHurt.SetStunOnObject(target.gameObject, Duration);
            BaronessReturn.Apply(target, gameObject, damageStat * 10f, Duration);
        }

        internal static HealthComponent FindTarget(CharacterBody caster, Ray aim)
        {
            const float minimumAimDot = 0.9781476f; // 12 degrees from the aim ray.
            if (!caster || !caster.teamComponent)
                return null;

            Vector3 origin = aim.origin;
            HealthComponent nearest = null;
            float distance = Range * Range;
            foreach (Collider collider in Physics.OverlapSphere(origin, Range, LayerIndex.entityPrecise.mask,
                         QueryTriggerInteraction.Collide))
            {
                HurtBox hurtBox = collider.GetComponent<HurtBox>();
                if (!hurtBox || !hurtBox.healthComponent || !hurtBox.healthComponent.alive)
                    continue;

                HealthComponent target = hurtBox.healthComponent;
                CharacterBody body = target.body;
                if (!body || !body.teamComponent || body.teamComponent.teamIndex == caster.teamComponent.teamIndex ||
                    body.teamComponent.teamIndex == TeamIndex.Neutral || body.HasBuff(DoormanAssets.Banished))
                    continue;

                Vector3 offset = hurtBox.transform.position - origin;
                float candidate = offset.sqrMagnitude;
                if (candidate >= distance || Vector3.Dot(aim.direction, offset.normalized) < minimumAimDot || Physics.Linecast(origin, hurtBox.transform.position, LayerIndex.world.mask,
                        QueryTriggerInteraction.Ignore))
                    continue;

                nearest = target;
                distance = candidate;
            }
            return nearest;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= AttackDuration)
                outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }

    internal sealed class BaronessReturn : MonoBehaviour
    {
        private HealthComponent target;
        private GameObject attacker;
        private float damage;
        private float remaining;

        internal static void Apply(HealthComponent target, GameObject attacker, float damage, float duration)
        {
            BaronessReturn effect = target.gameObject.AddComponent<BaronessReturn>();
            effect.target = target;
            effect.attacker = attacker;
            effect.damage = damage;
            effect.remaining = duration;
        }

        private void FixedUpdate()
        {
            remaining -= Time.fixedDeltaTime;
            if (remaining > 0f && target && target.alive)
                return;

            if (target && target.body && target.body.HasBuff(DoormanAssets.Banished))
            {
                target.body.RemoveBuff(DoormanAssets.Banished);
                Banishment visual = target.GetComponent<Banishment>();
                if (visual)
                    visual.Restore();

                if (target.alive)
                {
                    DamageInfo info = new DamageInfo
                    {
                        attacker = attacker,
                        damage = damage,
                        position = target.body.corePosition,
                        damageType = DamageTypeCombo.GenericSpecial,
                        procCoefficient = 1f
                    };
                    target.TakeDamage(info);
                    if (attacker && !info.rejected && GlobalEventManager.instance)
                    {
                        GlobalEventManager.instance.OnHitEnemy(info, target.gameObject);
                        GlobalEventManager.instance.OnHitAll(info, target.gameObject);
                    }
                }
            }

            Destroy(this);
        }
    }
}
