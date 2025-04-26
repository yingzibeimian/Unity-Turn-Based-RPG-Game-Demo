using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    private static InputManager instance;
    public static InputManager Instance => instance;

    public float clickInterval = 0.5f; //������ʱ��(��)
    private float lastClickTime = 0f; //��һ�ε����ʱ��

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //������� �ƶ���ɫ
        if(!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0)) //if (!Input.GetKey(KeyCode.LeftAlt) && !TurnManager.Instance.isInTurn && Input.GetMouseButtonDown(0))
        {
            //ȷ��������ʱ���� �� �����UIԪ��
            if (Time.time - lastClickTime >= clickInterval && !EventSystem.current.IsPointerOverGameObject())
            {
                lastClickTime = Time.time; // ������һ�ε��ʱ��

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 100))
                {
                    GridHelper grid = hit.collider.GetComponent<GridHelper>();
                    if (grid != null)
                    {
                        PartyManager.Instance.OnGridClicked(grid);
                    }
                }
            }
        }
        //���̰���F1��F2��F3��F4, �л�����
        if (Input.GetKeyDown(KeyCode.F1)) //if (Input.GetKeyDown(KeyCode.F1) && !TurnManager.Instance.isInTurn)
        {
            PartyManager.Instance.SwitchLeader(1);
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            PartyManager.Instance.SwitchLeader(2);
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            PartyManager.Instance.SwitchLeader(3);
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            PartyManager.Instance.SwitchLeader(4);
        }
        //���̰���I, ���½�ɫ���ͱ������Ŀɼ���
        if (Input.GetKeyDown(KeyCode.I))
        {
            //�����ɫ���ͱ�����岻����ͬһ״̬, ��һ����, һ���ر�, ���ùرյ�����
            if (UIPartyCharacterManager.Instance.partyCharacterPanel.activeSelf && !UIPartyInventoryManager.Instance.partyInventoryPanel.activeSelf)
            {
                UIPartyInventoryManager.Instance.UpdatePartyInventoryPanelVisibility();

            }
            else if (!UIPartyCharacterManager.Instance.partyCharacterPanel.activeSelf && UIPartyInventoryManager.Instance.partyInventoryPanel.activeSelf)
            {
                UIPartyCharacterManager.Instance.UpdatePartyCharacterPanelVisibility();
            }
            //�����ɫ���ͱ�����崦��ͬһ״̬, ��ͬʱ����������������״̬
            else
            {
                UIPartyCharacterManager.Instance.UpdatePartyCharacterPanelVisibility();
                UIPartyInventoryManager.Instance.UpdatePartyInventoryPanelVisibility();
            }
        }
    }
}
