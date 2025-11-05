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
  private bool shuffling = false;

  // Время перемещения плитки в секундах
  private float moveDuration = 0.05f;
  private bool isAnimating = false;

  // Запись ходов и автоматическое решение
  [SerializeField, Tooltip("Время в секундах для автоматической сборки пазла")]
  private float solveTimeInSeconds = 10f;
  private List<int> recordedMoves = new List<int>();
  private bool isSolving = false;

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

        // Делаем нижнюю правую плитку пустой — отключаем объект и сохраняем
        // её индекс в emptyLocation (последний индекс в массиве плиток).
        if ((row == rows - 1) && (col == cols - 1))
        {
          emptyLocation = (rows * cols) - 1;
          piece.gameObject.SetActive(false);
        }
        else
        {
          // Настраиваем UV‑координаты для прямоугольной сетки
          float gap = gapThickness / 2;

          // Получаем Mesh плитки и создаём новый массив UV для 4 вершин.
          Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
          Vector2[] uv = new Vector2[4];

          uv[0] = new Vector2((widthX * col) + gap, 1 - ((widthY * (row + 1)) - gap));
          uv[1] = new Vector2((widthX * (col + 1)) - gap, 1 - ((widthY * (row + 1)) - gap));
          uv[2] = new Vector2((widthX * col) + gap, 1 - ((widthY * row) + gap));
          uv[3] = new Vector2((widthX * (col + 1)) - gap, 1 - ((widthY * row) + gap));

          mesh.uv = uv;
        }
      }
    }
  }

  // Start is called before the first frame update
  void Start()
  {
    pieces = new List<Transform>();

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
  /// на позиции <c>i + offset</c>, если это допустимый ход в текущем состоянии поля.
  /// 
  /// Обновлено для работы с прямоугольной сеткой rows x cols.
  /// </summary>
  /// <param name="i">Индекс плитки в списке pieces, которую мы пытаемся переместить.</param>
  /// <param name="offset">Смещение для поиска целевой (соседней) ячейки: -cols (вверх), +cols (вниз), -1 (влево), +1 (вправо).</param>
  /// <param name="colCheck">
  /// Значение для проверки столбца, используемое для предотвращения горизонтального оборачивания.
  /// Для вертикальных ходов это поле обычно равно cols.
  /// </param>
  private bool SwapIfValid(int i, int offset, int colCheck)
  {
    // Обновленная проверка для прямоугольной сетки
    if (((i % cols) != colCheck) && ((i + offset) == emptyLocation))
    {
      // Запускаем анимацию вместо мгновенной смены позиций
      StartCoroutine(AnimateSwap(i, i + offset));
      return true;
    }

    // Если любое из условий не выполнено — ход недопустим.
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

  private IEnumerator WaitShuffle(float duration)
  {
    yield return new WaitForSeconds(duration);
    Shuffle();
    shuffling = false;
  }

  /// <summary>
  /// Перемешивает поле плиток для прямоугольной сетки rows x cols.
  /// </summary>
  private void Shuffle()
  {
    int count = 0;
    int last = 0;

    // Очищаем список записанных ходов перед новым перемешиванием
    recordedMoves.Clear();

    // Обновленная длина перемешивания для прямоугольной сетки
    while (count < (rows * cols * Mathf.Max(rows, cols)))
    {
      // Выбираем случайную позицию в прямоугольной сетке
      int rnd = Random.Range(0, rows * cols);

      if (rnd == last) { continue; }

      last = emptyLocation;

      // Для перемешивания можно использовать мгновенное перемещение
      // или сделать быструю анимацию, сохранив старую версию SwapIfValidInstant
      if (SwapIfValidInstant(rnd, -cols, cols))
      {
        recordedMoves.Add(rnd);
        count++;
      }
      else if (SwapIfValidInstant(rnd, +cols, cols))
      {
        recordedMoves.Add(rnd);
        count++;
      }
      else if (SwapIfValidInstant(rnd, -1, 0))
      {
        recordedMoves.Add(rnd);
        count++;
      }
      else if (SwapIfValidInstant(rnd, +1, cols - 1))
      {
        recordedMoves.Add(rnd);
        count++;
      }
    }
  }

  /// <summary>
  /// Мгновенная версия обмена для перемешивания (без анимации)
  /// </summary>
  private bool SwapIfValidInstant(int i, int offset, int colCheck)
  {
    if (((i % cols) != colCheck) && ((i + offset) == emptyLocation))
    {
      (pieces[i], pieces[i + offset]) = (pieces[i + offset], pieces[i]);
      (pieces[i].localPosition, pieces[i + offset].localPosition) =
        ((pieces[i + offset].localPosition, pieces[i].localPosition));
      emptyLocation = i;
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
    isSolving = true;

    // Проигрываем ходы в обратном порядке
    for (int i = recordedMoves.Count - 1; i >= 0; i--)
    {
      int pieceIndex = recordedMoves[i];

      // Ждём завершения текущей анимации
      while (isAnimating)
      {
        yield return null;
      }

      // Пытаемся переместить плитку (она должна быть рядом с пустым местом)
      if (!TryMovePiece(pieceIndex))
      {
        Debug.LogWarning($"Не удалось переместить плитку {pieceIndex} при автоматической сборке");
      }

      yield return null;
    }

    isSolving = false;
    Debug.Log("Пазл собран автоматически!");
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

