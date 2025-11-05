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
  // Время перемещения плитки в секундах
  private float moveDuration = 0.05f;

  private bool isAnimating = false;
  private List<int> recordedMoves = new List<int>();

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
  /// </summary>
  private IEnumerator AnimateSwap(int index1, int index2)
  {
    isAnimating = true;

    Transform piece1 = pieces[index1];
    Transform piece2 = pieces[index2];

    Vector3 startPos = piece1.localPosition;
    Vector3 endPos = piece2.localPosition;

    float elapsed = 0f;

    // Плавное перемещение с использованием Lerp
    while (elapsed < moveDuration)
    {
      elapsed += Time.deltaTime;
      float t = Mathf.Clamp01(elapsed / moveDuration);

      // Можно использовать SmoothStep для более плавного движения
      t = t * t * (3f - 2f * t);

      piece1.localPosition = Vector3.Lerp(startPos, endPos, t);
      yield return null;
    }

    // Гарантируем точную конечную позицию
    piece1.localPosition = endPos;

    // также устанавливаем позицию пустой плитки (piece2)
    piece2.localPosition = startPos;

    // Обновляем логическое состояние после завершения анимации
    (pieces[index1], pieces[index2]) = (pieces[index2], pieces[index1]);
    emptyLocation = index1;

    isAnimating = false;
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
    int last = -1;
    int prevLast = -1; // Добавляем память на два хода

    // Копия текущих плиток (ссылка на те же Transform, но порядок меняется только в tempPieces)
    List<Transform> tempPieces = new List<Transform>(pieces);
    int tempEmpty = emptyLocation;

    // Очищаем список записанных ходов перед новым перемешиванием
    recordedMoves.Clear();

    // Обновленная длина перемешивания для прямоугольной сетки
    int targetCountSwaps = (int)(solveTimeInSeconds / moveDuration);
    while (count < targetCountSwaps)
    {
      int rnd = Random.Range(0, rows * cols);
      if (rnd == last || rnd == prevLast) continue;

      bool swapped = false;
      int target = -1;

      // Вверх с оборачиванием
      if (SwapIfValidInstantMemory(rnd, -cols, cols, tempPieces, ref tempEmpty, out target))
      {
        recordedMoves.Add(target);
        swapped = true;
      }
      // Вниз с оборачиванием
      else if (SwapIfValidInstantMemory(rnd, +cols, cols, tempPieces, ref tempEmpty, out target))
      {
        recordedMoves.Add(target);
        swapped = true;
      }
      // Влево с оборачиванием
      else if (SwapIfValidInstantMemory(rnd, -1, 0, tempPieces, ref tempEmpty, out target))
      {
        recordedMoves.Add(target);
        swapped = true;
      }
      // Вправо с оборачиванием
      else if (SwapIfValidInstantMemory(rnd, +1, cols - 1, tempPieces, ref tempEmpty, out target))
      {
        recordedMoves.Add(target);
        swapped = true;
      }

      if (swapped)
      {
        count++;
        prevLast = last;
        last = rnd;
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

