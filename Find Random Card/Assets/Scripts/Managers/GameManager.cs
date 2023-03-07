using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TMPro.Examples;
using Unity.Mathematics;
using Unity.VisualScripting.FullSerializer;
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

// Grid Size�� ���� DIFFICULTY�� �޶���
public enum DIFFICULTY
{
    EASY = 3,
    NORMAL = 4,
    HARD = 5,
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
    public static GameManager                   instance;

    // ���� ���� ����
    private DIFFICULTY                          _difficulty;

    // ���� 
    [Header("�� Variables")]
    [SerializeField] private float              _maxGameTime = 60f;
    private float                               _gameTime;
    private bool                                _isGame = false;

    // �̸�����
    private float                               _maxPreviewTime;
    private float                               _previewTime;
    private bool                                _isPreview = false;

    // �޺� (�ǹ�)
    [SerializeField] private int                _maxComboStack = 5;
    private int                                 _comboStack = 0;
    private float                               _maxFeverTime;
    private float                               _feverTime = 0;
    private bool                                _isFever = false;

    // Getter, Setter
    public DIFFICULTY                           Difficulty { get { return _difficulty; } }
    public bool                                 _IsFever { get { return _isFever; } }


    // ī�� ���� ����
    [SerializeField] private int                _maxCardTypeCount;
    private List<CardInfo>                      cards;
    private List<CardInfo>                      newCards;

    private CardInfo                            findCard;
    private List<Card>                          curCards;

    private GridLayoutGroup                     _cardLayoutGroup;
    private int                                 _gridSize;

    // Show In Inspector
    [Header("�� Objects")]
    [SerializeField] private Transform          cardObj;
    [SerializeField] private Image              showTime;
    [SerializeField] private Image              showComboGauge;

    [Space(10)]
    [Header("�� Text Objects")]
    [SerializeField] private TextMeshProUGUI    showFindCardNumber;
    [SerializeField] private TextMeshProUGUI    showDifficulty;
    [SerializeField] private TextMeshProUGUI    showRemainPreviewTime;
    [SerializeField] private TextMeshProUGUI    showGameTimeInfo;

    [Space(10)]
    [Header("�� Managers")]
    public Transform                            objectManager;
    public ScreenManager                        screenManager;
    
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

        cards = new List<CardInfo>(new CardInfo[_maxCardTypeCount]);

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
        // �ǹ�
        Fever();

        // �̸�����
        Preview();

        // ���� ����
        Game();
    }


    // Update ���� �Լ�
    void Fever()
    {
        if (!_isFever) return;

        _feverTime += Time.deltaTime;

        // �ǹ� �ð� ���� ���� ī�带 ������ ��� ī�带 �����ش�.
        foreach (Card curCard in curCards)
        {
            curCard.FlipCard(true);
        }

        // �ǹ� ������ �ִϸ��̼�
        showComboGauge.fillAmount = 1 - (_feverTime / _maxFeverTime);

        if (_feverTime >= _maxFeverTime)
        {
            // �ǹ� Ÿ�� ��
            _isFever = false;
            _feverTime = 0;
            showComboGauge.fillAmount = 0;

            foreach (Card curCard in curCards)
            {
                curCard.FlipCard(false);
            }
        }
    }

    void Preview()
    {
        if (!_isPreview) return;

        // �̸����� ���� ���
        // �̸����� �ð� ����
        _previewTime += Time.deltaTime;

        showRemainPreviewTime.text = ((int)(_maxPreviewTime - _previewTime)).ToString();

        if (_previewTime >= _maxPreviewTime)
        {
            // �̸����� �ð��� ������ ���
            PreviewOver();
        }
    }

    void Game()
    {
        if (!_isGame) return;

        _gameTime += Time.deltaTime;

        showTime.fillAmount = 1 - (_gameTime / _maxGameTime);

        if (_gameTime >= _maxGameTime)
        {
            // ���� ��
            GameOver();
        }
    }

    // User Function
    void GameInit()
    {
        _gameTime = 0;
        _comboStack = 0;
        _feverTime = 0;
        _isFever = false;
        _isGame = true;
        showComboGauge.fillAmount = 0;
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

    void ShowComboGauge()
    {
        showComboGauge.fillAmount = (1.0f / _maxComboStack) * _comboStack;
    }

    void StackCombo(bool isCombo)
    {
        if (!isCombo)
        {
            // �޺��� �Ƚ��� (Easy, Normal�̸� �޺��� �� ĭ ���̰� Hard�� ���� ����)
            switch (_difficulty)
            {
                case DIFFICULTY.EASY:
                case DIFFICULTY.NORMAL:
                    _comboStack = (_comboStack - 1 > 0) ? _comboStack - 1 : 0;
                    break;
                case DIFFICULTY.HARD:
                    _comboStack = 0;
                    break;
            }
        }
        else
        {
            // �޺��� ����
            _comboStack = (_comboStack + 1 > _maxComboStack) ? _comboStack : _comboStack + 1;
            if (_comboStack == _maxComboStack && _isFever == false)
            {
                // �ǹ� Ÿ��
                _isFever = true;
                _feverTime = 0;
            }
        }

        ShowComboGauge();
    }

    // public �Լ�
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
            curCard.FlipCard(false);
        }

        GameStart();
    }

    public void SelectDifficulty(int gridSize)
    {
        int cellSize = 0;
        switch ((DIFFICULTY)gridSize)
        {
            case DIFFICULTY.EASY:
                // 10��
                //_difficulty = DIFFICULTY.EASY;
                _maxPreviewTime = 10;
                showDifficulty.text = "Easy";
                cellSize = 300;
                break;
            case DIFFICULTY.NORMAL:
                // 20��
                //_difficulty = DIFFICULTY.NORMAL;
                _maxPreviewTime = 20;
                showDifficulty.text = "Normal";
                cellSize = 200;
                break;
            case DIFFICULTY.HARD:
                // 30��
                //_difficulty = DIFFICULTY.HARD;
                showDifficulty.text = "Hard";
                _maxPreviewTime = 30;
                cellSize = 150;
                break;
        }
        _difficulty = (DIFFICULTY)gridSize;
        _gridSize = gridSize;
        _maxFeverTime = _maxPreviewTime / 5;
        // �׸��� ������ ����
        _cardLayoutGroup.cellSize = new Vector2(cellSize, cellSize + 50);
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
        //int cellSize = -50 * _gridSize + 400;
        //_cardLayoutGroup.cellSize = new Vector2(cellSize, cellSize + 50);

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
        _previewTime = 0;
        curCards.Clear();

        // ȭ�� �ʱ�ȭ -> ���� ���� ȭ������
        //screenManager.ScreenClear();
        // ���� ���� ȭ������
        screenManager.GoScreen("GameOver");

        // �ɸ� �ð� �����ֱ�
        showGameTimeInfo.text = "�ɸ� �ð�: " + _gameTime.ToString("F2");

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
        showFindCardNumber.text = findCard.Number.ToString();
    }

    public bool CheckNumber(CardInfo cardInfo)
    {
        if (cardInfo.Number == findCard.Number)
        {
            newCards.Remove(findCard);
            ChangeNumber();
            StackCombo(true);
            return true;
        }
        else
        {
            print("�����Դϴ�.");
            StackCombo(false);
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
 * 2023-03-06 20:43 -> ���� ���� ȭ�� �߰� �� �޺� ��� �߰�
 * 2023-03-07 12:20 -> �ǹ� ��� �߰� (�ǹ� �ð��� �̸����� �ð��� 1/5��)
 * 2023-03-07 15:10 -> 
 * TODO
 *  
*/