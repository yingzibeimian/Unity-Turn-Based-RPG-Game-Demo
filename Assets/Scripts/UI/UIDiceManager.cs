using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDiceManager : MonoBehaviour
{
    private static UIDiceManager instance;
    public static UIDiceManager Instance => instance;

    public GameObject diceRollPanel; //骰子投掷面板
    public Image diceRollerBg; //投掷者背景图
    public TextMeshProUGUI diceRollTitle; //投掷面板标题
    public TextMeshProUGUI diceRollAnimationNum; //投掷动画数字
    public TextMeshProUGUI diceRollResult; //投掷结果
    public Button diceRollButton; //投掷跳过按钮

    private bool isRolling = false;
    private float duration = 2.0f;
    private float timer = 0.0f;
    private WaitForSeconds waitForTwoSecond = new WaitForSeconds(2);
    private WaitForSeconds animationInterval = new WaitForSeconds(0.1f);

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        diceRollPanel.SetActive(false);
        diceRollButton.onClick.AddListener(OnClickDiceRollSkipButton); //为跳过按钮添加监听函数
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    /// <summary>
    /// 显示骰子投掷面板
    /// </summary>
    /// <param name="type"></param>    
    public void ShowDiceRollPanel(DiceType type, Sprite bustPortrait)
    {
        //初始化投掷标题文本
        if (type == DiceType.attackDice)
        {
            diceRollTitle.text = "攻击投掷";
        }
        else if (type == DiceType.damageDice)
        {
            diceRollTitle.text = "伤害投掷";
        }
        else if (type == DiceType.d20Dice)
        {
            diceRollTitle.text = "投掷";
        }
        //初始化投掷者背景图
        diceRollerBg.sprite = bustPortrait;
        //初始化投掷结果文本
        diceRollResult.text = "投掷中...";
        //初始化动画数字
        diceRollAnimationNum.text = "";
        //显示面板
        diceRollPanel.SetActive(true);
    }

    /// <summary>
    /// 播放掷骰动画
    /// </summary>
    /// <param name="result"></param>
    /// <param name="baseRoll"></param>
    /// <returns></returns>
    public IEnumerator AnimateRoll(DiceResult result, int baseRoll, int diceCount, int diceSides)
    {
        isRolling = true;

        timer = 0.0f;
        //随机数字动画
        while(timer < duration)
        {
            int randomValue = Random.Range(1, diceCount * diceSides + 1);
            diceRollAnimationNum.text = $"{randomValue}";

            timer += 0.1f;
            yield return animationInterval;
        }
        isRolling = false;

        //最终结果
        diceRollAnimationNum.text = $"{baseRoll}";
        diceRollResult.text = result.logText;
        
        yield return waitForTwoSecond;
        diceRollPanel.SetActive(false);
    }

    /// <summary>
    /// 跳过按钮的点击监听函数
    /// </summary>
    private void OnClickDiceRollSkipButton()
    {
        if (isRolling)
        {
            timer = duration;
        }
    }
}
