using UnityEngine;
using UnityEngine.UI;
using System;
using System.Data;
using TMPro;

public class DatabaseTableViewer : MonoBehaviour
{
    [SerializeField] private Transform tableContentParent;
    [SerializeField] private GameObject tableRowPrefab;
    [SerializeField] private GameObject tableRowTypePrefab;
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

    // Отображение данных с использованием префабов
    private void DisplayTableData(DataTable data)
    {
        if (data == null || data.Rows.Count == 0)
        {
            Debug.Log("Нет данных для отображения");

            // Создаем сообщение "Нет данных" в таблице
            GameObject emptyRow = Instantiate(tableRowPrefab, tableContentParent);
            TextMeshProUGUI[] texts = emptyRow.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                texts[0].text = "Нет данных, соответствующих запросу";
            }

            return;
        }

        // Создаем заголовок таблицы
        GameObject headerRow = Instantiate(tableRowTypePrefab, tableContentParent);
        TextMeshProUGUI[] headerTexts = headerRow.GetComponentsInChildren<TextMeshProUGUI>();

        for (int i = 0; i < headerTexts.Length && i < data.Columns.Count; i++)
        {
            headerTexts[i].text = data.Columns[i].ColumnName;
        }

        // Создаем строки с данными
        foreach (DataRow row in data.Rows)
        {
            GameObject rowObject = Instantiate(tableRowPrefab, tableContentParent);
            TextMeshProUGUI[] texts = rowObject.GetComponentsInChildren<TextMeshProUGUI>();

            for (int i = 0; i < texts.Length && i < row.ItemArray.Length; i++)
            {
                texts[i].text = row.ItemArray[i].ToString();
            }
        }
    }
}