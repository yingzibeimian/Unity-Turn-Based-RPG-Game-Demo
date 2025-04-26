using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ч��ʩ�Ӽ��ܱ༭�����
/// </summary>
public class EffectApplicationSkillDataEditor : OdinMenuEditorWindow
{
    CreateNewEffectApplicationSkillData createNewEffectApplicationSkillData; //�����������

    /// <summary>
    /// ��������Odin�Ŀ���չ�༭������, ����˵�·��
    /// </summary>
    [MenuItem("Tools/DataManger/Skill Data/EffectApplicationSkill Data")]
    private static void OpenWindow()
    {
        GetWindow<EffectApplicationSkillDataEditor>().Show(); //��������ʾ�༭������
    }

    /// <summary>
    /// ��ֹʵ�����գ���δ���������ڴ��ڱ��ر�ʱɾ��ʵ��
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewEffectApplicationSkillData != null)
        {
            DestroyImmediate(createNewEffectApplicationSkillData.effectApplicationSkillData); //����δ�������ʱ����
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
                EffectApplicationSkillData asset = selected.SelectedValue as EffectApplicationSkillData;
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

        createNewEffectApplicationSkillData = new CreateNewEffectApplicationSkillData();
        tree.Add("Create New Data", createNewEffectApplicationSkillData);
        tree.AddAllAssetsAtPath("EffectApplicationSkill Data", "Assets/Data/ScriptableObjects/Skill/EffectApplicationSkill/Instances", typeof(EffectApplicationSkillData)); //AddAllAssetsAtPathɨ��ָ��·���µ�ָ��������Դ

        return tree;
    }
}

/// <summary>
/// ���ݴ������
/// </summary>
public class CreateNewEffectApplicationSkillData
{
    //InlineEditor����: �����༭������ֱ�������༭����
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
