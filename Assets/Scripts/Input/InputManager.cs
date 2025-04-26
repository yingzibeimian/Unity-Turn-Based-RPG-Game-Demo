using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    private static InputManager instance;
    public static InputManager Instance => instance;

    public float clickInterval = 0.5f; //点击间隔时间(秒)
    private float lastClickTime = 0f; //上一次点击的时间

    private void Awake() => instance = this;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //点击网格 移动角色
        if(!Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0)) //if (!Input.GetKey(KeyCode.LeftAlt) && !TurnManager.Instance.isInTurn && Input.GetMouseButtonDown(0))
        {
            //确保满足点击时间间隔 和 点击非UI元素
            if (Time.time - lastClickTime >= clickInterval && !EventSystem.current.IsPointerOverGameObject())
            {
                lastClickTime = Time.time; // 更新上一次点击时间

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
        //键盘按下F1、F2、F3、F4, 切换主控
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
        //键盘按下I, 更新角色面板和背包面板的可见性
        if (Input.GetKeyDown(KeyCode.I))
        {
            //如果角色面板和背包面板不处于同一状态, 即一个打开, 一个关闭, 就让关闭的面板打开
            if (UIPartyCharacterManager.Instance.partyCharacterPanel.activeSelf && !UIPartyInventoryManager.Instance.partyInventoryPanel.activeSelf)
            {
                UIPartyInventoryManager.Instance.UpdatePartyInventoryPanelVisibility();

            }
            else if (!UIPartyCharacterManager.Instance.partyCharacterPanel.activeSelf && UIPartyInventoryManager.Instance.partyInventoryPanel.activeSelf)
            {
                UIPartyCharacterManager.Instance.UpdatePartyCharacterPanelVisibility();
            }
            //如果角色面板和背包面板处于同一状态, 就同时更新两个面板的现隐状态
            else
            {
                UIPartyCharacterManager.Instance.UpdatePartyCharacterPanelVisibility();
                UIPartyInventoryManager.Instance.UpdatePartyInventoryPanelVisibility();
            }
        }
    }
}
