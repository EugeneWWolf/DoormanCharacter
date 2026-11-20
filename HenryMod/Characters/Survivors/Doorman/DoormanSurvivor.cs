using DoormanMod.Modules;
using DoormanMod.Modules.Characters;
using DoormanMod.Survivors.Doorman.SkillStates;
using ExtraSkillSlots;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using UnityEngine;

namespace DoormanMod.Survivors.Doorman
{
    public class DoormanSurvivor : SurvivorBase<DoormanSurvivor>
    {
        //used to load the assetbundle for this character. must be unique
        public override string AssetBundleName => "doormanbundle"; //if you do not change this, you are giving permission to deprecate the mod

        //the name of the prefab we will create. conventionally ending in "Body". must be unique
        public override string BodyName => "DoormanBody"; //if you do not change this, you get the point by now

        //name of the ai master for vengeance and goobo. must be unique
        public override string MasterName => "DoormanMonsterMaster"; //if you do not

        //the names of the prefabs you set up in unity that we will use to build your character
        public override string ModelPrefabName => "mdlDoorman";
        public override string DisplayPrefabName => "DoormanDisplay";

        public const string DOORMAN_PREFIX = DoormanPlugin.DEVELOPER_PREFIX + "_DOORMAN_";

        //used when registering your survivor's language tokens
        public override string SurvivorTokenPrefix => DOORMAN_PREFIX;
        
        public override BodyInfo BodyInfo => new BodyInfo
        {
            bodyName = BodyName,
            bodyNameToken = DOORMAN_PREFIX + "NAME",
            subtitleNameToken = DOORMAN_PREFIX + "SUBTITLE",

            characterPortrait = AssetBundle ? AssetBundle.LoadAsset<Texture>("texDoormanIcon") : PrefabCharacterBody ? PrefabCharacterBody.portraitIcon : null,
            bodyColor = Color.white,
            sortPosition = 100,

            crosshair = Asset.LoadCrosshair("Standard"),
            podPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/SurvivorPod"),

            maxHealth = 110f,
            healthRegen = 1.5f,
            armor = 0f,

            jumpCount = 1,
        };

        public override CustomRendererInfo[] CustomRendererInfos => new CustomRendererInfo[]
        {
            new CustomRendererInfo { childName = "Model" }
        };

        public override UnlockableDef CharacterUnlockableDef => null;

        public override ItemDisplaysBase ItemDisplays => null;// new HenryItemDisplays();

        //set in base classes
        public override AssetBundle AssetBundle { get; protected set; }

        public override GameObject BodyPrefab { get; protected set; }
        public override CharacterBody PrefabCharacterBody { get; protected set; }
        public override GameObject CharacterModelObject { get; protected set; }
        public override CharacterModel PrefabCharacterModel { get; protected set; }
        public override GameObject DisplayPrefab { get; protected set; }

        protected override void InitializeCharacterBodyPrefab()
        {
            bool hasCustomModel = AssetBundle && AssetBundle.LoadAsset<GameObject>(ModelPrefabName);
            if (hasCustomModel)
            {
                base.InitializeCharacterBodyPrefab();
                return;
            }

            GameObject commando = LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody");
            BodyPrefab = PrefabAPI.InstantiateClone(commando, BodyName);
            PrefabCharacterBody = BodyPrefab.GetComponent<CharacterBody>();
            PrefabCharacterBody.baseNameToken = DOORMAN_PREFIX + "NAME";
            PrefabCharacterBody.subtitleNameToken = DOORMAN_PREFIX + "SUBTITLE";
            CharacterModelObject = BodyPrefab.GetComponent<ModelLocator>().modelTransform.gameObject;
            PrefabCharacterModel = CharacterModelObject.GetComponent<CharacterModel>();
            Content.AddCharacterBodyPrefab(BodyPrefab);
        }

        protected override void InitializeDisplayPrefab()
        {
            if (AssetBundle && AssetBundle.LoadAsset<GameObject>(DisplayPrefabName))
            {
                base.InitializeDisplayPrefab();
                return;
            }

            // Keep display renderers separate from the body and locate them on the clone.
            DisplayPrefab = PrefabAPI.InstantiateClone(CharacterModelObject, DisplayPrefabName, false);
            DisplayPrefab.transform.localPosition = Vector3.zero;
            DisplayPrefab.transform.localRotation = Quaternion.identity;
        }

        public override void InitializeCharacter()
        {
            base.InitializeCharacter();
            DoormanAssets.Init(AssetBundle);
            DoormanStates.Init();
            DoormanTokens.Init();
            InitializeEntityStateMachines();
            InitializeSkills();
            InitializeCharacterMaster();
        }

        public override void InitializeEntityStateMachines()
        {
            // Keep Commando's stock locomotion and firing state machines.
            Prefabs.AddEntityStateMachine(BodyPrefab, "DoormanUltimate");
        }

        public override void InitializeSkills()
        {
            GameObject commando = LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody");
            SkillLocator reference = commando.GetComponent<SkillLocator>();
            Skills.ClearGenericSkills(BodyPrefab);
            Skills.CreateSkillFamilies(BodyPrefab);
            SkillLocator slots = BodyPrefab.GetComponent<SkillLocator>();
            slots.passiveSkill = default;
            AddSkill<SkillDef>(slots.primary, typeof(Shoot), "SHOOT", "Weapon", 0f, reference.primary.skillFamily.variants[0].skillDef.icon);
            AddSkill<SkillDef>(slots.secondary, typeof(ThrowBell), "BELL", "Weapon", 4f, reference.secondary.skillFamily.variants[0].skillDef.icon);
            AddSkill<SkillDef>(slots.utility, typeof(DoorDash), "DOOR", "Body", 6f, reference.utility.skillFamily.variants[0].skillDef.icon, false);
            AddSkill<SkillDef>(slots.special, typeof(ThrowCart), "CART", "Weapon", 12f, reference.special.skillFamily.variants[0].skillDef.icon);

            ExtraSkillLocator extra = BodyPrefab.AddComponent<ExtraSkillLocator>();
            extra.extraFirst = Skills.CreateGenericSkillWithSkillFamily(BodyPrefab, "ExtraFirst");
            AddSkill<BaronessSkillDef>(extra.extraFirst, typeof(BaronessState), "ULTIMATE", "DoormanUltimate", 15f, reference.special.skillFamily.variants[0].skillDef.icon);
        }

        private void AddSkill<T>(GenericSkill slot, Type state, string id, string machine, float cooldown,
            Sprite defaultIcon, bool combat = true) where T : SkillDef
        {
            bool repeatable = cooldown == 0f;
            Sprite icon = AssetBundle ? AssetBundle.LoadAsset<Sprite>("tex" + id + "Icon") : null;
            SkillDef skill = Skills.CreateSkillDef<T>(new SkillDefInfo
            {
                skillName = "Doorman" + id,
                skillNameToken = DOORMAN_PREFIX + id + "_NAME",
                skillDescriptionToken = DOORMAN_PREFIX + id + "_DESCRIPTION",
                skillIcon = icon ? icon : defaultIcon,
                activationState = new EntityStates.SerializableEntityStateType(state),
                activationStateMachineName = machine,
                interruptPriority = EntityStates.InterruptPriority.Skill,
                baseRechargeInterval = cooldown,
                baseMaxStock = 1,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = repeatable ? 0 : 1,
                fullRestockOnAssign = true,
                mustKeyPress = !repeatable,
                beginSkillCooldownOnSkillEnd = !repeatable,
                isCombatSkill = combat,
                cancelSprintingOnActivation = combat
            });
            Skills.AddSkillToFamily(slot.skillFamily, skill);
        }

        public override void InitializeCharacterMaster()
        {
            Prefabs.CloneDopplegangerMaster(BodyPrefab, MasterName, "Commando");
        }
    }
}
