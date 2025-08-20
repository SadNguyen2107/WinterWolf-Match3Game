using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : ScriptableObject
{
    public int BoardSizeX = 5;

    public int BoardSizeY = 5;

    public int BottomBoardSizeX = 5;

    public int LevelMoves = 16;

    public float LevelTime = 30f;
}
