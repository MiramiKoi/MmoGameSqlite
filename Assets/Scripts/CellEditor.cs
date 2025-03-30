using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CellEditor : MonoBehaviour
{
    [SerializeField] private GameObject editWindow;
    [SerializeField] private TMP_InputField valueInputField;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI headerText;

    private DatabaseTableModifier _tableModifier;
    private string _currentTableName;
    private string _currentColumnName;
    private string _currentPrimaryKey;
    private string _currentPrimaryKeyValue;
    private string _originalValue;
    
    private void Awake()
    {
        _tableModifier = GetComponent<DatabaseTableModifier>();
        
        // Подписываемся на события кнопок
        saveButton.onClick.AddListener(OnSaveButtonClick);
        cancelButton.onClick.AddListener(OnCancelButtonClick);
        
        // По умолчанию окно редактирования скрыто
        editWindow.SetActive(false);
    }
    
    // Открытие окна редактирования ячейки
    public void OpenEditWindow(string tableName, string columnName, string primaryKey, string primaryKeyValue, string currentValue)
    {
        // Сохраняем параметры текущего редактирования
        _currentTableName = tableName;
        _currentColumnName = columnName;
        _currentPrimaryKey = primaryKey;
        _currentPrimaryKeyValue = primaryKeyValue;
        _originalValue = currentValue;
        
        // Обновляем UI
        headerText.text = $"Редактирование ячейки: {columnName}";
        valueInputField.text = currentValue;
        
        // Показываем окно
        editWindow.SetActive(true);
    }
    
    // Обработчик нажатия на кнопку "Сохранить"
    private void OnSaveButtonClick()
    {
        string newValue = valueInputField.text;
        
        // Если значение не изменилось, просто закрываем окно
        if (newValue == _originalValue)
        {
            CloseEditWindow();
            return;
        }
        
        // Сохраняем изменения в базе данных
        _tableModifier.UpdateCellValue(_currentTableName, _currentColumnName, 
            _currentPrimaryKey, _currentPrimaryKeyValue, newValue);
        
        // Закрываем окно
        CloseEditWindow();
    }
    
    // Обработчик нажатия на кнопку "Отмена"
    private void OnCancelButtonClick()
    {
        CloseEditWindow();
    }
    
    // Закрытие окна редактирования
    private void CloseEditWindow()
    {
        editWindow.SetActive(false);
    }
}