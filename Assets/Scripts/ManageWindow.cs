using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManageWindow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private Button deleteButton;

    private string _currentTableName;
    private string _currentPrimaryKeyColumn;
    private string _currentPrimaryKeyValue;
    private DatabaseTableModifier _databaseTableModifier;

    private void Start()
    {
        _databaseTableModifier = GetComponent<DatabaseTableModifier>();

        deleteButton.onClick.AddListener(OnDeleteButtonClick);
        ClearSelection();
    }

    public void SelectRow(string tableName, string primaryKeyColumn, string primaryKeyValue)
    {
        _currentTableName = tableName;
        _currentPrimaryKeyColumn = primaryKeyColumn;
        _currentPrimaryKeyValue = primaryKeyValue;

        keyText.text = $"Выбранный ключ: {primaryKeyValue}";
        deleteButton.interactable = true;
    }

    private void OnDeleteButtonClick()
    {
        if (string.IsNullOrEmpty(_currentTableName) ||
            string.IsNullOrEmpty(_currentPrimaryKeyColumn) ||
            string.IsNullOrEmpty(_currentPrimaryKeyValue)) return;
        
        _databaseTableModifier.DeleteRow(_currentTableName, _currentPrimaryKeyColumn, _currentPrimaryKeyValue);
        ClearSelection();
    }

    private void ClearSelection()
    {
        keyText.text = "Выберите строку";
        deleteButton.interactable = false;
    }
}