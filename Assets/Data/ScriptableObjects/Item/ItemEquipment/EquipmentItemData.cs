using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// װ������ö��
/// </summary>
public enum EquipmentType
{
    Helmet,
    Chest,
    Glowes,
    Belt,
    Boots,
    Amulet,
    Ring,
    Leggings,
    Weapon
}

public enum WeaponType
{
    Sword,
    Bow,
    Staff
}

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Legendary,
    Unique
}

/// <summary>
/// װ��ScriptableObject����
/// </summary>
[CreateAssetMenu(fileName = "New Equipment", menuName = "RPG/Item/Equipment")]
public class EquipmentItemData : ItemBaseData
{
    [BoxGroup("װ����Ϣ", centerLabel: false)]
    [HorizontalGroup("װ����Ϣ/װ������", Width = 0.5f)]
    [LabelText("װ������")]
    public EquipmentType type;
    [HorizontalGroup("װ����Ϣ/װ������", Width = 0.5f)]
    [LabelText("װ��Ʒ��")]
    public Rarity rarity;

    [BoxGroup("װ����Ϣ/���Լ�ֵ", centerLabel: false)]
    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��1", Width = 0.5f)]
    [LabelText("�������ֵ")]
    public int maxHpModifier = 0;
    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��1", Width = 0.5f)]
    [LabelText("��������")]
    public int attackDistanceModifier = 0;

    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��2", Width = 0.5f)]
    [LabelText("����")]
    public int strengthModifier = 0;
    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��2", Width = 0.5f)]
    [LabelText("����")]
    public int finesseModifier = 0;
    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��3", Width = 0.5f)]
    [LabelText("����")]
    public int intelligenceModifier = 0;
    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��3", Width = 0.5f)]
    [LabelText("����")]
    public int constitutionModifier = 0;
    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��4", Width = 0.5f)]
    [LabelText("����")]
    public int charismaModifier = 0;
    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��4", Width = 0.5f)]
    [LabelText("��֪")]
    public int witsModifier = 0;

    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��5", Width = 0.5f)]
    [LabelText("�˺����Ӹ���")]
    public int damageDiceCountModifier = 0;
    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��5", Width = 0.5f)]
    [LabelText("�˺���������")]
    public int damageDiceSidesModifier = 0;

    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��6", Width = 0.33f)]
    [LabelText("�����ȼ�")]
    public int armorClassModifier = 0;
    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��6", Width = 0.33f)]
    [LabelText("�ȹ�")]
    public int initiativeModifier = 0;
    [HorizontalGroup("װ����Ϣ/���Լ�ֵ/��6", Width = 0.33f)]
    [LabelText("�ٶ�")]
    public float speedModifier = 0;

    [BoxGroup("����Buff")]
    [LabelText("Buff����")]
    public List<Buff> equipmentItemBuffs;

    [BoxGroup("���Ӽ���")]
    [LabelText("��������")]
    public List<SkillBaseData> equipmentItemSkills;

    [LabelText("ģ��")]
    public GameObject model;
    [LabelText("��������")]
    public WeaponType weaponType;
}
