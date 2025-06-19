#!/usr/bin/env python3
"""
Fraud Detection Explainability Demo Script
Sistemi hızlıca test etmek için basit demo
"""

import json
import os
import sys
import uuid
from datetime import datetime
from typing import Dict, List

# Ana modülleri import et
try:
    from python_api_explainer import FraudDetectionAPIClient, ExplainabilityAnalyzer

    print("✅ Ana modüller başarıyla import edildi")
except ImportError as e:
    print(f"❌ Import hatası: {e}")
    print("python_api_explainer.py dosyasının aynı klasörde olduğundan emin olun.")
    sys.exit(1)


def create_demo_transaction() -> Dict:
    """Demo için gerçekçi bir transaction oluştur - Gerçek API format'ında"""
    from datetime import datetime

    return {
        'userId': str(uuid.uuid4()),
        'amount': 2847.91,  # Yüksek miktarlı transaction (risk faktörü)
        'merchantId': 'MERCHANT_SUSPICIOUS_001',
        'type': 0,  # Purchase

        # Location bilgileri
        'location': {
            'latitude': 40.7589,  # New York
            'longitude': -73.9851,
            'country': 'US',
            'city': 'New York'
        },

        # Device bilgileri
        'deviceInfo': {
            'deviceId': 'SUSPICIOUS_DEVICE_001',
            'deviceType': 'Mobile',
            'ipAddress': '192.168.1.100',
            'userAgent': 'Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X)',
            'additionalInfo': {
                'ipChanged': 'true'  # Risk faktörü - IP değişimi
            }
        },

        # Ek veri
        'additionalDataRequest': {
            # Kredi kartı bilgileri
            'cardType': 'Visa',
            'cardBin': '424242',
            'cardLast4': '4242',
            'cardExpiryMonth': 12,
            'cardExpiryYear': 2025,
            'bankName': 'Suspicious Bank',
            'bankCountry': 'US',

            # V-Faktör değerleri
            'vFactors': {
                'V1': -1.3598071336738,  # Unusual pattern
                'V2': -0.0727811733098497,
                'V3': 2.53634673796914,  # High value - risk indicator
                'V4': 1.37815522427443,  # High value - risk indicator
                'V5': -0.338320769942518,
                'V6': 0.462387777762292,
                'V7': 0.239598554061257,
                'V8': 0.0986979012610507,
                'V9': 0.363786969611213,
                'V10': 0.0907941719789316,
                'V11': -0.551599533260813,
                'V12': -0.617800855762348,
                'V13': -0.991389847235408,
                'V14': -0.311169353699879,  # Important feature in fraud detection
                'V15': 1.46817697209427,  # High value
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
            },

            # Risk faktörleri
            'daysSinceFirstTransaction': 1,  # Yeni hesap - risk faktörü
            'transactionVelocity24h': 10,  # Çok işlem - risk faktörü
            'averageTransactionAmount': 50.0,  # Normal ortalaması düşük
            'isNewPaymentMethod': True,  # Yeni ödeme yöntemi - risk faktörü
            'isInternational': False,

            # Özel değerler
            'customValues': {
                'isHighRiskRegion': 'true'
            }
        }
    }


def create_normal_transaction() -> Dict:
    """Normal (fraud olmayan) transaction oluştur - Gerçek API format'ında"""
    from datetime import datetime

    return {
        'userId': str(uuid.uuid4()),
        'amount': 89959.50,  # Düşük miktar
        'merchantId': 'MERCHANT_TRUSTED_001',
        'type': 0,  # Purchase

        # Location bilgileri
        'location': {
            'latitude': 37.7749,  # San Francisco
            'longitude': -122.4194,
            'country': 'US',
            'city': 'San Francisco'
        },

        # Device bilgileri
        'deviceInfo': {
            'deviceId': 'TRUSTED_DEVICE_001',
            'deviceType': 'Desktop',
            'ipAddress': '192.168.1.50',
            'userAgent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
            'additionalInfo': {
                'ipChanged': 'false'  # IP değişimi yok
            }
        },

        # Ek veri
        'additionalDataRequest': {
            # Kredi kartı bilgileri
            'cardType': 'MasterCard',
            'cardBin': '555555',
            'cardLast4': '5555',
            'cardExpiryMonth': 6,
            'cardExpiryYear': 2026,
            'bankName': 'Chase Bank',
            'bankCountry': 'US',

            # V-Faktör değerleri
            'vFactors': {
                'V1': 0.1234567890123,
                'V2': -0.234567890123,
                'V3': 0.345678901234,
                'V4': -0.456789012345,
                'V5': 0.567890123456,
                'V6': -0.678901234567,
                'V7': 0.789012345678,
                'V8': -0.890123456789,
                'V9': 0.012345678901,
                'V10': -0.123456789012,
                'V11': 0.234567890123,
                'V12': -0.345678901234,
                'V13': 0.456789012345,
                'V14': -0.567890123456,
                'V15': 0.678901234567,
                'V16': -0.789012345678,
                'V17': 0.890123456789,
                'V18': -0.901234567890,
                'V19': 0.123456789012,
                'V20': -0.234567890123,
                'V21': 0.345678901234,
                'V22': -0.456789012345,
                'V23': 0.567890123456,
                'V24': -0.678901234567,
                'V25': 0.789012345678,
                'V26': -0.890123456789,
                'V27': 0.901234567890,
                'V28': -0.012345678901
            },

            # Normal faktörler
            'daysSinceFirstTransaction': 365,  # Eski hesap
            'transactionVelocity24h': 1,  # Az işlem
            'averageTransactionAmount': 95.0,  # Benzer ortalama
            'isNewPaymentMethod': False,  # Bilinen ödeme yöntemi
            'isInternational': False,

            # Özel değerler
            'customValues': {
                'isHighRiskRegion': 'false'
            }
        }
    }

def print_banner():
    """Demo banner yazdır"""
    print("=" * 70)
    print("🔍 FRAUD DETECTION EXPLAINABILITY DEMO")
    print("=" * 70)
    print("Bu demo, fraud detection API'nizin kararlarını açıklar")
    print("SHAP ve LIME kullanarak model transparency sağlar")
    print("=" * 70)


def save_html_summary(results: list, output_path: str):
    """Demo sonuçlarını özetleyen bir HTML dosyası oluştur"""
    html = [
        '<!DOCTYPE html>',
        '<html lang="tr">',
        '<head>',
        '<meta charset="utf-8">',
        '<meta name="viewport" content="width=device-width, initial-scale=1">',
        '<title>Fraud Detection Analiz Raporu</title>',
        '<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">',
        '<style>',
        'body { font-family: "Segoe UI", system-ui, -apple-system, sans-serif; background-color: #f8f9fa; }',
        '.container { max-width: 1200px; margin: 2rem auto; }',
        '.card { border: none; border-radius: 15px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); margin-bottom: 2rem; }',
        '.card-header { background-color: #fff; border-bottom: 2px solid #f0f0f0; border-radius: 15px 15px 0 0 !important; padding: 1.5rem; }',
        '.card-body { padding: 1.5rem; }',
        '.transaction-id { font-family: monospace; background: #f8f9fa; padding: 0.5rem; border-radius: 5px; }',
        '.metric-card { background: #fff; border-radius: 10px; padding: 1rem; margin-bottom: 1rem; box-shadow: 0 2px 4px rgba(0,0,0,0.05); }',
        '.metric-value { font-size: 1.5rem; font-weight: bold; margin: 0.5rem 0; }',
        '.metric-label { color: #6c757d; font-size: 0.9rem; }',
        '.fraud { color: #dc3545; }',
        '.normal { color: #198754; }',
        '.risk { font-weight: 600; }',
        '.critical { color: #dc3545; }',
        '.high { color: #fd7e14; }',
        '.medium { color: #ffc107; }',
        '.low { color: #20c997; }',
        '.insight-card { background: #fff; border-left: 4px solid #0d6efd; padding: 1rem; margin: 1rem 0; border-radius: 0 10px 10px 0; }',
        '.chart-container { height: 300px; margin: 1rem 0; }',
        '.progress { height: 0.8rem; }',
        '.progress-bar { transition: width 1s ease-in-out; }',
        '.badge { font-size: 0.9rem; padding: 0.5rem 1rem; }',
        '.summary-stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin: 1rem 0; }',
        '.summary-stat { text-align: center; padding: 1rem; background: #fff; border-radius: 10px; box-shadow: 0 2px 4px rgba(0,0,0,0.05); }',
        '.summary-stat-value { font-size: 1.8rem; font-weight: bold; margin: 0.5rem 0; }',
        '.summary-stat-label { color: #6c757d; font-size: 0.9rem; }',
        '@media print {',
        '  .no-print { display: none; }',
        '  .card { box-shadow: none; border: 1px solid #dee2e6; }',
        '}',
        '</style>',
        '</head>',
        '<body>',
        '<div class="container">',
        '<div class="text-center mb-4">',
        '<h1 class="display-4">Fraud Detection Analiz Raporu</h1>',
        '<p class="lead text-muted">Transaction Analizi ve Risk Değerlendirmesi</p>',
        '<p class="text-muted">Oluşturulma Tarihi: ' + datetime.now().strftime('%d.%m.%Y %H:%M:%S') + '</p>',
        '</div>'
    ]

    # Özet istatistikler
    total_transactions = len(results)
    fraud_count = sum(1 for r in results if float(r.get('api_prediction', {}).get('Probability', 0)) > 0.5)
    avg_probability = sum(float(r.get('api_prediction', {}).get('Probability', 0)) for r in results) / total_transactions if total_transactions > 0 else 0
    
    html.extend([
        '<div class="summary-stats">',
        f'<div class="summary-stat">',
        f'<div class="summary-stat-label">Toplam İşlem</div>',
        f'<div class="summary-stat-value">{total_transactions}</div>',
        '</div>',
        f'<div class="summary-stat">',
        f'<div class="summary-stat-label">Şüpheli İşlem</div>',
        f'<div class="summary-stat-value fraud">{fraud_count}</div>',
        '</div>',
        f'<div class="summary-stat">',
        f'<div class="summary-stat-label">Ortalama Risk</div>',
        f'<div class="summary-stat-value">{avg_probability:.1%}</div>',
        '</div>',
        '</div>'
    ])
    
    for r in results:
        api_pred = r.get('api_prediction', {})
        business = r.get('business_explanation', {})
        
        # API yanıtından değerleri al
        transaction_id = api_pred.get('TransactionId', 'N/A')
        probability = float(api_pred.get('Probability', 0.0))
        score = float(api_pred.get('Score', 0.0))
        anomaly_score = float(api_pred.get('AnomalyScore', 0.0))
        risk_level = str(api_pred.get('RiskLevel', 'MEDIUM'))
        decision = str(api_pred.get('IsFraudulent', 'REVIEW_REQUIRED'))
        
        # Risk level için CSS class
        risk_class = risk_level.lower()
        decision_class = 'fraud' if decision in ['DENY', 'REVIEW_REQUIRED'] else 'normal'
        
        html.extend([
            '<div class="card">',
            '<div class="card-header">',
            f'<h3 class="mb-0">Transaction Analizi</h3>',
            f'<div class="transaction-id mt-2">ID: {transaction_id}</div>',
            '</div>',
            '<div class="card-body">',
            
            # Temel metrikler
            '<div class="row">',
            '<div class="col-md-6">',
            '<div class="metric-card">',
            f'<div class="metric-label">İşlem Tutarı</div>',
            f'<div class="metric-value">${r.get("amount", 0):,.2f}</div>',
            f'<div class="metric-label">Merchant: {r.get("merchantId", "N/A")}</div>',
            '</div>',
            '</div>',
            
            '<div class="col-md-6">',
            '<div class="metric-card">',
            f'<div class="metric-label">Risk Seviyesi</div>',
            f'<div class="metric-value {risk_class}">{risk_level}</div>',
            f'<div class="progress mt-2">',
            f'<div class="progress-bar bg-{risk_class}" role="progressbar" style="width: {probability*100}%" ',
            f'aria-valuenow="{probability*100}" aria-valuemin="0" aria-valuemax="100"></div>',
            '</div>',
            '</div>',
            '</div>',
            '</div>',
            
            # Detaylı metrikler
            '<div class="row mt-4">',
            '<div class="col-md-4">',
            '<div class="metric-card">',
            f'<div class="metric-label">Fraud Olasılığı</div>',
            f'<div class="metric-value {decision_class}">{probability:.1%}</div>',
            '</div>',
            '</div>',
            
            '<div class="col-md-4">',
            '<div class="metric-card">',
            f'<div class="metric-label">Risk Skoru</div>',
            f'<div class="metric-value">{score:.4f}</div>',
            '</div>',
            '</div>',
            
            '<div class="col-md-4">',
            '<div class="metric-card">',
            f'<div class="metric-label">Anomali Skoru</div>',
            f'<div class="metric-value">{anomaly_score:.4f}</div>',
            '</div>',
            '</div>',
            '</div>',
            
            # Karar ve öneriler
            '<div class="mt-4">',
            f'<h4>Karar: <span class="badge bg-{decision_class}">{decision}</span></h4>',
            '</div>'
        ])
        
        # Business açıklaması
        if business.get('summary'):
            summary = business['summary']
            html.extend([
                '<div class="insight-card mt-4">',
                '<h4>İş Analizi</h4>',
                f'<p><strong>Karar:</strong> {summary.get("decision", "N/A")}</p>',
                f'<p><strong>Risk Seviyesi:</strong> {summary.get("risk_level", "N/A")}</p>',
                '</div>'
            ])
        
        # Key insights
        if business.get('key_insights'):
            html.extend([
                '<div class="mt-4">',
                '<h4>Önemli Gözlemler</h4>',
                '<ul class="list-group">'
            ])
            for insight in business['key_insights'][:3]:
                html.append(f'<li class="list-group-item">{insight}</li>')
            html.append('</ul></div>')
        
        # Risk faktörleri
        if business.get('risk_factors'):
            html.extend([
                '<div class="mt-4">',
                '<h4>Risk Faktörleri</h4>',
                '<div class="row">'
            ])
            for factor in business['risk_factors'][:3]:
                risk_class = factor.get('risk_level', 'MEDIUM').lower()
                html.extend([
                    '<div class="col-md-4">',
                    '<div class="metric-card">',
                    f'<div class="metric-label {risk_class}">{factor["factor"]}</div>',
                    f'<div class="metric-value">{factor["value"]}</div>',
                    f'<small class="text-muted">{factor["explanation"]}</small>',
                    '</div>',
                    '</div>'
                ])
            html.append('</div></div>')
        
        # Öneriler
        if business.get('recommendations'):
            html.extend([
                '<div class="mt-4">',
                '<h4>Öneriler</h4>',
                '<div class="list-group">'
            ])
            for rec in business['recommendations'][:3]:
                html.append(f'<div class="list-group-item">{rec}</div>')
            html.append('</div></div>')
        
        html.append('</div></div>')  # card-body ve card kapanışı
    
    # Footer
    html.extend([
        '<div class="text-center mt-4 mb-4 text-muted">',
        '<p>Bu rapor otomatik olarak oluşturulmuştur.</p>',
        '<p>Fraud Detection System v2.0</p>',
        '</div>',
        '</div>',  # container kapanışı
        '<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>',
        '<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>',
        '</body></html>'
    ])
    
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(html))
    print(f"\n📄 HTML özet raporu oluşturuldu: {output_path}")


def run_api_test(api_client: FraudDetectionAPIClient) -> bool:
    """API bağlantısını test et"""
    print("\n🌐 API Bağlantı Testi...")

    # Health check
    if not api_client.health_check():
        print("❌ API'ye bağlanılamıyor!")
        print("Lütfen .NET API'nizin çalıştığından emin olun:")
        print("  - URL: http://localhost:5112")
        print("  - Health endpoint: /health")
        return False

    print("✅ API bağlantısı başarılı!")

    # Test prediction
    print("\n🧪 Test Prediction...")
    test_transaction = create_demo_transaction()

    try:
        result = api_client.predict(test_transaction)
        print(f"Debug - Test Prediction Result: {json.dumps(result, indent=2, default=str)}")

        if "error" in result:
            print(f"❌ Prediction hatası: {result['error']}")
            return False

        print("✅ Test prediction başarılı!")
        print(f"   Transaction ID: {result.get('TransactionId', 'N/A')}")
        print(f"   Amount: ${test_transaction['amount']:,.2f}")
        print(f"   Fraud Probability: {result.get('Probability', 0.0):.4f}")
        print(f"   Risk Score: {result.get('Score', 0.0):.4f}")
        print(f"   Anomaly Score: {result.get('AnomalyScore', 0.0):.4f}")
        print(f"   Risk Level: {result.get('RiskLevel', 'MEDIUM')}")
        print(f"   Decision: {result.get('IsFraudulent', 'REVIEW_REQUIRED')}")

        return True

    except Exception as e:
        print(f"❌ Test prediction hatası: {e}")
        import traceback
        traceback.print_exc()
        return False


def create_sample_transactions(count: int = 1000) -> List[Dict]:
    """Test için örnek işlemler oluştur"""
    import random
    from datetime import datetime, timedelta
    
    transactions = []
    cities = ['İstanbul', 'Ankara', 'İzmir', 'Antalya', 'Bursa', 'Adana', 'Gaziantep', 'Konya']
    merchants = [f'MERCHANT_{i:03d}' for i in range(1, 101)]
    card_types = ['Visa', 'MasterCard', 'Troy', 'Bonus']
    banks = ['Ziraat Bankası', 'İş Bankası', 'Garanti BBVA', 'Akbank', 'Yapı Kredi', 'Halkbank']
    
    for i in range(count):
        # Rastgele tutar (₺10 - ₺50,000 arası)
        amount = random.uniform(10, 100000)
        
        # Rastgele tarih (son 30 gün içinde)
        days_ago = random.randint(0, 30)
        transaction_date = datetime.now() - timedelta(days=days_ago)
        
        # Rastgele konum
        city = random.choice(cities)
        latitude = random.uniform(36.0, 42.0)  # Türkiye enlem aralığı
        longitude = random.uniform(26.0, 45.0)  # Türkiye boylam aralığı
        
        # Rastgele cihaz bilgileri
        device_types = ['Mobile', 'Desktop', 'Tablet']
        device_type = random.choice(device_types)
        
        # V-Faktörleri için rastgele değerler
        v_factors = {}
        for j in range(1, 29):
            v_factors[f'V{j}'] = random.uniform(-2, 2)
        
        # Risk faktörleri
        is_high_risk = random.random() < 0.2  # %20 yüksek risk
        is_new_account = random.random() < 0.3  # %30 yeni hesap
        is_ip_changed = random.random() < 0.15  # %15 IP değişimi
        is_new_payment = random.random() < 0.25  # %25 yeni ödeme yöntemi
        
        transaction = {
            'userId': str(uuid.uuid4()),
            'amount': round(amount, 2),
            'merchantId': random.choice(merchants),
            'type': 0,  # Purchase
            
            # Konum bilgileri
            'location': {
                'latitude': round(latitude, 4),
                'longitude': round(longitude, 4),
                'country': 'TR',
                'city': city
            },
            
            # Cihaz bilgileri
            'deviceInfo': {
                'deviceId': f'DEVICE_{i:06d}',
                'deviceType': device_type,
                'ipAddress': f'192.168.{random.randint(1, 255)}.{random.randint(1, 255)}',
                'userAgent': f'Mozilla/5.0 ({device_type})',
                'additionalInfo': {
                    'ipChanged': str(is_ip_changed).lower()
                }
            },
            
            # Ek veri
            'additionalDataRequest': {
                'cardType': random.choice(card_types),
                'cardBin': f'{random.randint(100000, 999999)}',
                'cardLast4': f'{random.randint(1000, 9999)}',
                'cardExpiryMonth': random.randint(1, 12),
                'cardExpiryYear': random.randint(2024, 2028),
                'bankName': random.choice(banks),
                'bankCountry': 'TR',
                'vFactors': v_factors,
                'daysSinceFirstTransaction': random.randint(1, 365) if not is_new_account else random.randint(1, 7),
                'transactionVelocity24h': random.randint(1, 20),
                'averageTransactionAmount': round(random.uniform(50, 1000), 2),
                'isNewPaymentMethod': is_new_payment,
                'isInternational': random.random() < 0.1,  # %10 uluslararası işlem
                'customValues': {
                    'isHighRiskRegion': str(is_high_risk).lower()
                }
            }
        }
        
        transactions.append(transaction)
    
    return transactions

def run_explanation_demo(analyzer: ExplainabilityAnalyzer):
    """Açıklama demo'sunu çalıştır"""
    print("\n🔬 AÇIKLAMA DEMOSU")
    print("-" * 40)

    # Örnek işlemler oluştur
    print("\n📊 Örnek işlemler oluşturuluyor...")
    transactions = create_sample_transactions(100)
    print(f"✅ {len(transactions)} adet örnek işlem oluşturuldu")

    # İşlemleri analiz et
    demo_results = []
    total = len(transactions)
    
    print("\n🔍 İşlemler analiz ediliyor...")
    for i, transaction in enumerate(transactions, 1):
        if i % 100 == 0:  # Her 100 işlemde bir ilerleme göster
            print(f"İşleniyor: {i}/{total} ({(i/total)*100:.1f}%)")
        
        try:
            # Sadece SHAP kullan (daha hızlı)
            explanation = analyzer.explain_transaction(
                transaction_data=transaction,
                model_type="Ensemble",
                method="shap",
                output_dir=f"demo_results/batch_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
            )

            if 'error' in explanation:
                print(f"   ❌ İşlem {i} açıklama hatası: {explanation['error']}")
                continue

            demo_results.append({**transaction, **explanation})

        except Exception as e:
            print(f"   ❌ İşlem {i} hatası: {e}")
            continue

    print(f"\n✅ Analiz tamamlandı. Başarılı: {len(demo_results)}, Hata: {total - len(demo_results)}")

    # HTML özet raporu oluştur
    save_html_summary(demo_results, "demo_results/summary2.html")


def create_sample_files():
    """Demo için örnek dosyalar oluştur"""
    print("\n📁 Örnek Dosyalar Oluşturuluyor...")

    # Output dizinlerini oluştur
    os.makedirs("demo_results", exist_ok=True)
    os.makedirs("sample_data", exist_ok=True)

    # Tek transaction örneği
    single_transaction = create_demo_transaction()
    with open("sample_data/sample_transaction.json", 'w') as f:
        json.dump(single_transaction, f, indent=2)

    # Batch transactions örneği
    batch_transactions = [
        create_demo_transaction(),
        create_normal_transaction(),
        create_demo_transaction(),
        create_normal_transaction(),
    ]

    # Transaction ID'leri benzersiz yap
    for i, tx in enumerate(batch_transactions):
        tx['transactionId'] = f"BATCH_{i + 1:03d}_{datetime.now().strftime('%Y%m%d_%H%M%S')}"

    with open("sample_data/sample_batch.json", 'w') as f:
        json.dump(batch_transactions, f, indent=2)

    print("✅ Örnek dosyalar oluşturuldu:")
    print("   • sample_data/sample_transaction.json")
    print("   • sample_data/sample_batch.json")


def print_usage_examples():
    """Kullanım örneklerini yazdır"""
    print("\n📚 KULLANIM ÖRNEKLERİ")
    print("-" * 40)
    print("CLI ile tek transaction açıklama:")
    print("  python fraud_explainer_cli.py explain-single -i sample_data/sample_transaction.json")
    print()
    print("CLI ile batch açıklama:")
    print("  python fraud_explainer_cli.py explain-batch -i sample_data/sample_batch.json")
    print()
    print("Python kodunda kullanım:")
    print("  from python_api_explainer import FraudDetectionAPIClient, ExplainabilityAnalyzer")
    print("  client = FraudDetectionAPIClient('http://localhost:5000')")
    print("  analyzer = ExplainabilityAnalyzer(client)")
    print("  explanation = analyzer.explain_transaction(transaction_data)")


def main():
    """Ana demo fonksiyonu"""
    print_banner()

    # API Client oluştur - Kullanıcının portuna göre
    print("🔧 API Client kuruluyor...")
    api_client = FraudDetectionAPIClient("http://localhost:5112", timeout=30)

    # API test
    if not run_api_test(api_client):
        print("\n❌ API testi başarısız!")
        print("Demo devam edemez. Lütfen .NET API'nizi başlatın.")
        return

    # Analyzer oluştur
    print("\n🔧 Explainability Analyzer kuruluyor...")
    analyzer = ExplainabilityAnalyzer(api_client, models_path="models")

    # Explainability demo
    run_explanation_demo(analyzer)

    # Örnek dosyalar oluştur
    create_sample_files()

    # Kullanım örnekleri
    print_usage_examples()

    # Final mesaj
    print("\n" + "=" * 70)
    print("🎉 DEMO TAMAMLANDI!")
    print("=" * 70)
    print("Sistem başarıyla test edildi. Artık kendi transaction'larınızı analiz edebilirsiniz!")
    print("\nÖnemli notlar:")
    print("• Model dosyaları bulunamazsa sadece API sonuçları gösterilir")
    print("• SHAP/LIME analizleri için model dosyalarına ihtiyaç vardır")
    print("• Finansal regülasyonlar için tüm kararlar açıklanabilir")
    print("\nDaha fazla bilgi için fraud_explainer_guide.md dosyasını inceleyin.")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n⏹️ Demo kullanıcı tarafından durduruldu.")
    except Exception as e:
        print(f"\n\n❌ Demo hatası: {e}")
        import traceback

        traceback.print_exc()
        sys.exit(1)