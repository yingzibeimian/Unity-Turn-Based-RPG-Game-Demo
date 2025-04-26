using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 角色信息编辑器面板, OdinMenuEditorWindow是Odin提供的带左侧菜单树的窗口基类
/// </summary>
public class CharacterDataEditor : OdinMenuEditorWindow
{
    CreateNewCharacterData createNewCharacterData; //创建面板数据

    /// <summary>
    /// 创建基于Odin的可扩展编辑器窗口, 定义菜单路径
    /// </summary>
    [MenuItem("Tools/DataManger/Character Data")]
    private static void OpenWindow()
    {
        GetWindow<CharacterDataEditor>().Show(); //创建并显示编辑器窗口
    }

    /// <summary>
    /// 防止实例悬空，若未被保存则在窗口被关闭时删除实例
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewCharacterData != null)
        {
            DestroyImmediate(createNewCharacterData.characterData); //清理未保存的临时数据
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
                CharacterData asset = selected.SelectedValue as CharacterData;
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

        createNewCharacterData = new CreateNewCharacterData();
        tree.Add("Create New Data", createNewCharacterData);
        tree.AddAllAssetsAtPath("Character Data", "Assets/Data/ScriptableObjects/Character/Instances", typeof(CharacterData)); //AddAllAssetsAtPath扫描指定路径下的指定类型资源

        return tree;
    }
}

/// <summary>
/// 数据创建面板
/// </summary>
public class CreateNewCharacterData
{
    //InlineEditor特性: 内联编辑器，可直接在面板编辑数据
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
    public CharacterData characterData;

    public CreateNewCharacterData()
    {
        characterData = ScriptableObject.CreateInstance<CharacterData>();
        characterData.characterName = "New Character Data";
    }

    [Button("Save Data")]
    private void SavaUnitData()
    {
        AssetDatabase.CreateAsset(characterData, "Assets/Data/ScriptableObjects/Character/Instances/" + characterData.characterName + ".asset");
        AssetDatabase.SaveAssets();
    }
}