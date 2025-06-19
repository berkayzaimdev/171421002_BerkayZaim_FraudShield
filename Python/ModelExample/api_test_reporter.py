#!/usr/bin/env python3
"""
Advanced ML Model API Test & Report Generator
Model performanslarını test eder, karşılaştırır ve raporlar
"""

import requests
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
import plotly.graph_objects as go
import plotly.express as px
from plotly.subplots import make_subplots
import json
import time
from datetime import datetime
import warnings

warnings.filterwarnings('ignore')

# HTML template için
from jinja2 import Template
import base64
from io import BytesIO


class AdvancedMLAPITester:
    """
    Advanced ML API Test ve Rapor Sınıfı
    """

    def __init__(self, base_url="http://localhost:5000"):
        self.base_url = base_url
        self.test_results = []
        self.model_metrics = {}
        self.prediction_results = []

        # Test verileri
        self.sample_transactions = self.generate_sample_transactions()

        # Görsel ayarları
        plt.style.use('seaborn-v0_8')
        sns.set_palette("husl")

    def generate_sample_transactions(self, n_transactions=100):
        """Test için sample transaction'lar oluştur"""
        np.random.seed(42)

        transactions = []
        for i in range(n_transactions):
            # Normal transaction (80%)
            if np.random.random() > 0.2:
                transaction = {
                    "transactionId": f"TXN_{i:04d}",
                    "amount": np.random.normal(100, 50),
                    "time": np.random.randint(0, 86400),  # 24 saat içinde
                    "v1": np.random.normal(0, 1),
                    "v2": np.random.normal(0, 1),
                    "v3": np.random.normal(0, 1),
                    "v4": np.random.normal(0, 1),
                    "v5": np.random.normal(0, 1),
                    "v6": np.random.normal(0, 1),
                    "v7": np.random.normal(0, 1),
                    "v8": np.random.normal(0, 1),
                    "v9": np.random.normal(0, 1),
                    "v10": np.random.normal(0, 1),
                    "v11": np.random.normal(0, 1),
                    "v12": np.random.normal(0, 1),
                    "v13": np.random.normal(0, 1),
                    "v14": np.random.normal(0, 1),
                    "v15": np.random.normal(0, 1),
                    "v16": np.random.normal(0, 1),
                    "v17": np.random.normal(0, 1),
                    "v18": np.random.normal(0, 1),
                    "v19": np.random.normal(0, 1),
                    "v20": np.random.normal(0, 1),
                    "v21": np.random.normal(0, 1),
                    "v22": np.random.normal(0, 1),
                    "v23": np.random.normal(0, 1),
                    "v24": np.random.normal(0, 1),
                    "v25": np.random.normal(0, 1),
                    "v26": np.random.normal(0, 1),
                    "v27": np.random.normal(0, 1),
                    "v28": np.random.normal(0, 1),
                    "actual_fraud": False
                }
            else:
                # Fraud transaction (20%)
                transaction = {
                    "transactionId": f"TXN_{i:04d}",
                    "amount": np.random.normal(500, 200),  # Yüksek amount
                    "time": np.random.choice([3600, 82800]),  # Gece saatleri
                    "v1": np.random.normal(-2, 0.5),  # Anormal V değerleri
                    "v2": np.random.normal(2.5, 0.5),
                    "v3": np.random.normal(-3, 0.5),
                    "v4": np.random.normal(-1.5, 0.5),
                    "v5": np.random.normal(0, 1),
                    "v6": np.random.normal(0, 1),
                    "v7": np.random.normal(-4, 0.5),
                    "v8": np.random.normal(0, 1),
                    "v9": np.random.normal(0, 1),
                    "v10": np.random.normal(-3, 0.5),
                    "v11": np.random.normal(2, 0.5),
                    "v12": np.random.normal(-2.5, 0.5),
                    "v13": np.random.normal(0, 1),
                    "v14": np.random.normal(-4, 0.5),
                    "v15": np.random.normal(0, 1),
                    "v16": np.random.normal(-2, 0.5),
                    "v17": np.random.normal(-3.5, 0.5),
                    "v18": np.random.normal(-2, 0.5),
                    "v19": np.random.normal(0, 1),
                    "v20": np.random.normal(0, 1),
                    "v21": np.random.normal(0, 1),
                    "v22": np.random.normal(0, 1),
                    "v23": np.random.normal(0, 1),
                    "v24": np.random.normal(0, 1),
                    "v25": np.random.normal(0, 1),
                    "v26": np.random.normal(0, 1),
                    "v27": np.random.normal(0, 1),
                    "v28": np.random.normal(0, 1),
                    "actual_fraud": True
                }

            transactions.append(transaction)

        return transactions

    def test_model_training(self):
        """Model eğitimi API'lerini test et"""
        print("🔄 Model eğitimi testleri başlatılıyor...")

        models_to_test = [
            ("lightgbm", "/api/model/train/lightgbm", {}),
            ("attention", "/api/model/train/attention", {
                "hidden_dim": 64, "num_heads": 4, "epochs": 10
            }),
            ("autoencoder", "/api/model/train/autoencoder", {
                "hidden_dims": [32, 16, 8], "epochs": 20
            }),
            ("isolation_forest", "/api/model/train/isolation-forest", {
                "n_estimators": 50, "contamination": 0.1
            })
        ]

        training_results = []

        for model_name, endpoint, config in models_to_test:
            print(f"  ⏳ {model_name} eğitiliyor...")

            start_time = time.time()
            try:
                response = requests.post(
                    f"{self.base_url}{endpoint}",
                    json=config,
                    timeout=300  # 5 dakika timeout
                )

                training_time = time.time() - start_time

                if response.status_code == 200:
                    result = response.json()

                    training_result = {
                        'model_name': model_name,
                        'success': True,
                        'training_time': training_time,
                        'metrics': result.get('BasicMetrics', {}),
                        'status_code': response.status_code
                    }

                    print(f"    ✅ {model_name} başarılı - {training_time:.1f}s")

                    # Metrikleri saklayalım
                    if 'BasicMetrics' in result:
                        self.model_metrics[model_name] = result['BasicMetrics']
                else:
                    training_result = {
                        'model_name': model_name,
                        'success': False,
                        'training_time': training_time,
                        'error': response.text,
                        'status_code': response.status_code
                    }
                    print(f"    ❌ {model_name} başarısız - {response.status_code}")

            except Exception as e:
                training_result = {
                    'model_name': model_name,
                    'success': False,
                    'training_time': time.time() - start_time,
                    'error': str(e),
                    'status_code': 0
                }
                print(f"    ❌ {model_name} hata - {str(e)}")

            training_results.append(training_result)
            time.sleep(2)  # API'ye nefes ver

        self.test_results.extend(training_results)
        return training_results

    def test_predictions(self):
        """Tahmin API'lerini test et"""
        print("\n🔮 Tahmin testleri başlatılıyor...")

        models_to_test = ["lightgbm", "attention", "autoencoder", "isolation_forest"]
        prediction_results = []

        for model_name in models_to_test:
            print(f"  🎯 {model_name} ile tahmin testleri...")

            model_predictions = []

            # İlk 20 transaction ile test et
            for transaction in self.sample_transactions[:20]:
                try:
                    # Advanced model endpoint'ini kullan
                    response = requests.post(
                        f"{self.base_url}/api/model/predict/advanced/{model_name}",
                        json=transaction,
                        timeout=30
                    )

                    if response.status_code == 200:
                        result = response.json()

                        prediction = {
                            'transaction_id': transaction['transactionId'],
                            'model_name': model_name,
                            'predicted_fraud': result.get('IsFraudulent', False),
                            'probability': result.get('Probability', 0.0),
                            'confidence': result.get('Confidence', 0.0),
                            'actual_fraud': transaction['actual_fraud'],
                            'amount': transaction['amount'],
                            'time_hour': int(transaction['time'] / 3600) % 24,
                            'success': True
                        }
                    else:
                        prediction = {
                            'transaction_id': transaction['transactionId'],
                            'model_name': model_name,
                            'success': False,
                            'error': response.text
                        }

                except Exception as e:
                    prediction = {
                        'transaction_id': transaction['transactionId'],
                        'model_name': model_name,
                        'success': False,
                        'error': str(e)
                    }

                model_predictions.append(prediction)

            # Başarılı tahminleri filtrele
            successful_predictions = [p for p in model_predictions if p.get('success', False)]

            if successful_predictions:
                # Model performans metrikleri hesapla
                y_true = [p['actual_fraud'] for p in successful_predictions]
                y_pred = [p['predicted_fraud'] for p in successful_predictions]
                y_prob = [p['probability'] for p in successful_predictions]

                # Confusion matrix
                tp = sum(1 for t, p in zip(y_true, y_pred) if t and p)
                tn = sum(1 for t, p in zip(y_true, y_pred) if not t and not p)
                fp = sum(1 for t, p in zip(y_true, y_pred) if not t and p)
                fn = sum(1 for t, p in zip(y_true, y_pred) if t and not p)

                accuracy = (tp + tn) / len(y_true) if len(y_true) > 0 else 0
                precision = tp / (tp + fp) if (tp + fp) > 0 else 0
                recall = tp / (tp + fn) if (tp + fn) > 0 else 0
                f1 = 2 * (precision * recall) / (precision + recall) if (precision + recall) > 0 else 0

                model_summary = {
                    'model_name': model_name,
                    'total_predictions': len(model_predictions),
                    'successful_predictions': len(successful_predictions),
                    'accuracy': accuracy,
                    'precision': precision,
                    'recall': recall,
                    'f1_score': f1,
                    'avg_probability': np.mean(y_prob),
                    'avg_confidence': np.mean([p['confidence'] for p in successful_predictions])
                }

                print(f"    📊 {model_name}: Acc={accuracy:.3f}, F1={f1:.3f}")
            else:
                model_summary = {
                    'model_name': model_name,
                    'total_predictions': len(model_predictions),
                    'successful_predictions': 0,
                    'error_rate': 1.0
                }
                print(f"    ❌ {model_name}: Tüm tahminler başarısız")

            prediction_results.append(model_summary)
            self.prediction_results.extend(model_predictions)

        return prediction_results

    def test_model_comparison(self):
        """Model karşılaştırma API'sini test et"""
        print("\n⚖️  Model karşılaştırma testi...")

        try:
            response = requests.post(
                f"{self.base_url}/api/model/compare-advanced",
                json=["attention", "autoencoder", "isolation_forest"],
                timeout=600  # 10 dakika
            )

            if response.status_code == 200:
                result = response.json()
                print("    ✅ Model karşılaştırması başarılı")
                return result
            else:
                print(f"    ❌ Model karşılaştırması başarısız - {response.status_code}")
                return None

        except Exception as e:
            print(f"    ❌ Model karşılaştırması hata - {str(e)}")
            return None

    def create_visualizations(self):
        """Görselleştirmeler oluştur"""
        print("\n📊 Görselleştirmeler oluşturuluyor...")

        visualizations = {}

        # 1. Model Training Times Comparison
        if self.test_results:
            training_data = [r for r in self.test_results if r.get('success', False)]

            if training_data:
                fig, ax = plt.subplots(figsize=(10, 6))
                models = [r['model_name'] for r in training_data]
                times = [r['training_time'] for r in training_data]

                bars = ax.bar(models, times, color=['#FF6B6B', '#4ECDC4', '#45B7D1', '#96CEB4'])
                ax.set_title('Model Eğitim Süreleri Karşılaştırması', fontsize=16, fontweight='bold')
                ax.set_ylabel('Süre (saniye)')
                ax.set_xlabel('Model Tipleri')

                # Bar'ların üstüne değerleri yaz
                for bar, time in zip(bars, times):
                    height = bar.get_height()
                    ax.text(bar.get_x() + bar.get_width() / 2., height + 0.1,
                            f'{time:.1f}s', ha='center', va='bottom')

                plt.xticks(rotation=45)
                plt.tight_layout()

                # Base64'e çevir
                buffer = BytesIO()
                plt.savefig(buffer, format='png', dpi=300, bbox_inches='tight')
                buffer.seek(0)
                training_chart = base64.b64encode(buffer.getvalue()).decode()
                plt.close()

                visualizations['training_times'] = training_chart

        # 2. Model Performance Metrics
        if self.model_metrics:
            metrics_df = pd.DataFrame(self.model_metrics).T

            if not metrics_df.empty:
                fig, axes = plt.subplots(2, 2, figsize=(15, 12))
                fig.suptitle('Model Performans Metrikleri', fontsize=16, fontweight='bold', y=0.98)

                # Accuracy
                if 'Accuracy' in metrics_df.columns:
                    axes[0, 0].bar(metrics_df.index, metrics_df['Accuracy'], color='#FF6B6B')
                    axes[0, 0].set_title('Accuracy')
                    axes[0, 0].set_ylim(0, 1)
                    for i, v in enumerate(metrics_df['Accuracy']):
                        axes[0, 0].text(i, v + 0.01, f'{v:.3f}', ha='center')

                # Precision
                if 'Precision' in metrics_df.columns:
                    axes[0, 1].bar(metrics_df.index, metrics_df['Precision'], color='#4ECDC4')
                    axes[0, 1].set_title('Precision')
                    axes[0, 1].set_ylim(0, 1)
                    for i, v in enumerate(metrics_df['Precision']):
                        axes[0, 1].text(i, v + 0.01, f'{v:.3f}', ha='center')

                # Recall
                if 'Recall' in metrics_df.columns:
                    axes[1, 0].bar(metrics_df.index, metrics_df['Recall'], color='#45B7D1')
                    axes[1, 0].set_title('Recall')
                    axes[1, 0].set_ylim(0, 1)
                    for i, v in enumerate(metrics_df['Recall']):
                        axes[1, 0].text(i, v + 0.01, f'{v:.3f}', ha='center')

                # F1 Score
                if 'F1Score' in metrics_df.columns:
                    axes[1, 1].bar(metrics_df.index, metrics_df['F1Score'], color='#96CEB4')
                    axes[1, 1].set_title('F1 Score')
                    axes[1, 1].set_ylim(0, 1)
                    for i, v in enumerate(metrics_df['F1Score']):
                        axes[1, 1].text(i, v + 0.01, f'{v:.3f}', ha='center')

                for ax in axes.flat:
                    ax.tick_params(axis='x', rotation=45)

                plt.tight_layout()

                buffer = BytesIO()
                plt.savefig(buffer, format='png', dpi=300, bbox_inches='tight')
                buffer.seek(0)
                metrics_chart = base64.b64encode(buffer.getvalue()).decode()
                plt.close()

                visualizations['performance_metrics'] = metrics_chart

        # 3. Prediction Probability Distribution
        if self.prediction_results:
            successful_predictions = [p for p in self.prediction_results if p.get('success', False)]

            if successful_predictions:
                fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(15, 6))
                fig.suptitle('Tahmin Probability Dağılımları', fontsize=16, fontweight='bold')

                # Model bazında probability dağılımı
                prob_data = {}
                for pred in successful_predictions:
                    model = pred['model_name']
                    if model not in prob_data:
                        prob_data[model] = []
                    prob_data[model].append(pred['probability'])

                # Box plot
                ax1.boxplot([prob_data[model] for model in prob_data.keys()],
                            labels=list(prob_data.keys()))
                ax1.set_title('Model Bazında Probability Dağılımı')
                ax1.set_ylabel('Fraud Probability')
                ax1.tick_params(axis='x', rotation=45)

                # Fraud vs Normal histogram
                fraud_probs = [p['probability'] for p in successful_predictions if p['actual_fraud']]
                normal_probs = [p['probability'] for p in successful_predictions if not p['actual_fraud']]

                ax2.hist(normal_probs, alpha=0.7, label='Normal Transactions', bins=20, color='green')
                ax2.hist(fraud_probs, alpha=0.7, label='Fraud Transactions', bins=20, color='red')
                ax2.set_title('Fraud vs Normal Transaction Probabilities')
                ax2.set_xlabel('Fraud Probability')
                ax2.set_ylabel('Frequency')
                ax2.legend()

                plt.tight_layout()

                buffer = BytesIO()
                plt.savefig(buffer, format='png', dpi=300, bbox_inches='tight')
                buffer.seek(0)
                probability_chart = base64.b64encode(buffer.getvalue()).decode()
                plt.close()

                visualizations['probability_distribution'] = probability_chart

        # 4. Interactive Plotly Chart - Model Comparison Radar
        if self.model_metrics:
            fig = go.Figure()

            metrics_to_show = ['Accuracy', 'Precision', 'Recall', 'F1Score', 'AUC']

            for model_name, metrics in self.model_metrics.items():
                values = [metrics.get(metric, 0) for metric in metrics_to_show]
                values.append(values[0])  # Close the radar chart

                fig.add_trace(go.Scatterpolar(
                    r=values,
                    theta=metrics_to_show + [metrics_to_show[0]],
                    fill='toself',
                    name=model_name,
                    line=dict(width=2)
                ))

            fig.update_layout(
                polar=dict(
                    radialaxis=dict(
                        visible=True,
                        range=[0, 1]
                    )),
                showlegend=True,
                title="Model Performans Karşılaştırması - Radar Chart",
                font=dict(size=14)
            )

            # HTML olarak kaydet
            radar_html = fig.to_html(include_plotlyjs='cdn')
            visualizations['radar_chart'] = radar_html

        return visualizations

    def generate_html_report(self, visualizations):
        """HTML raporu oluştur"""

        html_template = """
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Advanced ML Model Test Raporu</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
            color: #333;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
            padding: 30px;
        }
        .header {
            text-align: center;
            border-bottom: 3px solid #4ECDC4;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }
        .header h1 {
            color: #2C3E50;
            font-size: 2.5em;
            margin: 0;
        }
        .header p {
            color: #7F8C8D;
            font-size: 1.1em;
            margin: 10px 0 0 0;
        }
        .section {
            margin: 30px 0;
            padding: 20px;
            border-radius: 8px;
            background: #f8f9fa;
        }
        .section h2 {
            color: #2C3E50;
            border-left: 4px solid #4ECDC4;
            padding-left: 15px;
            margin-top: 0;
        }
        .metrics-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin: 20px 0;
        }
        .metric-card {
            background: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            border-left: 4px solid #4ECDC4;
        }
        .metric-card h3 {
            margin: 0 0 10px 0;
            color: #2C3E50;
        }
        .metric-value {
            font-size: 2em;
            font-weight: bold;
            color: #4ECDC4;
        }
        .chart-container {
            text-align: center;
            margin: 20px 0;
            padding: 20px;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .chart-container img {
            max-width: 100%;
            height: auto;
            border-radius: 4px;
        }
        .success {
            color: #27AE60;
            font-weight: bold;
        }
        .error {
            color: #E74C3C;
            font-weight: bold;
        }
        .warning {
            color: #F39C12;
            font-weight: bold;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
            background: white;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        th, td {
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }
        th {
            background-color: #4ECDC4;
            color: white;
            font-weight: bold;
        }
        tr:hover {
            background-color: #f5f5f5;
        }
        .footer {
            text-align: center;
            margin-top: 40px;
            padding: 20px;
            border-top: 2px solid #4ECDC4;
            color: #7F8C8D;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🤖 Advanced ML Model Test Raporu</h1>
            <p>{{ test_date }} tarihinde gerçekleştirilen API testleri</p>
        </div>

        <!-- Executive Summary -->
        <div class="section">
            <h2>📋 Özet</h2>
            <div class="metrics-grid">
                <div class="metric-card">
                    <h3>Toplam Test</h3>
                    <div class="metric-value">{{ total_tests }}</div>
                </div>
                <div class="metric-card">
                    <h3>Başarılı Model</h3>
                    <div class="metric-value success">{{ successful_models }}</div>
                </div>
                <div class="metric-card">
                    <h3>Başarısız Model</h3>
                    <div class="metric-value error">{{ failed_models }}</div>
                </div>
                <div class="metric-card">
                    <h3>En İyi Model</h3>
                    <div class="metric-value">{{ best_model }}</div>
                </div>
            </div>
        </div>

        <!-- Training Results -->
        <div class="section">
            <h2>🏋️ Model Eğitim Sonuçları</h2>
            {% if training_chart %}
            <div class="chart-container">
                <h3>Eğitim Süreleri Karşılaştırması</h3>
                <img src="data:image/png;base64,{{ training_chart }}" alt="Training Times Chart">
            </div>
            {% endif %}

            <table>
                <thead>
                    <tr>
                        <th>Model</th>
                        <th>Durum</th>
                        <th>Süre (s)</th>
                        <th>Accuracy</th>
                        <th>F1 Score</th>
                        <th>AUC</th>
                    </tr>
                </thead>
                <tbody>
                    {% for result in training_results %}
                    <tr>
                        <td>{{ result.model_name }}</td>
                        <td>
                            {% if result.success %}
                                <span class="success">✅ Başarılı</span>
                            {% else %}
                                <span class="error">❌ Başarısız</span>
                            {% endif %}
                        </td>
                        <td>{{ "%.1f"|format(result.training_time) }}</td>
                        <td>{{ "%.3f"|format(result.metrics.get('Accuracy', 0)) if result.success else '-' }}</td>
                        <td>{{ "%.3f"|format(result.metrics.get('F1Score', 0)) if result.success else '-' }}</td>
                        <td>{{ "%.3f"|format(result.metrics.get('AUC', 0)) if result.success else '-' }}</td>
                    </tr>
                    {% endfor %}
                </tbody>
            </table>
        </div>

        <!-- Performance Metrics -->
        {% if performance_chart %}
        <div class="section">
            <h2>📊 Performans Metrikleri</h2>
            <div class="chart-container">
                <img src="data:image/png;base64,{{ performance_chart }}" alt="Performance Metrics Chart">
            </div>
        </div>
        {% endif %}

        <!-- Prediction Results -->
        {% if probability_chart %}
        <div class="section">
            <h2>🔮 Tahmin Sonuçları</h2>
            <div class="chart-container">
                <h3>Probability Dağılımları</h3>
                <img src="data:image/png;base64,{{ probability_chart }}" alt="Probability Distribution Chart">
            </div>
        </div>
        {% endif %}

        <!-- Interactive Radar Chart -->
        {% if radar_chart %}
        <div class="section">
            <h2>🎯 İnteraktif Model Karşılaştırması</h2>
            <div style="height: 600px;">
                {{ radar_chart|safe }}
            </div>
        </div>
        {% endif %}

        <!-- Recommendations -->
        <div class="section">
            <h2>💡 Öneriler</h2>
            <ul>
                {% for recommendation in recommendations %}
                <li>{{ recommendation }}</li>
                {% else %}
                <li>Model performansları analiz ediliyor...</li>
                {% endfor %}
            </ul>
        </div>

        <div class="footer">
            <p>Bu rapor Advanced ML API Test Suite tarafından otomatik olarak oluşturulmuştur.</p>
            <p>© 2024 - Fraud Detection System</p>
        </div>
    </div>
</body>
</html>
        """

        # Template verilerini hazırla
        successful_training = [r for r in self.test_results if r.get('success', False)]

        template_data = {
            'test_date': datetime.now().strftime('%d.%m.%Y %H:%M'),
            'total_tests': len(self.test_results),
            'successful_models': len(successful_training),
            'failed_models': len(self.test_results) - len(successful_training),
            'best_model': self.find_best_model(),
            'training_results': self.test_results,
            'training_chart': visualizations.get('training_times'),
            'performance_chart': visualizations.get('performance_metrics'),
            'probability_chart': visualizations.get('probability_distribution'),
            'radar_chart': visualizations.get('radar_chart'),
            'recommendations': self.generate_recommendations()
        }

        template = Template(html_template)
        html_report = template.render(**template_data)

        # Dosyaya kaydet
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        report_filename = f'advanced_ml_test_report_{timestamp}.html'

        with open(report_filename, 'w', encoding='utf-8') as f:
            f.write(html_report)

        print(f"📄 HTML rapor oluşturuldu: {report_filename}")
        return report_filename

    def find_best_model(self):
        """En iyi modeli bul"""
        if not self.model_metrics:
            return "Belirsiz"

        best_model = None
        best_score = 0

        for model_name, metrics in self.model_metrics.items():
            # F1 Score + Accuracy + AUC ortalaması
            score = (metrics.get('F1Score', 0) +
                     metrics.get('Accuracy', 0) +
                     metrics.get('AUC', 0)) / 3

            if score > best_score:
                best_score = score
                best_model = model_name

        return best_model or "Belirsiz"

    def generate_recommendations(self):
        """Öneriler oluştur"""
        recommendations = []

        if not self.test_results:
            return ["Test sonuçları bulunamadı."]

        successful_models = [r for r in self.test_results if r.get('success', False)]

        if len(successful_models) == 0:
            recommendations.append("❌ Hiçbir model başarılı olmadı. API bağlantılarını kontrol edin.")
        elif len(successful_models) < len(self.test_results):
            recommendations.append("⚠️  Bazı modeller başarısız oldu. Log'ları kontrol ederek hataları giderin.")

        # Performans önerileri
        if self.model_metrics:
            for model_name, metrics in self.model_metrics.items():
                if metrics.get('Accuracy', 0) < 0.8:
                    recommendations.append(f"📈 {model_name} modelinin accuracy'si düşük. Hyperparameter tuning yapın.")

                if metrics.get('Precision', 0) < 0.7:
                    recommendations.append(
                        f"🎯 {model_name} modelinde false positive oranı yüksek. Threshold ayarlaması yapın.")

                if metrics.get('Recall', 0) < 0.7:
                    recommendations.append(
                        f"🔍 {model_name} modelinde false negative oranı yüksek. Daha fazla fraud örneği ile eğitin.")

        # Genel öneriler
        recommendations.extend([
            "🔄 Model performanslarını düzenli olarak izleyin ve karşılaştırın.",
            "📊 Production'da ensemble yaklaşımı kullanmayı düşünün.",
            "⚡ Real-time prediction için hızlı modelları tercih edin.",
            "🛡️  Business threshold'ları fraud detection gereksinimlerinize göre ayarlayın."
        ])

        return recommendations

    def run_full_test_suite(self):
        """Tüm test suite'ini çalıştır"""
        print("🚀 Advanced ML API Test Suite Başlatılıyor...")
        print("=" * 60)

        # 1. Model Training Tests
        training_results = self.test_model_training()

        # 2. Prediction Tests
        prediction_results = self.test_predictions()

        # 3. Model Comparison Test
        comparison_result = self.test_model_comparison()

        # 4. Create Visualizations
        visualizations = self.create_visualizations()

        # 5. Generate Report
        report_file = self.generate_html_report(visualizations)

        print("\n" + "=" * 60)
        print("🎉 Test Suite Tamamlandı!")
        print(f"📊 Rapor: {report_file}")
        print("🌐 Raporu tarayıcınızda açarak detaylı sonuçları görüntüleyebilirsiniz.")

        return {
            'training_results': training_results,
            'prediction_results': prediction_results,
            'comparison_result': comparison_result,
            'visualizations': visualizations,
            'report_file': report_file
        }


def main():
    """Ana fonksiyon"""
    # API Base URL - Kendi API'nizin URL'sini girin
    api_url = "http://localhost:5112"  # Değiştirin!

    print("🤖 Advanced ML API Test Suite")
    print(f"🔗 API URL: {api_url}")
    print("-" * 50)

    # Test suite'ini başlat
    tester = AdvancedMLAPITester(base_url=api_url)

    try:
        # Tüm testleri çalıştır
        results = tester.run_full_test_suite()

        print("\n✅ Tüm testler tamamlandı!")

    except KeyboardInterrupt:
        print("\n⏹️  Test suite durduruldu.")
    except Exception as e:
        print(f"\n❌ Test suite hatası: {e}")
        import traceback
        traceback.print_exc()


if __name__ == "__main__":
    main()