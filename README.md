# 🛡️ Fraud Shield V2 - Gelişmiş Dolandırıcılık Tespit Sistemi

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Python](https://img.shields.io/badge/Python-3.8+-green.svg)](https://www.python.org/downloads/)
[![React](https://img.shields.io/badge/React-19.1.0-blue.svg)](https://reactjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-4.9.5-blue.svg)](https://www.typescriptlang.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-orange.svg)](https://docs.docker.com/compose/)

## 📋 İçindekiler

- [Proje Hakkında](#-proje-hakkında)
- [Özellikler](#-özellikler)
- [Mimari Yapı](#-mimari-yapı)
- [Teknolojiler](#-teknolojiler)
- [Kurulum](#-kurulum)
- [Kullanım](#-kullanım)
- [API Dokümantasyonu](#-api-dokümantasyonu)
- [Model Performansı](#-model-performansı)
- [Katkıda Bulunma](#-katkıda-bulunma)

## 🎯 Proje Hakkında

Fraud Shield V2, modern teknolojiler kullanılarak geliştirilmiş kapsamlı bir dolandırıcılık tespit sistemidir. Bu proje, .NET 8, Python ve React teknolojilerini birleştirerek gerçek zamanlı işlem analizi, makine öğrenmesi tabanlı tahmin ve kullanıcı dostu bir dashboard sunar.

### 🎯 Ana Hedefler

- **Gerçek Zamanlı Analiz**: İşlemlerin anında analiz edilmesi
- **Yüksek Doğruluk**: %95+ doğruluk oranı ile fraud tespiti
- **Ölçeklenebilirlik**: Mikroservis mimarisi ile yüksek performans
- **Kullanıcı Dostu**: Modern React dashboard ile kolay yönetim
- **Hibrit Yaklaşım**: Kural tabanlı + ML tabanlı analiz

## ✨ Özellikler

### 🔍 Dolandırıcılık Tespiti
- **Ensemble Model**: LightGBM + PCA Anomaly Detection
- **Kural Tabanlı Sistem**: Özelleştirilebilir fraud kuralları
- **Risk Faktörü Analizi**: Çok boyutlu risk değerlendirmesi
- **SHAP Analizi**: Model kararlarının açıklanabilirliği

### 📊 Dashboard & Raporlama
- **Gerçek Zamanlı İzleme**: Canlı işlem takibi
- **Performans Metrikleri**: Model başarı oranları
- **Risk Haritası**: Coğrafi risk dağılımı
- **Trend Analizi**: Zaman bazlı fraud trendleri

### 🛡️ Güvenlik
- **Kara Liste Yönetimi**: IP, hesap, cihaz kara listesi
- **Otomatik Bloklama**: Şüpheli işlemlerin otomatik engellenmesi
- **Audit Trail**: Tüm işlemlerin kayıt altına alınması
- **Rol Tabanlı Erişim**: Güvenli kullanıcı yönetimi

### 🔧 Yönetim Araçları
- **Model Yönetimi**: Model eğitimi ve versiyonlama
- **Kural Yönetimi**: Dinamik kural oluşturma ve düzenleme
- **Alert Yönetimi**: Fraud uyarılarının yönetimi
- **Sistem İzleme**: Performans ve sağlık kontrolü

## 🏗️ Mimari Yapı

```
fraudV2/
├── src/                          # .NET Backend
│   ├── Analiz.API/              # REST API Katmanı
│   ├── Analiz.Application/      # İş Mantığı Katmanı
│   ├── Analiz.Domain/           # Domain Model Katmanı
│   ├── Analiz.Infrastructure/   # Altyapı Katmanı
│   ├── Analiz.Persistence/      # Veri Erişim Katmanı
│   └── Analiz.ML/               # Makine Öğrenmesi Katmanı
├── Python/                       # Python ML Servisleri
│   ├── fraud_prediction.py      # Ana tahmin motoru
│   ├── advanced_ml_models.py    # Gelişmiş ML modelleri
│   ├── flask_api.py             # Python API servisi
│   └── requirements.txt         # Python bağımlılıkları
├── react-fraud-dashboard/        # React Frontend
│   ├── src/
│   │   ├── components/          # UI Bileşenleri
│   │   ├── pages/              # Sayfa Bileşenleri
│   │   ├── services/           # API Servisleri
│   │   └── hooks/              # Custom React Hooks
│   └── package.json
├── Models/                       # Eğitilmiş ML Modelleri
├── Data/                        # Veri Setleri
└── docker-compose.yml           # Docker Konfigürasyonu
```

### 🔄 Sistem Akışı

1. **İşlem Alımı**: Transaction API'ye gelen işlem verisi
2. **Ön İşleme**: Feature extraction ve veri temizleme
3. **ML Analizi**: Python servislerinde model tahminleri
4. **Kural Kontrolü**: .NET tarafında kural tabanlı analiz
5. **Risk Değerlendirmesi**: Hibrit skorlama sistemi
6. **Karar Verme**: Otomatik aksiyon veya manuel inceleme
7. **Dashboard Güncelleme**: React frontend'de sonuç gösterimi

## 🛠️ Teknolojiler

### Backend (.NET 8)
- **Framework**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core
- **ML**: Microsoft.ML, ML.NET
- **Cache**: Redis
- **Database**: SQL Server / PostgreSQL
- **Authentication**: JWT Bearer Token
- **Logging**: Serilog
- **Testing**: xUnit, Moq

### Python ML Services
- **Framework**: Flask
- **ML Libraries**: 
  - scikit-learn (1.3.0)
  - LightGBM (4.0.0)
  - TensorFlow (2.13.0)
  - XGBoost (1.7.6)
- **Analytics**: 
  - SHAP (0.42.1)
  - LIME (0.2.0.1)
  - Plotly (5.15.0)
- **Data Processing**: pandas, numpy

### Frontend (React)
- **Framework**: React 19.1.0
- **Language**: TypeScript 4.9.5
- **UI Library**: Material-UI (MUI) 7.1.1
- **Charts**: Recharts 2.15.3
- **State Management**: React Context + Hooks
- **HTTP Client**: Axios 1.9.0
- **Routing**: React Router DOM 7.6.2

### DevOps & Infrastructure
- **Containerization**: Docker & Docker Compose
- **CI/CD**: GitHub Actions (önerilen)
- **Monitoring**: Application Insights
- **Version Control**: Git

## 🚀 Kurulum

### Ön Gereksinimler

- .NET 8.0 SDK
- Python 3.8+
- Node.js 18+
- Docker & Docker Compose
- SQL Server veya PostgreSQL

### 1. Repository Klonlama

```bash
git clone https://github.com/yourusername/fraud-shield-v2.git
cd fraud-shield-v2
```

### 2. Backend Kurulumu (.NET)

```bash
cd src
dotnet restore
dotnet build
```

### 3. Python ML Services Kurulumu

```bash
cd Python
python -m venv .venv
source .venv/bin/activate  # Windows: .venv\Scripts\activate
pip install -r requirements.txt
```

### 4. Frontend Kurulumu (React)

```bash
cd react-fraud-dashboard
npm install
```

### 5. Docker ile Hızlı Başlangıç

```bash
# Tüm servisleri başlat
docker-compose up -d

# Servisleri durdur
docker-compose down
```

### 6. Veritabanı Kurulumu

```bash
# Migration'ları çalıştır
cd src
dotnet ef database update --project Analiz.Persistence --startup-project Analiz.API
```

## 📖 Kullanım

### Backend API Başlatma

```bash
cd src/Analiz.API
dotnet run
# API: http://localhost:5000
```

### Python ML Services Başlatma

```bash
cd Python
python flask_api.py
# Python API: http://localhost:5001
```

### React Dashboard Başlatma

```bash
cd react-fraud-dashboard
npm start
# Dashboard: http://localhost:3000
```

### Model Eğitimi

```bash
# LightGBM model eğitimi
curl -X POST http://localhost:5000/api/model/train/lightgbm

# PCA model eğitimi
curl -X POST http://localhost:5000/api/model/train/pca
```

## 📚 API Dokümantasyonu

### Ana Endpoints

#### İşlem Analizi
```http
POST /api/frauddetection/analyze
Content-Type: application/json

{
  "userId": "user123",
  "amount": 1500.00,
  "merchantId": "merchant456",
  "ipAddress": "192.168.1.100",
  "deviceId": "device789",
  "location": {
    "country": "TR",
    "city": "Istanbul"
  }
}
```

#### Model Yönetimi
```http
# Model eğitimi
POST /api/model/train/lightgbm
POST /api/model/train/pca

# Model metrikleri
GET /api/model/{modelName}/metrics

# Model aktivasyonu
POST /api/model/{modelName}/versions/{version}/activate
```

#### Kural Yönetimi
```http
# Kural listesi
GET /api/fraudrules

# Yeni kural oluşturma
POST /api/fraudrules

# Kural güncelleme
PUT /api/fraudrules/{id}
```

#### Kara Liste Yönetimi
```http
# Kara liste kontrolü
POST /api/blacklist/check

# Kara liste öğesi ekleme
POST /api/blacklist
```

### Python API Endpoints

```http
# Sağlık kontrolü
GET http://localhost:5001/health

# SHAP analizi
POST http://localhost:5001/analyze/shap

# Model tahmini
POST http://localhost:5001/models/predict
```

## 📊 Model Performansı

### LightGBM Model Metrikleri
- **Accuracy**: 99.37%
- **Precision**: 15.84%
- **Recall**: 62.24%
- **F1-Score**: 25.26%
- **AUC**: 94.88%
- **Specificity**: 99.43%

### PCA Anomaly Detection
- **Explained Variance Ratio**: 56.37%
- **Anomaly Threshold**: 4.77
- **Balanced Accuracy**: 80.84%
- **Matthews Correlation**: 0.31

### Ensemble Model
- **Hibrit Skorlama**: LightGBM + PCA + Business Rules
- **Dinamik Threshold**: İş kurallarına göre ayarlanabilir
- **Confidence Score**: Tahmin güvenilirliği

## 🔧 Konfigürasyon

### Environment Variables

```bash
# .NET API
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=your_connection_string
JWT__SecretKey=your_jwt_secret
Redis__ConnectionString=localhost:6379

# Python ML Services
MODEL_PATH=/app/Models
DATA_PATH=/app/Data
FLASK_ENV=development
```

### Model Konfigürasyonu

```json
{
  "LightGBMConfiguration": {
    "NumLeaves": 31,
    "LearningRate": 0.1,
    "NumIterations": 100,
    "FeatureFraction": 0.8
  },
  "PCAConfiguration": {
    "NComponents": 0.95,
    "AnomalyThreshold": 3.0
  }
}
```

## 🧪 Test

### Unit Tests

```bash
# .NET tests
cd src
dotnet test

# Python tests
cd Python
python -m pytest tests/
```

### Integration Tests

```bash
# API integration tests
cd src/FraudDetection.ApiTester
dotnet test
```

### Performance Tests

```bash
# Load testing
cd src/FraudDetection.ApiTester.ScenarioTests
dotnet test
```

## 📈 Monitoring & Logging

### Application Insights
- Performans metrikleri
- Hata takibi
- Kullanıcı davranış analizi

### Logging
- Structured logging (Serilog)
- Log levels: Debug, Info, Warning, Error
- Centralized log management

### Health Checks
```http
GET /health
GET /api/health
GET http://localhost:5001/health
```

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add amazing feature'`)
4. Branch'inizi push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

### Geliştirme Kuralları

- **Code Style**: .NET için Microsoft conventions
- **Python**: PEP 8 style guide
- **React**: ESLint + Prettier
- **Testing**: Minimum %80 code coverage
- **Documentation**: XML comments (C#), docstrings (Python)