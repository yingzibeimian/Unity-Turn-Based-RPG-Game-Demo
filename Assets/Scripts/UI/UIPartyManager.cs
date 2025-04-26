using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// 队伍UI脚本, 挂载在PartyUI上
/// </summary>
public class UIPartyManager : MonoBehaviour
{
    public static UIPartyManager Instance;

    public GameObject portraitPrefab; //头像预设体
    public GameObject buffIconPrefab; //Buff图标预设体
    public Vector2 startPos = Vector2.zero; //头像起始位置
    public float portraitSpacing = 160.0f; //同一分队头像的间隔像素
    public float groupSpacing = 30.0f; //不同分队之间头像的间隔像素
    public float relayoutSpeed = 15.0f; //重新布局UI头像时头像移动的速度

    public Color highlightColor = Color.white; //高光(主控)边框颜色
    private Color followerColor = Color.gray; //队友边框颜色

    public Image bottomPortrait;
    public GameObject buffTip; //buff提示面板

    private GameObject lastLeaderPortrait; //上一个主控角色对应的UI头像物体

    private Character draggingChar; //鼠标拖动的头像的对应角色
    public bool relayouting = false; //标记是否正在重新布局UI头像

    private Dictionary<Character, GameObject> portraitDic = new Dictionary<Character, GameObject>(); //key:角色 value:头像物体
    private Dictionary<Buff, GameObject> buffIconDic = new Dictionary<Buff, GameObject>(); //key:角色Buff value:Buff图标

    // Start is called before the first frame update
    void Start()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        followerColor = portraitPrefab.GetComponent<Image>().color;
        StartCoroutine(WaitForGroupsInitialized());
    }

    // Update is called once per frame
    void Update()
    {

    }

    private IEnumerator WaitForGroupsInitialized()
    {
        yield return new WaitWhile(() => PartyManager.Instance == null || !PartyManager.Instance.groupsInitialized);
        //while (PartyManager.Instance == null || !PartyManager.Instance.groupsInitialized)
        //{
        //    yield return null;
        //}
        yield return InitializaPartyUI();
    }

    /// <summary>
    /// 初始化队伍UI界面
    /// </summary>
    public IEnumerator InitializaPartyUI()
    {
        //初始化左侧队伍头像
        Vector2 uiPos = startPos;
        foreach(LinkedList<Character> group in PartyManager.Instance.groups)
        {
            if(group.Count == 0)
            {
                continue;
            }
            foreach(Character character in group)
            {
                CreatePortrait(character, uiPos);

                uiPos -= new Vector2(0, portraitSpacing); //组内头像间隔为portraitSpacing

                yield return null;
            }
            uiPos -= new Vector2(0, groupSpacing); //不同组间间隔为portraitSpacing + groupSpacing
        }
    }

    /// <summary>
    /// 在UI上创建character对应的头像
    /// </summary>
    /// <param name="character"></param>
    /// <param name="uiPos"></param>
    private void CreatePortrait(Character character, Vector2 uiPos)
    {
        GameObject portrait = Instantiate(portraitPrefab); //实例化对象预设体
        portrait.transform.SetParent(transform); //将头像预设体作为PartyUI的子对象
        (portrait.transform as RectTransform).anchoredPosition = uiPos; //设置头像位置
        portrait.transform.localScale = Vector3.one; //确保缩放正确
        portrait.name = character.info.name + "_Portrait"; //设置头像名称
        portrait.transform.Find("Portrait").GetComponent<Image>().sprite = character.info.portrait; //设置头像图片

        portraitDic.Add(character, portrait); //建立角色和对应头像之间的关系

        //为场景上的UI头像物体添加事件处理
        EventTrigger trigger = portrait.GetComponent<EventTrigger>();
        AddEvent(trigger, EventTriggerType.BeginDrag, OnBeginDrag);
        AddEvent(trigger, EventTriggerType.Drag, OnDrag);
        AddEvent(trigger, EventTriggerType.EndDrag, OnEndDrag);
        AddEvent(trigger, EventTriggerType.PointerClick, OnClick);

        if(character == PartyManager.Instance.leader)
        {
            lastLeaderPortrait = portrait;
            UpdateHighlight(character); //初始化头像UI时同时高光主控头像边框
        }
    }

    /// <summary>
    /// (为头像)添加事件处理
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
    /// 开始拖拽响应函数
    /// </summary>
    /// <param name="data"></param>
    private void OnBeginDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData; //获得开始拖动时鼠标获得的数据
        if (relayouting) //if (relayouting && TurnManager.Instance.isInTurn) //防止重新布局过程中又拖拽头像 和 回合制中拖拽点击头像
        {
            return;
        }
        draggingChar = GetCharacterFromPortrait(pointerData.pointerDrag); //从当前指针(鼠标)拖动的对象得到角色
        
        //切换主控
        if(draggingChar != null && !draggingChar.isInTurn) //防止拖拽角色为空 或者 拖拽回合制中的头像
        {
            PartyManager.Instance.SwitchLeader(draggingChar);
        }
    }

    /// <summary>
    /// 拖拽中响应函数
    /// </summary>
    /// <param name="data"></param>
    private void OnDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;
        if (relayouting && draggingChar.isInTurn) //if(relayouting && TurnManager.Instance.isInTurn) //防止重新布局过程中又拖拽头像
        {
            return;
        }
        pointerData.pointerDrag.GetComponent<RectTransform>().anchoredPosition += pointerData.delta / GetComponentInParent<Canvas>().scaleFactor;
    }

    /// <summary>
    /// 结束拖拽响应函数
    /// </summary>
    /// <param name="data"></param>
    private void OnEndDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        if (relayouting && draggingChar.isInTurn) //if(relayouting && TurnManager.Instance.isInTurn) //防止重新布局过程中又拖拽头像
        {
            return;
        }

        //检测鼠标是否落在其他角色头像上
        Character targetChar = null; //拖动结束时拖动头像落在的目标头像
        List<RaycastResult> results = GetRaycastResults(); //当前鼠标所在位置的射线检测得到的所有UI元素

        foreach(RaycastResult result in results)
        {
            if(result.gameObject != portraitDic[draggingChar]) //如果检测到的物体不是正在拖动的头像物体
            {
                targetChar = GetCharacterFromPortrait(result.gameObject); //从物体得到目标角色信息
                break;
            }
        }

        UpdateGroups(targetChar); //更新队伍系统重的队伍分组groups
        StartCoroutine(SmoothRelayout()); //平滑地重新布局头像UI
    }

    /// <summary>
    /// 点击响应函数
    /// </summary>
    /// <param name="data"></param>
    private void OnClick(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        if (relayouting) //if (relayouting && TurnManager.Instance.isInTurn) //防止重新布局过程中又点击头像
        {
            return;
        }

        GameObject clickedObj = pointerData.pointerCurrentRaycast.gameObject;
        Character clickedChar = GetCharacterFromPortrait(clickedObj); //从当前指针(鼠标)射线检测到的对象得到角色
        if (clickedChar != null)
        {
            PartyManager.Instance.SwitchLeader(clickedChar);
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
    /// 拖拽后更新队伍分组
    /// </summary>
    /// <param name="targetChar"></param>
    private void UpdateGroups(Character targetChar)
    {
        if(targetChar != null) //如果检测到了目标头像角色
        {
            //将正在拖拽的头像的对应角色 移动到 目标头像角色所在的分组
            PartyManager.Instance.MoveToGroup(draggingChar, targetChar);
        }
        else //如果没有检测到目标头像角色
        {
            //为正在拖拽的头像的对应角色创建新的分组
            PartyManager.Instance.CreatNewGroup(draggingChar);
        }
    }

    /// <summary>
    /// 更新UI头像布局, 将头像从原位置平滑地移动到新位置
    /// </summary>
    /// <returns></returns>
    public IEnumerator SmoothRelayout()
    {
        relayouting = true;

        Vector2 targetPos = startPos;
        foreach (LinkedList<Character> group in PartyManager.Instance.groups)
        {
            if (group.Count == 0)
            {
                continue;
            }
            foreach (Character character in group)
            {
                if (portraitDic.ContainsKey(character)) //如果要更新的角色头像之前就存在
                {
                    GameObject portrait = portraitDic[character];
                    Vector2 nowPos = (portrait.transform as RectTransform).anchoredPosition;
                    while (Vector2.Distance(nowPos, targetPos) > 0.5f)
                    {
                        nowPos = (portrait.transform as RectTransform).anchoredPosition;
                        (portrait.transform as RectTransform).anchoredPosition = Vector2.Lerp(nowPos, targetPos, Time.deltaTime * relayoutSpeed);
                        yield return null; //每帧处理一部分, 提高性能
                    }
                    (portrait.transform as RectTransform).anchoredPosition = targetPos;
                }
                else //如果要更新的角色头像之前不存在
                {
                    CreatePortrait(character, targetPos);
                    yield return null;
                }

                targetPos -= new Vector2(0, portraitSpacing); //组内头像间隔为portraitSpacing
            }
            targetPos -= new Vector2(0, groupSpacing); //不同组间间隔为portraitSpacing + groupSpacing
        }

        relayouting = false;
    }

    /// <summary>
    /// 更新主控头像边框高光
    /// </summary>
    private void UpdateHighlight(GameObject leaderGameObj)
    {

        if(lastLeaderPortrait != null)
        {
            lastLeaderPortrait.GetComponent<Image>().color = followerColor;
        }
        leaderGameObj.GetComponent<Image>().color = highlightColor;
        lastLeaderPortrait = leaderGameObj;
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
        if(leader != null && portraitDic.ContainsKey(leader))
        {
            GameObject leaderGameObj = portraitDic[leader];
            leaderGameObj.GetComponent<Image>().color = highlightColor;
            lastLeaderPortrait = leaderGameObj;

            ChangeBottomLeaderPortrait(leader);
        }
    }

    /// <summary>
    /// 更换底部主控头像
    /// </summary>
    public void ChangeBottomLeaderPortrait(Character leader)
    {
        bottomPortrait.sprite = leader.info.portrait;
    }

    /// <summary>
    /// 在队伍角色头像旁边添加Buff图标
    /// </summary>
    /// <param name="character"></param>
    /// <param name="buff"></param>
    public void AddBuffIconBesidesPortrait(Character character, Buff buff)
    {
        //如果没有角色对应的队伍头像, 就直接返回
        if (!portraitDic.ContainsKey(character))
        {
            return;
        }
        Transform buffParent = portraitDic[character].transform.Find("BuffParent"); //找到带有水平布局组件的buffParent父对象
        GameObject buffIcon = Instantiate(buffIconPrefab, buffParent); //实例化Buff图标预设体
        Transform icon = buffIcon.transform.Find("BuffIcon");
        icon.GetComponent<Image>().sprite = buff.buffIcon; //设置Buff图标
        buffIconDic.TryAdd(buff, buffIcon); //加入buffIconDic

        //为图标icon按钮添加指针进入和退出事件
        EventTrigger trigger = icon.GetComponent<EventTrigger>();
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) =>
        {
            PointerEventData pointerData = data as PointerEventData;

            buffTip.transform.Find("BuffIcon").GetComponent<Image>().sprite = buff.buffIcon;
            if(buff.timeType == BuffTimeType.Temporary)
            {
                buffTip.transform.Find("BuffName").GetComponent<TextMeshProUGUI>().text = $"{buff.buffName}\t剩余回合数: {buff.remainingTurns}";
            }
            else if (buff.timeType == BuffTimeType.Permanet)
            {
                buffTip.transform.Find("BuffName").GetComponent<TextMeshProUGUI>().text = $"{buff.buffName}\t剩余回合数: 永久";
            }
            buffTip.transform.Find("BuffDescription_Effects").GetComponent<TextMeshProUGUI>().text = $"效果:\n{buff.buffDescription_Effects}";
            buffTip.transform.Find("BuffDescription_BG").GetComponent<TextMeshProUGUI>().text = $"({buff.buffDescription_BG})";

            buffTip.transform.position = buffIconDic[buff].transform.position + new Vector3(0, -32);
            buffTip.SetActive(true); //显示BuffTip
        });
        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) =>
        {
            buffTip.SetActive(false); //隐藏BuffTip
        });
        trigger.triggers.Add(enterEntry);
        trigger.triggers.Add(exitEntry);
    }

    /// <summary>
    /// 更新在队伍角色头像旁边的Buff图标
    /// </summary>
    /// <param name="character"></param>
    /// <param name="buff"></param>
    public void UpdateBuffIconBesidesPortrait(Buff buff)
    {
        //如果没有buff对应buffIcon, 就直接返回
        if (!buffIconDic.ContainsKey(buff))
        {
            return;
        }
        //根据Buff剩余时间更新图标边框
        buffIconDic[buff].transform.Find("BuffRemainMask").GetComponent<Image>().fillAmount = buff.remainingTurns / (buff.durationTurns * 1.0f);
    }

    /// <summary>
    /// 移除在队伍角色头像旁边的Buff图标
    /// </summary>
    /// <param name="character"></param>
    /// <param name="buff"></param>
    public void RemoveBuffIconBesidesPortrait(Buff buff)
    {
        //如果没有buff对应buffIcon, 就直接返回
        if (!buffIconDic.ContainsKey(buff))
        {
            return;
        }
        GameObject buffIcon = buffIconDic[buff];
        buffIconDic.Remove(buff); //从字典中移除
        Destroy(buffIcon); //销毁Buff图标

        buffTip.SetActive(false); //隐藏BuffTip面板
    }
}
