using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Events;

public class DatabaseManager : MonoBehaviour
{
    public string databasePath;

    private DatabaseUI _databaseUI;
    private DatabaseLoader _databaseLoader;
    private DatabaseTableViewer _tableViewer;
    private DatabaseSearcher _databaseSearcher;

    public UnityEvent onDataLoaded;

    private void Start()
    {
        _databaseLoader = GetComponent<DatabaseLoader>();
        _databaseUI = GetComponent<DatabaseUI>();
        _tableViewer = GetComponent<DatabaseTableViewer>();
        _databaseSearcher = GetComponent<DatabaseSearcher>();

        Initialize();
    }

    private void Initialize()
    {
        // Инициализация компонентов и подписка на события
        if (_databaseUI != null)
        {
            _databaseUI.OnTableSelected += TableSelected;
        }

        if (_databaseSearcher != null)
        {
            _databaseSearcher.Initialize(_tableViewer);
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
            List<string> availableTables = _databaseLoader.GetAvailableTables(databasePath);

            // Обновляем выпадающий список
            _databaseUI.UpdateTablesDropdown(availableTables);

            onDataLoaded.Invoke();
            
            Debug.Log("База данных успешно загружена");
        }
        catch (Exception ex)
        {
            Debug.LogError("Ошибка при загрузке базы данных: " + ex.Message);
        }
    }

    // Обработчик выбора таблицы
    private void TableSelected(string tableName)
    {
        if (!string.IsNullOrEmpty(tableName))
        {
            // Получаем информацию о структуре таблицы
            var structure = _databaseLoader.GetTableStructure(databasePath, tableName);

            // Отображаем структуру таблицы
            _databaseUI.ShowTableStructure(tableName, structure);

            // Обновляем выпадающий список столбцов для поиска
            _databaseSearcher.UpdateSearchColumns(structure);

            // Устанавливаем текущую таблицу для поиска
            _databaseSearcher.SetCurrentTable(databasePath, tableName);

            // Загружаем данные из выбранной таблицы
            _tableViewer.LoadTableData(databasePath, tableName);
        }
    }

    // Метод для загрузки файла базы данных через диалог
    public void LoadFile()
    {
#if UNITY_EDITOR
        var path = UnityEditor.EditorUtility.OpenFilePanel("Overwrite with db", "", "db");

        if (path.Length != 0)
        {
            databasePath = path;
            LoadDatabase();
        }
#else
        Debug.LogWarning("Функция доступна только в редакторе Unity");
#endif
    }
}