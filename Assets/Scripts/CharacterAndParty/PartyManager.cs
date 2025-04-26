using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance;

    public List<Character> partyMembers = new List<Character>(); //�����Ա�б�

    //party�������4���ֶ�group, ��groups[0],groups[1],groups[2],groups[3]
    //�ұ���groups0~3�õ��Ľ�ɫ˳�� ��Ϊ UI��ʾ�н�ɫ���ϵ��µ�˳��
    public List<LinkedList<Character>> groups = new List<LinkedList<Character>>();
    public bool groupsInitialized = false; //�������groups��ʼ��״̬

    public Character leader; //��ǰ���ؽ�ɫ
    public Dictionary<Character, int> groupDic = new Dictionary<Character, int>(); //��¼��ɫ����һ���ֶ�group���� 


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

        //for (int i = 0; i < 4; i++) // ��ʼ��4���ֶ�
        //{
            groups.Add(new LinkedList<Character>()); // ��ʼ��1���ֶ�
        //}
        if (partyMembers.Count > 0)
        {
            //Ĭ�Ͻ����ж��Ѽ����1���ֶӵ���
            for(int i = 0; i < partyMembers.Count; i++)
            {
                groups[0].AddLast(partyMembers[i]);
                groupDic.Add(partyMembers[i], 0);
            }
            //Ĭ�Ͻ���һ���������(����Ҵ�����ɫ)��Ϊ���ؽ�ɫ
            if(leader == null)
            {
                leader = partyMembers[0];
            }
        }
        groupsInitialized = true;
    }

    /// <summary>
    /// �л����ؽ�ɫ(��F1~F4)
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
        CameraMoveManager.Instance.ChangeToNewLeader(leader); //�������
        UIHealthPointManager.Instance.ChangeLeaderHpBar();  //�����·�����Ѫ��
        UIPartyManager.Instance.ChangeBottomLeaderPortrait(leader); //�����·�����ͷ��
        UISkillBarManager.Instance.UpdateLeaderSkillBar(); //�������ؼ�����
        if (!leader.isInTurn || leader == TurnManager.Instance.nowPlayer) //����ɫ���ڻغ����� ���� �ǻغ��Ƶ�ǰ�ִεĲ��ݽ�ɫʱ ����ͷ��߹�
        {
            UIPartyManager.Instance.UpdateHighlight(leader); //������Ļ���ͷ��߹�
            UIPartyCharacterManager.Instance.UpdateCharacterPanel(leader); //���½�ɫ���ͷ��߹�
        }
    }

    /// <summary>
    /// �л����ؽ�ɫ(ֱ�ӵ��������ͷ��)
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
        CameraMoveManager.Instance.ChangeToNewLeader(leader); //�������
        UIHealthPointManager.Instance.ChangeLeaderHpBar();  //�����·�����Ѫ��
        UIPartyManager.Instance.ChangeBottomLeaderPortrait(leader); //�����·�����ͷ��
        UISkillBarManager.Instance.UpdateLeaderSkillBar(); //�������ؼ�����
        if (!leader.isInTurn || leader == TurnManager.Instance.nowPlayer) //����ɫ���ڻغ����� ���� �ǻغ��Ƶ�ǰ�ִεĲ��ݽ�ɫʱ ����ͷ��߹�
        {
            UIPartyManager.Instance.UpdateHighlight(leader); //������Ļ���ͷ��߹�
            UIPartyCharacterManager.Instance.UpdateCharacterPanel(leader); //���½�ɫ���ͷ��߹�
        }
    }

    /// <summary>
    /// �õ�
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
    /// ��Ӧ������ذ�
    /// </summary>
    /// <param name="targetGrid"></param>
    public void OnGridClicked(GridHelper targetGrid)
    {
        if (leader.isInTurn || leader.isSkillTargeting) //����������ڻغ����� ���� ����ѡ����Ŀ��, ����Ӧ���, ����TurnManager��ר�Ź���غ�������ҽ�ɫ�ĵ��
        {
            return;
        }

        if(leader != null)
        {
            leader.MoveTo(targetGrid, groups[groupDic[leader]]);
        }
    }

    /// <summary>
    /// ��character�ƶ���targetGroup������
    /// </summary>
    /// <param name="character"></param>
    /// <param name="targetGroup"></param>
    public void MoveToGroup(Character draggingChar, Character targetChar)
    {
        int dragIndex = groupDic[draggingChar]; //��ק��ɫ���ڵķ�������
        int targetIndex = groupDic[targetChar]; //Ŀ���ɫ���ڵķ�������
        //����ק��ɫ�ƶ���Ŀ���ɫ�ķֶ��� Ŀ���ɫ����+1��λ��
        groups[dragIndex].Remove(draggingChar);
        groups[targetIndex].AddAfter(groups[targetIndex].Find(targetChar), draggingChar);
        groupDic[draggingChar] = targetIndex;
        //�����ק��ɫ��ԭ�ֶ��Ѿ�Ϊ��, ��ɾ���÷ֶ�, ����groupsĩβ����µķֶ�
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
    /// Ϊcharacter�����µķ���
    /// </summary>
    /// <param name="character"></param>
    public void CreatNewGroup(Character character)
    {
        int index = groupDic[character]; //��ק���ڵķ�������
        groups[index].Remove(character); //��ԭ�зֶ���ɾ����ɫ
        if(groups[index].Count == 0) //���ԭ�ֶ���û�н�ɫ��
        {
            groups.RemoveAt(index); //�Ƴ�ԭ�ֶ�
            //����groupDic��ÿ����ɫ��Ӧ�ֶӱ�ŵ���Ϣ
            for (int i = index; i < groups.Count; i++)
            {
                foreach (Character c in groups[i])
                {
                    groupDic[c]--;
                }
            }
        }
        //�����µķ���, �����뵽groups��
        LinkedList<Character> newGroup = new LinkedList<Character>();
        newGroup.AddLast(character);
        groups.Add(newGroup);
        groupDic[character] = groups.Count - 1;
    }

    /// <summary>
    /// ��ʼ������غϵĶ���, Ϊÿ����ɫ���·���ֶ�, ��leader����Ϊnull, ���¶���UI
    /// </summary>
    public void InitialiazePartyWhenStartTurn()
    {
        if (UIPartyManager.Instance.relayouting)
        {
            return;
        }

        int groupId = 0;
        foreach(LinkedList<Character> group in groups) //���·ֶ�
        {
            foreach(Character c in group)
            {
                groupDic[c] = groupId++;
            }
        }
        groups.Clear();
        for (int i = groups.Count; i < partyMembers.Count; i++) //ȷ�����㹻�����ֶ�
        {
            groups.Add(new LinkedList<Character>());
        }
        foreach (Character c in partyMembers)
        {
            groups[groupDic[c]].AddLast(c);
        }
        UIPartyManager.Instance.UpdateHighlight(leader = null); //��leader����Ϊnull + ����ͷ��߹�(����ͷ��ȡ���߹�)
        UIPartyCharacterManager.Instance.UpdateHighlight(leader);
        StartCoroutine(UIPartyManager.Instance.SmoothRelayout()); //����ͷ�񲼾�
    }

    /// <summary>
    /// ��ʼ�������غϵĶ���, Ϊÿ����ɫ���·���ֶ�, ��leader����ΪpartyMembers[0], ���¶���UI
    /// </summary>
    public void InitialiazePartyWhenEndTurn()
    {
        for(int i = 1; i < groups.Count; i++)
        {
            Character c = groups[i].First.Value;
            groups[0].AddLast(c); //����groups[0]�ֶ�
            groupDic[c] = 0;
        }
        while(groups.Count > 1)
        {
            groups.RemoveAt(groups.Count - 1);
        }
        //UIPartyManager.Instance.UpdateHighlight(leader = partyMembers[0]); //��leader����ΪpartyMembers[0] + ����ͷ��߹�
        //UIPartyCharacterManager.Instance.UpdateHighlight(leader);
        //UIHealthPointManager.Instance.UpdateLeaderHpBar();
        //CameraMoveManager.Instance.ChangeToNewLeader(leader);
        SwitchLeader(leader = partyMembers[0]);
        Debug.Log($"leader {leader.info.name}");
        StartCoroutine(UIPartyManager.Instance.SmoothRelayout()); //����ͷ�񲼾�
    }

    /// <summary>
    /// ��������½�ɫ
    /// </summary>
    /// <param name="character"></param>
    public bool AddCharacter(Character character)
    {
        return false;
    }

    /// <summary>
    /// ��ɫ���
    /// </summary>
    /// <param name="character"></param>
    public void RemoveCharacter(Character character)
    {
        partyMembers.Remove(character);
        for (int i = 0; i < groups.Count; i++)
        {
            groups[i].Remove(character);
        }
        //���¶���ͷ��UI

    }
}
