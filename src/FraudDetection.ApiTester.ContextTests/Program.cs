using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FraudDetection.ApiTester.ContextTests
{
    /// <summary>
    /// Bağlam (Context) analizi için özel test sınıfı
    /// Her bir bağlam türünü sırasıyla test eder (Transaction, Account, IP, Device, Session)
    /// </summary>
    public class ContextualAnalysisTester
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private static string _baseUrl = "http://localhost:5112"; // Default base URL

        // İşlem analizi sonuçlarını saklama (aynı işlem ID ile farklı bağlamları test etmek için)
        private static Guid _transactionId = Guid.Empty;
        private static Guid _accountId = Guid.Empty;
        private static string _ipAddress = string.Empty;
        private static string _deviceId = string.Empty;
        private static Guid _sessionId = Guid.Empty;

        // İlerlemeyi takip etme
        private static List<ContextTest> _completedTests = new List<ContextTest>();

        public class ContextTest
        {
            public string ContextName { get; set; }
            public string Endpoint { get; set; }
            public bool IsSuccess { get; set; }
            public int RiskScore { get; set; }
            public int TriggeredRuleCount { get; set; }
            public string ResultType { get; set; }
        }

        /// <summary>
        /// Bağlam analizini adım adım gerçekleştirir
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Fraud Detection API Tester: Contextual Analysis Flow ===");

            // API URL yapılandırması
            ConfigureApiUrl();

            try
            {
                Console.WriteLine("\n=== Contextual Analysis Flow ===\n");

                // Adım 1: İşlemi başlat ve analiz et (ana adım)
                Console.WriteLine("Step 1: Initial Transaction Analysis");
                await AnalyzeTransaction();
                PauseForReview("işlem analizi");

                // İşlem bilgileri var mı kontrol et
                if (_transactionId != Guid.Empty && _accountId != Guid.Empty)
                {
                    // Adım 2: İşlem bağlam kontrolü
                    Console.WriteLine("\nStep 2: Transaction Context Check");
                    await CheckTransactionContext();
                    PauseForReview("işlem bağlam kontrolü");

                    // Adım 3: Hesap bağlam kontrolü
                    Console.WriteLine("\nStep 3: Account Context Check");
                    await CheckAccountContext();
                    PauseForReview("hesap bağlam kontrolü");

                    // Adım 4: IP bağlam kontrolü
                    Console.WriteLine("\nStep 4: IP Address Context Check");
                    await CheckIpContext();
                    PauseForReview("IP bağlam kontrolü");

                    // Adım 5: Cihaz bağlam kontrolü
                    Console.WriteLine("\nStep 5: Device Context Check");
                    await CheckDeviceContext();
                    PauseForReview("cihaz bağlam kontrolü");

                    // Adım 6: Oturum bağlam kontrolü
                    Console.WriteLine("\nStep 6: Session Context Check");
                    await CheckSessionContext();
                    PauseForReview("oturum bağlam kontrolü");

                    // Adım 7: ML Modeli değerlendirmesi
                    Console.WriteLine("\nStep 7: ML Model Evaluation");
                    await EvaluateWithModel();
                    PauseForReview("ML değerlendirmesi");

                    // Adım 8: Kapsamlı Kontrol (tüm bağlamları birleştiren)
                    Console.WriteLine("\nStep 8: Comprehensive Fraud Check");
                    await PerformComprehensiveCheck();
                }
                else
                {
                    Console.WriteLine("İşlem analizi başarısız oldu veya gerekli bilgiler alınamadı.");
                }

                // Özet göster
                DisplayTestSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test sırasında bir hata oluştu: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"İç hata: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nTesti sonlandırmak için bir tuşa basın...");
            Console.ReadKey();
        }

        #region Test Methods

        /// <summary>
        /// 1. İlk işlem analizi - İşlem ID ve hesap ID elde etmek için
        /// </summary>
        private static async Task AnalyzeTransaction()
        {
            try
            {
                var request = CreateSampleTransactionRequest();

                Console.WriteLine("İşlem analizi isteği gönderiliyor...");
                DisplayRequestData("İşlem Analizi İsteği", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/analyze", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonDocumentOptions { AllowTrailingCommas = true };

                    using var document = JsonDocument.Parse(content, options);
                    var root = document.RootElement;

                    if (root.TryGetProperty("transactionId", out var idElement))
                    {
                        string transactionIdStr = idElement.GetString();
                        if (Guid.TryParse(transactionIdStr, out Guid transactionId))
                        {
                            _transactionId = transactionId;
                            Console.WriteLine($"İşlem analizi başarılı. ID: {_transactionId}");
                        }
                    }

                    // Kullanıcı ID'si oluştur (gerçek sistemde işlem analizi sonucundan alınabilir)
                    _accountId = Guid.NewGuid();
                    
                    // IP ve Cihaz bilgilerini kaydet (gerçek sistemde işlem analizi sonucundan alınabilir)
                    _ipAddress = "192.168.1.1";
                    _deviceId = "device_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    
                    // Oturum ID'si oluştur
                    _sessionId = Guid.NewGuid();

                    // Test sonucunu ekle
                    _completedTests.Add(new ContextTest
                    {
                        ContextName = "Initial Transaction Analysis",
                        Endpoint = "/api/FraudDetection/analyze",
                        IsSuccess = response.IsSuccessStatusCode,
                        RiskScore = ExtractRiskScore(content),
                        ResultType = ExtractResultType(content),
                        TriggeredRuleCount = ExtractTriggeredRuleCount(content)
                    });
                }

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İşlem analizi sırasında hata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 2. İşlem bağlam kontrolü (Transaction Context)
        /// </summary>
        private static async Task CheckTransactionContext()
        {
            try
            {
                var request = CreateSampleTransactionCheckRequest();

                Console.WriteLine($"İşlem bağlam kontrolü yapılıyor (İşlem ID: {_transactionId})...");
                DisplayRequestData("İşlem Bağlam Kontrolü İsteği", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-transaction", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Test sonucunu ekle
                    _completedTests.Add(new ContextTest
                    {
                        ContextName = "Transaction Context",
                        Endpoint = "/api/FraudDetection/check-transaction",
                        IsSuccess = response.IsSuccessStatusCode,
                        RiskScore = ExtractRiskScore(content),
                        ResultType = ExtractResultType(content),
                        TriggeredRuleCount = ExtractTriggeredRuleCount(content)
                    });
                }

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İşlem bağlam kontrolü sırasında hata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 3. Hesap bağlam kontrolü (Account Context)
        /// </summary>
        private static async Task CheckAccountContext()
        {
            try
            {
                var request = CreateSampleAccountAccessCheckRequest();

                Console.WriteLine($"Hesap bağlam kontrolü yapılıyor (Hesap ID: {_accountId})...");
                DisplayRequestData("Hesap Bağlam Kontrolü İsteği", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-account-access", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Test sonucunu ekle
                    _completedTests.Add(new ContextTest
                    {
                        ContextName = "Account Context",
                        Endpoint = "/api/FraudDetection/check-account-access",
                        IsSuccess = response.IsSuccessStatusCode,
                        RiskScore = ExtractRiskScore(content),
                        ResultType = ExtractResultType(content),
                        TriggeredRuleCount = ExtractTriggeredRuleCount(content)
                    });
                }

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hesap bağlam kontrolü sırasında hata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 4. IP bağlam kontrolü (IP Context)
        /// </summary>
        private static async Task CheckIpContext()
        {
            try
            {
                var request = CreateSampleIpCheckRequest();

                Console.WriteLine($"IP bağlam kontrolü yapılıyor (IP: {_ipAddress})...");
                DisplayRequestData("IP Bağlam Kontrolü İsteği", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-ip", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Test sonucunu ekle
                    _completedTests.Add(new ContextTest
                    {
                        ContextName = "IP Address Context",
                        Endpoint = "/api/FraudDetection/check-ip",
                        IsSuccess = response.IsSuccessStatusCode,
                        RiskScore = ExtractRiskScore(content),
                        ResultType = ExtractResultType(content),
                        TriggeredRuleCount = ExtractTriggeredRuleCount(content)
                    });
                }

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IP bağlam kontrolü sırasında hata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 5. Cihaz bağlam kontrolü (Device Context)
        /// </summary>
        private static async Task CheckDeviceContext()
        {
            try
            {
                var request = CreateSampleDeviceCheckRequest();

                Console.WriteLine($"Cihaz bağlam kontrolü yapılıyor (Cihaz ID: {_deviceId})...");
                DisplayRequestData("Cihaz Bağlam Kontrolü İsteği", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-device", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Test sonucunu ekle
                    _completedTests.Add(new ContextTest
                    {
                        ContextName = "Device Context",
                        Endpoint = "/api/FraudDetection/check-device",
                        IsSuccess = response.IsSuccessStatusCode,
                        RiskScore = ExtractRiskScore(content),
                        ResultType = ExtractResultType(content),
                        TriggeredRuleCount = ExtractTriggeredRuleCount(content)
                    });
                }

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cihaz bağlam kontrolü sırasında hata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 6. Oturum bağlam kontrolü (Session Context)
        /// </summary>
        private static async Task CheckSessionContext()
        {
            try
            {
                var request = CreateSampleSessionCheckRequest();

                Console.WriteLine($"Oturum bağlam kontrolü yapılıyor (Oturum ID: {_sessionId})...");
                DisplayRequestData("Oturum Bağlam Kontrolü İsteği", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-session", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Test sonucunu ekle
                    _completedTests.Add(new ContextTest
                    {
                        ContextName = "Session Context",
                        Endpoint = "/api/FraudDetection/check-session",
                        IsSuccess = response.IsSuccessStatusCode,
                        RiskScore = ExtractRiskScore(content),
                        ResultType = ExtractResultType(content),
                        TriggeredRuleCount = ExtractTriggeredRuleCount(content)
                    });
                }

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oturum bağlam kontrolü sırasında hata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 7. ML Modeli değerlendirmesi
        /// </summary>
        private static async Task EvaluateWithModel()
        {
            try
            {
                var request = CreateSampleModelEvaluationRequest();

                Console.WriteLine($"ML model değerlendirmesi yapılıyor (İşlem ID: {_transactionId})...");
                DisplayRequestData("ML Model Değerlendirme İsteği", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/evaluate-model", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Test sonucunu ekle
                    _completedTests.Add(new ContextTest
                    {
                        ContextName = "ML Model Evaluation",
                        Endpoint = "/api/FraudDetection/evaluate-model",
                        IsSuccess = response.IsSuccessStatusCode,
                        RiskScore = ExtractRiskScore(content, "riskLevel"),
                        ResultType = ExtractResultType(content, "riskLevel"),
                        TriggeredRuleCount = ExtractTriggeredRuleCount(content, "riskFactors")
                    });
                }

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ML model değerlendirmesi sırasında hata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 8. Kapsamlı kontrol (Comprehensive Check)
        /// </summary>
        private static async Task PerformComprehensiveCheck()
        {
            try
            {
                var request = CreateSampleComprehensiveCheckRequest();

                Console.WriteLine($"Kapsamlı dolandırıcılık kontrolü yapılıyor...");
                Console.WriteLine("İstek çok büyük olduğu için görüntülenmiyor, API'ye gönderiliyor...");

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/comprehensive-check", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Test sonucunu ekle
                    _completedTests.Add(new ContextTest
                    {
                        ContextName = "Comprehensive Fraud Check",
                        Endpoint = "/api/FraudDetection/comprehensive-check",
                        IsSuccess = response.IsSuccessStatusCode,
                        RiskScore = ExtractRiskScore(content, "overallRiskScore"),
                        ResultType = ExtractResultType(content, "overallResultType"),
                        TriggeredRuleCount = -1 // Burada toplam sayıyı hesaplamak zor, gösterilmiyor
                    });
                }

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kapsamlı kontrol sırasında hata: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Sample Request Creation Methods

        /// <summary>
        /// İşlem isteği oluştur
        /// </summary>
        private static TransactionRequest CreateSampleTransactionRequest()
        {
            return new TransactionRequest
            {
                UserId = Guid.NewGuid(),
                Amount = 4400.00m,
                MerchantId = "MERCHANT_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Type = TransactionType.Transfer,

                Location = new LocationRequest
                {
                    Latitude = 41.0082,
                    Longitude = 28.9784,
                    Country = "TR",
                    City = "Istanbul"
                },

                DeviceInfo = new DeviceInfoRequest
                {
                    DeviceId = "device_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    DeviceType = "Mobile",
                    IpAddress = "191.168.1.1",
                    UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.2 Mobile/15E148 Safari/604.1",
                    AdditionalInfo = new Dictionary<string, string>
                    {
                        { "OS", "iOS 14.7.1" },
                        { "Model", "iPhone 12" }
                    }
                },

                AdditionalDataRequest = new TransactionAdditionalDataRequest
                {
                    CardType = "VISA",
                    CardBin = "411111",
                    CardLast4 = "1234",
                    CardExpiryMonth = 12,
                    CardExpiryYear = 2025,
                    BankName = "XYZ Bank",
                    BankCountry = "TR",
                    DaysSinceFirstTransaction = 120,
                    TransactionVelocity24h = 5,
                    AverageTransactionAmount = 1800.00m,
                    IsNewPaymentMethod = false,
                    IsInternational = false,
                    VFactors = new Dictionary<string, float>
                    {
                        { "V1", -0.4532f },
                        { "V2", 1.6782f },
                        { "V3", -1.5032f },
                        { "V4", 0.4199f },
                        { "V5", -0.6366f },
                        { "V10", -1.9777f },
                        { "V14", -0.5423f }
                    },
                    CustomValues = new Dictionary<string, string>
                    {
                        { "Time", "86742" },
                        { "RecurringPayment", "false" },
                        { "HasRecentRefund", "false" }
                    }
                }
            };
        }

        /// <summary>
        /// İşlem bağlam kontrolü isteği oluştur
        /// </summary>
        private static TransactionCheckRequest CreateSampleTransactionCheckRequest()
        {
            return new TransactionCheckRequest
            {
                TransactionId = _transactionId,
                AccountId = _accountId,
                Amount = 1500.00m,
                Currency = "TRY",
                TransactionType = TransactionType.Transfer,
                TransactionDate = DateTime.UtcNow,
                RecipientAccountId = Guid.NewGuid(),
                RecipientAccountNumber = "TR123456789012345678901234",
                RecipientCountry = "TR",
                UserTransactionCount24h = 5,
                UserTotalAmount24h = 5000.00m,
                UserAverageTransactionAmount = 1200.00m,
                DaysSinceFirstTransaction = 120,
                UniqueRecipientCount1h = 2,
                AdditionalData = new Dictionary<string, object>
                {
                    { "IsNewPaymentMethod", false },
                    { "IsInternational", false },
                    { "DaysSinceLastTransaction", 2 },
                    { "V1", "-0.4532" },
                    { "V2", "1.6782" },
                    { "V3", "-1.5032" },
                    { "V4", "0.4199" },
                    { "V5", "-0.6366" },
                    { "V10", "-1.9777" },
                    { "V14", "-0.5423" },
                    { "Time", "86742" }
                }
            };
        }

        /// <summary>
        /// Hesap erişim kontrolü isteği oluştur
        /// </summary>
        private static AccountAccessCheckRequest CreateSampleAccountAccessCheckRequest()
        {
            return new AccountAccessCheckRequest
            {
                AccountId = _accountId,
                Username = "test.user",
                AccessDate = DateTime.UtcNow,
                IpAddress = _ipAddress,
                CountryCode = "TR",
                City = "Istanbul",
                DeviceId = _deviceId,
                IsTrustedDevice = true,
                UniqueIpCount24h = 2,
                UniqueCountryCount24h = 1,
                IsSuccessful = true,
                FailedLoginAttempts = 0,
                TypicalAccessHours = new List<int> { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                TypicalAccessDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                TypicalCountries = new List<string> { "TR" },
                AdditionalData = new Dictionary<string, object>
                {
                    { "Browser", "Chrome" },
                    { "OperatingSystem", "Windows" }
                }
            };
        }

        /// <summary>
        /// IP kontrolü isteği oluştur
        /// </summary>
        private static IpCheckRequest CreateSampleIpCheckRequest()
        {
            return new IpCheckRequest
            {
                IpAddress = _ipAddress,
                CountryCode = "TR",
                City = "Istanbul",
                IspAsn = "AS34984 TELLCOM ILETISIM HIZMETLERI A.S.",
                ReputationScore = 80,
                IsBlacklisted = false,
                BlacklistNotes = "",
                IsDatacenterOrProxy = false,
                NetworkType = "Residential",
                UniqueAccountCount10m = 1,
                UniqueAccountCount1h = 2,
                UniqueAccountCount24h = 3,
                FailedLoginCount10m = 0,
                AdditionalData = new Dictionary<string, object>
                {
                    { "LastSeenDate", DateTime.UtcNow.AddDays(-1) }
                }
            };
        }

        /// <summary>
        /// Cihaz kontrolü isteği oluştur
        /// </summary>
        private static DeviceCheckRequest CreateSampleDeviceCheckRequest()
        {
            return new DeviceCheckRequest
            {
                DeviceId = _deviceId,
                DeviceType = "Mobile",
                OperatingSystem = "iOS 14.7.1",
                Browser = "Safari Mobile",
                IpAddress = _ipAddress,
                CountryCode = "TR",
                IsEmulator = false,
                IsJailbroken = false,
                IsRooted = false,
                FirstSeenDate = DateTime.UtcNow.AddDays(-30),
                LastSeenDate = DateTime.UtcNow,
                UniqueAccountCount24h = 1,
                UniqueIpCount24h = 2,
                AdditionalData = new Dictionary<string, object>
                {
                    { "DeviceModel", "iPhone 12" },
                    { "AppVersion", "1.0.5" }
                }
            };
        }

        /// <summary>
        /// Oturum kontrolü isteği oluştur
        /// </summary>
        private static SessionCheckRequest CreateSampleSessionCheckRequest()
        {
            DateTime startTime = DateTime.UtcNow.AddMinutes(-15);
            DateTime lastActivityTime = DateTime.UtcNow;

            return new SessionCheckRequest
            {
                SessionId = _sessionId,
                AccountId = _accountId,
                StartTime = startTime,
                LastActivityTime = lastActivityTime,
                DurationMinutes = (int)(lastActivityTime - startTime).TotalMinutes,
                IpAddress = _ipAddress,
                DeviceId = _deviceId,
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.2 Mobile/15E148 Safari/604.1",
                RapidNavigationCount = 3,
                AdditionalData = new Dictionary<string, object>
                {
                    { "PageViews", 12 },
                    { "LastUrl", "/account/summary" }
                }
            };
        }

        /// <summary>
        /// ML modeli değerlendirme isteği oluştur
        /// </summary>
        private static ModelEvaluationRequest CreateSampleModelEvaluationRequest()
        {
            return new ModelEvaluationRequest
            {
                TransactionId = _transactionId,
                Amount = 1500.00m,
                TransactionDate = DateTime.UtcNow,
                TransactionType = TransactionType.Transfer,
                Features = new Dictionary<string, string>
                {
                    { "V1", "-0.4532" },
                    { "V2", "1.6782" },
                    { "V3", "-1.5032" },
                    { "V4", "0.4199" },
                    { "V5", "-0.6366" },
                    { "V6", "-2.3561" },
                    { "V7", "1.0487" },
                    { "V8", "-0.8795" },
                    { "V9", "0.3578" },
                    { "V10", "-1.9777" },
                    { "V11", "1.4576" },
                    { "V12", "2.7834" },
                    { "V13", "-1.5632" },
                    { "V14", "-0.5423" },
                    { "V15", "0.4521" },
                    { "V16", "-2.3487" },
                    { "V17", "-3.0045" },
                    { "V18", "0.3214" },
                    { "V19", "0.1257" },
                    { "V20", "-0.5897" },
                    { "V21", "-0.7845" },
                    { "V22", "0.2587" },
                    { "V23", "0.1257" },
                    { "V24", "-0.0587" },
                    { "V25", "0.1243" },
                    { "V26", "-0.3547" },
                    { "V27", "0.1456" },
                    { "V28", "-0.0321" },
                    { "Time", "86742" }
                },
                AdditionalData = new Dictionary<string, object>
                {
                    { "DaysSinceFirstTransaction", 120 },
                    { "TransactionVelocity24h", 5 },
                    { "AverageTransactionAmount", 1200.00 }
                }
            };
        }

        /// <summary>
        /// Kapsamlı dolandırıcılık kontrolü isteği oluştur
        /// </summary>
        private static ComprehensiveFraudCheckRequest CreateSampleComprehensiveCheckRequest()
        {
            // Tüm önceki istekleri kullanarak kapsamlı bir istek oluştur
            var transactionRequest = CreateSampleTransactionCheckRequest();
            var accountRequest = CreateSampleAccountAccessCheckRequest();
            var ipRequest = CreateSampleIpCheckRequest();
            var deviceRequest = CreateSampleDeviceCheckRequest();
            var sessionRequest = CreateSampleSessionCheckRequest();
            var modelRequest = CreateSampleModelEvaluationRequest();

            // Kapsamlı kontrolü tüm bağlam kontrolleriyle birleştir
            return new ComprehensiveFraudCheckRequest
            {
                Transaction = transactionRequest,
                Account = accountRequest,
                IpAddress = ipRequest,
                Device = deviceRequest,
                Session = sessionRequest,
                ModelEvaluation = modelRequest
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// API URL'sini yapılandır
        /// </summary>
        private static void ConfigureApiUrl()
        {
            Console.Write($"API temel URL'sini girin (varsayılan için Enter: {_baseUrl}): ");
            string baseUrlInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(baseUrlInput))
            {
                // URL formatını doğrula
                if (Uri.TryCreate(baseUrlInput, UriKind.Absolute, out Uri uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    _baseUrl = baseUrlInput.TrimEnd('/');
                }
                else
                {
                    Console.WriteLine($"Geçersiz URL formatı. Varsayılan URL kullanılıyor: {_baseUrl}");
                }
            }

            try
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                Console.WriteLine($"Kullanılan API URL'si: {_baseUrl}");
            }
            catch (UriFormatException)
            {
                Console.WriteLine($"Geçersiz URI formatı. Varsayılan URL kullanılıyor: {_baseUrl}");
                _baseUrl = "http://localhost:5112";
                _httpClient.BaseAddress = new Uri(_baseUrl);
            }
        }

        /// <summary>
        /// İnceleme için duraklatma
        /// </summary>
        private static void PauseForReview(string stepName)
        {
            Console.WriteLine($"\n{stepName.ToUpper()} tamamlandı. Sonraki adıma geçmek için bir tuşa basın...");
            Console.ReadKey();
            Console.WriteLine();
        }

        /// <summary>
        /// İstek verilerini görüntüle
        /// </summary>
        private static void DisplayRequestData<T>(string title, T data)
        {
            Console.WriteLine($"\n{title}:");
            Console.WriteLine(JsonSerializer.Serialize(data, _jsonOptions));
            Console.WriteLine();
        }

        /// <summary>
        /// Yanıtı görüntüle
        /// </summary>
        private static async Task DisplayResponse(HttpResponseMessage response)
        {
            Console.WriteLine($"Durum: {(int)response.StatusCode} {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("\nYanıt:");

                // JSON yanıtını formatla
                try
                {
                    var jsonDocument = JsonDocument.Parse(content);
                    string formattedJson = JsonSerializer.Serialize(jsonDocument, _jsonOptions);
                    Console.WriteLine(formattedJson);
                }
                catch
                {
                    // JSON olarak ayrıştırılamazsa olduğu gibi görüntüle
                    Console.WriteLine(content);
                }
            }
            else
            {
                Console.WriteLine($"Hata: {response.ReasonPhrase}");

                try
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Hata Detayları: {errorContent}");
                }
                catch
                {
                    Console.WriteLine("Hata detayları okunamadı.");
                }
            }
        }

        /// <summary>
        /// JSON yanıtından risk skorunu çıkar
        /// </summary>
        private static int ExtractRiskScore(string jsonContent, string scorePropertyName = "riskScore")
        {
            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                if (root.TryGetProperty(scorePropertyName, out var scoreElement) && scoreElement.ValueKind == JsonValueKind.Number)
                {
                    return scoreElement.GetInt32();
                }
            }
            catch {}

            return 0;
        }

        /// <summary>
        /// JSON yanıtından sonuç tipini çıkar
        /// </summary>
        private static string ExtractResultType(string jsonContent, string typePropertyName = "resultType")
        {
            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                if (root.TryGetProperty(typePropertyName, out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
                {
                    return typeElement.GetString();
                }
            }
            catch {}

            return "Unknown";
        }

        /// <summary>
        /// JSON yanıtından tetiklenen kural sayısını çıkar
        /// </summary>
        private static int ExtractTriggeredRuleCount(string jsonContent, string countPropertyName = "triggeredRuleCount")
        {
            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                if (root.TryGetProperty(countPropertyName, out var countElement) && countElement.ValueKind == JsonValueKind.Number)
                {
                    return countElement.GetInt32();
                }
                else if (countPropertyName == "riskFactors" && root.TryGetProperty(countPropertyName, out var factorsElement) && 
                         factorsElement.ValueKind == JsonValueKind.Array)
                {
                    return factorsElement.GetArrayLength();
                }
            }
            catch {}

            return 0;
        }

        /// <summary>
        /// Test özetini görüntüle
        /// </summary>
        private static void DisplayTestSummary()
        {
            Console.WriteLine("\n=== Bağlamsal Analiz Testi Özeti ===\n");
            Console.WriteLine($"İşlem ID: {_transactionId}");
            Console.WriteLine($"Hesap ID: {_accountId}");
            Console.WriteLine($"Tamamlanan Test Sayısı: {_completedTests.Count}\n");

            Console.WriteLine("Test Sonuçları:");
            Console.WriteLine(new string('-', 100));
            Console.WriteLine(string.Format("{0,-25} | {1,-15} | {2,-8} | {3,-25} | {4,-10}",
                "Bağlam Adı", "Başarılı", "Risk Skoru", "Sonuç Tipi", "Tetiklenen Kural"));
            Console.WriteLine(new string('-', 100));

            foreach (var test in _completedTests)
            {
                Console.WriteLine(string.Format("{0,-25} | {1,-15} | {2,-8} | {3,-25} | {4,-10}",
                    test.ContextName,
                    test.IsSuccess ? "Evet" : "Hayır",
                    test.RiskScore,
                    test.ResultType,
                    test.TriggeredRuleCount >= 0 ? test.TriggeredRuleCount.ToString() : "N/A"));
            }

            Console.WriteLine(new string('-', 100));
        }

        #endregion

    }

    #region Request DTO Classes

    /// <summary>
    /// İşlem isteği
    /// </summary>
    public class TransactionRequest
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string MerchantId { get; set; }
        public TransactionType Type { get; set; }
        public LocationRequest Location { get; set; }
        public DeviceInfoRequest DeviceInfo { get; set; }
        public TransactionAdditionalDataRequest AdditionalDataRequest { get; set; }
    }

    /// <summary>
    /// Lokasyon bilgileri
    /// </summary>
    public class LocationRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
    }

    /// <summary>
    /// Cihaz bilgileri
    /// </summary>
    public class DeviceInfoRequest
    {
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public Dictionary<string, string> AdditionalInfo { get; set; } = new();
    }

    /// <summary>
    /// İşlem ek bilgileri
    /// </summary>
    public class TransactionAdditionalDataRequest
    {
        public string CardType { get; set; }
        public string CardBin { get; set; }
        public string CardLast4 { get; set; }
        public int? CardExpiryMonth { get; set; }
        public int? CardExpiryYear { get; set; }
        public string BankName { get; set; }
        public string BankCountry { get; set; }
        public Dictionary<string, float> VFactors { get; set; } = new();
        public int? DaysSinceFirstTransaction { get; set; }
        public int? TransactionVelocity24h { get; set; }
        public decimal? AverageTransactionAmount { get; set; }
        public bool? IsNewPaymentMethod { get; set; }
        public bool? IsInternational { get; set; }
        public Dictionary<string, string> CustomValues { get; set; } = new();
    }

    /// <summary>
    /// İşlem kontrolü isteği
    /// </summary>
    public class TransactionCheckRequest
    {
        public Guid TransactionId { get; set; }
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public TransactionType TransactionType { get; set; }
        public DateTime TransactionDate { get; set; }
        public Guid? RecipientAccountId { get; set; }
        public string RecipientAccountNumber { get; set; }
        public string RecipientCountry { get; set; }
        public int UserTransactionCount24h { get; set; }
        public decimal UserTotalAmount24h { get; set; }
        public decimal UserAverageTransactionAmount { get; set; }
        public int DaysSinceFirstTransaction { get; set; }
        public int UniqueRecipientCount1h { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// Hesap erişimi kontrolü isteği
    /// </summary>
    public class AccountAccessCheckRequest
    {
        public Guid AccountId { get; set; }
        public string Username { get; set; }
        public DateTime AccessDate { get; set; }
        public string IpAddress { get; set; }
        public string CountryCode { get; set; }
        public string City { get; set; }
        public string DeviceId { get; set; }
        public bool IsTrustedDevice { get; set; }
        public int UniqueIpCount24h { get; set; }
        public int UniqueCountryCount24h { get; set; }
        public bool IsSuccessful { get; set; }
        public int FailedLoginAttempts { get; set; }
        public List<int> TypicalAccessHours { get; set; }
        public List<string> TypicalAccessDays { get; set; }
        public List<string> TypicalCountries { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// IP kontrolü isteği
    /// </summary>
    public class IpCheckRequest
    {
        public string IpAddress { get; set; }
        public string CountryCode { get; set; }
        public string City { get; set; }
        public string IspAsn { get; set; }
        public int ReputationScore { get; set; }
        public bool IsBlacklisted { get; set; }
        public string BlacklistNotes { get; set; }
        public bool IsDatacenterOrProxy { get; set; }
        public string NetworkType { get; set; }
        public int UniqueAccountCount10m { get; set; }
        public int UniqueAccountCount1h { get; set; }
        public int UniqueAccountCount24h { get; set; }
        public int FailedLoginCount10m { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// Oturum kontrolü isteği
    /// </summary>
    public class SessionCheckRequest
    {
        public Guid SessionId { get; set; }
        public Guid AccountId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastActivityTime { get; set; }
        public int DurationMinutes { get; set; }
        public string IpAddress { get; set; }
        public string DeviceId { get; set; }
        public string UserAgent { get; set; }
        public int RapidNavigationCount { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// Cihaz kontrolü isteği
    /// </summary>
    public class DeviceCheckRequest
    {
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public string OperatingSystem { get; set; }
        public string Browser { get; set; }
        public string IpAddress { get; set; }
        public string CountryCode { get; set; }
        public bool IsEmulator { get; set; }
        public bool IsJailbroken { get; set; }
        public bool IsRooted { get; set; }
        public DateTime? FirstSeenDate { get; set; }
        public DateTime? LastSeenDate { get; set; }
        public int UniqueAccountCount24h { get; set; }
        public int UniqueIpCount24h { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// ML modeli değerlendirme isteği
    /// </summary>
    public class ModelEvaluationRequest
    {
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionType TransactionType { get; set; }
        public Dictionary<string, string> Features { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// Kapsamlı dolandırıcılık kontrolü isteği
    /// </summary>
    public class ComprehensiveFraudCheckRequest
    {
        public TransactionCheckRequest Transaction { get; set; }
        public AccountAccessCheckRequest Account { get; set; }
        public IpCheckRequest IpAddress { get; set; }
        public DeviceCheckRequest Device { get; set; }
        public SessionCheckRequest Session { get; set; }
        public ModelEvaluationRequest ModelEvaluation { get; set; }
    }

    public enum TransactionType
    {
        Purchase,
        Withdrawal,
        Transfer,
        Deposit,
        CreditCard
    }

    #endregion
}