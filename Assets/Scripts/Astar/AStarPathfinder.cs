using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// A*寻路算法, 请将脚本挂载到角色上, 用于寻路
/// </summary>
public class AStarPathfinder : MonoBehaviour
{
    public float heightWeight = 0.5f; //寻路时的高度权重
    public Character character; //寻路脚本所控制的角色的Character脚本
    public Animator animator; //寻路脚本所控制的角色的Animator脚本
    public int maxNodesProcessedPerFrame = 500; //每帧最多处理的网格数
    //public bool isMoving = false; //用于调整镜头移动的细节

    private List<GridHelper> path = new List<GridHelper>(); //用于记录角色的移动路径
    private List<GridHelper> nearGrids = new List<GridHelper>(); //用于得到路径终点的附近网格

    public Transform linesParent; //路径网格线的父对象
    private Material outlineMaterial; //对象池中网格的本身材质
    private Material reachableMaterial; //可以到达的网格的渲染材质
    private Material unreachableMaterial; //不可以到达的网格的渲染材质
    private List<LineRenderer> pathGridsLineRenderer = new List<LineRenderer>(); //记录用于渲染path路径网格的LineRenderer

    //A*节点
    private class AstarNode : IComparable<AstarNode>
    {
        public GridHelper grid;
        public AstarNode comeFromNode;
        public float g; //从起点网格 到该网格的路径代价
        public float h; //从该网格 到终点网格的预估代价(启发式估算)
        public float f => g + h; //A*代价

        public AstarNode(GridHelper grid, AstarNode comeFromNode, float g, float h)
        {
            this.grid = grid;
            this.comeFromNode = comeFromNode;
            this.g = g;
            this.h = h;
        }

        public int CompareTo(AstarNode other)
        {
            return f.CompareTo(other.f);
        }
    }

    public void Start()
    {
        if (character == null)
        {
            character = this.GetComponent<Character>(); //绑定Character脚本
        }
        if (animator == null)
        {
            animator = this.GetComponent<Animator>(); //绑定Animator脚本
        }
        //初始化网格材质
        outlineMaterial = Resources.Load<Material>("Materials/outlineRendererMaterial");
        reachableMaterial = Resources.Load<Material>("Materials/reachableMaterial");
        unreachableMaterial = Resources.Load<Material>("Materials/unreachableMaterial");
    }

    /// <summary>
    /// 寻找从网格start到end的协程方法
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="moveSpeed"></param>
    /// <returns></returns>
    public IEnumerator FindPath(GridHelper start, GridHelper end)
    {
        //用最小堆存放待处理的网格节点, 每次排序取出f值最小的, 类似于C++中的priority_queue
        MinHeap<AstarNode> openSet = new MinHeap<AstarNode>();
        HashSet<GridHelper> closedSet = new HashSet<GridHelper>(); //存放已处理的节点

        Dictionary<GridHelper, AstarNode> openSetDic = new Dictionary<GridHelper, AstarNode>(); //用来快速查找节点是否在openSet中

        //初始化起点
        AstarNode startNode = new AstarNode(start, null, 0, HeuristicCostEstimate(start, end));
        openSet.Enqueue(startNode);
        openSetDic.Add(start, startNode);

        int nodesProcessedThisFrame = 0; //每帧处理的节点数量
        while (openSet.Count > 0)
        {
            //从堆中取出f最小的节点
            AstarNode current = openSet.Dequeue();

            //如果找到目标网格节点, 就再开启一个新协程, 重建路径, 并根据路径将角色移动到目标网格
            if(current.grid == end)
            {
                //逆向追踪路径, 重建路径
                path.Clear();
                while (current.grid != start)
                {
                    path.Add(current.grid);
                    current = current.comeFromNode;
                }
                path.Reverse();
                yield break;
            }

            //处理当前节点, 标记为已处理
            openSetDic.Remove(current.grid);
            closedSet.Add(current.grid);

            //遍历当前节点的所有邻居节点
            foreach(GridHelper neighbor in current.grid.neighborGrids)
            {
                //如果邻居节点不可行走或已处理, 跳过
                if (neighbor == null || !neighbor.info.walkable || closedSet.Contains(neighbor))
                {
                    continue;
                }
                
                //邻居节点的g值(代价受高度影响)
                float g = current.g + neighbor.info.movePrice + 
                    Mathf.Abs(current.grid.info.centerPos.y - neighbor.info.centerPos.y) * heightWeight;
                //邻居节点的h值
                float h = HeuristicCostEstimate(neighbor, end);

                //现在的邻居节点一定不在已处理列表中, 因此该邻居节点要么还未加入openSet, 要么已经加入但还未被处理
                //如果邻居节点未在待处理列表中
                if(!openSetDic.ContainsKey(neighbor))
                {
                    //记录到达该neighbor的路径, 将其A*节点加入待处理列表
                    AstarNode neighborNode = new AstarNode(neighbor, current, g, h);
                    openSet.Enqueue(neighborNode);
                    openSetDic.Add(neighbor, neighborNode);
                }
                //或者 邻居节点在待处理列表中, 但还未被处理, 且通过current到该neighbor的路径比之前记录的更短
                else if (openSetDic.ContainsKey(neighbor) && openSetDic[neighbor].f > g + h)
                {
                    //记录更短的新路径
                    openSetDic[neighbor].comeFromNode = current;
                    openSetDic[neighbor].g = g;
                    openSetDic[neighbor].h = h;
                }
                nodesProcessedThisFrame++;
                if(nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
                {
                    yield return null; //每帧处理一部分, 提高性能, 下一帧继续处理
                    nodesProcessedThisFrame = 0;
                }
            }
        }
        yield break;
        //如果代码执行到这里, 说明没有找到路径
    }

    /// <summary>
    /// 攻击时的寻路方法, 攻击目标进入范围后角色即停止寻路 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="moveSpeed"></param>
    /// <param name="rotateSpeed"></param>
    /// <param name="atkDistance">角色攻击距离</param>
    /// <returns></returns>
    public IEnumerator FindAttackPath(GridHelper start, GridHelper end, int attackDistance)
    {
        MinHeap<AstarNode> openSet = new MinHeap<AstarNode>();
        HashSet<GridHelper> closedSet = new HashSet<GridHelper>();

        Dictionary<GridHelper, AstarNode> openSetDic = new Dictionary<GridHelper, AstarNode>();

        AstarNode startNode = new AstarNode(start, null, 0, HeuristicCostEstimate(start, end));
        openSet.Enqueue(startNode);
        openSetDic.Add(start, startNode);

        int nodesProcessedThisFrame = 0;
        while (openSet.Count > 0)
        {
            AstarNode current = openSet.Dequeue();

            //如果当前处理的节点和目标网格节点之间的距离 小于 攻击距离, 就表明找到路径
            if (GetDistance(current.grid, end) <= attackDistance) 
            {
                //逆向追踪路径, 重建路径
                //List<GridHelper> path = new List<GridHelper>();
                path.Clear();
                while (current.grid != start)
                {
                    path.Add(current.grid);
                    current = current.comeFromNode;
                }
                path.Reverse();
                yield break;
            }

            openSetDic.Remove(current.grid);
            closedSet.Add(current.grid);

            foreach (GridHelper neighbor in current.grid.neighborGrids)
            {
                if (neighbor == null || !neighbor.info.walkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                float g = current.g + neighbor.info.movePrice +
                    Mathf.Abs(current.grid.info.centerPos.y - neighbor.info.centerPos.y) * heightWeight;
                float h = HeuristicCostEstimate(neighbor, end);

                if (!openSetDic.ContainsKey(neighbor))
                {
                    AstarNode neighborNode = new AstarNode(neighbor, current, g, h);
                    openSet.Enqueue(neighborNode);
                    openSetDic.Add(neighbor, neighborNode);
                }
                else if (openSetDic.ContainsKey(neighbor) && openSetDic[neighbor].f > g + h)
                {
                    openSetDic[neighbor].comeFromNode = current;
                    openSetDic[neighbor].g = g;
                    openSetDic[neighbor].h = h;
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
    /// 根据path路径绘制网格线
    /// </summary>
    /// <returns></returns>
    public IEnumerator DrawPathGrids(Action<float> calculateCost)
    {
        if(outlineMaterial == null || reachableMaterial == null || unreachableMaterial == null) //确认LineRenderer所需的材质是否存在
        {
            yield break;
        }
        //先清空之前用来绘制的pathGridsLineRenderer, 将所有LineRenderer还给对象池
        foreach (LineRenderer lr in pathGridsLineRenderer)
        {
            lr.material = outlineMaterial;
            LineRendererPool.Instance.ReturnLineRenderer(lr);
        }
        pathGridsLineRenderer.Clear();

        float apTakes = 0f; //到达终点path所需的总行动点数
        float apTakePerGrid = 1.0f / character.info.runSpeed; //每格所消耗的行动点数
        foreach(GridHelper grid in path)
        {
            GridInfo info = grid.info;
            LineRenderer lr = LineRendererPool.Instance.GetLineRenderer();
            //根据是否可到达选择渲染材质
            apTakes += apTakePerGrid;
            //Debug.Log($"apTakes{apTakes}");
            lr.material = apTakes < character.info.actionPoint ? reachableMaterial : unreachableMaterial;

            lr.transform.SetParent(linesParent);
            lr.positionCount = 6;
            lr.SetPositions(new Vector3[] { info.vertices[0],
                    info.vertices[1], info.vertices[2],
                    info.vertices[3], info.vertices[4],
                    info.vertices[5]});
            pathGridsLineRenderer.Add(lr);
        }
        calculateCost(apTakes);
        //Debug.Log("Draw Path Grids Finish");
        yield break;
    }

    public IEnumerator ReturnGridsToPool()
    {
        if (outlineMaterial == null) //确认LineRenderer所需的材质是否存在
        {
            yield break;
        }

        foreach (LineRenderer lr in pathGridsLineRenderer)
        {
            lr.material = outlineMaterial;
            LineRendererPool.Instance.ReturnLineRenderer(lr);
        }
        pathGridsLineRenderer.Clear();
        //Debug.Log("Return Path Grids Finish");
        yield break;
    }


    /// <summary>
    /// 根据路径path将该寻路脚本所挂载到的角色对象移动到目标网格
    /// </summary>
    /// <param name="moveSpeed"></param>
    /// <param name="rotateSpeed"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    public IEnumerator CharacterMove(float moveSpeed, float rotateSpeed, float weight)
    {
        animator.SetBool("isMoving", true); //播放WalkToRun动画
        animator.SetFloat("speed", weight); //设置移动动画混合权重
        character.isMoving = true;

        //逐网格移动角色
        if (moveSpeed > rotateSpeed) //确保旋转速度大于等于移动速度, 从而确保到达终点之前一定能完成转向
        {
            rotateSpeed = moveSpeed;
        }
        foreach(GridHelper grid in path.ToList())
        {
            Vector3 startPos = this.transform.position; //当前位置
            Vector3 endPos = grid.info.centerPos; //终点位置
            Vector3 direction = (endPos - startPos).normalized;
            direction.y = 0; //不改变角色y轴面向
            Quaternion startRotation = this.transform.rotation; //当前朝向
            Quaternion endRotation = Quaternion.LookRotation(direction); //终点朝向

            float moveWeight = 0;
            float rotateWeight = 0;
            //转向 + 移动
            while (Vector3.Distance(this.transform.position, grid.info.centerPos) > 0.05f)
            {
                moveWeight += Time.deltaTime * moveSpeed;
                rotateWeight += Time.deltaTime * rotateSpeed;
                //动态调整转向速度, 接近终点时转向减慢
                float dynamicRotateSpeed = Mathf.Max(rotateSpeed * (1 - moveWeight), 0);
                float dynamicRotateWeight = Time.deltaTime * dynamicRotateSpeed;
                this.transform.position = Vector3.Lerp(startPos, endPos, moveWeight);
                this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight + dynamicRotateWeight);
                yield return null;
            }
            transform.position = endPos;
            transform.rotation = endRotation;
            //更改character中的位置信息
            character.info.q = grid.info.q;
            character.info.r = grid.info.r;
            character.info.heightOrder = grid.info.heightOrder;
        }

        character.isMoving = false;
        animator.SetBool("isMoving", false); //结束播放WalkToRun动画, 回到Idle
    }

    /// <summary>
    /// 在回合制中, 根据路径path将该寻路脚本所挂载到的角色对象移动到最远的可到达网格, 并能在行走过程中不断更新减少角色行动点数, 以及检测敌人, 触发借机攻击和反击
    /// </summary>
    /// <param name="moveSpeed"></param>
    /// <param name="rotateSpeed"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    public IEnumerator CharacterMoveInTurn(float moveSpeed, float rotateSpeed, float weight)
    {
        if(character.info.actionPoint <= 0 || path.Count == 0) //判断初始行动点数是否能行走 或者 是否存在路径
        {
            yield break;
        }
        Debug.Log("Move Start");


        character.isMoving = true;
        //更新角色所在位置的行走限制
        character.nowGrid.info.dynamicLimit = false;
        character.nowGrid.info.walkable = true;
        //判断角色所属阵营, 更新角色所在阵营的格子
        bool isPlayer = false;
        if (this.CompareTag("Player"))
        {
            TurnManager.Instance.playerGrids.Remove(character.nowGrid);
            isPlayer = true;
        }
        else if (this.CompareTag("Enemy"))
        {
            TurnManager.Instance.enemyGrids.Remove(character.nowGrid);
        }
        //这次Move过程中发起借机攻击的角色
        HashSet<Character> opportunityAttackerList = new HashSet<Character>();

        animator.SetBool("isMoving", true); //播放WalkToRun动画
        animator.SetFloat("speed", weight); //设置移动动画混合权重

        //逐网格移动角色
        if (moveSpeed > rotateSpeed) //确保旋转速度大于等于移动速度, 从而确保到达终点之前一定能完成转向
        {
            rotateSpeed = moveSpeed;
        }
        float apTakePerGrid = 1.0f / character.info.runSpeed; //每格所消耗的行动点数
        foreach (GridHelper grid in path.ToList())
        {
            if (character.info.actionPoint <= 0 || character.info.hp <= 0)
            {
                yield break;
            }

            Vector3 startPos = this.transform.position; //当前位置
            Vector3 endPos = grid.info.centerPos; //终点位置
            Vector3 direction = (endPos - startPos).normalized;
            direction.y = 0; //不改变角色y轴面向
            Quaternion startRotation = this.transform.rotation; //当前朝向
            Quaternion endRotation = Quaternion.LookRotation(direction); //终点朝向

            float moveWeight = 0;
            float rotateWeight = 0;
            //转向 + 移动
            while (Vector3.Distance(this.transform.position, grid.info.centerPos) > 0.05f)
            {
                moveWeight += Time.deltaTime * moveSpeed;
                rotateWeight += Time.deltaTime * rotateSpeed;
                //动态调整转向速度, 接近终点时转向减慢
                float dynamicRotateSpeed = Mathf.Max(rotateSpeed * (1 - moveWeight), 0);
                float dynamicRotateWeight = Time.deltaTime * dynamicRotateSpeed;
                this.transform.position = Vector3.Lerp(startPos, endPos, moveWeight);
                this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight + dynamicRotateWeight);
                yield return null;
            }
            transform.position = endPos;
            transform.rotation = endRotation;

            //判断借机攻击
            bool needOpportunityAndCounterAttack = false;
            if (isPlayer) //如果正在移动的角色是玩家
            {
                foreach(GridHelper neighbor in character.nowGrid.neighborGrids.ToList())
                {
                    //如果邻居格子上存在不同阵营的角色
                    if (neighbor != null && TurnManager.Instance.enemyGrids.ContainsKey(neighbor))
                    {
                        Character opportunityAttacker = TurnManager.Instance.enemyGrids[neighbor];
                        //并且没有发起过借机攻击
                        if (!opportunityAttackerList.Contains(opportunityAttacker))
                        {
                            needOpportunityAndCounterAttack = true; //需要进行借机攻击和反击
                            opportunityAttackerList.Add(opportunityAttacker); //加入已经借机攻击过的角色列表
                            //发起借机攻击
                            Debug.Log("OpportunityAttack");
                            //CoroutineManager.Instance.PauseGroup(character.info.name); //暂停当前角色的协程队列
                            animator.SetBool("isMoving", false); //暂停移动动画
                            CoroutineManager.Instance.AddTaskToGroup(opportunityAttacker.OpportunityAttack(character), "OpportunityAndCounterAttack"); //借机攻击
                            CoroutineManager.Instance.AddTaskToGroup(character.CounterAttack(opportunityAttacker), "OpportunityAndCounterAttack"); //反击
                        }
                    }
                }
            }
            else //如果正在移动的角色是敌人
            {
                foreach (GridHelper neighbor in character.nowGrid.neighborGrids.ToList())
                {
                    //如果邻居格子上存在不同阵营的角色
                    if (neighbor != null && TurnManager.Instance.playerGrids.ContainsKey(neighbor))
                    {
                        Character opportunityAttacker = TurnManager.Instance.playerGrids[neighbor];
                        //并且没有发起过借机攻击
                        if (!opportunityAttackerList.Contains(opportunityAttacker))
                        {
                            needOpportunityAndCounterAttack = true; //需要进行借机攻击和反击
                            opportunityAttackerList.Add(opportunityAttacker); //加入已经借机攻击过的角色列表
                            //发起借机攻击
                            Debug.Log("OpportunityAttack");
                            //CoroutineManager.Instance.PauseGroup(character.info.name); //暂停当前角色的协程队列
                            animator.SetBool("isMoving", false); //暂停移动动画
                            CoroutineManager.Instance.AddTaskToGroup(opportunityAttacker.OpportunityAttack(character), "OpportunityAndCounterAttack"); //借机攻击
                            CoroutineManager.Instance.AddTaskToGroup(character.CounterAttack(opportunityAttacker), "OpportunityAndCounterAttack"); //反击
                        }
                    }
                }
            }
            yield return null;

            if (needOpportunityAndCounterAttack) //如果需要借机攻击和反击
            {
                CoroutineManager.Instance.PauseGroup(character.info.name); //暂停当前角色的协程队列
                //绑定回调
                CoroutineManager.Instance.BindCallback("OpportunityAndCounterAttack", () =>
                {
                    CoroutineManager.Instance.ResumeGroup(character.info.name); //恢复角色被暂停的协程队列
                    if (character.info.actionPoint > 0 && character.info.hp > 0)
                    {
                        animator.SetBool("isMoving", true); //恢复角色移动动画
                    }
                    Debug.Log("Resume");
                });
                CoroutineManager.Instance.StartGroup("OpportunityAndCounterAttack");
                //yield return new WaitWhile(() => !CoroutineManager.Instance.TaskInGroupIsEmpty("OpportunityAndCounterAttack"));
            }

            //更改character中的位置信息
            character.info.q = grid.info.q;
            character.info.r = grid.info.r;
            character.info.heightOrder = grid.info.heightOrder;

            //更改角色行动点数
            character.info.actionPoint -= apTakePerGrid;
            UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, 0);
            if (character.info.actionPoint <= 0)
            {
                break;
            }
            yield return null;
        }

        character.isMoving = false;
        animator.SetBool("isMoving", false); //结束播放WalkToRun动画, 回到Idle

        //更新角色所在位置的行走限制
        character.nowGrid.info.dynamicLimit = true;
        character.nowGrid.info.walkable = false;
        //更新角色所在阵营的格子
        if (isPlayer)
        {
            TurnManager.Instance.playerGrids.Add(character.nowGrid, character);
        }
        else
        {
            TurnManager.Instance.enemyGrids.Add(character.nowGrid, character);
        }

        Debug.Log("Move Finish");
        yield return null;
    }

    //private IEnumerator Test()
    //{
    //    yield return new WaitForSeconds(3f);
    //    Debug.Log("Test");
    //    yield return new WaitForSeconds(3f);
    //}

    /// <summary>
    /// 根据Character脚本中的nowGrid修正角色位置
    /// </summary>
    /// <returns></returns>
    public IEnumerator CorrectCharacterPositionInTurn(List<Character> triggers)
    {
        if (triggers.Count == 0)
        {
            yield break;
        }

        character.isMoving = true;
        animator.SetBool("isMoving", true); //播放WalkToRun动画
        animator.SetFloat("speed", 0);

        Vector3 startPos = this.transform.position; //起点位置
        Vector3 endPos = character.nowGrid.info.centerPos; //终点位置
        Vector3 direction = (endPos - this.transform.position).normalized;
        direction.y = 0; //不改变角色y轴面向
        Quaternion startRotation = this.transform.rotation; //当前朝向
        Quaternion endRotation = Quaternion.LookRotation(direction); //终点朝向

        float moveSpeed = character.info.walkSpeed;
        float rotateSpeed = character.info.rotateSpeed;
        float moveWeight = 0;
        float rotateWeight = 0;
        //转向 + 移动
        while (Vector3.Distance(this.transform.position, endPos) > 0.05f)
        {
            moveWeight += Time.deltaTime * moveSpeed;
            rotateWeight += Time.deltaTime * rotateSpeed;
            this.transform.position = Vector3.Lerp(startPos, endPos, moveWeight);
            this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
            yield return null;
        }
        transform.position = endPos;
        transform.rotation = endRotation;

        character.isMoving = false;
        animator.SetBool("isMoving", false);

        //如果回合制由玩家主动调入, 则不需要再更改角色朝向
        if(triggers.Count <= 1)
        {
            yield break;
        }

        //更改角色朝向, 玩家角色面向敌人, 敌人面向玩家
        rotateWeight = 0;
        if (CompareTag("Player"))
        {
            endRotation = Quaternion.LookRotation(triggers[triggers.Count - 1].transform.position - transform.position);
        }
        else if (CompareTag("Enemy"))
        {
            endRotation = Quaternion.LookRotation(triggers[0].transform.position - transform.position);
        }
        while (Quaternion.Angle(this.transform.rotation, endRotation) > 0.05f)
        {
            rotateWeight += Time.deltaTime * rotateSpeed * 0.5f;
            this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
            yield return null;
        }
        transform.rotation = endRotation;
    }


    /// <summary>
    /// 获得路径path终点附近的网格, 用作队友跟随
    /// </summary>
    /// <param name="groupCount"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public IEnumerator GetNearGrids(int groupCount, Action<List<GridHelper>> callback)
    {
        //path.Count <= 1: 如果主控角色没有移动, 则队友也不移动;
        //groupCount == 0: 作为主控没有队友, 也不执行
        if (path.Count <= 1 || groupCount == 0)
        {
            yield break;
        }

        nearGrids.Clear();
        GridHelper targetGrid = path[path.Count - 1];
        //如果至少移动了3格, 则优先将队友移动到路径上的倒数第4格, 以及与倒数第4格形成120角的倒数第3格的两个邻居网格
        if (nearGrids.Count < groupCount && path.Count >= 4)
        {
            GridHelper lastBut4 = path[path.Count - 4]; //倒数第4格
            GridHelper lastBut3 = path[path.Count - 3]; //倒数第3格
            int dir = 0; //确定倒数第4格在倒数第3格的哪个方向
            for (; dir < 6; dir++)
            {
                if (lastBut3.neighborGrids[dir] == lastBut4)
                {
                    break;
                }
                yield return null;
            }
            //如果该附近网格没有被记录过, 并且不是主控角色到达的终点网格, 就加入nearGrids中
            TryAddNearGrids(lastBut3.neighborGrids[(dir + 2) % 6], targetGrid);
            TryAddNearGrids(lastBut3.neighborGrids[(dir + 4) % 6], targetGrid);
            TryAddNearGrids(lastBut4, targetGrid);
        }
        //如果至少移动了2格, 则次优先将队友移动到路径上的倒数第3格, 以及与倒数第3格形成120角的倒数第2格的两个邻居网格
        if (nearGrids.Count < groupCount && path.Count >= 3)
        {
            GridHelper lastBut3 = path[path.Count - 3]; //倒数第3格
            GridHelper lastBut2 = path[path.Count - 2]; //倒数第2格
            int dir = 0;
            for (; dir < 6; dir++)
            {
                if (lastBut2.neighborGrids[dir] == lastBut3)
                {
                    break;
                }
                yield return null;
            }
            TryAddNearGrids(lastBut2.neighborGrids[(dir + 2) % 6], targetGrid);
            TryAddNearGrids(lastBut2.neighborGrids[(dir + 4) % 6], targetGrid);
            TryAddNearGrids(lastBut3, targetGrid);
        }
        //如果至少移动了1格, 则次次优先将队友移动到倒数第2格, 以及与倒数第2格、第1格都紧邻的网格
        if (nearGrids.Count < groupCount && path.Count >= 2)
        {
            GridHelper lastBut2 = path[path.Count - 2]; //倒数第2格
            int dir = 0;
            for (; dir < 6; dir++)
            {
                if (targetGrid.neighborGrids[dir] == lastBut2)
                {
                    break;
                }
                yield return null;
            }
            TryAddNearGrids(targetGrid.neighborGrids[(dir + 1) % 6], targetGrid);
            TryAddNearGrids(targetGrid.neighborGrids[(dir - 1) % 6], targetGrid);
            TryAddNearGrids(lastBut2, targetGrid);
        }
        //如果以上规则得到的附近网格不够, 或者 还有更多队友/npc/召唤物需要跟随移动, 则从倒数第5格开始倒序遍历path, 或者 向path以外的区域搜索
        int index = 5; //倒数第五个网格
        while (nearGrids.Count < groupCount && index <= path.Count)
        {
            nearGrids.Add(path[path.Count - index]);
            index++;
            yield return null;
        }
        Queue<GridHelper> queue = new Queue<GridHelper>();
        queue.Enqueue(path[path.Count - 1]);
        while (nearGrids.Count < groupCount && queue.Count > 0)
        {
            GridHelper current = queue.Dequeue();

            foreach (GridHelper grid in current.neighborGrids)
            {
                if (grid == null || nearGrids.Contains(grid))
                {
                    continue;
                }
                if (nearGrids.Count >= groupCount)
                {
                    break;
                }
                nearGrids.Add(grid);
                queue.Enqueue(grid);
            }
            yield return null;
        }
        //若得到的附近网格数量已经大于等于队友数量, 则为每个队友分配寻路任务
        if (nearGrids.Count >= groupCount)
        {
            callback?.Invoke(nearGrids);
        }
        yield return null;
    }

    /// <summary>
    /// 尝试向nearGrids中加入附近网格
    /// </summary>
    /// <param name="near"></param>
    /// <param name="target"></param>
    private void TryAddNearGrids(GridHelper near, GridHelper target)
    {
        if (!nearGrids.Contains(near) && near != target)
        {
            nearGrids.Add(near);
        }
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

    /// <summary>
    /// 计算网格start到end的启发式估算值h (最大轴向距离 + y轴高度影响)
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    private float HeuristicCostEstimate(GridHelper start, GridHelper end)
    {
        float xzDistance = GetDistance(start, end);
        float heightDiff = Mathf.Abs(start.info.centerPos.y - end.info.centerPos.y);
        return xzDistance + heightDiff * heightWeight;
    }
}
