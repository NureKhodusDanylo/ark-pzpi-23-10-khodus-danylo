
/* ===== В.1 ===== */
// Погано
/src
|-- api.ts            // Усі запити до сервера в одному файлі
|-- auth.ts           // Логіка автентифікації
|-- Button.tsx        // Компонент кнопки
|-- UserProfile.tsx   // Компонент профілю користувача
|-- ProductCard.tsx   // Компонент картки товару
|-- utils.ts          // Різноманітні допоміжні функції
|-- types.ts          // Усі типи та інтерфейси для всього проєкту
|-- main.ts


// Добре
/src
|-- app/                // Глобальні налаштування, стилі, провайдери
|-- pages/              // Компоненти, що відповідають за цілі сторінки
|-- features/           // Окремий бізнес-функціонал
|   |-- auth/           // Все, що стосується автентифікації
|   |   |-- ui/         // Компоненти (LoginForm, RegisterForm)
|   |   |-- model/      // Логіка, типи, хуки (useAuth, authSlice.ts)
|   |   `-- api/        // Запити (login.ts, logout.ts)
|-- shared/             // Перевикористовувані елементи для всього проєкту
|   |-- ui/             // Загальні компоненти (Button.tsx, Input.tsx)
|   |-- lib/            // Допоміжні функції (formatDate.ts)
|   |-- api/            // Базові налаштування API (axiosInstance.ts)
|   `-- types/          // Глобальні типи (index.ts)
`-- main.ts`

/* ===== В.2 ===== */
// Погано
// Константа названа як звичайна змінна
const admin_user_id = 1;

// Тип названо в snake_case, що робить його схожим на змінну
type user_profile = {
  name: string;
  age: number;
};

// Клас названо в camelCase
class dataFetcher {
  // Метод названо в PascalCase, наче це клас
  async FetchData(id: number): Promise<user_profile> {
    // ...
  }
}


// Добре
// Константа, значення якої незмінне протягом роботи програми
const ADMIN_USER_ID = 1;

// Інтерфейс (або тип) названо в PascalCase
interface UserProfile {
  name: string;
  age: number;
}

// Клас названо в PascalCase
class DataFetcher {
  // Метод названо в camelCase
  async fetchData(id: number): Promise<UserProfile> {
    // ...
  }
}

/* ===== В.3 ===== */

// Погано
function GetUserDetails(user: {id:number; name: string, isActive:boolean}){
    if(user.isActive){
return `Користувач "${user.name}" (ID: ${user.id}) активний`;
    } else {
        return "Користувач неактивний"
    }
}

// Добре
function getUserDetails(user: {
  id: number;
  name: string;
  isActive: boolean;
}) {
  if (user.isActive) {
    return `Користувач '${user.name}' (ID: ${user.id}) активний`;
  } else {
    return 'Користувач неактивний';
  }
}

/* ===== В.4 ===== */

// Погано
// Використання `any` дозволяє передати що завгодно,
// що може призвести до помилки під час виконання.
function displayUser(user: any) {
  // Якщо в user не буде поля `address`, програма впаде з помилкою
  console.log(`Ім'я: ${user.name}, Місто: ${user.address.city}`);
}

const userData = {
  name: 'Іван',
  // Поле address відсутнє
};

displayUser(userData); // Помилки на етапі компіляції не буде, але буде в рантаймі

// Добре
// Створюємо чіткі "контракти" для наших даних
interface Address {
  city: string;
  street: string;
}

interface User {
  name: string;
  address: Address;
}

// Функція тепер чітко декларує, що очікує об'єкт типу User
function displayUser(user: User) {
  console.log(`Ім'я: ${user.name}, Місто: ${user.address.city}`);
}

const userData = {
  name: 'Іван',
  // Поле address відсутнє
};

// TypeScript НЕ дозволить скомпілювати цей код,
// оскільки userData не відповідає інтерфейсу User.
// Error: Argument of type '{ name: string; }' is not assignable
// to parameter of type 'User'. Property 'address' is missing.
displayUser(userData);


/* ===== В.5 ===== */
// Погано
// Функція для додавання двох чисел
function add(a: number, b: number) {
  // Створюємо змінну для результату
  const result = a + b; // додаємо a до b
  // Повертаємо результат
  return result;
}

// Викликаємо функцію
add(5, 10);

// Добре

/**
 * Обчислює вартість замовлення з урахуванням ПДВ та персональної знижки клієнта.
 *
 * @remarks
 * Ця функція не застосовує знижку, якщо сума замовлення менша за мінімальний поріг.
 *
 * @param amount - Початкова сума замовлення.
 * @param discountRate - Відсоток персональної знижки клієнта (наприклад, 0.1 для 10%).
 * @returns Фінальна вартість замовлення.
 */
function calculateOrderTotal(amount: number, discountRate: number): number {
  const VAT_RATE = 0.2; // 20% ПДВ
  const MINIMUM_ORDER_FOR_DISCOUNT = 1000;

  let total = amount * (1 + VAT_RATE);

  // Чому ми перевіряємо саме total, а не amount?
  // Бізнес-вимога: знижка застосовується до суми з ПДВ, щоб стимулювати більші покупки.
  if (total > MINIMUM_ORDER_FOR_DISCOUNT) {
    total -= total * discountRate;
  }

  return total;
}

/* ===== В.6 ===== */
// Погано
async function fetchUserData(userId: number) {
  try {
    const response = await fetch(`https://api.example.com/users/${userId}`);
    const data = await response.json();
    return data;
  } catch (error) {
    // Помилку "проковтнули". Ми просто виводимо її в консоль,
    // але для решти програми вона зникає.
    console.log('Щось пішло не так');
    return null; // Повернення null приховує причину збою.
  }
}

// Уявімо, що сервер недоступний. fetchUserData поверне null.
const user = await fetchUserData(1);
if (!user) {
  // Ми знаємо, що щось не так, але не знаємо, що саме:
  // користувача не існує? Сервер впав? Проблеми з мережею?
  showGenericErrorMessage();
}

// Добре
// Можна створити власний клас для специфічних помилок
class ApiError extends Error {
  constructor(message: string, public status: number) {
    super(message);
    this.name = 'ApiError';
  }
}

async function fetchUserData(userId: number) {
  const response = await fetch(`https://api.example.com/users/${userId}`);

  // Завжди перевіряємо, чи був запит успішним
  if (!response.ok) {
    // Створюємо інформативну помилку і "викидаємо" її
    throw new ApiError(
      `Не вдалося завантажити дані користувача.`,
      response.status
    );
  }

  return response.json();
}

try {
  const user = await fetchUserData(1);
  displayUserData(user);
} catch (error) {
  // Тепер ми можемо обробити помилку більш осмислено
  if (error instanceof ApiError) {
    if (error.status === 404) {
      showNotFoundMessage('Такого користувача не існує.');
    } else {
      showServerError(`Виникла помилка сервера: ${error.status}`);
    }
  } else {
    // Для непередбачуваних помилок
    showGenericErrorMessage();
  }
}


/* ===== В.7 ===== */
// Погано
interface Product {
  id: number;
  price: number;
  isActive: boolean;
}

// Уявімо, що тут 100,000 товарів
const products: Product[] = getLargeArrayOfProducts();

function calculateTotal(products: Product[]): number {
  // 1. Створюється ПЕРШИЙ проміжний масив (наприклад, з 80,000 товарів)
  const activeProducts = products.filter(p => p.isActive);

  // 2. Створюється ДРУГИЙ проміжний масив (наприклад, з 10,000 товарів)
  const expensiveProducts = activeProducts.filter(p => p.price > 100);

  // 3. Створюється ТРЕТІЙ проміжний масив з цінами
  const prices = expensiveProducts.map(p => p.price);

  // 4. Обчислюється фінальна сума
  const total = prices.reduce((sum, price) => sum + price, 0);

  return total;
}

// Добре
interface Product {
  id: number;
  price: number;
  isActive: boolean;
}

const products: Product[] = getLargeArrayOfProducts();

function calculateTotal(products: Product[]): number {
  // Усі операції виконуються за один прохід по масиву
  return products.reduce((sum, product) => {
    if (product.isActive && product.price > 100) {
      return sum + product.price;
    }
    return sum; // Якщо умова не виконується, просто повертаємо поточну суму
  }, 0);
}

/* ===== В.8 ===== */
// Погано
/**
 * Розраховує вартість доставки.
 * - Для Києва доставка безкоштовна при замовленні від 1000 грн.
 * - Для інших міст - при замовленні від 2000 грн.
 * - Стандартна вартість: 70 грн для Києва, 120 грн для інших міст.
 */
function calculateShippingCost(orderAmount: number, region: 'Kyiv' | 'Other') {
  if (region === 'Kyiv') {
    if (orderAmount >= 1000) {
      return 0; // Безкоштовна доставка
    }
    return 70;
  } else {
    // Для інших регіонів
    if (orderAmount > 2000) { // Помилка: має бути >=
      return 0;
    }
    return 120;
  }
}

// Добре
// Файл: calculateShippingCost.test.ts
// Спочатку імпортуємо нашу функцію
import { calculateShippingCost } from './calculateShippingCost';

// describe - це блок, що об'єднує групу тестів
describe('calculateShippingCost', () => {
  // Кожен 'it' - це окремий тестовий випадок
  it('має повертати 0 для Києва, якщо сума >= 1000', () => {
    expect(calculateShippingCost(1000, 'Kyiv')).toBe(0);
    expect(calculateShippingCost(1500, 'Kyiv')).toBe(0);
  });

  it('має повертати 70 для Києва, якщо сума < 1000', () => {
    expect(calculateShippingCost(999, 'Kyiv')).toBe(70);
  });

  it('має повертати 0 для інших регіонів, якщо сума >= 2000', () => {
    // Цей тест ВПАДЕ і вкаже на помилку в логіці!
    expect(calculateShippingCost(2000, 'Other')).toBe(0);
    expect(calculateShippingCost(2500, 'Other')).toBe(0);
  });

  it('має повертати 120 для інших регіонів, якщо сума < 2000', () => {
    expect(calculateShippingCost(1999, 'Other')).toBe(120);
  });
});
