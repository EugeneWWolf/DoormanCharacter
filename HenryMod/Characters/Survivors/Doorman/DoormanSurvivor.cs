using BepInEx.Configuration;
using DoormanMod.Modules;
using DoormanMod.Modules.Characters;
using DoormanMod.Survivors.Doorman.Components;
using DoormanMod.Survivors.Doorman.SkillStates;
using ExtraSkillSlots;
using R2API;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
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

            characterPortrait = AssetBundle.LoadAsset<Texture>("texDoormanIcon"),
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

        public override UnlockableDef CharacterUnlockableDef => DoormanUnlockables.characterUnlockableDef;

        [Obsolete("will be deprecated with Hallowed Concepts update. move to using IDRSJson from KingEnderBrine")]
        public override ItemDisplaysBase ItemDisplays => null;// new HenryItemDisplays();

        //set in base classes
        public override AssetBundle AssetBundle { get; protected set; }

        public override GameObject BodyPrefab { get; protected set; }
        public override CharacterBody PrefabCharacterBody { get; protected set; }
        public override GameObject CharacterModelObject { get; protected set; }
        public override CharacterModel PrefabCharacterModel { get; protected set; }
        public override GameObject DisplayPrefab { get; protected set; }

        public override void Initialize()
        {
            //uncomment if you have multiple characters
            //ConfigEntry<bool> characterEnabled = Config.CharacterEnableConfig("Survivors", "Henry");

            //if (!characterEnabled.Value)
            //    return;

            base.Initialize();
        }

        public override void InitializeCharacter()
        {
            //need the character unlockable before you initialize the survivordef
            DoormanUnlockables.Init();

            base.InitializeCharacter();

            DoormanConfig.Init();
            DoormanStates.Init();
            DoormanTokens.Init();

            DoormanAssets.Init(AssetBundle);
            DoormanBuffs.Init(AssetBundle);

            InitializeEntityStateMachines();
            InitializeSkills();
            // InitializeSkins();
            InitializeCharacterMaster();

            AdditionalBodySetup();

            AddHooks();
        }

        private void AdditionalBodySetup()
        {
            AddHitboxes();
            BodyPrefab.AddComponent<DoormanWeaponComponent>();
            //bodyPrefab.AddComponent<HuntressTrackerComopnent>();
            //anything else here
        }

        public void AddHitboxes()
        {
            //example of how to create a HitBoxGroup. see summary for more details
            Prefabs.SetupHitBoxGroup(CharacterModelObject, "SwordGroup", "SwordHitbox");
        }

        public override void InitializeEntityStateMachines() 
        {
            //clear existing state machines from your cloned body (probably commando)
            //omit all this if you want to just keep theirs
            Prefabs.ClearEntityStateMachines(BodyPrefab);

            //the main "Body" state machine has some special properties
            Prefabs.AddMainEntityStateMachine(BodyPrefab, "Body", typeof(EntityStates.GenericCharacterMain), typeof(EntityStates.SpawnTeleporterState));
            //if you set up a custom main characterstate, set it up here
                //don't forget to register custom entitystates in your HenryStates.cs

            Prefabs.AddEntityStateMachine(BodyPrefab, "Weapon");
            Prefabs.AddEntityStateMachine(BodyPrefab, "Weapon2");
        }

        #region skills
        public override void InitializeSkills()
        {
            //remove the genericskills from the commando body we cloned
            Skills.ClearGenericSkills(BodyPrefab);
            //add our own
            //AddPassiveSkill();
            AddPrimarySkills();
            AddSecondarySkills();
            AddUtiitySkills();
            AddSpecialSkills();
        }


        //if this is your first look at skilldef creation, take a look at Secondary first
        private void AddPrimarySkills()
        {
            // Создаём SkillDef
            SkillDef shootSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "DoormanShoot",
                skillNameToken = DOORMAN_PREFIX + "PRIMARY_SHOOT_NAME",
                skillDescriptionToken = DOORMAN_PREFIX + "PRIMARY_SHOOT_DESCRIPTION",
                skillIcon = null, // Иконку добавишь позже

                // Привязываем наш новый EntityState
                activationState = new EntityStates.SerializableEntityStateType(typeof(Shoot)),
                activationStateMachineName = "Weapon", // Стрельба происходит в состоянии "Weapon"

                interruptPriority = EntityStates.InterruptPriority.Skill,

                // --- Настройки для стрельбы ---
                baseRechargeInterval = 0f, // Кулдаун не нужен (стреляем постоянно)
                baseMaxStock = 1,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 0, // Не тратим "заряды"

                mustKeyPress = false, // АВТОМАТИЧЕСКАЯ СТРЕЛЬБА ПРИ ЗАЖАТИИ (как у Commando)
                beginSkillCooldownOnSkillEnd = false,
                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = true, // При стрельбе персонаж перестаёт бежать
                forceSprintDuringState = false,
            });

            // Регистрируем SkillDef в игре
            ContentAddition.AddSkillDef(shootSkillDef);

            // Получаем текущее семейство Primary-способностей нашего персонажа
            GenericSkill primarySkill = BodyPrefab.GetComponent<SkillLocator>().primary;
            SkillFamily primaryFamily = primarySkill.skillFamily;

            // Добавляем нашу стрельбу как вариант в Primary
            Array.Resize(ref primaryFamily.variants, primaryFamily.variants.Length + 1);
            primaryFamily.variants[primaryFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = shootSkillDef,
                unlockableName = "", // Способность доступна сразу
                viewableNode = new ViewablesCatalog.Node(shootSkillDef.skillNameToken, false, null)
            };
        }

        private void AddSecondarySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(BodyPrefab, SkillSlot.Secondary);

            SkillDef bellSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "DoormanThrowBell",
                skillNameToken = DOORMAN_PREFIX + "SECONDARY_BELL_NAME",
                skillDescriptionToken = DOORMAN_PREFIX + "SECONDARY_BELL_DESCRIPTION",
                skillIcon = AssetBundle.LoadAsset<Sprite>("texBellIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(ThrowBell)),
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.Skill,
                baseRechargeInterval = 4f,
                baseMaxStock = 1,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,
                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = true,
                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });

            GenericSkill secondarySkill = BodyPrefab.GetComponent<SkillLocator>().secondary;
            Skills.AddSkillsToFamily(secondarySkill.skillFamily, bellSkillDef);
        }

        private void AddUtiitySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(BodyPrefab, SkillSlot.Utility);

            SkillDef doorSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "DoormanDoorDash",
                skillNameToken = DOORMAN_PREFIX + "UTILITY_DOOR_NAME",
                skillDescriptionToken = DOORMAN_PREFIX + "UTILITY_DOOR_DESCRIPTION",
                skillIcon = null,
                activationState = new EntityStates.SerializableEntityStateType(typeof(DoorDash)),
                activationStateMachineName = "Body", // Рывок/телепорт — это движение тела
                interruptPriority = EntityStates.InterruptPriority.Skill,
                baseRechargeInterval = 6f,
                baseMaxStock = 1,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,
                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = true,
                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false, // Рывок обычно не отменяет спринт
                forceSprintDuringState = false,
            });

            GenericSkill utilitySkill = BodyPrefab.GetComponent<SkillLocator>().utility;
            Skills.AddSkillsToFamily(utilitySkill.skillFamily, doorSkillDef);
        }

        private void AddSpecialSkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(BodyPrefab, SkillSlot.Special);

            SkillDef cartSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "DoormanThrowCart",
                skillNameToken = DOORMAN_PREFIX + "SPECIAL_CART_NAME",
                skillDescriptionToken = DOORMAN_PREFIX + "SPECIAL_CART_DESCRIPTION",
                skillIcon = null,
                activationState = new EntityStates.SerializableEntityStateType(typeof(ThrowCart)),
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.Skill,
                baseRechargeInterval = 12f,
                baseMaxStock = 1,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,
                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = true,
                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = true,
                forceSprintDuringState = false,
            });

            GenericSkill specialSkill = BodyPrefab.GetComponent<SkillLocator>().special;
            Skills.AddSkillsToFamily(specialSkill.skillFamily, cartSkillDef);
        }

        private void SetupExtraSkills()
        {
            // 1. Создаем SkillFamily для дополнительного слота
            var extraFirstFamily = ScriptableObject.CreateInstance<SkillFamily>();
            // Обязательно задаем имя, игра использует его для сохранения лоадаутов
            (extraFirstFamily as ScriptableObject).name = "DoormanBodyExtraFirstFamily";

            // 2. Создаем SkillDef для способности, которая будет в этом слоте
            // (например, для Ульты - Баронесса)
            SkillDef ultimateSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "DoormanUltimate",
                skillNameToken = DOORMAN_PREFIX + "ULTIMATE_NAME",
                skillDescriptionToken = DOORMAN_PREFIX + "ULTIMATE_DESCRIPTION",
                skillIcon = null,
                activationState = new EntityStates.SerializableEntityStateType(typeof(BaronessState)),
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.Skill,
                baseRechargeInterval = 15f, // Например, 15 секунд
                baseMaxStock = 1,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,
                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = true,
                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = true,
                forceSprintDuringState = false,
            });

            ContentAddition.AddSkillDef(ultimateSkillDef);

            // 2. Регистрируем SkillFamily (заменяем LoadoutAPI на ContentAddition)
            extraFirstFamily.variants = new SkillFamily.Variant[]
            {
                new SkillFamily.Variant { skillDef = ultimateSkillDef }
            };
            ContentAddition.AddSkillFamily(extraFirstFamily);

            // 3. Создаем компонент GenericSkill для слота и привязываем к нему Family
            var extraFirstSkill = BodyPrefab.AddComponent<GenericSkill>();
            extraFirstSkill.SetFieldValue("_skillFamily", extraFirstFamily);

            // 4. Добавляем ExtraSkillLocator и назначаем слот
            var extraSkillLocator = BodyPrefab.AddComponent<ExtraSkillLocator>();
            extraSkillLocator.extraFirst = extraFirstSkill;
        }
        #endregion skills

        #region skins
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Ненужное присваивание значения", Justification = "Needed for skins later")]
        public override void InitializeSkins()
        {
            ModelSkinController skinController = PrefabCharacterModel.gameObject.AddComponent<ModelSkinController>();
            ChildLocator childLocator = PrefabCharacterModel.GetComponent<ChildLocator>();

            CharacterModel.RendererInfo[] defaultRendererinfos = PrefabCharacterModel.baseRendererInfos;

            List<SkinDef> skins = new List<SkinDef>();

            #region DefaultSkin
            //this creates a SkinDef with all default fields
            SkinDef defaultSkin = Skins.CreateSkinDef("DEFAULT_SKIN",
                AssetBundle.LoadAsset<Sprite>("texMainSkin"),
                defaultRendererinfos,
                PrefabCharacterModel.gameObject);

            //these are your Mesh Replacements. The order here is based on your CustomRendererInfos from earlier
                //pass in meshes as they are named in your assetbundle
            //currently not needed as with only 1 skin they will simply take the default meshes
                //uncomment this when you have another skin
            //defaultSkin.meshReplacements = Modules.Skins.getMeshReplacements(assetBundle, defaultRendererinfos,
            //    "meshHenrySword",
            //    "meshHenryGun",
            //    "meshHenry");

            //add new skindef to our list of skindefs. this is what we'll be passing to the SkinController
            skins.Add(defaultSkin);
            #endregion

            //uncomment this when you have a mastery skin
            #region MasterySkin
            
            ////creating a new skindef as we did before
            //SkinDef masterySkin = Modules.Skins.CreateSkinDef(HENRY_PREFIX + "MASTERY_SKIN_NAME",
            //    assetBundle.LoadAsset<Sprite>("texMasteryAchievement"),
            //    defaultRendererinfos,
            //    prefabCharacterModel.gameObject,
            //    HenryUnlockables.masterySkinUnlockableDef);

            ////adding the mesh replacements as above. 
            ////if you don't want to replace the mesh (for example, you only want to replace the material), pass in null so the order is preserved
            //masterySkin.meshReplacements = Modules.Skins.getMeshReplacements(assetBundle, defaultRendererinfos,
            //    "meshHenrySwordAlt",
            //    null,//no gun mesh replacement. use same gun mesh
            //    "meshHenryAlt");

            ////masterySkin has a new set of RendererInfos (based on default rendererinfos)
            ////you can simply access the RendererInfos' materials and set them to the new materials for your skin.
            //masterySkin.rendererInfos[0].defaultMaterial = assetBundle.LoadMaterial("matHenryAlt");
            //masterySkin.rendererInfos[1].defaultMaterial = assetBundle.LoadMaterial("matHenryAlt");
            //masterySkin.rendererInfos[2].defaultMaterial = assetBundle.LoadMaterial("matHenryAlt");

            ////here's a barebones example of using gameobjectactivations that could probably be streamlined or rewritten entirely, truthfully, but it works
            //masterySkin.gameObjectActivations = new SkinDef.GameObjectActivation[]
            //{
            //    new SkinDef.GameObjectActivation
            //    {
            //        gameObject = childLocator.FindChildGameObject("GunModel"),
            //        shouldActivate = false,
            //    }
            //};
            ////simply find an object on your child locator you want to activate/deactivate and set if you want to activate/deacitvate it with this skin

            //skins.Add(masterySkin);
            
            #endregion

            skinController.skins = skins.ToArray();
        }
        #endregion skins

        //Character Master is what governs the AI of your character when it is not controlled by a player (artifact of vengeance, goobo)
        public override void InitializeCharacterMaster()
        {
            //you must only do one of these. adding duplicate masters breaks the game.

            //if you're lazy or prototyping you can simply copy the AI of a different character to be used
            //Modules.Prefabs.CloneDopplegangerMaster(bodyPrefab, masterName, "Merc");

            //how to set up AI in code
            DoormanAI.Init(BodyPrefab, MasterName);

            //how to load a master set up in unity, can be an empty gameobject with just AISkillDriver components
            //assetBundle.LoadMaster(bodyPrefab, masterName);
        }

        private void AddHooks()
        {
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {

            if (sender.HasBuff(DoormanBuffs.armorBuff))
            {
                args.armorAdd += 300;
            }
        }
    }
}