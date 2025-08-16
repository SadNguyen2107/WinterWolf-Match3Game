# WinterWolf-Game

This README documents the features and code changes implemented by GitHub Copilot in your Unity Match 3 project.

## Features & Changes Implemented

### 1. Match 3 Gameplay with Bottom Board

- Changed gameplay to use a bottom board for moving and matching items.
- Grouped items of the same type together in the bottom board after each move.
- Collapse logic after match clear, always making same-type items adjacent.
- Traditional Match 3 match detection and collapse logic.

### 2. UI Integration

- Added and wired up UI buttons for Autoplay, Autolose, and Time Attack modes in `UIPanelMain.cs`.
- Created click handlers and manager methods for new game modes.

### 3. Smarter Autoplay & Autolose

- Implemented smarter autoplay/autolose logic in `BoardController.cs`.
- Autoplay prioritizes matches and optimal moves.

### 4. Animation Improvements

- Fixed animation bug for newly added items in the bottom board (only the new item animates).
- Match clear animation now only scales items (no explosion effect).

### 5. Time Attack Mode

- Added Time Attack mode:
  - New button in UI to start Time Attack.
  - Player does not lose when bottom board is full.
  - Player can return items from bottom board to their original position by clicking.
  - Player loses if the board is not cleared within 1 minute.
- Created `LevelTimeAttack.cs` for timer and win/loss logic.
- Updated `GameManager.cs` and `BoardController.cs` to support Time Attack mode.

### 6. Code Structure & Bug Fixes

- Added `OriginalCell` property to `Item` for item return mechanic.
- Added `IsTimeAttackMode()` to `BoardController` and `CurrentLevelMode` to `GameManager`.
- Ensured all new features are only active in the correct game mode.
- Fixed compile errors and improved code readability.

## How to Use

- Start the game and use the UI buttons to select game modes.
- In Time Attack mode, click bottom board items to return them to their original position.
- Win by clearing the board before the timer runs out.

## Files Modified or Added

- `Assets/Scripts/Board/Board.cs`
- `Assets/Scripts/Board/Cell.cs`
- `Assets/Scripts/Board/Item.cs`
- `Assets/Scripts/Controllers/BoardController.cs`
- `Assets/Scripts/Controllers/GameManager.cs`
- `Assets/Scripts/Controllers/LevelTimeAttack.cs` (new)
- `Assets/Scripts/UI/UIPanelMain.cs`
- `Assets/Scripts/UI/UIMainManager.cs`
