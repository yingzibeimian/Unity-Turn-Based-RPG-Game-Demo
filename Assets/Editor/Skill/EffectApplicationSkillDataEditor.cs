using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 效果施加技能编辑器面板
/// </summary>
public class EffectApplicationSkillDataEditor : OdinMenuEditorWindow
{
    CreateNewEffectApplicationSkillData createNewEffectApplicationSkillData; //创建面板数据

    /// <summary>
    /// 创建基于Odin的可扩展编辑器窗口, 定义菜单路径
    /// </summary>
    [MenuItem("Tools/DataManger/Skill Data/EffectApplicationSkill Data")]
    private static void OpenWindow()
    {
        GetWindow<EffectApplicationSkillDataEditor>().Show(); //创建并显示编辑器窗口
    }

    /// <summary>
    /// 防止实例悬空，若未被保存则在窗口被关闭时删除实例
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewEffectApplicationSkillData != null)
        {
            DestroyImmediate(createNewEffectApplicationSkillData.effectApplicationSkillData); //清理未保存的临时数据
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
                EffectApplicationSkillData asset = selected.SelectedValue as EffectApplicationSkillData;
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

        createNewEffectApplicationSkillData = new CreateNewEffectApplicationSkillData();
        tree.Add("Create New Data", createNewEffectApplicationSkillData);
        tree.AddAllAssetsAtPath("EffectApplicationSkill Data", "Assets/Data/ScriptableObjects/Skill/EffectApplicationSkill/Instances", typeof(EffectApplicationSkillData)); //AddAllAssetsAtPath扫描指定路径下的指定类型资源

        return tree;
    }
}

/// <summary>
/// 数据创建面板
/// </summary>
public class CreateNewEffectApplicationSkillData
{
    //InlineEditor特性: 内联编辑器，可直接在面板编辑数据
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
    public EffectApplicationSkillData effectApplicationSkillData;

    public CreateNewEffectApplicationSkillData()
    {
        effectApplicationSkillData = ScriptableObject.CreateInstance<EffectApplicationSkillData>();
        effectApplicationSkillData.skillName = "New EffectApplicationSkill Data";
    }

    [Button("Save Data")]
    private void SavaUnitData()
    {
        AssetDatabase.CreateAsset(effectApplicationSkillData, "Assets/Data/ScriptableObjects/Skill/EffectApplicationSkill/Instances/" + effectApplicationSkillData.skillName + ".asset");
        AssetDatabase.SaveAssets();
    }
}
