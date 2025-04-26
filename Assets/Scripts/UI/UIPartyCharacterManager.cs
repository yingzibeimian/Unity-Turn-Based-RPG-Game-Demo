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

    public GameObject partyCharacterPanel; //�����ɫ���
    public Button characterPanelButton; //��ɫ�ͱ�����尴ť
    public Button hideCharacterPanelButton; //���ؽ�ɫ��尴ť

    public GameObject portraitPerfab; //ͷ��Ԥ����
    public Transform partyMembersParent; //�����Աͷ�񸸶���

    //��ɫ����
    public TextMeshProUGUI strengthData; //��������
    public TextMeshProUGUI finesseData; //��������
    public TextMeshProUGUI intelligenceData; //��������
    public TextMeshProUGUI constitutionData; //��������
    public TextMeshProUGUI charismaData; //��������
    public TextMeshProUGUI witsData; //�ǻ�����
    public TextMeshProUGUI initiativeData; //�ȹ�����
    public TextMeshProUGUI attackDistanceData; //������������
    public TextMeshProUGUI damageData; //�˺�����
    public TextMeshProUGUI armorClassData; //�����ȼ�AC����
    public TextMeshProUGUI speedData; //�ٶ�����

    //��ɫչʾ
    public Camera modelCamera; //��ɫģ�����
    public RenderTexture modelTexture; //��Ⱦ��ɫģ�͵�render texture

    public TextMeshProUGUI nameData; //��������
    public TextMeshProUGUI levelData; //�ȼ�����
    public TextMeshProUGUI healthPointData; //����ֵ����

    //���
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

    public GameObject itemImgPrefab; //װ����Ʒͼ��Ԥ����
    public GameObject equipmentItemClickedPanel; //װ���Ҽ�������
    private GameObject clickedItemIcon; //���װ��ͼ��UI

    public GameObject itemTip; //װ����Ϣ��ʾ���

    public Color highlightColor = Color.white; //�߹�(����)�߿���ɫ
    private Color followerColor = Color.gray; //���ѱ߿���ɫ

    private Dictionary<Character, GameObject> portraitDic = new Dictionary<Character, GameObject>(); //key:��ɫ value:ͷ������
    private Dictionary<GameObject, EquipmentItemData> itemMap = new Dictionary<GameObject, EquipmentItemData>(); //key:װ����Ʒͼ��UI value:װ��
    private GameObject lastLeaderPortrait; //��һ�����ؽ�ɫ��Ӧ��UIͷ������

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        BindFixedButtons(); //Ϊ�̶���ť����Ӧ�¼�

        followerColor = portraitPerfab.GetComponent<Image>().color;
        StartCoroutine(InitializaPartyCharacterPanelUI());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Ϊ�̶���ť����Ӧ�¼�
    /// </summary>
    private void BindFixedButtons()
    {
        characterPanelButton.onClick.AddListener(UpdatePartyCharacterPanelVisibility);
        hideCharacterPanelButton.onClick.AddListener(OnHideCharacterPanelButtonClicked);

        equipmentItemClickedPanel.transform.Find("UnequipButton").GetComponent<Button>().onClick.AddListener(OnUnequipButtonClicked);
        equipmentItemClickedPanel.transform.Find("DropItemButton").GetComponent<Button>().onClick.AddListener(OnDropItemButtonClicked);
    }

    /// <summary>
    /// ���¶����ɫ���Ŀɼ���
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
    /// ��ʼ�������ɫ���UI
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializaPartyCharacterPanelUI()
    {
        yield return new WaitWhile(() => PartyManager.Instance == null || !PartyManager.Instance.groupsInitialized || !PartyManager.Instance.leader.characterInitialized);

        //��ʼ�������ɫ���ͷ��
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
    /// ����ͷ��UI
    /// </summary>
    /// <param name="character"></param>
    private void CreatePortrait(Character character)
    {
        GameObject portrait = Instantiate(portraitPerfab, partyMembersParent); //ʵ��������Ԥ����, �����ø�����
        portrait.name = character.info.name + "_Portrait"; //��������, �������ͨ��ͷ��UI�����ҵ���Ӧ��ɫ
        portrait.transform.GetChild(0).GetComponent<Image>().sprite = character.info.portrait; //����ͷ��ͼƬ

        portraitDic.Add(character, portrait); //������ɫ�Ͷ�Ӧͷ��֮��Ĺ�ϵ

        //Ϊ����Ϸ���UIͷ��������ӵ������
        EventTrigger trigger = portrait.GetComponent<EventTrigger>();
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        enterEntry.callback.AddListener(OnClickPortrait);
        trigger.triggers.Add(enterEntry);

        if (character == PartyManager.Instance.leader)
        {
            lastLeaderPortrait = portrait;
            UpdateCharacterPanel(character); //���½�ɫ�����Ϣ
        }
    }

    /// <summary>
    /// ͷ������Ӧ����
    /// </summary>
    /// <param name="data"></param>
    private void OnClickPortrait(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        GameObject clickedObj = pointerData.pointerCurrentRaycast.gameObject;
        Character clickedChar = GetCharacterFromPortrait(clickedObj); //�ӵ�ǰָ��(���)���߼�⵽�Ķ���õ���ɫ
        if (clickedChar != null)
        {
            PartyManager.Instance.SwitchLeader(clickedChar); //�л����ص������ɫ��
        }
    }

    /// <summary>
    /// ��UIͷ��õ���Ӧ�Ľ�ɫ����
    /// </summary>
    /// <param name="portrait"></param>
    /// <returns></returns>
    private Character GetCharacterFromPortrait(GameObject portrait)
    {
        return PartyManager.Instance.partyMembers.Find(c => c.info.name + "_Portrait" == portrait.name);
    }

    /// <summary>
    /// ���¶����ɫ���UI
    /// </summary>
    /// <param name="leader"></param>
    public void UpdateCharacterPanel(Character leader)
    {
        UpdateHighlight(leader); //����ͷ��߹�
        UpdateCharacterInfo(leader); //���½�ɫ������Ϣ��ʾ
        UpdateModelCamera(leader); //����������Ⱦ����ģ��render texture�����
        UpdateCharacterEquipmentSlot(leader); //����װ�����ͼ��UI
    }

    /// <summary>
    /// ��������ͷ��߿�߹�(����)
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
    /// ���½�ɫ������Ϣ
    /// </summary>
    /// <param name="leader"></param>
    public void UpdateCharacterInfo(Character leader)
    {
        //����������
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

        //չʾ�������
        nameData.text = leader.info.name;
        levelData.text = $"�ȼ�: {leader.info.level}";
        healthPointData.text = $"����ֵ: {leader.info.hp}/{leader.info.maxHp}";
    }

    /// <summary>
    /// ��������ģ����Ⱦ�����Ϣ
    /// </summary>
    /// <param name="leader"></param>
    private void UpdateModelCamera(Character leader)
    {
        // ����һ����ɫ������������㼶�ָ�ΪCharacter
        if(modelCamera.transform.parent != null)
        {
            SetLayerRecursively(modelCamera.transform.parent.gameObject, LayerMask.NameToLayer("Character"));
        }

        // ���½�ɫ������������㼶����ΪModel
        SetLayerRecursively(leader.gameObject, LayerMask.NameToLayer("Model"));

        modelCamera.transform.SetParent(leader.transform); //��������Ⱦģ�͵������Ϊ���ص��Ӷ���

        //�����������transform��Ϣ
        modelCamera.transform.localPosition = new Vector3(0, 1, 3);
        modelCamera.transform.localRotation = Quaternion.Euler(0, 180, 0);
        modelCamera.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// �ݹ����ò㼶
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
    /// �л�����ʱ, ����װ�����ͼ��UI
    /// </summary>
    private void UpdateCharacterEquipmentSlot(Character leader)
    {
        //���֮ǰ����д�����һλ���ص�װ��, ����ж��װ��ͼ��UI
        if(slotHelmet.childCount > 0)
        {
            UnequipItemFromSlot(itemMap[slotHelmet.GetChild(0).gameObject]);
        }
        //��������صĶ��Ѳ��װ����Ϊ��, �͸���װ��ͼ��UI
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
    /// ��װ��UIͼ�������
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
        itemIcon.GetComponent<Image>().sprite = item.icon; //��item��ImageͼƬ����Ϊ��Ӧicon
        itemMap.Add(itemIcon, item); //����װ����Ʒͼ��UI �� װ��ScriptableObject���� ֮��Ĺ�ϵ

        //��ӵ����Ӧ�¼�
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
    /// ��װ��UIͼ��Ӳ�����Ƴ�
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
    /// װ��ͼ��UI�����Ӧ�¼�
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

        clickedItemIcon = pointerData.pointerCurrentRaycast.gameObject;
        if (!itemMap.ContainsKey(clickedItemIcon))
        {
            return;
        }
        equipmentItemClickedPanel.transform.position = pointerData.position; //�ƶ����λ��
        equipmentItemClickedPanel.SetActive(true); //��ʾ������
    }

    /// <summary>
    /// ��Ʒͼ��ָ��(���)������Ӧ����
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
        itemTip.transform.Find("ItemDescription").GetComponent<TextMeshProUGUI>().text = item.description;

        itemTip.transform.position = itemIcon.transform.position + new Vector3(366, -40);
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
    /// UnequipButton�����Ӧ�¼�
    /// </summary>
    private void OnUnequipButtonClicked()
    {
        HideAllClickedPanel();

        EquipmentItemData item = itemMap[clickedItemIcon]; //�������װ��
        Character leader = PartyManager.Instance.leader;
        leader.Unequip(item); //����װ��
    }

    /// <summary>
    /// DropItemButton�����Ӧ�¼�
    /// </summary>
    private void OnDropItemButtonClicked()
    {
        HideAllClickedPanel();

        EquipmentItemData item = itemMap[clickedItemIcon]; //�������װ��
        Character leader = PartyManager.Instance.leader;
        leader.DropEquipment(item); //����װ��
    }

    /// <summary>
    /// �ر��������
    /// </summary>
    private void HideAllClickedPanel()
    {
        equipmentItemClickedPanel.SetActive(false);
    }

    /// <summary>
    /// HideCharacterPanelButton�����Ӧ�¼�
    /// </summary>
    private void OnHideCharacterPanelButtonClicked()
    {
        partyCharacterPanel.SetActive(false);
    }
}