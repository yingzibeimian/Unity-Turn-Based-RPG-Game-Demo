using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class UITurnManager : MonoBehaviour
{
    private static UITurnManager instance;
    public static UITurnManager Instance => instance;

    [SerializeField] private float cooldown = 0.5f; //按钮点击间隔时间, 现在按钮点击频率

    public Button startAndEndTurnButton; //开启和关闭回合制的按钮

    public GameObject turnPlayerPanel; //玩家回合UI面板
    public Button endPlayerTurnButton; //主动结束玩家回合按钮
    public Button delayPlayerTurnButton; //主动推迟玩家回合按钮
    public GameObject delayButtonTip; //推迟按钮提示

    public GameObject turnParticipantsObj; //回合制头像列表的父对象
    public GameObject turnPortraitFrame; //回合制头像预设体
    public GameObject turnPortraitHpBarFill; //回合制头像血条预设体
    public GameObject turnSpliter; //回合制头像列表分割器
    public Color portraitFrameDefaultColor = new Color(150, 150, 150); //头像边框默认颜色
    private GameObject spliter;
    public int MaxShowCount = 20; //回合制头像列表最多可以显示的头像数量
    public float portraitWidth = 70.0f; //头像预设体宽度
    public float spliterWidth = 24.0f; //分割器宽度
    public Vector2 hpBarFillPos = new Vector2(-28, -36); //血条位置
    private List<GameObject> turnPortraits = new List<GameObject>();
    private Dictionary<Character, List<GameObject>> portraitMap = new Dictionary<Character, List<GameObject>>(); //角色与回合制头像之间的映射
    private Dictionary<Character, List<GameObject>> hpBarFillMap = new Dictionary<Character, List<GameObject>>(); //角色与回合制头像血条之间的映射
    private Dictionary<GameObject, Character> characterMap = new Dictionary<GameObject, Character>(); //头像与角色之间的映射

    public float relayoutSpeed = 10.0f; //重新布局速度
    public bool relayouting = false; //用于标记头像是否正在重新布局中

    public List<Image> actionPointBalls = new List<Image>(); //用于显示行动点数的图像
    public Sprite defaultBall;
    public Sprite greenBall;
    public Sprite redBall;

    public GameObject turnMessagePanel; //回合消息面板
    public Image turnStartTipImg; //开战提示背景图
    public TextMeshProUGUI turnStartTipText; //开战提示文字
    public TextMeshProUGUI turnCampTipText; //阵营提示文字

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        //初始化StartAndEndTurnButton按钮
        startAndEndTurnButton.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f; //按钮响应的最小alpha值
        startAndEndTurnButton.onClick.AddListener(OnClickStartAndEndTurnButton); //为按钮点击添加监听函数
        //初始化玩家回合面板
        InitializeTurnPlayerPanel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 初始化回合制UI
    /// </summary>
    public void InitializeTurnUI()
    {
        ShowTurnMessagePanel();
        StartCoroutine(InitialiazeTurnPortraitUI());
    }

    /// <summary>
    /// 结束回合制时更新UI
    /// </summary>
    public void UpdateWhenEndTurn()
    {
        StartCoroutine(SmoothEndTurnUI());
    }

    private IEnumerator SmoothEndTurnUI()
    {
        yield return new WaitWhile(() => relayouting);
        relayouting = true;

        turnParticipantsObj.SetActive(false);
        HideTurnPlayerPanel();
        HideTurnMessagePanel();

        for (int i = 0; i < turnPortraits.Count; i++)
        {
            Destroy(turnPortraits[i]);
        }
        turnPortraits.Clear();
        spliter = null;
        portraitMap.Clear();
        hpBarFillMap.Clear();
        characterMap.Clear();

        relayouting = false;
    }

    /// <summary>
    /// 显示回合制消息面板
    /// </summary>
    public void ShowTurnMessagePanel()
    {
        turnMessagePanel.SetActive(true);
    }

    /// <summary>
    /// 显示开战提示
    /// </summary>
    public void ShowTurnStartTip()
    {
        StartCoroutine(SmoothShowAndHideTurnStartTip());
    }

    /// <summary>
    /// 显示开战提示
    /// </summary>
    /// <returns></returns>
    private IEnumerator SmoothShowAndHideTurnStartTip()
    {
        //显示
        Color imgTargetColor = turnStartTipImg.color;
        turnStartTipImg.color -= new Color(0, 0, 0, 1);

        Color textTargetColor = turnStartTipText.color;
        turnStartTipText.color -= new Color(0, 0, 0, 1);

        turnStartTipImg.gameObject.SetActive(true);

        while (Mathf.Abs(1 - turnStartTipImg.color.a) > 0.1f)
        {
            turnStartTipImg.color = Color.Lerp(turnStartTipImg.color, imgTargetColor, Time.deltaTime * 10);
            turnStartTipText.color = Color.Lerp(turnStartTipText.color, textTargetColor, Time.deltaTime * 10);
            yield return null;
        }
        turnStartTipImg.color = imgTargetColor;
        turnStartTipText.color = textTargetColor;

        //等待
        yield return new WaitForSeconds(1.5f);

        //渐隐
        Color imgColor_Copy = turnStartTipImg.color;
        Color textColor_Copy = turnStartTipText.color;
        imgTargetColor -= new Color(0, 0, 0, 1);
        textTargetColor -= new Color(0, 0, 0, 1);
        
        while(Mathf.Abs(turnStartTipImg.color.a - 0) > 0.1f)
        {
            turnStartTipImg.color = Color.Lerp(turnStartTipImg.color, imgTargetColor, Time.deltaTime * 15);
            turnStartTipText.color = Color.Lerp(turnStartTipText.color, textTargetColor, Time.deltaTime * 15);
            yield return null;
        }
        turnStartTipImg.gameObject.SetActive(false);
        //失活后恢复原颜色
        turnStartTipImg.color = imgColor_Copy;
        turnStartTipText.color = textColor_Copy;
    }

    /// <summary>
    /// 隐藏回合制消息面板
    /// </summary>
    public void HideTurnMessagePanel()
    {
        turnMessagePanel.SetActive(false);
    }

    /// <summary>
    /// 更新回合制阵营提示
    /// </summary>
    /// <param name="portrait"></param>
    private void UpdateTurnCampTip(GameObject portrait)
    {
        if (characterMap[portrait].CompareTag("Player"))
        {
            turnCampTipText.text = "你的回合";
            turnCampTipText.color = Color.white;
        }
        else
        {
            turnCampTipText.text = "敌人回合";
            turnCampTipText.color = Color.red;
        }
    }

    /// <summary>
    /// 显示玩家回合面板
    /// </summary>
    public void ShowTurnPlayerPanel()
    {
        turnPlayerPanel.SetActive(true);
    }

    /// <summary>
    /// 隐藏玩家回合面板
    /// </summary>
    public void HideTurnPlayerPanel()
    {
        turnPlayerPanel.SetActive(false);
    }

    /// <summary>
    /// 初始化玩家回合面板
    /// </summary>
    public void InitializeTurnPlayerPanel()
    {
        endPlayerTurnButton.onClick.AddListener(OnClickEndPlayerTurnButton); //为主动结束玩家回合按钮添加点击监听函数
        delayPlayerTurnButton.onClick.AddListener(OnClickDelayPlayerTurnButton); //为主动推迟玩家回合按钮添加点击监听函数

        //为延迟回合按钮添加指针进入和退出事件
        EventTrigger trigger = delayPlayerTurnButton.GetComponent<EventTrigger>();
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) =>
        {
            if(delayButtonTip != null)
            {
                delayButtonTip.SetActive(true);
            }
        });
        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) =>
        {
            if (delayButtonTip != null)
            {
                delayButtonTip.SetActive(false);
            }
        });
        trigger.triggers.Add(enterEntry);
        trigger.triggers.Add(exitEntry);

        HideTurnPlayerPanel(); //隐藏面板
    }

    /// <summary>
    /// 初始化回合制参与者头像列表
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitialiazeTurnPortraitUI()
    {
        yield return new WaitWhile(() => relayouting);
        relayouting = true;

        turnParticipantsObj.SetActive(true);

        int participantsCount = Mathf.Min(TurnManager.Instance.participants.Count + TurnManager.Instance.nextTurnParticipants.Count, MaxShowCount); //头像列表显示头像数
        Vector2 uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0); //头像列表起始位置 
        //Vector2 uiPos = startPos;
        int index = 1; //从左到右头像计数
        foreach(Character participant in TurnManager.Instance.participants)
        {
            CreatPortrait(participant, uiPos, index++);
            uiPos.x += portraitWidth;
            yield return null;
        }

        //初始化分割器
        uiPos.x -= portraitWidth;
        uiPos.x += spliterWidth;
        spliter = Instantiate(turnSpliter);
        spliter.transform.SetParent(turnParticipantsObj.transform);
        (spliter.transform as RectTransform).anchoredPosition = uiPos;
        spliter.transform.localScale = Vector3.one;
        uiPos.x += portraitWidth;
        turnPortraits.Add(spliter);
        index++;

        foreach (Character participant in TurnManager.Instance.nextTurnParticipants)
        {
            CreatPortrait(participant, uiPos, index++);
            uiPos.x += portraitWidth;
            yield return null;
        }

        //当前回合行动者的头像放大至1.2倍, 边框颜色设置为白色
        turnPortraits[0].transform.localScale = Vector3.one * 1.2f;
        if (characterMap[turnPortraits[0]].CompareTag("Player"))
        {
            turnPortraits[0].GetComponent<Image>().color = Color.white;
        }

        //更新阵营提示
        UpdateTurnCampTip(turnPortraits[0]);

        relayouting = false;
    }

    /// <summary>
    /// 在回合制UI中的对应位置uiPos处创建对应角色的头像, 根据index决定现隐, 并建立角色和头像、血条之间的对应关系
    /// </summary>
    /// <param name="participant"></param>
    /// <param name="uiPos"></param>
    /// <param name="index"></param>
    private void CreatPortrait(Character participant, Vector2 uiPos, int index)
    {
        GameObject portrait = Instantiate(turnPortraitFrame); //实例化头像预设体
        portrait.transform.SetParent(turnParticipantsObj.transform); //将头像预设体作为PartyUI的子对象
        (portrait.transform as RectTransform).anchoredPosition = uiPos; //设置头像位置
        portrait.transform.localScale = Vector3.one; //确保缩放正确
        //portrait.name = character.info.name + "_Portrait"; //设置头像名称
        portrait.transform.GetChild(0).GetComponent<Image>().sprite = participant.info.portrait; //设置头像图片

        GameObject hpBarFill = Instantiate(turnPortraitHpBarFill); //实例化血条预设体
        hpBarFill.transform.SetParent(portrait.transform);
        (hpBarFill.transform as RectTransform).anchoredPosition = hpBarFillPos;
        hpBarFill.transform.localScale = Vector3.one;
        hpBarFill.GetComponent<Image>().fillAmount = participant.info.hp / (participant.info.maxHp * 1.0f); //初始化血条长度

        if (participant.CompareTag("Enemy"))
        {
            portrait.GetComponent<Image>().color = Color.red; //敌人的头像边框设置为红色
        }
        if(index > MaxShowCount)
        {
            portrait.SetActive(false); //超过最大显示数则暂时不显示
        }
        else
        {
            portrait.SetActive(true);
        }
        turnPortraits.Add(portrait);

        //建立角色和头像、血条之间的对应关系
        if (!portraitMap.ContainsKey(participant))
        {
            portraitMap.Add(participant, new List<GameObject>()); //建立角色和对应头像之间的关系
        }
        portraitMap[participant].Add(portrait);

        if (!hpBarFillMap.ContainsKey(participant))
        {
            hpBarFillMap.Add(participant, new List<GameObject>());
        }
        hpBarFillMap[participant].Add(hpBarFill);

        if (!characterMap.ContainsKey(portrait))
        {
            characterMap.Add(portrait, participant);
        }
    }

    /// <summary>
    /// 当前行动者非推迟回合的情况下, 结束回合后更新UI
    /// </summary>
    /// <param name="character"></param>
    public void UpdateTurnPortrait(Character character)
    {
        StartCoroutine(SmoothUpdateTurnPortrait(character));
    }

    /// <summary>
    /// 当前行动者结束回合后更新UI的协程方法
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    private IEnumerator SmoothUpdateTurnPortrait(Character character)
    {
        yield return new WaitWhile(() => relayouting);
        relayouting = true;

        if (portraitMap.ContainsKey(character))
        {
            StartCoroutine(SmoothDownAndHide(portraitMap[character][0], false));
            turnPortraits.Remove(portraitMap[character][0]);
            characterMap.Remove(portraitMap[character][0]);
            portraitMap[character].RemoveAt(0);
            hpBarFillMap[character].RemoveAt(0);
        }

        if (turnPortraits[0] != spliter) //如果第一个物体不是分割器
        {
            int participantsCount = Mathf.Min(turnPortraits.Count, MaxShowCount); //头像列表显示头像数
            Vector2 uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0); //头像列表起始位置
            //Vector2 uiPos = new Vector2((turnPortraits.Count * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0);
            for (int i = 0; i < turnPortraits.Count; i++)
            {
                //StartCoroutine(SmoothSlide(turnPortraits[i], (turnPortraits[i].transform as RectTransform).anchoredPosition - new Vector2(portraitWidth * 0.5f, 0), i + 1));

                if (turnPortraits[i] == spliter)
                {
                    uiPos.x -= portraitWidth;
                    uiPos.x += spliterWidth;
                }
                if (i < turnPortraits.Count - 1)
                {
                    StartCoroutine(SmoothSlide(turnPortraits[i], uiPos, i + 1));
                }
                else
                {
                    yield return SmoothSlide(turnPortraits[i], uiPos, i + 1); //最后一个头像更新位置完成前一直等待, 防止不同动画的SmoothSlide协程冲突
                }
                uiPos.x += portraitWidth;
            }
        }
        else //如果第一个物体时分割器, 则代表进入下一轮回合
        {
            //更新剩下头像的位置
            int participantsCount = Mathf.Min(TurnManager.Instance.nextTurnParticipants.Count * 2, MaxShowCount); //头像列表显示头像数
            Vector2 uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth - spliterWidth, 0); //头像列表起始位置 
            //Vector2 uiPos = new Vector2(((turnPortraits.Count + TurnManager.Instance.nextTurnParticipants.Count) * portraitWidth + spliterWidth) * -0.5f + portraitWidth - spliterWidth, 0);
            for (int i = 0; i < turnPortraits.Count; i++)
            {
                if (i != turnPortraits.Count - 1)
                {
                    StartCoroutine(SmoothSlide(turnPortraits[i], uiPos, i + 1));
                }
                else
                {
                    yield return SmoothSlide(turnPortraits[i], uiPos, i + 1); //最后一个头像更新位置完成前一直等待, 防止不同动画的SmoothSlide协程冲突
                }
                uiPos.x += portraitWidth;
            }
            //更新分割器的位置
            uiPos = (turnPortraits[turnPortraits.Count - 1].transform as RectTransform).anchoredPosition + new Vector2(spliterWidth, 0);
            (spliter.transform as RectTransform).anchoredPosition = uiPos;
            turnPortraits.Remove(spliter);
            turnPortraits.Add(spliter);
            if(turnPortraits.Count > MaxShowCount)
            {
                spliter.SetActive(false);
            }
            else
            {
                spliter.SetActive(true);
            }
            uiPos.x += portraitWidth;
            //添加下一回合角色的头像
            foreach(Character participant in TurnManager.Instance.nextTurnParticipants)
            {
                CreatPortrait(participant, uiPos, turnPortraits.Count + 1);
                uiPos.x += portraitWidth;
                yield return null;
            }
        }

        //更新回合行动者头像高亮
        turnPortraits[0].transform.localScale = Vector3.one * 1.2f;
        if (characterMap[turnPortraits[0]].CompareTag("Player"))
        {
            turnPortraits[0].GetComponent<Image>().color = Color.white;
        }

        //更新阵营提示
        UpdateTurnCampTip(turnPortraits[0]);

        //yield return ResortNextTurnParticipantsPortraits();

        relayouting = false;
    }

    /// <summary>
    /// 当前回合行动者推迟回合后更新UI
    /// </summary>
    public void UpdateTurnPortraitAfterDelay()
    {
        StartCoroutine(SmoothUpdateTurnPortraitAfterDelay());
    }

    private IEnumerator SmoothUpdateTurnPortraitAfterDelay()
    {
        yield return new WaitWhile(() => relayouting);
        relayouting = true;

        int spliterIndex = turnPortraits.IndexOf(spliter); //获得分割器的下标
        //取消对当前行动角色的高亮
        GameObject delayProtrait = turnPortraits[0]; //推迟回合角色的头像
        delayProtrait.transform.localScale = Vector3.one;
        delayProtrait.GetComponent<Image>().color = portraitFrameDefaultColor;
        yield return SmoothSlide(delayProtrait, (delayProtrait.transform as RectTransform).anchoredPosition - new Vector2(0, 100), 1); //向下滑动
        yield return SmoothSlide(delayProtrait, (turnPortraits[spliterIndex - 1].transform as RectTransform).anchoredPosition - new Vector2(0, 100), 1); //向右滑动
        //将推迟角色头像插入到分割器之前
        turnPortraits.Insert(spliterIndex, delayProtrait);
        turnPortraits.RemoveAt(0);

        //更新所有头像位置
        int participantsCount = Mathf.Min(turnPortraits.Count, MaxShowCount); //头像列表显示头像数
        Vector2 uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0); //头像列表起始位置
        //Vector2 uiPos = new Vector2((turnPortraits.Count * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0);
        for (int i = 0; i < turnPortraits.Count; i++)
        {
            if (turnPortraits[i] == spliter)
            {
                uiPos.x -= portraitWidth;
                uiPos.x += spliterWidth;
            }
            if(i != spliterIndex - 1)
            {
                StartCoroutine(SmoothSlide(turnPortraits[i], uiPos, i + 1));
            }
            else
            {
                yield return SmoothSlide(turnPortraits[i], uiPos, i + 1); //最后一个头像更新位置完成前一直等待, 防止不同动画的SmoothSlide协程冲突
            }
            uiPos.x += portraitWidth;
        }

        //更新回合行动者头像高亮
        turnPortraits[0].transform.localScale = Vector3.one * 1.2f;
        if (characterMap[turnPortraits[0]].CompareTag("Player"))
        {
            turnPortraits[0].GetComponent<Image>().color = Color.white;
        }

        //更新阵营提示
        UpdateTurnCampTip(turnPortraits[0]);

        relayouting = false;
    }

    /// <summary>
    /// 当回合制参与角色先攻发生变化 或者 有新的角色加入时, 对下一回合参与者的头像进行重新排序
    /// </summary>
    /// <returns></returns>
    public IEnumerator ResortNextTurnParticipantsPortraits()
    {
        yield return new WaitWhile(() => relayouting);
        relayouting = true;

        List<Character> participants = TurnManager.Instance.nextTurnParticipants; //获得下一回合参与角色列表
        List<GameObject> newParticipantsPortrait = new List<GameObject>(); //回合制新加入者的头像列表

        int spliterIndex = turnPortraits.IndexOf(spliter); //获得分割器的下标
        Vector2 uiPos = (spliter.transform as RectTransform).anchoredPosition + new Vector2(portraitWidth, 0); //起点设置为分割器后面第一个头像位置
        int index = turnPortraits.IndexOf(spliter) + 1; //计算下一回合参与者的首位在当前头像列表中的下标
        //GameObject nextTurnLastPortrait = turnPortraits[turnPortraits.Count - 1]; //原来头像列表中的最后一个头像
        //清空下一轮回合制头像在turnPortraits中的记录
        for (int i = turnPortraits.Count - 1; i >= index; i--)
        {
            turnPortraits.RemoveAt(i);
        }
        //更新下一轮所有参与角色的头像位置
        for (int i= 0; i < participants.Count; i++)
        {
            //如果不是新加入者, 已经有回合制头像
            if (portraitMap.ContainsKey(participants[i]))
            {
                if(portraitMap[participants[i]].Count > 0)
                {
                    List<GameObject> portraits = portraitMap[participants[i]];
                    GameObject portrait = portraits[portraits.Count - 1];
                    //如果要移动的头像不是原有头像列表中的最后一个
                    //if (portrait != nextTurnLastPortrait)
                    //{
                    //    yield return SmoothSlide(portrait, uiPos, index++); //将participant对应的头像(可能有1个或2个)的最后一个 平滑移动到 uiPos的位置
                    //}
                    ////如果要移动的头像是原有头像列表中的最后一个, 就等待其移动完成
                    //else
                    //{
                    //    yield return SmoothSlide(portrait, uiPos, index++);
                    //}
                    yield return SmoothSlide(portrait, uiPos, index++);
                    turnPortraits.Add(portrait); //将头像重新记录到turnPortraits中
                }
            }
            //如果是新加入者, 还没有回合制头像
            else
            {
                //在新加入者头像应在的位置下方, 创建对应头像
                CreatPortrait(participants[i], uiPos - new Vector2(0, 100), index++);
                if (portraitMap.ContainsKey(participants[i]))
                {
                    List<GameObject> portraits = portraitMap[participants[i]];
                    newParticipantsPortrait.Add(portraits[portraits.Count - 1]);
                }
                yield return null;
            }
            uiPos.x += portraitWidth; //更新uiPos位置
        }
        //如果有新加入者
        if (newParticipantsPortrait.Count > 0)
        {
            //将刚才创建的新头像 从下方待加入的位置移动到上方头像队列中
            for (int i = 0; i < newParticipantsPortrait.Count; i++)
            {
                GameObject portrait = newParticipantsPortrait[i];
                Vector2 pos = (portrait.transform as RectTransform).anchoredPosition;
                int indexOfPortrait = turnPortraits.IndexOf(portrait);
                if (i != newParticipantsPortrait.Count - 1)
                {
                    SmoothSlide(newParticipantsPortrait[i], pos + new Vector2(0, 100), indexOfPortrait);
                }
                else
                {
                    yield return SmoothSlide(newParticipantsPortrait[i], pos + new Vector2(0, 100), indexOfPortrait);
                }
            }
            //由于加入了新头像, 为了居中布局, 调整所有头像的位置
            int participantsCount = Mathf.Min(turnPortraits.Count, MaxShowCount); //头像列表显示头像数
            uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0); //头像列表起始位置 
            //uiPos = new Vector2((turnPortraits.Count * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0);
            for (int i = 0; i < turnPortraits.Count; i++)
            {
                if (turnPortraits[i] == spliter)
                {
                    uiPos.x -= portraitWidth;
                    uiPos.x += spliterWidth;
                }
                if (i != turnPortraits.Count - 1)
                {
                    StartCoroutine(SmoothSlide(turnPortraits[i], uiPos, i + 1));
                }
                else
                {
                    yield return SmoothSlide(turnPortraits[i], uiPos, i + 1); //最后一个头像更新位置完成前一直等待, 防止不同动画的SmoothSlide协程冲突
                }
                uiPos.x += portraitWidth;
            }
        }

        relayouting = false;
    }

    /// <summary>
    /// 将portrait平滑地滑动到targertPor, 并根据index决定现隐
    /// </summary>
    /// <param name="portrait"></param>
    /// <param name="targetPos"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private IEnumerator SmoothSlide(GameObject portrait, Vector2 targetPos, int index)
    {
        Vector2 nowPos = (portrait.transform as RectTransform).anchoredPosition;
        while (Vector2.Distance(nowPos, targetPos) > 3f)
        {
            nowPos = (portrait.transform as RectTransform).anchoredPosition;
            (portrait.transform as RectTransform).anchoredPosition = Vector2.Lerp(nowPos, targetPos, Time.deltaTime * relayoutSpeed);
            yield return null;
        }
        (portrait.transform as RectTransform).anchoredPosition = targetPos;

        if (index > MaxShowCount)
        {
            portrait.SetActive(false); //超过最大显示数则暂时不显示
        }
        else
        {
            portrait.SetActive(true);
        }
    }

    /// <summary>
    /// 将portrait平衡地向下移出并隐去
    /// </summary>
    /// <param name="portrait"></param>
    /// <returns></returns>
    private IEnumerator SmoothDownAndHide(GameObject portrait, bool die)
    {
        if(portrait == null)
        {
            yield break;
        }

        Image portraitFrameImg = portrait.GetComponent<Image>();
        Color portraitFrameImgTargetColor = portraitFrameImg.color;
        portraitFrameImgTargetColor.a = 0;

        Image portraitImg = portrait.transform.GetChild(0).GetComponent<Image>();
        if (die)
        {
            portraitImg.color = Color.gray;
            yield return new WaitForSeconds(0.25f);
        }
        Color portraitImgTargetColor = portraitImg.color;
        portraitImgTargetColor.a = 0;

        Image hpBarFillImg = portrait.transform.GetChild(1).GetComponent<Image>();
        Color hpBarFillImgTargetColor = hpBarFillImg.color;
        hpBarFillImgTargetColor.a = 0;


        Vector2 nowPos = (portrait.transform as RectTransform).anchoredPosition;
        Vector2 targetPos = nowPos - new Vector2(0, 100);
        while(Vector2.Distance(nowPos, targetPos) > 3f)
        {
            nowPos = (portrait.transform as RectTransform).anchoredPosition;
            (portrait.transform as RectTransform).anchoredPosition = Vector2.Lerp(nowPos, targetPos, Time.deltaTime * relayoutSpeed);

            portraitFrameImg.color = Color.Lerp(portraitFrameImg.color, portraitFrameImgTargetColor, Time.deltaTime * relayoutSpeed);
            portraitImg.color = Color.Lerp(portraitImg.color, portraitImgTargetColor, Time.deltaTime * relayoutSpeed);
            hpBarFillImg.color = Color.Lerp(hpBarFillImg.color, hpBarFillImgTargetColor, Time.deltaTime * relayoutSpeed);

            yield return null;
        }
        (portrait.transform as RectTransform).anchoredPosition = targetPos;
        portraitFrameImg.color = portraitFrameImgTargetColor;
        portraitImg.color = portraitImgTargetColor;
        hpBarFillImg.color = hpBarFillImgTargetColor;

        Destroy(portrait);
    }


    /// <summary>
    /// 移除角色的回合制头像UI
    /// </summary>
    public void RemoveParticipantPortrait(Character participant)
    {
        Debug.Log("RemoveParticipantPortrait");
        StartCoroutine(SmoothRemoveParticipantPortrait(participant));
    }

    private IEnumerator SmoothRemoveParticipantPortrait(Character participant)
    {
        yield return new WaitWhile(() => relayouting);
        relayouting = true;

        for (int i = 0; i < portraitMap[participant].Count; i++)
        {
            turnPortraits.Remove(portraitMap[participant][i]);
            if(i < 1)
            {
                StartCoroutine(SmoothDownAndHide(portraitMap[participant][i], true));
                yield return null;
            }
            else
            {
                yield return SmoothDownAndHide(portraitMap[participant][i], true);
            }
        }
        foreach(GameObject portrait in portraitMap[participant])
        {
            characterMap.Remove(portrait);
        }
        portraitMap.Remove(participant);
        hpBarFillMap.Remove(participant);

        //更新剩下头像的位置
        int participantsCount = Mathf.Min(turnPortraits.Count, MaxShowCount); //头像列表显示头像数
        Vector2 uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0); //头像列表起始位置
        //Vector2 uiPos = new Vector2((turnPortraits.Count * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0);
        for (int i = 0; i < turnPortraits.Count; i++)
        {
            if (turnPortraits[i] == spliter)
            {
                uiPos.x -= portraitWidth;
                uiPos.x += spliterWidth;
            }
            if(i < turnPortraits.Count - 1)
            {
                StartCoroutine(SmoothSlide(turnPortraits[i], uiPos, i + 1));
            }
            else
            {
                yield return null;
                yield return SmoothSlide(turnPortraits[i], uiPos, i + 1); //最后一个头像更新位置完成前一直等待, 防止不同动画的SmoothSlide协程冲突
            }

            uiPos.x += portraitWidth;
        }

        relayouting = false;
    }

    /// <summary>
    /// 更新回合制头像的血条长度
    /// </summary>
    /// <param name="participant"></param>
    public void UpdateTurnPortraitHpBarFill(Character participant)
    {
        if (CoroutineManager.Instance.TaskInGroupIsEmpty(participant.info.name + "hpBarUpdate"))
        {
            CoroutineManager.Instance.AddTaskToGroup(SmoothUpdateHpBar(participant), participant.info.name + "hpBarUpdate");
            CoroutineManager.Instance.StartGroup(participant.info.name + "hpBarUpdate");
        }
        else
        {
            CoroutineManager.Instance.AddTaskToGroup(SmoothUpdateHpBar(participant), participant.info.name + "hpBarUpdate");
        }
    }

    /// <summary>
    /// 更新回合制头像的血条长度的协程方法
    /// </summary>
    /// <param name="participant"></param>
    /// <returns></returns>
    private IEnumerator SmoothUpdateHpBar(Character participant)
    {
        //更新血条长度
        float targetFill = participant.info.hp / (participant.info.maxHp * 1.0f);

        List<Image> hpFillImg = new List<Image>();
        for(int i = 0; i < hpBarFillMap[participant].Count; i++)
        {
            hpFillImg.Add(hpBarFillMap[participant][i].GetComponent<Image>());
        }
        if(hpFillImg.Count == 0)
        {
            yield break;
        }

        while (Mathf.Abs(hpFillImg[0].fillAmount - targetFill) > 0.1f)
        {
            for(int i = 0; i < hpFillImg.Count; i++)
            {
                hpFillImg[i].fillAmount = Mathf.Lerp(hpFillImg[i].fillAmount, targetFill, Time.deltaTime * relayoutSpeed);
                yield return null;
            }
        }
        for (int i = 0; i < hpFillImg.Count; i++)
        {
            hpFillImg[i].fillAmount = targetFill;
        }
    }

    /// <summary>
    /// 根据当前行动点数和预估消耗更新行动点数UI
    /// </summary>
    /// <param name="actorActionPoint">行动者行动点数</param>
    /// <param name="cost">行动点预估消耗</param>
    public void UpdateActionPointBalls(float actorActionPoint, float acitonPointCost)
    {
        int actionPoint = Mathf.FloorToInt(actorActionPoint);
        int cost = Mathf.CeilToInt(acitonPointCost);
        int index = 0;
        for(; index < actionPoint && index < 6; index++)
        {
            actionPointBalls[index].sprite = greenBall; //更新剩余行动点数UI为绿色
        }
        for(int i = index - 1; index - 1 - i < cost && i >= 0; i--)
        {
            actionPointBalls[i].sprite = redBall; //更新预估消耗行动点数UI为红色
        }
        for(int i = index; i < 6; i++)
        {
            actionPointBalls[i].sprite = defaultBall; //将剩下的行动点数UI设置为默认灰色
        }
    }

    /// <summary>
    /// 开始/结束回合制按钮的点击监听函数
    /// </summary>
    private void OnClickStartAndEndTurnButton()
    {
        endPlayerTurnButton.interactable = false; //暂时让Button不可被点击, 限制点击频率
        Invoke(nameof(EnableButton), cooldown); //在cooldown时间过后, 恢复Button

        if (PartyManager.Instance == null || PartyManager.Instance.leader == null || TurnManager.Instance == null)
        {
            return;
        }
        if (PartyManager.Instance.leader.isInTurn)
        {
            TurnManager.Instance.EndTurn();
        }
        else
        {
            TurnManager.Instance.StartTurn(new List<Character>() { PartyManager.Instance.leader }, true);
        }
    }

    /// <summary>
    /// 主动结束玩家回合按钮的点击监听函数
    /// </summary>
    private void OnClickEndPlayerTurnButton()
    {
        endPlayerTurnButton.interactable = false;
        Invoke(nameof(EnableButton), cooldown);
        TurnManager.Instance.endPlayerTurn = true;
    }

    /// <summary>
    /// 主动推迟玩家回合按钮的点击监听函数
    /// </summary>
    private void OnClickDelayPlayerTurnButton()
    {
        delayPlayerTurnButton.interactable = false;
        Invoke(nameof(EnableButton), cooldown);
        TurnManager.Instance.delayPlayerTurn = true;
    }

    /// <summary>
    /// 恢复所有玩家按钮
    /// </summary>
    private void EnableButton()
    {
        startAndEndTurnButton.interactable = true;
        endPlayerTurnButton.interactable = true;
        delayPlayerTurnButton.interactable = true;
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}
