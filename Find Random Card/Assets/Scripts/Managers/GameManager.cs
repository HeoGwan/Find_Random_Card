using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TMPro.Examples;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using static UnityEditor.Experimental.GraphView.GraphView;

// Grid Size�� ���� DIFFICULTY�� �޶���
public enum DIFFICULTY
{
    EASY = 3,
    NORMAL = 4,
    HARD = 5,
}

public enum GAME_TYPE
{
    RANDOM,
    ORDER,
}

// ���� ��� Ŭ����
public abstract class GameMode
{
    protected GameManager gameManager;

    protected GameMode()
    {
        gameManager = GameManager._instance;
    }

    public abstract void ShuffleCards();
    public abstract void BringCards();
    public abstract void ChangeNumber();
}

public class RandomMode : GameMode
{
    public RandomMode() : base() { }

    public override void ShuffleCards()
    {
        if (gameManager.CardObj.childCount != 0) gameManager.ReturnCards();

        // ���� ����
        int random1, random2;
        CardInfo temp;

        for (int index = 0; index < gameManager.Cards.Count; ++index)
        {
            random1 = Random.Range(0, gameManager.Cards.Count);
            random2 = Random.Range(0, gameManager.Cards.Count);

            temp = gameManager.Cards[random1];
            gameManager.Cards[random1] = gameManager.Cards[random2];
            gameManager.Cards[random2] = temp;
        }
    }

    public override void BringCards()
    {
        // �������� ���� ī�� ���� �� pow(n, 2)�� ��ŭ ��������
        gameManager.NewCards = gameManager.Cards.GetRange(0, gameManager.CardCount);

        for (int index = 0; index < gameManager.NewCards.Count; ++index)
        {
            // ī�� ������Ʈ ��������
            gameManager._objectManager.GetChild(0).SetParent(gameManager.CardObj);

            // ������ ī�� ������Ʈ�� ���� ����
            Card cardScript = gameManager.CardObj.GetChild(index).GetComponent<Card>();

            cardScript.SetCardInfo(gameManager.NewCards[index], gameManager.FlipCardSize);

            cardScript.SetCardNumberSize(gameManager.GridSize);
            gameManager.CurCards.Add(cardScript);
        }
    }

    public override void ChangeNumber()
    {
        gameManager.NewCards.Remove(gameManager.FindCard);

        // ���� Ŭ����
        if (gameManager.NewCards.Count <= 0)
        {
            // ���� �� ī�尡 ������ ������ �����Ѵ�.
            gameManager.GameClear();
            return;
        }

        int findNumberIndex = Random.Range(0, gameManager.NewCards.Count);
        gameManager.FindCard = gameManager.NewCards[findNumberIndex];
        gameManager.ShowFindCardNumber.text = gameManager.FindCard.Number.ToString();
    }
}

public class OrderMode : GameMode
{
    private Queue<CardInfo> _findCardQueue = new Queue<CardInfo>();

    public OrderMode() : base() { }

    public override void ShuffleCards()
    {
        if (gameManager.CardObj.childCount != 0) gameManager.ReturnCards();

        gameManager.OrderCards = new List<CardInfo>(new CardInfo[gameManager.CardCount]);

        for (int index = 0; index < gameManager.OrderCards.Count; ++index)
        {
            CardInfo card = new CardInfo();
            card.Number = index + 1;

            gameManager.OrderCards[index] = card;
            _findCardQueue.Enqueue(card);
        }

        // ���� ����
        int random1, random2;
        CardInfo temp;

        for (int index = 0; index < gameManager.OrderCards.Count; ++index)
        {
            random1 = Random.Range(0, gameManager.OrderCards.Count);
            random2 = Random.Range(0, gameManager.OrderCards.Count);

            temp = gameManager.OrderCards[random1];
            gameManager.OrderCards[random1] = gameManager.OrderCards[random2];
            gameManager.OrderCards[random2] = temp;
        }
    }

    public override void BringCards()
    {
        // �������� ���� ī�� ���� �� pow(n, 2)�� ��ŭ ��������
        for (int index = 0; index < gameManager.OrderCards.Count; ++index)
        {
            // ī�� ������Ʈ ��������
            gameManager._objectManager.GetChild(0).SetParent(gameManager.CardObj);

            // ������ ī�� ������Ʈ�� ���� ����
            Card cardScript = gameManager.CardObj.GetChild(index).GetComponent<Card>();

            cardScript.SetCardInfo(gameManager.OrderCards[index], gameManager.FlipCardSize);

            cardScript.SetCardNumberSize(gameManager.GridSize);
            gameManager.CurCards.Add(cardScript);
        }
    }

    public override void ChangeNumber()
    {
        gameManager.OrderCards.Remove(gameManager.FindCard);

        // ���� Ŭ����
        if (gameManager.OrderCards.Count <= 0)
        {
            // ���� �� ī�尡 ������ ������ �����Ѵ�.
            gameManager.GameClear();
            return;
        }

        gameManager.FindCard = _findCardQueue.Dequeue();
        gameManager.ShowFindCardNumber.text = gameManager.FindCard.Number.ToString();
    }
}


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

    private string                              _path;

    // ���̵� ���� ����
    private DIFFICULTY                          _difficulty;
    private string[]                            _difficultyTypes = { "����", "����", "�����" };
    private float[]                             _difficultyTimes = { 60f, 75f, 90f };

    // ��ŷ ���� ����
    private GAME_TYPE                           _rankingGameType = GAME_TYPE.RANDOM;
    private string                              _rankingDifficulty = "easy";
    private Color                               _normalColor = new Color(1, 1, 1);
    private Color                               _selectColor = new Color(160 / 255, 160 / 255, 160 / 255);

    // ���� ���� ����
    // ���� 
    [SerializeField] private int                _gameModeCount;
    private GAME_TYPE                           _gameType;
    private GameMode                            _gameMode;
    private GameMode[]                          _gameModes;

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
    public GAME_TYPE                            GameType { get { return _gameType; } }
    public bool                                 IsFever { get { return _isFever; } }
    public float                                GameTime { get { return _gameTime; } }
    public int                                  GridSize { get { return _gridSize; } set { _gridSize = value; } }
    public int                                  CardCount { get { return _cardCount; } }
    public float                                FlipCardSize { get { return _flipCardSize; } }

    // ���� ��� ���� Getter, Setter
    public Transform                            CardObj { get { return _cardObj; } }
    public CardInfo                             FindCard { get { return _findCard; } set { _findCard = value; } }
    public List<Card>                           CurCards { get { return _curCards; } }
    public TextMeshProUGUI                      ShowFindCardNumber { get { return _showFindCardNumber; } }

    // ������ ī�� ���߱� ���
    public List<CardInfo>                       Cards { get { return _cards; } set { _cards = value; } }
    public List<CardInfo>                       NewCards { get { return _newCards; } set { _newCards = value; } }

    // ������� ī�� ���߱� ���
    public List<CardInfo>                       OrderCards { get { return _orderCards; } set { _orderCards = value; } }


    // ī�� ���� ����
    // ������ ī�� ���߱� ���
    [SerializeField] private int                _maxRandomCardCount;
    private List<CardInfo>                      _cards;
    private List<CardInfo>                      _newCards;

    private CardInfo                            _findCard;
    private List<Card>                          _curCards;

    private GridLayoutGroup                     _cardLayoutGroup;
    private int                                 _gridSize;
    private int                                 _cardCount;
    private float                               _flipCardSize;

    // ������� ī�� ���߱� ���
    private List<CardInfo>                      _orderCards;


    // Show In Inspector
    [Header("�� Objects")]
    [SerializeField] private Transform          _cardObj;
    [SerializeField] private Slider             _showTime;
    [SerializeField] private Image              _showTimeImage;
    [SerializeField] private Image              _showComboGauge;
    [SerializeField] private TMP_InputField     _nickname;
    [Header("�� Ranking Buttons")]
    [SerializeField] private Toggle[]             _rankingModeButtons;
    [SerializeField] private Toggle[]             _rankingDifficultyButtons;

    [Space(10)]
    [Header("�� Text Objects")]
    [SerializeField] private TextMeshProUGUI    _showFindCardNumber;
    [SerializeField] private TextMeshProUGUI    _showDifficulty;
    [SerializeField] private TextMeshProUGUI    _showRemainPreviewTime;
    [SerializeField] private TextMeshProUGUI    _showGameTimeInfo;
    [SerializeField] private TextMeshProUGUI    _showGameoverDifficulty;

    [Space(10)]
    [Header("�� Managers")]
    public Transform                            _objectManager;
    public ScreenManager                        _screenManager;
    public SoundManager                         _soundManager;
    public SettingManager                       _settingManager;
    public DatabaseManager                      _databaseManager;
    public PrefabManager                        _prefabManager;
    public AdMobManager                         _adMobManager;
    

    // �����ֱ� �Լ�
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

        _path = Path.Combine(Application.persistentDataPath , "setting.json");

        InitMode();
        SelectMode(GAME_TYPE.RANDOM);

        _cards = new List<CardInfo>(new CardInfo[_maxRandomCardCount]);

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

        _databaseManager.SetDatabase(_rankingGameType, _rankingDifficulty);
    }

    private void Start()
    {
        SetResolution();
        JsonLoad();
        _adMobManager.LoadAd();
        _adMobManager.HideAd();
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

    private void OnDestroy()
    {
        _adMobManager.DestroyAd();
        JsonSave();
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
        _showTimeImage.fillAmount = _showTime.value;

        if (_gameTime >= _maxGameTime)
        {
            // ���� ��
            GameOver();
        }
    }

    // User Function
    void JsonLoad()
    {
        AudioData saveSetting = new AudioData();

        if (!File.Exists(_path))
        {
            JsonSave();
        }
        else
        {
            string loadJson = File.ReadAllText(_path);
            saveSetting = JsonUtility.FromJson<AudioData>(loadJson);

            _soundManager.AudioSetting(saveSetting);
        }
    }

    public void JsonSave()
    {
        AudioData saveSetting = new AudioData();

        saveSetting.BGMVolume = _soundManager.BGMVolume;
        saveSetting.EffectVolume = _soundManager.EffectVolume;

        string json = JsonUtility.ToJson(saveSetting, true);

        File.WriteAllText(_path, json);
    }

    void InitMode()
    {
        _gameModes = new GameMode[_gameModeCount];

        _gameModes[0] = new RandomMode();
        _gameModes[1] = new OrderMode();
    }

    void SelectMode(GAME_TYPE gameType)
    {
        _gameType = gameType;
        _gameMode = _gameModes[(int)gameType];
    }

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

    //void ShuffleCards()
    //{
    //    if (_cardObj.childCount != 0) ReturnCards();

    //    // ���� ����
    //    int random1, random2;
    //    CardInfo temp;

    //    for (int index = 0; index < _cards.Count; ++index)
    //    {
    //        random1 = Random.Range(0, _cards.Count);
    //        random2 = Random.Range(0, _cards.Count);

    //        temp = _cards[random1];
    //        _cards[random1] = _cards[random2];
    //        _cards[random2] = temp;
    //    }
    //}

    //void BringCards()
    //{
    //    // �������� ���� ī�� ���� �� pow(n, 2)�� ��ŭ ��������
    //    _newCards = _cards.GetRange(0, (int)Mathf.Pow(_gridSize, 2));

    //    for (int index = 0; index < _newCards.Count; ++index)
    //    {
    //        // ī�� ������Ʈ ��������
    //        _objectManager.GetChild(0).SetParent(_cardObj);

    //        // ������ ī�� ������Ʈ�� ���� ����
    //        Card cardScript = _cardObj.GetChild(index).GetComponent<Card>();

    //        cardScript.SetCardInfo(_newCards[index], _flipCardSize);

    //        cardScript.SetCardNumberSize(_gridSize);
    //        _curCards.Add(cardScript);
    //    }
    //}

    void ShowPreview()
    {
        Time.timeScale = 1;
        _previewTime = 0;
        _isPreview = true;

        // Preview ȭ���� ������ ���̴� ȭ�鿡 ������
        _screenManager.CoverScreen("Preview");
    }

    void StackCombo(bool isCombo)
    {
        if (_gameType == GAME_TYPE.ORDER) return;

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
        _showTimeImage.fillAmount = _showTime.value;
        _showComboGauge.fillAmount = 0;

        _isGame = true;
    }

    void Incorrect()
    {
        // ������ ��� ����, ���� ���̵��� 1��
        // ����� ���̵��� 2�ʰ� ���̸� ���� ȿ������ �鸰��.
        switch (_difficulty)
        {
            case DIFFICULTY.EASY:
            case DIFFICULTY.NORMAL:
                _gameTime += 1f;
                break;
            case DIFFICULTY.HARD:
                _gameTime += 2f;
                break;
        }
        _soundManager.PlayEffectSound("Incorrect");
    }


    // public �Լ�
    public void ReturnCards()
    {
        // ī�� �ݳ�
        int cardObjChild = _cardObj.childCount;

        for (int index = 0; index < cardObjChild; ++index)
        {
            _cardObj.GetChild(0).SetParent(_objectManager);
        }
    }

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
        _cardCount = (int)Mathf.Pow(GridSize, 2);
        _maxGameTime = _difficultyTimes[gridSize - 3];
        _maxFeverTime = _maxPreviewTime / 5;

        // �׸��� ������ ����
        _flipCardSize = cellSize * 0.8f;
        _cardLayoutGroup.cellSize = new Vector2(_flipCardSize, cellSize);
        _cardLayoutGroup.spacing = new Vector2(cellSpacing, cellSpacing);
        
        // ������ ��������
        _databaseManager.SetDatabase(_gameType, _difficulty);
        
        // ���̵� �����ֱ�
        _showGameoverDifficulty.text = "���̵�: " + _difficultyTypes[(int)_difficulty - 3];
    }

    // Difficulty Screen���� ���� ���� ��ư�� ���� �� ȣ��
    public void GameReady()
    {
        _screenManager.GoScreen("Game");
        _soundManager.PlayEffectSound("CardButton");

        // ���� �����ֱ�
        _adMobManager.ShowAd();

        // ī�� ��ġ, �̸����� �����ֱ�

        /* ī�� ��ġ ���� */
        /*
            * ���� ���� �� ī�� ������ ����Ǿ� �ִ� ����Ʈ �� 25(pow(n))���� ��� �����´�.
        */
        // ���� ��忡 �°� ī�带 ���� ī�带 �����´�.
        // ī�� ����
        _gameMode.ShuffleCards();

        // ī�� ��������
        _gameMode.BringCards();

        // ã�ƾ� �ϴ� ī�� �̱�
        _gameMode.ChangeNumber();
        /* ī�� ��ġ ���� */

        // �̸����� ȭ�� �����ֱ�
        ShowPreview();
    }

    public void GoMain()
    {
        _soundManager.PlayEffectSound("CardButton");
        GameOver(false);
    }

    public void GameClear()
    {
        GameOver(true);

        // ������ ��������
        _databaseManager.GetDatas(false);

        // ���� ���� ȭ������
        _screenManager.GoScreen("GameOver");

        // �ɸ� �ð� �����ֱ�
        _showGameTimeInfo.text = "�ɸ� �ð�: " + _gameTime.ToString("F3");
    }

    public void GameOver(bool isClear = false)
    {
        _isGame = false;

        // ���� �����
        _adMobManager.HideAd();

        foreach (Card curCard in _curCards)
        {
            curCard.Init();
        }

        _showComboGauge.fillAmount = 0;
        _showTime.value = 0;
        _showTimeImage.fillAmount = _showTime.value;

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

        if (_gameType == GAME_TYPE.ORDER)
        {
            _orderCards = null;
        }

        // ȭ�� �ʱ�ȭ -> ���� ���� ȭ������
        if (!isClear) _screenManager.ScreenClear();
    }

    //public void ChangeNumber()
    //{
    //    // ���� Ŭ����
    //    if (_newCards.Count <= 0)
    //    {
    //        // ���� �� ī�尡 ������ ������ �����Ѵ�.
    //        GameClear();
    //        return;
    //    }

    //    int findNumberIndex = Random.Range(0, _newCards.Count);
    //    _findCard = _newCards[findNumberIndex];
    //    _showFindCardNumber.text = _findCard.Number.ToString();
    //}

    public bool CheckNumber(CardInfo cardInfo)
    {
        if (cardInfo.Number == _findCard.Number)
        {
            _gameMode.ChangeNumber();
            if (!_isFever) StackCombo(true);
            return true;
        }
        else
        {
#if UNITY_EDITOR
            print("�����Դϴ�.");
#endif
            Incorrect();
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

    /* ������ ���� �Լ� */
    public void SaveScore()
    {
        string nickname = _nickname.text;

        if (nickname == "" || nickname.Length > 7)
        {
            return;
        }

        // ����??��? ???? ��?��?
        if (_databaseManager.WriteData(nickname, _gameTime))
        {
#if UNITY_EDITOR
            print("������ ���� ����");
#endif
        }
        else
        {
#if UNITY_EDITOR
            print("������ ���� ����");
#endif
        }
        _nickname.text = "";
        _screenManager.PrevScreen();

        _databaseManager.GetDatas(false);
    }

    public void ShowRanking()
    {
        _screenManager.GoScreen("Ranking");
        _rankingModeButtons[0].Select();
        _rankingDifficultyButtons[0].Select();

        _databaseManager.GetDatas(true);
        //_databaseManager.GetDatas("easy");
    }

    public void CloseRanking()
    {
        _screenManager.PrevScreen();
        _databaseManager.PutBackScores();
    }

    public void SaveCancel()
    {
        _nickname.text = "";
        _screenManager.PrevScreen();
    }

    public void CheckNickname()
    {
        if (_nickname.text.Length >= 7)
        {
            _nickname.text = _nickname.text.Substring(0, 7);
        }
    }

    public void GoSelectDifficulty()
    {
        //SelectDifficulty(3);
        _screenManager.GoScreen("Choice");
        _soundManager.PlayEffectSound("CardButton");
    }

    public void ChoiceGame(int gameType)
    {
        /*
         * 0: ������ ī�� ã��
         * 1: ������� ã��
        */
        SelectMode((GAME_TYPE)gameType);

        _screenManager.GoScreen("Difficulty");
        _soundManager.PlayEffectSound("ButtonPopSound");
    }


    /* ��ŷ ���� �Լ� */
    public void RandomRanking(bool isOn)
    {
        if (isOn)
        {
            _rankingGameType = GAME_TYPE.RANDOM;

            _databaseManager.SetDatabase(_rankingGameType, _rankingDifficulty);
            _databaseManager.GetDatas(true);
        }
    }

    public void OrderRanking(bool isOn)
    {
        if (isOn)
        {
            _rankingGameType = GAME_TYPE.ORDER;

            _databaseManager.SetDatabase(_rankingGameType, _rankingDifficulty);
            _databaseManager.GetDatas(true);
        }
    }

    public void EasyRanking(bool isOn)
    {
        if (isOn)
        {
            _rankingDifficulty = "easy";

            _databaseManager.SetDatabase(_rankingGameType, _rankingDifficulty);
            _databaseManager.GetDatas(true);
        }
    }

    public void NormalRanking(bool isOn)
    {
        if (isOn)
        {
            _rankingDifficulty = "normal";

            _databaseManager.SetDatabase(_rankingGameType, _rankingDifficulty);
            _databaseManager.GetDatas(true);
        }
    }

    public void HardRanking(bool isOn)
    {
        if (isOn)
        {
            _rankingDifficulty = "hard";

            _databaseManager.SetDatabase(_rankingGameType, _rankingDifficulty);
            _databaseManager.GetDatas(true);
        }
    }


    //public void GameModeRanking(int gameType)
    //{
    //    /*
    //     * 0: ������ ī�� ���߱� ���
    //     * 1: ������� ī�� ���߱� ���
    //    */
    //    _rankingGameType = (GAME_TYPE)gameType;
    //    _databaseManager.SetDatabase(_rankingGameType, _rankingDifficulty);
    //    _databaseManager.GetDatas(true);
    //}

    //public void GetRanking(string difficulty)
    //{
    //    _rankingDifficulty = difficulty;

    //    _databaseManager.SetDatabase(_rankingGameType, _rankingDifficulty);
    //    _databaseManager.GetDatas(true);
    //}
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
 *  2023-03-25 23:14 -> �̹���, ��ƼŬ, ����, ȭ��, DB���� �߰�
    * ���ȭ�� �߰�
    * ������ �� ��ƼŬ ���̰� ��ƼŬ �߰�
    * ī�� �̹��� �� �ִϸ��̼� ����
    * �����, ȿ���� �߰�
    * ���� ȭ�� �߰�
    * ���� ���� ȭ�� ����
    * DB����
 * 2023-03-25 23:34 -> ���� �߰�
 * 2023-03-27 14:18 -> ��ŷ ȭ�� �߰�
 * 2023-03-27 16:41 -> ���̵��� �ð� ����, ���� ��� �߰�, ȿ���� �߰�, ���� �ִϸ��̼� �߰�
    * ���̵����� ���� �ð� �ٸ��� ����
    * ���� ȿ���� �߰�
    * ���� �� ���� �ð� ���� �� ȿ���� ��� ��� �߰�
    * ī�� ���� �ִϸ��̼� �߰�
 * 2023-03-27 20:05 -> Ʃ�丮�� ȭ�� ����
 * 2023-03-27 20:33 -> ���� ���� ��� �߰�
 * 2023-03-27 21:36 -> ���� ȭ�� ��� ����

 * 2023-03-28 17:14 -> ���� �ذ�

 * ���� ����

 * TODO
    * �� ����

 * ���� ���� �۾�
    * ���� ���߱� ��� �۾�
        * ī�� ����(O) -> ���� ��� �ٸ���(O) -> ����, �ǹ� ����(O) -> ������ ����(O) -> �ٹ̱� (O)
        * ��ŷ���� ���� ��� ���� �����ϰ� ���̵� ������ �����ؾ� �Ѵ�. (O)
        * ���� ��� ���� �� ��ư�� �ٸ�� �Ѵ�. (O)
*/


/*
 * ���� ���
    * 2023-03-20
        * ���� ���� ���� �� �̸������� �� ī�尡 ������ �Ѿ�� Fever���·� ���Ե�      O
        * ���� ���۰� ���ÿ� ī�带 ��ġ�ϸ� �ִϸ��̼��� ������� ����                  O
    * 2023-03-27
        * ����Ϸ� ���� ��
            * ������ ���� ������ �ȵ� -> Path.Combine(Application.dataPath + "/Resources/" + "/Data", "setting.json")�� �ƴ϶�
                * Path.Combine(Application.persistentDataPath, "setting.json") ���� �ϸ� �ذ� ��
            * ���� ���� �� ����, ī��, �̸����Ⱑ �ƿ� �ȶ߸� ���� ������ �ȵ� -> adMob ����
            * ���� Ŭ���� �Լ�(GameClear)�� ����� �۵����� ���� -> Ŭ��� �ȵǰ� ��� ������ �� -> adMob ����
            * �Ͻ����� �޴����� �������� ���ư��Ⱑ �ȵ� -> adMob ����
    * 2023-03-28
        * ������ ����� �� �ٽ� �����ϸ� ���̵��� �ٽ� �������� ���ư�
*/


/*
 * ���� ���� ���
 * 2023-03-20 15:37
    * �ִϸ��̼� ���� �� �̹����� ���� ���װ� ���� ��� �ִϸ��̼��� �����ϴ� ������Ʈ�� SpriteRenderer�� �����ϴ� ���� �ƴ϶�
    * �ִϸ��̼��� ������ �ߴ��ϸ� �ذ�ȴ�.
*/