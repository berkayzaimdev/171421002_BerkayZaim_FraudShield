using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FraudDetection.ApiTester.ContextTests;

namespace FraudDetection.RuleTester
{
    public class FraudRuleTester
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private static string _baseUrl = "http://localhost:5112"; // Default base URL

        // Test sonuçlarını saklayacak liste
        private static List<RuleTestResult> _testResults = new List<RuleTestResult>();

        // Her testte kullanılacak random sayı üretici
        private static Random _random = new Random();

        /// <summary>
        /// Test sonuçlarını temsil eden sınıf
        /// </summary>
        public class RuleTestResult
        {
            public string RuleName { get; set; }
            public string RuleCategory { get; set; }
            public string TestDescription { get; set; }
            public bool IsRuleTriggered { get; set; }
            public string TriggerDetails { get; set; }
            public int RiskScore { get; set; }
            public string ResultType { get; set; }
            public List<string> AppliedActions { get; set; } = new List<string>();
            public bool IsSuccess { get; set; }
        }

        /// <summary>
        /// Tüm kural testlerini çalıştırır
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Fraud Detection Rule Tester ===");

            // API URL yapılandırması
            ConfigureApiUrl();

            try
            {
                Console.WriteLine("\n=== Starting Rule Tests ===\n");

                // IP Bazlı Çoklu Hesap Erişimi Kuralları Testleri
                await TestIpMultipleAccountAccessRules();

                // IP Bazlı Başarısız Giriş Denemeleri Testleri
                await TestIpFailedLoginRules();

                // Hesap Bazlı Şüpheli Erişim Desenleri Testleri
                await TestAccountSuspiciousAccessRules();

                // Ağ Bazlı Kurallar Testleri
                await TestNetworkRules();

                // Cihaz Yönetimi Testleri
                await TestDeviceManagementRules();

                // Oturum ve Güvenlik Olayları Testleri 
                await TestSessionSecurityRules();

                // İşlem Tabanlı Kurallar Testleri
                await TestTransactionRules();

                // Test sonuçlarını görüntüle
                DisplayTestResults();
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

        #region Rule Category Tests

        /// <summary>
        /// IP Bazlı Çoklu Hesap Erişimi Kurallarını Test Eder
        /// </summary>
        private static async Task TestIpMultipleAccountAccessRules()
        {
            Console.WriteLine("\n--- Testing IP Multiple Account Access Rules ---");

            // Test 1: IP Bazlı 3 Farklı Hesap Erişimi (10 dakika)
            await Test3AccountsFrom1Ip();

            // Test 2: IP Bazlı 5 Farklı Hesap Erişimi (1 saat)
            await Test5AccountsFrom1Ip();

            // Test 3: IP Bazlı 10 Farklı Hesap Erişimi (48 saat)
            await Test10AccountsFrom1Ip();
        }

        /// <summary>
        /// IP Bazlı Başarısız Giriş Denemeleri Kurallarını Test Eder
        /// </summary>
        private static async Task TestIpFailedLoginRules()
        {
            Console.WriteLine("\n--- Testing IP Failed Login Rules ---");

            // Test: IP Bazlı 20 Başarısız Giriş (10 dakika)
            await Test20FailedLoginsFrom1Ip();
        }

        /// <summary>
        /// Hesap Bazlı Şüpheli Erişim Desenleri Kurallarını Test Eder
        /// </summary>
        private static async Task TestAccountSuspiciousAccessRules()
        {
            Console.WriteLine("\n--- Testing Account Suspicious Access Rules ---");

            // Test 1: Hesap Bazlı 5 Farklı IP Erişimi (1 saat)
            await Test5IpsToSameAccount();

            // Test 2: Hesap Bazlı 10 Farklı IP Erişimi (24 saat)
            await Test10IpsToSameAccount();

            // Test 3: Hesap Bazlı 4 Farklı Ülke Erişimi (24 saat)
            await Test4CountriesToSameAccount();
        }

        /// <summary>
        /// Ağ Bazlı Kuralları Test Eder
        /// </summary>
        private static async Task TestNetworkRules()
        {
            Console.WriteLine("\n--- Testing Network Rules ---");

            // Test 1: Tor Ağı Üzerinden Bağlantı
            await TestTorNetworkConnection();

            // Test 2: Kara Liste IP'ler
            await TestBlacklistedIp();
        }

        /// <summary>
        /// Cihaz Yönetimi Kurallarını Test Eder
        /// </summary>
        private static async Task TestDeviceManagementRules()
        {
            Console.WriteLine("\n--- Testing Device Management Rules ---");

            // Test 1: Bilinmeyen cihazdan giriş
            await TestUnknownDevice();

            // Test 2: 10. farklı cihaz talepleri
            await TestMaxDevicesPerAccount();

            // Test 3: Aynı IP'den çok sayıda yeni cihaz ekleme
            await TestMultipleDevicesFromSameIp();
        }

        /// <summary>
        /// Oturum ve Güvenlik Olayları Kurallarını Test Eder
        /// </summary>
        private static async Task TestSessionSecurityRules()
        {
            Console.WriteLine("\n--- Testing Session Security Rules ---");

            // Test 1: Hesap bilgileri sızıntı veritabanlarında tespit edildi
            await TestLeakedCredentials();

            // Test 2: Tek cihazda kesintisiz oturum ≥ 30 dakika
            await TestLongSession();

            // Test 3: 5 farklı alıcıya para transferi / 10 dakika
            await TestMultipleRecipients();
        }

        /// <summary>
        /// İşlem Tabanlı Kuralları Test Eder
        /// </summary>
        private static async Task TestTransactionRules()
        {
            Console.WriteLine("\n--- Testing Transaction Rules ---");

            // Test 1: Yüksek tutarlı işlem
            await TestHighAmountTransaction();

            // Test 2: Kullanıcı ortalamasının 5 katı işlem
            await TestHighAverageTransaction();

            // Test 3: Kısa sürede çok sayıda işlem
            await TestMultipleTransactions();

            // Test 4: Gece saatlerinde yüksek tutarlı işlem
            await TestNightHighAmountTransaction();
        }

        #endregion

        #region Specific Rule Tests

        /// <summary>
        /// IP Bazlı 3 Farklı Hesap Erişimi (10 dakika) kuralını test eder
        /// </summary>
        private static async Task Test3AccountsFrom1Ip()
        {
            Console.WriteLine("Testing: IP Bazlı 3 Farklı Hesap Erişimi (10 dakika)");

            // Test için ortak IP adresi
            string testIp = GenerateRandomIp();

            // 3 farklı hesap oluştur
            var accounts = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Tüm hesaplar için aynı IP'den giriş yap
            var testResults = new List<bool>();

            foreach (var accountId in accounts)
            {
                var request = new AccountAccessCheckRequest
                {
                    AccountId = accountId,
                    Username = $"user_{accountId.ToString().Substring(0, 8)}",
                    AccessDate = DateTime.UtcNow,
                    IpAddress = testIp,
                    CountryCode = "TR",
                    City = "Istanbul",
                    DeviceId = $"device_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                    IsTrustedDevice = true,
                    UniqueIpCount24h = 1,
                    UniqueCountryCount24h = 1,
                    IsSuccessful = true,
                    FailedLoginAttempts = 0,
                    TypicalAccessHours = new List<int> { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                    TypicalAccessDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                    TypicalCountries = new List<string> { "TR" },
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "UniqueAccountCount10m", accounts.Count - 1 }, // Aynı IP'den kaç farklı hesaba giriş yapıldı
                        { "IsRecentLogin", true }
                    }
                };

                // Hesap erişimini test et
                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-account-access", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    testResults.Add(IsRuleTriggered(content, "IP Bazlı 3 Farklı Hesap Erişimi"));

                    // 3. hesap erişiminde kuralın tetiklenmesi bekleniyor
                    if (accounts.IndexOf(accountId) == accounts.Count - 1)
                    {
                        SaveTestResult(content, "IP Bazlı 3 Farklı Hesap Erişimi (10 dakika)", "IP",
                            $"3 farklı hesap ({string.Join(", ", accounts.Select(a => a.ToString().Substring(0, 8)))}) aynı IP'den ({testIp}) 10 dakika içinde erişim yaptı");
                    }
                }
            }

            bool isRuleTriggered = testResults.Any(r => r);
            Console.WriteLine($"Result: Rule triggered: {isRuleTriggered}");
        }

        /// <summary>
        /// IP Bazlı 5 Farklı Hesap Erişimi (1 saat) kuralını test eder
        /// </summary>
        private static async Task Test5AccountsFrom1Ip()
        {
            Console.WriteLine("Testing: IP Bazlı 5 Farklı Hesap Erişimi (1 saat)");

            // Test için ortak IP adresi
            string testIp = GenerateRandomIp();

            // 5 farklı hesap oluştur
            var accounts = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();

            // Tüm hesaplar için aynı IP'den giriş yap
            var testResults = new List<bool>();

            foreach (var accountId in accounts)
            {
                var request = new AccountAccessCheckRequest
                {
                    AccountId = accountId,
                    Username = $"user_{accountId.ToString().Substring(0, 8)}",
                    AccessDate = DateTime.UtcNow,
                    IpAddress = testIp,
                    CountryCode = "TR",
                    City = "Istanbul",
                    DeviceId = $"device_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                    IsTrustedDevice = true,
                    UniqueIpCount24h = 1,
                    UniqueCountryCount24h = 1,
                    IsSuccessful = true,
                    FailedLoginAttempts = 0,
                    TypicalAccessHours = new List<int> { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                    TypicalAccessDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                    TypicalCountries = new List<string> { "TR" },
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "UniqueAccountCount1h", accounts.Count - 1 }, // Aynı IP'den kaç farklı hesaba giriş yapıldı
                        { "IsRecentLogin", true }
                    }
                };

                // Hesap erişimini test et
                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-account-access", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    testResults.Add(IsRuleTriggered(content, "IP Bazlı 5 Farklı Hesap Erişimi"));

                    // 5. hesap erişiminde kuralın tetiklenmesi bekleniyor
                    if (accounts.IndexOf(accountId) == accounts.Count - 1)
                    {
                        SaveTestResult(content, "IP Bazlı 5 Farklı Hesap Erişimi (1 saat)", "IP",
                            $"5 farklı hesap aynı IP'den ({testIp}) 1 saat içinde erişim yaptı");
                    }
                }
            }

            bool isRuleTriggered = testResults.Any(r => r);
            Console.WriteLine($"Result: Rule triggered: {isRuleTriggered}");
        }

        /// <summary>
        /// IP Bazlı 10 Farklı Hesap Erişimi (48 saat) kuralını test eder
        /// </summary>
        private static async Task Test10AccountsFrom1Ip()
        {
            Console.WriteLine("Testing: IP Bazlı 10 Farklı Hesap Erişimi (48 saat)");

            // Test için ortak IP adresi
            string testIp = GenerateRandomIp();

            // 10 farklı hesap oluştur
            var accounts = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();

            // IP CheckRequest ile IP'yi kontrol et
            var ipRequest = new IpCheckRequest
            {
                IpAddress = testIp,
                CountryCode = "TR",
                City = "Istanbul",
                IspAsn = "AS34984 TELLCOM ILETISIM HIZMETLERI A.S.",
                ReputationScore = 80,
                IsBlacklisted = false,
                BlacklistNotes = "",
                IsDatacenterOrProxy = false,
                NetworkType = "Residential",
                UniqueAccountCount10m = accounts.Count / 3,
                UniqueAccountCount1h = accounts.Count / 2,
                UniqueAccountCount24h = accounts.Count, // 48 saat içinde 10 farklı hesap
                FailedLoginCount10m = 0,
                AdditionalData = new Dictionary<string, object>
                {
                    { "LastSeenDate", DateTime.UtcNow.AddDays(-1) }
                }
            };

            // IP'yi test et
            var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-ip", ipRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "IP Bazlı 10 Farklı Hesap Erişimi");

                SaveTestResult(content, "IP Bazlı 10 Farklı Hesap Erişimi (48 saat)", "IP",
                    $"10 farklı hesap aynı IP'den ({testIp}) 48 saat içinde erişim yaptı");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// IP Bazlı 20 Başarısız Giriş (10 dakika) kuralını test eder
        /// </summary>
        private static async Task Test20FailedLoginsFrom1Ip()
        {
            Console.WriteLine("Testing: IP Bazlı 20 Başarısız Giriş (10 dakika)");

            // Test için IP adresi
            string testIp = GenerateRandomIp();

            // IP kontrolü için istek oluştur - 20 başarısız giriş içeren
            var ipRequest = new IpCheckRequest
            {
                IpAddress = testIp,
                CountryCode = "TR",
                City = "Istanbul",
                IspAsn = "AS34984 TELLCOM ILETISIM HIZMETLERI A.S.",
                ReputationScore = 80,
                IsBlacklisted = false,
                BlacklistNotes = "",
                IsDatacenterOrProxy = false,
                NetworkType = "Residential",
                UniqueAccountCount10m = 3,
                UniqueAccountCount1h = 5,
                UniqueAccountCount24h = 8,
                FailedLoginCount10m = 20, // 10 dakika içinde 20 başarısız giriş
                AdditionalData = new Dictionary<string, object>
                {
                    { "LastSeenDate", DateTime.UtcNow.AddDays(-1) }
                }
            };

            // IP'yi test et
            var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-ip", ipRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "IP Bazlı 20 Başarısız Giriş");

                SaveTestResult(content, "IP Bazlı 20 Başarısız Giriş (10 dakika)", "IP",
                    $"20 başarısız giriş denemesi 10 dakika içinde aynı IP'den ({testIp}) yapıldı");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Hesap Bazlı 5 Farklı IP Erişimi (1 saat) kuralını test eder
        /// </summary>
        private static async Task Test5IpsToSameAccount()
        {
            Console.WriteLine("Testing: Hesap Bazlı 5 Farklı IP Erişimi (1 saat)");

            // Test için hesap oluştur
            Guid accountId = Guid.NewGuid();

            // 5 farklı IP adresi oluştur
            var ipAddresses = Enumerable.Range(0, 5).Select(_ => GenerateRandomIp()).ToList();

            // Hesap erişim isteği oluştur
            var request = new AccountAccessCheckRequest
            {
                AccountId = accountId,
                Username = $"user_{accountId.ToString().Substring(0, 8)}",
                AccessDate = DateTime.UtcNow,
                IpAddress = ipAddresses.Last(), // Son IP adresini kullan
                CountryCode = "TR",
                City = "Istanbul",
                DeviceId = $"device_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                IsTrustedDevice = true,
                UniqueIpCount24h = ipAddresses.Count, // 1 saat içinde 5 farklı IP
                UniqueCountryCount24h = 1, // Hepsi aynı ülkeden
                IsSuccessful = true,
                FailedLoginAttempts = 0,
                TypicalAccessHours = new List<int> { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                TypicalAccessDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                TypicalCountries = new List<string> { "TR" },
                AdditionalData = new Dictionary<string, object>
                {
                    { "PreviousIPs", string.Join(",", ipAddresses.Take(ipAddresses.Count - 1)) },
                    { "TimeWindowMinutes", 60 } // 1 saat
                }
            };

            // Hesap erişimini test et
            var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-account-access", request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Hesap Bazlı 5 Farklı IP Erişimi");

                SaveTestResult(content, "Hesap Bazlı 5 Farklı IP Erişimi (1 saat)", "Account",
                    $"Aynı hesap ({accountId.ToString().Substring(0, 8)}) 1 saat içinde 5 farklı IP'den erişim yaptı");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Hesap Bazlı 10 Farklı IP Erişimi (24 saat) kuralını test eder
        /// </summary>
        private static async Task Test10IpsToSameAccount()
        {
            Console.WriteLine("Testing: Hesap Bazlı 10 Farklı IP Erişimi (24 saat)");

            // Test için hesap oluştur
            Guid accountId = Guid.NewGuid();

            // 10 farklı IP adresi oluştur
            var ipAddresses = Enumerable.Range(0, 10).Select(_ => GenerateRandomIp()).ToList();

            // Hesap erişim isteği oluştur
            var request = new AccountAccessCheckRequest
            {
                AccountId = accountId,
                Username = $"user_{accountId.ToString().Substring(0, 8)}",
                AccessDate = DateTime.UtcNow,
                IpAddress = ipAddresses.Last(), // Son IP adresini kullan
                CountryCode = "TR",
                City = "Istanbul",
                DeviceId = $"device_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                IsTrustedDevice = true,
                UniqueIpCount24h = ipAddresses.Count, // 24 saat içinde 10 farklı IP
                UniqueCountryCount24h = 1, // Hepsi aynı ülkeden
                IsSuccessful = true,
                FailedLoginAttempts = 0,
                TypicalAccessHours = new List<int> { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                TypicalAccessDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                TypicalCountries = new List<string> { "TR" },
                AdditionalData = new Dictionary<string, object>
                {
                    { "PreviousIPs", string.Join(",", ipAddresses.Take(ipAddresses.Count - 1)) },
                    { "TimeWindowMinutes", 1440 } // 24 saat
                }
            };

            // Hesap erişimini test et
            var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-account-access", request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Hesap Bazlı 10 Farklı IP Erişimi");

                SaveTestResult(content, "Hesap Bazlı 10 Farklı IP Erişimi (24 saat)", "Account",
                    $"Aynı hesap ({accountId.ToString().Substring(0, 8)}) 24 saat içinde 10 farklı IP'den erişim yaptı");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Hesap Bazlı 4 Farklı Ülke Erişimi (24 saat) kuralını test eder
        /// </summary>
        private static async Task Test4CountriesToSameAccount()
        {
            Console.WriteLine("Testing: Hesap Bazlı 4 Farklı Ülke Erişimi (24 saat)");

            // Test için hesap oluştur
            Guid accountId = Guid.NewGuid();

            // 4 farklı ülke tanımla
            var countries = new List<string> { "TR", "US", "DE", "GB" };

            // Hesap erişim isteği oluştur
            var request = new AccountAccessCheckRequest
            {
                AccountId = accountId,
                Username = $"user_{accountId.ToString().Substring(0, 8)}",
                AccessDate = DateTime.UtcNow,
                IpAddress = GenerateRandomIp(),
                CountryCode = countries.Last(), // Son ülkeyi kullan
                City = "London",
                DeviceId = $"device_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                IsTrustedDevice = true,
                UniqueIpCount24h = 4, // Her ülke için 1 farklı IP
                UniqueCountryCount24h = countries.Count, // 24 saat içinde 4 farklı ülke
                IsSuccessful = true,
                FailedLoginAttempts = 0,
                TypicalAccessHours = new List<int> { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                TypicalAccessDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                TypicalCountries = new List<string> { "TR" }, // Sadece TR tipik ülke
                AdditionalData = new Dictionary<string, object>
                {
                    { "PreviousCountries", string.Join(",", countries.Take(countries.Count - 1)) },
                    { "TimeWindowMinutes", 1440 } // 24 saat
                }
            };

            // Hesap erişimini test et
            var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-account-access", request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Hesap Bazlı 4 Farklı Ülke Erişimi");

                SaveTestResult(content, "Hesap Bazlı 4 Farklı Ülke Erişimi (24 saat)", "Account",
                    $"Aynı hesap ({accountId.ToString().Substring(0, 8)}) 24 saat içinde 4 farklı ülkeden ({string.Join(", ", countries)}) erişim yaptı");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Tor Ağı Üzerinden Bağlantı kuralını test eder
        /// </summary>
        private static async Task TestTorNetworkConnection()
        {
            Console.WriteLine("Testing: Tor Ağı Üzerinden Bağlantı");

            // Test için IP adresi
            string torIp = GenerateRandomIp();

            // IP kontrolü için istek oluştur
            var ipRequest = new IpCheckRequest
            {
                IpAddress = torIp,
                CountryCode = "US", // Tor exit node genellikle farklı ülkelerden
                City = "Unknown",
                IspAsn = "AS0 TOR EXIT NODE",
                ReputationScore = 40, // Düşük itibar skoru
                IsBlacklisted = false,
                BlacklistNotes = "",
                IsDatacenterOrProxy = true, // Proxy olarak işaretli
                NetworkType = "TOR", // Tor ağı olarak belirtilmiş
                UniqueAccountCount10m = 2,
                UniqueAccountCount1h = 5,
                UniqueAccountCount24h = 10,
                FailedLoginCount10m = 0,
                AdditionalData = new Dictionary<string, object>
                {
                    { "IsTorExitNode", true }
                }
            };

            // IP'yi test et
            var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-ip", ipRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Tor Ağı Üzerinden Bağlantı");

                SaveTestResult(content, "Tor Ağı Üzerinden Bağlantı", "Network",
                    $"Bağlantı Tor ağı üzerinden yapıldı, IP: {torIp}");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Kara Liste IP'ler kuralını test eder
        /// </summary>
        private static async Task TestBlacklistedIp()
        {
            Console.WriteLine("Testing: Kara Liste IP'ler");

            // Kara listedeki IP'lerden birini kullan
            string blacklistedIp = "123.456.789.0"; // Seeder'da tanımlanan kara liste IP'si

            // IP kontrolü için istek oluştur
            var ipRequest = new IpCheckRequest
            {
                IpAddress = blacklistedIp,
                CountryCode = "RU",
                City = "Moscow",
                IspAsn = "AS1234 UNKNOWN",
                ReputationScore = 10, // Çok düşük itibar skoru
                IsBlacklisted = true, // Kara listede olduğunu belirt
                BlacklistNotes = "Known malicious IP",
                IsDatacenterOrProxy = false,
                NetworkType = "Residential",
                UniqueAccountCount10m = 0,
                UniqueAccountCount1h = 0,
                UniqueAccountCount24h = 0,
                FailedLoginCount10m = 0,
                AdditionalData = new Dictionary<string, object>
                {
                    { "BlacklistReason", "Manual blacklist entry" }
                }
            };

            // IP'yi test et
            var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-ip", ipRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Kara Liste IP'ler");

                SaveTestResult(content, "Kara Liste IP'ler", "IP",
                    $"Kara listedeki IP adresi ({blacklistedIp}) erişim yapmaya çalıştı");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Bilinmeyen cihazdan giriş kuralını test eder
        /// </summary>
        private static async Task TestUnknownDevice()
        {
            Console.WriteLine("Testing: Bilinmeyen cihazdan giriş");

            // Test için hesap ve cihaz bilgileri
            Guid accountId = Guid.NewGuid();
            string newDeviceId = $"unknown_device_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            // Cihaz kontrolü için istek oluştur
            var deviceRequest = new DeviceCheckRequest
            {
                DeviceId = newDeviceId,
                DeviceType = "Mobile",
                OperatingSystem = "Android 12",
                Browser = "Chrome Mobile",
                IpAddress = GenerateRandomIp(),
                CountryCode = "TR",
                IsEmulator = false,
                IsJailbroken = false,
                IsRooted = false,
                FirstSeenDate = DateTime.UtcNow, // İlk kez görülen cihaz
                LastSeenDate = DateTime.UtcNow,
                UniqueAccountCount24h = 1,
                UniqueIpCount24h = 1,
                AdditionalData = new Dictionary<string, object>
                {
                    { "IsFirstLogin", true },
                    { "IsTrustedDevice", false }, // Güvenilir cihaz değil
                    { "DeviceModel", "Samsung Galaxy S22" },
                    { "AppVersion", "1.0.5" }
                }
            };

            // Cihazı test et
            var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-device", deviceRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Bilinmeyen cihazdan giriş");

                SaveTestResult(content, "Bilinmeyen cihazdan giriş", "Device",
                    $"Hesap ({accountId.ToString().Substring(0, 8)}) daha önce görülmemiş yeni bir cihazdan ({newDeviceId}) giriş yaptı");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Maksimum 10 Farklı Cihaz kuralını test eder
        /// </summary>
        private static async Task TestMaxDevicesPerAccount()
        {
            Console.WriteLine("Testing: Maksimum 10 Farklı Cihaz");

            // Test için hesap
            Guid accountId = Guid.NewGuid();

            // 11. cihaz (limit 10)
            string eleventhDeviceId = $"device_11_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            // Hesap erişimi için istek oluştur
            var accountRequest = new AccountAccessCheckRequest
            {
                AccountId = accountId,
                Username = $"user_{accountId.ToString().Substring(0, 8)}",
                AccessDate = DateTime.UtcNow,
                IpAddress = GenerateRandomIp(),
                CountryCode = "TR",
                City = "Istanbul",
                DeviceId = eleventhDeviceId,
                IsTrustedDevice = false, // Yeni cihaz
                UniqueIpCount24h = 3,
                UniqueCountryCount24h = 1,
                IsSuccessful = true,
                FailedLoginAttempts = 0,
                TypicalAccessHours = new List<int> { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                TypicalAccessDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                TypicalCountries = new List<string> { "TR" },
                AdditionalData = new Dictionary<string, object>
                {
                    { "DeviceCount", 10 }, // Zaten 10 cihaz kayıtlı
                    { "IsNewDevice", true },
                    {
                        "RegisteredDevices",
                        "device_1,device_2,device_3,device_4,device_5,device_6,device_7,device_8,device_9,device_10"
                    }
                }
            };

            // Hesap erişimini test et
            var response =
                await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-account-access", accountRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Maksimum 10 Farklı Cihaz");

                SaveTestResult(content, "Maksimum 10 Farklı Cihaz", "Device",
                    $"Hesap ({accountId.ToString().Substring(0, 8)}) zaten 10 cihaz kayıtlıyken 11. cihaz ({eleventhDeviceId}) eklemeye çalıştı");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Aynı IP'den Çok Sayıda Yeni Cihaz Ekleme kuralını test eder
        /// </summary>
        private static async Task TestMultipleDevicesFromSameIp()
        {
            Console.WriteLine("Testing: Aynı IP'den Çok Sayıda Yeni Cihaz Ekleme");

            // Test için ortak IP
            string sameIp = GenerateRandomIp();

            // 4 farklı hesap ve cihaz oluştur (limit 3)
            var accounts = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
            var devices = accounts.Select(a => $"device_{a.ToString().Substring(0, 8)}").ToList();

            // Son hesap/cihaz için istek oluştur
            var deviceRequest = new DeviceCheckRequest
            {
                DeviceId = devices.Last(),
                DeviceType = "Mobile",
                OperatingSystem = "iOS 15",
                Browser = "Safari Mobile",
                IpAddress = sameIp, // Aynı IP'den
                CountryCode = "TR",
                IsEmulator = false,
                IsJailbroken = false,
                IsRooted = false,
                FirstSeenDate = DateTime.UtcNow, // Yeni cihaz
                LastSeenDate = DateTime.UtcNow,
                UniqueAccountCount24h = 4, // 4 farklı hesap
                UniqueIpCount24h = 1, // Hep aynı IP
                AdditionalData = new Dictionary<string, object>
                {
                    { "NewDevicesFromIpCount", 3 }, // Aynı IP'den zaten 3 yeni cihaz eklenmiş
                    { "TimeWindowMinutes", 60 }, // Son 1 saat içinde
                    { "PreviousDevices", string.Join(",", devices.Take(devices.Count - 1)) },
                    {
                        "PreviousAccounts",
                        string.Join(",", accounts.Take(accounts.Count - 1).Select(a => a.ToString().Substring(0, 8)))
                    }
                }
            };

            // Cihazı test et
            var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-device", deviceRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Aynı IP'den Çok Sayıda Yeni Cihaz Ekleme");

                SaveTestResult(content, "Aynı IP'den Çok Sayıda Yeni Cihaz Ekleme", "Device",
                    $"Aynı IP'den ({sameIp}) 1 saat içinde 4 farklı yeni cihaz eklendi");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Sızdırılmış Hesap Bilgileri kuralını test eder
        /// </summary>
        private static async Task TestLeakedCredentials()
        {
            Console.WriteLine("Testing: Sızdırılmış Hesap Bilgileri");

            // Test için hesap
            Guid accountId = Guid.NewGuid();

            // Hesap erişimi için istek oluştur
            var accountRequest = new AccountAccessCheckRequest
            {
                AccountId = accountId,
                Username = $"leaked_user_{accountId.ToString().Substring(0, 8)}",
                AccessDate = DateTime.UtcNow,
                IpAddress = GenerateRandomIp(),
                CountryCode = "TR",
                City = "Istanbul",
                DeviceId = $"device_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                IsTrustedDevice = true,
                UniqueIpCount24h = 1,
                UniqueCountryCount24h = 1,
                IsSuccessful = true,
                FailedLoginAttempts = 0,
                TypicalAccessHours = new List<int> { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                TypicalAccessDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                TypicalCountries = new List<string> { "TR" },
                AdditionalData = new Dictionary<string, object>
                {
                    { "IsInBreachDatabase", true }, // Sızdırılmış veri tabanında bulundu
                    { "BreachDatabaseName", "HaveIBeenPwned" },
                    { "BreachDate", DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd") }
                }
            };

            // Hesap erişimini test et
            var response =
                await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-account-access", accountRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Sızdırılmış Hesap Bilgileri");

                SaveTestResult(content, "Sızdırılmış Hesap Bilgileri", "Account",
                    $"Hesap ({accountId.ToString().Substring(0, 8)}) sızıntı veritabanında bulundu");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Uzun Süreli Kesintisiz Oturum kuralını test eder
        /// </summary>
        private static async Task TestLongSession()
        {
            Console.WriteLine("Testing: Uzun Süreli Kesintisiz Oturum");

            // Test için oturum ve hesap
            Guid sessionId = Guid.NewGuid();
            Guid accountId = Guid.NewGuid();

            // Oturum başlangıç ve bitiş zamanları
            DateTime startTime = DateTime.UtcNow.AddMinutes(-40); // 40 dakika önce başlamış
            DateTime lastActivityTime = DateTime.UtcNow;

            // Oturum kontrolü için istek oluştur
            var sessionRequest = new SessionCheckRequest
            {
                SessionId = sessionId,
                AccountId = accountId,
                StartTime = startTime,
                LastActivityTime = lastActivityTime,
                DurationMinutes = (int)(lastActivityTime - startTime).TotalMinutes, // 40 dakika
                IpAddress = GenerateRandomIp(),
                DeviceId = $"device_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
                RapidNavigationCount = 0,
                AdditionalData = new Dictionary<string, object>
                {
                    { "IdleTimeMinutes", 5 }, // 5 dakikadır boşta
                    { "LastUrl", "/account/summary" },
                    { "PageViews", 15 }
                }
            };

            // Oturumu test et
            var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-session", sessionRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Uzun Süreli Kesintisiz Oturum");

                SaveTestResult(content, "Uzun Süreli Kesintisiz Oturum", "Session",
                    $"Oturum ({sessionId.ToString().Substring(0, 8)}) 40 dakikadır kesintisiz açık kaldı");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Hızlı Çoklu Para Transferi kuralını test eder
        /// </summary>
        private static async Task TestMultipleRecipients()
        {
            Console.WriteLine("Testing: Hızlı Çoklu Para Transferi");

            // Test için işlem ve hesap
            Guid transactionId = Guid.NewGuid();
            Guid accountId = Guid.NewGuid();

            // 5 farklı alıcı oluştur
            var recipients = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();

            // İşlem kontrolü için istek oluştur
            var transactionRequest = new TransactionCheckRequest
            {
                TransactionId = transactionId,
                AccountId = accountId,
                Amount = 1000.00m,
                Currency = "TRY",
                TransactionType = TransactionType.Transfer,
                TransactionDate = DateTime.UtcNow,
                RecipientAccountId = recipients.Last(), // Son alıcı
                RecipientAccountNumber = "TR123456789012345678901234",
                RecipientCountry = "TR",
                UserTransactionCount24h = 5, // 5 transfer
                UserTotalAmount24h = 5000.00m,
                UserAverageTransactionAmount = 1000.00m,
                DaysSinceFirstTransaction = 120,
                UniqueRecipientCount1h = 5, // 1 saat içinde 5 farklı alıcı
                AdditionalData = new Dictionary<string, object>
                {
                    {
                        "PreviousRecipients",
                        string.Join(",",
                            recipients.Take(recipients.Count - 1).Select(r => r.ToString().Substring(0, 8)))
                    },
                    { "TimeWindowMinutes", 10 }, // 10 dakika içinde
                    { "TransferCount10m", 5 } // 10 dakika içinde 5 transfer
                }
            };

            // İşlemi test et
            var response =
                await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-transaction", transactionRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Hızlı Çoklu Para Transferi");

                SaveTestResult(content, "Hızlı Çoklu Para Transferi", "Transaction",
                    $"Hesap ({accountId.ToString().Substring(0, 8)}) 10 dakika içinde 5 farklı alıcıya para transferi yaptı");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Yüksek Tutarlı İşlem kuralını test eder
        /// </summary>
        private static async Task TestHighAmountTransaction()
        {
            Console.WriteLine("Testing: Yüksek Tutarlı İşlem");

            // Test için işlem ve hesap
            Guid transactionId = Guid.NewGuid();
            Guid accountId = Guid.NewGuid();

            // Yüksek tutarlı işlem (10.000 TL üzeri)
            decimal amount = 15000.00m;

            // İşlem kontrolü için istek oluştur
            var transactionRequest = new TransactionCheckRequest
            {
                TransactionId = transactionId,
                AccountId = accountId,
                Amount = amount,
                Currency = "TRY",
                TransactionType = TransactionType.Transfer,
                TransactionDate = DateTime.UtcNow,
                RecipientAccountId = Guid.NewGuid(),
                RecipientAccountNumber = "TR123456789012345678901234",
                RecipientCountry = "TR",
                UserTransactionCount24h = 3,
                UserTotalAmount24h = amount,
                UserAverageTransactionAmount = 2000.00m,
                DaysSinceFirstTransaction = 180,
                UniqueRecipientCount1h = 1,
                AdditionalData = new Dictionary<string, object>
                {
                    { "IsHighValue", true },
                    { "NormalThreshold", 10000.00 } // Normal eşik değeri
                }
            };

            // İşlemi test et
            var response =
                await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-transaction", transactionRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Yüksek Tutarlı İşlem");

                SaveTestResult(content, "Yüksek Tutarlı İşlem", "Transaction",
                    $"Hesap ({accountId.ToString().Substring(0, 8)}) 10.000 TL üzerinde bir işlem gerçekleştirdi ({amount:C2})");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Ortalama Üzeri İşlem kuralını test eder
        /// </summary>
        private static async Task TestHighAverageTransaction()
        {
            Console.WriteLine("Testing: Ortalama Üzeri İşlem");

            // Test için işlem ve hesap
            Guid transactionId = Guid.NewGuid();
            Guid accountId = Guid.NewGuid();

            // Kullanıcı ortalaması ve işlem tutarı (5+ kat)
            decimal avgAmount = 1000.00m;
            decimal transactionAmount = avgAmount * 6; // 6 kat

            // İşlem kontrolü için istek oluştur
            var transactionRequest = new TransactionCheckRequest
            {
                TransactionId = transactionId,
                AccountId = accountId,
                Amount = transactionAmount,
                Currency = "TRY",
                TransactionType = TransactionType.Transfer,
                TransactionDate = DateTime.UtcNow,
                RecipientAccountId = Guid.NewGuid(),
                RecipientAccountNumber = "TR123456789012345678901234",
                RecipientCountry = "TR",
                UserTransactionCount24h = 1,
                UserTotalAmount24h = transactionAmount,
                UserAverageTransactionAmount = avgAmount,
                DaysSinceFirstTransaction = 90,
                UniqueRecipientCount1h = 1,
                AdditionalData = new Dictionary<string, object>
                {
                    { "IsAboveAverage", true },
                    { "AverageMultiple", 6.0 } // Ortalamanın 6 katı
                }
            };

            // İşlemi test et
            var response =
                await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-transaction", transactionRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Ortalama Üzeri İşlem");

                SaveTestResult(content, "Ortalama Üzeri İşlem", "Transaction",
                    $"Hesap ({accountId.ToString().Substring(0, 8)}) ortalama işlem tutarının ({avgAmount:C2}) 6 katı bir işlem gerçekleştirdi ({transactionAmount:C2})");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Kısa Sürede Çok Sayıda İşlem kuralını test eder
        /// </summary>
        private static async Task TestMultipleTransactions()
        {
            Console.WriteLine("Testing: Kısa Sürede Çok Sayıda İşlem");

            // Test için işlem ve hesap
            Guid transactionId = Guid.NewGuid();
            Guid accountId = Guid.NewGuid();

            // Kısa sürede çok sayıda işlem (1 saat içinde 10+ işlem)
            int transactionCount = 11;

            // İşlem kontrolü için istek oluştur
            var transactionRequest = new TransactionCheckRequest
            {
                TransactionId = transactionId,
                AccountId = accountId,
                Amount = 500.00m,
                Currency = "TRY",
                TransactionType = TransactionType.Transfer,
                TransactionDate = DateTime.UtcNow,
                RecipientAccountId = Guid.NewGuid(),
                RecipientAccountNumber = "TR123456789012345678901234",
                RecipientCountry = "TR",
                UserTransactionCount24h = 15,
                UserTotalAmount24h = 5500.00m,
                UserAverageTransactionAmount = 500.00m,
                DaysSinceFirstTransaction = 60,
                UniqueRecipientCount1h = 3,
                AdditionalData = new Dictionary<string, object>
                {
                    { "TransactionCount1h", transactionCount }, // 1 saat içinde 11 işlem
                    { "TimeWindowMinutes", 60 }, // 1 saat içinde
                    { "MaxTransactionCount", 10 } // Limit 10
                }
            };

            // İşlemi test et
            var response =
                await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-transaction", transactionRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Kısa Sürede Çok Sayıda İşlem");

                SaveTestResult(content, "Kısa Sürede Çok Sayıda İşlem", "Transaction",
                    $"Hesap ({accountId.ToString().Substring(0, 8)}) 1 saat içinde {transactionCount} işlem gerçekleştirdi (limit: 10)");

                Console.WriteLine($"Result: Rule triggered: {isTriggered}");
            }
        }

        /// <summary>
        /// Gece Saatlerinde Yüksek Tutarlı İşlem kuralını test eder
        /// </summary>
        public static async Task TestNightHighAmountTransaction()
        {
            Console.WriteLine("Testing: Gece Saatlerinde Yüksek Tutarlı İşlem");

            // 1. Önce bir TransactionRequest oluştur (FraudDetection/analyze için)
            var transactionRequest = new TransactionRequest
            {
                UserId = Guid.NewGuid(), // UserId'yi string olarak gönder
                Amount = 7500.00m, // 5000 TL üzeri
                MerchantId = "MERCHANT_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Type = (ApiTester.ContextTests.TransactionType)TransactionType.Transfer,

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
                    IpAddress = "192.168.1.1",
                    UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X)",
                    AdditionalInfo = new Dictionary<string, string>
                    {
                        { "OS", "iOS 14.7.1" },
                        { "Model", "iPhone 12" }
                    }
                },

                // Kritik: Gece saati simülasyonu
                AdditionalDataRequest = new TransactionAdditionalDataRequest
                {
                    // İşlem saati olarak gece 3:00'ı ayarla
                    CustomValues = new Dictionary<string, string>
                    {
                        { "Time", "10800" }, // Gece 3:00 (saniye cinsinden)
                        { "Hour", "3" }, // Saat bilgisi
                        { "IsNightHours", "true" }
                    }
                }
            };

            // 2. Önce işlemi analiz et
            Console.WriteLine("Step 1: Analyzing transaction...");
            var analyzeResponse = await _httpClient.PostAsJsonAsync("/api/FraudDetection/analyze", transactionRequest);

            if (!analyzeResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error analyzing transaction: {analyzeResponse.StatusCode}");
                return;
            }

            string analyzeContent = await analyzeResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Analysis completed. Response status: {analyzeResponse.StatusCode}");

            // 3. TransactionId'yi çıkart
            Guid transactionId = Guid.Empty;
            try
            {
                using var document = JsonDocument.Parse(analyzeContent);
                var root = document.RootElement;

                if (root.TryGetProperty("transactionId", out var idElement) &&
                    idElement.ValueKind == JsonValueKind.String)
                {
                    string transactionIdStr = idElement.GetString();
                    if (Guid.TryParse(transactionIdStr, out Guid parsedId))
                    {
                        transactionId = parsedId;
                    }
                }

                if (transactionId == Guid.Empty)
                {
                    Console.WriteLine("Could not extract transaction ID from response");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing response: {ex.Message}");
                return;
            }

            // 4. Şimdi rule test işlemini gerçekleştir
            Console.WriteLine($"Step 2: Testing rule with transaction ID: {transactionId}");

            // Kural kontrolü için endpoint'i çağır
            var ruleResponse = await _httpClient.GetAsync($"/api/FraudDetection/transaction/{transactionId}/rules");

            if (ruleResponse.IsSuccessStatusCode)
            {
                var content = await ruleResponse.Content.ReadAsStringAsync();
                bool isTriggered = IsRuleTriggered(content, "Gece Saatlerinde Yüksek Tutarlı İşlem");

                SaveTestResult(content, "Gece Saatlerinde Yüksek Tutarlı İşlem", "Complex",
                    $"Gece saatlerinde (3:00) yüksek tutarlı işlem (7,500.00 TL) gerçekleştirildi");

                Console.WriteLine($"Rule test result: {(isTriggered ? "TRIGGERED" : "NOT TRIGGERED")}");
            }
            else
            {
                Console.WriteLine($"Error testing rule: {ruleResponse.StatusCode}");
            }
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
        /// API yanıtından bir kuralın tetiklenip tetiklenmediğini kontrol eder
        /// </summary>
        private static bool IsRuleTriggered(string jsonContent, string ruleNameFragment)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                // 1. TriggeredRules[] kontrolü
                if (root.TryGetProperty("triggeredRules", out var rulesElement) &&
                    rulesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var rule in rulesElement.EnumerateArray())
                    {
                        if (rule.TryGetProperty("ruleName", out var nameElement) &&
                            nameElement.ValueKind == JsonValueKind.String)
                        {
                            string ruleName = nameElement.GetString();
                            if (ruleName != null &&
                                ruleName.Contains(ruleNameFragment, StringComparison.OrdinalIgnoreCase))
                                return true;
                        }
                    }
                }

                // 2. ResultMessage kontrolü (yedek kontrol)
                if (root.TryGetProperty("resultMessage", out var msgElement) &&
                    msgElement.ValueKind == JsonValueKind.String)
                {
                    string msg = msgElement.GetString();
                    if (msg != null && msg.Contains(ruleNameFragment, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // 3. RiskScore > 0 ve RequiresAction = true
                bool hasRiskScore = root.TryGetProperty("riskScore", out var scoreElement) &&
                                    scoreElement.ValueKind == JsonValueKind.Number &&
                                    scoreElement.GetInt32() > 0;

                bool requiresAction = root.TryGetProperty("requiresAction", out var actionElement) &&
                                      actionElement.ValueKind == JsonValueKind.True;

                if (hasRiskScore && requiresAction)
                    return true; // Bir kural tetiklenmiş olabilir
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Test sonucunu kaydeder
        /// </summary>
        private static void SaveTestResult(string jsonContent, string ruleName, string ruleCategory,
            string testDescription)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                var testResult = new RuleTestResult
                {
                    RuleName = ruleName,
                    RuleCategory = ruleCategory,
                    TestDescription = testDescription,
                    IsRuleTriggered = IsRuleTriggered(jsonContent, ruleName),
                    IsSuccess = true
                };

                // Risk Score
                if (root.TryGetProperty("riskScore", out var scoreElement) &&
                    scoreElement.ValueKind == JsonValueKind.Number)
                {
                    testResult.RiskScore = scoreElement.GetInt32();
                }

                // Result Type
                if (root.TryGetProperty("resultType", out var typeElement) &&
                    typeElement.ValueKind == JsonValueKind.String)
                {
                    testResult.ResultType = typeElement.GetString();
                }

                // Applied Actions
                if (root.TryGetProperty("actions", out var actionsElement) &&
                    actionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var action in actionsElement.EnumerateArray())
                    {
                        if (action.ValueKind == JsonValueKind.String)
                        {
                            testResult.AppliedActions.Add(action.GetString());
                        }
                    }
                }

                _testResults.Add(testResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test sonucu kaydedilirken hata: {ex.Message}");

                // Minimum bilgiyle kaydet
                _testResults.Add(new RuleTestResult
                {
                    RuleName = ruleName,
                    RuleCategory = ruleCategory,
                    TestDescription = testDescription,
                    IsRuleTriggered = false,
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Test sonuçlarını görüntüler
        /// </summary>
        private static void DisplayTestResults()
        {
            Console.WriteLine("\n=== Fraud Rule Test Results ===\n");
            Console.WriteLine($"Total Tests Run: {_testResults.Count}");
            Console.WriteLine($"Triggered Rules: {_testResults.Count(r => r.IsRuleTriggered)}");

            Console.WriteLine("\nDetailed Results:");
            Console.WriteLine(new string('-', 120));
            Console.WriteLine(string.Format("{0,-35} | {1,-12} | {2,-15} | {3,-10} | {4,-35}",
                "Rule Name", "Category", "Triggered", "Risk Score", "Applied Actions"));
            Console.WriteLine(new string('-', 120));

            foreach (var result in _testResults)
            {
                Console.WriteLine(string.Format("{0,-35} | {1,-12} | {2,-15} | {3,-10} | {4,-35}",
                    TruncateString(result.RuleName, 35),
                    result.RuleCategory,
                    result.IsRuleTriggered ? "YES" : "NO",
                    result.RiskScore,
                    TruncateString(string.Join(", ", result.AppliedActions), 35)));
            }

            Console.WriteLine(new string('-', 120));

            // Kategori bazında özet
            Console.WriteLine("\nCategory Summary:");
            var categorySummary = _testResults
                .GroupBy(r => r.RuleCategory)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Count(),
                    Triggered = g.Count(r => r.IsRuleTriggered)
                })
                .OrderBy(g => g.Category);

            foreach (var category in categorySummary)
            {
                Console.WriteLine(
                    $"{category.Category,-12}: {category.Triggered} triggered out of {category.Total} tests");
            }
        }

        /// <summary>
        /// Rastgele IP adresi oluşturur
        /// </summary>
        private static string GenerateRandomIp()
        {
            return $"{_random.Next(1, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(1, 255)}";
        }

        /// <summary>
        /// String'i belirli bir uzunlukta keser
        /// </summary>
        private static string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Length <= maxLength ? input : input.Substring(0, maxLength - 3) + "...";
        }

        #endregion

        #region Request DTO Classes

        // Gerekli DTO sınıflarını buraya ekleyin 
        // (Mevcut ContextualAnalysisTester'daki sınıfları kullanın)

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
}