# Запуск в Wokwi Эмуляторе

## Что загружать в эмулятор

### Обязательные файлы:

```
IotDronePi/
├── main.py                    ⭐ ГЛАВНЫЙ ФАЙЛ (FSM версия)
├── diagram.json               ⭐ СХЕМА WOKWI
│
├── config/
│   ├── config.py             ⭐ НАСТРОЙКИ (WiFi, API, Robot)
│   └── hardware_config.py    ⭐ GPIO конфигурация
│
├── core/
│   ├── robot.py              ⭐ Класс Robot
│   └── state_machine.py      ⭐ FSM (машина состояний)
│
├── modules/
│   ├── wifi_manager.py       ⭐ WiFi подключение
│   ├── auth_manager.py       ⭐ Аутентификация
│   ├── gps_simulator.py      ⭐ GPS навигация
│   ├── battery_manager.py    ⭐ Управление батареей
│   ├── telemetry.py          ⭐ Телеметрия
│   ├── order_manager.py      ⭐ Управление заказами (UPDATED)
│   └── hardware_controller.py⭐ GPIO контроллер (NEW)
│
└── utils/
    └── helpers.py            ⭐ Утилиты
```

**ИТОГО: 14 файлов**

---

## Что было изменено:

✅ **main.py** - теперь это `main_fsm.py` (с FSM вместо demo mode)
❌ **main_old_demo.py** - старая версия (НЕ загружать!)

---

## Настройка config.py перед загрузкой

### 1. WiFi Configuration (для Wokwi)
```python
WIFI_CONFIG = {
    "SSID": "Wokwi-GUEST",      # Для Wokwi оставить как есть
    "PASSWORD": ""               # Без пароля для Wokwi
}
```

### 2. API Configuration
```python
API_CONFIG = {
    "BASE_URL": "http://localhost:5102",  # ⚠️ ИЗМЕНИТЬ на ваш сервер!
    "AUTH_ENDPOINT": "/api/Auth/robot-login",
    "ROBOT_STATUS_ENDPOINT": "/api/Robot/status",
    "ROBOT_ME_ENDPOINT": "/api/Robot/me",
}
```

**ВАЖНО:** Для Wokwi эмулятора нужно использовать внешний URL (не localhost)!
- Если сервер на вашем компьютере, используйте ваш локальный IP: `http://192.168.x.x:5102`
- Или используйте ngrok для туннеля: `https://xxx.ngrok.io`

### 3. Robot Credentials
```python
ROBOT_CONFIG = {
    "SERIAL_NUMBER": "ESP32-DRONE-001",     # ⚠️ Уникальный для каждого дрона
    "ACCESS_KEY": "secret_key_12345"        # ⚠️ Секретный ключ
}
```

---

## Diagram.json - GPIO Configuration

Убедитесь что в `diagram.json` настроены пины:

```json
{
  "connections": [
    ["esp:25", "motor:VCC"],           // GPIO25 - моторы
    ["esp:26", "servo:PWM"],           // GPIO26 - отсек (servo)
    ["esp:27", "button:OUT"],          // GPIO27 - кнопка
    ["esp:32", "led1:A"],              // GPIO32 - status LED
    ["esp:33", "led2:A"]               // GPIO33 - battery LED
  ]
}
```

---

## Порядок загрузки в Wokwi

### Вариант 1: Через Web интерфейс

1. Открыть https://wokwi.com/
2. Создать новый проект ESP32
3. Загрузить все 14 файлов в правильную структуру папок
4. Загрузить `diagram.json`
5. Нажать "Start Simulation"

### Вариант 2: Через Wokwi CLI (если есть)

```bash
cd IotDronePi
wokwi-cli upload
```

---

## Что происходит при запуске:

```
[00:00:00] ================================================
[00:00:00] IoT Robot Delivery System (FSM) Starting...
[00:00:00] ================================================
[00:00:00] Initializing robot subsystems...
[00:00:01] WiFi: Connecting to Wokwi-GUEST...
[00:00:03] WiFi: Connected! IP: 192.168.1.100
[00:00:03] Authenticating with server...
[00:00:04] Authentication successful! Robot ID: 5
[00:00:04] FSM initialized in IDLE state
[00:00:05] Hardware controller initialized (hardware=True)
[00:00:05] Robot initialization complete!
[00:00:05] ================================================
[00:00:05] Entering main control loop with FSM...
[00:00:05] State transition: IDLE -> CHECK_ORDERS
[00:00:06] Fetched 0 order(s) from server
[00:00:06] State transition: CHECK_ORDERS -> IDLE
[00:00:16] State transition: IDLE -> CHECK_ORDERS    # Проверка каждые 10s
...
```

---

## Если заказ назначен:

```
[00:01:25] Fetched 1 order(s) from server
[00:01:25] State transition: CHECK_ORDERS -> ORDER_ASSIGNED
[00:01:26] Accepting order 123...
[00:01:26] Order 123 accepted: Order accepted successfully
[00:01:26] Order 123 started: Package-ABC (2.5 kg)
[00:01:26] Route: 5 waypoints, 1500m total, 12.5% battery
[00:01:26] State transition: ORDER_ASSIGNED -> MOTORS_ON
[00:01:27] Starting motors...
[00:01:28] Motors started
[00:01:28] State transition: MOTORS_ON -> FLIGHT_TO_PICKUP
[00:01:28] Updating order phase to: FLIGHT_TO_PICKUP
[00:01:29] Order phase updated successfully
[00:01:29] Setting destination: (50.0001, 36.0001)
[00:01:31] Moving to destination... (progress: 5%)
[00:01:33] Moving to destination... (progress: 10%)
...
```

---

## Логи GPIO (в эмуляторе):

```
[00:02:15] Motors started
              └─> GPIO25 = HIGH (моторы включены)

[00:02:45] Opening compartment...
              └─> GPIO26 = PWM(77) (servo 90°)

[00:02:47] Compartment opened
              └─> LED32 blinks 2x

[00:02:52] Closing compartment...
              └─> GPIO26 = PWM(26) (servo 0°)

[00:03:15] Waiting for button press...
              └─> Monitoring GPIO27

[00:03:30] Button pressed!
              └─> GPIO27 = LOW detected
```

---

## Troubleshooting

### Проблема: "Cannot fetch orders: Not authenticated"
**Решение:**
- Проверить ROBOT_CONFIG в config.py
- Убедиться что робот зарегистрирован на сервере
- Проверить что сервер доступен (BASE_URL)

### Проблема: "Failed to connect to WiFi"
**Решение:**
- Для Wokwi использовать "Wokwi-GUEST" без пароля
- Проверить WIFI_CONFIG в config.py

### Проблема: "Hardware not available - running in simulation mode"
**Решение:**
- Это нормально! Означает что код работает на PC, а не на ESP32
- Все GPIO операции будут симулированы
- Для реального hardware нужен настоящий ESP32

### Проблема: "Order fetching not implemented"
**Решение:**
- Это значит что вы запустили старый main.py!
- Убедитесь что используете main.py (FSM версию)
- Проверьте что order_manager.py обновлен

---

## Мониторинг в реальном времени

### На сервере:
```bash
# Смотреть статус робота
GET /api/Robot/me
Authorization: Bearer {robot_token}

# Смотреть заказы робота
GET /api/Robot/my-orders
Authorization: Bearer {robot_token}
```

### В логах дрона:
- Каждое изменение состояния FSM
- Все API вызовы
- Все GPIO операции
- Прогресс движения GPS

---

## Тестовый сценарий

1. **Запустить сервер:**
   ```bash
   dotnet run --project RobDeliveryAPI
   ```

2. **Создать тестовый заказ через Admin:**
   ```bash
   POST /api/Order/{orderId}/execute
   ```

3. **Запустить эмулятор Wokwi с дроном**

4. **Наблюдать:**
   - Дрон автоматически получит заказ через ~10 секунд
   - FSM пройдет все 19 состояний
   - Заказ будет доставлен!

---

## Что можно эмулировать в Wokwi:

✅ WiFi подключение
✅ HTTP requests к серверу
✅ GPS навигация (симуляция)
✅ GPIO pins (моторы, servo, кнопка, LEDs)
✅ FSM state transitions
✅ Battery management

❌ Реальный полет (это симуляция)
❌ Реальные препятствия
❌ Реальное время доставки

---

## Полезные команды для debugging:

В `config.py` установите:
```python
DEBUG = True
```

Это включит детальное логирование всех API вызовов и FSM transitions.

---

Готово! Теперь можно загружать в Wokwi и тестировать! 🚀
