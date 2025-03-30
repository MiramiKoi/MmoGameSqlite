using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;
using Mono.Data.Sqlite;
using System.Data;

public class DatabaseRecordAdder : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private TMP_Dropdown tablesDropdown;
    [SerializeField] private Transform headerRowParent;
    [SerializeField] private Transform inputRowParent;
    [SerializeField] private GameObject headerCellPrefab;
    [SerializeField] private GameObject inputFieldCellPrefab;
    [SerializeField] private Button addButton;
    
    private DatabaseLoader _databaseLoader;
    private DatabaseManager _databaseManager;
    private DatabaseTableViewer _tableViewer;
    private List<string> _availableTables = new List<string>();
    private List<TableColumn> _currentTableColumns = new List<TableColumn>();
    private List<TMP_InputField> _inputFields = new List<TMP_InputField>();
    private string _currentTableName;
    private string _currentDBPath;
    private string _primaryKeyColumn;
    
    private void Start()
    {
        _databaseLoader = GetComponent<DatabaseLoader>();
        _databaseManager = GetComponent<DatabaseManager>();
        _tableViewer = GetComponent<DatabaseTableViewer>();
        
        tablesDropdown.onValueChanged.AddListener(OnTableSelected);
        
        if (addButton != null)
        {
            addButton.onClick.AddListener(AddRecordToTable);
        }
        
        _databaseManager.onDataLoaded.AddListener(InitializeAddPage);
    }
    
    
    
    // Инициализация страницы добавления записей
    private void InitializeAddPage()
    {
        // Получаем список таблиц из DatabaseManager
        var databaseManager = GetComponent<DatabaseManager>();
        
        if (databaseManager == null) return;
        
        _currentDBPath = databaseManager.databasePath;
        UpdateTablesDropdown(_databaseLoader.GetAvailableTables(_currentDBPath));
    }
    
    // Обновление выпадающего списка таблиц
    private void UpdateTablesDropdown(List<string> tables)
    {
        _availableTables = tables;
        tablesDropdown.ClearOptions();
        
        var options = new List<TMP_Dropdown.OptionData>();
        
        foreach (var tableName in _availableTables)
        {
            options.Add(new TMP_Dropdown.OptionData(tableName));
        }
        
        tablesDropdown.AddOptions(options);
        
        // Если есть таблицы, выбираем первую по умолчанию
        if (_availableTables.Count <= 0) return;
        
        tablesDropdown.value = 0;
        OnTableSelected(0);
    }
    
    // Обработчик выбора таблицы в выпадающем списке
    private void OnTableSelected(int index)
    {
        if (index < 0 || index >= _availableTables.Count) return;
        
        _currentTableName = _availableTables[index];
        Debug.Log($"Выбрана таблица для добавления: {_currentTableName}");
            
        // Получаем структуру таблицы и определяем первичный ключ
        _currentTableColumns = _databaseLoader.GetTableStructure(_currentDBPath, _currentTableName);
        IdentifyPrimaryKey();
            
        // Создаем UI для ввода данных
        CreateInputUI();
    }
    
    // Определение первичного ключа таблицы
    private void IdentifyPrimaryKey()
    {
        try
        {
            var query = $"PRAGMA table_info({_currentTableName})";
            var tableInfo = _databaseLoader.ExecuteQuery(_currentDBPath, query);
            
            _primaryKeyColumn = null;
            
            // Ищем столбец с primary key
            foreach (DataRow row in tableInfo.Rows)
            {
                if (Convert.ToInt32(row["pk"]) > 0)
                {
                    _primaryKeyColumn = row["name"].ToString();
                    Debug.Log($"Первичный ключ для таблицы {_currentTableName}: {_primaryKeyColumn}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при определении первичного ключа: {ex.Message}");
            _primaryKeyColumn = null;
        }
    }
    
    // Создание UI для ввода данных
    private void CreateInputUI()
    {
        // Очищаем предыдущие строки
        ClearRows();
        _inputFields.Clear();
        
        // Создаем заголовки столбцов
        foreach (var column in _currentTableColumns)
        {
            // Создаем ячейку заголовка
            var headerCell = Instantiate(headerCellPrefab, headerRowParent);
            var headerText = headerCell.GetComponentInChildren<TextMeshProUGUI>();
            if (headerText != null)
            {
                headerText.text = column.Name;
            }
            
            // Создаем ячейку с полем ввода
            var inputCell = Instantiate(inputFieldCellPrefab, inputRowParent);
            var inputField = inputCell.GetComponentInChildren<TMP_InputField>();
            
            if (inputField == null) continue;
            
            // Настраиваем поле ввода в зависимости от типа данных
            SetupInputField(inputField, column);
            _inputFields.Add(inputField);
        }
    }
    
    // Настройка поля ввода в зависимости от типа данных
    private void SetupInputField(TMP_InputField inputField, TableColumn column)
    {
        // Если это первичный ключ и автоинкрементный, делаем поле недоступным
        if (column.Name == _primaryKeyColumn)
        {
            inputField.interactable = false;
            inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "AUTO";
            return;
        }
        
        // Настраиваем подсказку и ограничения в зависимости от типа
        switch (column.Type.ToLower())
        {
            case "integer":
                inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Введите число";
                break;
                
            case "real":
            case "float":
            case "double":
                inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Введите число";
                break;
                
            case "text":
            case "varchar":
            case "char":
                inputField.contentType = TMP_InputField.ContentType.Standard;
                inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Введите текст";
                break;
                
            case "date":
            case "datetime":
                inputField.contentType = TMP_InputField.ContentType.Standard;
                inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "YYYY-MM-DD";
                break;
                
            default:
                inputField.contentType = TMP_InputField.ContentType.Standard;
                inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Введите значение";
                break;
        }
    }
    
    // Очистка строк заголовков и полей ввода
    private void ClearRows()
    {
        foreach (Transform child in headerRowParent)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in inputRowParent)
        {
            Destroy(child.gameObject);
        }
    }
    
    // Метод для добавления записи в таблицу
    private void AddRecordToTable()
    {
        if (string.IsNullOrEmpty(_currentTableName) || _currentTableColumns.Count == 0 || _inputFields.Count == 0)
        {
            Debug.LogError("Недостаточно данных для добавления записи");
            return;
        }
        
        try
        {
            // Формируем SQL запрос
            var columns = "";
            var values = "";
            
            var nonEmptyColumns = new List<string>();
            var nonEmptyValues = new List<string>();
            
            // Собираем непустые поля и их значения
            for (var i = 0; i < _currentTableColumns.Count; i++)
            {
                // Пропускаем первичный ключ, если он автоинкрементный
                if (_currentTableColumns[i].Name == _primaryKeyColumn && !_inputFields[i].interactable)
                {
                    continue;
                }
                
                // Если поле заполнено, добавляем его в запрос
                if (string.IsNullOrEmpty(_inputFields[i].text)) continue;
                
                nonEmptyColumns.Add(_currentTableColumns[i].Name);
                nonEmptyValues.Add(_inputFields[i].text);
            }
            
            // Формируем строки для запроса
            columns = string.Join(", ", nonEmptyColumns);
            
            var paramNames = new List<string>();
            for (var i = 0; i < nonEmptyValues.Count; i++)
            {
                paramNames.Add($"@param{i}");
            }
            values = string.Join(", ", paramNames);
            
            // Создаем SQL запрос
            var query = $"INSERT INTO {_currentTableName} ({columns}) VALUES ({values})";
            
            using (var connection = new SqliteConnection($"URI=file:{_currentDBPath}"))
            {
                connection.Open();
                
                using (var command = new SqliteCommand(query, connection))
                {
                    // Добавляем параметры
                    for (var i = 0; i < nonEmptyValues.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@param{i}", nonEmptyValues[i]);
                    }
                    
                    // Выполняем запрос
                    var rowsAffected = command.ExecuteNonQuery();
                    
                    if (rowsAffected > 0)
                    {
                        Debug.Log("Запись успешно добавлена в таблицу");
                        
                        // Очищаем поля ввода
                        foreach (var field in _inputFields)
                        {
                            if (field.interactable)
                            {
                                field.text = "";
                            }
                        }
                        
                        // Обновляем отображение таблицы, если она открыта
                        if (_tableViewer != null && _currentTableName != null)
                        {
                            _tableViewer.LoadTableData(_currentDBPath, _currentTableName);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Запись не была добавлена. Проверьте введенные данные.");
                    }
                }
                
                connection.Close();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при добавлении записи: {ex.Message}");
        }
    }
}