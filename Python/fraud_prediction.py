#!/usr/bin/env python3
"""
Enhanced Fraud Detection Prediction Script - EVALUATION OPTIMIZED
Değerlendirme sonuçlarına göre optimize edilmiş tahmin sistemi
"""

import os
import json
import argparse
import numpy as np
import pandas as pd
import joblib
import warnings
from datetime import datetime

warnings.filterwarnings('ignore', category=UserWarning)
warnings.filterwarnings('ignore', category=FutureWarning)


class EnhancedFraudPredictor:
    """
    Geliştirilmiş Fraud Detection Tahmin Sistemi
    """

    def __init__(self):
        # Business-optimized thresholds (evaluation sonuçlarından)
        self.BUSINESS_THRESHOLDS = {
            'lightgbm': 0.12,  # F1-optimal'den biraz yüksek
            'pca': 0.08,  # PCA için düşük threshold
            'ensemble': 0.15  # Business-optimal
        }

        # Performance-based ensemble weights
        self.DYNAMIC_WEIGHTS = {
            'high_performance': {'lightgbm': 0.85, 'pca': 0.15},  # LightGBM çok iyiyse
            'medium_performance': {'lightgbm': 0.75, 'pca': 0.25},
            'balanced': {'lightgbm': 0.7, 'pca': 0.3}
        }

        # Confidence thresholds
        self.CONFIDENCE_LEVELS = {
            'high': 0.8,  # Very confident predictions
            'medium': 0.6,  # Moderately confident
            'low': 0.4  # Low confidence, needs review
        }

    def predict_with_enhanced_logic(self, model, model_info, features, model_type):
        """
        Geliştirilmiş logic ile tahmin yap
        """
        try:
            if model_type.lower() == 'ensemble':
                return self._predict_ensemble_enhanced(model, features, model_info)
            elif model_type.lower() == 'lightgbm':
                return self._predict_lightgbm_enhanced(model, features)
            elif model_type.lower() == 'pca':
                return self._predict_pca_enhanced(model, features, model_info)
            else:
                raise ValueError(f"Desteklenmeyen model tipi: {model_type}")

        except Exception as e:
            print(f"Enhanced prediction error: {e}")
            return self._create_fallback_prediction(features, model_type, str(e))

    def _predict_ensemble_enhanced(self, ensemble_model, features, model_info):
        """
        Geliştirilmiş ensemble tahmin - Performance-based weighting
        """
        print("=== ENHANCED ENSEMBLE PREDICTION START ===")

        # Alt modelleri çıkar
        lightgbm_model = ensemble_model['lightgbm_model']
        pca_model = ensemble_model['pca_model']
        pca_scaler = ensemble_model['pca_scaler']
        pca_threshold = ensemble_model['pca_threshold']

        # Alt model tahminleri
        lightgbm_result = self._predict_lightgbm_enhanced(lightgbm_model, features)

        # PCA için özel feature hazırlama
        pca_features = self._prepare_pca_features(features, model_info)
        pca_result = self._predict_pca_enhanced(pca_model, pca_features, model_info, pca_scaler, pca_threshold)

        # Performance-based weight selection
        lightgbm_performance = self._estimate_model_performance(lightgbm_result)
        weights = self._select_optimal_weights(lightgbm_performance)

        print(f"Selected weights - LightGBM: {weights['lightgbm']:.3f}, PCA: {weights['pca']:.3f}")

        # Ensemble calculation with enhanced logic
        base_ensemble_proba = (
                weights['lightgbm'] * lightgbm_result['probability'][0] +
                weights['pca'] * pca_result['probability'][0]
        )

        # Business rule adjustments
        adjusted_proba = self._apply_business_rules(base_ensemble_proba, features)

        # Confidence calculation
        confidence = self._calculate_ensemble_confidence(
            lightgbm_result['probability'][0],
            pca_result['probability'][0],
            weights
        )

        # Final prediction with business threshold
        business_threshold = self.BUSINESS_THRESHOLDS['ensemble']
        final_prediction = int(adjusted_proba >= business_threshold)

        print(f"Ensemble prediction: base={base_ensemble_proba:.4f}, adjusted={adjusted_proba:.4f}, "
              f"threshold={business_threshold:.4f}, final={final_prediction}")

        return {
            'probability': np.array([adjusted_proba]),
            'predicted_class': np.array([final_prediction]),
            'score': np.array([adjusted_proba]),
            'anomaly_score': pca_result['anomaly_score'],
            'lightgbm_probability': lightgbm_result['probability'],
            'pca_probability': pca_result['probability'],
            'ensemble_weights': weights,
            'confidence': confidence,
            'business_threshold': business_threshold,
            'business_adjustments': {
                'base_probability': base_ensemble_proba,
                'adjusted_probability': adjusted_proba,
                'adjustment_factor': adjusted_proba / base_ensemble_proba if base_ensemble_proba > 0 else 1.0
            },
            'method': 'enhanced_ensemble',
            'model_performance': lightgbm_performance
        }

    def _predict_lightgbm_enhanced(self, model, features):
        """
        Geliştirilmiş LightGBM tahmin
        """
        try:
            # Standard prediction
            probabilities = model.predict_proba(features)
            fraud_probability = probabilities[:, 1] if probabilities.shape[1] > 1 else probabilities[:, 0]

            # Business threshold application
            business_threshold = self.BUSINESS_THRESHOLDS['lightgbm']
            predicted_class = (fraud_probability >= business_threshold).astype(int)

            # Confidence based on probability extremity
            confidence = np.where(
                (fraud_probability <= 0.2) | (fraud_probability >= 0.8),
                0.9,  # High confidence
                0.7  # Medium confidence
            )

            return {
                'probability': fraud_probability,
                'predicted_class': predicted_class,
                'score': fraud_probability,
                'confidence': confidence[0] if len(confidence) > 0 else 0.7,
                'business_threshold': business_threshold,
                'method': 'enhanced_lightgbm'
            }

        except Exception as e:
            print(f"LightGBM enhanced prediction failed: {e}")
            return self._create_fallback_prediction(features, 'lightgbm', str(e))

    def _predict_pca_enhanced(self, model, features, model_info, scaler=None, threshold=None):
        """
        Geliştirilmiş PCA tahmin
        """
        try:
            # Feature scaling
            if scaler is not None:
                features_scaled = scaler.transform(features)
            else:
                # Manual scaling as fallback
                features_scaled = (features - np.mean(features, axis=0)) / (np.std(features, axis=0) + 1e-8)

            # PCA transformation
            features_pca = model.transform(features_scaled)

            # Reconstruction
            features_reconstructed = model.inverse_transform(features_pca)

            # Reconstruction error
            reconstruction_errors = np.mean(np.square(features_scaled - features_reconstructed), axis=1)

            # Adaptive threshold
            if threshold is None or threshold <= 0:
                threshold = self._calculate_adaptive_pca_threshold(reconstruction_errors)

            # Business-optimized threshold
            business_threshold = min(threshold, self.BUSINESS_THRESHOLDS['pca'])

            # Anomaly scores and probability
            anomaly_scores = reconstruction_errors / business_threshold

            # Enhanced probability calculation - more nuanced
            probability = self._calculate_pca_probability(anomaly_scores, reconstruction_errors)

            is_anomaly = (reconstruction_errors > business_threshold).astype(int)

            return {
                'probability': probability,
                'predicted_class': is_anomaly,
                'score': probability,
                'anomaly_score': anomaly_scores,
                'reconstruction_error': reconstruction_errors,
                'threshold_used': business_threshold,
                'adaptive_threshold': threshold,
                'method': 'enhanced_pca'
            }

        except Exception as e:
            print(f"PCA enhanced prediction failed: {e}")
            return self._create_fallback_prediction(features, 'pca', str(e))

    def _prepare_pca_features(self, features, model_info):
        """
        PCA için özel feature hazırlama
        """
        # PCA model info'sunden expected feature count'u al
        pca_expected = model_info.get('pca_expected_features', 30)

        if features.shape[1] == pca_expected:
            return features
        elif features.shape[1] > pca_expected:
            # İlk N feature'ı al (genellikle Amount, Time, V1-V28)
            return features.iloc[:, :pca_expected]
        else:
            # Eksik feature'ları 0 ile doldur
            missing_count = pca_expected - features.shape[1]
            zeros = pd.DataFrame(0, index=features.index, columns=[f'missing_{i}' for i in range(missing_count)])
            return pd.concat([features, zeros], axis=1)

    def _estimate_model_performance(self, prediction_result):
        """
        Model performansını tahmin et (prediction sonucundan)
        """
        confidence = prediction_result.get('confidence', 0.5)
        probability = prediction_result['probability'][0]

        # Extreme probabilities indicate better performance
        extremity_score = abs(probability - 0.5) * 2  # 0 to 1

        # Combined performance estimate
        performance_score = (confidence + extremity_score) / 2

        if performance_score >= 0.8:
            return 'high'
        elif performance_score >= 0.6:
            return 'medium'
        else:
            return 'balanced'

    def _select_optimal_weights(self, performance_level):
        """
        Performance seviyesine göre optimal weight'lar seç
        """
        return self.DYNAMIC_WEIGHTS.get(performance_level + '_performance',
                                        self.DYNAMIC_WEIGHTS['balanced'])

    def _apply_business_rules(self, base_probability, features):
        """
        İş kurallarını uygula
        """
        adjusted_probability = base_probability

        # Rule 1: High amount transactions are more risky
        if 'Amount' in features.columns:
            amount = features['Amount'].iloc[0]
            if amount > 0.8:  # Normalized amount > 0.8
                adjusted_probability *= 1.2
                print(f"Business rule: High amount detected, probability boosted")

        # Rule 2: Night time transactions
        if 'Time' in features.columns:
            time_val = features['Time'].iloc[0]
            hour = (time_val / 3600) % 24
            if hour < 6 or hour > 22:
                adjusted_probability *= 1.15
                print(f"Business rule: Night time transaction, probability boosted")

        # Rule 3: Extreme V values combination
        v_risk_count = 0
        v_columns = ['V1', 'V2', 'V3', 'V4', 'V10', 'V14']
        v_thresholds = [-2.0, 2.0, -3.0, -1.0, -3.0, -4.0]

        for v_col, threshold in zip(v_columns, v_thresholds):
            if v_col in features.columns:
                v_val = features[v_col].iloc[0]
                if (threshold < 0 and v_val < threshold) or (threshold > 0 and v_val > threshold):
                    v_risk_count += 1

        if v_risk_count >= 3:
            adjusted_probability *= 1.3
            print(f"Business rule: Multiple extreme V values detected, probability boosted")

        # Ensure probability stays within [0, 1]
        adjusted_probability = min(0.95, max(0.01, adjusted_probability))

        return adjusted_probability

    def _calculate_ensemble_confidence(self, lightgbm_prob, pca_prob, weights):
        """
        Ensemble için confidence hesapla
        """
        # Agreement between models
        agreement = 1 - abs(lightgbm_prob - pca_prob)

        # Weighted confidence based on model performance
        base_confidence = 0.8  # Ensemble typically more reliable

        # Boost confidence if models agree
        if agreement > 0.7:
            base_confidence += 0.1
        elif agreement < 0.3:
            base_confidence -= 0.2

        # Consider the strength of the stronger model
        stronger_prob = max(lightgbm_prob, pca_prob)
        if stronger_prob > 0.8 or stronger_prob < 0.2:
            base_confidence += 0.05

        return min(0.95, max(0.5, base_confidence))

    def _calculate_adaptive_pca_threshold(self, reconstruction_errors):
        """
        Adaptif PCA threshold hesapla
        """
        mean_error = np.mean(reconstruction_errors)
        std_error = np.std(reconstruction_errors)

        # Business-friendly threshold (not too sensitive)
        adaptive_threshold = mean_error + 1.5 * std_error

        return adaptive_threshold

    def _calculate_pca_probability(self, anomaly_scores, reconstruction_errors):
        """
        Geliştirilmiş PCA probability hesaplama
        """
        # Multi-factor probability calculation
        # Factor 1: Anomaly score based
        score_probability = 1 / (1 + np.exp(-anomaly_scores + 2))

        # Factor 2: Reconstruction error magnitude
        error_percentile = np.clip(reconstruction_errors / np.max(reconstruction_errors), 0, 1)
        error_probability = error_percentile ** 0.5  # Square root for more gradual increase

        # Combine factors
        combined_probability = (score_probability + error_probability) / 2

        # Apply business logic - PCA should be more conservative
        conservative_probability = combined_probability * 0.8

        return np.clip(conservative_probability, 0.001, 0.999)

    def _create_fallback_prediction(self, features, model_type, error_msg):
        """
        Fallback prediction oluştur
        """
        print(f"Creating fallback prediction for {model_type}: {error_msg}")

        # Rule-based fallback with business logic
        base_prob = 0.1

        # Amount risk
        if 'Amount' in features.columns:
            amount = features['Amount'].iloc[0]
            amount_risk = min(0.4, amount * 2)
            base_prob += amount_risk

        # V values risk
        high_risk_vs = ['V1', 'V2', 'V3', 'V4', 'V10', 'V14']
        v_risk = 0
        for v_col in high_risk_vs:
            if v_col in features.columns:
                v_val = abs(features[v_col].iloc[0])
                if v_val > 2.0:
                    v_risk += 0.05

        base_prob += v_risk
        final_prob = min(0.8, max(0.05, base_prob))

        # Business threshold
        business_threshold = self.BUSINESS_THRESHOLDS.get(model_type, 0.5)
        prediction = int(final_prob >= business_threshold)

        return {
            'probability': np.array([final_prob]),
            'predicted_class': np.array([prediction]),
            'score': np.array([final_prob]),
            'anomaly_score': np.array([final_prob * 2]),
            'method': f'enhanced_fallback_{model_type}',
            'fallback_reason': error_msg,
            'business_threshold': business_threshold,
            'confidence': 0.4  # Low confidence for fallback
        }


def enhanced_prediction_main():
    """
    Enhanced prediction ana fonksiyonu
    """
    parser = argparse.ArgumentParser(description='Enhanced Fraud Detection Prediction')
    parser.add_argument('--model-info', type=str, required=True, help='Model bilgi dosyasının yolu')
    parser.add_argument('--input', type=str, required=True, help='Girdi dosyasının yolu (JSON)')
    parser.add_argument('--output', type=str, required=True, help='Çıktı dosyasının yolu (JSON)')
    parser.add_argument('--model-type', type=str, default='ensemble',
                        choices=['lightgbm', 'pca', 'ensemble'], help='Kullanılacak model tipi')

    args = parser.parse_args()

    try:
        print(f"=== ENHANCED FRAUD PREDICTION START ===")
        print(f"Timestamp: {datetime.now()}")
        print(f"Model info: {args.model_info}")
        print(f"Input: {args.input}")
        print(f"Output: {args.output}")
        print(f"Model type: {args.model_type}")

        # Enhanced predictor oluştur
        predictor = EnhancedFraudPredictor()

        # Model ve bilgileri yükle
        with open(args.model_info, 'r', encoding='utf-8') as f:
            model_info = json.load(f)

        model_path = model_info.get('model_path')
        model = joblib.load(model_path)

        # Input yükle
        with open(args.input, 'r', encoding='utf-8') as f:
            input_data = json.load(f)

        # DataFrame'e dönüştür
        features = pd.DataFrame([input_data])

        # Feature preparation (basit)
        features = prepare_features_for_prediction(features, args.model_type)

        # Enhanced prediction
        result = predictor.predict_with_enhanced_logic(model, model_info, features, args.model_type)

        # JSON-safe conversion
        def json_safe_convert(obj):
            if isinstance(obj, np.ndarray):
                return obj.tolist()
            elif isinstance(obj, (np.integer, np.int8, np.int16, np.int32, np.int64)):
                return int(obj)
            elif isinstance(obj, (np.floating, np.float16, np.float32, np.float64)):
                return float(obj)
            elif isinstance(obj, np.bool_):
                return bool(obj)
            elif isinstance(obj, dict):
                return {key: json_safe_convert(value) for key, value in obj.items()}
            elif isinstance(obj, (list, tuple)):
                return [json_safe_convert(item) for item in obj]
            else:
                return obj

        # Output hazırla
        output = json_safe_convert(result)
        output['enhanced_features'] = {
            'business_rules_applied': True,
            'dynamic_weighting': args.model_type == 'ensemble',
            'confidence_calculated': True,
            'threshold_optimized': True
        }

        # Sonucu kaydet
        with open(args.output, 'w', encoding='utf-8') as f:
            json.dump(output, f, indent=2, ensure_ascii=False)

        print(f"=== ENHANCED PREDICTION COMPLETED ===")
        print(f"Probability: {output['probability'][0]:.6f}")
        print(f"Predicted Class: {output['predicted_class'][0]}")
        print(f"Confidence: {output.get('confidence', 'N/A')}")
        print(f"Method: {output.get('method', 'unknown')}")

    except Exception as e:
        print(f"❌ Enhanced Prediction Error: {e}")
        import traceback
        traceback.print_exc()

        # Emergency output
        emergency_output = {
            'probability': [0.3],
            'predicted_class': [0],
            'score': [0.3],
            'anomaly_score': [0.6],
            'error': str(e),
            'method': 'emergency_fallback'
        }

        with open(args.output, 'w', encoding='utf-8') as f:
            json.dump(emergency_output, f, indent=2)

        exit(1)


def prepare_features_for_prediction(df, model_type):
    """
    Prediction için feature hazırlama (basitleştirilmiş)
    """
    # Temel feature engineering
    if 'Amount' in df.columns and 'AmountLog' not in df.columns:
        df['AmountLog'] = np.log1p(df['Amount'])

    if 'Time' in df.columns:
        if 'TimeSin' not in df.columns:
            seconds_in_day = 24 * 60 * 60
            df['TimeSin'] = np.sin(2 * np.pi * df['Time'] / seconds_in_day)
        if 'TimeCos' not in df.columns:
            seconds_in_day = 24 * 60 * 60
            df['TimeCos'] = np.cos(2 * np.pi * df['Time'] / seconds_in_day)
        if 'DayOfWeek' not in df.columns:
            df['DayOfWeek'] = ((df['Time'] / (24 * 60 * 60)) % 7).astype(int)
        if 'HourOfDay' not in df.columns:
            df['HourOfDay'] = ((df['Time'] / 3600) % 24).astype(int)

    return df


if __name__ == "__main__":
    enhanced_prediction_main()