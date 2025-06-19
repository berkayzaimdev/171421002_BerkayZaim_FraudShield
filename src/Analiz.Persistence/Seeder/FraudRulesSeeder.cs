using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Persistence.Seeder;

/// <summary>
/// Örnek fraud kuralları için seed data
/// </summary>
public static class FraudRulesSeeder
{
    public static List<FraudRule> GetSeedRules()
    {
        var rules = new List<FraudRule>();
        var adminUser = "System.SeedData";

        #region Ağ Bazlı Kurallar

        // Tor Ağı Üzerinden Bağlantı
        var torRule = FraudRule.Create(
            "Tor Ağı Üzerinden Bağlantı",
            "Tor ağı üzerinden yapılan bağlantıları engeller.",
            RuleCategory.Network,
            RuleType.Simple,
            ImpactLevel.Critical,
            new List<RuleAction> { RuleAction.BlockIP },
            null, // Süresiz
            10, // Yüksek öncelik
            "", // Koşul yok (config'den okunacak)
            @"{
                    ""checkTorNetwork"": true,
                    ""networkType"": ""TOR""
                }",
            adminUser
        );
        torRule.Activate(adminUser);
        rules.Add(torRule);

        // Kara Liste IP'ler
        var blacklistIpRule = FraudRule.Create(
            "Kara Liste IP'ler",
            "Kara listedeki IP adreslerinden yapılan erişimleri engeller.",
            RuleCategory.IP,
            RuleType.Blacklist,
            ImpactLevel.Critical,
            new List<RuleAction> { RuleAction.BlacklistIP },
            null, // Süresiz
            10, // Yüksek öncelik
            "", // Koşul yok (config'den okunacak)
            @"{
                    ""blacklistType"": ""IP"",
                    ""blockedIps"": [""123.456.789.0"", ""111.222.333.444""],
                    ""blockedIpRanges"": [""192.168.0.0/24"", ""10.0.0.0/8""]
                }",
            adminUser
        );
        blacklistIpRule.Activate(adminUser);
        rules.Add(blacklistIpRule);

        #endregion

        #region IP Bazlı Çoklu Hesap Erişimi Kuralları

        // 3 farklı hesap / 10 dakika
        var ip3AccountsRule = FraudRule.Create(
            "IP Bazlı 3 Farklı Hesap Erişimi (10 dakika)",
            "Bir IP'den 10 dakika içinde 3 farklı hesaba erişim denemesi tespit edildiğinde yeni hesaplara girişi engeller.",
            RuleCategory.IP,
            RuleType.Threshold,
            ImpactLevel.Medium,
            new List<RuleAction> { RuleAction.RequireAdditionalVerification },
            TimeSpan.FromHours(12),
            20,
            "",
            @"{
                    ""maxDifferentAccounts"": 3,
                    ""timeWindowMinutes"": 10
                }",
            adminUser
        );
        ip3AccountsRule.Activate(adminUser);
        rules.Add(ip3AccountsRule);

        // 5 farklı hesap / 1 saat
        var ip5AccountsRule = FraudRule.Create(
            "IP Bazlı 5 Farklı Hesap Erişimi (1 saat)",
            "Bir IP'den 1 saat içinde 5 farklı hesaba erişim denemesi tespit edildiğinde IP'yi 12 saat boyunca engeller.",
            RuleCategory.IP,
            RuleType.Threshold,
            ImpactLevel.High,
            new List<RuleAction> { RuleAction.BlockIP },
            TimeSpan.FromHours(12),
            15,
            "",
            @"{
                    ""maxDifferentAccounts"": 5,
                    ""timeWindowMinutes"": 60
                }",
            adminUser
        );
        ip5AccountsRule.Activate(adminUser);
        rules.Add(ip5AccountsRule);

        // 10 farklı hesap / 48 saat
        var ip10AccountsRule = FraudRule.Create(
            "IP Bazlı 10 Farklı Hesap Erişimi (48 saat)",
            "Bir IP'den 48 saat içinde 10 farklı hesaba erişim denemesi tespit edildiğinde IP'yi kalıcı olarak kara listeye alır.",
            RuleCategory.IP,
            RuleType.Threshold,
            ImpactLevel.Critical,
            new List<RuleAction> { RuleAction.BlacklistIP },
            null, // Süresiz
            10,
            "",
            @"{
                    ""maxDifferentAccounts"": 10,
                    ""timeWindowMinutes"": 2880
                }",
            adminUser
        );
        ip10AccountsRule.Activate(adminUser);
        rules.Add(ip10AccountsRule);

        // 25 farklı hesap / 1 ay
        var ip25AccountsRule = FraudRule.Create(
            "IP Bazlı 25 Farklı Hesap Erişimi (1 ay)",
            "Bir IP'den 1 ay içinde 25 farklı hesaba erişim denemesi tespit edildiğinde IP'yi kalıcı olarak kara listeye alır.",
            RuleCategory.IP,
            RuleType.Threshold,
            ImpactLevel.Critical,
            new List<RuleAction> { RuleAction.BlacklistIP },
            null, // Süresiz
            10,
            "",
            @"{
                    ""maxDifferentAccounts"": 25,
                    ""timeWindowMinutes"": 43200
                }",
            adminUser
        );
        ip25AccountsRule.Activate(adminUser);
        rules.Add(ip25AccountsRule);

        #endregion

        #region IP Bazlı Başarısız Giriş Denemeleri

        // 20 başarısız giriş / 10 dakika
        var failedLoginsRule = FraudRule.Create(
            "IP Bazlı 20 Başarısız Giriş (10 dakika)",
            "Bir IP'den 10 dakika içinde 20 başarısız giriş denemesi tespit edildiğinde IP'yi 48 saat boyunca engeller.",
            RuleCategory.IP,
            RuleType.Threshold,
            ImpactLevel.High,
            new List<RuleAction> { RuleAction.BlockIP },
            TimeSpan.FromHours(48),
            15,
            "",
            @"{
                    ""maxFailedLogins"": 20,
                    ""timeWindowMinutes"": 10
                }",
            adminUser
        );
        failedLoginsRule.Activate(adminUser);
        rules.Add(failedLoginsRule);

        #endregion

        #region Hesap Bazlı Şüpheli Erişim Desenleri

        // 5 farklı IP / 1 saat
        var account5IpsRule = FraudRule.Create(
            "Hesap Bazlı 5 Farklı IP Erişimi (1 saat)",
            "Bir hesaba 1 saat içinde 5 farklı IP'den erişim denemesi tespit edildiğinde 6. IP'yi engeller ve hesabı 24 saat boyunca kilitler.",
            RuleCategory.Account,
            RuleType.Threshold,
            ImpactLevel.High,
            new List<RuleAction> { RuleAction.LockAccount, RuleAction.BlockIP },
            TimeSpan.FromHours(24),
            15,
            "",
            @"{
                    ""maxDifferentIps"": 5,
                    ""timeWindowMinutes"": 60
                }",
            adminUser
        );
        account5IpsRule.Activate(adminUser);
        rules.Add(account5IpsRule);

        // 10 farklı IP / 24 saat
        var account10IpsRule = FraudRule.Create(
            "Hesap Bazlı 10 Farklı IP Erişimi (24 saat)",
            "Bir hesaba 24 saat içinde 10 farklı IP'den erişim denemesi tespit edildiğinde şüpheli aktivite bildirimi gönderir ve hesabı 48 saat boyunca kilitler.",
            RuleCategory.Account,
            RuleType.Threshold,
            ImpactLevel.High,
            new List<RuleAction> { RuleAction.LockAccount, RuleAction.Notify },
            TimeSpan.FromHours(48),
            15,
            "",
            @"{
                    ""maxDifferentIps"": 10,
                    ""timeWindowMinutes"": 1440
                }",
            adminUser
        );
        account10IpsRule.Activate(adminUser);
        rules.Add(account10IpsRule);

        // 4 farklı ülke / 24 saat
        var account4CountriesRule = FraudRule.Create(
            "Hesap Bazlı 4 Farklı Ülke Erişimi (24 saat)",
            "Bir hesaba 24 saat içinde 4 farklı ülkeden erişim denemesi tespit edildiğinde hesabı KYC doğrulama statüsüne çeker.",
            RuleCategory.Account,
            RuleType.Threshold,
            ImpactLevel.Critical,
            new List<RuleAction> { RuleAction.RequireKYCVerification },
            null, // Süresiz
            10,
            "",
            @"{
                    ""maxDifferentCountries"": 4,
                    ""timeWindowMinutes"": 1440
                }",
            adminUser
        );
        account4CountriesRule.Activate(adminUser);
        rules.Add(account4CountriesRule);

        #endregion

        #region Cihaz Yönetimi

        // Bilinmeyen cihazdan giriş
        var unknownDeviceRule = FraudRule.Create(
            "Bilinmeyen Cihazdan Giriş",
            "Bilinmeyen bir cihazdan giriş yapıldığında OTP ve e-posta onayı ister.",
            RuleCategory.Device,
            RuleType.Simple,
            ImpactLevel.Medium,
            new List<RuleAction> { RuleAction.RequireAdditionalVerification },
            null, // Süresiz
            30,
            "",
            @"{
                    ""checkTrustedDevice"": true
                }",
            adminUser
        );
        unknownDeviceRule.Activate(adminUser);
        rules.Add(unknownDeviceRule);

        // 10. farklı cihaz talepleri
        var max10DevicesRule = FraudRule.Create(
            "Maksimum 10 Farklı Cihaz",
            "Bir hesap için 10 farklı cihaz kaydedilmişse, yeni cihaz taleplerini mevcut cihazlardan biri silinene kadar engeller.",
            RuleCategory.Device,
            RuleType.Threshold,
            ImpactLevel.Medium,
            new List<RuleAction> { RuleAction.BlockDevice },
            null, // Süresiz
            30,
            "",
            @"{
                    ""maxDifferentDevices"": 10
                }",
            adminUser
        );
        max10DevicesRule.Activate(adminUser);
        rules.Add(max10DevicesRule);

        // Aynı IP'den çok sayıda yeni cihaz ekleme
        var multiDeviceSameIpRule = FraudRule.Create(
            "Aynı IP'den Çok Sayıda Yeni Cihaz Ekleme",
            "Aynı IP'den kısa sürede çok sayıda yeni cihaz eklendiğinde yeni cihaz eklemeyi engeller ve hesabı incelemeye alır.",
            RuleCategory.Device,
            RuleType.Threshold,
            ImpactLevel.High,
            new List<RuleAction> { RuleAction.BlockDevice, RuleAction.PutUnderReview },
            TimeSpan.FromHours(48),
            20,
            "",
            @"{
                    ""maxNewDevicesPerIp"": 3,
                    ""timeWindowMinutes"": 60
                }",
            adminUser
        );
        multiDeviceSameIpRule.Activate(adminUser);
        rules.Add(multiDeviceSameIpRule);

        #endregion

        #region Oturum ve Güvenlik Olayları

        // Hesap bilgileri sızıntı veritabanlarında tespit edildi
        var leakedCredentialsRule = FraudRule.Create(
            "Sızdırılmış Hesap Bilgileri",
            "Hesap bilgileri sızıntı veritabanlarında tespit edildiğinde tüm oturumları kapatır ve yeni girişlerde OTP zorunlu kılar.",
            RuleCategory.Account,
            RuleType.Simple,
            ImpactLevel.High,
            new List<RuleAction> { RuleAction.TerminateSession, RuleAction.RequireAdditionalVerification },
            null, // Süresiz
            15,
            "",
            @"{
                    ""checkBreachDatabase"": true
                }",
            adminUser
        );
        leakedCredentialsRule.Activate(adminUser);
        rules.Add(leakedCredentialsRule);

        // Tek cihazda kesintisiz oturum ≥ 30 dakika
        var longSessionRule = FraudRule.Create(
            "Uzun Süreli Kesintisiz Oturum",
            "Tek cihazda 30 dakika veya daha uzun süre kesintisiz oturum açık kaldığında tüm oturumları kapatır ve yeni girişlerde OTP zorunlu kılar.",
            RuleCategory.Session,
            RuleType.Threshold,
            ImpactLevel.Medium,
            new List<RuleAction> { RuleAction.TerminateSession, RuleAction.RequireAdditionalVerification },
            null, // Süresiz
            40,
            "",
            @"{
                    ""maxSessionDuration"": 30
                }",
            adminUser
        );
        longSessionRule.Activate(adminUser);
        rules.Add(longSessionRule);

        // 5 farklı alıcıya para transferi / 10 dakika
        var multipleTransfersRule = FraudRule.Create(
            "Hızlı Çoklu Para Transferi",
            "10 dakika içinde 5 farklı alıcıya para transferi yapıldığında tüm oturumları kapatır ve yeni girişlerde OTP zorunlu kılar.",
            RuleCategory.Transaction,
            RuleType.Threshold,
            ImpactLevel.High,
            new List<RuleAction> { RuleAction.TerminateSession, RuleAction.RequireAdditionalVerification },
            null, // Süresiz
            15,
            "",
            @"{
                    ""maxDifferentRecipients"": 5,
                    ""timeWindowMinutes"": 10
                }",
            adminUser
        );
        multipleTransfersRule.Activate(adminUser);
        rules.Add(multipleTransfersRule);

        #endregion

        #region İşlem Tabanlı Kurallar

        // Yüksek tutarlı işlem
        var highAmountRule = FraudRule.Create(
            "Yüksek Tutarlı İşlem",
            "10.000 TL üzerindeki işlemlerde ilave doğrulama ister.",
            RuleCategory.Transaction,
            RuleType.Threshold,
            ImpactLevel.Medium,
            new List<RuleAction> { RuleAction.RequireAdditionalVerification },
            null, // Süresiz
            30,
            "",
            @"{
                    ""maxAmount"": 10000.00,
                    ""currency"": ""TRY""
                }",
            adminUser
        );
        highAmountRule.Activate(adminUser);
        rules.Add(highAmountRule);

        // Kullanıcı ortalamasının 5 katı işlem
        var highAverageRule = FraudRule.Create(
            "Ortalama Üzeri İşlem",
            "Kullanıcının ortalama işlem tutarının 5 katı veya daha fazla olan işlemlerde ilave doğrulama ister.",
            RuleCategory.Transaction,
            RuleType.Complex,
            ImpactLevel.Medium,
            new List<RuleAction> { RuleAction.RequireAdditionalVerification },
            null, // Süresiz
            30,
            "Transaction.Amount > (UserAverageTransactionAmount * 5)",
            @"{
                    ""maxMultipleOfAverage"": 5.0
                }",
            adminUser
        );
        highAverageRule.Activate(adminUser);
        rules.Add(highAverageRule);

        // Kısa sürede çok sayıda işlem
        var multipleTransactionsRule = FraudRule.Create(
            "Kısa Sürede Çok Sayıda İşlem",
            "1 saat içinde 10 veya daha fazla işlem yapılması durumunda ilave doğrulama ister.",
            RuleCategory.Transaction,
            RuleType.Threshold,
            ImpactLevel.Medium,
            new List<RuleAction> { RuleAction.RequireAdditionalVerification },
            null, // Süresiz
            30,
            "",
            @"{
                    ""maxTransactionCount"": 10,
                    ""timeWindowMinutes"": 60
                }",
            adminUser
        );
        multipleTransactionsRule.Activate(adminUser);
        rules.Add(multipleTransactionsRule);

        // Gece saatlerinde yüksek tutarlı işlem
        var nightHighAmountRule = FraudRule.Create(
            "Gece Saatlerinde Yüksek Tutarlı İşlem",
            "Gece saatlerinde (00:00-06:00) 5.000 TL üzerindeki işlemlerde ilave doğrulama ister.",
            RuleCategory.Complex,
            RuleType.Complex,
            ImpactLevel.High,
            new List<RuleAction> { RuleAction.RequireAdditionalVerification, RuleAction.PutUnderReview },
            null, // Süresiz
            20,
            "Hour BETWEEN 0 AND 6 AND Transaction.Amount > 5000",
            @"{
                    ""nightHoursStart"": 0,
                    ""nightHoursEnd"": 6,
                    ""maxAmount"": 5000.00,
                    ""currency"": ""TRY""
                }",
            adminUser
        );
        nightHighAmountRule.Activate(adminUser);
        rules.Add(nightHighAmountRule);

        #endregion

        return rules;
    }
}