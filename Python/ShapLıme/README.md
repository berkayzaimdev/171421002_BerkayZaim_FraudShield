# Fraud Detection Explainability Sistemi

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
