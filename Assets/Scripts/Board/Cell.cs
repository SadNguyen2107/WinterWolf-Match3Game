using System;
using DG.Tweening;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public int BoardX { get; private set; }

    public int BoardY { get; private set; }

    public Item Item { get; private set; }


    public bool IsEmpty => Item == null;

    public void Setup(int cellX, int cellY)
    {
        this.BoardX = cellX;
        this.BoardY = cellY;
    }

    private void OnMouseDown()
    {
        // Only allow return if this is a bottom cell and has an item
        if (BoardY == -1 && Item != null && Item.OriginalCell != null)
        {
            // Check if Time Attack mode is active
            var boardController = FindObjectOfType<BoardController>();
            if (boardController != null && boardController.IsTimeAttackMode())
            {
                // Animate item back to its original cell
                Item.View.DOMove(Item.OriginalCell.transform.position, 0.3f).OnComplete(() =>
                {
                    Item.OriginalCell.Assign(Item);
                    Item.OriginalCell.ApplyItemPosition(true);
                    this.Free();
                });
            }
        }
    }

    public bool IsNormalCell()
    {
        // Assuming bottom board cells have BoardY == -1
        return !IsEmpty && Item is NormalItem && BoardY != -1;
    }

    public bool IsNeighbour(Cell other)
    {
        return BoardX == other.BoardX && Mathf.Abs(BoardY - other.BoardY) == 1 ||
            BoardY == other.BoardY && Mathf.Abs(BoardX - other.BoardX) == 1;
    }


    public void Free()
    {
        Item = null;
    }

    public void Assign(Item item)
    {
        Item = item;
        Item.SetCell(this);
    }

    public void ApplyItemPosition(bool withAppearAnimation)
    {
        Item.SetViewPosition(this.transform.position);

        if (withAppearAnimation)
        {
            Item.ShowAppearAnimation();
        }
    }

    internal void Clear()
    {
        if (Item != null)
        {
            Item.Clear();
            Item = null;
        }
    }

    internal bool IsSameType(Cell other)
    {
        return Item != null && other.Item != null && Item.IsSameType(other.Item);
    }

    internal void ExplodeItem()
    {
        if (Item == null) return;

        Item.ExplodeView();
        Item = null;
    }

    internal void AnimateItemForHint()
    {
        Item.AnimateForHint();
    }

    internal void StopHintAnimation()
    {
        Item.StopAnimateForHint();
    }

    internal void ApplyItemMoveToPosition()
    {
        Item.AnimationMoveToPosition();
    }
}
