using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ����ƷScriptableObject����
/// </summary>
[CreateAssetMenu(fileName = "New Equipment", menuName = "RPG/Item/Consumable")]
public class ConsumableItemData : ItemBaseData
{
    [BoxGroup()]
    [LabelText("���Ļ��Buff")]
    public Buff comsumableItemBuff;
}
