# IoT Robot Delivery System (ESP32 MicroPython)

Програмний модуль IoT для автономних роботів-кур'єрів та дронів, що працює на платформі ESP32 з використанням MicroPython.

## Огляд Системи

Цей IoT модуль є частиною системи RobDeliveryAPI та забезпечує:
- Автономне управління роботом-доставником
- GPS навігацію та симуляцію руху
- Управління батареєю та енергоспоживанням
- Телеметрію в реальному часі
- Інтеграцію з серверним API

## Архітектура

### Структура Проекту

```
IotDronePi/
├── config/
│   └── config.py          # Конфігурація (WiFi, API, credentials)
├── core/
│   └── robot.py           # Клас Robot з характеристиками
├── modules/
│   ├── wifi_manager.py    # Управління WiFi підключенням
│   ├── auth_manager.py    # Аутентифікація робота
│   ├── gps_simulator.py   # GPS навігація та симуляція руху
│   ├── battery_manager.py # Управління батареєю
│   ├── telemetry.py       # Відправка телеметрії на сервер
│   └── order_manager.py   # Управління замовленнями
├── utils/
│   └── helpers.py         # Допоміжні функції
├── main.py                # Головний файл з основною логікою
├── diagram.json           # Конфігурація Wokwi емулятора
└── README.md              # Документація
```

### Ключові Компоненти

#### 1. Robot (core/robot.py)
Базовий клас робота з характеристиками:
- **Типи роботів**: Drone, GroundCourier
- **Стани**: Idle, Delivering, Returning, Charging, Maintenance
- **Характеристики**: Ємність батареї, споживання енергії, швидкість
- **Локація**: GPS координати, поточний та цільовий вузол

#### 2. WiFiManager (modules/wifi_manager.py)
Управління WiFi підключенням:
- Автоматичне підключення до мережі
- Повторне підключення при розриві
- Моніторинг якості сигналу

#### 3. AuthManager (modules/auth_manager.py)
Аутентифікація з сервером:
- JWT токен аутентифікація
- SerialNumber + AccessKey
- Автоматичне оновлення токену

#### 4. GPSSimulator (modules/gps_simulator.py)
Навігація та симуляція руху:
- Розрахунок маршрутів
- Симуляція GPS координат
- Відстежування прогресу до пункту призначення

#### 5. BatteryManager (modules/battery_manager.py)
Управління батареєю:
- Моніторинг рівня заряду
- Симуляція розряду при русі
- Автоматичне заряджання при низькому рівні

#### 6. TelemetryManager (modules/telemetry.py)
Телеметрія:
- Періодична відправка статусу
- Синхронізація з сервером
- Обробка помилок з'єднання

#### 7. OrderManager (modules/order_manager.py)
Управління замовленнями:
- Отримання призначених замовлень
- Виконання доставки (pickup → dropoff)
- Завершення місій

## Конфігурація

### config/config.py

```python
# WiFi Configuration
WIFI_CONFIG = {
    "SSID": "Wokwi-GUEST",
    "PASSWORD": ""
}

# API Configuration
API_CONFIG = {
    "BASE_URL": "https://your-api-server.com",
    "AUTH_ENDPOINT": "/api/Auth/robot-login",
    "ROBOT_STATUS_ENDPOINT": "/api/Robot/status",
    "ROBOT_ME_ENDPOINT": "/api/Robot/me",
}

# Robot Credentials (ВАЖЛИВО: налаштуйте для кожного робота!)
ROBOT_CONFIG = {
    "SERIAL_NUMBER": "ESP32-DRONE-001",
    "ACCESS_KEY": "your_secret_key_here"
}

# Robot Characteristics
ROBOT_CHARACTERISTICS = {
    "TYPE": "Drone",  # "Drone" або "GroundCourier"
    "BATTERY_CAPACITY_JOULES": 360000,  # 100Wh
    "ENERGY_CONSUMPTION_PER_METER": 36,  # Джоулів на метр
    "MAX_SPEED_MS": 10.0,  # метрів за секунду
    "MIN_BATTERY_LEVEL": 20.0
}
```

## Встановлення та Запуск

### Вимоги
- ESP32 плата
- MicroPython прошивка
- Доступ до Інтернету (WiFi)
- Налаштований сервер RobDeliveryAPI

### Крок 1: Налаштування Wokwi Емулятора

1. Відкрийте [Wokwi Simulator](https://wokwi.com/)
2. Створіть новий проект ESP32
3. Завантажте всі файли з папки `IotDronePi`
4. Завантажте `diagram.json`

### Крок 2: Конфігурація

Відредагуйте `config/config.py`:
- Вкажіть WiFi SSID та пароль (для Wokwi використовуйте "Wokwi-GUEST")
- Вкажіть URL вашого API сервера
- **ВАЖЛИВО**: Налаштуйте SERIAL_NUMBER та ACCESS_KEY для вашого робота

### Крок 3: Автоматична Реєстрація/Авторизація

**ВАЖЛИВО**: IoT модуль тепер **автоматично** реєструє робота при першому запуску!

При запуску `main.py`:
1. **Спочатку спроба реєстрації** (`POST /api/Auth/robot/register`)
   - Якщо успішно → робот зареєстрований, отримано JWT токен
2. **Якщо робот вже існує** → автоматичний логін (`POST /api/Auth/robot/login`)
   - Використовуються ті ж SerialNumber та AccessKey
   - Отримується JWT токен

**Налаштування в config.py**:
```python
ROBOT_CONFIG = {
    "SERIAL_NUMBER": "ESP32-DRONE-001",
    "ACCESS_KEY": "secret_robot_key_12345",
    "TYPE": "Drone"  # або "GroundCourier"
}
```

**При реєстрації автоматично встановлюються**:
- Name: `"{TYPE}-{останні_3_цифри_SerialNumber}"` (наприклад, "Drone-001")
- Model: `"ESP32-{TYPE}"` (наприклад, "ESP32-Drone")
- BatteryCapacityJoules: 360000 (100Wh)
- EnergyConsumptionPerMeterJoules: 36 (10km range)

**Ручна реєстрація НЕ потрібна** - просто запустіть IoT модуль!

### Крок 4: Запуск

1. Завантажте всі файли на ESP32
2. Запустіть `main.py`
3. Спостерігайте за логами в Serial Monitor

## Робота Системи

### Цикл Роботи

1. **Ініціалізація**
   - Підключення до WiFi
   - Аутентифікація з сервером
   - Отримання інформації про робота
   - Відправка початкового статусу

2. **Основний Цикл**
   - Моніторинг WiFi підключення
   - Оновлення батареї
   - Оновлення GPS позиції (якщо рухається)
   - Відправка телеметрії кожні 5 секунд
   - Обробка замовлень

3. **Demo Mode**
   - Автоматичний запуск тестової доставки кожні 60 секунд
   - Симуляція руху від pickup до dropoff точки

### Потік Доставки

```
Idle → Start Order → Navigate to Pickup → Arrived at Pickup →
Navigate to Dropoff → Arrived at Dropoff → Complete Order → Idle
```

### Управління Батареєю

- **Нормальний режим**: Батарея розряджається при русі (36J на метр)
- **Низький рівень (< 20%)**: Робот припиняє роботу та починає заряджання
- **Критичний рівень (< 10%)**: Екстрене заряджання, скасування активних замовлень
- **Заряджання**: 2% за секунду

## API Інтеграція

### Endpoints, що використовуються IoT модулем

#### 1. Robot Authentication
```http
POST /api/Auth/robot/login
Content-Type: application/json

{
  "serialNumber": "ESP32-DRONE-001",
  "accessKey": "your_secret_key_here"
}

Response: {
  "message": "Robot login successful",
  "robotId": 1,
  "serialNumber": "ESP32-DRONE-001",
  "robotName": "Drone-001",
  "token": "jwt_token"
}
```

#### 2. Send Telemetry
```http
POST /api/Robot/status
Authorization: Bearer {robot_jwt_token}
Content-Type: application/json

{
  "status": "Delivering",
  "batteryLevel": 85.5,
  "currentNodeId": null,
  "currentLatitude": 50.0001,
  "currentLongitude": 36.0001,
  "targetNodeId": 5
}
```

#### 3. Fetch Robot Info
```http
GET /api/Robot/me
Authorization: Bearer {robot_jwt_token}

Response: {
  "id": 1,
  "name": "Drone-001",
  "status": "Idle",
  "batteryLevel": 100,
  ...
}
```

## Розробка та Налагодження

### Увімкнення Debug режиму

В `config/config.py`:
```python
DEBUG = True
```

### Логування

Система використовує форматоване логування:
```
[HH:MM:SS] [LEVEL] Message
```

Рівні логування:
- **INFO**: Інформаційні повідомлення
- **DEBUG**: Детальна інформація (тільки при DEBUG=True)
- **WARNING**: Попередження
- **ERROR**: Помилки

### Симуляція GPS

GPS симулятор використовує формулу Haversine для розрахунку відстаней та курсу.
Рух симулюється з швидкістю `MAX_SPEED_MS` метрів за секунду.

### Тестування

1. **WiFi підключення**: Перевірте, що ESP32 підключається до WiFi
2. **Аутентифікація**: Перевірте логи аутентифікації
3. **Телеметрія**: Використовуйте Admin Panel або Swagger для перевірки статусу
4. **Demo Delivery**: Спостерігайте за виконанням тестової доставки

## Troubleshooting

### Помилки підключення WiFi
- Перевірте SSID та пароль
- Для Wokwi використовуйте "Wokwi-GUEST" без пароля

### Помилки аутентифікації
- Перевірте, що робот зареєстрований на сервері
- Перевірте SerialNumber та AccessKey
- Переконайтесь, що сервер доступний

### Помилки телеметрії
- Перевірте JWT токен
- Перевірте формат даних (batteryLevel: 0-100)
- Перевірте доступність API endpoint

### Проблеми з GPS
- GPS симулятор працює коректно при START_LATITUDE/LONGITUDE
- Перевірте, що координати валідні (-90 до 90, -180 до 180)

## Особливості Реалізації

### Енергоспоживання

Формула розрахунку витрат батареї:
```
Energy_consumed = Distance_meters * ENERGY_CONSUMPTION_PER_METER
Remaining_percentage = (Remaining_energy / Battery_capacity) * 100
```

### Навігація

Використовується формула Haversine для розрахунку:
- Відстані між двома GPS точками
- Курсу (bearing) до цільової точки
- Нових координат після руху

### Оптимізація

- Телеметрія відправляється кожні 5 секунд (налаштовується)
- GPS оновлюється кожні 2 секунди при русі
- Батарея перевіряється в кожній ітерації циклу

## Розширення Функціоналу

### Додавання Сенсорів

1. Додайте нові піни в `diagram.json`
2. Створіть новий модуль в `modules/`
3. Інтегруйте в `main.py`

### Інтеграція з Real GPS

Замініть `GPSSimulator` на модуль реального GPS:
```python
from gps_module import RealGPS
self.gps = RealGPS(robot)
```

### Додавання Камери

Для систем з камерою, додайте модуль розпізнавання перешкод.

## Ліцензія

© 2024 RobDelivery Team. Всі права захищені.

## Автор

Khodus Danylo (ark-pzpi-23-10-khodus-danylo)

## Контакти

Для питань та підтримки звертайтесь до команди розробників RobDeliveryAPI.
