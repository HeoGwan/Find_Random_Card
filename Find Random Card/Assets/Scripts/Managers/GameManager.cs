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
    public static GameManager                   _instance;

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
    private sbyte                               _comboStatus = 0;
    [SerializeField] private float              _maxComboAnimationTime = 0.5f;
    private float                               _comboAnimationTime = 0f;
    private float                               _comboPart;
    private float                               _comboStackPoint; // _comboPart * _comboStack
    private float                               _startValue;    // �޺� �ִϸ��̼ǿ� �ʿ��� ����
    private float                               _endValue;      // �޺� �ִϸ��̼ǿ� �ʿ��� ����


    // Getter, Setter
    public DIFFICULTY                           Difficulty { get { return _difficulty; } }
    public bool                                 IsFever { get { return _isFever; } }


    // ī�� ���� ����
    [SerializeField] private int                _maxCardTypeCount;
    private List<CardInfo>                      _cards;
    private List<CardInfo>                      _newCards;

    private CardInfo                            _findCard;
    private List<Card>                          _curCards;

    private GridLayoutGroup                     _cardLayoutGroup;
    private int                                 _gridSize;
    private float                               _flipCardSize;

    // Show In Inspector
    [Header("�� Objects")]
    [SerializeField] private Transform          _cardObj;
    [SerializeField] private Slider             _showTime;
    [SerializeField] private Image              _showComboGauge;

    [Space(10)]
    [Header("�� Text Objects")]
    [SerializeField] private TextMeshProUGUI    _showFindCardNumber;
    [SerializeField] private TextMeshProUGUI    _showDifficulty;
    [SerializeField] private TextMeshProUGUI    _showRemainPreviewTime;
    [SerializeField] private TextMeshProUGUI    _showGameTimeInfo;

    [Space(10)]
    [Header("�� Managers")]
    public Transform                            _objectManager;
    public ScreenManager                        _screenManager;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
#if UNITY_EDITOR
            print("���� �Ŵ����� �����մϴ�.");
#endif
            Destroy(gameObject);
        }

        _cards = new List<CardInfo>(new CardInfo[_maxCardTypeCount]);

        int number = 1;

        for (int index = 0; index < _cards.Count; ++index)
        {
            _cards[index] = new CardInfo();
            _cards[index].Number = number++;
        }

        _newCards = new List<CardInfo>();
        _findCard = new CardInfo();
        _curCards = new List<Card>();

        _cardLayoutGroup = _cardObj.gameObject.GetComponent<GridLayoutGroup>();

        _comboPart = 1.0f / _maxComboStack;
        _comboStackPoint = _comboPart * _comboStack;
    }

    private void Start()
    {
        SetResolution();
    }

    private void Update()
    {
        // �޺�
        Combo();

        // �ǹ�
        Fever();

        // �̸�����
        Preview();

        // ���� ����
        Game();
    }

    // Update ���� �Լ�
    void Combo()
    {
        if (_comboStatus == 0 && !_isFever) return;

        // ��� �ð� ������Ʈ
        _comboAnimationTime += Time.deltaTime;

        // �ִϸ��̼� ���� ���� ���
        float progress = Mathf.Clamp01(_comboAnimationTime / _maxComboAnimationTime);

        // ���� �� ���
        _startValue = Mathf.Lerp(_startValue, _endValue, progress);

        // ���� ������ �ִϸ��̼� ����
        _showComboGauge.fillAmount = _startValue;

        // �ִϸ��̼� ���� üũ
        if (progress >= 1f)
        {
            _comboStatus = 0;
            _comboAnimationTime = 0;
            return;
        }
    }

    void Fever()
    {
        if (!_isFever) return;

        _feverTime += Time.deltaTime;

        // �ǹ� ������ �ִϸ��̼�
        _showComboGauge.fillAmount = 1 - (_feverTime / _maxFeverTime);

        if (_feverTime >= _maxFeverTime)
        {
            // �ǹ� Ÿ�� ��
            _isFever = false;
            _feverTime = 0;
            _showComboGauge.fillAmount = 0;
            _comboStack = 0;

            foreach (Card curCard in _curCards)
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

        _showRemainPreviewTime.text = ((int)(_maxPreviewTime - _previewTime)).ToString();

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

        _showTime.value = 1 - (_gameTime / _maxGameTime);

        if (_gameTime >= _maxGameTime)
        {
            // ���� ��
            GameOver();
        }
    }

    // User Function

    void StartFever()
    {
        _isFever = true;
        _feverTime = 0;
        // �ǹ� �ð� ���� ���� ī�带 ������ ��� ī�带 �����ش�.
        foreach (Card curCard in _curCards)
        {
            curCard.FlipCard(true);
        }
    }

    void ReturnCards()
    {
        // ī�� �ݳ�
        int cardObjChild = _cardObj.childCount;

        for (int index = 0; index < cardObjChild; ++index)
        {
            _cardObj.GetChild(0).SetParent(_objectManager);
        }
    }

    void ShuffleCards()
    {
        if (_cardObj.childCount != 0) ReturnCards();

        // ���� ����
        int random1, random2;
        CardInfo temp;

        for (int index = 0; index < _cards.Count; ++index)
        {
            random1 = UnityEngine.Random.Range(0, _cards.Count);
            random2 = UnityEngine.Random.Range(0, _cards.Count);

            temp = _cards[random1];
            _cards[random1] = _cards[random2];
            _cards[random2] = temp;
        }
    }

    void BringCards()
    {
        // �������� ���� ī�� ���� �� pow(n, 2)�� ��ŭ ��������
        _newCards = _cards.GetRange(0, (int)Mathf.Pow(_gridSize, 2));

        for (int index = 0; index < _newCards.Count; ++index)
        {
            // ī�� ������Ʈ ��������
            _objectManager.GetChild(0).SetParent(_cardObj);

            _newCards[index].PreviewTime = _maxPreviewTime;

            // ������ ī�� ������Ʈ�� ���� ����
            Card cardScript = _cardObj.GetChild(index).GetComponent<Card>();
            cardScript.SetCardInfo(_newCards[index], _flipCardSize);
            cardScript.SetCardNumberSize(_gridSize);
            _curCards.Add(cardScript);
        }
    }

    void ShowPreview()
    {
        Time.timeScale = 1;
        _previewTime = 0;
        _isPreview = true;

        // Preview ȭ���� ������ ���̴� ȭ�鿡 ������
        _screenManager.CoverScreen("Preview");
    }

    void ShowComboGauge()
    {
        _showComboGauge.fillAmount = _comboPart * _comboStack;
    }

    void StackCombo(bool isCombo)
    {
        _comboStackPoint = _comboPart * _comboStack;
        if (!isCombo)
        {
            // �޺��� �Ƚ��� (Easy, Normal�̸� �޺��� �� ĭ ���̰� Hard�� ���� ����)
            if (_comboStatus != -1)
            {
                // ���� ������ �ƴϾ��ٰ� ������ ��쿡�� _comboStatus�� ��ȭ�ϰ� _startValue�� ��ȭ�Ѵ�.
                _comboStatus = -1;
                _startValue = _comboStackPoint;
            }

            // ���� �������� �����̰� ���絵 ������ ��� _startValue�� ��ȭ�� ����.

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

            _endValue = _comboPart * _comboStack;
        }
        else
        {
            // �޺��� ����

            if (_comboStatus != 1)
            {
                // ���� ������ �ƴϾ��ٰ� ������ ��쿡�� _comboStatus�� ��ȭ�ϰ� _startValue�� ��ȭ�Ѵ�.
                _comboStatus = 1;
                _startValue = _comboStackPoint;
            }

            _comboStack = (_comboStack + 1 > _maxComboStack) ? _comboStack : _comboStack + 1;
            _endValue = _comboPart * _comboStack;

            if (_comboStack == _maxComboStack && _isFever == false)
            {
                // �ǹ� Ÿ��
                StartFever();
            }
        }

        //ShowComboGauge();
    }

    void GameStart()
    {
        // �̸����Ⱑ ���� �� �̸����� ȭ���� ���� ����� ������ �����Ѵ�.
        _gameTime = 0;
        _comboStack = 0;
        _feverTime = 0;
        _isFever = false;
        Time.timeScale = 1;
        _showTime.value = 1;
        _showComboGauge.fillAmount = 0;

        _isGame = true;
    }


    // public �Լ�
    public void PreviewOver()
    {
        if (!_isPreview) return; // �̹� ����� �����̸� �۵����� �ʴ´�.

        // �̸����� ���� �� �ٷ� ���� ����
        _isPreview = false;
        // �̸����� ȭ���� ���� ���߿� �� ȭ���̹Ƿ� ȭ���� �����Ѵ�.
        _screenManager.PrevScreen();
        // ī�带 ���� �����´�. (ī�带 ���� �� ���̵��� �ٲ۴�.)
        foreach (Card curCard in _curCards)
        {
            //curCard.FlipCard(false);
            curCard.PreviewOver();
        }

        GameStart();
    }

    public void SelectDifficulty(int gridSize)
    {
        int cellSize = 0;
        int cellSpacing = 0;
        switch ((DIFFICULTY)gridSize)
        {
            case DIFFICULTY.EASY:
                // 10��
                _maxPreviewTime = 10;
                _showDifficulty.text = "����";
                cellSize = 300;
                cellSpacing = 100;
                break;
            case DIFFICULTY.NORMAL:
                // 20��
                _maxPreviewTime = 20;
                _showDifficulty.text = "����";
                cellSize = 250;
                cellSpacing = 50;
                break;
            case DIFFICULTY.HARD:
                // 30��
                _showDifficulty.text = "�����";
                _maxPreviewTime = 30;
                cellSize = 200;
                cellSpacing = 50;
                break;
        }
        _difficulty = (DIFFICULTY)gridSize;
        _gridSize = gridSize;
        _maxFeverTime = _maxPreviewTime / 5;
        // �׸��� ������ ����
        _flipCardSize = cellSize * 0.8f;
        _cardLayoutGroup.cellSize = new Vector2(_flipCardSize, cellSize);
        _cardLayoutGroup.spacing = new Vector2(cellSpacing, cellSpacing);
    }

    // Difficulty Screen���� ���� ���� ��ư�� ���� �� ȣ��
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

    public void GameClear()
    {
        GameOver(true);

        // ���� ���� ȭ������
        _screenManager.GoScreen("GameOver");

        // �ɸ� �ð� �����ֱ�
        _showGameTimeInfo.text = "�ɸ� �ð�: " + _gameTime.ToString("F2");
    }

    public void GameOver(bool isClear = false)
    {
        _isGame = false;

        foreach (Card curCard in _curCards)
        {
            curCard.Init();
        }

        _showComboGauge.fillAmount = 0;
        _showTime.value = 0;

        // ���� �ʱ�ȭ
        _comboStatus = 0;
        _isPreview = false;
        _previewTime = 0;
        _comboAnimationTime = 0;
        _startValue = 0;
        _endValue = 0;
        _curCards.Clear();

        // ī�� �ݳ�
        ReturnCards();

        // ȭ�� �ʱ�ȭ -> ���� ���� ȭ������
        if (!isClear) _screenManager.ScreenClear();
        //_screenManager.ScreenClear();
    }

    public void ChangeNumber()
    {
        // ���� Ŭ����
        if (_newCards.Count <= 0)
        {
            // ���� �� ī�尡 ������ ī�带 ��� ���Ӱ� �����´�. -> X
            // ���� �� ī�尡 ������ ������ �����Ѵ�.
            GameClear();
            return;
        }

        int findNumberIndex = Random.Range(0, _newCards.Count);
        _findCard = _newCards[findNumberIndex];
        _showFindCardNumber.text = _findCard.Number.ToString();
    }

    public bool CheckNumber(CardInfo cardInfo)
    {
        if (cardInfo.Number == _findCard.Number)
        {
            _newCards.Remove(_findCard);
            ChangeNumber();
            if (!_isFever) StackCombo(true);
            return true;
        }
        else
        {
#if UNITY_EDITOR
            print("�����Դϴ�.");
#endif
            if (!_isFever) StackCombo(false);
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
        Time.timeScale = isPause == true ? 0 : 1;
        _isGame = !isPause;
    }

    /* �ػ� �����ϴ� �Լ� */
    public void SetResolution()
    {
        int setWidth = 1080; // ����� ���� �ʺ�
        int setHeight = 1920; // ����� ���� ����

        int deviceWidth = Screen.width; // ��� �ʺ� ����
        int deviceHeight = Screen.height; // ��� ���� ����

        Screen.SetResolution(setWidth, (int)(((float)deviceHeight / deviceWidth) * setWidth), true); // SetResolution �Լ� ����� ����ϱ�

        if ((float)setWidth / setHeight < (float)deviceWidth / deviceHeight) // ����� �ػ� �� �� ū ���
        {
            float newWidth = ((float)setWidth / setHeight) / ((float)deviceWidth / deviceHeight); // ���ο� �ʺ�
            Camera.main.rect = new Rect((1f - newWidth) / 2f, 0f, newWidth, 1f); // ���ο� Rect ����
        }
        else // ������ �ػ� �� �� ū ���
        {
            float newHeight = ((float)deviceWidth / deviceHeight) / ((float)setWidth / setHeight); // ���ο� ����
            Camera.main.rect = new Rect(0f, (1f - newHeight) / 2f, 1f, newHeight); // ���ο� Rect ����
        }
    }
}

/*
 * Ŀ�� ����
 *  2023-03-05 00:30 -> ���� ���� �̱� �ϼ�
 *  2023-03-05 18:21 -> ���̵� ���� �� ȭ�� ����
 *  2023-03-06 19:34 -> ���� �ð� ���� �� �̸����� ��� ����
 *  2023-03-06 20:43 -> ���� ���� ȭ�� �߰� �� �޺� ��� �߰�
 *  2023-03-07 12:20 -> �ǹ� ��� �߰� (�ǹ� �ð��� �̸����� �ð��� 1/5��)
 *  2023-03-07 15:10 -> ���̵� ���� ��� ����
 *  2023-03-07 22:56 -> UI ��߳� �� ����
 *  2023-03-15 15:01 ->
     *  �� ������ 0.8�� ����
     *  ī�� ������ ���� �� ũ�� ����
     *  ȭ�� ���� ���ߴ� �ڵ� �߰�
     *  ī�� �ִϸ��̼� �߰�
 *  2023-03-15 16:49 -> �ǹ� �ִϸ��̼� ����, ���� ī�� �̹��� �߰�
 *  2023-03-16 18:16 -> �ð� ������ �� �ִϸ��̼� �߰�, �ð� ���� �߰�
 *  2023-03-20 16:54 -> �ΰ��� �ð�, �޺� �̹���, �ִϸ��̼� �߰�, ���� ���� ����(���̵� ��ư ����, �̸����� �ð� ����, ī�� �ִϸ��̼� ����, �޺� ���� ��)

 * ���� ����
 * 

 * TODO
 *  �ٹ̱�
 *  ���̵� ���� �κ� �ٹ̱�
*/


/*
 * ���� ���
    * 2023-03-20
        * ���� ���� ���� �� �̸������� �� ī�尡 ������ �Ѿ�� Fever���·� ���Ե�      O
        * ���� ���۰� ���ÿ� ī�带 ��ġ�ϸ� �ִϸ��̼��� ������� ����                  O
*/


/*
 * ���� ���� ���
 * 2023-03-20 15:37
    * �ִϸ��̼� ���� �� �̹����� ���� ���װ� ���� ��� �ִϸ��̼��� �����ϴ� ������Ʈ�� SpriteRenderer�� �����ϴ� ���� �ƴ϶�
    * �ִϸ��̼��� ������ �ߴ��ϸ� �ذ�ȴ�.
*/