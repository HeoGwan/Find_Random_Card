using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CardInfo
{
    public int Number { get; set; }
    public float PreviewTime { get; set; }
}

public class Card : MonoBehaviour
{
    private CardInfo _cardInfo;
    private Button button;
    private Image buttonImage;
    private TextMeshProUGUI showNumberText;
    private bool _isShow = false;

    private void Awake()
    {
        _cardInfo = new CardInfo();
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        showNumberText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetCardInfo(CardInfo cardInfo)
    {
        _cardInfo.Number = cardInfo.Number;
        _cardInfo.PreviewTime = cardInfo.PreviewTime;
        showNumberText.text = cardInfo.Number.ToString();

        button.onClick.AddListener(() =>
        {
            // �̹� ������ ī�尡 �ƴ϶�� ī�� Ŭ�� �� ī�带 �����ְ� ��� �� ������� �Ѵ�.
            if (!_isShow && _cardInfo.Number != -1 && !GameManager.instance._IsFever)
            {
                StartCoroutine(ShowNumber());
            }

            if (GameManager.instance.CheckNumber(_cardInfo))
            {
                // ����
                _cardInfo.Number = -1;
                buttonImage.color = new Color(150, 150, 150);
            }
        });
    }

    public void FlipCard(bool isShowNumber)
    {
        // �ӽ÷� �ؽ�Ʈ�� �Ⱥ��̵��� ����
        if (isShowNumber && _cardInfo.Number != -1) { showNumberText.text = _cardInfo.Number.ToString(); }
        else { showNumberText.text = ""; }
    }

    IEnumerator ShowNumber()
    {
        showNumberText.text = _cardInfo.Number.ToString();
        _isShow = true;
        yield return new WaitForSeconds(0.5f);
        if (_cardInfo.Number != -1) showNumberText.text = "";
        _isShow = false;
    }
}
