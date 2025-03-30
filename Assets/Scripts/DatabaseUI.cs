using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;

public class DatabaseUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown tablesDropdown;
    public Text tableInfoText;
    
    // Событие выбора таблицы
    public event Action<string> OnTableSelected;
    
    private List<string> _availableTables = new ();
    
    private void Start()
    {
        // Регистрация события выбора таблицы
        tablesDropdown.onValueChanged.AddListener(OnTableDropdownChanged);
    }
    
    // Обработчик изменения выбора в выпадающем списке
    private void OnTableDropdownChanged(int index)
    {
        if (index < 0 || index >= _availableTables.Count) return;
        
        var selectedTable = _availableTables[index];
        Debug.Log($"Выбрана таблица: {selectedTable}");
            
        // Вызываем событие выбора таблицы
        OnTableSelected?.Invoke(selectedTable);
    }
    
    // Обновление выпадающего списка таблиц
    public void UpdateTablesDropdown(List<string> tables)
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
        OnTableDropdownChanged(0);
    }
    
    // Отображение структуры таблицы
    public void ShowTableStructure(string tableName, List<TableColumn> structure)
    {
        if (tableInfoText == null)
            return;
            
        var structureInfo = $"Структура таблицы {tableName}:\n";
        
        foreach (var column in structure)
        {
            structureInfo += $"{column.Name} ({column.Type})\n";
        }
        
        tableInfoText.text = structureInfo;
    }
}