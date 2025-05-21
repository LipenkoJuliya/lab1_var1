using System;

namespace Книга_учета
{
    // Класс для финансовой операции.
    public class Transaction
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public Category Category { get; set; } // Ссылка на категорию
        public TransactionType Type { get; set; } // Доход или расход

        public Transaction() { } // Обязательный конструктор без параметров для десериализации JSON

        public Transaction(DateTime date, string description, decimal amount, Category category, TransactionType type)
        {
            Date = date;
            Description = description;
            Amount = amount;
            Category = category;
            Type = type;
        }

        public override string ToString()
        {
            return $"{Date.ToShortDateString()} - {Description} - {Amount:C} ({Category})";
        }
    }

    // Тип операции
    public enum TransactionType
    {
        Income,
        Expense
    }
}
