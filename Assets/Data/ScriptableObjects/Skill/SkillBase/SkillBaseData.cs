using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能基类
/// </summary>
public class SkillBaseData : ScriptableObject
{
    [BoxGroup("基础信息", centerLabel: false)]
    [HorizontalGroup("基础信息/行1", Width = 0.33f)]
    public int id; //技能id
    [HorizontalGroup("基础信息/行1", Width = 0.33f)]
    [LabelText("技能名称")]
    public string skillName; //技能名称
    [HorizontalGroup("基础信息/行1", Width = 0.33f)]
    [LabelText("触发动画")]
    public string animationTriggerStr = ""; //技能动画的触发器名称

    [HorizontalGroup("基础信息/行2", Width = 0.33f)]
    [LabelText("前置等级")]
    public int learnLevel; //学习技能要求等级
    [HorizontalGroup("基础信息/行2", Width = 0.33f)]
    [LabelText("冷却回合数")]
    public int cooldownTurns; //冷却回合数
    [HideInInspector]
    public int remainingTurns = 0; //冷却剩余回合数
    [HorizontalGroup("基础信息/行2", Width = 0.33f)]
    [LabelText("消耗行动点数")]
    public int actionPointTakes; //消耗行动点数

    [PreviewField(80)]
    [HorizontalGroup("基础信息/行3", Width = 150), LabelText("图标")]
    public Sprite icon; //技能图标

    [TextArea]
    [HorizontalGroup("基础信息/行4")]
    [LabelText("效果描述")]
    public string description_Effects; //技能效果描述

    [TextArea]
    [HorizontalGroup("基础信息/行5")]
    [LabelText("背景描述")]
    public string description_BG; //技能背景描述
}
