using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelMain : MonoBehaviour, IMenu
{

    [SerializeField] private Button btnMoves;
    [SerializeField] private Button btnTimeAttack;
    [SerializeField] private Button btnAutoPlay;
    [SerializeField] private Button btnAutoLose;

    private UIMainManager m_mngr;

    private void Awake()
    {
        btnMoves.onClick.AddListener(OnClickMoves);
        btnAutoPlay.onClick.AddListener(OnClickAutoPlay);
        btnAutoLose.onClick.AddListener(OnClickAutoLose);
        if (btnTimeAttack) btnTimeAttack.onClick.AddListener(OnClickTimeAttack);
    }

    private void OnDestroy()
    {
        if (btnMoves) btnMoves.onClick.RemoveAllListeners();
        if (btnTimeAttack) btnTimeAttack.onClick.RemoveAllListeners();
        if (btnAutoPlay) btnAutoPlay.onClick.RemoveAllListeners();
        if (btnAutoLose) btnAutoLose.onClick.RemoveAllListeners();
    }
    private void OnClickTimeAttack()
    {
        if (m_mngr != null)
        {
            m_mngr.LoadLevelTimeAttack();
        }
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    private void OnClickAutoPlay()
    {
        BoardController boardController = FindObjectOfType<BoardController>();
        boardController.AutoplayButtonClicked();
    }
    private void OnClickAutoLose()
    {
        BoardController boardController = FindObjectOfType<BoardController>();
        if (boardController != null)
        {
            boardController.AutoloseButtonClicked();
        }
    }


    private void OnClickMoves()
    {
        m_mngr.LoadLevelNormal();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
