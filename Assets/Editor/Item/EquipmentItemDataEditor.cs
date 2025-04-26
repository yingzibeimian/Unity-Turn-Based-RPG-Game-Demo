using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 装备信息编辑器面板
/// </summary>
public class EquipmentItemDataEditor : OdinMenuEditorWindow
{
    CreateNewEquipmentItemData createNewEquipmentItemData; //创建面板数据

    /// <summary>
    /// 创建基于Odin的可扩展编辑器窗口, 定义菜单路径
    /// </summary>
    [MenuItem("Tools/DataManger/Item Data/EquipmentItem Data")]
    private static void OpenWindow()
    {
        GetWindow<EquipmentItemDataEditor>().Show(); //创建并显示编辑器窗口
    }

    /// <summary>
    /// 防止实例悬空，若未被保存则在窗口被关闭时删除实例
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewEquipmentItemData != null)
        {
            DestroyImmediate(createNewEquipmentItemData.equipmentItemData); //清理未保存的临时数据
        }
    }

    /// <summary>
    /// 删除数据功能实现
    /// </summary>
    protected override void OnBeginDrawEditors()
    {
        OdinMenuTreeSelection selected = this.MenuTree.Selection;

        SirenixEditorGUI.BeginHorizontalToolbar();
        {
            GUILayout.FlexibleSpace();

            if (SirenixEditorGUI.ToolbarButton("Delete Data"))
            {
                EquipmentItemData asset = selected.SelectedValue as EquipmentItemData;
                string path = AssetDatabase.GetAssetPath(asset);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }

    /// <summary>
    /// 菜单树构建
    /// </summary>
    protected override OdinMenuTree BuildMenuTree()
    {
        OdinMenuTree tree = new OdinMenuTree();

        createNewEquipmentItemData = new CreateNewEquipmentItemData();
        tree.Add("Create New Data", createNewEquipmentItemData);
        tree.AddAllAssetsAtPath("EquipmentItem Data", "Assets/Data/ScriptableObjects/Item/ItemEquipment/Instances", typeof(EquipmentItemData)); //AddAllAssetsAtPath扫描指定路径下的指定类型资源

        return tree;
    }
}

/// <summary>
/// 数据创建面板
/// </summary>
public class CreateNewEquipmentItemData
{
    //InlineEditor特性: 内联编辑器，可直接在面板编辑数据
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
    public EquipmentItemData equipmentItemData;

    public CreateNewEquipmentItemData()
    {
        equipmentItemData = ScriptableObject.CreateInstance<EquipmentItemData>();
        equipmentItemData.itemName = "New EquipmentItem Data";
    }

    [Button("Save Data")]
    private void SavaUnitData()
    {
        AssetDatabase.CreateAsset(equipmentItemData, "Assets/Data/ScriptableObjects/Item/ItemEquipment/Instances/" + equipmentItemData.itemName + ".asset");
        AssetDatabase.SaveAssets();
    }
}
