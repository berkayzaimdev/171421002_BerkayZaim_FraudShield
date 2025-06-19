import argparse
import json
import os
from datetime import datetime

import joblib
import numpy as np
from lightgbm import LGBMClassifier
from sklearn.decomposition import PCA
from sklearn.metrics import (
    accuracy_score, precision_score, recall_score, f1_score, roc_auc_score,
    confusion_matrix, classification_report,
    roc_curve, matthews_corrcoef, cohen_kappa_score, log_loss, brier_score_loss,
    average_precision_score
)
from sklearn.preprocessing import StandardScaler

# Yardımcı fonksiyonları içe aktar
from utils import load_data, load_config


def calculate_comprehensive_metrics(y_true, y_pred, y_proba, model_type="binary"):
    """
    Kapsamlı model metriklerini hesapla

    Args:
        y_true: Gerçek etiketler
        y_pred: Tahmin edilen sınıflar
        y_proba: Tahmin olasılıkları
        model_type: Model tipi (binary, anomaly)

    Returns:
        Detaylı metrik sözlüğü
    """
    metrics = {}

    # Temel metrikler
    metrics['accuracy'] = accuracy_score(y_true, y_pred)
    metrics['precision'] = precision_score(y_true, y_pred, zero_division=0)
    metrics['recall'] = recall_score(y_true, y_pred, zero_division=0)
    metrics['f1_score'] = f1_score(y_true, y_pred, zero_division=0)

    # Confusion Matrix
    tn, fp, fn, tp = confusion_matrix(y_true, y_pred).ravel()
    metrics['true_positive'] = int(tp)
    metrics['true_negative'] = int(tn)
    metrics['false_positive'] = int(fp)
    metrics['false_negative'] = int(fn)

    # Confusion Matrix'ten türetilen metrikler
    total = tp + tn + fp + fn
    metrics['sensitivity'] = tp / (tp + fn) if (tp + fn) > 0 else 0  # True Positive Rate
    metrics['specificity'] = tn / (tn + fp) if (tn + fp) > 0 else 0  # True Negative Rate
    metrics['npv'] = tn / (tn + fn) if (tn + fn) > 0 else 0  # Negative Predictive Value
    metrics['fpr'] = fp / (tn + fp) if (tn + fp) > 0 else 0  # False Positive Rate
    metrics['fnr'] = fn / (tp + fn) if (tp + fn) > 0 else 0  # False Negative Rate
    metrics['fdr'] = fp / (tp + fp) if (tp + fp) > 0 else 0  # False Discovery Rate
    metrics['for'] = fn / (tn + fn) if (tn + fn) > 0 else 0  # False Omission Rate

    # Balanced Accuracy
    metrics['balanced_accuracy'] = (metrics['sensitivity'] + metrics['specificity']) / 2

    # ROC AUC
    if len(np.unique(y_true)) > 1:
        metrics['auc'] = roc_auc_score(y_true, y_proba)
        # Precision-Recall AUC
        metrics['auc_pr'] = average_precision_score(y_true, y_proba)
    else:
        metrics['auc'] = 0.5
        metrics['auc_pr'] = 0.0

    # İstatistiksel metrikler
    metrics['matthews_corrcoef'] = matthews_corrcoef(y_true, y_pred)
    metrics['cohen_kappa'] = cohen_kappa_score(y_true, y_pred)

    # Probabilistic metrikler
    if y_proba is not None and len(y_proba) > 0:
        # Log Loss
        try:
            metrics['log_loss'] = log_loss(y_true, y_proba)
        except:
            metrics['log_loss'] = 0.0

        # Brier Score
        try:
            metrics['brier_score'] = brier_score_loss(y_true, y_proba)
        except:
            metrics['brier_score'] = 0.0

        # Optimal threshold bulma (Youden's Index)
        fpr, tpr, thresholds = roc_curve(y_true, y_proba)
        optimal_idx = np.argmax(tpr - fpr)
        metrics['optimal_threshold'] = float(thresholds[optimal_idx])
    else:
        metrics['log_loss'] = 0.0
        metrics['brier_score'] = 0.0
        metrics['optimal_threshold'] = 0.5

    # Sınıf dağılımı
    metrics['support_class_0'] = int(np.sum(y_true == 0))
    metrics['support_class_1'] = int(np.sum(y_true == 1))
    metrics['class_imbalance_ratio'] = metrics['support_class_1'] / metrics['support_class_0'] if metrics[
                                                                                                      'support_class_0'] > 0 else 0

    # Sınıflandırma raporu
    try:
        report = classification_report(y_true, y_pred, output_dict=True, zero_division=0)
        metrics['classification_report'] = {
            'class_0': {
                'precision': report['0']['precision'],
                'recall': report['0']['recall'],
                'f1_score': report['0']['f1-score'],
                'support': report['0']['support']
            },
            'class_1': {
                'precision': report['1']['precision'],
                'recall': report['1']['recall'],
                'f1_score': report['1']['f1-score'],
                'support': report['1']['support']
            },
            'macro_avg': {
                'precision': report['macro avg']['precision'],
                'recall': report['macro avg']['recall'],
                'f1_score': report['macro avg']['f1-score'],
                'support': report['macro avg']['support']
            },
            'weighted_avg': {
                'precision': report['weighted avg']['precision'],
                'recall': report['weighted avg']['recall'],
                'f1_score': report['weighted avg']['f1-score'],
                'support': report['weighted avg']['support']
            }
        }
    except:
        metrics['classification_report'] = {}

    return metrics

def train_pca(config, X_train, X_test, y_test=None):
    """
    PCA anomali modeli eğit (Geliştirilmiş metriklerle)
    """
    print("PCA anomali modeli eğitiliyor...")

    # Konfigürasyonu al
    pca_config = config.get('pca', {})

    # Veriyi ölçeklendir
    scaler = StandardScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_test_scaled = scaler.transform(X_test)

    # PCA modeli oluştur
    n_components = pca_config.get('componentCount', 15)
    pca = PCA(n_components=n_components)
    pca.fit(X_train_scaled)

    # Boyut indirgenmiş veri
    X_pca = pca.transform(X_train_scaled)

    # Yeniden oluşturma
    X_reconstructed = pca.inverse_transform(X_pca)

    # Yeniden oluşturma hataları
    reconstruction_errors = np.mean(np.square(X_train_scaled - X_reconstructed), axis=1)

    # Anomali eşiği
    threshold_factor = pca_config.get('anomalyThreshold', 2.5)
    threshold = np.mean(reconstruction_errors) + threshold_factor * np.std(reconstruction_errors)

    # Test verisi üzerinde hatalar
    X_test_pca = pca.transform(X_test_scaled)
    X_test_reconstructed = pca.inverse_transform(X_test_pca)
    test_errors = np.mean(np.square(X_test_scaled - X_test_reconstructed), axis=1)

    # Anomali skorları
    anomaly_scores = test_errors / threshold

    # Tahminler
    predictions = (test_errors > threshold).astype(int)

    # Olasılığa dönüştür
    anomaly_proba = 1 / (1 + np.exp(-anomaly_scores + 2))

    # Temel PCA metrikleri
    metrics = {
        'explained_variance_ratio': float(np.sum(pca.explained_variance_ratio_)),
        'anomaly_threshold': float(threshold),
        'mean_reconstruction_error': float(np.mean(reconstruction_errors)),
        'std_reconstruction_error': float(np.std(reconstruction_errors)),
        'test_mean_error': float(np.mean(test_errors)),
        'test_max_error': float(np.max(test_errors))
    }

    # Eğer gerçek etiketler varsa, sınıflandırma metriklerini hesapla
    if y_test is not None:
        comprehensive_metrics = calculate_comprehensive_metrics(y_test, predictions, anomaly_proba, "pca")
        metrics.update(comprehensive_metrics)

    # Bileşen katkıları
    feature_contribution = {}
    for i, component in enumerate(pca.components_[:5]):  # İlk 5 bileşen
        feature_contribution[f'PC{i + 1}'] = dict(zip(X_train.columns, component))

    metrics['feature_contribution'] = feature_contribution

    # Metrik özetini yazdır
    print_metric_summary(metrics, "PCA")

    return {
        'model': pca,
        'scaler': scaler,
        'threshold': threshold,
        'metrics': metrics,
        'feature_contribution': feature_contribution
    }


def train_ensemble(config, X_train, y_train, X_test, y_test):
    """
    Ensemble model eğit (Geliştirilmiş metriklerle)
    """
    print("Ensemble model eğitiliyor...")

    # Alt modelleri eğit
    lightgbm_result = train_lightgbm(config, X_train, y_train, X_test, y_test)
    pca_result = train_pca(config, X_train, X_test, y_test)

    # Alt modelleri çıkar
    lightgbm_model = lightgbm_result['model']
    pca_model = pca_result['model']
    pca_scaler = pca_result['scaler']
    pca_threshold = pca_result['threshold']

    # Ensemble konfigürasyonu
    ensemble_config = config.get('ensemble', {})
    lightgbm_weight = ensemble_config.get('lightgbmWeight', 0.7)
    pca_weight = ensemble_config.get('pcaWeight', 0.3)
    threshold = ensemble_config.get('threshold', 0.5)

    # Alt model tahminleri
    lightgbm_proba = lightgbm_model.predict_proba(X_test)[:, 1]

    X_test_scaled = pca_scaler.transform(X_test)
    X_test_pca = pca_model.transform(X_test_scaled)
    X_test_reconstructed = pca_model.inverse_transform(X_test_pca)
    test_errors = np.mean(np.square(X_test_scaled - X_test_reconstructed), axis=1)
    anomaly_scores = test_errors / pca_threshold
    pca_proba = 1 / (1 + np.exp(-anomaly_scores + 2))

    # Ağırlıklı ensemble
    ensemble_proba = lightgbm_weight * lightgbm_proba + pca_weight * pca_proba
    ensemble_pred = (ensemble_proba >= threshold).astype(int)

    # Kapsamlı metrikler
    metrics = calculate_comprehensive_metrics(y_test, ensemble_pred, ensemble_proba, "ensemble")

    # Alt model metrikleri de ekle
    metrics['lightgbm_auc'] = lightgbm_result['metrics']['auc']
    metrics['pca_auc'] = pca_result['metrics'].get('auc', 0)
    metrics['lightgbm_weight'] = lightgbm_weight
    metrics['pca_weight'] = pca_weight
    metrics['ensemble_threshold'] = threshold

    # Metrik özetini yazdır
    print_metric_summary(metrics, "Ensemble")

    # Ensemble model nesnesi
    ensemble_model = {
        'lightgbm_model': lightgbm_model,
        'pca_model': pca_model,
        'pca_scaler': pca_scaler,
        'pca_threshold': pca_threshold,
        'lightgbm_weight': lightgbm_weight,
        'pca_weight': pca_weight,
        'threshold': threshold
    }

    return {
        'model': ensemble_model,
        'metrics': metrics,
        'feature_importance': lightgbm_result['feature_importance']
    }


# Training kodundaki save_model fonksiyonunu güncelleyin:

def save_model(model_result, model_type, output_dir):
    """
    Modeli geliştirilmiş metriklerle kaydet - Feature bilgileri ile
    """
    # Dizinin var olduğundan emin ol
    os.makedirs(output_dir, exist_ok=True)

    # Timestamp
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

    # Model dosyası
    model_path = os.path.join(output_dir, f"{model_type}_model_{timestamp}.joblib")
    joblib.dump(model_result['model'], model_path)

    # Feature bilgilerini çıkar
    feature_info = extract_feature_info(model_result, model_type)

    # Model bilgi dosyası (genişletilmiş)
    info = {
        'timestamp': timestamp,
        'model_type': model_type,
        'model_path': model_path,
        'metrics': model_result['metrics'],
        'performance_summary': generate_performance_summary(model_result['metrics']),
        'feature_info': feature_info  # Feature bilgilerini ekle
    }

    # Özellik önemi varsa ekle
    if 'feature_importance' in model_result:
        info['feature_importance'] = model_result['feature_importance']

    # Scaler bilgileri (PCA için)
    if 'scaler' in model_result:
        scaler_path = os.path.join(output_dir, f"{model_type}_scaler_{timestamp}.joblib")
        joblib.dump(model_result['scaler'], scaler_path)
        info['scaler_path'] = scaler_path

    # Threshold bilgisi (PCA için)
    if 'threshold' in model_result:
        info['threshold'] = model_result['threshold']

    info_path = os.path.join(output_dir, f"model_info_{timestamp}.json")

    with open(info_path, 'w', encoding='utf-8') as f:
        json.dump(info, f, indent=2, default=str)

    print(f"Model kaydedildi: {model_path}")
    print(f"Model bilgileri kaydedildi: {info_path}")

    return model_path, info_path



# train_lightgbm fonksiyonunu da güncelleyin:
def train_lightgbm(config, X_train, y_train, X_test, y_test):
    """
    LightGBM modeli eğit (Feature bilgileri ile)
    """
    print("LightGBM modeli eğitiliyor...")
    print(f"Training features: {list(X_train.columns)}")
    print(f"Training feature count: {len(X_train.columns)}")

    # Konfigürasyonu al
    lgbm_config = config.get('lightgbm', {})

    # Feature columns'u konfigürasyona ekle
    lgbm_config['featureColumns'] = list(X_train.columns)
    lgbm_config['featureCount'] = len(X_train.columns)

    # Sınıf ağırlıkları
    class_weights = None
    if lgbm_config.get('useClassWeights', True):
        class_weights = {
            0: lgbm_config.get('classWeights', {}).get('0', 1.0),
            1: lgbm_config.get('classWeights', {}).get('1', 75.0)
        }

    # Modeli oluştur
    model = LGBMClassifier(
        n_estimators=lgbm_config.get('numberOfTrees', 1000),
        num_leaves=lgbm_config.get('numberOfLeaves', 128),
        min_child_samples=lgbm_config.get('minDataInLeaf', 10),
        learning_rate=lgbm_config.get('learningRate', 0.005),
        feature_fraction=lgbm_config.get('featureFraction', 0.8),
        bagging_fraction=lgbm_config.get('baggingFraction', 0.8),
        bagging_freq=lgbm_config.get('baggingFrequency', 5),
        reg_alpha=lgbm_config.get('l1Regularization', 0.01),
        reg_lambda=lgbm_config.get('l2Regularization', 0.01),
        min_split_gain=lgbm_config.get('minGainToSplit', 0.0005),
        class_weight=class_weights,
        random_state=42
    )

    # Model eğitimi
    model.fit(X_train, y_train)

    print(f"Model trained with {model.n_features_in_} features")

    # Tahminler
    y_pred = model.predict(X_test)
    y_proba = model.predict_proba(X_test)[:, 1]

    # Kapsamlı metrikler
    metrics = calculate_comprehensive_metrics(y_test, y_pred, y_proba, "lightgbm")

    # Model spesifik bilgiler
    feature_importance = dict(zip(X_train.columns, model.feature_importances_))
    metrics['feature_importance'] = feature_importance

    # Feature bilgilerini metrics'e ekle
    metrics['n_features_in'] = model.n_features_in_ if hasattr(model, 'n_features_in_') else len(X_train.columns)
    metrics['feature_names'] = list(X_train.columns)

    # Konfigürasyonu metrics'e ekle
    metrics['model_config'] = lgbm_config

    # En önemli özellikleri göster
    importance_sorted = sorted(feature_importance.items(), key=lambda x: x[1], reverse=True)
    print("En önemli 10 özellik:")
    for feature, importance in importance_sorted[:10]:
        print(f"  {feature}: {importance:.4f}")

    # Metrik özetini yazdır
    print_metric_summary(metrics, "LightGBM")

    return {
        'model': model,
        'metrics': metrics,
        'feature_importance': feature_importance,
        'config': lgbm_config  # Konfigürasyonu da döndür
    }




def print_metric_summary(metrics, model_name):
    """
    Metrik özetini yazdır
    """
    print(f"\n{model_name} Model Performans Özeti:")
    print("-" * 50)
    print(f"Accuracy: {metrics.get('accuracy', 0):.4f}")
    print(f"Precision: {metrics.get('precision', 0):.4f}")
    print(f"Recall: {metrics.get('recall', 0):.4f}")
    print(f"F1-Score: {metrics.get('f1_score', 0):.4f}")
    print(f"AUC: {metrics.get('auc', 0):.4f}")
    print(f"AUC-PR: {metrics.get('auc_pr', 0):.4f}")

    if 'true_positive' in metrics:
        print(f"\nConfusion Matrix:")
        print(f"TP: {metrics['true_positive']}, TN: {metrics['true_negative']}")
        print(f"FP: {metrics['false_positive']}, FN: {metrics['false_negative']}")

        print(f"\nDetailed Metrics:")
        print(f"Sensitivity (Recall): {metrics.get('sensitivity', 0):.4f}")
        print(f"Specificity: {metrics.get('specificity', 0):.4f}")
        print(f"Balanced Accuracy: {metrics.get('balanced_accuracy', 0):.4f}")
        print(f"Matthews Correlation: {metrics.get('matthews_corrcoef', 0):.4f}")

    print("-" * 50)


def save_model(model_result, model_type, output_dir):
    """
    Modeli geliştirilmiş metriklerle kaydet
    """
    # Dizinin var olduğundan emin ol
    os.makedirs(output_dir, exist_ok=True)

    # Timestamp
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

    # Model dosyası
    model_path = os.path.join(output_dir, f"{model_type}_model_{timestamp}.joblib")
    joblib.dump(model_result['model'], model_path)

    # Model bilgi dosyası (genişletilmiş)
    info = {
        'timestamp': timestamp,
        'model_type': model_type,
        'model_path': model_path,
        'metrics': model_result['metrics'],
        'performance_summary': generate_performance_summary(model_result['metrics'])
    }

    # Özellik önemi varsa ekle
    if 'feature_importance' in model_result:
        info['feature_importance'] = model_result['feature_importance']

    info_path = os.path.join(output_dir, f"model_info_{timestamp}.json")

    with open(info_path, 'w', encoding='utf-8') as f:
        json.dump(info, f, indent=2, default=str)

    print(f"Model kaydedildi: {model_path}")
    print(f"Model bilgileri kaydedildi: {info_path}")

    return model_path, info_path


def generate_performance_summary(metrics):
    """
    Model performans özeti oluştur
    """
    accuracy = metrics.get('accuracy', 0)
    f1_score = metrics.get('f1_score', 0)
    auc = metrics.get('auc', 0)

    overall_score = (accuracy + f1_score + auc) / 3
    is_good_model = accuracy > 0.8 and f1_score > 0.7 and auc > 0.8

    # Zayıflıkları tespit et
    primary_weakness = "Genel performans kabul edilebilir"
    recommendations = []

    precision = metrics.get('precision', 0)
    recall = metrics.get('recall', 0)
    specificity = metrics.get('specificity', 0)
    imbalance_ratio = metrics.get('class_imbalance_ratio', 0)

    if precision < 0.7:
        primary_weakness = "Yüksek False Positive oranı - Precision düşük"
        recommendations.append("Class weights ayarlarını gözden geçirin")
    elif recall < 0.7:
        primary_weakness = "Yüksek False Negative oranı - Recall düşük"
        recommendations.append("Fraud sınıfı için daha fazla özellik mühendisliği yapın")
    elif specificity < 0.7:
        primary_weakness = "Normal işlemleri fraud olarak işaretliyor"
        recommendations.append("Model eşiğini ayarlayın")
    elif imbalance_ratio > 100:
        primary_weakness = "Ciddi sınıf dengesizliği problemi"
        recommendations.append("SMOTE veya benzeri oversampling teknikleri kullanın")

    if auc < 0.8:
        recommendations.append("Model karmaşıklığını artırın veya ensemble yöntemleri deneyin")

    return {
        'overall_score': overall_score,
        'is_good_model': is_good_model,
        'primary_weakness': primary_weakness,
        'recommended_actions': recommendations,
        'model_grade': 'A' if overall_score > 0.9 else 'B' if overall_score > 0.8 else 'C' if overall_score > 0.7 else 'D'
    }


def main():
    """Ana fonksiyon"""
    parser = argparse.ArgumentParser(description='Fraud Detection Model Training (Enhanced Metrics)')
    parser.add_argument('--data', type=str, required=True, help='CSV veri dosyasının yolu')
    parser.add_argument('--config', type=str, required=True, help='Konfigürasyon dosyasının yolu')
    parser.add_argument('--output', type=str, default='models', help='Çıktı dizini')
    parser.add_argument('--model-type', type=str, default='ensemble',
                        choices=['lightgbm', 'pca', 'ensemble'], help='Eğitilecek model tipi')

    args = parser.parse_args()

    try:
        print(f"Geliştirilmiş metriklerle {args.model_type} model eğitimi başlatılıyor...")

        # Veriyi yükle
        X_train, X_test, y_train, y_test = load_data(args.data)

        # Konfigürasyonu yükle
        config = load_config(args.config)

        # Model tipine göre eğitim
        if args.model_type == 'lightgbm':
            model_result = train_lightgbm(config, X_train, y_train, X_test, y_test)
        elif args.model_type == 'pca':
            model_result = train_pca(config, X_train, X_test, y_test)
        elif args.model_type == 'ensemble':
            model_result = train_ensemble(config, X_train, y_train, X_test, y_test)
        else:
            raise ValueError(f"Desteklenmeyen model tipi: {args.model_type}")

        # Modeli kaydet
        model_path, info_path = save_model(model_result, args.model_type, args.output)

        print(f"\n🎉 Model eğitimi başarıyla tamamlandı!")
        print(f"📊 Genel Skor: {model_result['metrics'].get('accuracy', 0):.4f}")
        print(f"🏆 Model Notu: {generate_performance_summary(model_result['metrics'])['model_grade']}")

    except Exception as e:
        print(f"❌ Hata: {e}")
        import traceback
        traceback.print_exc()
        exit(1)

def extract_feature_info(model_result, model_type):
    """
    Model'den feature bilgilerini çıkar
    """
    feature_info = {
        'model_type': model_type,
        'expected_feature_count': 0,
        'feature_columns': [],
        'feature_types': {}
    }

    try:
        model = model_result['model']

        if model_type == 'lightgbm':
            # LightGBM için beklenen feature'lar
            expected_features = [
                'Amount', 'AmountLog', 'TimeSin', 'TimeCos', 'DayOfWeek', 'HourOfDay',
                'V1', 'V2', 'V3', 'V4', 'V5', 'V6', 'V7', 'V8', 'V9', 'V10',
                'V11', 'V12', 'V13', 'V14', 'V15', 'V16', 'V17', 'V18', 'V19', 'V20',
                'V21', 'V22', 'V23', 'V24', 'V25', 'V26', 'V27', 'V28'
            ]

            feature_info['expected_feature_count'] = len(expected_features)
            feature_info['feature_columns'] = expected_features

            # Feature importance'tan da kontrol et
            if 'feature_importance' in model_result:
                actual_features = list(model_result['feature_importance'].keys())
                feature_info['actual_feature_columns'] = actual_features
                feature_info['actual_feature_count'] = len(actual_features)

                if len(actual_features) != len(expected_features):
                    print(
                        f"WARNING: Feature count mismatch! Expected: {len(expected_features)}, Actual: {len(actual_features)}")
                    print(f"Expected: {expected_features}")
                    print(f"Actual: {actual_features}")

        elif model_type == 'pca':
            # PCA için feature'lar
            expected_features = [
                'Amount', 'Time',
                'V1', 'V2', 'V3', 'V4', 'V5', 'V6', 'V7', 'V8', 'V9', 'V10',
                'V11', 'V12', 'V13', 'V14', 'V15', 'V16', 'V17', 'V18', 'V19', 'V20',
                'V21', 'V22', 'V23', 'V24', 'V25', 'V26', 'V27', 'V28'
            ]

            feature_info['expected_feature_count'] = len(expected_features)
            feature_info['feature_columns'] = expected_features

        elif model_type == 'ensemble':
            # Ensemble için LightGBM feature'larını kullan
            lightgbm_features = [
                'Amount', 'AmountLog', 'TimeSin', 'TimeCos', 'DayOfWeek', 'HourOfDay',
                'V1', 'V2', 'V3', 'V4', 'V5', 'V6', 'V7', 'V8', 'V9', 'V10',
                'V11', 'V12', 'V13', 'V14', 'V15', 'V16', 'V17', 'V18', 'V19', 'V20',
                'V21', 'V22', 'V23', 'V24', 'V25', 'V26', 'V27', 'V28'
            ]

            pca_features = [
                'Amount', 'Time',
                'V1', 'V2', 'V3', 'V4', 'V5', 'V6', 'V7', 'V8', 'V9', 'V10',
                'V11', 'V12', 'V13', 'V14', 'V15', 'V16', 'V17', 'V18', 'V19', 'V20',
                'V21', 'V22', 'V23', 'V24', 'V25', 'V26', 'V27', 'V28'
            ]

            feature_info['lightgbm_features'] = lightgbm_features
            feature_info['lightgbm_feature_count'] = len(lightgbm_features)
            feature_info['pca_features'] = pca_features
            feature_info['pca_feature_count'] = len(pca_features)

    except Exception as e:
        print(f"Feature info extraction error: {e}")
        feature_info['error'] = str(e)

    return feature_info


if __name__ == "__main__":
    main()