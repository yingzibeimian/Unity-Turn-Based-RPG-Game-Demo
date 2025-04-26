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

    public GameObject partyInventoryPanel; //���鱳�����
    public Button partyInventoryPanelButton; //���鱳����尴ť

    public Transform inventoryContentParent; //�������ݸ�����
    public GameObject characterBagPrefab; //��ɫ����Ԥ����
    public GameObject itemSlotPrefab; //��Ʒ���Ԥ����
    public GameObject itemImgPrefab; //��Ʒͼ��Ԥ����
    public GameObject sendButtonPrefab; //ת����ťԤ����
    private float sendButtonHeight; //ת����ť�߶�

    public GameObject equipmentItemClickedPanel; //װ����Ʒ������
    public GameObject magicItemClickedPanel; //ħ����Ʒ������
    public GameObject consumableItemClickedPanel; //����Ʒ������
    private Button equipButton; //Equip��ť
    private TextMeshProUGUI equipButtonText; //Equip��ť�ı�
    private Button learnSkillButton; //LearSkill��ť
    private TextMeshProUGUI learnSkillButtonText; //LearnSkill��ť�ı�
    private Button consumeButton; //Consume��ť
    private TextMeshProUGUI consumeButtonText; //Consume��ť�ı�


    public Button hideInventoryPanelButton; //����װ����尴ť
    public Button filterAllButton; //����ȫ����Ʒ��ť
    public Button filterEquipmentButton; //����װ����Ʒ��ť
    public Button filterMagicButton; //����ħ����Ʒ��ť
    public Button filterConsumableButton; //��������Ʒ��ť
    public Button sortButton; //������尴ť
    public Button sortByTypeButton; //�����������ť
    public Button sortByValueButton; //���ռ�ֵ����ť
    public Button sortByWeightButton; //������������ť
    public GameObject sortButtonClickedPanel; //�������

    public GameObject itemTip; //��Ʒ��Ϣ��ʾ���

    public float slotHeight = 55.0f; //��۸��Ӹ߶�
    private GameObject nullBag; //�ձ���, �������²������
    private int columns; //�����������

    private Dictionary<Character, IndividualBag> individualBagMap = new Dictionary<Character, IndividualBag>(); //key:��ɫ value:��Ӧ���˱���
    private Dictionary<GameObject, Character> bagSlotMap = new Dictionary<GameObject, Character>(); //key:������� value:�����������������Ӧ�Ľ�ɫ
    private Dictionary<GameObject, ItemBaseData> itemMap = new Dictionary<GameObject, ItemBaseData>(); //key:��Ʒͼ�� value:��Ʒ��Ӧ��ScriptableObject����

    private GameObject clickedItem; //�Ҽ����������Ʒ

    private Transform beginDragSlot; //��ʼ��ק��Ʒʱ, ��Ʒ�����Ĳ�۸���
    private GameObject draggingItem; //����ק��Ʒ
    private GameObject draggingItemWithoutBG; //��ק��Ʒ�ޱ�����

    /// <summary>
    /// ÿλ��ɫ�ĸ��˱�����
    /// </summary>
    private class IndividualBag
    {
        public GameObject bag; //���˱���
        public List<GameObject> bagSlot = new List<GameObject>(); //���˱������
        public int lastItemIndex = -1; //���˱����е����һ����Ʒ��index, ������û����Ʒʱ��Ϊ-1
    }

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        sendButtonHeight = sendButtonPrefab.GetComponent<RectTransform>().sizeDelta.y;
        Debug.Log($"sendButtonHeight {sendButtonHeight}");
        InitializePartyInventoryUI(); //��ʼ����������UI
        BindFixedButtons(); //Ϊɸѡ�����򡢵�����Ĺ̶���ť���¼�
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// ���¶��鱳�����ɼ���
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
    /// ���±��������
    /// </summary>
    private void UpdateSlotColumns()
    {
        Transform itemSlotParent = nullBag.transform.Find("ItemSlotParent").transform;
        GridLayoutGroup grid = itemSlotParent.GetComponent<GridLayoutGroup>(); //�õ�characterBag�ϵ�GridLayoutGroup�ű�
        float availableWidth = (itemSlotParent as RectTransform).rect.width; //�����ʹ�÷�Χ���
        columns = Mathf.FloorToInt(availableWidth / (grid.cellSize.x + grid.spacing.x)); //��������
        //Debug.Log($"availableWidth = {availableWidth}");
        //Debug.Log($"gridWidth = {grid.cellSize.x + grid.spacing.x}");
        //Debug.Log($"columns = {columns}");
    }

    /// <summary>
    /// ��ʼ���������UI
    /// </summary>
    private void InitializePartyInventoryUI()
    {
        StartCoroutine(InitializePartyInventoryUICoroutine());
    }

    /// <summary>
    /// ��ʼ���������UIЭ�̷���
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializePartyInventoryUICoroutine()
    {
        //�ȴ�����ϵͳ��ɳ�ʼ��
        yield return new WaitWhile(() => PartyManager.Instance == null || !PartyManager.Instance.groupsInitialized);
        
        //��ʼ���ձ��� �� �������
        nullBag = Instantiate(characterBagPrefab, inventoryContentParent);
        nullBag.SetActive(false);
        UpdateSlotColumns();

        float bagHeight = (characterBagPrefab.transform as RectTransform).sizeDelta.y; //�õ���ɫ�����߶�
        Vector2 uiPos = Vector2.zero;
        //�Ƚ�����ɫ����˱���֮��Ķ�Ӧ��ϵ, ��ֹ�ڳ�ʼ�������ɫ�������Ĺ�����, �ȴ��������Ľ�ɫ�ڸ��²�۸��������Լ����ֶ��鱳��UI�Ĺ������ٴδ�������ȴ���ʼ����ɫ�ı���, ����ظ�
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
        //�ٴ�������ĸ��˱���UI
        foreach (LinkedList<Character> group in PartyManager.Instance.groups)
        {
            if (group.Count == 0)
            {
                continue;
            }
            foreach (Character character in group)
            {
                //�ȴ���ɫ��ɳ�ʼ��
                yield return new WaitWhile(() => !character.characterInitialized);

                CreateCharacterBag(character, uiPos);

                //uiPos.y -= bagHeight;
                uiPos.y -= (individualBagMap[character].bag.transform as RectTransform).sizeDelta.y; //���¸��˱���uiλ��
                yield return null;
            }
        }
        //RelayoutPartyInventoryUIByCharacterSort();
    }

    /// <summary>
    /// ������Ӧ��ɫ�ı���
    /// </summary>
    /// <param name="character"></param>
    /// <param name="uiPos"></param>
    private void CreateCharacterBag(Character character, Vector2 uiPos)
    {
        //��ʼ��ÿ����ɫ�ĸ��˱�����, ������Ӧ�ı���UI�Ͳ��UI
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
        individualBag.bag.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = $"{character.info.name}�ı���";

        Transform itemSlotParent = individualBag.bag.transform.Find("ItemSlotParent").transform;
        //ÿ��������ʼ���ֻ��2��
        for (int i = 0; i <= 1; i++)
        {
            for(int j = 0; j < columns; j++)
            {
                GameObject slot = Instantiate(itemSlotPrefab, itemSlotParent);
                slot.name = "ItemSlot";
                individualBag.bagSlot.Add(slot);

                bagSlotMap.Add(slot, character); //������ۺͽ�ɫ�Ķ�Ӧ��ϵ
            }
        }
        //individualBagMap.Add(character, individualBag); //������ɫ�͸��˱����Ķ�Ӧ��ϵ
        //��ʼ��ÿ����ɫ�ı���UI
        InitializeBagItemsOnUI(character);
    }

    /// <summary>
    /// ��ʼ�����˱���bag����Ʒitem��UI
    /// </summary>
    private void InitializeBagItemsOnUI(Character character)
    {
        List<ItemBaseData> bagItems = character.bagItems;
        IndividualBag characterBag = individualBagMap[character];
        for(int i = 0; i < bagItems.Count; i++)
        {
            if(i < characterBag.bagSlot.Count)
            {
                //ʵ����item��iconͼ��
                GameObject item = Instantiate(itemImgPrefab, characterBag.bagSlot[i].transform);
                item.GetComponent<Image>().sprite = bagItems[i].icon; //��item��ImageͼƬ����Ϊ��Ӧicon
                itemMap.Add(item, bagItems[i]); //������Ʒͼ��UI �� ��ƷScriptableObject���� ֮��Ĺ�ϵ

                //Ϊitem��iconͼ�����EventTrigger��Ӧ�¼�
                EventTrigger trigger = item.GetComponent<EventTrigger>();
                AddEvent(trigger, EventTriggerType.PointerClick, OnItemIconClicked);
                AddEvent(trigger, EventTriggerType.BeginDrag, OnItemIconBeginDrag);
                AddEvent(trigger, EventTriggerType.Drag, OnItemIconDrag);
                AddEvent(trigger, EventTriggerType.EndDrag, OnItemIconEndDrag);
                AddEvent(trigger, EventTriggerType.PointerEnter, OnItemIconPointerEnter);
                AddEvent(trigger, EventTriggerType.PointerExit, OnItemIconPointerExit);

                //����lastItem����۸�������
                characterBag.lastItemIndex = i; //�������һ����Ʒ���±�����
                CheckAndUpdateBagSlot(character);
            }
        }
    }

    /// <summary>
    /// ��ʼ�����˱���bag������Ϊtype����Ʒitem��UI
    /// </summary>
    private void InitializeBagItemsOnUI(Character character, Type type)
    {
        List<ItemBaseData> bagItems = character.bagItems;
        IndividualBag characterBag = individualBagMap[character];
        for (int i = 0, j = 0; i < bagItems.Count && j < characterBag.bagSlot.Count; i++)
        {
            if (bagItems[i].GetType() == type) //ɸѡ��Ʒ����type
            {
                //Debug.Log($"Type is {type}");
                //ʵ����item��iconͼ��
                GameObject item = Instantiate(itemImgPrefab, characterBag.bagSlot[j].transform);
                item.GetComponent<Image>().sprite = bagItems[i].icon; //��item��ImageͼƬ����Ϊ��Ӧicon
                itemMap.Add(item, bagItems[i]); //������Ʒͼ��UI �� ��ƷScriptableObject���� ֮��Ĺ�ϵ

                //Ϊitem��iconͼ�����EventTrigger��Ӧ�¼�
                EventTrigger trigger = item.GetComponent<EventTrigger>();
                AddEvent(trigger, EventTriggerType.PointerClick, OnItemIconClicked);
                AddEvent(trigger, EventTriggerType.BeginDrag, OnItemIconBeginDrag);
                AddEvent(trigger, EventTriggerType.Drag, OnItemIconDrag);
                AddEvent(trigger, EventTriggerType.EndDrag, OnItemIconEndDrag);
                AddEvent(trigger, EventTriggerType.PointerEnter, OnItemIconPointerEnter);
                AddEvent(trigger, EventTriggerType.PointerExit, OnItemIconPointerExit);

                //����lastItem����۸�������
                characterBag.lastItemIndex = j; //�������һ����Ʒ���±�����
                CheckAndUpdateBagSlot(character);

                j++;
            }
        }
    }

    /// <summary>
    /// ���͸��¸��˱����Ĳ�۸�������
    /// </summary>
    /// <param name="bag"></param>    
    private void CheckAndUpdateBagSlot(Character character)
    {
        IndividualBag characterBag = individualBagMap[character]; //�õ���ɫcharacter�ĸ��˱���bag
        //����õ�������Ӧ�еĲ�۸�������
        //������һ����Ʒ���±�����Ϊ-1, ˵����ǰû����Ʒ�ڸ��˱�����, ��ֻ��Ҫ���и��Ӽ���; ����������һ����Ʒ, �����������Ҫ��������һ����Ʒ������һ��
        int lastItemRow = characterBag.lastItemIndex / columns; //����õ����˱���bag�����һ����Ʒ��������
        int targetRow = lastItemRow + 1; //����õ������в��Ӧ�е�����, ��lastItemRow+1
        int count = characterBag.bagSlot.Count; //��ǰ��������
        int currentRows = count / columns; //��ǰ�����ܵ�����
        int targetSlotCount = characterBag.lastItemIndex == -1 ? columns * 2 : columns * (targetRow + 1); //Ŀ���������
        if(characterBag.bagSlot.Count > targetSlotCount)
        {
            //Debug.Log("UpdateBagSlot");
            //�����ǰ��۸�����������Ŀ������
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
            //�����ǰ��۸�������С��Ŀ������, �Ͳ���
            Transform itemSlotParent = characterBag.bag.transform.Find("ItemSlotParent").transform;
            for (int i = count; i < targetSlotCount; i++)
            {
                GameObject slot = Instantiate(itemSlotPrefab, itemSlotParent);
                slot.name = "ItemSlot";
                characterBag.bagSlot.Add(slot);

                bagSlotMap.Add(slot, character); //������ۺͽ�ɫ�Ķ�Ӧ��ϵ
            }
        }
        //���bag.bagSlot.Count == targetSlotCount, ��������²�۸�������
        else
        {
            return;
        }
        //��Ӧ�ؽ���/���Ӹ��˱����Ͷ��鱳���ĸ߶�
        RectTransform rect = characterBag.bag.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + (targetRow - currentRows + 1) * slotHeight);

        RectTransform content = inventoryContentParent.GetComponent<RectTransform>();
        content.sizeDelta = new Vector2(content.sizeDelta.x, content.sizeDelta.y + (targetRow - currentRows + 1) * slotHeight);

        //������˱����߶ȷ����˱䶯, ����Ҫ���²��ֶ����Ա����λ��
        RelayoutPartyInventoryUIByCharacterSort();
    }

    /// <summary>
    /// ���½�ɫcharacter�ĸ��˱����е�lastItemIndex, ��������䶯, ����¸��˱����Ĳ�۸�������
    /// </summary>
    /// <param name="character"></param>
    private void UpdateLastItemIndexAndBagSlot(Character character)
    {
        IndividualBag characterBag = individualBagMap[character]; //�õ���ɫcharacter�ĸ��˱���bag
        int currentLastItemIndex = -1; //��ǰcharacterBag�����һ����Ʒ���±�����
        for(int i = 0; i < characterBag.bagSlot.Count; i++)
        {
            if (characterBag.bagSlot[i].transform.childCount > 0)
            {
                currentLastItemIndex = i;
            }
        }
        //Debug.Log($"UpdateLastItemIndex {currentLastItemIndex}");
        //������һ����Ʒ���±����������䶯, �͸���character�ĸ��˱����Ĳ�۸�������
        if(currentLastItemIndex != characterBag.lastItemIndex)
        {
            characterBag.lastItemIndex = currentLastItemIndex;
            CheckAndUpdateBagSlot(character);
        }
    }

    /// <summary>
    /// ���ݽ�ɫ˳��ͽ�ɫ���˱����߶������Ų����鱳��UI
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
                //����Ѿ����ڸý�ɫ�ĸ��˱���, ��ֻ��Ҫ����λ��
                if (individualBagMap.ContainsKey(character))
                {
                    GameObject bag = individualBagMap[character].bag;
                    if(bag != null)
                    {
                        RectTransform rect = bag.GetComponent<RectTransform>();
                        rect.anchoredPosition = uiPos;

                        uiPos.y -= rect.sizeDelta.y; //���±���UIλ��
                    }
                }
                //�����û�иý�ɫ�ı���, ����Ҫ�����ý�ɫ�ĸ��˱���
                else
                {
                    CreateCharacterBag(character, uiPos);

                    uiPos.y -= (individualBagMap[character].bag.transform as RectTransform).sizeDelta.y;
                }
            }
        }
    }

    /// <summary>
    /// (Ϊ��Ʒͼ��)����¼�����
    /// </summary>
    /// <param name="trigger"></param>
    /// <param name="type"></param>
    /// <param name="action"></param>
    private void AddEvent(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action)
    {
        //���� ��Ӧ�¼�����(����EventTriggerType.BeginDrag, EventTriggerType.EndDrag��) ���¼�����
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        //������������
        entry.callback.AddListener((data) => action(data));
        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// ��Ʒͼ������Ӧ����
    /// </summary>
    /// <param name="data"></param>
    private void OnItemIconClicked(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;
        HideAllClickedPanel(); //�ر����е�����
        //��������Ҽ����, �Ͳ���Ӧ
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
        GameObject clickedPanel = null; //������
        if(item is EquipmentItemData)
        {
            //Debug.Log("Show EquipmentItem ClickedPanel");
            clickedPanel = equipmentItemClickedPanel;
            equipButtonText.text = $"װ�� ({PartyManager.Instance.leader.info.name})";
        }
        else if(item is MagicItemData)
        {
            //Debug.Log("Show MagicItem ClickedPanel");
            clickedPanel = magicItemClickedPanel;
            learnSkillButtonText.text = $"ѧϰ���� ({PartyManager.Instance.leader.info.name})";
        }
        else if(item is ConsumableItemData)
        {
            //Debug.Log("Show ConsumableItem ClickedPanel");
            clickedPanel = consumableItemClickedPanel;
            consumeButtonText.GetComponent<TextMeshProUGUI>().text = $"ʹ�� ({PartyManager.Instance.leader.info.name})";
        }
        if (clickedPanel == null)
        {
            Debug.Log("ClickedPanel null");
            return;
        }

        //��ȡSendButton�ĸ�����
        Transform sendButtonParent = clickedPanel.transform.Find("SendButtonParent");
        //������֮ǰ��SendButton
        for (int i = sendButtonParent.childCount - 1; i >= 0; i--)
        {
            Destroy(sendButtonParent.GetChild(i).gameObject);
            (clickedPanel.transform as RectTransform).sizeDelta -= new Vector2(0, sendButtonHeight); //��Ӧ�������߶�
        }
        Character clickedItemOwner = bagSlotMap[clickedItem.transform.parent.gameObject]; //�������Ʒ��ӵ����
        //�����µ�SendButton
        foreach (LinkedList<Character> group in PartyManager.Instance.groups)
        {
            if (group.Count == 0)
            {
                continue;
            }
            foreach (Character character in group)
            {
                //�����Լ�, ��clickedItem���ڲ�۸��ӵ�������ɫ
                if(character == clickedItemOwner)
                {
                    continue;
                }

                //ʵ����sendButtonԤ���岢�����ı�
                GameObject btn = Instantiate(sendButtonPrefab, sendButtonParent);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = $"�� {character.info.name}";

                //�󶨵���¼�, ����SendĿ���ɫ
                Button button = btn.GetComponent<Button>();
                button.onClick.AddListener(() => OnSendButtonClicked(clickedItemOwner, character));

                (clickedPanel.transform as RectTransform).sizeDelta += new Vector2(0, sendButtonHeight); //��Ӧ�������߶�
            }
        }

        clickedPanel.transform.position = pointerData.position; //�ƶ����λ��
        clickedPanel.SetActive(true); //��ʾ������
    }

    /// <summary>
    /// ��Ʒͼ�꿪ʼ��ק��Ӧ����
    /// </summary>
    /// <param name="data"></param>
    private void OnItemIconBeginDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        draggingItem = pointerData.pointerDrag; //��¼��ק����ƷUIͼ��
        beginDragSlot = draggingItem.transform.parent.transform; //��¼��ʼ��קʱ, ��Ʒ�����Ĳ�۸���, ����������� 1���û����ק��������ʱ 2��ק�����¸�������Ʒ �������
        
        draggingItemWithoutBG = Instantiate(itemImgPrefab, beginDragSlot); //ʵ������Ʒ�ޱ���UIͼ��
        Image itemImgWithoutBG = draggingItemWithoutBG.GetComponent<Image>();
        itemImgWithoutBG.sprite = itemMap[draggingItem].iconWithoutBG; //�����ޱ���ͼƬ
        Color draggingColor = itemImgWithoutBG.color;
        draggingColor.a = 0.75f; //����͸����
        itemImgWithoutBG.color = draggingColor;
        draggingItemWithoutBG.transform.SetParent(partyInventoryPanel.transform); //��ʱ���draggingItemWithoutBG�Ĳ㼶, ��ֹ������UI�ڵ�
    }

    /// <summary>
    /// ��Ʒͼ����ק����Ӧ����
    /// </summary>
    /// <param name="data"></param>
    private void OnItemIconDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;
        draggingItemWithoutBG.GetComponent<RectTransform>().anchoredPosition += pointerData.delta / GetComponentInParent<Canvas>().scaleFactor; //ʵ����Ʒ�ޱ���UIͼ��������ƶ�
    }


    /// <summary>
    /// ��Ʒͼ�������ק��Ӧ����
    /// </summary>
    /// <param name="data"></param>
    private void OnItemIconEndDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        Destroy(draggingItemWithoutBG); //������Ʒ�ޱ���UIͼ��
        draggingItemWithoutBG = null;

        GameObject targetSlot = null; //�϶�����ʱ������ڵĲ�۸���
        List<RaycastResult> results = GetRaycastResults(); //��ǰ�������λ�õ����߼��õ�������UIԪ��

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.name == "ItemSlot") //�����⵽��۸���
            {
                targetSlot = result.gameObject;
                break;
            }
        }

        //���û�м�⵽��۸��� ���� Ŀ���۸��Ӻ���ק��Ʒԭ����������ͬ
        if(targetSlot == null || targetSlot == beginDragSlot)
        {
            //����ק��Ʒ��λ��ֱ�Ӹ�ֵ��ԭ�����ڸ���λ��
            draggingItem.transform.SetParent(beginDragSlot.transform);
            (draggingItem.transform as RectTransform).anchoredPosition = Vector2.zero;

            //������ק��Ʒ���ԭλһ������Ӱ����Ʒӵ���߸��˱�����lastItemIndex, ��������������UI
            return;
        }

        //���Ŀ���۸��Ӳ���������Ʒ
        if(targetSlot.transform.childCount == 0)
        {
            //����ק��Ʒ����ΪĿ����ӵ��Ӷ��� ������λ��
            draggingItem.transform.SetParent(targetSlot.transform);
            (draggingItem.transform as RectTransform).anchoredPosition = Vector2.zero;
            
            Character ownCharacter = bagSlotMap[beginDragSlot.gameObject]; //��ק��Ʒ��ԭ��ӵ����
            Character targetCharacter = bagSlotMap[targetSlot]; //Ŀ���۸��������Ľ�ɫ
            UpdateLastItemIndexAndBagSlot(ownCharacter); //����ԭ��ӵ���ߵı���UI

            //���Ŀ�����������ɫ ���� ԭ��ӵ����, �͵�����Ʒ�ڱ����е�������ϵ
            if (ownCharacter != targetCharacter)
            {
                UpdateLastItemIndexAndBagSlot(targetCharacter); //����Ŀ���۸���������ɫ�ı���UI

                if (itemMap.ContainsKey(draggingItem))
                {
                    var item = itemMap[draggingItem];
                    ownCharacter.bagItems.Remove(item); //��ԭ�������ߵı������Ƴ�
                    targetCharacter.bagItems.Add(item); //��ӽ�Ŀ���۸���������ɫ�ı���
                }
            }
            //���Ŀ�����������ɫ ���� ԭ��ӵ����, �����������ɫ����
        }
        //���Ŀ���۸��Ӵ�����Ʒ
        else
        {
            //�õ�Ŀ���۸����е� ����������Ʒ ��UIͼ��
            GameObject targetItem = targetSlot.transform.GetChild(0).gameObject;
            //����������Ʒ��λ�ø�ֵ�ڿ�ʼ��קʱ��۸��ӵ�λ��
            targetItem.transform.SetParent(beginDragSlot);
            (targetItem.transform as RectTransform).anchoredPosition = Vector2.zero;

            //����ק��Ʒ����ΪĿ����ӵ��Ӷ��� ������λ��
            draggingItem.transform.SetParent(targetSlot.transform);
            (draggingItem.transform as RectTransform).anchoredPosition = Vector2.zero;

            Character ownCharacter = bagSlotMap[beginDragSlot.gameObject]; //��ק��Ʒ��ԭ��ӵ����
            Character targetCharacter = bagSlotMap[targetSlot]; //Ŀ���۸��������Ľ�ɫ

            //���ڽ�����Ʒһ������Ӱ��ownCharacter��targetCharacter���˱�����lastItemIndex, ���Ҳ�����������UI

            //���Ŀ�����������ɫ ���� ԭ��ӵ����, �͵�����Ʒ�ڱ����е�������ϵ
            if (ownCharacter != targetCharacter)
            {
                if (itemMap.ContainsKey(draggingItem) && itemMap.ContainsKey(targetItem))
                {
                    var item = itemMap[draggingItem]; //��ק��Ʒ
                    var swappedItem = itemMap[targetItem]; //��������Ʒ

                    ownCharacter.bagItems.Remove(item); //����ק��Ʒ ��ԭ�������ߵı������Ƴ�
                    targetCharacter.bagItems.Add(item); //����ק��Ʒ ��ӽ�Ŀ���۸���������ɫ�ı���

                    targetCharacter.bagItems.Remove(swappedItem); //����������Ʒ ��Ŀ���۸���������ɫ�ı������Ƴ�
                    ownCharacter.bagItems.Add(swappedItem); //����������Ʒ ��ӽ���ק��Ʒԭ�������ߵı���
                }
            }
            //���Ŀ�����������ɫ ���� ԭ��ӵ����, �����������ɫ����
        }
    }

    /// <summary>
    /// ��Ʒͼ��ָ��(���)������Ӧ����
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
                rarityStr = $"<color=white>Ʒ��:��ͨ</color>";
            }
            else if (equipmentItem.rarity == Rarity.Uncommon)
            {
                rarityStr = $"<color=#00FF00>Ʒ��:����</color>";
            }
            else if (equipmentItem.rarity == Rarity.Rare)
            {
                rarityStr = $"<color=#00BFFF>Ʒ��:ϡ��</color>";
            }
            else if (equipmentItem.rarity == Rarity.Legendary)
            {
                rarityStr = $"<color=#9400D3>Ʒ��:��˵</color>";
            }
            else if (equipmentItem.rarity == Rarity.Unique)
            {
                rarityStr = $"<color=#DAA520>Ʒ��:����</color>";
            }

            string typeStr = "";
            if (equipmentItem.type == EquipmentType.Helmet)
            {
                typeStr = "ͷ��";
            }
            else if (equipmentItem.type == EquipmentType.Chest)
            {
                typeStr = "�ؼ�";
            }
            else if (equipmentItem.type == EquipmentType.Glowes)
            {
                typeStr = "����";
            }
            else if (equipmentItem.type == EquipmentType.Belt)
            {
                typeStr = "����";
            }
            else if (equipmentItem.type == EquipmentType.Boots)
            {
                typeStr = "ѥ��";
            }
            else if (equipmentItem.type == EquipmentType.Amulet)
            {
                typeStr = "����";
            }
            else if (equipmentItem.type == EquipmentType.Ring)
            {
                typeStr = "��ָ";
            }
            else if (equipmentItem.type == EquipmentType.Leggings)
            {
                typeStr = "����";
            }
            else if (equipmentItem.type == EquipmentType.Weapon)
            {
                typeStr = "����";
            }

            itemTip.transform.Find("ItemValueAndWeight").GetComponent<TextMeshProUGUI>().text = String.Format("{0} ����:{1} ��ֵ:{2} ����:{3}", rarityStr, typeStr, item.value, item.weight);
        }
        else
        {
            itemTip.transform.Find("ItemValueAndWeight").GetComponent<TextMeshProUGUI>().text = $"��ֵ: {item.value}   ����: {item.weight}";
        }
        itemTip.transform.Find("ItemDescription").GetComponent<TextMeshProUGUI>().text = item.description;

        itemTip.transform.position = itemIcon.transform.position + new Vector3(-25, -25);
        itemTip.SetActive(true);
    }

    /// <summary>
    /// ��Ʒͼ��ָ��(���)�˳���Ӧ����
    /// </summary>
    /// <param name="data"></param>
    private void OnItemIconPointerExit(BaseEventData data)
    {
        itemTip.SetActive(false);
    }

    /// <summary>
    /// ͨ�����߼���ҵ����λ�������пɽ�����UIԪ��
    /// </summary>
    /// <returns></returns>
    private List<RaycastResult> GetRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current); //����һ���µ�PointerEventData����, �������뵱ǰ���¼�ϵͳ����
        eventData.position = Input.mousePosition; //�����ĵ�ǰλ�ø�ֵ��PointerEventData��position����, ���ں��������߼��

        List<RaycastResult> results = new List<RaycastResult>(); //���ڴ洢���߼��Ľ��
        GraphicRaycaster raycaster = this.GetComponentInParent<GraphicRaycaster>(); //��ȡ��ǰUI�������ڵ�Canvas�ϵ�GraphicRaycaster���
        raycaster.Raycast(eventData, results); //ִ�����߼��, ������⵽��UIԪ�ؼ��������Ϣ��ӵ�results�б���

        return results;
    }

    /// <summary>
    /// �ڽ�ɫcharacter�ı����д���item��Ӧ��itemIconͼ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="item"></param>
    public void AddItemToBag(Character character, ItemBaseData item)
    {
        IndividualBag characterBag = individualBagMap[character]; //�õ���ɫcharacter�ĸ��˱���characterBag
        for(int i = 0; i < characterBag.bagSlot.Count; i++)
        {
            if (characterBag.bagSlot[i].transform.childCount == 0)
            {
                //ʵ����item��iconͼ��
                GameObject itemIcon = Instantiate(itemImgPrefab, characterBag.bagSlot[i].transform);
                itemIcon.GetComponent<Image>().sprite = item.icon; //��item��ImageͼƬ����Ϊ��Ӧicon
                itemMap.Add(itemIcon, item); //������Ʒͼ��UI �� ��ƷScriptableObject���� ֮��Ĺ�ϵ

                //Ϊitem��iconͼ�����EventTrigger��Ӧ�¼�
                EventTrigger trigger = itemIcon.GetComponent<EventTrigger>();
                AddEvent(trigger, EventTriggerType.PointerClick, OnItemIconClicked);
                AddEvent(trigger, EventTriggerType.BeginDrag, OnItemIconBeginDrag);
                AddEvent(trigger, EventTriggerType.Drag, OnItemIconDrag);
                AddEvent(trigger, EventTriggerType.EndDrag, OnItemIconEndDrag);
                AddEvent(trigger, EventTriggerType.PointerEnter, OnItemIconPointerEnter);
                AddEvent(trigger, EventTriggerType.PointerExit, OnItemIconPointerExit);

                //�����ӵ�����Ʒ��Ϊ�ý�ɫ���������һ����Ʒ, �����lastItemIndex, �������±�����۸�������
                if(i > characterBag.lastItemIndex)
                {
                    characterBag.lastItemIndex = i;
                    CheckAndUpdateBagSlot(character);
                }
                break;
            }
        }
        //character.bagItems.Add(item); //��item���뵽�ý�ɫCharacter�ű��б�����Ʒ�����б�
    }

    /// <summary>
    /// �ӽ�ɫcharacter�ĸ��˱������Ƴ�item�Լ���Ӧ��itemIconͼ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="itemIcon"></param>    
    public void RemoveItemFromBag(Character character, GameObject itemIcon)
    {
        character.bagItems.Remove(itemMap[itemIcon]); //��item�Ӹý�ɫCharacter�ű��б�����Ʒ�����б����Ƴ�
        itemMap.Remove(itemIcon); //��itemMap���Ƴ�
        DestroyImmediate(itemIcon);
        UpdateLastItemIndexAndBagSlot(character); //������character���˱��������һ����Ʒ���±�����, �������²�۸�������
    }

    /// <summary>
    /// �ر����е�����
    /// </summary>
    private void HideAllClickedPanel()
    {
        equipmentItemClickedPanel.SetActive(false);
        magicItemClickedPanel.SetActive(false);
        consumableItemClickedPanel.SetActive(false);

        sortButtonClickedPanel.SetActive(false);
    }


    #region ��ť��Ӧ�¼�
    /// <summary>
    /// �󶨸��̶ֹ���ť�¼�
    /// </summary>
    private void BindFixedButtons()
    {
        //Ϊ���ر�����尴ť�󶨵����Ӧ�¼�
        hideInventoryPanelButton.onClick.AddListener(OnHideInventoryPanelButtonClicked);

        //Ϊ������ť�󶨵����Ӧ�¼�
        partyInventoryPanelButton.onClick.AddListener(UpdatePartyInventoryPanelVisibility);

        //ΪFilter��ť�󶨵����Ӧ�¼�
        filterAllButton.onClick.AddListener(OnFilterAllButtonClicked);
        filterEquipmentButton.onClick.AddListener(() => OnFilterButtonClicked(typeof(EquipmentItemData)));
        filterMagicButton.onClick.AddListener(() => OnFilterButtonClicked(typeof(MagicItemData)));
        filterConsumableButton.onClick.AddListener(() => OnFilterButtonClicked(typeof(ConsumableItemData)));

        //Ϊ����ť�󶨵����Ӧ�¼�
        sortButton.onClick.AddListener(OnSortButtonClicked);
        sortByTypeButton.onClick.AddListener(() => OnSortByButtonClicked("type"));
        sortByValueButton.onClick.AddListener(() => OnSortByButtonClicked("value"));
        sortByWeightButton.onClick.AddListener(() => OnSortByButtonClicked("weight"));

        //Ϊװ�����Ĺ̶���ť����Ӧ�¼�
        equipButton = equipmentItemClickedPanel.transform.Find("EquipButton").GetComponent<Button>(); //EquipButton
        equipButtonText = equipmentItemClickedPanel.transform.Find("EquipButton/Text (TMP)").GetComponent<TextMeshProUGUI>();
        equipButton.onClick.AddListener(OnEquipButtonClicked);
        equipmentItemClickedPanel.transform.Find("DropItemButton").GetComponent<Button>().onClick.AddListener(OnDropItemButtonClicked); //DropItemButton
        //Ϊħ�����Ĺ̶���ť����Ӧ�¼�
        learnSkillButton = magicItemClickedPanel.transform.Find("LearnSkillButton").GetComponent<Button>(); //LearnSkillButton
        learnSkillButtonText = magicItemClickedPanel.transform.Find("LearnSkillButton/Text (TMP)").GetComponent<TextMeshProUGUI>();
        learnSkillButton.GetComponent<Button>().onClick.AddListener(OnLearnSkillButtonClicked);
        magicItemClickedPanel.transform.Find("DropItemButton").GetComponent<Button>().onClick.AddListener(OnDropItemButtonClicked); //DropItemButton
        //Ϊ����Ʒ���Ĺ̶���ť����Ӧ�¼�
        consumeButton = consumableItemClickedPanel.transform.Find("ConsumeButton").GetComponent<Button>(); //ConsumeButton
        consumeButtonText = consumableItemClickedPanel.transform.Find("ConsumeButton/Text (TMP)").GetComponent<TextMeshProUGUI>();
        consumeButton.GetComponent<Button>().onClick.AddListener(OnConsumeButtonClicked);
        consumableItemClickedPanel.transform.Find("DropItemButton").GetComponent<Button>().onClick.AddListener(OnDropItemButtonClicked); //DropItemButton
    }

    /// <summary>
    /// EquipButton����¼�
    /// </summary>
    private void OnEquipButtonClicked()
    {
        //ȷ�������Ʒ��Ϊ��
        if(clickedItem == null)
        {
            return;
        }
        if(PartyManager.Instance.leader == null)
        {
            return;
        }
        Character leader = PartyManager.Instance.leader; //����
        Character clickedItemOwner = bagSlotMap[clickedItem.transform.parent.gameObject]; //��ǰ���ѡ����Ʒ�����Ľ�ɫ
        EquipmentItemData item = itemMap[clickedItem] as EquipmentItemData;
        RemoveItemFromBag(clickedItemOwner, clickedItem); //�����װ����UIͼ���Լ�װ����Ϣ�������߱�����UI�зֱ��Ƴ�
        leader.Equip(item); //��������leader��ɫ�ű��е�Equip����, �����ض�Ӧװ���������ΪclickedItem��Ӧװ��, ���½�ɫinfo�ͽ�ɫ���UI
        HideAllClickedPanel();
    }

    /// <summary>
    /// LearnSkillButton����¼�
    /// </summary>
    private void OnLearnSkillButtonClicked()
    {
        if (PartyManager.Instance.leader == null)
        {
            return;
        }
        Character leader = PartyManager.Instance.leader; //����
        Character clickedItemOwner = bagSlotMap[clickedItem.transform.parent.gameObject]; //��ǰ���ѡ����Ʒ�����Ľ�ɫ
        if (SkillManager.Instance.LearnSkill(leader, (itemMap[clickedItem] as MagicItemData).magicItemSkill)) //����SkillManager�е�LearnSkill����
        {
            RemoveItemFromBag(clickedItemOwner, clickedItem); //���ѧϰ�ɹ�, �����װ����UIͼ���Լ�װ����Ϣ�������߱�����UI�зֱ��Ƴ�
        }
        HideAllClickedPanel();
    }

    /// <summary>
    /// ConsumeButton����¼�
    /// </summary>
    private void OnConsumeButtonClicked()
    {
        if (PartyManager.Instance.leader == null)
        {
            return;
        }
        Character leader = PartyManager.Instance.leader; //����
        Character clickedItemOwner = bagSlotMap[clickedItem.transform.parent.gameObject]; //��ǰ���ѡ����Ʒ�����Ľ�ɫ
        leader.Consume(itemMap[clickedItem] as ConsumableItemData); //��������leader��ɫ�ű��е�Consume����, ��������Ʒ���Ը��½�ɫinfo�ͽ�ɫ���UI
        RemoveItemFromBag(clickedItemOwner, clickedItem); //�����װ����UIͼ���Լ�װ����Ϣ�������߱�����UI�зֱ��Ƴ�
        HideAllClickedPanel();
    }

    /// <summary>
    /// DropItemButton����¼�
    /// </summary>
    private void OnDropItemButtonClicked()
    {
        //ȷ�������Ʒ��Ϊ��
        if (clickedItem == null)
        {
            return;
        }
        Character clickedItemOwner = bagSlotMap[clickedItem.transform.parent.gameObject]; //�������Ʒ��ӵ����
        RemoveItemFromBag(clickedItemOwner, clickedItem); //�����װ����UIͼ���Լ�װ����Ϣ��UI�ͱ����зֱ��Ƴ�
        HideAllClickedPanel();
    }

    /// <summary>
    /// SendButton����¼�
    /// </summary>
    /// <param name="from">��Ʒ��Դ��ɫ</param>
    /// <param name="to">��ƷҪ�����Ľ�ɫ����</param>
    private void OnSendButtonClicked(Character from, Character to)
    {
        AddItemToBag(to, itemMap[clickedItem]); //��clickedItem��ӽ�to���˱���
        to.bagItems.Add(itemMap[clickedItem]);
        RemoveItemFromBag(from, clickedItem); //��clickedItem��from���˱������Ƴ�
        HideAllClickedPanel(); //�ر����
    }
    
    /// <summary>
    /// FilterAllButton�����Ӧ�¼�
    /// </summary>
    private void OnFilterAllButtonClicked()
    {
        HideAllClickedPanel();

        //�����µ���ƷͼƬǰ, ������Ѿ����ڵ���ƷUIͼ��
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
    /// FilterEquipmentButton, FilterMagicButton, FilterConsumableButton�����Ӧ�¼�
    /// </summary>
    /// <param name="type"></param>
    private void OnFilterButtonClicked(Type type)
    {
        HideAllClickedPanel();

        //�����µ���ƷͼƬǰ, ������Ѿ����ڵ���ƷUIͼ��
        foreach (GameObject itemIcon in itemMap.Keys.ToListPooled())
        {
            itemMap.Remove(itemIcon);
            Destroy(itemIcon);
        }

        List<Character> members = PartyManager.Instance.partyMembers;
        for(int i = 0; i < members.Count; i++)
        {
            InitializeBagItemsOnUI(members[i], type); //����ɸѡ����Ʒ���ͳ�ʼ����ɫ����UI
        }
    }

    /// <summary>
    /// SortButton�����Ӧ�¼�
    /// </summary>
    private void OnSortButtonClicked()
    {
        HideAllClickedPanel();

        //�����������ɼ���
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
    /// SortByTypeButton, SortByValueButton, SortByWeightButton�����Ӧ��ť
    /// </summary>
    /// <param name="sortType"></param>
    private void OnSortByButtonClicked(string sortType)
    {
        HideAllClickedPanel();

        //�����µ���ƷͼƬǰ, ������Ѿ����ڵ���ƷUIͼ��
        foreach (GameObject itemIcon in itemMap.Keys.ToListPooled())
        {
            itemMap.Remove(itemIcon);
            Destroy(itemIcon);
        }

        List<Character> members = PartyManager.Instance.partyMembers;
        for (int i = 0; i < members.Count; i++)
        {
            members[i].SortBagItems(sortType); //����sortType�Խ�ɫ��Ʒ����bagItems��������
            InitializeBagItemsOnUI(members[i]); //����ɸѡ����Ʒ���ͳ�ʼ����ɫ����UI
        }
    }

    /// <summary>
    /// hideInventoryPanelButton�����Ӧ�¼�
    /// </summary>
    private void OnHideInventoryPanelButtonClicked()
    {
        HideAllClickedPanel();
        partyInventoryPanel.SetActive(false); //�رն��鱳�����
    }
    #endregion
}
