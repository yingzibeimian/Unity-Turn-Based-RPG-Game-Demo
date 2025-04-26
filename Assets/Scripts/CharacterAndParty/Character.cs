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
    //��������, �ȹ��ߵ���ǰ
    public int Compare(Character a, Character b) => b.info.initiative.CompareTo(a.info.initiative);
}


/// <summary>
/// ��ɫ��Ϣ
/// </summary>
[System.Serializable]
public class CharacterInfo
{
    public string name; //����
    public int level; //�ȼ�
    public int maxHp = 5; //�������ֵ
    public int hp = 5; //��ǰ����ֵ
    public int attackDistance = 1; //��������

    public int strength = 8; //����
    public int finesse = 8; //����
    public int intelligence = 8; //����
    public int constitution = 8; //����
    public int charisma = 8;
    public int wits = 8; //�ǻ�

    public int damageDiceCount = 1; //�˺����Ӹ���
    public int damageDiceSides = 3; //�˺���������

    public int armorClass; //�����ȼ�AC

    public AttackType attackType; //������Ӱ������
    public int proficiency; //����������ֵ

    public int initiative; //�ȹ�
    public float actionPoint; //�غ��ж�����

    public float walkSpeed; //�����ٶ�
    public float runSpeed; //�ܲ��ٶ�
    public float rotateSpeed; //ת���ٶ�

    //public bool isAI; //�����ж��Ƿ���AI����

    //����������Ϣ
    public int q;
    public int r;
    public int s => -q - r;
    public int heightOrder;

    public Sprite portrait; //ͷ��
    public Sprite bustPortrait; //������
}

/// <summary>
/// ��ɫװ��
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
/// �ٻ�����Ϣ
/// </summary>
public class SummonedUnitInfo
{
    public Character character; //�ٻ���λ�Ľ�ɫ�ű�
    public bool inheritSummonerLifespan = false; //�ٻ���λ�Ƿ������ٻ��ߴ��� 
}


public class Character : MonoBehaviour
{
    public CharacterData characterInfo; //��ɫScriptableObject����
    public CharacterInfo info; //��ɫ��Ϣ

    public CharacterEquipment equipment;

    public List<ItemBaseData> bagItems; //������Ʒ��Ϣ
    public List<SkillBaseData> characterSkills = new List<SkillBaseData>(); //��ɫӵ�м���
    public HashSet<Buff> buffs = new HashSet<Buff>(); //��ɫ���ϵ�buff��Ϣ

    public AStarPathfinder pathfinder; //��ɫ���Ϲ��ص�A*Ѱ·�ű�
    public Animator animator;
    public GridHelper nowGrid => GridMap.Instance.SearchGrid(info.q, info.r, info.heightOrder); //��ɫ��ǰ��������

    [HideInInspector]
    public bool characterInitialized = false; //��¼��ɫ�Ƿ��ʼ�����
    [HideInInspector]
    public bool isMoving = false; //���ڱ�ǽ�ɫ�Ƿ������ƶ�
    [HideInInspector]
    public bool isAttacking = false; //���ڱ�ǽ�ɫ�Ƿ����ڹ���
    [HideInInspector]
    public bool beingAttacked = false; //���ڱ�ǽ�ɫ�Ƿ������ܻ�
    [HideInInspector]
    public bool isSkillTargeting = false; //���ڱ�ǽ�ɫ�Ƿ�����Ԥʩ���׶�ѡ����Ŀ����
    [HideInInspector]
    public bool isInTurn = false; //���ڱ�ǽ�ɫ�Ƿ����ڻغ�����
    public bool isAI = false; //���ڱ�ǽ�ɫ�ж��Ƿ���AI�ű�����

    [HideInInspector]
    public List<SummonedUnitInfo> summonedUnits = new List<SummonedUnitInfo>(); //�ٻ�����Ϣ
    [HideInInspector]
    //public Character summoner; //�ٻ���

    private List<Character> group = new List<Character>(); //���ڵ�ǰ��ɫ��Ϊ����ʱ, ��¼����
    private Coroutine getNearGridsCoroutine; //���ڴ洢Э��, ��ֹ����ִ�ж������Ѱ·����
    private Coroutine followToCoroutine; //���ڴ洢Э��, ��ֹ����ִ�ж������Ѱ·����
    private float moveWeight; //��ɫ�ƶ��������Ȩ��
    private float moveSpeed; //��ɫ�ƶ��ٶ�

    private string attackerColor = "#F0F8FF"; //Ĭ��ս����־��ɫ

    public Transform weaponParent; //װ������������
    private GameObject weaponModel; //����ģ��

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

        //��ʼ����ɫ����
        InitializeCharacterInfo();

        //������������е�ScriptableObject��Ʒ����
        for (int i = 0; i < bagItems.Count; i++)
        {
            ItemBaseData original = bagItems[i];
            ItemBaseData copy = Instantiate(original);
            bagItems[i] = copy;
        }
        //��������б������е�ScriptableObject��������
        for (int i = 0; i < characterSkills.Count; i++)
        {
            SkillBaseData original = characterSkills[i];
            SkillBaseData copy = Instantiate(original);
            characterSkills[i] = copy;
        }

        attackerColor = CompareTag("Player") ? "#F0F8FF" : "#B22222"; //������Player����Enemy��������ս����־����ʾ��ɫ��

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
        //��SO���ݸ���info
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

        //����װ��
        if (equipment.helmetEquipment != null)
        {
            equipment.helmetEquipment = Instantiate(equipment.helmetEquipment); //���װ������
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
    /// ����ǰ�ű������ص������ؽ�ɫ�ƶ��� targetGrid
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
                    this.group.Add(c); //�����ؽ�ɫ�����ֶӵĶ��Ѽ���this.group��
                }
            }
        }
        if(pathfinder != null)
        {
            //��̬�����ƶ��ٶ�, �ƶ�����С��2ʱ, �ٶ�=walkSpeed; ����6ʱ, �ٶ�=runSpeed; 2��6ʱ, ��������֮��
            moveWeight = Mathf.Clamp(-.5f + getDistance(nowGrid, targetGrid) / 4.0f, 0, 1); //�������Ȩ��
            moveSpeed = Mathf.Lerp(info.walkSpeed, info.runSpeed, moveWeight);
            //pathfinder.StartPathfinding(nowGrid, targetGrid, moveSpeed, info.rotateSpeed, moveWeight, FollowTo);

            int followerCount = this.group.Count + summonedUnits.Count; //�����ƶ��Ľ�ɫ����
            if (this.group != null)
            {
                foreach (Character c in this.group)
                {
                    followerCount += c.summonedUnits.Count;
                }
            }

            //�ƶ����ؽ�ɫ
            CoroutineManager.Instance.StopGroup(info.name); //ֹͣ��ǰ��ɫ���ƶ�Э�̶���
            animator.SetBool("isMoving", false);
            CoroutineManager.Instance.AddTaskToGroup(pathfinder.FindPath(nowGrid, targetGrid), info.name); //Ѱ·
            CoroutineManager.Instance.AddTaskToGroup(pathfinder.GetNearGrids(followerCount, StartFollowTo), info.name); //Ϊ����Ѱ�Ҹ�������
            CoroutineManager.Instance.AddTaskToGroup(pathfinder.CharacterMove(moveSpeed, info.rotateSpeed, moveWeight), info.name); //�ƶ�
            CoroutineManager.Instance.StartGroup(info.name); //������ǰ��ɫ���ƶ�Э�̶���
        }
    }

    /// <summary>
    /// �������Ѹ���
    /// </summary>
    /// <param name="nearGrids"></param>
    public void StartFollowTo(List<GridHelper> nearGrids)
    {
        if(group.Count + summonedUnits.Count == 0)
        {
            return; //û�ж��Ѻ��ٻ���
        }
        if (getNearGridsCoroutine != null)
        {
            StopCoroutine(followToCoroutine); //���group�еĶ�������ͨ��Э�̷���Ѱ�Ҹ�������, ��ֹͣ��
        }
        followToCoroutine = StartCoroutine(FollowTo(nearGrids));
    }

    /// <summary>
    /// ��group�еĶ����ƶ���path·����󿿽�targetGrid������
    /// </summary>
    /// <param name="nearGrids"></param>
    /// <returns></returns>
    public IEnumerator FollowTo(List<GridHelper> nearGrids)
    {
        //�����ɫ�б�
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

        for (int i = 0; i < nearGrids.Count; i++) //�����õ�������·���յ�ĸ�������
        {
            Character follower = null;
            if (followerGroup.Count > i)
            {
                follower = followerGroup[i];
            }
            if(follower != null && follower.pathfinder != null && nearGrids[i] != null)
            {
                yield return new WaitForSeconds(0.5f); //Ϊ�˲�ʹ�����н�ӵ��, ��Ա����һ��ʱ������
                CoroutineManager.Instance.StopGroup(follower.info.name); //ֹͣ���ڽ��е��ƶ�Э�̶���
                CoroutineManager.Instance.AddTaskToGroup(follower.pathfinder.FindPath(follower.nowGrid, nearGrids[i]), follower.info.name); //Ѱ·
                CoroutineManager.Instance.AddTaskToGroup(follower.pathfinder.CharacterMove(moveSpeed - (i + 1) * 0.05f, follower.info.rotateSpeed, moveWeight), follower.info.name); //�ƶ�
                CoroutineManager.Instance.StartGroup(follower.info.name); //�������ѵ��ƶ�Э�̶���
            }
            if(i >= followerGroup.Count)
            {
                break;
            }
        }
        this.group.Clear();
    }

    /// <summary>
    /// ��ɫ����
    /// </summary>
    /// <param name="attackTarget"></param>
    /// <returns></returns>
    public IEnumerator Attack(Character attackTarget)
    {
        if(info.actionPoint < 2 || info.hp <= 0) //�жϳ�ʼ�ж������Ƿ��ܹ��� �����Ƿ�������
        {
            Debug.Log($"Cant Attack, actionPoint{info.actionPoint}");
            yield break;
        }
        isAttacking = true; //���Ϊ���ڹ�����
        //���½�ɫ����
        Vector3 direction = (attackTarget.transform.position - this.transform.position).normalized;
        Quaternion startRotation = this.transform.rotation; //��ǰ����
        Quaternion endRotation = Quaternion.LookRotation(direction); //�յ㳯��
        float rotateWeight = 0;
        while (Quaternion.Angle(this.transform.rotation, endRotation) > 0.05f)
        {
            rotateWeight += Time.deltaTime * info.rotateSpeed;
            this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
            yield return null;
        }
        transform.rotation = endRotation;
        //������������ ������������
        SetTriggerAttackAnimation();

        Debug.Log($"{info.name} Attack {attackTarget.info.name}");
        yield return new WaitWhile(() => isAttacking);

        //����Ͷ��
        DiceResult attackRoll = null;
        //�˺�Ͷ��
        DiceResult damageRoll = null;
        int damageDiceCount = info.damageDiceCount;
        //�ȴ�����Ͷ�����
        yield return DiceManager.Instance.RollDice(DiceType.attackDice, 1, 20, GetRollModifier(), info.bustPortrait, (result) => attackRoll = result);
        //���ݹ���Ͷ����������Ƿ�����˺�, �Ƿ񱩻�, ������ս����־
        if (attackRoll.criticalMiss)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>����</u></color>, ����Ͷ�����Ϊ1(��ʧ��), ����ʧ��");
            if (isInTurn)
            {
                info.actionPoint -= 2; //�����ж�����
                UITurnManager.Instance.UpdateActionPointBalls(info.actionPoint, 0);
            }
            yield break;
        }
        else if (attackRoll.criticalHit)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>����</u></color>, ����Ͷ�����Ϊ20(��ɹ�), ���<color=red>����</color>");
            damageDiceCount *= 2;
        }
        else
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>����</u></color>, ����Ͷ�����Ϊ{attackRoll.totalValue}");
        }
        //�ȴ��˺�Ͷ�����
        yield return DiceManager.Instance.RollDice(DiceType.damageDice, damageDiceCount, info.damageDiceSides, GetRollModifier(), info.bustPortrait, (result) => damageRoll = result);

        //�����˺�
        attackTarget.TakeDamage(attackRoll, damageRoll); //���������ܵ��˺�
        
        yield return new WaitWhile(() => attackTarget.beingAttacked);
        if (isInTurn)
        {
            info.actionPoint -= 2; //�����ж�����
            UITurnManager.Instance.UpdateActionPointBalls(info.actionPoint, 0);
        }
    }

    /// <summary>
    /// �������
    /// </summary>
    /// <param name="attackTarget"></param>
    /// <returns></returns>
    public IEnumerator OpportunityAttack(Character attackTarget)
    {
        //Ŀ������Ѿ�����, ��ֱ�ӷ���
        if (attackTarget.info.hp <= 0)
        {
            yield break;
        }

        isAttacking = true; //���Ϊ���ڹ�����
        //���½�ɫ����
        Vector3 direction = (attackTarget.transform.position - this.transform.position).normalized;
        Quaternion startRotation = this.transform.rotation; //��ǰ����
        Quaternion endRotation = Quaternion.LookRotation(direction); //�յ㳯��
        float rotateWeight = 0;
        while (Quaternion.Angle(this.transform.rotation, endRotation) > 0.05f)
        {
            rotateWeight += Time.deltaTime * info.rotateSpeed;
            this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
            yield return null;
        }
        transform.rotation = endRotation;
        //������������
        SetTriggerAttackAnimation();

        Debug.Log($"{info.name} OpportunityAttack {attackTarget.info.name}");
        yield return new WaitWhile(() => isAttacking);

        //����ս����־
        //UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>�������</u></color>");

        //attackTarget.TakeDamage(10, 1); //���������ܵ��˺�

        //����Ͷ��
        DiceResult attackRoll = null;
        //�˺�Ͷ��
        DiceResult damageRoll = null;
        int damageDiceCount = info.damageDiceCount;
        //�ȴ�����Ͷ�����
        yield return DiceManager.Instance.RollDice(DiceType.attackDice, 1, 20, GetRollModifier(), info.bustPortrait, (result) => attackRoll = result);
        //���ݹ���Ͷ����������Ƿ�����˺�, �Ƿ񱩻�, ������ս����־
        if (attackRoll.criticalMiss)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>����</u></color>, ����Ͷ�����Ϊ1(��ʧ��), ����ʧ��");
            yield break;
        }
        else if (attackRoll.criticalHit)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>����</u></color>, ����Ͷ�����Ϊ20(��ɹ�), ���<color=red>����</color>");
            damageDiceCount *= 2;
        }
        else
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>����</u></color>, ����Ͷ�����Ϊ{attackRoll.totalValue}");
        }
        //�ȴ��˺�Ͷ�����
        yield return DiceManager.Instance.RollDice(DiceType.damageDice, damageDiceCount, info.damageDiceSides, GetRollModifier(), info.bustPortrait, (result) => damageRoll = result);

        //�����˺�
        attackTarget.TakeDamage(attackRoll, damageRoll); //���������ܵ��˺�

        yield return new WaitWhile(() => attackTarget.beingAttacked);
    }

    /// <summary>
    /// ��ɫ���������ʱ�ķ�������
    /// </summary>
    /// <param name="counterTarget"></param>
    /// <returns></returns>
    public IEnumerator CounterAttack(Character counterTarget)
    {
        if (info.actionPoint < 2 || info.hp <= 0 || counterTarget.info.hp <= 0) //�жϳ�ʼ�ж������Ƿ��ܹ���
        {
            Debug.Log($"Cant CountAttack, actionPoint{info.actionPoint}");
            yield break;
        }
        isAttacking = true; //���Ϊ���ڹ�����
        //���½�ɫ����
        Vector3 direction = (counterTarget.transform.position - this.transform.position).normalized;
        Quaternion startRotation = this.transform.rotation; //��ǰ����
        Quaternion endRotation = Quaternion.LookRotation(direction); //�յ㳯��
        float rotateWeight = 0;
        while (Quaternion.Angle(this.transform.rotation, endRotation) > 0.05f)
        {
            rotateWeight += Time.deltaTime * info.rotateSpeed;
            this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
            yield return null;
        }
        transform.rotation = endRotation;
        //������������
        SetTriggerAttackAnimation();

        Debug.Log($"{info.name} CounterAttack {counterTarget.info.name}");
        yield return new WaitWhile(() => isAttacking);

        //����ս����־
        //UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>����</u></color>");

        //counterTarget.TakeDamage(10, 1); //���������ܵ��˺�

        //����Ͷ��
        DiceResult attackRoll = null;
        //�˺�Ͷ��
        DiceResult damageRoll = null;
        int damageDiceCount = info.damageDiceCount;
        //�ȴ�����Ͷ�����
        yield return DiceManager.Instance.RollDice(DiceType.attackDice, 1, 20, GetRollModifier(), info.bustPortrait, (result) => attackRoll = result);
        //���ݹ���Ͷ����������Ƿ�����˺�, �Ƿ񱩻�, ������ս����־
        if (attackRoll.criticalMiss)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>����</u></color>, ����Ͷ�����Ϊ1(��ʧ��), ����ʧ��");
            if (isInTurn)
            {
                info.actionPoint -= 2; //�����ж�����
                UITurnManager.Instance.UpdateActionPointBalls(info.actionPoint, 0);
            }
            yield break;
        }
        else if (attackRoll.criticalHit)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>����</u></color>, ����Ͷ�����Ϊ20(��ɹ�), ���<color=red>����</color>");
            damageDiceCount *= 2;
        }
        else
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ���� <color=#DAA520><u>����</u></color>, ����Ͷ�����Ϊ{attackRoll.totalValue}");
        }
        //�ȴ��˺�Ͷ�����
        yield return DiceManager.Instance.RollDice(DiceType.damageDice, damageDiceCount, info.damageDiceSides, GetRollModifier(), info.bustPortrait, (result) => damageRoll = result);

        //�����˺�
        counterTarget.TakeDamage(attackRoll, damageRoll); //���������ܵ��˺�

        yield return new WaitWhile(() => counterTarget.beingAttacked);
        info.actionPoint -= 2; //�����ж�����
    }

    /// <summary>
    /// ����Ч���༼�ܵ�Ч��Ӧ�÷���, �����������˺�, ����ݼ���skill���м��ܹ������˺�Ͷ��, �����˺������Buff; ������ܲ�����˺�, ��֮�����Buff
    /// </summary>
    /// <param name="skill"></param>
    /// <param name="effectsTarget"></param>
    /// <returns></returns>
    public IEnumerator AppplySkillEffectsOnTarget(EffectApplicationSkillData skill, HashSet<Character> effectsTarget)
    {
        Debug.Log("AppplySkillEffectsOnTarget1");
        if (skill.damageDiceCount > 0 && !string.IsNullOrEmpty(skill.animationTriggerStr) && skill.animationTriggerStr != "")
        {
            isAttacking = true; //���Ϊ���ڹ�����
        }
        SetTriggerSkillAnimation(skill); //�������ܶ���
        yield return new WaitWhile(() => isAttacking);
        Debug.Log("AppplySkillEffectsOnTarget2");

        //�����������˺�, ����ݼ���skill���м��ܹ������˺�Ͷ��
        if (skill.damageDiceCount > 0)
        {
            //����Ͷ��
            DiceResult attackRoll = null;
            //�˺�Ͷ��
            DiceResult damageRoll = null;
            int damageDiceCount = skill.damageDiceCount;
            //�ȴ�����Ͷ�����
            yield return DiceManager.Instance.RollDice(DiceType.attackDice, 1, 20, GetRollModifier(), info.bustPortrait, (result) => attackRoll = result);
            //���ݹ���Ͷ��������������Ƿ�����˺�, �Ƿ񱩻�, ������ս����־
            if (attackRoll.criticalMiss)
            {
                UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> �ͷ��� <color=#DAA520><u>{skill.skillName}</u></color>, ����Ͷ�����Ϊ1(��ʧ��), ����ʧ��");
                yield break;
            }
            else if (attackRoll.criticalHit)
            {
                UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> �ͷ��� <color=#DAA520><u>{skill.skillName}</u></color>, ����Ͷ�����Ϊ20(��ɹ�), ���<color=red>����</color>");
                damageDiceCount *= 2;
            }
            else
            {
                UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> �ͷ��� <color=#DAA520><u>{skill.skillName}</u></color>, ����Ͷ�����Ϊ{attackRoll.totalValue}");
            }
            //�ȴ��˺�Ͷ�����
            yield return DiceManager.Instance.RollDice(DiceType.damageDice, damageDiceCount, skill.damageDiceSides, GetRollModifier(), info.bustPortrait, (result) => damageRoll = result);

            //��effectsTarget�е�ÿһ����ɫ�����˺�
            foreach (Character target in effectsTarget)
            {
                bool isHit = target.TakeDamage(attackRoll, damageRoll); //�����˺�, ����¼�Ƿ��ܵ��˺�
                //������������˺� �� ���ܸ���Buff, �����н�ɫ�������Buff
                if (isHit && skill.applicateBuffs.Count > 0)
                {
                    foreach (Buff buff in skill.applicateBuffs)
                    {
                        BuffManager.Instance.AddBuff(target, buff);
                    }
                }
                yield return null; //ÿ֡����һλ��ɫ���ܵ������˺������Buff�߼�
            }
        }
        //������ܲ�����˺�, ���жϼ����Ƿ񸽴�Buff, �������˵��Ϊ��Buff����, ����Ҫ�����춨, ֱ�Ӷ�effectsTarget�е�ÿһλ��ɫ���Buff
        else
        {
            //����ս����־
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> �ͷ��� <color=#DAA520><u>{skill.skillName}</u></color>");
            //ΪeffectsTarget�е�ÿλ��ɫ���buff
            if (skill.applicateBuffs.Count > 0)
            {
                foreach (Character target in effectsTarget)
                {
                    foreach (Buff buff in skill.applicateBuffs)
                    {
                        BuffManager.Instance.AddBuff(target, buff);
                    }
                    yield return null; //ÿ֡����һλ��ɫ�����Buff�߼�
                }
            }
        }

        //�����ɫ�ڻغ�����, �Ҽ��ܸ�����buffӰ���˽�ɫ���ȹ�, �ͶԻغ�����һ���ֲ�������������
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
    /// �����ٻ��༼�ܵ�Ӧ�÷���, ��summonedUnitGrids��ʵ������Ӧskill�е�summonedUnitPrefab, ��ʼ���ٻ���λ�Ľ�ɫ��Ϣ, �����ٻ��ߺ��ٻ���λ֮��Ĺ�ϵ
    /// </summary>
    /// <param name="skill"></param>
    /// <param name="summonedUnitGrids"></param>
    /// <returns></returns>
    public IEnumerator CreatSummonedUnitOnTargetGrids(SummonSkillData skill, HashSet<GridHelper> summonedUnitGrids)
    {
        Debug.Log("CreatSummonedUnitOnTargetGrids1");
        isAttacking = true;
        SetTriggerSkillAnimation(skill); //�������ܶ���
        yield return new WaitWhile(() => isAttacking);
        Debug.Log("CreatSummonedUnitOnTargetGrids2");

        List<Character> units = new List<Character>();
        //ʵ�����ٻ���λ���������Ľ�ɫԤ����
        for (int i = 0; i < summonedUnitGrids.Count; i++)
        {
            Character unit = Instantiate(skill.summonedUnitPrefab).GetComponent<Character>();
            //unit.gameObject.SetActive(false);
            unit.tag = this.tag;
            units.Add(unit);
            yield return null;
        }
        //�����ٻ���λ������, ����(q, r), λ��, tag, summoner����Ϣ
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
        //����ս����־
        UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> �ͷ��� <color=#DAA520><u>{skill.skillName}</u></color>");
        //����ڻغ�����, ���ٻ���λ��ӵ��غ�����
        if (isInTurn)
        {
            yield return TurnManager.Instance.AddParticipant(units);
        }
        //yield break;
    }


    /// <summary>
    /// ��ɫ����������������
    /// </summary>
    public void OnAttackAnimationEnd()
    {
        Debug.Log("OnAttackAnimationEnd");
        isAttacking = false; //��ǹ�������
    }

    /// <summary>
    /// ��ɫ�ܵ��˺�
    /// </summary>
    public bool TakeDamage(DiceResult attackDice, DiceResult damageDice)
    {
        //����Ͷ���Ǵ�ɹ���Ͷ�����С�ڱ�������AC ���� ����Ͷ����ʧ��, ��δ����
        if((attackDice.totalValue < info.armorClass && !attackDice.criticalHit) || attackDice.criticalMiss)
        {
            UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> ͨ������Ͷ���춨(<color=#FF4500>AC{info.armorClass} > {attackDice.totalValue}</color>), δ�ܵ��˺�");
            beingAttacked = false;
            return false;
        }

        info.hp -= damageDice.totalValue;
        beingAttacked = true; //���Ϊ�����ܻ�
        if (this == PartyManager.Instance.leader) //���������, �͸�������Ѫ������
        {
            UIHealthPointManager.Instance.UpdateLeaderHpBar();
        }
        else
        {
            UIHealthPointManager.Instance.ShowTargetHpPanel(this); //���¹۲����Ѫ��
        }
        if (isInTurn) //����ڻغ�����, �͸���Ѫ������
        {
            UITurnManager.Instance.UpdateTurnPortraitHpBarFill(this);
        }

        //����ս����־
        UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> δͨ������Ͷ���춨(<color=#FF4500>AC{info.armorClass} <= {attackDice.totalValue}</color>), �ܵ� <color=#FF4500><u>{damageDice.totalValue}���˺�</u></color>");

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
    /// ��ɫ����
    /// </summary>
    public IEnumerator Die()
    {
        info.hp = 0;
        animator.SetBool("dieState", true);
        animator.SetTrigger("die"); //��������
        this.GetComponent<BoxCollider>().enabled = false; //ʧ���ɫ���ϵ���ײ��

        //�������ҽ�ɫ, �����
        if (CompareTag("Player"))
        {
            PartyManager.Instance.RemoveCharacter(this);
        }
        
        //����ڻغ�����, �͸��»غ���UI
        if (isInTurn)
        {
            isInTurn = false; //����˳��غ���
            TurnManager.Instance.RemoveParticipant(this); //�����ɫ�ڻغ�����, �ͽ���ӻغ����Ƴ�
        }

        //�����ٻ��ߵ���Ϣ
        //if (summoner != null)
        //{
        //    summoner.summonedUnits.Remove(summoner.summonedUnits.Where(info => info.character == this).Last());
        //}
        //���������ý�ɫ���ٻ������Ϣ
        if (summonedUnits.Count > 0)
        {
            yield return new WaitWhile(() => UITurnManager.Instance.relayouting);
            foreach (SummonedUnitInfo unitInfo in summonedUnits.ToList())
            {
                if (unitInfo.inheritSummonerLifespan && unitInfo.character.info.hp > 0)
                {
                    yield return unitInfo.character.Die(); //�̳иý�ɫ�������ڵ��ٻ���ͬʱ����
                    summonedUnits.Remove(unitInfo);
                }
            }
        }

        //����ս����־
        UIBattleLogManager.Instance.AddLog($"<color={attackerColor}><u>{info.name}</u></color> <color=#FF4500><u>����</u></color>");
    }

    /// <summary>
    /// ��ɫ�ܻ��������������������
    /// </summary>
    public void OnBeAttackedAnimationEnd()
    {
        beingAttacked = false; //����ܻ�����
    }

    /// <summary>
    /// ��ý�ɫͶ������ʱ�ĵ���ֵ
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, int> GetRollModifier()
    {
        Dictionary<string, int> modifiers = new Dictionary<string, int>();

        //���빥���춨���Ե���ֵ �� buff���Լ�ֵ
        if(info.attackType == AttackType.strength)
        {
            modifiers.Add("��������ֵ", (info.strength - 8) / 2);
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
            modifiers.Add("���ݵ���ֵ", (info.finesse - 8) / 2);
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
            modifiers.Add("��������ֵ", (info.intelligence - 8) / 2);
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
            modifiers.Add("����������ֵ", info.proficiency);
        }

        return modifiers;
    }

    /// <summary>
    /// ���ݵ�ǰ��ɫ��������, ������������
    /// </summary>
    private void SetTriggerAttackAnimation()
    {
        //������ �� �ֽ�������ʱ
        if (equipment.weaponEquipment == null || equipment.weaponEquipment.weaponType == WeaponType.Sword)
        {
            animator.SetTrigger("swordAttack");
        }
        //�ֹ�������ʱ
        else if (equipment.weaponEquipment.weaponType == WeaponType.Bow)
        {
            animator.SetTrigger("bowAttack");
        }
        //�ַ�������ʱ
        else if (equipment.weaponEquipment.weaponType == WeaponType.Staff)
        {
            animator.SetTrigger("staffAttack");
        }
    }

    /// <summary>
    /// ����ʩ�����ܵĴ�����������
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
    /// ��װ��itemװ������ɫ��Ӧ��λ��, �����½�ɫ���UI
    /// </summary>
    /// <param name="item"></param>
    public void Equip(EquipmentItemData item)
    {
        if(item.type == EquipmentType.Helmet)
        {
            //�����Ӧװ����λ��Ϊ��, �Ҳ���ڵ�װ��������Ҫװ����װ��(�����ʼ����ʱ����ж��װ��), ����ж��װ��
            if (equipment.helmetEquipment != null && equipment.helmetEquipment != item)
            {
                Unequip(equipment.helmetEquipment);
            }
            //����Ӧװ����λ����Ϊitem
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

        //��װ�����Լ�ֵ �ӵ���ɫ������
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

        //������ڻغ����и���װ�� ����װ���������ȹ�����ֵ��Ϊ0, �͸��»غ���ͷ���б�UI
        if (isInTurn && item.initiativeModifier != 0)
        {
            StartCoroutine(TurnManager.Instance.ResortTurnParticipants());
        }

        //���������, �ͽ�����ģ��ʵ����������ģ���ֲ�
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

        //���װ���и���Buff, ����ӵ���ɫ����
        if (item.equipmentItemBuffs.Count > 0)
        {
            foreach(Buff buff in item.equipmentItemBuffs)
            {
                BuffManager.Instance.AddBuff(this, buff);
            }
        }
        
        //�����װ���и��Ӽ���, ����ӵ���ɫ����
        if (item.equipmentItemSkills.Count > 0)
        {
            foreach (SkillBaseData skill in item.equipmentItemSkills)
            {
                SkillManager.Instance.LearnSkill(this, skill);
            }
        }

        //����UI(CharacterInfo������ϢUI �� ���ͼƬ)
        if (PartyManager.Instance != null && this == PartyManager.Instance.leader)
        {
            UIPartyCharacterManager.Instance.UpdateCharacterPanel(this);
            UIHealthPointManager.Instance.UpdateLeaderHpBar(); //��������Ѫ��
        }
    }

    /// <summary>
    /// ��װ��item�ӽ�ɫ��Ӧ��λ���Ƴ�, �����½�ɫ���UI
    /// </summary>
    /// <param name="item"></param>
    /// <param name="item"></param>
    public void Unequip(EquipmentItemData item)
    {
        //�����Ӧװ����λ��Ϊ��, �Ҳ���ڵ�װ��������Ҫװ����װ��(�����ʼ����ʱ����ж��װ��), ����ж��װ��
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

        //����ɫ���Խ�ȥװ�����Լ�ֵ
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

        //������ڻغ����и���װ�� ����װ���������ȹ�����ֵ��Ϊ0, �͸��»غ���ͷ���б�UI
        if (isInTurn && item.initiativeModifier != 0)
        {
            StartCoroutine(TurnManager.Instance.ResortTurnParticipants());
        }

        //���װ���и���Buff, �ʹӽ�ɫ�����Ƴ�
        if (item.equipmentItemBuffs.Count > 0)
        {
            foreach (Buff buff in item.equipmentItemBuffs)
            {
                BuffManager.Instance.RemoveBuff(this, buff);
            }
        }

        //�����װ���и��Ӽ���, �ʹӽ�ɫ�����Ƴ�
        if (item.equipmentItemSkills.Count > 0)
        {
            foreach (SkillBaseData skill in item.equipmentItemSkills)
            {
                SkillManager.Instance.RemoveSkill(this, skill);
            }
        }

        //����UI(CharacterInfo������ϢUI �� ���ͼƬ)
        if (this == PartyManager.Instance.leader)
        {
            UIPartyCharacterManager.Instance.UpdateCharacterPanel(this);
            UIHealthPointManager.Instance.UpdateLeaderHpBar(); //��������Ѫ��
        }

        //���������, ����������ģ���ֲ�������ģ��
        if (item.type == EquipmentType.Weapon)
        {
            if (weaponModel != null)
            {
                Destroy(weaponModel);
            }
        }

        //��ж��װ����ӵ�������
        bagItems.Add(item);
        UIPartyInventoryManager.Instance.AddItemToBag(this, item);
    }

    /// <summary>
    /// ��װ��item�ӽ�ɫ��Ӧ��λ���Ƴ�, ���Ҳ����ظ�����, ������
    /// </summary>
    /// <param name="item"></param>
    /// <param name="item"></param>
    public void DropEquipment(EquipmentItemData item)
    {
        //�����Ӧװ����λ��Ϊ��, �Ҳ���ڵ�װ��������Ҫװ����װ��(�����ʼ����ʱ����ж��װ��), ����ж��װ��
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

        //����ɫ���Խ�ȥװ�����Լ�ֵ
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

        //������ڻغ����и���װ�� ����װ���������ȹ�����ֵ��Ϊ0, �͸��»غ���ͷ���б�UI
        if (isInTurn && item.initiativeModifier != 0)
        {
            StartCoroutine(TurnManager.Instance.ResortTurnParticipants());
        }

        //���װ���и���Buff, �ʹӽ�ɫ�����Ƴ�
        if (item.equipmentItemBuffs.Count > 0)
        {
            foreach (Buff buff in item.equipmentItemBuffs)
            {
                BuffManager.Instance.RemoveBuff(this, buff);
            }
        }

        //�����װ���и��Ӽ���, �ʹӽ�ɫ�����Ƴ�
        if (item.equipmentItemSkills.Count > 0)
        {
            foreach (SkillBaseData skill in item.equipmentItemSkills)
            {
                SkillManager.Instance.RemoveSkill(this, skill);
            }
        }

        //���������, ����������ģ���ֲ�������ģ��
        if (item.type == EquipmentType.Weapon)
        {
            if (weaponModel != null)
            {
                Destroy(weaponModel);
            }
        }

        //����UI(CharacterInfo������ϢUI �� ���ͼƬ)
        if (this == PartyManager.Instance.leader)
        {
            UIPartyCharacterManager.Instance.UpdateCharacterPanel(this);
            UIHealthPointManager.Instance.UpdateLeaderHpBar(); //��������Ѫ��
        }
    }

    /// <summary>
    /// ʹ������Ʒitem, ���� �� ��һ���غ��� �ı��ɫ����, �����½�ɫ���UI
    /// </summary>
    /// <param name="item"></param>
    public void Consume(ConsumableItemData item)
    {
        if (this.isInTurn)
        {
            info.actionPoint--; //�غ���ʹ������Ʒ ����1�ж�����
        }
        BuffManager.Instance.AddBuff(this, item.comsumableItemBuff);
    }

    /// <summary>
    /// ����sortType��bagItems��������
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
    /// �����ͣ�ڽ�ɫ����ʱ, ��ʾ�͸��¹۲�Ŀ������Ѫ�����
    /// </summary>
    private void OnMouseEnter()
    {
        if(this != PartyManager.Instance.leader)
        {
            UIHealthPointManager.Instance.ShowTargetHpPanel(this);
        }
    }

    /// <summary>
    /// �����ӽ�ɫ�����ƿ�ʱ, ���ع۲�Ŀ������Ѫ�����
    /// </summary>
    private void OnMouseExit()
    {
        UIHealthPointManager.Instance.HideTargetHpPanel();
    }

    /// <summary>
    /// �õ�����start��end֮��ľ���
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