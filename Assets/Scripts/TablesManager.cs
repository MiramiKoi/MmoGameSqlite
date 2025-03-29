using UnityEngine;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.Events;

public class TablesManager : MonoBehaviour
{

    public TMP_Dropdown tablesDropdown;

    private readonly List<string> _availableTables = new ();
    private string _currentTable = "";

    private DatabaseManager _databaseManager;
    
    public UnityEvent<string> onTablesChanged;

    private void Awake()
    {
        _databaseManager = FindFirstObjectByType<DatabaseManager>();
        _databaseManager.onDatabaseSelected.AddListener(LoadDatabase);
    }

    // Загрузка базы данных и получение списка таблиц
    private void LoadDatabase()
    {
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
        _availableTables.Clear();

        var query = @"
            SELECT name 
            FROM sqlite_master 
            WHERE type='table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name";

        var result = _databaseManager.ExecuteQuery(query);

        foreach (DataRow row in result.Rows)
        {
            _availableTables.Add(row["name"].ToString());
        }

        Debug.Log($"Найдено таблиц: {_availableTables.Count}");
    }

    // Обновление выпадающего списка таблиц
    private void UpdateTablesDropdown()
    {
        tablesDropdown.ClearOptions();

        var options = _availableTables.Select(tableName => new TMP_Dropdown.OptionData(tableName)).ToList();

        tablesDropdown.AddOptions(options);

        // Если есть таблицы, выбираем первую по умолчанию
        if (_availableTables.Count <= 0) return;
        
        tablesDropdown.value = 0;
        OnTableSelected(0);
    }

    // Обработчик выбора таблицы
    private void OnTableSelected(int index)
    {
        if (index < 0 || index >= _availableTables.Count) return;
        
        _currentTable = _availableTables[index];
        Debug.Log($"Выбрана таблица: {_currentTable}");

        // Получаем информацию о структуре таблицы
        GetTableStructure(_currentTable);

        // Загружаем данные из выбранной таблицы
        onTablesChanged.Invoke(_currentTable);
    }

    // Получение структуры таблицы (для отображения информации)
    private void GetTableStructure(string tableName)
    {
        var query = $"PRAGMA table_info({tableName})";
        var structure = _databaseManager.ExecuteQuery(query);

        var structureInfo = $"Структура таблицы {tableName}:\n";

        structureInfo = structure.Rows.Cast<DataRow>()
            .Aggregate(structureInfo, (current, row) => current + $"{row["name"]} ({row["type"]})\n");
    }
}