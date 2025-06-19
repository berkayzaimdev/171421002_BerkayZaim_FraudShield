import requests
import json
import os
from datetime import datetime
from typing import Dict, Any, List, Optional
import time


class FraudDetectionAPIClient:
    """
    Fraud Detection API'sine istek atan client sınıfı
    """

    def __init__(self, base_url: str = "http://localhost:5000", timeout: int = 300):
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
                return result
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
            return {"error": str(e), "status_code": 500}

    # Model Eğitimi Metodları
    def train_lightgbm(self, config: Dict = None) -> Dict:
        """LightGBM modeli eğit"""
        endpoint = "/train/lightgbm" if config is None else "/train/lightgbm-config"
        return self._make_request("POST", endpoint, data=config)

    def train_pca(self, config: Dict = None) -> Dict:
        """PCA modeli eğit"""
        endpoint = "/train/pca" if config is None else "/train/pca-config"
        return self._make_request("POST", endpoint, data=config)

    def train_ensemble(self, config: Dict = None) -> Dict:
        """Ensemble modeli eğit"""
        endpoint = "/train/ensemble" if config is None else "/train/ensemble-config"
        return self._make_request("POST", endpoint, data=config)

    # Model Bilgileri
    def get_model_metrics(self, model_name: str) -> Dict:
        """Model metriklerini al"""
        return self._make_request("GET", f"/{model_name}/metrics")

    def get_performance_summary(self, model_name: str) -> Dict:
        """Model performans özetini al"""
        return self._make_request("GET", f"/{model_name}/performance-summary")

    def get_model_versions(self, model_name: str) -> Dict:
        """Model versiyonlarını al"""
        return self._make_request("GET", f"/{model_name}/versions")

    def compare_models(self, model_names: List[str]) -> Dict:
        """Modelleri karşılaştır"""
        return self._make_request("POST", "/compare", data=model_names)

    # Model Aktivasyonu
    def activate_model_version(self, model_name: str, version: str) -> Dict:
        """Model versiyonunu aktifleştir"""
        return self._make_request("POST", f"/{model_name}/versions/{version}/activate")

    # Tahmin
    def predict(self, transaction_data: Dict) -> Dict:
        """Genel tahmin yap"""
        return self._make_request("POST", "/predict", data=transaction_data)

    def predict_with_model(self, model_name: str, transaction_data: Dict) -> Dict:
        """Belirli bir modelle tahmin yap"""
        return self._make_request("POST", f"/{model_name}/predict", data=transaction_data)

    # Yardımcı Metodlar
    def health_check(self) -> bool:
        """API'nin sağlıklı olup olmadığını kontrol et"""
        try:
            # Basit bir GET isteği gönder
            response = self.session.get(f"{self.base_url}/health", timeout=5)
            return response.status_code == 200
        except:
            return False

    def wait_for_api(self, max_wait: int = 60) -> bool:
        """API'nin hazır olmasını bekle"""
        print(f"🔄 API hazır olması bekleniyor...")

        for i in range(max_wait):
            if self.health_check():
                print(f"✅ API hazır! ({i}s)")
                return True

            print(f"⏳ Bekleniyor... ({i + 1}/{max_wait}s)")
            time.sleep(1)

        print(f"❌ API {max_wait}s içinde hazır olmadı")
        return False


class ConfigurationGenerator:
    """
    Çeşitli model konfigürasyonları oluşturan sınıf
    """

    @staticmethod
    def get_lightgbm_config(preset: str = "default") -> Dict:
        """LightGBM konfigürasyonu oluştur"""

        configs = {
            "default": {
                "numberOfLeaves": 128,
                "minDataInLeaf": 10,
                "learningRate": 0.005,
                "numberOfTrees": 1000,
                "featureFraction": 0.8,
                "baggingFraction": 0.8,
                "baggingFrequency": 5,
                "l1Regularization": 0.01,
                "l2Regularization": 0.01,
                "earlyStoppingRound": 100,
                "minGainToSplit": 0.0005,
                "useClassWeights": True,
                "classWeights": {"0": 1.0, "1": 75.0},
                "predictionThreshold": 0.5
            },
            "fast": {
                "numberOfLeaves": 64,
                "minDataInLeaf": 20,
                "learningRate": 0.01,
                "numberOfTrees": 500,
                "featureFraction": 0.9,
                "baggingFraction": 0.9,
                "baggingFrequency": 3,
                "l1Regularization": 0.001,
                "l2Regularization": 0.001,
                "useClassWeights": True,
                "classWeights": {"0": 1.0, "1": 50.0}
            },
            "accurate": {
                "numberOfLeaves": 256,
                "minDataInLeaf": 5,
                "learningRate": 0.002,
                "numberOfTrees": 2000,
                "featureFraction": 0.7,
                "baggingFraction": 0.7,
                "baggingFrequency": 7,
                "l1Regularization": 0.05,
                "l2Regularization": 0.05,
                "useClassWeights": True,
                "classWeights": {"0": 1.0, "1": 100.0}
            },
            "balanced": {
                "numberOfLeaves": 100,
                "minDataInLeaf": 15,
                "learningRate": 0.01,
                "numberOfTrees": 1500,
                "featureFraction": 0.8,
                "baggingFraction": 0.8,
                "baggingFrequency": 5,
                "l1Regularization": 0.02,
                "l2Regularization": 0.02,
                "useClassWeights": True,
                "classWeights": {"0": 1.0, "1": 80.0}
            }
        }

        return configs.get(preset, configs["default"])

    @staticmethod
    def get_pca_config(preset: str = "default") -> Dict:
        """PCA konfigürasyonu oluştur"""

        configs = {
            "default": {
                "componentCount": 15,
                "anomalyThreshold": 2.5,
                "scaleFeatures": True,
                "randomState": 42
            },
            "sensitive": {
                "componentCount": 20,
                "anomalyThreshold": 2.0,
                "scaleFeatures": True,
                "randomState": 42
            },
            "conservative": {
                "componentCount": 10,
                "anomalyThreshold": 3.0,
                "scaleFeatures": True,
                "randomState": 42
            }
        }

        return configs.get(preset, configs["default"])

    @staticmethod
    def get_ensemble_config(preset: str = "default") -> Dict:
        """Ensemble konfigürasyonu oluştur"""

        configs = {
            "default": {
                "lightgbmWeight": 0.7,
                "pcaWeight": 0.3,
                "threshold": 0.5,
                "minConfidenceThreshold": 0.8,
                "enableCrossValidation": True,
                "crossValidationFolds": 5,
                "combinationStrategy": "WeightedAverage",
                "lightgbm": ConfigurationGenerator.get_lightgbm_config("default"),
                "pca": ConfigurationGenerator.get_pca_config("default")
            },
            "lgbm_heavy": {
                "lightgbmWeight": 0.85,
                "pcaWeight": 0.15,
                "threshold": 0.5,
                "lightgbm": ConfigurationGenerator.get_lightgbm_config("accurate"),
                "pca": ConfigurationGenerator.get_pca_config("conservative")
            },
            "balanced": {
                "lightgbmWeight": 0.6,
                "pcaWeight": 0.4,
                "threshold": 0.45,
                "lightgbm": ConfigurationGenerator.get_lightgbm_config("balanced"),
                "pca": ConfigurationGenerator.get_pca_config("sensitive")
            }
        }

        return configs.get(preset, configs["default"])


# Test fonksiyonu
def test_api_client():
    """API Client'ı test et"""

    # Client'ı başlat
    client = FraudDetectionAPIClient("http://localhost:5000")

    # Health check
    if not client.health_check():
        print("❌ API'ye bağlanılamıyor!")
        return

    print("✅ API bağlantısı başarılı!")

    # Basit bir model eğitimi test et
    print("\n📊 LightGBM model eğitimi test ediliyor...")
    config = ConfigurationGenerator.get_lightgbm_config("fast")

    result = client.train_lightgbm(config)

    if "error" not in result:
        print("✅ Model eğitimi başarılı!")
        print(f"Model ID: {result.get('ModelId')}")
        print(f"Accuracy: {result.get('BasicMetrics', {}).get('Accuracy', 'N/A')}")
    else:
        print(f"❌ Model eğitimi başarısız: {result['error']}")


if __name__ == "__main__":
    test_api_client()