#!/usr/bin/env python3
"""
Fraud Detection API Explainability Client
Mevcut .NET API'ye istek atarak SHAP/LIME analizi yapan client
"""

import requests
import json
import os
import numpy as np
import pandas as pd
import joblib
import warnings
from datetime import datetime
from typing import Dict, Any, List, Optional, Union
import time
import matplotlib
import uuid
from enum import Enum


matplotlib.use('Agg')  # Non-interactive backend
import matplotlib.pyplot as plt
import seaborn as sns

# Explainability libraries
import shap
import lime
from lime.lime_tabular import LimeTabularExplainer

# Sklearn utilities
from sklearn.preprocessing import StandardScaler

# Warnings'leri filtrele
warnings.filterwarnings('ignore', category=UserWarning)
warnings.filterwarnings('ignore', category=FutureWarning)


class DecisionType(Enum):
    APPROVE = "Onayla"
    DENY = "Reddet"
    REVIEW_REQUIRED = "İncelemeGerekli"
    ESCALATE_TO_MANAGER = "YöneticiyeYönlendir"
    REQUIRE_ADDITIONAL_VERIFICATION = "EkDoğrulamaGerekli"

class RiskLevel(Enum):
    LOW = "Düşük"
    MEDIUM = "Orta"
    HIGH = "Yüksek"
    CRITICAL = "Kritik"

class FraudDetectionAPIClient:
    """
    Fraud Detection API'sine istek atan client sınıfı
    """

    def __init__(self, base_url: str = "http://localhost:5112", timeout: int = 300):
        """
        API Client'ı başlat

        Args:
            base_url: API'nin base URL'i
            timeout: İstek timeout süresi (saniye)
        """
        self.base_url = base_url.rstrip('/')
        self.timeout = timeout
        self.session = requests.Session()

        # Default headers
        self.session.headers.update({
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        })

        print(f"API Client başlatıldı: {self.base_url}")

    def _format_api_response(self, response: Dict) -> Dict:
        """API yanıtını formatla"""
        try:
            print(f"Debug - Raw API Response: {json.dumps(response, indent=2)}")
            
            # TransactionId
            transaction_id = response.get('transactionId')
            
            # Decision enum'ını dönüştür
            decision_str = response.get('isFraudulent')
            if decision_str is None:
                print("Debug - Decision is None, using default REVIEW_REQUIRED")
                decision = DecisionType.REVIEW_REQUIRED
            else:
                try:
                    # API'den gelen string değeri enum'a dönüştür
                    decision = DecisionType(decision_str)
                    print(f"Debug - Decision converted to enum: {decision}")
                except ValueError:
                    print(f"Debug - Invalid decision value: {decision_str}, using REVIEW_REQUIRED")
                    decision = DecisionType.REVIEW_REQUIRED

            # Risk level enum'ını dönüştür
            risk_level_str = response.get('riskLevel')
            if risk_level_str is None:
                print("Debug - RiskLevel is None, using default MEDIUM")
                risk_level = RiskLevel.MEDIUM
            else:
                try:
                    # API'den gelen string değeri enum'a dönüştür
                    risk_level = RiskLevel(risk_level_str)
                    print(f"Debug - RiskLevel converted to enum: {risk_level}")
                except ValueError:
                    print(f"Debug - Invalid risk level value: {risk_level_str}, using MEDIUM")
                    risk_level = RiskLevel.MEDIUM

            # Score değerini al
            score_obj = response.get('score', {})
            if isinstance(score_obj, dict):
                score = float(score_obj.get('score', 0.0))
            else:
                score = float(score_obj)

            # Sayısal değerleri dönüştür
            try:
                probability = float(response.get('probability', 0.0))
                anomaly_score = float(response.get('anomalyScore', 0.0))
                print(f"Debug - Numeric values: Probability={probability}, Score={score}, AnomalyScore={anomaly_score}")
            except (ValueError, TypeError) as e:
                print(f"Debug - Error converting numeric values: {e}")
                probability = 0.0
                score = 0.0
                anomaly_score = 0.0

            formatted_response = {
                'TransactionId': transaction_id,
                'IsFraudulent': decision,
                'Probability': probability,
                'Score': score,
                'AnomalyScore': anomaly_score,
                'RiskLevel': risk_level
            }
            
            print(f"Debug - Formatted Response: {json.dumps(formatted_response, indent=2, default=str)}")
            return formatted_response
            
        except Exception as e:
            print(f"Response format hatası: {e}")
            import traceback
            traceback.print_exc()
            return response

    def _make_request(self, method: str, endpoint: str, data: Dict = None, params: Dict = None) -> Dict:
        """
        API'ye istek gönder

        Args:
            method: HTTP method (GET, POST, etc.)
            endpoint: API endpoint
            data: POST data
            params: Query parameters

        Returns:
            API response dict
        """
        url = f"{self.base_url}/api/model{endpoint}"

        try:
            print(f"🌐 {method} isteği gönderiliyor: {url}")
            if data:
                print(f"Debug - Request Data: {json.dumps(data, indent=2)}")

            if method.upper() == 'GET':
                response = self.session.get(url, params=params, timeout=self.timeout)
            elif method.upper() == 'POST':
                response = self.session.post(url, json=data, params=params, timeout=self.timeout)
            else:
                raise ValueError(f"Desteklenmeyen HTTP method: {method}")

            # Response kontrolü
            if response.status_code == 200:
                result = response.json()
                print(f"✅ İstek başarılı: {response.status_code}")
                print(f"Debug - Raw Response: {json.dumps(result, indent=2)}")
                return self._format_api_response(result)
            else:
                print(f"❌ İstek başarısız: {response.status_code}")
                print(f"Hata: {response.text}")
                return {"error": response.text, "status_code": response.status_code}

        except requests.exceptions.Timeout:
            print(f"⏰ İstek timeout oldu ({self.timeout}s)")
            return {"error": "Request timeout", "status_code": 408}
        except requests.exceptions.ConnectionError:
            print(f"🔌 Bağlantı hatası: {url}")
            return {"error": "Connection error", "status_code": 503}
        except Exception as e:
            print(f"🚨 Beklenmeyen hata: {str(e)}")
            import traceback
            traceback.print_exc()
            return {"error": str(e), "status_code": 500}

    # Tahmin metodları
    def predict(self, transaction_data: Dict) -> Dict:
        """Genel tahmin yap"""
        # .NET API format'ına uygun transaction objesi oluştur
        formatted_transaction = self._format_transaction_for_api(transaction_data)
        return self._make_request("POST", "/predict", data=formatted_transaction)

    def predict_with_model(self, model_type: str, transaction_data: Dict) -> Dict:
        """Belirli bir modelle tahmin yap"""
        formatted_transaction = self._format_transaction_for_api(transaction_data)
        return self._make_request("POST", f"/{model_type}/predict", data=formatted_transaction)

    def _format_transaction_for_api(self, transaction_data: Dict) -> Dict:
        """Transaction data'yı gerçek .NET API format'ına çevir"""
        # GUID'ları oluştur
        transaction_id = transaction_data.get('transactionId', str(uuid.uuid4()))
        if isinstance(transaction_id, str) and not self._is_valid_guid(transaction_id):
            transaction_id = str(uuid.uuid5(uuid.NAMESPACE_OID, transaction_id))

        user_id = transaction_data.get('userId', str(uuid.uuid4()))
        if isinstance(user_id, str) and not self._is_valid_guid(user_id):
            user_id = str(uuid.uuid5(uuid.NAMESPACE_OID, user_id))

        # V-Factors dictionary oluştur
        v_factors = {}
        for i in range(1, 29):
            v_key = f'V{i}'
            v_value = transaction_data.get(f'v{i}', transaction_data.get(f'V{i}', 0.0))
            v_factors[v_key] = float(v_value)

        # Gerçek .NET TransactionData format'ı
        formatted = {
            "userId": user_id,
            "amount": float(transaction_data.get('amount', 100.0)),
            "merchantId": transaction_data.get('merchantId', 'MERCHANT_001'),
            "type": transaction_data.get('type', 0),  # Purchase = 0
            "location": {
                "latitude": float(transaction_data.get('latitude', 40.7128)),
                "longitude": float(transaction_data.get('longitude', -74.0060)),
                "country": transaction_data.get('country', 'US'),
                "city": transaction_data.get('city', 'New York')
            },
            "deviceInfo": {
                "deviceId": transaction_data.get('deviceId', 'DEVICE_001'),
                "deviceType": transaction_data.get('deviceType', 'Mobile'),
                "ipAddress": transaction_data.get('ipAddress', '192.168.1.1'),
                "userAgent": transaction_data.get('userAgent', 'Mozilla/5.0'),
                "additionalInfo": {
                    "ipChanged": str(transaction_data.get('ipChanged', 'false')).lower()
                }
            },
            "additionalDataRequest": {
                "cardType": transaction_data.get('cardType', 'Visa'),
                "cardBin": transaction_data.get('cardBin', '424242'),
                "cardLast4": transaction_data.get('cardLast4', '4242'),
                "cardExpiryMonth": transaction_data.get('cardExpiryMonth', 12),
                "cardExpiryYear": transaction_data.get('cardExpiryYear', 2025),
                "bankName": transaction_data.get('bankName', 'Bank of America'),
                "bankCountry": transaction_data.get('bankCountry', 'US'),
                "vFactors": v_factors,
                "daysSinceFirstTransaction": transaction_data.get('daysSinceFirstTransaction', 30),
                "transactionVelocity24h": transaction_data.get('transactionVelocity24h', 1),
                "averageTransactionAmount": float(transaction_data.get('averageTransactionAmount', 150.0)),
                "isNewPaymentMethod": transaction_data.get('isNewPaymentMethod', False),
                "isInternational": transaction_data.get('isInternational', False),
                "customValues": {
                    "isHighRiskRegion": str(transaction_data.get('isHighRiskRegion', 'false')).lower()
                }
            }
        }

        return formatted

    def _is_valid_guid(self, guid_string: str) -> bool:
        """GUID formatının geçerli olup olmadığını kontrol et"""
        try:
            uuid.UUID(guid_string)
            return True
        except ValueError:
            return False

    # Model bilgileri
    def get_model_metrics(self, model_name: str) -> Dict:
        """Model metriklerini al"""
        return self._make_request("GET", f"/{model_name}/metrics")

    def get_performance_summary(self, model_name: str) -> Dict:
        """Model performans özetini al"""
        return self._make_request("GET", f"/{model_name}/performance-summary")

    def compare_models(self, model_names: List[str]) -> Dict:
        """Modelleri karşılaştır"""
        return self._make_request("POST", "/compare", data=model_names)

    # Health check
    def health_check(self) -> bool:
        """API'nin sağlıklı olup olmadığını kontrol et"""
        try:
            # Basit bir GET isteği gönder - önce /health'i dene, yoksa ana endpoint'i
            try:
                response = self.session.get(f"{self.base_url}/health", timeout=5)
                return response.status_code == 200
            except:
                # Health endpoint yoksa ana API endpoint'ini test et
                response = self.session.get(f"{self.base_url}/api/model", timeout=5)
                # 404 bile olsa API çalışıyor demektir
                return response.status_code in [200, 404]
        except:
            return False


class ExplainabilityAnalyzer:
    """
    API'den alınan tahminleri açıklayan sınıf
    SHAP ve LIME analizi yaparak model kararlarını açıklar
    """

    def __init__(self, api_client: FraudDetectionAPIClient, models_path: str = "models"):
        """
        Args:
            api_client: API client instance
            models_path: Modellerin bulunduğu dizin
        """
        self.api_client = api_client
        self.models_path = models_path
        self.loaded_models = {}
        self.feature_names = self._get_standard_features()

        # Explainer'ları sakla
        self.shap_explainers = {}
        self.lime_explainers = {}

        print(f"Explainability Analyzer başlatıldı. Models path: {models_path}")

    def _get_standard_features(self) -> List[str]:
        """Standart feature listesini döndür"""
        return [
            'Amount', 'AmountLog', 'TimeSin', 'TimeCos', 'DayOfWeek', 'HourOfDay',
            'V1', 'V2', 'V3', 'V4', 'V5', 'V6', 'V7', 'V8', 'V9', 'V10',
            'V11', 'V12', 'V13', 'V14', 'V15', 'V16', 'V17', 'V18', 'V19', 'V20',
            'V21', 'V22', 'V23', 'V24', 'V25', 'V26', 'V27', 'V28'
        ]

    def load_model(self, model_name: str, model_type: str = "ensemble") -> bool:
        """
        Modeli yükle (explainability için)

        Args:
            model_name: Model adı
            model_type: Model tipi (ensemble, lightgbm, pca)

        Returns:
            Yükleme başarılı mı?
        """
        try:
            print(f"Model yükleniyor: {model_name} (tip: {model_type})")

            # Model dosyasını bul
            model_files = []
            for root, dirs, files in os.walk(self.models_path):
                for file in files:
                    if model_name.lower() in file.lower() and file.endswith('.joblib'):
                        model_files.append(os.path.join(root, file))

            if not model_files:
                print(f"❌ Model dosyası bulunamadı: {model_name}")
                return False

            # En son modeli seç
            model_file = sorted(model_files)[-1]
            print(f"Model dosyası: {model_file}")

            # Modeli yükle
            model = joblib.load(model_file)
            self.loaded_models[model_name] = {
                'model': model,
                'type': model_type,
                'path': model_file
            }

            print(f"✅ Model yüklendi: {model_name}")
            return True

        except Exception as e:
            print(f"❌ Model yükleme hatası: {e}")
            return False

    def setup_explainers(self, model_name: str, background_data: Optional[np.ndarray] = None):
        """
        SHAP ve LIME explainer'ları kur

        Args:
            model_name: Model adı
            background_data: SHAP için background data (opsiyonel)
        """
        try:
            print(f"Explainer'lar kuruluyor: {model_name}")

            if model_name not in self.loaded_models:
                print(f"❌ Model yüklü değil: {model_name}")
                return

            model_info = self.loaded_models[model_name]
            model = model_info['model']
            model_type = model_info['type']

            # Background data oluştur
            if background_data is None:
                background_data = self._create_background_data()

            # SHAP Explainer
            if model_type == 'lightgbm':
                self.shap_explainers[model_name] = shap.TreeExplainer(model)
                print("✅ SHAP TreeExplainer kuruldu")

            elif model_type == 'ensemble':
                # Ensemble için custom wrapper
                def ensemble_predict_proba(X):
                    return self._ensemble_predict_wrapper(X, model)

                self.shap_explainers[model_name] = shap.Explainer(
                    ensemble_predict_proba, background_data
                )
                print("✅ SHAP Explainer (Ensemble) kuruldu")

            elif model_type == 'pca':
                # PCA için wrapper
                def pca_predict_proba(X):
                    return self._pca_predict_wrapper(X, model)

                self.shap_explainers[model_name] = shap.Explainer(
                    pca_predict_proba, background_data
                )
                print("✅ SHAP Explainer (PCA) kuruldu")

            # LIME Explainer
            categorical_features = []
            if 'DayOfWeek' in self.feature_names:
                categorical_features.append(self.feature_names.index('DayOfWeek'))
            if 'HourOfDay' in self.feature_names:
                categorical_features.append(self.feature_names.index('HourOfDay'))

            self.lime_explainers[model_name] = LimeTabularExplainer(
                background_data,
                feature_names=self.feature_names,
                class_names=['Normal', 'Fraud'],
                categorical_features=categorical_features,
                mode='classification',
                discretize_continuous=True
            )
            print("✅ LIME Explainer kuruldu")

        except Exception as e:
            print(f"❌ Explainer kurulum hatası: {e}")

    def _create_background_data(self, n_samples: int = 100) -> np.ndarray:
        """Sentetik background data oluştur"""
        np.random.seed(42)
        n_features = len(self.feature_names)

        # Gerçekçi fraud detection data'sına benzer dağılım
        background = np.random.normal(0, 1, (n_samples, n_features))

        # Amount için pozitif değerler
        if 'Amount' in self.feature_names:
            amount_idx = self.feature_names.index('Amount')
            background[:, amount_idx] = np.abs(np.random.lognormal(2, 1, n_samples))

        # Time-based feature'lar için gerçekçi değerler
        for feature in ['TimeSin', 'TimeCos']:
            if feature in self.feature_names:
                idx = self.feature_names.index(feature)
                background[:, idx] = np.random.uniform(-1, 1, n_samples)

        if 'DayOfWeek' in self.feature_names:
            idx = self.feature_names.index('DayOfWeek')
            background[:, idx] = np.random.randint(0, 7, n_samples)

        if 'HourOfDay' in self.feature_names:
            idx = self.feature_names.index('HourOfDay')
            background[:, idx] = np.random.randint(0, 24, n_samples)

        return background

    def _ensemble_predict_wrapper(self, X: np.ndarray, model: dict) -> np.ndarray:
        """Ensemble model için prediction wrapper"""
        try:
            if isinstance(X, np.ndarray):
                X_df = pd.DataFrame(X, columns=self.feature_names)
            else:
                X_df = X

            # LightGBM prediction
            lightgbm_model = model['lightgbm_model']
            lightgbm_proba = lightgbm_model.predict_proba(X_df)[:, 1]

            # PCA prediction
            pca_model = model['pca_model']
            pca_scaler = model['pca_scaler']
            pca_threshold = model['pca_threshold']

            X_scaled = pca_scaler.transform(X_df)
            X_pca = pca_model.transform(X_scaled)
            X_reconstructed = pca_model.inverse_transform(X_pca)
            errors = np.mean(np.square(X_scaled - X_reconstructed), axis=1)
            pca_proba = 1 / (1 + np.exp(-(errors / pca_threshold) + 2))

            # Ensemble
            lightgbm_weight = model.get('lightgbm_weight', 0.7)
            pca_weight = model.get('pca_weight', 0.3)
            ensemble_proba = lightgbm_weight * lightgbm_proba + pca_weight * pca_proba

            return np.array([1 - ensemble_proba, ensemble_proba]).T

        except Exception as e:
            print(f"Ensemble prediction wrapper error: {e}")
            # Fallback
            prob = np.full(len(X_df), 0.3)
            return np.array([1 - prob, prob]).T

    def _pca_predict_wrapper(self, X: np.ndarray, model: dict) -> np.ndarray:
        """PCA model için prediction wrapper"""
        try:
            if isinstance(X, np.ndarray):
                X_df = pd.DataFrame(X, columns=self.feature_names)
            else:
                X_df = X

            pca_model = model.get('pca_model', model)
            scaler = model.get('pca_scaler')
            threshold = model.get('pca_threshold', 0.1)

            X_scaled = scaler.transform(X_df) if scaler else X_df.values
            X_pca = pca_model.transform(X_scaled)
            X_reconstructed = pca_model.inverse_transform(X_pca)
            errors = np.mean(np.square(X_scaled - X_reconstructed), axis=1)
            proba = 1 / (1 + np.exp(-(errors / threshold) + 2))

            return np.array([1 - proba, proba]).T

        except Exception as e:
            print(f"PCA prediction wrapper error: {e}")
            prob = np.full(len(X_df), 0.3)
            return np.array([1 - prob, prob]).T

    def explain_transaction(self,
                            transaction_data: Dict,
                            model_type: str = "Ensemble",
                            method: str = "both",
                            output_dir: str = "explanations") -> Dict:
        """
        Transaction'ı açıkla (API + Local Analysis)

        Args:
            transaction_data: Transaction verileri
            model_type: Model tipi (Ensemble, LightGBM, PCA)
            method: Açıklama yöntemi (shap, lime, both)
            output_dir: Çıktı dizini

        Returns:
            Açıklama sonuçları
        """
        try:
            print(f"=== TRANSACTION EXPLANATION START ===")
            print(f"Transaction ID: {transaction_data.get('transactionId', 'N/A')}")
            print(f"Model Type: {model_type}")
            print(f"Method: {method}")

            # API'den tahmin al
            print("🌐 API'den tahmin alınıyor...")
            if model_type.lower() == "ensemble":
                api_prediction = self.api_client.predict(transaction_data)
            else:
                api_prediction = self.api_client.predict_with_model(model_type, transaction_data)

            if "error" in api_prediction:
                raise Exception(f"API prediction error: {api_prediction['error']}")

            probability = float(api_prediction.get('Probability', '0.0'))
            print(f"✅ API Prediction: {probability:.4f}")

            # Transaction data'yı feature'lara çevir
            features_df = self._prepare_features(transaction_data)

            # Modeli yükle (eğer yüklü değilse)
            model_name = f"fraud_model_{model_type.lower()}"
            if model_name not in self.loaded_models:
                print("📁 Model yükleniyor...")
                if not self.load_model(model_name, model_type.lower()):
                    print("⚠️ Model yüklenemedi, sadece API sonucu dönülüyor")
                    return self._create_api_only_response(api_prediction, transaction_data)

            # Explainer'ları kur (eğer kurulu değilse)
            if model_name not in self.shap_explainers:
                self.setup_explainers(model_name)

            # Output directory oluştur
            os.makedirs(output_dir, exist_ok=True)

            # Açıklama sonuçları
            explanation_results = {
                'timestamp': datetime.now().isoformat(),
                'transaction_id': transaction_data.get('transactionId', 'unknown'),
                'model_type': model_type,
                'api_prediction': api_prediction,
                'explanations': {}
            }

            # SHAP açıklaması
            if method in ['shap', 'both'] and model_name in self.shap_explainers:
                print("🔍 SHAP analizi yapılıyor...")
                shap_results = self._generate_shap_explanation(
                    model_name, features_df, output_dir
                )
                explanation_results['explanations']['shap'] = shap_results

            # LIME açıklaması
            if method in ['lime', 'both'] and model_name in self.lime_explainers:
                print("🔍 LIME analizi yapılıyor...")
                lime_results = self._generate_lime_explanation(
                    model_name, features_df, output_dir
                )
                explanation_results['explanations']['lime'] = lime_results

            # Business açıklaması
            business_explanation = self._generate_business_explanation(
                api_prediction, explanation_results['explanations'], transaction_data
            )
            explanation_results['business_explanation'] = business_explanation

            # Sonuçları kaydet
            result_file = os.path.join(output_dir, f"explanation_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json")
            with open(result_file, 'w', encoding='utf-8') as f:
                json.dump(explanation_results, f, indent=2, ensure_ascii=False, default=str)

            print(f"✅ Açıklama tamamlandı. Sonuç: {result_file}")
            return explanation_results

        except Exception as e:
            print(f"❌ Explanation error: {e}")
            import traceback
            traceback.print_exc()

            return {
                'error': str(e),
                'timestamp': datetime.now().isoformat(),
                'transaction_id': transaction_data.get('transactionId', 'unknown'),
                'api_prediction': api_prediction if 'api_prediction' in locals() else None
            }

    def _prepare_features(self, transaction_data: Dict) -> pd.DataFrame:
        """Transaction data'yı model feature'larına çevir - Gerçek API format'ından"""
        # Transaction data'dan feature'ları çıkar
        features = {}

        # Temel feature'lar
        features['Amount'] = transaction_data.get('amount', 0.0)

        # Time feature'ı timestamp'den çıkar
        timestamp = transaction_data.get('timestamp')
        if timestamp:
            try:
                from datetime import datetime
                dt = datetime.fromisoformat(timestamp.replace('Z', '+00:00'))
                # Gün içindeki saniye
                features['Time'] = dt.hour * 3600 + dt.minute * 60 + dt.second
            except:
                features['Time'] = 43200.0  # Default 12:00
        else:
            features['Time'] = 43200.0

        # V feature'ları - AdditionalData'dan VFactors'dan al
        v_factors = {}

        # Direkt V feature'lar varsa al
        for i in range(1, 29):
            v_key = f'V{i}'
            if v_key in transaction_data:
                v_factors[v_key] = transaction_data[v_key]
            elif f'v{i}' in transaction_data:
                v_factors[v_key] = transaction_data[f'v{i}']

        # Eğer V feature'lar yoksa sıfır ile doldur
        for i in range(1, 29):
            v_key = f'V{i}'
            if v_key not in v_factors:
                v_factors[v_key] = 0.0
            features[v_key] = v_factors[v_key]

        # Engineered features
        features['AmountLog'] = np.log1p(features['Amount'])

        # Time-based features
        seconds_in_day = 24 * 60 * 60
        features['TimeSin'] = np.sin(2 * np.pi * features['Time'] / seconds_in_day)
        features['TimeCos'] = np.cos(2 * np.pi * features['Time'] / seconds_in_day)
        features['DayOfWeek'] = int((features['Time'] / seconds_in_day) % 7)
        features['HourOfDay'] = int((features['Time'] / 3600) % 24)

        # DataFrame'e çevir
        features_df = pd.DataFrame([features])

        # Sadece gerekli feature'ları seç
        missing_features = set(self.feature_names) - set(features_df.columns)
        for feature in missing_features:
            features_df[feature] = 0.0

        return features_df[self.feature_names]

    def _generate_shap_explanation(self, model_name: str, features_df: pd.DataFrame, output_dir: str) -> Dict:
        """SHAP açıklaması oluştur"""
        try:
            explainer = self.shap_explainers[model_name]
            shap_values = explainer(features_df.values)

            # SHAP values extract
            if hasattr(shap_values, 'values'):
                values = shap_values.values[0] if len(shap_values.values.shape) > 2 else shap_values.values[0]
                base_value = shap_values.base_values[0] if hasattr(shap_values, 'base_values') else 0
            else:
                values = shap_values[0] if isinstance(shap_values, list) else shap_values[0]
                base_value = getattr(explainer, 'expected_value', 0)
                if isinstance(base_value, np.ndarray):
                    base_value = base_value[1] if len(base_value) > 1 else base_value[0]

            # Feature importance
            feature_importance = list(zip(self.feature_names, values))
            feature_importance.sort(key=lambda x: abs(x[1]), reverse=True)

            # Ana SHAP görselleştirmesi
            plt.figure(figsize=(15, 10))
            plt.style.use('seaborn')
            
            # Bar plot
            plt.subplot(2, 1, 1)
            top_features = feature_importance[:15]
            feature_names_plot = [f[0] for f in top_features]
            feature_values_plot = [f[1] for f in top_features]
            
            colors = ['#dc3545' if v > 0 else '#198754' for v in feature_values_plot]
            bars = plt.barh(range(len(feature_names_plot)), feature_values_plot, color=colors, alpha=0.7)
            
            # Bar etiketleri
            for i, bar in enumerate(bars):
                width = bar.get_width()
                plt.text(width if width > 0 else 0, 
                        bar.get_y() + bar.get_height()/2,
                        f'{width:.4f}',
                        ha='left' if width > 0 else 'right',
                        va='center',
                        color='white',
                        fontweight='bold',
                        padding=5)
            
            plt.yticks(range(len(feature_names_plot)), feature_names_plot)
            plt.xlabel('SHAP Değeri (Fraud Olasılığına Etkisi)')
            plt.title('SHAP Feature Önem Sıralaması - Fraud Detection', pad=20)
            plt.axvline(x=0, color='black', linestyle='-', alpha=0.3)
            
            # Waterfall plot
            plt.subplot(2, 1, 2)
            shap.initjs()
            shap.force_plot(base_value, values, features_df.iloc[0],
                          matplotlib=True, show=False, figsize=(15, 3))
            plt.title('SHAP Waterfall Plot - Feature Etkileri', pad=20)
            
            plt.tight_layout()
            shap_plot_path = os.path.join(output_dir, 'shap_explanation.png')
            plt.savefig(shap_plot_path, dpi=300, bbox_inches='tight', facecolor='white')
            plt.close()

            # HTML raporu için ek görselleştirmeler
            html_path = os.path.join(output_dir, 'shap_explanation.html')
            with open(html_path, 'w', encoding='utf-8') as f:
                f.write(f'''
                <!DOCTYPE html>
                <html lang="tr">
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>SHAP Analiz Raporu</title>
                    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
                    <script src="https://cdn.plot.ly/plotly-latest.min.js"></script>
                    <style>
                        body {{ font-family: "Segoe UI", system-ui, -apple-system, sans-serif; }}
                        .container {{ max-width: 1200px; margin: 2rem auto; }}
                        .card {{ border: none; border-radius: 15px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); margin-bottom: 2rem; }}
                        .feature-card {{ background: #fff; border-radius: 10px; padding: 1rem; margin: 0.5rem; }}
                        .positive {{ color: #dc3545; }}
                        .negative {{ color: #198754; }}
                    </style>
                </head>
                <body>
                    <div class="container">
                        <h1 class="text-center mb-4">SHAP Analiz Raporu</h1>
                        
                        <div class="card">
                            <div class="card-body">
                                <h3>Feature Önem Sıralaması</h3>
                                <div id="feature-importance-plot"></div>
                            </div>
                        </div>
                        
                        <div class="card">
                            <div class="card-body">
                                <h3>Detaylı Feature Analizi</h3>
                                <div class="row">
                ''')
                
                # Feature kartları
                for name, value in feature_importance[:10]:
                    impact = "Pozitif" if value > 0 else "Negatif"
                    color_class = "positive" if value > 0 else "negative"
                    f.write(f'''
                        <div class="col-md-6">
                            <div class="feature-card">
                                <h4>{name}</h4>
                                <p class="{color_class}">
                                    <strong>Etki:</strong> {impact}<br>
                                    <strong>SHAP Değeri:</strong> {value:.4f}<br>
                                    <strong>Gerçek Değer:</strong> {features_df[name].iloc[0]:.4f}
                                </p>
                            </div>
                        </div>
                    ''')
                
                f.write('''
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <script>
                        // Feature importance plot
                        var trace = {{
                            x: ''' + str([abs(v) for _, v in feature_importance[:15]]) + ''',
                            y: ''' + str([f for f, _ in feature_importance[:15]]) + ''',
                            type: 'bar',
                            orientation: 'h',
                            marker: {{
                                color: ''' + str(['#dc3545' if v > 0 else '#198754' for _, v in feature_importance[:15]]) + '''
                            }}
                        }};
                        
                        var layout = {{
                            title: 'Feature Önem Sıralaması',
                            xaxis: {{title: 'Mutlak SHAP Değeri'}},
                            yaxis: {{title: 'Feature'}},
                            height: 600
                        }};
                        
                        Plotly.newPlot('feature-importance-plot', [trace], layout);
                    </script>
                </body>
                </html>
                ''')

            return {
                'base_value': float(base_value),
                'prediction_value': float(base_value + np.sum(values)),
                'feature_contributions': [
                    {
                        'feature': name,
                        'value': float(features_df[name].iloc[0]),
                        'shap_value': float(shap_val),
                        'contribution': 'positive' if shap_val > 0 else 'negative',
                        'abs_importance': float(abs(shap_val))
                    }
                    for name, shap_val in feature_importance
                ],
                'top_positive_features': [
                    {'feature': name, 'shap_value': float(val)}
                    for name, val in feature_importance if val > 0
                ][:5],
                'top_negative_features': [
                    {'feature': name, 'shap_value': float(val)}
                    for name, val in feature_importance if val < 0
                ][:5],
                'visualization_path': shap_plot_path,
                'html_report_path': html_path
            }

        except Exception as e:
            print(f"SHAP explanation error: {e}")
            return {'error': str(e)}

    def _generate_lime_explanation(self, model_name: str, features_df: pd.DataFrame, output_dir: str) -> Dict:
        """LIME açıklaması oluştur"""
        try:
            lime_explainer = self.lime_explainers[model_name]
            model_info = self.loaded_models[model_name]

            # Prediction function
            def predict_fn(X):
                X_df = pd.DataFrame(X, columns=self.feature_names)
                if model_info['type'] == 'ensemble':
                    return self._ensemble_predict_wrapper(X_df.values, model_info['model'])
                elif model_info['type'] == 'lightgbm':
                    return model_info['model'].predict_proba(X_df)
                elif model_info['type'] == 'pca':
                    return self._pca_predict_wrapper(X_df.values, model_info['model'])
                else:
                    prob = np.full(len(X_df), 0.3)
                    return np.array([1 - prob, prob]).T

            # LIME explanation
            instance = features_df.iloc[0].values
            lime_exp = lime_explainer.explain_instance(
                instance,
                predict_fn,
                num_features=15,
                num_samples=1000
            )

            # Results extract
            lime_features = lime_exp.as_list()

            # Ana görselleştirme
            plt.figure(figsize=(15, 10))
            plt.style.use('seaborn')
            
            # Bar plot
            plt.subplot(2, 1, 1)
            feature_names = [f[0] for f in lime_features]
            feature_weights = [f[1] for f in lime_features]
            
            colors = ['#dc3545' if w > 0 else '#198754' for w in feature_weights]
            bars = plt.barh(range(len(feature_names)), feature_weights, color=colors, alpha=0.7)
            
            # Bar etiketleri
            for i, bar in enumerate(bars):
                width = bar.get_width()
                plt.text(width if width > 0 else 0,
                        bar.get_y() + bar.get_height()/2,
                        f'{width:.4f}',
                        ha='left' if width > 0 else 'right',
                        va='center',
                        color='white',
                        fontweight='bold',
                        padding=5)
            
            plt.yticks(range(len(feature_names)), feature_names)
            plt.xlabel('LIME Ağırlığı (Local Etki)')
            plt.title('LIME Feature Önem Sıralaması', pad=20)
            plt.axvline(x=0, color='black', linestyle='-', alpha=0.3)
            
            # Prediction probability
            plt.subplot(2, 1, 2)
            probs = lime_exp.predict_proba
            plt.bar(['Normal', 'Fraud'], probs, color=['#198754', '#dc3545'])
            plt.title('LIME Tahmin Olasılıkları', pad=20)
            plt.ylim(0, 1)
            
            # Probability etiketleri
            for i, v in enumerate(probs):
                plt.text(i, v/2, f'{v:.1%}',
                        ha='center', va='center',
                        color='white', fontweight='bold')
            
            plt.tight_layout()
            lime_plot_path = os.path.join(output_dir, 'lime_explanation.png')
            plt.savefig(lime_plot_path, dpi=300, bbox_inches='tight', facecolor='white')
            plt.close()

            # HTML raporu
            html_path = os.path.join(output_dir, 'lime_explanation.html')
            with open(html_path, 'w', encoding='utf-8') as f:
                f.write(f'''
                <!DOCTYPE html>
                <html lang="tr">
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>LIME Analiz Raporu</title>
                    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
                    <script src="https://cdn.plot.ly/plotly-latest.min.js"></script>
                    <style>
                        body {{ font-family: "Segoe UI", system-ui, -apple-system, sans-serif; }}
                        .container {{ max-width: 1200px; margin: 2rem auto; }}
                        .card {{ border: none; border-radius: 15px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); margin-bottom: 2rem; }}
                        .feature-card {{ background: #fff; border-radius: 10px; padding: 1rem; margin: 0.5rem; }}
                        .positive {{ color: #dc3545; }}
                        .negative {{ color: #198754; }}
                    </style>
                </head>
                <body>
                    <div class="container">
                        <h1 class="text-center mb-4">LIME Analiz Raporu</h1>
                        
                        <div class="card">
                            <div class="card-body">
                                <h3>Local Feature Önem Sıralaması</h3>
                                <div id="feature-importance-plot"></div>
                            </div>
                        </div>
                        
                        <div class="card">
                            <div class="card-body">
                                <h3>Tahmin Olasılıkları</h3>
                                <div id="prediction-probabilities"></div>
                            </div>
                        </div>
                        
                        <div class="card">
                            <div class="card-body">
                                <h3>Detaylı Feature Analizi</h3>
                                <div class="row">
                ''')
                
                # Feature kartları
                for condition, weight in lime_features[:10]:
                    impact = "Pozitif" if weight > 0 else "Negatif"
                    color_class = "positive" if weight > 0 else "negative"
                    f.write(f'''
                        <div class="col-md-6">
                            <div class="feature-card">
                                <h4>Feature Koşulu</h4>
                                <p>{condition}</p>
                                <p class="{color_class}">
                                    <strong>Etki:</strong> {impact}<br>
                                    <strong>LIME Ağırlığı:</strong> {weight:.4f}
                                </p>
                            </div>
                        </div>
                    ''')
                
                f.write('''
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <script>
                        // Feature importance plot
                        var trace1 = {{
                            x: ''' + str([abs(w) for _, w in lime_features[:15]]) + ''',
                            y: ''' + str([f for f, _ in lime_features[:15]]) + ''',
                            type: 'bar',
                            orientation: 'h',
                            marker: {{
                                color: ''' + str(['#dc3545' if w > 0 else '#198754' for _, w in lime_features[:15]]) + '''
                            }}
                        }};
                        
                        var layout1 = {{
                            title: 'Local Feature Önem Sıralaması',
                            xaxis: {{title: 'Mutlak LIME Ağırlığı'}},
                            yaxis: {{title: 'Feature'}},
                            height: 600
                        }};
                        
                        Plotly.newPlot('feature-importance-plot', [trace1], layout1);
                        
                        // Prediction probabilities
                        var trace2 = {{
                            x: ['Normal', 'Fraud'],
                            y: ''' + str(probs) + ''',
                            type: 'bar',
                            marker: {{
                                color: ['#198754', '#dc3545']
                            }}
                        }};
                        
                        var layout2 = {{
                            title: 'Tahmin Olasılıkları',
                            yaxis: {{
                                title: 'Olasılık',
                                range: [0, 1]
                            }}
                        }};
                        
                        Plotly.newPlot('prediction-probabilities', [trace2], layout2);
                    </script>
                </body>
                </html>
                ''')

            return {
                'local_explanation': [
                    {
                        'feature_condition': condition,
                        'weight': float(weight),
                        'contribution': 'positive' if weight > 0 else 'negative'
                    }
                    for condition, weight in lime_features
                ],
                'prediction_probability': float(lime_exp.predict_proba[1]),
                'intercept': float(lime_exp.intercept[1]),
                'visualization_path': lime_plot_path,
                'html_report_path': html_path,
                'top_contributing_features': sorted(
                    lime_features, key=lambda x: abs(x[1]), reverse=True
                )[:5]
            }

        except Exception as e:
            print(f"LIME explanation error: {e}")
            return {'error': str(e)}

    def _generate_business_explanation(self, api_prediction: Dict, explanations: Dict, transaction_data: Dict) -> Dict:
        """Business açıklaması oluştur - Gerçek transaction format'ı ile"""
        try:
            fraud_prob = api_prediction.get('Probability', 0.5)

            explanation = {
                'summary': {
                    'fraud_probability': f"{fraud_prob:.1%}",
                    'risk_level': 'YÜKSEK' if fraud_prob > 0.7 else 'ORTA' if fraud_prob > 0.3 else 'DÜŞÜK',
                    'decision': 'İNCELE' if fraud_prob > 0.5 else 'ONAYLA',
                    'confidence': 'YÜKSEK' if fraud_prob > 0.8 or fraud_prob < 0.2 else 'ORTA'
                },
                'risk_factors': [],
                'recommendations': [],
                'key_insights': [],
                'transaction_analysis': {}
            }

            # Risk faktörleri analizi
            risk_factors = []

            # Tutar analizi
            amount = transaction_data.get('amount', 0)
            if amount > 5000:
                risk_factors.append({
                    'factor': 'Yüksek İşlem Tutarı',
                    'value': f"₺{amount:,.2f}",
                    'risk_level': 'YÜKSEK' if amount > 10000 else 'ORTA',
                    'explanation': 'Yüksek tutarlı işlemler daha fazla dolandırıcılık riski taşır'
                })
            elif amount < 1:
                risk_factors.append({
                    'factor': 'Anormal Düşük Tutar',
                    'value': f"₺{amount:.2f}",
                    'risk_level': 'ORTA',
                    'explanation': 'Çok düşük tutarlar test amaçlı davranışları gösterebilir'
                })

            # Konum riski
            if transaction_data.get('isHighRiskRegion', False):
                risk_factors.append({
                    'factor': 'Yüksek Riskli Bölge',
                    'value': f"{transaction_data.get('city', 'Bilinmiyor')}, {transaction_data.get('country', 'Bilinmiyor')}",
                    'risk_level': 'YÜKSEK',
                    'explanation': 'Bilinen yüksek riskli bölgeden gelen işlem'
                })

            # Cihaz/IP riski
            if transaction_data.get('ipChanged', False):
                risk_factors.append({
                    'factor': 'IP Adresi Değişimi',
                    'value': 'Önceki işlemlerden farklı IP adresi',
                    'risk_level': 'ORTA',
                    'explanation': 'Farklı IP adresi hesap güvenliğinin tehlikeye girdiğini gösterebilir'
                })

            # Yeni ödeme yöntemi riski
            if transaction_data.get('isNewPaymentMethod', False):
                risk_factors.append({
                    'factor': 'Yeni Ödeme Yöntemi',
                    'value': 'Bu kart/hesap ilk kez kullanılıyor',
                    'risk_level': 'ORTA',
                    'explanation': 'Yeni ödeme yöntemleri daha yüksek dolandırıcılık riski taşır'
                })

            # Hesap yaşı riski
            days_since_first = transaction_data.get('daysSinceFirstTransaction', 30)
            if days_since_first < 7:
                risk_factors.append({
                    'factor': 'Yeni Hesap',
                    'value': f'{days_since_first} günlük',
                    'risk_level': 'YÜKSEK',
                    'explanation': 'Çok yeni hesaplar daha yüksek dolandırıcılık riski taşır'
                })

            # İşlem hızı riski
            velocity_24h = transaction_data.get('transactionVelocity24h', 1)
            if velocity_24h > 10:
                risk_factors.append({
                    'factor': 'Yüksek İşlem Hızı',
                    'value': f'Son 24 saatte {velocity_24h} işlem',
                    'risk_level': 'YÜKSEK',
                    'explanation': 'Kısa sürede çok sayıda işlem dolandırıcılık göstergesi olabilir'
                })

            # Ortalama tutar karşılaştırması
            avg_amount = transaction_data.get('averageTransactionAmount', amount)
            if amount > avg_amount * 5:
                risk_factors.append({
                    'factor': 'Ortalamadan Çok Yüksek Tutar',
                    'value': f'₺{amount:.2f} vs ort. ₺{avg_amount:.2f}',
                    'risk_level': 'YÜKSEK',
                    'explanation': 'İşlem tutarı kullanıcı davranışından önemli ölçüde yüksek'
                })

            explanation['risk_factors'] = risk_factors

            # SHAP içgörüleri
            if 'shap' in explanations and 'top_positive_features' in explanations['shap']:
                top_shap = explanations['shap']['top_positive_features'][:3]
                explanation['key_insights'].extend([
                    f"{feat['feature']}: Dolandırıcılık riskini {feat['shap_value']:.4f} artırıyor"
                    for feat in top_shap
                ])

            # LIME içgörüleri
            if 'lime' in explanations and 'local_explanation' in explanations['lime']:
                top_lime = [le for le in explanations['lime']['local_explanation'] if le['weight'] > 0][:2]
                explanation['key_insights'].extend([
                    f"{le['feature_condition']}: Yerel etki {le['weight']:.4f}"
                    for le in top_lime
                ])

            # Öneriler
            recommendations = []
            if fraud_prob > 0.8:
                recommendations.extend([
                    'ACİL BLOK - Çok yüksek dolandırıcılık olasılığı',
                    'Dolandırıcılık araştırma ekibini bilgilendir',
                    'Müşteriyle hemen iletişime geç ve doğrulama yap',
                    'Gerekirse hesaba geçici blok koy'
                ])
            elif fraud_prob > 0.5:
                recommendations.extend([
                    'MANUEL İNCELEME GEREKLİ',
                    'Ek kimlik doğrulama iste (2FA, SMS)',
                    'İşlemi müşteriyle doğrula',
                    'Hesabı olağandışı aktivite için izle'
                ])
            elif fraud_prob > 0.3:
                recommendations.extend([
                    'Gelişmiş izleme önerilir',
                    'Ek kimlik doğrulama düşün',
                    'İşlemi davranış analizi için kaydet'
                ])
            else:
                recommendations.extend([
                    'İşlemi normal şekilde işle',
                    'Standart izlemeye devam et'
                ])

            explanation['recommendations'] = recommendations

            # İşlem analizi özeti
            explanation['transaction_analysis'] = {
                'merchant': transaction_data.get('merchantId', 'Bilinmiyor'),
                'card_type': transaction_data.get('cardType', 'Bilinmiyor'),
                'bank': transaction_data.get('bankName', 'Bilinmiyor'),
                'international': transaction_data.get('isInternational', False),
                'device_type': transaction_data.get('deviceType', 'Bilinmiyor'),
                'user_risk_profile': self._calculate_user_risk_profile(transaction_data)
            }

            return explanation

        except Exception as e:
            print(f"İş açıklaması hatası: {e}")
            return {
                'error': str(e),
                'summary': {
                    'fraud_probability': f"{api_prediction.get('Probability', 0.5):.1%}",
                    'decision': 'MANUEL_İNCELEME',
                    'risk_level': 'BİLİNMİYOR'
                }
            }

    def _calculate_user_risk_profile(self, transaction_data: Dict) -> str:
        """Kullanıcı risk profilini hesapla"""
        risk_score = 0

        # Hesap yaşı
        days_since_first = transaction_data.get('daysSinceFirstTransaction', 30)
        if days_since_first < 7:
            risk_score += 3
        elif days_since_first < 30:
            risk_score += 1

        # İşlem hızı
        velocity = transaction_data.get('transactionVelocity24h', 1)
        if velocity > 20:
            risk_score += 3
        elif velocity > 10:
            risk_score += 2
        elif velocity > 5:
            risk_score += 1

        # Yeni ödeme yöntemi
        if transaction_data.get('isNewPaymentMethod', False):
            risk_score += 1

        # IP değişimleri
        if transaction_data.get('ipChanged', False):
            risk_score += 2

        # Yüksek riskli bölge
        if transaction_data.get('isHighRiskRegion', False):
            risk_score += 2

        # Risk profili
        if risk_score >= 7:
            return 'YÜKSEK_RİSK'
        elif risk_score >= 4:
            return 'ORTA_RİSK'
        elif risk_score >= 2:
            return 'DÜŞÜK_ORTA_RİSK'
        else:
            return 'DÜŞÜK_RİSK'

    def _create_api_only_response(self, api_prediction: Dict, transaction_data: Dict) -> Dict:
        """Sadece API sonucu olan response oluştur"""
        return {
            'timestamp': datetime.now().isoformat(),
            'transaction_id': transaction_data.get('transactionId', 'unknown'),
            'api_prediction': api_prediction,
            'explanations_available': False,
            'note': 'Local model not available, showing API prediction only',
            'business_explanation': {
                'summary': {
                    'fraud_probability': f"{api_prediction.get('Probability', 0.5):.1%}",
                    'decision': 'İNCELE' if api_prediction.get('Probability', 0.5) > 0.5 else 'ONAYLA',
                    'source': 'API_ONLY'
                }
            }
        }

    def batch_explain(self, transactions: List[Dict], model_type: str = "Ensemble", method: str = "shap") -> Dict:
        """Birden fazla transaction için batch açıklama"""
        print(f"=== BATCH EXPLANATION START ===")
        print(f"Transaction count: {len(transactions)}")

        results = []
        errors = []

        for i, transaction in enumerate(transactions):
            try:
                print(f"Processing transaction {i + 1}/{len(transactions)}")
                result = self.explain_transaction(
                    transaction,
                    model_type=model_type,
                    method=method,
                    output_dir=f"explanations/batch_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
                )
                results.append(result)
            except Exception as e:
                print(f"Error processing transaction {i + 1}: {e}")
                errors.append({
                    'transaction_index': i,
                    'transaction_id': transaction.get('transactionId', 'unknown'),
                    'error': str(e)
                })

        print(f"✅ Batch explanation completed. Success: {len(results)}, Errors: {len(errors)}")

        return {
            'timestamp': datetime.now().isoformat(),
            'total_transactions': len(transactions),
            'successful_explanations': len(results),
            'failed_explanations': len(errors),
            'results': results,
            'errors': errors
        }


def create_sample_transaction() -> Dict:
    """Test için örnek transaction oluştur"""
    return {
        'userId': str(uuid.uuid4()),
        'amount': 2847.91,
        'merchantId': 'MERCHANT_TEST_001',
        'type': 0,  # Purchase
        
        # Location bilgileri
        'location': {
            'latitude': 40.7128,
            'longitude': -74.0060,
            'country': 'US',
            'city': 'New York'
        },
        
        # Device bilgileri
        'deviceInfo': {
            'deviceId': 'TEST_DEVICE_001',
            'deviceType': 'Desktop',
            'ipAddress': '192.168.1.100',
            'userAgent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
            'additionalInfo': {
                'ipChanged': 'false'
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
            'bankName': 'Test Bank',
            'bankCountry': 'US',
            
            # V-Faktör değerleri
            'vFactors': {
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
            },
            
            # Risk faktörleri
            'daysSinceFirstTransaction': 1,
            'transactionVelocity24h': 10,
            'averageTransactionAmount': 50.0,
            'isNewPaymentMethod': True,
            'isInternational': False,
            
            # Özel değerler
            'customValues': {
                'isHighRiskRegion': 'true'
            }
        }
    }


def main():
    """Ana test fonksiyonu"""
    print("=== FRAUD EXPLAINABILITY DEMO ===")

    # API Client
    api_client = FraudDetectionAPIClient("http://localhost:5000")

    # Health check
    if not api_client.health_check():
        print("❌ API'ye bağlanılamıyor!")
        print("API'nin çalıştığından emin olun: http://localhost:5000")
        return

    print("✅ API bağlantısı başarılı!")

    # Explainability Analyzer
    analyzer = ExplainabilityAnalyzer(api_client, models_path="models")

    # Test transaction
    sample_transaction = create_sample_transaction()
    print(f"\n📊 Test Transaction: {sample_transaction['transactionId']}")
    print(f"Amount: ${sample_transaction['amount']:,.2f}")

    # Açıklama yap
    try:
        explanation = analyzer.explain_transaction(
            sample_transaction,
            model_type="Ensemble",
            method="both",
            output_dir="demo_explanations"
        )

        if 'error' not in explanation:
            print("\n✅ Açıklama başarılı!")

            # API prediction
            api_pred = explanation.get('api_prediction', {})
            print(f"🎯 Fraud Probability: {api_pred.get('Probability', 'N/A'):.4f}")
            print(f"🏷️ Predicted Class: {'FRAUD' if api_pred.get('IsFraudulent', False) else 'NORMAL'}")

            # Business explanation
            business = explanation.get('business_explanation', {})
            if 'summary' in business:
                print(f"📋 Decision: {business['summary'].get('decision', 'N/A')}")
                print(f"⚠️ Risk Level: {business['summary'].get('risk_level', 'N/A')}")

            # Key insights
            if 'key_insights' in business:
                print("\n🔍 Key Insights:")
                for insight in business['key_insights'][:3]:
                    print(f"  • {insight}")

            print(f"\n📁 Detaylı sonuçlar: demo_explanations/")

        else:
            print(f"❌ Açıklama hatası: {explanation['error']}")

    except Exception as e:
        print(f"❌ Test hatası: {e}")
        import traceback
        traceback.print_exc()


if __name__ == "__main__":
    main()