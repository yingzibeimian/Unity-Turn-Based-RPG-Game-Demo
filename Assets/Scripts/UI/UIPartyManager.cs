using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// ����UI�ű�, ������PartyUI��
/// </summary>
public class UIPartyManager : MonoBehaviour
{
    public static UIPartyManager Instance;

    public GameObject portraitPrefab; //ͷ��Ԥ����
    public GameObject buffIconPrefab; //Buffͼ��Ԥ����
    public Vector2 startPos = Vector2.zero; //ͷ����ʼλ��
    public float portraitSpacing = 160.0f; //ͬһ�ֶ�ͷ��ļ������
    public float groupSpacing = 30.0f; //��ͬ�ֶ�֮��ͷ��ļ������
    public float relayoutSpeed = 15.0f; //���²���UIͷ��ʱͷ���ƶ����ٶ�

    public Color highlightColor = Color.white; //�߹�(����)�߿���ɫ
    private Color followerColor = Color.gray; //���ѱ߿���ɫ

    public Image bottomPortrait;
    public GameObject buffTip; //buff��ʾ���

    private GameObject lastLeaderPortrait; //��һ�����ؽ�ɫ��Ӧ��UIͷ������

    private Character draggingChar; //����϶���ͷ��Ķ�Ӧ��ɫ
    public bool relayouting = false; //����Ƿ��������²���UIͷ��

    private Dictionary<Character, GameObject> portraitDic = new Dictionary<Character, GameObject>(); //key:��ɫ value:ͷ������
    private Dictionary<Buff, GameObject> buffIconDic = new Dictionary<Buff, GameObject>(); //key:��ɫBuff value:Buffͼ��

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
    /// ��ʼ������UI����
    /// </summary>
    public IEnumerator InitializaPartyUI()
    {
        //��ʼ��������ͷ��
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

                uiPos -= new Vector2(0, portraitSpacing); //����ͷ����ΪportraitSpacing

                yield return null;
            }
            uiPos -= new Vector2(0, groupSpacing); //��ͬ�����ΪportraitSpacing + groupSpacing
        }
    }

    /// <summary>
    /// ��UI�ϴ���character��Ӧ��ͷ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="uiPos"></param>
    private void CreatePortrait(Character character, Vector2 uiPos)
    {
        GameObject portrait = Instantiate(portraitPrefab); //ʵ��������Ԥ����
        portrait.transform.SetParent(transform); //��ͷ��Ԥ������ΪPartyUI���Ӷ���
        (portrait.transform as RectTransform).anchoredPosition = uiPos; //����ͷ��λ��
        portrait.transform.localScale = Vector3.one; //ȷ��������ȷ
        portrait.name = character.info.name + "_Portrait"; //����ͷ������
        portrait.transform.Find("Portrait").GetComponent<Image>().sprite = character.info.portrait; //����ͷ��ͼƬ

        portraitDic.Add(character, portrait); //������ɫ�Ͷ�Ӧͷ��֮��Ĺ�ϵ

        //Ϊ�����ϵ�UIͷ����������¼�����
        EventTrigger trigger = portrait.GetComponent<EventTrigger>();
        AddEvent(trigger, EventTriggerType.BeginDrag, OnBeginDrag);
        AddEvent(trigger, EventTriggerType.Drag, OnDrag);
        AddEvent(trigger, EventTriggerType.EndDrag, OnEndDrag);
        AddEvent(trigger, EventTriggerType.PointerClick, OnClick);

        if(character == PartyManager.Instance.leader)
        {
            lastLeaderPortrait = portrait;
            UpdateHighlight(character); //��ʼ��ͷ��UIʱͬʱ�߹�����ͷ��߿�
        }
    }

    /// <summary>
    /// (Ϊͷ��)����¼�����
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
    /// ��ʼ��ק��Ӧ����
    /// </summary>
    /// <param name="data"></param>
    private void OnBeginDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData; //��ÿ�ʼ�϶�ʱ����õ�����
        if (relayouting) //if (relayouting && TurnManager.Instance.isInTurn) //��ֹ���²��ֹ���������קͷ�� �� �غ�������ק���ͷ��
        {
            return;
        }
        draggingChar = GetCharacterFromPortrait(pointerData.pointerDrag); //�ӵ�ǰָ��(���)�϶��Ķ���õ���ɫ
        
        //�л�����
        if(draggingChar != null && !draggingChar.isInTurn) //��ֹ��ק��ɫΪ�� ���� ��ק�غ����е�ͷ��
        {
            PartyManager.Instance.SwitchLeader(draggingChar);
        }
    }

    /// <summary>
    /// ��ק����Ӧ����
    /// </summary>
    /// <param name="data"></param>
    private void OnDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;
        if (relayouting && draggingChar.isInTurn) //if(relayouting && TurnManager.Instance.isInTurn) //��ֹ���²��ֹ���������קͷ��
        {
            return;
        }
        pointerData.pointerDrag.GetComponent<RectTransform>().anchoredPosition += pointerData.delta / GetComponentInParent<Canvas>().scaleFactor;
    }

    /// <summary>
    /// ������ק��Ӧ����
    /// </summary>
    /// <param name="data"></param>
    private void OnEndDrag(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        if (relayouting && draggingChar.isInTurn) //if(relayouting && TurnManager.Instance.isInTurn) //��ֹ���²��ֹ���������קͷ��
        {
            return;
        }

        //�������Ƿ�����������ɫͷ����
        Character targetChar = null; //�϶�����ʱ�϶�ͷ�����ڵ�Ŀ��ͷ��
        List<RaycastResult> results = GetRaycastResults(); //��ǰ�������λ�õ����߼��õ�������UIԪ��

        foreach(RaycastResult result in results)
        {
            if(result.gameObject != portraitDic[draggingChar]) //�����⵽�����岻�������϶���ͷ������
            {
                targetChar = GetCharacterFromPortrait(result.gameObject); //������õ�Ŀ���ɫ��Ϣ
                break;
            }
        }

        UpdateGroups(targetChar); //���¶���ϵͳ�صĶ������groups
        StartCoroutine(SmoothRelayout()); //ƽ�������²���ͷ��UI
    }

    /// <summary>
    /// �����Ӧ����
    /// </summary>
    /// <param name="data"></param>
    private void OnClick(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;

        if (relayouting) //if (relayouting && TurnManager.Instance.isInTurn) //��ֹ���²��ֹ������ֵ��ͷ��
        {
            return;
        }

        GameObject clickedObj = pointerData.pointerCurrentRaycast.gameObject;
        Character clickedChar = GetCharacterFromPortrait(clickedObj); //�ӵ�ǰָ��(���)���߼�⵽�Ķ���õ���ɫ
        if (clickedChar != null)
        {
            PartyManager.Instance.SwitchLeader(clickedChar);
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
    /// ��ק����¶������
    /// </summary>
    /// <param name="targetChar"></param>
    private void UpdateGroups(Character targetChar)
    {
        if(targetChar != null) //�����⵽��Ŀ��ͷ���ɫ
        {
            //��������ק��ͷ��Ķ�Ӧ��ɫ �ƶ��� Ŀ��ͷ���ɫ���ڵķ���
            PartyManager.Instance.MoveToGroup(draggingChar, targetChar);
        }
        else //���û�м�⵽Ŀ��ͷ���ɫ
        {
            //Ϊ������ק��ͷ��Ķ�Ӧ��ɫ�����µķ���
            PartyManager.Instance.CreatNewGroup(draggingChar);
        }
    }

    /// <summary>
    /// ����UIͷ�񲼾�, ��ͷ���ԭλ��ƽ�����ƶ�����λ��
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
                if (portraitDic.ContainsKey(character)) //���Ҫ���µĽ�ɫͷ��֮ǰ�ʹ���
                {
                    GameObject portrait = portraitDic[character];
                    Vector2 nowPos = (portrait.transform as RectTransform).anchoredPosition;
                    while (Vector2.Distance(nowPos, targetPos) > 0.5f)
                    {
                        nowPos = (portrait.transform as RectTransform).anchoredPosition;
                        (portrait.transform as RectTransform).anchoredPosition = Vector2.Lerp(nowPos, targetPos, Time.deltaTime * relayoutSpeed);
                        yield return null; //ÿ֡����һ����, �������
                    }
                    (portrait.transform as RectTransform).anchoredPosition = targetPos;
                }
                else //���Ҫ���µĽ�ɫͷ��֮ǰ������
                {
                    CreatePortrait(character, targetPos);
                    yield return null;
                }

                targetPos -= new Vector2(0, portraitSpacing); //����ͷ����ΪportraitSpacing
            }
            targetPos -= new Vector2(0, groupSpacing); //��ͬ�����ΪportraitSpacing + groupSpacing
        }

        relayouting = false;
    }

    /// <summary>
    /// ��������ͷ��߿�߹�
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
    /// ��������ͷ��߿�߹�(����)
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
    /// �����ײ�����ͷ��
    /// </summary>
    public void ChangeBottomLeaderPortrait(Character leader)
    {
        bottomPortrait.sprite = leader.info.portrait;
    }

    /// <summary>
    /// �ڶ����ɫͷ���Ա����Buffͼ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="buff"></param>
    public void AddBuffIconBesidesPortrait(Character character, Buff buff)
    {
        //���û�н�ɫ��Ӧ�Ķ���ͷ��, ��ֱ�ӷ���
        if (!portraitDic.ContainsKey(character))
        {
            return;
        }
        Transform buffParent = portraitDic[character].transform.Find("BuffParent"); //�ҵ�����ˮƽ���������buffParent������
        GameObject buffIcon = Instantiate(buffIconPrefab, buffParent); //ʵ����Buffͼ��Ԥ����
        Transform icon = buffIcon.transform.Find("BuffIcon");
        icon.GetComponent<Image>().sprite = buff.buffIcon; //����Buffͼ��
        buffIconDic.TryAdd(buff, buffIcon); //����buffIconDic

        //Ϊͼ��icon��ť���ָ�������˳��¼�
        EventTrigger trigger = icon.GetComponent<EventTrigger>();
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) =>
        {
            PointerEventData pointerData = data as PointerEventData;

            buffTip.transform.Find("BuffIcon").GetComponent<Image>().sprite = buff.buffIcon;
            if(buff.timeType == BuffTimeType.Temporary)
            {
                buffTip.transform.Find("BuffName").GetComponent<TextMeshProUGUI>().text = $"{buff.buffName}\tʣ��غ���: {buff.remainingTurns}";
            }
            else if (buff.timeType == BuffTimeType.Permanet)
            {
                buffTip.transform.Find("BuffName").GetComponent<TextMeshProUGUI>().text = $"{buff.buffName}\tʣ��غ���: ����";
            }
            buffTip.transform.Find("BuffDescription_Effects").GetComponent<TextMeshProUGUI>().text = $"Ч��:\n{buff.buffDescription_Effects}";
            buffTip.transform.Find("BuffDescription_BG").GetComponent<TextMeshProUGUI>().text = $"({buff.buffDescription_BG})";

            buffTip.transform.position = buffIconDic[buff].transform.position + new Vector3(0, -32);
            buffTip.SetActive(true); //��ʾBuffTip
        });
        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) =>
        {
            buffTip.SetActive(false); //����BuffTip
        });
        trigger.triggers.Add(enterEntry);
        trigger.triggers.Add(exitEntry);
    }

    /// <summary>
    /// �����ڶ����ɫͷ���Աߵ�Buffͼ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="buff"></param>
    public void UpdateBuffIconBesidesPortrait(Buff buff)
    {
        //���û��buff��ӦbuffIcon, ��ֱ�ӷ���
        if (!buffIconDic.ContainsKey(buff))
        {
            return;
        }
        //����Buffʣ��ʱ�����ͼ��߿�
        buffIconDic[buff].transform.Find("BuffRemainMask").GetComponent<Image>().fillAmount = buff.remainingTurns / (buff.durationTurns * 1.0f);
    }

    /// <summary>
    /// �Ƴ��ڶ����ɫͷ���Աߵ�Buffͼ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="buff"></param>
    public void RemoveBuffIconBesidesPortrait(Buff buff)
    {
        //���û��buff��ӦbuffIcon, ��ֱ�ӷ���
        if (!buffIconDic.ContainsKey(buff))
        {
            return;
        }
        GameObject buffIcon = buffIconDic[buff];
        buffIconDic.Remove(buff); //���ֵ����Ƴ�
        Destroy(buffIcon); //����Buffͼ��

        buffTip.SetActive(false); //����BuffTip���
    }
}
