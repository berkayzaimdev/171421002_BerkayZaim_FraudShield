using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace FraudDetection.ApiTester.TransactionTests
{
    /// <summary>
    /// İşlem analizi için özel test sınıfı
    /// </summary>
    public class TransactionAnalysisTester
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private static string _baseUrl = "http://localhost:5112"; // Default base URL

        // İşlem analizi sonuçlarını sakla (UI flow için)
        private static Guid _lastTransactionId = Guid.Empty;
        private static List<string> _completedSteps = new List<string>();


        /// <summary>
        /// UI akışı simüle eder - işlem analizi başlar ve adım adım ilerler
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Fraud Detection API Tester: Transaction Analysis ===");

            // Configure base URL
            Console.Write($"Enter API base URL (press Enter for default: {_baseUrl}): ");
            string baseUrlInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(baseUrlInput))
            {
                // Validate URL format
                if (Uri.TryCreate(baseUrlInput, UriKind.Absolute, out Uri uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    _baseUrl = baseUrlInput.TrimEnd('/');
                }
                else
                {
                    Console.WriteLine($"Invalid URL format. Using default URL: {_baseUrl}");
                }
            }

            try
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                Console.WriteLine($"Using API base URL: {_baseUrl}");
            }
            catch (UriFormatException)
            {
                Console.WriteLine($"Invalid URI format. Using default URL: {_baseUrl}");
                _baseUrl = "http://localhost:5112";
                _httpClient.BaseAddress = new Uri(_baseUrl);
            }

            Console.WriteLine("\n=== UI Flow Simulation: Transaction Analysis Process ===\n");

            // Adım 1: İşlemi analiz et
            Console.WriteLine("Step 1: Transaction Analysis");
            await AnalyzeTransaction();
            _completedSteps.Add("Transaction Analysis");
            PauseForReview();

            // Adım 2: Step-by-step kontrollerini çalıştır (işlem ID varsa)
            if (_lastTransactionId != Guid.Empty)
            {
                Console.WriteLine("\nStep 2: Step-by-Step Fraud Checks");
                await PerformStepByStepCheck(_lastTransactionId);
                _completedSteps.Add("Step-by-Step Checks");
                PauseForReview();

                // Adım 3: Kapsamlı kontrol
                Console.WriteLine("\nStep 3: Comprehensive Fraud Check");
                await PerformComprehensiveCheck(_lastTransactionId);
                _completedSteps.Add("Comprehensive Check");
            }

            // Özet
            Console.WriteLine("\n=== UI Flow Simulation Completed ===");
            Console.WriteLine($"Transaction ID: {_lastTransactionId}");
            Console.WriteLine("Completed Steps:");
            foreach (var step in _completedSteps)
            {
                Console.WriteLine($"- {step}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// TransactionRequest ile işlem analizi başlatır
        /// </summary>
        public static async Task AnalyzeTransaction()
        {
            try
            {
                var request = CreateSampleTransactionRequest();

                Console.WriteLine("Sending transaction analysis request...");
                DisplayRequestData("Transaction Analysis Request", request);

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
                            _lastTransactionId = transactionId;
                            Console.WriteLine($"Transaction analyzed successfully. ID: {_lastTransactionId}");
                        }
                    }
                }

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing transaction: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Step-by-step kontrolleri çalıştırır
        /// </summary>
        public static async Task PerformStepByStepCheck(Guid transactionId)
        {
            try
            {
                Console.WriteLine($"Performing step-by-step checks for transaction {transactionId}...");

                var response =
                    await _httpClient.PostAsync($"/api/TransactionAnalysis/{transactionId}/step-by-step-check", null);

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing step-by-step check: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Kapsamlı kontrol çalıştırır
        /// </summary>
        public static async Task PerformComprehensiveCheck(Guid transactionId)
        {
            try
            {
                Console.WriteLine($"Performing comprehensive check for transaction {transactionId}...");

                var response =
                    await _httpClient.GetAsync($"/api/TransactionAnalysis/{transactionId}/comprehensive-check");

                await DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing comprehensive check: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        #region Helper Methods

        /// <summary>
        /// Örnek işlem isteği oluşturur
        /// </summary>
        private static TransactionRequest CreateSampleTransactionRequest()
        {
            return new TransactionRequest
            {
                UserId = Guid.NewGuid(),
                Amount = 2500.00m,
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
                    IpAddress = "192.168.1.1",
                    UserAgent =
                        "Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.2 Mobile/15E148 Safari/604.1",
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

        private static void PauseForReview()
        {
            Console.WriteLine("\nPress any key to continue to the next step...");
            Console.ReadKey();
            Console.WriteLine();
        }

        private static void DisplayRequestData<T>(string title, T data)
        {
            Console.WriteLine($"\n{title}:");
            Console.WriteLine(JsonSerializer.Serialize(data, _jsonOptions));
            Console.WriteLine();
        }

        private static async Task DisplayResponse(HttpResponseMessage response)
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