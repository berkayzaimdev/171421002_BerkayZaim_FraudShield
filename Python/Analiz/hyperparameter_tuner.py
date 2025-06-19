def _create_html_report(self, tuning_results: Dict) -> str:
    """Hiperparametre optimizasyonu için HTML raporu oluştur"""
    print("\n📊 Hiperparametre optimizasyon raporu oluşturuluyor...")

    html_content = """
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Hiperparametre Optimizasyon Raporu</title>
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

        h1, h2, h3, h4 { 
            color: var(--primary-color);
            margin-top: 0;
        }

        h1 { 
            text-align: center;
            border-bottom: 3px solid var(--secondary-color);
            padding-bottom: 15px;
            margin-bottom: 30px;
            font-size: 2.5em;
        }

        h2 { 
            border-left: 4px solid var(--secondary-color);
            padding-left: 15px;
            margin-top: 40px;
            font-size: 1.8em;
        }

        .optimization-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
            gap: 25px;
            margin: 30px 0;
        }

        .optimization-card {
            background: var(--card-bg);
            padding: 25px;
            border-radius: var(--border-radius);
            box-shadow: var(--box-shadow);
            border-top: 4px solid var(--secondary-color);
            transition: transform 0.2s;
        }

        .optimization-card:hover {
            transform: translateY(-5px);
        }

        .parameter-section {
            margin: 25px 0;
            padding: 20px;
            background: var(--light-bg);
            border-radius: var(--border-radius);
        }

        .parameter-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 15px;
            margin-top: 15px;
        }

        .parameter-card {
            background: white;
            padding: 15px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        }

        .parameter-name {
            font-weight: 600;
            color: var(--primary-color);
            margin-bottom: 8px;
        }

        .parameter-value {
            color: var(--secondary-color);
            font-weight: 600;
        }

        .parameter-range {
            font-size: 0.9em;
            color: #666;
            margin-top: 5px;
        }

        .metric-section {
            margin: 25px 0;
            padding: 20px;
            background: var(--light-bg);
            border-radius: var(--border-radius);
        }

        .metric-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            margin-top: 15px;
        }

        .metric-card {
            background: white;
            padding: 15px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
            text-align: center;
        }

        .metric-value {
            font-size: 1.5em;
            font-weight: 600;
            color: var(--secondary-color);
            margin: 10px 0;
        }

        .metric-label {
            font-size: 0.9em;
            color: #666;
        }

        .visualization-section {
            margin: 30px 0;
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
            box-shadow: var(--box-shadow);
            overflow: hidden;
        }

        .chart-header {
            padding: 15px 20px;
            background: var(--light-bg);
            border-bottom: 1px solid #eee;
        }

        .chart-container {
            padding: 20px;
            text-align: center;
        }

        .chart-container img {
            max-width: 100%;
            height: auto;
            border-radius: 8px;
        }

        .iteration-table {
            width: 100%;
            border-collapse: separate;
            border-spacing: 0;
            margin: 25px 0;
            border-radius: var(--border-radius);
            overflow: hidden;
            box-shadow: var(--box-shadow);
        }

        .iteration-table th,
        .iteration-table td {
            padding: 12px 15px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }

        .iteration-table th {
            background-color: var(--secondary-color);
            color: white;
            font-weight: 600;
        }

        .iteration-table tr:nth-child(even) {
            background-color: var(--light-bg);
        }

        .iteration-table tr:hover {
            background-color: #f5f5f5;
        }

        .best-iteration {
            background-color: #d4edda !important;
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
        }

        .timestamp {
            text-align: center;
            color: #666;
            font-style: italic;
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
        }

        @media (max-width: 768px) {
            .container {
                padding: 15px;
            }
            .optimization-grid,
            .parameter-grid,
            .metric-grid,
            .chart-grid {
                grid-template-columns: 1fr;
            }
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>🔧 Hiperparametre Optimizasyon Raporu</h1>
        <p class="timestamp">Rapor Tarihi: {tuning_results.get('timestamp', 'N/A')}</p>

        <h2>📊 Optimizasyon Özeti</h2>
        <div class="optimization-grid">
            <div class="optimization-card">
                <h3>Optimizasyon Detayları</h3>
                <div class="parameter-section">
                    <h4>Optimizasyon Stratejisi</h4>
                    <div class="parameter-grid">
                        <div class="parameter-card">
                            <div class="parameter-name">Metod</div>
                            <div class="parameter-value">{tuning_results.get('method', 'N/A')}</div>
                        </div>
                        <div class="parameter-card">
                            <div class="parameter-name">İterasyon Sayısı</div>
                            <div class="parameter-value">{tuning_results.get('n_iterations', 'N/A')}</div>
                        </div>
                        <div class="parameter-card">
                            <div class="parameter-name">Cross-Validation</div>
                            <div class="parameter-value">{tuning_results.get('cv_folds', 'N/A')} katlı</div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="optimization-card">
                <h3>En İyi Sonuçlar</h3>
                <div class="metric-section">
                    <div class="metric-grid">
                        <div class="metric-card">
                            <div class="metric-label">En İyi Skor</div>
                            <div class="metric-value">{tuning_results.get('best_score', 'N/A'):.4f}</div>
                        </div>
                        <div class="metric-card">
                            <div class="metric-label">İyileştirme</div>
                            <div class="metric-value">{tuning_results.get('improvement', 'N/A'):.2f}%</div>
                        </div>
                        <div class="metric-card">
                            <div class="metric-label">Optimizasyon Süresi</div>
                            <div class="metric-value">{tuning_results.get('duration', 'N/A')} saniye</div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <h2>🎯 En İyi Parametreler</h2>
        <div class="parameter-section">
            <div class="parameter-grid">
"""

        # En iyi parametreleri göster
        best_params = tuning_results.get('best_parameters', {})
        for param_name, param_value in best_params.items():
            html_content += f"""
                <div class="parameter-card">
                    <div class="parameter-name">{param_name}</div>
                    <div class="parameter-value">{param_value}</div>
                    <div class="parameter-range">
                        Aralık: {tuning_results.get('parameter_ranges', {}).get(param_name, 'N/A')}
                    </div>
                </div>
"""

        html_content += """
            </div>
        </div>

        <h2>📈 Optimizasyon Metrikleri</h2>
        <div class="metric-section">
            <div class="metric-grid">
"""

        # Optimizasyon metriklerini göster
        metrics = tuning_results.get('optimization_metrics', {})
        for metric_name, metric_value in metrics.items():
            html_content += f"""
                <div class="metric-card">
                    <div class="metric-label">{metric_name}</div>
                    <div class="metric-value">{metric_value:.4f}</div>
                </div>
"""

        html_content += """
            </div>
        </div>

        <h2>📊 İterasyon Geçmişi</h2>
        <div class="iteration-table-container">
            <table class="iteration-table">
                <thead>
                    <tr>
                        <th>İterasyon</th>
                        <th>Parametreler</th>
                        <th>Skor</th>
                        <th>Durum</th>
                    </tr>
                </thead>
                <tbody>
"""

        # İterasyon geçmişini göster
        iterations = tuning_results.get('iteration_history', [])
        best_iteration = tuning_results.get('best_iteration', -1)
        
        for i, iteration in enumerate(iterations):
            is_best = i == best_iteration
            row_class = 'best-iteration' if is_best else ''
            
            html_content += f"""
                    <tr class="{row_class}">
                        <td>{i + 1}</td>
                        <td>{json.dumps(iteration.get('parameters', {}), indent=2)}</td>
                        <td>{iteration.get('score', 'N/A'):.4f}</td>
                        <td>{'✅ En İyi' if is_best else '⏳'}</td>
                    </tr>
"""

        html_content += """
                </tbody>
            </table>
        </div>

        <h2>📊 Görselleştirmeler</h2>
        <div class="visualization-section">
            <div class="chart-grid">
"""

        # Görselleştirmeleri ekle
        charts = tuning_results.get('visualizations', {})
        for chart_name, chart_path in charts.items():
            html_content += f"""
                <div class="chart-card">
                    <div class="chart-header">
                        <h4>{chart_name}</h4>
                    </div>
                    <div class="chart-container">
                        <img src="{chart_path}" alt="{chart_name}">
                    </div>
                </div>
"""

        html_content += """
            </div>
        </div>

        <h2>💡 Öneriler</h2>
        <div class="recommendations">
            <ul>
"""

        # Önerileri ekle
        recommendations = tuning_results.get('recommendations', [])
        for rec in recommendations:
            html_content += f"                <li>{rec}</li>\n"

        html_content += """
            </ul>
        </div>

        <div class="timestamp">
            <p>Bu rapor otomatik olarak oluşturulmuştur.</p>
            <p>Rapor Tarihi: {tuning_results.get('timestamp', 'N/A')}</p>
        </div>
    </div>
</body>
</html>
"""

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        html_file = f"{self.output_dir}/hyperparameter_tuning_report_{timestamp}.html"

        with open(html_file, 'w', encoding='utf-8') as f:
            f.write(html_content)

        return html_file 