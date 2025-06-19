using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FraudDetection.ApiTester
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private static string _baseUrl = "http://localhost:5112"; // Default base URL

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Fraud Detection API Tester ===");

            // Configure base URL
            Console.Write($"Enter API base URL (press Enter for default: {_baseUrl}): ");
            string baseUrlInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(baseUrlInput))
            {
                _baseUrl = baseUrlInput.TrimEnd('/');
            }

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            bool exit = false;
            while (!exit)
            {
                DisplayMenu();

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await TestCheckTransaction();
                        break;
                    case "2":
                        await TestCheckAccountAccess();
                        break;
                    case "3":
                        await TestCheckIpAddress();
                        break;
                    case "4":
                        await TestCheckDevice();
                        break;
                    case "5":
                        await TestCheckSession();
                        break;
                    case "6":
                        await TestEvaluateModel();
                        break;
                    case "7":
                        await TestComprehensiveCheck();
                        break;
                    case "8":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }

                if (!exit)
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
        }

        static void DisplayMenu()
        {
            Console.WriteLine("\nChoose an API endpoint to test:");
            Console.WriteLine("1. Check Transaction");
            Console.WriteLine("2. Check Account Access");
            Console.WriteLine("3. Check IP Address");
            Console.WriteLine("4. Check Device");
            Console.WriteLine("5. Check Session");
            Console.WriteLine("6. Evaluate with ML Model");
            Console.WriteLine("7. Comprehensive Check");
            Console.WriteLine("8. Exit");
            Console.Write("\nEnter your choice (1-8): ");
        }

        #region API Test Methods

        static async Task TestCheckTransaction()
        {
            Console.WriteLine("\n=== Testing Check Transaction API ===");

            var request = CreateSampleTransactionCheckRequest();

            try
            {
                DisplayRequestData("Transaction Check Request", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-transaction", request);

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        static async Task TestCheckAccountAccess()
        {
            Console.WriteLine("\n=== Testing Check Account Access API ===");

            var request = CreateSampleAccountAccessCheckRequest();

            try
            {
                DisplayRequestData("Account Access Check Request", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-account-access", request);

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task TestCheckIpAddress()
        {
            Console.WriteLine("\n=== Testing Check IP Address API ===");

            var request = CreateSampleIpCheckRequest();

            try
            {
                DisplayRequestData("IP Check Request", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-ip", request);

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task TestCheckDevice()
        {
            Console.WriteLine("\n=== Testing Check Device API ===");

            var request = CreateSampleDeviceCheckRequest();

            try
            {
                DisplayRequestData("Device Check Request", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-device", request);

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task TestCheckSession()
        {
            Console.WriteLine("\n=== Testing Check Session API ===");

            var request = CreateSampleSessionCheckRequest();

            try
            {
                DisplayRequestData("Session Check Request", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-session", request);

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task TestEvaluateModel()
        {
            Console.WriteLine("\n=== Testing Evaluate with ML Model API ===");

            var request = CreateSampleModelEvaluationRequest();

            try
            {
                DisplayRequestData("Model Evaluation Request", request);

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/evaluate-model", request);

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task TestComprehensiveCheck()
        {
            Console.WriteLine("\n=== Testing Comprehensive Check API ===");

            var request = CreateSampleComprehensiveCheckRequest();

            try
            {
                Console.WriteLine("Sending comprehensive check request...");
                // We don't display the full request as it would be too large

                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/comprehensive-check", request);

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Sample Request Creation Methods

        static TransactionCheckRequest CreateSampleTransactionCheckRequest()
        {
            return new TransactionCheckRequest
            {
                TransactionId = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
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

        static AccountAccessCheckRequest CreateSampleAccountAccessCheckRequest()
        {
            return new AccountAccessCheckRequest
            {
                AccountId = Guid.NewGuid(),
                Username = "test.user",
                AccessDate = DateTime.UtcNow,
                IpAddress = "192.168.1.1",
                CountryCode = "TR",
                City = "Istanbul",
                DeviceId = "device_" + Guid.NewGuid().ToString("N").Substring(0, 8),
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

        static IpCheckRequest CreateSampleIpCheckRequest()
        {
            return new IpCheckRequest
            {
                IpAddress = "192.168.1.1",
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

        static DeviceCheckRequest CreateSampleDeviceCheckRequest()
        {
            return new DeviceCheckRequest
            {
                DeviceId = "device_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                DeviceType = "Mobile",
                OperatingSystem = "Android 12",
                Browser = "Chrome Mobile",
                IpAddress = "192.168.1.1",
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
                    { "DeviceModel", "Samsung Galaxy S21" },
                    { "AppVersion", "1.0.5" }
                }
            };
        }

        static SessionCheckRequest CreateSampleSessionCheckRequest()
        {
            DateTime startTime = DateTime.UtcNow.AddMinutes(-15);
            DateTime lastActivityTime = DateTime.UtcNow;

            return new SessionCheckRequest
            {
                SessionId = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                StartTime = startTime,
                LastActivityTime = lastActivityTime,
                DurationMinutes = (int)(lastActivityTime - startTime).TotalMinutes,
                IpAddress = "192.168.1.1",
                DeviceId = "device_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
                RapidNavigationCount = 3,
                AdditionalData = new Dictionary<string, object>
                {
                    { "PageViews", 12 },
                    { "LastUrl", "/account/summary" }
                }
            };
        }

        static ModelEvaluationRequest CreateSampleModelEvaluationRequest()
        {
            return new ModelEvaluationRequest
            {
                TransactionId = Guid.NewGuid(),
                Amount = 1500.00m,
                TransactionDate = DateTime.UtcNow,
                TransactionType = TransactionType.Transfer,
                Features = new Dictionary<string, string>
                {
                    { "V1", "-2.8796" },
                    { "V2", "4.6521" },
                    { "V3", "-4.2178" },
                    { "V4", "-2.8954" },
                    { "V5", "0.9867" },
                    { "V6", "-2.3561" },
                    { "V7", "1.0487" },
                    { "V8", "-0.8795" },
                    { "V9", "0.3578" },
                    { "V10", "-3.4891" },
                    { "V11", "1.4576" },
                    { "V12", "2.7834" },
                    { "V13", "-1.5632" },
                    { "V14", "-7.8945" },
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
                    { "Time", "13512" }
                },
                AdditionalData = new Dictionary<string, object>
                {
                    { "DaysSinceFirstTransaction", 120 },
                    { "TransactionVelocity24h", 5 },
                    { "AverageTransactionAmount", 1200.00 }
                }
            };
        }

        static ComprehensiveFraudCheckRequest CreateSampleComprehensiveCheckRequest()
        {
            // Use the same samples for each component
            var transactionRequest = CreateSampleTransactionCheckRequest();
            var accountRequest = CreateSampleAccountAccessCheckRequest();
            var ipRequest = CreateSampleIpCheckRequest();
            var deviceRequest = CreateSampleDeviceCheckRequest();
            var sessionRequest = CreateSampleSessionCheckRequest();
            var modelRequest = CreateSampleModelEvaluationRequest();

            // Create a comprehensive check with all components
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

        static void DisplayRequestData<T>(string title, T data)
        {
            Console.WriteLine($"\n{title}:");
            Console.WriteLine(JsonSerializer.Serialize(data, _jsonOptions));
            Console.WriteLine();
        }

        static async Task DisplayResponse(HttpResponseMessage response)
        {
            Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("\nResponse:");

                // Try to format the JSON response
                try
                {
                    var jsonDocument = JsonDocument.Parse(content);
                    string formattedJson = JsonSerializer.Serialize(jsonDocument, _jsonOptions);
                    Console.WriteLine(formattedJson);
                }
                catch
                {
                    // If unable to parse as JSON, display as-is
                    Console.WriteLine(content);
                }
            }
            else
            {
                Console.WriteLine($"Error: {response.ReasonPhrase}");

                try
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Details: {errorContent}");
                }
                catch
                {
                    Console.WriteLine("Could not read error details.");
                }
            }
        }

        #endregion
    }

    #region Request DTO Classes

    /// <summary>
    /// İşlem kontrolü isteği
    /// </summary>
    public class TransactionCheckRequest
    {
        /// <summary>
        /// İşlem ID'si
        /// </summary>
        public Guid TransactionId { get; set; }

        /// <summary>
        /// Hesap ID'si
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// İşlem tutarı
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Para birimi
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// İşlem tipi
        /// </summary>
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// İşlem tarihi
        /// </summary>
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// Alıcı hesap ID'si
        /// </summary>
        public Guid? RecipientAccountId { get; set; }

        /// <summary>
        /// Alıcı hesap numarası
        /// </summary>
        public string RecipientAccountNumber { get; set; }

        /// <summary>
        /// Alıcı ülkesi
        /// </summary>
        public string RecipientCountry { get; set; }

        /// <summary>
        /// Son 24 saatteki işlem sayısı
        /// </summary>
        public int UserTransactionCount24h { get; set; }

        /// <summary>
        /// Son 24 saatteki toplam işlem tutarı
        /// </summary>
        public decimal UserTotalAmount24h { get; set; }

        /// <summary>
        /// Ortalama işlem tutarı
        /// </summary>
        public decimal UserAverageTransactionAmount { get; set; }

        /// <summary>
        /// İlk işlemden bu yana geçen gün sayısı
        /// </summary>
        public int DaysSinceFirstTransaction { get; set; }

        /// <summary>
        /// Son 1 saatteki farklı alıcı sayısı
        /// </summary>
        public int UniqueRecipientCount1h { get; set; }

        /// <summary>
        /// Ek veriler
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// Hesap erişimi kontrolü isteği
    /// </summary>
    public class AccountAccessCheckRequest
    {
        /// <summary>
        /// Hesap ID'si
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Kullanıcı adı
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Erişim tarihi
        /// </summary>
        public DateTime AccessDate { get; set; }

        /// <summary>
        /// IP adresi
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Ülke kodu
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Şehir
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Cihaz ID'si
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Güvenilir cihaz mı?
        /// </summary>
        public bool IsTrustedDevice { get; set; }

        /// <summary>
        /// Son 24 saatteki farklı IP sayısı
        /// </summary>
        public int UniqueIpCount24h { get; set; }

        /// <summary>
        /// Son 24 saatteki farklı ülke sayısı
        /// </summary>
        public int UniqueCountryCount24h { get; set; }

        /// <summary>
        /// Başarılı giriş mi?
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Son başarısız giriş sayısı
        /// </summary>
        public int FailedLoginAttempts { get; set; }

        /// <summary>
        /// Tipik erişim saatleri
        /// </summary>
        public List<int> TypicalAccessHours { get; set; }

        /// <summary>
        /// Tipik erişim günleri
        /// </summary>
        public List<string> TypicalAccessDays { get; set; }

        /// <summary>
        /// Tipik erişim ülkeleri
        /// </summary>
        public List<string> TypicalCountries { get; set; }

        /// <summary>
        /// Ek veriler
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// IP kontrolü isteği
    /// </summary>
    public class IpCheckRequest
    {
        /// <summary>
        /// IP adresi
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Ülke kodu
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Şehir
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// ISP/ASN bilgisi
        /// </summary>
        public string IspAsn { get; set; }

        /// <summary>
        /// İtibar puanı
        /// </summary>
        public int ReputationScore { get; set; }

        /// <summary>
        /// Kara listede mi?
        /// </summary>
        public bool IsBlacklisted { get; set; }

        /// <summary>
        /// Kara liste notları
        /// </summary>
        public string BlacklistNotes { get; set; }

        /// <summary>
        /// Datacenter/proxy mu?
        /// </summary>
        public bool IsDatacenterOrProxy { get; set; }

        /// <summary>
        /// Ağ tipi
        /// </summary>
        public string NetworkType { get; set; }

        /// <summary>
        /// Son 10 dakikadaki farklı hesap sayısı
        /// </summary>
        public int UniqueAccountCount10m { get; set; }

        /// <summary>
        /// Son 1 saatteki farklı hesap sayısı
        /// </summary>
        public int UniqueAccountCount1h { get; set; }

        /// <summary>
        /// Son 24 saatteki farklı hesap sayısı
        /// </summary>
        public int UniqueAccountCount24h { get; set; }

        /// <summary>
        /// Son 10 dakikadaki başarısız giriş sayısı
        /// </summary>
        public int FailedLoginCount10m { get; set; }

        /// <summary>
        /// Ek veriler
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// Oturum kontrolü isteği
    /// </summary>
    public class SessionCheckRequest
    {
        /// <summary>
        /// Oturum ID'si
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Hesap ID'si
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Oturum başlangıç zamanı
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Son aktivite zamanı
        /// </summary>
        public DateTime LastActivityTime { get; set; }

        /// <summary>
        /// Oturum süresi (dakika)
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// IP adresi
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Cihaz ID'si
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// User-Agent
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Hızlı gezinme sayısı
        /// </summary>
        public int RapidNavigationCount { get; set; }

        /// <summary>
        /// Ek veriler
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// Cihaz kontrolü isteği
    /// </summary>
    public class DeviceCheckRequest
    {
        /// <summary>
        /// Cihaz ID'si
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Cihaz tipi
        /// </summary>
        public string DeviceType { get; set; }

        /// <summary>
        /// İşletim sistemi
        /// </summary>
        public string OperatingSystem { get; set; }

        /// <summary>
        /// Tarayıcı
        /// </summary>
        public string Browser { get; set; }

        /// <summary>
        /// IP adresi
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Ülke kodu
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Emulator mu?
        /// </summary>
        public bool IsEmulator { get; set; }

        /// <summary>
        /// Jailbreak yapılmış mı?
        /// </summary>
        public bool IsJailbroken { get; set; }

        /// <summary>
        /// Root yapılmış mı?
        /// </summary>
        public bool IsRooted { get; set; }

        /// <summary>
        /// İlk görülme tarihi
        /// </summary>
        public DateTime? FirstSeenDate { get; set; }

        /// <summary>
        /// Son görülme tarihi
        /// </summary>
        public DateTime? LastSeenDate { get; set; }

        /// <summary>
        /// Son 24 saatteki farklı hesap sayısı
        /// </summary>
        public int UniqueAccountCount24h { get; set; }

        /// <summary>
        /// Son 24 saatteki farklı IP sayısı
        /// </summary>
        public int UniqueIpCount24h { get; set; }

        /// <summary>
        /// Ek veriler
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// ML modeli değerlendirme isteği
    /// </summary>
    public class ModelEvaluationRequest
    {
        /// <summary>
        /// İşlem ID'si
        /// </summary>
        public Guid TransactionId { get; set; }

        /// <summary>
        /// İşlem tutarı
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// İşlem tarihi
        /// </summary>
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// İşlem tipi
        /// </summary>
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// V1-V28 değerleri ve diğer özellikler
        /// </summary>
        public Dictionary<string, string> Features { get; set; }

        /// <summary>
        /// Ek veriler
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// Kapsamlı dolandırıcılık kontrolü isteği
    /// </summary>
    public class ComprehensiveFraudCheckRequest
    {
        /// <summary>
        /// İşlem kontrolü verileri
        /// </summary>
        public TransactionCheckRequest Transaction { get; set; }

        /// <summary>
        /// Hesap erişimi verileri
        /// </summary>
        public AccountAccessCheckRequest Account { get; set; }

        /// <summary>
        /// IP adresi verileri
        /// </summary>
        public IpCheckRequest IpAddress { get; set; }

        /// <summary>
        /// Cihaz verileri
        /// </summary>
        public DeviceCheckRequest Device { get; set; }

        /// <summary>
        /// Oturum verileri
        /// </summary>
        public SessionCheckRequest Session { get; set; }

        /// <summary>
        /// ML modeli değerlendirme isteği
        /// </summary>
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