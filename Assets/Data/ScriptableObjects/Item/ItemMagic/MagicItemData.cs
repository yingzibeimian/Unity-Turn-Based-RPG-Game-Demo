using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ħ����ƷScriptableObject����
/// </summary>
[CreateAssetMenu(fileName = "New Equipment", menuName = "RPG/Item/Magic")]
public class MagicItemData : ItemBaseData
{
    [BoxGroup("ϰ�ü���")]
    [LabelText("����")]
    public SkillBaseData magicItemSkill;
}
