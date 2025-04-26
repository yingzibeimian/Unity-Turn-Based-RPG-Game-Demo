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
    public float angle = 50.0f; //�������б�Ƕ�
    public float dist = 12.0f; //�������Ŀ�����(�ɱ仯, ��minDist��MaxDist����)
    public float defaultDist = 12.0f; //�������Ŀ��Ĭ�Ͼ���
    public float minDist = 3.0f;
    public float maxDist = 20.0f;
    public float mouseScrollSpeed = 5.0f; //�����ֿ������������Զ����ٶ�

    public float moveSpeed = 20.0f; //������ƶ��ٶ�(WASD)
    public float rotateSpeed = 80.0f; //�������ת�ٶ�(����϶�)

    public float autoMoveSpeed = 10.0f; //������Զ��ƶ��ٶ�(changeToFollowʱ)
    public float autoRotateSpeed = 10.0f; //������Զ���ת�ٶ�(changeToFollowʱ)

    public Transform linesParent;
    public Material linesMaterial;

    private Vector3 cameraPos; //��ǰ�����Ӧ���ڵ�λ��
    private bool follow = true; //������Ƿ�����ɫ
    private bool changeTofollow = false; //�Ƿ�������л�Ϊ����(��ɫ)ģʽ
    private bool isTacticalView = false; //�Ƿ���ս���ӽ�
    private bool changeBetweenTacticalAndNormal = false; //�Ƿ�������л�Ϊ/��ս���ӽ�
    private Vector3 cameraRight = Vector3.zero; //���ڽ�ս���ӽ��л�Ϊ�����ӽ�
    private Vector3 cameraUp = Vector3.zero; //���ڽ�ս���ӽ��л�Ϊ�����ӽ�

    private GridHelper lastGrid; //ս��ģʽ����Ⱦ��ս������
    private LineRenderer lineRenderer; //������ʾս��ģʽ�����ε�����

    private Character newLeader;
    private bool changeToNewLeader = false;

    private float hardDistance = 1.0f; //��ͷ����Ŀ��λ�õ���ʱ, С��Ӳ����ʱֱ�Ӹ�ֵ

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
        //��ͷһ��ʼĬ�ϸ�������
        if(followCharacter == null)
        {
            followCharacter = PartyManager.Instance.GetNthCharacterOnUI(1);
        }
        if(followEmptyObj == null)
        {
            followEmptyObj = new GameObject("Main Camera FollowEmptyObj").transform;
            followEmptyObj.position = followCharacter.transform.position;
        }
        //��ʼ��lineRenderer
        lineRenderer = LineRendererPool.Instance.GetLineRenderer();
        lineRenderer.transform.SetParent(linesParent);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!EventSystem.current.IsPointerOverGameObject()) //��겻��UI��ʱ
        {
            dist -= Input.GetAxis("Mouse ScrollWheel") * mouseScrollSpeed; //ͨ�������ֹ������ı������Զ��
            dist = Mathf.Clamp(dist, minDist, maxDist); //���ƾ�ͷ��Զ�������
        }
        
        if (follow && !changeTofollow && !changeBetweenTacticalAndNormal && !changeToNewLeader && followCharacter != null)
        {
            followEmptyObj.position = followCharacter.transform.position; //���������ģʽ��, ���Ǹ���ģʽ�¸���Ŀ�����λ�ø��µ������ɫ����
            UpdateCameraPosition(followCharacter.transform.position);
        }
        else if(!follow && !changeTofollow && !changeBetweenTacticalAndNormal && !changeToNewLeader)
        {
            UpdateCameraPosition(followEmptyObj.position);
        }

        //�����������Ϊ����ģʽ
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cameraUp = transform.up;
            hardDistance = followCharacter.isMoving ? 1.5f : 0.1f;
            //followEmptyObj.position = followCharacter.transform.position;
            if (!changeBetweenTacticalAndNormal && !changeToNewLeader)
            {
                changeTofollow = true; //��ʼ�˾�
            }
        }
        if (changeTofollow)
        {
            SmoothFollowTransition();
        }

        //�л�Ϊս��ģʽ/�г�ս��ģʽ
        if (!changeBetweenTacticalAndNormal && Input.GetKeyDown(KeyCode.O))
        {
            cameraRight = transform.right;
            cameraUp = transform.up;
            hardDistance = followCharacter.isMoving ? 1.5f : 0.1f;
            if (!changeTofollow && !changeToNewLeader)
            {
                changeBetweenTacticalAndNormal = true; //��ʼ�˾�
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

        //����ս���ӽ�ʱ, ��Ⱦ��ǰ��������·�������
        if (isTacticalView && !changeBetweenTacticalAndNormal)
        {
            UpdateTacticalGrid();
        }

        //�л�����
        if (changeToNewLeader && !changeTofollow && !changeBetweenTacticalAndNormal)
        {
            SmoothChangeLeaderTransition(); //��ʼ�˾�
        }


        //����ͷ��ǰ/��/��/���ƶ�ʱ, �ı�followEmptyObj��λ��, ���������ģʽ��Ϊ�������ɫ
        HandleManualMovement();

        //�����Χ�Ƹ����ɫ���߿ն�����ת
        HandleRotation();
    }

    /// <summary>
    /// ����changeToFollow��changeBetweenTacticalAndNormalʱ, ��������ͷλ��
    /// </summary>
    /// <param name="targetPosition"></param>
    private void UpdateCameraPosition(Vector3 targetPosition)
    {
        //�Ǹ���ģʽ��, ��������߶�����������·�����߶ȱ仯
        float unfollowY = transform.position.y;
        if (!follow)
        {
            RaycastHit hit;
            if (Physics.Raycast(followEmptyObj.position + Vector3.up * 50, Vector3.down, out hit, 150, 1 << LayerMask.NameToLayer("Grid"))) //�����⵽collider
            {
                GridHelper grid = hit.collider.GetComponent<GridHelper>();
                if (grid != null) //�����⵽����
                {
                    unfollowY = grid.info.centerPos.y + Mathf.Sin(Mathf.Deg2Rad * angle) * dist;
                }
                else //�����⵽��collider��������
                {

                }
            }
            else //�����collider��û�м�⵽
            {

            }
        }


        if (!isTacticalView) //��ս���ӽ�
        {
            Vector3 selfBack = -transform.forward;
            selfBack.y = 0;
            cameraPos = targetPosition + (Quaternion.AngleAxis(angle, transform.right.normalized) * selfBack.normalized) * dist;
            cameraPos.y = follow ? cameraPos.y : unfollowY;
            transform.position = Vector3.Lerp(transform.position, cameraPos, Time.deltaTime * autoMoveSpeed);
        }
        else //ս���ӽ�
        {
            cameraPos = targetPosition + Vector3.up * dist;
            cameraPos.y = follow ? cameraPos.y : unfollowY;
            transform.position = Vector3.Lerp(transform.position, cameraPos, Time.deltaTime * autoMoveSpeed);
        }
    }

    /// <summary>
    /// �������ģʽ
    /// </summary>
    private void SmoothFollowTransition()
    {
        //����
        if(isTacticalView) //ս���ӽ�
        {
            cameraPos = followCharacter.transform.position + Vector3.up * defaultDist;
        }
        else //��ս���ӽ�
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

        // �ж��Ƿ���ɹ���
        if (Vector3.Distance(transform.position, cameraPos) < hardDistance &&
            Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            //Debug.Log("Change to Follow Success");

            //��������
            dist = defaultDist;

            changeTofollow = false; // �˾�����
            follow = true; // �л�Ϊ����ģʽ
        }
    }

    /// <summary>
    /// ����ս��ģʽ
    /// </summary>
    /// <param name="targetPos"></param>
    private void SmoothStartTacticalTransition(Vector3 targetPos)
    {
        //����
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

        // �ж��Ƿ���ɹ���
        if (Vector3.Distance(transform.position, cameraPos) < hardDistance &&
            Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            //Debug.Log("Change to Tactical Success");

            //��������
            //dist = defaultDist;
            cameraRight = Vector3.zero;
            cameraUp = Vector3.zero;

            changeBetweenTacticalAndNormal = false; // �˾�����
            isTacticalView = !isTacticalView;
        }
    }

    /// <summary>
    /// �г�ս��ģʽ
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
        

        // �ж��Ƿ���ɹ���
        if (Vector3.Distance(transform.position, cameraPos) < hardDistance &&
            Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            //Debug.Log("Change to Tactical Success");

            //��������
            //dist = defaultDist;
            cameraRight = Vector3.zero;
            cameraUp = Vector3.zero;

            changeBetweenTacticalAndNormal = false; // �˾�����
            isTacticalView = !isTacticalView;

            //������ս���ӽ�ʱ, ֹͣ��Ⱦս���ӽ��µ�����
            StopDrawLastTacticalGrid();
        }
    }

    /// <summary>
    /// WASD ���ƾ�ͷ�ƶ�
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
    /// alt+�Ҽ�+���X�����϶� �����������ת
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
    /// ����ս��������Ⱦ��Ϣ
    /// </summary>
    private void UpdateTacticalGrid()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100, 1 << LayerMask.NameToLayer("Grid"))) //�����⵽collider
        {
            GridHelper grid = hit.collider.GetComponent<GridHelper>();
            if (grid != null) //�����⵽����
            {

                if (lastGrid == null || lastGrid != grid) //����ǵ�һ�μ�⵽���� ���� ��μ�⵽�������֮ǰ����һ��
                {
                    DrawTacticalGrid(grid);
                }
            }
            else //�����⵽��collider��������
            {
                StopDrawLastTacticalGrid(); //ֹͣ��Ⱦ��һ��ս������
            }
        }
        else //�����collider��û�м�⵽
        {
            StopDrawLastTacticalGrid();
        }
    }

    /// <summary>
    /// ��Ⱦս������
    /// </summary>
    /// <param name="grid"></param>
    private void DrawTacticalGrid(GridHelper grid)
    {
        LineRendererPool.Instance.ReturnLineRenderer(lineRenderer); //�Ƚ���һ�������lineRenderer���������

        lineRenderer = LineRendererPool.Instance.GetLineRenderer(); //Ϊ��ǰ����ȡ��һ��lineRenderer
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
    /// ֹͣ��Ⱦս������
    /// </summary>
    private void StopDrawLastTacticalGrid()
    {
        if (lastGrid != null) //�����һ������Ϊ��, ��ֹͣ��Ⱦ��һ������, ������һ�������ÿ�
        {
            LineRendererPool.Instance.ReturnLineRenderer(lineRenderer);
            lastGrid = null;
        }
    }

    /// <summary>
    /// ��ʼ�л������˾�, �����µĸ�������
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
    /// ����ͷƽ�����ƶ����µ����ض�������
    /// </summary>
    private void SmoothChangeLeaderTransition()
    {
        //�������������˾�, ��ֱ�ӷ���(��ֹ��ͷ�˶���ͻ)
        if(changeTofollow || changeBetweenTacticalAndNormal)
        {
            return;
        }

        if (isTacticalView) //ս���ӽ�
        {
            cameraPos = newLeader.transform.position + Vector3.up * dist;
        }
        else //��ս���ӽ�
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

        // �ж��Ƿ���ɹ���
        if (Vector3.Distance(transform.position, cameraPos) < hardDistance)
        {
            //Debug.Log("Change Leader Success");

            //��������
            followCharacter = newLeader;

            changeToNewLeader = false; // �˾�����
            follow = true;
        }
    }
}
