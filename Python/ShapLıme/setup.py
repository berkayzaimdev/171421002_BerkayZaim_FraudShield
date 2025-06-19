#!/usr/bin/env python3
"""
Fraud Detection Explainability Kurulum Scripti
Gerekli dizinleri ve model dosyaları için placeholder'lar oluşturur
"""

import os
import json
import shutil
from datetime import datetime

def create_directory_structure():
    """Gerekli dizin yapısını oluştur"""
    directories = [
        'models',
        'results',
        'batch_results',
        'explanations',
        'sample_data',
        'demo_results'
    ]
    
    for directory in directories:
        os.makedirs(directory, exist_ok=True)
        print(f"✅ {directory}/ dizini oluşturuldu")

def create_model_placeholders():
    """Model dosyaları için placeholder'lar oluştur"""
    model_files = {
        'models/fraud_model_ensemble.joblib': 'Bu dosya eğitilmiş ensemble modelini içermelidir',
        'models/fraud_model_lightgbm.joblib': 'Bu dosya eğitilmiş LightGBM modelini içermelidir',
        'models/fraud_model_pca.joblib': 'Bu dosya eğitilmiş PCA modelini içermelidir'
    }
    
    for model_file, message in model_files.items():
        if not os.path.exists(model_file):
            with open(model_file + '.placeholder', 'w') as f:
                json.dump({
                    'model_type': os.path.basename(model_file).replace('.joblib', ''),
                    'created_at': datetime.now().isoformat(),
                    'status': 'placeholder',
                    'message': message,
                    'required_format': 'joblib',
                    'training_required': True
                }, f, indent=2)
            print(f"⚠️ {model_file} için placeholder oluşturuldu")
        else:
            print(f"✅ {model_file} zaten mevcut")

def create_readme():
    """README dosyası oluştur"""
    readme_content = """# Fraud Detection Explainability Sistemi

## Kurulum

1. Gerekli dizinler oluşturuldu:
   - `models/`: Model dosyaları için
   - `results/`: Tek transaction analiz sonuçları için
   - `batch_results/`: Toplu analiz sonuçları için
   - `explanations/`: Açıklama dosyaları için
   - `sample_data/`: Örnek veriler için
   - `demo_results/`: Demo sonuçları için

2. Model Dosyaları:
   - `models/fraud_model_ensemble.joblib`: Ensemble model
   - `models/fraud_model_lightgbm.joblib`: LightGBM model
   - `models/fraud_model_pca.joblib`: PCA model

## Model Eğitimi

Model dosyalarını oluşturmak için:

1. Veri setini hazırlayın
2. Model eğitimi yapın
3. Eğitilmiş modelleri .joblib formatında kaydedin
4. Model dosyalarını `models/` dizinine yerleştirin

## Kullanım

1. Tek Transaction Analizi:
   ```bash
   python fraud_explainer_cli.py explain-single -i sample_data/sample_transaction.json -m Ensemble -o results/
   ```

2. Toplu Analiz:
   ```bash
   python fraud_explainer_cli.py explain-batch -i sample_data/sample_batch.json -m LightGBM -o batch_results/
   ```

3. API Testi:
   ```bash
   python fraud_explainer_cli.py api-test -m Ensemble
   ```

## Önemli Notlar

- Model dosyaları olmadan sadece API sonuçları gösterilir
- SHAP/LIME analizleri için model dosyaları gereklidir
- Model dosyaları .joblib formatında olmalıdır
- API'nin çalışır durumda olması gerekiyor (port: 5112)
"""
    
    with open('README.md', 'w', encoding='utf-8') as f:
        f.write(readme_content)
    print("✅ README.md dosyası oluşturuldu")

def main():
    """Ana kurulum fonksiyonu"""
    print("=== FRAUD DETECTION EXPLAINABILITY KURULUM ===")
    
    # Dizin yapısını oluştur
    create_directory_structure()
    
    # Model placeholder'ları oluştur
    create_model_placeholders()
    
    # README oluştur
    create_readme()
    
    print("\n=== KURULUM TAMAMLANDI ===")
    print("\nÖnemli Notlar:")
    print("1. Model dosyalarını eğitip models/ dizinine eklemelisiniz")
    print("2. API'nin çalışır durumda olduğundan emin olun (port: 5112)")
    print("3. Detaylı bilgi için README.md dosyasını inceleyin")

if __name__ == "__main__":
    main() 