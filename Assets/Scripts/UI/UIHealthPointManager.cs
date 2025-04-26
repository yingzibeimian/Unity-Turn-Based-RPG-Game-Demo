using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthPointManager : MonoBehaviour
{
    private static UIHealthPointManager instance;
    public static UIHealthPointManager Instance => instance;
    
    //主控血量UI
    public Image leaderHpBarFill;
    public TextMeshProUGUI leaderHpBarInfo;
    public float hpUpdateSpeed = 3.0f;
    private Coroutine leaderHpUpdateCoroutine;

    //观察目标血量UI
    public GameObject targetHpPanel;
    public Image targetHpBarFill;
    public TextMeshProUGUI targetHpBarInfo;
    public TextMeshProUGUI targetName;

    //Buff图标相关
    public Transform buffParent; //Buff图标父对象
    public GameObject buffIconPrefab; //Buff图标预设体

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
        //测试
        //if (Input.GetKeyDown(KeyCode.X))
        //{
        //    PartyManager.Instance.leader.TakeDamage(10, 1);
        //}
    }

    /// <summary>
    /// 初始化血条信息
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
    /// 主控受伤时, 更新血条
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
        //更新血量信息
        leaderHpBarInfo.text = $"{leader.info.hp} / {leader.info.maxHp}";
        //更新血条长度
        float targetFill =  leader.info.hp / (leader.info.maxHp * 1.0f);
        while(Mathf.Abs(leaderHpBarFill.fillAmount - targetFill) > 0.01f)
        {
            leaderHpBarFill.fillAmount = Mathf.Lerp(leaderHpBarFill.fillAmount, targetFill, Time.deltaTime * hpUpdateSpeed);
            yield return null;
        }
        leaderHpBarFill.fillAmount = targetFill;
    }

    /// <summary>
    /// 切换主控时, 更新血条
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
        //更新血量信息
        leaderHpBarInfo.text = $"{leader.info.hp} / {leader.info.maxHp}";
        //更新血条长度
        float targetFill = leader.info.hp / (leader.info.maxHp * 1.0f);
        leaderHpBarFill.fillAmount = targetFill;
        yield return null;
    }

    /// <summary>
    /// 显示和更新观察目标对象的血量面板
    /// </summary>
    /// <param name="target"></param>
    public void ShowTargetHpPanel(Character target)
    {
        targetHpPanel.SetActive(true);
        //更新观察目标对象面板的名字信息
        targetName.text = target.info.name;
        //更新血量信息
        targetHpBarInfo.text = $"{target.info.hp} / {target.info.maxHp}";
        //更新血条长度
        float targetFill = target.info.hp / (target.info.maxHp * 1.0f);
        targetHpBarFill.fillAmount = targetFill;

        //初始化观察目标血条下方的Buff图标
        InitializeBuffIconOnTarget(target);
    }

    /// <summary>
    /// 初始化观察目标血条下方的Buff图标
    /// </summary>
    private void InitializeBuffIconOnTarget(Character target)
    {
        //先销毁之前的Buff图标
        for (int i = buffParent.childCount - 1; i >= 0; i--)
        {
            Destroy(buffParent.GetChild(i).gameObject);
        }
        //一一创建target的Buff图标
        foreach (Buff buff in target.buffs)
        {
            GameObject buffIcon = Instantiate(buffIconPrefab, buffParent); //实例化Buff图标预设体
            buffIcon.transform.Find("BuffIcon").GetComponent<Image>().sprite = buff.buffIcon; //设置Buff图标
            buffIcon.transform.Find("BuffRemainMask").GetComponent<Image>().fillAmount = buff.remainingTurns / (buff.durationTurns * 1.0f); //根据Buff剩余时间更新图标边框
        }
    }


    /// <summary>
    /// 隐藏观察目标对象的血量面板
    /// </summary>
    public void HideTargetHpPanel()
    {
        targetHpPanel.SetActive(false);
    }
}
