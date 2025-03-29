using System;
using System.Data;
using TMPro;
using UnityEngine;

public class TablesViewer : MonoBehaviour
{
    // UI элементы для отображения таблицы
    public GameObject tableRowPrefab;
    public GameObject tableRowTypePrefab;
    public Transform tableContentParent;
    
    private DatabaseManager _databaseManager;
    private TablesManager _tablesManager;

    private void Awake()
    {
        _databaseManager = FindFirstObjectByType<DatabaseManager>();
        
        _tablesManager = FindFirstObjectByType<TablesManager>();
        _tablesManager.onTablesChanged.AddListener(LoadTableData);
    }
    
     // Загрузка данных из выбранной таблицы
    private void LoadTableData(string tableName)
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
            var query = $"SELECT * FROM {tableName} LIMIT 100"; // Ограничение на 100 строк
            var tableData = _databaseManager.ExecuteQuery(query);
            
            // Отображаем данные
            DisplayTableData(tableData);
            
            Debug.Log($"Загружено {tableData.Rows.Count} строк из таблицы {tableName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при загрузке данных из таблицы {tableName}: {ex.Message}");
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
        var headerRow = Instantiate(tableRowTypePrefab, tableContentParent);
        var headerTexts = headerRow.GetComponentsInChildren<TextMeshProUGUI>();
        
        for (var i = 0; i < headerTexts.Length && i < data.Columns.Count; i++)
        {
            headerTexts[i].text = data.Columns[i].ColumnName;
        }

        // Создаем строки с данными
        foreach (DataRow row in data.Rows)
        {
            var rowObject = Instantiate(tableRowPrefab, tableContentParent);
            var texts = rowObject.GetComponentsInChildren<TextMeshProUGUI>();
            
            for (var i = 0; i < texts.Length && i < row.ItemArray.Length; i++)
            {
                texts[i].text = row.ItemArray[i].ToString();
            }
        }
    }

    // Очистка содержимого таблицы
    private void ClearTableContent()
    {
        foreach (Transform child in tableContentParent)
        {
            Destroy(child.gameObject);
        }
    }
}