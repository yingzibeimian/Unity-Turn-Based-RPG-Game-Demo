using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType
{
    strength, //������Ӱ��
    finesse, //������Ӱ��
    intelligence //������Ӱ��
}

/// <summary>
/// ��ɫ��Ϣ
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterData", menuName = "RPG/CharacterData")]
public class CharacterData : SerializedScriptableObject
{
    [BoxGroup("������Ϣ", centerLabel:false)]
    [HorizontalGroup("������Ϣ/��ɫ����", Width = 0.5f)]
    [LabelText("��ɫ����")]
    public string characterName; //����

    [PreviewField(80)]
    [HorizontalGroup("������Ϣ/ͷ��", Width = 125), LabelText("ͷ��")]
    public Sprite portrait; //ͷ��

    [PreviewField(80), HideLabel]
    [HorizontalGroup("������Ϣ/ͷ��", Width = 125), LabelText("������")]
    public Sprite bustPortrait; //������

    //[PreviewField(80), HideLabel]
    //[HorizontalGroup("������Ϣ/ͷ��", Width = 125), LabelText("ģ��")]
    //public GameObject model; //ģ��


    [BoxGroup("��������", centerLabel: false)]
    [BoxGroup("��������/��ɫ����")]
    [HorizontalGroup("��������/��ɫ����/��1", Width = 0.33f)]
    [LabelText("�ȼ�")]
    public int level = 1; //�ȼ�
    [HorizontalGroup("��������/��ɫ����/��2", Width = 0.33f)]
    [LabelText("�������ֵ")]
    public int maxHp = 5; //�������ֵ
    [HorizontalGroup("��������/��ɫ����/��2", Width = 0.33f)]
    [LabelText("��ǰ����ֵ")]
    public int hp = 5; //��ǰ����ֵ

    [Space]
    [HorizontalGroup("��������/��ɫ����/��3")]
    [LabelText("����")]
    public int strength = 8; //����
    [Space]
    [HorizontalGroup("��������/��ɫ����/��3")]
    [LabelText("����")]
    public int finesse = 8; //����
    [Space]
    [HorizontalGroup("��������/��ɫ����/��3")]
    [LabelText("����")]
    public int intelligence = 8; //����
    [HorizontalGroup("��������/��ɫ����/��4")]
    [LabelText("����")]
    public int constitution = 8; //����
    [HorizontalGroup("��������/��ɫ����/��4")]
    [LabelText("����")]
    public int charisma = 8; //����
    [HorizontalGroup("��������/��ɫ����/��4")]
    [LabelText("��֪")]
    public int wits = 8; //�ǻ�


    [BoxGroup("��������/�ƶ�����")]
    [HorizontalGroup("��������/�ƶ�����/��1")]
    [LabelText("�����ٶ�")]
    public float walkSpeed; //�����ٶ�
    [HorizontalGroup("��������/�ƶ�����/��1")]
    [LabelText("�����ٶ�")]
    public float runSpeed; //�ܲ��ٶ�
    [HorizontalGroup("��������/�ƶ�����/��1")]
    [LabelText("ת���ٶ�")]
    public float rotateSpeed; //ת���ٶ�

    //public bool isAI; //�����ж��Ƿ���AI����


    //����������Ϣ
    [BoxGroup("��������/λ����Ϣ")]
    [HorizontalGroup("��������/λ����Ϣ/��1")]
    public int q;
    [HorizontalGroup("��������/λ����Ϣ/��1")]
    public int r;
    public int s => -q - r;
    [HorizontalGroup("��������/λ����Ϣ/��1")]
    public int heightOrder;


    [BoxGroup("��������/ս������")]
    [HorizontalGroup("��������/ս������/��1")]
    [LabelText("��������")]
    public int attackDistance = 1; //��������
    [HorizontalGroup("��������/ս������/��1")]
    [LabelText("�����ȼ�AC")]
    public int armorClass = 10; //�����ȼ�AC
    [HorizontalGroup("��������/ս������/��2")]
    [LabelText("�ȹ�")]
    public int initiative = 1; //�ȹ�
    [HorizontalGroup("��������/ս������/��2")]
    [LabelText("�ж�����")]
    public float actionPoint = 0; //�غ��ж�����
    [HorizontalGroup("��������/ս������/��3")]
    [LabelText("�����춨����")]
    public AttackType attackType; //������Ӱ������
    [HorizontalGroup("��������/ս������/��3")]
    [LabelText("����������ֵ")]
    public int proficiency = 0; //����������ֵ

    //[BoxGroup("��������/����")]
    //[LabelText("����������")]
    //public RuntimeAnimatorController controller;
}