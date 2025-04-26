using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 召唤技能编辑器面板
/// </summary>
public class SummonSkillDataEditor : OdinMenuEditorWindow
{
    CreateNewSummonSkillData createNewSummonSkillData; //创建面板数据

    /// <summary>
    /// 创建基于Odin的可扩展编辑器窗口, 定义菜单路径
    /// </summary>
    [MenuItem("Tools/DataManger/Skill Data/SummonSkill Data")]
    private static void OpenWindow()
    {
        GetWindow<SummonSkillDataEditor>().Show(); //创建并显示编辑器窗口
    }

    /// <summary>
    /// 防止实例悬空，若未被保存则在窗口被关闭时删除实例
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewSummonSkillData != null)
        {
            DestroyImmediate(createNewSummonSkillData.summonSkillData); //清理未保存的临时数据
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
                SummonSkillData asset = selected.SelectedValue as SummonSkillData;
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

        createNewSummonSkillData = new CreateNewSummonSkillData();
        tree.Add("Create New Data", createNewSummonSkillData);
        tree.AddAllAssetsAtPath("SummonSkill Data", "Assets/Data/ScriptableObjects/Skill/SummonSkill/Instances", typeof(SummonSkillData)); //AddAllAssetsAtPath扫描指定路径下的指定类型资源

        return tree;
    }
}

/// <summary>
/// 数据创建面板
/// </summary>
public class CreateNewSummonSkillData
{
    //InlineEditor特性: 内联编辑器，可直接在面板编辑数据
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
    public SummonSkillData summonSkillData;

    public CreateNewSummonSkillData()
    {
        summonSkillData = ScriptableObject.CreateInstance<SummonSkillData>();
        summonSkillData.skillName = "New SummonSkill Data";
    }

    [Button("Save Data")]
    private void SavaUnitData()
    {
        AssetDatabase.CreateAsset(summonSkillData, "Assets/Data/ScriptableObjects/Skill/SummonSkill/Instances/" + summonSkillData.skillName + ".asset");
        AssetDatabase.SaveAssets();
    }
}
