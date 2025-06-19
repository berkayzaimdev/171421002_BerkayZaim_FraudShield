#!/usr/bin/env python3
"""
Fraud Detection Model Analiz ve Rapor Sistemi
Ana koordinatör script - Tüm işlemleri yönetir
"""

import os
import sys
import argparse
import json
from datetime import datetime
from typing import Dict, List, Optional
import time

# Kendi modüllerimizi import et
from api_client import FraudDetectionAPIClient, ConfigurationGenerator
from model_reporter import ModelReporter
from hyperparameter_tuning import HyperparameterTuner

class FraudDetectionAnalyzer:
    """
    Fraud Detection Model Analiz ve Rapor Sistemi Ana Sınıfı
    """

    def __init__(self, base_url: str = "http://localhost:5112", output_dir: str = "analysis_results"):
        """
        Analyzer'ı başlat

        Args:
            base_url: API base URL
            output_dir: Çıktı dizini
        """
        self.base_url = base_url
        self.output_dir = output_dir

        # Ana çıktı dizini oluştur
        os.makedirs(output_dir, exist_ok=True)

        # API Client
        self.api_client = FraudDetectionAPIClient(base_url)

        # Modüller
        self.reporter = ModelReporter(self.api_client, f"{output_dir}/reports")
        self.tuner = HyperparameterTuner(self.api_client, f"{output_dir}/tuning")

        print(f"🚀 Fraud Detection Analyzer başlatıldı")
        print(f"🌐 API: {base_url}")
        print(f"📁 Çıktı: {output_dir}")

    def run_quick_analysis(self) -> str:
        """
        Hızlı model analizi - Varsayılan konfigürasyonlarla

        Returns:
            HTML rapor dosyası yolu
        """
        print("\n🏃‍♂️ Hızlı Model Analizi Başlatılıyor...")

        # API kontrolü
        if not self._check_api_health():
            return None

        # Basit konfigürasyonlar
        configs = {
            "LightGBM_Quick": {
                "type": "lightgbm",
                "config": ConfigurationGenerator.get_lightgbm_config("fast")
            },
            "PCA_Quick": {
                "type": "pca",
                "config": ConfigurationGenerator.get_pca_config("default")
            },
            "Ensemble_Quick": {
                "type": "ensemble",
                "config": ConfigurationGenerator.get_ensemble_config("default")
            }
        }

        # Rapor oluştur
        html_report = self.reporter.create_full_report(configs)

        print(f"✅ Hızlı analiz tamamlandı: {html_report}")
        return html_report

    def run_comprehensive_analysis(self) -> str:
        """
        Kapsamlı model analizi - Çoklu konfigürasyonlarla

        Returns:
            HTML rapor dosyası yolu
        """
        print("\n🔬 Kapsamlı Model Analizi Başlatılıyor...")

        if not self._check_api_health():
            return None

        # Çoklu konfigürasyonlar
        configs = {
            "LightGBM_Fast": {
                "type": "lightgbm",
                "config": ConfigurationGenerator.get_lightgbm_config("fast")
            },
            "LightGBM_Accurate": {
                "type": "lightgbm",
                "config": ConfigurationGenerator.get_lightgbm_config("accurate")
            },
            "LightGBM_Balanced": {
                "type": "lightgbm",
                "config": ConfigurationGenerator.get_lightgbm_config("balanced")
            },
            "PCA_Default": {
                "type": "pca",
                "config": ConfigurationGenerator.get_pca_config("default")
            },
            "PCA_Sensitive": {
                "type": "pca",
                "config": ConfigurationGenerator.get_pca_config("sensitive")
            },
            "PCA_Conservative": {
                "type": "pca",
                "config": ConfigurationGenerator.get_pca_config("conservative")
            },
            "Ensemble_Balanced": {
                "type": "ensemble",
                "config": ConfigurationGenerator.get_ensemble_config("balanced")
            },
            "Ensemble_LGBMHeavy": {
                "type": "ensemble",
                "config": ConfigurationGenerator.get_ensemble_config("lgbm_heavy")
            }
        }

        html_report = self.reporter.create_full_report(configs)

        print(f"✅ Kapsamlı analiz tamamlandı: {html_report}")
        return html_report

    def run_hyperparameter_optimization(self, model_types: List[str] = None) -> Dict:
        """
        Hiperparametre optimizasyonu çalıştır

        Args:
            model_types: Optimize edilecek model tipleri ['lightgbm', 'pca', 'ensemble']

        Returns:
            Optimizasyon sonuçları
        """
        print("\n🔧 Hiperparametre Optimizasyonu Başlatılıyor...")

        if not self._check_api_health():
            return {}

        if model_types is None:
            model_types = ["lightgbm", "pca", "ensemble"]

        results = {}

        # LightGBM optimizasyonu
        if "lightgbm" in model_types:
            print("\n📊 LightGBM Optimizasyonu...")
            lgbm_result = self.tuner.tune_lightgbm(
                max_experiments=15,
                optimization_metric="f1_score"
            )
            results["lightgbm"] = lgbm_result

        # PCA optimizasyonu
        if "pca" in model_types:
            print("\n📊 PCA Optimizasyonu...")
            pca_result = self.tuner.tune_pca(
                max_experiments=10,
                optimization_metric="accuracy"
            )
            results["pca"] = pca_result

        # Ensemble optimizasyonu
        if "ensemble" in model_types:
            print("\n📊 Ensemble Optimizasyonu...")
            ensemble_result = self.tuner.tune_ensemble(max_experiments=12)
            results["ensemble"] = ensemble_result

        # Sonuçları kaydet
        self._save_optimization_summary(results)

        print(f"✅ Hiperparametre optimizasyonu tamamlandı")
        return results

    def run_custom_experiment(self, experiment_config: Dict) -> str:
        """
        Özel deneyim konfigürasyonu ile analiz

        Args:
            experiment_config: Deneyim konfigürasyonu

        Returns:
            HTML rapor dosyası yolu
        """
        print(f"\n🧪 Özel Deneyim Başlatılıyor: {experiment_config.get('name', 'Custom')}")

        if not self._check_api_health():
            return None

        # Konfigürasyonları hazırla
        configs = experiment_config.get("models", {})

        if not configs:
            print("❌ Model konfigürasyonları bulunamadı!")
            return None

        html_report = self.reporter.create_full_report(configs)

        print(f"✅ Özel deneyim tamamlandı: {html_report}")
        return html_report

    def compare_existing_models(self, model_names: List[str]) -> Dict:
        """
        Mevcut modelleri karşılaştır

        Args:
            model_names: Karşılaştırılacak model isimleri

        Returns:
            Karşılaştırma sonuçları
        """
        print(f"\n🔄 Model Karşılaştırması: {model_names}")

        if not self._check_api_health():
            return {}

        comparison = self.api_client.compare_models(model_names)

        if comparison and "error" not in comparison:
            # Karşılaştırma görselleştirmeleri oluştur
            self.reporter._create_comparison_visualizations(comparison)

            # Sonuçları kaydet
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            comparison_file = f"{self.output_dir}/model_comparison_{timestamp}.json"

            with open(comparison_file, 'w') as f:
                json.dump(comparison, f, indent=2, default=str)

            print(f"✅ Model karşılaştırması tamamlandı: {comparison_file}")

        return comparison

    def generate_production_recommendations(self) -> Dict:
        """
        Production için model önerileri oluştur

        Returns:
            Öneriler sözlüğü
        """
        print("\n🏭 Production Önerileri Oluşturuluyor...")

        if not self._check_api_health():
            return {}

        # Hızlı bir analiz yap
        quick_configs = {
            "Production_LightGBM": {
                "type": "lightgbm",
                "config": ConfigurationGenerator.get_lightgbm_config("balanced")
            },
            "Production_Ensemble": {
                "type": "ensemble",
                "config": ConfigurationGenerator.get_ensemble_config("balanced")
            }
        }

        # Modelleri eğit ve analiz et
        production_results = {}

        for model_name, config in quick_configs.items():
            print(f"📊 {model_name} analiz ediliyor...")

            try:
                # Model eğit
                if config["type"] == "lightgbm":
                    result = self.api_client.train_lightgbm(config["config"])
                elif config["type"] == "ensemble":
                    result = self.api_client.train_ensemble(config["config"])

                if result and "error" not in result:
                    # Metrikleri al - gerçek model ismini kullan
                    actual_model_name = result.get("modelName") or result.get("ModelName", model_name)
                    metrics = self.api_client.get_model_metrics(actual_model_name)

                    if metrics and "error" not in metrics:
                        production_results[model_name] = {
                            "training_result": result,
                            "metrics": metrics,
                            "actual_model_name": actual_model_name
                        }
                        print(f"✅ {model_name} analizi tamamlandı -> {actual_model_name}")
                    else:
                        print(f"❌ {model_name} metrikleri alınamadı: {metrics.get('error', 'Bilinmeyen hata')}")

            except Exception as e:
                print(f"⚠️ {model_name} hatası: {str(e)}")

        # Önerileri oluştur
        recommendations = self._generate_production_recommendations(production_results)

        # Kaydet
        rec_file = f"{self.output_dir}/production_recommendations_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        with open(rec_file, 'w') as f:
            json.dump(recommendations, f, indent=2, default=str)

        print(f"✅ Production önerileri: {rec_file}")
        return recommendations

    def _check_api_health(self) -> bool:
        """API sağlık kontrolü"""

        print("🔍 API bağlantısı kontrol ediliyor...")

        if not self.api_client.health_check():
            print("❌ API'ye bağlanılamıyor!")
            print(f"🔧 API'nin çalıştığından emin olun: {self.base_url}")
            return False

        print("✅ API bağlantısı başarılı!")
        return True

    def _save_optimization_summary(self, results: Dict):
        """Optimizasyon özetini kaydet"""

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        summary_file = f"{self.output_dir}/optimization_summary_{timestamp}.json"

        # Özet oluştur
        summary = {
            "timestamp": timestamp,
            "optimization_results": results,
            "best_configs": {},
            "recommendations": []
        }

        # En iyi konfigürasyonları çıkar
        for model_type, result in results.items():
            if isinstance(result, dict) and "best_config" in result:
                summary["best_configs"][model_type] = result["best_config"]
                summary["recommendations"].append(
                    f"{model_type.upper()}: En iyi skor {result.get('best_score', 0):.4f}"
                )

        with open(summary_file, 'w') as f:
            json.dump(summary, f, indent=2, default=str)

        print(f"💾 Optimizasyon özeti kaydedildi: {summary_file}")

    def _generate_production_recommendations(self, results: Dict) -> Dict:
        """Production önerileri oluştur"""

        recommendations = {
            "timestamp": datetime.now().isoformat(),
            "evaluated_models": list(results.keys()),
            "recommendations": [],
            "deployment_strategy": {},
            "monitoring_suggestions": []
        }

        # Model performanslarını karşılaştır
        best_model = None
        best_score = -1

        for model_name, data in results.items():
            try:
                basic_metrics = data["training_result"]["BasicMetrics"]
                f1_score = basic_metrics.get("F1Score", 0)

                if f1_score > best_score:
                    best_score = f1_score
                    best_model = model_name

                # Model spesifik öneriler
                if "LightGBM" in model_name:
                    if basic_metrics.get("Accuracy", 0) > 0.95:
                        recommendations["recommendations"].append(
                            f"{model_name}: Hızlı tahmin için ideal, real-time sistemlerde kullanılabilir"
                        )
                elif "Ensemble" in model_name:
                    if basic_metrics.get("F1Score", 0) > 0.8:
                        recommendations["recommendations"].append(
                            f"{model_name}: Yüksek doğruluk için ideal, batch processing'de kullanılabilir"
                        )

            except Exception as e:
                print(f"⚠️ {model_name} önerileri oluşturulurken hata: {str(e)}")

        # Ana öneriler
        if best_model:
            recommendations["recommendations"].insert(0, f"🏆 En iyi model: {best_model} (F1: {best_score:.3f})")

        # Deployment stratejisi
        recommendations["deployment_strategy"] = {
            "primary_model": best_model,
            "fallback_strategy": "LightGBM modelini fallback olarak kullanın",
            "performance_threshold": 0.8,
            "retraining_frequency": "Aylık",
            "monitoring_metrics": ["Accuracy", "Precision", "Recall", "F1Score"]
        }

        # İzleme önerileri
        recommendations["monitoring_suggestions"] = [
            "Model performansını günlük olarak izleyin",
            "False positive oranını düşük tutun",
            "Veri drift kontrolü yapın",
            "A/B testing ile model güncellemelerini test edin",
            "Feedback loop kurun"
        ]

        return recommendations


def main():
    """Ana fonksiyon - Command line interface"""

    parser = argparse.ArgumentParser(description="Fraud Detection Model Analiz ve Rapor Sistemi")

    parser.add_argument("--api-url", default="http://localhost:5000",
                       help="API base URL (default: http://localhost:5000)")
    parser.add_argument("--output-dir", default="analysis_results",
                       help="Çıktı dizini (default: analysis_results)")

    # Ana işlem tipleri
    parser.add_argument("--quick", action="store_true",
                       help="Hızlı model analizi çalıştır")
    parser.add_argument("--comprehensive", action="store_true",
                       help="Kapsamlı model analizi çalıştır")
    parser.add_argument("--optimize", nargs="*",
                       choices=["lightgbm", "pca", "ensemble"],
                       help="Hiperparametre optimizasyonu (belirtilen modeller için)")
    parser.add_argument("--compare-models", nargs="+",
                       help="Mevcut modelleri karşılaştır")
    parser.add_argument("--production-recommendations", action="store_true",
                       help="Production önerileri oluştur")

    # Özel deneyim
    parser.add_argument("--custom-config", type=str,
                       help="Özel deneyim konfigürasyon dosyası (JSON)")

    args = parser.parse_args()

    # Analyzer'ı başlat
    analyzer = FraudDetectionAnalyzer(args.api_url, args.output_dir)

    # İşlem seçimi
    if args.quick:
        print("🏃‍♂️ Hızlı analiz modu seçildi")
        html_report = analyzer.run_quick_analysis()
        if html_report:
            print(f"📄 Rapor: {html_report}")

    elif args.comprehensive:
        print("🔬 Kapsamlı analiz modu seçildi")
        html_report = analyzer.run_comprehensive_analysis()
        if html_report:
            print(f"📄 Rapor: {html_report}")

    elif args.optimize is not None:
        models_to_optimize = args.optimize if args.optimize else ["lightgbm", "pca", "ensemble"]
        print(f"🔧 Optimizasyon modu: {models_to_optimize}")
        results = analyzer.run_hyperparameter_optimization(models_to_optimize)
        print(f"📊 Optimizasyon sonuçları: {analyzer.output_dir}/tuning/")

    elif args.compare_models:
        print(f"🔄 Model karşılaştırma modu: {args.compare_models}")
        comparison = analyzer.compare_existing_models(args.compare_models)
        if comparison:
            print("✅ Karşılaştırma tamamlandı")

    elif args.production_recommendations:
        print("🏭 Production önerileri modu")
        recommendations = analyzer.generate_production_recommendations()
        if recommendations:
            print("✅ Öneriler oluşturuldu")

    elif args.custom_config:
        print(f"🧪 Özel deneyim modu: {args.custom_config}")

        if not os.path.exists(args.custom_config):
            print(f"❌ Konfigürasyon dosyası bulunamadı: {args.custom_config}")
            return

        with open(args.custom_config, 'r') as f:
            custom_config = json.load(f)

        html_report = analyzer.run_custom_experiment(custom_config)
        if html_report:
            print(f"📄 Rapor: {html_report}")

    else:
        print("❓ İşlem tipi belirtilmedi. --help için yardım alın")
        print("\nHızlı başlangıç:")
        print("  python main_coordinator.py --quick")
        print("  python main_coordinator.py --comprehensive")
        print("  python main_coordinator.py --optimize lightgbm pca")
        print("  python main_coordinator.py --production-recommendations")


# Örnek konfigürasyon dosyası oluşturucu
def create_sample_config():
    """Örnek konfigürasyon dosyası oluştur"""

    sample_config = {
        "name": "Custom Model Experiment",
        "description": "Özel model deneyimi",
        "models": {
            "Custom_LightGBM_1": {
                "type": "lightgbm",
                "config": {
                    "numberOfTrees": 800,
                    "numberOfLeaves": 100,
                    "learningRate": 0.01,
                    "classWeights": {"0": 1.0, "1": 60.0}
                }
            },
            "Custom_LightGBM_2": {
                "type": "lightgbm",
                "config": {
                    "numberOfTrees": 1200,
                    "numberOfLeaves": 150,
                    "learningRate": 0.005,
                    "classWeights": {"0": 1.0, "1": 90.0}
                }
            },
            "Custom_Ensemble": {
                "type": "ensemble",
                "config": {
                    "lightgbmWeight": 0.75,
                    "pcaWeight": 0.25,
                    "threshold": 0.45
                }
            }
        }
    }

    with open("sample_custom_config.json", "w") as f:
        json.dump(sample_config, f, indent=2)

    print("📝 Örnek konfigürasyon dosyası oluşturuldu: sample_custom_config.json")


if __name__ == "__main__":
    # Komut satırı argümanı yoksa interaktif mod
    if len(sys.argv) == 1:
        print("🎯 Fraud Detection Model Analiz Sistemi")
        print("="*50)
        print("1. Hızlı Analiz")
        print("2. Kapsamlı Analiz")
        print("3. Hiperparametre Optimizasyonu")
        print("4. Production Önerileri")
        print("5. Örnek Konfigürasyon Oluştur")
        print("0. Çıkış")

        choice = input("\nSeçiminizi yapın (0-5): ").strip()

        if choice == "1":
            analyzer = FraudDetectionAnalyzer()
            analyzer.run_quick_analysis()
        elif choice == "2":
            analyzer = FraudDetectionAnalyzer()
            analyzer.run_comprehensive_analysis()
        elif choice == "3":
            analyzer = FraudDetectionAnalyzer()
            analyzer.run_hyperparameter_optimization()
        elif choice == "4":
            analyzer = FraudDetectionAnalyzer()
            analyzer.generate_production_recommendations()
        elif choice == "5":
            create_sample_config()
        elif choice == "0":
            print("👋 Çıkış yapılıyor...")
        else:
            print("❌ Geçersiz seçim!")
    else:
        main()