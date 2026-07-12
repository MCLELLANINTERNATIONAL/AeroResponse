using AeroResponse.Models;
using AeroResponse.Repositories;

namespace AeroResponse.Services;

public class PerformanceService(
    IGenericRepository<SimulationReport> reportRepository)
{
    public Task<IReadOnlyList<SimulationReport>> GetAllAsync()
    {
        return reportRepository.GetAllAsync();
    }

    public Task<SimulationReport?> GetByIdAsync(int id)
    {
        return reportRepository.GetByIdAsync(id);
    }

    public async Task<IReadOnlyList<SimulationReport>> GetUserReportsAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var reports = await reportRepository.GetAllAsync();

        return [.. reports
            .Where(report => report.UserId == userId)
            .OrderByDescending(report => report.CreatedAt)];
    }

    public Task<SimulationReport> CreateAsync(SimulationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        report.CreatedAt = DateTime.UtcNow;

        return reportRepository.AddAsync(report);
    }

    public Task UpdateAsync(SimulationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return reportRepository.UpdateAsync(report);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return reportRepository.DeleteAsync(id);
    }

    public Task<bool> ExistsAsync(int id)
    {
        return reportRepository.ExistsAsync(id);
    }
}