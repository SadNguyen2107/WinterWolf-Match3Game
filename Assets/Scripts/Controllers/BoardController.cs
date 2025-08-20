using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;

    private bool m_gameOver;

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        Fill();
    }

    private void Fill()
    {
        m_board.Fill();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Cell cell = hit.collider.GetComponent<Cell>();
                if (cell != null && cell.Item != null && cell.IsNormalCell())
                {
                    bool moved = m_board.TryMoveToBottom(cell);
                    if (moved)
                    {
                        IsBusy = false;
                        return;
                    }
                }
                m_hitCollider = hit.collider;
            }
        }
    }

    public void AutoloseButtonClicked()
    {
        if (!isAutoplaying)
        {
            StartCoroutine(AutoloseCoroutine());
        }
    }

    private IEnumerator AutoloseCoroutine()
    {
        isAutoplaying = true;
        while (!m_board.IsWin())
        {
            Cell cellToMove = m_board.GetFirstMovableCell();
            if (cellToMove == null) break;
            m_board.TryMoveToBottom(cellToMove);
            yield return new WaitForSeconds(0.5f);
            // Stop if bottom board is full (lose condition)
            if (m_board.GetBottomGroupsWithCount(1).Count == 0 && m_board.GetBottomGroupsWithCount(2).Count == 0)
            {
                if (m_board.IsBottomBoardFull())
                {
                    break;
                }
            }
        }
        isAutoplaying = false;
    }

    private bool isAutoplaying = false;

    public void AutoplayButtonClicked()
    {
        if (!isAutoplaying)
        {
            StartCoroutine(AutoplayCoroutine());
        }
    }

    private IEnumerator AutoplayCoroutine()
    {
        isAutoplaying = true;
        while (!m_board.IsWin())
        {
            bool madeMove = false;
            // 1. Try to complete a match in the bottom board
            var bottomGroups = m_board.GetBottomGroupsWithCount(2);
            if (bottomGroups.Count > 0)
            {
                var targetType = bottomGroups[0];
                while (true)
                {
                    var cellToMove = m_board.GetFirstCellOfType(targetType);
                    if (cellToMove == null) break;
                    bool moved = m_board.TryMoveToBottom(cellToMove);
                    if (moved)
                    {
                        madeMove = true;
                        yield return new WaitForSeconds(0.5f);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                // 2. Move all items of the most frequent type
                var freqType = m_board.GetMostFrequentTypeOnMainBoard();
                if (freqType.HasValue)
                {
                    while (true)
                    {
                        var cellToMove = m_board.GetFirstCellOfType(freqType.Value);
                        if (cellToMove == null) break;
                        bool moved = m_board.TryMoveToBottom(cellToMove);
                        if (moved)
                        {
                            madeMove = true;
                            yield return new WaitForSeconds(0.5f);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    // 3. If still not, pick any valid cell
                    while (true)
                    {
                        var cellToMove = m_board.GetFirstMovableCell();
                        if (cellToMove == null) break;
                        bool moved = m_board.TryMoveToBottom(cellToMove);
                        if (moved)
                        {
                            madeMove = true;
                            yield return new WaitForSeconds(0.5f);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            if (!madeMove) break;
        }
        isAutoplaying = false;
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    public bool IsBoardCleared()
    {
        return m_board != null && m_board.IsWin();
    }

    public bool IsTimeAttackMode()
    {
        // Example: check GameManager for current mode
        var gm = GameObject.FindObjectOfType<GameManager>();
        return gm != null && gm.CurrentLevelMode == GameManager.eLevelMode.TIME_ATTACK;
    }
}
