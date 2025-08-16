using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelTimeAttack : LevelCondition
{
    private float m_time;
    private GameManager m_mngr;
    private BoardController m_boardController;

    public void Setup(float value, Text txt, GameManager mngr, BoardController boardController)
    {
        base.Setup(value, txt, mngr);
        m_mngr = mngr;
        m_boardController = boardController;
        m_time = value;
        UpdateText();
    }

    private void Update()
    {
        if (m_conditionCompleted) return;
        if (m_mngr.State != GameManager.eStateGame.GAME_STARTED) return;
        m_time -= Time.deltaTime;
        UpdateText();
        if (m_time <= 0f)
        {
            // Lose if board not cleared
            if (!m_boardController.IsBoardCleared())
            {
                OnConditionComplete();
            }
        }
        // Win if board cleared before time runs out
        if (m_boardController.IsBoardCleared())
        {
            m_conditionCompleted = true;
            m_mngr.SetState(GameManager.eStateGame.GAME_WIN);
        }
    }

    protected override void UpdateText()
    {
        if (m_time < 0f) return;
        m_txt.text = string.Format("TIME ATTACK:\n{0:00}", m_time);
    }
}
