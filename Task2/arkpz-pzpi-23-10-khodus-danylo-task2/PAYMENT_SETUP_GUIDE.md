# Руководство по настройке платежных систем RobDelivery

Это подробное руководство по настройке и подключению платежных систем PayPal, Stripe и Google Pay к вашему RobDelivery API.

## Содержание
1. [Обзор](#обзор)
2. [PayPal](#paypal)
3. [Stripe](#stripe)
4. [Google Pay](#google-pay)
5. [Тестирование](#тестирование)

---

## Обзор

RobDelivery поддерживает три платежные системы:
- **PayPal** - прямая интеграция через PayPal Checkout SDK
- **Stripe** - интеграция через Stripe .NET SDK
- **Google Pay** - токены обрабатываются через Stripe как платежный шлюз

Все сервисы реализованы с использованием официальных SDK и поддерживают:
- ✅ Обработку платежей
- ✅ Возвраты (refunds)
- ✅ Валидацию данных
- ✅ Логирование всех операций
- ✅ Обработку ошибок

---

## PayPal

### Шаг 1: Создание PayPal аккаунта

1. Перейдите на https://developer.paypal.com/
2. Войдите или создайте PayPal Business аккаунт
3. Перейдите в раздел "My Apps & Credentials"

### Шаг 2: Создание приложения

1. Нажмите "Create App"
2. Введите название приложения (например, "RobDelivery")
3. Выберите тип приложения "Merchant"
4. Нажмите "Create App"

### Шаг 3: Получение учетных данных

После создания приложения вы получите:
- **Client ID** (Sandbox и Live)
- **Secret** (Sandbox и Live)

**Важно:** Для разработки используйте Sandbox учетные данные!

### Шаг 4: Настройка appsettings.json

```json
"Payment": {
  "PayPal": {
    "ClientId": "AeHxdE8Z9P_FqR...",
    "ClientSecret": "EG7hJ2kL...",
    "Mode": "sandbox"
  }
}
```

**Параметры:**
- `ClientId` - Client ID из PayPal Developer Dashboard
- `ClientSecret` - Secret из PayPal Developer Dashboard
- `Mode` - `"sandbox"` для тестирования, `"live"` для production

### Шаг 5: Создание тестовых аккаунтов (Sandbox)

1. В PayPal Developer Dashboard перейдите в "Sandbox" → "Accounts"
2. Создайте тестовый Business аккаунт (продавец)
3. Создайте тестовый Personal аккаунт (покупатель)
4. Используйте email Personal аккаунта для тестов

### Тестовые данные PayPal Sandbox

```
Email: sb-buyer@personal.example.com
Password: (из Dashboard)
```

### Пример запроса

```json
POST /api/payments/process
{
  "orderId": 1,
  "amount": 100.50,
  "paymentMethod": "PayPal",
  "currency": "UAH",
  "payPalEmail": "sb-buyer@personal.example.com"
}
```

---

## Stripe

### Шаг 1: Создание Stripe аккаунта

1. Перейдите на https://dashboard.stripe.com/register
2. Создайте аккаунт
3. Подтвердите email

### Шаг 2: Получение API ключей

1. В Dashboard перейдите в "Developers" → "API keys"
2. Вы увидите:
   - **Publishable key** (начинается с `pk_test_` или `pk_live_`)
   - **Secret key** (начинается с `sk_test_` или `sk_live_`)

**Важно:** Никогда не публикуйте Secret key в коде или на клиенте!

### Шаг 3: Настройка Webhooks (опционально)

1. Перейдите в "Developers" → "Webhooks"
2. Нажмите "Add endpoint"
3. Введите URL: `https://your-domain.com/api/payments/webhook`
4. Выберите события: `payment_intent.succeeded`, `charge.refunded`
5. Скопируйте Webhook signing secret

### Шаг 4: Настройка appsettings.json

```json
"Payment": {
  "Stripe": {
    "SecretKey": "sk_test_51ABC...",
    "PublishableKey": "pk_test_51ABC...",
    "WebhookSecret": "whsec_..."
  }
}
```

### Тестовые карты Stripe

```
Успешная карта:
  Номер: 4242 4242 4242 4242
  CVV: любые 3 цифры
  Срок: любая будущая дата

3D Secure (требует подтверждения):
  Номер: 4000 0027 6000 3184

Отклоненная карта:
  Номер: 4000 0000 0000 0002
```

### Шаг 5: Создание Payment Method Token

На клиенте (фронтенде) используйте Stripe.js для создания токена:

```javascript
const stripe = Stripe('pk_test_YOUR_PUBLISHABLE_KEY');

// Создание Payment Method
const {paymentMethod} = await stripe.createPaymentMethod({
  type: 'card',
  card: cardElement,
});

// paymentMethod.id будет иметь формат "pm_..."
```

### Пример запроса

```json
POST /api/payments/process
{
  "orderId": 1,
  "amount": 100.50,
  "paymentMethod": "Stripe",
  "currency": "UAH",
  "stripeCardToken": "pm_1ABC..."
}
```

---

## Google Pay

### Важно: Google Pay работает через платежный шлюз

Google Pay не обрабатывает платежи напрямую. Он предоставляет зашифрованные токены платежных данных, которые обрабатываются через платежные шлюзы (Stripe, PayPal, Adyen и др.).

В нашей реализации Google Pay токены обрабатываются через **Stripe**.

### Шаг 1: Настройка Google Pay Console

1. Перейдите на https://pay.google.com/business/console/
2. Создайте Business Profile
3. Зарегистрируйте ваш домен
4. Получите Merchant ID

### Шаг 2: Интеграция на фронтенде

Используйте Google Pay API для получения токена:

```javascript
const paymentsClient = new google.payments.api.PaymentsClient({
  environment: 'TEST' // или 'PRODUCTION'
});

const paymentDataRequest = {
  apiVersion: 2,
  apiVersionMinor: 0,
  allowedPaymentMethods: [{
    type: 'CARD',
    parameters: {
      allowedAuthMethods: ['PAN_ONLY', 'CRYPTOGRAM_3DS'],
      allowedCardNetworks: ['VISA', 'MASTERCARD']
    },
    tokenizationSpecification: {
      type: 'PAYMENT_GATEWAY',
      parameters: {
        'gateway': 'stripe',
        'stripe:version': '2023-10-16',
        'stripe:publishableKey': 'pk_test_YOUR_KEY'
      }
    }
  }],
  merchantInfo: {
    merchantId: 'YOUR_MERCHANT_ID',
    merchantName: 'RobDelivery'
  },
  transactionInfo: {
    totalPriceStatus: 'FINAL',
    totalPrice: '100.50',
    currencyCode: 'UAH'
  }
};

const paymentData = await paymentsClient.loadPaymentData(paymentDataRequest);
// paymentData.paymentMethodData.tokenizationData.token содержит Stripe token
```

### Шаг 3: Настройка appsettings.json

```json
"Payment": {
  "Stripe": {
    "SecretKey": "sk_test_51ABC...",
    "PublishableKey": "pk_test_51ABC...",
    "WebhookSecret": "whsec_..."
  },
  "GooglePay": {
    "MerchantId": "12345678901234567890",
    "MerchantName": "RobDelivery",
    "Gateway": "stripe",
    "GatewayMerchantId": "YOUR_STRIPE_MERCHANT_ID"
  }
}
```

### Получение Stripe Merchant ID

1. Войдите в Stripe Dashboard
2. Перейдите в Settings → Account details
3. Скопируйте "Account ID" (начинается с `acct_`)

### Пример запроса

```json
POST /api/payments/process
{
  "orderId": 1,
  "amount": 100.50,
  "paymentMethod": "GooglePay",
  "currency": "UAH",
  "googlePayToken": "pm_1ABC..."
}
```

---

## Тестирование

### Запуск тестов

1. Убедитесь, что все учетные данные настроены в `appsettings.json`
2. Запустите API: `dotnet run --project RobDeliveryAPI`
3. Используйте Swagger UI: `http://localhost:5102/swagger`

### Тестовый сценарий PayPal

```bash
curl -X POST http://localhost:5102/api/payments/process \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": 1,
    "amount": 50.00,
    "paymentMethod": "PayPal",
    "currency": "UAH",
    "payPalEmail": "sb-buyer@personal.example.com"
  }'
```

### Тестовый сценарий Stripe

```bash
# Сначала создайте Payment Method через Stripe API или используйте тестовый токен
curl -X POST http://localhost:5102/api/payments/process \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": 1,
    "amount": 75.50,
    "paymentMethod": "Stripe",
    "currency": "UAH",
    "stripeCardToken": "pm_card_visa"
  }'
```

### Тест возврата (Refund)

```bash
curl -X POST http://localhost:5102/api/payments/refund \
  -H "Content-Type: application/json" \
  -d '{
    "transactionId": "pi_1ABC...",
    "amount": 50.00
  }'
```

---

## Безопасность

### Обязательные меры безопасности:

1. **Никогда** не храните API ключи в коде или Git репозитории
2. Используйте `appsettings.Development.json` для локальной разработки
3. В production используйте Azure Key Vault, AWS Secrets Manager или аналоги
4. Используйте HTTPS для всех запросов
5. Валидируйте webhooks используя подписи
6. Логируйте все транзакции для аудита

### Использование User Secrets (для разработки)

```bash
# Перейдите в папку RobDeliveryAPI
cd RobDeliveryAPI

# Установите secret для PayPal
dotnet user-secrets set "Payment:PayPal:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Payment:PayPal:ClientSecret" "YOUR_CLIENT_SECRET"

# Установите secret для Stripe
dotnet user-secrets set "Payment:Stripe:SecretKey" "sk_test_YOUR_KEY"
dotnet user-secrets set "Payment:Stripe:PublishableKey" "pk_test_YOUR_KEY"
```

---

## Переход в Production

### Контрольный список:

- [ ] Получены Live учетные данные для всех платежных систем
- [ ] `Mode` изменен с `"sandbox"` на `"live"` для PayPal
- [ ] Используются `sk_live_` и `pk_live_` ключи для Stripe
- [ ] Настроены Webhooks для всех систем
- [ ] API ключи перенесены в безопасное хранилище (Key Vault)
- [ ] Включено HTTPS
- [ ] Настроен мониторинг транзакций
- [ ] Проведено тестирование на staging окружении

---

## Частые проблемы

### PayPal: "AUTHENTICATION_FAILURE"
**Решение:** Проверьте Client ID и Client Secret, убедитесь что используете правильный Mode

### Stripe: "No such payment_method"
**Решение:** Убедитесь что Payment Method создан в том же Stripe аккаунте, что и используемый API ключ

### Google Pay: "Token validation failed"
**Решение:** Проверьте что Gateway настроен правильно и Stripe Publishable Key корректен

### Общая ошибка: "API key not found"
**Решение:** Убедитесь что `appsettings.json` правильно загружается и все ключи заполнены

---

## Дополнительная информация

### Официальная документация:

- **PayPal:** https://developer.paypal.com/docs/checkout/
- **Stripe:** https://stripe.com/docs/api
- **Google Pay:** https://developers.google.com/pay/api

### Поддержка:

Если у вас возникли проблемы с платежными системами:
1. Проверьте логи приложения
2. Проверьте Dashboard платежной системы
3. Убедитесь что используете тестовые данные для sandbox
4. Проверьте что все NuGet пакеты установлены

---

## Заключение

Теперь ваша RobDelivery API полностью интегрирована с тремя основными платежными системами. Все сервисы используют официальные SDK, поддерживают возвраты и имеют полное логирование.

**Не забудьте:**
- Регулярно обновлять SDK до последних версий
- Мониторить транзакции через Dashboard платежных систем
- Настроить алерты на неудачные платежи
- Хранить API ключи в безопасности
