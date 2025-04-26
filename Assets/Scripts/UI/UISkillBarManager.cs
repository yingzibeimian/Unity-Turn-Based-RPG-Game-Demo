using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 技能栏UI管理脚本
/// </summary>
public class UISkillBarManager : MonoBehaviour
{
    private static UISkillBarManager instance;
    public static UISkillBarManager Instance => instance;

    public GameObject skillBarParentPrefab; //角色技能栏父对象预设体
    public GameObject skillIconPrefab; //技能图标预设体

    public GameObject skillTip; //技能信息面板

    private GameObject lastLeaderSkillBarParent; //上一位主控的技能栏
    private Dictionary<Character, GameObject> skillBarMap = new Dictionary<Character, GameObject>(); //key:角色 value:角色技能栏父对象
    private Dictionary<GameObject, SkillBaseData> skillIconMap = new Dictionary<GameObject, SkillBaseData>(); //key:技能图标 value:技能数据
    private Dictionary<SkillBaseData, GameObject> skillMap = new Dictionary<SkillBaseData, GameObject>(); //key:技能数据 value:技能图标

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        InitializeSkillBarUI(); //初始化技能栏界面UI
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 初始化技能栏UI
    /// </summary>
    private void InitializeSkillBarUI()
    {
        StartCoroutine(InitializeSkillBarUICoroutine());
    }

    /// <summary>
    /// 初始化技能栏UI协程方法
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializeSkillBarUICoroutine()
    {
        //等待队伍系统完成初始化
        yield return new WaitWhile(() => PartyManager.Instance == null || !PartyManager.Instance.groupsInitialized);

        List<Character> members = PartyManager.Instance.partyMembers;
        foreach(Character character in members)
        {
            //等待角色完成初始化
            yield return new WaitWhile(() => !character.characterInitialized);
            CreatCharacterSkillBar(character);
            yield return null;
        }

        if (skillBarMap.ContainsKey(PartyManager.Instance.leader))
        {
            lastLeaderSkillBarParent = skillBarMap[PartyManager.Instance.leader]; //将游戏开始时主控的技能栏设置为 上一个主控的技能栏
        }
    }

    /// <summary>
    /// 创建角色对应的技能栏
    /// </summary>
    /// <param name="character"></param>
    private void CreatCharacterSkillBar(Character character)
    {
        GameObject characterSkillBarParent = Instantiate(skillBarParentPrefab, transform); //实例化角色技能栏父对象
        skillBarMap.TryAdd(character, characterSkillBarParent); //建立角色 与 技能栏父对象 之间的关系
        InitializeSkillIconInBar(character); //初始化每个角色技能栏中的技能图标

        if (character != PartyManager.Instance.leader)
        {
            characterSkillBarParent.SetActive(false); //如果不是主控, 就隐藏技能栏
        }
    }

    /// <summary>
    /// 初始化character角色技能栏UI中的技能图标
    /// </summary>
    /// <param name="character"></param>
    private void InitializeSkillIconInBar(Character character)
    {
        //如果还没有创建该角色的技能栏, 则创建对应的技能栏
        if (!skillBarMap.ContainsKey(character))
        {
            CreatCharacterSkillBar(character);
        }

        foreach (SkillBaseData skill in character.characterSkills)
        {
            AddSkillIconToBar(character, skill); //将skill的技能图标添加到技能栏skillBar中
        }
    }

    /// <summary>
    /// 向技能栏中添加技能图标
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    public void AddSkillIconToBar(Character character, SkillBaseData skill)
    {
        //如果还没有创建该角色的技能栏, 则创建对应的技能栏
        if (!skillBarMap.ContainsKey(character))
        {
            CreatCharacterSkillBar(character);
        }
        GameObject skillBar = skillBarMap[character]; //character角色技能栏

        GameObject skillIconParent = Instantiate(skillIconPrefab, skillBar.transform); //实例化技能图标UI
        skillIconParent.transform.Find("SkillIcon").GetComponent<Image>().sprite = skill.icon; //将UI图像设置为技能图标

        //为skill的skillIcon图标添加EventTrigger响应事件
        EventTrigger trigger = skillIconParent.transform.Find("SkillTrigger").GetComponent<EventTrigger>();
        AddEvent(trigger, EventTriggerType.PointerClick, OnSkillIconClicked); //点击
        AddEvent(trigger, EventTriggerType.PointerEnter, (data) => OnSkillIconPointerEnter(skill)); //指针进入
        AddEvent(trigger, EventTriggerType.PointerExit, OnSkillIconPointerExit); //指针退出

        skillIconMap.TryAdd(skillIconParent, skill); //建立技能图标skillIcon父对象与对应角色技能skill之间的关系
        skillMap.TryAdd(skill, skillIconParent);
    }

    /// <summary>
    /// 将技能图标从技能栏中移除
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    public void RemoveSkillIconFromBar(SkillBaseData skill)
    {
        //如果技能栏中没有与skill对应的图标, 则直接返回
        if (!skillMap.ContainsKey(skill))
        {
            return;
        }
        GameObject skillIcon = skillMap[skill]; //skill对应的技能图标
        skillMap.Remove(skill);
        skillIconMap.Remove(skillIcon);
        Destroy(skillIcon); //销毁技能图标
    }

    /// <summary>
    /// 玩家角色的技能冷却剩余回合变化时, 更新技能栏UI
    /// </summary>
    /// <param name="skill"></param>
    public void UpdateSkillBarUI(SkillBaseData skill)
    {
        //如果技能栏UI中没有skill对应的技能图标, 就直接返回
        if (!skillMap.ContainsKey(skill))
        {
            return;
        }
        GameObject skillIconParent = skillMap[skill];
        //根据冷却剩余时间更新图标遮罩
        skillIconParent.transform.Find("SkillIconMask").GetComponent<Image>().fillAmount = skill.remainingTurns / (skill.cooldownTurns * 1.0f);
        //根据冷却剩余时间更新计时器文本
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
    /// 切换主控时, 更新技能栏UI
    /// </summary>
    public void UpdateLeaderSkillBar()
    {
        Character leader = PartyManager.Instance.leader;
        //如果还没有创建leader的技能栏, 则创建leader对应的技能栏
        if (!skillBarMap.ContainsKey(leader))
        {
            CreatCharacterSkillBar(leader);
        }
        //如果上一位主控的技能栏对象为空, 就先隐藏所有角色的技能栏, 并设置上一位主控的技能栏对象, 避免报错
        if (lastLeaderSkillBarParent == null)
        {
            foreach (Character character in skillBarMap.Keys)
            {
                skillBarMap[character].SetActive(false);
                lastLeaderSkillBarParent = skillBarMap[character];
            }
        }

        //隐藏上一位主控的技能栏UI
        lastLeaderSkillBarParent.SetActive(false);
        //显示当前主控的技能栏UI
        skillBarMap[leader].SetActive(true);
        //将当前主控的技能栏设置为上一位主控的技能栏
        lastLeaderSkillBarParent = skillBarMap[leader];
    }

    /// <summary>
    /// (为技能图标)添加事件处理
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
    /// 技能图标点击响应事件
    /// </summary>
    /// <param name="data"></param>
    private void OnSkillIconClicked(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        SkillBaseData skill = skillIconMap[pointerData.pointerClick.transform.parent.gameObject]; //获得技能图标对应的技能
        SkillManager.Instance.ActivateSkill(PartyManager.Instance.leader, skill); //激活技能
    }

    /// <summary>
    /// 技能图标指针进入响应事件
    /// </summary>
    /// <param name="data"></param>
    private void OnSkillIconPointerEnter(SkillBaseData skill)
    {
        skillTip.transform.Find("SkillIcon").GetComponent<Image>().sprite = skill.icon;
        skillTip.transform.Find("SkillName").GetComponent<TextMeshProUGUI>().text = $"{skill.skillName}";
        skillTip.transform.Find("SkillTimer").GetComponent<TextMeshProUGUI>().text = $"剩余回合数:{skill.remainingTurns}   冷却回合数:{skill.cooldownTurns}   消耗行动点数:{skill.actionPointTakes}";
        skillTip.transform.Find("SkillDescription").GetComponent<TextMeshProUGUI>().text = $"效果:\n{skill.description_Effects}";

        skillTip.transform.position = skillMap[skill].transform.position + new Vector3(-30, 30);
        skillTip.SetActive(true); //显示SkillTip
    }

    /// <summary>
    /// 技能图标指针退出响应事件
    /// </summary>
    /// <param name="data"></param>
    private void OnSkillIconPointerExit(BaseEventData data)
    {
        skillTip.SetActive(false); //隐藏SkillTip
    }
}
