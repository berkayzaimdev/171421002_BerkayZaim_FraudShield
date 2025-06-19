import os
import json
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime
from typing import Dict, List, Any, Optional, Tuple
from itertools import product
import time
import warnings

warnings.filterwarnings('ignore')

from api_client import FraudDetectionAPIClient, ConfigurationGenerator


class HyperparameterTuner:
    """
    Model hiperparametreleri optimize eden sınıf
    """

    def __init__(self, api_client: FraudDetectionAPIClient, output_dir: str = "tuning_results"):
        """
        Hyperparameter Tuner'ı başlat

        Args:
            api_client: API client instance
            output_dir: Sonuçların kaydedileceği dizin
        """
        self.api_client = api_client
        self.output_dir = output_dir

        # Output dizinleri oluştur
        os.makedirs(output_dir, exist_ok=True)
        os.makedirs(f"{output_dir}/charts", exist_ok=True)
        os.makedirs(f"{output_dir}/configs", exist_ok=True)

        # Tuning sonuçları
        self.tuning_results = []

        print(f"🔧 Hyperparameter Tuner başlatıldı: {output_dir}")

    def tune_lightgbm(self,
                      param_grid: Dict[str, List] = None,
                      max_experiments: int = 20,
                      optimization_metric: str = "f1_score") -> Dict:
        """
        LightGBM hiperparametrelerini optimize et

        Args:
            param_grid: Parametre arama uzayı
            max_experiments: Maksimum deneme sayısı
            optimization_metric: Optimize edilecek metrik

        Returns:
            En iyi konfigürasyon ve sonuçlar
        """
        print(f"🔍 LightGBM hiperparametre optimizasyonu başlatılıyor...")
        print(f"📊 Metrik: {optimization_metric}, Max Deneme: {max_experiments}")

        if param_grid is None:
            param_grid = self._get_lightgbm_param_grid()

        # Grid search veya random search
        param_combinations = self._generate_param_combinations(param_grid, max_experiments)

        results = []
        best_score = -1
        best_config = None

        for i, params in enumerate(param_combinations):
            print(f"\n🔄 Deneme {i + 1}/{len(param_combinations)}")
            print(f"Parametreler: {params}")

            try:
                # Konfigürasyon oluştur
                config = self._create_lightgbm_config(params)

                # Model eğit
                result = self.api_client.train_lightgbm(config)

                if result and "error" not in result:
                    # Skoru al
                    score = self._extract_score(result, optimization_metric)

                    # Gerçek model ismini al - response'da "modelName" field'ı var
                    actual_model_name = result.get("modelName") or result.get("ModelName", f"lightgbm_exp_{i + 1}")

                    # Sonucu kaydet
                    experiment_result = {
                        "experiment_id": i + 1,
                        "parameters": params,
                        "config": config,
                        "training_result": result,
                        "actual_model_name": actual_model_name,
                        "score": score,
                        "timestamp": datetime.now().isoformat()
                    }

                    results.append(experiment_result)

                    print(f"✅ Skor: {score:.4f}")

                    # En iyi skor kontrolü
                    if score > best_score:
                        best_score = score
                        best_config = config
                        print(f"🏆 Yeni en iyi skor: {best_score:.4f}")

                else:
                    print(f"❌ Eğitim başarısız: {result.get('error', 'Bilinmeyen hata')}")

            except Exception as e:
                print(f"🚨 Deneme {i + 1} hatası: {str(e)}")

            # Kısa bekleme
            time.sleep(2)

        # Sonuçları analiz et ve kaydet
        tuning_summary = self._analyze_tuning_results(results, "LightGBM", optimization_metric)

        # Görselleştirmeler oluştur
        self._create_tuning_visualizations(results, "LightGBM", optimization_metric)

        return tuning_summary

    def tune_pca(self,
                 param_grid: Dict[str, List] = None,
                 max_experiments: int = 15,
                 optimization_metric: str = "accuracy") -> Dict:
        """
        PCA hiperparametrelerini optimize et
        """
        print(f"🔍 PCA hiperparametre optimizasyonu başlatılıyor...")

        if param_grid is None:
            param_grid = self._get_pca_param_grid()

        param_combinations = self._generate_param_combinations(param_grid, max_experiments)

        results = []
        best_score = -1
        best_config = None

        for i, params in enumerate(param_combinations):
            print(f"\n🔄 Deneme {i + 1}/{len(param_combinations)}")

            try:
                config = self._create_pca_config(params)
                result = self.api_client.train_pca(config)

                if result and "error" not in result:
                    score = self._extract_score(result, optimization_metric)
                    actual_model_name = result.get("modelName") or result.get("ModelName", f"pca_exp_{i + 1}")

                    experiment_result = {
                        "experiment_id": i + 1,
                        "parameters": params,
                        "config": config,
                        "training_result": result,
                        "actual_model_name": actual_model_name,
                        "score": score,
                        "timestamp": datetime.now().isoformat()
                    }

                    results.append(experiment_result)
                    print(f"✅ Skor: {score:.4f}")

                    if score > best_score:
                        best_score = score
                        best_config = config
                        print(f"🏆 Yeni en iyi skor: {best_score:.4f}")

            except Exception as e:
                print(f"🚨 Deneme {i + 1} hatası: {str(e)}")

            time.sleep(2)

        tuning_summary = self._analyze_tuning_results(results, "PCA", optimization_metric)
        self._create_tuning_visualizations(results, "PCA", optimization_metric)

        return tuning_summary

    def tune_ensemble(self,
                      lightgbm_grid: Dict = None,
                      pca_grid: Dict = None,
                      ensemble_grid: Dict = None,
                      max_experiments: int = 25) -> Dict:
        """
        Ensemble hiperparametrelerini optimize et
        """
        print(f"🔍 Ensemble hiperparametre optimizasyonu başlatılıyor...")

        if ensemble_grid is None:
            ensemble_grid = {
                "lightgbm_weight": [0.6, 0.7, 0.8, 0.85],
                "pca_weight": [0.15, 0.2, 0.3, 0.4],
                "threshold": [0.4, 0.45, 0.5, 0.55]
            }

        # Alt model konfigürasyonları
        if lightgbm_grid is None:
            lightgbm_configs = [
                ConfigurationGenerator.get_lightgbm_config("balanced"),
                ConfigurationGenerator.get_lightgbm_config("accurate")
            ]
        else:
            lightgbm_configs = [self._create_lightgbm_config(params)
                                for params in self._generate_param_combinations(lightgbm_grid, 3)]

        if pca_grid is None:
            pca_configs = [
                ConfigurationGenerator.get_pca_config("default"),
                ConfigurationGenerator.get_pca_config("sensitive")
            ]
        else:
            pca_configs = [self._create_pca_config(params)
                           for params in self._generate_param_combinations(pca_grid, 3)]

        # Ensemble parametreleri
        ensemble_combinations = self._generate_param_combinations(ensemble_grid, max_experiments)

        results = []
        best_score = -1
        best_config = None

        for i, ensemble_params in enumerate(ensemble_combinations):
            print(f"\n🔄 Ensemble Deneme {i + 1}/{len(ensemble_combinations)}")

            try:
                # Ensemble konfigürasyonu oluştur
                config = {
                    "lightgbmWeight": ensemble_params["lightgbm_weight"],
                    "pcaWeight": ensemble_params["pca_weight"],
                    "threshold": ensemble_params["threshold"],
                    "lightgbm": lightgbm_configs[i % len(lightgbm_configs)],
                    "pca": pca_configs[i % len(pca_configs)]
                }

                result = self.api_client.train_ensemble(config)

                if result and "error" not in result:
                    score = self._extract_score(result, "f1_score")
                    actual_model_name = result.get("modelName") or result.get("ModelName", f"ensemble_exp_{i + 1}")

                    experiment_result = {
                        "experiment_id": i + 1,
                        "parameters": ensemble_params,
                        "config": config,
                        "training_result": result,
                        "actual_model_name": actual_model_name,
                        "score": score,
                        "timestamp": datetime.now().isoformat()
                    }

                    results.append(experiment_result)
                    print(f"✅ F1-Score: {score:.4f}")

                    if score > best_score:
                        best_score = score
                        best_config = config
                        print(f"🏆 Yeni en iyi skor: {best_score:.4f}")

            except Exception as e:
                print(f"🚨 Deneme {i + 1} hatası: {str(e)}")

            time.sleep(3)  # Ensemble eğitimi daha uzun sürer

        tuning_summary = self._analyze_tuning_results(results, "Ensemble", "f1_score")
        self._create_tuning_visualizations(results, "Ensemble", "f1_score")

        return tuning_summary

    def _get_lightgbm_param_grid(self) -> Dict[str, List]:
        """LightGBM parametre arama uzayı"""
        return {
            "n_estimators": [500, 1000, 1500, 2000],
            "num_leaves": [64, 128, 256, 512],
            "learning_rate": [0.002, 0.005, 0.01, 0.02],
            "feature_fraction": [0.7, 0.8, 0.9],
            "bagging_fraction": [0.7, 0.8, 0.9],
            "min_child_samples": [5, 10, 20, 30],
            "reg_alpha": [0.001, 0.01, 0.1],
            "reg_lambda": [0.001, 0.01, 0.1],
            "class_weight_ratio": [50, 75, 100, 150]  # 1 sınıfının ağırlığı
        }

    def _get_pca_param_grid(self) -> Dict[str, List]:
        """PCA parametre arama uzayı"""
        return {
            "n_components": [10, 15, 20, 25, 30],
            "anomaly_threshold": [1.5, 2.0, 2.5, 3.0, 3.5]
        }

    def _generate_param_combinations(self, param_grid: Dict, max_combinations: int) -> List[Dict]:
        """Parametre kombinasyonları oluştur"""

        # Tüm kombinasyonları oluştur
        keys = list(param_grid.keys())
        values = list(param_grid.values())

        all_combinations = []
        for combination in product(*values):
            param_dict = dict(zip(keys, combination))
            all_combinations.append(param_dict)

        # Maksimum sayıda kombinasyon seç
        if len(all_combinations) > max_combinations:
            # Random sampling
            import random
            random.shuffle(all_combinations)
            all_combinations = all_combinations[:max_combinations]

        print(f"📋 {len(all_combinations)} parametre kombinasyonu oluşturuldu")
        return all_combinations

    def _create_lightgbm_config(self, params: Dict) -> Dict:
        """Parametrelerden LightGBM konfigürasyonu oluştur"""

        base_config = ConfigurationGenerator.get_lightgbm_config("default")

        # Parametreleri güncelle
        if "n_estimators" in params:
            base_config["numberOfTrees"] = params["n_estimators"]
        if "num_leaves" in params:
            base_config["numberOfLeaves"] = params["num_leaves"]
        if "learning_rate" in params:
            base_config["learningRate"] = params["learning_rate"]
        if "feature_fraction" in params:
            base_config["featureFraction"] = params["feature_fraction"]
        if "bagging_fraction" in params:
            base_config["baggingFraction"] = params["bagging_fraction"]
        if "min_child_samples" in params:
            base_config["minDataInLeaf"] = params["min_child_samples"]
        if "reg_alpha" in params:
            base_config["l1Regularization"] = params["reg_alpha"]
        if "reg_lambda" in params:
            base_config["l2Regularization"] = params["reg_lambda"]
        if "class_weight_ratio" in params:
            base_config["classWeights"]["1"] = float(params["class_weight_ratio"])

        return base_config

    def _create_pca_config(self, params: Dict) -> Dict:
        """Parametrelerden PCA konfigürasyonu oluştur"""

        base_config = ConfigurationGenerator.get_pca_config("default")

        if "n_components" in params:
            base_config["componentCount"] = params["n_components"]
        if "anomaly_threshold" in params:
            base_config["anomalyThreshold"] = params["anomaly_threshold"]

        return base_config

    def _extract_score(self, training_result: Dict, metric: str) -> float:
        """Eğitim sonucundan skoru çıkar"""

        # API response'unda "basicMetrics" altında metrikler var
        basic_metrics = training_result.get("basicMetrics", {})

        if metric == "accuracy":
            return basic_metrics.get("accuracy", 0)
        elif metric == "precision":
            return basic_metrics.get("precision", 0)
        elif metric == "recall":
            return basic_metrics.get("recall", 0)
        elif metric == "f1_score":
            return basic_metrics.get("f1Score", 0)  # Dikkat: camelCase
        elif metric == "auc":
            return basic_metrics.get("auc", 0)
        else:
            # Genel skor hesapla
            return (basic_metrics.get("accuracy", 0) +
                    basic_metrics.get("f1Score", 0) +
                    basic_metrics.get("auc", 0)) / 3

    def _analyze_tuning_results(self, results: List[Dict], model_type: str, metric: str) -> Dict:
        """Tuning sonuçlarını analiz et"""

        if not results:
            return {"error": "Hiç başarılı deneme bulunamadı"}

        # En iyi sonuçları bul
        best_result = max(results, key=lambda x: x["score"])
        worst_result = min(results, key=lambda x: x["score"])

        # İstatistikler
        scores = [r["score"] for r in results]

        summary = {
            "model_type": model_type,
            "optimization_metric": metric,
            "total_experiments": len(results),
            "best_score": best_result["score"],
            "worst_score": worst_result["score"],
            "mean_score": np.mean(scores),
            "std_score": np.std(scores),
            "best_config": best_result["config"],
            "best_parameters": best_result["parameters"],
            "all_results": results,
            "timestamp": datetime.now().isoformat()
        }

        # Parametre önemleri
        parameter_importance = self._analyze_parameter_importance(results)
        summary["parameter_importance"] = parameter_importance

        # Sonuçları kaydet
        self._save_tuning_results(summary, model_type)

        print(f"\n📊 {model_type} Tuning Özeti:")
        print(f"En İyi Skor: {best_result['score']:.4f}")
        print(f"Ortalama Skor: {np.mean(scores):.4f}")
        print(f"Standart Sapma: {np.std(scores):.4f}")
        print(f"En İyi Parametreler: {best_result['parameters']}")

        return summary

    def _analyze_parameter_importance(self, results: List[Dict]) -> Dict:
        """Parametre önemlerini analiz et"""

        if len(results) < 3:
            return {}

        # DataFrame oluştur
        data = []
        for result in results:
            row = result["parameters"].copy()
            row["score"] = result["score"]
            data.append(row)

        df = pd.DataFrame(data)

        # Korelasyonları hesapla
        importance = {}

        for param in df.columns:
            if param != "score" and df[param].dtype in ['int64', 'float64']:
                try:
                    correlation = df[param].corr(df["score"])
                    if not pd.isna(correlation):
                        importance[param] = abs(correlation)
                except:
                    pass

        # Önem sırasına göre sırala
        importance = dict(sorted(importance.items(), key=lambda x: x[1], reverse=True))

        return importance

    def _create_tuning_visualizations(self, results: List[Dict], model_type: str, metric: str):
        """Tuning görselleştirmeleri oluştur"""

        if not results:
            return

        try:
            # Skor dağılımı
            self._plot_score_distribution(results, model_type, metric)

            # Parametre önemleri
            self._plot_parameter_importance(results, model_type)

            # Skor gelişimi
            self._plot_score_evolution(results, model_type, metric)

            # Parametre scatter plots
            self._plot_parameter_scatter(results, model_type, metric)

            print(f"✅ {model_type} tuning görselleştirmeleri oluşturuldu")

        except Exception as e:
            print(f"⚠️ Görselleştirme hatası: {str(e)}")

    def _plot_score_distribution(self, results: List[Dict], model_type: str, metric: str):
        """Skor dağılımı histogramı"""

        scores = [r["score"] for r in results]

        plt.figure(figsize=(10, 6))
        plt.hist(scores, bins=min(10, len(scores) // 2), alpha=0.7, color='skyblue', edgecolor='black')
        plt.axvline(np.mean(scores), color='red', linestyle='--', linewidth=2, label=f'Ortalama: {np.mean(scores):.3f}')
        plt.axvline(max(scores), color='green', linestyle='--', linewidth=2, label=f'En İyi: {max(scores):.3f}')

        plt.title(f'{model_type} - {metric.upper()} Skor Dağılımı', fontsize=14, fontweight='bold')
        plt.xlabel(f'{metric.upper()} Skoru', fontsize=12)
        plt.ylabel('Frekans', fontsize=12)
        plt.legend()
        plt.grid(alpha=0.3)

        plt.tight_layout()
        plt.savefig(f'{self.output_dir}/charts/{model_type.lower()}_score_distribution.png',
                    dpi=300, bbox_inches='tight')
        plt.close()

    def _plot_parameter_importance(self, results: List[Dict], model_type: str):
        """Parametre önemleri bar chart"""

        importance = self._analyze_parameter_importance(results)

        if not importance:
            return

        params = list(importance.keys())
        values = list(importance.values())

        plt.figure(figsize=(12, 6))
        bars = plt.bar(params, values, color='coral', alpha=0.7)

        # Değerleri bar'ların üzerine yaz
        for bar, value in zip(bars, values):
            plt.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 0.005,
                     f'{value:.3f}', ha='center', va='bottom', fontweight='bold')

        plt.title(f'{model_type} - Parametre Önem Analizi (Korelasyon)', fontsize=14, fontweight='bold')
        plt.xlabel('Parametreler', fontsize=12)
        plt.ylabel('Mutlak Korelasyon', fontsize=12)
        plt.xticks(rotation=45, ha='right')
        plt.grid(axis='y', alpha=0.3)

        plt.tight_layout()
        plt.savefig(f'{self.output_dir}/charts/{model_type.lower()}_parameter_importance.png',
                    dpi=300, bbox_inches='tight')
        plt.close()

    def _plot_score_evolution(self, results: List[Dict], model_type: str, metric: str):
        """Skor gelişimi line chart"""

        experiment_ids = [r["experiment_id"] for r in results]
        scores = [r["score"] for r in results]

        # Running best scores
        running_best = []
        current_best = -1
        for score in scores:
            if score > current_best:
                current_best = score
            running_best.append(current_best)

        plt.figure(figsize=(12, 6))
        plt.plot(experiment_ids, scores, 'b-o', alpha=0.6, label='Deneme Skorları')
        plt.plot(experiment_ids, running_best, 'r-', linewidth=2, label='En İyi Skor')

        plt.title(f'{model_type} - Optimizasyon Süreci', fontsize=14, fontweight='bold')
        plt.xlabel('Deneme Numarası', fontsize=12)
        plt.ylabel(f'{metric.upper()} Skoru', fontsize=12)
        plt.legend()
        plt.grid(alpha=0.3)

        plt.tight_layout()
        plt.savefig(f'{self.output_dir}/charts/{model_type.lower()}_score_evolution.png',
                    dpi=300, bbox_inches='tight')
        plt.close()

    def _plot_parameter_scatter(self, results: List[Dict], model_type: str, metric: str):
        """Parametre scatter plots"""

        # DataFrame oluştur
        data = []
        for result in results:
            row = result["parameters"].copy()
            row["score"] = result["score"]
            data.append(row)

        df = pd.DataFrame(data)
        numeric_params = [col for col in df.columns if col != "score" and df[col].dtype in ['int64', 'float64']]

        if len(numeric_params) < 2:
            return

        # En önemli 4 parametreyi seç
        importance = self._analyze_parameter_importance(results)
        top_params = list(importance.keys())[:4] if importance else numeric_params[:4]

        fig, axes = plt.subplots(2, 2, figsize=(15, 12))
        axes = axes.flatten()

        for i, param in enumerate(top_params[:4]):
            if param in df.columns:
                ax = axes[i]
                scatter = ax.scatter(df[param], df["score"], c=df["score"],
                                     cmap='viridis', alpha=0.7, s=50)
                ax.set_xlabel(param, fontsize=10)
                ax.set_ylabel(f'{metric.upper()} Score', fontsize=10)
                ax.set_title(f'{param} vs {metric.upper()}', fontsize=12)
                ax.grid(alpha=0.3)

                # Colorbar
                plt.colorbar(scatter, ax=ax, label='Score')

        # Boş subplotları gizle
        for i in range(len(top_params), 4):
            axes[i].set_visible(False)

        plt.suptitle(f'{model_type} - Parametre Scatter Plots', fontsize=16, fontweight='bold')
        plt.tight_layout()
        plt.savefig(f'{self.output_dir}/charts/{model_type.lower()}_parameter_scatter.png',
                    dpi=300, bbox_inches='tight')
        plt.close()

    def _save_tuning_results(self, summary: Dict, model_type: str):
        """Tuning sonuçlarını kaydet"""

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

        # JSON dosyası
        json_file = f"{self.output_dir}/tuning_results_{model_type.lower()}_{timestamp}.json"
        with open(json_file, 'w') as f:
            json.dump(summary, f, indent=2, default=str)

        # En iyi konfigürasyonu ayrı kaydet
        best_config_file = f"{self.output_dir}/configs/best_{model_type.lower()}_config_{timestamp}.json"
        with open(best_config_file, 'w') as f:
            json.dump(summary["best_config"], f, indent=2)

        print(f"💾 Tuning sonuçları kaydedildi: {json_file}")
        print(f"⚙️ En iyi konfigürasyon: {best_config_file}")


# Test fonksiyonu
def run_comprehensive_tuning():
    """Kapsamlı hyperparameter tuning çalıştır"""

    # API Client
    client = FraudDetectionAPIClient("http://localhost:5000")

    if not client.health_check():
        print("❌ API'ye bağlanılamıyor!")
        return

    # Tuner
    tuner = HyperparameterTuner(client, "hyperparameter_results")

    print("🚀 Kapsamlı hiperparametre optimizasyonu başlatılıyor...")

    # LightGBM tuning
    print("\n" + "=" * 50)
    print("🔧 LightGBM Optimizasyonu")
    print("=" * 50)

    lightgbm_results = tuner.tune_lightgbm(max_experiments=10)

    # PCA tuning
    print("\n" + "=" * 50)
    print("🔧 PCA Optimizasyonu")
    print("=" * 50)

    pca_results = tuner.tune_pca(max_experiments=8)

    # Ensemble tuning (en iyi LightGBM ve PCA konfigürasyonları ile)
    print("\n" + "=" * 50)
    print("🔧 Ensemble Optimizasyonu")
    print("=" * 50)

    ensemble_results = tuner.tune_ensemble(max_experiments=12)

    print("\n🎉 Tüm optimizasyonlar tamamlandı!")
    print(f"📊 Sonuçlar: {tuner.output_dir}")


if __name__ == "__main__":
    run_comprehensive_tuning()