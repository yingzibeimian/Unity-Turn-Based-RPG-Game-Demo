using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ħ����Ʒ��Ϣ�༭�����
/// </summary>
public class MagicItemDataEditor : OdinMenuEditorWindow
{
    CreateNewMagicItemData createNewMagicItemData; //�����������

    /// <summary>
    /// ��������Odin�Ŀ���չ�༭������, ����˵�·��
    /// </summary>
    [MenuItem("Tools/DataManger/Item Data/MagicItem Data")]
    private static void OpenWindow()
    {
        GetWindow<MagicItemDataEditor>().Show(); //��������ʾ�༭������
    }

    /// <summary>
    /// ��ֹʵ�����գ���δ���������ڴ��ڱ��ر�ʱɾ��ʵ��
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewMagicItemData != null)
        {
            DestroyImmediate(createNewMagicItemData.magicItemData); //����δ�������ʱ����
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
                MagicItemData asset = selected.SelectedValue as MagicItemData;
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

        createNewMagicItemData = new CreateNewMagicItemData();
        tree.Add("Create New Data", createNewMagicItemData);
        tree.AddAllAssetsAtPath("MagicItem Data", "Assets/Data/ScriptableObjects/Item/ItemMagic/Instances", typeof(MagicItemData)); //AddAllAssetsAtPathɨ��ָ��·���µ�ָ��������Դ

        return tree;
    }
}

/// <summary>
/// ���ݴ������
/// </summary>
public class CreateNewMagicItemData
{
    //InlineEditor����: �����༭������ֱ�������༭����
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
    public MagicItemData magicItemData;

    public CreateNewMagicItemData()
    {
        magicItemData = ScriptableObject.CreateInstance<MagicItemData>();
        magicItemData.itemName = "New MagicItem Data";
    }

    [Button("Save Data")]
    private void SavaUnitData()
    {
        AssetDatabase.CreateAsset(magicItemData, "Assets/Data/ScriptableObjects/Item/ItemMagic/Instances/" + magicItemData.itemName + ".asset");
        AssetDatabase.SaveAssets();
    }
}
