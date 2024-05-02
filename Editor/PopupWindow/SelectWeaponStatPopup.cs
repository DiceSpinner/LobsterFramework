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
            selectionDrawer = new(collection, AddWeaponStatMenuAttribute.menus, DrawMenu, DrawItem);
            selectionDrawer.SetColorOptions(AbilityEditorConfig.MenuPopupColor, AbilityEditorConfig.ComponentPopupColor);
        }

        private GUIContent content = new();
        private void DrawMenu(MenuTree<Type> menu) {
            GUILayout.Label(menu.path, EditorStyles.boldLabel);
        }

        private void DrawItem(Type weaponStatType) {
            content.text = weaponStatType.Name;
            if (AddWeaponStatMenuAttribute.icons.TryGetValue(weaponStatType, out Texture2D icon))
            {
                content.image = icon;
            }
            else {
                content.image = icon;
            }
            if (GUILayout.Button(content, GUILayout.Height(30), GUILayout.Width(180)))
            {
                editor.selectedWeaponStat = data.weaponStats[weaponStatType.AssemblyQualifiedName];
                editorWindow.Close();
            }
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
