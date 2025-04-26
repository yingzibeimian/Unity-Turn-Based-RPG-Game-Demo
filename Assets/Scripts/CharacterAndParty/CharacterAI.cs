using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharactertAI : MonoBehaviour
{
    public List<Vector3Int> patrolPath = new List<Vector3Int>(); //巡逻路径, 记录路径网格的(q,r,heightorder)
    private List<GridHelper> patrolPathGrids = new List<GridHelper>();

    public float checkInterval = 2.0f; //检测附近是否存在玩家的间隔事件
    private WaitForSeconds waitForCheck;
    public float idleTime = 3.0f; //巡逻中的停留时间
    private WaitForSeconds waitForIdle;

    public float checkRadius = 10.0f; //入战检测范围半径

    public float proximityWeight = 0.2f; //距离优先权重 (优先攻击距离较近的敌人 0=不关注 1=最高优先级)
    public float groupDensityWeight = 0.3f; //群体聚集优先权重 (优先攻击聚集在一起的敌人)
    public float threatWeight = 0.25f; //威胁优先权重 (优先攻击伤害高(威胁更大)的敌人)
    public float aromrPenaltyWeight = 0.25f; //护甲惩罚权重 (敌人护甲越高, 优先攻击权值越低)

    public Character character;
    public AStarPathfinder pathfinder;
    public Animator animator;

    //敌人节点
    private class AITarget : IComparable<AITarget>
    {
        public Character character; //敌人角色
        public float targetValue; //敌人成为目标的权值

        public AITarget(Character character, float targetWeight)
        {
            this.character = character;
            this.targetValue = targetWeight;
        }

        public int CompareTo(AITarget other)
        {
            return targetValue.CompareTo(other.targetValue);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if(character == null)
        {
            character = gameObject.GetComponent<Character>();
        }
        if(pathfinder == null)
        {
            pathfinder = gameObject.GetComponent<AStarPathfinder>();
        }
        if(animator == null)
        {
            animator = gameObject.GetComponent<Animator>();
        }

        waitForCheck = new WaitForSeconds(checkInterval);
        waitForIdle = new WaitForSeconds(idleTime);
        if (!character.isInTurn && character.info.hp > 0) //if (!TurnManager.Instance.isInTurn && character.info.hp > 0)
        {
            if(patrolPath.Count > 1)
            {
                CoroutineManager.Instance.AddTaskToGroup(InitializePatrolPath(), character.info.name + "_Patrol");
                CoroutineManager.Instance.AddTaskToGroup(Patrol(), character.info.name + "_Patrol");
                CoroutineManager.Instance.StartGroup(character.info.name + "_Patrol");
            }
            CoroutineManager.Instance.AddTaskToGroup(CheckForPlayer(), character.info.name + "_Check");
            CoroutineManager.Instance.StartGroup(character.info.name + "_Check");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 初始化巡逻路径
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializePatrolPath()
    {
        yield return new WaitWhile(() => GridMap.Instance == null || !GridMap.Instance.gridMapInitialized || !character.characterInitialized);
        foreach(Vector3Int pos in patrolPath)
        {
            print(pos);
            GridHelper grid = GridMap.Instance.SearchGrid(pos.x, pos.y, pos.z);
            if (grid != null)
            {
                patrolPathGrids.Add(grid);
            }
            yield return null;
        }
        yield break;
    }

    /// <summary>
    /// Enemy自动巡逻函数
    /// </summary>
    /// <returns></returns>
    private IEnumerator Patrol()
    {
        while (!character.isInTurn && character.info.hp > 0) //while(!TurnManager.Instance.isInTurn && character.info.hp > 0)
        {
            for(int i = 0; i < patrolPathGrids.Count; i++)
            {
                Debug.Log($"{character.info.name} move");
                character.MoveTo(patrolPathGrids[i], null);
                yield return new WaitWhile(() => CoroutineManager.Instance.TaskInGroupIsEmpty(character.info.name));
                yield return new WaitUntil(() => CoroutineManager.Instance.TaskInGroupIsEmpty(character.info.name));
                yield return waitForIdle;
                if (i == patrolPathGrids.Count - 1)
                {
                    i = -1;
                }
                if (character.isInTurn) //if (TurnManager.Instance.isInTurn)
                {
                    yield break;
                }
            }
        }
        yield break;
    }

    /// <summary>
    /// 检测是否有玩家存在
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckForPlayer()
    {
        yield return waitForCheck;
        //如果已经在回合制中, 或者是玩家召唤物 就不检测
        if (character.isInTurn || this.tag == "Player")
        {
            yield break;
        }

        List<Character> nearPlayer = new List<Character>();
        while(!character.isInTurn && character.info.hp > 0) //while(!TurnManager.Instance.isInTurn && character.info.hp > 0)
        {
            Collider[] colliders = Physics.OverlapSphere(this.transform.position, checkRadius, LayerMask.GetMask("Character", "Model"));
            foreach (Collider collider in colliders)
            {
                Character participant = collider.GetComponent<Character>();
                if(participant != null && !participant.isInTurn && participant.tag != this.tag)
                {
                    nearPlayer.Add(participant);
                }
            }

            if(nearPlayer.Count > 0)
            {
                nearPlayer.Add(character);
                TurnManager.Instance.StartTurn(nearPlayer, false);
                yield break;
            }

            yield return waitForCheck;
        }

        yield break;
    }

    /// <summary>
    /// 处理AI行为逻辑
    /// </summary>
    /// <returns></returns>
    public IEnumerator HandleAIBehavior()
    {
        yield return new WaitUntil(() => character.characterInitialized); //确保AI角色完成初始化

        Debug.Log("HandleAIBehavior");
        //当回合制继续条件存在 && 回合行动点数>0 && 血量>0时 回合循环将一直停留在该AI角色的AI行为逻辑处理中
        while (TurnManager.Instance.CheckCombatContinueCondition() && character.info.actionPoint > 0 && character.info.hp > 0)
        {
            Character target = null; //本次AI行为的目标角色
            SkillBaseData skill = null; //本次AI行为释放的技能
            List<SkillBaseData> characterSkills = character.characterSkills; //角色的技能列表
            //如果血量低于1/3, 且终结技能(AI角色技能列表中的最后一个)的剩余冷却回合数为0, 消耗行动点数小于等于角色行动点数, 就释放终结技能
            if (character.info.hp <= (character.info.maxHp / 3) && characterSkills.Count > 0 && characterSkills[characterSkills.Count - 1].remainingTurns == 0 
                && characterSkills[characterSkills.Count - 1].actionPointTakes <= character.info.actionPoint)
            {
                skill = characterSkills[characterSkills.Count - 1];
            }
            if (skill == null)
            {
                //遍历角色的所有非终结技能, 如果剩余冷却回合数为0, 且消耗行动点数小于等于角色行动点数, 就释放该技能
                for (int i = 0; i < characterSkills.Count - 1; i++)
                {
                    if (characterSkills[i].remainingTurns == 0 && characterSkills[i].actionPointTakes <= character.info.actionPoint)
                    {
                        skill = characterSkills[i];
                        break;
                    }
                }
            }
            //如果存在待释放的技能, 并且 技能是效果应用类技能且能影响敌方阵营
            if (skill != null && skill is EffectApplicationSkillData && (skill as EffectApplicationSkillData).effectArea.affectOppositeCamp)
            {
                //如果AI脚本控制角色为Player, 则在Enemy中寻找行为AI目标target
                if (character.CompareTag("Player"))
                {
                    yield return GetAIBehaviorTargetCharater(TurnManager.Instance.enemyGrids.Values.ToList(), (result) => { target = result; });
                }
                //如果AI脚本控制角色为Enemy, 则在Player中寻找行为AI目标target
                else if (character.CompareTag("Enemy"))
                {
                    yield return GetAIBehaviorTargetCharater(TurnManager.Instance.playerGrids.Values.ToList(), (result) => { target = result; });
                }
                //如果AI行为目标不为空, 就在target身上应用skill技能效果
                if (target != null)
                {
                    yield return SkillManager.Instance.ApplyCharacterAISkillEffects(character, target, skill);
                    //CoroutineManager.Instance.AddTaskToGroup(SkillManager.Instance.ApplyCharacterAISkillEffects(character, target, skill), character.info.name);
                }
            }
            //如果存在待释放的技能, 并且 技能是效果应用类技能且能影响己方阵营
            else if (skill != null && skill is EffectApplicationSkillData && (skill as EffectApplicationSkillData).effectArea.affectSelfCamp)
            {
                //如果AI脚本控制角色为Player, 则在Player中寻找行为AI目标target
                if (character.CompareTag("Player"))
                {
                    yield return GetAIBehaviorTargetCharater(TurnManager.Instance.playerGrids.Values.ToList(), (result) => { target = result; });
                }
                //如果AI脚本控制角色为Enemy, 则在Enemy中寻找行为AI目标target
                else if (character.CompareTag("Enemy"))
                {
                    yield return GetAIBehaviorTargetCharater(TurnManager.Instance.enemyGrids.Values.ToList(), (result) => { target = result; });
                }
                if (target != null)
                {
                    yield return SkillManager.Instance.ApplyCharacterAISkillEffects(character, target, skill);
                }
            }
            //如果存在待释放的技能, 并且 技能是效果应用类技能且能影响施法者自身
            else if (skill != null && skill is EffectApplicationSkillData && (skill as EffectApplicationSkillData).effectArea.affectSelf)
            {
                //将AI目标target设为施法者本身
                target = character;
                yield return SkillManager.Instance.ApplyCharacterAISkillEffects(character, target, skill);
            }
            //如果存在待释放的技能, 并且 技能是召唤类技能
            else if (skill != null && skill is SummonSkillData)
            {
                //如果召唤范围以可选择点为中心
                if ((skill as SummonSkillData).summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
                {
                    //如果AI脚本控制角色为Player, 则在Enemy中寻找行为AI目标target
                    if (character.CompareTag("Player"))
                    {
                        yield return GetAIBehaviorTargetCharater(TurnManager.Instance.enemyGrids.Values.ToList(), (result) => { target = result; });
                    }
                    //如果AI脚本控制角色为Enemy, 则在Player中寻找行为AI目标target
                    else if (character.CompareTag("Enemy"))
                    {
                        yield return GetAIBehaviorTargetCharater(TurnManager.Instance.playerGrids.Values.ToList(), (result) => { target = result; });
                    }
                }
                //如果召唤范围以自身为中心
                else if ((skill as SummonSkillData).summonArea.effectAnchor == EffectAnchorMode.selfCentered)
                {
                    target = character;
                }
                if (target != null)
                {
                    yield return SkillManager.Instance.ApplyCharacterAISkillEffects(character, target, skill);
                    //CoroutineManager.Instance.AddTaskToGroup(SkillManager.Instance.ApplyCharacterAISkillEffects(character, target, skill), character.info.name);
                }
            }
            //如果没有要释放的技能, 则尝试寻路和攻击target目标角色
            else if (skill == null)
            {
                float apTakePerGrid = 1.0f / character.info.runSpeed; //AI脚本所控制角色的每格移动所消耗的行动点数
                //如果角色至少还能移动一格, 则进行寻路和攻击
                if (character.info.actionPoint >= apTakePerGrid)
                {
                    //如果AI脚本控制角色为Player, 则在Enemy中寻找行为AI目标target
                    if (character.CompareTag("Player"))
                    {
                        yield return GetAIBehaviorTargetCharater(TurnManager.Instance.enemyGrids.Values.ToList(), (result) => { target = result; });
                    }
                    //如果AI脚本控制角色为Enemy, 则在Player中寻找行为AI目标target
                    else if (character.CompareTag("Enemy"))
                    {
                        yield return GetAIBehaviorTargetCharater(TurnManager.Instance.playerGrids.Values.ToList(), (result) => { target = result; });
                    }
                    if (target != null)
                    {
                        //如果character距离攻击目标的距离 大于 character的攻击距离, 就先寻路和移动
                        if (GetDistance(character.nowGrid, target.nowGrid) > character.info.attackDistance)
                        {
                            CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, character.info.attackDistance), character.info.name);
                            CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1), character.info.name);

                            //yield return character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, character.info.attackDistance);
                            //yield return character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1);
                        }
                        //如果移动后, 角色行动点数不足以进行攻击, 就直接返回
                        if (character.info.actionPoint < 2)
                        {
                            yield break;
                        }
                        //yield return character.Attack(target);
                        CoroutineManager.Instance.AddTaskToGroup(character.Attack(target), character.info.name);
                    }
                }
                //如果角色一格也无法移动, 则结束AI行为逻辑处理协程, 结束角色回合
                else
                {
                    yield break;
                }
            }

            //如果AI行为目标角色为空, 代表没有可以选择的目标角色, 则结束AI行为逻辑处理协程, 结束角色回合
            if (target == null)
            {
                Debug.Log("target null");
                yield break;
            }
            //如果不为空, 就开始执行本次AI行为的协程队列
            else
            {
                CoroutineManager.Instance.StartGroup(character.info.name);
                yield return new WaitUntil(() => CoroutineManager.Instance.TaskInGroupIsEmpty(character.info.name)); //一直等待直到AI行为协程队列执行完毕
            }

             yield return null;
        }
    }

    /// <summary>
    /// 得到AI行为的目标角色target
    /// </summary>
    /// <param name="camp"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    private IEnumerator GetAIBehaviorTargetCharater(List<Character> camp, Action<Character> onTargetFound) 
    {
        MinHeap<AITarget> targets = new MinHeap<AITarget>(); //敌人节点的最小堆, 选取堆顶元素作为本次AI行为的目标敌人
        foreach (Character character in camp)
        {
            if(character.info.hp > 0)
            {
                targets.Enqueue(new AITarget(character, GetAITargetValue(character, camp)));
                yield return null;
            }
        }
        Character foundTarget = targets.Count > 0 ? targets.Dequeue().character : null;
        onTargetFound?.Invoke(foundTarget); // 通过回调返回结果
    }

    /// <summary>
    /// 计算得到AITarget中的目标权值变量targetValue
    /// </summary>
    /// <param name="enemy"></param>
    /// <param name="enemyCamp"></param>
    /// <returns></returns>
    private float GetAITargetValue(Character enemy, List<Character> enemyCamp)
    {
        float proximityValue = GetDistance(enemy.nowGrid, character.nowGrid);
        float groupDensityValue = 0.0f;
        foreach (Character otherEnemy in enemyCamp)
        {
            if (otherEnemy != enemy)
            {
                groupDensityValue += GetDistance(enemy.nowGrid, otherEnemy.nowGrid);
            }
        }
        groupDensityValue /= (enemyCamp.Count - 1);
        float threatValue = enemy.info.damageDiceCount * enemy.info.damageDiceCount * -1.0f;
        float armorPenaltyValue = enemy.info.armorClass;
        return proximityValue * proximityWeight + groupDensityValue * groupDensityWeight + threatValue * threatWeight + armorPenaltyValue * aromrPenaltyWeight;
    }

    /// <summary>
    /// 根据轴向坐标计算网格start到end的距离
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    private float GetDistance(GridHelper start, GridHelper end)
    {
        int dq = Mathf.Abs(start.info.q - end.info.q);
        int dr = Mathf.Abs(start.info.r - end.info.r);
        int ds = Mathf.Abs(start.info.s - end.info.s);
        return (dq + dr + ds) / 2.0f;
    }
}
