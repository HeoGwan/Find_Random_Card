using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/*
 * ������ ���̵��� Easy, Normal, Hard�� ������
 * Easy, Normal, Hard ������ 3x3, 4x4, 5x5�� ī���� ���� ������ �ǰ�
 * Easy�� ī�� ���Ⱑ 30sec, Normal�� 20sec, Hard�� 10sec
 * ���� �ٷ� �����ϰ� �ʹٸ� ���� ��ư�� ���� �ٷ� �����ϵ��� �Ѵ�.
 * ������ ����Ǹ� ī��� ���� �޸����� �ٲ�� �ȴ�.
 * ã�ƾ� �ϴ� ī�带 �����ְ� �ش� ī�带 �ٷ� ã���� Fever Gauge�� �� ĭ �ö󰡰�
 * Fever Gauge 5ĭ�� ��� ���� �Ǹ� �ǹ� Ÿ���� ����ǰ� �ǹ� Ÿ���� ���� ��� ī�带 �� �ո����� �ٲ۴�.
 * ���� �ٸ� ī�带 ���� �� Fever Gauge�� �� ĭ �پ��� �ǰ� (����) -> ���� ���̵��� Hard�� FG�� �� ���ش�.
 * ���� �ð��� 30��, 1��, 1�� 30�� �� �ϳ��� �����ϰ� or ���� �ϵ��� �ϰ� -> 1������ ����
 * ���� �ð��� ��� ������ ������ �����Ѵ�. (DB, Firebase, etc.. �̿�)
 * 
 * �� �߰��ϰ� �ʹٸ� ������ ������ ��� �޸� ��Ų�� �� �� �ֵ��� �Ѵ�.
*/

public enum DIFFICULTY
{
    EASY,
    NORMAL,
    HARD,
    NONE
}

/*
 * TODO
 * ���� ��� ���� (������Ÿ��)
    �� �� ī�� ���� : 30����
 * ���� �Ұ� �����ϰ�
 * �ٹв� �ٹ̰�
 * �߰��Ұ� �߰��ϰ�
*/

public class CardInfo
{
    public int Number { get; set; }
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private DIFFICULTY _difficulty;
    public DIFFICULTY Difficulty { get { return _difficulty; } }

    private List<CardInfo> cards;
    private List<CardInfo> newCards;

    private CardInfo findCard;

    private GridLayoutGroup _cardLayoutGroup;
    private int _gridSize;

    // Inspector
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private TextMeshProUGUI findCardNumber;
    [SerializeField] private Transform cardObj;
    [SerializeField] private TextMeshProUGUI showDifficulty;

    [Space(10)]
    [Header("�� Managers")]
    public Transform objectManager;
    public ScreenManager screenManager;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            print("���� �Ŵ����� �����մϴ�.");
            Destroy(gameObject);
        }

        cards = new List<CardInfo>(new CardInfo[30]);

        int number = 1;

        for (int index = 0; index < cards.Count; ++index)
        {
            cards[index] = new CardInfo();
            cards[index].Number = number++;
        }

        newCards = new List<CardInfo>();
        findCard = new CardInfo();

        _cardLayoutGroup = cardObj.gameObject.GetComponent<GridLayoutGroup>();
    }

    void GameInit()
    {
        //startScreen.SetActive(false);
        //gameScreen.SetActive(true);
        //newCards = Enumerable.Repeat(new CardInfo(), 25).ToList();
    }

    public void SelectDifficulty(int gridSize)
    {
        switch (gridSize)
        {
            case 3:
                _difficulty = DIFFICULTY.EASY;
                showDifficulty.text = "Easy";
                break;
            case 4:
                _difficulty = DIFFICULTY.NORMAL;
                showDifficulty.text = "Normal";
                break;
            case 5:
                _difficulty = DIFFICULTY.HARD;
                showDifficulty.text = "Hard";
                break;
            default:
                _difficulty = DIFFICULTY.NONE;
                showDifficulty.text = "???";
                print("���̵��� �����ؾ� �մϴ�.");
                break;
        }
        _gridSize = gridSize;
    }

    public void GameStart()
    {
        print("���� ����");
        //GameInit();

        /*
         * ���� ���� �� ī�� ������ ����Ǿ� �ִ� ����Ʈ �� 25(pow(n))���� ��� �����´�.
        */
        // ���� ����
        int random1, random2;
        CardInfo temp;

        for (int index = 0; index < cards.Count; ++index)
        {
            random1 = UnityEngine.Random.Range(0, cards.Count);
            random2 = UnityEngine.Random.Range(0, cards.Count);

            temp = cards[random1];
            cards[random1] = cards[random2];
            cards[random2] = temp;
        }

        // �������� ���� ī�� ���� �� pow(n, 2)�� ��ŭ ��������
        int cellSize = -50 * _gridSize + 400;
        _cardLayoutGroup.cellSize = new Vector2(cellSize, cellSize + 50);
        newCards = cards.GetRange(0, (int)Mathf.Pow(_gridSize, 2));

        for (int index = 0; index < newCards.Count; ++index)
        {
            // ī�� ������Ʈ ��������
            objectManager.GetChild(0).SetParent(cardObj);
            // ������ ī�� ������Ʈ�� ���� ����
            cardObj.GetChild(index).GetComponent<Card>().SetCardInfo(newCards[index]);
        }

        ChangeNumber();
    }

    public void GameOver()
    {
        screenManager.ScreenClear();
        int cardObjChild = cardObj.childCount;

        for (int index = 0; index < cardObjChild; ++index)
        {
            cardObj.GetChild(0).SetParent(objectManager);
        }
    }

    public void ChangeNumber()
    {
        if (newCards.Count <= 0)
        {
            GameOver();
            return;
        }

        int findNumberIndex = Random.Range(0, newCards.Count);
        findCard = newCards[findNumberIndex];
        findCardNumber.text = findCard.Number.ToString();
    }

    public bool CheckNumber(CardInfo cardInfo)
    {
        if (cardInfo.Number == findCard.Number)
        {
            print("������ ���߼̽��ϴ�!");
            newCards.Remove(findCard);
            ChangeNumber();
            return true;
        }
        else
        {
            print("�����Դϴ�.");
            return false;
        }
    }

    public void GameExit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();  
        #endif
    }
}

/*
 * 2023-03-05 00:30 -> ���� ���� �̱� �ϼ�
 * 2023-03-05 18:21 -> ���̵� ���� �� ȭ�� ����
 * TODO
 *  �׸��� ���ڿ� ���� �̴� ���� �� ī�� ���̴°� �޸� �ϱ�
*/