using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDiceManager : MonoBehaviour
{
    private static UIDiceManager instance;
    public static UIDiceManager Instance => instance;

    public GameObject diceRollPanel; //����Ͷ�����
    public Image diceRollerBg; //Ͷ���߱���ͼ
    public TextMeshProUGUI diceRollTitle; //Ͷ��������
    public TextMeshProUGUI diceRollAnimationNum; //Ͷ����������
    public TextMeshProUGUI diceRollResult; //Ͷ�����
    public Button diceRollButton; //Ͷ��������ť

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
        diceRollButton.onClick.AddListener(OnClickDiceRollSkipButton); //Ϊ������ť��Ӽ�������
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    /// <summary>
    /// ��ʾ����Ͷ�����
    /// </summary>
    /// <param name="type"></param>    
    public void ShowDiceRollPanel(DiceType type, Sprite bustPortrait)
    {
        //��ʼ��Ͷ�������ı�
        if (type == DiceType.attackDice)
        {
            diceRollTitle.text = "����Ͷ��";
        }
        else if (type == DiceType.damageDice)
        {
            diceRollTitle.text = "�˺�Ͷ��";
        }
        else if (type == DiceType.d20Dice)
        {
            diceRollTitle.text = "Ͷ��";
        }
        //��ʼ��Ͷ���߱���ͼ
        diceRollerBg.sprite = bustPortrait;
        //��ʼ��Ͷ������ı�
        diceRollResult.text = "Ͷ����...";
        //��ʼ����������
        diceRollAnimationNum.text = "";
        //��ʾ���
        diceRollPanel.SetActive(true);
    }

    /// <summary>
    /// ������������
    /// </summary>
    /// <param name="result"></param>
    /// <param name="baseRoll"></param>
    /// <returns></returns>
    public IEnumerator AnimateRoll(DiceResult result, int baseRoll, int diceCount, int diceSides)
    {
        isRolling = true;

        timer = 0.0f;
        //������ֶ���
        while(timer < duration)
        {
            int randomValue = Random.Range(1, diceCount * diceSides + 1);
            diceRollAnimationNum.text = $"{randomValue}";

            timer += 0.1f;
            yield return animationInterval;
        }
        isRolling = false;

        //���ս��
        diceRollAnimationNum.text = $"{baseRoll}";
        diceRollResult.text = result.logText;
        
        yield return waitForTwoSecond;
        diceRollPanel.SetActive(false);
    }

    /// <summary>
    /// ������ť�ĵ����������
    /// </summary>
    private void OnClickDiceRollSkipButton()
    {
        if (isRolling)
        {
            timer = duration;
        }
    }
}
