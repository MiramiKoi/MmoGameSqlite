using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using TMPro;

public class DatabaseSearcher : MonoBehaviour
{
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button searchButton;
    [SerializeField] private TMP_Dropdown searchColumnDropdown;
    
    private DatabaseLoader _databaseLoader;
    
    private DatabaseTableViewer tableViewer;
    private List<string> currentColumns = new List<string>();
    private string currentTable = "";
    private string currentDbPath = "";
    
    private void Awake()
    {
        _databaseLoader = GetComponent<DatabaseLoader>();
    
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
    
    public void Initialize(DatabaseTableViewer viewer)
    {
        tableViewer = viewer;
    }
    
    // Установка текущей таблицы для поиска
    public void SetCurrentTable(string dbPath, string tableName)
    {
        currentDbPath = dbPath;
        currentTable = tableName;
    }
    
    // Обновление выпадающего списка столбцов для поиска
    public void UpdateSearchColumns(List<TableColumn> columns)
    {
        currentColumns.Clear();
        foreach (var column in columns)
        {
            currentColumns.Add(column.Name);
        }
        
        UpdateSearchColumnDropdown();
    }
    
    // Обновление выпадающего списка столбцов для поиска
    private void UpdateSearchColumnDropdown()
    {
        if (searchColumnDropdown == null)
            return;
            
        searchColumnDropdown.ClearOptions();
        
        // Добавляем опцию для поиска по всем столбцам
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("Все столбцы"));
        
        foreach (string columnName in currentColumns)
        {
            options.Add(new TMP_Dropdown.OptionData(columnName));
        }
        
        searchColumnDropdown.AddOptions(options);
        searchColumnDropdown.value = 0; // По умолчанию "Все столбцы"
    }
    
    // Метод для обработки нажатия Enter в поле поиска
    private void OnSearchFieldEndEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SearchInDatabase();
        }
    }
    
    // Метод поиска строк в базе данных
    public void SearchInDatabase()
    {
        if (searchInputField == null || string.IsNullOrEmpty(currentTable) || string.IsNullOrEmpty(currentDbPath))
            return;
            
        string searchText = searchInputField.text;
        
        if (string.IsNullOrEmpty(searchText))
        {
            // Если поле поиска пустое, показываем все данные
            tableViewer.LoadTableData(currentDbPath, currentTable);
            return;
        }
        
        // Определяем, по какому столбцу искать
        string selectedColumn = "Все столбцы";
        if (searchColumnDropdown != null && searchColumnDropdown.value > 0)
        {
            selectedColumn = currentColumns[searchColumnDropdown.value - 1]; // -1 так как первый элемент "Все столбцы"
        }
        
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
                
                query = $"SELECT * FROM {currentTable} WHERE {whereClause} LIMIT 100";
            }
            else
            {
                // Создаем SQL запрос для поиска по выбранному столбцу
                query = $"SELECT * FROM {currentTable} WHERE {selectedColumn} LIKE @searchParam LIMIT 100";
            }
            
            // Выполняем запрос с параметром
            DataTable searchResults = _databaseLoader.ExecuteParameterizedQuery(currentDbPath, query, "@searchParam", $"%{searchText}%");
            
            // Отображаем результаты поиска
            tableViewer.DisplaySearchResults(searchResults);
            
            Debug.Log($"Поиск выполнен. Найдено строк: {searchResults.Rows.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при выполнении поиска: {ex.Message}");
        }
    }
}