import json
import os

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime
from typing import Dict, List, Any, Optional
import warnings

warnings.filterwarnings('ignore')

# Kendi modüllerimizi import et
from api_client import FraudDetectionAPIClient, ConfigurationGenerator


class ModelReporter:
    """
    Model performansı raporlama ve görselleştirme sınıfı
    """

    def __init__(self, api_client: FraudDetectionAPIClient, output_dir: str = "reports"):
        """
        Model Reporter'ı başlat

        Args:
            api_client: API client instance
            output_dir: Raporların kaydedileceği dizin
        """
        self.api_client = api_client
        self.output_dir = output_dir

        # Output dizini oluştur
        os.makedirs(output_dir, exist_ok=True)
        os.makedirs(f"{output_dir}/charts", exist_ok=True)
        os.makedirs(f"{output_dir}/data", exist_ok=True)

        # Matplotlib ayarları
        plt.style.use('seaborn-v0_8')
        sns.set_palette("husl")

        print(f"📊 Model Reporter başlatıldı: {output_dir}")

    def create_full_report(self, model_configs: Dict[str, Dict] = None) -> str:
        """
        Tüm modeller için kapsamlı rapor oluştur

        Args:
            model_configs: Model konfigürasyonları

        Returns:
            Rapor dosyasının yolu
        """
        print("🔄 Kapsamlı model raporu oluşturuluyor...")

        if model_configs is None:
            model_configs = self._get_default_configs()

        report_data = {
            "timestamp": datetime.now().isoformat(),
            "models": {},
            "comparisons": {},
            "recommendations": []
        }

        # Her model için eğitim ve analiz
        trained_models = []

        for model_name, config in model_configs.items():
            print(f"\n📈 {model_name} modeli işleniyor...")

            try:
                # Model eğit
                model_result = self._train_model(model_name, config)

                if model_result and "error" not in model_result:
                    # Metrikleri al - artık model_result'ı geçiyoruz
                    metrics = self._get_model_metrics(model_result)

                    if metrics and "error" not in metrics:
                        # Rapor verisi ekle
                        report_data["models"][model_name] = {
                            "training_result": model_result,
                            "detailed_metrics": metrics,
                            "config": config,
                            "actual_model_name": model_result.get("ActualModelName", model_name)
                        }

                        # Gerçek model ismini trained_models'e ekle
                        actual_name = model_result.get("ActualModelName", model_name)
                        trained_models.append(actual_name)

                        # Görselleştirmeler oluştur
                        self._create_model_visualizations(model_name, metrics)
                    else:
                        print(f"❌ {model_name} metrikleri alınamadı: {metrics.get('error', 'Bilinmeyen hata')}")
                        report_data["models"][model_name] = {
                            "training_result": model_result,
                            "metrics_error": metrics.get('error', 'Metrik hatası'),
                            "config": config
                        }

                else:
                    print(f"❌ {model_name} eğitimi başarısız")

            except Exception as e:
                print(f"🚨 {model_name} işlenirken hata: {str(e)}")
                report_data["models"][model_name] = {"error": str(e)}

        # Model karşılaştırması
        if len(trained_models) >= 2:
            print(f"\n🔄 {len(trained_models)} model karşılaştırılıyor...")
            comparison = self.api_client.compare_models(trained_models)

            if comparison and "error" not in comparison:
                report_data["comparisons"] = comparison
                self._create_comparison_visualizations(comparison)

        # Önerileri oluştur
        report_data["recommendations"] = self._generate_recommendations(report_data["models"])

        # Raporu kaydet
        report_file = self._save_report(report_data)

        # HTML raporu oluştur
        html_report = self._create_html_report(report_data)

        print(f"✅ Rapor oluşturuldu: {report_file}")
        print(f"🌐 HTML Rapor: {html_report}")

        return html_report

    def _get_default_configs(self) -> Dict[str, Dict]:
        """Varsayılan model konfigürasyonları"""
        return {
            "LightGBM_Fast": {
                "type": "lightgbm",
                "config": ConfigurationGenerator.get_lightgbm_config("fast")
            },
            "LightGBM_Accurate": {
                "type": "lightgbm",
                "config": ConfigurationGenerator.get_lightgbm_config("accurate")
            },
            "PCA_Default": {
                "type": "pca",
                "config": ConfigurationGenerator.get_pca_config("default")
            },
            "Ensemble_Balanced": {
                "type": "ensemble",
                "config": ConfigurationGenerator.get_ensemble_config("balanced")
            }
        }

    def _train_model(self, model_name: str, config: Dict) -> Dict:
        """Model eğit"""
        model_type = config.get("type", "lightgbm")
        model_config = config.get("config", {})

        print(f"🔧 {model_name} eğitiliyor...")

        try:
            if model_type == "lightgbm":
                result = self.api_client.train_lightgbm(model_config)
            elif model_type == "pca":
                result = self.api_client.train_pca(model_config)
            elif model_type == "ensemble":
                result = self.api_client.train_ensemble(model_config)
            else:
                return {"error": f"Bilinmeyen model tipi: {model_type}"}

            # API'den dönen gerçek model ismini ve metrikleri ekle
            if result and "error" not in result:
                # API yanıtında modelName veya ModelName olabilir
                actual_model_name = result.get("modelName") or result.get("ModelName")
                if actual_model_name:
                    # Eğitim sonucundaki metrikleri doğrudan kullan
                    if "basicMetrics" in result:
                        print(f"✅ {model_name} eğitildi ve metrikler alındı -> Gerçek isim: {actual_model_name}")
                        # Metrikleri doğrudan kullan
                        result["metrics"] = {
                            "basicMetrics": result.get("basicMetrics", {}),
                            "confusionMatrix": result.get("confusionMatrix", {}),
                            "extendedMetrics": result.get("extendedMetrics", {}),
                            "performanceSummary": result.get("performanceSummary", {})
                        }
                        # Model karşılaştırması için gerekli verileri ekle
                        result["comparison_data"] = {
                            "modelName": actual_model_name,
                            "overallScore": result.get("performanceSummary", {}).get("overallScore", 0),
                            "grade": result.get("performanceSummary", {}).get("modelGrade", "N/A"),
                            "keyMetrics": result.get("basicMetrics", {})
                        }
                    else:
                        print(f"✅ {model_name} eğitildi -> Gerçek isim: {actual_model_name}")
                    result["ActualModelName"] = actual_model_name
                else:
                    print(f"⚠️ {model_name} için model ismi bulunamadı")
                    print(f"API Yanıtı: {json.dumps(result, indent=2, ensure_ascii=False)}")
            else:
                print(f"❌ {model_name} eğitimi başarısız")
                if result and "error" in result:
                    print(f"Hata: {result['error']}")

            return result

        except Exception as e:
            print(f"❌ {model_name} eğitimi sırasında hata: {str(e)}")
            return {"error": str(e)}

    def _get_model_metrics(self, model_result: Dict) -> Dict:
        """Model metriklerini al"""
        try:
            # Eğer eğitim sonucunda metrikler varsa, onları kullan
            if "metrics" in model_result:
                return model_result["metrics"]

            # API'den dönen gerçek model ismini kullan
            actual_model_name = model_result.get("ActualModelName") or model_result.get("modelName") or model_result.get("ModelName")

            if not actual_model_name:
                print("❌ Model ismi bulunamadı")
                print(f"Model Sonucu: {json.dumps(model_result, indent=2, ensure_ascii=False)}")
                return {"error": "Model ismi bulunamadı"}

            print(f"📊 {actual_model_name} metrikleri alınıyor...")
            
            try:
                metrics = self.api_client.get_model_metrics(actual_model_name)
                
                # API'den gelen metrikleri kontrol et
                if isinstance(metrics, dict) and "error" in metrics:
                    print(f"⚠️ API'den hata döndü: {metrics['error']}")
                    # Eğitim sonucundaki metrikleri kullan
                    if "basicMetrics" in model_result:
                        print("ℹ️ Eğitim sonucundaki metrikler kullanılıyor...")
                        return {
                            "basicMetrics": model_result.get("basicMetrics", {}),
                            "confusionMatrix": model_result.get("confusionMatrix", {}),
                            "extendedMetrics": model_result.get("extendedMetrics", {})
                        }
                    return metrics

                # Debug: API'den gelen metrikleri kontrol et
                print("\n🔍 API'den Gelen Metrikler:")
                print(f"Model: {actual_model_name}")
                print("Basic Metrics:", json.dumps(metrics.get("basicMetrics", {}), indent=2, ensure_ascii=False))
                print("Confusion Matrix:", json.dumps(metrics.get("confusionMatrix", {}), indent=2, ensure_ascii=False))
                print("Extended Metrics:", json.dumps(metrics.get("extendedMetrics", {}), indent=2, ensure_ascii=False))
                
                return metrics

            except Exception as e:
                print(f"⚠️ Metrik alma hatası: {str(e)}")
                # Eğitim sonucundaki metrikleri kullan
                if "basicMetrics" in model_result:
                    print("ℹ️ Eğitim sonucundaki metrikler kullanılıyor...")
                    return {
                        "basicMetrics": model_result.get("basicMetrics", {}),
                        "confusionMatrix": model_result.get("confusionMatrix", {}),
                        "extendedMetrics": model_result.get("extendedMetrics", {})
                    }
                return {"error": str(e)}

        except Exception as e:
            print(f"🚨 Metrik alma işlemi sırasında hata: {str(e)}")
            return {"error": str(e)}

    def _create_model_visualizations(self, model_name: str, metrics: Dict):
        """Model için görselleştirmeler oluştur"""

        try:
            print(f"📊 {model_name} için görselleştirmeler oluşturuluyor...")
            
            # Metriklerin varlığını kontrol et
            if not metrics:
                print(f"⚠️ {model_name} için metrik verisi bulunamadı")
                return

            # Temel metrikler grafiği
            if metrics.get("basicMetrics"):
                print(f"📈 {model_name} temel metrikler grafiği oluşturuluyor...")
                self._plot_basic_metrics(model_name, metrics.get("basicMetrics", {}))
            else:
                print(f"⚠️ {model_name} için temel metrikler bulunamadı")

            # Confusion Matrix
            if metrics.get("confusionMatrix"):
                print(f"🎯 {model_name} karışıklık matrisi oluşturuluyor...")
                self._plot_confusion_matrix(model_name, metrics.get("confusionMatrix", {}))
            else:
                print(f"⚠️ {model_name} için karışıklık matrisi bulunamadı")

            # ROC Curve
            if metrics.get("basicMetrics", {}).get("auc"):
                print(f"📊 {model_name} ROC eğrisi oluşturuluyor...")
                self._plot_roc_curve_simulation(model_name, metrics.get("basicMetrics", {}))
            else:
                print(f"⚠️ {model_name} için AUC değeri bulunamadı")

            # Genişletilmiş metrikler
            if metrics.get("extendedMetrics"):
                print(f"📊 {model_name} genişletilmiş metrikler grafiği oluşturuluyor...")
                self._plot_extended_metrics(model_name, metrics.get("extendedMetrics", {}))
            else:
                print(f"⚠️ {model_name} için genişletilmiş metrikler bulunamadı")

            # Grafiklerin oluşturulduğunu kontrol et
            chart_files = [f for f in os.listdir(f"{self.output_dir}/charts") if model_name in f]
            if chart_files:
                print(f"✅ {model_name} için {len(chart_files)} grafik oluşturuldu:")
                for chart in chart_files:
                    print(f"   - {chart}")
            else:
                print(f"⚠️ {model_name} için hiç grafik oluşturulamadı")

        except Exception as e:
            print(f"🚨 {model_name} görselleştirme hatası: {str(e)}")
            import traceback
            print(f"🔍 Hata detayı:\n{traceback.format_exc()}")

    def _plot_basic_metrics(self, model_name: str, basic_metrics: Dict):
        """Temel metrikler bar grafiği"""

        # API response formatına uygun field isimleri
        metrics = {
            'Accuracy': basic_metrics.get('accuracy', 0),
            'Precision': basic_metrics.get('precision', 0),
            'Recall': basic_metrics.get('recall', 0),
            'F1Score': basic_metrics.get('f1Score', 0),  # camelCase
            'AUC': basic_metrics.get('auc', 0)
        }

        plt.figure(figsize=(10, 6))

        bars = plt.bar(metrics.keys(), metrics.values(),
                       color=['#1f77b4', '#ff7f0e', '#2ca02c', '#d62728', '#9467bd'])

        # Değerleri bar'ların üzerine yaz
        for bar, value in zip(bars, metrics.values()):
            plt.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 0.01,
                     f'{value:.3f}', ha='center', va='bottom', fontweight='bold')

        plt.title(f'{model_name} - Temel Performans Metrikleri', fontsize=14, fontweight='bold')
        plt.ylabel('Değer', fontsize=12)
        plt.ylim(0, 1.1)
        plt.grid(axis='y', alpha=0.3)

        # Renk kodlaması için legend
        performance_levels = []
        for name, value in metrics.items():
            if value >= 0.9:
                level = "Mükemmel"
            elif value >= 0.8:
                level = "İyi"
            elif value >= 0.7:
                level = "Orta"
            else:
                level = "Zayıf"
            performance_levels.append(f"{name}: {level}")

        plt.figtext(0.02, 0.02, "\n".join(performance_levels), fontsize=8,
                    bbox=dict(boxstyle="round,pad=0.3", facecolor="lightgray", alpha=0.7))

        plt.tight_layout()
        plt.savefig(f'{self.output_dir}/charts/{model_name}_basic_metrics.png',
                    dpi=300, bbox_inches='tight')
        plt.close()

    def _plot_confusion_matrix(self, model_name: str, confusion_data: Dict):
        """Confusion Matrix heatmap"""

        # API response formatına uygun field isimleri (camelCase)
        tp = confusion_data.get('truePositive', 0)
        tn = confusion_data.get('trueNegative', 0)
        fp = confusion_data.get('falsePositive', 0)
        fn = confusion_data.get('falseNegative', 0)

        # Confusion matrix oluştur
        cm = np.array([[tn, fp], [fn, tp]])

        plt.figure(figsize=(8, 6))

        # Heatmap
        sns.heatmap(cm, annot=True, fmt='d', cmap='Blues',
                    xticklabels=['Normal', 'Fraud'],
                    yticklabels=['Normal', 'Fraud'],
                    cbar_kws={'label': 'Tahmin Sayısı'})

        plt.title(f'{model_name} - Confusion Matrix', fontsize=14, fontweight='bold')
        plt.xlabel('Tahmin Edilen', fontsize=12)
        plt.ylabel('Gerçek', fontsize=12)

        # Performans bilgileri ekle
        total = tp + tn + fp + fn
        accuracy = (tp + tn) / total if total > 0 else 0
        precision = tp / (tp + fp) if (tp + fp) > 0 else 0
        recall = tp / (tp + fn) if (tp + fn) > 0 else 0

        info_text = f"""
Toplam Örnek: {total}
Accuracy: {accuracy:.3f}
Precision: {precision:.3f}
Recall: {recall:.3f}
        """

        plt.figtext(0.02, 0.02, info_text.strip(), fontsize=10,
                    bbox=dict(boxstyle="round,pad=0.3", facecolor="lightblue", alpha=0.7))

        plt.tight_layout()
        plt.savefig(f'{self.output_dir}/charts/{model_name}_confusion_matrix.png',
                    dpi=300, bbox_inches='tight')
        plt.close()

    def _plot_roc_curve_simulation(self, model_name: str, basic_metrics: Dict):
        """ROC Curve simülasyonu (gerçek veri olmadığı için)"""

        # API response formatına uygun field ismi
        auc = basic_metrics.get('auc', 0.5)

        # Simulated ROC curve points
        if auc > 0.5:
            # Good performance curve
            fpr = np.linspace(0, 1, 100)
            tpr = np.power(fpr, 0.5) * auc + fpr * (1 - auc)
            tpr = np.minimum(tpr, 1.0)
        else:
            # Random classifier
            fpr = np.linspace(0, 1, 100)
            tpr = fpr

        plt.figure(figsize=(8, 6))

        # ROC Curve
        plt.plot(fpr, tpr, 'b-', linewidth=2, label=f'ROC Curve (AUC = {auc:.3f})')
        plt.plot([0, 1], [0, 1], 'r--', linewidth=2, label='Random Classifier (AUC = 0.5)')

        plt.xlim([0.0, 1.0])
        plt.ylim([0.0, 1.05])
        plt.xlabel('False Positive Rate', fontsize=12)
        plt.ylabel('True Positive Rate', fontsize=12)
        plt.title(f'{model_name} - ROC Curve', fontsize=14, fontweight='bold')
        plt.legend(loc="lower right")
        plt.grid(True, alpha=0.3)

        # AUC değerlendirmesi
        if auc >= 0.9:
            performance = "Mükemmel"
            color = "green"
        elif auc >= 0.8:
            performance = "İyi"
            color = "blue"
        elif auc >= 0.7:
            performance = "Orta"
            color = "orange"
        else:
            performance = "Zayıf"
            color = "red"

        plt.text(0.6, 0.2, f'Performans: {performance}',
                 bbox=dict(boxstyle="round,pad=0.3", facecolor=color, alpha=0.3),
                 fontsize=12, fontweight='bold')

        plt.tight_layout()
        plt.savefig(f'{self.output_dir}/charts/{model_name}_roc_curve.png',
                    dpi=300, bbox_inches='tight')
        plt.close()

    def _plot_extended_metrics(self, model_name: str, extended_metrics: Dict):
        """Genişletilmiş metrikler radar chart"""

        # API response formatına uygun field isimleri (camelCase)
        metrics = {
            'Sensitivity': extended_metrics.get('sensitivity', 0),
            'Specificity': extended_metrics.get('specificity', 0),
            'Balanced Accuracy': extended_metrics.get('balancedAccuracy', 0),
            'Matthews Corr': extended_metrics.get('matthewsCorrCoef', 0)
        }

        # Matthews correlation'ı normalize et (-1,1) -> (0,1)
        if 'Matthews Corr' in metrics:
            metrics['Matthews Corr'] = (metrics['Matthews Corr'] + 1) / 2

        # Radar chart
        angles = np.linspace(0, 2 * np.pi, len(metrics), endpoint=False).tolist()
        values = list(metrics.values())

        # Döngüyü tamamla
        angles += angles[:1]
        values += values[:1]

        fig, ax = plt.subplots(figsize=(8, 8), subplot_kw=dict(projection='polar'))

        ax.plot(angles, values, 'o-', linewidth=2, color='blue')
        ax.fill(angles, values, alpha=0.25, color='blue')

        ax.set_xticks(angles[:-1])
        ax.set_xticklabels(metrics.keys())
        ax.set_ylim(0, 1)
        ax.set_yticks([0.2, 0.4, 0.6, 0.8, 1.0])
        ax.set_yticklabels(['0.2', '0.4', '0.6', '0.8', '1.0'])
        ax.grid(True)

        plt.title(f'{model_name} - Detaylı Performans Metrikleri',
                  size=14, fontweight='bold', pad=20)

        plt.tight_layout()
        plt.savefig(f'{self.output_dir}/charts/{model_name}_extended_metrics.png',
                    dpi=300, bbox_inches='tight')
        plt.close()

    def _create_comparison_visualizations(self, comparison_data: Dict):
        """Model karşılaştırma görselleştirmeleri"""

        try:
            compared_models = comparison_data.get("ComparedModels", [])

            if len(compared_models) < 2:
                return

            # Model isimlerini ve metriklerini çıkar
            model_names = []
            accuracies = []
            precisions = []
            recalls = []
            f1_scores = []
            aucs = []

            for model in compared_models:
                if isinstance(model, dict) and "modelName" in model:  # API response formatı
                    model_names.append(model["modelName"])

                    key_metrics = model.get("keyMetrics", {})
                    # camelCase field isimleri
                    accuracies.append(key_metrics.get("accuracy", 0))
                    precisions.append(key_metrics.get("precision", 0))
                    recalls.append(key_metrics.get("recall", 0))
                    f1_scores.append(key_metrics.get("f1Score", 0))
                    aucs.append(key_metrics.get("auc", 0))

            # Karşılaştırma grafiği
            self._plot_model_comparison(model_names, {
                'Accuracy': accuracies,
                'Precision': precisions,
                'Recall': recalls,
                'F1Score': f1_scores,
                'AUC': aucs
            })

            print("✅ Model karşılaştırma görselleştirmeleri oluşturuldu")

        except Exception as e:
            print(f"⚠️ Karşılaştırma görselleştirme hatası: {str(e)}")

    def _plot_model_comparison(self, model_names: List[str], metrics: Dict[str, List[float]]):
        """Model karşılaştırma grafiği"""

        # Veri hazırlama
        df = pd.DataFrame(metrics, index=model_names)

        # Grouped bar chart
        ax = df.plot(kind='bar', figsize=(12, 8), width=0.8)

        plt.title('Model Performans Karşılaştırması', fontsize=16, fontweight='bold')
        plt.xlabel('Modeller', fontsize=12)
        plt.ylabel('Performans Değeri', fontsize=12)
        plt.legend(title='Metrikler', bbox_to_anchor=(1.05, 1), loc='upper left')
        plt.xticks(rotation=45, ha='right')
        plt.grid(axis='y', alpha=0.3)

        # Değerleri bar'ların üzerine yaz
        for container in ax.containers:
            ax.bar_label(container, fmt='%.3f', rotation=90, fontsize=8)

        plt.tight_layout()
        plt.savefig(f'{self.output_dir}/charts/model_comparison.png',
                    dpi=300, bbox_inches='tight')
        plt.close()

        # Heatmap versiyonu
        plt.figure(figsize=(10, 6))

        sns.heatmap(df.T, annot=True, fmt='.3f', cmap='RdYlBu_r',
                    cbar_kws={'label': 'Performans Değeri'})

        plt.title('Model Performans Heatmap', fontsize=14, fontweight='bold')
        plt.xlabel('Modeller', fontsize=12)
        plt.ylabel('Metrikler', fontsize=12)

        plt.tight_layout()
        plt.savefig(f'{self.output_dir}/charts/model_comparison_heatmap.png',
                    dpi=300, bbox_inches='tight')
        plt.close()

    def _generate_recommendations(self, models_data: Dict) -> List[str]:
        """Model verilerine göre öneriler oluştur"""

        recommendations = []

        # Her model için analiz
        for model_name, model_data in models_data.items():
            if "error" in model_data:
                continue

            try:
                training_result = model_data.get("training_result", {})
                performance_summary = training_result.get("PerformanceSummary", {})

                if not performance_summary.get("IsGoodModel", False):
                    weakness = performance_summary.get("PrimaryWeakness", "")
                    if weakness:
                        recommendations.append(f"{model_name}: {weakness}")

                model_recommendations = training_result.get("Recommendations", [])
                for rec in model_recommendations:
                    recommendations.append(f"{model_name}: {rec}")

            except Exception as e:
                print(f"⚠️ {model_name} önerileri işlenirken hata: {str(e)}")

        # Genel öneriler
        if len(models_data) > 1:
            recommendations.append("Ensemble modelini production ortamında kullanmayı düşünün")
            recommendations.append("Performans izleme sistemini kurun")

        return recommendations

    def _save_report(self, report_data: Dict) -> str:
        """Raporu JSON olarak kaydet"""

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        report_file = f"{self.output_dir}/model_report_{timestamp}.json"

        with open(report_file, 'w', encoding='utf-8') as f:
            json.dump(report_data, f, indent=2, ensure_ascii=False)

        return report_file

    def _create_html_report(self, report_data: Dict) -> str:
        """HTML raporu oluştur"""
        print("\n📊 HTML raporu oluşturuluyor...")

        # HTML başlangıç şablonu
        html_content = """
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Dolandırıcılık Tespiti Model Raporu</title>
    <style>
        :root {
            --primary-color: #2c3e50;
            --secondary-color: #3498db;
            --success-color: #27ae60;
            --warning-color: #f39c12;
            --danger-color: #e74c3c;
            --light-bg: #f8f9fa;
            --card-bg: #ffffff;
            --border-radius: 10px;
            --box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }

        body { 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
            color: var(--primary-color);
            line-height: 1.6;
        }

        .container { 
            max-width: 1200px;
            margin: 0 auto;
            background: var(--card-bg);
            padding: 30px;
            border-radius: var(--border-radius);
            box-shadow: var(--box-shadow);
        }

        h1 { 
            color: var(--primary-color);
            text-align: center;
            border-bottom: 3px solid var(--secondary-color);
            padding-bottom: 15px;
            margin-bottom: 30px;
            font-size: 2.5em;
        }

        h2 { 
            color: var(--primary-color);
            border-left: 4px solid var(--secondary-color);
            padding-left: 15px;
            margin-top: 40px;
            font-size: 1.8em;
        }

        h3 { 
            color: var(--primary-color);
            margin-top: 25px;
            font-size: 1.4em;
        }

        h4 {
            color: var(--primary-color);
            margin: 15px 0;
            font-size: 1.2em;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .metric-grid { 
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
            gap: 25px;
            margin: 30px 0;
        }

        .metric-card { 
            background: var(--card-bg);
            padding: 25px;
            border-radius: var(--border-radius);
            box-shadow: var(--box-shadow);
            border-top: 4px solid var(--secondary-color);
            transition: transform 0.2s;
        }

        .metric-card:hover {
            transform: translateY(-5px);
        }

        .metric-section { 
            margin: 25px 0;
            padding: 20px;
            background: var(--light-bg);
            border-radius: var(--border-radius);
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        }

        .metric-row { 
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin: 12px 0;
            padding: 12px;
            background: white;
            border-radius: 8px;
            box-shadow: 0 1px 3px rgba(0,0,0,0.05);
        }

        .metric-label { 
            font-weight: 600;
            color: var(--primary-color);
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .metric-value { 
            color: var(--secondary-color);
            font-weight: 600;
            font-size: 1.1em;
        }

        .metric-description {
            font-size: 0.85em;
            color: #666;
            margin-top: 4px;
            padding-left: 24px;
        }

        .model-header { 
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 20px;
            padding-bottom: 15px;
            border-bottom: 2px solid var(--light-bg);
        }

        .model-grade { 
            font-size: 1.8em;
            font-weight: bold;
            padding: 8px 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }

        .grade-a { 
            background: #d4edda;
            color: #155724;
            border: 2px solid #c3e6cb;
        }

        .grade-b { 
            background: #fff3cd;
            color: #856404;
            border: 2px solid #ffeeba;
        }

        .grade-c { 
            background: #f8d7da;
            color: #721c24;
            border: 2px solid #f5c6cb;
        }

        .confusion-matrix { 
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 10px;
            margin: 20px 0;
            max-width: 400px;
        }

        .confusion-cell { 
            padding: 15px;
            text-align: center;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            transition: transform 0.2s;
        }

        .confusion-cell:hover {
            transform: scale(1.02);
        }

        .true-positive { 
            background: #d4edda;
            border: 2px solid #c3e6cb;
        }

        .true-negative { 
            background: #d4edda;
            border: 2px solid #c3e6cb;
        }

        .false-positive { 
            background: #f8d7da;
            border: 2px solid #f5c6cb;
        }

        .false-negative { 
            background: #f8d7da;
            border: 2px solid #f5c6cb;
        }

        .recommendations { 
            background: #fff3cd;
            border: 2px solid #ffeeba;
            border-radius: var(--border-radius);
            padding: 20px;
            margin: 25px 0;
        }

        .recommendations ul {
            margin: 0;
            padding-left: 20px;
        }

        .recommendations li {
            margin: 10px 0;
            line-height: 1.4;
        }

        .error { 
            background: #f8d7da;
            border: 2px solid #f5c6cb;
            border-radius: var(--border-radius);
            padding: 20px;
            color: #721c24;
        }

        .success { 
            background: #d4edda;
            border: 2px solid #c3e6cb;
            border-radius: var(--border-radius);
            padding: 20px;
            color: #155724;
        }

        .warning { 
            background: #fff3cd;
            border: 2px solid #ffeeba;
            border-radius: var(--border-radius);
            padding: 20px;
            color: #856404;
        }

        .info {
            background: #e2f3f5;
            border: 2px solid #b8e0e5;
            border-radius: var(--border-radius);
            padding: 20px;
            color: #0c5460;
        }

        table { 
            width: 100%;
            border-collapse: separate;
            border-spacing: 0;
            margin: 25px 0;
            border-radius: var(--border-radius);
            overflow: hidden;
            box-shadow: var(--box-shadow);
        }

        th, td { 
            padding: 12px 15px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }

        th { 
            background-color: var(--secondary-color);
            color: white;
            font-weight: 600;
        }

        tr:nth-child(even) { 
            background-color: var(--light-bg);
        }

        tr:hover { 
            background-color: #f5f5f5;
        }

        .timestamp { 
            text-align: center;
            color: #666;
            font-style: italic;
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
        }

        .metric-info {
            position: relative;
            display: inline-block;
            margin-left: 5px;
            cursor: help;
        }

        .metric-info:hover::after {
            content: attr(data-tooltip);
            position: absolute;
            bottom: 100%;
            left: 50%;
            transform: translateX(-50%);
            padding: 8px;
            background: rgba(0,0,0,0.8);
            color: white;
            border-radius: 4px;
            font-size: 0.9em;
            white-space: nowrap;
            z-index: 1;
        }

        .visualization-grid {
            display: flex;
            flex-direction: column;
            gap: 40px;
            margin: 30px 0;
        }

        .visualization-category {
            background: var(--light-bg);
            border-radius: var(--border-radius);
            padding: 25px;
            box-shadow: var(--box-shadow);
        }

        .visualization-category h3 {
            color: var(--primary-color);
            margin-bottom: 20px;
            padding-bottom: 10px;
            border-bottom: 2px solid var(--secondary-color);
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .chart-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
            gap: 25px;
            margin-top: 20px;
        }

        .chart-card {
            background: white;
            border-radius: var(--border-radius);
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            overflow: hidden;
            transition: transform 0.2s, box-shadow 0.2s;
        }

        .chart-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 4px 8px rgba(0,0,0,0.15);
        }

        .chart-header {
            padding: 15px 20px;
            background: var(--light-bg);
            border-bottom: 1px solid #eee;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .chart-header h4 {
            margin: 0;
            color: var(--primary-color);
            font-size: 1.1em;
        }

        .model-tag {
            background: var(--secondary-color);
            color: white;
            padding: 4px 12px;
            border-radius: 15px;
            font-size: 0.9em;
            font-weight: 500;
        }

        .chart-container {
            padding: 20px;
            text-align: center;
            background: white;
        }

        .chart-container img {
            max-width: 100%;
            height: auto;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }

        .chart-description {
            padding: 15px 20px;
            background: var(--light-bg);
            border-top: 1px solid #eee;
            font-size: 0.9em;
            color: #666;
        }

        @media (max-width: 768px) {
            .metric-grid {
                grid-template-columns: 1fr;
            }
            .container {
                padding: 15px;
            }
            .metric-card {
                padding: 15px;
            }
            .chart-grid {
                grid-template-columns: 1fr;
            }
            .visualization-category {
                padding: 15px;
            }
            .chart-card {
                margin-bottom: 20px;
            }
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>🛡️ Dolandırıcılık Tespiti Model Performans Raporu</h1>
        <p class="timestamp">Rapor Tarihi: {report_data.get('timestamp', 'N/A')}</p>

        <h2>📊 Model Özetleri</h2>
        <div class="metric-grid">
"""

        # Her model için detaylı bölüm
        for model_name, model_data in report_data.get("models", {}).items():
            if "error" not in model_data:
                training_result = model_data.get("training_result", {})
                detailed_metrics = model_data.get("detailed_metrics", {})
                actual_model_name = model_data.get("actual_model_name", model_name)
                performance_summary = training_result.get("performanceSummary", {})
                grade = performance_summary.get("modelGrade", "N/A")

                html_content += f"""
            <div class="metric-card">
                <div class="model-header">
                    <h3>{actual_model_name}</h3>
                    <span class="model-grade grade-{grade.lower()}">{grade}</span>
                </div>
                
                <div class="metric-section">
                    <h4>📈 Temel Performans Metrikleri</h4>
"""

                # Temel metrikler
                basic_metrics = detailed_metrics.get("basicMetrics", {}) if detailed_metrics else training_result.get("basicMetrics", {})
                
                metrics_to_show = {
                    "Doğruluk (Accuracy)": {
                        "value": basic_metrics.get("accuracy", "N/A"),
                        "tooltip": "Tüm tahminlerin doğru olma oranı",
                        "description": "Modelin tüm sınıfları doğru tahmin etme yeteneğini gösterir. 1'e yakın değerler daha iyi performansı gösterir."
                    },
                    "Kesinlik (Precision)": {
                        "value": basic_metrics.get("precision", "N/A"),
                        "tooltip": "Dolandırıcılık olarak tahmin edilen işlemlerin gerçekten dolandırıcılık olma oranı",
                        "description": "Modelin dolandırıcılık olarak işaretlediği işlemlerin ne kadarının gerçekten dolandırıcılık olduğunu gösterir."
                    },
                    "Duyarlılık (Recall)": {
                        "value": basic_metrics.get("recall", "N/A"),
                        "tooltip": "Gerçek dolandırıcılık işlemlerinin ne kadarının tespit edildiği",
                        "description": "Gerçek dolandırıcılık işlemlerinin ne kadarının başarıyla tespit edildiğini gösterir."
                    },
                    "F1-Skoru": {
                        "value": basic_metrics.get("f1Score", "N/A"),
                        "tooltip": "Kesinlik ve Duyarlılık metriklerinin harmonik ortalaması",
                        "description": "Kesinlik ve Duyarlılık metriklerinin dengeli bir ölçüsüdür. Özellikle dengesiz veri setlerinde önemlidir."
                    },
                    "AUC": {
                        "value": basic_metrics.get("auc", "N/A"),
                        "tooltip": "Modelin sınıfları ayırt etme yeteneğinin ölçüsü",
                        "description": "Modelin sınıfları ayırt etme yeteneğini gösterir. 1'e yakın değerler daha iyi performansı gösterir."
                    }
                }

                for label, data in metrics_to_show.items():
                    value = data["value"]
                    if isinstance(value, (int, float)):
                        formatted_value = f"{value:.4f}"
                    else:
                        formatted_value = str(value)

                    html_content += f"""
                    <div class="metric-row">
                        <span class="metric-label">
                            {label}
                            <span class="metric-info" data-tooltip="{data['tooltip']}">ⓘ</span>
                        </span>
                        <span class="metric-value">{formatted_value}</span>
                    </div>
                    <div class="metric-description">
                        {data['description']}
                    </div>
"""

                # Karışıklık matrisi
                if detailed_metrics.get("confusionMatrix"):
                    confusion = detailed_metrics["confusionMatrix"]
                    html_content += f"""
                </div>

                <div class="metric-section">
                    <h4>🎯 Karışıklık Matrisi (Confusion Matrix)</h4>
                    <div class="confusion-matrix">
                        <div class="confusion-cell true-negative">
                            <strong>Doğru Negatif</strong><br>
                            {confusion.get('trueNegative', 0)}
                            <div class="metric-description">Normal işlemlerin doğru tespit edilme sayısı</div>
                        </div>
                        <div class="confusion-cell false-positive">
                            <strong>Yanlış Pozitif</strong><br>
                            {confusion.get('falsePositive', 0)}
                            <div class="metric-description">Normal işlemlerin yanlışlıkla dolandırıcılık olarak işaretlenme sayısı</div>
                        </div>
                        <div class="confusion-cell false-negative">
                            <strong>Yanlış Negatif</strong><br>
                            {confusion.get('falseNegative', 0)}
                            <div class="metric-description">Dolandırıcılık işlemlerinin kaçırılma sayısı</div>
                        </div>
                        <div class="confusion-cell true-positive">
                            <strong>Doğru Pozitif</strong><br>
                            {confusion.get('truePositive', 0)}
                            <div class="metric-description">Dolandırıcılık işlemlerinin doğru tespit edilme sayısı</div>
                        </div>
                    </div>
                </div>
"""

                # Genişletilmiş metrikler
                if detailed_metrics.get("extendedMetrics"):
                    extended = detailed_metrics["extendedMetrics"]
                    html_content += """
                <div class="metric-section">
                    <h4>📊 Genişletilmiş Metrikler</h4>
"""
                    extended_metrics = {
                        "Özgüllük (Specificity)": {
                            "value": extended.get("specificity", "N/A"),
                            "tooltip": "Normal işlemlerin doğru tespit edilme oranı",
                            "description": "Modelin normal işlemleri doğru tespit etme yeteneğini gösterir."
                        },
                        "Hassasiyet (Sensitivity)": {
                            "value": extended.get("sensitivity", "N/A"),
                            "tooltip": "Dolandırıcılık işlemlerini tespit etme yeteneği",
                            "description": "Modelin dolandırıcılık işlemlerini tespit etme yeteneğini gösterir."
                        },
                        "Dengeli Doğruluk (Balanced Accuracy)": {
                            "value": extended.get("balancedAccuracy", "N/A"),
                            "tooltip": "Özgüllük ve Hassasiyet metriklerinin ortalaması",
                            "description": "Dengesiz veri setlerinde model performansını değerlendirmek için kullanılır."
                        },
                        "Matthews Korelasyon Katsayısı": {
                            "value": extended.get("matthewsCorrCoef", "N/A"),
                            "tooltip": "Sınıflandırma performansının dengeli bir ölçüsü",
                            "description": "Sınıflandırma performansının dengeli bir ölçüsüdür. -1 ile 1 arasında değer alır."
                        }
                    }

                    for label, data in extended_metrics.items():
                        value = data["value"]
                        if isinstance(value, (int, float)):
                            formatted_value = f"{value:.4f}"
                        else:
                            formatted_value = str(value)

                        html_content += f"""
                    <div class="metric-row">
                        <span class="metric-label">
                            {label}
                            <span class="metric-info" data-tooltip="{data['tooltip']}">ⓘ</span>
                        </span>
                        <span class="metric-value">{formatted_value}</span>
                    </div>
                    <div class="metric-description">
                        {data['description']}
                    </div>
"""

                    html_content += """
                </div>
"""

                # Performans özeti
                html_content += f"""
                <div class="metric-section">
                    <h4>📝 Performans Özeti</h4>
                    <div class="metric-row">
                        <span class="metric-label">
                            Genel Skor
                            <span class="metric-info" data-tooltip="Modelin genel performans değerlendirmesi">ⓘ</span>
                        </span>
                        <span class="metric-value">{performance_summary.get('overallScore', 'N/A'):.4f}</span>
                    </div>
                    <div class="metric-description">
                        Modelin tüm metrikler değerlendirilerek hesaplanan genel performans puanı.
                    </div>

                    <div class="metric-row">
                        <span class="metric-label">
                            Model Durumu
                            <span class="metric-info" data-tooltip="Modelin genel performans değerlendirmesi">ⓘ</span>
                        </span>
                        <span class="metric-value">{'✅ İyi Model' if performance_summary.get('isGoodModel', False) else '⚠️ İyileştirme Gerekli'}</span>
                    </div>
                    <div class="metric-description">
                        Modelin genel performans durumunu gösterir.
                    </div>

                    <div class="metric-row">
                        <span class="metric-label">
                            Zayıf Yön
                            <span class="metric-info" data-tooltip="Modelin iyileştirilmesi gereken yönü">ⓘ</span>
                        </span>
                        <span class="metric-value">{performance_summary.get('primaryWeakness', 'N/A')}</span>
                    </div>
                    <div class="metric-description">
                        Modelin performansını artırmak için odaklanılması gereken alan.
                    </div>
                </div>
"""

                # Model önerileri
                if training_result.get("recommendations"):
                    html_content += """
                <div class="recommendations">
                    <h4>💡 İyileştirme Önerileri</h4>
                    <ul>
"""
                    for rec in training_result["recommendations"]:
                        html_content += f"                        <li>{rec}</li>\n"
                    html_content += """
                    </ul>
                </div>
"""

                html_content += """
            </div>
"""

        html_content += """
        </div>

        <h2>📈 Model Görselleştirmeleri</h2>
        <div class="visualization-grid">
            <div class="visualization-category">
                <h3>🎯 Temel Metrikler</h3>
                <div class="chart-grid">
"""

        # Grafikleri ekle
        for model_name in report_data.get("models", {}).keys():
            chart_files = [f for f in os.listdir(f"{self.output_dir}/charts") if model_name in f and "basic_metrics" in f]
            if chart_files:
                for chart in sorted(chart_files):
                    html_content += f"""
                    <div class="chart-card">
                        <div class="chart-header">
                            <h4>{model_name} Temel Metrikler</h4>
                            <span class="model-tag">{model_name}</span>
                        </div>
                        <div class="chart-container">
                            <img src="charts/{chart}" alt="{chart}">
                        </div>
                        <div class="chart-description">
                            Modelin temel performans metriklerini gösteren grafik.
                        </div>
                    </div>
"""

        html_content += """
                </div>
            </div>

            <div class="visualization-category">
                <h3>🎯 Karışıklık Matrisi</h3>
                <div class="chart-grid">
"""

        for model_name in report_data.get("models", {}).keys():
            chart_files = [f for f in os.listdir(f"{self.output_dir}/charts") if model_name in f and "confusion_matrix" in f]
            if chart_files:
                for chart in sorted(chart_files):
                    html_content += f"""
                    <div class="chart-card">
                        <div class="chart-header">
                            <h4>{model_name} Karışıklık Matrisi</h4>
                            <span class="model-tag">{model_name}</span>
                        </div>
                        <div class="chart-container">
                            <img src="charts/{chart}" alt="{chart}">
                        </div>
                        <div class="chart-description">
                            Modelin tahminlerinin gerçek değerlerle karşılaştırmasını gösteren matris.
                        </div>
                    </div>
"""

        html_content += """
                </div>
            </div>

            <div class="visualization-category">
                <h3>🎯 ROC ve AUC</h3>
                <div class="chart-grid">
"""

        for model_name in report_data.get("models", {}).keys():
            chart_files = [f for f in os.listdir(f"{self.output_dir}/charts") if model_name in f and "roc_curve" in f]
            if chart_files:
                for chart in sorted(chart_files):
                    html_content += f"""
                    <div class="chart-card">
                        <div class="chart-header">
                            <h4>{model_name} ROC Eğrisi</h4>
                            <span class="model-tag">{model_name}</span>
                        </div>
                        <div class="chart-container">
                            <img src="charts/{chart}" alt="{chart}">
                        </div>
                        <div class="chart-description">
                            Modelin ROC eğrisi ve AUC değeri.
                        </div>
                    </div>
"""

        html_content += """
                </div>
            </div>

            <div class="visualization-category">
                <h3>🎯 Model Karşılaştırması</h3>
                <div class="chart-grid">
"""

        comparison_charts = [f for f in os.listdir(f"{self.output_dir}/charts") if "comparison" in f]
        if comparison_charts:
            for chart in sorted(comparison_charts):
                html_content += f"""
                    <div class="chart-card">
                        <div class="chart-header">
                            <h4>Model Karşılaştırması</h4>
                        </div>
                        <div class="chart-container">
                            <img src="charts/{chart}" alt="{chart}">
                        </div>
                        <div class="chart-description">
                            Modellerin performans metriklerinin karşılaştırmalı analizi.
                        </div>
                    </div>
"""

        html_content += """
                </div>
            </div>
        </div>

        <div class="timestamp">
            <p>Bu rapor otomatik olarak oluşturulmuştur.</p>
            <p>Rapor Tarihi: {report_data.get('timestamp', 'N/A')}</p>
        </div>
    </div>
</body>
</html>
"""

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        html_file = f"{self.output_dir}/model_report_{timestamp}.html"

        with open(html_file, 'w', encoding='utf-8') as f:
            f.write(html_content)

        return html_file


# Test fonksiyonu
def create_sample_report():
    """Örnek rapor oluştur"""

    # API Client
    client = FraudDetectionAPIClient("http://localhost:5000")

    # Reporter
    reporter = ModelReporter(client, "sample_reports")

    # Basit konfigürasyonlar
    configs = {
        "LightGBM_Test": {
            "type": "lightgbm",
            "config": ConfigurationGenerator.get_lightgbm_config("fast")
        }
    }

    # Rapor oluştur
    html_report = reporter.create_full_report(configs)

    print(f"✅ Örnek rapor oluşturuldu: {html_report}")


if __name__ == "__main__":
    create_sample_report()