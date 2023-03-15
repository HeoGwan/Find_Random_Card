using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardInfo
{
    public int Number { get; set; }
    public float PreviewTime { get; set; }
    public bool isCorrect { get; set; } = false;
}

public class Card : MonoBehaviour
{
    [SerializeField] private Sprite _cardBack;
    [SerializeField] private Sprite _cardFront;
    [SerializeField] private Sprite _cardFrontCorrect;

    [Space(10)]
    [SerializeField] private float _showAnimation = 0.7f;
    [SerializeField] private float _speedTimes = 1;

    [Space(10)]
    [SerializeField] private Animator _flipAnim;

    private CardInfo _cardInfo;
    private Button _button;
    private Image _buttonImage;
    private SpriteRenderer _animationImage;
    private TextMeshProUGUI _showNumberText;
    private bool _isShow = false;

    private void Awake()
    {
        _cardInfo = new CardInfo();
        _cardInfo.isCorrect = false;
        _button = GetComponent<Button>();
        _buttonImage = GetComponent<Image>();
        _showNumberText = GetComponentInChildren<TextMeshProUGUI>();
        _animationImage = GetComponentInChildren<SpriteRenderer>();
    }


    void PlayAnimation(string animationName, bool isPlay = true)
    {
        _buttonImage.color = new Color(0, 0, 0, 0);
        _showNumberText.text = "";
        _flipAnim.SetBool(animationName, isPlay);
        StartCoroutine(Stop(animationName, 0.7f / _speedTimes));
    }

    IEnumerator Stop(string animationName, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        _flipAnim.SetBool(animationName, false);
        if (animationName == "Card_Flip")
        {
            _showNumberText.text = _cardInfo.Number.ToString();
            _buttonImage.sprite = _cardInfo.isCorrect == true ? _cardFrontCorrect : _cardFront;
        }
        else if (animationName == "Card_Flip_Reverse")
        {
            _showNumberText.text = "";
            _buttonImage.sprite = _cardBack;

        }
        _buttonImage.color = new Color(255, 255, 255, 255);
    }

    public void SetCardNumberSize(int gridSize)
    {
        // grid -> text size
        // 3 -> 130
        // 4 -> 105
        // 5 -> 80
        switch (gridSize)
        {
            case 3:
                _showNumberText.fontSize = 100;
                break;
            case 4:
                _showNumberText.fontSize = 80;
                break;
            case 5:
                _showNumberText.fontSize = 60;
                break;
        }
    }

    public void SetCardInfo(CardInfo cardInfo, float flipCardSize)
    {
        _buttonImage.sprite = _cardFront;
        _cardInfo.Number = cardInfo.Number;
        _cardInfo.PreviewTime = cardInfo.PreviewTime;
        _cardInfo.isCorrect = false;
        _showNumberText.text = cardInfo.Number.ToString();
        _flipAnim.transform.localScale = new Vector2(flipCardSize, flipCardSize);

        _button.onClick.AddListener(() =>
        {
            // �̹� ������ ī�尡 �ƴ϶�� ī�� Ŭ�� �� ī�带 �����ְ� ��� �� ������� �Ѵ�.
            if (!_isShow && _cardInfo.isCorrect == false && !GameManager._instance.IsFever)
            {
                StartCoroutine(ShowNumber());
            }

            if (GameManager._instance.CheckNumber(_cardInfo))
            {
                // ����
                _cardInfo.isCorrect = true;
                _buttonImage.sprite = _cardFrontCorrect;
            }
        });
    }

    public void PreviewOver()
    {
        _showNumberText.text = "";
        PlayAnimation("Card_Flip_Reverse");
    }

    public void Init()
    {
        _animationImage.sprite = null;
        _cardInfo.isCorrect = false;
        _isShow = false;
        _buttonImage.sprite = _cardFront;
        _buttonImage.color = new Color(255, 255, 255, 255);
        _button.onClick.RemoveAllListeners();
        StopAllCoroutines();
    }

    public void FlipCard(bool isShowNumber)
    {
        if (_cardInfo.isCorrect) return;

        // ī�� �޸� -> �ո��� �⺻ �ִϸ��̼�
        // ī�� �ո� -> �޸��� �� �ִϸ��̼�
        string animationName = isShowNumber == true ? "Card_Flip" : "Card_Flip_Reverse";

        PlayAnimation(animationName);
    }

    IEnumerator ShowNumber()
    {
        // ī�� �޸� -> �ո�
        PlayAnimation("Card_Flip");
        _isShow = true;

        yield return new WaitForSeconds(_showAnimation / _speedTimes);

        if (_cardInfo.isCorrect == false)
        {
            // ī�� �ո� -> �޸�
            PlayAnimation("Card_Flip_Reverse");
        }
        _isShow = false;
    }
}
