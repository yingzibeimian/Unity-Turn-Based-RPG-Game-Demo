using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ����Ʒ��Ϣ�༭�����
/// </summary>
public class ConsumableItemDataEditor : OdinMenuEditorWindow
{
    CreateNewConsumableItemData createNewConsumableItemData; //�����������

    /// <summary>
    /// ��������Odin�Ŀ���չ�༭������, ����˵�·��
    /// </summary>
    [MenuItem("Tools/DataManger/Item Data/ConsumableItem Data")]
    private static void OpenWindow()
    {
        GetWindow<ConsumableItemDataEditor>().Show(); //��������ʾ�༭������
    }

    /// <summary>
    /// ��ֹʵ�����գ���δ���������ڴ��ڱ��ر�ʱɾ��ʵ��
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (createNewConsumableItemData != null)
        {
            DestroyImmediate(createNewConsumableItemData.consumableItemData); //����δ�������ʱ����
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
                ConsumableItemData asset = selected.SelectedValue as ConsumableItemData;
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

        createNewConsumableItemData = new CreateNewConsumableItemData();
        tree.Add("Create New Data", createNewConsumableItemData);
        tree.AddAllAssetsAtPath("ConsumableItem Data", "Assets/Data/ScriptableObjects/Item/ItemConsumable/Instances", typeof(ConsumableItemData)); //AddAllAssetsAtPathɨ��ָ��·���µ�ָ��������Դ

        return tree;
    }
}

/// <summary>
/// ���ݴ������
/// </summary>
public class CreateNewConsumableItemData
{
    //InlineEditor����: �����༭������ֱ�������༭����
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
