using UnityEngine;
using UnityEngine.UI;
using System;
using System.Data;
using TMPro;

public class DatabaseTableViewer : MonoBehaviour
{
    [SerializeField] private Transform tableContentParent;
    [SerializeField] private GameObject tableRowPrefab;
    [SerializeField] private GameObject cellPrefab; // Префаб для одной ячейки данных
    private DatabaseLoader _databaseLoader;
    
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

        // Очищаем предыдущие строки
        ClearTableContent();

        try
        {
            // Выполняем запрос и получаем данные
            string query = $"SELECT * FROM {tableName} LIMIT 100"; // Ограничение на 100 строк
            DataTable tableData = _databaseLoader.ExecuteQuery(dbPath, query);
            
            // Отображаем данные
            DisplayTableData(tableData);
            
            Debug.Log($"Загружено {tableData.Rows.Count} строк из таблицы {tableName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при загрузке данных из таблицы {tableName}: {ex.Message}");
        }
    }
    
    // Отображение результатов поиска
    public void DisplaySearchResults(DataTable data)
    {
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

        // Создаем заголовок таблицы
        var headerRow = CreateRow(columnCount);
        headerRow.transform.SetParent(tableContentParent, false);
        
        // Заполняем заголовки
        for (int i = 0; i < columnCount; i++)
        {
            var cellTransform = headerRow.transform.GetChild(i);
            var cellText = cellTransform.GetComponentInChildren<TextMeshProUGUI>();
            if (cellText != null)
            {
                cellText.text = data.Columns[i].ColumnName;
            }
        }

        // Создаем строки с данными
        foreach (DataRow row in data.Rows)
        {
            var rowObject = CreateRow(columnCount);
            rowObject.transform.SetParent(tableContentParent, false);
            
            // Заполняем ячейки данными
            for (var i = 0; i < columnCount; i++)
            {
                var cellTransform = rowObject.transform.GetChild(i);
                var cellText = cellTransform.GetComponentInChildren<TextMeshProUGUI>();
                if (cellText != null)
                {
                    cellText.text = row[i].ToString();
                }
            }
        }
    }
    
    // Метод для создания строки с нужным количеством ячеек
    private GameObject CreateRow(int cellCount)
    {
        var row = Instantiate(tableRowPrefab);
        
        // Получаем RectTransform для установки высоты и ширины строки
        RectTransform rowRect = row.GetComponent<RectTransform>();
        
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
        
        // Создаем нужное количество ячеек
        for (int i = 0; i < cellCount; i++)
        {
            GameObject cell = Instantiate(cellPrefab, row.transform);
            
            // Устанавливаем размер ячейки через LayoutElement
            LayoutElement layoutElement = cell.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = cell.AddComponent<LayoutElement>();
            }
            
            // Все ячейки имеют одинаковую ширину (если таблица широкая, можно использовать скролл)
            layoutElement.flexibleWidth = 1;
            layoutElement.minWidth = 80;
        }
        
        return row;
    }
}