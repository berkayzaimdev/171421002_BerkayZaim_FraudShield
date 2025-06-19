#!/usr/bin/env python3
"""
Fraud Detection Analysis Starter Script
Kolay başlatma ve kullanım için wrapper script
"""

import os
import sys
import subprocess
from pathlib import Path

# Python modüllerinin bulunduğu dizini PATH'e ekle
current_dir = Path(__file__).parent
sys.path.insert(0, str(current_dir))


def check_requirements():
    """Gerekli modüllerin yüklü olup olmadığını kontrol et"""
    required_modules = [
        'requests', 'pandas', 'numpy', 'matplotlib', 'seaborn', 'sqlite3'
    ]

    missing_modules = []

    for module in required_modules:
        try:
            __import__(module)
        except ImportError:
            missing_modules.append(module)

    if missing_modules:
        print("❌ Eksik modüller tespit edildi:")
        for module in missing_modules:
            print(f"  - {module}")
        print("\n📦 Kurulum için:")
        print(f"pip install {' '.join(missing_modules)}")
        return False

    return True


def check_api_connection(api_url="http://localhost:5112"):
    """API bağlantısını kontrol et"""
    try:
        import requests
        response = requests.get(f"{api_url}/health", timeout=5)
        return response.status_code == 200
    except:
        return False


def main():
    """Ana menü"""
    print("🎯 Fraud Detection Model Analiz Sistemi")
    print("=" * 60)

    # Gereklilik kontrolü
    if not check_requirements():
        return

    # API bağlantı kontrolü
    api_url = "http://localhost:5112"
    if not check_api_connection(api_url):
        print(f"⚠️  API bağlantısı kontrol edilemiyor: {api_url}")
        print("API'nin çalıştığından emin olun ve tekrar deneyin.")

        user_input = input("\nYine de devam etmek istiyor musunuz? (y/N): ").strip().lower()
        if user_input != 'y':
            return
    else:
        print(f"✅ API bağlantısı başarılı: {api_url}")

    print("\n📋 Mevcut Analiz Seçenekleri:")
    print("1. 🏃‍♂️ Hızlı Model Analizi (3 model, ~5 dakika)")
    print("2. 🔬 Kapsamlı Model Analizi (8 model, ~15 dakika)")
    print("3. 🔧 Hiperparametre Optimizasyonu (20+ deneyim, ~30 dakika)")
    print("4. 🏭 Production Önerileri (2 model, ~3 dakika)")
    print("5. 🔄 Mevcut Modelleri Karşılaştır")
    print("6. 🧪 Özel Deneyim (JSON konfigürasyon)")
    print("7. 📊 Toplu Model Eğitimi")
    print("8. 🎛️  Manuel Komut Girişi")
    print("0. ❌ Çıkış")

    while True:
        choice = input("\n🎯 Seçiminizi yapın (0-8): ").strip()

        if choice == "0":
            print("👋 Çıkış yapılıyor...")
            break

        elif choice == "1":
            print("\n🏃‍♂️ Hızlı analiz başlatılıyor...")
            run_analysis("--quick")

        elif choice == "2":
            print("\n🔬 Kapsamlı analiz başlatılıyor...")
            run_analysis("--comprehensive")

        elif choice == "3":
            print("\n🔧 Hiperparametre optimizasyonu menüsü:")
            print("  a) Sadece LightGBM")
            print("  b) Sadece PCA")
            print("  c) Sadece Ensemble")
            print("  d) Tümü (önerilen)")

            opt_choice = input("Seçim (a/b/c/d): ").strip().lower()

            if opt_choice == "a":
                run_analysis("--optimize", "lightgbm")
            elif opt_choice == "b":
                run_analysis("--optimize", "pca")
            elif opt_choice == "c":
                run_analysis("--optimize", "ensemble")
            elif opt_choice == "d":
                run_analysis("--optimize", "lightgbm", "pca", "ensemble")
            else:
                print("❌ Geçersiz seçim!")

        elif choice == "4":
            print("\n🏭 Production önerileri oluşturuluyor...")
            run_analysis("--production-recommendations")

        elif choice == "5":
            model_names = input("Karşılaştırılacak model isimlerini girin (virgülle ayırın): ").strip()
            if model_names:
                models = [name.strip() for name in model_names.split(",")]
                run_analysis("--compare-models", *models)
            else:
                print("❌ Model isimleri gerekli!")

        elif choice == "6":
            config_file = input("JSON konfigürasyon dosyası yolu: ").strip()
            if os.path.exists(config_file):
                run_analysis("--custom-config", config_file)
            else:
                print("❌ Dosya bulunamadı!")

        elif choice == "7":
            print("\n📊 Toplu model eğitimi başlatılıyor...")
            run_batch_training()

        elif choice == "8":
            print("\n🎛️  Manuel komut girişi:")
            print("Örnekler:")
            print("  --quick")
            print("  --optimize lightgbm pca")
            print("  --compare-models Model1 Model2")

            manual_cmd = input("Komut: ").strip()
            if manual_cmd:
                run_analysis(*manual_cmd.split())

        else:
            print("❌ Geçersiz seçim! Lütfen 0-8 arası bir sayı girin.")


def run_analysis(*args):
    """Analiz scriptini çalıştır"""
    try:
        from main_coordinator import FraudDetectionAnalyzer

        # Analyzer'ı oluştur
        analyzer = FraudDetectionAnalyzer()

        # Komuta göre işlem yap
        if "--quick" in args:
            analyzer.run_quick_analysis()
        elif "--comprehensive" in args:
            analyzer.run_comprehensive_analysis()
        elif "--optimize" in args:
            models = [arg for arg in args if arg in ["lightgbm", "pca", "ensemble"]]
            analyzer.run_hyperparameter_optimization(models if models else None)
        elif "--production-recommendations" in args:
            analyzer.generate_production_recommendations()
        elif "--compare-models" in args:
            # --compare-models'den sonraki tüm argümanlar model isimleri
            idx = list(args).index("--compare-models")
            model_names = list(args)[idx + 1:]
            if model_names:
                analyzer.compare_existing_models(model_names)
        elif "--custom-config" in args:
            idx = list(args).index("--custom-config")
            if idx + 1 < len(args):
                config_file = args[idx + 1]
                if os.path.exists(config_file):
                    import json
                    with open(config_file, 'r') as f:
                        config = json.load(f)
                    analyzer.run_custom_experiment(config)

        print("\n✅ İşlem tamamlandı!")

    except Exception as e:
        print(f"❌ Hata oluştu: {str(e)}")
        print("Detaylı log için main_coordinator.py'yi manuel çalıştırın.")


def run_batch_training():
    """Toplu model eğitimi çalıştır"""
    try:
        from batch_processor import BatchModelProcessor, ExperimentGenerator
        from api_client import FraudDetectionAPIClient

        client = FraudDetectionAPIClient("http://localhost:5112")
        processor = BatchModelProcessor(client, "batch_results", max_workers=2)

        print("📊 Toplu eğitim türü seçin:")
        print("1. LightGBM Grid Search (10 deneyim)")
        print("2. Rastgele Deneyimler (15 deneyim)")
        print("3. Önceden Tanımlı Karışık Deneyimler (5 deneyim)")

        batch_choice = input("Seçim (1/2/3): ").strip()

        if batch_choice == "1":
            # LightGBM grid experiments
            param_grid = {
                "numberOfTrees": [500, 1000, 1500],
                "numberOfLeaves": [64, 128, 256],
                "learningRate": [0.005, 0.01, 0.02],
                "classWeightRatio": [50, 75, 100]
            }
            configs = ExperimentGenerator.generate_lightgbm_grid_experiments(param_grid)[:10]
            processor.run_lightgbm_experiments(configs)

        elif batch_choice == "2":
            # Random experiments
            configs = ExperimentGenerator.generate_random_experiments("lightgbm", 15)
            processor.run_lightgbm_experiments(configs)

        elif batch_choice == "3":
            # Mixed preset experiments
            experiments = ExperimentGenerator.create_preset_experiments()
            processor.run_mixed_experiments(experiments)

        else:
            print("❌ Geçersiz seçim!")

    except Exception as e:
        print(f"❌ Toplu eğitim hatası: {str(e)}")


def create_sample_files():
    """Örnek dosyalar oluştur"""
    # Örnek konfigürasyon dosyası
    sample_config = {
        "name": "Örnek Model Deneyi",
        "description": "Özel model parametreleri ile deneyim",
        "models": {
            "Custom_LightGBM_HighAccuracy": {
                "type": "lightgbm",
                "config": {
                    "numberOfTrees": 1200,
                    "numberOfLeaves": 150,
                    "learningRate": 0.005,
                    "classWeights": {"0": 1.0, "1": 80.0}
                }
            },
            "Custom_PCA_Sensitive": {
                "type": "pca",
                "config": {
                    "componentCount": 20,
                    "anomalyThreshold": 2.0
                }
            }
        }
    }

    with open("örnek_deneyim.json", "w", encoding="utf-8") as f:
        import json
        json.dump(sample_config, f, indent=2, ensure_ascii=False)

    print("📝 Örnek dosyalar oluşturuldu:")
    print("  - örnek_deneyim.json")


if __name__ == "__main__":
    # Örnek dosyaları oluştur
    if not os.path.exists("örnek_deneyim.json"):
        create_sample_files()

    try:
        main()
    except KeyboardInterrupt:
        print("\n\n👋 Kullanıcı tarafından iptal edildi. Çıkış yapılıyor...")
    except Exception as e:
        print(f"\n❌ Beklenmeyen hata: {str(e)}")
        print("Detaylar için loglara bakın.")