using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// װ����Ϣ�༭�����
/// </summary>
public class EquipmentItemDataEditor : OdinMenuEditorWindow
{
    CreateNewEquipmentItemData createNewEquipmentItemData; //�����������

    /// <summary>
    /// ��������Odin�Ŀ���չ�༭������, ����˵�·��
    /// </summary>
    [MenuItem("Tools/DataManger/Item Data/EquipmentItem Data")]
    private static void OpenWindow()
    {
        GetWindow<EquipmentItemDataEditor>().Show(); //��������ʾ�༭������
    }

    /// <summary>
    /// ��ֹʵ�����գ���δ���������ڴ��ڱ��ر�ʱɾ��ʵ��
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewEquipmentItemData != null)
        {
            DestroyImmediate(createNewEquipmentItemData.equipmentItemData); //����δ�������ʱ����
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
                EquipmentItemData asset = selected.SelectedValue as EquipmentItemData;
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

        createNewEquipmentItemData = new CreateNewEquipmentItemData();
        tree.Add("Create New Data", createNewEquipmentItemData);
        tree.AddAllAssetsAtPath("EquipmentItem Data", "Assets/Data/ScriptableObjects/Item/ItemEquipment/Instances", typeof(EquipmentItemData)); //AddAllAssetsAtPathɨ��ָ��·���µ�ָ��������Դ

        return tree;
    }
}

/// <summary>
/// ���ݴ������
/// </summary>
public class CreateNewEquipmentItemData
{
    //InlineEditor����: �����༭������ֱ�������༭����
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
