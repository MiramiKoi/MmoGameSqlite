using UnityEngine;
using System;
using System.IO;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class DatabaseManager1 : MonoBehaviour
{
    public string databasePath;

    // UI элементы для ввода
    public InputField inputField1;
    public InputField inputField2;
    public InputField inputField3;

    // UI элементы для выбора и отображения таблицы
    public Dropdown tablesDropdown;
    public Transform tableContentParent;
    public GameObject tableRowPrefab;
    public Text tableInfoText;
    
    // Добавляем элементы для поиска строк в таблице
    public InputField searchInputField;
    public Button searchButton;
    public Dropdown searchColumnDropdown;  // Выпадающий список для выбора столбца поиска
    
    private List<string> availableTables = new List<string>();
    private string currentTable = "";
    private List<string> currentColumns = new List<string>();  // Список столбцов текущей таблицы
    private DataTable lastLoadedData;  // Последние загруженные данные из таблицы

    // Инициализация при старте
    void Start()
    {
        // Регистрация события выбора таблицы
        tablesDropdown.onValueChanged.AddListener(OnTableSelected);
        
        // Инициализация поиска
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(SearchInTable);
        }
        
        // Добавляем слушатель для поиска при нажатии Enter
        if (searchInputField != null)
        {
            searchInputField.onEndEdit.AddListener(OnSearchFieldEndEdit);
        }
    }
    
    // Метод для обработки нажатия Enter в поле поиска
    private void OnSearchFieldEndEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SearchInTable();
        }
    }

    // Загрузка базы данных и получение списка таблиц
    public void LoadDatabase()
    {
        if (string.IsNullOrEmpty(databasePath))
        {
            Debug.LogError("Путь к базе данных не выбран!");
            return;
        }

        if (!File.Exists(databasePath))
        {
            Debug.LogError("Файл базы данных не найден: " + databasePath);
            return;
        }

        try
        {
            // Получаем список таблиц
            GetAvailableTables();
            
            // Обновляем выпадающий список
            UpdateTablesDropdown();
            
            Debug.Log("База данных успешно загружена");
        }
        catch (Exception ex)
        {
            Debug.LogError("Ошибка при загрузке базы данных: " + ex.Message);
        }
    }

    // Получение списка таблиц из базы данных
    private void GetAvailableTables()
    {
        availableTables.Clear();
        
        string query = @"
            SELECT name 
            FROM sqlite_master 
            WHERE type='table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name";
            
        DataTable result = ExecuteQuery(query);
        
        foreach (DataRow row in result.Rows)
        {
            availableTables.Add(row["name"].ToString());
        }
        
        Debug.Log($"Найдено таблиц: {availableTables.Count}");
    }

    // Обновление выпадающего списка таблиц
    private void UpdateTablesDropdown()
    {
        tablesDropdown.ClearOptions();
        
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        foreach (string tableName in availableTables)
        {
            options.Add(new Dropdown.OptionData(tableName));
        }
        
        tablesDropdown.AddOptions(options);
        
        // Если есть таблицы, выбираем первую по умолчанию
        if (availableTables.Count > 0)
        {
            tablesDropdown.value = 0;
            OnTableSelected(0);
        }
    }

    // Обработчик выбора таблицы
    public void OnTableSelected(int index)
    {
        if (index >= 0 && index < availableTables.Count)
        {
            currentTable = availableTables[index];
            Debug.Log($"Выбрана таблица: {currentTable}");
            
            // Получаем информацию о структуре таблицы
            GetTableStructure(currentTable);
            
            // Загружаем данные из выбранной таблицы
            LoadTableData(currentTable);
        }
    }

    // Получение структуры таблицы (для отображения информации)
    private void GetTableStructure(string tableName)
    {
        string query = $"PRAGMA table_info({tableName})";
        DataTable structure = ExecuteQuery(query);
        
        string structureInfo = $"Структура таблицы {tableName}:\n";
        
        // Очищаем предыдущий список столбцов
        currentColumns.Clear();
        
        foreach (DataRow row in structure.Rows)
        {
            string columnName = row["name"].ToString();
            string columnType = row["type"].ToString();
            
            structureInfo += $"{columnName} ({columnType})\n";
            currentColumns.Add(columnName);
        }
        
        if (tableInfoText != null)
        {
            tableInfoText.text = structureInfo;
        }
        
        // Обновляем выпадающий список столбцов для поиска
        UpdateSearchColumnDropdown();
    }
    
    // Обновление выпадающего списка столбцов для поиска
    private void UpdateSearchColumnDropdown()
    {
        if (searchColumnDropdown == null)
            return;
            
        searchColumnDropdown.ClearOptions();
        
        // Добавляем опцию для поиска по всем столбцам
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        options.Add(new Dropdown.OptionData("Все столбцы"));
        
        foreach (string columnName in currentColumns)
        {
            options.Add(new Dropdown.OptionData(columnName));
        }
        
        searchColumnDropdown.AddOptions(options);
        searchColumnDropdown.value = 0; // По умолчанию "Все столбцы"
    }

    // Загрузка данных из выбранной таблицы
    public void LoadTableData(string tableName)
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
            DataTable tableData = ExecuteQuery(query);
            
            // Сохраняем полученные данные
            lastLoadedData = tableData;
            
            // Отображаем данные
            DisplayTableData(tableData);
            
            Debug.Log($"Загружено {tableData.Rows.Count} строк из таблицы {tableName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при загрузке данных из таблицы {tableName}: {ex.Message}");
        }
    }
    
    // Метод поиска строк в таблице
    public void SearchInTable()
    {
        if (searchInputField == null || string.IsNullOrEmpty(currentTable) || lastLoadedData == null)
            return;
            
        string searchText = searchInputField.text.ToLower();
        
        // Если поле поиска пустое, показываем все строки
        if (string.IsNullOrEmpty(searchText))
        {
            DisplayTableData(lastLoadedData);
            return;
        }
        
        // Создаем новую таблицу для результатов поиска
        DataTable filteredData = lastLoadedData.Clone();
        
        // Определяем, по какому столбцу искать
        string selectedColumn = "Все столбцы";
        if (searchColumnDropdown != null && searchColumnDropdown.value > 0)
        {
            selectedColumn = currentColumns[searchColumnDropdown.value - 1]; // -1 так как первый элемент "Все столбцы"
        }
        
        foreach (DataRow row in lastLoadedData.Rows)
        {
            bool rowMatched = false;
            
            if (selectedColumn == "Все столбцы")
            {
                // Поиск по всем столбцам
                foreach (DataColumn column in lastLoadedData.Columns)
                {
                    if (row[column].ToString().ToLower().Contains(searchText))
                    {
                        rowMatched = true;
                        break;
                    }
                }
            }
            else
            {
                // Поиск только по выбранному столбцу
                if (row[selectedColumn].ToString().ToLower().Contains(searchText))
                {
                    rowMatched = true;
                }
            }
            
            if (rowMatched)
            {
                filteredData.ImportRow(row);
            }
        }
        
        // Очищаем и отображаем отфильтрованные данные
        ClearTableContent();
        DisplayTableData(filteredData);
        
        Debug.Log($"Поиск выполнен. Найдено строк: {filteredData.Rows.Count}");
    }
    
    // Очистка содержимого таблицы
    private void ClearTableContent()
    {
        if (tableContentParent == null)
            return;
            
        // Удаляем все дочерние объекты из родительского элемента
        foreach (Transform child in tableContentParent)
        {
            Destroy(child.gameObject);
        }
    }

    // Отображение данных с использованием префабов
    private void DisplayTableData(DataTable data)
    {
        if (data == null || data.Rows.Count == 0)
        {
            Debug.Log("Нет данных для отображения");
            return;
        }

        // Получаем имена столбцов для отображения в заголовке
        // Создаем заголовок таблицы
        GameObject headerRow = Instantiate(tableRowPrefab, tableContentParent);
        Text[] headerTexts = headerRow.GetComponentsInChildren<Text>();
        
        for (int i = 0; i < headerTexts.Length && i < data.Columns.Count; i++)
        {
            headerTexts[i].text = data.Columns[i].ColumnName;
            headerTexts[i].fontStyle = FontStyle.Bold;
        }

        // Создаем строки с данными
        foreach (DataRow row in data.Rows)
        {
            GameObject rowObject = Instantiate(tableRowPrefab, tableContentParent);
            Text[] texts = rowObject.GetComponentsInChildren<Text>();
            
            for (int i = 0; i < texts.Length && i < row.ItemArray.Length; i++)
            {
                texts[i].text = row.ItemArray[i].ToString();
            }
        }
    }
    
    // Выполнение SQL-запроса и получение результата в виде DataTable
    private DataTable ExecuteQuery(string query)
    {
        DataTable dataTable = new DataTable();
        
        using (SqliteConnection connection = new SqliteConnection($"URI=file:{databasePath}"))
        {
            connection.Open();
            
            using (SqliteCommand command = new SqliteCommand(query, connection))
            {
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }
            
            connection.Close();
        }
        
        return dataTable;
    }
}