import React, { useState } from 'react';
import { FraudDetectionAPI, TransactionRequest, TransactionCreateResponse } from '../services/FraudDetectionAPI';
import { useNavigate } from 'react-router-dom';

export const TransactionCreate: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<TransactionCreateResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [formData, setFormData] = useState<TransactionRequest>({
    userId: '',
    amount: 0,
    merchantId: '',
    type: 'Transfer',
    location: {
      country: 'TR',
      city: 'Istanbul',
      latitude: 41.0082,
      longitude: 28.9784,
    },
    deviceInfo: {
      deviceId: `device-${Date.now()}`,
      deviceType: 'Desktop',
      ipAddress: '192.168.1.100',
      userAgent: navigator.userAgent,
    },
    additionalDataRequest: {
      cardType: 'Visa',
      cardBin: '424242',
      cardLast4: '4242',
      bankName: 'Test Bank',
      bankCountry: 'TR',
      vFactors: {},
    },
  });

  const handleInputChange = (field: string, value: any) => {
    if (field.includes('.')) {
      const [parent, child] = field.split('.');
      setFormData(prev => ({
        ...prev,
        [parent]: {
          ...(prev[parent as keyof TransactionRequest] as any),
          [child]: value,
        },
      }));
    } else {
      setFormData(prev => ({
        ...prev,
        [field]: value,
      }));
    }
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    try {
      setLoading(true);
      setError(null);

      const response = await FraudDetectionAPI.createTransaction(formData);
      setResult(response);

      // Başarılı ise transaction detay sayfasına yönlendir
      if (response.isSuccessful) {
        setTimeout(() => {
          navigate(`/transaction/${response.transactionId}/detail`);
        }, 3000);
      }

    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const getRiskColor = (riskScore: string) => {
    switch (riskScore?.toLowerCase()) {
      case 'low': return 'text-green-600';
      case 'medium': return 'text-yellow-600';
      case 'high': return 'text-red-600';
      case 'critical': return 'text-red-800';
      default: return 'text-gray-600';
    }
  };

  const getDecisionColor = (decision: string) => {
    switch (decision?.toLowerCase()) {
      case 'approve': return 'text-green-600 bg-green-50';
      case 'deny': return 'text-red-600 bg-red-50';
      case 'reviewrequired': return 'text-yellow-600 bg-yellow-50';
      default: return 'text-gray-600 bg-gray-50';
    }
  };

  return (
    <div className="max-w-4xl mx-auto p-6 space-y-6">
      <div className="bg-white rounded-lg shadow-lg">
        <div className="p-6 border-b">
          <h1 className="text-2xl font-bold">1. ADIM: İşlem Oluştur + Fraud Analizi</h1>
          <p className="text-gray-600 mt-2">Yeni işlem oluştur ve otomatik fraud analizi yap</p>
        </div>

        <div className="p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Temel İşlem Bilgileri */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label htmlFor="userId" className="block text-sm font-medium text-gray-700 mb-2">
                  Kullanıcı ID
                </label>
                <input
                  id="userId"
                  type="text"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  value={formData.userId}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('userId', e.target.value)}
                  placeholder="user-12345"
                  required
                />
              </div>

              <div>
                <label htmlFor="amount" className="block text-sm font-medium text-gray-700 mb-2">
                  Tutar
                </label>
                <input
                  id="amount"
                  type="number"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  value={formData.amount}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('amount', parseFloat(e.target.value))}
                  placeholder="100.00"
                  step="0.01"
                  required
                />
              </div>

              <div>
                <label htmlFor="merchantId" className="block text-sm font-medium text-gray-700 mb-2">
                  Merchant ID
                </label>
                <input
                  id="merchantId"
                  type="text"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  value={formData.merchantId}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('merchantId', e.target.value)}
                  placeholder="merchant-12345"
                  required
                />
              </div>

              <div>
                <label htmlFor="type" className="block text-sm font-medium text-gray-700 mb-2">
                  İşlem Tipi
                </label>
                <select
                  id="type"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  value={formData.type}
                  onChange={(e: React.ChangeEvent<HTMLSelectElement>) => handleInputChange('type', e.target.value)}
                >
                  <option value="Transfer">Transfer</option>
                  <option value="Payment">Payment</option>
                  <option value="Withdrawal">Withdrawal</option>
                  <option value="Deposit">Deposit</option>
                </select>
              </div>
            </div>

            {/* Lokasyon Bilgileri */}
            <div className="space-y-4">
              <h3 className="text-lg font-semibold">Lokasyon Bilgileri</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="country" className="block text-sm font-medium text-gray-700 mb-2">
                    Ülke
                  </label>
                  <input
                    id="country"
                    type="text"
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    value={formData.location.country}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('location.country', e.target.value)}
                    placeholder="TR"
                  />
                </div>

                <div>
                  <label htmlFor="city" className="block text-sm font-medium text-gray-700 mb-2">
                    Şehir
                  </label>
                  <input
                    id="city"
                    type="text"
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    value={formData.location.city}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('location.city', e.target.value)}
                    placeholder="Istanbul"
                  />
                </div>
              </div>
            </div>

            {/* Device Bilgileri */}
            <div className="space-y-4">
              <h3 className="text-lg font-semibold">Device Bilgileri</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="deviceType" className="block text-sm font-medium text-gray-700 mb-2">
                    Device Tipi
                  </label>
                  <select
                    id="deviceType"
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    value={formData.deviceInfo.deviceType}
                    onChange={(e: React.ChangeEvent<HTMLSelectElement>) => handleInputChange('deviceInfo.deviceType', e.target.value)}
                  >
                    <option value="Desktop">Desktop</option>
                    <option value="Mobile">Mobile</option>
                    <option value="Tablet">Tablet</option>
                  </select>
                </div>

                <div>
                  <label htmlFor="ipAddress" className="block text-sm font-medium text-gray-700 mb-2">
                    IP Adresi
                  </label>
                  <input
                    id="ipAddress"
                    type="text"
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    value={formData.deviceInfo.ipAddress}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleInputChange('deviceInfo.ipAddress', e.target.value)}
                    placeholder="192.168.1.100"
                  />
                </div>
              </div>
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              className="w-full bg-blue-600 text-white py-3 px-4 rounded-md hover:bg-blue-700 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              disabled={loading}
            >
              {loading ? 'İşlem Oluşturuluyor...' : 'İşlem Oluştur + Analiz Et'}
            </button>
          </form>

          {/* Error Display */}
          {error && (
            <div className="mt-4 p-4 border border-red-200 bg-red-50 rounded-md">
              <p className="text-red-800">{error}</p>
            </div>
          )}

          {/* Result Display */}
          {result && (
            <div className="mt-6 space-y-4">
              <div className={`p-4 border-2 rounded-md ${result.isSuccessful ? 'border-green-200 bg-green-50' : 'border-red-200 bg-red-50'}`}>
                <p className={result.isSuccessful ? 'text-green-800' : 'text-red-800'}>
                  {result.message}
                </p>
              </div>

              <div className="bg-white rounded-lg shadow-lg">
                <div className="p-6 border-b">
                  <h3 className="text-lg font-semibold">Fraud Analiz Sonuçları</h3>
                </div>
                <div className="p-6 space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    <div className="p-4 bg-gray-50 rounded-lg">
                      <div className="text-sm text-gray-600">Transaction ID</div>
                      <div className="font-mono text-sm">{result.transactionId}</div>
                    </div>

                    <div className="p-4 bg-gray-50 rounded-lg">
                      <div className="text-sm text-gray-600">Fraud Probability</div>
                      <div className="text-lg font-semibold">
                        {(result.fraudProbability * 100).toFixed(2)}%
                      </div>
                    </div>

                    <div className="p-4 bg-gray-50 rounded-lg">
                      <div className="text-sm text-gray-600">Risk Score</div>
                      <div className={`text-lg font-semibold ${getRiskColor(typeof result.riskScore === 'object' ? (result.riskScore as any)?.level || 'Unknown' : result.riskScore)}`}>
                        {typeof result.riskScore === 'object' ? (result.riskScore as any)?.score || (result.riskScore as any)?.level || 'Unknown' : result.riskScore}
                      </div>
                    </div>

                    <div className="p-4 bg-gray-50 rounded-lg">
                      <div className="text-sm text-gray-600">Decision</div>
                      <div className={`inline-block px-2 py-1 rounded-full text-sm font-semibold ${getDecisionColor(result.decision)}`}>
                        {result.decision}
                      </div>
                    </div>

                    <div className="p-4 bg-gray-50 rounded-lg">
                      <div className="text-sm text-gray-600">Anomaly Score</div>
                      <div className="text-lg font-semibold">
                        {result.anomalyScore.toFixed(2)}
                      </div>
                    </div>

                    <div className="p-4 bg-gray-50 rounded-lg">
                      <div className="text-sm text-gray-600">Risk Faktör Sayısı</div>
                      <div className="text-lg font-semibold">
                        {result.riskFactorCount}
                      </div>
                    </div>
                  </div>

                  <div className="mt-4 flex gap-2">
                    <button
                      onClick={() => navigate(`/transaction/${result.transactionId}/detail`)}
                      className="px-4 py-2 border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                    >
                      Detay Sayfasına Git
                    </button>
                    <button
                      onClick={() => navigate('/transactions')}
                      className="px-4 py-2 border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                    >
                      Tüm İşlemleri Listele
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}; 