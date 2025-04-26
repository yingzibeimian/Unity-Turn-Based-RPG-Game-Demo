using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ������UI����ű�
/// </summary>
public class UISkillBarManager : MonoBehaviour
{
    private static UISkillBarManager instance;
    public static UISkillBarManager Instance => instance;

    public GameObject skillBarParentPrefab; //��ɫ������������Ԥ����
    public GameObject skillIconPrefab; //����ͼ��Ԥ����

    public GameObject skillTip; //������Ϣ���

    private GameObject lastLeaderSkillBarParent; //��һλ���صļ�����
    private Dictionary<Character, GameObject> skillBarMap = new Dictionary<Character, GameObject>(); //key:��ɫ value:��ɫ������������
    private Dictionary<GameObject, SkillBaseData> skillIconMap = new Dictionary<GameObject, SkillBaseData>(); //key:����ͼ�� value:��������
    private Dictionary<SkillBaseData, GameObject> skillMap = new Dictionary<SkillBaseData, GameObject>(); //key:�������� value:����ͼ��

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        InitializeSkillBarUI(); //��ʼ������������UI
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// ��ʼ��������UI
    /// </summary>
    private void InitializeSkillBarUI()
    {
        StartCoroutine(InitializeSkillBarUICoroutine());
    }

    /// <summary>
    /// ��ʼ��������UIЭ�̷���
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializeSkillBarUICoroutine()
    {
        //�ȴ�����ϵͳ��ɳ�ʼ��
        yield return new WaitWhile(() => PartyManager.Instance == null || !PartyManager.Instance.groupsInitialized);

        List<Character> members = PartyManager.Instance.partyMembers;
        foreach(Character character in members)
        {
            //�ȴ���ɫ��ɳ�ʼ��
            yield return new WaitWhile(() => !character.characterInitialized);
            CreatCharacterSkillBar(character);
            yield return null;
        }

        if (skillBarMap.ContainsKey(PartyManager.Instance.leader))
        {
            lastLeaderSkillBarParent = skillBarMap[PartyManager.Instance.leader]; //����Ϸ��ʼʱ���صļ���������Ϊ ��һ�����صļ�����
        }
    }

    /// <summary>
    /// ������ɫ��Ӧ�ļ�����
    /// </summary>
    /// <param name="character"></param>
    private void CreatCharacterSkillBar(Character character)
    {
        GameObject characterSkillBarParent = Instantiate(skillBarParentPrefab, transform); //ʵ������ɫ������������
        skillBarMap.TryAdd(character, characterSkillBarParent); //������ɫ �� ������������ ֮��Ĺ�ϵ
        InitializeSkillIconInBar(character); //��ʼ��ÿ����ɫ�������еļ���ͼ��

        if (character != PartyManager.Instance.leader)
        {
            characterSkillBarParent.SetActive(false); //�����������, �����ؼ�����
        }
    }

    /// <summary>
    /// ��ʼ��character��ɫ������UI�еļ���ͼ��
    /// </summary>
    /// <param name="character"></param>
    private void InitializeSkillIconInBar(Character character)
    {
        //�����û�д����ý�ɫ�ļ�����, �򴴽���Ӧ�ļ�����
        if (!skillBarMap.ContainsKey(character))
        {
            CreatCharacterSkillBar(character);
        }

        foreach (SkillBaseData skill in character.characterSkills)
        {
            AddSkillIconToBar(character, skill); //��skill�ļ���ͼ����ӵ�������skillBar��
        }
    }

    /// <summary>
    /// ����������Ӽ���ͼ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    public void AddSkillIconToBar(Character character, SkillBaseData skill)
    {
        //�����û�д����ý�ɫ�ļ�����, �򴴽���Ӧ�ļ�����
        if (!skillBarMap.ContainsKey(character))
        {
            CreatCharacterSkillBar(character);
        }
        GameObject skillBar = skillBarMap[character]; //character��ɫ������

        GameObject skillIconParent = Instantiate(skillIconPrefab, skillBar.transform); //ʵ��������ͼ��UI
        skillIconParent.transform.Find("SkillIcon").GetComponent<Image>().sprite = skill.icon; //��UIͼ������Ϊ����ͼ��

        //Ϊskill��skillIconͼ�����EventTrigger��Ӧ�¼�
        EventTrigger trigger = skillIconParent.transform.Find("SkillTrigger").GetComponent<EventTrigger>();
        AddEvent(trigger, EventTriggerType.PointerClick, OnSkillIconClicked); //���
        AddEvent(trigger, EventTriggerType.PointerEnter, (data) => OnSkillIconPointerEnter(skill)); //ָ�����
        AddEvent(trigger, EventTriggerType.PointerExit, OnSkillIconPointerExit); //ָ���˳�

        skillIconMap.TryAdd(skillIconParent, skill); //��������ͼ��skillIcon���������Ӧ��ɫ����skill֮��Ĺ�ϵ
        skillMap.TryAdd(skill, skillIconParent);
    }

    /// <summary>
    /// ������ͼ��Ӽ��������Ƴ�
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    public void RemoveSkillIconFromBar(SkillBaseData skill)
    {
        //�����������û����skill��Ӧ��ͼ��, ��ֱ�ӷ���
        if (!skillMap.ContainsKey(skill))
        {
            return;
        }
        GameObject skillIcon = skillMap[skill]; //skill��Ӧ�ļ���ͼ��
        skillMap.Remove(skill);
        skillIconMap.Remove(skillIcon);
        Destroy(skillIcon); //���ټ���ͼ��
    }

    /// <summary>
    /// ��ҽ�ɫ�ļ�����ȴʣ��غϱ仯ʱ, ���¼�����UI
    /// </summary>
    /// <param name="skill"></param>
    public void UpdateSkillBarUI(SkillBaseData skill)
    {
        //���������UI��û��skill��Ӧ�ļ���ͼ��, ��ֱ�ӷ���
        if (!skillMap.ContainsKey(skill))
        {
            return;
        }
        GameObject skillIconParent = skillMap[skill];
        //������ȴʣ��ʱ�����ͼ������
        skillIconParent.transform.Find("SkillIconMask").GetComponent<Image>().fillAmount = skill.remainingTurns / (skill.cooldownTurns * 1.0f);
        //������ȴʣ��ʱ����¼�ʱ���ı�
        if (skill.remainingTurns > 0)
        {
            skillIconParent.transform.Find("SkillTimer").GetComponent<TextMeshProUGUI>().text = skill.remainingTurns.ToString();
        }
        else
        {
            skillIconParent.transform.Find("SkillTimer").GetComponent<TextMeshProUGUI>().text = "";
        }
    }

    /// <summary>
    /// �л�����ʱ, ���¼�����UI
    /// </summary>
    public void UpdateLeaderSkillBar()
    {
        Character leader = PartyManager.Instance.leader;
        //�����û�д���leader�ļ�����, �򴴽�leader��Ӧ�ļ�����
        if (!skillBarMap.ContainsKey(leader))
        {
            CreatCharacterSkillBar(leader);
        }
        //�����һλ���صļ���������Ϊ��, �����������н�ɫ�ļ�����, ��������һλ���صļ���������, ���ⱨ��
        if (lastLeaderSkillBarParent == null)
        {
            foreach (Character character in skillBarMap.Keys)
            {
                skillBarMap[character].SetActive(false);
                lastLeaderSkillBarParent = skillBarMap[character];
            }
        }

        //������һλ���صļ�����UI
        lastLeaderSkillBarParent.SetActive(false);
        //��ʾ��ǰ���صļ�����UI
        skillBarMap[leader].SetActive(true);
        //����ǰ���صļ���������Ϊ��һλ���صļ�����
        lastLeaderSkillBarParent = skillBarMap[leader];
    }

    /// <summary>
    /// (Ϊ����ͼ��)����¼�����
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
    /// ����ͼ������Ӧ�¼�
    /// </summary>
    /// <param name="data"></param>
    private void OnSkillIconClicked(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        SkillBaseData skill = skillIconMap[pointerData.pointerClick.transform.parent.gameObject]; //��ü���ͼ���Ӧ�ļ���
        SkillManager.Instance.ActivateSkill(PartyManager.Instance.leader, skill); //�����
    }

    /// <summary>
    /// ����ͼ��ָ�������Ӧ�¼�
    /// </summary>
    /// <param name="data"></param>
    private void OnSkillIconPointerEnter(SkillBaseData skill)
    {
        skillTip.transform.Find("SkillIcon").GetComponent<Image>().sprite = skill.icon;
        skillTip.transform.Find("SkillName").GetComponent<TextMeshProUGUI>().text = $"{skill.skillName}";
        skillTip.transform.Find("SkillTimer").GetComponent<TextMeshProUGUI>().text = $"ʣ��غ���:{skill.remainingTurns}   ��ȴ�غ���:{skill.cooldownTurns}   �����ж�����:{skill.actionPointTakes}";
        skillTip.transform.Find("SkillDescription").GetComponent<TextMeshProUGUI>().text = $"Ч��:\n{skill.description_Effects}";

        skillTip.transform.position = skillMap[skill].transform.position + new Vector3(-30, 30);
        skillTip.SetActive(true); //��ʾSkillTip
    }

    /// <summary>
    /// ����ͼ��ָ���˳���Ӧ�¼�
    /// </summary>
    /// <param name="data"></param>
    private void OnSkillIconPointerExit(BaseEventData data)
    {
        skillTip.SetActive(false); //����SkillTip
    }
}
