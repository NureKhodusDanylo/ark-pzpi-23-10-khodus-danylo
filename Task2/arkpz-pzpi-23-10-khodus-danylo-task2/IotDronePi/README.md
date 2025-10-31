# IoT Drone - Raspberry Pi Pico + MicroPython

Модуль IoT устройства для дрона на базе **Raspberry Pi Pico (RP2040)** с **MicroPython**.

Интегрируется с **RobDeliveryAPI** через WiFi (с использованием ESP8266/ESP32 WiFi модуля).

---

## 🎯 Архитектура системы

```
┌─────────────────────────┐
│   RobDeliveryAPI        │
│   ASP.NET Core          │
│   http://server:5102    │
└───────────┬─────────────┘
            │ HTTP REST
            │ JWT Auth
            ▼
┌─────────────────────────┐
│   WiFi Module           │
│   (ESP8266/ESP-01)      │
│   AT Commands           │
└───────────┬─────────────┘
            │ UART
            ▼
┌─────────────────────────┐
│   Raspberry Pi Pico     │
│   (RP2040)              │
│   MicroPython           │
└───────────┬─────────────┘
            │
    ┌───────┴───────┬──────────┬─────────┐
    ▼               ▼          ▼         ▼
[GPS NEO-6M]  [Voltage]   [Motors]  [Servo]
```

---

## 🔧 Железо (Hardware)

### Основные компоненты

1. **Raspberry Pi Pico (RP2040)** - $4
   - Dual-core ARM Cortex-M0+ @ 133MHz
   - 264KB SRAM
   - 2MB Flash
   - 26x GPIO pins
   - UART, I2C, SPI

2. **ESP-01 (ESP8266) WiFi Module** - $2-3
   - WiFi 802.11 b/g/n
   - AT команды через UART
   - 3.3V питание

3. **GPS NEO-6M** - $10
   - UART интерфейс
   - Update rate: 5Hz
   - Точность: 2.5m

4. **Voltage Sensor** - $2
   - Измерение напряжения батареи
   - Analog input

5. **Servo MG90S** - $3
   - Для грузового отсека

6. **LiPo Battery 3S (11.1V)** - $20-30
   - 2200-3000 mAh

### Схема подключения

```
Raspberry Pi Pico (RP2040)
┌─────────────────────────────────┐
│                                 │
│  GP0 (UART0 TX) ─────────────────> ESP-01 RX  (WiFi)
│  GP1 (UART0 RX) <─────────────── ESP-01 TX
│                                 │
│  GP4 (UART1 TX) ─────────────────> GPS NEO-6M RX
│  GP5 (UART1 RX) <─────────────── GPS NEO-6M TX
│                                 │
│  GP26 (ADC0) <──────────────── Voltage Sensor
│                                 │
│  GP15 (PWM) ────────────────────> Servo Signal (Cargo)
│                                 │
│  GP20 (PWM) ────────────────────> Motor 1 (ESC)
│  GP21 (PWM) ────────────────────> Motor 2 (ESC)
│  GP22 (PWM) ────────────────────> Motor 3 (ESC)
│  GP27 (PWM) ────────────────────> Motor 4 (ESC)
│                                 │
│  GP25 (LED) ────────────────────> Built-in LED
│                                 │
└─────────────────────────────────┘

Питание:
- 3.3V (OUT) → GPS, ESP-01
- GND → Общий ground
- VSYS (2-5V) ← 5V regulator от LiPo
```

---

## 📂 Структура проекта

```
IotDronePi/
├── README.md                    # Этот файл
├── main.py                      # Главный файл (entry point)
├── config.py                    # Конфигурация
├── lib/
│   ├── network_module.py       # WiFi и HTTP клиент
│   ├── gps_module.py           # GPS парсинг
│   ├── battery_module.py       # Управление батареей
│   ├── navigation_module.py    # Навигация и маршруты
│   ├── motor_controller.py     # Управление моторами
│   ├── cargo_module.py         # Грузовой отсек
│   └── state_machine.py        # State machine
├── utils/
│   ├── geo_utils.py            # Geographic calculations
│   └── json_parser.py          # JSON парсинг
├── wokwi/
│   ├── diagram.json            # Wokwi схема
│   └── wokwi.toml              # Wokwi конфигурация
└── tests/
    ├── test_network.py         # Тесты WiFi
    ├── test_gps.py             # Тесты GPS
    └── test_navigation.py      # Тесты навигации
```

---

## 🚀 Быстрый старт

### 1. Установка MicroPython на Pico

1. Скачать `.uf2` файл: https://micropython.org/download/rp2-pico/
2. Подключить Pico с зажатой кнопкой BOOTSEL
3. Скопировать `.uf2` файл на появившийся диск

### 2. Загрузка кода на Pico

Используя Thonny IDE или rshell:

```bash
# Установка ampy для загрузки файлов
pip install adafruit-ampy

# Загрузка файлов
ampy --port COM3 put main.py
ampy --port COM3 put config.py
ampy --port COM3 put lib/
```

### 3. Конфигурация

Отредактируй `config.py`:

```python
# WiFi
WIFI_SSID = "YourWiFi"
WIFI_PASSWORD = "YourPassword"

# API
API_HOST = "192.168.1.50"
API_PORT = 5102
API_BASE_URL = f"http://{API_HOST}:{API_PORT}/api"

# Robot credentials
ROBOT_SERIAL_NUMBER = "PICO-DRONE-001"
ROBOT_ACCESS_KEY = "your-secret-key"
```

### 4. Тестирование на Wokwi

1. Открыть https://wokwi.com/
2. Создать новый проект "Raspberry Pi Pico"
3. Скопировать содержимое `wokwi/diagram.json`
4. Загрузить код из проекта
5. Запустить симуляцию

---

## 📡 Протокол связи с API

### 1. Аутентификация

**Запрос:**
```http
POST /api/Auth/robot/login HTTP/1.1
Content-Type: application/json

{
  "serialNumber": "PICO-DRONE-001",
  "accessKey": "your-secret-key"
}
```

**Ответ:**
```json
{
  "token": "eyJhbGc...",
  "robotId": 1,
  "expiresIn": 2073600
}
```

### 2. Получение информации о роботе

**Запрос:**
```http
GET /api/Robot/me HTTP/1.1
Authorization: Bearer {token}
```

### 3. Отправка телеметрии

**Запрос:**
```http
POST /api/Robot/status HTTP/1.1
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Delivering",
  "batteryLevel": 75.5,
  "currentLatitude": 50.4501,
  "currentLongitude": 30.5234,
  "targetNodeId": 8
}
```

### 4. Получение заказов

**Запрос:**
```http
GET /api/Robot/me/orders HTTP/1.1
Authorization: Bearer {token}
```

---

## 🔄 State Machine

```
┌──────┐
│ BOOT │
└───┬──┘
    ↓
┌────────────┐
│ WIFI_CONN  │
└─────┬──────┘
      ↓
┌─────────────┐
│    AUTH     │
└──────┬──────┘
       ↓
┌──────────────┐      Battery < 20%
│     IDLE     │ ────────────────┐
└──────┬───────┘                 │
       │                         ↓
       │ Order received    ┌──────────┐
       ↓                   │ CHARGING │
┌─────────────────┐        └──────────┘
│ NAV_TO_PICKUP   │
└────────┬────────┘
         ↓
┌─────────────┐
│  AT_PICKUP  │
└──────┬──────┘
       ↓
┌─────────────────┐
│ NAV_TO_DROPOFF  │
└────────┬────────┘
         ↓
┌──────────────┐
│  AT_DROPOFF  │
└──────┬───────┘
       ↓
  Order complete → IDLE
```

---

## 🧪 Тестирование

### Тест 1: WiFi подключение
```python
from lib.network_module import NetworkModule

network = NetworkModule()
if network.connect_wifi():
    print("✓ WiFi connected")
    print(f"IP: {network.get_ip()}")
```

### Тест 2: Аутентификация
```python
if network.authenticate():
    print("✓ Authenticated")
    print(f"Token: {network.token[:20]}...")
```

### Тест 3: GPS
```python
from lib.gps_module import GPSModule

gps = GPSModule()
gps.update()

if gps.has_fix():
    print(f"GPS: {gps.latitude}, {gps.longitude}")
```

### Тест 4: Телеметрия
```python
success = network.send_telemetry(
    status="Idle",
    battery=85.5,
    lat=50.4501,
    lon=30.5234
)
print(f"Telemetry sent: {success}")
```

---

## 📊 Производительность

**MicroPython на RP2040:**
- CPU: 133 MHz dual-core
- RAM: 264 KB
- HTTP запрос: ~200-500ms
- GPS парсинг: ~10-20ms
- Навигационные расчеты: ~5-10ms

**Частота обновлений:**
- GPS: 5 Hz (каждые 200ms)
- Телеметрия: каждые 5 секунд (DELIVERING)
- Проверка заказов: каждые 10 секунд (IDLE)
- Батарея: каждые 2 секунды

---

## 🛠️ Разработка

### Структура main.py

```python
import machine
import time
from config import *
from lib.state_machine import StateMachine, State
from lib.network_module import NetworkModule
from lib.gps_module import GPSModule
from lib.battery_module import BatteryModule
from lib.navigation_module import NavigationModule
from lib.motor_controller import MotorController
from lib.cargo_module import CargoModule

def main():
    # Инициализация модулей
    state_machine = StateMachine()
    network = NetworkModule()
    gps = GPSModule()
    battery = BatteryModule()
    navigation = NavigationModule()
    motors = MotorController()
    cargo = CargoModule()

    # Главный цикл
    while True:
        # Обновление модулей
        gps.update()
        battery.update()

        # State machine логика
        state_machine.update()

        # Обработка состояний
        if state_machine.state == State.IDLE:
            handle_idle(network, battery)
        elif state_machine.state == State.NAVIGATING_TO_PICKUP:
            handle_navigation(gps, navigation, motors)
        # ... и т.д.

        time.sleep(0.1)

if __name__ == "__main__":
    main()
```

---

## 📚 Библиотеки

MicroPython встроенные:
- `machine` - работа с GPIO, PWM, ADC
- `network` - WiFi (для ESP32)
- `urequests` - HTTP клиент
- `ujson` - JSON парсинг
- `utime` - время
- `math` - математические функции

Внешние (включены в проект):
- `micropyGPS` - GPS NMEA парсинг
- `simpleESP8266` - AT команды для ESP-01

---

## 🐛 Отладка

### Serial Monitor
```python
# Подключение к Pico через UART
# Используй Putty/Thonny на COM порту

import machine
uart = machine.UART(0, baudrate=115200)
print("Debug message")
```

### LED индикация
```python
led = machine.Pin(25, machine.Pin.OUT)

# Blink pattern для разных состояний
# IDLE: медленное мигание (1 раз/сек)
# DELIVERING: быстрое мигание (5 раз/сек)
# ERROR: постоянно горит
```

### Логирование
```python
def log(level, message):
    timestamp = time.time()
    print(f"[{timestamp}] {level}: {message}")

log("INFO", "Drone started")
log("ERROR", "WiFi connection failed")
```

---

## 🔒 Безопасность

1. **JWT Token Management**
   - Токен хранится в RAM (не в flash)
   - Автоматическое обновление при истечении

2. **WiFi Security**
   - WPA2 соединение
   - Таймауты на подключение

3. **Failsafe**
   - Автоматическая посадка при потере WiFi > 30 сек
   - Emergency stop при критическом уровне батареи

4. **Watchdog Timer**
   - Перезагрузка при зависании программы

---

## 📖 Полезные ссылки

- MicroPython Docs: https://docs.micropython.org/en/latest/rp2/quickref.html
- Raspberry Pi Pico Datasheet: https://datasheets.raspberrypi.com/pico/pico-datasheet.pdf
- Wokwi Simulator: https://wokwi.com/
- GPS NMEA Format: http://aprs.gids.nl/nmea/
- ESP8266 AT Commands: https://www.espressif.com/sites/default/files/documentation/4a-esp8266_at_instruction_set_en.pdf
