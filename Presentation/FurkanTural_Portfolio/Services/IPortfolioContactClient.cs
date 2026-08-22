using FurkanTural_Portfolio.Models;

namespace FurkanTural_Portfolio.Services;

public interface IPortfolioContactClient
{
    Task<bool> SubmitContactAsync(ContactFormModel model, CancellationToken ct = default);
}