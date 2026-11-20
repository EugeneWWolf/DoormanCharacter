using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DoormanMod.Survivors.Doorman
{
    internal sealed class Banishment : MonoBehaviour
    {
        private CharacterModel model;
        private HurtBoxGroup hurtBoxes;
        private CharacterMotor motor;
        private Rigidbody rigidbody;
        private bool originalKinematic;
        private HealthComponent health;
        private bool originalGodMode;

        private void Awake()
        {
            CharacterBody body = GetComponent<CharacterBody>();
            model = body.modelLocator && body.modelLocator.modelTransform
                ? body.modelLocator.modelTransform.GetComponent<CharacterModel>()
                : null;
            hurtBoxes = body.hurtBoxGroup;
            if (model)
                model.invisibilityCount++;
            if (hurtBoxes)
                hurtBoxes.hurtBoxesDeactivatorCounter++;

            if (NetworkServer.active)
            {
                health = body.healthComponent;
                if (health)
                {
                    originalGodMode = health.godMode;
                    health.godMode = true;
                }

                motor = body.characterMotor;
                if (motor)
                {
                    motor.velocity = Vector3.zero;
                    if (motor.enabled)
                        motor.enabled = false;
                    else
                        motor = null;
                }

                rigidbody = body.GetComponent<Rigidbody>();
                if (rigidbody)
                {
                    originalKinematic = rigidbody.isKinematic;
                    if (!originalKinematic)
                        rigidbody.velocity = Vector3.zero;
                    rigidbody.isKinematic = true;
                }
            }
        }

        internal void Restore()
        {
            if (model)
                model.invisibilityCount--;
            if (hurtBoxes)
                hurtBoxes.hurtBoxesDeactivatorCounter--;
            if (motor)
                motor.enabled = true;
            if (health)
                health.godMode = originalGodMode;
            if (rigidbody)
                rigidbody.isKinematic = originalKinematic;

            health = null;
            model = null;
            hurtBoxes = null;
            motor = null;
            rigidbody = null;
        }

        private void OnDestroy() => Restore();
    }
}
