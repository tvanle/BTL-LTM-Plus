# Chi Tiết Gameplay Client - WordBrain

## 1. GameManager - Lớp Quản Lý Trung Tâm

### Vai trò
- Singleton pattern - instance duy nhất quản lý toàn bộ game
- Điều phối giữa các component: LetterBoard, WordGrid
- Quản lý save/load game state
- Xử lý logic hoàn thành level và chuyển màn

### Data Classes

#### CategoryInfo
```csharp
public class CategoryInfo {
    string name;        // Tên danh mục (unique)
    string description; // Mô tả ngắn
    Sprite icon;        // Icon hiển thị
    List<LevelInfo> levelInfos; // Danh sách levels
}
```

#### LevelInfo
```csharp
public class LevelInfo {
    string[] words;     // Các từ cần tìm trong level
}
```

#### BoardState - Trạng thái bàn chơi
```csharp
public class BoardState {
    // Định danh
    string wordBoardId;     // ID duy nhất của board
    int wordBoardSize;      // Kích thước grid (vd: 4x4, 5x5)

    // Nội dung game
    string[] words;         // Danh sách từ cần tìm
    bool[] foundWords;      // Đánh dấu từ đã tìm thấy
    char[] tileLetters;     // Chữ cái trên mỗi ô

    // Trạng thái từng ô
    TileState[] tileStates; // NotUsed/Found/UsedButNotFound

    // Hint tracking
    int nextHintIndex;      // Vị trí hint tiếp theo
    List<int[]> hintLettersShown; // Lưu vị trí đã show hint
}
```

### Properties quan trọng
- `CurrentHints`: Số hint hiện có
- `ActiveCategory`: Danh mục đang chơi
- `ActiveLevelIndex`: Level hiện tại
- `ActiveBoardState`: Trạng thái board đang chơi
- `SavedBoardStates`: Dictionary lưu các board đã chơi dở
- `CompletedLevels`: Dictionary đánh dấu level đã hoàn thành

### Methods chính

#### StartLevel(category, levelIndex)
1. Lưu category và level đang chơi
2. Load WordBoard từ Resources (file JSON)
3. Kiểm tra SavedBoardStates có board này chưa
4. Nếu chưa → tạo BoardState mới
5. Setup LetterBoard và WordGrid với BoardState
6. Save game

#### OnWordFound(word, letterTiles, foundAllWords)
1. Đánh dấu các tile là Found trong BoardState
2. Set foundWords[wordIndex] = true
3. Save game
4. Animate tiles từ LetterBoard → WordGrid
5. Nếu foundAllWords → gọi BoardComplete()

#### BoardComplete()
1. Tính điểm thưởng hints:
   - Level thường lần đầu: `completeNormalLevelAward`
   - Daily puzzle: `completeDailyPuzzleAward`
2. Cập nhật CompletedLevels
3. Xóa BoardState khỏi SavedBoardStates
4. Hiển thị màn hình Complete
5. Chuyển sang level tiếp theo hoặc về menu

#### Save/Load System
- Save dạng JSON tại `Application.persistentDataPath/save.dat`
- Lưu: hints, active level, board states, completed levels
- Load với xử lý compatibility cho format cũ

## 2. LetterBoard - Bảng Chữ Cái Tương Tác

### Vai trò
- Hiển thị grid chữ cái cho player tương tác
- Xử lý drag/swipe để chọn từ
- Kiểm tra từ hợp lệ
- Vẽ line nối các chữ được chọn

### Components & Variables
```csharp
Canvas uiCanvas;                    // Canvas reference cho tính toán
GridLayoutGroup letterTileContainer; // Container chứa tiles
float tileTouchOffset;              // Vùng có thể touch (0-1)
float tileSpacing;                  // Khoảng cách giữa tiles
bool enableLine;                    // Bật/tắt vẽ line
```

### Data Structure
```csharp
List<LetterTile> letterTiles;       // Tất cả tiles trên board
List<LetterTile> selectedLetterTiles; // Tiles đang được chọn
string selectedWord;                // Từ đang được tạo
List<string> currentWords;          // Danh sách từ cần tìm
```

### Cơ chế Drag & Select

#### OnBeginDrag/OnDrag
1. Tính vị trí mouse/touch
2. Duyệt qua letterTiles, kiểm tra vị trí
3. Tính vùng touch cho mỗi tile:
   ```csharp
   scaleTileSize = currentTileSize * uiCanvas.scaleFactor * tileTouchOffset
   // Kiểm tra mouse trong vùng tile
   if (position.x > left && position.x < right &&
       position.y > bottom && position.y < top)
   ```
4. Kiểm tra tile có thể chọn không (CheckCanSelectTile)
5. Thêm vào selectedLetterTiles nếu hợp lệ

#### CheckCanSelectTile(tileIndex)
- Tile đầu tiên: luôn có thể chọn
- Tile tiếp theo: phải kề cận tile cuối cùng được chọn
- Kiểm tra 8 hướng (trên, dưới, trái, phải, 4 góc):
  ```csharp
  // Tính row/col của tile cuối và tile kiểm tra
  Mathf.Abs(lastRow - tileRow) <= 1 &&
  Mathf.Abs(lastCol - tileCol) <= 1
  ```

#### OnEndDrag → TrySelectWord()
1. So sánh selectedWord với currentWords
2. Nếu đúng → gọi FoundWord()
3. Reset selection (clear tiles, word = "")

### Line Drawing System
- Sử dụng ObjectPool cho line segments
- Vẽ line nối giữa các tiles được chọn
- Tính góc và độ dài cho mỗi segment
- Line end có hướng dựa vào segment cuối

## 3. WordGrid - Hiển Thị Từ Cần Tìm

### Vai trò
- Hiển thị placeholder cho các từ cần tìm
- Nhận tiles từ LetterBoard khi tìm thấy từ
- Quản lý hints
- Tự động layout theo chiều rộng container

### Data Structure
```csharp
class GridTile {
    GameObject gridTileObject;   // Placeholder tile
    GameObject letterTileObject; // Letter tile (khi found)
    bool displayed;             // Đã hiển thị chưa
    char letter;                // Chữ cái
}

Dictionary<string, List<GridTile>> allGridTiles; // Map từ → tiles
List<GameObject> rowObjects;     // Các hàng trong grid
```

### Setup Process
1. Tạo rows dựa vào container width
2. Với mỗi từ:
   - Tính width = số chữ × tileSize + spacing
   - Kiểm tra có vừa row hiện tại không
   - Nếu không → tạo row mới
   - Tạo GridTile cho mỗi chữ
3. Restore từ đã found từ BoardState
4. Restore hint letters đã shown

### FoundWord Animation
```csharp
public void FoundWord(word, letterTiles, onFinished) {
    // Map mỗi letterTile với gridTile tương ứng
    for (i = 0; i < letterTiles.Count; i++) {
        gridTiles[i].displayed = true;
        // Animate từ vị trí LetterBoard → WordGrid
        TransitionAnimateOver(letterTile, gridTile);
    }
}
```

Animation sử dụng Tween:
- Scale từ kích thước board → grid size
- Move position với EaseOut style
- Duration: 400ms

### Hint System
```csharp
DisplayNextHint(ref nextHintIndex, out wordIndex, out letterIndex)
```
1. Duyệt từ nextHintIndex trong danh sách từ
2. Tìm chữ cái chưa displayed
3. Gọi DisplayLetter() để hiển thị
4. Cập nhật nextHintIndex cho lần sau
5. Trả về wordIndex và letterIndex để lưu

## 4. LetterTile - Component Ô Chữ

### Properties
```csharp
int TileIndex;      // Vị trí trong grid
char Letter;        // Chữ cái
Text LetterText;    // UI Text component
bool Selected;      // Đang được chọn
bool Found;         // Đã tìm thấy
```

### States
- **Normal**: Hiển thị trên LetterBoard
- **Selected**: Đang được drag qua (đổi màu/scale)
- **Found**: Đã tìm thấy từ (animate sang WordGrid)

## 5. Game Flow Chi Tiết

### Khởi động Level
1. GameManager.StartLevel() được gọi
2. Load WordBoard từ Resources/WordBoards/
3. Tạo/load BoardState
4. LetterBoard.Setup():
   - Tạo grid với kích thước phù hợp
   - Instantiate LetterTiles cho các ô có chữ
   - Scale tiles theo container size
5. WordGrid.Setup():
   - Tạo placeholder cho mỗi từ
   - Auto-layout theo rows
   - Restore từ/hints đã found

### Gameplay Loop
1. **Player drag finger/mouse**:
   - LetterBoard nhận input events
   - UpdateSelectedTiles() mỗi frame
   - Highlight tiles được chọn
   - Vẽ line nối

2. **Release drag**:
   - TrySelectWord() kiểm tra từ
   - Nếu đúng:
     - Đánh dấu found trong BoardState
     - Trigger animation
     - Update WordGrid
   - Nếu sai: reset selection

3. **Animation**:
   - Tiles "bay" từ board → grid
   - Scale down để vừa grid size
   - Fade in tại vị trí mới

4. **Check completion**:
   - Kiểm tra foundAllWords
   - Nếu xong → BoardComplete()
   - Award hints, save progress
   - Load next level hoặc về menu

### Save System
- Auto-save sau mỗi action quan trọng
- Lưu trạng thái từng tile
- Lưu hints đã dùng
- Cho phép continue sau khi thoát

### Daily Puzzle
- Random chọn từ danh sách dailyPuzzles
- Reset lúc 00:00 mỗi ngày
- Award hints cao hơn level thường
- Không lưu progress qua ngày

## 6. Optimization & Performance

### Object Pooling
- LetterTilePool: Tái sử dụng tiles
- LineSegmentPool: Tái sử dụng line segments
- Giảm garbage collection

### Layout Optimization
- Sử dụng Unity Layout Groups
- Tự động scale theo screen size
- ContentSizeFitter cho dynamic height

### Memory Management
- Clear references khi reset
- Destroy unused GameObjects
- Dictionary cho fast lookup

## 7. Multiplayer Integration Points

Khi chuyển sang multiplayer, các điểm cần modify:

1. **GameManager**:
   - Nhận words từ server thay vì local
   - Sync BoardState với server
   - Report score/progress real-time

2. **LetterBoard**:
   - Send word submissions to server
   - Receive validation từ server
   - Show opponent selections

3. **WordGrid**:
   - Display opponent progress
   - Sync found words across players

4. **Scoring**:
   - Time-based scoring
   - Leaderboard updates
   - Multiplayer bonuses