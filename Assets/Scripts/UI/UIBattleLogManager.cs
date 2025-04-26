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

    public Button battleLogButton; //�����ر�ս����־�İ�ť
    public GameObject battleLogTip; //��ť��ʾ

    public GameObject battleLog; //ս����־
    public ScrollRect scrollRect; //ScrollView��ScrollRect���
    public Transform contentParent; //ScrollView��Content������
    public GameObject logItemPrefab; //��־��Ŀ��Ԥ����
    public float logHeight = 35.0f; //��־��Ŀ�߶�

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
    /// �����ⲿ���������־
    /// </summary>
    /// <param name="message"></param>
    public void AddLog(string message)
    {
        // ��������־��Ŀ
        GameObject newLog = Instantiate(logItemPrefab, contentParent);
        newLog.GetComponent<TextMeshProUGUI>().text = message;

        //�������ݸ߶�
        (contentParent as RectTransform).sizeDelta += new Vector2(0, logHeight);

        UpdateBattleLogUI();
    }

    /// <summary>
    /// ��ʾս����־����
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
    /// ����ս����־����
    /// </summary>
    public void HideBattleLogPanel()
    {
        if (battleLog != null && battleLog.activeSelf)
        {
            battleLog.SetActive(false);
        }
    }

    /// <summary>
    /// ��ʼ��ս����־��ť
    /// </summary>
    private void InitializeBattleLogButton()
    {
        //��ӵ����������
        battleLogButton.onClick.AddListener(OnClickBattleLogButton);

        //���ָ�������˳��¼�
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
    /// ս����־��ť�����������
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
    /// ����ս����־UI, ����������ײ�
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
    /// ��ս����־�������ײ���Э��
    /// </summary>
    /// <returns></returns>
    private IEnumerator ScrollToBottom()
    {
        //�ȴ�һ֡��UI��ɲ��ָ���
        yield return new WaitForEndOfFrame();

        //������λ��ǿ�����õ��ײ���0��ʾ�ײ�, 1��ʾ������
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
