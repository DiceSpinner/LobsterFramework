using System;
using UnityEngine;
using UnityEditor;
using LobsterFramework.AbilitySystem.WeaponSystem;
using LobsterFramework.Utility;
using System.Collections;
using System.Linq;
using Unity.Plastic.Antlr3.Runtime.Tree;

namespace LobsterFramework.Editors
{
    public class SelectWeaponStatPopup : PopupWindowContent
    {
        private WeaponDataEditor editor;
        private WeaponData data;
        private MenuTreeItemCollectionDrawer<Type> selectionDrawer;

        public SelectWeaponStatPopup(WeaponDataEditor editor, WeaponData data)
        {
            this.editor = editor;
            this.data = data;
            var collection = data.weaponStats.Values.Select((WeaponStat stat) => { return stat.GetType(); }).ToArray();
            selectionDrawer = new(collection, AddWeaponStatMenuAttribute.menus, DrawMenu, DrawItem, SelectWeaponStat);
            selectionDrawer.SetColorOptions(AbilityEditorConfig.MenuPopupColor, AbilityEditorConfig.ComponentPopupColor);
        }

        private GUIContent treeContent = new();
        private GUIContent DrawMenu(MenuTree<Type> menu) {
            treeContent.text = menu.path;
            return treeContent;
        }

        private GUIContent content = new();
        private GUIContent DrawItem(Type weaponStatType) {
            content.text = weaponStatType.Name;
            if (AddWeaponStatMenuAttribute.icons.TryGetValue(weaponStatType, out Texture2D icon))
            {
                content.image = icon;
            }
            else {
                content.image = icon;
            }
            content.tooltip = weaponStatType.FullName;
            return content;
        }

        private void SelectWeaponStat(Type weaponStatType)
        {
            editor.selectedWeaponStat = data.weaponStats[weaponStatType.AssemblyQualifiedName];
            editorWindow.Close();
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Select Weapon Stat", EditorUtils.CentredTitleLabelStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (data == null || editor == null)
            {
                EditorGUILayout.LabelField("Cannot Find WeaponData or Editor!");
                return;
            }
            selectionDrawer.Draw();
        }
    }
}
