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

    [SerializeField] private float cooldown = 0.5f; //��ť������ʱ��, ���ڰ�ť���Ƶ��

    public Button startAndEndTurnButton; //�����͹رջغ��Ƶİ�ť

    public GameObject turnPlayerPanel; //��һغ�UI���
    public Button endPlayerTurnButton; //����������һغϰ�ť
    public Button delayPlayerTurnButton; //�����Ƴ���һغϰ�ť
    public GameObject delayButtonTip; //�Ƴٰ�ť��ʾ

    public GameObject turnParticipantsObj; //�غ���ͷ���б�ĸ�����
    public GameObject turnPortraitFrame; //�غ���ͷ��Ԥ����
    public GameObject turnPortraitHpBarFill; //�غ���ͷ��Ѫ��Ԥ����
    public GameObject turnSpliter; //�غ���ͷ���б�ָ���
    public Color portraitFrameDefaultColor = new Color(150, 150, 150); //ͷ��߿�Ĭ����ɫ
    private GameObject spliter;
    public int MaxShowCount = 20; //�غ���ͷ���б���������ʾ��ͷ������
    public float portraitWidth = 70.0f; //ͷ��Ԥ������
    public float spliterWidth = 24.0f; //�ָ������
    public Vector2 hpBarFillPos = new Vector2(-28, -36); //Ѫ��λ��
    private List<GameObject> turnPortraits = new List<GameObject>();
    private Dictionary<Character, List<GameObject>> portraitMap = new Dictionary<Character, List<GameObject>>(); //��ɫ��غ���ͷ��֮���ӳ��
    private Dictionary<Character, List<GameObject>> hpBarFillMap = new Dictionary<Character, List<GameObject>>(); //��ɫ��غ���ͷ��Ѫ��֮���ӳ��
    private Dictionary<GameObject, Character> characterMap = new Dictionary<GameObject, Character>(); //ͷ�����ɫ֮���ӳ��

    public float relayoutSpeed = 10.0f; //���²����ٶ�
    public bool relayouting = false; //���ڱ��ͷ���Ƿ��������²�����

    public List<Image> actionPointBalls = new List<Image>(); //������ʾ�ж�������ͼ��
    public Sprite defaultBall;
    public Sprite greenBall;
    public Sprite redBall;

    public GameObject turnMessagePanel; //�غ���Ϣ���
    public Image turnStartTipImg; //��ս��ʾ����ͼ
    public TextMeshProUGUI turnStartTipText; //��ս��ʾ����
    public TextMeshProUGUI turnCampTipText; //��Ӫ��ʾ����

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        //��ʼ��StartAndEndTurnButton��ť
        startAndEndTurnButton.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f; //��ť��Ӧ����Сalphaֵ
        startAndEndTurnButton.onClick.AddListener(OnClickStartAndEndTurnButton); //Ϊ��ť�����Ӽ�������
        //��ʼ����һغ����
        InitializeTurnPlayerPanel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// ��ʼ���غ���UI
    /// </summary>
    public void InitializeTurnUI()
    {
        ShowTurnMessagePanel();
        StartCoroutine(InitialiazeTurnPortraitUI());
    }

    /// <summary>
    /// �����غ���ʱ����UI
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
    /// ��ʾ�غ�����Ϣ���
    /// </summary>
    public void ShowTurnMessagePanel()
    {
        turnMessagePanel.SetActive(true);
    }

    /// <summary>
    /// ��ʾ��ս��ʾ
    /// </summary>
    public void ShowTurnStartTip()
    {
        StartCoroutine(SmoothShowAndHideTurnStartTip());
    }

    /// <summary>
    /// ��ʾ��ս��ʾ
    /// </summary>
    /// <returns></returns>
    private IEnumerator SmoothShowAndHideTurnStartTip()
    {
        //��ʾ
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

        //�ȴ�
        yield return new WaitForSeconds(1.5f);

        //����
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
        //ʧ���ָ�ԭ��ɫ
        turnStartTipImg.color = imgColor_Copy;
        turnStartTipText.color = textColor_Copy;
    }

    /// <summary>
    /// ���ػغ�����Ϣ���
    /// </summary>
    public void HideTurnMessagePanel()
    {
        turnMessagePanel.SetActive(false);
    }

    /// <summary>
    /// ���»غ�����Ӫ��ʾ
    /// </summary>
    /// <param name="portrait"></param>
    private void UpdateTurnCampTip(GameObject portrait)
    {
        if (characterMap[portrait].CompareTag("Player"))
        {
            turnCampTipText.text = "��Ļغ�";
            turnCampTipText.color = Color.white;
        }
        else
        {
            turnCampTipText.text = "���˻غ�";
            turnCampTipText.color = Color.red;
        }
    }

    /// <summary>
    /// ��ʾ��һغ����
    /// </summary>
    public void ShowTurnPlayerPanel()
    {
        turnPlayerPanel.SetActive(true);
    }

    /// <summary>
    /// ������һغ����
    /// </summary>
    public void HideTurnPlayerPanel()
    {
        turnPlayerPanel.SetActive(false);
    }

    /// <summary>
    /// ��ʼ����һغ����
    /// </summary>
    public void InitializeTurnPlayerPanel()
    {
        endPlayerTurnButton.onClick.AddListener(OnClickEndPlayerTurnButton); //Ϊ����������һغϰ�ť��ӵ����������
        delayPlayerTurnButton.onClick.AddListener(OnClickDelayPlayerTurnButton); //Ϊ�����Ƴ���һغϰ�ť��ӵ����������

        //Ϊ�ӳٻغϰ�ť���ָ�������˳��¼�
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

        HideTurnPlayerPanel(); //�������
    }

    /// <summary>
    /// ��ʼ���غ��Ʋ�����ͷ���б�
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitialiazeTurnPortraitUI()
    {
        yield return new WaitWhile(() => relayouting);
        relayouting = true;

        turnParticipantsObj.SetActive(true);

        int participantsCount = Mathf.Min(TurnManager.Instance.participants.Count + TurnManager.Instance.nextTurnParticipants.Count, MaxShowCount); //ͷ���б���ʾͷ����
        Vector2 uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0); //ͷ���б���ʼλ�� 
        //Vector2 uiPos = startPos;
        int index = 1; //������ͷ�����
        foreach(Character participant in TurnManager.Instance.participants)
        {
            CreatPortrait(participant, uiPos, index++);
            uiPos.x += portraitWidth;
            yield return null;
        }

        //��ʼ���ָ���
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

        //��ǰ�غ��ж��ߵ�ͷ��Ŵ���1.2��, �߿���ɫ����Ϊ��ɫ
        turnPortraits[0].transform.localScale = Vector3.one * 1.2f;
        if (characterMap[turnPortraits[0]].CompareTag("Player"))
        {
            turnPortraits[0].GetComponent<Image>().color = Color.white;
        }

        //������Ӫ��ʾ
        UpdateTurnCampTip(turnPortraits[0]);

        relayouting = false;
    }

    /// <summary>
    /// �ڻغ���UI�еĶ�Ӧλ��uiPos��������Ӧ��ɫ��ͷ��, ����index��������, ��������ɫ��ͷ��Ѫ��֮��Ķ�Ӧ��ϵ
    /// </summary>
    /// <param name="participant"></param>
    /// <param name="uiPos"></param>
    /// <param name="index"></param>
    private void CreatPortrait(Character participant, Vector2 uiPos, int index)
    {
        GameObject portrait = Instantiate(turnPortraitFrame); //ʵ����ͷ��Ԥ����
        portrait.transform.SetParent(turnParticipantsObj.transform); //��ͷ��Ԥ������ΪPartyUI���Ӷ���
        (portrait.transform as RectTransform).anchoredPosition = uiPos; //����ͷ��λ��
        portrait.transform.localScale = Vector3.one; //ȷ��������ȷ
        //portrait.name = character.info.name + "_Portrait"; //����ͷ������
        portrait.transform.GetChild(0).GetComponent<Image>().sprite = participant.info.portrait; //����ͷ��ͼƬ

        GameObject hpBarFill = Instantiate(turnPortraitHpBarFill); //ʵ����Ѫ��Ԥ����
        hpBarFill.transform.SetParent(portrait.transform);
        (hpBarFill.transform as RectTransform).anchoredPosition = hpBarFillPos;
        hpBarFill.transform.localScale = Vector3.one;
        hpBarFill.GetComponent<Image>().fillAmount = participant.info.hp / (participant.info.maxHp * 1.0f); //��ʼ��Ѫ������

        if (participant.CompareTag("Enemy"))
        {
            portrait.GetComponent<Image>().color = Color.red; //���˵�ͷ��߿�����Ϊ��ɫ
        }
        if(index > MaxShowCount)
        {
            portrait.SetActive(false); //���������ʾ������ʱ����ʾ
        }
        else
        {
            portrait.SetActive(true);
        }
        turnPortraits.Add(portrait);

        //������ɫ��ͷ��Ѫ��֮��Ķ�Ӧ��ϵ
        if (!portraitMap.ContainsKey(participant))
        {
            portraitMap.Add(participant, new List<GameObject>()); //������ɫ�Ͷ�Ӧͷ��֮��Ĺ�ϵ
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
    /// ��ǰ�ж��߷��Ƴٻغϵ������, �����غϺ����UI
    /// </summary>
    /// <param name="character"></param>
    public void UpdateTurnPortrait(Character character)
    {
        StartCoroutine(SmoothUpdateTurnPortrait(character));
    }

    /// <summary>
    /// ��ǰ�ж��߽����غϺ����UI��Э�̷���
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

        if (turnPortraits[0] != spliter) //�����һ�����岻�Ƿָ���
        {
            int participantsCount = Mathf.Min(turnPortraits.Count, MaxShowCount); //ͷ���б���ʾͷ����
            Vector2 uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0); //ͷ���б���ʼλ��
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
                    yield return SmoothSlide(turnPortraits[i], uiPos, i + 1); //���һ��ͷ�����λ�����ǰһֱ�ȴ�, ��ֹ��ͬ������SmoothSlideЭ�̳�ͻ
                }
                uiPos.x += portraitWidth;
            }
        }
        else //�����һ������ʱ�ָ���, ����������һ�ֻغ�
        {
            //����ʣ��ͷ���λ��
            int participantsCount = Mathf.Min(TurnManager.Instance.nextTurnParticipants.Count * 2, MaxShowCount); //ͷ���б���ʾͷ����
            Vector2 uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth - spliterWidth, 0); //ͷ���б���ʼλ�� 
            //Vector2 uiPos = new Vector2(((turnPortraits.Count + TurnManager.Instance.nextTurnParticipants.Count) * portraitWidth + spliterWidth) * -0.5f + portraitWidth - spliterWidth, 0);
            for (int i = 0; i < turnPortraits.Count; i++)
            {
                if (i != turnPortraits.Count - 1)
                {
                    StartCoroutine(SmoothSlide(turnPortraits[i], uiPos, i + 1));
                }
                else
                {
                    yield return SmoothSlide(turnPortraits[i], uiPos, i + 1); //���һ��ͷ�����λ�����ǰһֱ�ȴ�, ��ֹ��ͬ������SmoothSlideЭ�̳�ͻ
                }
                uiPos.x += portraitWidth;
            }
            //���·ָ�����λ��
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
            //�����һ�غϽ�ɫ��ͷ��
            foreach(Character participant in TurnManager.Instance.nextTurnParticipants)
            {
                CreatPortrait(participant, uiPos, turnPortraits.Count + 1);
                uiPos.x += portraitWidth;
                yield return null;
            }
        }

        //���»غ��ж���ͷ�����
        turnPortraits[0].transform.localScale = Vector3.one * 1.2f;
        if (characterMap[turnPortraits[0]].CompareTag("Player"))
        {
            turnPortraits[0].GetComponent<Image>().color = Color.white;
        }

        //������Ӫ��ʾ
        UpdateTurnCampTip(turnPortraits[0]);

        //yield return ResortNextTurnParticipantsPortraits();

        relayouting = false;
    }

    /// <summary>
    /// ��ǰ�غ��ж����ƳٻغϺ����UI
    /// </summary>
    public void UpdateTurnPortraitAfterDelay()
    {
        StartCoroutine(SmoothUpdateTurnPortraitAfterDelay());
    }

    private IEnumerator SmoothUpdateTurnPortraitAfterDelay()
    {
        yield return new WaitWhile(() => relayouting);
        relayouting = true;

        int spliterIndex = turnPortraits.IndexOf(spliter); //��÷ָ������±�
        //ȡ���Ե�ǰ�ж���ɫ�ĸ���
        GameObject delayProtrait = turnPortraits[0]; //�ƳٻغϽ�ɫ��ͷ��
        delayProtrait.transform.localScale = Vector3.one;
        delayProtrait.GetComponent<Image>().color = portraitFrameDefaultColor;
        yield return SmoothSlide(delayProtrait, (delayProtrait.transform as RectTransform).anchoredPosition - new Vector2(0, 100), 1); //���»���
        yield return SmoothSlide(delayProtrait, (turnPortraits[spliterIndex - 1].transform as RectTransform).anchoredPosition - new Vector2(0, 100), 1); //���һ���
        //���Ƴٽ�ɫͷ����뵽�ָ���֮ǰ
        turnPortraits.Insert(spliterIndex, delayProtrait);
        turnPortraits.RemoveAt(0);

        //��������ͷ��λ��
        int participantsCount = Mathf.Min(turnPortraits.Count, MaxShowCount); //ͷ���б���ʾͷ����
        Vector2 uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0); //ͷ���б���ʼλ��
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
                yield return SmoothSlide(turnPortraits[i], uiPos, i + 1); //���һ��ͷ�����λ�����ǰһֱ�ȴ�, ��ֹ��ͬ������SmoothSlideЭ�̳�ͻ
            }
            uiPos.x += portraitWidth;
        }

        //���»غ��ж���ͷ�����
        turnPortraits[0].transform.localScale = Vector3.one * 1.2f;
        if (characterMap[turnPortraits[0]].CompareTag("Player"))
        {
            turnPortraits[0].GetComponent<Image>().color = Color.white;
        }

        //������Ӫ��ʾ
        UpdateTurnCampTip(turnPortraits[0]);

        relayouting = false;
    }

    /// <summary>
    /// ���غ��Ʋ����ɫ�ȹ������仯 ���� ���µĽ�ɫ����ʱ, ����һ�غϲ����ߵ�ͷ�������������
    /// </summary>
    /// <returns></returns>
    public IEnumerator ResortNextTurnParticipantsPortraits()
    {
        yield return new WaitWhile(() => relayouting);
        relayouting = true;

        List<Character> participants = TurnManager.Instance.nextTurnParticipants; //�����һ�غϲ����ɫ�б�
        List<GameObject> newParticipantsPortrait = new List<GameObject>(); //�غ����¼����ߵ�ͷ���б�

        int spliterIndex = turnPortraits.IndexOf(spliter); //��÷ָ������±�
        Vector2 uiPos = (spliter.transform as RectTransform).anchoredPosition + new Vector2(portraitWidth, 0); //�������Ϊ�ָ��������һ��ͷ��λ��
        int index = turnPortraits.IndexOf(spliter) + 1; //������һ�غϲ����ߵ���λ�ڵ�ǰͷ���б��е��±�
        //GameObject nextTurnLastPortrait = turnPortraits[turnPortraits.Count - 1]; //ԭ��ͷ���б��е����һ��ͷ��
        //�����һ�ֻغ���ͷ����turnPortraits�еļ�¼
        for (int i = turnPortraits.Count - 1; i >= index; i--)
        {
            turnPortraits.RemoveAt(i);
        }
        //������һ�����в����ɫ��ͷ��λ��
        for (int i= 0; i < participants.Count; i++)
        {
            //��������¼�����, �Ѿ��лغ���ͷ��
            if (portraitMap.ContainsKey(participants[i]))
            {
                if(portraitMap[participants[i]].Count > 0)
                {
                    List<GameObject> portraits = portraitMap[participants[i]];
                    GameObject portrait = portraits[portraits.Count - 1];
                    //���Ҫ�ƶ���ͷ����ԭ��ͷ���б��е����һ��
                    //if (portrait != nextTurnLastPortrait)
                    //{
                    //    yield return SmoothSlide(portrait, uiPos, index++); //��participant��Ӧ��ͷ��(������1����2��)�����һ�� ƽ���ƶ��� uiPos��λ��
                    //}
                    ////���Ҫ�ƶ���ͷ����ԭ��ͷ���б��е����һ��, �͵ȴ����ƶ����
                    //else
                    //{
                    //    yield return SmoothSlide(portrait, uiPos, index++);
                    //}
                    yield return SmoothSlide(portrait, uiPos, index++);
                    turnPortraits.Add(portrait); //��ͷ�����¼�¼��turnPortraits��
                }
            }
            //������¼�����, ��û�лغ���ͷ��
            else
            {
                //���¼�����ͷ��Ӧ�ڵ�λ���·�, ������Ӧͷ��
                CreatPortrait(participants[i], uiPos - new Vector2(0, 100), index++);
                if (portraitMap.ContainsKey(participants[i]))
                {
                    List<GameObject> portraits = portraitMap[participants[i]];
                    newParticipantsPortrait.Add(portraits[portraits.Count - 1]);
                }
                yield return null;
            }
            uiPos.x += portraitWidth; //����uiPosλ��
        }
        //������¼�����
        if (newParticipantsPortrait.Count > 0)
        {
            //���ղŴ�������ͷ�� ���·��������λ���ƶ����Ϸ�ͷ�������
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
            //���ڼ�������ͷ��, Ϊ�˾��в���, ��������ͷ���λ��
            int participantsCount = Mathf.Min(turnPortraits.Count, MaxShowCount); //ͷ���б���ʾͷ����
            uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0); //ͷ���б���ʼλ�� 
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
                    yield return SmoothSlide(turnPortraits[i], uiPos, i + 1); //���һ��ͷ�����λ�����ǰһֱ�ȴ�, ��ֹ��ͬ������SmoothSlideЭ�̳�ͻ
                }
                uiPos.x += portraitWidth;
            }
        }

        relayouting = false;
    }

    /// <summary>
    /// ��portraitƽ���ػ�����targertPor, ������index��������
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
            portrait.SetActive(false); //���������ʾ������ʱ����ʾ
        }
        else
        {
            portrait.SetActive(true);
        }
    }

    /// <summary>
    /// ��portraitƽ��������Ƴ�����ȥ
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
    /// �Ƴ���ɫ�Ļغ���ͷ��UI
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

        //����ʣ��ͷ���λ��
        int participantsCount = Mathf.Min(turnPortraits.Count, MaxShowCount); //ͷ���б���ʾͷ����
        Vector2 uiPos = new Vector2((participantsCount * portraitWidth + spliterWidth) * -0.5f + portraitWidth, 0); //ͷ���б���ʼλ��
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
                yield return SmoothSlide(turnPortraits[i], uiPos, i + 1); //���һ��ͷ�����λ�����ǰһֱ�ȴ�, ��ֹ��ͬ������SmoothSlideЭ�̳�ͻ
            }

            uiPos.x += portraitWidth;
        }

        relayouting = false;
    }

    /// <summary>
    /// ���»غ���ͷ���Ѫ������
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
    /// ���»غ���ͷ���Ѫ�����ȵ�Э�̷���
    /// </summary>
    /// <param name="participant"></param>
    /// <returns></returns>
    private IEnumerator SmoothUpdateHpBar(Character participant)
    {
        //����Ѫ������
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
    /// ���ݵ�ǰ�ж�������Ԥ�����ĸ����ж�����UI
    /// </summary>
    /// <param name="actorActionPoint">�ж����ж�����</param>
    /// <param name="cost">�ж���Ԥ������</param>
    public void UpdateActionPointBalls(float actorActionPoint, float acitonPointCost)
    {
        int actionPoint = Mathf.FloorToInt(actorActionPoint);
        int cost = Mathf.CeilToInt(acitonPointCost);
        int index = 0;
        for(; index < actionPoint && index < 6; index++)
        {
            actionPointBalls[index].sprite = greenBall; //����ʣ���ж�����UIΪ��ɫ
        }
        for(int i = index - 1; index - 1 - i < cost && i >= 0; i--)
        {
            actionPointBalls[i].sprite = redBall; //����Ԥ�������ж�����UIΪ��ɫ
        }
        for(int i = index; i < 6; i++)
        {
            actionPointBalls[i].sprite = defaultBall; //��ʣ�µ��ж�����UI����ΪĬ�ϻ�ɫ
        }
    }

    /// <summary>
    /// ��ʼ/�����غ��ư�ť�ĵ����������
    /// </summary>
    private void OnClickStartAndEndTurnButton()
    {
        endPlayerTurnButton.interactable = false; //��ʱ��Button���ɱ����, ���Ƶ��Ƶ��
        Invoke(nameof(EnableButton), cooldown); //��cooldownʱ�����, �ָ�Button

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
    /// ����������һغϰ�ť�ĵ����������
    /// </summary>
    private void OnClickEndPlayerTurnButton()
    {
        endPlayerTurnButton.interactable = false;
        Invoke(nameof(EnableButton), cooldown);
        TurnManager.Instance.endPlayerTurn = true;
    }

    /// <summary>
    /// �����Ƴ���һغϰ�ť�ĵ����������
    /// </summary>
    private void OnClickDelayPlayerTurnButton()
    {
        delayPlayerTurnButton.interactable = false;
        Invoke(nameof(EnableButton), cooldown);
        TurnManager.Instance.delayPlayerTurn = true;
    }

    /// <summary>
    /// �ָ�������Ұ�ť
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
