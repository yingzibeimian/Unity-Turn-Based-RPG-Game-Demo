using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �ٻ��������
/// </summary>
[System.Serializable]
public struct SummonAreaParams
{
    [HorizontalGroup("��1", Width = 0.5f), LabelText("�ٻ�ê��")]
    public EffectAnchorMode effectAnchor;

    [HorizontalGroup("��2", Width = 0.5f), LabelText("����ٻ�����")]
    public int maxDistance;
    [HorizontalGroup("��2", Width = 0.5f), LabelText("�ٻ���Χ�뾶")]
    public int radius;
}

/// <summary>
/// �ٻ�����ScriptableObject����
/// </summary>
[CreateAssetMenu(fileName = "New SummonSkill", menuName = "RPG/Skill/SummonSkill")]
public class SummonSkillData : SkillBaseData
{
    [BoxGroup("��������")]
    [HorizontalGroup("��������/��1", Width = 0.5f), LabelText("�ٻ���λ����")]
    public int summonedUnitCount; //�ٻ���λ����
    [HorizontalGroup("��������/��1", Width = 300), LabelText("�����ٻ��ߴ���")]
    [Tooltip("Trueʱ�ٻ�ʦ������ݻٸõ�λ")]
    public bool inheritSummonerLifespan; //�ٻ���λ�Ƿ������ٻ��ߴ���

    [BoxGroup("��������/�ٻ���Χ")]
    [HorizontalGroup("��������/��2"), LabelText("��Χ����")]
    public SummonAreaParams summonArea; //�ٻ�����

    [BoxGroup("��������")]
    [PreviewField(80)]
    [HorizontalGroup("��������/��3"), LabelText("�ٻ���λԤ����")]
    public GameObject summonedUnitPrefab; //�ٻ���
}
