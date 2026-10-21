using Dentify.Contracts.Models;

namespace Dentify.Api.Strategies
{
    public interface IReportService
    {
        // Writes a short damage report, comparing with the previous inspection when there is one
        Task<string> WriteSummaryAsync(InspectionDetailDto inspection, InspectionDetailDto? previousInspection);
    }
}
