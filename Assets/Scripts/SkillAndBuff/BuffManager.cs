using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BuffTimeType
{
    Permanet, //永久Buff
    Temporary, //临时Buff
    Transient //瞬时增益, 如加血, 根据增益直接修改角色信息, 不记录buff信息, 不可逆
}

[System.Serializable]
public class Buff
{
    [BoxGroup("Buff数据", centerLabel: false)]
    [HorizontalGroup("Buff数据/基础信息", Width = 0.33f)]
    [LabelText("名称")]
    public string buffName; //buff名称
    [HorizontalGroup("Buff数据/基础信息", Width = 0.33f)]
    [LabelText("持续类型")]
    public BuffTimeType timeType; //buff持续时间类型
    [HorizontalGroup("Buff数据/基础信息", Width = 0.33f)]
    [LabelText("持续回合数")]
    public int durationTurns; //持续回合数
    [HideInInspector]
    public int remainingTurns; //剩余回合数

    [TextArea]
    [HorizontalGroup("Buff数据/效果描述"), LabelText("效果描述")]
    public string buffDescription_Effects;

    [TextArea]
    [HorizontalGroup("Buff数据/背景描述"), LabelText("背景描述")]
    public string buffDescription_BG;

    [PreviewField(80)]
    [HorizontalGroup("Buff数据/Buff图标", Width = 150), LabelText("图标")]
    public Sprite buffIcon; //剩余回合数

    [BoxGroup("Buff数据/属性加值", centerLabel: false)]
    [HorizontalGroup("Buff数据/属性加值/行1", Width = 0.33f)]
    [LabelText("当前生命值")]
    public int hpModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行1", Width = 0.33f)]
    [LabelText("最大生命值")]
    public int maxHpModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行1", Width = 0.33f)]
    [LabelText("攻击距离")]
    public int attackDistanceModifier = 0;

    [HorizontalGroup("Buff数据/属性加值/行2", Width = 0.33f)]
    [LabelText("力量")]
    public int strengthModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行2", Width = 0.33f)]
    [LabelText("敏捷")]
    public int finesseModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行2", Width = 0.33f)]
    [LabelText("智力")]
    public int intelligenceModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行3", Width = 0.33f)]
    [LabelText("体质")]
    public int constitutionModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行3", Width = 0.33f)]
    [LabelText("魅力")]
    public int charismaModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行3", Width = 0.33f)]
    [LabelText("感知")]
    public int witsModifier = 0;

    [HorizontalGroup("Buff数据/属性加值/行4", Width = 0.33f)]
    [LabelText("防御等级")]
    public int armorClassModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行4", Width = 0.33f)]
    [LabelText("先攻")]
    public int initiativeModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行4", Width = 0.33f)]
    [LabelText("速度")]
    public float speedModifier = 0;

    [HorizontalGroup("Buff数据/属性加值/行5", Width = 0.33f)]
    [LabelText("行动点数")]
    public int actionPointModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行5", Width = 0.33f)]
    [LabelText("加值骰子个数")]
    public int diceCountModifier = 0;
    [HorizontalGroup("Buff数据/属性加值/行5", Width = 0.33f)]
    [LabelText("加值骰子面数")]
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
    /// 用于回合制管理器中, 每当一名角色回合结束, 更新其身上的Buff持续时间
    /// </summary>
    /// <param name="character"></param>
    public void UpdateBuffAfterFinishTurn(Character character)
    {
        foreach(Buff buff in character.buffs.ToList())
        {
            //回合结束只更新临时buff剩余时间
            if (buff.timeType == BuffTimeType.Temporary)
            {
                buff.remainingTurns--; //buff剩余回合-1

                //如果角色是玩家队伍角色, 更新在队伍角色头像旁的Buff图标
                if (character.CompareTag("Player"))
                {
                    UIPartyManager.Instance.UpdateBuffIconBesidesPortrait(buff);
                }

                if (buff.remainingTurns == 0)
                {
                    RemoveBuff(character, buff); //如果剩余回合数降低为0, 就移除Buff
                }
            }
        }
    }
    
    /// <summary>
    /// 回合制结束时, 将角色身上的Buff从回合制计时转为即时制计时, 等待buff剩余时间后, 将角色身上的buff移除
    /// </summary>
    /// <param name="character"></param>
    public void UpdateBuffAfterEndTurn(Character character)
    {
        foreach (Buff buff in character.buffs)
        {
            //回合结束只更新临时buff剩余时间
            if (buff.timeType == BuffTimeType.Temporary)
            {
                CoroutineManager.Instance.AddTaskToGroup(RemoveBuffAfterDelay(character, buff), character.info.name + buff.buffName);
                CoroutineManager.Instance.StartGroup(character.info.name + buff.buffName);
            }
        }
    }

    /// <summary>
    /// 回合制开始时, 将角色身上的Buff从即时制计时转为回合制计时, 停止角色身上每一个Buff的即时制计时协程
    /// </summary>
    /// <param name="character"></param>
    public void UpdateBuffAfterStartTurn(Character character)
    {
        foreach (Buff buff in character.buffs)
        {
            //回合结束只更新临时buff剩余时间
            if (buff.timeType == BuffTimeType.Temporary)
            {
                CoroutineManager.Instance.StopGroup(character.info.name + buff.buffName);
            }
        }
    }

    /// <summary>
    /// 向角色character身上添加buff, 更新角色信息 和 角色面板信息
    /// </summary>
    /// <param name="character"></param>
    /// <param name="buff"></param>
    public void AddBuff(Character character, Buff buff)
    {
        //首先判断角色身上是否已经存在相同buff
        foreach(Buff characterBuff in character.buffs)
        {
            //如果角色身上已经存在该buff
            if (characterBuff.buffName == buff.buffName)
            {
                //如果该buff是临时buff, 就延长持续回合数
                if (buff.timeType == BuffTimeType.Temporary)
                {
                    characterBuff.remainingTurns += buff.durationTurns;
                    UIPartyManager.Instance.UpdateBuffIconBesidesPortrait(buff); //更新buff图标
                }
                //直接返回, 无需增加新buff
                return;
            }
        }

        //如果角色身上不存在该buff, 就实例化一份新的buff, 并加入到character.buffs;
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

        //更新角色信息
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

        attackerColor = character.CompareTag("Player") ? "#F0F8FF" : "#B22222"; //根据是Player还是Enemy来决定在战斗日志中显示的色彩
        //向战斗日志中增加条目
        UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{character.info.name}</u></color> 获得Buff状态 <color=#32CD32><u>{buff.buffName}</u></color> ({buff.buffDescription_Effects})");

        //如果是主控, 就更新角色面板信息
        if (character == PartyManager.Instance.leader)
        {
            UIPartyCharacterManager.Instance.UpdateCharacterInfo(character);
            UIHealthPointManager.Instance.UpdateLeaderHpBar(); //更新主控血条
        }

        //回合制中血量发生变动, 就更新回合制头像血条
        if (character.isInTurn && (newBuff.maxHpModifier != 0 || newBuff.hpModifier != 0))
        {
            UITurnManager.Instance.UpdateTurnPortraitHpBarFill(character);
        }

        //如果角色是玩家队伍角色, 且Buff不为瞬时Buff, 在队伍角色头像旁添加Buff图标
        if (character.CompareTag("Player") && newBuff.timeType != BuffTimeType.Transient)
        {
            UIPartyManager.Instance.AddBuffIconBesidesPortrait(character, newBuff);
        }

        //如果是瞬时增益, 则只根据buff修改角色信息即可, 不记录和更新buff状态。如果是永久buff或临时buff, 则需要记录和更新
        if (newBuff.timeType != BuffTimeType.Transient)
        {
            character.buffs.Add(newBuff);
            //如果是临时Buff, 并且不是在回合制中, 则还需要等待buff持续时间之后自动移除buff
            if (newBuff.timeType == BuffTimeType.Temporary && !character.isInTurn)
            {
                CoroutineManager.Instance.AddTaskToGroup(RemoveBuffAfterDelay(character, newBuff), character.info.name + newBuff.buffName);
                CoroutineManager.Instance.StartGroup(character.info.name + newBuff.buffName);
            }
        }
    }

    /// <summary>
    /// 用于非回合状态下, 延迟Buff持续回合数对应的时间后, 移除该Buff
    /// </summary>
    /// <param name="durationTurns"></param>
    /// <returns></returns>
    public IEnumerator RemoveBuffAfterDelay(Character character, Buff buff)
    {
        while(buff.remainingTurns > 0)
        {
            yield return oneTurnTime; //一回合等价于6s, 等待一回合时间

            buff.remainingTurns--; //持续回合-1

            //如果角色是玩家队伍角色, 更新在队伍角色头像旁的Buff图标
            if (character.CompareTag("Player"))
            {
                UIPartyManager.Instance.UpdateBuffIconBesidesPortrait(buff);
            }
        }
        RemoveBuff(character, buff);
    }

    /// <summary>
    /// 移除角色character身上的buff, 更新角色信息 和 角色面板信息
    /// </summary>
    /// <param name="character"></param>
    /// <param name="buff"></param>
    public void RemoveBuff(Character character, Buff buff)
    {
        if (!character.buffs.Contains(buff))
        {
            //如果character.buffs中不包含要移除的buff, 则查找character.buffs中是否有AddBuff时根据buff创建的新实例, 即两种buffName相同, 如果有, 则移除该buff 
            buff = character.buffs.Where(characterBuff => characterBuff.buffName == buff.buffName).LastOrDefault();
            if (!character.buffs.Contains(buff))
            {
                //如果character.buffs中依然没有相同buff实例, 则返回
                return;
            }
        }

        //更新角色信息
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

        attackerColor = character.CompareTag("Player") ? "#F0F8FF" : "#B22222"; //根据是Player还是Enemy来决定在战斗日志中显示的色彩
        //向战斗日志中增加条目
        UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{character.info.name}</u></color> 失去Buff状态 <color=#32CD32><u>{buff.buffName}</u></color>");

        //如果是主控, 就更新角色面板信息
        if (character == PartyManager.Instance.leader)
        {
            UIPartyCharacterManager.Instance.UpdateCharacterInfo(character);
        }

        //回合制中血量发生变动, 就更新回合制头像血条
        if (character.isInTurn && (buff.maxHpModifier != 0 || buff.hpModifier != 0))
        {
            UITurnManager.Instance.UpdateTurnPortraitHpBarFill(character);
        }

        //如果角色是玩家队伍角色, 移除在队伍角色头像旁的Buff图标
        if (character.CompareTag("Player"))
        {
            UIPartyManager.Instance.RemoveBuffIconBesidesPortrait(buff);
            UIHealthPointManager.Instance.UpdateLeaderHpBar(); //更新主控血条
        }

        //从character.buffs中移除记录的对应buff
        character.buffs.Remove(buff);
    }
}
