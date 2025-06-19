#!/usr/bin/env python3
"""
Fraud Detection Python Flask API
React Frontend için basit API servisi
"""

from flask import Flask, jsonify, request
from flask_cors import CORS
import json
import os
import sys
from datetime import datetime
import traceback

# Fraud detection scripts'leri import et
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

app = Flask(__name__)

# CORS konfigürasyonu - React için
CORS(app, origins=['http://localhost:3000', 'http://localhost:3001', 'http://127.0.0.1:3000'])

@app.route('/health', methods=['GET'])
def health_check():
    """API sağlık kontrolü"""
    return jsonify({
        'status': 'healthy',
        'timestamp': datetime.utcnow().strftime('%Y-%m-%d %H:%M:%S'),
        'service': 'Fraud Detection Python API',
        'version': '1.0.0'
    })

@app.route('/analyze/shap', methods=['POST'])
def analyze_shap():
    """SHAP analizi endpoint'i"""
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({'error': 'JSON verisi gerekli'}), 400
        
        transaction_id = data.get('transactionId', 'unknown')
        model_name = data.get('modelName', 'default')
        
        # Mock SHAP analizi - gerçek implementation için SHAP library kullanılabilir
        mock_response = {
            'transactionId': transaction_id,
            'modelName': model_name,
            'prediction': 0.75,
            'expectedValue': 0.5,
            'features': [
                {
                    'name': 'amount',
                    'value': 1500.0,
                    'shapValue': 0.2,
                    'impact': 'positive'
                },
                {
                    'name': 'time',
                    'value': 14.5,
                    'shapValue': -0.1,
                    'impact': 'negative'
                },
                {
                    'name': 'v1',
                    'value': -2.3,
                    'shapValue': 0.15,
                    'impact': 'positive'
                },
                {
                    'name': 'v4',
                    'value': 1.8,
                    'shapValue': -0.05,
                    'impact': 'negative'
                }
            ],
            'summary': {
                'topPositiveFeatures': [
                    {'name': 'amount', 'value': 0.2},
                    {'name': 'v1', 'value': 0.15}
                ],
                'topNegativeFeatures': [
                    {'name': 'time', 'value': -0.1},
                    {'name': 'v4', 'value': -0.05}
                ],
                'totalPositiveImpact': 0.35,
                'totalNegativeImpact': -0.15
            }
        }
        
        return jsonify(mock_response)
        
    except Exception as e:
        app.logger.error(f"SHAP analizi hatası: {str(e)}")
        app.logger.error(traceback.format_exc())
        return jsonify({'error': f'SHAP analizi hatası: {str(e)}'}), 500

@app.route('/models/train', methods=['POST'])
def train_model():
    """Model eğitimi endpoint'i"""
    try:
        data = request.get_json()
        
        model_type = data.get('type', 'lightgbm')
        config = data.get('config', {})
        
        # Mock training response
        mock_response = {
            'success': True,
            'modelId': f'model_{datetime.now().strftime("%Y%m%d_%H%M%S")}',
            'modelType': model_type,
            'trainingTime': 120.5,
            'metrics': {
                'accuracy': 0.952,
                'precision': 0.847,
                'recall': 0.893,
                'f1_score': 0.869,
                'auc': 0.934
            },
            'message': f'{model_type} modeli başarıyla eğitildi'
        }
        
        return jsonify(mock_response)
        
    except Exception as e:
        app.logger.error(f"Model eğitimi hatası: {str(e)}")
        return jsonify({'error': f'Model eğitimi hatası: {str(e)}'}), 500

@app.route('/models/predict', methods=['POST'])
def predict():
    """Tahmin endpoint'i"""
    try:
        data = request.get_json()
        
        # Mock prediction response
        mock_response = {
            'transactionId': data.get('transactionId', 'unknown'),
            'prediction': {
                'isFraudulent': True,
                'probability': 0.78,
                'confidence': 0.85,
                'riskLevel': 'High'
            },
            'modelInfo': {
                'modelType': 'ensemble',
                'version': '1.0.0',
                'lastTrainedAt': '2024-01-15T10:30:00Z'
            }
        }
        
        return jsonify(mock_response)
        
    except Exception as e:
        app.logger.error(f"Tahmin hatası: {str(e)}")
        return jsonify({'error': f'Tahmin hatası: {str(e)}'}), 500

@app.route('/status', methods=['GET'])
def get_status():
    """Sistem durumu"""
    return jsonify({
        'service': 'Fraud Detection Python API',
        'status': 'running',
        'timestamp': datetime.utcnow().isoformat(),
        'endpoints': [
            '/health',
            '/analyze/shap',
            '/models/train',
            '/models/predict',
            '/status'
        ],
        'cors_enabled': True,
        'allowed_origins': ['http://localhost:3000', 'http://localhost:3001']
    })

@app.errorhandler(404)
def not_found(error):
    return jsonify({'error': 'Endpoint bulunamadı'}), 404

@app.errorhandler(500)
def internal_error(error):
    return jsonify({'error': 'Sunucu hatası'}), 500

if __name__ == '__main__':
    print("🐍 Fraud Detection Python API başlatılıyor...")
    print("📡 CORS aktif - React frontend'e izin veriliyor")
    print("🌐 Çalışma adresi: http://localhost:5001")
    print("🔗 Health check: http://localhost:5001/health")
    
    # Development mode'da çalıştır
    app.run(
        host='0.0.0.0',
        port=5001,
        debug=True,
        threaded=True
    ) 