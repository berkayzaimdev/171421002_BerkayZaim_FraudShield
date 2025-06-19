#!/usr/bin/env python3
"""
Batch Model Processor - Toplu model eğitimi ve analizi
Çoklu konfigürasyonlarla paralel model eğitimi
"""

import os
import json
import time
import pandas as pd
import numpy as np
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import Dict, List, Any, Optional, Tuple
import threading
import queue
import warnings

warnings.filterwarnings('ignore')

from api_client import FraudDetectionAPIClient, ConfigurationGenerator


class BatchModelProcessor:
    """
    Toplu model eğitimi ve analiz sınıfı
    """

    def __init__(self, api_client: FraudDetectionAPIClient,
                 output_dir: str = "batch_results",
                 max_workers: int = 3,
                 delay_between_requests: float = 5.0):
        """
        Batch Processor'ı başlat

        Args:
            api_client: API client instance
            output_dir: Sonuçların kaydedileceği dizin
            max_workers: Maksimum paralel worker sayısı
            delay_between_requests: İstekler arası bekleme süresi (saniye)
        """
        self.api_client = api_client
        self.output_dir = output_dir
        self.max_workers = max_workers
        self.delay = delay_between_requests

        # Thread-safe queue for results
        self.results_queue = queue.Queue()
        self.lock = threading.Lock()

        # Sonuçları saklamak için
        self.batch_results = []
        self.failed_experiments = []

        # Dizinleri oluştur
        os.makedirs(output_dir, exist_ok=True)
        os.makedirs(f"{output_dir}/individual_results", exist_ok=True)
        os.makedirs(f"{output_dir}/summary", exist_ok=True)
        os.makedirs(f"{output_dir}/configs", exist_ok=True)

        print(f"🚀 Batch Model Processor başlatıldı")
        print(f"📁 Çıktı dizini: {output_dir}")
        print(f"👥 Max workers: {max_workers}")
        print(f"⏱️ İstek gecikme: {delay}s")

    def run_lightgbm_experiments(self, experiment_configs: List[Dict]) -> Dict:
        """
        LightGBM deneyimlerini toplu olarak çalıştır

        Args:
            experiment_configs: Deneyim konfigürasyonları listesi

        Returns:
            Toplu sonuçlar
        """
        print(f"\n🔬 {len(experiment_configs)} LightGBM deneyi başlatılıyor...")

        return self._run_batch_experiments(experiment_configs, "lightgbm")

    def run_pca_experiments(self, experiment_configs: List[Dict]) -> Dict:
        """
        PCA deneyimlerini toplu olarak çalıştır
        """
        print(f"\n🔬 {len(experiment_configs)} PCA deneyi başlatılıyor...")

        return self._run_batch_experiments(experiment_configs, "pca")

    def run_ensemble_experiments(self, experiment_configs: List[Dict]) -> Dict:
        """
        Ensemble deneyimlerini toplu olarak çalıştır
        """
        print(f"\n🔬 {len(experiment_configs)} Ensemble deneyi başlatılıyor...")

        return self._run_batch_experiments(experiment_configs, "ensemble")

    def run_mixed_experiments(self, experiments: List[Dict]) -> Dict:
        """
        Karışık model türlerinde deneyimler çalıştır

        Args:
            experiments: [{"type": "lightgbm", "config": {...}, "name": "..."}, ...]
        """
        print(f"\n🎯 {len(experiments)} karışık model deneyi başlatılıyor...")

        with ThreadPoolExecutor(max_workers=self.max_workers) as executor:
            future_to_experiment = {}

            for i, experiment in enumerate(experiments):
                future = executor.submit(self._run_single_mixed_experiment, experiment, i)
                future_to_experiment[future] = experiment

            # Sonuçları topla
            completed_count = 0
            for future in as_completed(future_to_experiment):
                experiment = future_to_experiment[future]

                try:
                    result = future.result()

                    with self.lock:
                        if result["success"]:
                            self.batch_results.append(result)
                            print(
                                f"✅ {experiment.get('name', 'Unnamed')} tamamlandı ({completed_count + 1}/{len(experiments)})")
                        else:
                            self.failed_experiments.append(result)
                            print(f"❌ {experiment.get('name', 'Unnamed')} başarısız")

                        completed_count += 1

                except Exception as e:
                    print(f"🚨 {experiment.get('name', 'Unnamed')} exception: {str(e)}")
                    with self.lock:
                        self.failed_experiments.append({
                            "experiment": experiment,
                            "error": str(e),
                            "success": False
                        })
                        completed_count += 1

        # Sonuçları analiz et ve kaydet
        summary = self._create_batch_summary("mixed", self.batch_results, self.failed_experiments)

        return summary

    def _run_batch_experiments(self, experiment_configs: List[Dict], model_type: str) -> Dict:
        """
        Toplu deneyim çalıştırma ana fonksiyonu
        """
        start_time = time.time()

        with ThreadPoolExecutor(max_workers=self.max_workers) as executor:
            future_to_config = {}

            # Tüm deneyleri gönder
            for i, config in enumerate(experiment_configs):
                future = executor.submit(self._run_single_experiment, config, model_type, i)
                future_to_config[future] = config

            # Sonuçları topla
            completed_count = 0
            for future in as_completed(future_to_config):
                config = future_to_config[future]

                try:
                    result = future.result()

                    with self.lock:
                        if result["success"]:
                            self.batch_results.append(result)
                            print(f"✅ Deneyim {completed_count + 1}/{len(experiment_configs)} tamamlandı")
                        else:
                            self.failed_experiments.append(result)
                            print(f"❌ Deneyim {completed_count + 1}/{len(experiment_configs)} başarısız")

                        completed_count += 1

                        # İlerleme göster
                        progress = (completed_count / len(experiment_configs)) * 100
                        print(f"📊 İlerleme: {progress:.1f}% ({completed_count}/{len(experiment_configs)})")

                except Exception as e:
                    print(f"🚨 Deneyim exception: {str(e)}")
                    with self.lock:
                        self.failed_experiments.append({
                            "config": config,
                            "error": str(e),
                            "success": False
                        })
                        completed_count += 1

        elapsed_time = time.time() - start_time

        # Sonuçları analiz et ve kaydet
        summary = self._create_batch_summary(model_type, self.batch_results, self.failed_experiments)
        summary["total_time_seconds"] = elapsed_time
        summary["experiments_per_minute"] = (len(experiment_configs) / elapsed_time) * 60

        print(f"\n✅ Toplu işlem tamamlandı!")
        print(f"⏱️ Toplam süre: {elapsed_time:.1f}s")
        print(f"🏆 Başarılı: {len(self.batch_results)}")
        print(f"❌ Başarısız: {len(self.failed_experiments)}")

        return summary

    def _run_single_experiment(self, config: Dict, model_type: str, experiment_id: int) -> Dict:
        """
        Tek deneyim çalıştır
        """
        result = {
            "experiment_id": experiment_id,
            "model_type": model_type,
            "config": config,
            "success": False,
            "timestamp": datetime.now().isoformat()
        }

        try:
            # API isteği gönder
            if model_type == "lightgbm":
                api_result = self.api_client.train_lightgbm(config)
            elif model_type == "pca":
                api_result = self.api_client.train_pca(config)
            elif model_type == "ensemble":
                api_result = self.api_client.train_ensemble(config)
            else:
                raise ValueError(f"Bilinmeyen model tipi: {model_type}")

            if api_result and "error" not in api_result:
                result["training_result"] = api_result
                result["success"] = True

                # Gerçek model ismini ekle
                actual_model_name = api_result.get("modelName") or api_result.get("ModelName")
                if actual_model_name:
                    result["actual_model_name"] = actual_model_name

                # Başarılı sonuçları ayrı dosyaya kaydet
                self._save_individual_result(result)
            else:
                result["error"] = api_result.get("error", "Bilinmeyen API hatası")

            # Rate limiting için bekleme
            time.sleep(self.delay)

        except Exception as e:
            result["error"] = str(e)

        return result

    def _run_single_mixed_experiment(self, experiment: Dict, experiment_id: int) -> Dict:
        """
        Karışık model türünde tek deneyim çalıştır
        """
        model_type = experiment.get("type", "lightgbm")
        config = experiment.get("config", {})
        name = experiment.get("name", f"Experiment_{experiment_id}")

        result = {
            "experiment_id": experiment_id,
            "name": name,
            "model_type": model_type,
            "config": config,
            "success": False,
            "timestamp": datetime.now().isoformat()
        }

        try:
            # API isteği gönder
            if model_type == "lightgbm":
                api_result = self.api_client.train_lightgbm(config)
            elif model_type == "pca":
                api_result = self.api_client.train_pca(config)
            elif model_type == "ensemble":
                api_result = self.api_client.train_ensemble(config)
            else:
                raise ValueError(f"Bilinmeyen model tipi: {model_type}")

            if api_result and "error" not in api_result:
                result["training_result"] = api_result
                result["success"] = True

                # Gerçek model ismini ekle
                actual_model_name = api_result.get("modelName") or api_result.get("ModelName")
                if actual_model_name:
                    result["actual_model_name"] = actual_model_name

                self._save_individual_result(result)
            else:
                result["error"] = api_result.get("error", "Bilinmeyen API hatası")

            time.sleep(self.delay)

        except Exception as e:
            result["error"] = str(e)

        return result

    def _save_individual_result(self, result: Dict):
        """
        Bireysel sonucu kaydet
        """
        filename = f"result_{result['model_type']}_{result['experiment_id']}_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        filepath = os.path.join(self.output_dir, "individual_results", filename)

        with open(filepath, 'w') as f:
            json.dump(result, f, indent=2, default=str)

    def _create_batch_summary(self, model_type: str, successful_results: List[Dict],
                              failed_results: List[Dict]) -> Dict:
        """
        Toplu işlem özeti oluştur
        """
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

        summary = {
            "batch_info": {
                "model_type": model_type,
                "timestamp": timestamp,
                "total_experiments": len(successful_results) + len(failed_results),
                "successful_experiments": len(successful_results),
                "failed_experiments": len(failed_results),
                "success_rate": len(successful_results) / (len(successful_results) + len(failed_results)) * 100 if (
                                                                                                                               len(successful_results) + len(
                                                                                                                           failed_results)) > 0 else 0
            },
            "performance_analysis": {},
            "best_configurations": {},
            "recommendations": []
        }

        if successful_results:
            summary["performance_analysis"] = self._analyze_batch_performance(successful_results)
            summary["best_configurations"] = self._find_best_configurations(successful_results)
            summary["recommendations"] = self._generate_batch_recommendations(successful_results)

        # Başarısız deneyleri analiz et
        if failed_results:
            summary["failure_analysis"] = self._analyze_failures(failed_results)

        # Özeti kaydet
        summary_file = os.path.join(self.output_dir, "summary", f"batch_summary_{model_type}_{timestamp}.json")
        with open(summary_file, 'w') as f:
            json.dump(summary, f, indent=2, default=str)

        print(f"📊 Özet kaydedildi: {summary_file}")

        return summary

    def _analyze_batch_performance(self, results: List[Dict]) -> Dict:
        """
        Toplu performans analizi
        """
        metrics_data = []

        for result in results:
            try:
                basic_metrics = result.get("training_result", {}).get("BasicMetrics", {})

                if basic_metrics:
                    metrics_data.append({
                        "accuracy": basic_metrics.get("Accuracy", 0),
                        "precision": basic_metrics.get("Precision", 0),
                        "recall": basic_metrics.get("Recall", 0),
                        "f1_score": basic_metrics.get("F1Score", 0),
                        "auc": basic_metrics.get("AUC", 0)
                    })
            except:
                continue

        if not metrics_data:
            return {"error": "Analiz edilebilir metrik bulunamadı"}

        df = pd.DataFrame(metrics_data)

        analysis = {
            "statistics": {
                "mean": df.mean().to_dict(),
                "std": df.std().to_dict(),
                "min": df.min().to_dict(),
                "max": df.max().to_dict(),
                "median": df.median().to_dict()
            },
            "correlations": df.corr().to_dict(),
            "best_metric_values": {
                "highest_accuracy": df["accuracy"].max(),
                "highest_precision": df["precision"].max(),
                "highest_recall": df["recall"].max(),
                "highest_f1": df["f1_score"].max(),
                "highest_auc": df["auc"].max()
            }
        }

        return analysis

    def _find_best_configurations(self, results: List[Dict]) -> Dict:
        """
        En iyi konfigürasyonları bul
        """
        best_configs = {
            "by_accuracy": None,
            "by_precision": None,
            "by_recall": None,
            "by_f1_score": None,
            "by_auc": None,
            "overall_best": None
        }

        best_scores = {
            "accuracy": -1,
            "precision": -1,
            "recall": -1,
            "f1_score": -1,
            "auc": -1,
            "overall": -1
        }

        for result in results:
            try:
                basic_metrics = result.get("training_result", {}).get("BasicMetrics", {})
                config = result.get("config", {})

                if not basic_metrics:
                    continue

                # Her metrik için en iyiyi bul
                metrics = {
                    "accuracy": basic_metrics.get("Accuracy", 0),
                    "precision": basic_metrics.get("Precision", 0),
                    "recall": basic_metrics.get("Recall", 0),
                    "f1_score": basic_metrics.get("F1Score", 0),
                    "auc": basic_metrics.get("AUC", 0)
                }

                # Genel skor hesapla
                overall_score = (metrics["accuracy"] + metrics["f1_score"] + metrics["auc"]) / 3
                metrics["overall"] = overall_score

                for metric_name, score in metrics.items():
                    if score > best_scores[metric_name]:
                        best_scores[metric_name] = score

                        if metric_name == "overall":
                            best_configs["overall_best"] = {
                                "config": config,
                                "score": score,
                                "all_metrics": basic_metrics
                            }
                        else:
                            best_configs[f"by_{metric_name}"] = {
                                "config": config,
                                "score": score,
                                "all_metrics": basic_metrics
                            }

            except Exception as e:
                print(f"⚠️ Konfigürasyon analizi hatası: {str(e)}")
                continue

        return best_configs

    def _generate_batch_recommendations(self, results: List[Dict]) -> List[str]:
        """
        Toplu sonuçlara göre öneriler oluştur
        """
        recommendations = []

        try:
            # Performans analizi
            analysis = self._analyze_batch_performance(results)
            stats = analysis.get("statistics", {})

            if stats:
                mean_accuracy = stats.get("mean", {}).get("accuracy", 0)
                mean_f1 = stats.get("mean", {}).get("f1_score", 0)

                if mean_accuracy > 0.95:
                    recommendations.append("Modeller genel olarak yüksek accuracy değerleri gösteriyor")
                elif mean_accuracy < 0.8:
                    recommendations.append("Accuracy değerleri düşük - model karmaşıklığını artırın")

                if mean_f1 > 0.8:
                    recommendations.append("F1-Score değerleri tatmin edici seviyede")
                elif mean_f1 < 0.7:
                    recommendations.append("F1-Score düşük - class balancing stratejilerini gözden geçirin")

                # Standart sapma analizi
                std_accuracy = stats.get("std", {}).get("accuracy", 0)
                if std_accuracy > 0.05:
                    recommendations.append("Accuracy değerlerinde yüksek varyans - konfigürasyonları stabilize edin")

        except Exception as e:
            recommendations.append(f"Otomatik öneri oluşturulurken hata: {str(e)}")

        # Genel öneriler
        if len(results) > 10:
            recommendations.append("Büyük deneyim setinde en iyi 3-5 konfigürasyonu seçin")

        recommendations.append("En iyi performans gösteren konfigürasyonları production'da test edin")

        return recommendations

    def _analyze_failures(self, failed_results: List[Dict]) -> Dict:
        """
        Başarısız deneyleri analiz et
        """
        if not failed_results:
            return {}

        error_types = {}
        error_patterns = []

        for failure in failed_results:
            error = failure.get("error", "Bilinmeyen hata")

            # Hata tiplerini kategorize et
            if "timeout" in error.lower():
                error_types["timeout"] = error_types.get("timeout", 0) + 1
            elif "connection" in error.lower():
                error_types["connection"] = error_types.get("connection", 0) + 1
            elif "memory" in error.lower():
                error_types["memory"] = error_types.get("memory", 0) + 1
            elif "api" in error.lower():
                error_types["api"] = error_types.get("api", 0) + 1
            else:
                error_types["other"] = error_types.get("other", 0) + 1

            error_patterns.append(error)

        analysis = {
            "total_failures": len(failed_results),
            "error_types": error_types,
            "most_common_error": max(error_types.items(), key=lambda x: x[1]) if error_types else None,
            "unique_errors": len(set(error_patterns)),
            "suggestions": []
        }

        # Hata tipine göre öneriler
        if error_types.get("timeout", 0) > len(failed_results) * 0.3:
            analysis["suggestions"].append("Çok fazla timeout hatası - API timeout süresini artırın")

        if error_types.get("memory", 0) > 0:
            analysis["suggestions"].append("Memory hataları - model karmaşıklığını azaltın")

        if error_types.get("connection", 0) > 0:
            analysis["suggestions"].append("Bağlantı hataları - API'nin stabil olduğundan emin olun")

        return analysis


# Utility functions for creating experiment configurations
class ExperimentGenerator:
    """
    Deneyim konfigürasyonları oluşturucu
    """

    @staticmethod
    def generate_lightgbm_grid_experiments(param_ranges: Dict[str, List]) -> List[Dict]:
        """
        LightGBM grid search deneyleri oluştur
        """
        from itertools import product

        # Parametre kombinasyonları oluştur
        param_names = list(param_ranges.keys())
        param_values = list(param_ranges.values())

        experiments = []

        for combination in product(*param_values):
            params = dict(zip(param_names, combination))

            # Base config ile birleştir
            config = ConfigurationGenerator.get_lightgbm_config("default")

            # Parametreleri güncelle
            if "numberOfTrees" in params:
                config["numberOfTrees"] = params["numberOfTrees"]
            if "numberOfLeaves" in params:
                config["numberOfLeaves"] = params["numberOfLeaves"]
            if "learningRate" in params:
                config["learningRate"] = params["learningRate"]
            if "classWeightRatio" in params:
                config["classWeights"]["1"] = float(params["classWeightRatio"])

            experiments.append(config)

        return experiments

    @staticmethod
    def generate_random_experiments(model_type: str, count: int = 20) -> List[Dict]:
        """
        Rastgele deneyim konfigürasyonları oluştur
        """
        import random

        experiments = []

        for i in range(count):
            if model_type == "lightgbm":
                config = ConfigurationGenerator.get_lightgbm_config("default")

                # Rastgele parametreler
                config["numberOfTrees"] = random.choice([500, 800, 1000, 1500, 2000])
                config["numberOfLeaves"] = random.choice([64, 96, 128, 192, 256])
                config["learningRate"] = random.choice([0.002, 0.005, 0.01, 0.02])
                config["classWeights"]["1"] = random.choice([30, 50, 75, 100, 120])

            elif model_type == "pca":
                config = ConfigurationGenerator.get_pca_config("default")

                config["componentCount"] = random.choice([10, 15, 20, 25, 30])
                config["anomalyThreshold"] = random.choice([1.5, 2.0, 2.5, 3.0, 3.5])

            elif model_type == "ensemble":
                config = ConfigurationGenerator.get_ensemble_config("default")

                config["lightgbmWeight"] = random.choice([0.6, 0.7, 0.8, 0.85])
                config["pcaWeight"] = 1.0 - config["lightgbmWeight"]
                config["threshold"] = random.choice([0.4, 0.45, 0.5, 0.55, 0.6])

            experiments.append(config)

        return experiments

    @staticmethod
    def create_preset_experiments() -> List[Dict]:
        """
        Önceden tanımlanmış karışık deneyimler oluştur
        """
        experiments = [
            {
                "name": "LightGBM_Fast_Tuned",
                "type": "lightgbm",
                "config": {
                    "numberOfTrees": 800,
                    "numberOfLeaves": 100,
                    "learningRate": 0.01,
                    "classWeights": {"0": 1.0, "1": 60.0}
                }
            },
            {
                "name": "LightGBM_Accurate_Tuned",
                "type": "lightgbm",
                "config": {
                    "numberOfTrees": 1500,
                    "numberOfLeaves": 200,
                    "learningRate": 0.005,
                    "classWeights": {"0": 1.0, "1": 100.0}
                }
            },
            {
                "name": "PCA_Sensitive",
                "type": "pca",
                "config": {
                    "componentCount": 25,
                    "anomalyThreshold": 2.0
                }
            },
            {
                "name": "PCA_Conservative",
                "type": "pca",
                "config": {
                    "componentCount": 15,
                    "anomalyThreshold": 3.0
                }
            },
            {
                "name": "Ensemble_Balanced_Custom",
                "type": "ensemble",
                "config": {
                    "lightgbmWeight": 0.75,
                    "pcaWeight": 0.25,
                    "threshold": 0.45
                }
            }
        ]

        return experiments


# Test fonksiyonu
def run_batch_test():
    """
    Batch processor test et
    """
    # API Client
    client = FraudDetectionAPIClient("http://localhost:5000")

    if not client.health_check():
        print("❌ API'ye bağlanılamıyor!")
        return

    # Batch processor
    processor = BatchModelProcessor(client, "batch_test_results", max_workers=2, delay_between_requests=3.0)

    print("🧪 Batch processor test ediliyor...")

    # Basit LightGBM deneyleri
    lgbm_configs = ExperimentGenerator.generate_random_experiments("lightgbm", 5)

    print(f"📊 {len(lgbm_configs)} LightGBM deneyi çalıştırılıyor...")
    results = processor.run_lightgbm_experiments(lgbm_configs)

    print("✅ Batch test tamamlandı!")
    print(f"Başarılı: {results['batch_info']['successful_experiments']}")
    print(f"Başarısız: {results['batch_info']['failed_experiments']}")


if __name__ == "__main__":
    run_batch_test()