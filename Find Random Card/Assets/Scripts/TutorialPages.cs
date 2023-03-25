using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialPages : MonoBehaviour
{
    [SerializeField] private GameObject[] _pages;

    private int _currentPageIndex = 0;

    private void Awake()
    {
        foreach (GameObject page in _pages)
        {
            page.SetActive(false);
        }

        _pages[0].SetActive(true);
    }
        
    public void NextPage()
    {
        // ���� ������(_currentPageIndex)�� ������ ������(_pages.Length - 1)�� ��� �۵����� ����
        if (_currentPageIndex == _pages.Length - 1) return;

        _pages[_currentPageIndex++].SetActive(false);
        _pages[_currentPageIndex].SetActive(true);
    }

    public void PrevPage()
    {
        // ���� ������(_currentPageIndex)�� ù ��° ������(0)�� ��� �۵����� ����
        if (_currentPageIndex == 0) return;

        _pages[_currentPageIndex--].SetActive(false);
        _pages[_currentPageIndex].SetActive(true);
    }

    public void StopTutorial()
    {
        _currentPageIndex = 0;

        GameManager._instance._screenManager.PrevScreen();
        GameManager._instance._soundManager.PlayEffectSound("ButtonPopSoundDown");
    }
}
