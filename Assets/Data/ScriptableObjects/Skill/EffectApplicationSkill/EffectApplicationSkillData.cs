using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ʩ������ê��
/// </summary>
public enum EffectAnchorMode
{
    selfCentered, //��ʩ��������Ϊ����
    selectableCentered, //����ָ��ʩ������λ��
}

/// <summary>
/// Ӱ��Ŀ������
/// </summary>
public enum EffectTargetingPattern
{
    SingleTarget, //����Ŀ��
    CircleAOE, //Բ��AOE
    ConeAOE, //����AOE
    LineAOE //����AOE
}


/// <summary>
/// ����Ӱ���������
/// </summary>
[System.Serializable]
public struct EffectAreaParams
{
    [HorizontalGroup("��1", Width = 0.5f), LabelText("ʩ��ê��")]
    public EffectAnchorMode effectAnchor;
    [HorizontalGroup("��1", Width = 0.5f), LabelText("Ӱ�췶Χ")]
    public EffectTargetingPattern targetingPattern;

    [HorizontalGroup("��2", Width = 0.33f), LabelText("���ʩ������")]
    public int maxDistance;
    [HorizontalGroup("��2", Width = 0.33f), LabelText("���ܷ�Χ�뾶")]
    public int radius;
    [HorizontalGroup("��2", Width = 0.33f), LabelText("���ܿ��")]
    public int width;

    [BoxGroup("����Ӱ�����")]
    [HorizontalGroup("����Ӱ�����/��1", Width = 0.33f), LabelText("����")]
    public bool affectSelf;
    [HorizontalGroup("����Ӱ�����/��1", Width = 0.33f), LabelText("������Ӫ")]
    public bool affectSelfCamp;
    [HorizontalGroup("����Ӱ�����/��1", Width = 0.33f), LabelText("�ж���Ӫ")]
    public bool affectOppositeCamp;
}

//Ч��ʩ�Ӽ���ScriptableObject����
[CreateAssetMenu(fileName = "New EffectApplicationSkill", menuName = "RPG/Skill/EffectApplicationSkill")]
public class EffectApplicationSkillData : SkillBaseData
{
    [BoxGroup("��������")]
    [HorizontalGroup("��������/��1", Width = 0.5f)]
    [LabelText("�˺����Ӹ���")]
    public int damageDiceCount = 0;
    [HorizontalGroup("��������/��1", Width = 0.5f)]
    [LabelText("�˺���������")]
    public int damageDiceSides = 0;

    [HorizontalGroup("��������/��2")]
    [LabelText("ʩ�����Ƿ�λ�Ƶ�Ŀ��λ��")]
    public bool shouldMoveToTarget = false;

    [BoxGroup("��������/Ӱ�췶Χ")]
    [LabelText("��Χ����")]
    public EffectAreaParams effectArea;

    [BoxGroup("ʩ��Buff")]
    [LabelText("Buff����")]
    public List<Buff> applicateBuffs;
}
