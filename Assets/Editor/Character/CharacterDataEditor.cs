using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ��ɫ��Ϣ�༭�����, OdinMenuEditorWindow��Odin�ṩ�Ĵ����˵����Ĵ��ڻ���
/// </summary>
public class CharacterDataEditor : OdinMenuEditorWindow
{
    CreateNewCharacterData createNewCharacterData; //�����������

    /// <summary>
    /// ��������Odin�Ŀ���չ�༭������, ����˵�·��
    /// </summary>
    [MenuItem("Tools/DataManger/Character Data")]
    private static void OpenWindow()
    {
        GetWindow<CharacterDataEditor>().Show(); //��������ʾ�༭������
    }

    /// <summary>
    /// ��ֹʵ�����գ���δ���������ڴ��ڱ��ر�ʱɾ��ʵ��
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewCharacterData != null)
        {
            DestroyImmediate(createNewCharacterData.characterData); //����δ�������ʱ����
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
                CharacterData asset = selected.SelectedValue as CharacterData;
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

        createNewCharacterData = new CreateNewCharacterData();
        tree.Add("Create New Data", createNewCharacterData);
        tree.AddAllAssetsAtPath("Character Data", "Assets/Data/ScriptableObjects/Character/Instances", typeof(CharacterData)); //AddAllAssetsAtPathɨ��ָ��·���µ�ָ��������Դ

        return tree;
    }
}

/// <summary>
/// ���ݴ������
/// </summary>
public class CreateNewCharacterData
{
    //InlineEditor����: �����༭������ֱ�������༭����
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