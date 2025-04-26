using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

public class CharacterInitiativeComparer : IComparer<Character>
{
    //降序排序, 先攻高的在前
    public int Compare(Character a, Character b) => b.info.initiative.CompareTo(a.info.initiative);
}


/// <summary>
/// 角色信息
/// </summary>
[System.Serializable]
public class CharacterInfo
{
    public string name; //名字
    public int level; //等级
    public int maxHp = 5; //最大生命值
    public int hp = 5; //当前生命值
    public int attackDistance = 1; //攻击距离

    public int strength = 8; //力量
    public int finesse = 8; //敏捷
    public int intelligence = 8; //智力
    public int constitution = 8; //体质
    public int charisma = 8;
    public int wits = 8; //智慧

    public int damageDiceCount = 1; //伤害骰子个数
    public int damageDiceSides = 3; //伤害骰子面数

    public int armorClass; //防御等级AC

    public AttackType attackType; //攻击受影响属性
    public int proficiency; //武器熟练加值

    public int initiative; //先攻
    public float actionPoint; //回合行动点数

    public float walkSpeed; //行走速度
    public float runSpeed; //跑步速度
    public float rotateSpeed; //转身速度

    //public bool isAI; //用来判断是否是AI控制

    //所在网格信息
    public int q;
    public int r;
    public int s => -q - r;
    public int heightOrder;

    public Sprite portrait; //头像
    public Sprite bustPortrait; //半身像
}

/// <summary>
/// 角色装备
/// </summary>
[System.Serializable]
public class CharacterEquipment
{
    public EquipmentItemData helmetEquipment;
    public EquipmentItemData chestEquipment;
    public EquipmentItemData glowesEquipment;
    public EquipmentItemData beltEquipment;
    public EquipmentItemData bootsEquipment;
    public EquipmentItemData amuletEquipment;
    public EquipmentItemData leftRingEquipment;
    public EquipmentItemData rightRingEquipment;
    public EquipmentItemData leggingsEquipment;
    public EquipmentItemData weaponEquipment;
}

/// <summary>
/// 召唤物信息
/// </summary>
public class SummonedUnitInfo
{
    public Character character; //召唤单位的角色脚本
    public bool inheritSummonerLifespan = false; //召唤单位是否依赖召唤者存在 
}


public class Character : MonoBehaviour
{
    public CharacterData characterInfo; //角色ScriptableObject数据
    public CharacterInfo info; //角色信息

    public CharacterEquipment equipment;

    public List<ItemBaseData> bagItems; //背包物品信息
    public List<SkillBaseData> characterSkills = new List<SkillBaseData>(); //角色拥有技能
    public HashSet<Buff> buffs = new HashSet<Buff>(); //角色身上的buff信息

    public AStarPathfinder pathfinder; //角色身上挂载的A*寻路脚本
    public Animator animator;
    public GridHelper nowGrid => GridMap.Instance.SearchGrid(info.q, info.r, info.heightOrder); //角色当前所在网格

    [HideInInspector]
    public bool characterInitialized = false; //记录角色是否初始化完成
    [HideInInspector]
    public bool isMoving = false; //用于标记角色是否正在移动
    [HideInInspector]
    public bool isAttacking = false; //用于标记角色是否正在攻击
    [HideInInspector]
    public bool beingAttacked = false; //用于标记角色是否正在受击
    [HideInInspector]
    public bool isSkillTargeting = false; //用于标记角色是否正在预施法阶段选择技能目标中
    [HideInInspector]
    public bool isInTurn = false; //用于标记角色是否正在回合制中
    public bool isAI = false; //用于标记角色行动是否游AI脚本控制

    [HideInInspector]
    public List<SummonedUnitInfo> summonedUnits = new List<SummonedUnitInfo>(); //召唤物信息
    [HideInInspector]
    //public Character summoner; //召唤者

    private List<Character> group = new List<Character>(); //用于当前角色作为主控时, 记录队友
    private Coroutine getNearGridsCoroutine; //用于存储协程, 防止并行执行多个跟随寻路任务
    private Coroutine followToCoroutine; //用于存储协程, 防止并行执行多个跟随寻路任务
    private float moveWeight; //角色移动动画混合权重
    private float moveSpeed; //角色移动速度

    private string attackerColor = "#F0F8FF"; //默认战斗日志颜色

    public Transform weaponParent; //装备武器父对象
    private GameObject weaponModel; //武器模型

    // Start is called before the first frame update
    void Start()
    {
        if(pathfinder == null)
        {
            pathfinder = this.GetComponent<AStarPathfinder>();
        }
        if(animator == null)
        {
            animator = this.GetComponent<Animator>();
        }

        //初始化角色数据
        InitializeCharacterInfo();

        //深拷贝背包中所有的ScriptableObject物品数据
        for (int i = 0; i < bagItems.Count; i++)
        {
            ItemBaseData original = bagItems[i];
            ItemBaseData copy = Instantiate(original);
            bagItems[i] = copy;
        }
        //深拷贝技能列表中所有的ScriptableObject技能数据
        for (int i = 0; i < characterSkills.Count; i++)
        {
            SkillBaseData original = characterSkills[i];
            SkillBaseData copy = Instantiate(original);
            characterSkills[i] = copy;
        }

        attackerColor = CompareTag("Player") ? "#F0F8FF" : "#B22222"; //根据是Player还是Enemy来决定在战斗日志中显示的色彩

        characterInitialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("P");
            CoroutineManager.Instance.StopGroup(info.name);
        }
    }

    private void InitializeCharacterInfo()
    {
        //将SO数据赋给info
        info.name = characterInfo.characterName;
        info.level = characterInfo.level;
        info.maxHp = characterInfo.maxHp;
        info.hp = characterInfo.hp;
        info.attackDistance = characterInfo.attackDistance;
        info.strength = characterInfo.strength;
        info.finesse = characterInfo.finesse;
        info.intelligence = characterInfo.intelligence;
        info.charisma = characterInfo.charisma;
        info.wits = characterInfo.wits;
        info.armorClass = characterInfo.armorClass;
        info.attackType = characterInfo.attackType;
        info.proficiency = characterInfo.proficiency;
        info.initiative = characterInfo.initiative;
        info.actionPoint = characterInfo.actionPoint;
        info.walkSpeed = characterInfo.walkSpeed;
        info.runSpeed = characterInfo.runSpeed;
        info.rotateSpeed = characterInfo.rotateSpeed;
        info.q = characterInfo.q;
        info.r = characterInfo.r;
        info.heightOrder = characterInfo.heightOrder;
        info.portrait = characterInfo.portrait;
        info.bustPortrait = characterInfo.bustPortrait;

        //进行装备
        if (equipment.helmetEquipment != null)
        {
            equipment.helmetEquipment = Instantiate(equipment.helmetEquipment); //深拷贝装备数据
            Equip(equipment.helmetEquipment);
        }
        if (equipment.chestEquipment != null)
        {
            equipment.chestEquipment = Instantiate(equipment.chestEquipment);
            Equip(equipment.chestEquipment);
        }
        if (equipment.glowesEquipment != null)
        {
            equipment.glowesEquipment = Instantiate(equipment.glowesEquipment);
            Equip(equipment.glowesEquipment);
        }
        if (equipment.beltEquipment != null)
        {   equipment.beltEquipment = Instantiate(equipment.beltEquipment);
            Equip(equipment.beltEquipment);
        }
        if (equipment.bootsEquipment != null)
        {   equipment.bootsEquipment = Instantiate(equipment.bootsEquipment);
            Equip(equipment.bootsEquipment);
        }
        if (equipment.amuletEquipment != null)
        {   equipment.amuletEquipment = Instantiate(equipment.amuletEquipment);
            Equip(equipment.amuletEquipment);
        }
        if (equipment.leftRingEquipment != null)
        {   equipment.leftRingEquipment = Instantiate(equipment.leftRingEquipment);
            Equip(equipment.leftRingEquipment);
        }
        if (equipment.rightRingEquipment != null)
        {   equipment.rightRingEquipment = Instantiate(equipment.rightRingEquipment);
            Equip(equipment.rightRingEquipment);
        }
        if (equipment.leggingsEquipment != null)
        {   equipment.leggingsEquipment = Instantiate(equipment.leggingsEquipment);
            Equip(equipment.leggingsEquipment);
        }
        if (equipment.weaponEquipment != null)
        {   equipment.weaponEquipment = Instantiate(equipment.weaponEquipment);
            Equip(equipment.weaponEquipment);
        }
    }

    /// <summary>
    /// 将当前脚本所挂载到的主控角色移动到 targetGrid
    /// </summary>
    /// <param name="targetGrid"></param>
    /// <param name="group"></param>
    public void MoveTo(GridHelper targetGrid, LinkedList<Character> group)
    {
        if(group != null)
        {
            foreach (Character c in group)
            {
                if (c != this)
                {
                    this.group.Add(c); //将主控角色所属分队的队友加入this.group中
                }
            }
        }
        if(pathfinder != null)
        {
            //动态调整移动速度, 移动距离小于2时, 速度=walkSpeed; 大于6时, 速度=runSpeed; 2到6时, 介于两者之间
            moveWeight = Mathf.Clamp(-.5f + getDistance(nowGrid, targetGrid) / 4.0f, 0, 1); //动画混合权重
            moveSpeed = Mathf.Lerp(info.walkSpeed, info.runSpeed, moveWeight);
            //pathfinder.StartPathfinding(nowGrid, targetGrid, moveSpeed, info.rotateSpeed, moveWeight, FollowTo);

            int followerCount = this.group.Count + summonedUnits.Count; //跟随移动的角色数量
            if (this.group != null)
            {
                foreach (Character c in this.group)
                {
                    followerCount += c.summonedUnits.Count;
                }
            }

            //移动主控角色
            CoroutineManager.Instance.StopGroup(info.name); //停止当前角色的移动协程队列
            animator.SetBool("isMoving", false);
            CoroutineManager.Instance.AddTaskToGroup(pathfinder.FindPath(nowGrid, targetGrid), info.name); //寻路
            CoroutineManager.Instance.AddTaskToGroup(pathfinder.GetNearGrids(followerCount, StartFollowTo), info.name); //为队友寻找附近网格
            CoroutineManager.Instance.AddTaskToGroup(pathfinder.CharacterMove(moveSpeed, info.rotateSpeed, moveWeight), info.name); //移动
            CoroutineManager.Instance.StartGroup(info.name); //启动当前角色的移动协程队列
        }
    }

    /// <summary>
    /// 启动队友跟随
    /// </summary>
    /// <param name="nearGrids"></param>
    public void StartFollowTo(List<GridHelper> nearGrids)
    {
        if(group.Count + summonedUnits.Count == 0)
        {
            return; //没有队友和召唤物
        }
        if (getNearGridsCoroutine != null)
        {
            StopCoroutine(followToCoroutine); //如果group中的队友正在通过协程方法寻找附近网格, 先停止它
        }
        followToCoroutine = StartCoroutine(FollowTo(nearGrids));
    }

    /// <summary>
    /// 将group中的队友移动到path路径最后靠近targetGrid的网格
    /// </summary>
    /// <param name="nearGrids"></param>
    /// <returns></returns>
    public IEnumerator FollowTo(List<GridHelper> nearGrids)
    {
        //跟随角色列表
        List<Character> followerGroup = new List<Character>();
        foreach (Character c in group)
        {
            followerGroup.Add(c);
        }
        foreach (SummonedUnitInfo unitInfo in summonedUnits)
        {
            followerGroup.Add(unitInfo.character);
        }
        foreach (Character c in group)
        {
            foreach (SummonedUnitInfo unitInfo in c.summonedUnits)
            {
                followerGroup.Add(unitInfo.character);
            }
        }

        for (int i = 0; i < nearGrids.Count; i++) //遍历得到的主控路径终点的附近网格
        {
            Character follower = null;
            if (followerGroup.Count > i)
            {
                follower = followerGroup[i];
            }
            if(follower != null && follower.pathfinder != null && nearGrids[i] != null)
            {
                yield return new WaitForSeconds(0.5f); //为了不使队伍行进拥挤, 队员间间隔一段时间再走
                CoroutineManager.Instance.StopGroup(follower.info.name); //停止正在进行的移动协程队列
                CoroutineManager.Instance.AddTaskToGroup(follower.pathfinder.FindPath(follower.nowGrid, nearGrids[i]), follower.info.name); //寻路
                CoroutineManager.Instance.AddTaskToGroup(follower.pathfinder.CharacterMove(moveSpeed - (i + 1) * 0.05f, follower.info.rotateSpeed, moveWeight), follower.info.name); //移动
                CoroutineManager.Instance.StartGroup(follower.info.name); //启动队友的移动协程队列
            }
            if(i >= followerGroup.Count)
            {
                break;
            }
        }
        this.group.Clear();
    }

    /// <summary>
    /// 角色攻击
    /// </summary>
    /// <param name="attackTarget"></param>
    /// <returns></returns>
    public IEnumerator Attack(Character attackTarget)
    {
        if(info.actionPoint < 2 || info.hp <= 0) //判断初始行动点数是否能攻击 或者是否已死亡
        {
            Debug.Log($"Cant Attack, actionPoint{info.actionPoint}");
            yield break;
        }
        isAttacking = true; //标记为正在攻击中
        //更新角色朝向
        Vector3 direction = (attackTarget.transform.position - this.transform.position).normalized;
        Quaternion startRotation = this.transform.rotation; //当前朝向
        Quaternion endRotation = Quaternion.LookRotation(direction); //终点朝向
        float rotateWeight = 0;
        while (Quaternion.Angle(this.transform.rotation, endRotation) > 0.05f)
        {
            rotateWeight += Time.deltaTime * info.rotateSpeed;
            this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
            yield return null;
        }
        transform.rotation = endRotation;
        //根据武器类型 触发攻击动画
        SetTriggerAttackAnimation();

        Debug.Log($"{info.name} Attack {attackTarget.info.name}");
        yield return new WaitWhile(() => isAttacking);

        //攻击投掷
        DiceResult attackRoll = null;
        //伤害投掷
        DiceResult damageRoll = null;
        int damageDiceCount = info.damageDiceCount;
        //等待攻击投掷结果
        yield return DiceManager.Instance.RollDice(DiceType.attackDice, 1, 20, GetRollModifier(), info.bustPortrait, (result) => attackRoll = result);
        //根据攻击投掷结果决定是否造成伤害, 是否暴击, 并更新战斗日志
        if (attackRoll.criticalMiss)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>攻击</u></color>, 攻击投掷结果为1(大失败), 攻击失败");
            if (isInTurn)
            {
                info.actionPoint -= 2; //更新行动点数
                UITurnManager.Instance.UpdateActionPointBalls(info.actionPoint, 0);
            }
            yield break;
        }
        else if (attackRoll.criticalHit)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>攻击</u></color>, 攻击投掷结果为20(大成功), 造成<color=red>暴击</color>");
            damageDiceCount *= 2;
        }
        else
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>攻击</u></color>, 攻击投掷结果为{attackRoll.totalValue}");
        }
        //等待伤害投掷结果
        yield return DiceManager.Instance.RollDice(DiceType.damageDice, damageDiceCount, info.damageDiceSides, GetRollModifier(), info.bustPortrait, (result) => damageRoll = result);

        //传递伤害
        attackTarget.TakeDamage(attackRoll, damageRoll); //攻击对象受到伤害
        
        yield return new WaitWhile(() => attackTarget.beingAttacked);
        if (isInTurn)
        {
            info.actionPoint -= 2; //更新行动点数
            UITurnManager.Instance.UpdateActionPointBalls(info.actionPoint, 0);
        }
    }

    /// <summary>
    /// 借机攻击
    /// </summary>
    /// <param name="attackTarget"></param>
    /// <returns></returns>
    public IEnumerator OpportunityAttack(Character attackTarget)
    {
        //目标对象已经死亡, 则直接返回
        if (attackTarget.info.hp <= 0)
        {
            yield break;
        }

        isAttacking = true; //标记为正在攻击中
        //更新角色朝向
        Vector3 direction = (attackTarget.transform.position - this.transform.position).normalized;
        Quaternion startRotation = this.transform.rotation; //当前朝向
        Quaternion endRotation = Quaternion.LookRotation(direction); //终点朝向
        float rotateWeight = 0;
        while (Quaternion.Angle(this.transform.rotation, endRotation) > 0.05f)
        {
            rotateWeight += Time.deltaTime * info.rotateSpeed;
            this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
            yield return null;
        }
        transform.rotation = endRotation;
        //触发攻击动画
        SetTriggerAttackAnimation();

        Debug.Log($"{info.name} OpportunityAttack {attackTarget.info.name}");
        yield return new WaitWhile(() => isAttacking);

        //更新战斗日志
        //UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>借机攻击</u></color>");

        //attackTarget.TakeDamage(10, 1); //攻击对象受到伤害

        //攻击投掷
        DiceResult attackRoll = null;
        //伤害投掷
        DiceResult damageRoll = null;
        int damageDiceCount = info.damageDiceCount;
        //等待攻击投掷结果
        yield return DiceManager.Instance.RollDice(DiceType.attackDice, 1, 20, GetRollModifier(), info.bustPortrait, (result) => attackRoll = result);
        //根据攻击投掷结果决定是否造成伤害, 是否暴击, 并更新战斗日志
        if (attackRoll.criticalMiss)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>攻击</u></color>, 攻击投掷结果为1(大失败), 攻击失败");
            yield break;
        }
        else if (attackRoll.criticalHit)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>攻击</u></color>, 攻击投掷结果为20(大成功), 造成<color=red>暴击</color>");
            damageDiceCount *= 2;
        }
        else
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>攻击</u></color>, 攻击投掷结果为{attackRoll.totalValue}");
        }
        //等待伤害投掷结果
        yield return DiceManager.Instance.RollDice(DiceType.damageDice, damageDiceCount, info.damageDiceSides, GetRollModifier(), info.bustPortrait, (result) => damageRoll = result);

        //传递伤害
        attackTarget.TakeDamage(attackRoll, damageRoll); //攻击对象受到伤害

        yield return new WaitWhile(() => attackTarget.beingAttacked);
    }

    /// <summary>
    /// 角色被借机攻击时的反击方法
    /// </summary>
    /// <param name="counterTarget"></param>
    /// <returns></returns>
    public IEnumerator CounterAttack(Character counterTarget)
    {
        if (info.actionPoint < 2 || info.hp <= 0 || counterTarget.info.hp <= 0) //判断初始行动点数是否能攻击
        {
            Debug.Log($"Cant CountAttack, actionPoint{info.actionPoint}");
            yield break;
        }
        isAttacking = true; //标记为正在攻击中
        //更新角色朝向
        Vector3 direction = (counterTarget.transform.position - this.transform.position).normalized;
        Quaternion startRotation = this.transform.rotation; //当前朝向
        Quaternion endRotation = Quaternion.LookRotation(direction); //终点朝向
        float rotateWeight = 0;
        while (Quaternion.Angle(this.transform.rotation, endRotation) > 0.05f)
        {
            rotateWeight += Time.deltaTime * info.rotateSpeed;
            this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
            yield return null;
        }
        transform.rotation = endRotation;
        //触发攻击动画
        SetTriggerAttackAnimation();

        Debug.Log($"{info.name} CounterAttack {counterTarget.info.name}");
        yield return new WaitWhile(() => isAttacking);

        //更新战斗日志
        //UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>反击</u></color>");

        //counterTarget.TakeDamage(10, 1); //攻击对象受到伤害

        //攻击投掷
        DiceResult attackRoll = null;
        //伤害投掷
        DiceResult damageRoll = null;
        int damageDiceCount = info.damageDiceCount;
        //等待攻击投掷结果
        yield return DiceManager.Instance.RollDice(DiceType.attackDice, 1, 20, GetRollModifier(), info.bustPortrait, (result) => attackRoll = result);
        //根据攻击投掷结果决定是否造成伤害, 是否暴击, 并更新战斗日志
        if (attackRoll.criticalMiss)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>攻击</u></color>, 攻击投掷结果为1(大失败), 攻击失败");
            if (isInTurn)
            {
                info.actionPoint -= 2; //更新行动点数
                UITurnManager.Instance.UpdateActionPointBalls(info.actionPoint, 0);
            }
            yield break;
        }
        else if (attackRoll.criticalHit)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>攻击</u></color>, 攻击投掷结果为20(大成功), 造成<color=red>暴击</color>");
            damageDiceCount *= 2;
        }
        else
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 发起 <color=#DAA520><u>攻击</u></color>, 攻击投掷结果为{attackRoll.totalValue}");
        }
        //等待伤害投掷结果
        yield return DiceManager.Instance.RollDice(DiceType.damageDice, damageDiceCount, info.damageDiceSides, GetRollModifier(), info.bustPortrait, (result) => damageRoll = result);

        //传递伤害
        counterTarget.TakeDamage(attackRoll, damageRoll); //攻击对象受到伤害

        yield return new WaitWhile(() => counterTarget.beingAttacked);
        info.actionPoint -= 2; //更新行动点数
    }

    /// <summary>
    /// 处理效果类技能的效果应用方法, 如果技能造成伤害, 则根据技能skill进行技能攻击和伤害投掷, 传递伤害和添加Buff; 如果技能不造成伤害, 则之间添加Buff
    /// </summary>
    /// <param name="skill"></param>
    /// <param name="effectsTarget"></param>
    /// <returns></returns>
    public IEnumerator AppplySkillEffectsOnTarget(EffectApplicationSkillData skill, HashSet<Character> effectsTarget)
    {
        Debug.Log("AppplySkillEffectsOnTarget1");
        if (skill.damageDiceCount > 0 && !string.IsNullOrEmpty(skill.animationTriggerStr) && skill.animationTriggerStr != "")
        {
            isAttacking = true; //标记为正在攻击中
        }
        SetTriggerSkillAnimation(skill); //触发技能动画
        yield return new WaitWhile(() => isAttacking);
        Debug.Log("AppplySkillEffectsOnTarget2");

        //如果技能造成伤害, 则根据技能skill进行技能攻击和伤害投掷
        if (skill.damageDiceCount > 0)
        {
            //攻击投掷
            DiceResult attackRoll = null;
            //伤害投掷
            DiceResult damageRoll = null;
            int damageDiceCount = skill.damageDiceCount;
            //等待攻击投掷结果
            yield return DiceManager.Instance.RollDice(DiceType.attackDice, 1, 20, GetRollModifier(), info.bustPortrait, (result) => attackRoll = result);
            //根据攻击投掷结果决定技能是否造成伤害, 是否暴击, 并更新战斗日志
            if (attackRoll.criticalMiss)
            {
                UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 释放了 <color=#DAA520><u>{skill.skillName}</u></color>, 攻击投掷结果为1(大失败), 攻击失败");
                yield break;
            }
            else if (attackRoll.criticalHit)
            {
                UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 释放了 <color=#DAA520><u>{skill.skillName}</u></color>, 攻击投掷结果为20(大成功), 造成<color=red>暴击</color>");
                damageDiceCount *= 2;
            }
            else
            {
                UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 释放了 <color=#DAA520><u>{skill.skillName}</u></color>, 攻击投掷结果为{attackRoll.totalValue}");
            }
            //等待伤害投掷结果
            yield return DiceManager.Instance.RollDice(DiceType.damageDice, damageDiceCount, skill.damageDiceSides, GetRollModifier(), info.bustPortrait, (result) => damageRoll = result);

            //对effectsTarget中的每一个角色传递伤害
            foreach (Character target in effectsTarget)
            {
                bool isHit = target.TakeDamage(attackRoll, damageRoll); //传递伤害, 并记录是否受到伤害
                //技能命中造成伤害 且 技能附带Buff, 在命中角色身上添加Buff
                if (isHit && skill.applicateBuffs.Count > 0)
                {
                    foreach (Buff buff in skill.applicateBuffs)
                    {
                        BuffManager.Instance.AddBuff(target, buff);
                    }
                }
                yield return null; //每帧处理一位角色的受到技能伤害和添加Buff逻辑
            }
        }
        //如果技能不造成伤害, 则判断技能是否附带Buff, 如果附带说明为纯Buff技能, 不需要掷骰检定, 直接对effectsTarget中的每一位角色添加Buff
        else
        {
            //更新战斗日志
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 释放了 <color=#DAA520><u>{skill.skillName}</u></color>");
            //为effectsTarget中的每位角色添加buff
            if (skill.applicateBuffs.Count > 0)
            {
                foreach (Character target in effectsTarget)
                {
                    foreach (Buff buff in skill.applicateBuffs)
                    {
                        BuffManager.Instance.AddBuff(target, buff);
                    }
                    yield return null; //每帧处理一位角色的添加Buff逻辑
                }
            }
        }

        //如果角色在回合制中, 且技能附带的buff影响了角色的先攻, 就对回合制中一下轮参与者重新排序
        if (isInTurn)
        {
            foreach (Buff buff in skill.applicateBuffs)
            {
                if (buff.initiativeModifier > 0)
                {
                    yield return TurnManager.Instance.ResortTurnParticipants();
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// 处理召唤类技能的应用方法, 在summonedUnitGrids上实例化对应skill中的summonedUnitPrefab, 初始化召唤单位的角色信息, 建立召唤者和召唤单位之间的关系
    /// </summary>
    /// <param name="skill"></param>
    /// <param name="summonedUnitGrids"></param>
    /// <returns></returns>
    public IEnumerator CreatSummonedUnitOnTargetGrids(SummonSkillData skill, HashSet<GridHelper> summonedUnitGrids)
    {
        Debug.Log("CreatSummonedUnitOnTargetGrids1");
        isAttacking = true;
        SetTriggerSkillAnimation(skill); //触发技能动画
        yield return new WaitWhile(() => isAttacking);
        Debug.Log("CreatSummonedUnitOnTargetGrids2");

        List<Character> units = new List<Character>();
        //实例化召唤单位格子数量的角色预设体
        for (int i = 0; i < summonedUnitGrids.Count; i++)
        {
            Character unit = Instantiate(skill.summonedUnitPrefab).GetComponent<Character>();
            //unit.gameObject.SetActive(false);
            unit.tag = this.tag;
            units.Add(unit);
            yield return null;
        }
        //设置召唤单位的名字, 坐标(q, r), 位置, tag, summoner等信息
        int index = 0;
        foreach (GridHelper grid in summonedUnitGrids)
        {
            Character unit = units[index];
            unit.info.name += $"(SummonedBy{info.name}{summonedUnits.Count})";
            
            unit.info.q = grid.info.q;
            unit.info.r = grid.info.r;
            unit.info.heightOrder = grid.info.heightOrder;
            unit.transform.position = grid.transform.position;
            //unit.gameObject.SetActive(true);
            //unit.tag = this.tag;

            //unit.summoner = this;

            summonedUnits.Add(new SummonedUnitInfo()
            {
                character = unit,
                inheritSummonerLifespan = skill.inheritSummonerLifespan
            });
            index++;
        }
        //更新战斗日志
        UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 释放了 <color=#DAA520><u>{skill.skillName}</u></color>");
        //如果在回合制中, 则将召唤单位添加到回合制中
        if (isInTurn)
        {
            yield return TurnManager.Instance.AddParticipant(units);
        }
        //yield break;
    }


    /// <summary>
    /// 角色攻击动画结束调用
    /// </summary>
    public void OnAttackAnimationEnd()
    {
        Debug.Log("OnAttackAnimationEnd");
        isAttacking = false; //标记攻击结束
    }

    /// <summary>
    /// 角色受到伤害
    /// </summary>
    public bool TakeDamage(DiceResult attackDice, DiceResult damageDice)
    {
        //攻击投掷非大成功且投掷结果小于被攻击者AC 或者 攻击投掷大失败, 则未命中
        if((attackDice.totalValue < info.armorClass && !attackDice.criticalHit) || attackDice.criticalMiss)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 通过攻击投掷检定(<color=#FF4500>AC{info.armorClass} > {attackDice.totalValue}</color>), 未受到伤害");
            beingAttacked = false;
            return false;
        }

        info.hp -= damageDice.totalValue;
        beingAttacked = true; //标记为正在受击
        if (this == PartyManager.Instance.leader) //如果是主控, 就更新主控血条长度
        {
            UIHealthPointManager.Instance.UpdateLeaderHpBar();
        }
        else
        {
            UIHealthPointManager.Instance.ShowTargetHpPanel(this); //更新观察面板血条
        }
        if (isInTurn) //如果在回合制中, 就更新血条长度
        {
            UITurnManager.Instance.UpdateTurnPortraitHpBarFill(this);
        }

        //更新战斗日志
        UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> 未通过攻击投掷检定(<color=#FF4500>AC{info.armorClass} <= {attackDice.totalValue}</color>), 受到 <color=#FF4500><u>{damageDice.totalValue}点伤害</u></color>");

        if (info.hp > 0)
        {
            animator.SetTrigger("takeDamage");
        }
        else
        {
            StartCoroutine(Die());
        }
        return true;
    }

    /// <summary>
    /// 角色死亡
    /// </summary>
    public IEnumerator Die()
    {
        info.hp = 0;
        animator.SetBool("dieState", true);
        animator.SetTrigger("die"); //死亡动画
        this.GetComponent<BoxCollider>().enabled = false; //失活角色身上的碰撞器

        //如果是玩家角色, 就离队
        if (CompareTag("Player"))
        {
            PartyManager.Instance.RemoveCharacter(this);
        }
        
        //如果在回合制中, 就更新回合制UI
        if (isInTurn)
        {
            isInTurn = false; //标记退出回合制
            TurnManager.Instance.RemoveParticipant(this); //如果角色在回合制中, 就将其从回合中移除
        }

        //更新召唤者的信息
        //if (summoner != null)
        //{
        //    summoner.summonedUnits.Remove(summoner.summonedUnits.Where(info => info.character == this).Last());
        //}
        //更新依赖该角色的召唤物的信息
        if (summonedUnits.Count > 0)
        {
            yield return new WaitWhile(() => UITurnManager.Instance.relayouting);
            foreach (SummonedUnitInfo unitInfo in summonedUnits.ToList())
            {
                if (unitInfo.inheritSummonerLifespan && unitInfo.character.info.hp > 0)
                {
                    yield return unitInfo.character.Die(); //继承该角色生命周期的召唤物同时死亡
                    summonedUnits.Remove(unitInfo);
                }
            }
        }

        //更新战斗日志
        UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> <color=#FF4500><u>死亡</u></color>");
    }

    /// <summary>
    /// 角色受击或死亡动画结束后调用
    /// </summary>
    public void OnBeAttackedAnimationEnd()
    {
        beingAttacked = false; //标记受击结束
    }

    /// <summary>
    /// 获得角色投掷骰子时的调整值
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, int> GetRollModifier()
    {
        Dictionary<string, int> modifiers = new Dictionary<string, int>();

        //加入攻击检定属性调整值 和 buff属性加值
        if(info.attackType == AttackType.strength)
        {
            modifiers.Add("力量调整值", (info.strength - 8) / 2);
            foreach (Buff buff in buffs)
            {
                if(buff.strengthModifier > 0)
                {
                    modifiers.Add(buff.buffName, buff.strengthModifier);
                }
            }
        }
        else if(info.attackType == AttackType.finesse)
        {
            modifiers.Add("敏捷调整值", (info.finesse - 8) / 2);
            foreach (Buff buff in buffs)
            {
                if (buff.finesseModifier > 0)
                {
                    modifiers.Add(buff.buffName, buff.finesseModifier);
                }
            }
        }
        else if(info.attackType == AttackType.intelligence)
        {
            modifiers.Add("智力调整值", (info.intelligence - 8) / 2);
            foreach (Buff buff in buffs)
            {
                if (buff.intelligenceModifier > 0)
                {
                    modifiers.Add(buff.buffName, buff.intelligenceModifier);
                }
            }
        }

        foreach (Buff buff in buffs)
        {
            if (buff.diceCountModifier > 0)
            {
                modifiers.Add(string.Format("{0}({1}/{2})", buff.buffName, buff.diceCountModifier, buff.diceSidesModifier), buff.diceCountModifier * Random.Range(1, buff.diceSidesModifier + 1));
            }
        }

        if (info.proficiency > 0)
        {
            modifiers.Add("武器熟练加值", info.proficiency);
        }

        return modifiers;
    }

    /// <summary>
    /// 根据当前角色武器类型, 触发攻击动画
    /// </summary>
    private void SetTriggerAttackAnimation()
    {
        //无武器 或 持剑类武器时
        if (equipment.weaponEquipment == null || equipment.weaponEquipment.weaponType == WeaponType.Sword)
        {
            animator.SetTrigger("swordAttack");
        }
        //持弓箭武器时
        else if (equipment.weaponEquipment.weaponType == WeaponType.Bow)
        {
            animator.SetTrigger("bowAttack");
        }
        //持法杖武器时
        else if (equipment.weaponEquipment.weaponType == WeaponType.Staff)
        {
            animator.SetTrigger("staffAttack");
        }
    }

    /// <summary>
    /// 根据施法技能的触发攻击动画
    /// </summary>
    /// <param name="triggerStr"></param>
    private void SetTriggerSkillAnimation(SkillBaseData skill)
    {
        if(string.IsNullOrEmpty(skill.animationTriggerStr) || skill.animationTriggerStr == "")
        {
            return;
        }
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger && param.name == skill.animationTriggerStr)
            {
                animator.SetTrigger(skill.animationTriggerStr);
                return;
            }
            //Debug.Log(param.name);
        }
    }


    /// <summary>
    /// 将装备item装备到角色对应槽位上, 并更新角色面板UI
    /// </summary>
    /// <param name="item"></param>
    public void Equip(EquipmentItemData item)
    {
        if(item.type == EquipmentType.Helmet)
        {
            //如果对应装备槽位不为空, 且插槽内的装备不等于要装备的装备(避免初始化的时候先卸下装备), 就先卸下装备
            if (equipment.helmetEquipment != null && equipment.helmetEquipment != item)
            {
                Unequip(equipment.helmetEquipment);
            }
            //将相应装备槽位设置为item
            equipment.helmetEquipment = item;
        }
        else if (item.type == EquipmentType.Chest)
        {
            if (equipment.chestEquipment != null && equipment.chestEquipment != item)
            {
                Unequip(equipment.chestEquipment);
            }
            equipment.chestEquipment = item;
        }
        else if (item.type == EquipmentType.Glowes)
        {
            if (equipment.glowesEquipment != null && equipment.glowesEquipment != item)
            {
                Unequip(equipment.glowesEquipment);
            }
            equipment.glowesEquipment = item;
        }
        else if (item.type == EquipmentType.Belt)
        {
            if (equipment.beltEquipment != null && equipment.beltEquipment != item)
            {
                Unequip(equipment.beltEquipment);
            }
            equipment.beltEquipment = item;
        }
        else if (item.type == EquipmentType.Boots)
        {
            if (equipment.bootsEquipment != null && equipment.bootsEquipment != item)
            {
                Unequip(equipment.bootsEquipment);
            }
            equipment.bootsEquipment = item;
        }
        else if (item.type == EquipmentType.Amulet)
        {
            if (equipment.amuletEquipment != null && equipment.amuletEquipment != item)
            {
                Unequip(equipment.amuletEquipment);
            }
            equipment.amuletEquipment = item;
        }
        else if (item.type == EquipmentType.Ring)
        {
            if (equipment.leftRingEquipment != null && equipment.leftRingEquipment != item)
            {
                if(equipment.rightRingEquipment != null && equipment.rightRingEquipment != item)
                {
                    Unequip(equipment.leftRingEquipment);
                    equipment.leftRingEquipment = item;
                }
                else
                {
                    equipment.rightRingEquipment = item;
                }
            }
            else
            {
                equipment.leftRingEquipment = item;
            }
        }
        else if (item.type == EquipmentType.Leggings)
        {
            if (equipment.leggingsEquipment != null && equipment.leggingsEquipment != item)
            {
                Unequip(equipment.leggingsEquipment);
            }
            equipment.leggingsEquipment = item;
        }
        else if (item.type == EquipmentType.Weapon)
        {
            if (equipment.weaponEquipment != null && equipment.weaponEquipment != item)
            {
                Unequip(equipment.weaponEquipment);
            }
            equipment.weaponEquipment = item;
        }

        //将装备属性加值 加到角色属性上
        info.maxHp += item.maxHpModifier;
        info.hp += item.maxHpModifier;
        info.attackDistance += item.attackDistanceModifier;
        info.strength += item.strengthModifier;
        info.finesse += item.finesseModifier;
        info.intelligence += item.intelligenceModifier;
        info.charisma += item.charismaModifier;
        info.wits += item.witsModifier;
        info.damageDiceCount += item.damageDiceCountModifier;
        info.damageDiceSides += item.damageDiceSidesModifier;
        info.armorClass += item.armorClassModifier;
        info.initiative += item.initiativeModifier;
        info.walkSpeed += item.speedModifier;
        info.runSpeed += item.speedModifier;

        //如果是在回合制中更新装备 并且装备附带的先攻调整值不为0, 就更新回合制头像列表UI
        if (isInTurn && item.initiativeModifier != 0)
        {
            StartCoroutine(TurnManager.Instance.ResortTurnParticipants());
        }

        //如果是武器, 就将武器模型实例化到人物模型手部
        if (item.type == EquipmentType.Weapon)
        {
            weaponModel = Instantiate(item.model, weaponParent);
            if (item.weaponType == WeaponType.Sword)
            {
                weaponModel.transform.localPosition = new Vector3(0.1f, 0.45f, 0);
                weaponModel.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            }
            else if (item.weaponType == WeaponType.Bow)
            {
                weaponModel.transform.localPosition = new Vector3(0.1f, 0.1f, 0);
                weaponModel.transform.localScale = new Vector3(2f, 2f, 2f);
            }
            else if (item.weaponType == WeaponType.Staff)
            {
                weaponModel.transform.localPosition = new Vector3(0, 0.2f, 0);
                weaponModel.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            }
        }

        //如果装备有附加Buff, 就添加到角色身上
        if (item.equipmentItemBuffs.Count > 0)
        {
            foreach(Buff buff in item.equipmentItemBuffs)
            {
                BuffManager.Instance.AddBuff(this, buff);
            }
        }
        
        //如果有装备有附加技能, 就添加到角色身上
        if (item.equipmentItemSkills.Count > 0)
        {
            foreach (SkillBaseData skill in item.equipmentItemSkills)
            {
                SkillManager.Instance.LearnSkill(this, skill);
            }
        }

        //更新UI(CharacterInfo数据信息UI 和 插槽图片)
        if (PartyManager.Instance != null && this == PartyManager.Instance.leader)
        {
            UIPartyCharacterManager.Instance.UpdateCharacterPanel(this);
            UIHealthPointManager.Instance.UpdateLeaderHpBar(); //更新主控血条
        }
    }

    /// <summary>
    /// 将装备item从角色对应槽位中移除, 并更新角色面板UI
    /// </summary>
    /// <param name="item"></param>
    /// <param name="item"></param>
    public void Unequip(EquipmentItemData item)
    {
        //如果对应装备槽位不为空, 且插槽内的装备不等于要装备的装备(避免初始化的时候先卸下装备), 就先卸下装备
        if (item.type == EquipmentType.Helmet && item == equipment.helmetEquipment)
        {
            equipment.helmetEquipment = null;
        }
        else if (item.type == EquipmentType.Chest && item == equipment.chestEquipment)
        {
            equipment.chestEquipment = null;
        }
        else if (item.type == EquipmentType.Glowes && item == equipment.glowesEquipment)
        {
            equipment.glowesEquipment = null;
        }
        else if (item.type == EquipmentType.Belt && item == equipment.beltEquipment)
        {
            equipment.beltEquipment = null;
        }
        else if (item.type == EquipmentType.Boots && item == equipment.bootsEquipment)
        {
            equipment.bootsEquipment = null;
        }
        else if (item.type == EquipmentType.Amulet && item == equipment.amuletEquipment)
        {
            equipment.amuletEquipment = null;
        }
        else if (item.type == EquipmentType.Ring)
        {
            if (equipment.leftRingEquipment == item)
            {
                equipment.leftRingEquipment = null;
            }
            else if (equipment.rightRingEquipment == item)
            {
                equipment.rightRingEquipment = null;
            }
        }
        else if (item.type == EquipmentType.Leggings && item == equipment.leggingsEquipment)
        {
            equipment.leggingsEquipment = null;
        }
        else if (item.type == EquipmentType.Weapon && item == equipment.weaponEquipment)
        {
            equipment.weaponEquipment = null;
        }

        //将角色属性将去装备属性加值
        info.maxHp -= item.maxHpModifier;
        info.hp -= item.maxHpModifier;
        info.attackDistance -= item.attackDistanceModifier;
        info.strength -= item.strengthModifier;
        info.finesse -= item.finesseModifier;
        info.intelligence -= item.intelligenceModifier;
        info.charisma -= item.charismaModifier;
        info.wits -= item.witsModifier;
        info.damageDiceCount -= item.damageDiceCountModifier;
        info.damageDiceSides -= item.damageDiceSidesModifier;
        info.armorClass -= item.armorClassModifier;
        info.initiative -= item.initiativeModifier;
        info.walkSpeed -= item.speedModifier;
        info.runSpeed -= item.speedModifier;

        //如果是在回合制中更新装备 并且装备附带的先攻调整值不为0, 就更新回合制头像列表UI
        if (isInTurn && item.initiativeModifier != 0)
        {
            StartCoroutine(TurnManager.Instance.ResortTurnParticipants());
        }

        //如果装备有附加Buff, 就从角色身上移除
        if (item.equipmentItemBuffs.Count > 0)
        {
            foreach (Buff buff in item.equipmentItemBuffs)
            {
                BuffManager.Instance.RemoveBuff(this, buff);
            }
        }

        //如果有装备有附加技能, 就从角色身上移除
        if (item.equipmentItemSkills.Count > 0)
        {
            foreach (SkillBaseData skill in item.equipmentItemSkills)
            {
                SkillManager.Instance.RemoveSkill(this, skill);
            }
        }

        //更新UI(CharacterInfo数据信息UI 和 插槽图片)
        if (this == PartyManager.Instance.leader)
        {
            UIPartyCharacterManager.Instance.UpdateCharacterPanel(this);
            UIHealthPointManager.Instance.UpdateLeaderHpBar(); //更新主控血条
        }

        //如果是武器, 就销毁人物模型手部的武器模型
        if (item.type == EquipmentType.Weapon)
        {
            if (weaponModel != null)
            {
                Destroy(weaponModel);
            }
        }

        //将卸下装备添加到背包中
        bagItems.Add(item);
        UIPartyInventoryManager.Instance.AddItemToBag(this, item);
    }

    /// <summary>
    /// 将装备item从角色对应槽位中移除, 并且不返回给背包, 即丢弃
    /// </summary>
    /// <param name="item"></param>
    /// <param name="item"></param>
    public void DropEquipment(EquipmentItemData item)
    {
        //如果对应装备槽位不为空, 且插槽内的装备不等于要装备的装备(避免初始化的时候先卸下装备), 就先卸下装备
        if (item.type == EquipmentType.Helmet && item == equipment.helmetEquipment)
        {
            equipment.helmetEquipment = null;
        }
        else if (item.type == EquipmentType.Chest && item == equipment.chestEquipment)
        {
            equipment.chestEquipment = null;
        }
        else if (item.type == EquipmentType.Glowes && item == equipment.glowesEquipment)
        {
            equipment.glowesEquipment = null;
        }
        else if (item.type == EquipmentType.Belt && item == equipment.beltEquipment)
        {
            equipment.beltEquipment = null;
        }
        else if (item.type == EquipmentType.Boots && item == equipment.bootsEquipment)
        {
            equipment.bootsEquipment = null;
        }
        else if (item.type == EquipmentType.Amulet && item == equipment.amuletEquipment)
        {
            equipment.amuletEquipment = null;
        }
        else if (item.type == EquipmentType.Ring)
        {
            if (equipment.leftRingEquipment == item)
            {
                equipment.leftRingEquipment = null;
            }
            else if (equipment.rightRingEquipment == item)
            {
                equipment.rightRingEquipment = null;
            }
        }
        else if (item.type == EquipmentType.Leggings && item == equipment.leggingsEquipment)
        {
            equipment.leggingsEquipment = null;
        }
        else if (item.type == EquipmentType.Weapon && item == equipment.weaponEquipment)
        {
            equipment.weaponEquipment = null;
        }

        //将角色属性将去装备属性加值
        info.maxHp -= item.maxHpModifier;
        info.hp -= item.maxHpModifier;
        info.attackDistance -= item.attackDistanceModifier;
        info.strength -= item.strengthModifier;
        info.finesse -= item.finesseModifier;
        info.intelligence -= item.intelligenceModifier;
        info.charisma -= item.charismaModifier;
        info.wits -= item.witsModifier;
        info.damageDiceCount -= item.damageDiceCountModifier;
        info.damageDiceSides -= item.damageDiceSidesModifier;
        info.armorClass -= item.armorClassModifier;
        info.initiative -= item.initiativeModifier;
        info.walkSpeed -= item.speedModifier;
        info.runSpeed -= item.speedModifier;

        //如果是在回合制中更新装备 并且装备附带的先攻调整值不为0, 就更新回合制头像列表UI
        if (isInTurn && item.initiativeModifier != 0)
        {
            StartCoroutine(TurnManager.Instance.ResortTurnParticipants());
        }

        //如果装备有附加Buff, 就从角色身上移除
        if (item.equipmentItemBuffs.Count > 0)
        {
            foreach (Buff buff in item.equipmentItemBuffs)
            {
                BuffManager.Instance.RemoveBuff(this, buff);
            }
        }

        //如果有装备有附加技能, 就从角色身上移除
        if (item.equipmentItemSkills.Count > 0)
        {
            foreach (SkillBaseData skill in item.equipmentItemSkills)
            {
                SkillManager.Instance.RemoveSkill(this, skill);
            }
        }

        //如果是武器, 就销毁人物模型手部的武器模型
        if (item.type == EquipmentType.Weapon)
        {
            if (weaponModel != null)
            {
                Destroy(weaponModel);
            }
        }

        //更新UI(CharacterInfo数据信息UI 和 插槽图片)
        if (this == PartyManager.Instance.leader)
        {
            UIPartyCharacterManager.Instance.UpdateCharacterPanel(this);
            UIHealthPointManager.Instance.UpdateLeaderHpBar(); //更新主控血条
        }
    }

    /// <summary>
    /// 使用消耗品item, 永久 或 在一定回合内 改变角色属性, 并更新角色面板UI
    /// </summary>
    /// <param name="item"></param>
    public void Consume(ConsumableItemData item)
    {
        if (this.isInTurn)
        {
            info.actionPoint--; //回合中使用消耗品 花费1行动点数
        }
        BuffManager.Instance.AddBuff(this, item.comsumableItemBuff);
    }

    /// <summary>
    /// 根据sortType对bagItems进行排序
    /// </summary>
    /// <param name="sortType"></param>
    public void SortBagItems(string sortType)
    {
        if(sortType == "type")
        {
            bagItems.Sort(new ItemTypeComparer());
        }
        else if(sortType == "value")
        {
            bagItems.Sort(new ItemValueComparer());
        }
        else if(sortType == "weight")
        {
            bagItems.Sort(new ItemWeightComparer());
        }
    }


    /// <summary>
    /// 鼠标悬停在角色身上时, 显示和更新观察目标对象的血量面板
    /// </summary>
    private void OnMouseEnter()
    {
        if(this != PartyManager.Instance.leader)
        {
            UIHealthPointManager.Instance.ShowTargetHpPanel(this);
        }
    }

    /// <summary>
    /// 当鼠标从角色身上移开时, 隐藏观察目标对象的血量面板
    /// </summary>
    private void OnMouseExit()
    {
        UIHealthPointManager.Instance.HideTargetHpPanel();
    }

    /// <summary>
    /// 得到网格start和end之间的距离
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    private float getDistance(GridHelper start, GridHelper end)
    {
        int dq = Mathf.Abs(start.info.q - end.info.q);
        int dr = Mathf.Abs(start.info.r - end.info.r);
        int ds = Mathf.Abs(start.info.s - end.info.s);
        return (dq + dr + ds) / 2.0f;
    }
}