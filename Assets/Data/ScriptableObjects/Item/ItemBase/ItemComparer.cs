using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemTypeComparer : IComparer<ItemBaseData>
{
    public int Compare(ItemBaseData x, ItemBaseData y) => GetOrder(x).CompareTo(GetOrder(y));

    private int GetOrder(ItemBaseData item)
    {
        if (item is EquipmentItemData)
        {
            return 0;
        }
        if (item is MagicItemData)
        {
            return 1;
        }
        if (item is ConsumableItemData)
        {
            return 2;
        }
        throw new System.ArgumentException("Unknown item type");
    }
}

public class ItemValueComparer : IComparer<ItemBaseData>
{
    //降序排序 价值大的在前
    public int Compare(ItemBaseData x, ItemBaseData y) => y.value.CompareTo(x.value);
}

public class ItemWeightComparer : IComparer<ItemBaseData>
{
    //降序排序 重量大的在前
    public int Compare(ItemBaseData x, ItemBaseData y) => y.weight.CompareTo(x.weight);
}
