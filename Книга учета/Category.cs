using System;

namespace Книга_учета
{
    // Класс для категории операции
    public class Category
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public Category() { } // Обязательный конструктор без параметров для десериализации JSON
        public Category(string name, string description = "")
        {
            Name = name;
            Description = description;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}