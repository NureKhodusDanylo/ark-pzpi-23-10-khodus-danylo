
/*=====В.1 Код до застосування методу Remove Setting Method===== */

// Проблема: Публічний сетер дозволяє змінювати важливе поле, яке має бути незмінним.
class Order {
    public orderId: string;
    public amount: number;

    constructor(orderId: string, amount: number) {
        this.orderId = orderId;
        this.amount = amount;
    }

    // Цей метод дозволяє будь-кому ззовні змінити ID замовлення, що є небезпечним.
    public setOrderId(newId: string): void {
        console.log(`Order ID змінено з ${this.orderId} на ${newId}`);
        this.orderId = newId;
    }
}

const myOrder = new Order("ID-123", 100);
myOrder.setOrderId("ID-999");

/*=====В.2 Код після застосування методу Remove Setting Method===== */
// Рішення: Видалити сетер і зробити поле доступним лише для читання.
class Order {
    public readonly orderId: string; // Модифікатор readonly
    public amount: number;

    constructor(orderId: string, amount: number) {
        // Значення присвоюється лише один раз у конструкторі.
        this.orderId = orderId;
        this.amount = amount;
    }
}

const myOrder = new Order("ID-123", 100);
// Тепер спроба змінити ID призведе до помилки компіляції:
// myOrder.orderId = "ID-999"; // Error: Cannot assign to 'orderId' because it is a read-only property.

/*=====В.3 Код до застосування методу Hide Method===== */

// Проблема: Допоміжні методи є публічними, що засмічує API класу.
class ReportGenerator {
    // Основний метод, призначений для зовнішнього використання.
    public generateReport(): string {
        const data = this.fetchDataFromDB();
        const formattedData = this.formatData(data);
        return `Звіт: ${formattedData}`;
    }

    // Цей метод є деталлю реалізації і не повинен викликатися ззовні.
    public fetchDataFromDB(): string {
        // ... 
        return "Необроблені дані";
    }

    // Цей метод також є внутрішнім.
    public formatData(data: string): string {
        // ... 
        return data.toUpperCase();
    }
}

const report = new ReportGenerator();
// Зовнішній код може викликати внутрішні методи, що є поганою практикою.
const rawData = report.fetchDataFromDB();

/*=====В.4 Код після застосування методу Hide Method===== */

// Рішення: Зробити допоміжні методи приватними.
class ReportGenerator {
    public generateReport(): string {
        const data = this.fetchDataFromDB();
        const formattedData = this.formatData(data);
        return `Звіт: ${formattedData}`;
    }

    // Метод тепер приватний і доступний лише всередині цього класу.
    private fetchDataFromDB(): string {
        // ... 
        return "Необроблені дані";
    }

    // Цей метод також приховано.
    private formatData(data: string): string {
        // ... 
        return data.toUpperCase();
    }
}

const report = new ReportGenerator();
report.generateReport();
// Тепер виклик внутрішніх методів ззовні призведе до помилки компіляції.
// const rawData = report.fetchDataFromDB(); // Error: Property 'fetchDataFromDB' is private.

/*=====В.5 Код до застосування методу Remove Parameter===== */

// Проблема: Метод приймає параметр, значення якого вже є у класі.
class User {
    public username: string;

    constructor(username: string) {
        this.username = username;
    }

    // Параметр `name` є надлишковим, оскільки це те саме, що `this.username`.
    public createWelcomeMessage(name: string): string {
        return `Ласкаво просимо, ${name}!`;
    }
}

const user = new User("Alice");
// Незручний виклик: доводиться передавати дані, які об'єкт вже має.
const message = user.createWelcomeMessage(user.username);
console.log(message);

/*=====В.6 Код після застосування методу Remove Parameter===== */

// Рішення: Видалити зайвий параметр і використовувати стан об'єкта.
class User {
    public username: string;

    constructor(username: string) {
        this.username = username;
    }

    // Метод тепер не приймає параметрів і використовує `this.username`.
    public createWelcomeMessage(): string {
        return `Ласкаво просимо, ${this.username}!`;
    }
}

const user = new User("Alice");
// Виклик став набагато простішим і логічнішим.
const message = user.createWelcomeMessage();
console.log(message);