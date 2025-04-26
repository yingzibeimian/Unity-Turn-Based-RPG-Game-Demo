using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.Text;

public class SkillManager : MonoBehaviour
{
    private static SkillManager instance;
    public static SkillManager Instance => instance;

    private List<LineRenderer> maxDistanceEdgeGridsLineRenderers = new List<LineRenderer>(); //������Ⱦ�������ʩ����Χ��Ե��LineRenderer�б�
    private List<LineRenderer> skillTargetEdgeGridsLineRenderers = new List<LineRenderer>(); //������Ⱦ����ʩ��Ŀ�귶Χ��Ե��LindRenderer�б�
    public Transform linesParent; //·�������ߵĸ�����
    private Material outlineMaterial; //�����������ı������
    private Material skillMaxDistanceMaterial; //���ʩ����Χ�����߲���
    private Material skillTargetMaterial; //����Ŀ�귶Χ�����߲���

    public int maxNodesProcessedPerFrame = 100; //ÿ֡��ദ��ڵ�
    private WaitForSeconds oneTurnTime = new WaitForSeconds(6.0f); //�ȴ�6s

    void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        //��ʼ���������
        outlineMaterial = Resources.Load<Material>("Materials/outlineRendererMaterial");
        skillMaxDistanceMaterial = Resources.Load<Material>("Materials/skillMaxDistanceMaterial");
        skillTargetMaterial = Resources.Load<Material>("Materials/skillTargetMaterial");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// �����, ����Ԥʩ���׶�
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    public void ActivateSkill(Character character, SkillBaseData skill)
    {
        //���ʣ��غ�����δ��ȴ��� �� �����ƶ���������ѡ����Ŀ��, �ͷ���
        if (skill.remainingTurns > 0 || (character.isInTurn && character.info.actionPoint < skill.actionPointTakes)
            || character.isMoving || character.isAttacking || character.isSkillTargeting)
        {
            return;
        }
        Debug.Log("ActicateSkill");
        character.isSkillTargeting = true; //��ǽ�ɫ����ѡ����Ŀ����
        StartCoroutine(ProcessSkillTargeting(character, skill)); //����ѡ����Ŀ���Э��
    }

    /// <summary>
    /// Ԥʩ���׶�, ������Ŀ��ѡ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    private IEnumerator ProcessSkillTargeting(Character character, SkillBaseData skill)
    {
        if (skill is EffectApplicationSkillData)
        {
            yield return ProcessEffectApplicationSkillTargeting(character, skill as EffectApplicationSkillData); //����Ч��ʩ�Ӽ��ܵ�Ŀ��ѡ��
        }
        else if (skill is SummonSkillData)
        {
            yield return ProcessSummonSkillDataTargeting(character, skill as SummonSkillData); //�����ٻ����ܵ�Ŀ��ѡ��
        }
    }

    /// <summary>
    /// ����Ч��ʩ�Ӽ��ܵ�Ŀ��ѡ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    private IEnumerator ProcessEffectApplicationSkillTargeting(Character character, EffectApplicationSkillData skill)
    {
        Debug.Log("ProcessEffectApplicationSkillTargeting");
        //�������ʩ��ê���ǿ�����ѡ���Ϊ���ĵ�, �Ͷ�����ͷž���ı�Ե�����߽�����Ⱦ
        if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered)
        {
            HashSet<GridHelper> maxDistanceGrids = new HashSet<GridHelper>();
            CoroutineManager.Instance.AddTaskToGroup(GetSkillMaxDistanceAreaGrids(character, skill, maxDistanceGrids), character.info.name + "_skillTargeting");
            CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(maxDistanceGrids, maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
            CoroutineManager.Instance.StartGroup(character.info.name + "_skillTargeting");
            yield return new WaitUntil(() => CoroutineManager.Instance.TaskInGroupIsEmpty(character.info.name + "_skillTargeting"));
        }

        bool skillTargeting = true; //��������Ƿ�����ѡ����Ŀ��
        while (skillTargeting)
        {
            yield return null;

            if (character != PartyManager.Instance.leader)
            {
                StartCoroutine(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers)); //��������Ⱦ�������ʩ����Χ��Ե��LineRenderer�����������
                StartCoroutine(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers)); //��������Ⱦ����ʩ��Ŀ�귶Χ��Ե��LindRenderer���ظ������
                yield return new WaitUntil(() => character == PartyManager.Instance.leader); //�����ǰ����ִ����ٿصĽ�ɫ��������, ��һֱ�ȴ�, ֱ����Ϊ����
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Character targetCharacter = null;
            GridHelper targetGrid = null;
            //���Ƽ��ܷ�Χ����
            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character", "Model"))
                && !character.isMoving && !character.isAttacking) //if (isInTurn && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character")))
            {
                targetCharacter = hit.collider.GetComponent<Character>();
                targetGrid = hit.collider.GetComponent<GridHelper>();
                //���弼�� SingleTarget
                if (skill.effectArea.targetingPattern == EffectTargetingPattern.SingleTarget)
                {
                    //�����⵽��ɫ ���� ʩ���ߺ�Ŀ���ɫ�ľ��� С�ڵ��� ���ʩ������
                    if (targetCharacter != null && GetDistance(character.nowGrid, targetCharacter.nowGrid) <= skill.effectArea.maxDistance)
                    {
                        CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(new HashSet<GridHelper>() { targetCharacter.nowGrid }, skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                        UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, skill.actionPointTakes);
                    }
                    //���û�м�⵽��ɫ
                    else
                    {
                        CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    }
                }
                //AOE���� CircleAOE ConeAOE LineAOE
                else
                {
                    if (targetCharacter != null) //��⵽��ɫ
                    {
                        targetGrid = targetCharacter.nowGrid;
                    }
                    //������ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�, Ŀ������Ϊ��, ���� ʩ���ߺ�Ŀ���ɫ�ľ��� С�ڵ��� ���ʩ������
                    //if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered && targetGrid != null && getDistance(character.nowGrid, targetGrid) <= skill.effectArea.maxDistance)
                    //{
                    //    HashSet<GridHelper> skillTargetGrids = new HashSet<GridHelper>();
                    //    CoroutineManager.Instance.AddTaskToGroup(GetSkillEffectsAreaGrids(character, skill, targetGrid, skillTargetGrids), character.info.name + "_skillTargeting");
                    //    CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(skillTargetGrids, skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    //    UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, skill.actionPointTakes);
                    //}
                    ////������ܵ�ʩ��ê����������Ϊ���ĵ�
                    //else if (skill.effectArea.effectAnchor == EffectAnchorMode.selfCentered && targetGrid != null)
                    //{
                    //    HashSet<GridHelper> skillTargetGrids = new HashSet<GridHelper>();
                    //    CoroutineManager.Instance.AddTaskToGroup(GetSkillEffectsAreaGrids(character, skill, targetGrid, skillTargetGrids), character.info.name + "_skillTargeting");
                    //    CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(skillTargetGrids, skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    //    UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, skill.actionPointTakes);
                    //}

                    //������ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�, Ŀ������Ϊ��, ���� ʩ���ߺ�Ŀ���ɫ�ľ��� С�ڵ��� ���ʩ������
                    //���� ������ܵ�ʩ��ê����������Ϊ���ĵ�, ��Ŀ������Ϊ��
                    //�ͻ��Ƽ��ܷ�Χ����
                    if ((skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered && targetGrid != null && GetDistance(character.nowGrid, targetGrid) <= skill.effectArea.maxDistance)
                        || (skill.effectArea.effectAnchor == EffectAnchorMode.selfCentered && targetGrid != null))
                    {
                        HashSet<GridHelper> skillTargetGrids = new HashSet<GridHelper>();
                        CoroutineManager.Instance.AddTaskToGroup(GetSkillEffectsAreaGrids(character, skill, targetGrid, skillTargetGrids), character.info.name + "_skillTargeting");
                        CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(skillTargetGrids, skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                        UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, skill.actionPointTakes);
                    }
                }
            }
            else //��ɫ������û�м�⵽
            {
                //������ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�, �ͽ�������Ⱦ����ʩ��Ŀ�귶Χ��Ե��LindRenderer���ظ������
                if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered)
                {
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                }
            }

            if (!Input.GetKey(KeyCode.LeftAlt) && !EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0)) //��������
            {
                bool applySkillEffects = false; //���ڱ�����������Ƿ񴥷� �Լ���Ч����Ӧ��

                //���弼�� SingleTarget
                if (skill.effectArea.targetingPattern == EffectTargetingPattern.SingleTarget)
                {
                    //���Ŀ���ɫ��Ϊ��
                    if(targetCharacter != null && GetDistance(character.nowGrid, targetCharacter.nowGrid) <= skill.effectArea.maxDistance)
                    {
                        CoroutineManager.Instance.AddTaskToGroup(ApplyEffectApplicationSkillEffects(character, skill, targetCharacter), character.info.name);
                        applySkillEffects = true;
                    }
                }
                //AOE���� CircleAOE ConeAOE LineAOE
                else
                {
                    //���Ŀ������Ϊ��
                    //if (targetGrid != null)
                    //{
                    //    CoroutineManager.Instance.AddTaskToGroup(ApplySkillEffects(character, skill, targetGrid), character.info.name);
                    //    applySkillEffects = true;
                    //}

                    //������ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�, Ŀ������Ϊ��, ���� ʩ���ߺ�Ŀ���ɫ�ľ��� С�ڵ��� ���ʩ������
                    //���� ������ܵ�ʩ��ê����������Ϊ���ĵ�, ��Ŀ������Ϊ��
                    //��Ӧ�ü���Ч��
                    if ((skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered && targetGrid != null && GetDistance(character.nowGrid, targetGrid) <= skill.effectArea.maxDistance)
                        || (skill.effectArea.effectAnchor == EffectAnchorMode.selfCentered && targetGrid != null))
                    {
                        CoroutineManager.Instance.AddTaskToGroup(ApplyEffectApplicationSkillEffects(character, skill, targetGrid), character.info.name);
                        applySkillEffects = true;
                    }
                }

                //�����ε�������Լ���Ч����Ӧ��, ������ѭ��, ֹͣ��Ⱦ���ܷ�Χ����
                if (applySkillEffects)
                {
                    skillTargeting = false; //ֹͣѡ����Ŀ��

                    //��������Ⱦ�������ʩ����Χ��Ե��LineRenderer�����������
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    //��������Ⱦ����ʩ��Ŀ�귶Χ��Ե��LindRenderer���ظ������
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");

                    CoroutineManager.Instance.StartGroup(character.info.name); //ִ�н�ɫЭ�̶���, Ӧ�ü���Ч��
                }
            }

            //�������ESC��, ������ѭ��, ֹͣ�ͷż���
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                skillTargeting = false; //ֹͣѡ����Ŀ��
                character.isSkillTargeting = false;

                //��������Ⱦ�������ʩ����Χ��Ե��LineRenderer�����������
                CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                //��������Ⱦ����ʩ��Ŀ�귶Χ��Ե��LindRenderer���ظ������
                CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                Debug.Log("StopSkillTargeting");
            }

            CoroutineManager.Instance.StartGroup(character.info.name + "_skillTargeting");
        }
    }

    /// <summary>
    /// �����ٻ����ܵ�Ŀ��ѡ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    private IEnumerator ProcessSummonSkillDataTargeting(Character character, SummonSkillData skill)
    {
        Debug.Log("ProcessSummonSkillDataTargeting");
        //�������ʩ��ê���ǿ�����ѡ���Ϊ���ĵ�
        //�������ʩ��ê���ǿ�����ѡ���Ϊ���ĵ�, �Ͷ�����ͷž���ı�Ե�����߽�����Ⱦ
        if (skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
        {
            HashSet<GridHelper> maxDistanceGrids = new HashSet<GridHelper>();
            CoroutineManager.Instance.AddTaskToGroup(GetSkillMaxDistanceAreaGrids(character, skill, maxDistanceGrids), character.info.name + "_skillTargeting");
            CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(maxDistanceGrids, maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
            CoroutineManager.Instance.StartGroup(character.info.name + "_skillTargeting");
            yield return new WaitUntil(() => CoroutineManager.Instance.TaskInGroupIsEmpty(character.info.name + "_skillTargeting"));
        }

        bool skillTargeting = true; //��������Ƿ�����ѡ����Ŀ��
        while (skillTargeting)
        {
            yield return null;

            if (character != PartyManager.Instance.leader)
            {
                StartCoroutine(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers)); //��������Ⱦ�������ʩ����Χ��Ե��LineRenderer�����������
                StartCoroutine(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers)); //��������Ⱦ����ʩ��Ŀ�귶Χ��Ե��LindRenderer���ظ������
                yield return new WaitUntil(() => character == PartyManager.Instance.leader); //�����ǰ����ִ����ٿصĽ�ɫ��������, ��һֱ�ȴ�, ֱ����Ϊ����
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Character targetCharacter = null;
            GridHelper targetGrid = null;
            //���Ƽ��ܷ�Χ����
            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character", "Model"))
                && !character.isMoving && !character.isAttacking) //if (isInTurn && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character")))
            {
                targetCharacter = hit.collider.GetComponent<Character>();
                targetGrid = hit.collider.GetComponent<GridHelper>();
                //���ܵ�ʩ��ê����������Ϊ���ĵ�, ��⵽��ɫ, �͵õ�targetGrid, ����������Ⱦ���ܷ�Χ����; ���ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�, ��⵽��ɫ, �Ͳ���Ⱦ���ܷ�Χ����
                if (skill.summonArea.effectAnchor == EffectAnchorMode.selfCentered && targetCharacter != null)
                {
                    targetGrid = targetCharacter.nowGrid;
                }

                //������ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�, Ŀ������Ϊ��, ���� ʩ���ߺ�Ŀ���ɫ�ľ��� С�ڵ��� ���ʩ������
                //���� ������ܵ�ʩ��ê����������Ϊ���ĵ�, ��Ŀ������Ϊ��
                //�ͻ��Ƽ��ܷ�Χ����
                if ((skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered && targetGrid != null && GetDistance(character.nowGrid, targetGrid) <= skill.summonArea.maxDistance)
                    || (skill.summonArea.effectAnchor == EffectAnchorMode.selfCentered && targetGrid != null))
                {
                    HashSet<GridHelper> skillTargetGrids = new HashSet<GridHelper>();
                    CoroutineManager.Instance.AddTaskToGroup(GetSkillEffectsAreaGrids(character, skill, targetGrid, skillTargetGrids), character.info.name + "_skillTargeting");
                    CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(skillTargetGrids, skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, skill.actionPointTakes);
                }
            }
            else //��ɫ������û�м�⵽
            {
                //������ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�, �ͽ�������Ⱦ����ʩ��Ŀ�귶Χ��Ե��LindRenderer���ظ������
                if (skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
                {
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                }
            }

            if (!Input.GetKey(KeyCode.LeftAlt) && !EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0)) //��������
            {
                bool applySkillEffects = false; //���ڱ�����������Ƿ񴥷� �Լ���Ч����Ӧ��

                //������ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�, Ŀ������Ϊ��, ���� ʩ���ߺ�Ŀ���ɫ�ľ��� С�ڵ��� ���ʩ������
                //���� ������ܵ�ʩ��ê����������Ϊ���ĵ�, ��Ŀ������Ϊ��
                //��Ӧ�ü���Ч��
                if ((skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered && targetGrid != null && GetDistance(character.nowGrid, targetGrid) <= skill.summonArea.maxDistance)
                    || (skill.summonArea.effectAnchor == EffectAnchorMode.selfCentered && targetGrid != null))
                {
                    CoroutineManager.Instance.AddTaskToGroup(ApplySummonSkillEffects(character, skill, targetGrid), character.info.name);
                    applySkillEffects = true;
                }

                //�����ε�������Լ���Ч����Ӧ��, ������ѭ��, ֹͣ��Ⱦ���ܷ�Χ����
                if (applySkillEffects)
                {
                    skillTargeting = false; //ֹͣѡ����Ŀ��

                    //��������Ⱦ�������ʩ����Χ��Ե��LineRenderer�����������
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    //��������Ⱦ����ʩ��Ŀ�귶Χ��Ե��LindRenderer���ظ������
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");

                    CoroutineManager.Instance.StartGroup(character.info.name); //ִ�н�ɫЭ�̶���, Ӧ�ü���Ч��
                }
            }

            //�������ESC��, ������ѭ��, ֹͣ�ͷż���
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                skillTargeting = false; //ֹͣѡ����Ŀ��
                character.isSkillTargeting = false;

                //��������Ⱦ�������ʩ����Χ��Ե��LineRenderer�����������
                CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                //��������Ⱦ����ʩ��Ŀ�귶Χ��Ե��LindRenderer���ظ������
                CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                Debug.Log("StopSkillTargeting");
            }

            CoroutineManager.Instance.StartGroup(character.info.name + "_skillTargeting");
        }
    }

    /// <summary>
    /// ����Ԥʩ��, ��targetCharacter���ϴ���Ч���༼���˺���Ч����Ӧ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="targetCharacter"></param>
    /// <returns></returns>
    public IEnumerator ApplyEffectApplicationSkillEffects(Character character, EffectApplicationSkillData skill, Character targetCharacter)
    {
        Debug.Log($"{character.info.name} ApplyEffectApplicationSkillEffects {skill.skillName} to {targetCharacter.info.name}");
        //�����ɫ�ڻغ������ҵ�ǰ�ж��������� �� ��ɫ������ ��ֱ�ӷ���
        //if ((character.isInTurn && character.info.actionPoint < skill.actionPointTakes) || character.info.hp <= 0)
        //{
        //    yield break;
        //}

        //�������Ӱ���ɫ���� �� targetCharacter������, ��ֱ�ӷ���
        if ((!skill.effectArea.affectSelf && targetCharacter == character) //���ܲ�Ӱ��ʩ�������� ��targetCharacter����ʩ����
            || (!skill.effectArea.affectSelfCamp && targetCharacter.tag == character.tag && targetCharacter != character) //���ܲ���Ӱ����ͬ��Ӫ��ɫ ��targetCharacter��ʩ����ͬһ��Ӫ, �Ҳ���ʩ��������
            || (!skill.effectArea.affectOppositeCamp && targetCharacter.tag != character.tag)) //���ܲ�Ӱ��з���Ӫ��ɫ ��targetCharacter��ʩ���߲�ͬ��Ӫ
        {
            character.isSkillTargeting = false; //�������ͷű�Ǹ�Ϊfalse
            yield break;
        }
        //�������Ӱ���ɫ���� �� targetCharacter����, �ͼ���ִ��, Ӧ�ü���Ч��

        //�������Ŀ�겻��ʩ��������, �͸��½�ɫ����
        if(targetCharacter != character)
        {
            Vector3 direction = (targetCharacter.transform.position - character.transform.position).normalized;
            direction.y = 0; //���ı��ɫy������
            Quaternion startRotation = character.transform.rotation; //��ǰ����
            Quaternion endRotation = Quaternion.LookRotation(direction); //�յ㳯��
            float rotateWeight = 0;
            while (Quaternion.Angle(character.transform.rotation, endRotation) > 0.5f)
            {
                rotateWeight += Time.deltaTime * character.info.rotateSpeed;
                character.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
                yield return null;
            }
            character.transform.rotation = endRotation;
        }

        //����λ��
        if (skill.shouldMoveToTarget)
        {
            character.transform.position = targetCharacter.transform.position;
        }

        //�����������˺� ���� ����Buff, ��Ӧ�ý�ɫ�ű��д������˺���BuffЧ���ķ���, ��targetCharacterʩ�Ӽ���Ч��
        if (skill.damageDiceCount > 0 || skill.applicateBuffs.Count > 0)
        {
            yield return character.AppplySkillEffectsOnTarget(skill, new HashSet<Character>() { targetCharacter });
        }

        //���¼�����ȴ��UI
        UpdateSkillInfoAfterUseSkill(character, skill);

        //�����ɫ�ڻغ�����, �ͼ�ȥ���ĵ��ж�����
        if (character.isInTurn)
        {
            character.info.actionPoint -= skill.actionPointTakes;
            UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, 0);
            Debug.Log($"actionPoint {character.info.actionPoint}");
        }

        character.isSkillTargeting = false;
        yield break;
    }

    /// <summary>
    /// ����Ԥʩ��, ��targetGridΪ���Ĵ���Ч���༼���˺���Ч����Ӧ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="targetGrid"></param>
    /// <returns></returns>
    public IEnumerator ApplyEffectApplicationSkillEffects(Character character, EffectApplicationSkillData skill, GridHelper targetGrid)
    {
        Debug.Log($"{character.info.name} ApplyEffectApplicationSkillEffects {skill.skillName} to grid{targetGrid.info.q},{targetGrid.info.r}");
        //�����ɫ�ڻغ������ҵ�ǰ�ж��������� �� ��ɫ������ ��ֱ�ӷ���
        //if ((character.isInTurn && character.info.actionPoint < skill.actionPointTakes) || character.info.hp <= 0)
        //{
        //    yield break;
        //}

        //�������Ŀ�겻��ʩ��������, �͸��½�ɫ����
        if (targetGrid != character.nowGrid)
        {
            Vector3 direction = (targetGrid.transform.position - character.transform.position).normalized;
            direction.y = 0; //���ı��ɫy������
            Quaternion startRotation = character.transform.rotation; //��ǰ����
            Quaternion endRotation = Quaternion.LookRotation(direction); //�յ㳯��
            float rotateWeight = 0;
            while (Quaternion.Angle(character.transform.rotation, endRotation) > 0.5f)
            {
                rotateWeight += Time.deltaTime * character.info.rotateSpeed;
                character.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
                yield return null;
            }
            character.transform.rotation = endRotation;
        }

        //����λ��
        if (skill.shouldMoveToTarget)
        {
            GridHelper lastGrid = character.nowGrid;
            character.transform.position = targetGrid.transform.position;
            character.info.q = targetGrid.info.q;
            character.info.r = targetGrid.info.r;
            //����ڻغ�����, ���޸�playerGrids����enemyGrids
            if (character.isInTurn)
            {
                if (character.tag == "Player")
                {
                    if (TurnManager.Instance.playerGrids.ContainsKey(lastGrid))
                    {
                        TurnManager.Instance.playerGrids.Remove(lastGrid);
                    }
                    TurnManager.Instance.playerGrids.TryAdd(character.nowGrid, character);
                }
                else if (character.tag == "Enemy")
                {
                    if (TurnManager.Instance.enemyGrids.ContainsKey(lastGrid))
                    {
                        TurnManager.Instance.enemyGrids.Remove(lastGrid);
                    }
                    TurnManager.Instance.enemyGrids.TryAdd(character.nowGrid, character);
                }
            }
        }

        //�õ�����ʩ����Χ�ڵ���������
        HashSet<GridHelper> skillTargetGrids = new HashSet<GridHelper>();
        yield return GetSkillEffectsAreaGrids(character, skill, targetGrid, skillTargetGrids);

        //��⼼��Ч��Ӧ�õĽ�ɫ����
        HashSet<Character> effectsTarget = new HashSet<Character>();
        //�����ɫ�ڻغ�����
        if (character.isInTurn)
        {
            //����ʩ����Χ�ڵ���������, �������TurnManager������һ��ߵ������ڵĸ���, �ͽ������ϵĽ�ɫ����effectsTarget��
            foreach (GridHelper grid in skillTargetGrids)
            {
                if (TurnManager.Instance.playerGrids.ContainsKey(grid))
                {
                    Character targetCharacter = TurnManager.Instance.playerGrids[grid];
                    if (!effectsTarget.Contains(targetCharacter))
                    {
                        effectsTarget.Add(targetCharacter);
                    }
                }
                if (TurnManager.Instance.enemyGrids.ContainsKey(grid))
                {
                    Character targetCharacter = TurnManager.Instance.enemyGrids[grid];
                    if (!effectsTarget.Contains(targetCharacter))
                    {
                        effectsTarget.Add(targetCharacter);
                    }
                }
            }
            yield return null;
        }
        //�����ɫ���ڻغ�����
        else
        {
            //����ʩ����Χ�ڵ���������, �������Ϸ����������߼��, �����⵽��ɫ, �ͼ���effectsTarget��
            foreach (GridHelper grid in skillTargetGrids)
            {
                RaycastHit hit;
                Character targetCharacter = null;
                //���Ƽ��ܷ�Χ����
                if (Physics.Raycast(grid.transform.position + Vector3.up * 50, Vector3.down, out hit, 100, LayerMask.GetMask("Character", "Model")))
                {
                    targetCharacter = hit.collider.GetComponent<Character>();
                    if (targetCharacter != null && !effectsTarget.Contains(targetCharacter))
                    {
                        effectsTarget.Add(targetCharacter);
                    }
                }
                //yield return null;
            }
        }

        //���ݼ���Ӱ���ɫ���� ɸѡ effectsTarget
        if (effectsTarget.Contains(character) && !skill.effectArea.affectSelf)
        {
            effectsTarget.Remove(character); //������ܲ�Ӱ��ʩ�������� ��ʩ������Ӱ��Ŀ����, �ͽ������Ƴ�
        }
        foreach (Character target in effectsTarget.ToList())
        {
            if ((character.tag == target.tag && character != target && !skill.effectArea.affectSelfCamp) //���ܲ�Ӱ��ͬ��Ӫ��λ, ��ʩ���ߺ�target��ͬ��Ӫ��ɫ, ��target����ʩ��������
                || (character.tag != target.tag && !skill.effectArea.affectOppositeCamp)) //���ܲ�Ӱ��з���Ӫ��λ, ��ʩ���ߺ�target�ǲ�ͬ��Ӫ��ɫ
            {
                effectsTarget.Remove(target);
            }
            yield return null;
        }

        //�����������˺� ���� ����Buff, ��Ӧ�ý�ɫ�ű��д������˺���BuffЧ���ķ���
        if (skill.damageDiceCount > 0 || skill.applicateBuffs.Count > 0)
        {
            yield return character.AppplySkillEffectsOnTarget(skill, effectsTarget);
        }

        //���¼�����ȴ��UI
        UpdateSkillInfoAfterUseSkill(character, skill);

        //�����ɫ�ڻغ�����, �ͼ�ȥ���ĵ��ж�����
        if (character.isInTurn)
        {
            character.info.actionPoint -= skill.actionPointTakes;
            if (character.CompareTag("Player"))
            {
                UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, 0);
            }
        }

        character.isSkillTargeting = false;
        yield break;
    }

    /// <summary>
    /// ����Ԥʩ��, ��targetGridΪ���Ĵ����ٻ��༼��, ������Ӧ���ٻ���λ
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="grid"></param>
    /// <returns></returns>
    public IEnumerator ApplySummonSkillEffects(Character character, SummonSkillData skill, GridHelper targetGrid)
    {
        Debug.Log($"{character.info.name} ApplySummonSkillEffects {skill.skillName} to grid{targetGrid.info.q},{targetGrid.info.r}");

        //�������Ŀ�겻��ʩ��������, �͸��½�ɫ����
        if (targetGrid != character.nowGrid)
        {
            Vector3 direction = (targetGrid.transform.position - character.transform.position).normalized;
            direction.y = 0; //���ı��ɫy������
            Quaternion startRotation = character.transform.rotation; //��ǰ����
            Quaternion endRotation = Quaternion.LookRotation(direction); //�յ㳯��
            float rotateWeight = 0;
            while (Quaternion.Angle(character.transform.rotation, endRotation) > 0.5f)
            {
                rotateWeight += Time.deltaTime * character.info.rotateSpeed;
                character.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
                yield return null;
            }
            character.transform.rotation = endRotation;
            Debug.Log("ApplySummonSkillEffects2");
        }

        //����ٻ���λ���ڸ���
        HashSet<GridHelper> summonedUnitGrids = new HashSet<GridHelper>();
        //������ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�, �������ѡ���targetGridΪ���Ļ���ٻ���λ����
        if (skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
        {
            yield return InitializeSummonedUnitsGrids(character, skill, targetGrid, summonedUnitGrids);
        }
        //������ܵ�ʩ��ê����������Ϊ���ĵ�, �����ٻ������ڸ���Ϊ���Ļ���ٻ���λ����
        else
        {
            yield return InitializeSummonedUnitsGrids(character, skill, character.nowGrid, summonedUnitGrids);
        }
        Debug.Log("ApplySummonSkillEffects3");
        //Ӧ�ý�ɫ�ű��д����ٻ���ķ���
        yield return character.CreatSummonedUnitOnTargetGrids(skill, summonedUnitGrids);

        //���¼�����ȴ��UI
        UpdateSkillInfoAfterUseSkill(character, skill);

        //�����ɫ�ڻغ�����, �ͼ�ȥ���ĵ��ж�����
        if (character.isInTurn)
        {
            character.info.actionPoint -= skill.actionPointTakes;
            UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, 0);
            Debug.Log($"actionPoint {character.info.actionPoint}");
        }

        character.isSkillTargeting = false;
        yield break;
    }


    /// <summary>
    /// ϰ�ü���, �����¶�Ӧ��ɫSkillBar��UI
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    public bool LearnSkill(Character character, SkillBaseData skill)
    {
        //�����ɫcharacter�Ѿ�ϰ�ü���skill, ��ֱ�ӷ���
        if(character.characterSkills.Any(characterSkill => characterSkill.skillName == skill.skillName))
        {
            return false;
        }
        //���Ҫѧϰ�ļ���
        SkillBaseData copy = Instantiate(skill);
        skill = copy;
        //�����б������skill
        character.characterSkills.Add(skill);

        //����SkillBarUI
        UISkillBarManager.Instance.AddSkillIconToBar(character, skill);
        return true;
    }

    /// <summary>
    /// �Ƴ�����, �����¶�Ӧ��ɫSkillBar��UI
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    public void RemoveSkill(Character character, SkillBaseData skill)
    {
        if (!character.characterSkills.Contains(skill))
        {
            skill = character.characterSkills.Where(characterSkill => characterSkill.skillName == skill.skillName).LastOrDefault();
            if (!character.characterSkills.Contains(skill))
            {
                //���character.characterSkills����Ȼû����ͬskillʵ��, �򷵻�
                return;
            }
        }
        character.characterSkills.Remove(skill);

        //����SkillBarUI
        UISkillBarManager.Instance.RemoveSkillIconFromBar(skill);
    }

    /// <summary>
    /// �õ���centerGridΪ����, radiusΪ�뾶��Բ���ڵ���������, ���뵽grids��
    /// </summary>
    /// <param name="centerGrid"></param>
    /// <param name="radius"></param>
    /// <param name="grids"></param>
    /// <returns></returns>
    private IEnumerator GetCircleAreaGrids(GridHelper centerGrid, int radius, HashSet<GridHelper> grids, bool includeStartGrid)
    {
        Queue<Vector2Int> openSet = new Queue<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Vector2Int startPos = new Vector2Int(centerGrid.info.q, centerGrid.info.r);
        openSet.Enqueue(startPos);

        int nodesProcessedThisFrame = 0;
        //bfs��������
        Vector2Int[] dirs = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1)};
        //��centerGrid��ʼ, �԰뾶radius��Բ�����ڵ�Vector2 (q, r)���� ����bfs����
        int currentRadius = 0;
        while (currentRadius <= radius)
        {
            int count = openSet.Count;
            //�����뾶currentRadius���ڵ�����(q, r)����
            for (int i = 0; i < count; i++)
            {
                Vector2Int current = openSet.Dequeue();
                closedSet.Add(current);

                foreach (Vector2Int dir in dirs)
                {
                    Vector2Int neighbor = current + dir; //�ھ������(q, r)����
                    if (closedSet.Contains(neighbor)) //����Ѿ�������������, ������
                    {
                        continue;
                    }
                    openSet.Enqueue(neighbor);
                }
            }

            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
            {
                yield return null; //ÿ֡����һ����, �������, ��һ֡��������
                nodesProcessedThisFrame = 0;
            }

            currentRadius++;
        }
        //������񲻰�����ʼλ��, ���Ƴ�
        if (!includeStartGrid)
        {
            closedSet.Remove(startPos);
        }
        //���closedSet (q, r)����, ȷ�϶�ӦGridHelper�Ƿ����
        foreach(Vector3Int pos in closedSet)
        {
            GridHelper grid = GridMap.Instance.SearchGrid(pos.x, pos.y, 0);
            if (grid != null && !grids.Contains(grid))
            {
                grids.Add(grid); //�����������(q,r,s)��Ӧ������, �ͼ���grids
            }
        }
        yield break;
    }

    /// <summary>
    /// �õ���startGridΪ���, targetGridΪĿ�귽��, radiusΪ�뾶�������ڵ���������, ���뵽grids��
    /// </summary>
    /// <param name="startGrid"></param>
    /// <param name="targetDirection"></param>
    /// <param name="radius"></param>
    /// <param name="grids"></param>
    /// <param name="includeStartGrid"></param>
    /// <returns></returns>
    private IEnumerator GetConeAreaGrids(GridHelper startGrid, Vector2Int targetDirection, int radius, HashSet<GridHelper> grids, bool includeStartGrid)
    {
        Vector2Int dir = new Vector2Int(-1, 0); //����ʱ�ĺ�����������
        if (targetDirection == new Vector2Int(0, 1))
        {
            dir = new Vector2Int(-1, 0);
        }
        else if (targetDirection == new Vector2Int(-1, 1))
        {
            dir = new Vector2Int(0, -1);
        }
        else if (targetDirection == new Vector2Int(-1, 0))
        {
            dir = new Vector2Int(1, -1);
        }
        else if (targetDirection == new Vector2Int(0, -1))
        {
            dir = new Vector2Int(1, 0);
        }
        else if (targetDirection == new Vector2Int(1, -1))
        {
            dir = new Vector2Int(0, 1);
        }
        else if (targetDirection == new Vector2Int(1, 0))
        {
            dir = new Vector2Int(-1, 1);
        }
        else
        {
            yield break;
        }

        Queue<Vector2Int> openSet = new Queue<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Vector2Int startPos = new Vector2Int(startGrid.info.q, startGrid.info.r);
        openSet.Enqueue(startPos);

        int nodesProcessedThisFrame = 0;
        //��startGrid��ʼ, �԰뾶radius���������ڵ�Vector2 (q, r)���� ���б���
        int currentRadius = 0;
        while (currentRadius <= radius)
        {
            Vector2Int current = openSet.Dequeue();
            closedSet.Add(current);
            openSet.Enqueue(current + targetDirection);

            //������current��ʼ, ��dirΪ�����ͬһ�е�����(q, r)����
            for (int i = 0; i < currentRadius; i++)
            {
                Vector2Int oneRowGrid = current + dir * (i + 1); //ͬһ�������(q, r)����
                if (closedSet.Contains(oneRowGrid)) //����Ѿ�������������, ������
                {
                    continue;
                }
                closedSet.Add(oneRowGrid);
            }
            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
            {
                yield return null; //ÿ֡����һ����, �������, ��һ֡��������
                nodesProcessedThisFrame = 0;
            }

            currentRadius++;
        }
        //������񲻰�����ʼλ��, ���Ƴ�
        if (!includeStartGrid)
        {
            closedSet.Remove(startPos);
        }
        //���closedSet (q, r)����, ȷ�϶�ӦGridHelper�Ƿ����
        foreach (Vector3Int pos in closedSet)
        {
            GridHelper grid = GridMap.Instance.SearchGrid(pos.x, pos.y, 0);
            if (grid != null && !grids.Contains(grid))
            {
                grids.Add(grid); //�����������(q,r,s)��Ӧ������, �ͼ���grids
            }
        }

        yield break;
    }

    /// <summary>
    /// �õ���startGridΪ���, targetGridΪĿ�귽��, lengthΪ����, widthΪ��ȵ�ֱ���ϵ���������, ���뵽grids��
    /// </summary>
    /// <param name="startGrid"></param>
    /// <param name="targetDirection"></param>
    /// <param name="length"></param>
    /// <param name="width"></param>
    /// <param name="grids"></param>
    /// <param name="includeStartGrid"></param>
    /// <returns></returns>
    private IEnumerator GetLineAreaGrids(GridHelper startGrid, Vector2Int targetDirection, int length, int width, HashSet<GridHelper> grids, bool includeStartGrid)
    {
        Vector2Int dir = new Vector2Int(-1, 0); //����ʱ�ĺ�����������
        if (targetDirection == new Vector2Int(0, 1))
        {
            dir = new Vector2Int(-1, 0);
        }
        else if (targetDirection == new Vector2Int(-1, 1))
        {
            dir = new Vector2Int(0, -1);
        }
        else if (targetDirection == new Vector2Int(-1, 0))
        {
            dir = new Vector2Int(1, -1);
        }
        else if (targetDirection == new Vector2Int(0, -1))
        {
            dir = new Vector2Int(1, 0);
        }
        else if (targetDirection == new Vector2Int(1, -1))
        {
            dir = new Vector2Int(0, 1);
        }
        else if (targetDirection == new Vector2Int(1, 0))
        {
            dir = new Vector2Int(-1, 1);
        }
        else
        {
            yield break;
        }

        Queue<Vector2Int> openSet = new Queue<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Vector2Int startPos = new Vector2Int(startGrid.info.q, startGrid.info.r);
        openSet.Enqueue(startPos);

        int nodesProcessedThisFrame = 0;
        //��startGrid��ʼ, �԰뾶radius���������ڵ�Vector2 (q, r)���� ���б���
        int currentLength = 0;
        while (currentLength <= length)
        {
            Vector2Int current = openSet.Dequeue();
            closedSet.Add(current);
            openSet.Enqueue(current + targetDirection);

            //������current��ʼ, ��dirΪ�����ͬһ��with����ڵ�����(q, r)����
            for (int i = 1; i < width; i++)
            {
                int index = Mathf.CeilToInt(i / 2.0f);
                index *= (i % 2 == 1) ? 1 : -1;
                Vector2Int oneRowGrid = current + dir * index; //ͬһ�������(q, r)����
                if (closedSet.Contains(oneRowGrid)) //����Ѿ�������������, ������
                {
                    continue;
                }
                closedSet.Add(oneRowGrid);
            }
            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
            {
                yield return null; //ÿ֡����һ����, �������, ��һ֡��������
                nodesProcessedThisFrame = 0;
            }

            currentLength++;
        }
        //������񲻰�����ʼλ��, ���Ƴ���ʼ�е���������
        if (!includeStartGrid)
        {
            closedSet.Remove(startPos);
            for (int i = 0; i < width; i++)
            {
                Vector2Int startRowGrid = startPos + dir * (i + 1); //��ʼ�������(q, r)����
                startRowGrid *= ((i % 2 == 1) ? -1 : 1);
                if (closedSet.Contains(startRowGrid)) //����Ѿ�������������, ������
                {
                    closedSet.Remove(startRowGrid);
                }
            }
        }
        //���closedSet (q, r)����, ȷ�϶�ӦGridHelper�Ƿ����
        foreach (Vector3Int pos in closedSet)
        {
            GridHelper grid = GridMap.Instance.SearchGrid(pos.x, pos.y, 0);
            if (grid != null && !grids.Contains(grid))
            {
                grids.Add(grid); //�����������(q,r,s)��Ӧ������, �ͼ���grids
            }
        }

        yield break;
    }

    /// <summary>
    /// �õ�targetGrid�����startGrid��Ŀ�귽��(q, r, s)
    /// </summary>
    /// <param name="startGrid"></param>
    /// <param name="targetGrid"></param>
    /// <returns></returns>
    private Vector2Int GetTargetDirection(GridHelper startGrid, GridHelper targetGrid)
    {
        //���targetGrid��startGrid��ͬ, ��Ĭ��Ϊ(0,1)����
        if (targetGrid == startGrid)
        {
            return new Vector2Int(0, 1);
        }

        if (targetGrid.info.q <= startGrid.info.q && targetGrid.info.r > startGrid.info.r && targetGrid.info.s < startGrid.info.s)
        {
            return new Vector2Int(0, 1);
        }
        else if (targetGrid.info.q < startGrid.info.q && targetGrid.info.r > startGrid.info.r && targetGrid.info.s >= startGrid.info.s)
        {
            return new Vector2Int(-1, 1);
        }
        else if (targetGrid.info.q < startGrid.info.q && targetGrid.info.r <= startGrid.info.r && targetGrid.info.s > startGrid.info.s)
        {
            return new Vector2Int(-1, 0);
        }
        else if (targetGrid.info.q >= startGrid.info.q && targetGrid.info.r < startGrid.info.r && targetGrid.info.s > startGrid.info.s)
        {
            return new Vector2Int(0, -1);
        }
        else if (targetGrid.info.q > startGrid.info.q && targetGrid.info.r < startGrid.info.r && targetGrid.info.s <= startGrid.info.s)
        {
            return new Vector2Int(1, -1);
        }
        else if (targetGrid.info.q > startGrid.info.q && targetGrid.info.r >= startGrid.info.r && targetGrid.info.s < startGrid.info.s)
        {
            return new Vector2Int(1, 0);
        }

        return new Vector2Int(0, 1);
    }

    /// <summary>
    /// ��ʼ���ٻ���λ���ڸ����б�
    /// </summary>
    /// <param name="summoner"></param>
    /// <param name="skill"></param>
    /// <param name="summonUnitGrids"></param>
    /// <returns></returns>
    private IEnumerator InitializeSummonedUnitsGrids(Character summoner, SummonSkillData skill, GridHelper targetGrid, HashSet<GridHelper> summonedUnitGrids)
    {
        if (skill.summonedUnitCount == 0)
        {
            yield break;
        }
        //�ٻ�1����λ
        else if (skill.summonedUnitCount == 1)
        {
            //���Լ���targetGrid
            TryAddSummonedUnitGrid(summoner, targetGrid, summonedUnitGrids);
            //�����������Ȼ����, ����targetGridΪ���Ľ���bfs����, ֱ��summonUnitGrids����������summonedUnitCount ���� ���������и���
            if (summonedUnitGrids.Count < 1)
            {
                yield return FillSummonedUnitGrids(summoner, skill.summonedUnitCount, targetGrid, summonedUnitGrids);
            }
        }
        //�ٻ�2����λ
        else if (skill.summonedUnitCount == 2)
        {
            //���Լ���(-1,0), (1,0)��������ĸ���
            GridHelper grid = GridMap.Instance.SearchGrid(targetGrid.info.q - 2, targetGrid.info.r, targetGrid.info.heightOrder);
            if(grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q + 2, targetGrid.info.r, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }

            if (summonedUnitGrids.Count < 2)
            {
                yield return FillSummonedUnitGrids(summoner, skill.summonedUnitCount, targetGrid, summonedUnitGrids);
            }
        }
        //�ٻ�3����λ
        else if (skill.summonedUnitCount == 3)
        {
            //���Լ���targetGrid��(-1,0), (1,0)��������ĸ���
            TryAddSummonedUnitGrid(summoner, targetGrid, summonedUnitGrids);
            GridHelper grid = GridMap.Instance.SearchGrid(targetGrid.info.q - 1, targetGrid.info.r, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q + 1, targetGrid.info.r, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }

            if (summonedUnitGrids.Count < 3)
            {
                yield return FillSummonedUnitGrids(summoner, skill.summonedUnitCount, targetGrid, summonedUnitGrids);
            }
        }
        //�ٻ�4����λ
        else if (skill.summonedUnitCount == 4)
        {
            //���Լ���(-1,1), (0,1), (1,-1), (0,-1)�ĸ�����ĸ���
            GridHelper grid = GridMap.Instance.SearchGrid(targetGrid.info.q - 1, targetGrid.info.r + 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q, targetGrid.info.r + 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q + 1, targetGrid.info.r - 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q, targetGrid.info.r - 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }

            if (summonedUnitGrids.Count < 4)
            {
                Debug.Log($"summonedUnitGrids.Count == {summonedUnitGrids.Count}");
                yield return FillSummonedUnitGrids(summoner, skill.summonedUnitCount, targetGrid, summonedUnitGrids);
            }
        }
        //�ٻ�5����λ
        else if (skill.summonedUnitCount == 5)
        {
            //���Լ���targetGrid��(-1,1), (0,1), (1,-1), (0,-1)�ĸ�����ĸ���
            TryAddSummonedUnitGrid(summoner, targetGrid, summonedUnitGrids);
            GridHelper grid = GridMap.Instance.SearchGrid(targetGrid.info.q - 1, targetGrid.info.r + 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q, targetGrid.info.r + 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q + 1, targetGrid.info.r - 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q, targetGrid.info.r - 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }

            if (summonedUnitGrids.Count < 5)
            {
                yield return FillSummonedUnitGrids(summoner, skill.summonedUnitCount, targetGrid, summonedUnitGrids);
            }
        }
        //�ٻ�6����λ
        else if (skill.summonedUnitCount == 6)
        {
            //���Լ���6������ĸ���
            GridHelper grid = GridMap.Instance.SearchGrid(targetGrid.info.q - 1, targetGrid.info.r, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q - 1, targetGrid.info.r + 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q, targetGrid.info.r + 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q + 1, targetGrid.info.r, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q + 1, targetGrid.info.r - 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q, targetGrid.info.r - 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }

            if (summonedUnitGrids.Count < 6)
            {
                yield return FillSummonedUnitGrids(summoner, skill.summonedUnitCount, targetGrid, summonedUnitGrids);
            }
        }
        //�ٻ�7����λ
        else if (skill.summonedUnitCount == 7)
        {
            //���Լ���targetGird��6������ĸ���
            TryAddSummonedUnitGrid(summoner, targetGrid, summonedUnitGrids);
            GridHelper grid = GridMap.Instance.SearchGrid(targetGrid.info.q - 1, targetGrid.info.r, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q - 1, targetGrid.info.r + 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q, targetGrid.info.r + 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q + 1, targetGrid.info.r, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q + 1, targetGrid.info.r - 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }
            grid = GridMap.Instance.SearchGrid(targetGrid.info.q, targetGrid.info.r - 1, targetGrid.info.heightOrder);
            if (grid != null)
            {
                TryAddSummonedUnitGrid(summoner, grid, summonedUnitGrids);
            }

            if (summonedUnitGrids.Count < 7)
            {
                yield return FillSummonedUnitGrids(summoner, skill.summonedUnitCount, targetGrid, summonedUnitGrids);
            }
        }
        //�ٻ�7�����ϵ�λ
        else
        {
            //��targetGridΪ���Ľ���bfs����, ֱ��summonUnitGrids����������summonedUnitCount ���� ���������и���
            yield return FillSummonedUnitGrids(summoner, skill.summonedUnitCount, targetGrid, summonedUnitGrids);
        }
    }

    /// <summary>
    /// ��targetGridΪ���Ľ���bfs����, ���������ĸ�����ӵ�summonUnitGrids��, ֱ��summonUnitGrids����������summonedUnitCount ���� ���������и���
    /// </summary>
    /// <param name="summoner"></param>
    /// <param name="summonUnitCount"></param>
    /// <param name="targetGrid"></param>
    /// <param name="summonUnitGrids"></param>
    /// <returns></returns>
    private IEnumerator FillSummonedUnitGrids(Character summoner, int summonUnitCount, GridHelper targetGrid, HashSet<GridHelper> summonedUnitGrids)
    {
        Queue<GridHelper> openSet = new Queue<GridHelper>();
        openSet.Enqueue(targetGrid);

        int nodesProcessedThisFrame = 0;
        while (openSet.Count > 0 && summonedUnitGrids.Count < summonUnitCount)
        {
            GridHelper current = openSet.Dequeue();
            TryAddSummonedUnitGrid(summoner, current, summonedUnitGrids);

            foreach (GridHelper neighbor in current.neighborGrids)
            {
                if (neighbor == null || summonedUnitGrids.Contains(neighbor)) //��������ڸ��ھ�����, ���� ����Ѿ�������������, ������
                {
                    continue;
                }
                openSet.Enqueue(neighbor);
            }

            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
            {
                yield return null; //ÿ֡����һ����, �������, ��һ֡��������
                nodesProcessedThisFrame = 0;
            }
            //�������Ľڵ�������һ������, ��ֱ�ӷ���, ��ֹ����ʱ�����
            if (nodesProcessedThisFrame > 200)
            {
                yield break;
            }
        }
        yield break;
    }

    /// <summary>
    /// �������ٻ���λ���ڸ����б�����Ӹ���, ����������е�λ ���� �Ѿ����б���, �����ʧ��; ���û�е�λ, ����ӳɹ�
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="summonUnitGrids"></param>
    private void TryAddSummonedUnitGrid(Character summoner, GridHelper grid, HashSet<GridHelper> summonedUnitGrids)
    {
        //�����Ѿ����б���
        if (summonedUnitGrids.Contains(grid))
        {
            return;
        }

        //�ٻ����ڻغ�����
        if (summoner.isInTurn)
        {
            if (TurnManager.Instance.playerGrids.ContainsKey(grid) || TurnManager.Instance.enemyGrids.ContainsKey(grid))
            {
                return;
            }
            summonedUnitGrids.Add(grid);
        }
        //�ٻ��߲��ڻغ�����
        else
        {
            RaycastHit hit;
            //���Ƽ��ܷ�Χ����
            if (Physics.Raycast(grid.transform.position + Vector3.up * 50, Vector3.down, out hit, 100, LayerMask.GetMask("Character", "Model")))
            {
                return;
            }
            summonedUnitGrids.Add(grid);
        }
    }


    /// <summary>
    /// �õ�����ʩ�����Χ�ڵ���������, ����grids��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="grids"></param>
    /// <returns></returns>
    private IEnumerator GetSkillMaxDistanceAreaGrids(Character character, SkillBaseData skill, HashSet<GridHelper> grids)
    {
        int radius = 1;
        if (skill is EffectApplicationSkillData)
        {
            radius = (skill as EffectApplicationSkillData).effectArea.maxDistance;
        }
        else if (skill is SummonSkillData)
        {
            radius = (skill as SummonSkillData).summonArea.maxDistance;
        }
        yield return GetCircleAreaGrids(character.nowGrid, radius, grids, true);
    }

    /// <summary>
    /// �õ������ͷŷ�Χ�ڵ���������, ����grids��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="targetGrid"></param>
    /// <param name="grids"></param>
    /// <returns></returns>
    private IEnumerator GetSkillEffectsAreaGrids(Character character, SkillBaseData skill, GridHelper targetGrid, HashSet<GridHelper> grids)
    {
        if (skill is EffectApplicationSkillData)
        {
            yield return GetEffectApplicationSkillAreaGrids(character, skill as EffectApplicationSkillData, targetGrid, grids);
        }
        else if (skill is SummonSkillData)
        {
            yield return GetSummonSkillAreaGrids(character, skill as SummonSkillData, targetGrid, grids);
        }
    }

    /// <summary>
    /// �õ�EffectApplicationSkill�ͷŷ�Χ�ڵ���������, ����grids��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="targetGrid"></param>
    /// <param name="grids"></param>
    /// <returns></returns>
    private IEnumerator GetEffectApplicationSkillAreaGrids(Character character, EffectApplicationSkillData skill, GridHelper targetGrid, HashSet<GridHelper> grids)
    {
        //���弼�� SingleTarget
        if (skill.effectArea.targetingPattern == EffectTargetingPattern.SingleTarget)
        {
            if (!grids.Contains(targetGrid))
            {
                grids.Add(targetGrid);
            }
        }
        //Բ��AOE���� CircleAOE
        else if (skill.effectArea.targetingPattern == EffectTargetingPattern.CircleAOE)
        {
            //���ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�
            if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered)
            {
                yield return GetCircleAreaGrids(targetGrid, skill.effectArea.radius, grids, true);
            }
            //���ܵ�ʩ��ê����������Ϊ���ĵ�
            else
            {
                yield return GetCircleAreaGrids(character.nowGrid, skill.effectArea.radius, grids, true);
            }
        }
        //����AOE���� ConeAOE
        else if (skill.effectArea.targetingPattern == EffectTargetingPattern.ConeAOE)
        {
            //���ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�
            if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered)
            {
                yield return GetConeAreaGrids(targetGrid, GetTargetDirection(character.nowGrid, targetGrid), skill.effectArea.radius, grids, true);
            }
            //���ܵ�ʩ��ê����������Ϊ���ĵ�
            else
            {
                yield return GetConeAreaGrids(character.nowGrid, GetTargetDirection(character.nowGrid, targetGrid), skill.effectArea.radius, grids, false);
            }
        }
        //����AOE���� ConeAOE
        else if (skill.effectArea.targetingPattern == EffectTargetingPattern.LineAOE)
        {
            //���ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�
            if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered)
            {
                yield return GetLineAreaGrids(targetGrid, GetTargetDirection(character.nowGrid, targetGrid), skill.effectArea.radius, skill.effectArea.width, grids, true);
            }
            //���ܵ�ʩ��ê����������Ϊ���ĵ�
            else
            {
                yield return GetLineAreaGrids(character.nowGrid, GetTargetDirection(character.nowGrid, targetGrid), skill.effectArea.radius, skill.effectArea.width, grids, false);
            }
        }

        yield break;
    }

    /// <summary>
    /// �õ�SummonSkill�ͷŷ�Χ�ڵ���������, ����grids��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="targetGrid"></param>
    /// <param name="grids"></param>
    /// <returns></returns>
    private IEnumerator GetSummonSkillAreaGrids(Character character, SummonSkillData skill, GridHelper targetGrid, HashSet<GridHelper> grids)
    {
        //���ܵ�ʩ��ê�����Կ�ѡ���Ϊ���ĵ�
        if (skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
        {
            yield return GetCircleAreaGrids(targetGrid, skill.summonArea.radius, grids, true);
        }
        //���ܵ�ʩ��ê����������Ϊ���ĵ�
        else
        {
            yield return GetCircleAreaGrids(character.nowGrid, skill.summonArea.radius, grids, true);
        }
    }

    /// <summary>
    /// ����grids, ��Ⱦ���еı�Ե������, ��������Ⱦ��;, ��������Ⱦ��LienRenderer���뵽lineRenderers��
    /// </summary>
    /// <param name="grids"></param>
    /// <param name="lineRenderers"></param>
    /// <returns></returns>
    private IEnumerator DrawEdgeGrids(HashSet<GridHelper> grids, List<LineRenderer> lineRenderers)
    {
        if (skillMaxDistanceMaterial == null || skillTargetMaterial == null) //ȷ��LineRenderer����Ĳ����Ƿ����
        {
            yield break;
        }
        //�����֮ǰ�������Ƶ�lineRenderers, ������LineRenderer���������
        foreach (LineRenderer lr in lineRenderers)
        {
            lr.material = outlineMaterial;
            LineRendererPool.Instance.ReturnLineRenderer(lr);
        }
        lineRenderers.Clear();

        int nodesProcessedThisFrame = 0;
        //����grids�е�ÿһ������grid
        foreach (GridHelper grid in grids)
        {
            GridInfo info = grid.info;
            //����grid��ÿһ���ھ�����grid.neighborGrids[i]
            for (int i = 0; i < 6; i++)
            {
                //����ھ�������grids��, ˵���÷����������Ϊ��Ե������, ��Ҫ��Ⱦ
                if (!grids.Contains(grid.neighborGrids[i]))
                {
                    int next = (i + 1) % 6;
                    LineRenderer lr = LineRendererPool.Instance.GetLineRenderer();
                    lr.material = lineRenderers == maxDistanceEdgeGridsLineRenderers ? skillMaxDistanceMaterial : skillTargetMaterial; //ѡ�����
                    lr.transform.SetParent(linesParent);
                    lr.positionCount = 2;
                    lr.SetPositions(new Vector3[]{ info.vertices[i], info.vertices[next]});
                    lineRenderers.Add(lr); //��¼����ʹ�õ�LineRenderer
                }
            }
            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
            {
                yield return null; //ÿ֡����һ����, �������, ��һ֡��������
                nodesProcessedThisFrame = 0;
            }
        }
        yield break;
    }

    /// <summary>
    /// ��lineRenderers��������Ⱦ�����ߵ�LineRenderer���������
    /// </summary>
    /// <param name="lineRenderers"></param>
    /// <returns></returns>
    private IEnumerator ReturnEdgeGridsToPool(List<LineRenderer> lineRenderers)
    {
        if (outlineMaterial == null) //ȷ��LineRenderer����Ĳ����Ƿ����
        {
            yield break;
        }

        foreach (LineRenderer lr in lineRenderers)
        {
            lr.material = outlineMaterial;
            LineRendererPool.Instance.ReturnLineRenderer(lr);
        }
        lineRenderers.Clear();
        yield break;
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



    #region CharacterAI���
    /// <summary>
    /// ��AI��ɫcharacter�ͷż���, �Խ�ɫtargetΪĿ��ΪĿ�괦����Ч����Ӧ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="target"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    public IEnumerator ApplyCharacterAISkillEffects(Character character, Character target, SkillBaseData skill)
    {
        //Ч��Ӧ���༼��
        if (skill is EffectApplicationSkillData)
        {
            EffectApplicationSkillData effectApplicationSkill = skill as EffectApplicationSkillData;
            //��������ͷ�ê��������Ϊ����, ��character��target�ľ�����ڼ��ܷ�Χ�뾶, �����ƶ������ܿ������еĵط�
            if (effectApplicationSkill.effectArea.effectAnchor == EffectAnchorMode.selfCentered && GetDistance(character.nowGrid, target.nowGrid) > effectApplicationSkill.effectArea.radius)
            {
                CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, effectApplicationSkill.effectArea.radius), character.info.name);
                CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1), character.info.name);

                //yield return character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, effectApplicationSkill.effectArea.radius);
                //yield return character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1);
            }
            //��������ͷ�ê���Կ�ѡ���Ϊ����, ��charater��target�ľ�����ڼ������ʩ������, �����ƶ������ܿ������еĵط�
            else if (effectApplicationSkill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered && GetDistance(character.nowGrid, target.nowGrid) > effectApplicationSkill.effectArea.maxDistance)
            {
                CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, effectApplicationSkill.effectArea.maxDistance), character.info.name);
                CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1), character.info.name);

                //yield return character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, effectApplicationSkill.effectArea.maxDistance);
                //yield return character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1);
            }
            //����ƶ���, ��ɫ�ж������������ͷż���, ��ֱ�ӷ���
            if (character.info.actionPoint < effectApplicationSkill.actionPointTakes)
            {
                yield break;
            }
            //Ӧ�õ��弼��Ч��
            if (effectApplicationSkill.effectArea.targetingPattern == EffectTargetingPattern.SingleTarget)
            {
                yield return ApplyEffectApplicationSkillEffects(character, effectApplicationSkill, target);
            }
            //Ӧ��AOE����Ч��
            else
            {
                yield return ApplyEffectApplicationSkillEffects(character, effectApplicationSkill, target.nowGrid);
            }
        }
        //�ٻ��༼��
        else if (skill is SummonSkillData)
        {
            SummonSkillData summonSkill = skill as SummonSkillData;
            //��������ͷ�ê��������Ϊ����
            if (summonSkill.summonArea.effectAnchor == EffectAnchorMode.selfCentered)
            {
                yield return ApplySummonSkillEffects(character, summonSkill, character.nowGrid);
            }
            //��������ͷ�ê���Կ�ѡ���Ϊ����
            else if (summonSkill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
            {
                GridHelper targetGrid = null; //�ٻ������ͷ�ê��
                float minDistance = float.MaxValue; //�����ͷ�ê�����targetĿ��ľ���
                HashSet<GridHelper> maxDistanceGrids = new HashSet<GridHelper>();
                yield return GetSkillMaxDistanceAreaGrids(character, skill, maxDistanceGrids);
                //�ҵ����ܼ������ʩ�����뷶Χ�� ����target����ĸ���
                foreach (GridHelper grid in maxDistanceGrids)
                {
                    float distance = GetDistance(grid, target.nowGrid);
                    if (distance < minDistance)
                    {
                        targetGrid = grid;
                        minDistance = distance;
                    }
                    yield return null;
                }
                //���û���ҵ�����, �ͷ���
                if(targetGrid == null)
                {
                    yield break;
                }
                //Ӧ���ٻ�����
                yield return ApplySummonSkillEffects(character, summonSkill, targetGrid);
            }
        }
    }
    #endregion



    #region SkillBarUI���
    /// <summary>
    /// ��ɫʹ��
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    private void UpdateSkillInfoAfterUseSkill(Character character, SkillBaseData skill)
    {
        skill.remainingTurns = skill.cooldownTurns;
        //�����ɫ����Ҷ����ɫ, ����SkillBarUI
        if (character.CompareTag("Player"))
        {
            UISkillBarManager.Instance.UpdateSkillBarUI(skill);
        }
        //�����ɫ���ڻغ�����, �Ϳ�����ʱ�Ƽ�ʱЭ��
        if (!character.isInTurn)
        {
            CoroutineManager.Instance.AddTaskToGroup(ResumeSkillAfterCooldownTurnsDalay(character, skill), character.info.name + skill.skillName);
            CoroutineManager.Instance.StartGroup(character.info.name + skill.skillName);
        }
    }

    /// <summary>
    /// ���ڻغ��ƹ�������, ÿ��һ����ɫ�غϽ���, ���������ϵ�Skill����ȴʣ��غ�����UI
    /// </summary>
    /// <param name="character"></param>
    public void UpdateSkillInfoAfterFinishTurn(Character character)
    {
        foreach (SkillBaseData skill in character.characterSkills)
        {
            //�غϽ���ֻ������ȴʣ��غ�������0�ļ���
            if (skill.remainingTurns > 0)
            {
                skill.remainingTurns--; //buffʣ��غ�-1

                //�����ɫ����Ҷ����ɫ, ����SkillBarUI
                if (character.CompareTag("Player"))
                {
                    UISkillBarManager.Instance.UpdateSkillBarUI(skill);
                }
            }
        }
    }

    /// <summary>
    /// �غ��ƽ���ʱ, ����ɫ���ϵ�Skill�Ӽ�ʱ�Ƽ�ʱתΪ�غ��Ƽ�ʱ
    /// </summary>
    /// <param name="character"></param>
    public void UpdateSkillBarUIAfterEndTurn(Character character)
    {
        foreach (SkillBaseData skill in character.characterSkills)
        {
            //�غϽ���, �Խ�ɫ������ȴʣ��غ�������0��Skill������ʱ�Ƽ�ʱЭ��
            if (skill.remainingTurns > 0)
            {
                CoroutineManager.Instance.AddTaskToGroup(ResumeSkillAfterCooldownTurnsDalay(character, skill), character.info.name + skill.skillName);
                CoroutineManager.Instance.StartGroup(character.info.name + skill.skillName);
            }
        }
    }

    /// <summary>
    /// �غ��ƿ�ʼʱ, ����ɫ���ϵ�Skill�ӻغ��Ƽ�ʱתΪ��ʱ�Ƽ�ʱ, ֹͣ��ɫ����ÿһ��Skill�ļ�ʱ�Ƽ�ʱЭ��
    /// </summary>
    /// <param name="character"></param>
    public void UpdateSkillBarUIAfterStartTurn(Character character)
    {
        foreach (SkillBaseData skill in character.characterSkills)
        {
            //�غϿ�ʼֹͣ��ɫ����ÿһ����ȴʣ��غ�������0��Skill�ļ�ʱ�Ƽ�ʱЭ��
            if (skill.remainingTurns > 0)
            {
                CoroutineManager.Instance.StopGroup(character.info.name + skill.skillName);
            }
        }
    }

    /// <summary>
    /// ���ڷǻغ�״̬(��ʱ��״̬)��, ��cooldownTurns�и��¼�����ȴʣ��غ���, ��remainingTurns==0, ��ȴ����, �ָ��ü���
    /// </summary>
    /// <param name="skill"></param>
    /// <returns></returns>
    public IEnumerator ResumeSkillAfterCooldownTurnsDalay(Character character, SkillBaseData skill)
    {
        while (skill.remainingTurns > 0)
        {
            yield return oneTurnTime; //һ�غϵȼ���6s, �ȴ�һ�غ�ʱ��

            skill.remainingTurns--; //�����غ�-1

            //�����ɫ����Ҷ����ɫ, ����SkillBarUI
            if (character.CompareTag("Player"))
            {
                UISkillBarManager.Instance.UpdateSkillBarUI(skill);
            }
        }
    }
    #endregion
}
