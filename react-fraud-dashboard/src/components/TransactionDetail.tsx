import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { FraudDetectionAPI, TransactionDetailResponse } from '../services/FraudDetectionAPI';

export const TransactionDetail: React.FC = () => {
  const { transactionId } = useParams<{ transactionId: string }>();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<TransactionDetailResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState('overview');

  useEffect(() => {
    if (transactionId) {
      loadTransactionDetail();
    }
  }, [transactionId]);

  const loadTransactionDetail = async () => {
    if (!transactionId) return;

    try {
      setLoading(true);
      setError(null);

      const response = await FraudDetectionAPI.getTransactionDetail(transactionId);
      setData(response);

    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const getRiskColor = (riskScore: string) => {
    switch (riskScore?.toLowerCase()) {
      case 'low': return 'text-green-600 bg-green-100';
      case 'medium': return 'text-yellow-600 bg-yellow-100';
      case 'high': return 'text-red-600 bg-red-100';
      case 'critical': return 'text-red-800 bg-red-200';
      default: return 'text-gray-600 bg-gray-100';
    }
  };

  const getDecisionColor = (decision: string) => {
    switch (decision?.toLowerCase()) {
      case 'approve': return 'text-green-600 bg-green-100';
      case 'deny': return 'text-red-600 bg-red-100';
      case 'reviewrequired': return 'text-yellow-600 bg-yellow-100';
      default: return 'text-gray-600 bg-gray-100';
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity?.toLowerCase()) {
      case 'low': return 'text-green-600 bg-green-100';
      case 'medium': return 'text-yellow-600 bg-yellow-100';
      case 'high': return 'text-red-600 bg-red-100';
      case 'critical': return 'text-red-800 bg-red-200';
      default: return 'text-gray-600 bg-gray-100';
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('tr-TR');
  };

  const formatAmount = (amount: number) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
    }).format(amount);
  };

  if (loading) {
    return (
      <div className="max-w-7xl mx-auto p-6">
        <div className="bg-white rounded-lg shadow-lg p-6">
          <div className="animate-pulse">
            <div className="h-8 bg-gray-200 rounded w-1/4 mb-4"></div>
            <div className="h-4 bg-gray-200 rounded w-1/2 mb-8"></div>
            <div className="space-y-4">
              <div className="h-4 bg-gray-200 rounded"></div>
              <div className="h-4 bg-gray-200 rounded w-3/4"></div>
              <div className="h-4 bg-gray-200 rounded w-1/2"></div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="max-w-7xl mx-auto p-6">
        <div className="p-4 border border-red-200 bg-red-50 rounded-md">
          <p className="text-red-800">{error}</p>
          <button
            onClick={() => navigate('/transactions')}
            className="mt-4 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700"
          >
            Geri Dön
          </button>
        </div>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="max-w-7xl mx-auto p-6">
        <div className="p-4 border border-gray-200 bg-gray-50 rounded-md">
          <p className="text-gray-800">Transaction bulunamadı</p>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="bg-white rounded-lg shadow-lg">
        <div className="p-6 border-b">
          <div className="flex justify-between items-start">
            <div>
              <h1 className="text-2xl font-bold">3. ADIM: Transaction Detayı</h1>
              <p className="text-gray-600 mt-2">
                Transaction ID: <span className="font-mono">{data.transaction.transactionId}</span>
              </p>
            </div>
            <button
              onClick={() => navigate('/transactions')}
              className="px-4 py-2 border border-gray-300 rounded-md hover:bg-gray-50"
            >
              ← Listeye Dön
            </button>
          </div>
        </div>

        {/* Tabs */}
        <div className="border-b">
          <nav className="flex space-x-8 px-6">
            {[
              { id: 'overview', name: 'Genel Bilgiler' },
              { id: 'analysis', name: 'Analiz Sonucu' },
              { id: 'riskfactors', name: `Risk Faktörleri (${data.riskFactors.length})` },
              { id: 'evaluations', name: `ML Değerlendirmeleri (${data.riskEvaluations.length})` },
              { id: 'contexts', name: 'Context Analizleri' },
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`py-4 px-1 border-b-2 font-medium text-sm ${activeTab === tab.id
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
              >
                {tab.name}
              </button>
            ))}
          </nav>
        </div>
      </div>

      {/* Tab Content */}
      {activeTab === 'overview' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Transaction Info */}
          <div className="bg-white rounded-lg shadow-lg">
            <div className="p-6 border-b">
              <h3 className="text-lg font-semibold">Transaction Bilgileri</h3>
            </div>
            <div className="p-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <div className="text-sm text-gray-600">Kullanıcı ID</div>
                  <div className="font-semibold">{data.transaction.userId}</div>
                </div>
                <div>
                  <div className="text-sm text-gray-600">Tutar</div>
                  <div className="font-semibold text-lg">{formatAmount(data.transaction.amount)}</div>
                </div>
                <div>
                  <div className="text-sm text-gray-600">Merchant ID</div>
                  <div className="font-semibold">{data.transaction.merchantId}</div>
                </div>
                <div>
                  <div className="text-sm text-gray-600">İşlem Tipi</div>
                  <span className="inline-block px-2 py-1 text-xs font-semibold bg-gray-100 text-gray-800 rounded-full">
                    {data.transaction.type}
                  </span>
                </div>
                <div>
                  <div className="text-sm text-gray-600">Tarih</div>
                  <div className="font-semibold">{formatDate(data.transaction.createdAt)}</div>
                </div>
              </div>
            </div>
          </div>

          {/* Location & Device Info */}
          <div className="bg-white rounded-lg shadow-lg">
            <div className="p-6 border-b">
              <h3 className="text-lg font-semibold">Lokasyon & Device Bilgileri</h3>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <div className="text-sm text-gray-600 mb-2">Lokasyon</div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <div className="text-xs text-gray-500">Ülke</div>
                    <div className="font-semibold">{data.transaction.location.country}</div>
                  </div>
                  <div>
                    <div className="text-xs text-gray-500">Şehir</div>
                    <div className="font-semibold">{data.transaction.location.city}</div>
                  </div>
                </div>
              </div>

              <div>
                <div className="text-sm text-gray-600 mb-2">Device Bilgileri</div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <div className="text-xs text-gray-500">Device ID</div>
                    <div className="font-mono text-sm">{data.transaction.deviceInfo.deviceId}</div>
                  </div>
                  <div>
                    <div className="text-xs text-gray-500">Device Tipi</div>
                    <div className="font-semibold">{data.transaction.deviceInfo.deviceType}</div>
                  </div>
                  <div>
                    <div className="text-xs text-gray-500">IP Adresi</div>
                    <div className="font-mono">{data.transaction.deviceInfo.ipAddress}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'analysis' && (
        <div className="bg-white rounded-lg shadow-lg">
          <div className="p-6 border-b">
            <h3 className="text-lg font-semibold">🔍 Fraud Analiz Sonucu</h3>
            <p className="text-gray-600 mt-2">İşlem için yapılan kapsamlı fraud analizi sonuçları</p>
          </div>
          <div className="p-6">
            {/* Ana Analiz Sonuçları */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
              <div className="p-6 bg-gradient-to-br from-blue-50 to-blue-100 rounded-lg border border-blue-200">
                <div className="text-sm text-blue-600 font-medium mb-2">Risk Score</div>
                <div className={`text-3xl font-bold ${getRiskColor(typeof data.analysisResult.riskScore === 'object' ? (data.analysisResult.riskScore as any)?.level || 'Unknown' : data.analysisResult.riskScore).replace('bg-', '').replace('100', '600')}`}>
                  {typeof data.analysisResult.riskScore === 'object' ? (data.analysisResult.riskScore as any)?.score || (data.analysisResult.riskScore as any)?.level || 'Unknown' : data.analysisResult.riskScore}
                </div>
                <div className="text-xs text-blue-500 mt-1">
                  {typeof data.analysisResult.riskScore === 'object' ? (data.analysisResult.riskScore as any)?.level || 'Unknown' : 'Risk Level'}
                </div>
              </div>

              <div className="p-6 bg-gradient-to-br from-green-50 to-green-100 rounded-lg border border-green-200">
                <div className="text-sm text-green-600 font-medium mb-2">Decision</div>
                <div className={`text-3xl font-bold ${getDecisionColor(data.analysisResult.decision)}`}>
                  {data.analysisResult.decision}
                </div>
                <div className="text-xs text-green-500 mt-1">
                  Final Karar
                </div>
              </div>

              <div className="p-6 bg-gradient-to-br from-purple-50 to-purple-100 rounded-lg border border-purple-200">
                <div className="text-sm text-purple-600 font-medium mb-2">Fraud Probability</div>
                <div className="text-3xl font-bold text-purple-700">
                  {(data.analysisResult.fraudProbability * 100).toFixed(1)}%
                </div>
                <div className="text-xs text-purple-500 mt-1">
                  Dolandırıcılık Olasılığı
                </div>
              </div>

              <div className="p-6 bg-gradient-to-br from-orange-50 to-orange-100 rounded-lg border border-orange-200">
                <div className="text-sm text-orange-600 font-medium mb-2">Anomaly Score</div>
                <div className="text-3xl font-bold text-orange-700">
                  {data.analysisResult.anomalyScore.toFixed(2)}
                </div>
                <div className="text-xs text-orange-500 mt-1">
                  Anomali Skoru
                </div>
              </div>
            </div>

            {/* Detaylı Metrikler */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
              <div className="p-4 bg-gray-50 rounded-lg">
                <div className="text-sm text-gray-600 font-medium">Toplam Kural</div>
                <div className="text-2xl font-semibold text-gray-800">{data.analysisResult.totalRuleCount}</div>
                <div className="text-xs text-gray-500 mt-1">Değerlendirilen Kural</div>
              </div>

              <div className="p-4 bg-red-50 rounded-lg">
                <div className="text-sm text-red-600 font-medium">Tetiklenen Kural</div>
                <div className="text-2xl font-semibold text-red-700">{data.analysisResult.triggeredRuleCount}</div>
                <div className="text-xs text-red-500 mt-1">Risk Tespit Edilen</div>
              </div>

              <div className="p-4 bg-blue-50 rounded-lg">
                <div className="text-sm text-blue-600 font-medium">Analiz Zamanı</div>
                <div className="text-lg font-semibold text-blue-700">{formatDate(data.analysisResult.analyzedAt)}</div>
                <div className="text-xs text-blue-500 mt-1">İşlem Süresi</div>
              </div>
            </div>

            {/* Risk Faktörleri Özeti */}
            {data.riskFactors.length > 0 && (
              <div className="mb-8">
                <h4 className="text-lg font-semibold mb-4">⚠️ Tespit Edilen Risk Faktörleri</h4>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {data.riskFactors.slice(0, 6).map((factor) => (
                    <div key={factor.riskFactorId} className="p-4 border border-gray-200 rounded-lg hover:shadow-md transition-shadow">
                      <div className="flex justify-between items-start mb-2">
                        <span className="text-sm font-medium text-gray-800">{factor.type}</span>
                        <span className={`px-2 py-1 text-xs font-semibold rounded-full ${getSeverityColor(factor.severity)}`}>
                          {factor.severity}
                        </span>
                      </div>
                      <div className="text-sm text-gray-600 mb-2 line-clamp-2">{factor.description}</div>
                      <div className="flex justify-between items-center text-xs text-gray-500">
                        <span>Güven: {(factor.confidence * 100).toFixed(0)}%</span>
                        <span>{factor.source || 'N/A'}</span>
                      </div>
                    </div>
                  ))}
                  {data.riskFactors.length > 6 && (
                    <div className="p-4 border border-gray-200 rounded-lg bg-gray-50 flex items-center justify-center">
                      <span className="text-sm text-gray-600">
                        +{data.riskFactors.length - 6} daha fazla risk faktörü...
                      </span>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Uygulanan Aksiyonlar */}
            {data.analysisResult.appliedActions.length > 0 && (
              <div className="mb-8">
                <h4 className="text-lg font-semibold mb-4">🎯 Uygulanan Aksiyonlar</h4>
                <div className="flex flex-wrap gap-3">
                  {data.analysisResult.appliedActions.map((action, index) => (
                    <span key={index} className="px-4 py-2 bg-blue-100 text-blue-800 text-sm font-medium rounded-full border border-blue-200">
                      {action}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Analiz Detayları */}
            <div className="bg-gray-50 p-6 rounded-lg">
              <h4 className="text-lg font-semibold mb-4">📊 Analiz Detayları</h4>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <h5 className="font-medium text-gray-700 mb-2">İşlem Bilgileri</h5>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-600">Transaction ID:</span>
                      <span className="font-mono">{data.transaction.transactionId}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600">Kullanıcı ID:</span>
                      <span>{data.transaction.userId}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600">Tutar:</span>
                      <span className="font-semibold">{formatAmount(data.transaction.amount)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600">İşlem Tipi:</span>
                      <span>{data.transaction.type}</span>
                    </div>
                  </div>
                </div>
                <div>
                  <h5 className="font-medium text-gray-700 mb-2">Lokasyon & Cihaz</h5>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-600">Lokasyon:</span>
                      <span>{data.transaction.location.country}, {data.transaction.location.city}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600">IP Adresi:</span>
                      <span className="font-mono">{data.transaction.deviceInfo.ipAddress}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600">Cihaz Tipi:</span>
                      <span>{data.transaction.deviceInfo.deviceType}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600">Analiz Tarihi:</span>
                      <span>{formatDate(data.analysisResult.analyzedAt)}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'riskfactors' && (
        <div className="bg-white rounded-lg shadow-lg">
          <div className="p-6 border-b">
            <h3 className="text-lg font-semibold">Risk Faktörleri ({data.riskFactors.length})</h3>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Tip</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Açıklama</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Şiddet</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Güven</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Kaynak</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Tarih</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {data.riskFactors.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-6 py-4 text-center text-gray-500">
                      Risk faktörü bulunamadı
                    </td>
                  </tr>
                ) : (
                  data.riskFactors.map((factor) => (
                    <tr key={factor.riskFactorId} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className="text-sm font-medium">{factor.type}</span>
                      </td>
                      <td className="px-6 py-4">
                        <div className="text-sm text-gray-900 max-w-md">{factor.description}</div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className={`inline-block px-2 py-1 text-xs font-semibold rounded-full ${getSeverityColor(factor.severity)}`}>
                          {factor.severity}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm font-semibold">{(factor.confidence * 100).toFixed(1)}%</div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className="text-sm text-gray-600">{factor.source || 'N/A'}</span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm text-gray-600">{formatDate(factor.createdAt)}</div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab === 'evaluations' && (
        <div className="bg-white rounded-lg shadow-lg">
          <div className="p-6 border-b">
            <h3 className="text-lg font-semibold">ML Değerlendirmeleri ({data.riskEvaluations.length})</h3>
          </div>
          <div className="p-6 space-y-6">
            {data.riskEvaluations.length === 0 ? (
              <div className="text-center text-gray-500 py-8">
                ML değerlendirmesi bulunamadı
              </div>
            ) : (
              data.riskEvaluations.map((evaluation, index) => (
                <div key={evaluation.riskEvaluationId} className="border rounded-lg p-4">
                  <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
                    <div>
                      <div className="text-sm text-gray-600">Fraud Probability</div>
                      <div className="text-lg font-semibold">{(evaluation.fraudProbability * 100).toFixed(2)}%</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-600">Anomaly Score</div>
                      <div className="text-lg font-semibold">{evaluation.anomalyScore.toFixed(2)}</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-600">Risk Level</div>
                      <span className={`inline-block px-2 py-1 text-xs font-semibold rounded-full ${getRiskColor(evaluation.riskLevel)}`}>
                        {evaluation.riskLevel}
                      </span>
                    </div>
                    <div>
                      <div className="text-sm text-gray-600">Güven</div>
                      <div className="text-lg font-semibold">{(evaluation.confidence * 100).toFixed(1)}%</div>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
                    <div>
                      <div className="text-gray-600">Model Version</div>
                      <div>{evaluation.modelVersion || 'N/A'}</div>
                    </div>
                    <div>
                      <div className="text-gray-600">İşlem Süresi</div>
                      <div>{evaluation.processingTimeMs ? `${evaluation.processingTimeMs}ms` : 'N/A'}</div>
                    </div>
                    <div>
                      <div className="text-gray-600">Değerlendirme Zamanı</div>
                      <div>{formatDate(evaluation.evaluatedAt)}</div>
                    </div>
                  </div>

                  {evaluation.modelInfo && Object.keys(evaluation.modelInfo).length > 0 && (
                    <div className="mt-4">
                      <div className="text-sm text-gray-600 mb-2">Model Bilgileri</div>
                      <div className="bg-gray-50 p-3 rounded text-xs font-mono max-h-32 overflow-y-auto">
                        <pre>{JSON.stringify(evaluation.modelInfo, null, 2)}</pre>
                      </div>
                    </div>
                  )}
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {activeTab === 'contexts' && (
        <div className="bg-white rounded-lg shadow-lg">
          <div className="p-6 border-b">
            <h3 className="text-lg font-semibold">4. ADIM: Context Analizleri</h3>
            <p className="text-gray-600 mt-2">Bu transaction için farklı context'lerde fraud analizi yap</p>
          </div>
          <div className="p-6">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              <button className="p-4 border-2 border-blue-200 hover:border-blue-500 rounded-lg transition-colors">
                <div className="text-lg font-semibold text-blue-600">Transaction Context</div>
                <div className="text-sm text-gray-600 mt-1">İşlem bazlı analiz</div>
              </button>

              <button className="p-4 border-2 border-green-200 hover:border-green-500 rounded-lg transition-colors">
                <div className="text-lg font-semibold text-green-600">Account Context</div>
                <div className="text-sm text-gray-600 mt-1">Hesap bazlı analiz</div>
              </button>

              <button className="p-4 border-2 border-yellow-200 hover:border-yellow-500 rounded-lg transition-colors">
                <div className="text-lg font-semibold text-yellow-600">IP Context</div>
                <div className="text-sm text-gray-600 mt-1">IP bazlı analiz</div>
              </button>

              <button className="p-4 border-2 border-purple-200 hover:border-purple-500 rounded-lg transition-colors">
                <div className="text-lg font-semibold text-purple-600">Device Context</div>
                <div className="text-sm text-gray-600 mt-1">Cihaz bazlı analiz</div>
              </button>

              <button className="p-4 border-2 border-red-200 hover:border-red-500 rounded-lg transition-colors">
                <div className="text-lg font-semibold text-red-600">Session Context</div>
                <div className="text-sm text-gray-600 mt-1">Oturum bazlı analiz</div>
              </button>
            </div>

            <div className="mt-6 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
              <div className="text-sm text-yellow-800">
                <strong>Not:</strong> Context analizleri henüz implement edilmemiştir. Bu butonlar gelecekteki geliştirmeler için tasarlanmıştır.
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}; 