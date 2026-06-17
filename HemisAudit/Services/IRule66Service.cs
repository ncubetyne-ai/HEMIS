using HemisAudit.ViewModels;

namespace HemisAudit.Services
{
    public interface IRule66Service
    {
        Task<DatabaseListResult> GetDatabasesAsync(string server, string driver);
        Task<Rule66TableDiscoveryResult> GetTablesAsync(string server, string database, string driver);
        Task<ColumnListResult> GetColumnsAsync(string server, string database, string driver, string tableName);
        Task<Rule66VerifyResult> VerifyTablesAsync(Rule66VerifyRequest request);
        Task<Rule66ValidationSummary> RunValidationAsync(Rule66ValidationRequest request, string? userEmail = null, string? userName = null);
        Task<Rule66ValidationSummary?> GetPendingValidationPreviewAsync(int clientId, string reviewerEmail);
        Task<bool> HasPendingValidationAsync(int clientId, string reviewerEmail);
        Task<int?> GetClientIdForRunAsync(int runId);
        Task<Rule66WorkspaceStateViewModel?> GetCurrentWorkspaceStateAsync(int clientId, string? currentUserEmail = null, bool includeSummary = true);
        Task<Rule66RunReviewViewModel?> GetSavedRunAsync(int runId, string? currentUserEmail = null, bool includeFullResults = false);
        Task<Rule66WorkspaceSaveResult> SaveWorkspaceAsync(Rule66ValidationRequest request, string reviewerEmail, string? reviewerName = null);
        Task<Rule66WorkspaceSaveResult> BeginWorkspaceEditAsync(int runId, string reviewerEmail, string? reviewerName = null);
        Task AddOrUpdateSignoffAsync(int runId, string reviewerEmail, string comment);
        Task RemoveSignoffAsync(int runId, string reviewerEmail);
        Task<string> GenerateSqlAsync(Rule66ValidationRequest request);
        Task<Rule66ValidationSummary> GetExportSummaryAsync(Rule66ValidationRequest request);
    }
}
