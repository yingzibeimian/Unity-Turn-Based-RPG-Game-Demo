using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 施法中心锚点
/// </summary>
public enum EffectAnchorMode
{
    selfCentered, //以施法者自身为中心
    selectableCentered, //可以指定施法中心位置
}

/// <summary>
/// 影响目标类型
/// </summary>
public enum EffectTargetingPattern
{
    SingleTarget, //单体目标
    CircleAOE, //圆形AOE
    ConeAOE, //扇形AOE
    LineAOE //线性AOE
}


/// <summary>
/// 技能影响区域参数
/// </summary>
[System.Serializable]
public struct EffectAreaParams
{
    [HorizontalGroup("行1", Width = 0.5f), LabelText("施法锚点")]
    public EffectAnchorMode effectAnchor;
    [HorizontalGroup("行1", Width = 0.5f), LabelText("影响范围")]
    public EffectTargetingPattern targetingPattern;

    [HorizontalGroup("行2", Width = 0.33f), LabelText("最大施法距离")]
    public int maxDistance;
    [HorizontalGroup("行2", Width = 0.33f), LabelText("技能范围半径")]
    public int radius;
    [HorizontalGroup("行2", Width = 0.33f), LabelText("技能宽度")]
    public int width;

    [BoxGroup("技能影响对象")]
    [HorizontalGroup("技能影响对象/行1", Width = 0.33f), LabelText("自身")]
    public bool affectSelf;
    [HorizontalGroup("技能影响对象/行1", Width = 0.33f), LabelText("自身阵营")]
    public bool affectSelfCamp;
    [HorizontalGroup("技能影响对象/行1", Width = 0.33f), LabelText("敌对阵营")]
    public bool affectOppositeCamp;
}

//效果施加技能ScriptableObject数据
[CreateAssetMenu(fileName = "New EffectApplicationSkill", menuName = "RPG/Skill/EffectApplicationSkill")]
public class EffectApplicationSkillData : SkillBaseData
{
    [BoxGroup("技能数据")]
    [HorizontalGroup("技能数据/行1", Width = 0.5f)]
    [LabelText("伤害骰子个数")]
    public int damageDiceCount = 0;
    [HorizontalGroup("技能数据/行1", Width = 0.5f)]
    [LabelText("伤害骰子面数")]
    public int damageDiceSides = 0;

    [HorizontalGroup("技能数据/行2")]
    [LabelText("施法者是否位移到目标位置")]
    public bool shouldMoveToTarget = false;

    [BoxGroup("技能数据/影响范围")]
    [LabelText("范围参数")]
    public EffectAreaParams effectArea;

    [BoxGroup("施加Buff")]
    [LabelText("Buff数据")]
    public List<Buff> applicateBuffs;
}
