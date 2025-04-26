using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品基类
/// </summary>
public class ItemBaseData : ScriptableObject
{
    [BoxGroup("基础信息", centerLabel: false)]
    [HorizontalGroup("基础信息/行1", Width = 0.5f)]
    public int id; //物品id
    [HorizontalGroup("基础信息/行1", Width = 0.5f)]
    [LabelText("物品名称")]
    public string itemName; //物品名称

    [HorizontalGroup("基础信息/行2", Width = 0.5f)]
    [LabelText("价值")]
    public float value; //物品价值
    [HorizontalGroup("基础信息/行2", Width = 0.5f)]
    [LabelText("重量")]
    public float weight; //物品重量

    [PreviewField(80)]
    [HorizontalGroup("基础信息/行3", Width = 150), LabelText("图标")]
    public Sprite icon; //物品图标
    [PreviewField(80)]
    [HorizontalGroup("基础信息/行3", Width = 150), LabelText("无背景图标")]
    public Sprite iconWithoutBG; //无背景物品图标

    [TextArea]
    [HorizontalGroup("基础信息/行4")]
    [LabelText("物品描述")]
    public string description; //物品描述
}
