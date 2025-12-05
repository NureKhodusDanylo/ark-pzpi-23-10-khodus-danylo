# Анализ интеграции IoT устройства с сервером

## Текущая ситуация

### Сервер (OrderService.ExecuteOrderAsync)

**Что работает:**
1. ✅ Находит оптимального дрона на зарядных станциях
2. ✅ Рассчитывает полный маршрут с учетом зарядки
3. ✅ Назначает дрона на заказ через `AssignRobotToOrderAsync`
4. ✅ Пытается отправить команду дрону через IP/Port (строки 407-456)

**Проблемы:**
- ❌ Отправка через IP не работает для ESP32 (нет статического IP в реальных условиях)
- ❌ Дрон не получает задание автоматически
- ❌ Нет подтверждения от дрона о готовности

### IoT устройство (main.py)

**Что работает:**
1. ✅ Инициализация (WiFi, Auth, GPS, Battery, Telemetry)
2. ✅ Отправка телеметрии на сервер (POST /api/Robot/status)
3. ✅ Demo mode - самостоятельный запуск тестовых доставок

**Проблемы:**
- ❌ Нет механизма получения реальных заказов от сервера
- ❌ Нет подтверждения приема задания
- ❌ Примитивная логика (только 2 фазы: going_to_pickup, delivering)
- ❌ Нет машины состояний (FSM)
- ❌ Нет управления GPIO (моторы, отсек, кнопки)
- ❌ Нет обработки получения посылки получателем
- ❌ Не использует маршрут от сервера

---

## КРИТИЧЕСКИЕ ПРОБЛЕМЫ

### 1. Нет pull-механизма
**Проблема:** Дрон не умеет забирать назначенные ему заказы.
**Текущий код:** OrderManager.fetch_assigned_orders() - заглушка (строка 44)
```python
log_message("Order fetching not implemented (requires server endpoint)")
return []
```

### 2. Push-механизм не работает
**Проблема:** Сервер пытается отправить команду через IP, но ESP32 не имеет статического IP.
**Код:** OrderService.cs:409-456 - `_droneConnectionService.SendDeliveryCommandAsync()`

### 3. Нет машины состояний
**Проблема:** Текущая логика упрощенная - только "going_to_pickup" и "delivering".
**Нужно:** Полноценная FSM с состояниями для каждого этапа доставки.

### 4. Нет hardware контроля
**Проблема:** Нет кода для управления:
- Моторами (начало/остановка полета)
- Отсеком (открытие/закрытие)
- Кнопкой подтверждения получения

### 5. Не используется маршрут от сервера
**Проблема:** Сервер рассчитывает маршрут с зарядными станциями, но дрон его не получает.

---

## АРХИТЕКТУРА РЕШЕНИЯ

### 1. Новые Server Endpoints

#### GET /api/Robot/my-orders
**Назначение:** Дрон получает список назначенных ему заказов
**Response:**
```json
[
  {
    "orderId": 123,
    "orderName": "Package-123",
    "weight": 2.5,
    "pickupNode": {
      "id": 5,
      "name": "User-John",
      "latitude": 50.0001,
      "longitude": 36.0001
    },
    "dropoffNode": {
      "id": 8,
      "name": "User-Alice",
      "latitude": 50.0015,
      "longitude": 36.0020
    },
    "route": [
      {
        "segmentNumber": 1,
        "latitude": 50.0001,
        "longitude": 36.0001,
        "action": "travel",
        "distanceMeters": 500
      },
      {
        "segmentNumber": 2,
        "latitude": 50.0010,
        "longitude": 36.0010,
        "action": "charge",
        "distanceMeters": 300
      }
    ],
    "totalDistanceMeters": 1500,
    "estimatedBatteryUsage": 25.5
  }
]
```

#### POST /api/Robot/order/{orderId}/accept
**Назначение:** Дрон подтверждает прием задания
**Request:** (пустой body)
**Response:**
```json
{
  "message": "Order accepted by drone",
  "orderId": 123,
  "status": "Processing"
}
```

#### POST /api/Robot/order/{orderId}/phase
**Назначение:** Дрон обновляет фазу выполнения заказа
**Request:**
```json
{
  "phase": "AT_PICKUP",
  "latitude": 50.0001,
  "longitude": 36.0001,
  "timestamp": "2024-12-05T10:30:00Z"
}
```
**Response:**
```json
{
  "message": "Order phase updated",
  "currentPhase": "AT_PICKUP"
}
```

---

### 2. Машина состояний (FSM) для дрона

```
┌─────────────────────────────────────────────────────────────────────┐
│                        DRONE STATE MACHINE                           │
└─────────────────────────────────────────────────────────────────────┘

    IDLE
     │
     │ [Check for orders every 10s]
     ↓
    CHECK_ORDERS ──────→ [No orders] ─────→ IDLE
     │
     │ [Order found]
     ↓
    ORDER_ASSIGNED
     │
     │ [Accept order + Get route]
     ↓
    MOTORS_ON
     │
     │ [Enable motor pins]
     ↓
    FLIGHT_TO_PICKUP ──────→ [Send telemetry every 5s]
     │
     │ [Arrived at pickup]
     ↓
    AT_PICKUP
     │
     │ [Notify server]
     ↓
    OPEN_COMPARTMENT
     │
     │ [Enable compartment pin]
     ↓
    LOADING
     │
     │ [Wait 5s]
     ↓
    CLOSE_COMPARTMENT
     │
     │ [Disable compartment pin]
     ↓
    FLIGHT_TO_DROPOFF ──────→ [Send telemetry every 5s]
     │
     │ [Arrived at dropoff]
     ↓
    AT_DROPOFF
     │
     │ [Notify server]
     ↓
    OPEN_COMPARTMENT
     │
     │ [Enable compartment pin]
     ↓
    WAIT_FOR_PICKUP
     │
     │ [Wait for button press]
     ↓
    PACKAGE_DELIVERED
     │
     │ [Notify server + Close compartment]
     ↓
    FLIGHT_TO_CHARGING ──────→ [Navigate to nearest charging station]
     │
     │ [Arrived at charging station]
     ↓
    AT_CHARGING_STATION
     │
     │ [Notify server]
     ↓
    CHARGING ──────→ [Battery > 95%] ─────→ IDLE
     │
     │ [Low battery emergency]
     ↓
    ERROR ──────→ [Cancel order] ─────→ IDLE
```

---

### 3. GPIO Pin Configuration

```python
# config/hardware_config.py
GPIO_CONFIG = {
    # Motor control (motors on/off)
    "MOTOR_PIN": 25,          # GPIO25 - Motor power (HIGH=flying, LOW=stopped)

    # Compartment control
    "COMPARTMENT_PIN": 26,    # GPIO26 - Compartment servo (HIGH=open, LOW=closed)

    # Button for package pickup confirmation
    "BUTTON_PIN": 27,         # GPIO27 - Button input (pullup, active LOW)

    # LED indicators
    "LED_STATUS": 32,         # GPIO32 - Status LED (blinking patterns)
    "LED_BATTERY": 33,        # GPIO33 - Battery warning LED
}
```

---

### 4. Полный цикл взаимодействия

```
┌──────────┐                  ┌──────────┐                  ┌──────────┐
│   USER   │                  │  SERVER  │                  │   DRONE  │
└──────────┘                  └──────────┘                  └──────────┘
     │                              │                              │
     │ 1. Click "Execute Order"     │                              │
     ├─────────────────────────────>│                              │
     │                              │                              │
     │                              │ 2. Find optimal drone        │
     │                              │    Calculate route           │
     │                              │    Assign drone to order     │
     │                              │                              │
     │<─────────────────────────────┤ 3. Response: Drone assigned  │
     │                              │                              │
     │                              │                              │
     │                              │<─────────────────────────────┤ 4. GET /my-orders (polling)
     │                              │                              │
     │                              ├─────────────────────────────>│ 5. Order + Route
     │                              │                              │
     │                              │<─────────────────────────────┤ 6. POST /order/123/accept
     │                              │                              │
     │                              ├─────────────────────────────>│ 7. Accepted
     │                              │                              │
     │                              │                              │ 8. Turn ON motors (GPIO)
     │                              │                              │
     │                              │<─────────────────────────────┤ 9. POST /status (flying)
     │                              │                              │
     │                              │<─────────────────────────────┤ 10. POST /status (position updates)
     │                              │                              │
     │                              │<─────────────────────────────┤ 11. POST /order/123/phase (AT_PICKUP)
     │                              │                              │
     │                              │                              │ 12. Open compartment (GPIO)
     │                              │                              │ 13. Wait 5s (loading)
     │                              │                              │ 14. Close compartment
     │                              │                              │
     │                              │<─────────────────────────────┤ 15. POST /status (flying to dropoff)
     │                              │                              │
     │                              │<─────────────────────────────┤ 16. POST /order/123/phase (AT_DROPOFF)
     │                              │                              │
     │                              │                              │ 17. Open compartment
     │                              │                              │ 18. Wait for BUTTON press
     │ 19. Notification:            │                              │
     │     "Drone arrived!"         │                              │
     │<─────────────────────────────┤                              │
     │                              │                              │
     │                              │<─────────────────────────────┤ 20. POST /order/123/phase (DELIVERED)
     │                              │                              │
     │                              ├─────────────────────────────>│ 21. Order completed
     │                              │                              │
     │                              │                              │ 22. Navigate to charging station
     │                              │                              │
     │                              │<─────────────────────────────┤ 23. POST /status (charging)
     │                              │                              │
     │                              │<─────────────────────────────┤ 24. POST /status (idle, battery=100%)
```

---

## РЕАЛИЗАЦИЯ

### Приоритеты

1. **ВЫСОКИЙ ПРИОРИТЕТ** - Без этого система не работает:
   - [ ] Server: Endpoint GET /api/Robot/my-orders
   - [ ] Server: Endpoint POST /api/Robot/order/{id}/accept
   - [ ] Server: Endpoint POST /api/Robot/order/{id}/phase
   - [ ] IoT: Машина состояний (FSM)
   - [ ] IoT: Обновление OrderManager для работы с реальными заказами

2. **СРЕДНИЙ ПРИОРИТЕТ** - Нужно для полноценной работы:
   - [ ] IoT: GPIO контроллер (моторы, отсек)
   - [ ] IoT: Обработка кнопки подтверждения
   - [ ] IoT: Использование маршрута от сервера

3. **НИЗКИЙ ПРИОРИТЕТ** - Улучшения:
   - [ ] Server: Уведомления пользователю (SignalR/WebSockets)
   - [ ] IoT: LED индикаторы статуса
   - [ ] IoT: Логирование событий в файл

---

## ФАЙЛЫ ДЛЯ ИЗМЕНЕНИЯ

### Server Side

1. **RobDeliveryAPI/Controllers/RobotController.cs**
   - Добавить endpoint `GetMyOrders()`
   - Добавить endpoint `AcceptOrder(orderId)`
   - Добавить endpoint `UpdateOrderPhase(orderId, phase)`

2. **Application/Abstractions/Interfaces/IRobotService.cs**
   - Добавить методы для новых endpoints

3. **Application/Services/RobotService.cs**
   - Реализовать логику получения заказов
   - Реализовать логику приема заказа
   - Реализовать логику обновления фазы

4. **Application/DTOs/RobotDTOs/OrderAssignmentDTO.cs** (новый файл)
   - DTO для передачи заказа дрону

5. **Application/DTOs/RobotDTOs/OrderPhaseUpdateDTO.cs** (новый файл)
   - DTO для обновления фазы заказа

### IoT Side

1. **IotDronePi/core/state_machine.py** (новый файл)
   - Машина состояний (FSM)

2. **IotDronePi/modules/hardware_controller.py** (новый файл)
   - GPIO контроллер для пинов

3. **IotDronePi/modules/order_manager.py**
   - Добавить fetch_assigned_orders() - реальная реализация
   - Добавить accept_order()
   - Добавить update_order_phase()

4. **IotDronePi/config/hardware_config.py** (новый файл)
   - Конфигурация GPIO пинов

5. **IotDronePi/main.py**
   - Интегрировать FSM
   - Убрать demo mode или сделать опциональным
   - Добавить polling заказов

---

## ТЕСТИРОВАНИЕ

### Unit Tests

1. Server:
   - [ ] Test GET /api/Robot/my-orders
   - [ ] Test POST /api/Robot/order/{id}/accept
   - [ ] Test POST /api/Robot/order/{id}/phase

2. IoT:
   - [ ] Test FSM state transitions
   - [ ] Test GPIO controller
   - [ ] Test OrderManager API calls

### Integration Tests

1. [ ] Полный цикл: User → Server → Drone → Delivery
2. [ ] Тест с несколькими дронами
3. [ ] Тест с низким зарядом батареи
4. [ ] Тест отмены заказа

---

## NOTES

- Все изменения должны соблюдать Clean Architecture
- IoT код должен работать на ESP32 с MicroPython
- Использовать существующие механизмы аутентификации (JWT)
- Логировать все важные события
- Обрабатывать ошибки сети (retry logic)
