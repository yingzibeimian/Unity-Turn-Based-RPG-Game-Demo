using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharactertAI : MonoBehaviour
{
    public List<Vector3Int> patrolPath = new List<Vector3Int>(); //Ѳ��·��, ��¼·�������(q,r,heightorder)
    private List<GridHelper> patrolPathGrids = new List<GridHelper>();

    public float checkInterval = 2.0f; //��⸽���Ƿ������ҵļ���¼�
    private WaitForSeconds waitForCheck;
    public float idleTime = 3.0f; //Ѳ���е�ͣ��ʱ��
    private WaitForSeconds waitForIdle;

    public float checkRadius = 10.0f; //��ս��ⷶΧ�뾶

    public float proximityWeight = 0.2f; //��������Ȩ�� (���ȹ�������Ͻ��ĵ��� 0=����ע 1=������ȼ�)
    public float groupDensityWeight = 0.3f; //Ⱥ��ۼ�����Ȩ�� (���ȹ����ۼ���һ��ĵ���)
    public float threatWeight = 0.25f; //��в����Ȩ�� (���ȹ����˺���(��в����)�ĵ���)
    public float aromrPenaltyWeight = 0.25f; //���׳ͷ�Ȩ�� (���˻���Խ��, ���ȹ���ȨֵԽ��)

    public Character character;
    public AStarPathfinder pathfinder;
    public Animator animator;

    //���˽ڵ�
    private class AITarget : IComparable<AITarget>
    {
        public Character character; //���˽�ɫ
        public float targetValue; //���˳�ΪĿ���Ȩֵ

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
    /// ��ʼ��Ѳ��·��
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
    /// Enemy�Զ�Ѳ�ߺ���
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
    /// ����Ƿ�����Ҵ���
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckForPlayer()
    {
        yield return waitForCheck;
        //����Ѿ��ڻغ�����, ����������ٻ��� �Ͳ����
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
    /// ����AI��Ϊ�߼�
    /// </summary>
    /// <returns></returns>
    public IEnumerator HandleAIBehavior()
    {
        yield return new WaitUntil(() => character.characterInitialized); //ȷ��AI��ɫ��ɳ�ʼ��

        Debug.Log("HandleAIBehavior");
        //���غ��Ƽ����������� && �غ��ж�����>0 && Ѫ��>0ʱ �غ�ѭ����һֱͣ���ڸ�AI��ɫ��AI��Ϊ�߼�������
        while (TurnManager.Instance.CheckCombatContinueCondition() && character.info.actionPoint > 0 && character.info.hp > 0)
        {
            Character target = null; //����AI��Ϊ��Ŀ���ɫ
            SkillBaseData skill = null; //����AI��Ϊ�ͷŵļ���
            List<SkillBaseData> characterSkills = character.characterSkills; //��ɫ�ļ����б�
            //���Ѫ������1/3, ���սἼ��(AI��ɫ�����б��е����һ��)��ʣ����ȴ�غ���Ϊ0, �����ж�����С�ڵ��ڽ�ɫ�ж�����, ���ͷ��սἼ��
            if (character.info.hp <= (character.info.maxHp / 3) && characterSkills.Count > 0 && characterSkills[characterSkills.Count - 1].remainingTurns == 0 
                && characterSkills[characterSkills.Count - 1].actionPointTakes <= character.info.actionPoint)
            {
                skill = characterSkills[characterSkills.Count - 1];
            }
            if (skill == null)
            {
                //������ɫ�����з��սἼ��, ���ʣ����ȴ�غ���Ϊ0, �������ж�����С�ڵ��ڽ�ɫ�ж�����, ���ͷŸü���
                for (int i = 0; i < characterSkills.Count - 1; i++)
                {
                    if (characterSkills[i].remainingTurns == 0 && characterSkills[i].actionPointTakes <= character.info.actionPoint)
                    {
                        skill = characterSkills[i];
                        break;
                    }
                }
            }
            //������ڴ��ͷŵļ���, ���� ������Ч��Ӧ���༼������Ӱ��з���Ӫ
            if (skill != null && skill is EffectApplicationSkillData && (skill as EffectApplicationSkillData).effectArea.affectOppositeCamp)
            {
                //���AI�ű����ƽ�ɫΪPlayer, ����Enemy��Ѱ����ΪAIĿ��target
                if (character.CompareTag("Player"))
                {
                    yield return GetAIBehaviorTargetCharater(TurnManager.Instance.enemyGrids.Values.ToList(), (result) => { target = result; });
                }
                //���AI�ű����ƽ�ɫΪEnemy, ����Player��Ѱ����ΪAIĿ��target
                else if (character.CompareTag("Enemy"))
                {
                    yield return GetAIBehaviorTargetCharater(TurnManager.Instance.playerGrids.Values.ToList(), (result) => { target = result; });
                }
                //���AI��ΪĿ�겻Ϊ��, ����target����Ӧ��skill����Ч��
                if (target != null)
                {
                    yield return SkillManager.Instance.ApplyCharacterAISkillEffects(character, target, skill);
                    //CoroutineManager.Instance.AddTaskToGroup(SkillManager.Instance.ApplyCharacterAISkillEffects(character, target, skill), character.info.name);
                }
            }
            //������ڴ��ͷŵļ���, ���� ������Ч��Ӧ���༼������Ӱ�켺����Ӫ
            else if (skill != null && skill is EffectApplicationSkillData && (skill as EffectApplicationSkillData).effectArea.affectSelfCamp)
            {
                //���AI�ű����ƽ�ɫΪPlayer, ����Player��Ѱ����ΪAIĿ��target
                if (character.CompareTag("Player"))
                {
                    yield return GetAIBehaviorTargetCharater(TurnManager.Instance.playerGrids.Values.ToList(), (result) => { target = result; });
                }
                //���AI�ű����ƽ�ɫΪEnemy, ����Enemy��Ѱ����ΪAIĿ��target
                else if (character.CompareTag("Enemy"))
                {
                    yield return GetAIBehaviorTargetCharater(TurnManager.Instance.enemyGrids.Values.ToList(), (result) => { target = result; });
                }
                if (target != null)
                {
                    yield return SkillManager.Instance.ApplyCharacterAISkillEffects(character, target, skill);
                }
            }
            //������ڴ��ͷŵļ���, ���� ������Ч��Ӧ���༼������Ӱ��ʩ��������
            else if (skill != null && skill is EffectApplicationSkillData && (skill as EffectApplicationSkillData).effectArea.affectSelf)
            {
                //��AIĿ��target��Ϊʩ���߱���
                target = character;
                yield return SkillManager.Instance.ApplyCharacterAISkillEffects(character, target, skill);
            }
            //������ڴ��ͷŵļ���, ���� �������ٻ��༼��
            else if (skill != null && skill is SummonSkillData)
            {
                //����ٻ���Χ�Կ�ѡ���Ϊ����
                if ((skill as SummonSkillData).summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
                {
                    //���AI�ű����ƽ�ɫΪPlayer, ����Enemy��Ѱ����ΪAIĿ��target
                    if (character.CompareTag("Player"))
                    {
                        yield return GetAIBehaviorTargetCharater(TurnManager.Instance.enemyGrids.Values.ToList(), (result) => { target = result; });
                    }
                    //���AI�ű����ƽ�ɫΪEnemy, ����Player��Ѱ����ΪAIĿ��target
                    else if (character.CompareTag("Enemy"))
                    {
                        yield return GetAIBehaviorTargetCharater(TurnManager.Instance.playerGrids.Values.ToList(), (result) => { target = result; });
                    }
                }
                //����ٻ���Χ������Ϊ����
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
            //���û��Ҫ�ͷŵļ���, ����Ѱ·�͹���targetĿ���ɫ
            else if (skill == null)
            {
                float apTakePerGrid = 1.0f / character.info.runSpeed; //AI�ű������ƽ�ɫ��ÿ���ƶ������ĵ��ж�����
                //�����ɫ���ٻ����ƶ�һ��, �����Ѱ·�͹���
                if (character.info.actionPoint >= apTakePerGrid)
                {
                    //���AI�ű����ƽ�ɫΪPlayer, ����Enemy��Ѱ����ΪAIĿ��target
                    if (character.CompareTag("Player"))
                    {
                        yield return GetAIBehaviorTargetCharater(TurnManager.Instance.enemyGrids.Values.ToList(), (result) => { target = result; });
                    }
                    //���AI�ű����ƽ�ɫΪEnemy, ����Player��Ѱ����ΪAIĿ��target
                    else if (character.CompareTag("Enemy"))
                    {
                        yield return GetAIBehaviorTargetCharater(TurnManager.Instance.playerGrids.Values.ToList(), (result) => { target = result; });
                    }
                    if (target != null)
                    {
                        //���character���빥��Ŀ��ľ��� ���� character�Ĺ�������, ����Ѱ·���ƶ�
                        if (GetDistance(character.nowGrid, target.nowGrid) > character.info.attackDistance)
                        {
                            CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, character.info.attackDistance), character.info.name);
                            CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1), character.info.name);

                            //yield return character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, character.info.attackDistance);
                            //yield return character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1);
                        }
                        //����ƶ���, ��ɫ�ж����������Խ��й���, ��ֱ�ӷ���
                        if (character.info.actionPoint < 2)
                        {
                            yield break;
                        }
                        //yield return character.Attack(target);
                        CoroutineManager.Instance.AddTaskToGroup(character.Attack(target), character.info.name);
                    }
                }
                //�����ɫһ��Ҳ�޷��ƶ�, �����AI��Ϊ�߼�����Э��, ������ɫ�غ�
                else
                {
                    yield break;
                }
            }

            //���AI��ΪĿ���ɫΪ��, ����û�п���ѡ���Ŀ���ɫ, �����AI��Ϊ�߼�����Э��, ������ɫ�غ�
            if (target == null)
            {
                Debug.Log("target null");
                yield break;
            }
            //�����Ϊ��, �Ϳ�ʼִ�б���AI��Ϊ��Э�̶���
            else
            {
                CoroutineManager.Instance.StartGroup(character.info.name);
                yield return new WaitUntil(() => CoroutineManager.Instance.TaskInGroupIsEmpty(character.info.name)); //һֱ�ȴ�ֱ��AI��ΪЭ�̶���ִ�����
            }

             yield return null;
        }
    }

    /// <summary>
    /// �õ�AI��Ϊ��Ŀ���ɫtarget
    /// </summary>
    /// <param name="camp"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    private IEnumerator GetAIBehaviorTargetCharater(List<Character> camp, Action<Character> onTargetFound) 
    {
        MinHeap<AITarget> targets = new MinHeap<AITarget>(); //���˽ڵ����С��, ѡȡ�Ѷ�Ԫ����Ϊ����AI��Ϊ��Ŀ�����
        foreach (Character character in camp)
        {
            if(character.info.hp > 0)
            {
                targets.Enqueue(new AITarget(character, GetAITargetValue(character, camp)));
                yield return null;
            }
        }
        Character foundTarget = targets.Count > 0 ? targets.Dequeue().character : null;
        onTargetFound?.Invoke(foundTarget); // ͨ���ص����ؽ��
    }

    /// <summary>
    /// ����õ�AITarget�е�Ŀ��Ȩֵ����targetValue
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
    /// �������������������start��end�ľ���
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
