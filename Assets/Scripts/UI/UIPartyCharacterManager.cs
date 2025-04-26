using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPartyCharacterManager : MonoBehaviour
{
    private static UIPartyCharacterManager instance;
    public static UIPartyCharacterManager Instance => instance;

    public GameObject partyCharacterPanel; //队伍角色面板
    public Button characterPanelButton; //角色和背包面板按钮
    public Button hideCharacterPanelButton; //隐藏角色面板按钮

    public GameObject portraitPerfab; //头像预设体
    public Transform partyMembersParent; //队伍成员头像父对象

    //角色数据
    public TextMeshProUGUI strengthData; //力量数据
    public TextMeshProUGUI finesseData; //敏捷数据
    public TextMeshProUGUI intelligenceData; //智力数据
    public TextMeshProUGUI constitutionData; //体质数据
    public TextMeshProUGUI charismaData; //魅力数据
    public TextMeshProUGUI witsData; //智慧数据
    public TextMeshProUGUI initiativeData; //先攻数据
    public TextMeshProUGUI attackDistanceData; //攻击距离数据
    public TextMeshProUGUI damageData; //伤害数据
    public TextMeshProUGUI armorClassData; //防御等级AC数据
    public TextMeshProUGUI speedData; //速度数据

    //角色展示
    public Camera modelCamera; //角色模型相机
    public RenderTexture modelTexture; //渲染角色模型的render texture

    public TextMeshProUGUI nameData; //名字数据
    public TextMeshProUGUI levelData; //等级数据
    public TextMeshProUGUI healthPointData; //生命值数据

    //插槽
    public Transform slotHelmet;
    public Transform slotChest;
    public Transform slotGlowes;
    public Transform slotBelt;
    public Transform slotBoots;
    public Transform slotAmulet;
    public Transform slotLeftRing;
    public Transform slotRightRing;
    public Transform slotLeggings;
    public Transform slotWeapon;

    public GameObject itemImgPrefab; //装备物品图标预设体
    public GameObject equipmentItemClickedPanel; //装备右键点击面板
    private GameObject clickedItemIcon; //点击装备图标UI

    public GameObject itemTip; //装备信息提示面板

    public Color highlightColor = Color.white; //高光(主控)边框颜色
    private Color followerColor = Color.gray; //队友边框颜色

    private Dictionary<Character, GameObject> portraitDic = new Dictionary<Character, GameObject>(); //key:角色 value:头像物体
    private Dictionary<GameObject, EquipmentItemData> itemMap = new Dictionary<GameObject, EquipmentItemData>(); //key:装备物品图标UI value:装备
    private GameObject lastLeaderPortrait; //上一个主控角色对应的UI头像物体

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        BindFixedButtons(); //为固定按钮绑定响应事件

        followerColor = portraitPerfab.GetComponent<Image>().color;
        StartCoroutine(InitializaPartyCharacterPanelUI());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 为固定按钮绑定响应事件
    /// </summary>
    private void BindFixedButtons()
    {
        characterPanelButton.onClick.AddListener(UpdatePartyCharacterPanelVisibility);
        hideCharacterPanelButton.onClick.AddListener(OnHideCharacterPanelButtonClicked);

        equipmentItemClickedPanel.transform.Find("UnequipButton").GetComponent<Button>().onClick.AddListener(OnUnequipButtonClicked);
        equipmentItemClickedPanel.transform.Find("DropItemButton").GetComponent<Button>().onClick.AddListener(OnDropItemButtonClicked);
    }

    /// <summary>
    /// 更新队伍角色面板的可见性
    /// </summary>
    public void UpdatePartyCharacterPanelVisibility()
    {
        if (partyCharacterPanel.activeSelf)
        {
            partyCharacterPanel.SetActive(false);
        }
        else
        {
            partyCharacterPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 初始化队伍角色面板UI
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializaPartyCharacterPanelUI()
    {
        yield return new WaitWhile(() => PartyManager.Instance == null || !PartyManager.Instance.groupsInitialized || !PartyManager.Instance.leader.characterInitialized);

        //初始化队伍角色面板头像
        foreach (LinkedList<Character> group in PartyManager.Instance.groups)
        {
            if (group.Count == 0)
            {
                continue;
            }
            foreach (Character character in group)
            {
                CreatePortrait(character);
                yield return null;
            }
        }
    }

    /// <summary>
    /// 创建头像UI
    /// </summary>
    /// <param name="character"></param>
    private void CreatePortrait(Character character)
    {
        GameObject portrait = Instantiate(portraitPerfab, partyMembersParent); //实例化对象预设体, 并设置父对象
        portrait.name = character.info.name + "_Portrait"; //设置名字, 方便后续通过头像UI物体找到对应角色
        portrait.transform.GetChild(0).GetComponent<Image>().sprite = character.info.portrait; //设置头像图片

        portraitDic.Add(character, portrait); //建立角色和对应头像之间的关系

        //为面板上方的UI头像物体添加点击处理
        EventTrigger trigger = portrait.GetComponent<EventTrigger>();
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        enterEntry.callback.AddListener(OnClickPortrait);
        trigger.triggers.Add(enterEntry);

        if (character == PartyManager.Instance.leader)
        {
            lastLeaderPortrait = portrait;
            UpdateCharacterPanel(character); //更新角色面板信息
        }
    }

    /// <summary>
    /// 头像点击相应函数
    /// </summary>
    /// <param name="data"></param>
    private void OnClickPortrait(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        GameObject clickedObj = pointerData.pointerCurrentRaycast.gameObject;
        Character clickedChar = GetCharacterFromPortrait(clickedObj); //从当前指针(鼠标)射线检测到的对象得到角色
        if (clickedChar != null)
        {
            PartyManager.Instance.SwitchLeader(clickedChar); //切换主控到点击角色上
        }
    }

    /// <summary>
    /// 从UI头像得到对应的角色数据
    /// </summary>
    /// <param name="portrait"></param>
    /// <returns></returns>
    private Character GetCharacterFromPortrait(GameObject portrait)
    {
        return PartyManager.Instance.partyMembers.Find(c => c.info.name + "_Portrait" == portrait.name);
    }

    /// <summary>
    /// 更新队伍角色面板UI
    /// </summary>
    /// <param name="leader"></param>
    public void UpdateCharacterPanel(Character leader)
    {
        UpdateHighlight(leader); //更新头像高光
        UpdateCharacterInfo(leader); //更新角色属性信息显示
        UpdateModelCamera(leader); //更新用于渲染人物模型render texture的相机
        UpdateCharacterEquipmentSlot(leader); //更新装备插槽图标UI
    }

    /// <summary>
    /// 更新主控头像边框高光(重载)
    /// </summary>
    public void UpdateHighlight(Character leader)
    {
        if (lastLeaderPortrait != null)
        {
            lastLeaderPortrait.GetComponent<Image>().color = followerColor;
        }
        if (leader != null && portraitDic.ContainsKey(leader))
        {
            GameObject leaderGameObj = portraitDic[leader];
            leaderGameObj.GetComponent<Image>().color = highlightColor;
            lastLeaderPortrait = leaderGameObj;
        }
    }

    /// <summary>
    /// 更新角色属性信息
    /// </summary>
    /// <param name="leader"></param>
    public void UpdateCharacterInfo(Character leader)
    {
        //左侧属性面板
        strengthData.text = $"{leader.info.strength}";
        finesseData.text = $"{leader.info.finesse}";
        intelligenceData.text = $"{leader.info.intelligence}";
        constitutionData.text = $"{leader.info.constitution}";
        charismaData.text = $"{leader.info.charisma}";
        witsData.text = $"{leader.info.wits}";

        initiativeData.text = $"{leader.info.initiative}";
        attackDistanceData.text = $"{leader.info.attackDistance}";
        damageData.text = $"{leader.info.damageDiceCount}d{leader.info.damageDiceSides}";
        armorClassData.text = $"{leader.info.armorClass}";

        speedData.text = $"{leader.info.runSpeed}";

        //展示面板属性
        nameData.text = leader.info.name;
        levelData.text = $"等级: {leader.info.level}";
        healthPointData.text = $"生命值: {leader.info.hp}/{leader.info.maxHp}";
    }

    /// <summary>
    /// 更新主控模型渲染相机信息
    /// </summary>
    /// <param name="leader"></param>
    private void UpdateModelCamera(Character leader)
    {
        // 将上一个角色的所有子物体层级恢复为Character
        if(modelCamera.transform.parent != null)
        {
            SetLayerRecursively(modelCamera.transform.parent.gameObject, LayerMask.NameToLayer("Character"));
        }

        // 将新角色的所有子物体层级设置为Model
        SetLayerRecursively(leader.gameObject, LayerMask.NameToLayer("Model"));

        modelCamera.transform.SetParent(leader.transform); //将用来渲染模型的相机设为主控的子对象

        //重置相机本地transform信息
        modelCamera.transform.localPosition = new Vector3(0, 1, 3);
        modelCamera.transform.localRotation = Quaternion.Euler(0, 180, 0);
        modelCamera.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 递归设置层级
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="layer"></param>
    public void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null)
        {
            return;
        }
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// 切换主控时, 更新装备插槽图标UI
    /// </summary>
    private void UpdateCharacterEquipmentSlot(Character leader)
    {
        //如果之前插槽中存在上一位主控的装备, 就先卸下装备图标UI
        if(slotHelmet.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotHelmet.GetChild(0).gameObject]);
        }
        //如果新主控的队友插槽装备不为空, 就更新装备图标UI
        if (leader.equipment.helmetEquipment != null)
        {
            EquipItemToSlot(leader.equipment.helmetEquipment);
        }

        if (slotChest.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotChest.GetChild(0).gameObject]);
        }
        if (leader.equipment.chestEquipment != null)
        {
            EquipItemToSlot(leader.equipment.chestEquipment);
        }

        if (slotGlowes.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotGlowes.GetChild(0).gameObject]);
        }
        if (leader.equipment.glowesEquipment != null)
        {
            EquipItemToSlot(leader.equipment.glowesEquipment);
        }

        if (slotBelt.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotBelt.GetChild(0).gameObject]);
        }
        if (leader.equipment.beltEquipment != null)
        {
            EquipItemToSlot(leader.equipment.beltEquipment);
        }

        if (slotBoots.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotBoots.GetChild(0).gameObject]);
        }
        if (leader.equipment.bootsEquipment != null)
        {
            EquipItemToSlot(leader.equipment.bootsEquipment);
        }

        if (slotAmulet.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotAmulet.GetChild(0).gameObject]);
        }
        if (leader.equipment.amuletEquipment != null)
        {
            EquipItemToSlot(leader.equipment.amuletEquipment);
        }

        if (slotLeftRing.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotLeftRing.GetChild(0).gameObject]);
        }
        if (leader.equipment.leftRingEquipment != null)
        {
            EquipItemToSlot(leader.equipment.leftRingEquipment);
        }

        if (slotRightRing.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotRightRing.GetChild(0).gameObject]);
        }
        if (leader.equipment.rightRingEquipment != null)
        {
            EquipItemToSlot(leader.equipment.rightRingEquipment);
        }

        if (slotLeggings.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotLeggings.GetChild(0).gameObject]);
        }
        if (leader.equipment.leggingsEquipment != null)
        {
            EquipItemToSlot(leader.equipment.leggingsEquipment);
        }

        if (slotWeapon.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotWeapon.GetChild(0).gameObject]);
        }
        if (leader.equipment.weaponEquipment != null)
        {
            EquipItemToSlot(leader.equipment.weaponEquipment);
        }
    }


    /// <summary>
    /// 将装备UI图标加入插槽
    /// </summary>
    /// <param name="leader"></param>
    public void EquipItemToSlot(EquipmentItemData item)
    {
        Transform slotParent = null;
        if (item.type == EquipmentType.Helmet)
        {
            slotParent = slotHelmet;
        }
        else if (item.type == EquipmentType.Chest)
        {
            slotParent = slotChest;
        }
        else if (item.type == EquipmentType.Glowes)
        {
            slotParent = slotGlowes;
        }
        else if (item.type == EquipmentType.Belt)
        {
            slotParent = slotBelt;
        }
        else if (item.type == EquipmentType.Boots)
        {
            slotParent = slotBoots;
        }
        else if (item.type == EquipmentType.Amulet)
        {
            slotParent = slotAmulet;
        }
        else if (item.type == EquipmentType.Ring)
        {
            if(slotLeftRing.childCount == 0)
            {
                slotParent = slotLeftRing;
            }
            else
            {
                slotParent = slotRightRing;
            }
        }
        else if (item.type == EquipmentType.Leggings)
        {
            slotParent = slotLeggings;
        }
        else if (item.type == EquipmentType.Weapon)
        {
            slotParent = slotWeapon;
        }
        GameObject itemIcon = Instantiate(itemImgPrefab, slotParent);
        itemIcon.GetComponent<Image>().sprite = item.icon; //将item的Image图片设置为对应icon
        itemMap.Add(itemIcon, item); //建立装备物品图标UI 与 装备ScriptableObject数据 之间的关系

        //添加点击响应事件
        EventTrigger trigger = itemIcon.GetComponent<EventTrigger>();
        EventTrigger.Entry clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        clickEntry.callback.AddListener(OnItemIconClicked);
        enterEntry.callback.AddListener(OnItemIconPointerEnter);
        exitEntry.callback.AddListener(OnItemIconPointerExit);
        trigger.triggers.Add(clickEntry);
        trigger.triggers.Add(enterEntry);
        trigger.triggers.Add(exitEntry);
    }

    /// <summary>
    /// 将装备UI图标从插槽中移除
    /// </summary>
    /// <param name="item"></param>
    public void UnequipItemFromSlot(EquipmentItemData item)
    {
        Transform slotParent = null;

        if (item.type == EquipmentType.Helmet)
        {
            slotParent = slotHelmet;
        }
        else if (item.type == EquipmentType.Chest)
        {
            slotParent = slotChest;
        }
        else if (item.type == EquipmentType.Glowes)
        {
            slotParent = slotGlowes;
        }
        else if (item.type == EquipmentType.Belt)
        {
            slotParent = slotBelt;
        }
        else if (item.type == EquipmentType.Boots)
        {
            slotParent = slotBoots;
        }
        else if (item.type == EquipmentType.Amulet)
        {
            slotParent = slotAmulet;
        }
        else if (item.type == EquipmentType.Ring)
        {
            if (slotLeftRing.childCount > 0 && itemMap[slotLeftRing.GetChild(0).gameObject] == item)
            {
                slotParent = slotLeftRing;
            }
            else if (slotRightRing.childCount > 0 && itemMap[slotRightRing.GetChild(0).gameObject] == item)
            {
                slotParent = slotRightRing;
            }
        }
        else if (item.type == EquipmentType.Leggings)
        {
            slotParent = slotLeggings;
        }
        else if (item.type == EquipmentType.Weapon)
        {
            slotParent = slotWeapon;
        }

        GameObject itemIcon = slotParent.GetChild(0).gameObject;
        itemMap.Remove(itemIcon);
        DestroyImmediate(itemIcon);
    }

    /// <summary>
    /// 装备图标UI点击响应事件
    /// </summary>
    /// <param name="data"></param>
    private void OnItemIconClicked(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;
        HideAllClickedPanel(); //关闭所有点击面板
        //如果不是右键点击, 就不响应
        if (pointerData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        clickedItemIcon = pointerData.pointerCurrentRaycast.gameObject;
        if (!itemMap.ContainsKey(clickedItemIcon))
        {
            return;
        }
        equipmentItemClickedPanel.transform.position = pointerData.position; //移动面板位置
        equipmentItemClickedPanel.SetActive(true); //显示点击面板
    }

    /// <summary>
    /// 物品图标指针(鼠标)进入响应函数
    /// </summary>
    /// <param name="item"></param>
    private void OnItemIconPointerEnter(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;
        GameObject itemIcon = pointerData.pointerEnter.gameObject;
        if (itemIcon == null || !itemMap.ContainsKey(itemIcon))
        {
            return;
        }
        ItemBaseData item = itemMap[itemIcon];
        EquipmentItemData equipmentItem = item as EquipmentItemData;
        itemTip.transform.Find("ItemIcon").GetComponent<Image>().sprite = item.icon;
        itemTip.transform.Find("ItemName").GetComponent<TextMeshProUGUI>().text = item.itemName;
        
        string rarityStr = "";
        if (equipmentItem.rarity == Rarity.Common)
        {
            rarityStr = $"<color=white>品质:普通</color>";
        }
        else if (equipmentItem.rarity == Rarity.Uncommon)
        {
            rarityStr = $"<color=#00FF00>品质:精良</color>";
        }
        else if (equipmentItem.rarity == Rarity.Rare)
        {
            rarityStr = $"<color=#00BFFF>品质:稀有</color>";
        }
        else if (equipmentItem.rarity == Rarity.Legendary)
        {
            rarityStr = $"<color=#9400D3>品质:传说</color>";
        }
        else if (equipmentItem.rarity == Rarity.Unique)
        {
            rarityStr = $"<color=#DAA520>品质:独特</color>";
        }

        string typeStr = "";
        if (equipmentItem.type == EquipmentType.Helmet)
        {
            typeStr = "头盔";
        }
        else if (equipmentItem.type == EquipmentType.Chest)
        {
            typeStr = "胸甲";
        }
        else if (equipmentItem.type == EquipmentType.Glowes)
        {
            typeStr = "手套";
        }
        else if (equipmentItem.type == EquipmentType.Belt)
        {
            typeStr = "腰带";
        }
        else if (equipmentItem.type == EquipmentType.Boots)
        {
            typeStr = "靴子";
        }
        else if (equipmentItem.type == EquipmentType.Amulet)
        {
            typeStr = "护符";
        }
        else if (equipmentItem.type == EquipmentType.Ring)
        {
            typeStr = "戒指";
        }
        else if (equipmentItem.type == EquipmentType.Leggings)
        {
            typeStr = "绑腿";
        }
        else if (equipmentItem.type == EquipmentType.Weapon)
        {
            typeStr = "武器";
        }

        itemTip.transform.Find("ItemValueAndWeight").GetComponent<TextMeshProUGUI>().text = String.Format("{0} 类型:{1} 价值:{2} 重量:{3}", rarityStr, typeStr, item.value, item.weight);
        itemTip.transform.Find("ItemDescription").GetComponent<TextMeshProUGUI>().text = item.description;

        itemTip.transform.position = itemIcon.transform.position + new Vector3(366, -40);
        itemTip.SetActive(true);
    }

    /// <summary>
    /// 物品图标指针(鼠标)退出响应函数
    /// </summary>
    /// <param name="data"></param>
    private void OnItemIconPointerExit(BaseEventData data)
    {
        itemTip.SetActive(false);
    }

    /// <summary>
    /// UnequipButton点击响应事件
    /// </summary>
    private void OnUnequipButtonClicked()
    {
        HideAllClickedPanel();

        EquipmentItemData item = itemMap[clickedItemIcon]; //所点击的装备
        Character leader = PartyManager.Instance.leader;
        leader.Unequip(item); //丢弃装备
    }

    /// <summary>
    /// DropItemButton点击响应事件
    /// </summary>
    private void OnDropItemButtonClicked()
    {
        HideAllClickedPanel();

        EquipmentItemData item = itemMap[clickedItemIcon]; //所点击的装备
        Character leader = PartyManager.Instance.leader;
        leader.DropEquipment(item); //丢弃装备
    }

    /// <summary>
    /// 关闭所有面板
    /// </summary>
    private void HideAllClickedPanel()
    {
        equipmentItemClickedPanel.SetActive(false);
    }

    /// <summary>
    /// HideCharacterPanelButton点击响应事件
    /// </summary>
    private void OnHideCharacterPanelButtonClicked()
    {
        partyCharacterPanel.SetActive(false);
    }
}