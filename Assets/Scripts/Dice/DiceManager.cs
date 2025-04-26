using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// ��������
/// </summary>
public enum DiceType
{
    /// <summary>
    /// ����Ͷ��
    /// </summary>
    attackDice,
    /// <summary>
    /// �˺�Ͷ��
    /// </summary>
    damageDice,
    /// <summary>
    /// ��attackRollͬ����d20����, ���ڶԻ��������еĸ���Ͷ���춨
    /// </summary>
    d20Dice
}

/// <summary>
/// Ͷ�����
/// </summary>
public class DiceResult
{
    public int totalValue; //�����ֵ
    public bool criticalHit; //�Ƿ��ɹ�
    public bool criticalMiss; //�Ƿ��ʧ��
    public string logText; //������ʾ�ı�
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
    /// ����Ͷ������
    /// </summary>
    /// <param name="type">��������</param>
    /// <param name="diceCount">��������</param>
    /// <param name="diceSides">��������</param>
    /// <param name="modifier">����ֵ(key:����ֵ��Դ, value:����ֵ)</param>
    /// <param name="callback">�ص�����(��������Ͷ�����)</param>
    /// <returns></returns>
    public IEnumerator RollDice(DiceType type, int diceCount, int diceSides, Dictionary<string, int> modifiers, Sprite bustPortrait, Action<DiceResult> callback)
    {
        DiceResult result = new DiceResult();

        //�������Ӽ���
        int baseRoll = 0;
        for (int i = 0; i < diceCount; i++)
        {
            baseRoll += Random.Range(1, diceSides + 1);
        }

        //����Ͷ��
        if (type == DiceType.attackDice)
        {
            if (baseRoll == 20)
            {
                result.totalValue = baseRoll; //�����ֵ
                result.criticalHit = true; //��ɹ�
                result.logText = "����Ͷ�����:\n <color=#FFD700>20(��ɹ�)</color>";
            }
            else if (baseRoll == 1)
            {
                result.totalValue = baseRoll; //�����ֵ
                result.criticalMiss = true; //��ʧ��
                result.logText = "����Ͷ�����:\n <color=#FF4500>1(��ʧ��)</color>";
            }
            else
            {
                int total = baseRoll;
                foreach (var modifierValue in modifiers.Values)
                {
                    total += modifierValue;
                }
                string formula = $"����Ͷ�����:\n <color=#FF4500>{total}</color> = <color=#FF4500>{baseRoll}</color>({diceCount}d{diceSides})";
                foreach (var modifier in modifiers)
                {
                    formula += $" + <color=#FF4500>{modifier.Value}</color>({modifier.Key})";
                }
                result.totalValue = total; //�����ֵ
                result.logText = formula; //Ͷ����������ı�
            }
        }
        //1D20����
        else if (type == DiceType.d20Dice)
        {
            if (baseRoll == 20)
            {
                result.totalValue = baseRoll; //�����ֵ
                result.criticalHit = true; //��ɹ�
                result.logText = "Ͷ�����:\n <color=#FFD700>20(��ɹ�)</color>";
            }
            else if (baseRoll == 1)
            {
                result.totalValue = baseRoll; //�����ֵ
                result.criticalMiss = true; //��ʧ��
                result.logText = "Ͷ�����:\n <color=#FF4500>1(��ʧ��)</color>";
            }
            else
            {
                int total = baseRoll;
                foreach (var modifierValue in modifiers.Values)
                {
                    total += modifierValue;
                }
                string formula = $"Ͷ�����:\n <color=#FF4500>{total}</color> = <color=#FF4500>{baseRoll}</color>({diceCount}d{diceSides})";
                foreach (var modifier in modifiers)
                {
                    formula += $" + <color=#FF4500>{modifier.Value}</color>({modifier.Key})";
                }
                result.totalValue = total; //�����ֵ
                result.logText = formula; //Ͷ����������ı�
            }
        }
        //�˺�Ͷ��
        else if (type == DiceType.damageDice)
        {
            int total = baseRoll;
            foreach(var modifierValue in modifiers.Values)
            {
                total += modifierValue;
            }
            string formula = $"�˺�Ͷ�����:\n <color=#FF4500>{total}</color> = <color=#FF4500>{baseRoll}</color>({diceCount}d{diceSides})";
            foreach(var modifier in modifiers)
            {
                formula += $" + <color=#FF4500>{modifier.Value}</color>({modifier.Key})";
            }
            result.totalValue = total; //�����ֵ
            result.logText = formula; //Ͷ����������ı�
        }

        //��ʾ����UI
        UIDiceManager.Instance.ShowDiceRollPanel(type, bustPortrait);
        //������������
        yield return UIDiceManager.Instance.AnimateRoll(result, baseRoll, diceCount, diceSides);

        callback?.Invoke(result);
    }
}
