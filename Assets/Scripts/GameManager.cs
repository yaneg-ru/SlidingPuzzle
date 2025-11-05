using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

  [SerializeField] private Transform gameTransform;
  [SerializeField] private Transform piecePrefab;

  private List<Transform> pieces;
  private int emptyLocation;
  [SerializeField, Min(2), Tooltip("Number of rows in the puzzle (min 2).")] private int rows = 16;
  [SerializeField, Min(2), Tooltip("Number of columns in the puzzle (min 2).")] private int cols = 9;

  // Время в секундах для автоматической сборки пазла
  private float solveTimeInSeconds = 30f;
  // Время перемещения плитки в секундах для перемещения на одну клетку (базовая длительность)
  private float moveDuration = 0.15f;

  // Эталонное расстояние между соседними плитками в локальных координатах.
  // Заполняется в CreateGamePieces и используется для масштабирования длительности при оборачивании.
  private float singleTileDistance = 1f;

  private bool isAnimating = false;
  private List<int> recordedMoves = new List<int>();

  // Количество последних позиций пустой плитки, которые следует избегать при перемешивании
  private int shuffleHistorySize = 10;

  // Create the game setup with rows x cols pieces.
  /// <summary>
  /// Создаёт поле головоломки размера <c>rows x cols</c>, инстанцируя префабы плиток
  /// и настраивая их позицию, масштаб и UV‑координаты для корректного отображения
  /// исходного изображения (текстуры). Последняя (нижняя правая) плитка делается
  /// пустой (отключается), и её индекс сохраняется в <see cref="emptyLocation"/>.
  ///
  /// Метод не выполняет проверку корректности полей <c>rows</c> и <c>cols</c> — предполагается,
  /// что они уже заданы в другом месте (например, в <see cref="Start"/>).
  /// </summary>
  /// <param name="gapThickness">
  /// Толщина зазора (в локальных координатах игрового поля) — используется для
  /// уменьшения видимого размера плитки и подрезки UV, чтобы между плитками
  /// был визуальный промежуток.
  /// </param>
  private void CreateGamePieces(float gapThickness)
  {
    // Вычисляем aspect ratio сетки
    float aspectRatio = (float)cols / (float)rows;

    // Определяем масштабирующие коэффициенты для заполнения всего экрана
    float scaleX, scaleY;
    if (aspectRatio > 1) // Сетка шире, чем выше
    {
      scaleX = 1f;
      scaleY = 1f / aspectRatio;
    }
    else // Сетка выше, чем шире
    {
      scaleX = aspectRatio;
      scaleY = 1f;
    }

    float widthX = scaleX / (float)cols;
    float widthY = scaleY / (float)rows;

    // UV ширины/высоты в нормализованном пространстве текстуры (0..1).
    // Используем 1/cols и 1/rows чтобы покрыть всю текстуру независимо от визуального масштаба.
    float uvWidthX = 1f / (float)cols;
    float uvWidthY = 1f / (float)rows;

    // Проходимся по строкам и столбцам, создаём плитки слева направо сверху вниз.
    for (int row = 0; row < rows; row++)
    {
      for (int col = 0; col < cols; col++)
      {
        // Создаём инстанс префаба плитки внутри контейнера gameTransform.
        Transform piece = Instantiate(piecePrefab, gameTransform);
        // Сохраняем ссылку в списке для управления состоянием игры.
        pieces.Add(piece);

        // Вычисляем позицию плитки для прямоугольной сетки с учетом aspect ratio
        piece.localPosition = new Vector3(
          -scaleX + (2 * widthX * col) + widthX,
          +scaleY - (2 * widthY * row) - widthY,
          0);

        // Устанавливаем масштаб плитки с учетом разных размеров по осям
        float tileSizeX = (2 * widthX) - gapThickness;
        float tileSizeY = (2 * widthY) - gapThickness;
        piece.localScale = new Vector3(tileSizeX, tileSizeY, 1);

        // Присваиваем читаемое имя в виде индекса (row * cols + col).
        piece.name = $"{(row * cols) + col}";

        // Настраиваем UV‑координаты для ВСЕХ плиток (убрали специальную обработку "нижней правой")
        float uvGap = 0f;

        Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
        Vector2[] uv = new Vector2[4];

        float uvX = uvWidthX * col;
        float uvY = uvWidthY * row;

        uv[0] = new Vector2(uvX + uvGap, 1f - (uvY + uvWidthY - uvGap));
        uv[1] = new Vector2(uvX + uvWidthX - uvGap, 1f - (uvY + uvWidthY - uvGap));
        uv[2] = new Vector2(uvX + uvGap, 1f - (uvY + uvGap));
        uv[3] = new Vector2(uvX + uvWidthX - uvGap, 1f - (uvY + uvGap));

        mesh.uv = uv;
      }
    }

    // Выбираем случайную плитку, которую сделаем пустой, и деактивируем её.
    // Это гарантирует, что пустая плитка на старте всегда случайная.
    if (pieces.Count > 0)
    {
      emptyLocation = Random.Range(0, pieces.Count);
      pieces[emptyLocation].gameObject.SetActive(false);
    }

    // Вычисляем эталонное расстояние между соседними плитками (одна клетка).
    // Ищем простого соседа: индекс 1 (если в той же строке) или индекс cols (следующая строка).
    if (pieces.Count > 1)
    {
      int neighborIndex = 1;
      if (cols > 1 && (1 / cols) != 0) // безопасный выбор: если cols>1, индекс 1 будет соседним по столбцу
      {
        neighborIndex = 1;
      }
      else if (pieces.Count > cols)
      {
        neighborIndex = cols;
      }
      else
      {
        neighborIndex = 1;
      }

      // Защита на случай некорректных индексов
      if (neighborIndex >= pieces.Count) neighborIndex = Mathf.Clamp(neighborIndex, 0, pieces.Count - 1);

      singleTileDistance = Vector3.Distance(pieces[0].localPosition, pieces[neighborIndex].localPosition);
      if (singleTileDistance <= 0f) singleTileDistance = 1f; // fallback
    }
    else
    {
      singleTileDistance = 1f;
    }
  }

  // Start is called before the first frame update
  void Start()
  {
    pieces = new List<Transform>();

    // Инициализируем генератор случайных чисел с уникальным seed на основе времени
    Random.InitState(System.DateTime.Now.Millisecond + System.DateTime.Now.Second * 1000);

    // Защитная проверка: не позволяем задать значения меньше 2 через инспектор.
    rows = Mathf.Max(2, rows);
    cols = Mathf.Max(2, cols);

    CreateGamePieces(0.01f);

    // Выполняем перемешивание один раз при старте
    StartCoroutine(ShuffleAndSolve());
  }

  /// <summary>
  /// Метод Update вызывается каждый кадр.
  /// </summary>
  void Update()
  {
    // Можно добавить сюда логику автоматического решения пазла
  }

  /// <summary>
  /// Пытается выполнить обмен плитки в позиции <paramref name="i"/> с плиткой, находящейся
  /// на позиции с учётом offset, если это допустимый ход в текущем состоянии поля.
  /// 
  /// Поддерживает оборачивание по вертикали и горизонтали для прямоугольной сетки rows x cols.
  /// </summary>
  /// <param name="i">Индекс плитки в списке pieces, которую мы пытаемся переместить.</param>
  /// <param name="offset">Смещение для поиска целевой (соседней) ячейки: -cols (вверх), +cols (вниз), -1 (влево), +1 (вправо).</param>
  /// <param name="colCheck">Не используется при оборачивании, оставлен для обратной совместимости.</param>
  private bool SwapIfValid(int i, int offset, int colCheck)
  {
    int target;

    // Вертикальное перемещение (вверх/вниз с оборачиванием)
    if (Mathf.Abs(offset) == cols)
    {
      // Корректно обрабатываем отрицательные offset для оборачивания
      target = ((i + offset) % pieces.Count + pieces.Count) % pieces.Count;
    }
    // Горизонтальное перемещение (влево/вправо с оборачиванием)
    else if (Mathf.Abs(offset) == 1)
    {
      int currentRow = i / cols;
      int currentCol = i % cols;

      // Вычисляем новый столбец с оборачиванием (корректно обрабатываем отрицательные значения)
      int newCol = ((currentCol + offset) % cols + cols) % cols;
      target = currentRow * cols + newCol;
    }
    else
    {
      return false;
    }

    if (target == emptyLocation)
    {
      StartCoroutine(AnimateSwap(i, target));
      return true;
    }

    return false;
  }

  /// <summary>
  /// Анимирует плавное перемещение плитки из позиции index1 в позицию index2
  /// При оборачивании (крайняя колонка/строка) делает анимацию через край:
  /// 1) двигает плитку к краю (с небольшим выходом),
  /// 2) телепортирует на противоположный край (за пределом),
  /// 3) добирает до целевой ячейки.
  /// </summary>
  private IEnumerator AnimateSwap(int index1, int index2)
  {
    isAnimating = true;

    Transform piece1 = pieces[index1];
    Transform piece2 = pieces[index2];

    if (piece1 == null || piece2 == null)
    {
      isAnimating = false;
      yield break;
    }

    Vector3 startPos = piece1.localPosition;
    Vector3 endPos = piece2.localPosition;

    // Подготовим параметры сетки (те же, что используются в CreateGamePieces)
    float aspectRatio = (float)cols / (float)rows;
    float scaleX, scaleY;
    if (aspectRatio > 1f) { scaleX = 1f; scaleY = 1f / aspectRatio; }
    else { scaleX = aspectRatio; scaleY = 1f; }
    float widthX = scaleX / (float)cols;
    float widthY = scaleY / (float)rows;

    float minX = -scaleX + widthX;
    float maxX = scaleX - widthX;
    float minY = -scaleY + widthY;
    float maxY = scaleY - widthY;

    // расчёт overshoot (насколько уходим за край)
    float overshoot = Mathf.Max(0.1f * singleTileDistance, singleTileDistance * 0.5f);

    // координаты плиток в сетке
    int row1 = index1 / cols;
    int col1 = index1 % cols;
    int row2 = index2 / cols;
    int col2 = index2 % cols;

    bool horizontalWrap = (row1 == row2) && (Mathf.Abs(col1 - col2) == cols - 1);
    bool verticalWrap = (col1 == col2) && (Mathf.Abs(row1 - row2) == rows - 1);

    // Хелпер: вычислить длительность для данного расстояния, масштабируя относительно singleTileDistance
    float CalcDurationForDistance(float distance)
    {
      float baseDur = moveDuration;
      if (singleTileDistance > 1e-5f)
        baseDur = moveDuration * (distance / singleTileDistance);
      return Mathf.Max(0.02f, baseDur);
    }

    // Если оборачивания нет — обычная одна Lerp анимация
    if (!horizontalWrap && !verticalWrap)
    {
      float distance = Vector3.Distance(startPos, endPos);
      float duration = CalcDurationForDistance(distance);

      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        t = t * t * (3f - 2f * t);
        piece1.localPosition = Vector3.Lerp(startPos, endPos, t);
        yield return null;
      }

      piece1.localPosition = endPos;
      piece2.localPosition = startPos;

      (pieces[index1], pieces[index2]) = (pieces[index2], pieces[index1]);
      emptyLocation = index1;

      isAnimating = false;
      yield break;
    }

    // Обрабатываем оборачивание через край: два сегмента
    if (horizontalWrap)
    {
      // определяем направление оборачивания
      bool wrapLeft = (col1 == 0 && col2 == cols - 1);
      bool wrapRight = (col1 == cols - 1 && col2 == 0);

      Vector3 edgePosOut;   // позиция за текущим краем, до которой анимируем
      Vector3 edgePosIn;    // позиция с противоположного края, откуда анимируем дальше

      if (wrapLeft)
      {
        edgePosOut = new Vector3(minX - overshoot, startPos.y, startPos.z);
        edgePosIn = new Vector3(maxX + overshoot, endPos.y, endPos.z);
      }
      else // wrapRight
      {
        edgePosOut = new Vector3(maxX + overshoot, startPos.y, startPos.z);
        edgePosIn = new Vector3(minX - overshoot, endPos.y, endPos.z);
      }

      // сегмент 1
      float dist1 = Vector3.Distance(startPos, edgePosOut);
      float dur1 = CalcDurationForDistance(dist1);
      float e1 = 0f;
      while (e1 < dur1)
      {
        e1 += Time.deltaTime;
        float t = Mathf.Clamp01(e1 / dur1);
        t = t * t * (3f - 2f * t);
        piece1.localPosition = Vector3.Lerp(startPos, edgePosOut, t);
        yield return null;
      }

      // телепортируем на противоположный край (edgePosIn) — имитируем прохождение через границу
      piece1.localPosition = edgePosIn;

      // сегмент 2: от edgePosIn до endPos
      float dist2 = Vector3.Distance(edgePosIn, endPos);
      float dur2 = CalcDurationForDistance(dist2);
      float e2 = 0f;
      Vector3 segStart = piece1.localPosition;
      while (e2 < dur2)
      {
        e2 += Time.deltaTime;
        float t = Mathf.Clamp01(e2 / dur2);
        t = t * t * (3f - 2f * t);
        piece1.localPosition = Vector3.Lerp(segStart, endPos, t);
        yield return null;
      }

      // Завершаем
      piece1.localPosition = endPos;
      piece2.localPosition = startPos;

      (pieces[index1], pieces[index2]) = (pieces[index2], pieces[index1]);
      emptyLocation = index1;

      isAnimating = false;
      yield break;
    }

    if (verticalWrap)
    {
      // определяем направление оборачивания по вертикали
      bool wrapUp = (row1 == 0 && row2 == rows - 1);     // идём "вверх" через верхний край к нижней строке
      bool wrapDown = (row1 == rows - 1 && row2 == 0);   // идём "вниз" через нижний край к верхней строке

      Vector3 edgePosOut;
      Vector3 edgePosIn;

      if (wrapUp)
      {
        edgePosOut = new Vector3(startPos.x, maxY + overshoot, startPos.z);
        edgePosIn = new Vector3(endPos.x, minY - overshoot, endPos.z);
      }
      else // wrapDown
      {
        edgePosOut = new Vector3(startPos.x, minY - overshoot, startPos.z);
        edgePosIn = new Vector3(endPos.x, maxY + overshoot, endPos.z);
      }

      // сегмент 1
      float dist1 = Vector3.Distance(startPos, edgePosOut);
      float dur1 = CalcDurationForDistance(dist1);
      float e1 = 0f;
      while (e1 < dur1)
      {
        e1 += Time.deltaTime;
        float t = Mathf.Clamp01(e1 / dur1);
        t = t * t * (3f - 2f * t);
        piece1.localPosition = Vector3.Lerp(startPos, edgePosOut, t);
        yield return null;
      }

      // телепортируем на противоположный край
      piece1.localPosition = edgePosIn;

      // сегмент 2
      float dist2 = Vector3.Distance(edgePosIn, endPos);
      float dur2 = CalcDurationForDistance(dist2);
      float e2 = 0f;
      Vector3 segStart = piece1.localPosition;
      while (e2 < dur2)
      {
        e2 += Time.deltaTime;
        float t = Mathf.Clamp01(e2 / dur2);
        t = t * t * (3f - 2f * t);
        piece1.localPosition = Vector3.Lerp(segStart, endPos, t);
        yield return null;
      }

      // Завершаем
      piece1.localPosition = endPos;
      piece2.localPosition = startPos;

      (pieces[index1], pieces[index2]) = (pieces[index2], pieces[index1]);
      emptyLocation = index1;

      isAnimating = false;
      yield break;
    }

    // На всякий случай — fallback к прежнему поведению (не должно наступать)
    {
      float distance = Vector3.Distance(startPos, endPos);
      float duration = CalcDurationForDistance(distance);
      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        t = t * t * (3f - 2f * t);
        piece1.localPosition = Vector3.Lerp(startPos, endPos, t);
        yield return null;
      }
      piece1.localPosition = endPos;
      piece2.localPosition = startPos;
      (pieces[index1], pieces[index2]) = (pieces[index2], pieces[index1]);
      emptyLocation = index1;
      isAnimating = false;
    }
  }

  // We name the pieces in order so we can use this to check completion.
  private bool CheckCompletion()
  {
    for (int i = 0; i < pieces.Count; i++)
    {
      if (pieces[i].name != $"{i}")
      {
        return false;
      }
    }
    return true;
  }

  /// <summary>
  /// Перемешивает поле плиток для прямоугольной сетки rows x cols.
  /// </summary>
  private void Shuffle()
  {
    // Перемешивание в памяти: работаем с копией списка плиток, чтобы ничего не было видно на сцене.
    int count = 0;
    Queue<int> recentPositions = new Queue<int>(); // История последних N позиций

    // Копия текущих плиток (ссылка на те же Transform, но порядок меняется только в tempPieces)
    List<Transform> tempPieces = new List<Transform>(pieces);
    int tempEmpty = emptyLocation;

    // Очищаем список записанных ходов перед новым перемешиванием
    recordedMoves.Clear();

    // Обновленная длина перемешивания для прямоугольной сетки
    int targetCountSwaps = (int)(solveTimeInSeconds / moveDuration);
    while (count < targetCountSwaps)
    {
      int total = tempPieces.Count;

      // Вычисляем индексы ЧЕТЫРЕХ соседей, которые могут переместиться в tempEmpty (с учётом оборачивания)
      int row = tempEmpty / cols;
      int col = tempEmpty % cols;

      int upNeighbor = (tempEmpty + cols) % total; // piece that would move up (offset -cols)
      int downNeighbor = ((tempEmpty - cols) % total + total) % total; // (offset +cols)
      int leftNeighbor = row * cols + ((col + 1) % cols); // piece right of empty moves left (offset -1)
      int rightNeighbor = row * cols + ((col - 1 + cols) % cols); // piece left of empty moves right (offset +1)

      // Список кандидатов (index, offset, colCheck)
      (int idx, int offset, int colCheck)[] candidates = new (int, int, int)[]
      {
        (upNeighbor, -cols, cols),
        (downNeighbor, +cols, cols),
        (leftNeighbor, -1, 0),
        (rightNeighbor, +1, cols - 1)
      };

      // Перемешиваем порядок кандидатов, чтобы не всегда брать в одном порядке
      for (int i = 0; i < candidates.Length; i++)
      {
        int j = Random.Range(0, candidates.Length);
        var tmp = candidates[i];
        candidates[i] = candidates[j];
        candidates[j] = tmp;
      }

      bool swapped = false;
      int chosenRnd = -1;
      int chosenTarget = -1;

      // Пробуем в перемешанном порядке соседей
      foreach (var cand in candidates)
      {
        int candidateIndex = cand.idx;
        // Пропускаем кандидата, если он в истории
        if (recentPositions.Contains(candidateIndex)) continue;

        int target;
        if (SwapIfValidInstantMemory(candidateIndex, cand.offset, cand.colCheck, tempPieces, ref tempEmpty, out target))
        {
          recordedMoves.Add(target);
          swapped = true;
          chosenRnd = candidateIndex;
          chosenTarget = target;
          break;
        }
      }

      if (swapped)
      {
        count++;

        // Проверяем, не заблокируем ли мы все возможные ходы после добавления chosenRnd в историю
        Queue<int> testQueue = new Queue<int>(recentPositions);
        testQueue.Enqueue(chosenRnd);
        if (testQueue.Count > shuffleHistorySize)
        {
          testQueue.Dequeue();
        }

        // Пересчитываем соседей для новой текущей пустой позиции (tempEmpty уже обновлён SwapIfValidInstantMemory)
        int upPos = (tempEmpty + cols) % total;
        int downPos = ((tempEmpty - cols) % total + total) % total;
        int currentRow = tempEmpty / cols;
        int currentCol = tempEmpty % cols;
        int leftCol = ((currentCol + 1) % cols);
        int rightCol = ((currentCol - 1 + cols) % cols);
        int leftPos = currentRow * cols + leftCol;
        int rightPos = currentRow * cols + rightCol;

        bool allBlocked = testQueue.Contains(upPos) &&
                          testQueue.Contains(downPos) &&
                          testQueue.Contains(leftPos) &&
                          testQueue.Contains(rightPos);

        // Добавляем в историю только если не все ходы будут заблокированы
        if (!allBlocked)
        {
          recentPositions.Enqueue(chosenRnd);

          // Если история превысила лимит, удаляем самую старую позицию
          if (recentPositions.Count > shuffleHistorySize)
          {
            recentPositions.Dequeue();
          }
        }
        else
        {
          // Если добавление заблокирует все ходы — не добавляем, но если история слишком большая и дальнейший прогресс невозможен,
          // снимаем старый элемент, чтобы не застрять.
          if (recentPositions.Count > 0)
          {
            recentPositions.Dequeue();
          }
        }
      }
      else
      {
        // Ни один сосед не подошёл (скорее всего все в recentPositions) — освобождаем самый старый элемент истории
        // чтобы гарантировать возможность хода и избежать бесконечного цикла.
        if (recentPositions.Count > 0)
        {
          recentPositions.Dequeue();
        }
        else
        {
          // Защита на случай неожиданной ситуации: делаем произвольный проход по всем элементам и пытаемся найти возможный swap.
          bool forced = false;
          for (int i = 0; i < total && !forced; i++)
          {
            int target;
            if (SwapIfValidInstantMemory(i, -cols, cols, tempPieces, ref tempEmpty, out target) ||
                SwapIfValidInstantMemory(i, +cols, cols, tempPieces, ref tempEmpty, out target) ||
                SwapIfValidInstantMemory(i, -1, 0, tempPieces, ref tempEmpty, out target) ||
                SwapIfValidInstantMemory(i, +1, cols - 1, tempPieces, ref tempEmpty, out target))
            {
              recordedMoves.Add(target);
              count++;
              forced = true;
            }
          }
          // Если и это не помогло — просто выходим из цикла, чтобы не зависнуть
          if (!forced) break;
        }
      }

    }

    // После завершения перемешивания применяем итоговый порядок к реальным плиткам (без анимаций).
    // Вычислим параметры сетки аналогично CreateGamePieces (используем тот же gapThickness).
    float gapThickness = 0.01f;
    float aspectRatio = (float)cols / (float)rows;
    float scaleX, scaleY;
    if (aspectRatio > 1f) { scaleX = 1f; scaleY = 1f / aspectRatio; }
    else { scaleX = aspectRatio; scaleY = 1f; }
    float widthX = scaleX / (float)cols;
    float widthY = scaleY / (float)rows;

    // Применяем порядок: для каждого слота i устанавливаем соответствующий Transform из tempPieces[i]
    pieces = tempPieces;
    for (int i = 0; i < pieces.Count; i++)
    {
      Transform piece = pieces[i];
      if (piece == null) continue;

      int row = i / cols;
      int col = i % cols;

      piece.localPosition = new Vector3(
        -scaleX + (2 * widthX * col) + widthX,
        +scaleY - (2 * widthY * row) - widthY,
        0);

      float tileSizeX = (2 * widthX) - gapThickness;
      float tileSizeY = (2 * widthY) - gapThickness;
      piece.localScale = new Vector3(tileSizeX, tileSizeY, 1);

      // Активируем все плитки, затем скроем пустую
      piece.gameObject.SetActive(true);
    }

    // Устанавливаем пустую позицию и скрываем соответствующую плитку
    emptyLocation = tempEmpty;
    if (emptyLocation >= 0 && emptyLocation < pieces.Count)
    {
      pieces[emptyLocation].gameObject.SetActive(false);
    }

    // Выводим статистику по завершившемуся перемешиванию
    LogShuffleSummary();
  }

  // Выводит в консоль краткую статистику перемешивания:
  // - сколько ходов было выполнено,
  // - сколько плиток сейчас не на своих местах,
  // - общее количество плиток.
  private void LogShuffleSummary()
  {
    int movesPerformed = recordedMoves != null ? recordedMoves.Count : 0;
    int total = pieces != null ? pieces.Count : 0;
    int misplaced = 0;
    if (pieces != null)
    {
      for (int i = 0; i < pieces.Count; i++)
      {
        var p = pieces[i];
        if (p == null) continue;
        if (p.name != $"{i}") misplaced++;
      }
    }
    Debug.Log($"Shuffle finished: moves={movesPerformed}, misplaced={misplaced}, total={total}");
  }

  /// <summary>
  /// Мгновенное переставление в памяти — меняет порядок в tempPieces и обновляет tempEmpty, но не трогает transforms.
  /// Поддерживает оборачивание по вертикали и горизонтали.
  /// </summary>
  private bool SwapIfValidInstantMemory(int i, int offset, int colCheck, List<Transform> tempPieces, ref int tempEmpty, out int calculatedTarget)
  {
    calculatedTarget = -1;
    int target;

    // Вертикальное перемещение (вверх/вниз с оборачиванием)
    if (Mathf.Abs(offset) == cols)
    {
      // Корректно обрабатываем отрицательные offset для оборачивания
      target = ((i + offset) % tempPieces.Count + tempPieces.Count) % tempPieces.Count;
    }
    // Горизонтальное перемещение (влево/вправо с оборачиванием)
    else if (Mathf.Abs(offset) == 1)
    {
      int currentRow = i / cols;
      int currentCol = i % cols;

      // Вычисляем новый столбец с оборачиванием (корректно обрабатываем отрицательные значения)
      int newCol = ((currentCol + offset) % cols + cols) % cols;
      target = currentRow * cols + newCol;
    }
    else
    {
      return false;
    }

    if (target == tempEmpty)
    {
      // Меняем местами элементы в списке: плитку и пустую позицию
      (tempPieces[i], tempPieces[target]) = (tempPieces[target], tempPieces[i]);
      tempEmpty = i;
      calculatedTarget = target;
      return true;
    }

    return false;
  }

  /// <summary>
  /// Корутина для перемешивания и последующей автоматической сборки
  /// </summary>
  private IEnumerator ShuffleAndSolve()
  {
    // Перемешиваем и записываем ходы
    Shuffle();

    // Небольшая пауза перед началом сборки
    yield return new WaitForSeconds(0.5f);

    // Рассчитываем длительность одного хода для укладывания в заданное время
    if (recordedMoves.Count > 0)
    {
      moveDuration = solveTimeInSeconds / recordedMoves.Count;
    }

    // Запускаем автоматическую сборку
    yield return StartCoroutine(AutoSolve());
  }

  /// <summary>
  /// Автоматически решает пазл, воспроизводя записанные ходы в обратном порядке
  /// </summary>
  private IEnumerator AutoSolve()
  {
    // Проигрываем ходы в обратном порядке
    for (int i = recordedMoves.Count - 1; i >= 0; i--)
    {
      int pieceIndex = recordedMoves[i];

      // Ждём завершения текущей анимации
      while (isAnimating)
      {
        // пауза корутины до следующего кадра
        yield return null;
      }

      TryMovePiece(pieceIndex);
      // пауза корутины до следующего кадра
      yield return null;
    }


    // показываем пустую плитку и настраиваем её UV, чтобы она отображала свою часть текстуры
    RevealEmptyTile();
  }

  // Показывает пустую плитку и настраивает её UV на ту часть текстуры, которая соответствует её исходному индексу.
  private void RevealEmptyTile()
  {
    if (emptyLocation < 0 || emptyLocation >= pieces.Count) return;

    Transform piece = pieces[emptyLocation];
    if (piece == null) return;

    // Если плитка уже активна — ничего не делаем
    if (piece.gameObject.activeSelf) return;

    // Вычисляем UV точно так же, как в CreateGamePieces
    float uvWidthX = 1f / (float)cols;
    float uvWidthY = 1f / (float)rows;
    float uvGap = 0f;

    // Имя плитки содержит её исходный индекс
    if (!int.TryParse(piece.name, out int originalIndex)) originalIndex = 0;
    int row = originalIndex / cols;
    int col = originalIndex % cols;

    MeshFilter mf = piece.GetComponent<MeshFilter>();
    if (mf != null && mf.mesh != null)
    {
      Mesh mesh = mf.mesh;
      Vector2[] uv = new Vector2[4];

      float uvX = uvWidthX * col;
      float uvY = uvWidthY * row;

      uv[0] = new Vector2(uvX + uvGap, 1f - (uvY + uvWidthY - uvGap));
      uv[1] = new Vector2(uvX + uvWidthX - uvGap, 1f - (uvY + uvWidthY - uvGap));
      uv[2] = new Vector2(uvX + uvGap, 1f - (uvY + uvGap));
      uv[3] = new Vector2(uvX + uvWidthX - uvGap, 1f - (uvY + uvGap));

      mesh.uv = uv;
    }

    piece.gameObject.SetActive(true);
  }

  /// <summary>
  /// Пытается переместить плитку с индексом pieceIndex
  /// </summary>
  private bool TryMovePiece(int pieceIndex)
  {
    return SwapIfValid(pieceIndex, -cols, cols) ||
           SwapIfValid(pieceIndex, +cols, cols) ||
           SwapIfValid(pieceIndex, -1, 0) ||
           SwapIfValid(pieceIndex, +1, cols - 1);
  }

  // Автоматическая корректировка значений в редакторе (и при изменении в инспекторе).
  private void OnValidate()
  {
    // Если по какой-то причине в инспекторе оказалось 0 или отрицательное — вернуть удобные дефолты.
    if (rows <= 0) rows = 16;
    if (cols <= 0) cols = 9;

    // Обеспечить минимальное значение 2.
    rows = Mathf.Max(2, rows);
    cols = Mathf.Max(2, cols);
  }
}

