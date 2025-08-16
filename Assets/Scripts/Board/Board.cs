using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class Board
{

    // Returns true if all bottom cells are filled
    public bool IsBottomBoardFull()
    {
        foreach (var cell in m_bottomCells)
        {
            if (cell.IsEmpty) return false;
        }
        return true;
    }

    public NormalItem.eNormalType? GetMostFrequentTypeOnMainBoard()
    {
        var typeCounts = new Dictionary<NormalItem.eNormalType, int>();
        foreach (var cell in m_cells)
        {
            if (!cell.IsEmpty && cell.Item is NormalItem n)
            {
                if (!typeCounts.ContainsKey(n.ItemType))
                    typeCounts[n.ItemType] = 0;
                typeCounts[n.ItemType]++;
            }
        }
        if (typeCounts.Count == 0) return null;
        return typeCounts.OrderByDescending(kv => kv.Value).First().Key;
    }

    // Returns list of NormalItem.eNormalType that have exactly 'count' items in bottom board
    public List<NormalItem.eNormalType> GetBottomGroupsWithCount(int count)
    {
        return m_bottomCells
            .Where(c => !c.IsEmpty && c.Item is NormalItem)
            .GroupBy(c => ((NormalItem)c.Item).ItemType)
            .Where(g => g.Count() == count)
            .Select(g => g.Key)
            .ToList();
    }

    // Returns first cell in main board with given type
    public Cell GetFirstCellOfType(NormalItem.eNormalType type)
    {
        foreach (var cell in m_cells)
        {
            if (!cell.IsEmpty && cell.Item is NormalItem n && n.ItemType == type)
                return cell;
        }
        return null;
    }

    public bool IsWin()
    {
        return m_cells.Cast<Cell>().All(c => c.IsEmpty);
    }

    public Cell GetFirstMovableCell()
    {
        foreach (var cell in m_cells)
        {
            if (!cell.IsEmpty && cell.Item is NormalItem)
                return cell;
        }
        return null;
    }

    public enum eMatchDirection
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        ALL
    }

    private int boardSizeX;

    private int boardSizeY;

    private int bottomBoardSizeX;

    private Cell[,] m_cells;
    private Cell[] m_bottomCells;

    private Transform m_root;

    private int m_matchMin;

    public Board(Transform transform, GameSettings gameSettings)
    {
        m_root = transform;

        m_matchMin = gameSettings.MatchesMin;

        this.boardSizeX = gameSettings.BoardSizeX;
        this.boardSizeY = gameSettings.BoardSizeY;

        this.bottomBoardSizeX = gameSettings.BottomBoardSizeX;

        m_cells = new Cell[boardSizeX, boardSizeY];
        m_bottomCells = new Cell[this.bottomBoardSizeX];

        CreateBoard();
        CreateBottomBoard();
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = origin + new Vector3(x, y, 0f);
                go.transform.SetParent(m_root);

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y);

                m_cells[x, y] = cell;
            }
        }

        //set neighbours
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (y + 1 < boardSizeY) m_cells[x, y].NeighbourUp = m_cells[x, y + 1];
                if (x + 1 < boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
                if (y > 0) m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
                if (x > 0) m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
            }
        }
    }

    void CreateBottomBoard()
    {
        Vector3 origin = new Vector3(-bottomBoardSizeX * 0.5f + 0.5f, -4.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < bottomBoardSizeX; x++)
        {
            GameObject go = GameObject.Instantiate(prefabBG);
            go.transform.position = origin + new Vector3(x, 0f, 0f);

            go.transform.SetParent(m_root);

            Cell cell = go.GetComponent<Cell>();
            cell.Setup(x, -1); // -1 for bottom row

            m_bottomCells[x] = cell;
        }
    }

    public bool TryMoveToBottom(Cell fromCell)
    {
        // First, check if there's any empty cell in the bottom board
        int emptyIndex = -1;
        for (int i = 0; i < m_bottomCells.Length; i++)
        {
            if (m_bottomCells[i].IsEmpty)
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex >= 0 && fromCell.Item != null)
        {
            // Store a reference to the item BEFORE we free the cell
            Item movingItem = fromCell.Item;

            // Set the original cell for Time Attack mode
            movingItem.OriginalCell = fromCell;

            // Animate item moving from board to bottom cell
            movingItem.View?.DOMove(m_bottomCells[emptyIndex].transform.position, 0.3f);

            // Assign the item to the bottom cell and set its properties
            m_bottomCells[emptyIndex].Assign(movingItem);
            movingItem.SetSortingLayerHigher();
            m_bottomCells[emptyIndex].ApplyItemPosition(true);

            // Now we can safely free the original cell
            fromCell.Free();

            // Then rearrange all items to group by type
            var items = m_bottomCells.Where(c => !c.IsEmpty).Select(c => c.Item).ToList();

            // Sort items by type (group same types together)
            var sortedItems = items.OrderBy(it =>
            {
                // If the item is a NormalItem, sort by its ItemType
                if (it is NormalItem normalItem)
                {
                    return normalItem.ItemType.ToString();
                }
                // Otherwise, sort by the item's class type
                else
                {
                    return it.GetType().ToString();
                }
            }).ToList();

            // Track which item was just added using our saved reference
            Item addedItem = movingItem;  // Use the stored reference

            // Store the position of the newly added item before sorting
            Vector3 originalItemPosition = Vector3.zero;
            if (addedItem != null && addedItem.View != null)
            {
                originalItemPosition = addedItem.View.transform.position;
            }

            // Reassign sorted items without animation first
            for (int j = 0; j < m_bottomCells.Length; j++)
            {
                if (j < sortedItems.Count)
                {
                    // Assign item to cell without animation
                    m_bottomCells[j].Assign(sortedItems[j]);
                    var sr = sortedItems[j].View.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = j;

                    // If it's the added item, keep its original position temporarily
                    if (sortedItems[j] == addedItem && addedItem.View != null)
                    {
                        // Keep at original position temporarily
                        addedItem.View.transform.position = originalItemPosition;
                    }
                    else
                    {
                        // Apply position for other items immediately
                        m_bottomCells[j].ApplyItemPosition(false);
                    }
                }
                else
                {
                    m_bottomCells[j].Free();
                }
            }

            // Now animate only the newly added item to its final position
            if (addedItem != null && addedItem.View != null)
            {
                for (int j = 0; j < m_bottomCells.Length; j++)
                {
                    if (j < sortedItems.Count && sortedItems[j] == addedItem)
                    {
                        // Only now animate the newly added item to its final position
                        addedItem.View.DOMove(m_bottomCells[j].transform.position, 0.3f);
                        break;
                    }
                }
            }

            CheckBottomMatch();
            CheckWinLose();
            return true;
        }

        return false; // No space in bottom or no item in fromCell
    }

    private void CheckBottomMatch()
    {
        // Find groups of 3 matching items (traditional match-3 rule)
        var groups = m_bottomCells
            .Where(c => !c.IsEmpty && c.Item is NormalItem)
            .GroupBy(c => ((NormalItem)c.Item).ItemType)
            .Where(g => g.Count() >= 3);

        bool matched = false;

        foreach (var group in groups)
        {
            // Clear all items in the matching group
            foreach (var cell in group)
            {
                // Add visual feedback - only scale animation (no explosion)
                if (cell.Item != null && cell.Item.View != null)
                {
                    // Scale down and then up for a pulse effect
                    cell.Item.View.DOScale(0.1f, 0.1f).OnComplete(() =>
                    {
                        // Free the cell immediately after the scale animation
                        cell.ExplodeItem();
                    });
                }
                else
                {
                    // Just free the cell if there's no view to animate
                    cell.ExplodeItem();
                }
                matched = true;
            }
        }

        // If we had matches, shift remaining items
        if (matched)
        {
            // Wait for animations to finish before collapsing
            DOVirtual.DelayedCall(0.3f, () =>
            {
                // Get remaining items
                var remainingItems = m_bottomCells
                    .Where(c => !c.IsEmpty)
                    .Select(c => c.Item)
                    .ToList();

                // Clear all cells
                foreach (var cell in m_bottomCells)
                {
                    cell.Free();
                }

                // Reposition remaining items from left to right (standard collapse)
                for (int i = 0; i < remainingItems.Count; i++)
                {
                    m_bottomCells[i].Assign(remainingItems[i]);
                    m_bottomCells[i].ApplyItemPosition(true);
                }
            });
        }
    }


    private void CheckWinLose()
    {
        bool boardCleared = m_cells.Cast<Cell>().All(c => c.IsEmpty);
        bool bottomFull = m_bottomCells.All(c => !c.IsEmpty);

        if (boardCleared)
        {
            var gm = GameObject.FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.SetState(GameManager.eStateGame.GAME_WIN);
            }
        }
        else if (bottomFull)
        {
            // Before triggering game over, check if any matches are possible in the bottom board
            bool possibleMatch = false;

            // Group items by type
            var groups = m_bottomCells
                .Where(c => !c.IsEmpty && c.Item is NormalItem)
                .GroupBy(c => ((NormalItem)c.Item).ItemType);

            // Check if any group has 3 or more items (can form a match)
            foreach (var group in groups)
            {
                if (group.Count() >= 3)
                {
                    possibleMatch = true;
                    break;
                }
            }

            // Only trigger game over if there are no possible matches
            if (!possibleMatch)
            {
                var gm = GameObject.FindObjectOfType<GameManager>();
                if (gm != null)
                {
                    gm.SetState(GameManager.eStateGame.GAME_OVER);
                }
            }
        }
    }

    internal void Fill()
    {
        // Guarantee each item type count is divisible by 3
        int totalCells = boardSizeX * boardSizeY;
        var allTypes = Enum.GetValues(typeof(NormalItem.eNormalType)).Cast<NormalItem.eNormalType>().ToList();
        List<NormalItem> items = new List<NormalItem>();

        int typeCount = allTypes.Count;
        int minGroup = 3;
        int groups = totalCells / (typeCount * minGroup);
        int remainder = totalCells % (typeCount * minGroup);

        // Add full groups
        for (int g = 0; g < groups; g++)
        {
            foreach (var t in allTypes)
            {
                for (int i = 0; i < minGroup; i++)
                {
                    var item = new NormalItem();
                    item.SetType(t);
                    item.SetView();
                    item.SetViewRoot(m_root);
                    items.Add(item);
                }
            }
        }

        // Add remainder, always in multiples of 3
        int extraGroups = remainder / minGroup;
        for (int i = 0; i < extraGroups; i++)
        {
            var t = allTypes[i % typeCount];
            for (int j = 0; j < minGroup; j++)
            {
                var item = new NormalItem();
                item.SetType(t);
                item.SetView();
                item.SetViewRoot(m_root);
                items.Add(item);
            }
        }

        // Shuffle items
        items = items.OrderBy(_ => UnityEngine.Random.value).ToList();

        int idx = 0;
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.Assign(items[idx]);
                cell.ApplyItemPosition(false);
                idx++;
            }
        }
    }

    internal void Shuffle()
    {
        List<Item> list = new List<Item>();
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                list.Add(m_cells[x, y].Item);
                m_cells[x, y].Free();
            }
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                int rnd = UnityEngine.Random.Range(0, list.Count);
                m_cells[x, y].Assign(list[rnd]);
                m_cells[x, y].ApplyItemMoveToPosition();

                list.RemoveAt(rnd);
            }
        }
    }


    internal void FillGapsWithNewItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (!cell.IsEmpty) continue;

                NormalItem item = new NormalItem();

                item.SetType(Utils.GetRandomNormalType());
                item.SetView();
                item.SetViewRoot(m_root);

                cell.Assign(item);
                cell.ApplyItemPosition(true);
            }
        }
    }

    internal void ExplodeAllItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.ExplodeItem();
            }
        }
    }

    public void Swap(Cell cell1, Cell cell2, Action callback)
    {
        Item item = cell1.Item;
        cell1.Free();
        Item item2 = cell2.Item;
        cell1.Assign(item2);
        cell2.Free();
        cell2.Assign(item);

        item.View.DOMove(cell2.transform.position, 0.3f);
        item2.View.DOMove(cell1.transform.position, 0.3f).OnComplete(() => { if (callback != null) callback(); });
    }

    public List<Cell> GetHorizontalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        //check horizontal match
        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourRight;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourLeft;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return list;
    }


    public List<Cell> GetVerticalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourUp;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourBottom;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return list;
    }

    internal void ConvertNormalToBonus(List<Cell> matches, Cell cellToConvert)
    {
        eMatchDirection dir = GetMatchDirection(matches);

        BonusItem item = new BonusItem();
        switch (dir)
        {
            case eMatchDirection.ALL:
                item.SetType(BonusItem.eBonusType.ALL);
                break;
            case eMatchDirection.HORIZONTAL:
                item.SetType(BonusItem.eBonusType.HORIZONTAL);
                break;
            case eMatchDirection.VERTICAL:
                item.SetType(BonusItem.eBonusType.VERTICAL);
                break;
        }

        if (item != null)
        {
            if (cellToConvert == null)
            {
                int rnd = UnityEngine.Random.Range(0, matches.Count);
                cellToConvert = matches[rnd];
            }

            item.SetView();
            item.SetViewRoot(m_root);

            cellToConvert.Free();
            cellToConvert.Assign(item);
            cellToConvert.ApplyItemPosition(true);
        }
    }


    internal eMatchDirection GetMatchDirection(List<Cell> matches)
    {
        if (matches == null || matches.Count < m_matchMin) return eMatchDirection.NONE;

        var listH = matches.Where(x => x.BoardX == matches[0].BoardX).ToList();
        if (listH.Count == matches.Count)
        {
            return eMatchDirection.VERTICAL;
        }

        var listV = matches.Where(x => x.BoardY == matches[0].BoardY).ToList();
        if (listV.Count == matches.Count)
        {
            return eMatchDirection.HORIZONTAL;
        }

        if (matches.Count > 5)
        {
            return eMatchDirection.ALL;
        }

        return eMatchDirection.NONE;
    }

    internal List<Cell> FindFirstMatch()
    {
        List<Cell> list = new List<Cell>();

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                var listhor = GetHorizontalMatches(cell);
                if (listhor.Count >= m_matchMin)
                {
                    list = listhor;
                    break;
                }

                var listvert = GetVerticalMatches(cell);
                if (listvert.Count >= m_matchMin)
                {
                    list = listvert;
                    break;
                }
            }
        }

        return list;
    }

    public List<Cell> CheckBonusIfCompatible(List<Cell> matches)
    {
        var dir = GetMatchDirection(matches);

        var bonus = matches.Where(x => x.Item is BonusItem).FirstOrDefault();
        if (bonus == null)
        {
            return matches;
        }

        List<Cell> result = new List<Cell>();
        switch (dir)
        {
            case eMatchDirection.HORIZONTAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.HORIZONTAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.VERTICAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.VERTICAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.ALL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.ALL)
                    {
                        result.Add(cell);
                    }
                }
                break;
        }

        return result;
    }

    internal List<Cell> GetPotentialMatches()
    {
        List<Cell> result = new List<Cell>();
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                //check right
                /* example *\
                  * * * * *
                  * * * * *
                  * * * ? *
                  * & & * ?
                  * * * ? *
                \* example  */

                if (cell.NeighbourRight != null)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourRight, cell.NeighbourRight.NeighbourRight);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check up
                /* example *\
                  * ? * * *
                  ? * ? * *
                  * & * * *
                  * & * * *
                  * * * * *
                \* example  */
                if (cell.NeighbourUp != null)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourUp, cell.NeighbourUp.NeighbourUp);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check bottom
                /* example *\
                  * * * * *
                  * & * * *
                  * & * * *
                  ? * ? * *
                  * ? * * *
                \* example  */
                if (cell.NeighbourBottom != null)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourBottom, cell.NeighbourBottom.NeighbourBottom);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check left
                /* example *\
                  * * * * *
                  * * * * *
                  * ? * * *
                  ? * & & *
                  * ? * * *
                \* example  */
                if (cell.NeighbourLeft != null)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourLeft, cell.NeighbourLeft.NeighbourLeft);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * * * * *
                  * * ? * *
                  * & * & *
                  * * ? * *
                \* example  */
                Cell neib = cell.NeighbourRight;
                if (neib != null && neib.NeighbourRight != null && neib.NeighbourRight.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellVertical(neib, cell);
                    if (second != null)
                    {
                        result.Add(cell);
                        result.Add(neib.NeighbourRight);
                        result.Add(second);
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * & * * *
                  ? * ? * *
                  * & * * *
                  * * * * *
                \* example  */
                neib = null;
                neib = cell.NeighbourUp;
                if (neib != null && neib.NeighbourUp != null && neib.NeighbourUp.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellHorizontal(neib, cell);
                    if (second != null)
                    {
                        result.Add(cell);
                        result.Add(neib.NeighbourUp);
                        result.Add(second);
                        break;
                    }
                }
            }

            if (result.Count > 0) break;
        }

        return result;
    }

    private List<Cell> GetPotentialMatch(Cell cell, Cell neighbour, Cell target)
    {
        List<Cell> result = new List<Cell>();

        if (neighbour != null && neighbour.IsSameType(cell))
        {
            Cell third = LookForTheThirdCell(target, neighbour);
            if (third != null)
            {
                result.Add(cell);
                result.Add(neighbour);
                result.Add(third);
            }
        }

        return result;
    }

    private Cell LookForTheSecondCellHorizontal(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look right
        Cell second = null;
        second = target.NeighbourRight;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look left
        second = null;
        second = target.NeighbourLeft;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheSecondCellVertical(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look up        
        Cell second = target.NeighbourUp;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look bottom
        second = null;
        second = target.NeighbourBottom;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheThirdCell(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look up
        Cell third = CheckThirdCell(target.NeighbourUp, main);
        if (third != null)
        {
            return third;
        }

        //look right
        third = null;
        third = CheckThirdCell(target.NeighbourRight, main);
        if (third != null)
        {
            return third;
        }

        //look bottom
        third = null;
        third = CheckThirdCell(target.NeighbourBottom, main);
        if (third != null)
        {
            return third;
        }

        //look left
        third = null;
        third = CheckThirdCell(target.NeighbourLeft, main); ;
        if (third != null)
        {
            return third;
        }

        return null;
    }

    private Cell CheckThirdCell(Cell target, Cell main)
    {
        if (target != null && target != main && target.IsSameType(main))
        {
            return target;
        }

        return null;
    }

    internal void ShiftDownItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            int shifts = 0;
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell.IsEmpty)
                {
                    shifts++;
                    continue;
                }

                if (shifts == 0) continue;

                Cell holder = m_cells[x, y - shifts];

                Item item = cell.Item;
                cell.Free();

                holder.Assign(item);
                item.View.DOMove(holder.transform.position, 0.3f);
            }
        }
    }

    public void Clear()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.Clear();

                GameObject.Destroy(cell.gameObject);
                m_cells[x, y] = null;
            }
        }
    }
}
