using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 魔法物品ScriptableObject数据
/// </summary>
[CreateAssetMenu(fileName = "New Equipment", menuName = "RPG/Item/Magic")]
public class MagicItemData : ItemBaseData
{
    [BoxGroup("习得技能")]
    [LabelText("技能")]
    public SkillBaseData magicItemSkill;
}
