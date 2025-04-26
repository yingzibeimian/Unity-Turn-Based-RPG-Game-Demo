using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

public class CameraMoveManager : MonoBehaviour
{
    public static CameraMoveManager Instance;

    public Character followCharacter;
    public Transform followEmptyObj;
    public float angle = 50.0f; //摄像机倾斜角度
    public float dist = 12.0f; //摄像机离目标距离(可变化, 受minDist和MaxDist限制)
    public float defaultDist = 12.0f; //摄像机离目标默认距离
    public float minDist = 3.0f;
    public float maxDist = 20.0f;
    public float mouseScrollSpeed = 5.0f; //鼠标滚轮控制摄像机拉近远离的速度

    public float moveSpeed = 20.0f; //摄像机移动速度(WASD)
    public float rotateSpeed = 80.0f; //摄像机旋转速度(鼠标拖动)

    public float autoMoveSpeed = 10.0f; //摄像机自动移动速度(changeToFollow时)
    public float autoRotateSpeed = 10.0f; //摄像机自动旋转速度(changeToFollow时)

    public Transform linesParent;
    public Material linesMaterial;

    private Vector3 cameraPos; //当前摄像机应该在的位置
    private bool follow = true; //摄像机是否跟随角色
    private bool changeTofollow = false; //是否将摄像机切换为跟随(角色)模式
    private bool isTacticalView = false; //是否处于战术视角
    private bool changeBetweenTacticalAndNormal = false; //是否将摄像机切换为/出战术视角
    private Vector3 cameraRight = Vector3.zero; //用于将战术视角切换为正常视角
    private Vector3 cameraUp = Vector3.zero; //用于将战术视角切换为正常视角

    private GridHelper lastGrid; //战术模式下渲染的战术网格
    private LineRenderer lineRenderer; //用于显示战术模式下显形的网格

    private Character newLeader;
    private bool changeToNewLeader = false;

    private float hardDistance = 1.0f; //镜头在向目标位置调整时, 小于硬距离时直接赋值

    // Start is called before the first frame update
    void Start()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        //镜头一开始默认跟随主控
        if(followCharacter == null)
        {
            followCharacter = PartyManager.Instance.GetNthCharacterOnUI(1);
        }
        if(followEmptyObj == null)
        {
            followEmptyObj = new GameObject("Main Camera FollowEmptyObj").transform;
            followEmptyObj.position = followCharacter.transform.position;
        }
        //初始化lineRenderer
        lineRenderer = LineRendererPool.Instance.GetLineRenderer();
        lineRenderer.transform.SetParent(linesParent);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!EventSystem.current.IsPointerOverGameObject()) //鼠标不在UI上时
        {
            dist -= Input.GetAxis("Mouse ScrollWheel") * mouseScrollSpeed; //通过鼠标滚轮滚动来改变摄像机远近
            dist = Mathf.Clamp(dist, minDist, maxDist); //限制镜头最远最近距离
        }
        
        if (follow && !changeTofollow && !changeBetweenTacticalAndNormal && !changeToNewLeader && followCharacter != null)
        {
            followEmptyObj.position = followCharacter.transform.position; //摄像机跟随模式下, 将非跟随模式下跟随的空物体位置更新到跟随角色身上
            UpdateCameraPosition(followCharacter.transform.position);
        }
        else if(!follow && !changeTofollow && !changeBetweenTacticalAndNormal && !changeToNewLeader)
        {
            UpdateCameraPosition(followEmptyObj.position);
        }

        //将摄像机设置为跟随模式
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cameraUp = transform.up;
            hardDistance = followCharacter.isMoving ? 1.5f : 0.1f;
            //followEmptyObj.position = followCharacter.transform.position;
            if (!changeBetweenTacticalAndNormal && !changeToNewLeader)
            {
                changeTofollow = true; //开始运镜
            }
        }
        if (changeTofollow)
        {
            SmoothFollowTransition();
        }

        //切换为战术模式/切出战术模式
        if (!changeBetweenTacticalAndNormal && Input.GetKeyDown(KeyCode.O))
        {
            cameraRight = transform.right;
            cameraUp = transform.up;
            hardDistance = followCharacter.isMoving ? 1.5f : 0.1f;
            if (!changeTofollow && !changeToNewLeader)
            {
                changeBetweenTacticalAndNormal = true; //开始运镜
            }
        }
        if(changeBetweenTacticalAndNormal)
        {
            if(isTacticalView)
            {
                SmoothEndTacticalTransition(follow ? followCharacter.transform.position : followEmptyObj.position);
            }
            else
            {
                SmoothStartTacticalTransition(follow ? followCharacter.transform.position : followEmptyObj.position);
            }
        }

        //处于战术视角时, 渲染当前摄像机正下方的网格
        if (isTacticalView && !changeBetweenTacticalAndNormal)
        {
            UpdateTacticalGrid();
        }

        //切换主控
        if (changeToNewLeader && !changeTofollow && !changeBetweenTacticalAndNormal)
        {
            SmoothChangeLeaderTransition(); //开始运镜
        }


        //将镜头向前/后/左/右推动时, 改变followEmptyObj的位置, 并将摄像机模式改为不跟随角色
        HandleManualMovement();

        //摄像机围绕跟随角色或者空对象旋转
        HandleRotation();
    }

    /// <summary>
    /// 不在changeToFollow或changeBetweenTacticalAndNormal时, 更新摄像头位置
    /// </summary>
    /// <param name="targetPosition"></param>
    private void UpdateCameraPosition(Vector3 targetPosition)
    {
        //非跟随模式下, 让摄像机高度随摄像机正下方网格高度变化
        float unfollowY = transform.position.y;
        if (!follow)
        {
            RaycastHit hit;
            if (Physics.Raycast(followEmptyObj.position + Vector3.up * 50, Vector3.down, out hit, 150, 1 << LayerMask.NameToLayer("Grid"))) //如果检测到collider
            {
                GridHelper grid = hit.collider.GetComponent<GridHelper>();
                if (grid != null) //如果检测到网格
                {
                    unfollowY = grid.info.centerPos.y + Mathf.Sin(Mathf.Deg2Rad * angle) * dist;
                }
                else //如果检测到的collider不是网格
                {

                }
            }
            else //如果连collider都没有检测到
            {

            }
        }


        if (!isTacticalView) //非战术视角
        {
            Vector3 selfBack = -transform.forward;
            selfBack.y = 0;
            cameraPos = targetPosition + (Quaternion.AngleAxis(angle, transform.right.normalized) * selfBack.normalized) * dist;
            cameraPos.y = follow ? cameraPos.y : unfollowY;
            transform.position = Vector3.Lerp(transform.position, cameraPos, Time.deltaTime * autoMoveSpeed);
        }
        else //战术视角
        {
            cameraPos = targetPosition + Vector3.up * dist;
            cameraPos.y = follow ? cameraPos.y : unfollowY;
            transform.position = Vector3.Lerp(transform.position, cameraPos, Time.deltaTime * autoMoveSpeed);
        }
    }

    /// <summary>
    /// 进入跟随模式
    /// </summary>
    private void SmoothFollowTransition()
    {
        //缓动
        if(isTacticalView) //战术视角
        {
            cameraPos = followCharacter.transform.position + Vector3.up * defaultDist;
        }
        else //非战术视角
        {
            cameraPos = followCharacter.transform.position + (Quaternion.AngleAxis(angle, Vector3.right) * Vector3.back) * defaultDist;
        }
        if (Vector3.Distance(transform.position, cameraPos) > hardDistance)
        {
            //followEmptyObj.position = Vector3.Lerp(startPos, endPos, moveWeight);
            //followEmptyObj.position = Vector3.Lerp(followEmptyObj.position, followCharacter.transform.position, Time.deltaTime * moveSpeed * 0.5f);
            transform.position = Vector3.Lerp(transform.position, cameraPos, Time.deltaTime * autoMoveSpeed);
        }
        else
        {
            transform.position = cameraPos;
        }

        Quaternion targetRotation = Quaternion.identity;
        if(isTacticalView)
        {
            targetRotation = Quaternion.LookRotation(Vector3.down, cameraUp);
        }
        else
        {
            Vector3 directionToTarget = Quaternion.AngleAxis(angle, Vector3.right) * Vector3.forward;
            targetRotation = Quaternion.LookRotation(directionToTarget);
        }
        //if (Vector3.Distance(transform.position, cameraPos) < Mathf.Cos(Mathf.Deg2Rad * angle) * defaultDist)
        //{
        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            //this.transform.rotation = Quaternion.Slerp(startRotation, endRotation, rotateWeight);
            this.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * autoRotateSpeed);
        }
        else
        {
            this.transform.rotation = targetRotation;
        }
        //}

        // 判断是否完成过渡
        if (Vector3.Distance(transform.position, cameraPos) < hardDistance &&
            Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            //Debug.Log("Change to Follow Success");

            //参数重置
            dist = defaultDist;

            changeTofollow = false; // 运镜结束
            follow = true; // 切换为跟随模式
        }
    }

    /// <summary>
    /// 切入战术模式
    /// </summary>
    /// <param name="targetPos"></param>
    private void SmoothStartTacticalTransition(Vector3 targetPos)
    {
        //缓动
        cameraPos = targetPos + Vector3.up * dist;
        if (Vector3.Distance(transform.position, cameraPos) > hardDistance)
        {
            transform.position = Vector3.Lerp(transform.position, cameraPos, Time.deltaTime * autoMoveSpeed);
        }
        else
        {
            transform.position = cameraPos;
        }

        cameraUp.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.down, cameraUp);
        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            this.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * autoRotateSpeed);
        }
        else
        {
            this.transform.rotation = targetRotation;
        }

        // 判断是否完成过渡
        if (Vector3.Distance(transform.position, cameraPos) < hardDistance &&
            Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            //Debug.Log("Change to Tactical Success");

            //参数重置
            //dist = defaultDist;
            cameraRight = Vector3.zero;
            cameraUp = Vector3.zero;

            changeBetweenTacticalAndNormal = false; // 运镜结束
            isTacticalView = !isTacticalView;
        }
    }

    /// <summary>
    /// 切出战术模式
    /// </summary>
    /// <param name="targetPos"></param>
    private void SmoothEndTacticalTransition(Vector3 targetPos)
    {
        cameraPos = targetPos + (Quaternion.AngleAxis(angle, cameraRight.normalized) * -cameraUp.normalized) * dist;
        if (Vector3.Distance(transform.position, cameraPos) > hardDistance)
        {
            transform.position = Vector3.Lerp(transform.position, cameraPos, Time.deltaTime * autoMoveSpeed);
        }
        else
        {
            transform.position = cameraPos;
        }

        
        Vector3 directionToTarget = Quaternion.AngleAxis(angle, cameraRight) * cameraUp;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            this.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * autoRotateSpeed);
        }
        else
        {
            this.transform.rotation = targetRotation;
        }
        

        // 判断是否完成过渡
        if (Vector3.Distance(transform.position, cameraPos) < hardDistance &&
            Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            //Debug.Log("Change to Tactical Success");

            //参数重置
            //dist = defaultDist;
            cameraRight = Vector3.zero;
            cameraUp = Vector3.zero;

            changeBetweenTacticalAndNormal = false; // 运镜结束
            isTacticalView = !isTacticalView;

            //不处于战术视角时, 停止渲染战术视角下的网格
            StopDrawLastTacticalGrid();
        }
    }

    /// <summary>
    /// WASD 控制镜头推动
    /// </summary>
    private void HandleManualMovement()
    {
        if (Input.GetKey(KeyCode.W))
        {
            Vector3 selfForward = Vector3.zero;
            if (isTacticalView)
            {
                selfForward = this.transform.up;
            }
            else
            {
                selfForward = this.transform.forward;
            }
            selfForward.y = 0;
            followEmptyObj.Translate(selfForward.normalized * moveSpeed * Time.deltaTime, Space.World);
            follow = false;
        }
        if (Input.GetKey(KeyCode.S))
        {
            Vector3 selfBack = Vector3.zero;
            if (isTacticalView)
            {
                selfBack = -this.transform.up;
            }
            else
            {
                selfBack = -this.transform.forward;
            }
            selfBack.y = 0;
            followEmptyObj.Translate(selfBack.normalized * moveSpeed * Time.deltaTime, Space.World);
            follow = false;
        }
        if (Input.GetKey(KeyCode.A))
        {
            Vector3 selfLeft = -this.transform.right;
            selfLeft.y = 0;
            followEmptyObj.Translate(selfLeft.normalized * moveSpeed * Time.deltaTime, Space.World);
            follow = false;
        }
        if (Input.GetKey(KeyCode.D))
        {
            Vector3 selfRight = this.transform.right;
            selfRight.y = 0;
            followEmptyObj.Translate(selfRight.normalized * moveSpeed * Time.deltaTime, Space.World);
            follow = false;
        }
    }

    /// <summary>
    /// alt+右键+鼠标X方向拖动 控制摄像机旋转
    /// </summary>
    private void HandleRotation()
    {
        if (!changeTofollow && !changeBetweenTacticalAndNormal && Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
        {
            transform.RotateAround(follow ? followCharacter.transform.position : followEmptyObj.position, Vector3.up,
                Time.deltaTime * rotateSpeed * Input.GetAxis("Mouse X"));
        }
    }

    /// <summary>
    /// 更新战术网格渲染信息
    /// </summary>
    private void UpdateTacticalGrid()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100, 1 << LayerMask.NameToLayer("Grid"))) //如果检测到collider
        {
            GridHelper grid = hit.collider.GetComponent<GridHelper>();
            if (grid != null) //如果检测到网格
            {

                if (lastGrid == null || lastGrid != grid) //如果是第一次检测到网格 或者 这次检测到的网格和之前不是一个
                {
                    DrawTacticalGrid(grid);
                }
            }
            else //如果检测到的collider不是网格
            {
                StopDrawLastTacticalGrid(); //停止渲染上一个战术网格
            }
        }
        else //如果连collider都没有检测到
        {
            StopDrawLastTacticalGrid();
        }
    }

    /// <summary>
    /// 渲染战术网格
    /// </summary>
    /// <param name="grid"></param>
    private void DrawTacticalGrid(GridHelper grid)
    {
        LineRendererPool.Instance.ReturnLineRenderer(lineRenderer); //先将上一个网格的lineRenderer还给对象池

        lineRenderer = LineRendererPool.Instance.GetLineRenderer(); //为当前网格取出一个lineRenderer
        lineRenderer.material = linesMaterial;
        lineRenderer.transform.SetParent(linesParent);
        lineRenderer.positionCount = 6;
        lineRenderer.SetPositions(new Vector3[] { grid.info.vertices[0],
                            grid.info.vertices[1], grid.info.vertices[2],
                            grid.info.vertices[3], grid.info.vertices[4],
                            grid.info.vertices[5]});

        lastGrid = grid;
        //Debug.Log("Draw");
    }

    /// <summary>
    /// 停止渲染战术网格
    /// </summary>
    private void StopDrawLastTacticalGrid()
    {
        if (lastGrid != null) //如果上一个网格不为空, 就停止渲染上一个网格, 并将上一个网格置空
        {
            LineRendererPool.Instance.ReturnLineRenderer(lineRenderer);
            lastGrid = null;
        }
    }

    /// <summary>
    /// 开始切换主控运镜, 设置新的跟随主控
    /// </summary>
    /// <param name="newLeader"></param>
    public void ChangeToNewLeader(Character newLeader)
    {
        if(newLeader != null)
        {
            changeToNewLeader = true;
            this.newLeader = newLeader;
            hardDistance = followCharacter.isMoving ? 1.5f : 0.1f;
        }
    }

    /// <summary>
    /// 将镜头平滑地移动到新的主控对象身上
    /// </summary>
    private void SmoothChangeLeaderTransition()
    {
        //如果摄像机正在运镜, 则直接返回(防止镜头运动冲突)
        if(changeTofollow || changeBetweenTacticalAndNormal)
        {
            return;
        }

        if (isTacticalView) //战术视角
        {
            cameraPos = newLeader.transform.position + Vector3.up * dist;
        }
        else //非战术视角
        {
            Vector3 selfBack = -transform.forward;
            selfBack.y = 0;
            cameraPos = newLeader.transform.position + (Quaternion.AngleAxis(angle, transform.right.normalized) * selfBack.normalized) * dist;
        }
        if (Vector3.Distance(transform.position, cameraPos) > hardDistance)
        {
            transform.position = Vector3.Lerp(transform.position, cameraPos, Time.deltaTime * autoMoveSpeed);
        }
        else
        {
            transform.position = cameraPos;
        }

        // 判断是否完成过渡
        if (Vector3.Distance(transform.position, cameraPos) < hardDistance)
        {
            //Debug.Log("Change Leader Success");

            //参数重置
            followCharacter = newLeader;

            changeToNewLeader = false; // 运镜结束
            follow = true;
        }
    }
}
