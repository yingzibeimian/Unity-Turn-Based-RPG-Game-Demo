using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance;

    public List<Character> partyMembers = new List<Character>(); //队伍成员列表

    //party中最多有4个分队group, 即groups[0],groups[1],groups[2],groups[3]
    //且遍历groups0~3得到的角色顺序 即为 UI显示中角色从上到下的顺序
    public List<LinkedList<Character>> groups = new List<LinkedList<Character>>();
    public bool groupsInitialized = false; //用来标记groups初始化状态

    public Character leader; //当前主控角色
    public Dictionary<Character, int> groupDic = new Dictionary<Character, int>(); //记录角色在哪一个分队group当中 


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

        //for (int i = 0; i < 4; i++) // 初始化4个分队
        //{
            groups.Add(new LinkedList<Character>()); // 初始化1个分队
        //}
        if (partyMembers.Count > 0)
        {
            //默认将所有队友加入第1个分队当中
            for(int i = 0; i < partyMembers.Count; i++)
            {
                groups[0].AddLast(partyMembers[i]);
                groupDic.Add(partyMembers[i], 0);
            }
            //默认将第一个加入队伍(即玩家创建角色)作为主控角色
            if(leader == null)
            {
                leader = partyMembers[0];
            }
        }
        groupsInitialized = true;
    }

    /// <summary>
    /// 切换主控角色(按F1~F4)
    /// </summary>
    /// <param name="index"></param>
    public void SwitchLeader(int index)
    {
        if(GetNthCharacterOnUI(index) != null)
        {
            leader = GetNthCharacterOnUI(index);
        }
        else
        {
            return;
        }
        CameraMoveManager.Instance.ChangeToNewLeader(leader); //更新相机
        UIHealthPointManager.Instance.ChangeLeaderHpBar();  //更新下方主控血条
        UIPartyManager.Instance.ChangeBottomLeaderPortrait(leader); //更新下方主控头像
        UISkillBarManager.Instance.UpdateLeaderSkillBar(); //更新主控技能栏
        if (!leader.isInTurn || leader == TurnManager.Instance.nowPlayer) //当角色不在回合制中 或者 是回合制当前轮次的操纵角色时 更新头像高光
        {
            UIPartyManager.Instance.UpdateHighlight(leader); //更新屏幕左侧头像高光
            UIPartyCharacterManager.Instance.UpdateCharacterPanel(leader); //更新角色面板头像高光
        }
    }

    /// <summary>
    /// 切换主控角色(直接点击左侧队伍头像)
    /// </summary>
    /// <param name="cha"></param>
    public void SwitchLeader(Character cha)
    {
        if (groupDic.ContainsKey(cha))
        {
            leader = cha;
        }
        else
        {
            return;
        }
        CameraMoveManager.Instance.ChangeToNewLeader(leader); //更新相机
        UIHealthPointManager.Instance.ChangeLeaderHpBar();  //更新下方主控血条
        UIPartyManager.Instance.ChangeBottomLeaderPortrait(leader); //更新下方主控头像
        UISkillBarManager.Instance.UpdateLeaderSkillBar(); //更新主控技能栏
        if (!leader.isInTurn || leader == TurnManager.Instance.nowPlayer) //当角色不在回合制中 或者 是回合制当前轮次的操纵角色时 更新头像高光
        {
            UIPartyManager.Instance.UpdateHighlight(leader); //更新屏幕左侧头像高光
            UIPartyCharacterManager.Instance.UpdateCharacterPanel(leader); //更新角色面板头像高光
        }
    }

    /// <summary>
    /// 得到
    /// </summary>
    /// <returns></returns>
    public Character GetNthCharacterOnUI(int index)
    {
        int count = 0;
        foreach (LinkedList<Character> group in groups)
        {
            foreach (Character character in group)
            {
                count++;
                if (count == index)
                {
                    return character;
                }
            }
        }
        return null;
    }


    /// <summary>
    /// 回应鼠标点击地板
    /// </summary>
    /// <param name="targetGrid"></param>
    public void OnGridClicked(GridHelper targetGrid)
    {
        if (leader.isInTurn || leader.isSkillTargeting) //如果主控正在回合制中 或者 正在选择技能目标, 则不响应点击, 交由TurnManager来专门管理回合制中玩家角色的点击
        {
            return;
        }

        if(leader != null)
        {
            leader.MoveTo(targetGrid, groups[groupDic[leader]]);
        }
    }

    /// <summary>
    /// 将character移动到targetGroup分组中
    /// </summary>
    /// <param name="character"></param>
    /// <param name="targetGroup"></param>
    public void MoveToGroup(Character draggingChar, Character targetChar)
    {
        int dragIndex = groupDic[draggingChar]; //拖拽角色所在的分组索引
        int targetIndex = groupDic[targetChar]; //目标角色所在的分组索引
        //将拖拽角色移动到目标角色的分队中 目标角色索引+1的位置
        groups[dragIndex].Remove(draggingChar);
        groups[targetIndex].AddAfter(groups[targetIndex].Find(targetChar), draggingChar);
        groupDic[draggingChar] = targetIndex;
        //如果拖拽角色的原分队已经为空, 就删除该分队, 并在groups末尾添加新的分队
        if (groups[dragIndex].Count == 0)
        {
            groups.RemoveAt(dragIndex);
            for(int i = dragIndex; i < groups.Count; i++)
            {
                foreach(Character character in groups[i])
                {
                    groupDic[character]--;
                }
            }
        }
    }

    /// <summary>
    /// 为character创建新的分组
    /// </summary>
    /// <param name="character"></param>
    public void CreatNewGroup(Character character)
    {
        int index = groupDic[character]; //拖拽所在的分组索引
        groups[index].Remove(character); //从原有分队中删除角色
        if(groups[index].Count == 0) //如果原分队中没有角色了
        {
            groups.RemoveAt(index); //移除原分队
            //更新groupDic中每个角色对应分队编号的信息
            for (int i = index; i < groups.Count; i++)
            {
                foreach (Character c in groups[i])
                {
                    groupDic[c]--;
                }
            }
        }
        //创建新的分组, 并加入到groups中
        LinkedList<Character> newGroup = new LinkedList<Character>();
        newGroup.AddLast(character);
        groups.Add(newGroup);
        groupDic[character] = groups.Count - 1;
    }

    /// <summary>
    /// 初始化进入回合的队伍, 为每个角色重新分配分队, 将leader设置为null, 更新队伍UI
    /// </summary>
    public void InitialiazePartyWhenStartTurn()
    {
        if (UIPartyManager.Instance.relayouting)
        {
            return;
        }

        int groupId = 0;
        foreach(LinkedList<Character> group in groups) //重新分队
        {
            foreach(Character c in group)
            {
                groupDic[c] = groupId++;
            }
        }
        groups.Clear();
        for (int i = groups.Count; i < partyMembers.Count; i++) //确保有足够数量分队
        {
            groups.Add(new LinkedList<Character>());
        }
        foreach (Character c in partyMembers)
        {
            groups[groupDic[c]].AddLast(c);
        }
        UIPartyManager.Instance.UpdateHighlight(leader = null); //将leader设置为null + 更新头像高光(所有头像都取消高光)
        UIPartyCharacterManager.Instance.UpdateHighlight(leader);
        StartCoroutine(UIPartyManager.Instance.SmoothRelayout()); //更新头像布局
    }

    /// <summary>
    /// 初始化结束回合的队伍, 为每个角色重新分配分队, 将leader设置为partyMembers[0], 更新队伍UI
    /// </summary>
    public void InitialiazePartyWhenEndTurn()
    {
        for(int i = 1; i < groups.Count; i++)
        {
            Character c = groups[i].First.Value;
            groups[0].AddLast(c); //移入groups[0]分队
            groupDic[c] = 0;
        }
        while(groups.Count > 1)
        {
            groups.RemoveAt(groups.Count - 1);
        }
        //UIPartyManager.Instance.UpdateHighlight(leader = partyMembers[0]); //将leader设置为partyMembers[0] + 更新头像高光
        //UIPartyCharacterManager.Instance.UpdateHighlight(leader);
        //UIHealthPointManager.Instance.UpdateLeaderHpBar();
        //CameraMoveManager.Instance.ChangeToNewLeader(leader);
        SwitchLeader(leader = partyMembers[0]);
        Debug.Log($"leader {leader.info.name}");
        StartCoroutine(UIPartyManager.Instance.SmoothRelayout()); //更新头像布局
    }

    /// <summary>
    /// 队伍加入新角色
    /// </summary>
    /// <param name="character"></param>
    public bool AddCharacter(Character character)
    {
        return false;
    }

    /// <summary>
    /// 角色离队
    /// </summary>
    /// <param name="character"></param>
    public void RemoveCharacter(Character character)
    {
        partyMembers.Remove(character);
        for (int i = 0; i < groups.Count; i++)
        {
            groups[i].Remove(character);
        }
        //更新队伍头像UI

    }
}
