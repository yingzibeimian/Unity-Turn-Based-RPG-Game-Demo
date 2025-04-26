using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIBattleLogManager : MonoBehaviour
{
    private static UIBattleLogManager instance;
    public static UIBattleLogManager Instance => instance;

    public Button battleLogButton; //开启关闭战斗日志的按钮
    public GameObject battleLogTip; //按钮提示

    public GameObject battleLog; //战斗日志
    public ScrollRect scrollRect; //ScrollView的ScrollRect组件
    public Transform contentParent; //ScrollView的Content父对象
    public GameObject logItemPrefab; //日志条目的预制体
    public float logHeight = 35.0f; //日志条目高度

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        HideBattleLogPanel();
        InitializeBattleLogButton();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 用于外部调用添加日志
    /// </summary>
    /// <param name="message"></param>
    public void AddLog(string message)
    {
        // 创建新日志条目
        GameObject newLog = Instantiate(logItemPrefab, contentParent);
        newLog.GetComponent<TextMeshProUGUI>().text = message;

        //增加内容高度
        (contentParent as RectTransform).sizeDelta += new Vector2(0, logHeight);

        UpdateBattleLogUI();
    }

    /// <summary>
    /// 显示战斗日志界面
    /// </summary>
    public void ShowBattleLogPanel()
    {
        if(battleLog != null && !battleLog.activeSelf)
        {
            battleLog.SetActive(true);
        }

        UpdateBattleLogUI();
    }

    /// <summary>
    /// 隐藏战斗日志界面
    /// </summary>
    public void HideBattleLogPanel()
    {
        if (battleLog != null && battleLog.activeSelf)
        {
            battleLog.SetActive(false);
        }
    }

    /// <summary>
    /// 初始化战斗日志按钮
    /// </summary>
    private void InitializeBattleLogButton()
    {
        //添加点击监听函数
        battleLogButton.onClick.AddListener(OnClickBattleLogButton);

        //添加指针进入和退出事件
        EventTrigger trigger = battleLogButton.GetComponent<EventTrigger>();
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) =>
        {
            if (battleLogTip != null)
            {
                battleLogTip.SetActive(true);
            }
        });
        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) =>
        {
            if (battleLogTip != null)
            {
                battleLogTip.SetActive(false);
            }
        });
        trigger.triggers.Add(enterEntry);
        trigger.triggers.Add(exitEntry);
    }


    /// <summary>
    /// 战斗日志按钮点击监听函数
    /// </summary>
    private void OnClickBattleLogButton()
    {
        if (battleLog.activeSelf)
        {
            HideBattleLogPanel();
        }
        else
        {
            ShowBattleLogPanel();
        }
    }

    /// <summary>
    /// 更新战斗日志UI, 让其滚动到底部
    /// </summary>
    private void UpdateBattleLogUI()
    {
        if (CoroutineManager.Instance.TaskInGroupIsEmpty("battleLogUpdate"))
        {
            CoroutineManager.Instance.AddTaskToGroup(ScrollToBottom(), "battleLogUpdate");
            CoroutineManager.Instance.StartGroup("battleLogUpdate");
        }
        else
        {
            CoroutineManager.Instance.AddTaskToGroup(ScrollToBottom(), "battleLogUpdate");
        }
    }

    /// <summary>
    /// 让战斗日志滚动到底部的协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator ScrollToBottom()
    {
        //等待一帧让UI完成布局更新
        yield return new WaitForEndOfFrame();

        //将滚动位置强制设置到底部（0表示底部, 1表示顶部）
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
