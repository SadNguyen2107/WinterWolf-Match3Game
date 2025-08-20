using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class LevelNormal : LevelCondition
{
    private BoardController m_board;

    public override void Setup(float value, Text txt, BoardController board)
    {
        base.Setup(value, txt);
        m_board = board;
        m_board.OnMoveEvent += OnMove;
        UpdateText();
    }

    private void OnMove()
    {
        if (m_conditionCompleted) return;
        // In normal mode, just check if the board is cleared
        if (m_board.IsBoardCleared())
        {
            OnConditionComplete();
        }
        UpdateText();
    }

    protected override void UpdateText()
    {
        m_txt.text = ""; // No moves to display in normal mode
    }

    protected override void OnDestroy()
    {
        if (m_board != null) m_board.OnMoveEvent -= OnMove;
        base.OnDestroy();
    }
}
