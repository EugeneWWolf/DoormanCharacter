using EntityStates;

namespace DoormanMod.Survivors.Doorman.SkillStates
{
    public class ThrowBell : GenericProjectileBaseState
    {
        public override void OnEnter()
        {
            projectilePrefab = DoormanAssets.BellProjectile;
            baseDuration = 0.35f;
            damageCoefficient = 8f;
            base.OnEnter();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }
}
