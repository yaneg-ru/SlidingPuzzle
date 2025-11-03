using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

  [SerializeField] private Transform gameTransform;
  [SerializeField] private Transform piecePrefab;

  private List<Transform> pieces;
  private int emptyLocation;
  private int rows;
  private int cols;
  private bool shuffling = false;

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
    rows = 16;
    cols = 9;
    CreateGamePieces(0.01f);
  }

  /// <summary>
  /// Метод Update вызывается каждый кадр и выполняет следующие обязанности:
  /// 1) Проверяет, собран ли пазл (CheckCompletion). Если да — запускает корутину перемешивания
  ///    через короткую задержку (WaitShuffle), чтобы показать победу перед следующим тасованием.
  /// 2) Обрабатывает ввод мыши — одиночный клик левой кнопкой. Выполняется Raycast2D из
  ///    позиции курсора в мир и, если был попадание в плитку, пытается переместить её в пустую
  ///    ячейку проверяя четыре направления (вверх/вниз/влево/вправо).
  /// 
  /// Подробные замечания:
  /// - Переменная shuffling служит для того, чтобы не пытаться распознавать победу/перетасовывать
  ///   во время уже текущей операции тасовки.
  /// - SwapIfValid выполняет проверку корректности хода (включая предотвращение горизонтального
  ///   "оборачивания" через границу) и обновляет состояние pieces и emptyLocation при успешном ходе.
  /// - Raycast2D используется с направлением Vector2.zero потому, ///   что мы хотим получить попадание по точке (реже — можно было бы использовать Physics2D.OverlapPoint).
  /// - colCheck параметр для SwapIfValid ограничивает допустимые горизонтальные ходы (см. реализацию SwapIfValid).
  /// </summary>
  void Update()
  {
    // 1) Проверка завершения и запуск тасовки:
    // Если в текущий момент не происходит тасовка и пазл собран — помечаем shuffling=true
    // и запускаем корутину, которая через небольшой интервал вызовет Shuffle().
    if (!shuffling && CheckCompletion())
    {
      shuffling = true; // Блокируем повторный вход, пока идёт подготовка к тасовке.
      StartCoroutine(WaitShuffle(0.5f)); // Даем игроку полсекунды увидеть результат.
    }

    // 2) Обработка клика левой кнопкой мыши:
    // Реагируем только на нажатие, а не на удержание (GetMouseButtonDown(0)).
    if (Input.GetMouseButtonDown(0))
    {
      // Преобразуем экранные координаты курсора в координаты мира и выполняем Raycast2D.
      // Vector2.zero указывает, что луч — это по сути проверка точки.
      RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

      // Если Raycast обнаружил объект (попадание в Collider) — продолжаем.
      if (hit)
      {
        // Проходим по списку плиток. Индекс в списке соответствует позиции в логическом поле.
        // Это позволяет сопоставлять позицию плитки с пустой ячейкой (emptyLocation).
        for (int i = 0; i < pieces.Count; i++)
        {
          // Сравниваем Transform плитки в списке с Transform'ом, в который попал Raycast.
          // Если это та самая плитка — пытаемся совершить ход в 4 направления.
          if (pieces[i] == hit.transform)
          {
            // Обновленные направления для прямоугольной сетки
            if (SwapIfValid(i, -cols, cols)) { break; }       // вверх
            if (SwapIfValid(i, +cols, cols)) { break; }       // вниз
            if (SwapIfValid(i, -1, 0)) { break; }             // влево
            if (SwapIfValid(i, +1, cols - 1)) { break; }      // вправо
          }
        }
      }
    }
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
      // Обмен в состоянии игры: меняем позиции в списке pieces, чтобы логика отражала новый порядок.
      (pieces[i], pieces[i + offset]) = (pieces[i + offset], pieces[i]);

      // Обмен локальных позиций (Transform) у соответствующих объектов, чтобы визуально
      // отобразить перемещение плиток на сцене.
      (pieces[i].localPosition, pieces[i + offset].localPosition) = ((pieces[i + offset].localPosition, pieces[i].localPosition));

      // Обновляем индекс пустой ячейки — теперь она находится в позиции i (туда переместилась плитка).
      emptyLocation = i;

      // Возвращаем true, сигнализируя об успешном ходе.
      return true;
    }

    // Если любое из условий не выполнено — ход недопустим.
    return false;
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

    // Обновленная длина перемешивания для прямоугольной сетки
    while (count < (rows * cols * Mathf.Max(rows, cols)))
    {
      // Выбираем случайную позицию в прямоугольной сетке
      int rnd = Random.Range(0, rows * cols);

      if (rnd == last) { continue; }

      last = emptyLocation;

      // Обновленные направления для прямоугольной сетки
      if (SwapIfValid(rnd, -cols, cols))
      {
        count++;
      }
      else if (SwapIfValid(rnd, +cols, cols))
      {
        count++;
      }
      else if (SwapIfValid(rnd, -1, 0))
      {
        count++;
      }
      else if (SwapIfValid(rnd, +1, cols - 1))
      {
        count++;
      }
    }
  }
}

