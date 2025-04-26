using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType
{
    strength, //受力量影响
    finesse, //受敏捷影响
    intelligence //受智力影响
}

/// <summary>
/// 角色信息
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterData", menuName = "RPG/CharacterData")]
public class CharacterData : SerializedScriptableObject
{
    [BoxGroup("基础信息", centerLabel:false)]
    [HorizontalGroup("基础信息/角色名称", Width = 0.5f)]
    [LabelText("角色名称")]
    public string characterName; //名字

    [PreviewField(80)]
    [HorizontalGroup("基础信息/头像", Width = 125), LabelText("头像")]
    public Sprite portrait; //头像

    [PreviewField(80), HideLabel]
    [HorizontalGroup("基础信息/头像", Width = 125), LabelText("半身像")]
    public Sprite bustPortrait; //半身像

    //[PreviewField(80), HideLabel]
    //[HorizontalGroup("基础信息/头像", Width = 125), LabelText("模型")]
    //public GameObject model; //模型


    [BoxGroup("基础属性", centerLabel: false)]
    [BoxGroup("基础属性/角色属性")]
    [HorizontalGroup("基础属性/角色属性/行1", Width = 0.33f)]
    [LabelText("等级")]
    public int level = 1; //等级
    [HorizontalGroup("基础属性/角色属性/行2", Width = 0.33f)]
    [LabelText("最大生命值")]
    public int maxHp = 5; //最大生命值
    [HorizontalGroup("基础属性/角色属性/行2", Width = 0.33f)]
    [LabelText("当前生命值")]
    public int hp = 5; //当前生命值

    [Space]
    [HorizontalGroup("基础属性/角色属性/行3")]
    [LabelText("力量")]
    public int strength = 8; //力量
    [Space]
    [HorizontalGroup("基础属性/角色属性/行3")]
    [LabelText("敏捷")]
    public int finesse = 8; //敏捷
    [Space]
    [HorizontalGroup("基础属性/角色属性/行3")]
    [LabelText("智力")]
    public int intelligence = 8; //智力
    [HorizontalGroup("基础属性/角色属性/行4")]
    [LabelText("体质")]
    public int constitution = 8; //体质
    [HorizontalGroup("基础属性/角色属性/行4")]
    [LabelText("魅力")]
    public int charisma = 8; //魅力
    [HorizontalGroup("基础属性/角色属性/行4")]
    [LabelText("感知")]
    public int wits = 8; //智慧


    [BoxGroup("基础属性/移动属性")]
    [HorizontalGroup("基础属性/移动属性/行1")]
    [LabelText("行走速度")]
    public float walkSpeed; //行走速度
    [HorizontalGroup("基础属性/移动属性/行1")]
    [LabelText("奔跑速度")]
    public float runSpeed; //跑步速度
    [HorizontalGroup("基础属性/移动属性/行1")]
    [LabelText("转身速度")]
    public float rotateSpeed; //转身速度

    //public bool isAI; //用来判断是否是AI控制


    //所在网格信息
    [BoxGroup("基础属性/位置信息")]
    [HorizontalGroup("基础属性/位置信息/行1")]
    public int q;
    [HorizontalGroup("基础属性/位置信息/行1")]
    public int r;
    public int s => -q - r;
    [HorizontalGroup("基础属性/位置信息/行1")]
    public int heightOrder;


    [BoxGroup("基础属性/战斗属性")]
    [HorizontalGroup("基础属性/战斗属性/行1")]
    [LabelText("攻击距离")]
    public int attackDistance = 1; //攻击距离
    [HorizontalGroup("基础属性/战斗属性/行1")]
    [LabelText("防御等级AC")]
    public int armorClass = 10; //防御等级AC
    [HorizontalGroup("基础属性/战斗属性/行2")]
    [LabelText("先攻")]
    public int initiative = 1; //先攻
    [HorizontalGroup("基础属性/战斗属性/行2")]
    [LabelText("行动点数")]
    public float actionPoint = 0; //回合行动点数
    [HorizontalGroup("基础属性/战斗属性/行3")]
    [LabelText("攻击检定属性")]
    public AttackType attackType; //攻击受影响属性
    [HorizontalGroup("基础属性/战斗属性/行3")]
    [LabelText("武器熟练加值")]
    public int proficiency = 0; //武器熟练加值

    //[BoxGroup("基础属性/动画")]
    //[LabelText("动画控制器")]
    //public RuntimeAnimatorController controller;
}