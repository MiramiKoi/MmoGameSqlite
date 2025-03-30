using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;

public class DatabaseSearcher : MonoBehaviour
{
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button searchButton;
    [SerializeField] private TMP_Dropdown searchColumnDropdown;
    
    private DatabaseLoader _databaseLoader;
    
    private DatabaseTableViewer _tableViewer;
    private List<string> _currentColumns = new ();
    private string _currentTable = "";
    private string _currentDbPath = "";
    
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
        _tableViewer = viewer;
    }
    
    // Установка текущей таблицы для поиска
    public void SetCurrentTable(string dbPath, string tableName)
    {
        _currentDbPath = dbPath;
        _currentTable = tableName;
    }
    
    // Обновление выпадающего списка столбцов для поиска
    public void UpdateSearchColumns(List<TableColumn> columns)
    {
        _currentColumns.Clear();
        foreach (var column in columns)
        {
            _currentColumns.Add(column.Name);
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
        var options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("Все столбцы"));
        
        foreach (var columnName in _currentColumns)
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
    private void SearchInDatabase()
    {
        if (searchInputField == null || string.IsNullOrEmpty(_currentTable) || string.IsNullOrEmpty(_currentDbPath))
            return;
            
        var searchText = searchInputField.text;
        
        if (string.IsNullOrEmpty(searchText))
        {
            // Если поле поиска пустое, показываем все данные
            _tableViewer.LoadTableData(_currentDbPath, _currentTable);
            return;
        }
        
        // Определяем, по какому столбцу искать
        var selectedColumn = "Все столбцы";
        if (searchColumnDropdown != null && searchColumnDropdown.value > 0)
        {
            selectedColumn = _currentColumns[searchColumnDropdown.value - 1]; // -1 так как первый элемент "Все столбцы"
        }
        
        try
        {
            string query;
            
            if (selectedColumn == "Все столбцы")
            {
                // Создаем SQL запрос для поиска по всем столбцам
                var whereClause = new StringBuilder();
                
                for (var i = 0; i < _currentColumns.Count; i++)
                {
                    if (i > 0) whereClause.Append(" OR ");
                    whereClause.Append($"{_currentColumns[i]} LIKE @searchParam");
                }
                
                query = $"SELECT * FROM {_currentTable} WHERE {whereClause} LIMIT 100";
            }
            else
            {
                // Создаем SQL запрос для поиска по выбранному столбцу
                query = $"SELECT * FROM {_currentTable} WHERE {selectedColumn} LIKE @searchParam LIMIT 100";
            }
            
            // Выполняем запрос с параметром
            var searchResults = _databaseLoader.ExecuteParameterizedQuery(_currentDbPath, query, "@searchParam", $"%{searchText}%");
            
            // Отображаем результаты поиска
            _tableViewer.DisplaySearchResults(searchResults);
            
            Debug.Log($"Поиск выполнен. Найдено строк: {searchResults.Rows.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при выполнении поиска: {ex.Message}");
        }
    }
}