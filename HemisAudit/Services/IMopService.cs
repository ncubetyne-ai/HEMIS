using HemisAudit.ViewModels;

namespace HemisAudit.Services
{
    public interface IMopService
    {
        Task<DatabaseListResult> GetDatabasesAsync(string server, string driver);
        Task<MopTableDiscoveryResult> GetTablesAsync(string server, string database, string driver);
        Task<ColumnListResult> GetColumnsAsync(string server, string database, string driver, string tableName);
        Task<MopVerifyResult> VerifyTablesAsync(MopVerifyRequest request);
        Task<MopValidationSummary> RunValidationAsync(MopValidationRequest request, string? userEmail = null, string? userName = null);
        Task<string> GenerateSqlAsync(MopValidationRequest request);
        Task<MopWorkspaceState?> GetCurrentWorkspaceStateAsync(int clientId, string? userEmail = null);
        Task<bool> SaveWorkspaceStateAsync(int clientId, MopValidationRequest config, string? userEmail = null);
        Task AddOrUpdateSignoffAsync(int runId, string email, string comment);
        Task RemoveSignoffAsync(int runId, string email);
        Task<MopValidationSummary?> GetFullSummaryByRunIdAsync(int runId);
    }
}
