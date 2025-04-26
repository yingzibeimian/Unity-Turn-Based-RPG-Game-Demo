using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 回合制管理器
/// </summary>
public class TurnManager : MonoBehaviour
{
    private static TurnManager instance;
    public static TurnManager Instance => instance;

    public List<Character> participants = new List<Character>(); //回合制战斗参与者
    public List<Character> nextTurnParticipants = new List<Character>(); //下一回合的参与者
    public Dictionary<GridHelper, Character> playerGrids = new Dictionary<GridHelper, Character>(); //key:玩家阵营角色的格子 value:对应角色
    public Dictionary<GridHelper, Character> enemyGrids = new Dictionary<GridHelper, Character>(); //key:敌方阵营角色的格子 value:对应角色

    public float triggerRadius = 15.0f; //初始化时, 触发回合的角色的该半径以内的角色, 同样加入回合制战斗
    public bool endPlayerTurn = false; //用来手动调用结束玩家回合
    public bool delayPlayerTurn = false; //用来推迟玩家回合

    public int CurrentRound { get; private set; } = 0; //当前回合轮数

    public Character nowPlayer; //提供给外部使用, 查看回合制中玩家轮次的玩家角色
    //public bool isInTurn = false; //用来提供给外部 区分是否在回合制当中
    private List<Character> triggers = new List<Character>(); //触发回合制角色列表
    private bool isManualTurnStart = false; //区分回合制是由敌人AI调用 还是 玩家手动调用
    private WaitForSeconds waitHalfSecond = new WaitForSeconds(0.25f); //协程检测时间间隔
    private bool isMouseOverGrid = false; //判断鼠标是否在网格上
    private bool isMouseOverCharacter = false; //判断鼠标是否在角色身上
    private bool participantRemoved = false; //判断当前轮次的角色是否被移除
    private float apTakes; //预计消耗的行动点数

    private bool isInTurn = false; //判断回合制是否开启

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
    /// 提供给外部的回合制开启方法, 可以在玩家进入敌人视野范围后进入, 也可以由玩家主动调用进入回合制
    /// </summary>
    /// <param name="triggers">触发本次回合制角色</param>
    public void StartTurn(List<Character> triggers, bool isManualTurnStart = false)
    {
        isInTurn = true;
        this.isManualTurnStart = isManualTurnStart;

        this.triggers = triggers.ToList();

        InitializeParticipants(triggers); //初始化参与者
        PartyManager.Instance.InitialiazePartyWhenStartTurn(); //初始化回合制中的队伍, 将队伍分为每个角色一支分队, 将leader设置为null(停止角色的移动功能)
        CurrentRound = 0; //初始化回合数
        foreach(Character participant in participants)
        {
            participant.isInTurn = true;

            CoroutineManager.Instance.StopGroup(participant.info.name); //停止角色身上的协程队列

            CoroutineManager.Instance.AddTaskToGroup(participant.pathfinder.CorrectCharacterPositionInTurn(triggers), participant.info.name);
            CoroutineManager.Instance.BindCallback(participant.info.name, () =>
            {
                participant.animator.SetBool("combat", true); //入战动画
            });
            CoroutineManager.Instance.StartGroup(participant.info.name);
            //participant.isMoving = false;
            //participant.animator.SetBool("isMoving", false);
            //participant.animator.SetBool("combat", true); //入战动画

            participant.info.actionPoint = 0; //初始化行动点数AP

            //将所有参与者脚下的网格设置为dynamicLimit
            participant.nowGrid.info.dynamicLimit = true;
            participant.nowGrid.info.walkable = false;

            //更新玩家角色和敌方角色的格子列表, 方便移动过程中的借机攻击判断
            if (participant.CompareTag("Player"))
            {
                playerGrids.TryAdd(participant.nowGrid, participant);
            }
            else if (participant.CompareTag("Enemy"))
            {
                enemyGrids.TryAdd(participant.nowGrid, participant);
            }

            //回合开始时, 将每位角色身上的Buff从即时制计时转为回合制计时
            BuffManager.Instance.UpdateBuffAfterStartTurn(participant);
            //回合开始时, 将每位角色身上的Skill从即时制计时转为回合制计时
            SkillManager.Instance.UpdateSkillBarUIAfterStartTurn(participant);
        }

        //初始化回合制UI
        UITurnManager.Instance.InitializeTurnUI();
        if (!isManualTurnStart)
        {
            UITurnManager.Instance.ShowTurnStartTip(); //如果不是手动开启回合制, 即遭遇战斗, 则显示回合开始提示
        }
        //显示战斗日志面板
        UIBattleLogManager.Instance.ShowBattleLogPanel();
        UIBattleLogManager.Instance.AddLog("<color=red>~ 回合开始 ~</color>");

        StartCoroutine(RoundLoop()); //开启回合制主循环
    }

    /// <summary>
    /// 结束回合制的方法, 也可以提供给由玩家主动调用进入的回合使用, 但在主动结束前, 要先检查回合继续条件
    /// </summary>
    public void EndTurn()
    {
        if (!isManualTurnStart && CheckCombatContinueCondition()) //不是玩家主动进入的回合制 且 不满足回合制结束条件, 则不能主动结束
        {
            return;
        }
        //Debug.Log("EndTurn被调用，调用堆栈:\n" + Environment.StackTrace);
        StartCoroutine(EndTurnCoroutine());
    }

    /// <summary>
    /// 结束回合制的协程方法
    /// </summary>
    /// <returns></returns>
    private IEnumerator EndTurnCoroutine()
    {
        yield return new WaitWhile(() => UIPartyManager.Instance.relayouting); //如果队伍头像UI正在布局过程中则等待

        isInTurn = false;

        triggers.Clear();

        foreach (Character participant in participants)
        {
            participant.isInTurn = false;

            participant.animator.SetBool("combat", false); //出战动画

            participant.nowGrid.info.dynamicLimit = false;
            participant.nowGrid.info.walkable = true;

            if (participant.CompareTag("Player"))
            {
                //Debug.Log($"{participant.info.name} Start ReturnGridsToPool");
                yield return new WaitUntil(() => CoroutineManager.Instance.TaskInGroupIsEmpty(participant.info.name + "_pathfinding"));
                StartCoroutine(participant.pathfinder.ReturnGridsToPool());
            }

            //回合结束后, 将每位角色身上的Buff从回合制计时转为即时制计时, 根据剩余回合数计算剩余秒数, 剩余秒数结束后移除Buff
            BuffManager.Instance.UpdateBuffAfterEndTurn(participant);
            //回合结束后, 将每位角色身上的Skill从回合制计时转为即时制计时
            SkillManager.Instance.UpdateSkillBarUIAfterEndTurn(participant);
        }

        PartyManager.Instance.InitialiazePartyWhenEndTurn();

        nowPlayer = null;
        playerGrids.Clear();
        enemyGrids.Clear();

        participants.Clear();
        nextTurnParticipants.Clear();

        //更新回合制头像UI, 隐藏面板
        UITurnManager.Instance.UpdateWhenEndTurn();
        //隐藏战斗日志面板
        UIBattleLogManager.Instance.AddLog("<color=red>~ 回合结束 ~</color>");
        UIBattleLogManager.Instance.HideBattleLogPanel();


        Debug.Log("回合结束");
    }

    /// <summary>
    /// 初始化回合制参与者participants
    /// </summary>
    /// <param name="triggers"></param>
    private void InitializeParticipants(List<Character> triggers)
    {
        foreach(Character trigger in triggers)
        {
            //将回合制触发者加入participants
            if (trigger != null && !participants.Contains(trigger))
            {
                participants.Add(trigger);
                nextTurnParticipants.Add(trigger);
            }
            //将回合制触发者triggerRadius半径以内的角色也加入participants
            Collider[] colliders = Physics.OverlapSphere(trigger.transform.position, triggerRadius, LayerMask.GetMask("Character", "Model"));
            foreach(Collider collider in colliders)
            {
                Character participant = collider.GetComponent<Character>();
                if(participant != null && !participants.Contains(participant))
                {
                    participants.Add(participant);
                    nextTurnParticipants.Add(participant);
                }
            }
            //SortParticipantsByInitiative(participants);
            //SortParticipantsByInitiative(nextTurnParticipants);
            participants.Sort(new CharacterInitiativeComparer());
            nextTurnParticipants.Sort(new CharacterInitiativeComparer());
        }
    }

    /// <summary>
    /// (由于回合制参与者的先攻值发生变化)对下一轮回合参与者进行重新排序, 并更新回合制头像UI排序
    /// </summary>
    /// <returns></returns>
    public IEnumerator ResortTurnParticipants()
    {
        nextTurnParticipants.Sort(new CharacterInitiativeComparer());
        yield return UITurnManager.Instance.ResortNextTurnParticipantsPortraits();
    }

    /// <summary>
    /// 对回合制参与者按先攻initiative属性进行降序排序
    /// </summary>
    /// <param name="participants"></param>
    private void SortParticipantsByInitiative(List<Character> participants)
    {
        participants = participants.OrderByDescending(p => p.info.initiative).ToList();
    }

    /// <summary>
    /// 回合制主循环协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator RoundLoop()
    {
        while (CheckCombatContinueCondition()) //检查回合是否继续的条件
        {
            CurrentRound++; //回合数+1
            UIBattleLogManager.Instance.AddLog($"<color=#808080>~第{CurrentRound}回合~</color>");
            Debug.Log($"第{CurrentRound}回合开始");

            for (int i = 0; i < participants.Count; i++)
            {
                Debug.Log($"{participants[i].info.name}的回合");

                nowPlayer = participants[i];

                if (nowPlayer.info.hp <= 0) //如果角色血量小于0, 说明该角色死亡, 已经从下一轮回合中移除, 本轮直接跳过
                {
                    continue;
                }

                nowPlayer.info.actionPoint = Mathf.Min(nowPlayer.info.actionPoint + 4, 6); //恢复行动点数
                Debug.Log($"{nowPlayer.info.name} actionPoint {nowPlayer.info.actionPoint}");

                //处理玩家回合
                if (!nowPlayer.isAI)
                {
                    UITurnManager.Instance.UpdateActionPointBalls(nowPlayer.info.actionPoint, 0);
                    UITurnManager.Instance.ShowTurnPlayerPanel(); //显示玩家回合面板
                    yield return HandlePlayerTurn(nowPlayer); //处理玩家输入
                }
                //处理AI回合
                else
                {
                    UITurnManager.Instance.HideTurnPlayerPanel(); //隐藏玩家回合面板
                    yield return HandleAITurn(nowPlayer); //处理AI回合
                }

                nowPlayer.info.actionPoint = Mathf.Max(Mathf.Floor(nowPlayer.info.actionPoint), 0); //设置行动点数
                Debug.Log($"{nowPlayer.info.name} actionPoint {nowPlayer.info.actionPoint}");

                //如果不满足回合制战斗条件, 直接跳出回合制主循环
                if (!CheckCombatContinueCondition())
                {
                    break;
                }
                if (participantRemoved)
                {
                    i--; //如果有角色从回合制中移除, 则调整下标
                    participantRemoved = false;
                }

                if (!delayPlayerTurn) //如果不是玩家主动推迟回合
                {
                    BuffManager.Instance.UpdateBuffAfterFinishTurn(nowPlayer); //更新buff持续回合
                    yield return ResortTurnParticipants(); //更新下一回合头像排序(因为更新buff后, 角色的先攻值可能发生变化)
                    UITurnManager.Instance.UpdateTurnPortrait(nowPlayer); //更新回合制头像列表UI
                    SkillManager.Instance.UpdateSkillInfoAfterFinishTurn(nowPlayer); //更新技能冷却剩余回合数
                }
                else //如果是玩家主动推迟回合
                {
                    delayPlayerTurn = false;
                    nowPlayer.info.actionPoint -= 4; //将推迟回合得到的行动点数减去
                    participants.Add(nowPlayer); //将推迟角色添加到参与者队列末尾
                    UITurnManager.Instance.UpdateTurnPortraitAfterDelay(); //更新回合制头像列表UI
                }
            }

            //foreach (Character participant in participants.ToList())
            //{
            //    Debug.Log($"{participant.info.name}的回合");

            //    if(participant.info.hp <= 0)
            //    {
            //        continue;
            //    }

            //    nowPlayer = participant;

            //    participant.info.actionPoint = Mathf.Min(participant.info.actionPoint + 4, 6); //恢复行动点数
            //    Debug.Log($"{participant.info.name} actionPoint {participant.info.actionPoint}");

            //    if (participant.CompareTag("Player"))
            //    {
            //        UITurnManager.Instance.ShowTurnPlayerPanel(); //显示玩家回合面板
            //        yield return HandlePlayerTurn(participant); //处理玩家输入
            //    }
            //    else if (participant.CompareTag("Enemy"))
            //    {
            //        UITurnManager.Instance.HideTurnPlayerPanel(); //隐藏玩家回合面板
            //        yield return HandleEnemyTurn(participant); //处理敌人AI
            //    }

            //    participant.info.actionPoint = Mathf.Max(Mathf.Floor(participant.info.actionPoint), 0); //设置行动点数
            //    Debug.Log($"{participant.info.name} actionPoint {participant.info.actionPoint}");

            //    if (!CheckCombatContinueCondition())
            //    {
            //        break;
            //    }

            //    UITurnManager.Instance.UpdateTurnPortrait(nowPlayer); //更新回合制头像列表UI
            //}

            participants = nextTurnParticipants.ToList();
            Debug.Log($"第{CurrentRound}回合结束");
        }
        EndTurn();
    }

    /// <summary>
    /// 检测回合制战斗是否继续
    /// </summary>
    /// <returns></returns>
    public bool CheckCombatContinueCondition()
    {
        bool playersAlive = participants.Any(p => p.CompareTag("Player") && p.info.hp > 0);
        bool enemiesAlive = participants.Any(p => p.CompareTag("Enemy") && p.info.hp > 0);
        //敌人AI调用进入回合制时, 结束条件为玩家队伍一方 或 敌人一方 全部阵亡
        //玩家手动调用进入回合制时, 结束条件为玩家队伍一方全部阵亡 或 玩家手动调用EndCombat()函数
        return playersAlive && (enemiesAlive || isManualTurnStart) && isInTurn;
    }

    /// <summary>
    /// 新参与者加入(如召唤物等)
    /// </summary>
    /// <param name="newParticipants"></param>
    public IEnumerator AddParticipant(List<Character> newParticipants)
    {
        //将新参与者加入下一轮回合的角色列表中
        foreach (Character character in newParticipants)
        {
            nextTurnParticipants.Add(character);

            character.isInTurn = true;

            CoroutineManager.Instance.StopGroup(character.info.name); //停止角色身上的协程队列

            CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.CorrectCharacterPositionInTurn(triggers), character.info.name);
            CoroutineManager.Instance.BindCallback(character.info.name, () =>
            {
                character.animator.SetBool("combat", true); //入战动画
            });
            CoroutineManager.Instance.StartGroup(character.info.name);

            character.info.actionPoint = 0; //初始化行动点数AP

            //将所有参与者脚下的网格设置为dynamicLimit
            character.nowGrid.info.dynamicLimit = true;
            character.nowGrid.info.walkable = false;

            //更新玩家角色和敌方角色的格子列表, 方便移动过程中的借机攻击判断
            if (character.CompareTag("Player"))
            {
                playerGrids.Add(character.nowGrid, character);
            }
            else if (character.CompareTag("Enemy"))
            {
                enemyGrids.Add(character.nowGrid, character);
            }

            //回合开始时, 将每位角色身上的Buff从即时制计时转为回合制计时
            BuffManager.Instance.UpdateBuffAfterStartTurn(character);
            //回合开始时, 将每位角色身上的Skill从即时制计时转为回合制计时
            SkillManager.Instance.UpdateSkillBarUIAfterStartTurn(character);
        }
        //对下一轮回合的角色列表按照先攻值重新排序
        nextTurnParticipants.Sort(new CharacterInitiativeComparer());
        //更新回合制头像列表UI
        yield return UITurnManager.Instance.ResortNextTurnParticipantsPortraits();
    }

    /// <summary>
    /// 移除参与者(如角色死亡等)
    /// </summary>
    /// <param name="participant"></param>
    public void RemoveParticipant(Character participant)
    {
        //if(participant == nowPlayer)
        //{
        //    nowPlayerRemoved = true;
        //}
        //participantRemoved = true;

        //participants.Remove(participant); 先不移除本回合的该角色, 避免因为删除发生其他角色下标的变化
        nextTurnParticipants.Remove(participant);
        if (participant.CompareTag("Player"))
        {
            playerGrids.Remove(participant.nowGrid);
        }
        else if (participant.CompareTag("Enemy"))
        {
            enemyGrids.Remove(participant.nowGrid);
        }
        //更新回合制头像列表UI
        UITurnManager.Instance.RemoveParticipantPortrait(participant);
    }


    #region 玩家控制角色回合
    /// <summary>
    /// 处理玩家回合
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private IEnumerator HandlePlayerTurn(Character player)
    {
        //nowPlayer = player; //更新回合制中的操控玩家角色
        PartyManager.Instance.SwitchLeader(player);
        
        //当回合制继续条件存在 && 回合行动点数>0 && 没有手动结束回合时 回合循环将一直停留在该角色的回合
        while (CheckCombatContinueCondition() && player.info.actionPoint > 0 && player.info.hp > 0 && !endPlayerTurn && !delayPlayerTurn)
        {
            yield return HandlePlayerInput(player); //等待1秒
        }
        endPlayerTurn = false;

        CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.ReturnGridsToPool(), player.info.name + "_pathfinding");
        CoroutineManager.Instance.StartGroup(player.info.name + "_pathfinding");
        yield return new WaitWhile(() => !CoroutineManager.Instance.TaskInGroupIsEmpty(player.info.name + "_pathfinding"));
    }

    //玩家回合, 鼠标位置射线检测, 高亮移动路径, 超出ap点数的格子显示为红色, 触发借机攻击的格子显示为战斗图标 FindPathInTurn
    //控制好检测的协程等待时间, 不能因为鼠标频繁移动位置而消耗太多性能

    /// <summary>
    /// 处理玩家输入
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private IEnumerator HandlePlayerInput(Character player)
    {
        yield return null;

        if (player != PartyManager.Instance.leader || player.isSkillTargeting)
        {
            StartCoroutine(player.pathfinder.ReturnGridsToPool());
            yield return new WaitUntil(() => player == PartyManager.Instance.leader && !player.isSkillTargeting); //如果当前玩家轮次所操控的角色不是主控, 或者正在施法, 就一直等待, 直到成为主控和施法完毕
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Character attackTarget = null;
        GridHelper targetGrid = null;
        //绘制路径网格
        if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character", "Model")) 
            && !player.isMoving && !player.isAttacking && !player.isSkillTargeting) //if (isInTurn && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character")))
        {
            //if (!player.isMoving && !player.isAttacking && !player.isSkillTargeting) //角色非移动、非攻击、非预施法选择目标期间
            //{
                attackTarget = hit.collider.GetComponent<Character>();
                targetGrid = hit.collider.GetComponent<GridHelper>();
                if (attackTarget != null && attackTarget != player) //检测到角色
                {
                    //Debug.Log("raycast character");

                    isMouseOverCharacter = true;
                    isMouseOverGrid = false;
                    CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.FindAttackPath(player.nowGrid, attackTarget.nowGrid, player.info.attackDistance), player.info.name + "_pathfinding");
                    CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.DrawPathGrids((cost) => apTakes = cost), player.info.name + "_pathfinding");
                    UITurnManager.Instance.UpdateActionPointBalls(player.info.actionPoint, apTakes + 2);
                }
                else if (targetGrid != null) //检测到网格
                {
                    //Debug.Log("raycast grid");

                    isMouseOverCharacter = false;
                    isMouseOverGrid = true;
                    CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.FindPath(player.nowGrid, targetGrid), player.info.name + "_pathfinding");
                    CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.DrawPathGrids((cost) => apTakes = cost), player.info.name + "_pathfinding");
                    UITurnManager.Instance.UpdateActionPointBalls(player.info.actionPoint, apTakes);
                }
            //}
        }
        else //都没有检测到
        {
            //Debug.Log("raycast nothing");

            isMouseOverCharacter = false;
            isMouseOverGrid = false;
            CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.ReturnGridsToPool(), player.info.name + "_pathfinding");
            UITurnManager.Instance.UpdateActionPointBalls(player.info.actionPoint, 0);
        }

        if (!Input.GetKey(KeyCode.LeftAlt) && !EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0)) //检测鼠标点击
        {
            if (isMouseOverCharacter) //如果点击到的是角色, 就先移动, 再攻击
            {
                CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.CharacterMoveInTurn(player.info.runSpeed, player.info.rotateSpeed, 1), player.info.name);
                CoroutineManager.Instance.AddTaskToGroup(player.Attack(attackTarget), player.info.name);
            }
            else if (isMouseOverGrid) //如果点击到的事网格, 就移动
            {
                CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.CharacterMoveInTurn(player.info.runSpeed, player.info.rotateSpeed, 1), player.info.name);
            }
            CoroutineManager.Instance.StartGroup(player.info.name);
        }

        CoroutineManager.Instance.StartGroup(player.info.name + "_pathfinding");
        //yield return new WaitWhile(() => !CoroutineManager.Instance.TaskInGroupIsEmpty(player.info.name));
    }
    #endregion


    #region 敌人AI回合
    /// <summary>
    /// 处理敌人AI回合
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    private IEnumerator HandleAITurn(Character character)
    {
        UIPartyManager.Instance.UpdateHighlight(character);
        CameraMoveManager.Instance.ChangeToNewLeader(character); //将镜头平衡移动到当前回合角色身上
        CharactertAI charactertAI = character.GetComponent<CharactertAI>(); //获得AI角色身上挂载的CharacterAI脚本
        if (charactertAI != null)
        {
            yield return charactertAI.HandleAIBehavior();
        }
        yield return new WaitForSeconds(1.0f);
    }
    #endregion

}
