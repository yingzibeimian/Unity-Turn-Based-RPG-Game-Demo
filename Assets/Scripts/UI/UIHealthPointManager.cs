using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthPointManager : MonoBehaviour
{
    private static UIHealthPointManager instance;
    public static UIHealthPointManager Instance => instance;
    
    //����Ѫ��UI
    public Image leaderHpBarFill;
    public TextMeshProUGUI leaderHpBarInfo;
    public float hpUpdateSpeed = 3.0f;
    private Coroutine leaderHpUpdateCoroutine;

    //�۲�Ŀ��Ѫ��UI
    public GameObject targetHpPanel;
    public Image targetHpBarFill;
    public TextMeshProUGUI targetHpBarInfo;
    public TextMeshProUGUI targetName;

    //Buffͼ�����
    public Transform buffParent; //Buffͼ�길����
    public GameObject buffIconPrefab; //Buffͼ��Ԥ����

    private void Awake() => instance = this;


    // Start is called before the first frame update
    void Start()
    {
        leaderHpUpdateCoroutine = StartCoroutine(InitializeLeaderHpBar());
        HideTargetHpPanel();
    }

    // Update is called once per frame
    void Update()
    {
        //����
        //if (Input.GetKeyDown(KeyCode.X))
        //{
        //    PartyManager.Instance.leader.TakeDamage(10, 1);
        //}
    }

    /// <summary>
    /// ��ʼ��Ѫ����Ϣ
    /// </summary>
    /// <returns></returns>
    public IEnumerator InitializeLeaderHpBar()
    {
        yield return new WaitWhile(() => PartyManager.Instance == null || PartyManager.Instance.leader == null);
        
        Character leader = PartyManager.Instance.leader;
        leaderHpBarInfo.text = $"{leader.info.hp} / {leader.info.maxHp}";
        float targetFill = leader.info.hp / (leader.info.maxHp * 1.0f);
        leaderHpBarFill.fillAmount = targetFill;
    }


    /// <summary>
    /// ��������ʱ, ����Ѫ��
    /// </summary>
    public void UpdateLeaderHpBar()
    {
        if(leaderHpUpdateCoroutine != null)
        {
            StopCoroutine(leaderHpUpdateCoroutine);
        }
        leaderHpUpdateCoroutine = StartCoroutine(SmoothUpdateLeaderHpBar(PartyManager.Instance.leader));
    }

    private IEnumerator SmoothUpdateLeaderHpBar(Character leader)
    {
        //����Ѫ����Ϣ
        leaderHpBarInfo.text = $"{leader.info.hp} / {leader.info.maxHp}";
        //����Ѫ������
        float targetFill =  leader.info.hp / (leader.info.maxHp * 1.0f);
        while(Mathf.Abs(leaderHpBarFill.fillAmount - targetFill) > 0.01f)
        {
            leaderHpBarFill.fillAmount = Mathf.Lerp(leaderHpBarFill.fillAmount, targetFill, Time.deltaTime * hpUpdateSpeed);
            yield return null;
        }
        leaderHpBarFill.fillAmount = targetFill;
    }

    /// <summary>
    /// �л�����ʱ, ����Ѫ��
    /// </summary>
    public void ChangeLeaderHpBar()
    {
        if (leaderHpUpdateCoroutine != null)
        {
            StopCoroutine(leaderHpUpdateCoroutine);
        }
        leaderHpUpdateCoroutine = StartCoroutine(SmoothChangeLeaderHpBar(PartyManager.Instance.leader));
    }
    
    private IEnumerator SmoothChangeLeaderHpBar(Character leader)
    {
        //����Ѫ����Ϣ
        leaderHpBarInfo.text = $"{leader.info.hp} / {leader.info.maxHp}";
        //����Ѫ������
        float targetFill = leader.info.hp / (leader.info.maxHp * 1.0f);
        leaderHpBarFill.fillAmount = targetFill;
        yield return null;
    }

    /// <summary>
    /// ��ʾ�͸��¹۲�Ŀ������Ѫ�����
    /// </summary>
    /// <param name="target"></param>
    public void ShowTargetHpPanel(Character target)
    {
        targetHpPanel.SetActive(true);
        //���¹۲�Ŀ���������������Ϣ
        targetName.text = target.info.name;
        //����Ѫ����Ϣ
        targetHpBarInfo.text = $"{target.info.hp} / {target.info.maxHp}";
        //����Ѫ������
        float targetFill = target.info.hp / (target.info.maxHp * 1.0f);
        targetHpBarFill.fillAmount = targetFill;

        //��ʼ���۲�Ŀ��Ѫ���·���Buffͼ��
        InitializeBuffIconOnTarget(target);
    }

    /// <summary>
    /// ��ʼ���۲�Ŀ��Ѫ���·���Buffͼ��
    /// </summary>
    private void InitializeBuffIconOnTarget(Character target)
    {
        //������֮ǰ��Buffͼ��
        for (int i = buffParent.childCount - 1; i >= 0; i--)
        {
            Destroy(buffParent.GetChild(i).gameObject);
        }
        //һһ����target��Buffͼ��
        foreach (Buff buff in target.buffs)
        {
            GameObject buffIcon = Instantiate(buffIconPrefab, buffParent); //ʵ����Buffͼ��Ԥ����
            buffIcon.transform.Find("BuffIcon").GetComponent<Image>().sprite = buff.buffIcon; //����Buffͼ��
            buffIcon.transform.Find("BuffRemainMask").GetComponent<Image>().fillAmount = buff.remainingTurns / (buff.durationTurns * 1.0f); //����Buffʣ��ʱ�����ͼ��߿�
        }
    }


    /// <summary>
    /// ���ع۲�Ŀ������Ѫ�����
    /// </summary>
    public void HideTargetHpPanel()
    {
        targetHpPanel.SetActive(false);
    }
}
