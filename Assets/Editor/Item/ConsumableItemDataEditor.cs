using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 消耗品信息编辑器面板
/// </summary>
public class ConsumableItemDataEditor : OdinMenuEditorWindow
{
    CreateNewConsumableItemData createNewConsumableItemData; //创建面板数据

    /// <summary>
    /// 创建基于Odin的可扩展编辑器窗口, 定义菜单路径
    /// </summary>
    [MenuItem("Tools/DataManger/Item Data/ConsumableItem Data")]
    private static void OpenWindow()
    {
        GetWindow<ConsumableItemDataEditor>().Show(); //创建并显示编辑器窗口
    }

    /// <summary>
    /// 防止实例悬空，若未被保存则在窗口被关闭时删除实例
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewConsumableItemData != null)
        {
            DestroyImmediate(createNewConsumableItemData.consumableItemData); //清理未保存的临时数据
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
                ConsumableItemData asset = selected.SelectedValue as ConsumableItemData;
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

        createNewConsumableItemData = new CreateNewConsumableItemData();
        tree.Add("Create New Data", createNewConsumableItemData);
        tree.AddAllAssetsAtPath("ConsumableItem Data", "Assets/Data/ScriptableObjects/Item/ItemConsumable/Instances", typeof(ConsumableItemData)); //AddAllAssetsAtPath扫描指定路径下的指定类型资源

        return tree;
    }
}

/// <summary>
/// 数据创建面板
/// </summary>
public class CreateNewConsumableItemData
{
    //InlineEditor特性: 内联编辑器，可直接在面板编辑数据
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
    public ConsumableItemData consumableItemData;

    public CreateNewConsumableItemData()
    {
        consumableItemData = ScriptableObject.CreateInstance<ConsumableItemData>();
        consumableItemData.itemName = "New ConsumableItem Data";
    }

    [Button("Save Data")]
    private void SavaUnitData()
    {
        AssetDatabase.CreateAsset(consumableItemData, "Assets/Data/ScriptableObjects/Item/ItemConsumable/Instances/" + consumableItemData.itemName + ".asset");
        AssetDatabase.SaveAssets();
    }
}
