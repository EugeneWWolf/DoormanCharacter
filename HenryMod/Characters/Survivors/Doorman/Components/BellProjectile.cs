using System.Collections.Generic;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace DoormanMod.Survivors.Doorman
{
    internal sealed class BellProjectile : MonoBehaviour
    {
        private const float Lifetime = 10f;
        private const float InitialSpeed = 22f;
        private const float Deceleration = 18f;
        private const float CollisionRadius = 0.65f;
        private const float ShotRadius = 1.15f;
        private const float BlastRadius = 16f;
        private const int RingSegments = 120;
        private Transform indicatorTransform;

        private static readonly HashSet<BellProjectile> Live = new HashSet<BellProjectile>();
        private ProjectileController controller;
        private ProjectileDamage damage;
        private float age;
        private float speed = InitialSpeed;

        private void Awake()
        {
            controller = GetComponent<ProjectileController>();
            damage = GetComponent<ProjectileDamage>();
        }

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);

        private void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;

            float dt = Time.fixedDeltaTime;
            age += dt;
            if (age >= Lifetime || !controller.owner)
            {
                NetworkServer.Destroy(gameObject);
                return;
            }

            float nextSpeed = Mathf.MoveTowards(speed, 0f, Deceleration * dt);
            float distance = (speed + nextSpeed) * 0.5f * dt;
            Vector3 direction = transform.forward;
            if (distance > 0f && Physics.SphereCast(transform.position, CollisionRadius, direction,
                    out RaycastHit wall, distance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                distance = Mathf.Max(0f, wall.distance - 0.02f);
                nextSpeed = 0f;
            }

            transform.position += direction * distance;
            speed = nextSpeed;
        }

        internal static bool TryShoot(GameObject shooter, Ray aim, float range)
        {
            float limit = Physics.Raycast(aim, out RaycastHit wall, range, LayerIndex.world.mask,
                QueryTriggerInteraction.Ignore) ? wall.distance : range;

            BellProjectile nearest = null;
            foreach (BellProjectile bell in Live)
            {
                if (!bell || bell.controller.owner != shooter)
                    continue;

                Vector3 offset = bell.transform.position - aim.origin;
                float center = Vector3.Dot(offset, aim.direction);
                float perpendicular = Vector3.Cross(offset, aim.direction).sqrMagnitude;
                float discriminant = ShotRadius * ShotRadius - perpendicular;
                if (center < 0f || discriminant < 0f)
                    continue;

                float entry = Mathf.Max(0f, center - Mathf.Sqrt(discriminant));
                if (entry < limit)
                {
                    limit = entry;
                    nearest = bell;
                }
            }

            if (!nearest)
                return false;

            if (NetworkServer.active)
                nearest.Detonate();
            return true;
        }

        private void Start()
        {
            // The indicator is optional. No shader lookup or gameplay initialization happens here.
            try
            {
                CreateBlastIndicator();
            }
            catch (System.Exception exception)
            {
                if (indicatorTransform)
                    Destroy(indicatorTransform.gameObject);
                Log.Error($"Could not draw the bell blast indicator: {exception}");
            }
        }

        private void CreateBlastIndicator()
        {
            Renderer visual = GetComponentInChildren<Renderer>();
            if (!visual || !visual.sharedMaterial)
                return;

            GameObject indicator = new GameObject("BlastRadius");
            indicatorTransform = indicator.transform;
            indicatorTransform.SetParent(transform, false);
            indicatorTransform.rotation = Quaternion.identity;

            DrawBlastRing(visual.sharedMaterial, Vector3.right, Vector3.forward);
            DrawBlastRing(visual.sharedMaterial, Vector3.right, Vector3.up);
            DrawBlastRing(visual.sharedMaterial, Vector3.forward, Vector3.up);
        }

        private void DrawBlastRing(Material material, Vector3 firstAxis, Vector3 secondAxis)
        {
            GameObject ring = new GameObject("RadiusRing");
            ring.transform.SetParent(indicatorTransform, false);
            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            MaterialPropertyBlock tint = new MaterialPropertyBlock();
            tint.SetColor("_Color", Color.red);
            tint.SetColor("_BaseColor", Color.red);
            line.SetPropertyBlock(tint);
            line.useWorldSpace = false;
            line.loop = true;
            line.startColor = line.endColor = Color.red;
            line.startWidth = line.endWidth = 0.12f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            Vector3[] points = new Vector3[RingSegments];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * (2f * Mathf.PI / points.Length);
                points[i] = (firstAxis * Mathf.Cos(angle) + secondAxis * Mathf.Sin(angle)) * BlastRadius;
            }
            line.positionCount = points.Length;
            line.SetPositions(points);
        }

        private void LateUpdate()
        {
            if (indicatorTransform)
                indicatorTransform.rotation = Quaternion.identity;
        }

        private void Detonate()
        {
            Live.Remove(this);
            new BlastAttack
            {
                attacker = controller.owner,
                inflictor = gameObject,
                teamIndex = GetComponent<TeamFilter>().teamIndex,
                position = transform.position,
                radius = BlastRadius,
                baseDamage = damage.damage,
                crit = damage.crit,
                procCoefficient = 1f,
                falloffModel = BlastAttack.FalloffModel.None,
                attackerFiltering = AttackerFiltering.NeverHitSelf
            }.Fire();
            if (DoormanAssets.BellExplosion)
                EffectManager.SimpleEffect(DoormanAssets.BellExplosion, transform.position, Quaternion.identity, true);
            NetworkServer.Destroy(gameObject);
        }
    }
}
