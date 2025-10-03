# Ingenico ECR API - Endpoint Kullanım Kılavuzu

**Base URL:** `http://localhost:9000/ingenico/`

**Port:** Varsayılan 9000 (GMP.INI veya App.config'den değiştirilebilir)

---

## İçindekiler
- [Sistem & Durum](#sistem--durum)
- [Bağlantı & Pairing](#bağlantı--pairing)
- [Banka & Vergi](#banka--vergi)
- [Fiş İşlemleri](#fiş-işlemleri)
- [Sipariş Takibi](#sipariş-takibi)
- [Raporlar](#raporlar)
- [Ayarlar](#ayarlar)

---

## Sistem & Durum

### 1. Health Check
**Endpoint:** `GET /Health`

**Açıklama:** API ve Ingenico cihazının sağlık durumunu kontrol eder. Lock mekanizması ile korunmuştur ancak timeout=0 olduğu için cihaz meşgulse direkt "busy" döner.

**cURL:**
```bash
curl --location 'http://localhost:9000/ingenico/Health'
```

**Response (Başarılı):**
```json
{
    "status": "healthy",
    "message": "API ve cihaz çalışıyor",
    "apiRunning": true,
    "deviceStatus": "available",
    "connectionStatus": "Connected",
    "activeCashier": "ADMIN",
    "serialNumber": "JI20098756",
    "timestamp": "2025-10-02T10:30:45"
}
```

**Response (Meşgul):**
```json
{
    "status": "busy",
    "message": "EFT-POS CİHAZI MEŞGUL",
    "apiRunning": true,
    "deviceStatus": "busy",
    "timestamp": "2025-10-02T10:30:45"
}
```

---

### 2. Cash Register Status
**Endpoint:** `GET /CashRegisterStatus`

**Açıklama:** Yazarkasanın anlık durumunu döner.

**cURL:**
```bash
curl --location 'http://localhost:9000/ingenico/CashRegisterStatus'
```

**Response:**
```json
"YAZARKASA BOŞTA"
```

Olası durumlar:
- `"YAZARKASA BOŞTA"`
- `"FİŞ YAZDIRILIYOR"`
- `"ÖDEME BEKLENİYOR"`
- `"FİŞ KAPATILIYOR"`
- `"FİŞ İPTAL EDİLİYOR"`

---

## Bağlantı & Pairing

### 3. Pairing
**Endpoint:** `POST /Pairing`

**Açıklama:** Ingenico cihazına bağlanır ve pairing işlemini gerçekleştirir. Cihaz bilgilerini döner.

**cURL:**
```bash
curl --location --request POST 'http://localhost:9000/ingenico/Pairing'
```

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Message": "Pairing başarılı",
    "Data": {
        "ReturnCode": 0,
        "ReturnCodeMessage": "0x0000 : TRAN_RESULT_OK [0]",
        "GmpInfo": {
            "EcrSerialNumber": "JI20098756",
            "EcrModel": "IWE281",
            "ActiveCashier": "ADMIN",
            "ActiveCashierNo": 0,
            "FirmwareVersion": "1.2.3",
            "CurrentInterface": 123456,
            "ZNo": 45,
            "FNo": 1234
        }
    }
}
```

---

### 4. Echo
**Endpoint:** `POST /Echo`

**Açıklama:** Cihaz echo testi yapar.

**cURL:**
```bash
curl --location --request POST 'http://localhost:9000/ingenico/Echo'
```

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Message": "Echo başarılı"
}
```

---

### 5. Ping
**Endpoint:** `POST /Ping`

**Açıklama:** Hafif bağlantı kontrolü. Cihaz meşgulse direkt "busy" döner (timeout=0).

**cURL:**
```bash
curl --location --request POST 'http://localhost:9000/ingenico/Ping'
```

**Response (Başarılı):**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Message": "Ping başarılı",
    "Data": {
        "ReturnCode": 0,
        "ReturnCodeMessage": "0x0000 : TRAN_RESULT_OK [0]"
    }
}
```

**Response (Meşgul):**
```json
{
    "Status": false,
    "ErrorCode": "0xF01C",
    "Message": "YAZARKASA MEŞGUL. EFT-POS CİHAZINI KONTROL EDİNİZ.",
    "Data": {
        "ReturnCode": 61468,
        "ReturnCodeMessage": "DLL_RETCODE_RECV_BUSY [61468]"
    }
}
```

---

## Banka & Vergi

### 6. Bank List
**Endpoint:** `POST /BankList`

**Açıklama:** Cihazda tanımlı banka listesini döner.

**cURL:**
```bash
curl --location --request POST 'http://localhost:9000/ingenico/BankList'
```

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Message": "Banka listesi alındı",
    "Data": [
        {
            "BankId": 1,
            "BankName": "AKBANK",
            "IsActive": true
        },
        {
            "BankId": 2,
            "BankName": "GARANTİ BANKASI",
            "IsActive": true
        }
    ]
}
```

---

### 7. Get Tax Groups
**Endpoint:** `POST /GetTaxGroups`

**Açıklama:** KDV gruplarını döner.

**cURL:**
```bash
curl --location --request POST 'http://localhost:9000/ingenico/GetTaxGroups'
```

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Data": {
        "TaxGroups": [
            {
                "GroupId": 1,
                "TaxRate": 18.0,
                "GroupName": "KDV %18"
            },
            {
                "GroupId": 2,
                "TaxRate": 8.0,
                "GroupName": "KDV %8"
            },
            {
                "GroupId": 3,
                "TaxRate": 1.0,
                "GroupName": "KDV %1"
            }
        ]
    }
}
```

---

### 8. Get Departmans
**Endpoint:** `POST /GetDepartmans`

**Açıklama:** Departman listesini döner.

**cURL:**
```bash
curl --location --request POST 'http://localhost:9000/ingenico/GetDepartmans'
```

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Data": [
        {
            "DepartmentId": 1,
            "DepartmentName": "GENEL",
            "TaxGroupId": 1
        },
        {
            "DepartmentId": 2,
            "DepartmentName": "GIDA",
            "TaxGroupId": 2
        }
    ]
}
```

---

## Fiş İşlemleri

### 9. EftPos Print Order (Fiş Yazdırma)
**Endpoint:** `POST /EftPosPrintOrder`

**Açıklama:** Satış, iade veya iptal fişi yazdırır. En kritik endpoint.

**cURL (Satış Fişi):**
```bash
curl --location 'http://localhost:9000/ingenico/EftPosPrintOrder' \
--header 'Content-Type: application/json' \
--data '{
    "OrderID": 12345,
    "OrderKey": "550e8400-e29b-41d4-a716-446655440000",
    "PrintInvoice": false,
    "fiscalLines": [
        {
            "ItemName": "COCA COLA 330ML",
            "Quantity": 2,
            "UnitPrice": 1500,
            "TaxGroupId": 1,
            "DepartmentId": 1
        },
        {
            "ItemName": "SU 500ML",
            "Quantity": 1,
            "UnitPrice": 500,
            "TaxGroupId": 2,
            "DepartmentId": 2
        }
    ],
    "paymentLines": [
        {
            "PaymentBaseTypeID": 1,
            "Amount": 3500,
            "PaymentName": "NAKİT"
        }
    ]
}'
```

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Message": "İşlem başarılı",
    "Data": {
        "ReturnCode": 0,
        "ReturnCodeMessage": "0x0000 : TRAN_RESULT_OK [0]",
        "TicketInfo": {
            "FNo": 1235,
            "ZNo": 45,
            "TotalReceiptAmount": 3500,
            "TotalReceiptPayment": 3500,
            "ChangeAmount": 0,
            "DateTime": "2025-10-02T10:45:30"
        }
    }
}
```

**PaymentBaseTypeID Değerleri:**
- `1` - Nakit
- `2` - Kredi Kartı
- `3` - Çek
- `7,8,9,10` - Yemek Çeki

---

### 10. Void All
**Endpoint:** `POST /VoidAll`

**Açıklama:** Tüm fişi iptal eder (FP3_VoidAll).

**cURL:**
```bash
curl --location --request POST 'http://localhost:9000/ingenico/VoidAll'
```

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Message": "İşlem başarılı",
    "Data": {
        "ReturnCode": 0,
        "TicketInfo": {
            "FNo": 0
        }
    }
}
```

---

### 11. Void Payment
**Endpoint:** `POST /VoidPayment`

**Açıklama:** Belirli bir ödemeyi iptal eder. **ÖNEMLI:** Sadece FP3_PrintTotalsAndPayments çağrılmadan ÖNCE kullanılabilir.

**cURL:**
```bash
curl --location 'http://localhost:9000/ingenico/VoidPayment' \
--header 'Content-Type: application/json' \
--data '{
    "PaymentIndex": 0
}'
```

**Parametreler:**
- `PaymentIndex`: İptal edilecek ödemenin index'i (0'dan başlar)
  - İlk ödeme = 0
  - İkinci ödeme = 1
  - vb.

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Message": "İşlem başarılı",
    "Data": {
        "ReturnCode": 0
    }
}
```

---

## Sipariş Takibi

### 12. Is Completed
**Endpoint:** `GET /IsCompleted/{orderKey}`

**Açıklama:** Sipariş tamamlandı mı kontrol eder.

**cURL:**
```bash
curl --location 'http://localhost:9000/ingenico/IsCompleted/550e8400-e29b-41d4-a716-446655440000'
```

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Message": "ÖDEME DAHA ÖNCE ALINMIŞ VE BAŞARILI İLE KAPANMIŞ",
    "Data": {
        "TicketInfo": {
            "FNo": 1235,
            "ZNo": 45
        }
    }
}
```

---

### 13. Get Completed Order
**Endpoint:** `GET /Completed/{orderKey}`

**Açıklama:** Tamamlanmış sipariş detaylarını döner.

**cURL:**
```bash
curl --location 'http://localhost:9000/ingenico/Completed/550e8400-e29b-41d4-a716-446655440000'
```

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Data": {
        "TicketInfo": {
            "FNo": 1235,
            "ZNo": 45,
            "TotalReceiptAmount": 3500,
            "TotalReceiptPayment": 3500
        },
        "PrintReceiptInfo": "{...}"
    }
}
```

---

### 14. Get Fiscal
**Endpoint:** `GET /GetFiscal/{orderKey}`

**Açıklama:** Sipariş fiscal bilgilerini döner.

**cURL:**
```bash
curl --location 'http://localhost:9000/ingenico/GetFiscal/550e8400-e29b-41d4-a716-446655440000'
```

---

### 15. Is Waiting
**Endpoint:** `GET /IsWaiting`

**Açıklama:** Bekleyen siparişlerin listesini döner.

**cURL:**
```bash
curl --location 'http://localhost:9000/ingenico/IsWaiting'
```

**Response:**
```json
{
    "Status": true,
    "Data": [
        "550e8400-e29b-41d4-a716-446655440000",
        "660e8400-e29b-41d4-a716-446655440001"
    ]
}
```

---

### 16. Order Status
**Endpoint:** `GET /OrderStatus/{orderKey}`

**Açıklama:** Sipariş durumunu kontrol eder.

**cURL:**
```bash
curl --location 'http://localhost:9000/ingenico/OrderStatus/550e8400-e29b-41d4-a716-446655440000'
```

---

## Raporlar

### 17. Report Print
**Endpoint:** `POST /ReportPrint`

**Açıklama:** Çeşitli raporları yazdırır veya okur. Admin şifresi gerektirir.

#### Z Raporu (Günlük Kapanış)
**DİKKAT:** Cihazı resetler!

```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 0,
    "AdminPassword": "0000"
}'
```

**Response:**
```json
{
    "Status": true,
    "ErrorCode": "0",
    "Data": "{\"ZNo\":45,\"TotalSales\":125000,\"TotalVoid\":5000,...}"
}
```

---

#### X Raporu (Ara Rapor)
Cihazı resetlemez.

```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 1
}'
```

**Response:**
```json
{
    "Status": true,
    "Data": "X RAPORU YAZDIRILDI"
}
```

---

#### X Raporu (Tarih Aralıklı)
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 1,
    "startDate": "2025-10-01T00:00:00",
    "lastDate": "2025-10-02T23:59:59"
}'
```

---

#### EKÜ Raporu
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 2
}'
```

---

#### EKÜ Doluluk Oranı (EkuRead)
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 15
}'
```

**Response:**
```json
{
    "Status": true,
    "Data": "Kullanılan Ekü : % 45.67 , Kalan Ekü : % 54.33"
}
```

---

#### EKÜ Detaylı Bilgi (EkuReading)
Cihazdan yazdırmaz, sadece JSON verisi döner.

```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 22
}'
```

**Response:**
```json
{
    "Status": true,
    "Data": "{\"Eku\":{\"DataUsedArea\":123456,\"DataFreeArea\":876544,...}}"
}
```

---

#### Kümülatif Rapor
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 3,
    "startDate": "2025-10-01T00:00:00",
    "lastDate": "2025-10-02T23:59:59"
}'
```

---

#### Mali Rapor
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 4,
    "startDate": "2025-10-01T00:00:00",
    "lastDate": "2025-10-02T23:59:59"
}'
```

---

#### İki Tarih Arası EKÜ Raporu
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 5,
    "startDate": "2025-10-01T00:00:00",
    "lastDate": "2025-10-02T23:59:59"
}'
```

---

#### Fiş Arası EKÜ Raporu
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 6,
    "Zno": "45",
    "startZno": "100",
    "lastZno": "200"
}'
```

---

#### Banka Gün Sonu
Tüm banka POS'larının gün sonu işlemi.

```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 7
}'
```

**Response:**
```json
{
    "Status": true,
    "Data": "BANKA GÜN SONLARI YAZDIRILDI"
}
```

---

#### Son Fiş Kopyası
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 8
}'
```

---

#### Parametre Yükleme
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 16
}'
```

---

#### Z Raporu (Z No Arası)
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 18,
    "startZno": "40",
    "lastZno": "45"
}'
```

---

#### Belirli Z Raporunu Oku (Web Servis)
**NOT:** Cihazdan yazdırmaz, sadece JSON verisi döner.

```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 21,
    "Zno": "45"
}'
```

**Response:**
```json
{
    "Status": true,
    "Data": "{\"ZNo\":45,\"Date\":\"2025-10-02\",\"TotalSales\":125000,...}"
}
```

---

#### Z Raporları Aralığını Oku (Web Servis)
Başlangıç Z'den mevcut Z'ye kadar tüm raporları döner.

```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 20,
    "Zno": "40"
}'
```

**Response:**
```json
{
    "Status": true,
    "Data": "[{\"ZNo\":40,...},{\"ZNo\":41,...},{\"ZNo\":42,...}]"
}
```

---

### Report Type Listesi

| ReportType | İsim | Açıklama | Cihazdan Yazdırır |
|------------|------|----------|-------------------|
| 0 | ZReport | Günlük kapanış (cihazı resetler) | ✅ |
| 1 | XReport | Ara rapor | ✅ |
| 2 | EkuReport | EKÜ raporu | ✅ |
| 3 | KumulatifReport | Kümülatif rapor | ✅ |
| 4 | MaliReport | Mali rapor | ✅ |
| 5 | DateBetweenEkuReport | İki tarih arası EKÜ | ✅ |
| 6 | ReceiptBetweenEkuReport | Fiş arası EKÜ | ✅ |
| 7 | BankEndOfDay | Banka gün sonu | ✅ |
| 8 | ReceiptCopy | Son fiş kopyası | ✅ |
| 15 | EkuRead | EKÜ doluluk oranı | ❌ (Sadece veri) |
| 16 | ParamsLoad | Parametre yükleme | ✅ |
| 17 | ZBetween | Tarih arası Z | ✅ |
| 18 | ZNoBetween | Z no arası | ✅ |
| 20 | ZreportBetweenWebServis | Z aralığı (JSON) | ❌ (Sadece veri) |
| 21 | ZreportWebServis | Belirli Z (JSON) | ❌ (Sadece veri) |
| 22 | EkuReading | Detaylı EKÜ bilgisi | ❌ (Sadece veri) |

---

### 18. Read Z Report
**Endpoint:** `POST /ReadZReport`

**Açıklama:** Z raporu okur (yazdırmaz).

**cURL:**
```bash
curl --location --request POST 'http://localhost:9000/ingenico/ReadZReport'
```

---

## Ayarlar

### 19. Get Header
**Endpoint:** `POST /Header`

**Açıklama:** Fiş header bilgilerini alır.

**cURL:**
```bash
curl --location --request POST 'http://localhost:9000/ingenico/Header'
```

**Response:**
```json
{
    "Status": true,
    "Data": {
        "Line1": "ŞİRKET ADI",
        "Line2": "ADRES BİLGİSİ",
        "Line3": "TEL: 0212 123 45 67"
    }
}
```

---

### 20. Set Header
**Endpoint:** `POST /SetHeader`

**Açıklama:** Fiş header ayarlar.

**cURL:**
```bash
curl --location 'http://localhost:9000/ingenico/SetHeader' \
--header 'Content-Type: application/json' \
--data '{
    "Line1": "ÖRNEK FİRMA A.Ş.",
    "Line2": "İSTANBUL/TÜRKİYE",
    "Line3": "TEL: 0212 999 88 77"
}'
```

**Response:**
```json
{
    "Status": true,
    "Message": "Header başarıyla ayarlandı"
}
```

---

### 21. Tax Groups Pairing
**Endpoint:** `POST /TaxGroupsPairing`

**Açıklama:** Vergi grubu pairing yapar.

**cURL:**
```bash
curl --location --request POST 'http://localhost:9000/ingenico/TaxGroupsPairing'
```

---

## Hata Kodları

### Genel Hata Response Formatı
```json
{
    "Status": false,
    "ErrorCode": "2438",
    "Message": "0x0986 : APP_ERR_GMP3_INCORRECT_PASSWORD [2438] - YAZARKASA YANLIŞ ŞİFRE",
    "Data": null
}
```

### Yaygın Hata Kodları

| Kod | Hex | Açıklama |
|-----|-----|----------|
| 0 | 0x0000 | TRAN_RESULT_OK - Başarılı |
| 61468 | 0xF01C | RECV_BUSY - Cihaz meşgul |
| 2438 | 0x0986 | INCORRECT_PASSWORD - Yanlış şifre |
| 2346 | - | INVALID_SEQUENCE - Geçersiz sıra numarası |
| 9999 | - | Exception - Genel hata |
| 9998 | - | Request body boş |

---

## Güvenlik & Lock Mekanizması

### Global Lock
Tüm kritik endpoint'ler global lock ile korunmuştur:

- **Timeout:** 120 saniye (2 dakika)
- **Ping & Health:** Timeout=0 (meşgul ise direkt döner)
- **Amaç:** Aynı anda birden fazla işlemin cihaza gitmesini engeller

**Lock korumalı endpoint'ler:**
- Pairing
- Echo
- BankList
- Header
- TaxGroupsPairing
- GetTaxGroups
- GetDepartmans
- ReadZReport
- EftPosPrintOrder
- ReportPrint
- SetHeader
- VoidAll
- VoidPayment

**Lock olmayan endpoint'ler:**
- Health
- CashRegisterStatus
- IsCompleted
- IsWaiting
- OrderStatus

---

## Admin Şifresi Yönetimi

Admin şifresi 3 şekilde ayarlanabilir:

### 1. Request'te Gönderme (Önerilen)
```bash
curl --location 'http://localhost:9000/ingenico/ReportPrint' \
--header 'Content-Type: application/json' \
--data '{
    "ReportType": 0,
    "AdminPassword": "1234"
}'
```

### 2. Settings.ini Dosyası
**Dosya:** `Ecr.Host/bin/Debug/Modules/ingenico/settings.ini`
```ini
adminpassword=0000
```

### 3. Varsayılan
Eğer hiçbir yerde tanımlı değilse: `"0000"`

**Öncelik Sırası:**
1. Request body → AdminPassword
2. settings.ini → adminpassword
3. Default → "0000"

---

## Postman Collection

### Import Edilebilir JSON

Tüm endpoint'leri içeren Postman collection:

```json
{
    "info": {
        "name": "Ingenico ECR API",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
    },
    "item": [
        {
            "name": "Health",
            "request": {
                "method": "GET",
                "url": "http://localhost:9000/ingenico/Health"
            }
        },
        {
            "name": "Ping",
            "request": {
                "method": "POST",
                "url": "http://localhost:9000/ingenico/Ping"
            }
        },
        {
            "name": "EftPosPrintOrder - Satış",
            "request": {
                "method": "POST",
                "header": [{"key": "Content-Type", "value": "application/json"}],
                "url": "http://localhost:9000/ingenico/EftPosPrintOrder",
                "body": {
                    "mode": "raw",
                    "raw": "{\n    \"OrderID\": 12345,\n    \"OrderKey\": \"550e8400-e29b-41d4-a716-446655440000\",\n    \"PrintInvoice\": false,\n    \"fiscalLines\": [\n        {\n            \"ItemName\": \"ÜRÜN 1\",\n            \"Quantity\": 1,\n            \"UnitPrice\": 1000,\n            \"TaxGroupId\": 1,\n            \"DepartmentId\": 1\n        }\n    ],\n    \"paymentLines\": [\n        {\n            \"PaymentBaseTypeID\": 1,\n            \"Amount\": 1000\n        }\n    ]\n}"
                }
            }
        },
        {
            "name": "ReportPrint - Z Raporu",
            "request": {
                "method": "POST",
                "header": [{"key": "Content-Type", "value": "application/json"}],
                "url": "http://localhost:9000/ingenico/ReportPrint",
                "body": {
                    "mode": "raw",
                    "raw": "{\n    \"ReportType\": 0,\n    \"AdminPassword\": \"0000\"\n}"
                }
            }
        },
        {
            "name": "VoidAll",
            "request": {
                "method": "POST",
                "url": "http://localhost:9000/ingenico/VoidAll"
            }
        }
    ]
}
```

---

## Notlar

### Önemli Dikkat Edilmesi Gerekenler

1. **Z Raporu** cihazı resetler - dikkatli kullanın
2. **VoidPayment** sadece totaller yazdırılmadan önce çalışır
3. **Lock timeout** 120 saniye - uzun işlemler olabilir
4. **Admin şifresi** settings.ini veya request'te olmalı
5. **OrderKey** unique olmalı (GUID formatı önerilir)
6. **Tarih formatı:** ISO 8601 (yyyy-MM-ddTHH:mm:ss)
7. **Fiyatlar** kuruş cinsinden (1 TL = 100)
8. **PaymentBaseTypeID** doğru kullanılmalı

### Test Ortamı İçin

```bash
# Health check
curl http://localhost:9000/ingenico/Health

# Ping test
curl -X POST http://localhost:9000/ingenico/Ping

# EKÜ kontrolü
curl -X POST http://localhost:9000/ingenico/ReportPrint \
  -H "Content-Type: application/json" \
  -d '{"ReportType": 15}'
```

---

## Versiyon Bilgisi

- **API Version:** 1.0
- **Ingenico GMP Version:** 3.x
- **Framework:** .NET Framework 4.8
- **Protocol:** OWIN/WebAPI
- **Default Port:** 9000

---

## Destek

Sorunlar için:
1. Log dosyalarını kontrol edin: `Ecr.Host/bin/Debug/EcrLog/`
2. GMP.INI ayarlarını doğrulayın
3. Settings.ini'deki admin şifresini kontrol edin
4. Health endpoint'i ile cihaz durumunu kontrol edin

---

**Son Güncelleme:** 2025-10-02
