using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem.WeaponSystem;
using System;
using LobsterFramework.Utility;


namespace LobsterFramework.Editors
{
    [CustomEditor(typeof(CharacterWeaponAnimationData))]
    public class CharacterWeaponAnimationDataEditor : Editor
    {
        private WeaponType selectedWeaponType;

        public CharacterWeaponAnimationDataEditor() {
            selectedWeaponType = WeaponType.Sword;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            selectedWeaponType = (WeaponType)EditorGUILayout.EnumPopup("Weapon Type", selectedWeaponType);
            CharacterWeaponAnimationData data = (CharacterWeaponAnimationData)target;
            AbilityAnimationDictionary setting = data.abilityAnimations[(int)selectedWeaponType];

            var collection = WeaponArtAttribute.weaponArtsByWeaponType[(int)selectedWeaponType];

            foreach (Type ability in collection)
            {
                if (!setting.ContainsKey(ability.AssemblyQualifiedName))
                {
                    setting[ability.AssemblyQualifiedName] = null;
                }
                DisplayAbilityAnimationEntries(setting, ability);
                DisplayAnimationAddonEditor(data.animationAddons[(int)selectedWeaponType], ability);
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            int selected = (int)selectedWeaponType;

            EditorGUI.BeginChangeCheck();
            var item = (AnimationClip)EditorGUILayout.ObjectField("Move", data.movementAnimations[selected], typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RegisterCompleteObjectUndo(target, "Modify weapon moving animation entry");
                EditorUtility.SetDirty(target);
                data.movementAnimations[selected] = item;
            }
        }
         
        private void DisplayAbilityAnimationEntries(AbilityAnimationDictionary setting, Type abilityType) {
            AnimationClip[] clips = setting[abilityType.AssemblyQualifiedName];
            if (!WeaponAnimationAttribute.AbilityAnimationInfo.ContainsKey(abilityType))
            {
                EditorGUI.BeginChangeCheck();
                var item = (AnimationClip)EditorGUILayout.ObjectField(abilityType.Name, clips[0], typeof(AnimationClip), false);
                if (EditorGUI.EndChangeCheck()) {
                    EditorUtility.SetDirty(target);
                    Undo.RegisterCompleteObjectUndo(target, "Modify weapon ability animation entry");
                    clips[0] = item;
                }
            }
            else {
                EditorGUILayout.LabelField(abilityType.Name);
                EditorGUI.indentLevel++;
                var enums = EnumCache.GetNames(WeaponAnimationAttribute.AbilityAnimationInfo[abilityType]);
                for (int i = 0;i < enums.Count;i++) {
                    EditorGUI.BeginChangeCheck();
                    var item = (AnimationClip)EditorGUILayout.ObjectField(enums[i], clips[i], typeof(AnimationClip), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(target);
                        Undo.RegisterCompleteObjectUndo(target, "Modify weapon ability animation entry");
                        clips[i] = item;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private Dictionary<Type, Editor> editors = new();
        private void DisplayAnimationAddonEditor(WeaponAbilityAddOnDictionary setting, Type abilityType) {
            if (!setting.ContainsKey(abilityType.AssemblyQualifiedName)) {
                return;
            }
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (!editors.ContainsKey(abilityType)) {
                editors[abilityType] = CreateEditor(setting[abilityType.AssemblyQualifiedName]);
            }
            editors[abilityType].OnInspectorGUI();
            EditorGUI.indentLevel--;
        }

        private void OnDestroy()
        {
            foreach (Editor editor in editors.Values) {
                DestroyImmediate(editor);
            }
        }
    }
}
