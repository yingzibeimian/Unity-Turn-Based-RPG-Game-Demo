using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��Ʒ����
/// </summary>
public class ItemBaseData : ScriptableObject
{
    [BoxGroup("������Ϣ", centerLabel: false)]
    [HorizontalGroup("������Ϣ/��1", Width = 0.5f)]
    public int id; //��Ʒid
    [HorizontalGroup("������Ϣ/��1", Width = 0.5f)]
    [LabelText("��Ʒ����")]
    public string itemName; //��Ʒ����

    [HorizontalGroup("������Ϣ/��2", Width = 0.5f)]
    [LabelText("��ֵ")]
    public float value; //��Ʒ��ֵ
    [HorizontalGroup("������Ϣ/��2", Width = 0.5f)]
    [LabelText("����")]
    public float weight; //��Ʒ����

    [PreviewField(80)]
    [HorizontalGroup("������Ϣ/��3", Width = 150), LabelText("ͼ��")]
    public Sprite icon; //��Ʒͼ��
    [PreviewField(80)]
    [HorizontalGroup("������Ϣ/��3", Width = 150), LabelText("�ޱ���ͼ��")]
    public Sprite iconWithoutBG; //�ޱ�����Ʒͼ��

    [TextArea]
    [HorizontalGroup("������Ϣ/��4")]
    [LabelText("��Ʒ����")]
    public string description; //��Ʒ����
}
