using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;
using static UnityEngine.UI.GridLayoutGroup;

public class UIPartyInventoryManager : MonoBehaviour
{
    private static UIPartyInventoryManager instance;
    public static UIPartyInventoryManager Instance => instance;

    public GameObject partyInventoryPanel; //队伍背包面板
    public Button partyInventoryPanelButton; //队伍背包面板按钮

    public Transform inventoryContentParent; //背包内容父对象
    public GameObject characterBagPrefab; //角色背包预设体
    public GameObject itemSlotPrefab; //物品插槽预设体
    public GameObject itemImgPrefab; //物品图标预设体
    public GameObject sendButtonPrefab; //转交按钮预设体
    private float sendButtonHeight; //转交按钮高度

    public GameObject equipmentItemClickedPanel; //装备物品点击面板
    public GameObject magicItemClickedPanel; //魔法物品点击面板
    public GameObject consumableItemClickedPanel; //消耗品点击面板
    private Button equipButton; //Equip按钮
    private TextMeshProUGUI equipButtonText; //Equip按钮文本
    private Button learnSkillButton; //LearSkill按钮
    private TextMeshProUGUI learnSkillButtonText; //LearnSkill按钮文本
    private Button consumeButton; //Consume按钮
    private TextMeshProUGUI consumeButtonText; //Consume按钮文本


    public Button hideInventoryPanelButton; //隐藏装备面板按钮
    public Button filterAllButton; //过滤全部物品按钮
    public Button filterEquipmentButton; //过滤装备物品按钮
    public Button filterMagicButton; //过滤魔法物品按钮
    public Button filterConsumableButton; //过滤消耗品按钮
    public Button sortButton; //排序面板按钮
    public Button sortByTypeButton; //按照类别排序按钮
    public Button sortByValueButton; //按照价值排序按钮
    public Button sortByWeightButton; //按照重量排序按钮
    public GameObject sortButtonClickedPanel; //排序面板

    public GameObject itemTip; //物品信息提示面板

    public float slotHeight = 55.0f; //插槽格子高度
    private GameObject nullBag; //空背包, 用作更新插槽列数
    private int columns; //背包插槽列数

    private Dictionary<Character, IndividualBag> individualBagMap = new Dictionary<Character, IndividualBag>(); //key:角色 value:对应个人背包
    private Dictionary<GameObject, Character> bagSlotMap = new Dictionary<GameObject, Character>(); //key:背包插槽 value:背包插槽所属背包对应的角色
    private Dictionary<GameObject, ItemBaseData> itemMap = new Dictionary<GameObject, ItemBaseData>(); //key:物品图标 value:物品对应的ScriptableObject数据

    private GameObject clickedItem; //右键所点击的物品

    private Transform beginDragSlot; //开始拖拽物品时, 物品所属的插槽格子
    private GameObject draggingItem; //所拖拽物品
    private GameObject draggingItemWithoutBG; //拖拽物品无背景版

    /// <summary>
    /// 每位角色的个人背包类
    /// </summary>
    private class IndividualBag
    {
        public GameObject bag; //个人背包
        public List<GameObject> bagSlot = new List<GameObject>(); //个人背包插槽
        public int lastItemIndex = -1; //个人背包中的最后一件物品的index, 背包中没有物品时记为-1
    }

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        sendButtonHeight = sendButtonPrefab.GetComponent<RectTransform>().sizeDelta.y;
        Debug.Log($"sendButtonHeight {sendButtonHeight}");
        InitializePartyInventoryUI(); //初始化背包界面UI
        BindFixedButtons(); //为筛选、排序、点击面板的固定按钮绑定事件
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// 更新队伍背包面板可见性
    /// </summary>
    public void UpdatePartyInventoryPanelVisibility()
    {
        if (partyInventoryPanel.activeSelf)
        {
            partyInventoryPanel.SetActive(false);
        }
        else
        {
            partyInventoryPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 更新背包插槽数
    /// </summary>
    private void UpdateSlotColumns()
    {
        Transform itemSlotParent = nullBag.transform.Find("ItemSlotParent").transform;
        GridLayoutGroup grid = itemSlotParent.GetComponent<GridLayoutGroup>(); //得到characterBag上的GridLayoutGroup脚本
        float availableWidth = (itemSlotParent as RectTransform).rect.width; //计算可使用范围宽度
        columns = Mathf.FloorToInt(availableWidth / (grid.cellSize.x + grid.spacing.x)); //计算列数
        //Debug.Log($"availableWidth = {availableWidth}");
        //Debug.Log($"gridWidth = {grid.cellSize.x + grid.spacing.x}");
        //Debug.Log($"columns = {columns}");
    }

    /// <summary>
    /// 初始化背包面板UI
    /// </summary>
    private void InitializePartyInventoryUI()
    {
        StartCoroutine(InitializePartyInventoryUICoroutine());
    }

    /// <summary>
    /// 初始化背包面板UI协程方法
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializePartyInventoryUICoroutine()
    {
        //等待队伍系统完成初始化
        yield return new WaitWhile(() => PartyManager.Instance == null || !PartyManager.Instance.groupsInitialized);
        
        //初始化空背包 和 插槽列数
        nullBag = Instantiate(characterBagPrefab, inventoryContentParent);
        nullBag.SetActive(false);
        UpdateSlotColumns();

        float bagHeight = (characterBagPrefab.transform as RectTransform).sizeDelta.y; //得到角色背包高度
        Vector2 uiPos = Vector2.zero;
        //先建立角色与个人背包之间的对应关系, 防止在初始化队伍角色背包面板的过程中, 先创建背包的角色在更新插槽格子数量以及布局队伍背包UI的过程中再次创建后面等待初始化角色的背包, 造成重复
        foreach (LinkedList<Character> group in PartyManager.Instance.groups)
        {
            if (group.Count == 0)
            {
                continue;
            }
            foreach (Character character in group)
            {
                individualBagMap.Add(character, new IndividualBag());
            }
        }
        //再创建具体的个人背包UI
        foreach (LinkedList<Character> group in PartyManager.Instance.groups)
        {
            if (group.Count == 0)
            {
                continue;
            }
            foreach (Character character in group)
            {
                //等待角色完成初始化
                yield return new WaitWhile(() => !character.characterInitialized);

                CreateCharacterBag(character, uiPos);

                //uiPos.y -= bagHeight;
                uiPos.y -= (individualBagMap[character].bag.transform as RectTransform).sizeDelta.y; //更新个人背包ui位置
                yield return null;
            }
        }
        //RelayoutPartyInventoryUIByCharacterSort();
    }

    /// <summary>
    /// 创建对应角色的背包
    /// </summary>
    /// <param name="character"></param>
    /// <param name="uiPos"></param>
    private void CreateCharacterBag(Character character, Vector2 uiPos)
    {
        //初始化每个角色的个人背包类, 创建对应的背包UI和插槽UI
        //IndividualBag individualBag = new IndividualBag();
        IndividualBag individualBag;
        if (individualBagMap.ContainsKey(character))
        {
            individualBag = individualBagMap[character];
        }
        else
        {
            individualBag = new IndividualBag();
            individualBagMap.Add(character, individualBag);
        }

        individualBag.bag = Instantiate(characterBagPrefab, inventoryContentParent);
        (individualBag.bag.transform as RectTransform).anchoredPosition = uiPos;
        individualBag.bag.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = $"{character.info.name}的背包";

        Transform itemSlotParent = individualBag.bag.transform.Find("ItemSlotParent").transform;
        //每个背包初始插槽只有2行
        for (int i = 0; i <= 1; i++)
        {
            for(int j = 0; j < columns; j++)
            {
                GameObject slot = Instantiate(itemSlotPrefab, itemSlotParent);
                slot.name = "ItemSlot";
                individualBag.bagSlot.Add(slot);

                bagSlotMap.Add(slot, character); //建立插槽和角色的对应关系
            }
        }
        //individualBagMap.Add(character, individualBag); //建立角色和个人背包的对应关系
        //初始化每个角色的背包UI
        InitializeBagItemsOnUI(character);
    }

    /// <summary>
    /// 初始化个人背包bag的物品item的UI
    /// </summary>
    private void InitializeBagItemsOnUI(Character character)
    {
        List<ItemBaseData> bagItems = character.bagItems;
        IndividualBag characterBag = individualBagMap[character];
        for(int i = 0; i < bagItems.Count; i++)
        {
            if(i < characterBag.bagSlot.Count)
            {
                //实例化item的icon图标
                GameObject item = Instantiate(itemImgPrefab, characterBag.bagSlot[i].transform);
                item.GetComponent<Image>().sprite = bagItems[i].icon; //将item的Image图片设置为对应icon
                itemMap.Add(item, bagItems[i]); //建立物品图标UI 与 物品ScriptableObject数据 之间的关系

                //为item的icon图标添加EventTrigger响应事件
                EventTrigger trigger = item.GetComponent<EventTrigger>();
                AddEvent(trigger, EventTriggerType.PointerClick, OnItemIconClicked);
                AddEvent(trigger, EventTriggerType.BeginDrag, OnItemIconBeginDrag);
                AddEvent(trigger, EventTriggerType.Drag, OnItemIconDrag);
                AddEvent(trigger, EventTriggerType.EndDrag, OnItemIconEndDrag);
                AddEvent(trigger, EventTriggerType.PointerEnter, OnItemIconPointerEnter);
                AddEvent(trigger, EventTriggerType.PointerExit, OnItemIconPointerExit);

                //根据lastItem检查插槽格子数量
                characterBag.lastItemIndex = i; //更新最后一件物品的下标索引
                CheckAndUpdateBagSlot(character);
            }
        }
    }

    /// <summary>
    /// 初始化个人背包bag中类型为type的物品item的UI
    /// </summary>
    private void InitializeBagItemsOnUI(Character character, Type type)
    {
        List<ItemBaseData> bagItems = character.bagItems;
        IndividualBag characterBag = individualBagMap[character];
        for (int i = 0, j = 0; i < bagItems.Count && j < characterBag.bagSlot.Count; i++)
        {
            if (bagItems[i].GetType() == type) //筛选物品类型type
            {
                //Debug.Log($"Type is {type}");
                //实例化item的icon图标
                GameObject item = Instantiate(itemImgPrefab, characterBag.bagSlot[j].transform);
                item.GetComponent<Image>().sprite = bagItems[i].icon; //将item的Image图片设置为对应icon
                itemMap.Add(item, bagItems[i]); //建立物品图标UI 与 物品ScriptableObject数据 之间的关系

                //为item的icon图标添加EventTrigger响应事件
                EventTrigger trigger = item.GetComponent<EventTrigger>();
                AddEvent(trigger, EventTriggerType.PointerClick, OnItemIconClicked);
                AddEvent(trigger, EventTriggerType.BeginDrag, OnItemIconBeginDrag);
                AddEvent(trigger, EventTriggerType.Drag, OnItemIconDrag);
                AddEvent(trigger, EventTriggerType.EndDrag, OnItemIconEndDrag);
                AddEvent(trigger, EventTriggerType.PointerEnter, OnItemIconPointerEnter);
                AddEvent(trigger, EventTriggerType.PointerExit, OnItemIconPointerExit);

                //根据lastItem检查插槽格子数量
                characterBag.lastItemIndex = j; //更新最后一件物品的下标索引
                CheckAndUpdateBagSlot(character);

                j++;
            }
        }
    }

    /// <summary>
    /// 检查和更新个人背包的插槽格子数量
    /// </summary>
    /// <param name="bag"></param>    
    private void CheckAndUpdateBagSlot(Character character)
    {
        IndividualBag characterBag = individualBagMap[character]; //得到角色character的个人背包bag
        //计算得到背包中应有的插槽格子数量
        //如果最后一件物品的下标索引为-1, 说明当前没有物品在个人背包中, 则只需要两行格子即可; 如果存在最后一件物品, 则格子数量需要满足比最后一件物品行数多一行
        int lastItemRow = characterBag.lastItemIndex / columns; //计算得到个人背包bag中最后一件物品所在行数
        int targetRow = lastItemRow + 1; //计算得到背包中插槽应有的行数, 即lastItemRow+1
        int count = characterBag.bagSlot.Count; //当前格子数量
        int currentRows = count / columns; //当前格子总的行数
        int targetSlotCount = characterBag.lastItemIndex == -1 ? columns * 2 : columns * (targetRow + 1); //目标格子数量
        if(characterBag.bagSlot.Count > targetSlotCount)
        {
            //Debug.Log("UpdateBagSlot");
            //如果当前插槽格子数量大于目标数量
            for(int i = count - 1; i >= targetSlotCount; i--)
            {
                GameObject slot = characterBag.bagSlot[i];
                characterBag.bagSlot.RemoveAt(i);
                bagSlotMap.Remove(slot);
                Destroy(slot);
            }
        }
        else if(characterBag.bagSlot.Count < targetSlotCount)
        {
            //Debug.Log("UpdateBagSlot");
            //如果当前插槽格子数量小于目标数量, 就补齐
            Transform itemSlotParent = characterBag.bag.transform.Find("ItemSlotParent").transform;
            for (int i = count; i < targetSlotCount; i++)
            {
                GameObject slot = Instantiate(itemSlotPrefab, itemSlotParent);
                slot.name = "ItemSlot";
                characterBag.bagSlot.Add(slot);

                bagSlotMap.Add(slot, character); //建立插槽和角色的对应关系
            }
        }
        //如果bag.bagSlot.Count == targetSlotCount, 则无需更新插槽格子数量
        else
        {
            return;
        }
        //相应地降低/增加个人背包和队伍背包的高度
        RectTransform rect = characterBag.bag.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + (targetRow - currentRows + 1) * slotHeight);

        RectTransform content = inventoryContentParent.GetComponent<RectTransform>();
        content.sizeDelta = new Vector2(content.sizeDelta.x, content.sizeDelta.y + (targetRow - currentRows + 1) * slotHeight);

        //如果个人背包高度发生了变动, 则需要重新布局队伍成员背包位置
        RelayoutPartyInventoryUIByCharacterSort();
    }

    /// <summary>
    /// 更新角色character的个人背包中的lastItemIndex, 如果发生变动, 则更新个人背包的插槽格子数量
    /// </summary>
    /// <param name="character"></param>
    private void UpdateLastItemIndexAndBagSlot(Character character)
    {
        IndividualBag characterBag = individualBagMap[character]; //得到角色character的个人背包bag
        int currentLastItemIndex = -1; //当前characterBag中最后一件物品的下标索引
        for(int i = 0; i < characterBag.bagSlot.Count; i++)
        {
            if (characterBag.bagSlot[i].transform.childCount > 0)
            {
                currentLastItemIndex = i;
            }
        }
        //Debug.Log($"UpdateLastItemIndex {currentLastItemIndex}");
        //如果最后一件物品的下标索引发生变动, 就更新character的个人背包的插槽格子数量
        if(currentLastItemIndex != characterBag.lastItemIndex)
        {
            characterBag.lastItemIndex = currentLastItemIndex;
            CheckAndUpdateBagSlot(character);
        }
    }

    /// <summary>
    /// 根据角色顺序和角色个人背包高度重新排布队伍背包UI
    /// </summary>
    private void RelayoutPartyInventoryUIByCharacterSort()
    {
        Vector2 uiPos = Vector2.zero;
        foreach (LinkedList<Character> group in PartyManager.Instance.groups)
        {
            if (group.Count == 0)
            {
                continue;
            }
            foreach (Character character in group)
            {
                //如果已经存在该角色的个人背包, 则只需要调整位置
                if (individualBagMap.ContainsKey(character))
                {
                    GameObject bag = individualBagMap[character].bag;
                    if(bag != null)
                    {
                        RectTransform rect = bag.GetComponent<RectTransform>();
                        rect.anchoredPosition = uiPos;

                        uiPos.y -= rect.sizeDelta.y; //更新背包UI位置
                    }
                }
                //如果还没有该角色的背包, 则需要创建该角色的个人背包
                else
                {
                    CreateCharacterBag(character, uiPos);

                    uiPos.y -= (individualBagMap[character].bag.transform as RectTransform).sizeDelta.y;
                }
            }
        }
    }

    /// <summary>
    /// (为物品图标)添加事件处理
    /// </summary>
    /// <param name="trigger"></param>
    /// <param name="type"></param>
    /// <param name="action"></param>
    private void AddEvent(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action)
    {
        //声明 对应事件类型(比如EventTriggerType.BeginDrag, EventTriggerType.EndDrag等) 的事件对象
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        //关联监听函数
        entry.callback.AddListener((data) => action(data));
        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// 物品图标点击响应函数
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
        
        clickedItem = pointerData.pointerCurrentRaycast.gameObject;
        if (!itemMap.ContainsKey(clickedItem))
        {
            return;
        }
        ItemBaseData item = itemMap[clickedItem];
        GameObject clickedPanel = null; //点击面板
        if(item is EquipmentItemData)
        {
            //Debug.Log("Show EquipmentItem ClickedPanel");
            clickedPanel = equipmentItemClickedPanel;
            equipButtonText.text = $"装备 ({PartyManager.Instance.leader.info.name})";
        }
        else if(item is MagicItemData)
        {
            //Debug.Log("Show MagicItem ClickedPanel");
            clickedPanel = magicItemClickedPanel;
            learnSkillButtonText.text = $"学习技能 ({PartyManager.Instance.leader.info.name})";
        }
        else if(item is ConsumableItemData)
        {
            //Debug.Log("Show ConsumableItem ClickedPanel");
            clickedPanel = consumableItemClickedPanel;
            consumeButtonText.GetComponent<TextMeshProUGUI>().text = $"使用 ({PartyManager.Instance.leader.info.name})";
        }
        if (clickedPanel == null)
        {
            Debug.Log("ClickedPanel null");
            return;
        }

        //获取SendButton的父对象
        Transform sendButtonParent = clickedPanel.transform.Find("SendButtonParent");
        //先销毁之前的SendButton
        for (int i = sendButtonParent.childCount - 1; i >= 0; i--)
        {
            Destroy(sendButtonParent.GetChild(i).gameObject);
            (clickedPanel.transform as RectTransform).sizeDelta -= new Vector2(0, sendButtonHeight); //相应降低面板高度
        }
        Character clickedItemOwner = bagSlotMap[clickedItem.transform.parent.gameObject]; //所点击物品的拥有者
        //创建新的SendButton
        foreach (LinkedList<Character> group in PartyManager.Instance.groups)
        {
            if (group.Count == 0)
            {
                continue;
            }
            foreach (Character character in group)
            {
                //跳过自己, 即clickedItem所在插槽格子的所属角色
                if(character == clickedItemOwner)
                {
                    continue;
                }

                //实例化sendButton预设体并设置文本
                GameObject btn = Instantiate(sendButtonPrefab, sendButtonParent);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = $"给 {character.info.name}";

                //绑定点击事件, 传递Send目标角色
                Button button = btn.GetComponent<Button>();
                button.onClick.AddListener(() => OnSendButtonClicked(clickedItemOwner, character));

                (clickedPanel.transform as RectTransform).sizeDelta += new Vector2(0, sendButtonHeight); //相应增加面板高度
            }
        }

        clickedPanel.transform.position = pointerData.position; //移动面板位置
        clickedPanel.SetActive(true); //显示点击面板
    }

    /// <summary>
    /// 物品图标开始拖拽响应函数
    /// </summary>
    /// <param name="data"></param>
    private void OnItemIconBeginDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        draggingItem = pointerData.pointerDrag; //记录拖拽的物品UI图标
        beginDragSlot = draggingItem.transform.parent.transform; //记录开始拖拽时, 物品所属的插槽格子, 方便后续处理 1最后没有拖拽到格子上时 2拖拽到的新格子有物品 两种情况
        
        draggingItemWithoutBG = Instantiate(itemImgPrefab, beginDragSlot); //实例化物品无背景UI图标
        Image itemImgWithoutBG = draggingItemWithoutBG.GetComponent<Image>();
        itemImgWithoutBG.sprite = itemMap[draggingItem].iconWithoutBG; //设置无背景图片
        Color draggingColor = itemImgWithoutBG.color;
        draggingColor.a = 0.75f; //设置透明度
        itemImgWithoutBG.color = draggingColor;
        draggingItemWithoutBG.transform.SetParent(partyInventoryPanel.transform); //暂时提高draggingItemWithoutBG的层级, 防止被其他UI遮挡
    }

    /// <summary>
    /// 物品图标拖拽中响应函数
    /// </summary>
    /// <param name="data"></param>
    private void OnItemIconDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;
        draggingItemWithoutBG.GetComponent<RectTransform>().anchoredPosition += pointerData.delta / GetComponentInParent<Canvas>().scaleFactor; //实现物品无背景UI图标随鼠标移动
    }


    /// <summary>
    /// 物品图标结束拖拽响应函数
    /// </summary>
    /// <param name="data"></param>
    private void OnItemIconEndDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        Destroy(draggingItemWithoutBG); //销毁物品无背景UI图标
        draggingItemWithoutBG = null;

        GameObject targetSlot = null; //拖动结束时鼠标所在的插槽格子
        List<RaycastResult> results = GetRaycastResults(); //当前鼠标所在位置的射线检测得到的所有UI元素

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.name == "ItemSlot") //如果检测到插槽格子
            {
                targetSlot = result.gameObject;
                break;
            }
        }

        //如果没有检测到插槽格子 或者 目标插槽格子和拖拽物品原所属格子相同
        if(targetSlot == null || targetSlot == beginDragSlot)
        {
            //将拖拽物品的位置直接赋值在原来所在格子位置
            draggingItem.transform.SetParent(beginDragSlot.transform);
            (draggingItem.transform as RectTransform).anchoredPosition = Vector2.zero;

            //由于拖拽物品物归原位一定不会影响物品拥有者个人背包的lastItemIndex, 因此无需调整背包UI
            return;
        }

        //如果目标插槽格子并不存在物品
        if(targetSlot.transform.childCount == 0)
        {
            //将拖拽物品设置为目标格子的子对象 并重置位置
            draggingItem.transform.SetParent(targetSlot.transform);
            (draggingItem.transform as RectTransform).anchoredPosition = Vector2.zero;
            
            Character ownCharacter = bagSlotMap[beginDragSlot.gameObject]; //拖拽物品的原先拥有者
            Character targetCharacter = bagSlotMap[targetSlot]; //目标插槽格子所属的角色
            UpdateLastItemIndexAndBagSlot(ownCharacter); //调整原先拥有者的背包UI

            //如果目标格子所属角色 不是 原先拥有者, 就调整物品在背包中的所属关系
            if (ownCharacter != targetCharacter)
            {
                UpdateLastItemIndexAndBagSlot(targetCharacter); //调整目标插槽格子所属角色的背包UI

                if (itemMap.ContainsKey(draggingItem))
                {
                    var item = itemMap[draggingItem];
                    ownCharacter.bagItems.Remove(item); //从原先所有者的背包中移除
                    targetCharacter.bagItems.Add(item); //添加进目标插槽格子所属角色的背包
                }
            }
            //如果目标格子所属角色 就是 原先拥有者, 就无需调整角色背包
        }
        //如果目标插槽格子存在物品
        else
        {
            //得到目标插槽格子中的 被交换的物品 的UI图标
            GameObject targetItem = targetSlot.transform.GetChild(0).gameObject;
            //将被交换物品的位置赋值在开始拖拽时插槽格子的位置
            targetItem.transform.SetParent(beginDragSlot);
            (targetItem.transform as RectTransform).anchoredPosition = Vector2.zero;

            //将拖拽物品设置为目标格子的子对象 并重置位置
            draggingItem.transform.SetParent(targetSlot.transform);
            (draggingItem.transform as RectTransform).anchoredPosition = Vector2.zero;

            Character ownCharacter = bagSlotMap[beginDragSlot.gameObject]; //拖拽物品的原先拥有者
            Character targetCharacter = bagSlotMap[targetSlot]; //目标插槽格子所属的角色

            //由于交换物品一定不会影响ownCharacter和targetCharacter个人背包的lastItemIndex, 因此也无需调整背包UI

            //如果目标格子所属角色 不是 原先拥有者, 就调整物品在背包中的所属关系
            if (ownCharacter != targetCharacter)
            {
                if (itemMap.ContainsKey(draggingItem) && itemMap.ContainsKey(targetItem))
                {
                    var item = itemMap[draggingItem]; //拖拽物品
                    var swappedItem = itemMap[targetItem]; //被交换物品

                    ownCharacter.bagItems.Remove(item); //将拖拽物品 从原先所有者的背包中移除
                    targetCharacter.bagItems.Add(item); //将拖拽物品 添加进目标插槽格子所属角色的背包

                    targetCharacter.bagItems.Remove(swappedItem); //将被交换物品 从目标插槽格子所属角色的背包中移除
                    ownCharacter.bagItems.Add(swappedItem); //将被交换物品 添加进拖拽物品原先所有者的背包
                }
            }
            //如果目标格子所属角色 就是 原先拥有者, 就无需调整角色背包
        }
    }

    /// <summary>
    /// 物品图标指针(鼠标)进入响应函数
    /// </summary>
    /// <param name="item"></param>
    private void OnItemIconPointerEnter(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;
        GameObject itemIcon = pointerData.pointerEnter.gameObject;
        if(itemIcon == null || !itemMap.ContainsKey(itemIcon))
        {
            return;
        }
        ItemBaseData item = itemMap[itemIcon];
        itemTip.transform.Find("ItemIcon").GetComponent<Image>().sprite = item.icon;
        itemTip.transform.Find("ItemName").GetComponent<TextMeshProUGUI>().text = item.itemName;
        if (item is EquipmentItemData)
        {
            EquipmentItemData equipmentItem = item as EquipmentItemData;
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
        }
        else
        {
            itemTip.transform.Find("ItemValueAndWeight").GetComponent<TextMeshProUGUI>().text = $"价值: {item.value}   重量: {item.weight}";
        }
        itemTip.transform.Find("ItemDescription").GetComponent<TextMeshProUGUI>().text = item.description;

        itemTip.transform.position = itemIcon.transform.position + new Vector3(-25, -25);
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
    /// 通过射线检测找到鼠标位置下所有可交互的UI元素
    /// </summary>
    /// <returns></returns>
    private List<RaycastResult> GetRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current); //创建一个新的PointerEventData对象, 并将其与当前的事件系统关联
        eventData.position = Input.mousePosition; //将鼠标的当前位置赋值给PointerEventData的position属性, 用于后续的射线检测

        List<RaycastResult> results = new List<RaycastResult>(); //用于存储射线检测的结果
        GraphicRaycaster raycaster = this.GetComponentInParent<GraphicRaycaster>(); //获取当前UI对象所在的Canvas上的GraphicRaycaster组件
        raycaster.Raycast(eventData, results); //执行射线检测, 并将检测到的UI元素及其相关信息添加到results列表中

        return results;
    }

    /// <summary>
    /// 在角色character的背包中创建item相应的itemIcon图标
    /// </summary>
    /// <param name="character"></param>
    /// <param name="item"></param>
    public void AddItemToBag(Character character, ItemBaseData item)
    {
        IndividualBag characterBag = individualBagMap[character]; //得到角色character的个人背包characterBag
        for(int i = 0; i < characterBag.bagSlot.Count; i++)
        {
            if (characterBag.bagSlot[i].transform.childCount == 0)
            {
                //实例化item的icon图标
                GameObject itemIcon = Instantiate(itemImgPrefab, characterBag.bagSlot[i].transform);
                itemIcon.GetComponent<Image>().sprite = item.icon; //将item的Image图片设置为对应icon
                itemMap.Add(itemIcon, item); //建立物品图标UI 与 物品ScriptableObject数据 之间的关系

                //为item的icon图标添加EventTrigger响应事件
                EventTrigger trigger = itemIcon.GetComponent<EventTrigger>();
                AddEvent(trigger, EventTriggerType.PointerClick, OnItemIconClicked);
                AddEvent(trigger, EventTriggerType.BeginDrag, OnItemIconBeginDrag);
                AddEvent(trigger, EventTriggerType.Drag, OnItemIconDrag);
                AddEvent(trigger, EventTriggerType.EndDrag, OnItemIconEndDrag);
                AddEvent(trigger, EventTriggerType.PointerEnter, OnItemIconPointerEnter);
                AddEvent(trigger, EventTriggerType.PointerExit, OnItemIconPointerExit);

                //如果添加的新物品成为该角色背包中最后一件物品, 则更新lastItemIndex, 并检查更新背包插槽格子数量
                if(i > characterBag.lastItemIndex)
                {
                    characterBag.lastItemIndex = i;
                    CheckAndUpdateBagSlot(character);
                }
                break;
            }
        }
        //character.bagItems.Add(item); //将item加入到该角色Character脚本中背包物品数据列表
    }

    /// <summary>
    /// 从角色character的个人背包中移除item以及对应的itemIcon图标
    /// </summary>
    /// <param name="character"></param>
    /// <param name="itemIcon"></param>    
    public void RemoveItemFromBag(Character character, GameObject itemIcon)
    {
        character.bagItems.Remove(itemMap[itemIcon]); //将item从该角色Character脚本中背包物品数据列表中移除
        itemMap.Remove(itemIcon); //从itemMap中移除
        DestroyImmediate(itemIcon);
        UpdateLastItemIndexAndBagSlot(character); //检查更新character个人背包中最后一件物品的下标索引, 并检查更新插槽格子数量
    }

    /// <summary>
    /// 关闭所有点击面板
    /// </summary>
    private void HideAllClickedPanel()
    {
        equipmentItemClickedPanel.SetActive(false);
        magicItemClickedPanel.SetActive(false);
        consumableItemClickedPanel.SetActive(false);

        sortButtonClickedPanel.SetActive(false);
    }


    #region 按钮响应事件
    /// <summary>
    /// 绑定各种固定按钮事件
    /// </summary>
    private void BindFixedButtons()
    {
        //为隐藏背包面板按钮绑定点击响应事件
        hideInventoryPanelButton.onClick.AddListener(OnHideInventoryPanelButtonClicked);

        //为背包按钮绑定点击响应事件
        partyInventoryPanelButton.onClick.AddListener(UpdatePartyInventoryPanelVisibility);

        //为Filter按钮绑定点击响应事件
        filterAllButton.onClick.AddListener(OnFilterAllButtonClicked);
        filterEquipmentButton.onClick.AddListener(() => OnFilterButtonClicked(typeof(EquipmentItemData)));
        filterMagicButton.onClick.AddListener(() => OnFilterButtonClicked(typeof(MagicItemData)));
        filterConsumableButton.onClick.AddListener(() => OnFilterButtonClicked(typeof(ConsumableItemData)));

        //为排序按钮绑定点击响应事件
        sortButton.onClick.AddListener(OnSortButtonClicked);
        sortByTypeButton.onClick.AddListener(() => OnSortByButtonClicked("type"));
        sortByValueButton.onClick.AddListener(() => OnSortByButtonClicked("value"));
        sortByWeightButton.onClick.AddListener(() => OnSortByButtonClicked("weight"));

        //为装备面板的固定按钮绑定响应事件
        equipButton = equipmentItemClickedPanel.transform.Find("EquipButton").GetComponent<Button>(); //EquipButton
        equipButtonText = equipmentItemClickedPanel.transform.Find("EquipButton/Text (TMP)").GetComponent<TextMeshProUGUI>();
        equipButton.onClick.AddListener(OnEquipButtonClicked);
        equipmentItemClickedPanel.transform.Find("DropItemButton").GetComponent<Button>().onClick.AddListener(OnDropItemButtonClicked); //DropItemButton
        //为魔法面板的固定按钮绑定响应事件
        learnSkillButton = magicItemClickedPanel.transform.Find("LearnSkillButton").GetComponent<Button>(); //LearnSkillButton
        learnSkillButtonText = magicItemClickedPanel.transform.Find("LearnSkillButton/Text (TMP)").GetComponent<TextMeshProUGUI>();
        learnSkillButton.GetComponent<Button>().onClick.AddListener(OnLearnSkillButtonClicked);
        magicItemClickedPanel.transform.Find("DropItemButton").GetComponent<Button>().onClick.AddListener(OnDropItemButtonClicked); //DropItemButton
        //为消耗品面板的固定按钮绑定响应事件
        consumeButton = consumableItemClickedPanel.transform.Find("ConsumeButton").GetComponent<Button>(); //ConsumeButton
        consumeButtonText = consumableItemClickedPanel.transform.Find("ConsumeButton/Text (TMP)").GetComponent<TextMeshProUGUI>();
        consumeButton.GetComponent<Button>().onClick.AddListener(OnConsumeButtonClicked);
        consumableItemClickedPanel.transform.Find("DropItemButton").GetComponent<Button>().onClick.AddListener(OnDropItemButtonClicked); //DropItemButton
    }

    /// <summary>
    /// EquipButton点击事件
    /// </summary>
    private void OnEquipButtonClicked()
    {
        //确保点击物品不为空
        if(clickedItem == null)
        {
            return;
        }
        if(PartyManager.Instance.leader == null)
        {
            return;
        }
        Character leader = PartyManager.Instance.leader; //主控
        Character clickedItemOwner = bagSlotMap[clickedItem.transform.parent.gameObject]; //当前点击选中物品所属的角色
        EquipmentItemData item = itemMap[clickedItem] as EquipmentItemData;
        RemoveItemFromBag(clickedItemOwner, clickedItem); //将点击装备的UI图标以及装备信息从所属者背包和UI中分别移除
        leader.Equip(item); //调用主控leader角色脚本中的Equip函数, 将主控对应装备插槽设置为clickedItem对应装备, 更新角色info和角色面板UI
        HideAllClickedPanel();
    }

    /// <summary>
    /// LearnSkillButton点击事件
    /// </summary>
    private void OnLearnSkillButtonClicked()
    {
        if (PartyManager.Instance.leader == null)
        {
            return;
        }
        Character leader = PartyManager.Instance.leader; //主控
        Character clickedItemOwner = bagSlotMap[clickedItem.transform.parent.gameObject]; //当前点击选中物品所属的角色
        if (SkillManager.Instance.LearnSkill(leader, (itemMap[clickedItem] as MagicItemData).magicItemSkill)) //调用SkillManager中的LearnSkill函数
        {
            RemoveItemFromBag(clickedItemOwner, clickedItem); //如果学习成功, 将点击装备的UI图标以及装备信息从所属者背包和UI中分别移除
        }
        HideAllClickedPanel();
    }

    /// <summary>
    /// ConsumeButton点击事件
    /// </summary>
    private void OnConsumeButtonClicked()
    {
        if (PartyManager.Instance.leader == null)
        {
            return;
        }
        Character leader = PartyManager.Instance.leader; //主控
        Character clickedItemOwner = bagSlotMap[clickedItem.transform.parent.gameObject]; //当前点击选中物品所属的角色
        leader.Consume(itemMap[clickedItem] as ConsumableItemData); //调用主控leader角色脚本中的Consume函数, 根据消耗品属性更新角色info和角色面板UI
        RemoveItemFromBag(clickedItemOwner, clickedItem); //将点击装备的UI图标以及装备信息从所属者背包和UI中分别移除
        HideAllClickedPanel();
    }

    /// <summary>
    /// DropItemButton点击事件
    /// </summary>
    private void OnDropItemButtonClicked()
    {
        //确保点击物品不为空
        if (clickedItem == null)
        {
            return;
        }
        Character clickedItemOwner = bagSlotMap[clickedItem.transform.parent.gameObject]; //所点击物品的拥有者
        RemoveItemFromBag(clickedItemOwner, clickedItem); //将点击装备的UI图标以及装备信息从UI和背包中分别移除
        HideAllClickedPanel();
    }

    /// <summary>
    /// SendButton点击事件
    /// </summary>
    /// <param name="from">物品来源角色</param>
    /// <param name="to">物品要交给的角色对象</param>
    private void OnSendButtonClicked(Character from, Character to)
    {
        AddItemToBag(to, itemMap[clickedItem]); //将clickedItem添加进to个人背包
        to.bagItems.Add(itemMap[clickedItem]);
        RemoveItemFromBag(from, clickedItem); //将clickedItem从from个人背包中移除
        HideAllClickedPanel(); //关闭面板
    }
    
    /// <summary>
    /// FilterAllButton点击响应事件
    /// </summary>
    private void OnFilterAllButtonClicked()
    {
        HideAllClickedPanel();

        //生成新的物品图片前, 先清楚已经存在的物品UI图标
        foreach (GameObject itemIcon in itemMap.Keys.ToListPooled())
        {
            itemMap.Remove(itemIcon);
            Destroy(itemIcon);
        }

        List<Character> members = PartyManager.Instance.partyMembers;
        for (int i = 0; i < members.Count; i++)
        {
            InitializeBagItemsOnUI(members[i]);
        }
    }

    /// <summary>
    /// FilterEquipmentButton, FilterMagicButton, FilterConsumableButton点击响应事件
    /// </summary>
    /// <param name="type"></param>
    private void OnFilterButtonClicked(Type type)
    {
        HideAllClickedPanel();

        //生成新的物品图片前, 先清楚已经存在的物品UI图标
        foreach (GameObject itemIcon in itemMap.Keys.ToListPooled())
        {
            itemMap.Remove(itemIcon);
            Destroy(itemIcon);
        }

        List<Character> members = PartyManager.Instance.partyMembers;
        for(int i = 0; i < members.Count; i++)
        {
            InitializeBagItemsOnUI(members[i], type); //根据筛选的物品类型初始化角色背包UI
        }
    }

    /// <summary>
    /// SortButton点击响应事件
    /// </summary>
    private void OnSortButtonClicked()
    {
        HideAllClickedPanel();

        //更新排序面板可见性
        if (sortButtonClickedPanel.activeSelf)
        {
            sortButtonClickedPanel.SetActive(false);
        }
        else
        {
            sortButtonClickedPanel.SetActive(true);
        }
    }

    /// <summary>
    /// SortByTypeButton, SortByValueButton, SortByWeightButton点击响应按钮
    /// </summary>
    /// <param name="sortType"></param>
    private void OnSortByButtonClicked(string sortType)
    {
        HideAllClickedPanel();

        //生成新的物品图片前, 先清楚已经存在的物品UI图标
        foreach (GameObject itemIcon in itemMap.Keys.ToListPooled())
        {
            itemMap.Remove(itemIcon);
            Destroy(itemIcon);
        }

        List<Character> members = PartyManager.Instance.partyMembers;
        for (int i = 0; i < members.Count; i++)
        {
            members[i].SortBagItems(sortType); //按照sortType对角色物品背包bagItems进行排序
            InitializeBagItemsOnUI(members[i]); //根据筛选的物品类型初始化角色背包UI
        }
    }

    /// <summary>
    /// hideInventoryPanelButton点击响应事件
    /// </summary>
    private void OnHideInventoryPanelButtonClicked()
    {
        HideAllClickedPanel();
        partyInventoryPanel.SetActive(false); //关闭队伍背包面板
    }
    #endregion
}
