using Analiz.Domain;

namespace Analiz.Application.Interfaces.Services;

public interface IUserProfileRepository
{
    Task<RiskProfile> GetUserRiskProfileAsync(string userId);
    Task SaveUserRiskProfileAsync(RiskProfile profile);
}