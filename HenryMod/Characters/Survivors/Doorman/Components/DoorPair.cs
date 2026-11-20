using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DoormanMod.Survivors.Doorman
{
    internal sealed class DoorPair : MonoBehaviour
    {
        internal const float Lifetime = 8f;
        private const float CrossingGrace = 0.35f;
        private const float DoorHalfHeight = 1.35f;

        private CharacterBody owner;
        private GameObject entranceDoor;
        private GameObject exitDoor;
        private Vector3 entrance;
        private Vector3 exit;
        private Vector3 forward;
        private float expiresAt;
        private float canCrossAt;
        private bool entranceArmed;
        private bool exitArmed;

        internal void OpenEntrance(CharacterBody body, Vector3 start, Vector3 heading)
        {
            owner = body;
            entrance = start;
            forward = heading;
            expiresAt = Time.fixedTime + Lifetime;

            Quaternion rotation = Quaternion.LookRotation(forward);
            entranceDoor = SpawnDoor(entrance, rotation);
        }

        internal void OpenExit(Vector3 footPosition)
        {
            exit = footPosition + Vector3.up * DoorHalfHeight;
            exitDoor = SpawnDoor(exit, Quaternion.LookRotation(forward));
            expiresAt = Time.fixedTime + Lifetime;
            canCrossAt = Time.fixedTime + CrossingGrace;
        }

        private static GameObject SpawnDoor(Vector3 position, Quaternion rotation)
        {
            GameObject door = Instantiate(DoormanAssets.DoorPrefab, position, rotation);
            NetworkServer.Spawn(door);
            return door;
        }

        private void OnDestroy()
        {
            if (!NetworkServer.active)
                return;

            if (entranceDoor)
                NetworkServer.Destroy(entranceDoor);
            if (exitDoor)
                NetworkServer.Destroy(exitDoor);
        }

        private void FixedUpdate()
        {
            if (!owner || !owner.healthComponent || !owner.healthComponent.alive || Time.fixedTime >= expiresAt)
            {
                Destroy(this);
                return;
            }

            if (!entranceDoor || !exitDoor || Time.fixedTime < canCrossAt)
                return;

            Vector3 position = owner.footPosition + Vector3.up * DoorHalfHeight;
            bool atEntrance = IsInside(position - entrance);
            bool atExit = IsInside(position - exit);

            entranceArmed |= !atEntrance;
            exitArmed |= !atExit;
            if (atEntrance && entranceArmed)
                Cross(exit);
            else if (atExit && exitArmed)
                Cross(entrance);
        }

        private bool IsInside(Vector3 offset)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return Mathf.Abs(offset.y) <= DoorHalfHeight
                && Mathf.Abs(Vector3.Dot(offset, right)) <= 0.8f
                && Mathf.Abs(Vector3.Dot(offset, forward)) <= 0.75f;
        }

        private void Cross(Vector3 destination)
        {
            Vector3 footPosition = destination - Vector3.up * DoorHalfHeight;
            TeleportHelper.TeleportBody(owner, footPosition, false);
            entranceArmed = false;
            exitArmed = false;
            canCrossAt = Time.fixedTime + CrossingGrace;
        }
    }
}
