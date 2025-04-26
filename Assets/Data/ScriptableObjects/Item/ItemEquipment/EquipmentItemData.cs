using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装备类型枚举
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
/// 装备ScriptableObject数据
/// </summary>
[CreateAssetMenu(fileName = "New Equipment", menuName = "RPG/Item/Equipment")]
public class EquipmentItemData : ItemBaseData
{
    [BoxGroup("装备信息", centerLabel: false)]
    [HorizontalGroup("装备信息/装备类型", Width = 0.5f)]
    [LabelText("装备类型")]
    public EquipmentType type;
    [HorizontalGroup("装备信息/装备类型", Width = 0.5f)]
    [LabelText("装备品质")]
    public Rarity rarity;

    [BoxGroup("装备信息/属性加值", centerLabel: false)]
    [HorizontalGroup("装备信息/属性加值/行1", Width = 0.5f)]
    [LabelText("最大生命值")]
    public int maxHpModifier = 0;
    [HorizontalGroup("装备信息/属性加值/行1", Width = 0.5f)]
    [LabelText("攻击距离")]
    public int attackDistanceModifier = 0;

    [HorizontalGroup("装备信息/属性加值/行2", Width = 0.5f)]
    [LabelText("力量")]
    public int strengthModifier = 0;
    [HorizontalGroup("装备信息/属性加值/行2", Width = 0.5f)]
    [LabelText("敏捷")]
    public int finesseModifier = 0;
    [HorizontalGroup("装备信息/属性加值/行3", Width = 0.5f)]
    [LabelText("智力")]
    public int intelligenceModifier = 0;
    [HorizontalGroup("装备信息/属性加值/行3", Width = 0.5f)]
    [LabelText("体质")]
    public int constitutionModifier = 0;
    [HorizontalGroup("装备信息/属性加值/行4", Width = 0.5f)]
    [LabelText("魅力")]
    public int charismaModifier = 0;
    [HorizontalGroup("装备信息/属性加值/行4", Width = 0.5f)]
    [LabelText("感知")]
    public int witsModifier = 0;

    [HorizontalGroup("装备信息/属性加值/行5", Width = 0.5f)]
    [LabelText("伤害骰子个数")]
    public int damageDiceCountModifier = 0;
    [HorizontalGroup("装备信息/属性加值/行5", Width = 0.5f)]
    [LabelText("伤害骰子面数")]
    public int damageDiceSidesModifier = 0;

    [HorizontalGroup("装备信息/属性加值/行6", Width = 0.33f)]
    [LabelText("防御等级")]
    public int armorClassModifier = 0;
    [HorizontalGroup("装备信息/属性加值/行6", Width = 0.33f)]
    [LabelText("先攻")]
    public int initiativeModifier = 0;
    [HorizontalGroup("装备信息/属性加值/行6", Width = 0.33f)]
    [LabelText("速度")]
    public float speedModifier = 0;

    [BoxGroup("附加Buff")]
    [LabelText("Buff数据")]
    public List<Buff> equipmentItemBuffs;

    [BoxGroup("附加技能")]
    [LabelText("技能数据")]
    public List<SkillBaseData> equipmentItemSkills;

    [LabelText("模型")]
    public GameObject model;
    [LabelText("武器类型")]
    public WeaponType weaponType;
}
