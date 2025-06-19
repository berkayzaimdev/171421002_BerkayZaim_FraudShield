import React, { useState, useEffect } from 'react';
import { FraudDetectionAPI, TransactionListRequest, TransactionListResponse, TransactionListItem } from '../services/FraudDetectionAPI';
import { useNavigate } from 'react-router-dom';

export const TransactionList: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<TransactionListResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [filters, setFilters] = useState<TransactionListRequest>({
    page: 1,
    pageSize: 20,
    userId: '',
    startDate: '',
    endDate: '',
  });

  const loadTransactions = async () => {
    try {
      setLoading(true);
      setError(null);

      const response = await FraudDetectionAPI.getTransactionList(filters);
      setData(response);

    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTransactions();
  }, [filters.page]);

  const handleSearch = () => {
    setFilters(prev => ({ ...prev, page: 1 }));
    loadTransactions();
  };

  const handlePageChange = (newPage: number) => {
    setFilters(prev => ({ ...prev, page: newPage }));
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

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('tr-TR');
  };

  const formatAmount = (amount: number) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
    }).format(amount);
  };

  return (
    <div className="max-w-7xl mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="bg-white rounded-lg shadow-lg">
        <div className="p-6 border-b">
          <h1 className="text-2xl font-bold">2. ADIM: Transaction Listeleme</h1>
          <p className="text-gray-600 mt-2">Tüm işlemleri ve analiz sonuçlarını görüntüle</p>
        </div>

        {/* Filters */}
        <div className="p-6 border-b bg-gray-50">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
              <label htmlFor="userId" className="block text-sm font-medium text-gray-700 mb-2">
                Kullanıcı ID
              </label>
              <input
                id="userId"
                type="text"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                value={filters.userId}
                onChange={(e) => setFilters(prev => ({ ...prev, userId: e.target.value }))}
                placeholder="user-12345"
              />
            </div>

            <div>
              <label htmlFor="startDate" className="block text-sm font-medium text-gray-700 mb-2">
                Başlangıç Tarihi
              </label>
              <input
                id="startDate"
                type="date"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                value={filters.startDate}
                onChange={(e) => setFilters(prev => ({ ...prev, startDate: e.target.value }))}
              />
            </div>

            <div>
              <label htmlFor="endDate" className="block text-sm font-medium text-gray-700 mb-2">
                Bitiş Tarihi
              </label>
              <input
                id="endDate"
                type="date"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                value={filters.endDate}
                onChange={(e) => setFilters(prev => ({ ...prev, endDate: e.target.value }))}
              />
            </div>

            <div className="flex items-end">
              <button
                onClick={handleSearch}
                disabled={loading}
                className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {loading ? 'Aranıyor...' : 'Ara'}
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Error Display */}
      {error && (
        <div className="p-4 border border-red-200 bg-red-50 rounded-md">
          <p className="text-red-800">{error}</p>
        </div>
      )}

      {/* Results */}
      {data && (
        <div className="bg-white rounded-lg shadow-lg">
          <div className="p-6 border-b">
            <div className="flex justify-between items-center">
              <h2 className="text-lg font-semibold">
                İşlemler ({data.totalCount} toplam)
              </h2>
              <button
                onClick={() => navigate('/transaction/create')}
                className="bg-green-600 text-white py-2 px-4 rounded-md hover:bg-green-700 focus:ring-2 focus:ring-green-500 focus:ring-offset-2 transition-colors"
              >
                Yeni İşlem Oluştur
              </button>
            </div>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Transaction ID
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Kullanıcı ID
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Tutar
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Tip
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Risk Score
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Decision
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Tarih
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    İşlemler
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {data.transactions.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="px-6 py-4 text-center text-gray-500">
                      Hiç işlem bulunamadı
                    </td>
                  </tr>
                ) : (
                  data.transactions.map((transaction: TransactionListItem) => (
                    <tr key={transaction.transactionId} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm font-mono text-gray-900">
                          {transaction.transactionId.substring(0, 8)}...
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm text-gray-900">{transaction.userId}</div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm font-semibold text-gray-900">
                          {formatAmount(transaction.amount)}
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className="inline-block px-2 py-1 text-xs font-semibold bg-gray-100 text-gray-800 rounded-full">
                          {transaction.type}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        {transaction.analysisResult ? (
                          <span className={`inline-block px-2 py-1 text-xs font-semibold rounded-full ${getRiskColor(typeof transaction.analysisResult.riskScore === 'object' ? (transaction.analysisResult.riskScore as any)?.level || 'Unknown' : transaction.analysisResult.riskScore)}`}>
                            {typeof transaction.analysisResult.riskScore === 'object' ? (transaction.analysisResult.riskScore as any)?.score || (transaction.analysisResult.riskScore as any)?.level || 'Unknown' : transaction.analysisResult.riskScore}
                          </span>
                        ) : (
                          <span className="text-gray-400 text-sm">Analiz yok</span>
                        )}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        {transaction.analysisResult ? (
                          <span className={`inline-block px-2 py-1 text-xs font-semibold rounded-full ${getDecisionColor(transaction.analysisResult.decision)}`}>
                            {transaction.analysisResult.decision}
                          </span>
                        ) : (
                          <span className="text-gray-400 text-sm">-</span>
                        )}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm text-gray-900">
                          {formatDate(transaction.createdAt)}
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                        <button
                          onClick={() => navigate(`/transaction/${transaction.transactionId}/detail`)}
                          className="text-blue-600 hover:text-blue-900 mr-3"
                        >
                          Detay
                        </button>
                        {transaction.analysisResult && (
                          <span className="text-gray-400">
                            {transaction.analysisResult.riskFactorCount} risk faktör
                          </span>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {data.totalCount > data.pageSize && (
            <div className="px-6 py-4 border-t bg-gray-50">
              <div className="flex items-center justify-between">
                <div className="text-sm text-gray-700">
                  Sayfa {data.page} / {Math.ceil(data.totalCount / data.pageSize)}
                  ({data.transactions.length} / {data.totalCount} gösteriliyor)
                </div>
                <div className="flex space-x-2">
                  <button
                    onClick={() => handlePageChange(data.page - 1)}
                    disabled={data.page === 1}
                    className="px-3 py-2 border border-gray-300 rounded-md text-sm hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Önceki
                  </button>
                  <button
                    onClick={() => handlePageChange(data.page + 1)}
                    disabled={data.page >= Math.ceil(data.totalCount / data.pageSize)}
                    className="px-3 py-2 border border-gray-300 rounded-md text-sm hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Sonraki
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}; 