using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using System.Collections.Generic;
using RoR2.Skills;

namespace DoormanMod.Modules.Characters
{
    public abstract class SurvivorBase<T> : CharacterBase<T> where T : SurvivorBase<T>, new()
    {
        public abstract string MasterName { get; }

        public abstract string DisplayPrefabName { get; }

        public abstract string SurvivorTokenPrefix { get; }

        public abstract UnlockableDef CharacterUnlockableDef { get; }

        public abstract GameObject DisplayPrefab { get; protected set; }

        public override void InitializeCharacter()
        {
            base.InitializeCharacter();

            InitializeDisplayPrefab();

            InitializeSurvivor();
        }

        protected virtual void InitializeDisplayPrefab()
        {
            DisplayPrefab = Prefabs.CreateDisplayPrefab(AssetBundle, DisplayPrefabName, BodyPrefab);
        }

        protected virtual void InitializeSurvivor()
        {
            Content.CreateSurvivor(BodyPrefab, DisplayPrefab, BodyInfo.bodyColor, SurvivorTokenPrefix, CharacterUnlockableDef, BodyInfo.sortPosition);
        }

        #region CharacterSelectSurvivorPreviewDisplayController
        protected virtual void AddCssPreviewSkill(int indexFromEditor, SkillFamily skillFamily, SkillDef skillDef)
        {
            CharacterSelectSurvivorPreviewDisplayController CSSPreviewDisplayConroller = DisplayPrefab.GetComponent<CharacterSelectSurvivorPreviewDisplayController>();
            if (!CSSPreviewDisplayConroller)
            {
                Log.Error("trying to add skillChangeResponse to null CharacterSelectSurvivorPreviewDisplayController.\nMake sure you created one on your Display prefab in editor");
                return;
            }

            CSSPreviewDisplayConroller.skillChangeResponses[indexFromEditor].triggerSkillFamily = skillFamily;
            CSSPreviewDisplayConroller.skillChangeResponses[indexFromEditor].triggerSkill = skillDef;
        }

        protected virtual void FinalizeCSSPreviewDisplayController()
        {
            if (!DisplayPrefab)
                return;

            CharacterSelectSurvivorPreviewDisplayController CSSPreviewDisplayConroller = DisplayPrefab.GetComponent<CharacterSelectSurvivorPreviewDisplayController>();
            if (!CSSPreviewDisplayConroller)
                return;

            //set body prefab
            CSSPreviewDisplayConroller.bodyPrefab = BodyPrefab;

            //clear list of null entries
            List<CharacterSelectSurvivorPreviewDisplayController.SkillChangeResponse> newlist = new List<CharacterSelectSurvivorPreviewDisplayController.SkillChangeResponse>();

            for (int i = 0; i < CSSPreviewDisplayConroller.skillChangeResponses.Length; i++)
            {
                if (CSSPreviewDisplayConroller.skillChangeResponses[i].triggerSkillFamily != null)
                {
                    newlist.Add(CSSPreviewDisplayConroller.skillChangeResponses[i]);
                }
            }

            CSSPreviewDisplayConroller.skillChangeResponses = newlist.ToArray();
        }
        #endregion
    }
}
