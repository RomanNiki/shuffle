using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{
    [SerializeField] private GridItem gridItem;
    [SerializeField] private Transform grid;

    void Start()
    {
        Random.InitState(1032109982);
        var matrix = MatrixFluidFillAlgorithm(10, 10, 3, 100);

        Dictionary<int, Color> mapColors = new Dictionary<int, Color>();

        var colors = GenerateColors(100);
        for (int i = 0; i < 100; i++)
        {
            mapColors.Add(i, colors[i]);
        }

        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                var item = Instantiate(gridItem, grid);
                var mItem = matrix[i, j];
                item.Setup(mapColors[mItem], new Vector2Int(i,j));
            }
        }
    }
    
    List<Color> GenerateColors(int count)
    {
        HashSet<Color> uniqueColors = new HashSet<Color>();
        
        while (uniqueColors.Count < count)
        {
            // Генерируем случайный цвет
            Color color = new Color(Random.value, Random.value, Random.value);
            uniqueColors.Add(color);
        }

        return new List<Color>(uniqueColors);
    }

static readonly (int, int)[] DIRECTIONS = new (int, int)[]
{
    (0, 1),  // Right
    (1, 0),  // Down
    (0, -1), // Left
    (-1, 0)  // Up
};

static bool IsWithinBounds(int x, int y, int rows, int cols)
{
    return x >= 0 && x < rows && y >= 0 && y < cols;
}

static List<(int, int)> FindAdjacentEmpty(int[,] matrix, int x, int y)
{
    List<(int, int)> adjacent = new List<(int, int)>();
    int rows = matrix.GetLength(0);
    int cols = matrix.GetLength(1);

    foreach (var (dx, dy) in DIRECTIONS)
    {
        int nx = x + dx;
        int ny = y + dy;
        if (IsWithinBounds(nx, ny, rows, cols) && matrix[nx, ny] == 0)
        {
            adjacent.Add((nx, ny));
        }
    }

    return adjacent;
}

// Проверка наличия свободного пространства для группы размера n
static bool CanFitGroup(int[,] matrix, int x, int y, int n)
{
    Queue<(int, int)> queue = new Queue<(int, int)>();
    HashSet<(int, int)> visited = new HashSet<(int, int)>();
    queue.Enqueue((x, y));
    visited.Add((x, y));
    int count = 1;

    while (queue.Count > 0 && count < n)
    {
        var (cx, cy) = queue.Dequeue();
        foreach (var (nx, ny) in FindAdjacentEmpty(matrix, cx, cy))
        {
            if (!visited.Contains((nx, ny)))
            {
                visited.Add((nx, ny));
                queue.Enqueue((nx, ny));
                count++;
                if (count == n)
                    return true; // Достаточно места для группы
            }
        }
    }

    return count >= n; // Возвращает true, если нашлось достаточно места
}

static int PlaceGroup(int[,] matrix, int groupNum, int n, int startX, int startY)
{
    matrix[startX, startY] = groupNum;
    int groupSize = 1;
    var toFill = new List<(int, int)> { (startX, startY) };

    while (groupSize < n && toFill.Count > 0)
    {
        // Выбираем случайную клетку из текущих заполненных
        int index = Random.Range(0, toFill.Count);
        (int x, int y) = toFill[index];
        var adjacent = FindAdjacentEmpty(matrix, x, y);

        if (adjacent.Count > 0)
        {
            // Заполняем одну из соседних пустых клеток
            var (nx, ny) = adjacent[Random.Range(0, adjacent.Count)];
            matrix[nx, ny] = groupNum;
            toFill.Add((nx, ny));
            groupSize++;
        }

        // Удаляем клетку, если все её соседи заполнены
        if (FindAdjacentEmpty(matrix, x, y).Count == 0)
        {
            toFill.RemoveAt(index);
        }
    }

    return groupSize;
}

static int[,] MatrixFluidFillAlgorithm(int rows, int cols, int n, float p)
{
    // Инициализация матрицы нулями
    int[,] matrix = new int[rows, cols];

    // Расчет общего числа элементов, которые нужно заполнить на основе процента p
    int totalElements = rows * cols;
    int totalFilled = (int)Mathf.Floor(p / 100 * totalElements / n) * n;

    // Номер группы
    int groupNum = 1;
    int filledElements = 0;

    while (filledElements < totalFilled)
    {
        // Получаем список всех пустых клеток
        List<(int, int)> emptyCells = new List<(int, int)>();
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (matrix[i, j] == 0)
                {
                    emptyCells.Add((i, j));
                }
            }
        }

        if (emptyCells.Count == 0)
        {
            break; // Нет больше свободных клеток
        }

        // Выбираем случайную пустую клетку для начала новой группы
        var (startX, startY) = emptyCells[Random.Range(0, emptyCells.Count)];

        // Проверяем, можно ли разместить группу размера n, начиная с этой клетки
        if (CanFitGroup(matrix, startX, startY, n))
        {
            // Размещаем группу
            int groupSize = PlaceGroup(matrix, groupNum, n, startX, startY);

            // Обновляем счетчики
            filledElements += groupSize;
            groupNum++;
        }
        else
        {
            // Если не можем разместить группу, убираем эту клетку из списка
            emptyCells.Remove((startX, startY));
        }
    }

    return matrix;
}
}