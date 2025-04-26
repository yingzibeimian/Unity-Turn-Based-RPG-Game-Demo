using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// �ٻ����ܱ༭�����
/// </summary>
public class SummonSkillDataEditor : OdinMenuEditorWindow
{
    CreateNewSummonSkillData createNewSummonSkillData; //�����������

    /// <summary>
    /// ��������Odin�Ŀ���չ�༭������, ����˵�·��
    /// </summary>
    [MenuItem("Tools/DataManger/Skill Data/SummonSkill Data")]
    private static void OpenWindow()
    {
        GetWindow<SummonSkillDataEditor>().Show(); //��������ʾ�༭������
    }

    /// <summary>
    /// ��ֹʵ�����գ���δ���������ڴ��ڱ��ر�ʱɾ��ʵ��
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewSummonSkillData != null)
        {
            DestroyImmediate(createNewSummonSkillData.summonSkillData); //����δ�������ʱ����
        }
    }

    /// <summary>
    /// ɾ�����ݹ���ʵ��
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
    /// �˵�������
    /// </summary>
    protected override OdinMenuTree BuildMenuTree()
    {
        OdinMenuTree tree = new OdinMenuTree();

        createNewSummonSkillData = new CreateNewSummonSkillData();
        tree.Add("Create New Data", createNewSummonSkillData);
        tree.AddAllAssetsAtPath("SummonSkill Data", "Assets/Data/ScriptableObjects/Skill/SummonSkill/Instances", typeof(SummonSkillData)); //AddAllAssetsAtPathɨ��ָ��·���µ�ָ��������Դ

        return tree;
    }
}

/// <summary>
/// ���ݴ������
/// </summary>
public class CreateNewSummonSkillData
{
    //InlineEditor����: �����༭������ֱ�������༭����
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
