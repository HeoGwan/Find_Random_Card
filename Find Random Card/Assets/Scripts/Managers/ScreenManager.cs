using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _screens;

    private IDictionary<string, GameObject> _screenDic;
    private Stack<string> _screenStack;

    private void Start()
    {
        _screenDic = new Dictionary<string, GameObject>();
        _screenStack = new Stack<string>();

        // ��ũ�� �ʱ�ȭ
        foreach (GameObject screen in _screens)
        {
            _screenDic.Add(screen.name.Split(' ')[0], screen);
            screen.SetActive(false);
        }

        // ���� �޴��� Ȱ��ȭ
        //_screenDic["Start"].SetActive(true);
        _screenStack.Push("Start");
        ShowScreen(true, _screenStack.Peek());
    }

    void ShowScreen(bool isShow, string screenName)
    {
        _screenDic[screenName].SetActive(isShow);
    }

    public void ScreenClear()
    {
        // ���� �ֱٿ� ������ ȭ�� ���ֱ�
        // ��� ȭ�� ���ָ鼭 Ȱ��ȭ�� ȭ���� ȭ�� ���� ���ֱ�
        //ShowScreen(false, _screenStack.Peek());
        while(_screenStack.Count > 0)
        {
            ShowScreen(false, _screenStack.Pop());
        }

        // _screenStack �ʱ�ȭ
        _screenStack.Clear();
        _screenStack.Push("Start");
        ShowScreen(true, _screenStack.Peek());
    }

    public void PrevScreen()
    {
        // ���� ȭ���� ���ÿ��� Pop�Ͽ� �� �� ���� �ֱٿ� Ȱ��ȭ�� ȭ���� �ٽ� �Ҵ�.
        ShowScreen(false, _screenStack.Pop());
        ShowScreen(true, _screenStack.Peek());
    }

    public void GoScreen(string screenName)
    {
        // ���� �޴� ȭ���� ���� ���̵� ȭ������ �Ѿ��
        ShowScreen(false, _screenStack.Peek());
        _screenStack.Push(screenName);
        ShowScreen(true, _screenStack.Peek());
    }

    public void CoverScreen(string screenName)
    {
        _screenStack.Push(screenName);
        ShowScreen(true, _screenStack.Peek());
    }
}
