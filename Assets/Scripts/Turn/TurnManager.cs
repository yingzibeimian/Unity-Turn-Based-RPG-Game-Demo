using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// �غ��ƹ�����
/// </summary>
public class TurnManager : MonoBehaviour
{
    private static TurnManager instance;
    public static TurnManager Instance => instance;

    public List<Character> participants = new List<Character>(); //�غ���ս��������
    public List<Character> nextTurnParticipants = new List<Character>(); //��һ�غϵĲ�����
    public Dictionary<GridHelper, Character> playerGrids = new Dictionary<GridHelper, Character>(); //key:�����Ӫ��ɫ�ĸ��� value:��Ӧ��ɫ
    public Dictionary<GridHelper, Character> enemyGrids = new Dictionary<GridHelper, Character>(); //key:�з���Ӫ��ɫ�ĸ��� value:��Ӧ��ɫ

    public float triggerRadius = 15.0f; //��ʼ��ʱ, �����غϵĽ�ɫ�ĸð뾶���ڵĽ�ɫ, ͬ������غ���ս��
    public bool endPlayerTurn = false; //�����ֶ����ý�����һغ�
    public bool delayPlayerTurn = false; //�����Ƴ���һغ�

    public int CurrentRound { get; private set; } = 0; //��ǰ�غ�����

    public Character nowPlayer; //�ṩ���ⲿʹ��, �鿴�غ���������ִε���ҽ�ɫ
    //public bool isInTurn = false; //�����ṩ���ⲿ �����Ƿ��ڻغ��Ƶ���
    private List<Character> triggers = new List<Character>(); //�����غ��ƽ�ɫ�б�
    private bool isManualTurnStart = false; //���ֻغ������ɵ���AI���� ���� ����ֶ�����
    private WaitForSeconds waitHalfSecond = new WaitForSeconds(0.25f); //Э�̼��ʱ����
    private bool isMouseOverGrid = false; //�ж�����Ƿ���������
    private bool isMouseOverCharacter = false; //�ж�����Ƿ��ڽ�ɫ����
    private bool participantRemoved = false; //�жϵ�ǰ�ִεĽ�ɫ�Ƿ��Ƴ�
    private float apTakes; //Ԥ�����ĵ��ж�����

    private bool isInTurn = false; //�жϻغ����Ƿ���

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
    /// �ṩ���ⲿ�Ļغ��ƿ�������, ��������ҽ��������Ұ��Χ�����, Ҳ����������������ý���غ���
    /// </summary>
    /// <param name="triggers">�������λغ��ƽ�ɫ</param>
    public void StartTurn(List<Character> triggers, bool isManualTurnStart = false)
    {
        isInTurn = true;
        this.isManualTurnStart = isManualTurnStart;

        this.triggers = triggers.ToList();

        InitializeParticipants(triggers); //��ʼ��������
        PartyManager.Instance.InitialiazePartyWhenStartTurn(); //��ʼ���غ����еĶ���, �������Ϊÿ����ɫһ֧�ֶ�, ��leader����Ϊnull(ֹͣ��ɫ���ƶ�����)
        CurrentRound = 0; //��ʼ���غ���
        foreach(Character participant in participants)
        {
            participant.isInTurn = true;

            CoroutineManager.Instance.StopGroup(participant.info.name); //ֹͣ��ɫ���ϵ�Э�̶���

            CoroutineManager.Instance.AddTaskToGroup(participant.pathfinder.CorrectCharacterPositionInTurn(triggers), participant.info.name);
            CoroutineManager.Instance.BindCallback(participant.info.name, () =>
            {
                participant.animator.SetBool("combat", true); //��ս����
            });
            CoroutineManager.Instance.StartGroup(participant.info.name);
            //participant.isMoving = false;
            //participant.animator.SetBool("isMoving", false);
            //participant.animator.SetBool("combat", true); //��ս����

            participant.info.actionPoint = 0; //��ʼ���ж�����AP

            //�����в����߽��µ���������ΪdynamicLimit
            participant.nowGrid.info.dynamicLimit = true;
            participant.nowGrid.info.walkable = false;

            //������ҽ�ɫ�͵з���ɫ�ĸ����б�, �����ƶ������еĽ�������ж�
            if (participant.CompareTag("Player"))
            {
                playerGrids.TryAdd(participant.nowGrid, participant);
            }
            else if (participant.CompareTag("Enemy"))
            {
                enemyGrids.TryAdd(participant.nowGrid, participant);
            }

            //�غϿ�ʼʱ, ��ÿλ��ɫ���ϵ�Buff�Ӽ�ʱ�Ƽ�ʱתΪ�غ��Ƽ�ʱ
            BuffManager.Instance.UpdateBuffAfterStartTurn(participant);
            //�غϿ�ʼʱ, ��ÿλ��ɫ���ϵ�Skill�Ӽ�ʱ�Ƽ�ʱתΪ�غ��Ƽ�ʱ
            SkillManager.Instance.UpdateSkillBarUIAfterStartTurn(participant);
        }

        //��ʼ���غ���UI
        UITurnManager.Instance.InitializeTurnUI();
        if (!isManualTurnStart)
        {
            UITurnManager.Instance.ShowTurnStartTip(); //��������ֶ������غ���, ������ս��, ����ʾ�غϿ�ʼ��ʾ
        }
        //��ʾս����־���
        UIBattleLogManager.Instance.ShowBattleLogPanel();
        UIBattleLogManager.Instance.AddLog("<color=red>~ �غϿ�ʼ ~</color>");

        StartCoroutine(RoundLoop()); //�����غ�����ѭ��
    }

    /// <summary>
    /// �����غ��Ƶķ���, Ҳ�����ṩ��������������ý���Ļغ�ʹ��, ������������ǰ, Ҫ�ȼ��غϼ�������
    /// </summary>
    public void EndTurn()
    {
        if (!isManualTurnStart && CheckCombatContinueCondition()) //���������������Ļغ��� �� ������غ��ƽ�������, ������������
        {
            return;
        }
        //Debug.Log("EndTurn�����ã����ö�ջ:\n" + Environment.StackTrace);
        StartCoroutine(EndTurnCoroutine());
    }

    /// <summary>
    /// �����غ��Ƶ�Э�̷���
    /// </summary>
    /// <returns></returns>
    private IEnumerator EndTurnCoroutine()
    {
        yield return new WaitWhile(() => UIPartyManager.Instance.relayouting); //�������ͷ��UI���ڲ��ֹ�������ȴ�

        isInTurn = false;

        triggers.Clear();

        foreach (Character participant in participants)
        {
            participant.isInTurn = false;

            participant.animator.SetBool("combat", false); //��ս����

            participant.nowGrid.info.dynamicLimit = false;
            participant.nowGrid.info.walkable = true;

            if (participant.CompareTag("Player"))
            {
                //Debug.Log($"{participant.info.name} Start ReturnGridsToPool");
                yield return new WaitUntil(() => CoroutineManager.Instance.TaskInGroupIsEmpty(participant.info.name + "_pathfinding"));
                StartCoroutine(participant.pathfinder.ReturnGridsToPool());
            }

            //�غϽ�����, ��ÿλ��ɫ���ϵ�Buff�ӻغ��Ƽ�ʱתΪ��ʱ�Ƽ�ʱ, ����ʣ��غ�������ʣ������, ʣ�������������Ƴ�Buff
            BuffManager.Instance.UpdateBuffAfterEndTurn(participant);
            //�غϽ�����, ��ÿλ��ɫ���ϵ�Skill�ӻغ��Ƽ�ʱתΪ��ʱ�Ƽ�ʱ
            SkillManager.Instance.UpdateSkillBarUIAfterEndTurn(participant);
        }

        PartyManager.Instance.InitialiazePartyWhenEndTurn();

        nowPlayer = null;
        playerGrids.Clear();
        enemyGrids.Clear();

        participants.Clear();
        nextTurnParticipants.Clear();

        //���»غ���ͷ��UI, �������
        UITurnManager.Instance.UpdateWhenEndTurn();
        //����ս����־���
        UIBattleLogManager.Instance.AddLog("<color=red>~ �غϽ��� ~</color>");
        UIBattleLogManager.Instance.HideBattleLogPanel();


        Debug.Log("�غϽ���");
    }

    /// <summary>
    /// ��ʼ���غ��Ʋ�����participants
    /// </summary>
    /// <param name="triggers"></param>
    private void InitializeParticipants(List<Character> triggers)
    {
        foreach(Character trigger in triggers)
        {
            //���غ��ƴ����߼���participants
            if (trigger != null && !participants.Contains(trigger))
            {
                participants.Add(trigger);
                nextTurnParticipants.Add(trigger);
            }
            //���غ��ƴ�����triggerRadius�뾶���ڵĽ�ɫҲ����participants
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
    /// (���ڻغ��Ʋ����ߵ��ȹ�ֵ�����仯)����һ�ֻغϲ����߽�����������, �����»غ���ͷ��UI����
    /// </summary>
    /// <returns></returns>
    public IEnumerator ResortTurnParticipants()
    {
        nextTurnParticipants.Sort(new CharacterInitiativeComparer());
        yield return UITurnManager.Instance.ResortNextTurnParticipantsPortraits();
    }

    /// <summary>
    /// �Իغ��Ʋ����߰��ȹ�initiative���Խ��н�������
    /// </summary>
    /// <param name="participants"></param>
    private void SortParticipantsByInitiative(List<Character> participants)
    {
        participants = participants.OrderByDescending(p => p.info.initiative).ToList();
    }

    /// <summary>
    /// �غ�����ѭ��Э��
    /// </summary>
    /// <returns></returns>
    private IEnumerator RoundLoop()
    {
        while (CheckCombatContinueCondition()) //���غ��Ƿ����������
        {
            CurrentRound++; //�غ���+1
            UIBattleLogManager.Instance.AddLog($"<color=#808080>~��{CurrentRound}�غ�~</color>");
            Debug.Log($"��{CurrentRound}�غϿ�ʼ");

            for (int i = 0; i < participants.Count; i++)
            {
                Debug.Log($"{participants[i].info.name}�Ļغ�");

                nowPlayer = participants[i];

                if (nowPlayer.info.hp <= 0) //�����ɫѪ��С��0, ˵���ý�ɫ����, �Ѿ�����һ�ֻغ����Ƴ�, ����ֱ������
                {
                    continue;
                }

                nowPlayer.info.actionPoint = Mathf.Min(nowPlayer.info.actionPoint + 4, 6); //�ָ��ж�����
                Debug.Log($"{nowPlayer.info.name} actionPoint {nowPlayer.info.actionPoint}");

                //������һغ�
                if (!nowPlayer.isAI)
                {
                    UITurnManager.Instance.UpdateActionPointBalls(nowPlayer.info.actionPoint, 0);
                    UITurnManager.Instance.ShowTurnPlayerPanel(); //��ʾ��һغ����
                    yield return HandlePlayerTurn(nowPlayer); //�����������
                }
                //����AI�غ�
                else
                {
                    UITurnManager.Instance.HideTurnPlayerPanel(); //������һغ����
                    yield return HandleAITurn(nowPlayer); //����AI�غ�
                }

                nowPlayer.info.actionPoint = Mathf.Max(Mathf.Floor(nowPlayer.info.actionPoint), 0); //�����ж�����
                Debug.Log($"{nowPlayer.info.name} actionPoint {nowPlayer.info.actionPoint}");

                //���������غ���ս������, ֱ�������غ�����ѭ��
                if (!CheckCombatContinueCondition())
                {
                    break;
                }
                if (participantRemoved)
                {
                    i--; //����н�ɫ�ӻغ������Ƴ�, ������±�
                    participantRemoved = false;
                }

                if (!delayPlayerTurn) //���������������Ƴٻغ�
                {
                    BuffManager.Instance.UpdateBuffAfterFinishTurn(nowPlayer); //����buff�����غ�
                    yield return ResortTurnParticipants(); //������һ�غ�ͷ������(��Ϊ����buff��, ��ɫ���ȹ�ֵ���ܷ����仯)
                    UITurnManager.Instance.UpdateTurnPortrait(nowPlayer); //���»غ���ͷ���б�UI
                    SkillManager.Instance.UpdateSkillInfoAfterFinishTurn(nowPlayer); //���¼�����ȴʣ��غ���
                }
                else //�������������Ƴٻغ�
                {
                    delayPlayerTurn = false;
                    nowPlayer.info.actionPoint -= 4; //���Ƴٻغϵõ����ж�������ȥ
                    participants.Add(nowPlayer); //���Ƴٽ�ɫ��ӵ������߶���ĩβ
                    UITurnManager.Instance.UpdateTurnPortraitAfterDelay(); //���»غ���ͷ���б�UI
                }
            }

            //foreach (Character participant in participants.ToList())
            //{
            //    Debug.Log($"{participant.info.name}�Ļغ�");

            //    if(participant.info.hp <= 0)
            //    {
            //        continue;
            //    }

            //    nowPlayer = participant;

            //    participant.info.actionPoint = Mathf.Min(participant.info.actionPoint + 4, 6); //�ָ��ж�����
            //    Debug.Log($"{participant.info.name} actionPoint {participant.info.actionPoint}");

            //    if (participant.CompareTag("Player"))
            //    {
            //        UITurnManager.Instance.ShowTurnPlayerPanel(); //��ʾ��һغ����
            //        yield return HandlePlayerTurn(participant); //�����������
            //    }
            //    else if (participant.CompareTag("Enemy"))
            //    {
            //        UITurnManager.Instance.HideTurnPlayerPanel(); //������һغ����
            //        yield return HandleEnemyTurn(participant); //�������AI
            //    }

            //    participant.info.actionPoint = Mathf.Max(Mathf.Floor(participant.info.actionPoint), 0); //�����ж�����
            //    Debug.Log($"{participant.info.name} actionPoint {participant.info.actionPoint}");

            //    if (!CheckCombatContinueCondition())
            //    {
            //        break;
            //    }

            //    UITurnManager.Instance.UpdateTurnPortrait(nowPlayer); //���»غ���ͷ���б�UI
            //}

            participants = nextTurnParticipants.ToList();
            Debug.Log($"��{CurrentRound}�غϽ���");
        }
        EndTurn();
    }

    /// <summary>
    /// ���غ���ս���Ƿ����
    /// </summary>
    /// <returns></returns>
    public bool CheckCombatContinueCondition()
    {
        bool playersAlive = participants.Any(p => p.CompareTag("Player") && p.info.hp > 0);
        bool enemiesAlive = participants.Any(p => p.CompareTag("Enemy") && p.info.hp > 0);
        //����AI���ý���غ���ʱ, ��������Ϊ��Ҷ���һ�� �� ����һ�� ȫ������
        //����ֶ����ý���غ���ʱ, ��������Ϊ��Ҷ���һ��ȫ������ �� ����ֶ�����EndCombat()����
        return playersAlive && (enemiesAlive || isManualTurnStart) && isInTurn;
    }

    /// <summary>
    /// �²����߼���(���ٻ����)
    /// </summary>
    /// <param name="newParticipants"></param>
    public IEnumerator AddParticipant(List<Character> newParticipants)
    {
        //���²����߼�����һ�ֻغϵĽ�ɫ�б���
        foreach (Character character in newParticipants)
        {
            nextTurnParticipants.Add(character);

            character.isInTurn = true;

            CoroutineManager.Instance.StopGroup(character.info.name); //ֹͣ��ɫ���ϵ�Э�̶���

            CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.CorrectCharacterPositionInTurn(triggers), character.info.name);
            CoroutineManager.Instance.BindCallback(character.info.name, () =>
            {
                character.animator.SetBool("combat", true); //��ս����
            });
            CoroutineManager.Instance.StartGroup(character.info.name);

            character.info.actionPoint = 0; //��ʼ���ж�����AP

            //�����в����߽��µ���������ΪdynamicLimit
            character.nowGrid.info.dynamicLimit = true;
            character.nowGrid.info.walkable = false;

            //������ҽ�ɫ�͵з���ɫ�ĸ����б�, �����ƶ������еĽ�������ж�
            if (character.CompareTag("Player"))
            {
                playerGrids.Add(character.nowGrid, character);
            }
            else if (character.CompareTag("Enemy"))
            {
                enemyGrids.Add(character.nowGrid, character);
            }

            //�غϿ�ʼʱ, ��ÿλ��ɫ���ϵ�Buff�Ӽ�ʱ�Ƽ�ʱתΪ�غ��Ƽ�ʱ
            BuffManager.Instance.UpdateBuffAfterStartTurn(character);
            //�غϿ�ʼʱ, ��ÿλ��ɫ���ϵ�Skill�Ӽ�ʱ�Ƽ�ʱתΪ�غ��Ƽ�ʱ
            SkillManager.Instance.UpdateSkillBarUIAfterStartTurn(character);
        }
        //����һ�ֻغϵĽ�ɫ�б����ȹ�ֵ��������
        nextTurnParticipants.Sort(new CharacterInitiativeComparer());
        //���»غ���ͷ���б�UI
        yield return UITurnManager.Instance.ResortNextTurnParticipantsPortraits();
    }

    /// <summary>
    /// �Ƴ�������(���ɫ������)
    /// </summary>
    /// <param name="participant"></param>
    public void RemoveParticipant(Character participant)
    {
        //if(participant == nowPlayer)
        //{
        //    nowPlayerRemoved = true;
        //}
        //participantRemoved = true;

        //participants.Remove(participant); �Ȳ��Ƴ����غϵĸý�ɫ, ������Ϊɾ������������ɫ�±�ı仯
        nextTurnParticipants.Remove(participant);
        if (participant.CompareTag("Player"))
        {
            playerGrids.Remove(participant.nowGrid);
        }
        else if (participant.CompareTag("Enemy"))
        {
            enemyGrids.Remove(participant.nowGrid);
        }
        //���»غ���ͷ���б�UI
        UITurnManager.Instance.RemoveParticipantPortrait(participant);
    }


    #region ��ҿ��ƽ�ɫ�غ�
    /// <summary>
    /// ������һغ�
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private IEnumerator HandlePlayerTurn(Character player)
    {
        //nowPlayer = player; //���»غ����еĲٿ���ҽ�ɫ
        PartyManager.Instance.SwitchLeader(player);
        
        //���غ��Ƽ����������� && �غ��ж�����>0 && û���ֶ������غ�ʱ �غ�ѭ����һֱͣ���ڸý�ɫ�Ļغ�
        while (CheckCombatContinueCondition() && player.info.actionPoint > 0 && player.info.hp > 0 && !endPlayerTurn && !delayPlayerTurn)
        {
            yield return HandlePlayerInput(player); //�ȴ�1��
        }
        endPlayerTurn = false;

        CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.ReturnGridsToPool(), player.info.name + "_pathfinding");
        CoroutineManager.Instance.StartGroup(player.info.name + "_pathfinding");
        yield return new WaitWhile(() => !CoroutineManager.Instance.TaskInGroupIsEmpty(player.info.name + "_pathfinding"));
    }

    //��һغ�, ���λ�����߼��, �����ƶ�·��, ����ap�����ĸ�����ʾΪ��ɫ, ������������ĸ�����ʾΪս��ͼ�� FindPathInTurn
    //���ƺü���Э�̵ȴ�ʱ��, ������Ϊ���Ƶ���ƶ�λ�ö�����̫������

    /// <summary>
    /// �����������
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private IEnumerator HandlePlayerInput(Character player)
    {
        yield return null;

        if (player != PartyManager.Instance.leader || player.isSkillTargeting)
        {
            StartCoroutine(player.pathfinder.ReturnGridsToPool());
            yield return new WaitUntil(() => player == PartyManager.Instance.leader && !player.isSkillTargeting); //�����ǰ����ִ����ٿصĽ�ɫ��������, ��������ʩ��, ��һֱ�ȴ�, ֱ����Ϊ���غ�ʩ�����
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Character attackTarget = null;
        GridHelper targetGrid = null;
        //����·������
        if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character", "Model")) 
            && !player.isMoving && !player.isAttacking && !player.isSkillTargeting) //if (isInTurn && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character")))
        {
            //if (!player.isMoving && !player.isAttacking && !player.isSkillTargeting) //��ɫ���ƶ����ǹ�������Ԥʩ��ѡ��Ŀ���ڼ�
            //{
                attackTarget = hit.collider.GetComponent<Character>();
                targetGrid = hit.collider.GetComponent<GridHelper>();
                if (attackTarget != null && attackTarget != player) //��⵽��ɫ
                {
                    //Debug.Log("raycast character");

                    isMouseOverCharacter = true;
                    isMouseOverGrid = false;
                    CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.FindAttackPath(player.nowGrid, attackTarget.nowGrid, player.info.attackDistance), player.info.name + "_pathfinding");
                    CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.DrawPathGrids((cost) => apTakes = cost), player.info.name + "_pathfinding");
                    UITurnManager.Instance.UpdateActionPointBalls(player.info.actionPoint, apTakes + 2);
                }
                else if (targetGrid != null) //��⵽����
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
        else //��û�м�⵽
        {
            //Debug.Log("raycast nothing");

            isMouseOverCharacter = false;
            isMouseOverGrid = false;
            CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.ReturnGridsToPool(), player.info.name + "_pathfinding");
            UITurnManager.Instance.UpdateActionPointBalls(player.info.actionPoint, 0);
        }

        if (!Input.GetKey(KeyCode.LeftAlt) && !EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0)) //��������
        {
            if (isMouseOverCharacter) //�����������ǽ�ɫ, �����ƶ�, �ٹ���
            {
                CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.CharacterMoveInTurn(player.info.runSpeed, player.info.rotateSpeed, 1), player.info.name);
                CoroutineManager.Instance.AddTaskToGroup(player.Attack(attackTarget), player.info.name);
            }
            else if (isMouseOverGrid) //����������������, ���ƶ�
            {
                CoroutineManager.Instance.AddTaskToGroup(player.pathfinder.CharacterMoveInTurn(player.info.runSpeed, player.info.rotateSpeed, 1), player.info.name);
            }
            CoroutineManager.Instance.StartGroup(player.info.name);
        }

        CoroutineManager.Instance.StartGroup(player.info.name + "_pathfinding");
        //yield return new WaitWhile(() => !CoroutineManager.Instance.TaskInGroupIsEmpty(player.info.name));
    }
    #endregion


    #region ����AI�غ�
    /// <summary>
    /// �������AI�غ�
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    private IEnumerator HandleAITurn(Character character)
    {
        UIPartyManager.Instance.UpdateHighlight(character);
        CameraMoveManager.Instance.ChangeToNewLeader(character); //����ͷƽ���ƶ�����ǰ�غϽ�ɫ����
        CharactertAI charactertAI = character.GetComponent<CharactertAI>(); //���AI��ɫ���Ϲ��ص�CharacterAI�ű�
        if (charactertAI != null)
        {
            yield return charactertAI.HandleAIBehavior();
        }
        yield return new WaitForSeconds(1.0f);
    }
    #endregion

}
