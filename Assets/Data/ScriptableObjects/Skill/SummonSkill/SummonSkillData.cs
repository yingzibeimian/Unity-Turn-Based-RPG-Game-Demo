using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 召唤区域参数
/// </summary>
[System.Serializable]
public struct SummonAreaParams
{
    [HorizontalGroup("行1", Width = 0.5f), LabelText("召唤锚点")]
    public EffectAnchorMode effectAnchor;

    [HorizontalGroup("行2", Width = 0.5f), LabelText("最大召唤距离")]
    public int maxDistance;
    [HorizontalGroup("行2", Width = 0.5f), LabelText("召唤范围半径")]
    public int radius;
}

/// <summary>
/// 召唤技能ScriptableObject数据
/// </summary>
[CreateAssetMenu(fileName = "New SummonSkill", menuName = "RPG/Skill/SummonSkill")]
public class SummonSkillData : SkillBaseData
{
    [BoxGroup("技能数据")]
    [HorizontalGroup("技能数据/行1", Width = 0.5f), LabelText("召唤单位数量")]
    public int summonedUnitCount; //召唤单位数量
    [HorizontalGroup("技能数据/行1", Width = 300), LabelText("依赖召唤者存在")]
    [Tooltip("True时召唤师死亡会摧毁该单位")]
    public bool inheritSummonerLifespan; //召唤单位是否依赖召唤者存在

    [BoxGroup("技能数据/召唤范围")]
    [HorizontalGroup("技能数据/行2"), LabelText("范围参数")]
    public SummonAreaParams summonArea; //召唤区域

    [BoxGroup("技能数据")]
    [PreviewField(80)]
    [HorizontalGroup("技能数据/行3"), LabelText("召唤单位预设体")]
    public GameObject summonedUnitPrefab; //召唤物
}
