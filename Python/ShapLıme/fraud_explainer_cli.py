#!/usr/bin/env python3
"""
Fraud Detection Explainability CLI Tool
Komut satırından kolayca fraud açıklaması yapmanızı sağlar
"""

import argparse
import json
import sys
import os
from datetime import datetime
from typing import Dict, List
import pandas as pd

# Ana explainer sınıfını import et
try:
    from python_api_explainer import FraudDetectionAPIClient, ExplainabilityAnalyzer
except ImportError:
    print("❌ python_api_explainer.py dosyası bulunamadı!")
    print("Bu dosyanın aynı klasörde olduğundan emin olun.")
    sys.exit(1)


class FraudExplainerCLI:
    """Command Line Interface for Fraud Explainability"""

    def __init__(self):
        self.api_client = None
        self.analyzer = None

    def setup_clients(self, api_url: str, models_path: str):
        """API client ve analyzer'ı kur"""
        print(f"🔧 Clients kuruluyor...")
        print(f"API URL: {api_url}")
        print(f"Models Path: {models_path}")

        self.api_client = FraudDetectionAPIClient(api_url)

        # Health check
        if not self.api_client.health_check():
            print(f"❌ API'ye bağlanılamıyor: {api_url}")
            print("API'nin çalıştığından emin olun.")
            return False

        print("✅ API bağlantısı başarılı!")

        self.analyzer = ExplainabilityAnalyzer(self.api_client, models_path)
        print("✅ Analyzer kuruldu!")

        return True

    def explain_single_transaction(self, args):
        """Tek transaction açıkla"""
        print(f"=== SINGLE TRANSACTION EXPLANATION ===")

        # Transaction data yükle
        if args.input.endswith('.json'):
            with open(args.input, 'r', encoding='utf-8') as f:
                transaction_data = json.load(f)
        else:
            print("❌ Sadece JSON dosyalar destekleniyor")
            return False

        print(f"📄 Transaction yüklendi: {args.input}")
        print(f"Transaction ID: {transaction_data.get('transactionId', 'N/A')}")

        # Açıklama yap
        try:
            explanation = self.analyzer.explain_transaction(
                transaction_data=transaction_data,
                model_type=args.model_type,
                method=args.method,
                output_dir=args.output_dir
            )

            if 'error' in explanation:
                print(f"❌ Açıklama hatası: {explanation['error']}")
                return False

            # Sonuçları göster
            self._print_explanation_summary(explanation)

            # Sonucu kaydet
            output_file = os.path.join(args.output_dir, 'explanation_result.json')
            with open(output_file, 'w', encoding='utf-8') as f:
                json.dump(explanation, f, indent=2, ensure_ascii=False, default=str)

            print(f"💾 Sonuç kaydedildi: {output_file}")
            return True

        except Exception as e:
            print(f"❌ Hata: {e}")
            return False

    def explain_batch_transactions(self, args):
        """Batch transaction açıkla"""
        print(f"=== BATCH TRANSACTION EXPLANATION ===")

        # Transaction listesi yükle
        if args.input.endswith('.json'):
            with open(args.input, 'r', encoding='utf-8') as f:
                data = json.load(f)
                if isinstance(data, list):
                    transactions = data
                else:
                    transactions = [data]
        elif args.input.endswith('.csv'):
            df = pd.read_csv(args.input)
            transactions = df.to_dict('records')
        else:
            print("❌ Sadece JSON ve CSV dosyalar destekleniyor")
            return False

        print(f"📄 {len(transactions)} transaction yüklendi")

        # Batch limit kontrolü
        if len(transactions) > args.batch_limit:
            print(f"⚠️ Transaction sayısı limit aşıyor ({len(transactions)} > {args.batch_limit})")
            if not self._confirm_action("Devam etmek istiyor musunuz?"):
                return False

        # Batch açıklama
        try:
            results = self.analyzer.batch_explain(
                transactions=transactions,
                model_type=args.model_type,
                method=args.method
            )

            # Sonuçları göster
            print(f"\n📊 BATCH RESULTS:")
            print(f"Total Transactions: {results['total_transactions']}")
            print(f"Successful: {results['successful_explanations']}")
            print(f"Failed: {results['failed_explanations']}")

            if results['failed_explanations'] > 0:
                print(f"\n❌ Failed Transactions:")
                for error in results['errors'][:5]:  # İlk 5 hatayı göster
                    print(f"  • {error['transaction_id']}: {error['error']}")

            # Sonucu kaydet
            output_file = os.path.join(args.output_dir, 'batch_explanation_results.json')
            with open(output_file, 'w', encoding='utf-8') as f:
                json.dump(results, f, indent=2, ensure_ascii=False, default=str)

            print(f"💾 Batch sonuçları kaydedildi: {output_file}")

            # Özet rapor oluştur
            self._create_batch_summary_report(results, args.output_dir)

            return True

        except Exception as e:
            print(f"❌ Batch processing hatası: {e}")
            return False

    def create_sample_transactions(self, args):
        """Örnek transaction'lar oluştur"""
        print(f"=== SAMPLE TRANSACTIONS CREATION ===")

        samples = []
        for i in range(args.count):
            sample = self._create_sample_transaction(i)
            samples.append(sample)

        # Kaydet
        output_file = args.output if args.output else 'sample_transactions.json'
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(samples, f, indent=2, ensure_ascii=False)

        print(f"✅ {args.count} örnek transaction oluşturuldu: {output_file}")
        return True

    def api_test(self, args):
        """API bağlantısını test et"""
        print(f"=== API TEST ===")

        # Basic health check
        if not self.api_client.health_check():
            print("❌ API Health Check başarısız!")
            return False

        print("✅ API Health Check başarılı!")

        # Test prediction
        sample_transaction = self._create_sample_transaction(0)

        try:
            print("🧪 Test prediction yapılıyor...")

            if args.model_type.lower() == "ensemble":
                result = self.api_client.predict(sample_transaction)
            else:
                result = self.api_client.predict_with_model(args.model_type, sample_transaction)

            if "error" in result:
                print(f"❌ Prediction hatası: {result['error']}")
                return False

            print("✅ Test prediction başarılı!")
            print(f"Fraud Probability: {result.get('Probability', 'N/A')}")
            print(f"Predicted Class: {'FRAUD' if result.get('IsFraudulent', False) else 'NORMAL'}")

            return True

        except Exception as e:
            print(f"❌ Test hatası: {e}")
            return False

    def _print_explanation_summary(self, explanation: Dict):
        """Açıklama özetini yazdır"""
        print(f"\n📋 EXPLANATION SUMMARY:")
        print("=" * 50)

        # API Prediction
        api_pred = explanation.get('api_prediction', {})
        probability = float(api_pred.get('Probability', '0.0'))
        print(f"🎯 Fraud Probability: {probability:.4f}")
        print(f"🏷️ Predicted Class: {'FRAUD' if api_pred.get('IsFraudulent', False) else 'NORMAL'}")
        print(f"📊 Anomaly Score: {api_pred.get('AnomalyScore', 'N/A')}")

        # Business Explanation
        business = explanation.get('business_explanation', {})
        if 'summary' in business:
            summary = business['summary']
            print(f"\n📋 Business Decision:")
            print(f"   Decision: {summary.get('decision', 'N/A')}")
            print(f"   Risk Level: {summary.get('risk_level', 'N/A')}")
            print(f"   Confidence: {summary.get('confidence', 'N/A')}")

        # Risk Factors
        if 'risk_factors' in business and business['risk_factors']:
            print(f"\n⚠️ Risk Factors:")
            for factor in business['risk_factors'][:3]:
                print(f"   • {factor.get('factor', 'N/A')}: {factor.get('value', 'N/A')}")

        # Key Insights
        if 'key_insights' in business and business['key_insights']:
            print(f"\n🔍 Key Insights:")
            for insight in business['key_insights'][:3]:
                print(f"   • {insight}")

        # Explanations Available
        explanations = explanation.get('explanations', {})
        if explanations:
            print(f"\n🔬 Available Explanations:")
            if 'shap' in explanations:
                print("   ✅ SHAP Analysis")
            if 'lime' in explanations:
                print("   ✅ LIME Analysis")

    def _create_batch_summary_report(self, results: Dict, output_dir: str):
        """Batch sonuçları için özet rapor oluştur"""
        try:
            # HTML rapor şablonu
            html_template = """
            <!DOCTYPE html>
            <html>
            <head>
                <title>Fraud Detection Batch Analysis Report</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 40px; }
                    .header { background-color: #f4f4f4; padding: 20px; border-radius: 5px; }
                    .summary { margin: 20px 0; }
                    .transaction { border: 1px solid #ddd; margin: 10px 0; padding: 15px; border-radius: 5px; }
                    .high-risk { background-color: #ffebee; }
                    .medium-risk { background-color: #fff3e0; }
                    .low-risk { background-color: #e8f5e8; }
                    .error { background-color: #ffcccb; }
                    .stats { display: flex; justify-content: space-around; }
                    .stat-box { text-align: center; padding: 10px; background-color: #f9f9f9; border-radius: 5px; }
                </style>
            </head>
            <body>
                <div class="header">
                    <h1>🔍 Fraud Detection Batch Analysis Report</h1>
                    <p>Generated: {timestamp}</p>
                </div>

                <div class="summary">
                    <h2>📊 Summary Statistics</h2>
                    <div class="stats">
                        <div class="stat-box">
                            <h3>{total_transactions}</h3>
                            <p>Total Transactions</p>
                        </div>
                        <div class="stat-box">
                            <h3>{successful_explanations}</h3>
                            <p>Successful</p>
                        </div>
                        <div class="stat-box">
                            <h3>{failed_explanations}</h3>
                            <p>Failed</p>
                        </div>
                        <div class="stat-box">
                            <h3>{high_risk_count}</h3>
                            <p>High Risk</p>
                        </div>
                    </div>
                </div>

                <div class="transactions">
                    <h2>🚨 High Risk Transactions</h2>
                    {high_risk_transactions}
                </div>

                <div class="errors">
                    <h2>❌ Failed Analyses</h2>
                    {error_transactions}
                </div>
            </body>
            </html>
            """

            # High risk transactions
            high_risk_transactions = []
            high_risk_count = 0

            for result in results.get('results', []):
                api_pred = result.get('api_prediction', {})
                prob = float(api_pred.get('Probability', '0.0'))

                if prob > 0.7:
                    high_risk_count += 1
                    transaction_html = f"""
                    <div class="transaction high-risk">
                        <h4>Transaction ID: {result.get('transaction_id', 'N/A')}</h4>
                        <p><strong>Fraud Probability:</strong> {prob:.1%}</p>
                        <p><strong>Decision:</strong> INVESTIGATE</p>
                    </div>
                    """
                    high_risk_transactions.append(transaction_html)

            # Error transactions
            error_transactions = []
            for error in results.get('errors', []):
                error_html = f"""
                <div class="transaction error">
                    <h4>Transaction ID: {error.get('transaction_id', 'N/A')}</h4>
                    <p><strong>Error:</strong> {error.get('error', 'N/A')}</p>
                </div>
                """
                error_transactions.append(error_html)

            # HTML'i oluştur
            html_content = html_template.format(
                timestamp=results.get('timestamp', 'N/A'),
                total_transactions=results.get('total_transactions', 0),
                successful_explanations=results.get('successful_explanations', 0),
                failed_explanations=results.get('failed_explanations', 0),
                high_risk_count=high_risk_count,
                high_risk_transactions=''.join(
                    high_risk_transactions) if high_risk_transactions else '<p>No high risk transactions found.</p>',
                error_transactions=''.join(error_transactions) if error_transactions else '<p>No errors occurred.</p>'
            )

            # Raporu kaydet
            report_file = os.path.join(output_dir, 'batch_summary_report.html')
            with open(report_file, 'w', encoding='utf-8') as f:
                f.write(html_content)

            print(f"📊 HTML rapor oluşturuldu: {report_file}")

        except Exception as e:
            print(f"⚠️ Rapor oluşturma hatası: {e}")

    def _create_sample_transaction(self, index: int) -> Dict:
        """Örnek transaction oluştur - Gerçek API format'ında"""
        import random
        import uuid
        from datetime import datetime

        # Realistic fraud detection transaction
        base_amount = random.choice([50, 100, 250, 500, 1000, 2500, 5000])
        amount_variation = random.uniform(0.8, 1.5)
        amount = round(base_amount * amount_variation, 2)

        # Random risk factors
        is_high_risk = random.choice([True, False])

        return {
            'transactionId': str(uuid.uuid4()),
            'userId': str(uuid.uuid4()),
            'amount': amount,
            'merchantId': f'MERCHANT_{random.randint(1000, 9999)}',
            'timestamp': datetime.now().isoformat(),
            'type': 0,  # Purchase

            # Location
            'latitude': random.uniform(25.0, 49.0),  # US bounds
            'longitude': random.uniform(-125.0, -66.0),
            'country': 'US',
            'city': random.choice(['New York', 'Los Angeles', 'Chicago', 'Houston', 'Phoenix']),
            'isHighRiskRegion': is_high_risk,

            # Device Info
            'deviceId': f'DEVICE_{random.randint(10000, 99999)}',
            'deviceType': random.choice(['Mobile', 'Desktop', 'Tablet']),
            'ipAddress': f'192.168.{random.randint(1, 255)}.{random.randint(1, 255)}',
            'userAgent': 'Mozilla/5.0 (compatible sample)',
            'ipChanged': random.choice([True, False]),

            # Card Info
            'cardType': random.choice(['Visa', 'MasterCard', 'Amex']),
            'cardBin': f'{random.randint(400000, 599999)}',
            'cardLast4': f'{random.randint(1000, 9999)}',
            'cardExpiryMonth': random.randint(1, 12),
            'cardExpiryYear': random.randint(2024, 2029),
            'bankName': random.choice(['Chase', 'Bank of America', 'Wells Fargo', 'Citi']),
            'bankCountry': 'US',

            # Risk factors
            'daysSinceFirstTransaction': random.randint(1, 1000),
            'transactionVelocity24h': random.randint(1, 15),
            'averageTransactionAmount': round(random.uniform(50, 500), 2),
            'isNewPaymentMethod': random.choice([True, False]),
            'isInternational': random.choice([True, False]),

            # V Features - Random realistic values
            **{f'V{i}': round(random.gauss(0, 1), 6) for i in range(1, 29)},

            'isFraudulent': False
        }

    def _confirm_action(self, message: str) -> bool:
        """Kullanıcıdan onay al"""
        response = input(f"{message} (y/N): ").strip().lower()
        return response in ['y', 'yes', 'evet']


def main():
    """Ana CLI fonksiyonu"""
    parser = argparse.ArgumentParser(
        description='Fraud Detection Explainability CLI Tool',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Örnekler:
  # Tek transaction açıkla
  python fraud_explainer_cli.py explain-single -i transaction.json -m Ensemble -o results/

  # Batch transaction açıkla
  python fraud_explainer_cli.py explain-batch -i transactions.json -m LightGBM -o batch_results/

  # API test et
  python fraud_explainer_cli.py api-test -m Ensemble

  # Örnek transaction'lar oluştur
  python fraud_explainer_cli.py create-samples -c 10 -o samples.json
        """
    )

    # Global arguments
    parser.add_argument('--api-url', default='http://localhost:5112', help='API URL (default: http://localhost:5112)')
    parser.add_argument('--models-path', default='models', help='Models directory path (default: models)')

    # Subcommands
    subparsers = parser.add_subparsers(dest='command', help='Available commands')

    # Explain single transaction
    single_parser = subparsers.add_parser('explain-single', help='Explain single transaction')
    single_parser.add_argument('-i', '--input', required=True, help='Input transaction JSON file')
    single_parser.add_argument('-m', '--model-type', default='Ensemble', choices=['Ensemble', 'LightGBM', 'PCA'],
                               help='Model type')
    single_parser.add_argument('--method', default='both', choices=['shap', 'lime', 'both'], help='Explanation method')
    single_parser.add_argument('-o', '--output-dir', default='explanations', help='Output directory')

    # Explain batch transactions
    batch_parser = subparsers.add_parser('explain-batch', help='Explain batch transactions')
    batch_parser.add_argument('-i', '--input', required=True, help='Input transactions file (JSON/CSV)')
    batch_parser.add_argument('-m', '--model-type', default='Ensemble', choices=['Ensemble', 'LightGBM', 'PCA'],
                              help='Model type')
    batch_parser.add_argument('--method', default='shap', choices=['shap', 'lime', 'both'], help='Explanation method')
    batch_parser.add_argument('-o', '--output-dir', default='batch_explanations', help='Output directory')
    batch_parser.add_argument('--batch-limit', type=int, default=100, help='Maximum batch size')

    # API test
    test_parser = subparsers.add_parser('api-test', help='Test API connection and prediction')
    test_parser.add_argument('-m', '--model-type', default='Ensemble', choices=['Ensemble', 'LightGBM', 'PCA'],
                             help='Model type for testing')

    # Create samples
    samples_parser = subparsers.add_parser('create-samples', help='Create sample transactions')
    samples_parser.add_argument('-c', '--count', type=int, default=5, help='Number of samples to create')
    samples_parser.add_argument('-o', '--output', help='Output file (default: sample_transactions.json)')

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        return

    # CLI Tool oluştur
    cli = FraudExplainerCLI()

    # Create samples komutu için client setup gerekmez
    if args.command == 'create-samples':
        success = cli.create_sample_transactions(args)
        sys.exit(0 if success else 1)

    # Diğer komutlar için client setup
    if not cli.setup_clients(args.api_url, args.models_path):
        sys.exit(1)

    # Komutları çalıştır
    success = False

    if args.command == 'explain-single':
        success = cli.explain_single_transaction(args)
    elif args.command == 'explain-batch':
        success = cli.explain_batch_transactions(args)
    elif args.command == 'api-test':
        success = cli.api_test(args)

    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()