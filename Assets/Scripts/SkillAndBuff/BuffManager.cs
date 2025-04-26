using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BuffTimeType
{
    Permanet, //����Buff
    Temporary, //��ʱBuff
    Transient //˲ʱ����, ���Ѫ, ��������ֱ���޸Ľ�ɫ��Ϣ, ����¼buff��Ϣ, ������
}

[System.Serializable]
public class Buff
{
    [BoxGroup("Buff����", centerLabel: false)]
    [HorizontalGroup("Buff����/������Ϣ", Width = 0.33f)]
    [LabelText("����")]
    public string buffName; //buff����
    [HorizontalGroup("Buff����/������Ϣ", Width = 0.33f)]
    [LabelText("��������")]
    public BuffTimeType timeType; //buff����ʱ������
    [HorizontalGroup("Buff����/������Ϣ", Width = 0.33f)]
    [LabelText("�����غ���")]
    public int durationTurns; //�����غ���
    [HideInInspector]
    public int remainingTurns; //ʣ��غ���

    [TextArea]
    [HorizontalGroup("Buff����/Ч������"), LabelText("Ч������")]
    public string buffDescription_Effects;

    [TextArea]
    [HorizontalGroup("Buff����/��������"), LabelText("��������")]
    public string buffDescription_BG;

    [PreviewField(80)]
    [HorizontalGroup("Buff����/Buffͼ��", Width = 150), LabelText("ͼ��")]
    public Sprite buffIcon; //ʣ��غ���

    [BoxGroup("Buff����/���Լ�ֵ", centerLabel: false)]
    [HorizontalGroup("Buff����/���Լ�ֵ/��1", Width = 0.33f)]
    [LabelText("��ǰ����ֵ")]
    public int hpModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��1", Width = 0.33f)]
    [LabelText("�������ֵ")]
    public int maxHpModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��1", Width = 0.33f)]
    [LabelText("��������")]
    public int attackDistanceModifier = 0;

    [HorizontalGroup("Buff����/���Լ�ֵ/��2", Width = 0.33f)]
    [LabelText("����")]
    public int strengthModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��2", Width = 0.33f)]
    [LabelText("����")]
    public int finesseModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��2", Width = 0.33f)]
    [LabelText("����")]
    public int intelligenceModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��3", Width = 0.33f)]
    [LabelText("����")]
    public int constitutionModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��3", Width = 0.33f)]
    [LabelText("����")]
    public int charismaModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��3", Width = 0.33f)]
    [LabelText("��֪")]
    public int witsModifier = 0;

    [HorizontalGroup("Buff����/���Լ�ֵ/��4", Width = 0.33f)]
    [LabelText("�����ȼ�")]
    public int armorClassModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��4", Width = 0.33f)]
    [LabelText("�ȹ�")]
    public int initiativeModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��4", Width = 0.33f)]
    [LabelText("�ٶ�")]
    public float speedModifier = 0;

    [HorizontalGroup("Buff����/���Լ�ֵ/��5", Width = 0.33f)]
    [LabelText("�ж�����")]
    public int actionPointModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��5", Width = 0.33f)]
    [LabelText("��ֵ���Ӹ���")]
    public int diceCountModifier = 0;
    [HorizontalGroup("Buff����/���Լ�ֵ/��5", Width = 0.33f)]
    [LabelText("��ֵ��������")]
    public int diceSidesModifier = 0;
}


public class BuffManager : MonoBehaviour
{
    private static BuffManager instance;
    public static BuffManager Instance => instance;

    private WaitForSeconds oneTurnTime = new WaitForSeconds(6.0f);
    private string attackerColor = "#F0F8FF";

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
    /// ���ڻغ��ƹ�������, ÿ��һ����ɫ�غϽ���, ���������ϵ�Buff����ʱ��
    /// </summary>
    /// <param name="character"></param>
    public void UpdateBuffAfterFinishTurn(Character character)
    {
        foreach(Buff buff in character.buffs.ToList())
        {
            //�غϽ���ֻ������ʱbuffʣ��ʱ��
            if (buff.timeType == BuffTimeType.Temporary)
            {
                buff.remainingTurns--; //buffʣ��غ�-1

                //�����ɫ����Ҷ����ɫ, �����ڶ����ɫͷ���Ե�Buffͼ��
                if (character.CompareTag("Player"))
                {
                    UIPartyManager.Instance.UpdateBuffIconBesidesPortrait(buff);
                }

                if (buff.remainingTurns == 0)
                {
                    RemoveBuff(character, buff); //���ʣ��غ�������Ϊ0, ���Ƴ�Buff
                }
            }
        }
    }
    
    /// <summary>
    /// �غ��ƽ���ʱ, ����ɫ���ϵ�Buff�ӻغ��Ƽ�ʱתΪ��ʱ�Ƽ�ʱ, �ȴ�buffʣ��ʱ���, ����ɫ���ϵ�buff�Ƴ�
    /// </summary>
    /// <param name="character"></param>
    public void UpdateBuffAfterEndTurn(Character character)
    {
        foreach (Buff buff in character.buffs)
        {
            //�غϽ���ֻ������ʱbuffʣ��ʱ��
            if (buff.timeType == BuffTimeType.Temporary)
            {
                CoroutineManager.Instance.AddTaskToGroup(RemoveBuffAfterDelay(character, buff), character.info.name + buff.buffName);
                CoroutineManager.Instance.StartGroup(character.info.name + buff.buffName);
            }
        }
    }

    /// <summary>
    /// �غ��ƿ�ʼʱ, ����ɫ���ϵ�Buff�Ӽ�ʱ�Ƽ�ʱתΪ�غ��Ƽ�ʱ, ֹͣ��ɫ����ÿһ��Buff�ļ�ʱ�Ƽ�ʱЭ��
    /// </summary>
    /// <param name="character"></param>
    public void UpdateBuffAfterStartTurn(Character character)
    {
        foreach (Buff buff in character.buffs)
        {
            //�غϽ���ֻ������ʱbuffʣ��ʱ��
            if (buff.timeType == BuffTimeType.Temporary)
            {
                CoroutineManager.Instance.StopGroup(character.info.name + buff.buffName);
            }
        }
    }

    /// <summary>
    /// ���ɫcharacter�������buff, ���½�ɫ��Ϣ �� ��ɫ�����Ϣ
    /// </summary>
    /// <param name="character"></param>
    /// <param name="buff"></param>
    public void AddBuff(Character character, Buff buff)
    {
        //�����жϽ�ɫ�����Ƿ��Ѿ�������ͬbuff
        foreach(Buff characterBuff in character.buffs)
        {
            //�����ɫ�����Ѿ����ڸ�buff
            if (characterBuff.buffName == buff.buffName)
            {
                //�����buff����ʱbuff, ���ӳ������غ���
                if (buff.timeType == BuffTimeType.Temporary)
                {
                    characterBuff.remainingTurns += buff.durationTurns;
                    UIPartyManager.Instance.UpdateBuffIconBesidesPortrait(buff); //����buffͼ��
                }
                //ֱ�ӷ���, ����������buff
                return;
            }
        }

        //�����ɫ���ϲ����ڸ�buff, ��ʵ����һ���µ�buff, �����뵽character.buffs;
        Buff newBuff = new Buff()
        {
            buffName = buff.buffName,
            timeType = buff.timeType,
            durationTurns = buff.durationTurns,
            remainingTurns = buff.durationTurns,
            buffDescription_Effects = buff.buffDescription_Effects,
            buffDescription_BG = buff.buffDescription_BG,
            buffIcon = buff.buffIcon,
            hpModifier = buff.hpModifier,
            maxHpModifier = buff.maxHpModifier,
            attackDistanceModifier = buff.attackDistanceModifier,
            strengthModifier = buff.strengthModifier,
            finesseModifier = buff.finesseModifier,
            intelligenceModifier = buff.intelligenceModifier,
            constitutionModifier = buff.constitutionModifier,
            charismaModifier = buff.charismaModifier,
            witsModifier = buff.witsModifier,
            armorClassModifier = buff.armorClassModifier,
            initiativeModifier = buff.initiativeModifier,
            speedModifier = buff.speedModifier,
            actionPointModifier = buff.actionPointModifier,
            diceCountModifier = buff.diceCountModifier,
            diceSidesModifier = buff.diceSidesModifier
        };

        //���½�ɫ��Ϣ
        character.info.maxHp += newBuff.maxHpModifier;
        character.info.hp = Mathf.Min(character.info.hp + newBuff.hpModifier, character.info.maxHp);
        character.info.attackDistance += newBuff.attackDistanceModifier;
        character.info.strength += newBuff.strengthModifier;
        character.info.finesse += newBuff.finesseModifier;
        character.info.intelligence += newBuff.intelligenceModifier;
        character.info.constitution += newBuff.constitutionModifier;
        character.info.charisma += newBuff.charismaModifier;
        character.info.wits += newBuff.witsModifier;
        character.info.armorClass += newBuff.armorClassModifier;
        character.info.initiative += newBuff.initiativeModifier;
        character.info.walkSpeed += newBuff.speedModifier;
        character.info.runSpeed += newBuff.speedModifier;
        character.info.actionPoint += newBuff.actionPointModifier;
        Debug.Log($"actionPoint {character.info.actionPoint}");
        //character.info.damageDiceCount += newBuff.diceCountModifier;
        //character.info.damageDiceSides += newBuff.diceSidesModifier;

        attackerColor = character.CompareTag("Player") ? "#F0F8FF" : "#B22222"; //������Player����Enemy��������ս����־����ʾ��ɫ��
        //��ս����־��������Ŀ
        UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{character.info.name}</u></color> ���Buff״̬ <color=#32CD32><u>{buff.buffName}</u></color> ({buff.buffDescription_Effects})");

        //���������, �͸��½�ɫ�����Ϣ
        if (character == PartyManager.Instance.leader)
        {
            UIPartyCharacterManager.Instance.UpdateCharacterInfo(character);
            UIHealthPointManager.Instance.UpdateLeaderHpBar(); //��������Ѫ��
        }

        //�غ�����Ѫ�������䶯, �͸��»غ���ͷ��Ѫ��
        if (character.isInTurn && (newBuff.maxHpModifier != 0 || newBuff.hpModifier != 0))
        {
            UITurnManager.Instance.UpdateTurnPortraitHpBarFill(character);
        }

        //�����ɫ����Ҷ����ɫ, ��Buff��Ϊ˲ʱBuff, �ڶ����ɫͷ�������Buffͼ��
        if (character.CompareTag("Player") && newBuff.timeType != BuffTimeType.Transient)
        {
            UIPartyManager.Instance.AddBuffIconBesidesPortrait(character, newBuff);
        }

        //�����˲ʱ����, ��ֻ����buff�޸Ľ�ɫ��Ϣ����, ����¼�͸���buff״̬�����������buff����ʱbuff, ����Ҫ��¼�͸���
        if (newBuff.timeType != BuffTimeType.Transient)
        {
            character.buffs.Add(newBuff);
            //�������ʱBuff, ���Ҳ����ڻغ�����, ����Ҫ�ȴ�buff����ʱ��֮���Զ��Ƴ�buff
            if (newBuff.timeType == BuffTimeType.Temporary && !character.isInTurn)
            {
                CoroutineManager.Instance.AddTaskToGroup(RemoveBuffAfterDelay(character, newBuff), character.info.name + newBuff.buffName);
                CoroutineManager.Instance.StartGroup(character.info.name + newBuff.buffName);
            }
        }
    }

    /// <summary>
    /// ���ڷǻغ�״̬��, �ӳ�Buff�����غ�����Ӧ��ʱ���, �Ƴ���Buff
    /// </summary>
    /// <param name="durationTurns"></param>
    /// <returns></returns>
    public IEnumerator RemoveBuffAfterDelay(Character character, Buff buff)
    {
        while(buff.remainingTurns > 0)
        {
            yield return oneTurnTime; //һ�غϵȼ���6s, �ȴ�һ�غ�ʱ��

            buff.remainingTurns--; //�����غ�-1

            //�����ɫ����Ҷ����ɫ, �����ڶ����ɫͷ���Ե�Buffͼ��
            if (character.CompareTag("Player"))
            {
                UIPartyManager.Instance.UpdateBuffIconBesidesPortrait(buff);
            }
        }
        RemoveBuff(character, buff);
    }

    /// <summary>
    /// �Ƴ���ɫcharacter���ϵ�buff, ���½�ɫ��Ϣ �� ��ɫ�����Ϣ
    /// </summary>
    /// <param name="character"></param>
    /// <param name="buff"></param>
    public void RemoveBuff(Character character, Buff buff)
    {
        if (!character.buffs.Contains(buff))
        {
            //���character.buffs�в�����Ҫ�Ƴ���buff, �����character.buffs���Ƿ���AddBuffʱ����buff��������ʵ��, ������buffName��ͬ, �����, ���Ƴ���buff 
            buff = character.buffs.Where(characterBuff => characterBuff.buffName == buff.buffName).LastOrDefault();
            if (!character.buffs.Contains(buff))
            {
                //���character.buffs����Ȼû����ͬbuffʵ��, �򷵻�
                return;
            }
        }

        //���½�ɫ��Ϣ
        character.info.hp -= buff.hpModifier;
        character.info.maxHp -= buff.maxHpModifier;
        character.info.attackDistance -= buff.attackDistanceModifier;
        character.info.strength -= buff.strengthModifier;
        character.info.finesse -= buff.finesseModifier;
        character.info.intelligence -= buff.intelligenceModifier;
        character.info.constitution -= buff.constitutionModifier;
        character.info.charisma -= buff.charismaModifier;
        character.info.wits -= buff.witsModifier;
        character.info.armorClass -= buff.armorClassModifier;
        character.info.initiative -= buff.initiativeModifier;
        character.info.walkSpeed -= buff.speedModifier;
        character.info.runSpeed -= buff.speedModifier;
        character.info.actionPoint = Mathf.Max(character.info.actionPoint - buff.actionPointModifier, 0);
        //character.info.damageDiceCount -= buff.diceCountModifier;
        //character.info.damageDiceSides -= buff.diceSidesModifier;

        attackerColor = character.CompareTag("Player") ? "#F0F8FF" : "#B22222"; //������Player����Enemy��������ս����־����ʾ��ɫ��
        //��ս����־��������Ŀ
        UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{character.info.name}</u></color> ʧȥBuff״̬ <color=#32CD32><u>{buff.buffName}</u></color>");

        //���������, �͸��½�ɫ�����Ϣ
        if (character == PartyManager.Instance.leader)
        {
            UIPartyCharacterManager.Instance.UpdateCharacterInfo(character);
        }

        //�غ�����Ѫ�������䶯, �͸��»غ���ͷ��Ѫ��
        if (character.isInTurn && (buff.maxHpModifier != 0 || buff.hpModifier != 0))
        {
            UITurnManager.Instance.UpdateTurnPortraitHpBarFill(character);
        }

        //�����ɫ����Ҷ����ɫ, �Ƴ��ڶ����ɫͷ���Ե�Buffͼ��
        if (character.CompareTag("Player"))
        {
            UIPartyManager.Instance.RemoveBuffIconBesidesPortrait(buff);
            UIHealthPointManager.Instance.UpdateLeaderHpBar(); //��������Ѫ��
        }

        //��character.buffs���Ƴ���¼�Ķ�Ӧbuff
        character.buffs.Remove(buff);
    }
}
