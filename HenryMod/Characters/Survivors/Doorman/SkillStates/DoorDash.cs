using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DoormanMod.Survivors.Doorman.SkillStates
{
    public class DoorDash : BaseSkillState
    {
        private const float Distance = 15f;
        private const float Duration = 0.4f;
        private Vector3 direction;
        private float remaining;
        private float travelDistance;
        private Vector3 entrance;
        private DoorPair doors;

        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority)
            {
                direction = Vector3.ProjectOnPlane(GetAimRay().direction, Vector3.up).normalized;
                if (direction.sqrMagnitude < 0.001f)
                    direction = characterDirection ? characterDirection.forward : transform.forward;

                travelDistance = Distance;
                if (characterMotor && characterMotor.GetComponent<CapsuleCollider>() is CapsuleCollider capsule)
                {
                    float height = Mathf.Max(capsule.height * 0.5f - capsule.radius, 0f);
                    Vector3 center = transform.TransformPoint(capsule.center);
                    if (Physics.CapsuleCast(center + Vector3.up * height, center - Vector3.up * height,
                            capsule.radius * 0.92f, direction, out RaycastHit wall, Distance,
                            LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                        travelDistance = Mathf.Max(0f, wall.distance - 0.1f);
                }
                remaining = travelDistance;
                entrance = characterBody.footPosition + Vector3.up * 1.35f;
            }

            if (NetworkServer.active)
                OpenEntrance();
        }

        private void OpenEntrance()
        {
            if (doors || direction.sqrMagnitude < 0.001f || !characterBody)
                return;

            doors = gameObject.AddComponent<DoorPair>();
            doors.OpenEntrance(characterBody, entrance, direction);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (NetworkServer.active)
                OpenEntrance();

            if (!isAuthority)
                return;

            if (characterMotor)
            {
                Vector3 velocity = characterMotor.velocity;
                float speed = Mathf.Min(Distance / Duration, remaining / Time.fixedDeltaTime);
                characterMotor.velocity = direction * speed + Vector3.up * velocity.y;
            }

            remaining = Mathf.Max(0f, remaining - Distance / Duration * Time.fixedDeltaTime);
            if (fixedAge >= Duration + Time.fixedDeltaTime)
                outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (isAuthority && characterMotor)
                characterMotor.velocity = Vector3.up * characterMotor.velocity.y;
            if (NetworkServer.active && doors && characterBody && healthComponent && healthComponent.alive)
                doors.OpenExit(characterBody.footPosition);

            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(direction);
            writer.Write(travelDistance);
            writer.Write(entrance);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            direction = reader.ReadVector3();
            travelDistance = reader.ReadSingle();
            entrance = reader.ReadVector3();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }
}
