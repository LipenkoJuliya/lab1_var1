using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Книга_учета
{
    // Класс для хранения всех данных
    public class AccountingData
    {
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();

        // Конструктор
        public AccountingData() { }

        // Методы CRUD для категорий
        public void AddCategory(Category category)
        {
            if (!Categories.Any(c => c.Name == category.Name))
            {
                Categories.Add(category);
            }
            else
            {
                Console.WriteLine("Категория с таким именем уже существует.");
            }
        }

        public void UpdateCategory(string oldName, Category newCategory)
        {
            Category existingCategory = Categories.FirstOrDefault(c => c.Name == oldName);
            if (existingCategory != null)
            {
                existingCategory.Name = newCategory.Name;
                existingCategory.Description = newCategory.Description;
            }
            else
            {
                Console.WriteLine("Категория не найдена.");
            }
        }

        public void DeleteCategory(string categoryName)
        {
            Category categoryToRemove = Categories.FirstOrDefault(c => c.Name == categoryName);
            if (categoryToRemove != null)
            {
                Categories.Remove(categoryToRemove);
                // Удалить все транзакции, связанные с этой категорией.  Важно!
                Transactions.RemoveAll(t => t.Category.Name == categoryName);
            }
            else
            {
                Console.WriteLine("Категория не найдена.");
            }
        }


        // Методы CRUD для операций
        public void AddTransaction(Transaction transaction)
        {
            Transactions.Add(transaction);
        }

        public void UpdateTransaction(DateTime oldDate, string oldDescription, decimal oldAmount, Transaction newTransaction)
        {
            Transaction existingTransaction = Transactions.FirstOrDefault(t => t.Date == oldDate && t.Description == oldDescription && t.Amount == oldAmount);
            if (existingTransaction != null)
            {
                existingTransaction.Date = newTransaction.Date;
                existingTransaction.Description = newTransaction.Description;
                existingTransaction.Amount = newTransaction.Amount;
                existingTransaction.Category = newTransaction.Category;
                existingTransaction.Type = newTransaction.Type;
            }
            else
            {
                Console.WriteLine("Транзакция не найдена.");
            }
        }

        public void DeleteTransaction(DateTime date, string description, decimal amount)
        {
            Transaction transactionToRemove = Transactions.FirstOrDefault(t => t.Date == date && t.Description == description && t.Amount == amount);
            if (transactionToRemove != null)
            {
                Transactions.Remove(transactionToRemove);
            }
            else
            {
                Console.WriteLine("Транзакция не найдена.");
            }
        }

        // Подсчет баланса
        public decimal CalculateBalance()
        {
            decimal income = Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            decimal expense = Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            return income - expense;
        }

        // Подсчет суммы по категориям
        public Dictionary<string, decimal> CalculateCategoryTotals()
        {
            return Transactions.GroupBy(t => t.Category.Name)
                                   .ToDictionary(g => g.Key, g => g.Sum(t => (t.Type == TransactionType.Income ? t.Amount : -t.Amount)));
        }

        // Сохранение в JSON
        public void SaveToJson(string filePath)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json);
        }

        // Загрузка из JSON
        public static AccountingData LoadFromJson(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<AccountingData>(json);
            }
            else
            {
                Console.WriteLine("Файл не найден. Создан новый экземпляр AccountingData.");
                return new AccountingData(); // Возвращаем новый экземпляр, если файл не найден
            }
        }

        // Подготовка данных для графика (пример)
        public Dictionary<string, decimal> GetDataForChart()
        {
            return CalculateCategoryTotals();  // Используем уже посчитанные суммы по категориям
        }
    }
}