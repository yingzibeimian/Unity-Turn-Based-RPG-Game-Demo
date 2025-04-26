using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 消耗品ScriptableObject数据
/// </summary>
[CreateAssetMenu(fileName = "New Equipment", menuName = "RPG/Item/Consumable")]
public class ConsumableItemData : ItemBaseData
{
    [BoxGroup()]
    [LabelText("消耗获得Buff")]
    public Buff comsumableItemBuff;
}
