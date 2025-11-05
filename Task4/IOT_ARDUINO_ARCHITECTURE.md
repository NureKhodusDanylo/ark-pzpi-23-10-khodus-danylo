# Архитектура IoT модуля для Arduino дрона - RobDelivery

Полное руководство по разработке IoT устройства на базе Arduino для интеграции с RobDelivery API.

---

## 📋 Содержание

1. [Обзор системы](#обзор-системы)
2. [API Integration](#api-integration)
3. [Железо (Hardware)](#железо-hardware)
4. [Архитектура прошивки](#архитектура-прошивки)
5. [Протокол связи](#протокол-связи)
6. [Структура кода Arduino](#структура-кода-arduino)
7. [Пошаговая реализация](#пошаговая-реализация)
8. [Тестирование](#тестирование)

---

## Обзор системы

### Архитектура взаимодействия

```
┌──────────────────────┐
│   RobDelivery API    │
│   (ASP.NET Core)     │
│   Port: 5102         │
└──────────┬───────────┘
           │ HTTP/HTTPS
           │ JWT Auth
           ▼
┌──────────────────────┐
│   WiFi/Ethernet      │
│   Network Layer      │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│   Arduino IoT        │
│   (ESP32/Mega+WiFi)  │
│   Web Server: 80     │
└──────────┬───────────┘
           │
    ┌──────┴──────┬──────────┬─────────┐
    ▼             ▼          ▼         ▼
 [GPS]      [Battery]   [Motors]   [Sensors]
```

### Роль Arduino дрона в системе

**Что делает сервер (RobDeliveryAPI):**
- Управление заказами
- Назначение роботов на заказы
- Предоставление координат точек (Nodes)
- Аутентификация устройств (JWT токены)
- Мониторинг состояния флота
- Логирование доставок

**Что делает Arduino дрон:**
- Получение заданий от сервера
- Автономная навигация по координатам
- Обход препятствий
- Планирование маршрута
- Управление батареей
- Отправка телеметрии на сервер
- Контроль вантажного отсека

---

## API Integration

### Существующие эндпоинты для IoT

#### 1. Аутентификация робота

Роботы уже имеют систему аутентификации через JWT токены с ролью `"Iot"`.

**Информация из модели Robot (Robot.cs:29-31):**
- `SerialNumber` - уникальный идентификатор дрона
- `AccessKeyHash` - хэшированный ключ доступа
- `IpAddress` и `Port` - адрес Arduino устройства

**Получение токена:**
- Метод: `POST /api/Auth/robot/login` (нужно реализовать на сервере)
- Body:
```json
{
  "serialNumber": "DRONE-001",
  "accessKey": "your-secret-key"
}
```
- Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "robotId": 1,
  "expiresIn": 2073600
}
```

**Срок действия токена:** 24 дня (2073600 секунд) - BaseTokenService.cs:139

#### 2. Получение информации о себе

**Endpoint:** `GET /api/Robot/me`
**Authorization:** Bearer token (роль Iot)
**Response:**
```json
{
  "id": 1,
  "name": "Drone Alpha",
  "model": "DJI-Custom",
  "typeName": "Drone",
  "statusName": "Idle",
  "batteryLevel": 85.5,
  "currentNodeId": 5,
  "currentLatitude": 50.4501,
  "currentLongitude": 30.5234,
  "targetNodeId": null,
  "ipAddress": "192.168.1.100",
  "port": 80
}
```

#### 3. Отправка телеметрии (статус обновление)

**Endpoint:** `POST /api/Robot/status`
**Authorization:** Bearer token (роль Iot)
**Content-Type:** application/json
**Body (RobotStatusUpdateDTO.cs:5-20):**
```json
{
  "status": "Delivering",
  "batteryLevel": 75.3,
  "currentNodeId": null,
  "currentLatitude": 50.4525,
  "currentLongitude": 30.5280,
  "targetNodeId": 8
}
```

**Status значения:**
- `"Idle"` - дрон в режиме ожидания
- `"Delivering"` - дрон доставляет заказ
- `"Charging"` - дрон на зарядке
- `"Maintenance"` - дрон на обслуживании

**Response:**
```json
{
  "message": "Robot status updated successfully",
  "robotId": 1,
  "status": "Delivering",
  "batteryLevel": 75.3,
  "currentNodeId": null,
  "targetNodeId": 8,
  "coordinates": {
    "latitude": 50.4525,
    "longitude": 30.5280
  }
}
```

#### 4. Получение заказов (нужно добавить на сервер)

**Рекомендуемый endpoint:** `GET /api/Robot/me/orders`
**Authorization:** Bearer token (роль Iot)
**Response:**
```json
{
  "activeOrders": [
    {
      "orderId": 123,
      "orderName": "Package #123",
      "packageWeight": 2.5,
      "pickupNodeId": 5,
      "pickupNodeName": "User: John Doe",
      "pickupLatitude": 50.4501,
      "pickupLongitude": 30.5234,
      "dropoffNodeId": 8,
      "dropoffNodeName": "User: Jane Smith",
      "dropoffLatitude": 50.4612,
      "dropoffLongitude": 30.5420,
      "status": "Processing"
    }
  ]
}
```

#### 5. Получение узлов (Nodes)

**Endpoint:** `GET /api/Node`
**Authorization:** Bearer token
**Query params:** `?type=ChargingStation`
**Response:**
```json
[
  {
    "id": 10,
    "name": "Charging Station Alpha",
    "typeName": "ChargingStation",
    "latitude": 50.4550,
    "longitude": 30.5300,
    "address": "Kyiv, Ukraine"
  }
]
```

---

## Железо (Hardware)

### Минимальная конфигурация для дрона

#### 1. **Основной контроллер**

**Вариант A: ESP32 DevKit (РЕКОМЕНДУЕТСЯ)**
- WiFi встроен (802.11 b/g/n)
- Bluetooth 4.2
- 240 MHz dual-core
- 520 KB SRAM
- GPIO: 34 пина
- Цена: ~$5-10

**Вариант B: Arduino Mega 2560 + WiFi Shield**
- Arduino Mega 2560
- ESP8266 WiFi Shield или ESP-01 модуль
- Больше памяти для сложной логики
- Цена: ~$15-25

**Вариант C: Arduino Nano 33 IoT**
- WiFi встроен
- Компактный размер
- ARM Cortex-M0+ 48MHz
- Цена: ~$20-25

#### 2. **GPS модуль**

**NEO-6M или NEO-7M (рекомендуется NEO-7M)**
- Точность: 2.5m
- Update rate: 5Hz (можно до 10Hz)
- UART интерфейс
- Цена: ~$10-15
- Pins: TX, RX, VCC (3.3V/5V), GND

**Подключение к ESP32:**
```
GPS TX  → ESP32 RX (GPIO 16)
GPS RX  → ESP32 TX (GPIO 17)
GPS VCC → ESP32 3.3V
GPS GND → ESP32 GND
```

#### 3. **Датчик батареи**

**Voltage Sensor (0-25V)**
- Измерение напряжения LiPo батареи
- Analog output
- Цена: ~$2-3

**INA219 Current Sensor (рекомендуется)**
- Напряжение + ток
- I2C интерфейс
- Точность: ±0.8mA
- Цена: ~$5-8

#### 4. **Датчики препятствий**

**Ultrasonic HC-SR04 (4 штуки - спереди, сзади, слева, справа)**
- Дальность: 2cm - 400cm
- Точность: 3mm
- Цена за штуку: ~$2

**ИЛИ LiDAR TF-Luna (лучше для дронов)**
- Дальность: 0.2m - 8m
- I2C/UART интерфейс
- Высокая точность
- Цена: ~$25-35

#### 5. **Контроллер моторов**

Зависит от типа дрона:

**Для квадрокоптера:**
- 4x Brushless моторы (1000-1500 KV)
- 4x ESC (Electronic Speed Controller) 20A-30A
- Flight Controller (опционально): F450 или аналог

**Для наземного робота:**
- 2-4x DC моторы с энкодерами
- L298N Motor Driver или аналог
- Цена: ~$5-10

#### 6. **Источник питания**

**LiPo батарея:**
- Напряжение: 11.1V (3S) или 14.8V (4S)
- Емкость: 2200-5000 mAh
- C-rating: 25C минимум
- Цена: ~$20-40

**Voltage regulator:**
- Step-down converter 5V для ESP32
- Step-down converter 3.3V для GPS
- Цена: ~$3-5

#### 7. **Дополнительно**

- **MicroSD Card Module** - для логирования полетов (~$2)
- **LED индикаторы** - статус дрона (~$1)
- **Buzzer** - звуковые сигналы (~$1)
- **Сервопривод** - для открытия грузового отсека (~$5)

### Полная стоимость

**Бюджетный вариант (ESP32 + базовые датчики):** ~$80-120
**Средний вариант (ESP32 + LiDAR + хорошие моторы):** ~$200-300
**Профессиональный вариант (с готовым Flight Controller):** ~$400-600

---

## Архитектура прошивки

### Структура модулей Arduino прошивки

```
┌─────────────────────────────────────────┐
│           Main Loop (Core)              │
│  - Task scheduling                      │
│  - State machine                        │
└───────────┬─────────────────────────────┘
            │
    ┌───────┴────────┬──────────┬──────────────┐
    ▼                ▼          ▼              ▼
┌─────────┐   ┌──────────┐  ┌────────┐   ┌──────────┐
│ Network │   │Navigation│  │Sensors │   │ Battery  │
│ Module  │   │ Module   │  │ Module │   │ Module   │
└─────────┘   └──────────┘  └────────┘   └──────────┘
    │              │             │              │
    ▼              ▼             ▼              ▼
┌─────────┐   ┌──────────┐  ┌────────┐   ┌──────────┐
│HTTP/REST│   │   GPS    │  │LiDAR/  │   │ Voltage/ │
│  Client │   │ Parser   │  │Sonar   │   │ Current  │
└─────────┘   └──────────┘  └────────┘   └──────────┘
```

### Основные модули

#### 1. **NetworkModule** - Связь с сервером
- Подключение к WiFi
- HTTP REST клиент
- JWT токен management
- Отправка телеметрии (каждые 5-10 секунд)
- Получение заданий

#### 2. **NavigationModule** - Навигация
- GPS позиционирование
- Калькуляция bearing (направления) к цели
- PID контроллер для движения
- Waypoint tracking
- Определение достижения точки

#### 3. **SensorsModule** - Датчики
- Чтение GPS данных
- Obstacle detection (LiDAR/Sonar)
- Altitude sensor (для дронов)

#### 4. **BatteryModule** - Управление энергией
- Мониторинг напряжения
- Калькуляция оставшейся дистанции
- Решение о необходимости зарядки
- Защита от глубокого разряда

#### 5. **MotorControlModule** - Управление движением
- PWM сигналы для ESC/моторов
- Стабилизация (для дронов)
- Скорость и направление

#### 6. **CargoModule** - Грузовой отсек
- Контроль сервопривода
- Датчик веса (опционально)
- Датчик закрытия дверцы

---

## Протокол связи

### Жизненный цикл дрона

```
1. [BOOT]
   ↓
2. Connect to WiFi
   ↓
3. Authenticate with API (get JWT token)
   ↓
4. Enter IDLE state
   ↓
5. Poll for orders (every 10 seconds)
   ↓
6. [ORDER RECEIVED] → Change to DELIVERING
   ↓
7. Navigate to PICKUP location
   ↓
8. Open cargo (user loads package)
   ↓
9. Navigate to DROPOFF location
   ↓
10. Open cargo (user takes package)
    ↓
11. Send completion status to API
    ↓
12. Return to IDLE
    ↓
    [If battery < 20%] → Go to CHARGING station
```

### Частота отправки телеметрии

**Режим IDLE:**
- Каждые 30 секунд
- Отправляется: `status`, `batteryLevel`, `currentNodeId`

**Режим DELIVERING:**
- Каждые 5 секунд
- Отправляется: `status`, `batteryLevel`, `currentLatitude`, `currentLongitude`, `targetNodeId`

**Режим CHARGING:**
- Каждые 60 секунд
- Отправляется: `status`, `batteryLevel`, `currentNodeId`

### Формат HTTP запросов

#### Отправка телеметрии

```cpp
POST http://api-server:5102/api/Robot/status HTTP/1.1
Host: api-server:5102
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "status": "Delivering",
  "batteryLevel": 75.3,
  "currentNodeId": null,
  "currentLatitude": 50.4525,
  "currentLongitude": 30.5280,
  "targetNodeId": 8
}
```

#### Получение заказов

```cpp
GET http://api-server:5102/api/Robot/me/orders HTTP/1.1
Host: api-server:5102
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Структура кода Arduino

### Файловая структура проекта

```
RobDelivery_Drone/
├── RobDelivery_Drone.ino          // Главный файл
├── config.h                        // Конфигурация (WiFi, API, pins)
├── network.h / network.cpp         // HTTP клиент, API коммуникация
├── navigation.h / navigation.cpp   // GPS, навигация
├── sensors.h / sensors.cpp         // Датчики
├── battery.h / battery.cpp         // Батарея
├── motors.h / motors.cpp           // Управление моторами
├── cargo.h / cargo.cpp             // Грузовой отсек
└── state_machine.h / state_machine.cpp  // State machine логика
```

### config.h - Конфигурация

```cpp
#ifndef CONFIG_H
#define CONFIG_H

// WiFi Configuration
#define WIFI_SSID "YourWiFiSSID"
#define WIFI_PASSWORD "YourWiFiPassword"

// API Configuration
#define API_HOST "192.168.1.50"  // IP адрес сервера RobDeliveryAPI
#define API_PORT 5102
#define API_BASE_URL "http://192.168.1.50:5102/api"

// Robot Authentication
#define ROBOT_SERIAL_NUMBER "DRONE-001"
#define ROBOT_ACCESS_KEY "your-secret-access-key"

// GPS Configuration
#define GPS_SERIAL Serial2
#define GPS_BAUD_RATE 9600
#define GPS_RX_PIN 16
#define GPS_TX_PIN 17

// Battery Configuration
#define BATTERY_PIN 34  // Analog pin
#define BATTERY_MIN_VOLTAGE 10.5  // 3S LiPo minimum safe voltage
#define BATTERY_MAX_VOLTAGE 12.6  // 3S LiPo full charge
#define BATTERY_CAPACITY_JOULES 360000.0  // 100Wh
#define ENERGY_PER_METER_JOULES 36.0      // Energy consumption per meter

// Motor Pins (for quadcopter with 4 ESCs)
#define MOTOR_FL_PIN 25  // Front Left
#define MOTOR_FR_PIN 26  // Front Right
#define MOTOR_BL_PIN 27  // Back Left
#define MOTOR_BR_PIN 14  // Back Right

// Sensor Pins
#define SONAR_FRONT_TRIG 32
#define SONAR_FRONT_ECHO 33
#define SONAR_BACK_TRIG 18
#define SONAR_BACK_ECHO 19

// Cargo Servo
#define CARGO_SERVO_PIN 13

// LED Indicators
#define LED_STATUS_PIN 2  // Built-in LED on ESP32

// Navigation Configuration
#define WAYPOINT_THRESHOLD_METERS 5.0  // Считать точку достигнутой при 5 метрах
#define TELEMETRY_INTERVAL_MS 5000     // Отправка телеметрии каждые 5 секунд
#define ORDER_CHECK_INTERVAL_MS 10000  // Проверка заказов каждые 10 секунд

// Battery thresholds
#define BATTERY_LOW_THRESHOLD 30.0     // Go to charging at 30%
#define BATTERY_CRITICAL_THRESHOLD 20.0  // Emergency landing at 20%

#endif
```

### state_machine.h - State Machine

```cpp
#ifndef STATE_MACHINE_H
#define STATE_MACHINE_H

// Drone states
enum DroneState {
  STATE_BOOT,
  STATE_CONNECTING_WIFI,
  STATE_AUTHENTICATING,
  STATE_IDLE,
  STATE_AWAITING_ORDER,
  STATE_NAVIGATING_TO_PICKUP,
  STATE_AT_PICKUP,
  STATE_NAVIGATING_TO_DROPOFF,
  STATE_AT_DROPOFF,
  STATE_NAVIGATING_TO_CHARGING,
  STATE_CHARGING,
  STATE_ERROR
};

class StateMachine {
private:
  DroneState currentState;
  unsigned long lastStateChange;

public:
  StateMachine();
  void setState(DroneState newState);
  DroneState getState();
  void update();
  String getStateName();
};

#endif
```

### network.h - API Client

```cpp
#ifndef NETWORK_H
#define NETWORK_H

#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>

class NetworkModule {
private:
  String jwtToken;
  int robotId;
  unsigned long tokenExpiry;
  HTTPClient http;

public:
  NetworkModule();

  // WiFi
  bool connectWiFi();
  bool isWiFiConnected();

  // Authentication
  bool authenticate();
  bool isTokenValid();

  // API Calls
  bool sendTelemetry(String status, double batteryLevel,
                     double lat, double lon,
                     int* currentNodeId, int* targetNodeId);

  bool getRobotInfo();
  bool getActiveOrders(JsonDocument& ordersDoc);
  bool getChargingStations(JsonDocument& stationsDoc);

  // Utility
  String getJwtToken();
  int getRobotId();
};

#endif
```

### navigation.h - Navigation Module

```cpp
#ifndef NAVIGATION_H
#define NAVIGATION_H

#include <TinyGPS++.h>

struct Waypoint {
  double latitude;
  double longitude;
  String action;  // "travel", "pickup", "dropoff", "charge"
  int nodeId;
};

class NavigationModule {
private:
  TinyGPSPlus gps;
  Waypoint currentTarget;
  bool hasTarget;

  double currentLat;
  double currentLon;
  bool gpsFixed;

public:
  NavigationModule();

  void begin();
  void update();  // Читает GPS данные

  bool hasGPSFix();
  double getLatitude();
  double getLongitude();
  double getAltitude();

  void setTarget(double lat, double lon, String action, int nodeId);
  bool hasReachedTarget();
  double getDistanceToTarget();  // В метрах
  double getBearingToTarget();   // В градусах (0-360)

  bool isTargetSet();
  Waypoint getTarget();
  void clearTarget();

  // Utility
  static double calculateDistance(double lat1, double lon1,
                                   double lat2, double lon2);
  static double calculateBearing(double lat1, double lon1,
                                  double lat2, double lon2);
};

#endif
```

### battery.h - Battery Management

```cpp
#ifndef BATTERY_H
#define BATTERY_H

class BatteryModule {
private:
  double currentVoltage;
  double currentPercentage;
  double capacityJoules;
  double energyPerMeterJoules;

public:
  BatteryModule(double capacityJ, double energyPerMeterJ);

  void begin();
  void update();  // Читает напряжение с датчика

  double getVoltage();
  double getPercentage();
  double getEstimatedRangeMeters();

  bool isLow();       // < 30%
  bool isCritical();  // < 20%

  bool canReachDistance(double distanceMeters);
};

#endif
```

### motors.h - Motor Control

```cpp
#ifndef MOTORS_H
#define MOTORS_H

#include <ESP32Servo.h>

class MotorController {
private:
  Servo escFL, escFR, escBL, escBR;  // ESCs for quadcopter
  int throttle;  // 0-100%

public:
  MotorController();

  void begin();
  void arm();        // Arm ESCs
  void disarm();     // Disarm ESCs

  void setThrottle(int percent);
  void hover();      // Maintain altitude
  void moveForward(int speed);
  void moveBackward(int speed);
  void turnLeft(int speed);
  void turnRight(int speed);
  void stop();

  bool isArmed();
};

#endif
```

### cargo.h - Cargo Bay Control

```cpp
#ifndef CARGO_H
#define CARGO_H

#include <ESP32Servo.h>

class CargoModule {
private:
  Servo doorServo;
  bool isOpen;

public:
  CargoModule();

  void begin();
  void openDoor();
  void closeDoor();
  bool isDoorOpen();

  void waitForUserAction(int timeoutSeconds);  // Ждет загрузки/разгрузки
};

#endif
```

---

## Пошаговая реализация

### Шаг 1: Настройка Arduino IDE

1. Установите Arduino IDE (версия 1.8.19 или 2.x)
2. Добавьте поддержку ESP32:
   - File → Preferences
   - Additional Board Manager URLs: `https://dl.espressif.com/dl/package_esp32_index.json`
   - Tools → Board → Boards Manager → Найти "ESP32" → Install

3. Установите библиотеки:
   - Tools → Manage Libraries
   - Установите:
     - `ArduinoJson` (by Benoit Blanchon) - для JSON парсинга
     - `TinyGPSPlus` (by Mikal Hart) - для GPS
     - `ESP32Servo` - для управления сервоприводами
     - `WiFi` (встроенная для ESP32)
     - `HTTPClient` (встроенная для ESP32)

### Шаг 2: Базовая структура Main файла

```cpp
// RobDelivery_Drone.ino

#include "config.h"
#include "network.h"
#include "navigation.h"
#include "battery.h"
#include "motors.h"
#include "cargo.h"
#include "state_machine.h"

// Global modules
NetworkModule network;
NavigationModule navigation;
BatteryModule battery(BATTERY_CAPACITY_JOULES, ENERGY_PER_METER_JOULES);
MotorController motors;
CargoModule cargo;
StateMachine stateMachine;

// Timers
unsigned long lastTelemetryTime = 0;
unsigned long lastOrderCheckTime = 0;

// Current order data
int currentOrderId = 0;
Waypoint pickupWaypoint;
Waypoint dropoffWaypoint;

void setup() {
  Serial.begin(115200);
  Serial.println("RobDelivery Drone Starting...");

  // Initialize modules
  stateMachine.setState(STATE_BOOT);

  pinMode(LED_STATUS_PIN, OUTPUT);
  digitalWrite(LED_STATUS_PIN, LOW);

  battery.begin();
  navigation.begin();
  motors.begin();
  cargo.begin();

  // Connect to WiFi
  stateMachine.setState(STATE_CONNECTING_WIFI);
  if (!network.connectWiFi()) {
    Serial.println("WiFi connection failed!");
    stateMachine.setState(STATE_ERROR);
    return;
  }

  // Authenticate with API
  stateMachine.setState(STATE_AUTHENTICATING);
  if (!network.authenticate()) {
    Serial.println("Authentication failed!");
    stateMachine.setState(STATE_ERROR);
    return;
  }

  Serial.println("Drone ready!");
  stateMachine.setState(STATE_IDLE);
  digitalWrite(LED_STATUS_PIN, HIGH);
}

void loop() {
  // Update all modules
  battery.update();
  navigation.update();
  stateMachine.update();

  // Check battery critical level
  if (battery.isCritical() && stateMachine.getState() != STATE_CHARGING) {
    Serial.println("CRITICAL BATTERY! Returning to charging station...");
    goToNearestChargingStation();
    return;
  }

  // Send telemetry periodically
  if (millis() - lastTelemetryTime >= TELEMETRY_INTERVAL_MS) {
    sendTelemetry();
    lastTelemetryTime = millis();
  }

  // State machine logic
  switch (stateMachine.getState()) {
    case STATE_IDLE:
      handleIdleState();
      break;

    case STATE_AWAITING_ORDER:
      handleAwaitingOrderState();
      break;

    case STATE_NAVIGATING_TO_PICKUP:
      handleNavigatingToPickupState();
      break;

    case STATE_AT_PICKUP:
      handleAtPickupState();
      break;

    case STATE_NAVIGATING_TO_DROPOFF:
      handleNavigatingToDropoffState();
      break;

    case STATE_AT_DROPOFF:
      handleAtDropoffState();
      break;

    case STATE_NAVIGATING_TO_CHARGING:
      handleNavigatingToChargingState();
      break;

    case STATE_CHARGING:
      handleChargingState();
      break;

    case STATE_ERROR:
      handleErrorState();
      break;
  }

  delay(100);  // Small delay to prevent watchdog timer issues
}

void handleIdleState() {
  motors.stop();

  // Check for new orders periodically
  if (millis() - lastOrderCheckTime >= ORDER_CHECK_INTERVAL_MS) {
    StaticJsonDocument<2048> ordersDoc;
    if (network.getActiveOrders(ordersDoc)) {
      if (!ordersDoc["activeOrders"].isNull() &&
          ordersDoc["activeOrders"].size() > 0) {

        // Got an order!
        JsonObject order = ordersDoc["activeOrders"][0];
        currentOrderId = order["orderId"];

        pickupWaypoint.latitude = order["pickupLatitude"];
        pickupWaypoint.longitude = order["pickupLongitude"];
        pickupWaypoint.nodeId = order["pickupNodeId"];
        pickupWaypoint.action = "pickup";

        dropoffWaypoint.latitude = order["dropoffLatitude"];
        dropoffWaypoint.longitude = order["dropoffLongitude"];
        dropoffWaypoint.nodeId = order["dropoffNodeId"];
        dropoffWaypoint.action = "dropoff";

        Serial.printf("New order received: #%d\n", currentOrderId);

        stateMachine.setState(STATE_NAVIGATING_TO_PICKUP);
        navigation.setTarget(pickupWaypoint.latitude,
                           pickupWaypoint.longitude,
                           pickupWaypoint.action,
                           pickupWaypoint.nodeId);
      }
    }
    lastOrderCheckTime = millis();
  }

  // Check battery level
  if (battery.isLow()) {
    Serial.println("Battery low, going to charging station...");
    goToNearestChargingStation();
  }
}

void handleNavigatingToPickupState() {
  if (!navigation.hasGPSFix()) {
    Serial.println("Waiting for GPS fix...");
    motors.hover();
    return;
  }

  // Navigate towards pickup location
  double distance = navigation.getDistanceToTarget();
  double bearing = navigation.getBearingToTarget();

  Serial.printf("Distance to pickup: %.2f m, Bearing: %.1f°\n",
                distance, bearing);

  // Simple navigation logic (adjust based on your drone type)
  motors.moveForward(50);  // 50% throttle

  // Check if reached
  if (navigation.hasReachedTarget()) {
    Serial.println("Arrived at pickup location!");
    motors.stop();
    stateMachine.setState(STATE_AT_PICKUP);
  }
}

void handleAtPickupState() {
  Serial.println("Opening cargo bay for package loading...");
  cargo.openDoor();

  // Wait for user to load package (timeout 60 seconds)
  cargo.waitForUserAction(60);

  cargo.closeDoor();
  Serial.println("Package loaded. Heading to dropoff...");

  navigation.setTarget(dropoffWaypoint.latitude,
                      dropoffWaypoint.longitude,
                      dropoffWaypoint.action,
                      dropoffWaypoint.nodeId);

  stateMachine.setState(STATE_NAVIGATING_TO_DROPOFF);
}

void handleNavigatingToDropoffState() {
  if (!navigation.hasGPSFix()) {
    Serial.println("Waiting for GPS fix...");
    motors.hover();
    return;
  }

  double distance = navigation.getDistanceToTarget();
  double bearing = navigation.getBearingToTarget();

  Serial.printf("Distance to dropoff: %.2f m, Bearing: %.1f°\n",
                distance, bearing);

  motors.moveForward(50);

  if (navigation.hasReachedTarget()) {
    Serial.println("Arrived at dropoff location!");
    motors.stop();
    stateMachine.setState(STATE_AT_DROPOFF);
  }
}

void handleAtDropoffState() {
  Serial.println("Opening cargo bay for package unloading...");
  cargo.openDoor();

  cargo.waitForUserAction(60);

  cargo.closeDoor();
  Serial.println("Package delivered!");

  // Notify server about completion
  // TODO: Add API endpoint for order completion

  currentOrderId = 0;
  navigation.clearTarget();
  stateMachine.setState(STATE_IDLE);
}

void handleChargingState() {
  motors.disarm();

  // Check if battery is full
  if (battery.getPercentage() >= 95.0) {
    Serial.println("Battery charged! Returning to service.");
    motors.arm();
    stateMachine.setState(STATE_IDLE);
  }
}

void sendTelemetry() {
  String status = stateMachine.getStateName();
  double batteryLevel = battery.getPercentage();
  double lat = navigation.getLatitude();
  double lon = navigation.getLongitude();

  int* currentNodeId = nullptr;
  int* targetNodeId = nullptr;

  // Set node IDs based on state
  if (stateMachine.getState() == STATE_AT_PICKUP ||
      stateMachine.getState() == STATE_AT_DROPOFF) {
    int nodeId = navigation.getTarget().nodeId;
    currentNodeId = &nodeId;
  }

  if (navigation.isTargetSet()) {
    int targetId = navigation.getTarget().nodeId;
    targetNodeId = &targetId;
  }

  bool success = network.sendTelemetry(status, batteryLevel, lat, lon,
                                       currentNodeId, targetNodeId);

  if (success) {
    Serial.println("Telemetry sent successfully");
  } else {
    Serial.println("Failed to send telemetry");
  }
}

void goToNearestChargingStation() {
  StaticJsonDocument<1024> stationsDoc;
  if (network.getChargingStations(stationsDoc)) {
    // Find nearest charging station
    // TODO: Implement nearest station selection

    JsonObject station = stationsDoc[0];
    double lat = station["latitude"];
    double lon = station["longitude"];
    int nodeId = station["id"];

    navigation.setTarget(lat, lon, "charge", nodeId);
    stateMachine.setState(STATE_NAVIGATING_TO_CHARGING);
  }
}

void handleErrorState() {
  motors.disarm();
  digitalWrite(LED_STATUS_PIN, LOW);
  Serial.println("ERROR STATE - System halted");
  delay(1000);

  // Blink LED to indicate error
  digitalWrite(LED_STATUS_PIN, !digitalRead(LED_STATUS_PIN));
}

void handleNavigatingToChargingState() {
  if (navigation.hasReachedTarget()) {
    Serial.println("Arrived at charging station");
    motors.stop();
    stateMachine.setState(STATE_CHARGING);
  } else {
    motors.moveForward(30);  // Slower speed when low battery
  }
}

void handleAwaitingOrderState() {
  // Same as IDLE for now
  handleIdleState();
}
```

### Шаг 3: Реализация network.cpp

```cpp
// network.cpp

#include "network.h"
#include "config.h"

NetworkModule::NetworkModule() {
  jwtToken = "";
  robotId = 0;
  tokenExpiry = 0;
}

bool NetworkModule::connectWiFi() {
  Serial.print("Connecting to WiFi");
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 30) {
    delay(500);
    Serial.print(".");
    attempts++;
  }

  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\nWiFi connected!");
    Serial.print("IP: ");
    Serial.println(WiFi.localIP());
    return true;
  }

  Serial.println("\nWiFi connection failed!");
  return false;
}

bool NetworkModule::isWiFiConnected() {
  return WiFi.status() == WL_CONNECTED;
}

bool NetworkModule::authenticate() {
  if (!isWiFiConnected()) {
    return false;
  }

  http.begin(String(API_BASE_URL) + "/Auth/robot/login");
  http.addHeader("Content-Type", "application/json");

  StaticJsonDocument<256> doc;
  doc["serialNumber"] = ROBOT_SERIAL_NUMBER;
  doc["accessKey"] = ROBOT_ACCESS_KEY;

  String requestBody;
  serializeJson(doc, requestBody);

  int httpCode = http.POST(requestBody);

  if (httpCode == 200) {
    String response = http.getString();

    StaticJsonDocument<512> responseDoc;
    deserializeJson(responseDoc, response);

    jwtToken = responseDoc["token"].as<String>();
    robotId = responseDoc["robotId"];
    tokenExpiry = millis() + (responseDoc["expiresIn"].as<long>() * 1000);

    http.end();

    Serial.println("Authentication successful!");
    Serial.printf("Robot ID: %d\n", robotId);

    return true;
  }

  http.end();
  Serial.printf("Authentication failed! HTTP Code: %d\n", httpCode);
  return false;
}

bool NetworkModule::isTokenValid() {
  return (millis() < tokenExpiry) && (jwtToken.length() > 0);
}

bool NetworkModule::sendTelemetry(String status, double batteryLevel,
                                  double lat, double lon,
                                  int* currentNodeId, int* targetNodeId) {
  if (!isTokenValid()) {
    Serial.println("Token expired, re-authenticating...");
    if (!authenticate()) {
      return false;
    }
  }

  http.begin(String(API_BASE_URL) + "/Robot/status");
  http.addHeader("Content-Type", "application/json");
  http.addHeader("Authorization", "Bearer " + jwtToken);

  StaticJsonDocument<512> doc;
  doc["status"] = status;
  doc["batteryLevel"] = batteryLevel;

  if (currentNodeId != nullptr) {
    doc["currentNodeId"] = *currentNodeId;
  }

  if (lat != 0.0 && lon != 0.0) {
    doc["currentLatitude"] = lat;
    doc["currentLongitude"] = lon;
  }

  if (targetNodeId != nullptr) {
    doc["targetNodeId"] = *targetNodeId;
  }

  String requestBody;
  serializeJson(doc, requestBody);

  int httpCode = http.POST(requestBody);
  http.end();

  return (httpCode == 200);
}

bool NetworkModule::getActiveOrders(JsonDocument& ordersDoc) {
  if (!isTokenValid()) {
    if (!authenticate()) return false;
  }

  http.begin(String(API_BASE_URL) + "/Robot/me/orders");
  http.addHeader("Authorization", "Bearer " + jwtToken);

  int httpCode = http.GET();

  if (httpCode == 200) {
    String response = http.getString();
    deserializeJson(ordersDoc, response);
    http.end();
    return true;
  }

  http.end();
  return false;
}

bool NetworkModule::getChargingStations(JsonDocument& stationsDoc) {
  if (!isTokenValid()) {
    if (!authenticate()) return false;
  }

  http.begin(String(API_BASE_URL) + "/Node?type=ChargingStation");
  http.addHeader("Authorization", "Bearer " + jwtToken);

  int httpCode = http.GET();

  if (httpCode == 200) {
    String response = http.getString();
    deserializeJson(stationsDoc, response);
    http.end();
    return true;
  }

  http.end();
  return false;
}

String NetworkModule::getJwtToken() {
  return jwtToken;
}

int NetworkModule::getRobotId() {
  return robotId;
}
```

---

## Тестирование

### 1. Тестирование на столе (без полета)

**Тест WiFi и аутентификации:**
```cpp
void setup() {
  Serial.begin(115200);

  NetworkModule network;
  if (network.connectWiFi()) {
    Serial.println("✓ WiFi OK");

    if (network.authenticate()) {
      Serial.println("✓ Authentication OK");
      Serial.println("Token: " + network.getJwtToken());
    }
  }
}
```

**Тест GPS:**
```cpp
void loop() {
  navigation.update();

  if (navigation.hasGPSFix()) {
    Serial.printf("GPS: %.6f, %.6f\n",
                  navigation.getLatitude(),
                  navigation.getLongitude());
  } else {
    Serial.println("No GPS fix");
  }

  delay(1000);
}
```

**Тест батареи:**
```cpp
void loop() {
  battery.update();

  Serial.printf("Battery: %.2fV (%.1f%%)\n",
                battery.getVoltage(),
                battery.getPercentage());
  Serial.printf("Estimated range: %.1f meters\n",
                battery.getEstimatedRangeMeters());

  delay(2000);
}
```

### 2. Тестирование с API

**Запустить API сервер:**
```bash
cd RobDeliveryAPI
dotnet run
```

**Создать тестового робота через Swagger:**
1. Открыть http://localhost:5102/swagger
2. Войти как Admin
3. POST /api/Robot/register
```json
{
  "name": "Test Drone",
  "model": "Arduino-ESP32",
  "type": "Drone",
  "serialNumber": "DRONE-001",
  "accessKey": "test-secret-key",
  "batteryCapacityJoules": 360000,
  "energyConsumptionPerMeterJoules": 36,
  "ipAddress": "192.168.1.100",
  "port": 80
}
```

**Мониторить телеметрию:**
- Проверить логи сервера
- GET /api/Robot/{id} - проверить обновление позиции

### 3. Полевое тестирование

**Этап 1: Статический тест (дрон на земле)**
- Проверка всех датчиков
- Проверка отправки телеметрии
- Проверка получения заказов

**Этап 2: Тест навигации без полета**
- Поставить target waypoint в 10 метрах
- Проверить расчет distance и bearing
- Проверить определение достижения точки

**Этап 3: Первый полет (ручное управление)**
- Взлет на 2 метра
- Проверка стабилизации
- Посадка

**Этап 4: Автономный полет (короткая дистанция)**
- Waypoint на расстоянии 20 метров
- Автономная навигация
- Мониторинг телеметрии

**Этап 5: Полная миссия**
- Получение реального заказа
- Навигация к pickup
- Загрузка груза
- Навигация к dropoff
- Разгрузка
- Возврат

---

## Рекомендации по безопасности

1. **Всегда тестируйте на привязи** (tethered flight) в начале
2. **Emergency stop** - кнопка физического отключения моторов
3. **Geofencing** - ограничение зоны полета в коде
4. **Failsafe** - автоматическая посадка при потере сигнала WiFi
5. **Battery monitoring** - критическая посадка при 15% заряда
6. **Obstacle avoidance** - использование сенсоров расстояния

---

## Заключение

Эта архитектура обеспечивает:
- ✅ Полную интеграцию с RobDeliveryAPI
- ✅ Автономную навигацию по GPS
- ✅ Управление батареей и энергией
- ✅ Real-time телеметрию
- ✅ Систему обработки заказов
- ✅ Модульную структуру кода

**Следующие шаги:**
1. Закупить необходимое железо
2. Собрать прототип на макетной плате
3. Загрузить и протестировать код
4. Провести серию тестов (WiFi → GPS → Motors → Full mission)
5. Итеративно улучшать алгоритмы навигации

**Полезные ресурсы:**
- ESP32 Documentation: https://docs.espressif.com/projects/esp-idf/
- TinyGPS++: http://arduiniana.org/libraries/tinygpsplus/
- ArduinoJson: https://arduinojson.org/
- Drone programming tutorials: https://www.hackster.io/drones
