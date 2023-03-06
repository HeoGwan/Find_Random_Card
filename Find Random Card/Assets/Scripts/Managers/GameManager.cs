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

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // ���� ���� ����
    private DIFFICULTY _difficulty;
    public DIFFICULTY Difficulty { get { return _difficulty; } }

    private float _maxGameTime = 60f;
    private float _gameTime;
    private float _maxPreviewTime;
    private float _previewTime;
    private bool _isGame;
    private bool _isPreview;

    // ī�� ���� ����
    private List<CardInfo> cards;
    private List<CardInfo> newCards;

    private CardInfo findCard;
    private List<Card> curCards;

    private GridLayoutGroup _cardLayoutGroup;
    private int _gridSize;

    // Show In Inspector
    [SerializeField] private TextMeshProUGUI findCardNumber;
    [SerializeField] private Transform cardObj;
    [SerializeField] private TextMeshProUGUI showDifficulty;
    [SerializeField] private Image showTime;
    [SerializeField] private TextMeshProUGUI showRemainPreviewTime;

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
        curCards = new List<Card>();

        _cardLayoutGroup = cardObj.gameObject.GetComponent<GridLayoutGroup>();
    }

    private void Update()
    {
        if (_isPreview)
        {
            // �̸����� ���� ���
            // �̸����� �ð� ����
            _previewTime += Time.deltaTime;

            if (_previewTime >= _maxPreviewTime)
            {
                // �̸����� �ð��� ������ ���
                PreviewOver();
            }
            showRemainPreviewTime.text = ((int)(_maxPreviewTime - _previewTime)).ToString();
        }

        if (!_isGame) return;

        _gameTime += Time.deltaTime;

        if (_gameTime >= _maxGameTime)
        {
            // ���� ��
            GameOver();
        }

        showTime.fillAmount = 1 - (_gameTime / _maxGameTime);
    }

    void GameInit()
    {
        _gameTime = 0;
        _isGame = true;
        showTime.fillAmount = 1;
    }

    void ReturnCards()
    {
        // ī�� �ݳ�
        int cardObjChild = cardObj.childCount;

        for (int index = 0; index < cardObjChild; ++index)
        {
            cardObj.GetChild(0).SetParent(objectManager);
        }
    }

    void ShuffleCards()
    {
        if (cardObj.childCount != 0) ReturnCards();

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
    }

    void BringCards()
    {
        // �������� ���� ī�� ���� �� pow(n, 2)�� ��ŭ ��������
        newCards = cards.GetRange(0, (int)Mathf.Pow(_gridSize, 2));

        for (int index = 0; index < newCards.Count; ++index)
        {
            // ī�� ������Ʈ ��������
            objectManager.GetChild(0).SetParent(cardObj);

            newCards[index].PreviewTime = _maxPreviewTime;

            // ������ ī�� ������Ʈ�� ���� ����
            cardObj.GetChild(index).GetComponent<Card>().SetCardInfo(newCards[index]);
            curCards.Add(cardObj.GetChild(index).GetComponent<Card>());
        }
    }

    void ShowPreview()
    {
        _previewTime = 0;
        _isPreview = true;

        // Preview ȭ���� ������ ���̴� ȭ�鿡 ������
        screenManager.CoverScreen("Preview");
    }

    public void PreviewOver()
    {
        if (!_isPreview) return; // �̹� ����� �����̸� �۵����� �ʴ´�.

        // �̸����� ���� �� �ٷ� ���� ����
        _isPreview = false;
        // �̸����� ȭ���� ���� ���߿� �� ȭ���̹Ƿ� ȭ���� �����Ѵ�.
        screenManager.PrevScreen();
        // ī�带 ���� �����´�. (ī�带 ���� �� ���̵��� �ٲ۴�.)
        foreach (Card curCard in curCards)
        {
            curCard.FlipCard();
        }

        GameStart();
    }

    public void SelectDifficulty(int gridSize)
    {
        switch (gridSize)
        {
            case 3:
                // 10��
                _difficulty = DIFFICULTY.EASY;
                _maxPreviewTime = 10;
                showDifficulty.text = "Easy";
                break;
            case 4:
                // 20��
                _difficulty = DIFFICULTY.NORMAL;
                _maxPreviewTime = 20;
                showDifficulty.text = "Normal";
                break;
            case 5:
                // 30��
                _difficulty = DIFFICULTY.HARD;
                showDifficulty.text = "Hard";
                _maxPreviewTime = 30;
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
        // �̸����Ⱑ ���� �� �̸����� ȭ���� ���� ����� ������ �����Ѵ�.
        GameInit();
    }

    public void GameReady()
    {
        // ī�� ��ġ, �̸����� �����ֱ�

        /* ī�� ��ġ ���� */
        // �׸��� ����� �°� ī�� ������ ����
        int cellSize = -50 * _gridSize + 400;
        _cardLayoutGroup.cellSize = new Vector2(cellSize, cellSize + 50);

        /*
         * ���� ���� �� ī�� ������ ����Ǿ� �ִ� ����Ʈ �� 25(pow(n))���� ��� �����´�.
        */
        // ī�� ����
        ShuffleCards();

        // ī�� ��������
        BringCards();

        // ã�ƾ� �ϴ� ī�� �̱�
        ChangeNumber();
        /* ī�� ��ġ ���� */

        // �̸����� ȭ�� �����ֱ�
        ShowPreview();
    }

    public void GameOver()
    {
        // ���� �ʱ�ȭ
        _isGame = false;
        _isPreview = false;
        curCards.Clear();

        // ȭ�� �ʱ�ȭ -> ���� ���� ȭ������
        screenManager.ScreenClear();

        // ī�� �ݳ�
        ReturnCards();
    }

    public void ChangeNumber()
    {
        if (newCards.Count <= 0)
        {
            // ���� �� ī�尡 ������ ī�带 ��� ���Ӱ� �����´�. -> X
            // ���� �� ī�尡 ������ ������ �����Ѵ�.
            GameOver();
            return;
            //ShuffleCards();
            //BringCards();
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

    public void IsGamePause(bool isPause)
    {
        _isGame = !isPause;
    }
}

/*
 * 2023-03-05 00:30 -> ���� ���� �̱� �ϼ�
 * 2023-03-05 18:21 -> ���̵� ���� �� ȭ�� ����
 * 2023-03-06 19:34 -> ���� �ð� ���� �� �̸����� ��� ����
 * TODO
 *  ���� �ð� ����(1��) : O
 *  ���̵��� ���� �̸����� �ð� ���� -> �̸����� ��� ����
*/