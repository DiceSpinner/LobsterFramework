using System;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem.WeaponSystem;
using LobsterFramework.Utility;

namespace LobsterFramework.Editors
{
    public class AddWeaponStatPopup : PopupWindowContent
    {
        private WeaponData data;
        private MenuTreeDrawer<Type> menuTreeDrawer;

        public AddWeaponStatPopup(WeaponData data) {
            this.data = data;
            menuTreeDrawer = new(AddWeaponStatMenuAttribute.root, AddWeaponStat, DrawMenu, DrawItem);
            menuTreeDrawer.SetEmptyNote("Option Exhausted");
            menuTreeDrawer.SetColors(AbilityEditorConfig.MenuPopupColor, AbilityEditorConfig.ComponentPopupColor);
        }

        #region Handles
        private void AddWeaponStat(Type weaponStatType) {
            data.AddWeaponStat(weaponStatType);
        }

        private GUIContent content = new();
        private GUIContent DrawItem(Type weaponStatType) {
            if (data.weaponStats.ContainsKey(weaponStatType.AssemblyQualifiedName)) {
                return null;
            }
            content.text = weaponStatType.Name;
            if (AddWeaponStatMenuAttribute.icons.TryGetValue(weaponStatType, out Texture2D icon))
            {
                content.image = icon;
            }
            else
            {
                content.image = null;
            }
            return content;
        }

        private GUIContent DrawMenu(MenuTree<Type> menu) {
            content.text = menu.menuName;
            content.image = AbilityEditorConfig.GetFolderIcon(menu.path[(AddWeaponStatMenuAttribute.RootName.Length + 1)..]);
            return content;
        }
        #endregion

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Add Weapon Stats", EditorUtils.CentredTitleLabelStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (data == null)
            {
                EditorGUILayout.LabelField("Cannot Find WeaponData!");
                return;
            }
            menuTreeDrawer.Draw();
        }
    }
}
