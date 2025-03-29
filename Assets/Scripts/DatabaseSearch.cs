using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Mono.Data.Sqlite;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DatabaseSearch : MonoBehaviour
{
    // Добавляем элементы для поиска строк в таблице
    public TMP_InputField searchInputField;
    public Button searchButton;
    public TMP_Dropdown searchColumnDropdown; // Выпадающий список для выбора столбца поиска

    public List<string> currentColumns = new(); // Список столбцов текущей таблицы
    public DataTable lastLoadedData; // Последние загруженные данные из таблицы

    private TablesManager _tablesManager;
    private TablesViewer _tablesViewer;
    
    private DatabaseManager _databaseManager;

    private void Awake()
    {
        _databaseManager = FindFirstObjectByType<DatabaseManager>();
        
        _tablesManager = FindFirstObjectByType<TablesManager>();

        _tablesViewer = FindFirstObjectByType<TablesViewer>();

        // Инициализация поиска
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(SearchInDatabase);
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
            SearchInDatabase();
        }
    }

    // Обновление выпадающего списка столбцов для поиска
    public void UpdateSearchColumnDropdown()
    {
        if (searchColumnDropdown == null)
            return;

        searchColumnDropdown.ClearOptions();

        // Добавляем опцию для поиска по всем столбцам
        var options = new List<TMP_Dropdown.OptionData> { new ("Все столбцы") };

        options.AddRange(currentColumns.Select(columnName => new TMP_Dropdown.OptionData(columnName)));

        searchColumnDropdown.AddOptions(options);
        searchColumnDropdown.value = 0; // По умолчанию "Все столбцы"
    }

    // Метод поиска строк в базе данных
    public void SearchInDatabase()
    {
        if (searchInputField == null || string.IsNullOrEmpty(_tablesManager.currentTable))
            return;
            
        string searchText = searchInputField.text;
        
        if (string.IsNullOrEmpty(searchText))
        {
            // Если поле поиска пустое, показываем все данные
            _tablesViewer.LoadTableData(_tablesManager.currentTable);
            return;
        }
        
        // Определяем, по какому столбцу искать
        string selectedColumn = "Все столбцы";
        if (searchColumnDropdown != null && searchColumnDropdown.value > 0)
        {
            selectedColumn = currentColumns[searchColumnDropdown.value - 1]; // -1 так как первый элемент "Все столбцы"
        }
        
        // Очищаем предыдущие строки
        _tablesViewer.ClearTableContent();
        
        try
        {
            string query;
            
            if (selectedColumn == "Все столбцы")
            {
                // Создаем SQL запрос для поиска по всем столбцам
                StringBuilder whereClause = new StringBuilder();
                
                for (int i = 0; i < currentColumns.Count; i++)
                {
                    if (i > 0) whereClause.Append(" OR ");
                    whereClause.Append($"{currentColumns[i]} LIKE @searchParam");
                }
                
                query = $"SELECT * FROM {_tablesManager.currentTable} WHERE {whereClause} LIMIT 100";
            }
            else
            {
                // Создаем SQL запрос для поиска по выбранному столбцу
                query = $"SELECT * FROM {_tablesManager.currentTable} WHERE {selectedColumn} LIKE @searchParam LIMIT 100";
            }
            
            // Выполняем запрос с параметром
            DataTable searchResults = ExecuteParameterizedQuery(query, "@searchParam", $"%{searchText}%");
            
            // Отображаем результаты поиска
            _tablesViewer.DisplayTableData(searchResults);
            
            Debug.Log($"Поиск выполнен. Найдено строк: {searchResults.Rows.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при выполнении поиска: {ex.Message}");
        }
    }
    
    // Выполнение параметризованного SQL-запроса для безопасного поиска
    private DataTable ExecuteParameterizedQuery(string query, string paramName, string paramValue)
    {
        DataTable dataTable = new DataTable();
        
        using (SqliteConnection connection = new SqliteConnection($"URI=file:{_databaseManager.databasePath}"))
        {
            connection.Open();
            
            using (SqliteCommand command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue(paramName, paramValue);
                
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