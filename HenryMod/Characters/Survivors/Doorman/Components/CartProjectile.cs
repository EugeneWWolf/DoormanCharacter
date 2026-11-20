using System.Collections.Generic;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace DoormanMod.Survivors.Doorman
{
    internal sealed class CartProjectile : MonoBehaviour
    {
        private const float Speed = 22f;
        private const float Lifetime = 2f;
        private const float GroundClearance = 0.65f;

        private readonly HashSet<HealthComponent> hit = new HashSet<HealthComponent>();
        private readonly List<CharacterBody> passengers = new List<CharacterBody>();
        private ProjectileController controller;
        private ProjectileDamage damage;
        private TeamFilter team;
        private float age;

        private void Awake()
        {
            controller = GetComponent<ProjectileController>();
            damage = GetComponent<ProjectileDamage>();
            team = GetComponent<TeamFilter>();
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;

            age += Time.fixedDeltaTime;
            if (age >= Lifetime || !controller.owner)
            {
                NetworkServer.Destroy(gameObject);
                return;
            }

            Vector3 position = transform.position;
            Vector3 heading = transform.forward;
            float distance = Speed * Time.fixedDeltaTime;
            bool impact = false;
            RaycastHit wall = default;
            foreach (RaycastHit hit in Physics.SphereCastAll(position + heading * 0.8f, 0.5f,
                         heading, distance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y >= 0.5f || (impact && hit.distance >= wall.distance))
                    continue;

                wall = hit;
                impact = true;
            }
            if (impact)
                distance = Mathf.Max(0f, wall.distance - 0.02f);

            position += heading * distance;
            if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit floor, 2f,
                    LayerIndex.world.mask, QueryTriggerInteraction.Ignore) && floor.normal.y >= 0.5f)
                position.y = floor.point.y + GroundClearance;

            transform.position = position;
            CollectPassengers(position);
            foreach (CharacterBody passenger in passengers)
            {
                if (!passenger || !passenger.healthComponent || !passenger.healthComponent.alive ||
                    passenger.HasBuff(DoormanAssets.Banished))
                    continue;

                Carry(passenger, position);
                if (impact)
                {
                    DealDamage(passenger.healthComponent, 1f);
                    SetStateOnHurt.SetStunOnObject(passenger.gameObject, 2f);
                }
            }

            if (impact)
                NetworkServer.Destroy(gameObject);
        }

        private void CollectPassengers(Vector3 position)
        {
            foreach (Collider collider in Physics.OverlapBox(position, new Vector3(1f, 0.8f, 1.5f),
                         transform.rotation, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Collide))
            {
                HurtBox hurtBox = collider.GetComponent<HurtBox>();
                if (!hurtBox || !hurtBox.healthComponent || !hurtBox.healthComponent.alive)
                    continue;

                HealthComponent health = hurtBox.healthComponent;
                CharacterBody victim = health.body;
                if (!victim || !victim.teamComponent || victim.teamComponent.teamIndex == team.teamIndex ||
                    victim.teamComponent.teamIndex == TeamIndex.Neutral ||
                    victim.HasBuff(DoormanAssets.Banished) || !hit.Add(health))
                    continue;

                DealDamage(health, 0.5f);
                if (health.alive)
                    passengers.Add(victim);
            }
        }

        private static void Carry(CharacterBody body, Vector3 position)
        {
            CharacterMotor motor = body.characterMotor;
            if (motor)
            {
                motor.velocity = Vector3.zero;
                float pivotHeight = body.transform.position.y - body.footPosition.y;
                Vector3 destination = position + Vector3.up * (pivotHeight - GroundClearance);
                motor.Motor.SetPosition(destination);
                return;
            }

            Rigidbody rigidbody = body.GetComponent<Rigidbody>();
            if (rigidbody && !rigidbody.isKinematic)
            {
                rigidbody.velocity = Vector3.zero;
                rigidbody.MovePosition(new Vector3(position.x, rigidbody.position.y, position.z));
            }
        }

        private void DealDamage(HealthComponent target, float coefficient)
        {
            DamageInfo info = new DamageInfo
            {
                attacker = controller.owner,
                inflictor = gameObject,
                position = target.body.corePosition,
                damage = damage.damage * coefficient,
                crit = damage.crit,
                damageType = DamageTypeCombo.GenericSpecial,
                procCoefficient = 0.7f
            };
            target.TakeDamage(info);
            if (!info.rejected && GlobalEventManager.instance)
            {
                GlobalEventManager.instance.OnHitEnemy(info, target.gameObject);
                GlobalEventManager.instance.OnHitAll(info, target.gameObject);
            }
        }
    }
}
