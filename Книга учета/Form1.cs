using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Книга_учета;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;

namespace Книга_учета
{
    public partial class Form1 : Form
    {
        private AccountingData accountingData = new AccountingData();
        private BindingSource categoriesBindingSource = new BindingSource();
        private BindingSource transactionsBindingSource = new BindingSource();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Инициализация BindingSource и DataGridView
            categoriesBindingSource.DataSource = accountingData.Categories;
            dgvCategories.DataSource = categoriesBindingSource;
            dgvCategories.AutoGenerateColumns = false;
            // *** ИЗМЕНЕНИЯ ДЛЯ КАТЕГОРИЙ ***
            // Запрещаем редактирование ячеек напрямую
            foreach (DataGridViewColumn column in dgvCategories.Columns)
            {
                column.ReadOnly = true;
            }
            dgvCategories.AllowUserToAddRows = false; //Отключаем добавление строк
            dgvCategories.AllowUserToDeleteRows = false; //Отключаем удаление строк
            // *** КОНЕЦ ИЗМЕНЕНИЙ ДЛЯ КАТЕГОРИЙ ***

            transactionsBindingSource.DataSource = accountingData.Transactions;
            dgvTransactions.DataSource = transactionsBindingSource;
            dgvTransactions.AutoGenerateColumns = false;
            dgvTransactions.DataError += dgvTransactions_DataError;
            dgvTransactions.EditingControlShowing += dgvTransactions_EditingControlShowing; // Подписываемся на событие

            // Настройка колонок для DataGridView (транзакции)
            dgvTransactions.Columns.Clear();
            DataGridViewTextBoxColumn dateColumn = new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "Дата", Name = "Date" };
            dgvTransactions.Columns.Add(dateColumn);
            if (dgvTransactions.Columns.Contains("Date"))
            {
                dgvTransactions.Columns["Date"].DefaultCellStyle.Format = "yyyy-MM-dd";
            }

            DataGridViewTextBoxColumn descriptionColumn = new DataGridViewTextBoxColumn { DataPropertyName = "Description", HeaderText = "Описание", Name = "Description" };
            dgvTransactions.Columns.Add(descriptionColumn);

            DataGridViewTextBoxColumn amountColumn = new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "Сумма", Name = "Amount" };
            dgvTransactions.Columns.Add(amountColumn);

            DataGridViewTextBoxColumn categoryColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Category",
                HeaderText = "Категория",  // Заголовок на русском
                Name = "Category"
            };
            dgvTransactions.Columns.Add(categoryColumn);

            DataGridViewTextBoxColumn typeColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Type",
                HeaderText = "Тип",  // Заголовок на русском
                Name = "Type"
            };
            dgvTransactions.Columns.Add(typeColumn);

            // Запрещаем редактирование ячеек напрямую
            foreach (DataGridViewColumn column in dgvTransactions.Columns)
            {
                column.ReadOnly = true;
            }

            dgvTransactions.AllowUserToAddRows = false; //Отключаем добавление строк
            dgvTransactions.AllowUserToDeleteRows = false; //Отключаем удаление строк

            openFileDialog1.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            saveFileDialog1.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            // LoadDataFromFile();  // Используем LoadDataFromFile при загрузке формы - УДАЛЕНО!!!
            UpdateBalance();
            UpdateCategoryTotals();
            UpdateChart();
        }

        private void dgvTransactions_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            string columnName = "";
            if (dgvTransactions.Columns.Count > 0 && e.ColumnIndex >= 0 && e.ColumnIndex < dgvTransactions.Columns.Count)
            {
                columnName = dgvTransactions.Columns[e.ColumnIndex].Name;  // Получаем имя колонки
            }
            if (e.Exception is FormatException)
            {
                MessageBox.Show($"Некорректный формат данных в колонке '{columnName}'. Пожалуйста, выберите допустимое значение из списка.", "Ошибка формата", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.ThrowException = false;
                e.Cancel = true;
            }
            else
            {
                MessageBox.Show($"Произошла ошибка в колонке '{columnName}': {e.Exception.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.ThrowException = false;
            }
        }

        private void dgvTransactions_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            //Больше не нужно
        }


        private void LoadDataFromFile()
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog1.FileName;
                try
                {
                    string jsonData = File.ReadAllText(filePath);
                    accountingData = JsonConvert.DeserializeObject<AccountingData>(jsonData);

                    // Проверка категорий в транзакциях
                    foreach (var transaction in accountingData.Transactions.ToList()) // ToList() чтобы избежать изменения коллекции во время итерации
                    {
                        if (transaction.Category != null && !accountingData.Categories.Any(c => c.Name == transaction.Category.Name)) //Проверяем, есть ли категория с таким именем
                        {
                            // Категория не найдена
                            MessageBox.Show($"Категория '{transaction.Category.Name}' не найдена. Транзакция будет удалена.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            accountingData.Transactions.Remove(transaction); // Удаляем транзакцию
                            //transaction.Category = accountingData.Categories.FirstOrDefault(); // Или заменяем на категорию по умолчанию
                        }
                    }

                    // Обновление BindingSource и UI
                    categoriesBindingSource.DataSource = accountingData.Categories;
                    transactionsBindingSource.DataSource = accountingData.Transactions;

                    categoriesBindingSource.ResetBindings(false);
                    transactionsBindingSource.ResetBindings(false);
                    UpdateCategoryComboBox();
                    UpdateBalance();
                    UpdateCategoryTotals();
                    UpdateChart();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveDataToFile()
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog1.FileName;
                try
                {
                    string jsonData = JsonConvert.SerializeObject(accountingData, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(filePath, jsonData);
                    MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Методы для категорий
        private void btnAddCategory_Click(object sender, EventArgs e)
        {
            string name = txtNameCategory.Text.Trim();
            string description = txtDescriptionCategory.Text.Trim();

            if (!string.IsNullOrEmpty(name))
            {
                accountingData.AddCategory(new Category(name, description));
                categoriesBindingSource.ResetBindings(false);
                UpdateCategoryComboBox();
                txtNameCategory.Clear();
                txtDescriptionCategory.Clear();
                UpdateCategoryTotals();
                UpdateChart();
            }
            else
            {
                MessageBox.Show("Введите название категории.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnEditCategory_Click(object sender, EventArgs e)
        {
            if (dgvCategories.SelectedRows.Count > 0)
            {
                Category selectedCategory = (Category)dgvCategories.SelectedRows[0].DataBoundItem;
                if (selectedCategory != null)
                {
                    string oldName = selectedCategory.Name;
                    string newName = txtNameCategory.Text.Trim();
                    string newDescription = txtDescriptionCategory.Text.Trim();

                    if (!string.IsNullOrEmpty(newName))
                    {
                        Category newCategory = new Category(newName, newDescription);
                        accountingData.UpdateCategory(oldName, newCategory);
                        categoriesBindingSource.ResetBindings(false);
                        transactionsBindingSource.ResetBindings(false);
                        UpdateCategoryComboBox();
                        UpdateCategoryTotals();
                        UpdateChart();
                        txtNameCategory.Clear();
                        txtDescriptionCategory.Clear();
                    }
                    else
                    {
                        MessageBox.Show("Введите новое название категории.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите категорию для редактирования.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnDeleteCategory_Click(object sender, EventArgs e)
        {
            if (dgvCategories.SelectedRows.Count > 0)
            {
                Category selectedCategory = (Category)dgvCategories.SelectedRows[0].DataBoundItem;
                if (selectedCategory != null)
                {
                    DialogResult result = MessageBox.Show($"Вы уверены, что хотите удалить категорию '{selectedCategory.Name}'?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        accountingData.DeleteCategory(selectedCategory.Name);
                        categoriesBindingSource.ResetBindings(false);
                        transactionsBindingSource.ResetBindings(false);
                        UpdateCategoryComboBox();
                        UpdateCategoryTotals();
                        UpdateChart();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите категорию для удаления.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateCategoryComboBox()
        {
            cmbTransactionCategory.DataSource = null;
            cmbTransactionCategory.DataSource = accountingData.Categories;
            cmbTransactionCategory.DisplayMember = "Name";
            cmbTransactionCategory.ValueMember = "Name";
        }

        // Методы для транзакций
        private void btnAddTransaction_Click(object sender, EventArgs e)
        {
            if (cmbTransactionCategory.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите категорию.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Category selectedCategory = (Category)cmbTransactionCategory.SelectedItem;

            // Проверяем, существует ли выбранная категория в списке категорий
            if (!accountingData.Categories.Contains(selectedCategory))
            {
                MessageBox.Show("Выбранная категория не существует. Пожалуйста, выберите другую категорию.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime date = dtpTransactionDate.Value.Date; // Сохраняем только дату
            string description = txtTransactionDescription.Text.Trim();
            decimal amount = nudTransactionAmount.Value;

            TransactionType type = rdbTransactionExpense.Checked ? TransactionType.Expense : TransactionType.Income; // Получаем тип из RadioButton

            if (!string.IsNullOrEmpty(description))
            {
                Transaction transaction = new Transaction(date, description, amount, selectedCategory, type);

                accountingData.AddTransaction(transaction);
                transactionsBindingSource.ResetBindings(false);
                UpdateBalance();
                UpdateCategoryTotals();
                UpdateChart();
                ClearTransactionFields();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите описание.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnEditTransaction_Click(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count > 0)
            {
                Transaction selectedTransaction = (Transaction)dgvTransactions.SelectedRows[0].DataBoundItem;
                if (selectedTransaction != null)
                {
                    DateTime newDate = dtpTransactionDate.Value.Date; // Сохраняем только дату
                    string newDescription = txtTransactionDescription.Text.Trim();
                    decimal amount = nudTransactionAmount.Value;

                    TransactionType type = rdbTransactionExpense.Checked ? TransactionType.Expense : TransactionType.Income; // Получаем тип из RadioButton

                    Category selectedCategory = (Category)cmbTransactionCategory.SelectedItem;

                    if (selectedCategory != null && !string.IsNullOrEmpty(newDescription))
                    {
                        selectedTransaction.Date = newDate;
                        selectedTransaction.Description = newDescription;
                        selectedTransaction.Amount = amount;
                        selectedTransaction.Category = selectedCategory;
                        selectedTransaction.Type = type;

                        dgvTransactions.Refresh();
                        transactionsBindingSource.ResetBindings(false);

                        UpdateBalance();
                        UpdateCategoryTotals();
                        UpdateChart();
                        ClearTransactionFields();
                    }
                    else
                    {
                        MessageBox.Show("Пожалуйста, выберите категорию и введите описание.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите транзакцию для редактирования.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnDeleteTransaction_Click(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count > 0)
            {
                Transaction selectedTransaction = (Transaction)dgvTransactions.SelectedRows[0].DataBoundItem;
                if (selectedTransaction != null)
                {
                    DialogResult result = MessageBox.Show($"Вы уверены, что хотите удалить транзакцию '{selectedTransaction.Description}'?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        accountingData.DeleteTransaction(selectedTransaction.Date, selectedTransaction.Description, selectedTransaction.Amount);
                        transactionsBindingSource.ResetBindings(false);
                        UpdateBalance();
                        UpdateCategoryTotals();
                        UpdateChart();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите транзакцию для удаления.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClearTransactionFields()
        {
            txtTransactionDescription.Clear();
            nudTransactionAmount.Value = 0;
            rdbTransactionIncome.Checked = true;
        }

        private void UpdateBalance()
        {
            decimal balance = accountingData.CalculateBalance();
            lblBalance.Text = $"Баланс: {balance:C}";
        }

        private void UpdateCategoryTotals()
        {
            Dictionary<string, decimal> categoryTotals = accountingData.CalculateCategoryTotals();

            // Вывод в ListBox
            lstCategoryTotals.Items.Clear();
            foreach (var kvp in categoryTotals)
            {
                lstCategoryTotals.Items.Add($"{kvp.Key}: {kvp.Value:C}");
            }
            UpdateChart();
        }

        private void UpdateChart()
        {
            chtCategoryTotals.Series.Clear();

            Series series = new Series("Category Totals");
            series.ChartType = SeriesChartType.Pie;

            Dictionary<string, decimal> chartData = accountingData.GetDataForChart();

            foreach (var kvp in chartData)
            {
                series.Points.AddXY(kvp.Key, kvp.Value);
            }

            chtCategoryTotals.Series.Add(series);
        }

        private void btnSaveTransactions_Click(object sender, EventArgs e)
        {
            SaveDataToFile();
        }

        private void btnLoadTransactions_Click(object sender, EventArgs e)
        {
            LoadDataFromFile();
        }

        // Обработчики событий для текстовых полей (можно добавлять валидацию)
        private void txtNameCategory_TextChanged(object sender, EventArgs e) { }
        private void txtDescriptionCategory_TextChanged(object sender, EventArgs e) { }
        private void dtpTransactionDate_ValueChanged(object sender, EventArgs e) { }
        private void cmbTransactionCategory_SelectedIndexChanged(object sender, EventArgs e) { }
        private void txtTransactionDescription_TextChanged(object sender, EventArgs e) { }
        private void nudTransactionAmount_ValueChanged(object sender, EventArgs e) { }
        private void rdbTransactionIncome_CheckedChanged(object sender, EventArgs e) { }
        private void rdbTransactionExpense_CheckedChanged(object sender, EventArgs e) { }

        private void btnCalculateBalance_Click(object sender, EventArgs e)
        {
            UpdateBalance();
            UpdateCategoryTotals();
            UpdateChart();
        }
    }
}