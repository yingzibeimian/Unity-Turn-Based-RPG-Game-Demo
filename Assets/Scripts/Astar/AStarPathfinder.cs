using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// A*Ѱ·�㷨, �뽫�ű����ص���ɫ��, ����Ѱ·
/// </summary>
public class AStarPathfinder : MonoBehaviour
{
    public float heightWeight = 0.5f; //Ѱ·ʱ�ĸ߶�Ȩ��
    public Character character; //Ѱ·�ű������ƵĽ�ɫ��Character�ű�
    public Animator animator; //Ѱ·�ű������ƵĽ�ɫ��Animator�ű�
    public int maxNodesProcessedPerFrame = 500; //ÿ֡��ദ���������
    //public bool isMoving = false; //���ڵ�����ͷ�ƶ���ϸ��

    private List<GridHelper> path = new List<GridHelper>(); //���ڼ�¼��ɫ���ƶ�·��
    private List<GridHelper> nearGrids = new List<GridHelper>(); //���ڵõ�·���յ�ĸ�������

    public Transform linesParent; //·�������ߵĸ�����
    private Material outlineMaterial; //�����������ı������
    private Material reachableMaterial; //���Ե�����������Ⱦ����
    private Material unreachableMaterial; //�����Ե�����������Ⱦ����
    private List<LineRenderer> pathGridsLineRenderer = new List<LineRenderer>(); //��¼������Ⱦpath·�������LineRenderer

    //A*�ڵ�
    private class AstarNode : IComparable<AstarNode>
    {
        public GridHelper grid;
        public AstarNode comeFromNode;
        public float g; //��������� ���������·������
        public float h; //�Ӹ����� ���յ������Ԥ������(����ʽ����)
        public float f => g + h; //A*����

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
            character = this.GetComponent<Character>(); //��Character�ű�
        }
        if (animator == null)
        {
            animator = this.GetComponent<Animator>(); //��Animator�ű�
        }
        //��ʼ���������
        outlineMaterial = Resources.Load<Material>("Materials/outlineRendererMaterial");
        reachableMaterial = Resources.Load<Material>("Materials/reachableMaterial");
        unreachableMaterial = Resources.Load<Material>("Materials/unreachableMaterial");
    }

    /// <summary>
    /// Ѱ�Ҵ�����start��end��Э�̷���
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="moveSpeed"></param>
    /// <returns></returns>
    public IEnumerator FindPath(GridHelper start, GridHelper end)
    {
        //����С�Ѵ�Ŵ����������ڵ�, ÿ������ȡ��fֵ��С��, ������C++�е�priority_queue
        MinHeap<AstarNode> openSet = new MinHeap<AstarNode>();
        HashSet<GridHelper> closedSet = new HashSet<GridHelper>(); //����Ѵ���Ľڵ�

        Dictionary<GridHelper, AstarNode> openSetDic = new Dictionary<GridHelper, AstarNode>(); //�������ٲ��ҽڵ��Ƿ���openSet��

        //��ʼ�����
        AstarNode startNode = new AstarNode(start, null, 0, HeuristicCostEstimate(start, end));
        openSet.Enqueue(startNode);
        openSetDic.Add(start, startNode);

        int nodesProcessedThisFrame = 0; //ÿ֡����Ľڵ�����
        while (openSet.Count > 0)
        {
            //�Ӷ���ȡ��f��С�Ľڵ�
            AstarNode current = openSet.Dequeue();

            //����ҵ�Ŀ������ڵ�, ���ٿ���һ����Э��, �ؽ�·��, ������·������ɫ�ƶ���Ŀ������
            if(current.grid == end)
            {
                //����׷��·��, �ؽ�·��
                path.Clear();
                while (current.grid != start)
                {
                    path.Add(current.grid);
                    current = current.comeFromNode;
                }
                path.Reverse();
                yield break;
            }

            //����ǰ�ڵ�, ���Ϊ�Ѵ���
            openSetDic.Remove(current.grid);
            closedSet.Add(current.grid);

            //������ǰ�ڵ�������ھӽڵ�
            foreach(GridHelper neighbor in current.grid.neighborGrids)
            {
                //����ھӽڵ㲻�����߻��Ѵ���, ����
                if (neighbor == null || !neighbor.info.walkable || closedSet.Contains(neighbor))
                {
                    continue;
                }
                
                //�ھӽڵ��gֵ(�����ܸ߶�Ӱ��)
                float g = current.g + neighbor.info.movePrice + 
                    Mathf.Abs(current.grid.info.centerPos.y - neighbor.info.centerPos.y) * heightWeight;
                //�ھӽڵ��hֵ
                float h = HeuristicCostEstimate(neighbor, end);

                //���ڵ��ھӽڵ�һ�������Ѵ����б���, ��˸��ھӽڵ�Ҫô��δ����openSet, Ҫô�Ѿ����뵫��δ������
                //����ھӽڵ�δ�ڴ������б���
                if(!openSetDic.ContainsKey(neighbor))
                {
                    //��¼�����neighbor��·��, ����A*�ڵ����������б�
                    AstarNode neighborNode = new AstarNode(neighbor, current, g, h);
                    openSet.Enqueue(neighborNode);
                    openSetDic.Add(neighbor, neighborNode);
                }
                //���� �ھӽڵ��ڴ������б���, ����δ������, ��ͨ��current����neighbor��·����֮ǰ��¼�ĸ���
                else if (openSetDic.ContainsKey(neighbor) && openSetDic[neighbor].f > g + h)
                {
                    //��¼���̵���·��
                    openSetDic[neighbor].comeFromNode = current;
                    openSetDic[neighbor].g = g;
                    openSetDic[neighbor].h = h;
                }
                nodesProcessedThisFrame++;
                if(nodesProcessedThisFrame >= maxNodesProcessedPerFrame)
                {
                    yield return null; //ÿ֡����һ����, �������, ��һ֡��������
                    nodesProcessedThisFrame = 0;
                }
            }
        }
        yield break;
        //�������ִ�е�����, ˵��û���ҵ�·��
    }

    /// <summary>
    /// ����ʱ��Ѱ·����, ����Ŀ����뷶Χ���ɫ��ֹͣѰ· 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="moveSpeed"></param>
    /// <param name="rotateSpeed"></param>
    /// <param name="atkDistance">��ɫ��������</param>
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

            //�����ǰ����Ľڵ��Ŀ������ڵ�֮��ľ��� С�� ��������, �ͱ����ҵ�·��
            if (GetDistance(current.grid, end) <= attackDistance) 
            {
                //����׷��·��, �ؽ�·��
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
                yield return null; //ÿ֡����һ����, �������, ��һ֡��������
                nodesProcessedThisFrame = 0;
            }
        }
        yield break;
    }

    /// <summary>
    /// ����path·������������
    /// </summary>
    /// <returns></returns>
    public IEnumerator DrawPathGrids(Action<float> calculateCost)
    {
        if(outlineMaterial == null || reachableMaterial == null || unreachableMaterial == null) //ȷ��LineRenderer����Ĳ����Ƿ����
        {
            yield break;
        }
        //�����֮ǰ�������Ƶ�pathGridsLineRenderer, ������LineRenderer���������
        foreach (LineRenderer lr in pathGridsLineRenderer)
        {
            lr.material = outlineMaterial;
            LineRendererPool.Instance.ReturnLineRenderer(lr);
        }
        pathGridsLineRenderer.Clear();

        float apTakes = 0f; //�����յ�path��������ж�����
        float apTakePerGrid = 1.0f / character.info.runSpeed; //ÿ�������ĵ��ж�����
        foreach(GridHelper grid in path)
        {
            GridInfo info = grid.info;
            LineRenderer lr = LineRendererPool.Instance.GetLineRenderer();
            //�����Ƿ�ɵ���ѡ����Ⱦ����
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
        if (outlineMaterial == null) //ȷ��LineRenderer����Ĳ����Ƿ����
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
    /// ����·��path����Ѱ·�ű������ص��Ľ�ɫ�����ƶ���Ŀ������
    /// </summary>
    /// <param name="moveSpeed"></param>
    /// <param name="rotateSpeed"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    public IEnumerator CharacterMove(float moveSpeed, float rotateSpeed, float weight)
    {
        animator.SetBool("isMoving", true); //����WalkToRun����
        animator.SetFloat("speed", weight); //�����ƶ��������Ȩ��
        character.isMoving = true;

        //�������ƶ���ɫ
        if (moveSpeed > rotateSpeed) //ȷ����ת�ٶȴ��ڵ����ƶ��ٶ�, �Ӷ�ȷ�������յ�֮ǰһ�������ת��
        {
            rotateSpeed = moveSpeed;
        }
        foreach(GridHelper grid in path.ToList())
        {
            Vector3 startPos = this.transform.position; //��ǰλ��
            Vector3 endPos = grid.info.centerPos; //�յ�λ��
            Vector3 direction = (endPos - startPos).normalized;
            direction.y = 0; //���ı��ɫy������
            Quaternion startRotation = this.transform.rotation; //��ǰ����
            Quaternion endRotation = Quaternion.LookRotation(direction); //�յ㳯��

            float moveWeight = 0;
            float rotateWeight = 0;
            //ת�� + �ƶ�
            while (Vector3.Distance(this.transform.position, grid.info.centerPos) > 0.05f)
            {
                moveWeight += Time.deltaTime * moveSpeed;
                rotateWeight += Time.deltaTime * rotateSpeed;
                //��̬����ת���ٶ�, �ӽ��յ�ʱת�����
                float dynamicRotateSpeed = Mathf.Max(rotateSpeed * (1 - moveWeight), 0);
                float dynamicRotateWeight = Time.deltaTime * dynamicRotateSpeed;
                this.transform.position = Vector3.Lerp(startPos, endPos, moveWeight);
                this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight + dynamicRotateWeight);
                yield return null;
            }
            transform.position = endPos;
            transform.rotation = endRotation;
            //����character�е�λ����Ϣ
            character.info.q = grid.info.q;
            character.info.r = grid.info.r;
            character.info.heightOrder = grid.info.heightOrder;
        }

        character.isMoving = false;
        animator.SetBool("isMoving", false); //��������WalkToRun����, �ص�Idle
    }

    /// <summary>
    /// �ڻغ�����, ����·��path����Ѱ·�ű������ص��Ľ�ɫ�����ƶ�����Զ�Ŀɵ�������, ���������߹����в��ϸ��¼��ٽ�ɫ�ж�����, �Լ�������, ������������ͷ���
    /// </summary>
    /// <param name="moveSpeed"></param>
    /// <param name="rotateSpeed"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    public IEnumerator CharacterMoveInTurn(float moveSpeed, float rotateSpeed, float weight)
    {
        if(character.info.actionPoint <= 0 || path.Count == 0) //�жϳ�ʼ�ж������Ƿ������� ���� �Ƿ����·��
        {
            yield break;
        }
        Debug.Log("Move Start");


        character.isMoving = true;
        //���½�ɫ����λ�õ���������
        character.nowGrid.info.dynamicLimit = false;
        character.nowGrid.info.walkable = true;
        //�жϽ�ɫ������Ӫ, ���½�ɫ������Ӫ�ĸ���
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
        //���Move�����з����������Ľ�ɫ
        HashSet<Character> opportunityAttackerList = new HashSet<Character>();

        animator.SetBool("isMoving", true); //����WalkToRun����
        animator.SetFloat("speed", weight); //�����ƶ��������Ȩ��

        //�������ƶ���ɫ
        if (moveSpeed > rotateSpeed) //ȷ����ת�ٶȴ��ڵ����ƶ��ٶ�, �Ӷ�ȷ�������յ�֮ǰһ�������ת��
        {
            rotateSpeed = moveSpeed;
        }
        float apTakePerGrid = 1.0f / character.info.runSpeed; //ÿ�������ĵ��ж�����
        foreach (GridHelper grid in path.ToList())
        {
            if (character.info.actionPoint <= 0 || character.info.hp <= 0)
            {
                yield break;
            }

            Vector3 startPos = this.transform.position; //��ǰλ��
            Vector3 endPos = grid.info.centerPos; //�յ�λ��
            Vector3 direction = (endPos - startPos).normalized;
            direction.y = 0; //���ı��ɫy������
            Quaternion startRotation = this.transform.rotation; //��ǰ����
            Quaternion endRotation = Quaternion.LookRotation(direction); //�յ㳯��

            float moveWeight = 0;
            float rotateWeight = 0;
            //ת�� + �ƶ�
            while (Vector3.Distance(this.transform.position, grid.info.centerPos) > 0.05f)
            {
                moveWeight += Time.deltaTime * moveSpeed;
                rotateWeight += Time.deltaTime * rotateSpeed;
                //��̬����ת���ٶ�, �ӽ��յ�ʱת�����
                float dynamicRotateSpeed = Mathf.Max(rotateSpeed * (1 - moveWeight), 0);
                float dynamicRotateWeight = Time.deltaTime * dynamicRotateSpeed;
                this.transform.position = Vector3.Lerp(startPos, endPos, moveWeight);
                this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight + dynamicRotateWeight);
                yield return null;
            }
            transform.position = endPos;
            transform.rotation = endRotation;

            //�жϽ������
            bool needOpportunityAndCounterAttack = false;
            if (isPlayer) //��������ƶ��Ľ�ɫ�����
            {
                foreach(GridHelper neighbor in character.nowGrid.neighborGrids.ToList())
                {
                    //����ھӸ����ϴ��ڲ�ͬ��Ӫ�Ľ�ɫ
                    if (neighbor != null && TurnManager.Instance.enemyGrids.ContainsKey(neighbor))
                    {
                        Character opportunityAttacker = TurnManager.Instance.enemyGrids[neighbor];
                        //����û�з�����������
                        if (!opportunityAttackerList.Contains(opportunityAttacker))
                        {
                            needOpportunityAndCounterAttack = true; //��Ҫ���н�������ͷ���
                            opportunityAttackerList.Add(opportunityAttacker); //�����Ѿ�����������Ľ�ɫ�б�
                            //����������
                            Debug.Log("OpportunityAttack");
                            //CoroutineManager.Instance.PauseGroup(character.info.name); //��ͣ��ǰ��ɫ��Э�̶���
                            animator.SetBool("isMoving", false); //��ͣ�ƶ�����
                            CoroutineManager.Instance.AddTaskToGroup(opportunityAttacker.OpportunityAttack(character), "OpportunityAndCounterAttack"); //�������
                            CoroutineManager.Instance.AddTaskToGroup(character.CounterAttack(opportunityAttacker), "OpportunityAndCounterAttack"); //����
                        }
                    }
                }
            }
            else //��������ƶ��Ľ�ɫ�ǵ���
            {
                foreach (GridHelper neighbor in character.nowGrid.neighborGrids.ToList())
                {
                    //����ھӸ����ϴ��ڲ�ͬ��Ӫ�Ľ�ɫ
                    if (neighbor != null && TurnManager.Instance.playerGrids.ContainsKey(neighbor))
                    {
                        Character opportunityAttacker = TurnManager.Instance.playerGrids[neighbor];
                        //����û�з�����������
                        if (!opportunityAttackerList.Contains(opportunityAttacker))
                        {
                            needOpportunityAndCounterAttack = true; //��Ҫ���н�������ͷ���
                            opportunityAttackerList.Add(opportunityAttacker); //�����Ѿ�����������Ľ�ɫ�б�
                            //����������
                            Debug.Log("OpportunityAttack");
                            //CoroutineManager.Instance.PauseGroup(character.info.name); //��ͣ��ǰ��ɫ��Э�̶���
                            animator.SetBool("isMoving", false); //��ͣ�ƶ�����
                            CoroutineManager.Instance.AddTaskToGroup(opportunityAttacker.OpportunityAttack(character), "OpportunityAndCounterAttack"); //�������
                            CoroutineManager.Instance.AddTaskToGroup(character.CounterAttack(opportunityAttacker), "OpportunityAndCounterAttack"); //����
                        }
                    }
                }
            }
            yield return null;

            if (needOpportunityAndCounterAttack) //�����Ҫ��������ͷ���
            {
                CoroutineManager.Instance.PauseGroup(character.info.name); //��ͣ��ǰ��ɫ��Э�̶���
                //�󶨻ص�
                CoroutineManager.Instance.BindCallback("OpportunityAndCounterAttack", () =>
                {
                    CoroutineManager.Instance.ResumeGroup(character.info.name); //�ָ���ɫ����ͣ��Э�̶���
                    if (character.info.actionPoint > 0 && character.info.hp > 0)
                    {
                        animator.SetBool("isMoving", true); //�ָ���ɫ�ƶ�����
                    }
                    Debug.Log("Resume");
                });
                CoroutineManager.Instance.StartGroup("OpportunityAndCounterAttack");
                //yield return new WaitWhile(() => !CoroutineManager.Instance.TaskInGroupIsEmpty("OpportunityAndCounterAttack"));
            }

            //����character�е�λ����Ϣ
            character.info.q = grid.info.q;
            character.info.r = grid.info.r;
            character.info.heightOrder = grid.info.heightOrder;

            //���Ľ�ɫ�ж�����
            character.info.actionPoint -= apTakePerGrid;
            UITurnManager.Instance.UpdateActionPointBalls(character.info.actionPoint, 0);
            if (character.info.actionPoint <= 0)
            {
                break;
            }
            yield return null;
        }

        character.isMoving = false;
        animator.SetBool("isMoving", false); //��������WalkToRun����, �ص�Idle

        //���½�ɫ����λ�õ���������
        character.nowGrid.info.dynamicLimit = true;
        character.nowGrid.info.walkable = false;
        //���½�ɫ������Ӫ�ĸ���
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
    /// ����Character�ű��е�nowGrid������ɫλ��
    /// </summary>
    /// <returns></returns>
    public IEnumerator CorrectCharacterPositionInTurn(List<Character> triggers)
    {
        if (triggers.Count == 0)
        {
            yield break;
        }

        character.isMoving = true;
        animator.SetBool("isMoving", true); //����WalkToRun����
        animator.SetFloat("speed", 0);

        Vector3 startPos = this.transform.position; //���λ��
        Vector3 endPos = character.nowGrid.info.centerPos; //�յ�λ��
        Vector3 direction = (endPos - this.transform.position).normalized;
        direction.y = 0; //���ı��ɫy������
        Quaternion startRotation = this.transform.rotation; //��ǰ����
        Quaternion endRotation = Quaternion.LookRotation(direction); //�յ㳯��

        float moveSpeed = character.info.walkSpeed;
        float rotateSpeed = character.info.rotateSpeed;
        float moveWeight = 0;
        float rotateWeight = 0;
        //ת�� + �ƶ�
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

        //����غ����������������, ����Ҫ�ٸ��Ľ�ɫ����
        if(triggers.Count <= 1)
        {
            yield break;
        }

        //���Ľ�ɫ����, ��ҽ�ɫ�������, �����������
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
    /// ���·��path�յ㸽��������, �������Ѹ���
    /// </summary>
    /// <param name="groupCount"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public IEnumerator GetNearGrids(int groupCount, Action<List<GridHelper>> callback)
    {
        //path.Count <= 1: ������ؽ�ɫû���ƶ�, �����Ҳ���ƶ�;
        //groupCount == 0: ��Ϊ����û�ж���, Ҳ��ִ��
        if (path.Count <= 1 || groupCount == 0)
        {
            yield break;
        }

        nearGrids.Clear();
        GridHelper targetGrid = path[path.Count - 1];
        //��������ƶ���3��, �����Ƚ������ƶ���·���ϵĵ�����4��, �Լ��뵹����4���γ�120�ǵĵ�����3��������ھ�����
        if (nearGrids.Count < groupCount && path.Count >= 4)
        {
            GridHelper lastBut4 = path[path.Count - 4]; //������4��
            GridHelper lastBut3 = path[path.Count - 3]; //������3��
            int dir = 0; //ȷ��������4���ڵ�����3����ĸ�����
            for (; dir < 6; dir++)
            {
                if (lastBut3.neighborGrids[dir] == lastBut4)
                {
                    break;
                }
                yield return null;
            }
            //����ø�������û�б���¼��, ���Ҳ������ؽ�ɫ������յ�����, �ͼ���nearGrids��
            TryAddNearGrids(lastBut3.neighborGrids[(dir + 2) % 6], targetGrid);
            TryAddNearGrids(lastBut3.neighborGrids[(dir + 4) % 6], targetGrid);
            TryAddNearGrids(lastBut4, targetGrid);
        }
        //��������ƶ���2��, ������Ƚ������ƶ���·���ϵĵ�����3��, �Լ��뵹����3���γ�120�ǵĵ�����2��������ھ�����
        if (nearGrids.Count < groupCount && path.Count >= 3)
        {
            GridHelper lastBut3 = path[path.Count - 3]; //������3��
            GridHelper lastBut2 = path[path.Count - 2]; //������2��
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
        //��������ƶ���1��, ��δ����Ƚ������ƶ���������2��, �Լ��뵹����2�񡢵�1�񶼽��ڵ�����
        if (nearGrids.Count < groupCount && path.Count >= 2)
        {
            GridHelper lastBut2 = path[path.Count - 2]; //������2��
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
        //������Ϲ���õ��ĸ������񲻹�, ���� ���и������/npc/�ٻ�����Ҫ�����ƶ�, ��ӵ�����5��ʼ�������path, ���� ��path�������������
        int index = 5; //�������������
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
        //���õ��ĸ������������Ѿ����ڵ��ڶ�������, ��Ϊÿ�����ѷ���Ѱ·����
        if (nearGrids.Count >= groupCount)
        {
            callback?.Invoke(nearGrids);
        }
        yield return null;
    }

    /// <summary>
    /// ������nearGrids�м��븽������
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

    /// <summary>
    /// ��������start��end������ʽ����ֵh (���������� + y��߶�Ӱ��)
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
