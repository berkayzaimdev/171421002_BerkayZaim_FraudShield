using System.Net.Http.Json;
using System.Text.Json;

namespace FraudDetection.ApiTester.ScenarioTests
{
    /// <summary>
    /// Extends the API tester with pre-defined fraud detection scenarios
    /// </summary>
    public class FraudScenarioTester
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public FraudScenarioTester(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        /// <summary>
        /// Run all predefined scenarios
        /// </summary>
        public async Task RunAllScenarios()
        {
            Console.WriteLine("\n=== Running All Fraud Detection Scenarios ===\n");

            await Scenario1_HighValueTransaction();
            PauseForReview();

            await Scenario2_MultipleCountryAccess();
            PauseForReview();

            await Scenario3_SuspiciousDeviceAndIP();
            PauseForReview();

            await Scenario4_AnomalousVValues();
            PauseForReview();

            await Scenario5_ComprehensiveHighRiskCheck();

            Console.WriteLine("\n=== Completed All Fraud Detection Scenarios ===");
        }

        #region Predefined Scenarios

        /// <summary>
        /// Scenario 1: High value transaction that triggers the high amount rule
        /// </summary>
        public async Task Scenario1_HighValueTransaction()
        {
            Console.WriteLine("\n=== Scenario 1: High Value Transaction ===");
            Console.WriteLine(
                "Testing a transaction with a high amount (15,000 TRY) that should trigger the high amount rule");

            var request = new TransactionCheckRequest
            {
                TransactionId = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Amount = 15000.00m, // High amount to trigger rule
                Currency = "TRY",
                TransactionType = TransactionType.Transfer,
                TransactionDate = DateTime.UtcNow,
                RecipientAccountId = Guid.NewGuid(),
                RecipientAccountNumber = "TR123456789012345678901234",
                RecipientCountry = "TR",
                UserTransactionCount24h = 2,
                UserTotalAmount24h = 2000.00m,
                UserAverageTransactionAmount = 1200.00m,
                DaysSinceFirstTransaction = 120,
                UniqueRecipientCount1h = 1,
                AdditionalData = new Dictionary<string, object>
                {
                    { "IsNewPaymentMethod", false },
                    { "IsInternational", false }
                }
            };

            try
            {
                Console.WriteLine("Sending high value transaction request...");
                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-transaction", request);
                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Scenario 1: {ex.Message}");
            }
        }

        /// <summary>
        /// Scenario 2: Multiple country access that triggers the suspicious account activity rule
        /// </summary>
        public async Task Scenario2_MultipleCountryAccess()
        {
            Console.WriteLine("\n=== Scenario 2: Multiple Country Access ===");
            Console.WriteLine(
                "Testing an account access from multiple countries that should trigger the suspicious account activity rule");

            var request = new AccountAccessCheckRequest
            {
                AccountId = Guid.NewGuid(),
                Username = "test.user",
                AccessDate = DateTime.UtcNow,
                IpAddress = "185.220.101.134", // Suspicious IP
                CountryCode = "RU", // Different from typical countries
                City = "Moscow",
                DeviceId = "device_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                IsTrustedDevice = false,
                UniqueIpCount24h = 5, // High number of IPs
                UniqueCountryCount24h = 4, // Multiple countries (above threshold)
                IsSuccessful = true,
                FailedLoginAttempts = 0,
                TypicalAccessHours = new List<int> { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                TypicalAccessDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                TypicalCountries = new List<string> { "TR" }, // Typical country is different
                AdditionalData = new Dictionary<string, object>
                {
                    { "Browser", "Chrome" },
                    { "OperatingSystem", "Windows" }
                }
            };

            try
            {
                Console.WriteLine("Sending multiple country access request...");
                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-account-access", request);
                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Scenario 2: {ex.Message}");
            }
        }

        /// <summary>
        /// Scenario 3: Suspicious device and IP characteristics
        /// </summary>
        public async Task Scenario3_SuspiciousDeviceAndIP()
        {
            Console.WriteLine("\n=== Scenario 3: Suspicious Device and IP ===");
            Console.WriteLine("Testing a suspicious device with emulator detection and high-risk IP characteristics");

            var request = new DeviceCheckRequest
            {
                DeviceId = "emulator_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                DeviceType = "Mobile",
                OperatingSystem = "Android 12",
                Browser = "Chrome Mobile",
                IpAddress = "185.220.100.240", // Tor exit node IP
                CountryCode = "NL",
                IsEmulator = true, // Emulator flag
                IsJailbroken = true, // Jailbroken flag
                IsRooted = true, // Rooted flag
                FirstSeenDate = DateTime.UtcNow.AddMinutes(-30), // Recently seen device
                LastSeenDate = DateTime.UtcNow,
                UniqueAccountCount24h = 8, // High number of accounts
                UniqueIpCount24h = 5, // Multiple IPs
                AdditionalData = new Dictionary<string, object>
                {
                    { "DeviceModel", "Generic Android Device" },
                    { "AppVersion", "1.0.5" }
                }
            };

            try
            {
                Console.WriteLine("Sending suspicious device check request...");
                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/check-device", request);
                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Scenario 3: {ex.Message}");
            }
        }

        /// <summary>
        /// Scenario 4: ML model with anomalous V values (based on known fraud patterns)
        /// </summary>
        public async Task Scenario4_AnomalousVValues()
        {
            Console.WriteLine("\n=== Scenario 4: Anomalous V Values ===");
            Console.WriteLine("Testing ML evaluation with V values that match known fraud patterns");

            var request = new ModelEvaluationRequest
            {
                TransactionId = Guid.NewGuid(),
                Amount = 3500.00m,
                TransactionDate = DateTime.UtcNow,
                TransactionType = TransactionType.Transfer,
                Features = new Dictionary<string, string>
                {
                    // Fraud pattern values based on critical V features
                    { "V1", "-4.7700" },
                    { "V2", "3.6300" },
                    { "V3", "-7.0400" },
                    { "V4", "-2.4600" },
                    { "V5", "-3.1600" },
                    { "V6", "-2.3561" },
                    { "V7", "1.0487" },
                    { "V8", "-0.8795" },
                    { "V9", "-2.7700" },
                    { "V10", "-5.5700" },
                    { "V11", "3.3200" },
                    { "V12", "-2.7000" },
                    { "V13", "-1.5632" },
                    { "V14", "-8.7500" }, // Most significant fraud indicator
                    { "V15", "0.4521" },
                    { "V16", "-1.7100" },
                    { "V17", "-4.6500" },
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
                    { "Time", "43200" } // 12 hours in seconds
                },
                AdditionalData = new Dictionary<string, object>
                {
                    { "DaysSinceFirstTransaction", 5 }, // New account
                    { "TransactionVelocity24h", 12 }, // High transaction velocity
                    { "AverageTransactionAmount", 500.00 } // Much higher than average
                }
            };

            try
            {
                Console.WriteLine("Sending ML model evaluation request with anomalous V values...");
                var response = await _httpClient.PostAsJsonAsync("/api/FraudDetection/evaluate-model", request);
                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Scenario 4: {ex.Message}");
            }
        }

        /// <summary>
        /// Scenario 5: Comprehensive high-risk check with multiple risk factors
        /// </summary>
        public async Task Scenario5_ComprehensiveHighRiskCheck()
        {
            Console.WriteLine("\n=== Scenario 5: Comprehensive High-Risk Check ===");
            Console.WriteLine("Testing a comprehensive check with multiple high-risk factors across all contexts");

            // Shared IDs for consistency across requests
            var accountId = Guid.NewGuid();
            var deviceId = "suspicious_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var ipAddress = "185.220.101.33"; // Suspicious IP
            var transactionId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();

            // Transaction with high amount, international transfer
            var transaction = new TransactionCheckRequest
            {
                TransactionId = transactionId,
                AccountId = accountId,
                Amount = 12000.00m,
                Currency = "USD", // International currency
                TransactionType = TransactionType.Transfer,
                TransactionDate = DateTime.UtcNow,
                RecipientAccountId = Guid.NewGuid(),
                RecipientAccountNumber = "UA123456789012345678901234",
                RecipientCountry = "UA", // International transfer
                UserTransactionCount24h = 8, // High number of transactions
                UserTotalAmount24h = 25000.00m, // High total amount
                UserAverageTransactionAmount = 2000.00m,
                DaysSinceFirstTransaction = 10, // Relatively new account
                UniqueRecipientCount1h = 4, // Multiple recipients in short time
                AdditionalData = new Dictionary<string, object>
                {
                    { "IsNewPaymentMethod", true }, // New payment method
                    { "IsInternational", true } // International flag
                }
            };

            // Account access from unusual location
            var account = new AccountAccessCheckRequest
            {
                AccountId = accountId,
                Username = "test.user",
                AccessDate = DateTime.UtcNow,
                IpAddress = ipAddress,
                CountryCode = "RU", // High risk country
                City = "Moscow",
                DeviceId = deviceId,
                IsTrustedDevice = false, // Not trusted
                UniqueIpCount24h = 6, // Multiple IPs
                UniqueCountryCount24h = 3, // Multiple countries
                IsSuccessful = true,
                FailedLoginAttempts = 2, // Some failed attempts
                TypicalAccessHours = new List<int> { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                TypicalAccessDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                TypicalCountries = new List<string> { "TR" }, // Different from current
                AdditionalData = new Dictionary<string, object>
                {
                    { "Browser", "Firefox" },
                    { "OperatingSystem", "Linux" } // Unusual OS
                }
            };

            // Suspicious IP
            var ip = new IpCheckRequest
            {
                IpAddress = ipAddress,
                CountryCode = "RU",
                City = "Moscow",
                IspAsn = "AS0000 SUSPICIOUS-NETWORK",
                ReputationScore = 30, // Low reputation
                IsBlacklisted = false, // Not blacklisted yet
                BlacklistNotes = "",
                IsDatacenterOrProxy = true, // Proxy flag
                NetworkType = "TOR", // TOR network
                UniqueAccountCount10m = 3,
                UniqueAccountCount1h = 12, // Many accounts
                UniqueAccountCount24h = 25, // Very many accounts
                FailedLoginCount10m = 5, // Failed logins
                AdditionalData = new Dictionary<string, object>
                {
                    { "LastSeenDate", DateTime.UtcNow.AddDays(-1) }
                }
            };

            // Suspicious device
            var device = new DeviceCheckRequest
            {
                DeviceId = deviceId,
                DeviceType = "Mobile",
                OperatingSystem = "Android 10",
                Browser = "Chrome Mobile",
                IpAddress = ipAddress,
                CountryCode = "RU",
                IsEmulator = true, // Emulator
                IsJailbroken = true, // Jailbroken
                IsRooted = true, // Rooted
                FirstSeenDate = DateTime.UtcNow.AddHours(-2), // New device
                LastSeenDate = DateTime.UtcNow,
                UniqueAccountCount24h = 7, // Multiple accounts
                UniqueIpCount24h = 8, // Multiple IPs
                AdditionalData = new Dictionary<string, object>
                {
                    { "DeviceModel", "Generic Android Device" },
                    { "AppVersion", "1.0.5" }
                }
            };

            // Suspicious session
            var session = new SessionCheckRequest
            {
                SessionId = sessionId,
                AccountId = accountId,
                StartTime = DateTime.UtcNow.AddHours(-3),
                LastActivityTime = DateTime.UtcNow,
                DurationMinutes = 180, // Long session
                IpAddress = ipAddress,
                DeviceId = deviceId,
                UserAgent =
                    "Mozilla/5.0 (Linux; Android 10; Generic) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Mobile Safari/537.36",
                RapidNavigationCount = 24, // Very rapid navigation
                AdditionalData = new Dictionary<string, object>
                {
                    { "PageViews", 120 }, // Unusually high
                    { "LastUrl", "/account/transfer" }
                }
            };

            // Anomalous ML features
            var model = new ModelEvaluationRequest
            {
                TransactionId = transactionId,
                Amount = 12000.00m,
                TransactionDate = DateTime.UtcNow,
                TransactionType = TransactionType.Transfer,
                Features = new Dictionary<string, string>
                {
                    // Fraud pattern values based on critical V features
                    { "V1", "-4.7700" },
                    { "V2", "3.6300" },
                    { "V3", "-7.0400" },
                    { "V4", "-2.4600" },
                    { "V5", "-3.1600" },
                    { "V9", "-2.7700" },
                    { "V10", "-5.5700" },
                    { "V11", "3.3200" },
                    { "V12", "-2.7000" },
                    { "V14", "-8.7500" }, // Most significant fraud indicator
                    { "V16", "-1.7100" },
                    { "V17", "-4.6500" },
                    { "Time", "3600" } // 1 AM in seconds
                },
                AdditionalData = new Dictionary<string, object>
                {
                    { "DaysSinceFirstTransaction", 10 },
                    { "TransactionVelocity24h", 8 },
                    { "AverageTransactionAmount", 2000.00 }
                }
            };

            // Create comprehensive check request
            var comprehensiveRequest = new ComprehensiveFraudCheckRequest
            {
                Transaction = transaction,
                Account = account,
                IpAddress = ip,
                Device = device,
                Session = session,
                ModelEvaluation = model
            };

            try
            {
                Console.WriteLine("Sending comprehensive high-risk check request...");
                var response =
                    await _httpClient.PostAsJsonAsync("/api/FraudDetection/comprehensive-check", comprehensiveRequest);
                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Scenario 5: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private void PauseForReview()
        {
            Console.WriteLine("\nPress any key to continue to the next scenario...");
            Console.ReadKey();
            Console.WriteLine();
        }

        private async Task DisplayResponse(HttpResponseMessage response)
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
    }
}