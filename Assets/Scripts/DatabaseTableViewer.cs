using UnityEngine;
using UnityEngine.UI;
using System;
using System.Data;
using TMPro;

public class DatabaseTableViewer : MonoBehaviour
{
    [SerializeField] private Transform tableContentParent;
    [SerializeField] private GameObject tableRowPrefab;
    [SerializeField] private GameObject cellPrefab; // Префаб для ячеек данных
    [SerializeField] private GameObject cellTypePrefab; // Префаб для ячеек заголовков
    private DatabaseLoader _databaseLoader;
    
    private DataTable _currentData; // Храним текущие данные
    private string _currentSortColumn; // Текущий столбец сортировки
    private bool _ascending = true; // Направление сортировки (по возрастанию/убыванию)
    private string _currentDBPath; // Текущий путь к БД
    private string _currentTableName; // Текущее имя таблицы
    
    private void Start()
    {
        _databaseLoader = GetComponent<DatabaseLoader>();
    }
    
    // Загрузка данных из выбранной таблицы
    public void LoadTableData(string dbPath, string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            Debug.LogError("Имя таблицы не указано!");
            return;
        }

        _currentDBPath = dbPath;
        _currentTableName = tableName;

        // Очищаем предыдущие строки
        ClearTableContent();

        try
        {
            // Выполняем запрос и получаем данные
            string query = $"SELECT * FROM {tableName} LIMIT 100"; // Ограничение на 100 строк
            _currentData = _databaseLoader.ExecuteQuery(dbPath, query);
            
            // Отображаем данные
            DisplayTableData(_currentData);
            
            Debug.Log($"Загружено {_currentData.Rows.Count} строк из таблицы {tableName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при загрузке данных из таблицы {tableName}: {ex.Message}");
        }
    }
    
    // Отображение результатов поиска
    public void DisplaySearchResults(DataTable data)
    {
        _currentData = data;
        ClearTableContent();
        DisplayTableData(data);
    }
    
    // Очистка содержимого таблицы
    public void ClearTableContent()
    {
        if (tableContentParent == null)
            return;
            
        // Удаляем все дочерние объекты из родительского элемента
        foreach (Transform child in tableContentParent)
        {
            Destroy(child.gameObject);
        }
    }

    // Отображение данных с динамическим созданием столбцов
    private void DisplayTableData(DataTable data)
    {
        if (data == null || data.Rows.Count == 0)
        {
            Debug.Log("Нет данных для отображения");
            
            // Создаем сообщение "Нет данных" в таблице
            var emptyRow = Instantiate(tableRowPrefab, tableContentParent);
            var emptyText = emptyRow.GetComponentInChildren<TextMeshProUGUI>();
            if (emptyText != null)
            {
                emptyText.text = "Нет данных, соответствующих запросу";
            }
            else
            {
                // Если в префабе нет компонента Text, создаем его
                var cell = Instantiate(cellPrefab, emptyRow.transform);
                var cellText = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (cellText != null)
                {
                    cellText.text = "Нет данных, соответствующих запросу";
                }
            }
            
            return;
        }

        var columnCount = data.Columns.Count;

        // Создаем заголовок таблицы с использованием специального метода
        var headerRow = CreateHeaderRow(data);
        headerRow.transform.SetParent(tableContentParent, false);

        // Создаем строки с данными
        foreach (DataRow row in data.Rows)
        {
            var rowObject = CreateDataRow(row, data.Columns);
            rowObject.transform.SetParent(tableContentParent, false);
        }
    }
    
    // Метод для создания строки заголовка
    private GameObject CreateHeaderRow(DataTable data)
    {
        var columnCount = data.Columns.Count;
        var row = Instantiate(tableRowPrefab);
        
        // Настраиваем компоненты Layout для строки
        SetupRowLayoutComponents(row);
        
        // Создаем ячейки заголовков
        for (int i = 0; i < columnCount; i++)
        {
            string columnName = data.Columns[i].ColumnName;
            
            // Используем cellTypePrefab для заголовков
            GameObject headerCell = Instantiate(cellTypePrefab, row.transform);
            
            // Настраиваем размер ячейки
            SetupCellLayoutElement(headerCell);
            
            // Находим компонент текста
            var cellText = headerCell.GetComponentInChildren<TextMeshProUGUI>();
            if (cellText != null)
            {
                // Добавляем индикатор сортировки, если этот столбец сортируется
                cellText.text = columnName;
                if (columnName == _currentSortColumn)
                {
                    cellText.text = columnName + (_ascending ? " ▲" : " ▼");
                }
            }
            
            // Добавляем компонент Button (если его нет)
            Button headerButton = headerCell.GetComponent<Button>();
            if (headerButton == null)
            {
                headerButton = headerCell.AddComponent<Button>();
                
                // Добавляем визуальный эффект при наведении курсора
                ColorBlock colors = headerButton.colors;
                colors.highlightedColor = new Color(0.9f, 0.9f, 1f);
                headerButton.colors = colors;
            }
            
            // Сохраняем имя столбца для использования в лямбда-выражении
            string columnNameCopy = columnName;
            
            // Добавляем обработчик нажатия
            headerButton.onClick.RemoveAllListeners();
            headerButton.onClick.AddListener(() => SortDataByColumn(columnNameCopy));
        }
        
        return row;
    }
    
    // Метод для создания строки данных
    private GameObject CreateDataRow(DataRow dataRow, DataColumnCollection columns)
    {
        var row = Instantiate(tableRowPrefab);
        
        // Настраиваем компоненты Layout для строки
        SetupRowLayoutComponents(row);
        
        // Создаем ячейки данных
        for (int i = 0; i < columns.Count; i++)
        {
            // Используем cellPrefab для ячеек данных
            GameObject cell = Instantiate(cellPrefab, row.transform);
            
            // Настраиваем размер ячейки
            SetupCellLayoutElement(cell);
            
            // Находим компонент текста
            var cellText = cell.GetComponentInChildren<TextMeshProUGUI>();
            if (cellText != null)
            {
                cellText.text = dataRow[i].ToString();
            }
        }
        
        return row;
    }
    
    // Настройка компонентов Layout для строки
    private void SetupRowLayoutComponents(GameObject row)
    {
        // Уничтожаем все существующие ячейки (если они есть в префабе)
        foreach (Transform child in row.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Добавляем Layout компонент, если его еще нет
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);
        }
        
        // Добавляем Content Size Fitter для автоматического изменения размера
        ContentSizeFitter sizeFitter = row.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = row.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
    
    // Настройка компонентов Layout для ячейки
    private void SetupCellLayoutElement(GameObject cell)
    {
        LayoutElement layoutElement = cell.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = cell.AddComponent<LayoutElement>();
        }
        
        // Все ячейки имеют одинаковую ширину (если таблица широкая, можно использовать скролл)
        layoutElement.flexibleWidth = 1;
        layoutElement.minWidth = 80;
    }
    
    // Метод для сортировки данных по указанному столбцу
    private void SortDataByColumn(string columnName)
    {
        if (_currentData == null || _currentData.Rows.Count == 0)
            return;
            
        // Проверяем, сортируем ли мы по тому же столбцу
        if (_currentSortColumn == columnName)
        {
            // Если да, то меняем направление сортировки
            _ascending = !_ascending;
        }
        else
        {
            // Если нет, то устанавливаем новый столбец и сортируем по возрастанию
            _currentSortColumn = columnName;
            _ascending = true;
        }
        
        // Создаем отсортированную таблицу
        DataTable sortedTable = _currentData.Clone();
        
        // Определяем порядок сортировки
        DataRow[] sortedRows;
        
        try
        {
            // Проверяем тип данных в столбце для правильной сортировки
            Type columnType = _currentData.Columns[columnName].DataType;
            
            // Сортируем в зависимости от направления
            if (_ascending)
            {
                sortedRows = _currentData.Select("", columnName + " ASC");
            }
            else
            {
                sortedRows = _currentData.Select("", columnName + " DESC");
            }
            
            // Заполняем таблицу отсортированными данными
            foreach (DataRow row in sortedRows)
            {
                sortedTable.ImportRow(row);
            }
            
            // Обновляем текущие данные и отображение
            _currentData = sortedTable;
            ClearTableContent();
            DisplayTableData(_currentData);
            
            Debug.Log($"Данные отсортированы по столбцу {columnName} {(_ascending ? "по возрастанию" : "по убыванию")}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при сортировке данных: {ex.Message}");
        }
    }
}