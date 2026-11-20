using DoormanMod.Modules;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace DoormanMod.Survivors.Doorman
{
    public static class DoormanAssets
    {
        internal static GameObject BellProjectile { get; private set; }
        internal static GameObject CartProjectile { get; private set; }
        internal static GameObject DoorPrefab { get; private set; }
        internal static GameObject BellExplosion { get; private set; }
        internal static BuffDef Banished { get; private set; }

        public static void Init(AssetBundle bundle)
        {
            BellProjectile = CreateProjectile<BellProjectile>("DoormanBell", bundle, "BellVisual",
                PrimitiveType.Sphere, Vector3.one * 1.2f, new Color(1f, 0.72f, 0.12f));
            CartProjectile = CreateProjectile<CartProjectile>("DoormanCart", bundle, "CartVisual",
                PrimitiveType.Cube, new Vector3(2f, 1f, 3f), new Color(0.65f, 0.17f, 0.10f));

            GameObject door = new GameObject("DoormanDoorTemplate");
            door.SetActive(false);
            door.AddComponent<NetworkIdentity>();
            GameObject doorVisual = CreateVisual(bundle, "DoorVisual", PrimitiveType.Cube,
                new Vector3(1.6f, 2.7f, 0.16f), new Color(0.62f, 0.12f, 0.36f));
            doorVisual.transform.SetParent(door.transform, false);
            doorVisual.SetActive(true);
            DoorPrefab = PrefabAPI.InstantiateClone(door, "DoormanPortalDoor", true);
            DoorPrefab.SetActive(true);
            Object.Destroy(door);

            BellExplosion = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/ExplosionCommandoGrenade");
            Banished = Content.CreateAndAddBuff("DoormanBanished", null, Color.cyan, false, true);
            On.RoR2.CharacterBody.FixedUpdate += UpdateBanishment;
        }

        private static void UpdateBanishment(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody body)
        {
            orig(body);
            Banishment visual = body.GetComponent<Banishment>();
            if (body.HasBuff(Banished))
            {
                if (!visual)
                    body.gameObject.AddComponent<Banishment>();
            }
            else if (visual)
            {
                visual.Restore();
                Object.Destroy(visual);
            }
        }

        private static GameObject CreateProjectile<T>(string name, AssetBundle bundle, string visualName,
            PrimitiveType shape, Vector3 size, Color color) where T : MonoBehaviour
        {
            GameObject prototype = new GameObject(name + "Template");
            prototype.SetActive(false);
            prototype.AddComponent<NetworkIdentity>();
            prototype.AddComponent<TeamFilter>();
            Rigidbody rigidbody = prototype.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            ProjectileController controller = prototype.AddComponent<ProjectileController>();
            controller.allowPrediction = false;
            controller.cannotBeDeleted = typeof(T) == typeof(BellProjectile);
            controller.rigidbody = rigidbody;
            controller.procCoefficient = 1f;
            prototype.AddComponent<ProjectileDamage>();
            ProjectileNetworkTransform networkTransform = prototype.AddComponent<ProjectileNetworkTransform>();
            networkTransform.transform = prototype.transform;
            networkTransform.rb = rigidbody;
            networkTransform.projectileController = controller;
            networkTransform.positionTransmitInterval = 0.05f;
            networkTransform.interpolationFactor = 1f;
            prototype.AddComponent<T>();

            // A networked projectile carries its visual as a child. It needs no separate ghost.
            GameObject visual = CreateVisual(bundle, visualName, shape, size, color);
            visual.transform.SetParent(prototype.transform, false);
            visual.SetActive(true);
            GameObject prefab = PrefabAPI.InstantiateClone(prototype, name, true);
            prefab.SetActive(true);
            Object.Destroy(prototype);
            Content.AddProjectilePrefab(prefab);
            return prefab;
        }

        private static GameObject CreateVisual(AssetBundle bundle, string name, PrimitiveType shape,
            Vector3 size, Color color)
        {
            GameObject asset = bundle ? bundle.LoadAsset<GameObject>(name) : null;
            if (asset)
                return Object.Instantiate(asset);

            GameObject visual = GameObject.CreatePrimitive(shape);
            visual.name = name;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.localScale = size;
            Renderer renderer = visual.GetComponent<Renderer>();
            // Use the default material and a per-renderer tint for the editor-free prototype.
            MaterialPropertyBlock tint = new MaterialPropertyBlock();
            tint.SetColor("_Color", color);
            renderer.SetPropertyBlock(tint);
            return visual;
        }
    }
}
