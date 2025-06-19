#!/usr/bin/env python3
"""
API Test Scripti
API'nin doğru çalışıp çalışmadığını ve tahminlerin doğru dönüp dönmediğini kontrol eder
"""

import requests
import json
from datetime import datetime
import uuid

def create_test_transaction():
    """Test için şüpheli bir transaction oluştur (API'nin beklediği formatta)"""
    return {
        'transactionId': str(uuid.uuid4()),
        'userId': str(uuid.uuid4()),
        'amount': 2847.91,
        'timestamp': datetime.now().isoformat(),
        'type': 0,
        'MerchantId': 'MERCHANT_SUSPICIOUS_001',
        'Location': {
            'latitude': 40.7589,
            'longitude': -73.9851,
            'country': 'US',
            'city': 'New York',
            'isHighRiskRegion': True
        },
        'DeviceInfo': {
            'deviceId': 'SUSPICIOUS_DEVICE_001',
            'deviceType': 'Mobile',
            'ipAddress': '192.168.1.100',
            'userAgent': 'Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X)',
            'ipChanged': True
        },
        'AdditionalData': {
            'cardType': 'Visa',
            'cardBin': '424242',
            'cardLast4': '4242',
            'cardExpiryMonth': 12,
            'cardExpiryYear': 2025,
            'bankName': 'Suspicious Bank',
            'bankCountry': 'US',
            'daysSinceFirstTransaction': 1,
            'transactionVelocity24h': 10,
            'averageTransactionAmount': 50.0,
            'isNewPaymentMethod': True,
            'isInternational': False,
            'V1': -1.3598071336738,
            'V2': -0.0727811733098497,
            'V3': 2.53634673796914,
            'V4': 1.37815522427443,
            'V5': -0.338320769942518,
            'V6': 0.462387777762292,
            'V7': 0.239598554061257,
            'V8': 0.0986979012610507,
            'V9': 0.363786969611213,
            'V10': 0.0907941719789316,
            'V11': -0.551599533260813,
            'V12': -0.617800855762348,
            'V13': -0.991389847235408,
            'V14': -0.311169353699879,
            'V15': 1.46817697209427,
            'V16': -0.470400525259478,
            'V17': 0.207971241929242,
            'V18': 0.0257905801985591,
            'V19': 0.403992960255733,
            'V20': 0.251412098239705,
            'V21': -0.018306777944153,
            'V22': 0.277837575558899,
            'V23': -0.110473910188767,
            'V24': 0.0669280749146731,
            'V25': 0.128539358273528,
            'V26': -0.189114843888824,
            'V27': 0.133558376740387,
            'V28': -0.0210530534538215
        }
    }

def test_api_health():
    """API sağlık kontrolü"""
    try:
        response = requests.get("http://localhost:5112/health", timeout=5)
        if response.status_code == 200:
            print("✅ API Health Check başarılı")
            return True
        else:
            print(f"❌ API Health Check başarısız: {response.status_code}")
            print(f"Hata: {response.text}")
            return False
    except Exception as e:
        print(f"❌ API Health Check hatası: {e}")
        return False

def test_api_prediction():
    """API tahmin testi"""
    try:
        # Test transaction oluştur
        transaction = create_test_transaction()
        
        # API'ye gönder
        print("\n🧪 Test Prediction yapılıyor...")
        print(f"Transaction ID: {transaction['transactionId']}")
        print(f"Amount: ${transaction['amount']:,.2f}")
        
        response = requests.post(
            "http://localhost:5112/api/model/predict",
            json=transaction,
            timeout=10
        )
        
        if response.status_code == 200:
            result = response.json()
            print("\n✅ API Prediction başarılı!")
            print(f"Status Code: {response.status_code}")
            print(f"Response Headers: {dict(response.headers)}")
            print(f"Raw Response: {response.text}")
            
            # Tahmin sonuçlarını kontrol et
            probability = float(result.get('Probability', result.get('probability', 0)))
            is_fraud = result.get('IsFraudulent', result.get('isFraudulent', False))
            
            print(f"\n📊 Tahmin Sonuçları:")
            print(f"Fraud Probability: {probability:.4f}")
            print(f"Is Fraudulent: {is_fraud}")
            
            # Sonuçları değerlendir
            if probability == 0:
                print("\n⚠️ UYARI: Probability değeri 0 dönüyor!")
                print("Bu durum şunlardan kaynaklanabilir:")
                print("1. API'deki model doğru yüklenmemiş olabilir")
                print("2. API'deki model eğitilmemiş olabilir")
                print("3. API'deki model hatalı olabilir")
                print("4. API'deki model sıfırlanmış olabilir")
            
            return True
        else:
            print(f"❌ API Prediction hatası: {response.status_code}")
            print(f"Hata: {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ API Prediction test hatası: {e}")
        return False

def main():
    """Ana test fonksiyonu"""
    print("=== API TEST BAŞLIYOR ===")
    
    # Health check
    if not test_api_health():
        print("\n❌ API sağlık kontrolü başarısız!")
        print("API'nin çalıştığından emin olun: http://localhost:5112")
        return
    
    # Prediction test
    if not test_api_prediction():
        print("\n❌ API prediction testi başarısız!")
        print("API'nin doğru çalıştığından ve modelin yüklü olduğundan emin olun")
        return
    
    print("\n=== API TEST TAMAMLANDI ===")

if __name__ == "__main__":
    main() 