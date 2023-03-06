using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    [SerializeField] private GameObject[] screens;

    private IDictionary<string, GameObject> screenDic;
    private Stack<string> screenStack;

    private void Start()
    {
        screenDic = new Dictionary<string, GameObject>();
        screenStack = new Stack<string>();

        // ��ũ�� �ʱ�ȭ
        foreach (GameObject screen in screens)
        {
            screenDic.Add(screen.name.Split(' ')[0], screen);
            screen.SetActive(false);
        }

        // ���� �޴��� Ȱ��ȭ
        //screenDic["Start"].SetActive(true);
        screenStack.Push("Start");
        ShowScreen(true, screenStack.Peek());
    }

    void ShowScreen(bool isShow, string screenName)
    {
        screenDic[screenName].SetActive(isShow);
    }

    public void ScreenClear()
    {
        // ���� �ֱٿ� ������ ȭ�� ���ֱ�
        ShowScreen(false, screenStack.Peek());

        // screenStack �ʱ�ȭ
        screenStack.Clear();
        screenStack.Push("Start");
        ShowScreen(true, screenStack.Peek());
    }

    public void PrevScreen()
    {
        // ���� ȭ���� ���ÿ��� Pop�Ͽ� �� �� ���� �ֱٿ� Ȱ��ȭ�� ȭ���� �ٽ� �Ҵ�.
        ShowScreen(false, screenStack.Pop());
        ShowScreen(true, screenStack.Peek());
    }

    public void GoScreen(string screenName)
    {
        // ���� �޴� ȭ���� ���� ���̵� ȭ������ �Ѿ��
        ShowScreen(false, screenStack.Peek());
        screenStack.Push(screenName);
        ShowScreen(true, screenStack.Peek());
    }

    public void CoverScreen(string screenName)
    {
        screenStack.Push(screenName);
        ShowScreen(true, screenStack.Peek());
    }
}
