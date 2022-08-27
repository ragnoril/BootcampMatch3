using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public BoardManager Board;

    public Text ScoreText;
    public Text MoveText;

    public int MoveCounter;
    public int Score;

    // Start is called before the first frame update
    void Start()
    {
        MoveCounter = 0;
        Score = 0;

        Board.InitBoard();

        Board.OnBoardMove += BoardMoved;
        Board.OnTilesPopped += ScoreUpdate;
    }

    private void OnDestroy()
    {
        Board.OnBoardMove -= BoardMoved;
        Board.OnTilesPopped -= ScoreUpdate;
    }

    private void ScoreUpdate(int val)
    {
        Score += (val * 150);
        ScoreText.text = "Score: " + Score;
    }

    private void BoardMoved()
    {
        MoveCounter += 1;
        MoveText.text = "Move: " + MoveCounter;
    }
}
