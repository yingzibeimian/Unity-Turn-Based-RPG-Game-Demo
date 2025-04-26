using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���ܻ���
/// </summary>
public class SkillBaseData : ScriptableObject
{
    [BoxGroup("������Ϣ", centerLabel: false)]
    [HorizontalGroup("������Ϣ/��1", Width = 0.33f)]
    public int id; //����id
    [HorizontalGroup("������Ϣ/��1", Width = 0.33f)]
    [LabelText("��������")]
    public string skillName; //��������
    [HorizontalGroup("������Ϣ/��1", Width = 0.33f)]
    [LabelText("��������")]
    public string animationTriggerStr = ""; //���ܶ����Ĵ���������

    [HorizontalGroup("������Ϣ/��2", Width = 0.33f)]
    [LabelText("ǰ�õȼ�")]
    public int learnLevel; //ѧϰ����Ҫ��ȼ�
    [HorizontalGroup("������Ϣ/��2", Width = 0.33f)]
    [LabelText("��ȴ�غ���")]
    public int cooldownTurns; //��ȴ�غ���
    [HideInInspector]
    public int remainingTurns = 0; //��ȴʣ��غ���
    [HorizontalGroup("������Ϣ/��2", Width = 0.33f)]
    [LabelText("�����ж�����")]
    public int actionPointTakes; //�����ж�����

    [PreviewField(80)]
    [HorizontalGroup("������Ϣ/��3", Width = 150), LabelText("ͼ��")]
    public Sprite icon; //����ͼ��

    [TextArea]
    [HorizontalGroup("������Ϣ/��4")]
    [LabelText("Ч������")]
    public string description_Effects; //����Ч������

    [TextArea]
    [HorizontalGroup("������Ϣ/��5")]
    [LabelText("��������")]
    public string description_BG; //���ܱ�������
}
