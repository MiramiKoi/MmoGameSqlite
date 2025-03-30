using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using SFB;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

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
    private void LoadDatabase()
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
            var availableTables = _databaseLoader.GetAvailableTables(databasePath);

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
        if (string.IsNullOrEmpty(tableName)) return;
        
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

    // Метод для загрузки файла базы данных через диалог
    public void LoadFile()
    {
#if UNITY_EDITOR
        var path = UnityEditor.EditorUtility.OpenFilePanel("Overwrite with db", "", "db");

        if (path.Length == 0) return;
        
        databasePath = path;
        LoadDatabase();
#else
        // Открыть диалог выбора файла, который работает в билде
        var paths = StandaloneFileBrowser.OpenFilePanel("Overwrite with db", "", "db", false);

        // Проверяем, был ли выбран файл
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

        databasePath = paths[0];
        LoadDatabase();
#endif
    }
}