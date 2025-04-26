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

    private List<LineRenderer> maxDistanceEdgeGridsLineRenderers = new List<LineRenderer>(); //用于渲染技能最大施法范围边缘的LineRenderer列表
    private List<LineRenderer> skillTargetEdgeGridsLineRenderers = new List<LineRenderer>(); //用于渲染技能施法目标范围边缘的LindRenderer列表
    public Transform linesParent; //路径网格线的父对象
    private Material outlineMaterial; //对象池中网格的本身材质
    private Material skillMaxDistanceMaterial; //最大施法范围网格线材质
    private Material skillTargetMaterial; //技能目标范围网格线材质

    public int maxNodesProcessedPerFrame = 100; //每帧最多处理节点
    private WaitForSeconds oneTurnTime = new WaitForSeconds(6.0f); //等待6s

    void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        //初始化网格材质
        outlineMaterial = Resources.Load<Material>("Materials/outlineRendererMaterial");
        skillMaxDistanceMaterial = Resources.Load<Material>("Materials/skillMaxDistanceMaterial");
        skillTargetMaterial = Resources.Load<Material>("Materials/skillTargetMaterial");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 激活技能, 进入预施法阶段
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    public void ActivateSkill(Character character, SkillBaseData skill)
    {
        //如果剩余回合数还未冷却完毕 或 正在移动、攻击、选择技能目标, 就返回
        if (skill.remainingTurns > 0 || (character.isInTurn && character.info.actionPoint < skill.actionPointTakes)
            || character.isMoving || character.isAttacking || character.isSkillTargeting)
        {
            return;
        }
        Debug.Log("ActicateSkill");
        character.isSkillTargeting = true; //标记角色正在选择技能目标中
        StartCoroutine(ProcessSkillTargeting(character, skill)); //开启选择技能目标的协程
    }

    /// <summary>
    /// 预施法阶段, 处理技能目标选择
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    private IEnumerator ProcessSkillTargeting(Character character, SkillBaseData skill)
    {
        if (skill is EffectApplicationSkillData)
        {
            yield return ProcessEffectApplicationSkillTargeting(character, skill as EffectApplicationSkillData); //处理效果施加技能的目标选择
        }
        else if (skill is SummonSkillData)
        {
            yield return ProcessSummonSkillDataTargeting(character, skill as SummonSkillData); //处理召唤技能的目标选择
        }
    }

    /// <summary>
    /// 处理效果施加技能的目标选择
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    private IEnumerator ProcessEffectApplicationSkillTargeting(Character character, EffectApplicationSkillData skill)
    {
        Debug.Log("ProcessEffectApplicationSkillTargeting");
        //如果技能施法锚点是可以以选择点为中心的, 就对最大释放距离的边缘网格线进行渲染
        if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered)
        {
            HashSet<GridHelper> maxDistanceGrids = new HashSet<GridHelper>();
            CoroutineManager.Instance.AddTaskToGroup(GetSkillMaxDistanceAreaGrids(character, skill, maxDistanceGrids), character.info.name + "_skillTargeting");
            CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(maxDistanceGrids, maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
            CoroutineManager.Instance.StartGroup(character.info.name + "_skillTargeting");
            yield return new WaitUntil(() => CoroutineManager.Instance.TaskInGroupIsEmpty(character.info.name + "_skillTargeting"));
        }

        bool skillTargeting = true; //用来标记是否正在选择技能目标
        while (skillTargeting)
        {
            yield return null;

            if (character != PartyManager.Instance.leader)
            {
                StartCoroutine(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers)); //将用于渲染技能最大施法范围边缘的LineRenderer返还给对象池
                StartCoroutine(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers)); //将用于渲染技能施法目标范围边缘的LindRenderer返回给对象池
                yield return new WaitUntil(() => character == PartyManager.Instance.leader); //如果当前玩家轮次所操控的角色不是主控, 就一直等待, 直到成为主控
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Character targetCharacter = null;
            GridHelper targetGrid = null;
            //绘制技能范围网格
            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character", "Model"))
                && !character.isMoving && !character.isAttacking) //if (isInTurn && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character")))
            {
                targetCharacter = hit.collider.GetComponent<Character>();
                targetGrid = hit.collider.GetComponent<GridHelper>();
                //单体技能 SingleTarget
                if (skill.effectArea.targetingPattern == EffectTargetingPattern.SingleTarget)
                {
                    //如果检测到角色 并且 施法者和目标角色的距离 小于等于 最大施法距离
                    if (targetCharacter != null && GetDistance(character.nowGrid, targetCharacter.nowGrid) <= skill.effectArea.maxDistance)
                    {
                        CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(new HashSet<GridHelper>() { targetCharacter.nowGrid }, skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                        UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, skill.actionPointTakes);
                    }
                    //如果没有检测到角色
                    else
                    {
                        CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    }
                }
                //AOE技能 CircleAOE ConeAOE LineAOE
                else
                {
                    if (targetCharacter != null) //检测到角色
                    {
                        targetGrid = targetCharacter.nowGrid;
                    }
                    //如果技能的施法锚点是以可选择点为中心的, 目标网格不为空, 并且 施法者和目标角色的距离 小于等于 最大施法距离
                    //if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered && targetGrid != null && getDistance(character.nowGrid, targetGrid) <= skill.effectArea.maxDistance)
                    //{
                    //    HashSet<GridHelper> skillTargetGrids = new HashSet<GridHelper>();
                    //    CoroutineManager.Instance.AddTaskToGroup(GetSkillEffectsAreaGrids(character, skill, targetGrid, skillTargetGrids), character.info.name + "_skillTargeting");
                    //    CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(skillTargetGrids, skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    //    UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, skill.actionPointTakes);
                    //}
                    ////如果技能的施法锚点是以自身为中心的
                    //else if (skill.effectArea.effectAnchor == EffectAnchorMode.selfCentered && targetGrid != null)
                    //{
                    //    HashSet<GridHelper> skillTargetGrids = new HashSet<GridHelper>();
                    //    CoroutineManager.Instance.AddTaskToGroup(GetSkillEffectsAreaGrids(character, skill, targetGrid, skillTargetGrids), character.info.name + "_skillTargeting");
                    //    CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(skillTargetGrids, skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    //    UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, skill.actionPointTakes);
                    //}

                    //如果技能的施法锚点是以可选择点为中心的, 目标网格不为空, 并且 施法者和目标角色的距离 小于等于 最大施法距离
                    //或者 如果技能的施法锚点是以自身为中心的, 且目标网格不为空
                    //就绘制技能范围网格
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
            else //角色和网格都没有检测到
            {
                //如果技能的施法锚点是以可选择点为中心的, 就将用于渲染技能施法目标范围边缘的LindRenderer返回给对象池
                if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered)
                {
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                }
            }

            if (!Input.GetKey(KeyCode.LeftAlt) && !EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0)) //检测鼠标点击
            {
                bool applySkillEffects = false; //用于标记这次鼠标点击是否触发 对技能效果的应用

                //单体技能 SingleTarget
                if (skill.effectArea.targetingPattern == EffectTargetingPattern.SingleTarget)
                {
                    //如果目标角色不为空
                    if(targetCharacter != null && GetDistance(character.nowGrid, targetCharacter.nowGrid) <= skill.effectArea.maxDistance)
                    {
                        CoroutineManager.Instance.AddTaskToGroup(ApplyEffectApplicationSkillEffects(character, skill, targetCharacter), character.info.name);
                        applySkillEffects = true;
                    }
                }
                //AOE技能 CircleAOE ConeAOE LineAOE
                else
                {
                    //如果目标网格不为空
                    //if (targetGrid != null)
                    //{
                    //    CoroutineManager.Instance.AddTaskToGroup(ApplySkillEffects(character, skill, targetGrid), character.info.name);
                    //    applySkillEffects = true;
                    //}

                    //如果技能的施法锚点是以可选择点为中心的, 目标网格不为空, 并且 施法者和目标角色的距离 小于等于 最大施法距离
                    //或者 如果技能的施法锚点是以自身为中心的, 且目标网格不为空
                    //就应用技能效果
                    if ((skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered && targetGrid != null && GetDistance(character.nowGrid, targetGrid) <= skill.effectArea.maxDistance)
                        || (skill.effectArea.effectAnchor == EffectAnchorMode.selfCentered && targetGrid != null))
                    {
                        CoroutineManager.Instance.AddTaskToGroup(ApplyEffectApplicationSkillEffects(character, skill, targetGrid), character.info.name);
                        applySkillEffects = true;
                    }
                }

                //如果这次点击触发对技能效果的应用, 就跳出循环, 停止渲染技能范围网格
                if (applySkillEffects)
                {
                    skillTargeting = false; //停止选择技能目标

                    //将用于渲染技能最大施法范围边缘的LineRenderer返还给对象池
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    //将用于渲染技能施法目标范围边缘的LindRenderer返回给对象池
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");

                    CoroutineManager.Instance.StartGroup(character.info.name); //执行角色协程队列, 应用技能效果
                }
            }

            //如果按下ESC键, 就跳出循环, 停止释放技能
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                skillTargeting = false; //停止选择技能目标
                character.isSkillTargeting = false;

                //将用于渲染技能最大施法范围边缘的LineRenderer返还给对象池
                CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                //将用于渲染技能施法目标范围边缘的LindRenderer返回给对象池
                CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                Debug.Log("StopSkillTargeting");
            }

            CoroutineManager.Instance.StartGroup(character.info.name + "_skillTargeting");
        }
    }

    /// <summary>
    /// 处理召唤技能的目标选择
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    private IEnumerator ProcessSummonSkillDataTargeting(Character character, SummonSkillData skill)
    {
        Debug.Log("ProcessSummonSkillDataTargeting");
        //如果技能施法锚点是可以以选择点为中心的
        //如果技能施法锚点是可以以选择点为中心的, 就对最大释放距离的边缘网格线进行渲染
        if (skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
        {
            HashSet<GridHelper> maxDistanceGrids = new HashSet<GridHelper>();
            CoroutineManager.Instance.AddTaskToGroup(GetSkillMaxDistanceAreaGrids(character, skill, maxDistanceGrids), character.info.name + "_skillTargeting");
            CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(maxDistanceGrids, maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
            CoroutineManager.Instance.StartGroup(character.info.name + "_skillTargeting");
            yield return new WaitUntil(() => CoroutineManager.Instance.TaskInGroupIsEmpty(character.info.name + "_skillTargeting"));
        }

        bool skillTargeting = true; //用来标记是否正在选择技能目标
        while (skillTargeting)
        {
            yield return null;

            if (character != PartyManager.Instance.leader)
            {
                StartCoroutine(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers)); //将用于渲染技能最大施法范围边缘的LineRenderer返还给对象池
                StartCoroutine(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers)); //将用于渲染技能施法目标范围边缘的LindRenderer返回给对象池
                yield return new WaitUntil(() => character == PartyManager.Instance.leader); //如果当前玩家轮次所操控的角色不是主控, 就一直等待, 直到成为主控
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Character targetCharacter = null;
            GridHelper targetGrid = null;
            //绘制技能范围网格
            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character", "Model"))
                && !character.isMoving && !character.isAttacking) //if (isInTurn && Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Grid", "Character")))
            {
                targetCharacter = hit.collider.GetComponent<Character>();
                targetGrid = hit.collider.GetComponent<GridHelper>();
                //技能的施法锚点是以自身为中心的, 检测到角色, 就得到targetGrid, 后续正常渲染技能范围网格; 技能的施法锚点是以可选择点为中心的, 检测到角色, 就不渲染技能范围网格
                if (skill.summonArea.effectAnchor == EffectAnchorMode.selfCentered && targetCharacter != null)
                {
                    targetGrid = targetCharacter.nowGrid;
                }

                //如果技能的施法锚点是以可选择点为中心的, 目标网格不为空, 并且 施法者和目标角色的距离 小于等于 最大施法距离
                //或者 如果技能的施法锚点是以自身为中心的, 且目标网格不为空
                //就绘制技能范围网格
                if ((skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered && targetGrid != null && GetDistance(character.nowGrid, targetGrid) <= skill.summonArea.maxDistance)
                    || (skill.summonArea.effectAnchor == EffectAnchorMode.selfCentered && targetGrid != null))
                {
                    HashSet<GridHelper> skillTargetGrids = new HashSet<GridHelper>();
                    CoroutineManager.Instance.AddTaskToGroup(GetSkillEffectsAreaGrids(character, skill, targetGrid, skillTargetGrids), character.info.name + "_skillTargeting");
                    CoroutineManager.Instance.AddTaskToGroup(DrawEdgeGrids(skillTargetGrids, skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, skill.actionPointTakes);
                }
            }
            else //角色和网格都没有检测到
            {
                //如果技能的施法锚点是以可选择点为中心的, 就将用于渲染技能施法目标范围边缘的LindRenderer返回给对象池
                if (skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
                {
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                }
            }

            if (!Input.GetKey(KeyCode.LeftAlt) && !EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0)) //检测鼠标点击
            {
                bool applySkillEffects = false; //用于标记这次鼠标点击是否触发 对技能效果的应用

                //如果技能的施法锚点是以可选择点为中心的, 目标网格不为空, 并且 施法者和目标角色的距离 小于等于 最大施法距离
                //或者 如果技能的施法锚点是以自身为中心的, 且目标网格不为空
                //就应用技能效果
                if ((skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered && targetGrid != null && GetDistance(character.nowGrid, targetGrid) <= skill.summonArea.maxDistance)
                    || (skill.summonArea.effectAnchor == EffectAnchorMode.selfCentered && targetGrid != null))
                {
                    CoroutineManager.Instance.AddTaskToGroup(ApplySummonSkillEffects(character, skill, targetGrid), character.info.name);
                    applySkillEffects = true;
                }

                //如果这次点击触发对技能效果的应用, 就跳出循环, 停止渲染技能范围网格
                if (applySkillEffects)
                {
                    skillTargeting = false; //停止选择技能目标

                    //将用于渲染技能最大施法范围边缘的LineRenderer返还给对象池
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                    //将用于渲染技能施法目标范围边缘的LindRenderer返回给对象池
                    CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");

                    CoroutineManager.Instance.StartGroup(character.info.name); //执行角色协程队列, 应用技能效果
                }
            }

            //如果按下ESC键, 就跳出循环, 停止释放技能
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                skillTargeting = false; //停止选择技能目标
                character.isSkillTargeting = false;

                //将用于渲染技能最大施法范围边缘的LineRenderer返还给对象池
                CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(maxDistanceEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                //将用于渲染技能施法目标范围边缘的LindRenderer返回给对象池
                CoroutineManager.Instance.AddTaskToGroup(ReturnEdgeGridsToPool(skillTargetEdgeGridsLineRenderers), character.info.name + "_skillTargeting");
                Debug.Log("StopSkillTargeting");
            }

            CoroutineManager.Instance.StartGroup(character.info.name + "_skillTargeting");
        }
    }

    /// <summary>
    /// 结束预施法, 在targetCharacter身上处理效果类技能伤害和效果的应用
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="targetCharacter"></param>
    /// <returns></returns>
    public IEnumerator ApplyEffectApplicationSkillEffects(Character character, EffectApplicationSkillData skill, Character targetCharacter)
    {
        Debug.Log($"{character.info.name} ApplyEffectApplicationSkillEffects {skill.skillName} to {targetCharacter.info.name}");
        //如果角色在回合制中且当前行动点数不足 或 角色已死亡 则直接返回
        //if ((character.isInTurn && character.info.actionPoint < skill.actionPointTakes) || character.info.hp <= 0)
        //{
        //    yield break;
        //}

        //如果技能影响角色类型 与 targetCharacter不符合, 就直接返回
        if ((!skill.effectArea.affectSelf && targetCharacter == character) //技能不影响施法者自身 且targetCharacter就是施法者
            || (!skill.effectArea.affectSelfCamp && targetCharacter.tag == character.tag && targetCharacter != character) //技能不能影响相同阵营角色 且targetCharacter和施法者同一阵营, 且不是施法者自身
            || (!skill.effectArea.affectOppositeCamp && targetCharacter.tag != character.tag)) //技能不影响敌方阵营角色 且targetCharacter和施法者不同阵营
        {
            character.isSkillTargeting = false; //将技能释放标记改为false
            yield break;
        }
        //如果技能影响角色类型 与 targetCharacter符合, 就继续执行, 应用技能效果

        //如果技能目标不是施法者自身, 就更新角色朝向
        if(targetCharacter != character)
        {
            Vector3 direction = (targetCharacter.transform.position - character.transform.position).normalized;
            direction.y = 0; //不改变角色y轴面向
            Quaternion startRotation = character.transform.rotation; //当前朝向
            Quaternion endRotation = Quaternion.LookRotation(direction); //终点朝向
            float rotateWeight = 0;
            while (Quaternion.Angle(character.transform.rotation, endRotation) > 0.5f)
            {
                rotateWeight += Time.deltaTime * character.info.rotateSpeed;
                character.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
                yield return null;
            }
            character.transform.rotation = endRotation;
        }

        //处理位移
        if (skill.shouldMoveToTarget)
        {
            character.transform.position = targetCharacter.transform.position;
        }

        //如果技能造成伤害 或者 附加Buff, 就应用角色脚本中处理技能伤害和Buff效果的方法, 对targetCharacter施加技能效果
        if (skill.damageDiceCount > 0 || skill.applicateBuffs.Count > 0)
        {
            yield return character.AppplySkillEffectsOnTarget(skill, new HashSet<Character>() { targetCharacter });
        }

        //更新技能冷却和UI
        UpdateSkillInfoAfterUseSkill(character, skill);

        //如果角色在回合制中, 就减去消耗的行动点数
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
    /// 结束预施法, 以targetGrid为中心处理效果类技能伤害和效果的应用
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="targetGrid"></param>
    /// <returns></returns>
    public IEnumerator ApplyEffectApplicationSkillEffects(Character character, EffectApplicationSkillData skill, GridHelper targetGrid)
    {
        Debug.Log($"{character.info.name} ApplyEffectApplicationSkillEffects {skill.skillName} to grid{targetGrid.info.q},{targetGrid.info.r}");
        //如果角色在回合制中且当前行动点数不足 或 角色已死亡 则直接返回
        //if ((character.isInTurn && character.info.actionPoint < skill.actionPointTakes) || character.info.hp <= 0)
        //{
        //    yield break;
        //}

        //如果技能目标不是施法者自身, 就更新角色朝向
        if (targetGrid != character.nowGrid)
        {
            Vector3 direction = (targetGrid.transform.position - character.transform.position).normalized;
            direction.y = 0; //不改变角色y轴面向
            Quaternion startRotation = character.transform.rotation; //当前朝向
            Quaternion endRotation = Quaternion.LookRotation(direction); //终点朝向
            float rotateWeight = 0;
            while (Quaternion.Angle(character.transform.rotation, endRotation) > 0.5f)
            {
                rotateWeight += Time.deltaTime * character.info.rotateSpeed;
                character.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
                yield return null;
            }
            character.transform.rotation = endRotation;
        }

        //处理位移
        if (skill.shouldMoveToTarget)
        {
            GridHelper lastGrid = character.nowGrid;
            character.transform.position = targetGrid.transform.position;
            character.info.q = targetGrid.info.q;
            character.info.r = targetGrid.info.r;
            //如果在回合制中, 就修改playerGrids或者enemyGrids
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

        //得到技能施法范围内的所有网格
        HashSet<GridHelper> skillTargetGrids = new HashSet<GridHelper>();
        yield return GetSkillEffectsAreaGrids(character, skill, targetGrid, skillTargetGrids);

        //检测技能效果应用的角色对象
        HashSet<Character> effectsTarget = new HashSet<Character>();
        //如果角色在回合制中
        if (character.isInTurn)
        {
            //遍历施法范围内的所有网格, 如果包含TurnManager中有玩家或者敌人所在的格子, 就将格子上的角色加入effectsTarget中
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
        //如果角色不在回合制中
        else
        {
            //遍历施法范围内的所有网格, 在网格上方向下做射线检测, 如果检测到角色, 就加入effectsTarget中
            foreach (GridHelper grid in skillTargetGrids)
            {
                RaycastHit hit;
                Character targetCharacter = null;
                //绘制技能范围网格
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

        //根据技能影响角色类型 筛选 effectsTarget
        if (effectsTarget.Contains(character) && !skill.effectArea.affectSelf)
        {
            effectsTarget.Remove(character); //如果技能不影响施法者自身 且施法者在影响目标中, 就将自身移除
        }
        foreach (Character target in effectsTarget.ToList())
        {
            if ((character.tag == target.tag && character != target && !skill.effectArea.affectSelfCamp) //技能不影响同阵营单位, 且施法者和target是同阵营角色, 且target不是施法者自身
                || (character.tag != target.tag && !skill.effectArea.affectOppositeCamp)) //技能不影响敌方阵营单位, 且施法者和target是不同阵营角色
            {
                effectsTarget.Remove(target);
            }
            yield return null;
        }

        //如果技能造成伤害 或者 附加Buff, 就应用角色脚本中处理技能伤害和Buff效果的方法
        if (skill.damageDiceCount > 0 || skill.applicateBuffs.Count > 0)
        {
            yield return character.AppplySkillEffectsOnTarget(skill, effectsTarget);
        }

        //更新技能冷却和UI
        UpdateSkillInfoAfterUseSkill(character, skill);

        //如果角色在回合制中, 就减去消耗的行动点数
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
    /// 结束预施法, 以targetGrid为中心处理召唤类技能, 创建对应的召唤单位
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="grid"></param>
    /// <returns></returns>
    public IEnumerator ApplySummonSkillEffects(Character character, SummonSkillData skill, GridHelper targetGrid)
    {
        Debug.Log($"{character.info.name} ApplySummonSkillEffects {skill.skillName} to grid{targetGrid.info.q},{targetGrid.info.r}");

        //如果技能目标不是施法者自身, 就更新角色朝向
        if (targetGrid != character.nowGrid)
        {
            Vector3 direction = (targetGrid.transform.position - character.transform.position).normalized;
            direction.y = 0; //不改变角色y轴面向
            Quaternion startRotation = character.transform.rotation; //当前朝向
            Quaternion endRotation = Quaternion.LookRotation(direction); //终点朝向
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

        //获得召唤单位所在格子
        HashSet<GridHelper> summonedUnitGrids = new HashSet<GridHelper>();
        //如果技能的施法锚点是以可选择点为中心的, 就以鼠标选择的targetGrid为中心获得召唤单位格子
        if (skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
        {
            yield return InitializeSummonedUnitsGrids(character, skill, targetGrid, summonedUnitGrids);
        }
        //如果技能的施法锚点是以自身为中心的, 就以召唤者所在格子为中心获得召唤单位格子
        else
        {
            yield return InitializeSummonedUnitsGrids(character, skill, character.nowGrid, summonedUnitGrids);
        }
        Debug.Log("ApplySummonSkillEffects3");
        //应用角色脚本中创建召唤物的方法
        yield return character.CreatSummonedUnitOnTargetGrids(skill, summonedUnitGrids);

        //更新技能冷却和UI
        UpdateSkillInfoAfterUseSkill(character, skill);

        //如果角色在回合制中, 就减去消耗的行动点数
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
    /// 习得技能, 并更新对应角色SkillBar的UI
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    public bool LearnSkill(Character character, SkillBaseData skill)
    {
        //如果角色character已经习得技能skill, 则直接返回
        if(character.characterSkills.Any(characterSkill => characterSkill.skillName == skill.skillName))
        {
            return false;
        }
        //深拷贝要学习的技能
        SkillBaseData copy = Instantiate(skill);
        skill = copy;
        //向技能列表中添加skill
        character.characterSkills.Add(skill);

        //更新SkillBarUI
        UISkillBarManager.Instance.AddSkillIconToBar(character, skill);
        return true;
    }

    /// <summary>
    /// 移除技能, 并更新对应角色SkillBar的UI
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
                //如果character.characterSkills中依然没有相同skill实例, 则返回
                return;
            }
        }
        character.characterSkills.Remove(skill);

        //更新SkillBarUI
        UISkillBarManager.Instance.RemoveSkillIconFromBar(skill);
    }

    /// <summary>
    /// 得到以centerGrid为中心, radius为半径的圆形内的所有网格, 加入到grids中
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
        //bfs遍历方向
        Vector2Int[] dirs = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1)};
        //从centerGrid开始, 对半径radius的圆形以内的Vector2 (q, r)坐标 进行bfs遍历
        int currentRadius = 0;
        while (currentRadius <= radius)
        {
            int count = openSet.Count;
            //遍历半径currentRadius以内的网格(q, r)坐标
            for (int i = 0; i < count; i++)
            {
                Vector2Int current = openSet.Dequeue();
                closedSet.Add(current);

                foreach (Vector2Int dir in dirs)
                {
                    Vector2Int neighbor = current + dir; //邻居网格的(q, r)坐标
                    if (closedSet.Contains(neighbor)) //如果已经遍历过该网格, 就跳过
                    {
                        continue;
                    }
                    openSet.Enqueue(neighbor);
                }
            }

            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
            {
                yield return null; //每帧处理一部分, 提高性能, 下一帧继续处理
                nodesProcessedThisFrame = 0;
            }

            currentRadius++;
        }
        //如果网格不包含起始位置, 则移除
        if (!includeStartGrid)
        {
            closedSet.Remove(startPos);
        }
        //检查closedSet (q, r)集合, 确认对应GridHelper是否存在
        foreach(Vector3Int pos in closedSet)
        {
            GridHelper grid = GridMap.Instance.SearchGrid(pos.x, pos.y, 0);
            if (grid != null && !grids.Contains(grid))
            {
                grids.Add(grid); //如果存在坐标(q,r,s)对应的网格, 就加入grids
            }
        }
        yield break;
    }

    /// <summary>
    /// 得到以startGrid为起点, targetGrid为目标方向, radius为半径的扇形内的所有网格, 加入到grids中
    /// </summary>
    /// <param name="startGrid"></param>
    /// <param name="targetDirection"></param>
    /// <param name="radius"></param>
    /// <param name="grids"></param>
    /// <param name="includeStartGrid"></param>
    /// <returns></returns>
    private IEnumerator GetConeAreaGrids(GridHelper startGrid, Vector2Int targetDirection, int radius, HashSet<GridHelper> grids, bool includeStartGrid)
    {
        Vector2Int dir = new Vector2Int(-1, 0); //遍历时的横向搜索方向
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
        //从startGrid开始, 对半径radius的扇形以内的Vector2 (q, r)坐标 进行遍历
        int currentRadius = 0;
        while (currentRadius <= radius)
        {
            Vector2Int current = openSet.Dequeue();
            closedSet.Add(current);
            openSet.Enqueue(current + targetDirection);

            //遍历从current开始, 以dir为方向的同一行的网格(q, r)坐标
            for (int i = 0; i < currentRadius; i++)
            {
                Vector2Int oneRowGrid = current + dir * (i + 1); //同一行网格的(q, r)坐标
                if (closedSet.Contains(oneRowGrid)) //如果已经遍历过该网格, 就跳过
                {
                    continue;
                }
                closedSet.Add(oneRowGrid);
            }
            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
            {
                yield return null; //每帧处理一部分, 提高性能, 下一帧继续处理
                nodesProcessedThisFrame = 0;
            }

            currentRadius++;
        }
        //如果网格不包含起始位置, 则移除
        if (!includeStartGrid)
        {
            closedSet.Remove(startPos);
        }
        //检查closedSet (q, r)集合, 确认对应GridHelper是否存在
        foreach (Vector3Int pos in closedSet)
        {
            GridHelper grid = GridMap.Instance.SearchGrid(pos.x, pos.y, 0);
            if (grid != null && !grids.Contains(grid))
            {
                grids.Add(grid); //如果存在坐标(q,r,s)对应的网格, 就加入grids
            }
        }

        yield break;
    }

    /// <summary>
    /// 得到以startGrid为起点, targetGrid为目标方向, length为长度, width为宽度的直线上的所有网格, 加入到grids中
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
        Vector2Int dir = new Vector2Int(-1, 0); //遍历时的横向搜索方向
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
        //从startGrid开始, 对半径radius的扇形以内的Vector2 (q, r)坐标 进行遍历
        int currentLength = 0;
        while (currentLength <= length)
        {
            Vector2Int current = openSet.Dequeue();
            closedSet.Add(current);
            openSet.Enqueue(current + targetDirection);

            //遍历从current开始, 以dir为方向的同一行with宽度内的网格(q, r)坐标
            for (int i = 1; i < width; i++)
            {
                int index = Mathf.CeilToInt(i / 2.0f);
                index *= (i % 2 == 1) ? 1 : -1;
                Vector2Int oneRowGrid = current + dir * index; //同一行网格的(q, r)坐标
                if (closedSet.Contains(oneRowGrid)) //如果已经遍历过该网格, 就跳过
                {
                    continue;
                }
                closedSet.Add(oneRowGrid);
            }
            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
            {
                yield return null; //每帧处理一部分, 提高性能, 下一帧继续处理
                nodesProcessedThisFrame = 0;
            }

            currentLength++;
        }
        //如果网格不包含起始位置, 则移除起始行的网格坐标
        if (!includeStartGrid)
        {
            closedSet.Remove(startPos);
            for (int i = 0; i < width; i++)
            {
                Vector2Int startRowGrid = startPos + dir * (i + 1); //起始行网格的(q, r)坐标
                startRowGrid *= ((i % 2 == 1) ? -1 : 1);
                if (closedSet.Contains(startRowGrid)) //如果已经遍历过该网格, 就跳过
                {
                    closedSet.Remove(startRowGrid);
                }
            }
        }
        //检查closedSet (q, r)集合, 确认对应GridHelper是否存在
        foreach (Vector3Int pos in closedSet)
        {
            GridHelper grid = GridMap.Instance.SearchGrid(pos.x, pos.y, 0);
            if (grid != null && !grids.Contains(grid))
            {
                grids.Add(grid); //如果存在坐标(q,r,s)对应的网格, 就加入grids
            }
        }

        yield break;
    }

    /// <summary>
    /// 得到targetGrid相对于startGrid的目标方向(q, r, s)
    /// </summary>
    /// <param name="startGrid"></param>
    /// <param name="targetGrid"></param>
    /// <returns></returns>
    private Vector2Int GetTargetDirection(GridHelper startGrid, GridHelper targetGrid)
    {
        //如果targetGrid和startGrid相同, 则默认为(0,1)方向
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
    /// 初始化召唤单位所在格子列表
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
        //召唤1个单位
        else if (skill.summonedUnitCount == 1)
        {
            //尝试加入targetGrid
            TryAddSummonedUnitGrid(summoner, targetGrid, summonedUnitGrids);
            //如果格子数仍然不够, 就以targetGrid为中心进行bfs遍历, 直到summonUnitGrids的数量等于summonedUnitCount 或者 遍历完所有格子
            if (summonedUnitGrids.Count < 1)
            {
                yield return FillSummonedUnitGrids(summoner, skill.summonedUnitCount, targetGrid, summonedUnitGrids);
            }
        }
        //召唤2个单位
        else if (skill.summonedUnitCount == 2)
        {
            //尝试加入(-1,0), (1,0)两个方向的格子
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
        //召唤3个单位
        else if (skill.summonedUnitCount == 3)
        {
            //尝试加入targetGrid和(-1,0), (1,0)两个方向的格子
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
        //召唤4个单位
        else if (skill.summonedUnitCount == 4)
        {
            //尝试加入(-1,1), (0,1), (1,-1), (0,-1)四个方向的格子
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
        //召唤5个单位
        else if (skill.summonedUnitCount == 5)
        {
            //尝试加入targetGrid和(-1,1), (0,1), (1,-1), (0,-1)四个方向的格子
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
        //召唤6个单位
        else if (skill.summonedUnitCount == 6)
        {
            //尝试加入6个方向的格子
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
        //召唤7个单位
        else if (skill.summonedUnitCount == 7)
        {
            //尝试加入targetGird和6个方向的格子
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
        //召唤7个以上单位
        else
        {
            //以targetGrid为中心进行bfs遍历, 直到summonUnitGrids的数量等于summonedUnitCount 或者 遍历完所有格子
            yield return FillSummonedUnitGrids(summoner, skill.summonedUnitCount, targetGrid, summonedUnitGrids);
        }
    }

    /// <summary>
    /// 以targetGrid为中心进行bfs遍历, 将遍历到的格子添加到summonUnitGrids中, 直到summonUnitGrids的数量等于summonedUnitCount 或者 遍历完所有格子
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
                if (neighbor == null || summonedUnitGrids.Contains(neighbor)) //如果不存在该邻居网格, 或者 如果已经遍历过该网格, 就跳过
                {
                    continue;
                }
                openSet.Enqueue(neighbor);
            }

            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
            {
                yield return null; //每帧处理一部分, 提高性能, 下一帧继续处理
                nodesProcessedThisFrame = 0;
            }
            //如果处理的节点数超过一定界限, 就直接返回, 防止搜索时间过长
            if (nodesProcessedThisFrame > 200)
            {
                yield break;
            }
        }
        yield break;
    }

    /// <summary>
    /// 尝试向召唤单位所在格子列表中添加格子, 如果格子上有单位 或者 已经在列表中, 则添加失败; 如果没有单位, 则添加成功
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="summonUnitGrids"></param>
    private void TryAddSummonedUnitGrid(Character summoner, GridHelper grid, HashSet<GridHelper> summonedUnitGrids)
    {
        //格子已经在列表中
        if (summonedUnitGrids.Contains(grid))
        {
            return;
        }

        //召唤者在回合制中
        if (summoner.isInTurn)
        {
            if (TurnManager.Instance.playerGrids.ContainsKey(grid) || TurnManager.Instance.enemyGrids.ContainsKey(grid))
            {
                return;
            }
            summonedUnitGrids.Add(grid);
        }
        //召唤者不在回合制中
        else
        {
            RaycastHit hit;
            //绘制技能范围网格
            if (Physics.Raycast(grid.transform.position + Vector3.up * 50, Vector3.down, out hit, 100, LayerMask.GetMask("Character", "Model")))
            {
                return;
            }
            summonedUnitGrids.Add(grid);
        }
    }


    /// <summary>
    /// 得到技能施法最大范围内的所有网格, 加入grids中
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
    /// 得到技能释放范围内的所有网格, 加入grids中
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
    /// 得到EffectApplicationSkill释放范围内的所有网格, 加入grids中
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="targetGrid"></param>
    /// <param name="grids"></param>
    /// <returns></returns>
    private IEnumerator GetEffectApplicationSkillAreaGrids(Character character, EffectApplicationSkillData skill, GridHelper targetGrid, HashSet<GridHelper> grids)
    {
        //单体技能 SingleTarget
        if (skill.effectArea.targetingPattern == EffectTargetingPattern.SingleTarget)
        {
            if (!grids.Contains(targetGrid))
            {
                grids.Add(targetGrid);
            }
        }
        //圆形AOE技能 CircleAOE
        else if (skill.effectArea.targetingPattern == EffectTargetingPattern.CircleAOE)
        {
            //技能的施法锚点是以可选择点为中心的
            if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered)
            {
                yield return GetCircleAreaGrids(targetGrid, skill.effectArea.radius, grids, true);
            }
            //技能的施法锚点是以自身为中心的
            else
            {
                yield return GetCircleAreaGrids(character.nowGrid, skill.effectArea.radius, grids, true);
            }
        }
        //扇形AOE技能 ConeAOE
        else if (skill.effectArea.targetingPattern == EffectTargetingPattern.ConeAOE)
        {
            //技能的施法锚点是以可选择点为中心的
            if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered)
            {
                yield return GetConeAreaGrids(targetGrid, GetTargetDirection(character.nowGrid, targetGrid), skill.effectArea.radius, grids, true);
            }
            //技能的施法锚点是以自身为中心的
            else
            {
                yield return GetConeAreaGrids(character.nowGrid, GetTargetDirection(character.nowGrid, targetGrid), skill.effectArea.radius, grids, false);
            }
        }
        //线性AOE技能 ConeAOE
        else if (skill.effectArea.targetingPattern == EffectTargetingPattern.LineAOE)
        {
            //技能的施法锚点是以可选择点为中心的
            if (skill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered)
            {
                yield return GetLineAreaGrids(targetGrid, GetTargetDirection(character.nowGrid, targetGrid), skill.effectArea.radius, skill.effectArea.width, grids, true);
            }
            //技能的施法锚点是以自身为中心的
            else
            {
                yield return GetLineAreaGrids(character.nowGrid, GetTargetDirection(character.nowGrid, targetGrid), skill.effectArea.radius, skill.effectArea.width, grids, false);
            }
        }

        yield break;
    }

    /// <summary>
    /// 得到SummonSkill释放范围内的所有网格, 加入grids中
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    /// <param name="targetGrid"></param>
    /// <param name="grids"></param>
    /// <returns></returns>
    private IEnumerator GetSummonSkillAreaGrids(Character character, SummonSkillData skill, GridHelper targetGrid, HashSet<GridHelper> grids)
    {
        //技能的施法锚点是以可选择点为中心的
        if (skill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
        {
            yield return GetCircleAreaGrids(targetGrid, skill.summonArea.radius, grids, true);
        }
        //技能的施法锚点是以自身为中心的
        else
        {
            yield return GetCircleAreaGrids(character.nowGrid, skill.summonArea.radius, grids, true);
        }
    }

    /// <summary>
    /// 遍历grids, 渲染其中的边缘网格线, 并根据渲染用途, 将用于渲染的LienRenderer加入到lineRenderers中
    /// </summary>
    /// <param name="grids"></param>
    /// <param name="lineRenderers"></param>
    /// <returns></returns>
    private IEnumerator DrawEdgeGrids(HashSet<GridHelper> grids, List<LineRenderer> lineRenderers)
    {
        if (skillMaxDistanceMaterial == null || skillTargetMaterial == null) //确认LineRenderer所需的材质是否存在
        {
            yield break;
        }
        //先清空之前用来绘制的lineRenderers, 将所有LineRenderer还给对象池
        foreach (LineRenderer lr in lineRenderers)
        {
            lr.material = outlineMaterial;
            LineRendererPool.Instance.ReturnLineRenderer(lr);
        }
        lineRenderers.Clear();

        int nodesProcessedThisFrame = 0;
        //遍历grids中的每一个网格grid
        foreach (GridHelper grid in grids)
        {
            GridInfo info = grid.info;
            //遍历grid的每一个邻居网格grid.neighborGrids[i]
            for (int i = 0; i < 6; i++)
            {
                //如果邻居网格不在grids中, 说明该方向的网格线为边缘网格线, 需要渲染
                if (!grids.Contains(grid.neighborGrids[i]))
                {
                    int next = (i + 1) % 6;
                    LineRenderer lr = LineRendererPool.Instance.GetLineRenderer();
                    lr.material = lineRenderers == maxDistanceEdgeGridsLineRenderers ? skillMaxDistanceMaterial : skillTargetMaterial; //选择材质
                    lr.transform.SetParent(linesParent);
                    lr.positionCount = 2;
                    lr.SetPositions(new Vector3[]{ info.vertices[i], info.vertices[next]});
                    lineRenderers.Add(lr); //记录正在使用的LineRenderer
                }
            }
            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
            {
                yield return null; //每帧处理一部分, 提高性能, 下一帧继续处理
                nodesProcessedThisFrame = 0;
            }
        }
        yield break;
    }

    /// <summary>
    /// 将lineRenderers中用于渲染网格线的LineRenderer还给对象池
    /// </summary>
    /// <param name="lineRenderers"></param>
    /// <returns></returns>
    private IEnumerator ReturnEdgeGridsToPool(List<LineRenderer> lineRenderers)
    {
        if (outlineMaterial == null) //确认LineRenderer所需的材质是否存在
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



    #region CharacterAI相关
    /// <summary>
    /// 让AI角色character释放技能, 以角色target为目标为目标处理技能效果的应用
    /// </summary>
    /// <param name="character"></param>
    /// <param name="target"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    public IEnumerator ApplyCharacterAISkillEffects(Character character, Character target, SkillBaseData skill)
    {
        //效果应用类技能
        if (skill is EffectApplicationSkillData)
        {
            EffectApplicationSkillData effectApplicationSkill = skill as EffectApplicationSkillData;
            //如果技能释放锚点以自身为中心, 且character和target的距离大于技能范围半径, 就先移动到技能可以命中的地方
            if (effectApplicationSkill.effectArea.effectAnchor == EffectAnchorMode.selfCentered && GetDistance(character.nowGrid, target.nowGrid) > effectApplicationSkill.effectArea.radius)
            {
                CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, effectApplicationSkill.effectArea.radius), character.info.name);
                CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1), character.info.name);

                //yield return character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, effectApplicationSkill.effectArea.radius);
                //yield return character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1);
            }
            //如果技能释放锚点以可选择点为中心, 且charater和target的距离大于技能最大施法距离, 就先移动到技能可以命中的地方
            else if (effectApplicationSkill.effectArea.effectAnchor == EffectAnchorMode.selectableCentered && GetDistance(character.nowGrid, target.nowGrid) > effectApplicationSkill.effectArea.maxDistance)
            {
                CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, effectApplicationSkill.effectArea.maxDistance), character.info.name);
                CoroutineManager.Instance.AddTaskToGroup(character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1), character.info.name);

                //yield return character.pathfinder.FindAttackPath(character.nowGrid, target.nowGrid, effectApplicationSkill.effectArea.maxDistance);
                //yield return character.pathfinder.CharacterMoveInTurn(character.info.runSpeed, character.info.rotateSpeed, 1);
            }
            //如果移动后, 角色行动点数不足以释放技能, 就直接返回
            if (character.info.actionPoint < effectApplicationSkill.actionPointTakes)
            {
                yield break;
            }
            //应用单体技能效果
            if (effectApplicationSkill.effectArea.targetingPattern == EffectTargetingPattern.SingleTarget)
            {
                yield return ApplyEffectApplicationSkillEffects(character, effectApplicationSkill, target);
            }
            //应用AOE技能效果
            else
            {
                yield return ApplyEffectApplicationSkillEffects(character, effectApplicationSkill, target.nowGrid);
            }
        }
        //召唤类技能
        else if (skill is SummonSkillData)
        {
            SummonSkillData summonSkill = skill as SummonSkillData;
            //如果技能释放锚点以自身为中心
            if (summonSkill.summonArea.effectAnchor == EffectAnchorMode.selfCentered)
            {
                yield return ApplySummonSkillEffects(character, summonSkill, character.nowGrid);
            }
            //如果技能释放锚点以可选择点为中心
            else if (summonSkill.summonArea.effectAnchor == EffectAnchorMode.selectableCentered)
            {
                GridHelper targetGrid = null; //召唤技能释放锚点
                float minDistance = float.MaxValue; //技能释放锚点距离target目标的距离
                HashSet<GridHelper> maxDistanceGrids = new HashSet<GridHelper>();
                yield return GetSkillMaxDistanceAreaGrids(character, skill, maxDistanceGrids);
                //找到技能技能最大施法距离范围内 距离target最近的格子
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
                //如果没有找到格子, 就返回
                if(targetGrid == null)
                {
                    yield break;
                }
                //应用召唤技能
                yield return ApplySummonSkillEffects(character, summonSkill, targetGrid);
            }
        }
    }
    #endregion



    #region SkillBarUI相关
    /// <summary>
    /// 角色使用
    /// </summary>
    /// <param name="character"></param>
    /// <param name="skill"></param>
    private void UpdateSkillInfoAfterUseSkill(Character character, SkillBaseData skill)
    {
        skill.remainingTurns = skill.cooldownTurns;
        //如果角色是玩家队伍角色, 更新SkillBarUI
        if (character.CompareTag("Player"))
        {
            UISkillBarManager.Instance.UpdateSkillBarUI(skill);
        }
        //如果角色不在回合制中, 就开启即时制计时协程
        if (!character.isInTurn)
        {
            CoroutineManager.Instance.AddTaskToGroup(ResumeSkillAfterCooldownTurnsDalay(character, skill), character.info.name + skill.skillName);
            CoroutineManager.Instance.StartGroup(character.info.name + skill.skillName);
        }
    }

    /// <summary>
    /// 用于回合制管理器中, 每当一名角色回合结束, 更新其身上的Skill的冷却剩余回合数和UI
    /// </summary>
    /// <param name="character"></param>
    public void UpdateSkillInfoAfterFinishTurn(Character character)
    {
        foreach (SkillBaseData skill in character.characterSkills)
        {
            //回合结束只更新冷却剩余回合数大于0的技能
            if (skill.remainingTurns > 0)
            {
                skill.remainingTurns--; //buff剩余回合-1

                //如果角色是玩家队伍角色, 更新SkillBarUI
                if (character.CompareTag("Player"))
                {
                    UISkillBarManager.Instance.UpdateSkillBarUI(skill);
                }
            }
        }
    }

    /// <summary>
    /// 回合制结束时, 将角色身上的Skill从即时制计时转为回合制计时
    /// </summary>
    /// <param name="character"></param>
    public void UpdateSkillBarUIAfterEndTurn(Character character)
    {
        foreach (SkillBaseData skill in character.characterSkills)
        {
            //回合结束, 对角色身上冷却剩余回合数大于0的Skill开启即时制计时协程
            if (skill.remainingTurns > 0)
            {
                CoroutineManager.Instance.AddTaskToGroup(ResumeSkillAfterCooldownTurnsDalay(character, skill), character.info.name + skill.skillName);
                CoroutineManager.Instance.StartGroup(character.info.name + skill.skillName);
            }
        }
    }

    /// <summary>
    /// 回合制开始时, 将角色身上的Skill从回合制计时转为即时制计时, 停止角色身上每一个Skill的即时制计时协程
    /// </summary>
    /// <param name="character"></param>
    public void UpdateSkillBarUIAfterStartTurn(Character character)
    {
        foreach (SkillBaseData skill in character.characterSkills)
        {
            //回合开始停止角色身上每一个冷却剩余回合数大于0的Skill的即时制计时协程
            if (skill.remainingTurns > 0)
            {
                CoroutineManager.Instance.StopGroup(character.info.name + skill.skillName);
            }
        }
    }

    /// <summary>
    /// 用于非回合状态(即时制状态)下, 在cooldownTurns中更新技能冷却剩余回合数, 待remainingTurns==0, 冷却结束, 恢复该技能
    /// </summary>
    /// <param name="skill"></param>
    /// <returns></returns>
    public IEnumerator ResumeSkillAfterCooldownTurnsDalay(Character character, SkillBaseData skill)
    {
        while (skill.remainingTurns > 0)
        {
            yield return oneTurnTime; //一回合等价于6s, 等待一回合时间

            skill.remainingTurns--; //持续回合-1

            //如果角色是玩家队伍角色, 更新SkillBarUI
            if (character.CompareTag("Player"))
            {
                UISkillBarManager.Instance.UpdateSkillBarUI(skill);
            }
        }
    }
    #endregion
}
