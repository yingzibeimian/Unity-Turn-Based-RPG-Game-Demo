using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 骰子类型
/// </summary>
public enum DiceType
{
    /// <summary>
    /// 攻击投掷
    /// </summary>
    attackDice,
    /// <summary>
    /// 伤害投掷
    /// </summary>
    damageDice,
    /// <summary>
    /// 和attackRoll同样的d20骰子, 用于对话、环境中的各种投掷检定
    /// </summary>
    d20Dice
}

/// <summary>
/// 投掷结果
/// </summary>
public class DiceResult
{
    public int totalValue; //结果总值
    public bool criticalHit; //是否大成功
    public bool criticalMiss; //是否大失败
    public string logText; //骰子显示文本
}

public class DiceManager : MonoBehaviour
{
    private static DiceManager instance;
    public static DiceManager Instance => instance;

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// 骰子投掷方法
    /// </summary>
    /// <param name="type">骰子类型</param>
    /// <param name="diceCount">骰子数量</param>
    /// <param name="diceSides">骰子面数</param>
    /// <param name="modifier">调整值(key:调整值来源, value:调整值)</param>
    /// <param name="callback">回调函数(用来返回投掷结果)</param>
    /// <returns></returns>
    public IEnumerator RollDice(DiceType type, int diceCount, int diceSides, Dictionary<string, int> modifiers, Sprite bustPortrait, Action<DiceResult> callback)
    {
        DiceResult result = new DiceResult();

        //基础骰子计算
        int baseRoll = 0;
        for (int i = 0; i < diceCount; i++)
        {
            baseRoll += Random.Range(1, diceSides + 1);
        }

        //攻击投掷
        if (type == DiceType.attackDice)
        {
            if (baseRoll == 20)
            {
                result.totalValue = baseRoll; //结果总值
                result.criticalHit = true; //大成功
                result.logText = "攻击投掷结果:\n <color=#FFD700>20(大成功)</color>";
            }
            else if (baseRoll == 1)
            {
                result.totalValue = baseRoll; //结果总值
                result.criticalMiss = true; //大失败
                result.logText = "攻击投掷结果:\n <color=#FF4500>1(大失败)</color>";
            }
            else
            {
                int total = baseRoll;
                foreach (var modifierValue in modifiers.Values)
                {
                    total += modifierValue;
                }
                string formula = $"攻击投掷结果:\n <color=#FF4500>{total}</color> = <color=#FF4500>{baseRoll}</color>({diceCount}d{diceSides})";
                foreach (var modifier in modifiers)
                {
                    formula += $" + <color=#FF4500>{modifier.Value}</color>({modifier.Key})";
                }
                result.totalValue = total; //结果总值
                result.logText = formula; //投掷结果构成文本
            }
        }
        //1D20骰子
        else if (type == DiceType.d20Dice)
        {
            if (baseRoll == 20)
            {
                result.totalValue = baseRoll; //结果总值
                result.criticalHit = true; //大成功
                result.logText = "投掷结果:\n <color=#FFD700>20(大成功)</color>";
            }
            else if (baseRoll == 1)
            {
                result.totalValue = baseRoll; //结果总值
                result.criticalMiss = true; //大失败
                result.logText = "投掷结果:\n <color=#FF4500>1(大失败)</color>";
            }
            else
            {
                int total = baseRoll;
                foreach (var modifierValue in modifiers.Values)
                {
                    total += modifierValue;
                }
                string formula = $"投掷结果:\n <color=#FF4500>{total}</color> = <color=#FF4500>{baseRoll}</color>({diceCount}d{diceSides})";
                foreach (var modifier in modifiers)
                {
                    formula += $" + <color=#FF4500>{modifier.Value}</color>({modifier.Key})";
                }
                result.totalValue = total; //结果总值
                result.logText = formula; //投掷结果构成文本
            }
        }
        //伤害投掷
        else if (type == DiceType.damageDice)
        {
            int total = baseRoll;
            foreach(var modifierValue in modifiers.Values)
            {
                total += modifierValue;
            }
            string formula = $"伤害投掷结果:\n <color=#FF4500>{total}</color> = <color=#FF4500>{baseRoll}</color>({diceCount}d{diceSides})";
            foreach(var modifier in modifiers)
            {
                formula += $" + <color=#FF4500>{modifier.Value}</color>({modifier.Key})";
            }
            result.totalValue = total; //结果总值
            result.logText = formula; //投掷结果构成文本
        }

        //显示掷骰UI
        UIDiceManager.Instance.ShowDiceRollPanel(type, bustPortrait);
        //播放掷骰动画
        yield return UIDiceManager.Instance.AnimateRoll(result, baseRoll, diceCount, diceSides);

        callback?.Invoke(result);
    }
}
