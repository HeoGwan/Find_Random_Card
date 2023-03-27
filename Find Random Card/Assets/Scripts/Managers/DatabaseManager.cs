using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Runtime.CompilerServices;
using System;
using Unity.VisualScripting;

// �����ϴ� ������ ����ü
public struct SaveData
{
    public SaveData(string nickname, string elapsedTime)
    {
        Nickname = nickname;
        ElapsedTime = elapsedTime;
    }

    public string Nickname { get; set; }
    public string ElapsedTime { get; set; }
}

public class DatabaseManager : MonoBehaviour
{
    private string[]                            _databaseTypes = { "easy", "normal", "hard" };
    private string                              _databaseType;

    private Transform                           _showScores;
    [SerializeField] private Transform          _gameOverScores;
    [SerializeField] private Transform          _rankingScores;

    private DatabaseReference                   _db;
    private Queue<SaveData>                     _scores;
    private bool                                _isShowScore = false;
    private bool                                _reShowScore = false;

    private void Awake()
    {
        _db = FirebaseDatabase.DefaultInstance.RootReference;
        _scores = new Queue<SaveData>();
    }

    private void LateUpdate()
    {
        if (_isShowScore) { ShowScores(); }
    }

    /// <summary>
    /// Firebase���� ������ Ÿ�Կ� �´� �����͸� �����´�.
    /// </summary>
    public void GetDatas(bool reLoad = false)
    {
        // �����͸� �������� �ڵ�
        _db.Child(_databaseType).OrderByChild("elapsed_time").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
#if UNITY_EDITOR
                Debug.LogError("Database Error");
#endif
                return;
            }

            if(!task.IsCompleted)
            {
#if UNITY_EDITOR
                Debug.LogError("Fail Get");
#endif
                return;
            }

            DataSnapshot snapshot = task.Result;

            foreach(DataSnapshot child in snapshot.Children)
            {
                IDictionary data = (IDictionary)child.Value;

                _scores.Enqueue(new SaveData(data["nickname"].ToString(), data["elapsed_time"].ToString()));
            }

            _showScores = _gameOverScores;
            _isShowScore = true;
        });
    }
    
    public void GetDatas(string dataType)
    {
        if (dataType == "" || dataType == null) return;

        // �����͸� �������� �ڵ�
        _db.Child(dataType).OrderByChild("elapsed_time").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
#if UNITY_EDITOR
                Debug.LogError("Database Error");
#endif
                return;
            }

            if(!task.IsCompleted)
            {
#if UNITY_EDITOR
                Debug.LogError("Fail Get");
#endif
                return;
            }

            DataSnapshot snapshot = task.Result;

            foreach(DataSnapshot child in snapshot.Children)
            {
                IDictionary data = (IDictionary)child.Value;

                _scores.Enqueue(new SaveData(data["nickname"].ToString(), data["elapsed_time"].ToString()));
            }

            _showScores = _rankingScores;
            _isShowScore = true;
        });
    }

    public bool WriteData(string nickname, float elapsedTime)
    {
        try
        {
            string time = string.Format("{0:0.###}", elapsedTime);

            // �����͸� �����ϴ� �ڵ�
            DatabaseReference data = _db.Child(_databaseType).Push();

            data.Child("nickname").SetValueAsync(nickname);
            data.Child("elapsed_time").SetValueAsync(time);

            return true;
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogException(e);
#endif
            return false;
        }
    }

    public void ShowScores()
    {
        _isShowScore = false;

        if (_showScores.childCount > 0) PutBackScores();

        while (_scores.Count > 0)
        {
            // objectManager���� ���ھ� �������� ������ �� �����͸� ����ִ´�.
            GameObject scoreObj = GameManager._instance._prefabManager.GetScoreObj();
            SaveData score = _scores.Dequeue();

            // ������ ����ֱ�
            scoreObj.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = score.Nickname;
            scoreObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = score.ElapsedTime;

            scoreObj.transform.SetParent(_showScores);
            scoreObj.transform.localScale = Vector3.one;
            scoreObj.transform.position = Vector3.zero;
        }
    }

    //public void ReShowScores()
    //{
    //    _reShowScore = false;
    //    PutBackScores();
    //    ShowScores();
    //}

    public void SetDatabase(DIFFICULTY difficulty)
    {
        _databaseType = _databaseTypes[(int)difficulty - 3];
        //GetDatas();
    }

    public void SaveCancel()
    {
        PutBackScores();
        GameManager._instance._screenManager.PrevScreen();
    }

    public void GoMain()
    {
        PutBackScores();
        GameManager._instance._screenManager.ScreenClear();
    }

    public void PutBackScores()
    {
        int childCount = _showScores.childCount;

        for (int index = 0; index < childCount; ++index)
        {
            GameManager._instance._prefabManager.PutBackScoreObj(_showScores.GetChild(0).gameObject);
        }
    }
}
