import os
import json
import numpy as np
import pandas as pd
from sklearn.model_selection import train_test_split


def load_data(csv_path):
    """
    Veri setini CSV dosyasından yükle ve gerekli dönüşümleri uygula

    Args:
        csv_path: CSV dosyasının yolu

    Returns:
        X_train, X_test, y_train, y_test: Eğitim ve test verileri
    """
    print(f"Veri seti yükleniyor: {csv_path}")

    # Dosya varlığını kontrol et
    if not os.path.exists(csv_path):
        raise FileNotFoundError(f"Veri seti dosyası bulunamadı: {csv_path}")

    # CSV'yi oku
    df = pd.read_csv(csv_path)

    # Gereken sütunları kontrol et
    required_columns = ['Time', 'Amount', 'Class'] + [f'V{i}' for i in range(1, 29)]
    missing_columns = [col for col in required_columns if col not in df.columns]

    if missing_columns:
        raise ValueError(f"Eksik sütunlar: {', '.join(missing_columns)}")

    # Veri seti hakkında bilgi
    print(f"Veri seti boyutu: {df.shape}")
    print(f"Dolandırıcılık oranı: {df['Class'].mean():.4f}")

    # Özellik mühendisliği
    # Zaman özellikleri
    seconds_in_day = 24 * 60 * 60
    df['TimeSin'] = np.sin(2 * np.pi * df['Time'] / seconds_in_day)
    df['TimeCos'] = np.cos(2 * np.pi * df['Time'] / seconds_in_day)
    df['DayFeature'] = (df['Time'] / seconds_in_day) % 7
    df['HourFeature'] = (df['Time'] / 3600) % 24

    # Tutar için logaritmik dönüşüm
    df['AmountLog'] = np.log1p(df['Amount'])

    # Özellikler ve hedef
    X = df.drop(['Class'], axis=1)
    y = df['Class']

    # Verileri böl
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42, stratify=y)

    print(f"Eğitim seti: {X_train.shape}, Test seti: {X_test.shape}")
    print(f"Eğitim setindeki dolandırıcılık oranı: {y_train.mean():.4f}")
    print(f"Test setindeki dolandırıcılık oranı: {y_test.mean():.4f}")

    return X_train, X_test, y_train, y_test


def load_config(config_path):
    """
    Konfigürasyon dosyasını yükle

    Args:
        config_path: JSON konfigürasyon dosyasının yolu

    Returns:
        Konfigürasyon sözlüğü
    """
    print(f"Konfigürasyon yükleniyor: {config_path}")

    # Dosya varlığını kontrol et
    if not os.path.exists(config_path):
        raise FileNotFoundError(f"Konfigürasyon dosyası bulunamadı: {config_path}")

    # JSON'ı oku
    with open(config_path, 'r', encoding='utf-8') as f:
        config = json.load(f)

    print(f"Konfigürasyon yüklendi: {list(config.keys())}")
    return config

def prepare_features_for_training(df, model_type='lightgbm'):
    """
    Training için feature preparation - prediction ile uyumlu
    """
    print(f"Preparing features for training: {model_type}")
    print(f"Input shape: {df.shape}")

    df_processed = df.copy()

    if model_type == 'lightgbm':
        # LightGBM için özel feature engineering

        # Amount features
        if 'Amount' in df_processed.columns:
            df_processed['AmountLog'] = np.log1p(df_processed['Amount'])

        # Time features
        if 'Time' in df_processed.columns:
            seconds_in_day = 24 * 60 * 60
            df_processed['TimeSin'] = np.sin(2 * np.pi * df_processed['Time'] / seconds_in_day)
            df_processed['TimeCos'] = np.cos(2 * np.pi * df_processed['Time'] / seconds_in_day)
            df_processed['DayOfWeek'] = ((df_processed['Time'] / seconds_in_day) % 7).astype(int)
            df_processed['HourOfDay'] = ((df_processed['Time'] / 3600) % 24).astype(int)

        # Expected features for LightGBM
        expected_features = [
            'Amount', 'AmountLog', 'TimeSin', 'TimeCos', 'DayOfWeek', 'HourOfDay',
            'V1', 'V2', 'V3', 'V4', 'V5', 'V6', 'V7', 'V8', 'V9', 'V10',
            'V11', 'V12', 'V13', 'V14', 'V15', 'V16', 'V17', 'V18', 'V19', 'V20',
            'V21', 'V22', 'V23', 'V24', 'V25', 'V26', 'V27', 'V28'
        ]

    elif model_type == 'pca':
        # PCA için temel features
        expected_features = [
            'Amount', 'Time',
            'V1', 'V2', 'V3', 'V4', 'V5', 'V6', 'V7', 'V8', 'V9', 'V10',
            'V11', 'V12', 'V13', 'V14', 'V15', 'V16', 'V17', 'V18', 'V19', 'V20',
            'V21', 'V22', 'V23', 'V24', 'V25', 'V26', 'V27', 'V28'
        ]

    # Sadece expected features'ı seç
    available_features = [f for f in expected_features if f in df_processed.columns]
    missing_features = [f for f in expected_features if f not in df_processed.columns]

    if missing_features:
        print(f"Warning: Missing features in training data: {missing_features}")

    df_final = df_processed[available_features].copy()

    print(f"Final training features ({len(df_final.columns)}): {list(df_final.columns)}")

    return df_final