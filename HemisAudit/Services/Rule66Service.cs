using System.Globalization;
using System.Security.Cryptography;
using HemisAudit.ViewModels;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace HemisAudit.Services
{
    public class Rule66Service : IRule66Service
    {
        private const int BrowserPreviewRowLimit = 10;
        private readonly IConfiguration _configuration;
        private readonly IPendingValidationCacheService _pendingValidationCache;

        public Rule66Service(IConfiguration configuration, IPendingValidationCacheService pendingValidationCache)
        {
            _configuration = configuration;
            _pendingValidationCache = pendingValidationCache;
        }

        public async Task<DatabaseListResult> GetDatabasesAsync(string server, string driver)
        {
            try
            {
                var connStr = BuildConnectionString(server, "master", driver);
                await using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();
                await using var cmd = conn.CreateConfiguredCommand();
                cmd.CommandText = "SELECT name FROM sys.databases WHERE name NOT IN ('master','tempdb','model','msdb') ORDER BY name;";
                await using var reader = await cmd.ExecuteReaderAsync();
                var items = new List<string>();
                while (await reader.ReadAsync()) items.Add(reader.GetString(0));
                return new DatabaseListResult { Success = true, Databases = items };
            }
            catch (Exception ex) { return new DatabaseListResult { Success = false, Error = ex.Message }; }
        }

        public async Task<Rule66TableDiscoveryResult> GetTablesAsync(string server, string database, string driver)
        {
            try
            {
                var connStr = BuildConnectionString(server, database, driver);
                await using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();
                await using var cmd = conn.CreateConfiguredCommand();
                cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME;";
                await using var reader = await cmd.ExecuteReaderAsync();
                var tables = new List<string>();
                while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
                return new Rule66TableDiscoveryResult
                {
                    Success = true,
                    Tables = tables,
                    AutoStudTable = FindFirst(tables, ["dbo_STUD_VALPAC"], ["stud_valpac", "dbo_stud"]),
                    AutoCregTable = FindFirst(tables, ["dbo_CREG"], ["dbo_creg", "creg"])
                };
            }
            catch (Exception ex) { return new Rule66TableDiscoveryResult { Success = false, Error = ex.Message }; }
        }

        public async Task<ColumnListResult> GetColumnsAsync(string server, string database, string driver, string tableName)
        {
            try
            {
                ValidateObjectName(tableName);
                var tbl = Sanitise(tableName);
                var connStr = BuildConnectionString(server, database, driver);
                await using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();
                await using var cmd = conn.CreateConfiguredCommand();
                cmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @T ORDER BY ORDINAL_POSITION;";
                cmd.Parameters.AddWithValue("@T", tbl);
                await using var reader = await cmd.ExecuteReaderAsync();
                var cols = new List<string>();
                while (await reader.ReadAsync()) if (!reader.IsDBNull(0)) cols.Add(reader.GetString(0));
                return new ColumnListResult { Success = true, Columns = cols, AutoSelected = cols.FirstOrDefault() };
            }
            catch (Exception ex) { return new ColumnListResult { Success = false, Error = ex.Message }; }
        }

        public async Task<Rule66VerifyResult> VerifyTablesAsync(Rule66VerifyRequest request)
        {
            try
            {
                ValidateRequest(request);
                var connStr = BuildConnectionString(request.Server, request.Database, request.Driver);
                await using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                var studTable = Sanitise(request.StudTable);
                var cregTable = Sanitise(request.CregTable);
                var fundingCol = Sanitise(string.IsNullOrWhiteSpace(request.FundingSourceCol) ? "_019" : request.FundingSourceCol);
                var fundingValues = ParseFilterValues(request.FundingSourceValues);

                var studCount = await CountAsync(conn, $"SELECT COUNT(*) FROM [{studTable}];");
                var cregCount = await CountAsync(conn, $"SELECT COUNT(*) FROM [{cregTable}];");

                int nsfasCount = 0;
                if (fundingValues.Count > 0)
                {
                    var inList = BuildInClauseSql(fundingValues);
                    nsfasCount = await CountAsync(conn, $"SELECT COUNT(*) FROM [{studTable}] WHERE UPPER(LTRIM(RTRIM(CONVERT(nvarchar(255),[{fundingCol}])))) IN ({inList});");
                }
                else
                {
                    nsfasCount = studCount;
                }

                return new Rule66VerifyResult { Success = true, StudRecordCount = studCount, StudNsfasCount = nsfasCount, CregRecordCount = cregCount };
            }
            catch (Exception ex) { return new Rule66VerifyResult { Success = false, Error = ex.Message }; }
        }

        public async Task<Rule66ValidationSummary> RunValidationAsync(Rule66ValidationRequest request, string? userEmail = null, string? userName = null)
        {
            try
            {
                ValidateRequest(request);
                var browserSummary = await AnalyseAsync(request, includeAllReviewRows: false);
                if (browserSummary.Success && request.ClientId > 0)
                {
                    try
                    {
                        var full = CloneSummary(browserSummary);
                        if (full.IsPreviewOnly || full.ReviewRows.Count < full.TotalValidated)
                            full = await AnalyseAsync(request, includeAllReviewRows: true);
                        full.SavedRunId = null;
                        browserSummary.SavedRunId = await SaveValidationRunAsync(CloneRequest(request), full, userEmail, userName, markWorkspaceSaved: false);
                        if (!string.IsNullOrWhiteSpace(userEmail))
                            _pendingValidationCache.ClearPending(66, request.ClientId, userEmail!);
                    }
                    catch (Exception ex)
                    {
                        browserSummary.Warning = $"Analysis completed, but the workspace could not be saved automatically: {ex.Message}";
                    }
                }

                if (!browserSummary.SavedRunId.HasValue)
                {
                    if (browserSummary.Success && request.ClientId > 0 && !string.IsNullOrWhiteSpace(userEmail))
                        _pendingValidationCache.StorePending(66, request.ClientId, userEmail!, request, CloneSummary(browserSummary), userName);
                    browserSummary.Warning = string.IsNullOrWhiteSpace(browserSummary.Warning)
                        ? "Counts reflect the full NSFAS student population. Browser review rows are limited for performance."
                        : browserSummary.Warning;
                }
                else
                {
                    browserSummary.Warning = "The current Rule 66 run has been written to the system database. Click Save Workspace to finalize it for signoff.";
                }

                ApplyBrowserPreview(browserSummary);
                return browserSummary;
            }
            catch (Exception ex) { return new Rule66ValidationSummary { Success = false, Error = ex.Message }; }
        }

        public async Task<Rule66ValidationSummary> GetExportSummaryAsync(Rule66ValidationRequest request)
        {
            ValidateRequest(request);
            return await AnalyseAsync(request, includeAllReviewRows: true);
        }

        public Task<Rule66ValidationSummary?> GetPendingValidationPreviewAsync(int clientId, string reviewerEmail)
        {
            var pending = _pendingValidationCache.GetPending<Rule66ValidationRequest, Rule66ValidationSummary>(66, clientId, reviewerEmail);
            if (pending == null) return Task.FromResult<Rule66ValidationSummary?>(null);
            var preview = CloneSummary(pending.Summary);
            preview.SavedRunId = null;
            preview.Warning = "This Rule 66 validation is still pending. Click Save Workspace to write it to the system database.";
            ApplyBrowserPreview(preview);
            return Task.FromResult<Rule66ValidationSummary?>(preview);
        }

        public Task<bool> HasPendingValidationAsync(int clientId, string reviewerEmail)
            => Task.FromResult(_pendingValidationCache.HasPending(66, clientId, reviewerEmail));

        public async Task<int?> GetClientIdForRunAsync(int runId)
        {
            await using var connection = await OpenSystemConnectionAsync();
            return await GetClientIdForRunAsync(connection, runId);
        }

        public async Task<Rule66WorkspaceStateViewModel?> GetCurrentWorkspaceStateAsync(int clientId, string? currentUserEmail = null, bool includeSummary = true)
        {
            await using var connection = await OpenSystemConnectionAsync();
            var currentUserId = await GetSystemUserIdByEmailAsync(connection, currentUserEmail);

            await using var command = connection.CreateConfiguredCommand();
            command.CommandText = @"
SELECT TOP 1
    vr.RunID, vr.ClientID,
    ISNULL(vr.HemisServer, '') AS HemisServer,
    ISNULL(vr.AuditDatabase, '') AS AuditDatabase,
    ISNULL(vr.StudTable, '') AS StudTable,
    ISNULL(vr.DeceasedTable, '') AS CregTable,
    ISNULL(vr.Status, '') AS Status,
    vr.LastEditedByUserName, vr.LastEditedAt, vr.ResultsJSON
FROM dbo.ValidationRuns vr
WHERE vr.ClientID = @ClientID AND vr.RuleNumber = 66 AND vr.IsCurrent = 1
ORDER BY vr.RunTimestamp DESC, vr.RunID DESC;";
            command.Parameters.AddWithValue("@ClientID", clientId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var runId                = reader.GetInt32(0);
            var workspaceClientId    = reader.GetInt32(1);
            var server               = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var database             = reader.IsDBNull(3) ? "" : reader.GetString(3);
            var studTable            = reader.IsDBNull(4) ? "dbo_STUD_VALPAC" : reader.GetString(4);
            var cregTable            = reader.IsDBNull(5) ? "" : reader.GetString(5);
            var currentStatus        = reader.IsDBNull(6) ? "" : reader.GetString(6);
            var lastEditedByUserName = reader.IsDBNull(7) ? null : reader.GetString(7);
            DateTime? lastEditedAt   = reader.IsDBNull(8) ? null : reader.GetDateTime(8);
            var encodedSummary       = reader.IsDBNull(9) ? null : reader.GetString(9);

            await reader.CloseAsync();

            var deserializedSummary = DeserializeSummary(encodedSummary);
            if (deserializedSummary != null && includeSummary)
            {
                deserializedSummary = await ExpandAndPersistSavedSummaryIfNeededAsync(connection, runId, deserializedSummary, server);
                ApplyBrowserPreview(deserializedSummary);
            }
            var summary = includeSummary ? deserializedSummary : null;

            var workspace = new Rule66WorkspaceStateViewModel
            {
                ClientId             = workspaceClientId,
                RunId                = runId,
                Server               = server,
                Database             = database,
                StudTable            = deserializedSummary?.StudTable ?? studTable,
                CregTable            = deserializedSummary?.CregTable ?? cregTable,
                StudStudentNoCol     = deserializedSummary?.StudStudentNoCol ?? "_007",
                CregStudentNoCol     = deserializedSummary?.CregStudentNoCol ?? "_007",
                FundingSourceCol     = deserializedSummary?.FundingSourceCol ?? "_019",
                FundingSourceValues  = deserializedSummary?.FundingSourceValues ?? "NS",
                CurrentStatus        = currentStatus,
                LastEditedByUserName = lastEditedByUserName,
                LastEditedAt         = lastEditedAt,
                Summary              = summary
            };

            if (summary != null) workspace.CurrentStatus = summary.Status;
            workspace.Driver = "ODBC Driver 17 for SQL Server";
            workspace.CurrentUserEngagementRole = currentUserId.HasValue
                ? await GetEngagementRoleAsync(connection, clientId, currentUserId.Value) ?? "" : "";

            var signoffs = await GetRunSignoffsAsync(connection, workspace.RunId!.Value, currentUserId);
            workspace.HasDataAnalystSignoff = signoffs.Any(s => string.Equals(s.SignoffRole, "DataAnalyst", StringComparison.OrdinalIgnoreCase));
            var currentRoleSignoff = signoffs.FirstOrDefault(s =>
                HemisAudit.Helpers.ValidationRunAccessPolicy.IsSignoffOwnedByEngagementRole(s.SignoffRole, workspace.CurrentUserEngagementRole));
            workspace.CurrentUserHasSignedOff = currentRoleSignoff != null;
            workspace.CurrentUserSignoffComment = currentRoleSignoff?.Comment ?? "";
            workspace.IsWorkspaceSaved = await IsWorkspaceSavedAsync(connection, workspace.RunId!.Value);
            if (workspace.Summary != null) workspace.Summary.SavedRunId = workspace.RunId;

            return workspace;
        }

        public async Task<Rule66RunReviewViewModel?> GetSavedRunAsync(int runId, string? currentUserEmail = null, bool includeFullResults = false)
        {
            await using var connection = await OpenSystemConnectionAsync();
            var currentUserId = await GetSystemUserIdByEmailAsync(connection, currentUserEmail);

            await using var command = connection.CreateConfiguredCommand();
            command.CommandText = @"
SELECT vr.RunID, vr.ClientID, vr.IsCurrent, c.EngagementName, c.MaconomyNumber, vr.HemisServer, vr.ResultsJSON
FROM dbo.ValidationRuns vr
INNER JOIN dbo.Clients c ON c.ClientID = vr.ClientID
WHERE vr.RunID = @RunID AND vr.RuleNumber = 66;";
            command.Parameters.AddWithValue("@RunID", runId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var savedRunId     = reader.GetInt32(0);
            var clientId       = reader.GetInt32(1);
            var isCurrentRun   = !reader.IsDBNull(2) && reader.GetBoolean(2);
            var engagementName = reader.IsDBNull(3) ? "" : reader.GetString(3);
            var maconomyNumber = reader.IsDBNull(4) ? "" : reader.GetString(4);
            var sourceServer   = reader.IsDBNull(5) ? "" : reader.GetString(5);
            var encodedSummary = reader.IsDBNull(6) ? null : reader.GetString(6);
            var summary        = DeserializeSummary(encodedSummary);
            if (summary == null) return null;

            summary.ClientId = clientId;
            if (summary.SavedRunId.GetValueOrDefault() <= 0) summary.SavedRunId = runId;
            await reader.CloseAsync();

            summary = await ExpandAndPersistSavedSummaryIfNeededAsync(connection, runId, summary, sourceServer);

            if (includeFullResults) { summary.DisplayedCount = summary.ReviewRows.Count; summary.IsPreviewOnly = false; summary.PreviewLimit = 0; }
            else { ApplyBrowserPreview(summary); }

            var review = new Rule66RunReviewViewModel
            {
                RunId = savedRunId, ClientId = clientId, IsCurrentRun = isCurrentRun,
                EngagementName = engagementName, MaconomyNumber = maconomyNumber,
                SourceServer = sourceServer, Summary = summary
            };

            review.CurrentUserEngagementRole = currentUserId.HasValue
                ? await GetEngagementRoleAsync(connection, clientId, currentUserId.Value) ?? "" : "";
            review.Signoffs = await GetRunSignoffsAsync(connection, runId, currentUserId);
            review.HasDataAnalystSignoff = review.Signoffs.Any(s => string.Equals(s.SignoffRole, "DataAnalyst", StringComparison.OrdinalIgnoreCase));

            return review;
        }

        public async Task<Rule66WorkspaceSaveResult> SaveWorkspaceAsync(Rule66ValidationRequest request, string reviewerEmail, string? reviewerName = null)
        {
            try
            {
                ValidateRequest(request);

                if (request.RunId.HasValue && request.RunId.Value > 0)
                {
                    await using var connection = await OpenSystemConnectionAsync();
                    var clientId = await GetClientIdForRunAsync(connection, request.RunId.Value);
                    if (!clientId.HasValue || clientId.Value != request.ClientId)
                        return new Rule66WorkspaceSaveResult { Success = false, Error = "The saved workspace could not be found for this engagement." };

                    await EnsureClientNotArchivedAsync(connection, request.ClientId);
                    var clearedSignoffs = await ClearSignoffsAndFlagForReviewAsync(connection, request.RunId.Value);
                    var previousHash    = await GetValidationRecordHashAsync(connection, request.RunId.Value);

                    await using var cmd = connection.CreateConfiguredCommand();
                    cmd.CommandText = @"
UPDATE dbo.ValidationRuns
SET LastEditedByUserName=@LastEditedByUserName, LastEditedAt=GETDATE(), WorkspaceSavedAt=GETDATE(),
    PreviousHash=@PreviousHash, RecordHash=@RecordHash, Status='Needs Review'
WHERE RunID=@RunID AND ClientID=@ClientID;";
                    cmd.Parameters.AddWithValue("@RunID", request.RunId.Value);
                    cmd.Parameters.AddWithValue("@ClientID", request.ClientId);
                    cmd.Parameters.AddWithValue("@LastEditedByUserName", (object?)reviewerName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PreviousHash", (object?)previousHash ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RecordHash", ComputeHash($"WorkspaceSave|Rule66|{request.RunId.Value}|{request.ClientId}|{(reviewerName ?? reviewerEmail)}|{DateTime.UtcNow:o}|{previousHash}"));
                    await cmd.ExecuteNonQueryAsync();

                    if (!string.IsNullOrWhiteSpace(reviewerEmail))
                        _pendingValidationCache.ClearPending(66, request.ClientId, reviewerEmail);

                    var currentWorkspace = await GetCurrentWorkspaceStateAsync(request.ClientId, reviewerEmail, includeSummary: false);
                    return new Rule66WorkspaceSaveResult { Success = true, Message = clearedSignoffs > 0 ? "Workspace saved. Existing signoffs were removed." : "Workspace saved and marked for review.", SignoffsCleared = clearedSignoffs > 0, ClearedSignoffCount = clearedSignoffs, Workspace = currentWorkspace };
                }

                var pending = _pendingValidationCache.GetPending<Rule66ValidationRequest, Rule66ValidationSummary>(66, request.ClientId, reviewerEmail);
                if (pending == null)
                    return new Rule66WorkspaceSaveResult { Success = false, Error = "Run Rule 66 first so the current workspace is written to the system database." };

                if (!RequestsMatch(request, pending.Request))
                    return new Rule66WorkspaceSaveResult { Success = false, Error = "Workspace settings changed after validation. Run Rule 66 again before saving." };

                var summaryToSave = CloneSummary(pending.Summary);
                if (summaryToSave.IsPreviewOnly || summaryToSave.ReviewRows.Count < summaryToSave.TotalValidated)
                    summaryToSave = await AnalyseAsync(pending.Request, includeAllReviewRows: true);

                summaryToSave.SavedRunId = null;
                var savedRunId = await SaveValidationRunAsync(CloneRequest(pending.Request), summaryToSave, reviewerEmail, reviewerName ?? pending.ReviewerName, markWorkspaceSaved: true);
                _pendingValidationCache.ClearPending(66, request.ClientId, reviewerEmail);

                var workspace = await GetCurrentWorkspaceStateAsync(request.ClientId, reviewerEmail, includeSummary: false);
                return new Rule66WorkspaceSaveResult { Success = true, Message = $"Workspace saved as Run #{savedRunId}.", SignoffsCleared = false, ClearedSignoffCount = 0, Workspace = workspace };
            }
            catch (Exception ex) { return new Rule66WorkspaceSaveResult { Success = false, Error = ex.Message }; }
        }

        public async Task<Rule66WorkspaceSaveResult> BeginWorkspaceEditAsync(int runId, string reviewerEmail, string? reviewerName = null)
        {
            try
            {
                await using var connection = await OpenSystemConnectionAsync();
                var clientId = await GetClientIdForRunAsync(connection, runId);
                if (!clientId.HasValue) return new Rule66WorkspaceSaveResult { Success = false, Error = "Saved workspace was not found." };

                await EnsureClientNotArchivedAsync(connection, clientId.Value);
                var clearedSignoffs = await ClearSignoffsAndFlagForReviewAsync(connection, runId);
                var previousHash    = await GetValidationRecordHashAsync(connection, runId);

                await using var cmd = connection.CreateConfiguredCommand();
                cmd.CommandText = @"
UPDATE dbo.ValidationRuns
SET LastEditedByUserName=@LastEditedByUserName, LastEditedAt=GETDATE(), WorkspaceSavedAt=NULL,
    PreviousHash=@PreviousHash, RecordHash=@RecordHash, Status='Needs Review'
WHERE RunID=@RunID;";
                cmd.Parameters.AddWithValue("@RunID", runId);
                cmd.Parameters.AddWithValue("@LastEditedByUserName", (object?)reviewerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PreviousHash", (object?)previousHash ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RecordHash", ComputeHash($"BeginEdit|Rule66|{runId}|{reviewerEmail}|{DateTime.UtcNow:o}|{previousHash}"));
                await cmd.ExecuteNonQueryAsync();

                if (!string.IsNullOrWhiteSpace(reviewerEmail))
                    _pendingValidationCache.ClearPending(66, clientId.Value, reviewerEmail);

                var workspace = await GetCurrentWorkspaceStateAsync(clientId.Value, reviewerEmail, includeSummary: false);
                return new Rule66WorkspaceSaveResult { Success = true, Message = clearedSignoffs > 0 ? "Editing begun. Existing signoffs removed." : "Editing begun. Save the workspace when ready.", SignoffsCleared = clearedSignoffs > 0, ClearedSignoffCount = clearedSignoffs, Workspace = workspace };
            }
            catch (Exception ex) { return new Rule66WorkspaceSaveResult { Success = false, Error = ex.Message }; }
        }

        public async Task AddOrUpdateSignoffAsync(int runId, string reviewerEmail, string comment)
        {
            await using var connection = await OpenSystemConnectionAsync();
            var reviewerId = await GetSystemUserIdByEmailAsync(connection, reviewerEmail);
            if (!reviewerId.HasValue) throw new InvalidOperationException("The reviewer could not be resolved.");
            var clientId = await GetClientIdForRunAsync(connection, runId);
            if (!clientId.HasValue) throw new InvalidOperationException("The selected Rule 66 run could not be found.");
            await EnsureClientNotArchivedAsync(connection, clientId.Value);
            if (!await IsWorkspaceSavedAsync(connection, runId)) throw new InvalidOperationException("The data analyst must save the workspace before signoff.");
            var signoffRole = await GetEngagementRoleAsync(connection, clientId.Value, reviewerId.Value);
            if (!CanSignOffAsRole(signoffRole)) throw new InvalidOperationException("Only the assigned data analyst, manager, or director can sign off.");
            if (!string.Equals(signoffRole, "DataAnalyst", StringComparison.OrdinalIgnoreCase) && !await HasSignoffRoleAsync(connection, runId, "DataAnalyst"))
                throw new InvalidOperationException("The data analyst must sign off before this review can be completed.");

            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM dbo.ReviewSignoffs WHERE RunID=@RunID AND ReviewerID=@ReviewerID)
    UPDATE dbo.ReviewSignoffs SET SignoffRole=@SignoffRole,ReviewType='Final',Comment=@Comment,SignedOffAt=GETDATE() WHERE RunID=@RunID AND ReviewerID=@ReviewerID;
ELSE
    INSERT INTO dbo.ReviewSignoffs (ClientID,RunID,ReviewerID,SignoffRole,ReviewType,Comment,SignedOffAt) VALUES (@ClientID,@RunID,@ReviewerID,@SignoffRole,'Final',@Comment,GETDATE());";
            cmd.Parameters.AddWithValue("@ClientID", clientId.Value);
            cmd.Parameters.AddWithValue("@RunID", runId);
            cmd.Parameters.AddWithValue("@ReviewerID", reviewerId.Value);
            cmd.Parameters.AddWithValue("@SignoffRole", signoffRole!);
            cmd.Parameters.AddWithValue("@Comment", string.IsNullOrWhiteSpace(comment) ? DBNull.Value : comment.Trim());
            await cmd.ExecuteNonQueryAsync();
            await UpdateRunStatusFromSignoffsAsync(connection, runId);
        }

        public async Task RemoveSignoffAsync(int runId, string reviewerEmail)
        {
            await using var connection = await OpenSystemConnectionAsync();
            var reviewerId = await GetSystemUserIdByEmailAsync(connection, reviewerEmail);
            if (!reviewerId.HasValue) throw new InvalidOperationException("The reviewer could not be resolved.");
            var clientId = await GetClientIdForRunAsync(connection, runId);
            if (!clientId.HasValue) throw new InvalidOperationException("The selected Rule 66 run could not be found.");
            await EnsureClientNotArchivedAsync(connection, clientId.Value);
            var engagementRole = await GetEngagementRoleAsync(connection, clientId.Value, reviewerId.Value);
            if (!HemisAudit.Helpers.ValidationRunAccessPolicy.CanAssignedUserRemoveSignoff(engagementRole))
                throw new InvalidOperationException("Only the assigned data analyst, manager, or director can remove signoff.");
            await ReviewSignoffSqlHelper.RemoveRoleSignoffWithVersioningAsync(connection, runId, engagementRole!, reviewerEmail);
        }

        public Task<string> GenerateSqlAsync(Rule66ValidationRequest request)
        {
            ValidateRequest(request);
            var studTable = Sanitise(request.StudTable);
            var cregTable = Sanitise(request.CregTable);
            var studNoCol = Sanitise(request.StudStudentNoCol);
            var cregNoCol = Sanitise(request.CregStudentNoCol);
            var fundingCol = Sanitise(string.IsNullOrWhiteSpace(request.FundingSourceCol) ? "_019" : request.FundingSourceCol);
            var fundingValues = ParseFilterValues(request.FundingSourceValues);
            var fundingValuesText = fundingValues.Count > 0 ? string.Join(", ", fundingValues.Select(v => $"'{v}'")) : "ALL — no filter applied";

            var sql = $@"-- HEMIS RULE 66: NSFAS STUDENTS IN CREG
-- Check: All NSFAS students in [{studTable}] where [{fundingCol}] IN ({fundingValuesText}) must have a record in [{cregTable}] on [{cregNoCol}]
-- PASS when found in CREG; FAIL when missing

{BuildSourceCtes(studTable, cregTable, studNoCol, cregNoCol, fundingCol, fundingValues)}
SELECT
    'Control_1' AS Control_Type,
    'CONTROL 1: NSFAS students [{studTable}].[{studNoCol}] exist in [{cregTable}].[{cregNoCol}]' AS Control_Label,
    STUD_NO, FUNDING_SOURCE, CREG_STUD_NO,
    CASE WHEN CREG_STUD_NO IS NOT NULL THEN 'PASS' ELSE 'FAIL' END AS Validation_Result
FROM ValidationResults
ORDER BY CASE WHEN CREG_STUD_NO IS NULL THEN 0 ELSE 1 END, STUD_NO;

SELECT
    (SELECT COUNT(1) FROM NsfasStudents)                                       AS NSFAS_Total,
    (SELECT COUNT(1) FROM ValidationResults WHERE CREG_STUD_NO IS NOT NULL)    AS Found_In_CREG,
    (SELECT COUNT(1) FROM ValidationResults WHERE CREG_STUD_NO IS NULL)        AS Missing_From_CREG;";

            return Task.FromResult(sql.Trim());
        }

        // ─── Core Analysis ────────────────────────────────────────────────────

        private async Task<Rule66ValidationSummary> AnalyseAsync(Rule66ValidationRequest request, bool includeAllReviewRows)
        {
            var connStr = BuildConnectionString(request.Server, request.Database, request.Driver);
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            var studTable = Sanitise(request.StudTable);
            var cregTable = Sanitise(request.CregTable);
            var studNoCol = Sanitise(request.StudStudentNoCol);
            var cregNoCol = Sanitise(request.CregStudentNoCol);
            var fundingCol = Sanitise(string.IsNullOrWhiteSpace(request.FundingSourceCol) ? "_019" : request.FundingSourceCol);
            var fundingValues = ParseFilterValues(request.FundingSourceValues);
            var fundingValuesText = fundingValues.Count > 0 ? string.Join(", ", fundingValues) : "ALL — no filter applied";

            var studCount = await CountAsync(conn, $"SELECT COUNT(*) FROM [{studTable}];");
            var cregCount = await CountAsync(conn, $"SELECT COUNT(*) FROM [{cregTable}];");

            await using var countCmd = conn.CreateConfiguredCommand();
            countCmd.CommandText = BuildPopulationCountSql(studTable, cregTable, studNoCol, cregNoCol, fundingCol, fundingValues);
            await using var countReader = await countCmd.ExecuteReaderAsync();

            int nsfasCount = 0, matched = 0, missing = 0;
            if (await countReader.ReadAsync())
            {
                nsfasCount = GetInt(countReader, 0);
                matched    = GetInt(countReader, 1);
                missing    = GetInt(countReader, 2);
            }
            await countReader.CloseAsync();

            var reviewRows = await LoadRowsAsync(conn, studTable, cregTable, includeAllReviewRows ? null : BrowserPreviewRowLimit, studNoCol, cregNoCol, fundingCol, fundingValues);
            reviewRows = NormalizeRows(reviewRows);

            var totalValidated = nsfasCount;
            var passCount      = matched;
            var failCount      = missing;
            var isPreviewOnly  = !includeAllReviewRows && totalValidated > reviewRows.Count;

            var controlSummaries = new List<Rule66ControlSummaryItemViewModel>
            {
                new()
                {
                    ControlType  = "Control_1",
                    ControlLabel = "Control 1",
                    CriteriaText = $"All NSFAS students in {studTable} where [{fundingCol}] IN ({fundingValuesText}) must have a record in {cregTable} on [{cregNoCol}]",
                    TotalCount   = totalValidated,
                    PassCount    = passCount,
                    FailCount    = failCount,
                    Status       = failCount == 0 ? "PASS" : "FAIL"
                }
            };

            return new Rule66ValidationSummary
            {
                Success          = true,
                StudRecordCount  = studCount,
                StudNsfasCount   = nsfasCount,
                CregRecordCount  = cregCount,
                TotalValidated   = totalValidated,
                DisplayedCount   = reviewRows.Count,
                IsPreviewOnly    = isPreviewOnly,
                PreviewLimit     = isPreviewOnly ? BrowserPreviewRowLimit : 0,
                PassCount        = passCount,
                FailCount        = failCount,
                ExceptionRate    = totalValidated == 0 ? 0m : Math.Round(failCount * 100m / totalValidated, 2),
                Status           = failCount == 0 ? "PASS" : "FAIL",
                Timestamp        = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Database         = request.Database,
                StudTable        = request.StudTable,
                CregTable        = request.CregTable,
                StudStudentNoCol = studNoCol,
                CregStudentNoCol = cregNoCol,
                FundingSourceCol    = fundingCol,
                FundingSourceValues = fundingValues.Count > 0 ? string.Join(",", fundingValues) : "",
                TableLinkageText = $"{request.StudTable}.{studNoCol} <> {request.CregTable}.{cregNoCol} (filtered to [{fundingCol}] IN {fundingValuesText})",
                RuleModeText     = $"NSFAS students where [{fundingCol}] IN ({fundingValuesText}) in {request.StudTable} must exist as [{cregNoCol}] in {request.CregTable}",
                ProcedureSteps   = new List<string>
                {
                    $"Filter {request.StudTable} to rows where [{fundingCol}] IN ({fundingValuesText}) — these are the NSFAS student population.",
                    $"For each NSFAS student [{studNoCol}], check if a matching row exists in {request.CregTable} on column [{cregNoCol}].",
                    "Mark PASS when the student is found in CREG (has course registrations); FAIL when the student is missing from CREG.",
                    "A FAIL indicates the NSFAS student appears in VALPAC but has no course registration record in CREG."
                },
                ClientId         = request.ClientId,
                ControlSummaries = controlSummaries,
                ReviewRows       = reviewRows,
                Warning = includeAllReviewRows
                    ? "Rule 66 completed with the full NSFAS student population result set."
                    : "Counts reflect the full NSFAS student population. Browser review rows are limited for performance."
            };
        }

        // ─── SQL Builders ─────────────────────────────────────────────────────

        private static string BuildSourceCtes(string studTable, string cregTable, string studNoCol, string cregNoCol, string fundingCol, IReadOnlyList<string> fundingValues) => $@"
WITH NsfasStudents AS
(
    SELECT
        CONVERT(nvarchar(255), S.[{studNoCol}]) AS STUD_NO,
        CONVERT(nvarchar(255), S.[{fundingCol}]) AS FUNDING_SOURCE
    FROM [{studTable}] S
    WHERE S.[{studNoCol}] IS NOT NULL
      AND LTRIM(RTRIM(CONVERT(nvarchar(255), S.[{studNoCol}]))) <> ''
      {BuildFundingFilterSql(fundingCol, fundingValues)}
),
CregStudents AS
(
    SELECT DISTINCT CONVERT(nvarchar(255), C.[{cregNoCol}]) AS CREG_STUD_NO
    FROM [{cregTable}] C
    WHERE C.[{cregNoCol}] IS NOT NULL
),
ValidationResults AS
(
    SELECT
        NS.STUD_NO,
        NS.FUNDING_SOURCE,
        CS.CREG_STUD_NO,
        CASE WHEN CS.CREG_STUD_NO IS NOT NULL THEN 'PASS' ELSE 'FAIL' END AS ValidationResult
    FROM NsfasStudents NS
    LEFT JOIN CregStudents CS ON CS.CREG_STUD_NO = NS.STUD_NO
)";

        private static string BuildPopulationCountSql(string studTable, string cregTable, string studNoCol, string cregNoCol, string fundingCol, IReadOnlyList<string> fundingValues) => $@"
{BuildSourceCtes(studTable, cregTable, studNoCol, cregNoCol, fundingCol, fundingValues)}
SELECT
    COUNT(1)                                                              AS NsfasCount,
    SUM(CASE WHEN CREG_STUD_NO IS NOT NULL THEN 1 ELSE 0 END)            AS MatchedCount,
    SUM(CASE WHEN CREG_STUD_NO IS NULL     THEN 1 ELSE 0 END)            AS MissingCount
FROM ValidationResults;";

        private static string BuildAllRowsSql(string studTable, string cregTable, int? maxRows, string studNoCol, string cregNoCol, string fundingCol, IReadOnlyList<string> fundingValues)
        {
            var top = maxRows.HasValue && maxRows.Value > 0 ? $"TOP {maxRows.Value}" : string.Empty;
            var orderBy = maxRows.HasValue && maxRows.Value > 0
                ? "CASE WHEN CREG_STUD_NO IS NULL THEN 0 ELSE 1 END, STUD_NO"
                : "STUD_NO";
            return $@"
{BuildSourceCtes(studTable, cregTable, studNoCol, cregNoCol, fundingCol, fundingValues)}
SELECT {top}
    1 AS Control_Sort, 'Control_1' AS Control_Type,
    'CONTROL 1: NSFAS students [{studTable}].[{studNoCol}] must exist in [{cregTable}].[{cregNoCol}]' AS Control_Label,
    ValidationResult,
    CASE WHEN ValidationResult = 'PASS'
        THEN 'PASS: NSFAS student found in CREG — course registration record exists.'
        ELSE 'FAIL: NSFAS student not found in CREG — no course registration record.'
    END AS Validation_Explanation,
    STUD_NO, FUNDING_SOURCE, CREG_STUD_NO
FROM ValidationResults
ORDER BY {orderBy};";
        }

        private async Task<List<Rule66ValidationRowRecord>> LoadRowsAsync(SqlConnection connection, string studTable, string cregTable, int? maxRows, string studNoCol, string cregNoCol, string fundingCol, IReadOnlyList<string> fundingValues)
        {
            await using var command = connection.CreateConfiguredCommand();
            command.CommandText = BuildAllRowsSql(studTable, cregTable, maxRows, studNoCol, cregNoCol, fundingCol, fundingValues);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<Rule66ValidationRowRecord>();
            while (await reader.ReadAsync())
            {
                var displayValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                    displayValues[reader.GetName(i)] = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture);

                rows.Add(new Rule66ValidationRowRecord
                {
                    ValidationNumber      = rows.Count + 1,
                    ControlType           = ReadValue(displayValues, "Control_Type"),
                    ControlLabel          = ReadValue(displayValues, "Control_Label"),
                    ValidationResult      = ReadValue(displayValues, "ValidationResult"),
                    ValidationExplanation = ReadValue(displayValues, "Validation_Explanation"),
                    DisplayValues         = displayValues
                });
                EnrichDisplayValues(rows[^1]);
            }
            return rows;
        }

        private static void EnrichDisplayValues(Rule66ValidationRowRecord row)
        {
            var v = row.DisplayValues;
            var isPass    = string.Equals(ReadValue(v, "ValidationResult"), "PASS", StringComparison.OrdinalIgnoreCase);
            var studNo    = ReadValue(v, "STUD_NO");
            var fundingSrc = ReadValue(v, "FUNDING_SOURCE");

            v["FINAL_RESULT_MESSAGE"] = isPass
                ? $"PASS: NSFAS student '{studNo}' (funding: {fundingSrc}) found in CREG."
                : $"FAIL: NSFAS student '{studNo}' (funding: {fundingSrc}) not found in CREG — no course registration record.";
            row.ValidationExplanation = ReadValue(v, "FINAL_RESULT_MESSAGE");
        }

        // ─── Save / Persist ───────────────────────────────────────────────────

        private async Task<int> SaveValidationRunAsync(Rule66ValidationRequest request, Rule66ValidationSummary summary, string? userEmail, string? userName, bool markWorkspaceSaved)
        {
            await using var connection = await OpenSystemConnectionAsync();
            await EnsureClientNotArchivedAsync(connection, request.ClientId);
            await MarkPreviousRunsHistoricalAsync(connection, request.ClientId, 66);

            var systemUserId = await GetSystemUserIdByEmailAsync(connection, userEmail);
            if (!systemUserId.HasValue) throw new InvalidOperationException("The current analyst could not be resolved.");

            var previousHash = await GetLatestValidationHashAsync(connection, request.ClientId, 66);
            var failRows     = summary.ReviewRows.Where(r => string.Equals(r.ValidationResult, "FAIL", StringComparison.OrdinalIgnoreCase)).ToList();

            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = @"
INSERT INTO dbo.ValidationRuns
(ClientID,UserID,RuleNumber,RuleName,Status,TotalRecords,PassCount,FailCount,ExceptionRate,RunTimestamp,
 HemisServer,AuditDatabase,StudTable,DeceasedTable,StudColumn,DeceasedColumn,
 ExceptionsJSON,ResultsJSON,RunByUserName,LastEditedByUserName,LastEditedAt,PreviousHash,RecordHash,WorkspaceSavedAt,IsCurrent)
VALUES
(@ClientID,@UserID,66,@RuleName,@Status,@TotalRecords,@PassCount,@FailCount,@ExceptionRate,GETDATE(),
 @HemisServer,@AuditDatabase,@StudTable,@DeceasedTable,@StudColumn,@DeceasedColumn,
 @ExceptionsJSON,@ResultsJSON,@RunByUserName,NULL,NULL,@PreviousHash,NULL,@WorkspaceSavedAt,1);
SELECT CAST(SCOPE_IDENTITY() AS int);";
            cmd.Parameters.AddWithValue("@ClientID", request.ClientId);
            cmd.Parameters.AddWithValue("@UserID", systemUserId.Value);
            cmd.Parameters.AddWithValue("@RuleName", "NSFAS Students in CREG");
            cmd.Parameters.AddWithValue("@Status", summary.Status);
            cmd.Parameters.AddWithValue("@TotalRecords", summary.TotalValidated);
            cmd.Parameters.AddWithValue("@PassCount", summary.PassCount);
            cmd.Parameters.AddWithValue("@FailCount", summary.FailCount);
            cmd.Parameters.AddWithValue("@ExceptionRate", summary.ExceptionRate);
            cmd.Parameters.AddWithValue("@HemisServer", request.Server);
            cmd.Parameters.AddWithValue("@AuditDatabase", request.Database);
            cmd.Parameters.AddWithValue("@StudTable", request.StudTable);
            cmd.Parameters.AddWithValue("@DeceasedTable", request.CregTable);
            cmd.Parameters.AddWithValue("@StudColumn", request.StudStudentNoCol);
            cmd.Parameters.AddWithValue("@DeceasedColumn", request.CregStudentNoCol);
            cmd.Parameters.AddWithValue("@ExceptionsJSON", ValidationPayloadCodec.Encode(JsonConvert.SerializeObject(failRows)));
            cmd.Parameters.AddWithValue("@ResultsJSON", ValidationPayloadCodec.Encode(JsonConvert.SerializeObject(summary)));
            cmd.Parameters.AddWithValue("@RunByUserName", (object?)userName ?? (object?)userEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PreviousHash", (object?)previousHash ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@WorkspaceSavedAt", markWorkspaceSaved ? DateTime.UtcNow : (object)DBNull.Value);

            var runId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            summary.SavedRunId = runId;

            await using var hashCmd = connection.CreateConfiguredCommand();
            hashCmd.CommandText = "UPDATE dbo.ValidationRuns SET RecordHash=@RecordHash WHERE RunID=@RunID;";
            hashCmd.Parameters.AddWithValue("@RunID", runId);
            hashCmd.Parameters.AddWithValue("@RecordHash", ComputeHash($"ValidationRun|Rule66|{runId}|{request.ClientId}|{systemUserId.Value}|{summary.Status}|{summary.TotalValidated}|{summary.FailCount}|{summary.Timestamp}|{previousHash}"));
            await hashCmd.ExecuteNonQueryAsync();

            await UpdateStoredSummaryAsync(connection, runId, summary);
            return runId;
        }

        // ─── Preview / Clone ──────────────────────────────────────────────────

        private static void ApplyBrowserPreview(Rule66ValidationSummary summary)
        {
            var rows = summary.ReviewRows;
            if (rows.Count <= BrowserPreviewRowLimit) { summary.DisplayedCount = rows.Count; summary.IsPreviewOnly = false; summary.PreviewLimit = 0; return; }

            var failRows = rows.Where(r => string.Equals(r.ValidationResult, "FAIL", StringComparison.OrdinalIgnoreCase)).OrderBy(r => r.ValidationNumber).ToList();
            var passRows = rows.Where(r => !string.Equals(r.ValidationResult, "FAIL", StringComparison.OrdinalIgnoreCase)).OrderBy(r => r.ValidationNumber).ToList();

            int halfLimit = BrowserPreviewRowLimit / 2;
            int failTake  = Math.Min(failRows.Count, passRows.Count > 0 ? halfLimit : BrowserPreviewRowLimit);
            int passTake  = Math.Min(passRows.Count, BrowserPreviewRowLimit - failTake);

            var preview = failRows.Take(failTake).Concat(passRows.Take(passTake)).ToList();
            summary.ReviewRows     = preview;
            summary.DisplayedCount = preview.Count;
            summary.IsPreviewOnly  = summary.TotalValidated > preview.Count;
            summary.PreviewLimit   = preview.Count;
        }

        private static Rule66ValidationSummary CloneSummary(Rule66ValidationSummary s) => new()
        {
            Success = s.Success, StudRecordCount = s.StudRecordCount, StudNsfasCount = s.StudNsfasCount, CregRecordCount = s.CregRecordCount,
            TotalValidated = s.TotalValidated, DisplayedCount = s.DisplayedCount, IsPreviewOnly = s.IsPreviewOnly, PreviewLimit = s.PreviewLimit,
            PassCount = s.PassCount, FailCount = s.FailCount, ExceptionRate = s.ExceptionRate, Status = s.Status,
            Timestamp = s.Timestamp, Database = s.Database, StudTable = s.StudTable, CregTable = s.CregTable,
            StudStudentNoCol = s.StudStudentNoCol, CregStudentNoCol = s.CregStudentNoCol,
            FundingSourceCol = s.FundingSourceCol, FundingSourceValues = s.FundingSourceValues,
            TableLinkageText = s.TableLinkageText, RuleModeText = s.RuleModeText, ProcedureSteps = s.ProcedureSteps.ToList(),
            ClientId = s.ClientId, SavedRunId = s.SavedRunId,
            ControlSummaries = s.ControlSummaries.Select(i => new Rule66ControlSummaryItemViewModel
            {
                ControlType = i.ControlType, ControlLabel = i.ControlLabel, CriteriaText = i.CriteriaText,
                TotalCount = i.TotalCount, PassCount = i.PassCount, FailCount = i.FailCount, Status = i.Status
            }).ToList(),
            ReviewRows = s.ReviewRows.Select(r => new Rule66ValidationRowRecord
            {
                ValidationNumber = r.ValidationNumber, ControlType = r.ControlType, ControlLabel = r.ControlLabel,
                ValidationResult = r.ValidationResult, ValidationExplanation = r.ValidationExplanation,
                DisplayValues = new Dictionary<string, string?>(r.DisplayValues, StringComparer.OrdinalIgnoreCase)
            }).ToList(),
            Warning = s.Warning, Error = s.Error
        };

        private static Rule66ValidationRequest CloneRequest(Rule66ValidationRequest r) => new()
        {
            ClientId = r.ClientId, RunId = r.RunId, Server = r.Server, Database = r.Database, Driver = r.Driver,
            StudTable = r.StudTable, CregTable = r.CregTable, StudStudentNoCol = r.StudStudentNoCol, CregStudentNoCol = r.CregStudentNoCol,
            FundingSourceCol = r.FundingSourceCol, FundingSourceValues = r.FundingSourceValues
        };

        private static bool RequestsMatch(Rule66ValidationRequest a, Rule66ValidationRequest b) =>
            a.ClientId == b.ClientId &&
            string.Equals(a.Server?.Trim(), b.Server?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.Database?.Trim(), b.Database?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.StudTable?.Trim(), b.StudTable?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.CregTable?.Trim(), b.CregTable?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.FundingSourceCol?.Trim(), b.FundingSourceCol?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.FundingSourceValues?.Trim(), b.FundingSourceValues?.Trim(), StringComparison.OrdinalIgnoreCase);

        private static List<Rule66ValidationRowRecord> NormalizeRows(IEnumerable<Rule66ValidationRowRecord> rows) =>
            rows.Select((r, i) => { r.ValidationNumber = i + 1; return r; }).ToList();

        // ─── Expand Saved Summary ─────────────────────────────────────────────

        private async Task<Rule66ValidationSummary> ExpandSavedSummaryIfNeededAsync(Rule66ValidationSummary summary, string? server)
        {
            if (!summary.IsPreviewOnly && summary.ReviewRows.Count >= summary.TotalValidated) return summary;
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(summary.Database) || string.IsNullOrWhiteSpace(summary.StudTable) || string.IsNullOrWhiteSpace(summary.CregTable)) return summary;
            try
            {
                var expanded = await AnalyseAsync(new Rule66ValidationRequest
                {
                    ClientId = summary.ClientId, RunId = summary.SavedRunId, Server = server, Database = summary.Database,
                    Driver = "ODBC Driver 17 for SQL Server", StudTable = summary.StudTable, CregTable = summary.CregTable,
                    StudStudentNoCol = summary.StudStudentNoCol, CregStudentNoCol = summary.CregStudentNoCol,
                    FundingSourceCol = summary.FundingSourceCol, FundingSourceValues = summary.FundingSourceValues
                }, includeAllReviewRows: true);
                expanded.Timestamp = string.IsNullOrWhiteSpace(summary.Timestamp) ? expanded.Timestamp : summary.Timestamp;
                expanded.ClientId  = summary.ClientId;
                expanded.SavedRunId = summary.SavedRunId;
                return expanded;
            }
            catch { return summary; }
        }

        private async Task<Rule66ValidationSummary> ExpandAndPersistSavedSummaryIfNeededAsync(SqlConnection connection, int runId, Rule66ValidationSummary summary, string? server)
        {
            var expanded = await ExpandSavedSummaryIfNeededAsync(summary, server);
            if (!ReferenceEquals(expanded, summary)) { expanded.SavedRunId = runId; await UpdateStoredSummaryAsync(connection, runId, expanded); }
            return expanded;
        }

        // ─── DB Helpers ───────────────────────────────────────────────────────

        private static void ValidateRequest(Rule66ValidationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Server)) throw new InvalidOperationException("Server name is required.");
            if (string.IsNullOrWhiteSpace(request.Database)) throw new InvalidOperationException("Database is required.");
            if (string.IsNullOrWhiteSpace(request.StudTable)) throw new InvalidOperationException("STUD table is required.");
            if (string.IsNullOrWhiteSpace(request.CregTable)) throw new InvalidOperationException("CREG table is required.");
            ValidateObjectName(request.StudTable); ValidateObjectName(request.CregTable);
            if (!string.IsNullOrWhiteSpace(request.FundingSourceCol)) ValidateObjectName(request.FundingSourceCol);
        }

        private static void ValidateRequest(Rule66VerifyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Server)) throw new InvalidOperationException("Server name is required.");
            if (string.IsNullOrWhiteSpace(request.Database)) throw new InvalidOperationException("Database is required.");
            if (string.IsNullOrWhiteSpace(request.StudTable)) throw new InvalidOperationException("STUD table is required.");
            if (string.IsNullOrWhiteSpace(request.CregTable)) throw new InvalidOperationException("CREG table is required.");
            ValidateObjectName(request.StudTable); ValidateObjectName(request.CregTable);
            if (!string.IsNullOrWhiteSpace(request.FundingSourceCol)) ValidateObjectName(request.FundingSourceCol);
        }

        private static List<string> ParseFilterValues(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(SanitiseFilterValue)
                .Where(v => v.Length > 0)
                .Select(v => v.ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string SanitiseFilterValue(string value) =>
            value.Replace("'", "").Replace(";", "").Replace("--", "").Replace("[", "").Replace("]", "").Trim();

        private static string BuildFundingFilterSql(string fundingCol, IReadOnlyList<string> fundingValues)
        {
            if (fundingValues.Count == 0) return "";
            var list = string.Join(",", fundingValues.Select(v => $"'{v}'"));
            return $"AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(255), S.[{fundingCol}])))) IN ({list})";
        }

        private static string BuildInClauseSql(IReadOnlyList<string> values) =>
            string.Join(",", values.Select(v => $"'{v}'"));

        private static void ValidateObjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Table or column name is required.");
            foreach (var bad in new[] { ";", "'", "\"", "--", "/*", "*/" })
                if (value.Contains(bad, StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe table or column name.");
        }

        private static string? FindFirst(IEnumerable<string> values, string[] exactMatches, string[] containsMatches)
        {
            foreach (var exact in exactMatches)
            {
                var match = values.FirstOrDefault(c => c.Equals(exact, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match)) return match;
            }
            foreach (var fragment in containsMatches)
            {
                var match = values.FirstOrDefault(c => c.Contains(fragment, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match)) return match;
            }
            return values.FirstOrDefault();
        }

        private static string Sanitise(string name) => name.Replace("]", "").Replace("[", "").Replace("'", "").Replace(";", "").Trim();
        private static string BuildConnectionString(string server, string database, string driver) =>
            $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;Connection Timeout=180;";
        private static string ReadValue(Dictionary<string, string?> d, string key) =>
            d.TryGetValue(key, out var v) ? v ?? "" : "";

        private async Task<SqlConnection> OpenSystemConnectionAsync()
        {
            var server   = _configuration["SystemDatabase:Server"] ?? @"(localdb)\MSSQLLocalDB";
            var database = _configuration["SystemDatabase:Name"] ?? "HEMISBaseSystem";
            var trust    = _configuration.GetValue("SystemDatabase:TrustServerCertificate", true);
            var builder  = new SqlConnectionStringBuilder { DataSource = server, InitialCatalog = database, IntegratedSecurity = true, TrustServerCertificate = trust, Encrypt = false, ConnectTimeout = 180 };
            var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static async Task<int> CountAsync(SqlConnection connection, string sql)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = sql;
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private static async Task<int?> GetClientIdForRunAsync(SqlConnection connection, int runId)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = "SELECT TOP 1 ClientID FROM dbo.ValidationRuns WHERE RunID=@RunID;";
            cmd.Parameters.AddWithValue("@RunID", runId);
            var val = await cmd.ExecuteScalarAsync();
            return val == null || val == DBNull.Value ? null : Convert.ToInt32(val);
        }

        private static async Task<int?> GetSystemUserIdByEmailAsync(SqlConnection connection, string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = "SELECT TOP 1 UserID FROM dbo.Users WHERE Email=@Email;";
            cmd.Parameters.AddWithValue("@Email", email);
            var val = await cmd.ExecuteScalarAsync();
            return val == null || val == DBNull.Value ? null : Convert.ToInt32(val);
        }

        private static async Task<string?> GetEngagementRoleAsync(SqlConnection connection, int clientId, int userId)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = "SELECT TOP 1 EngagementRole FROM dbo.UserClientAssignments WHERE ClientID=@ClientID AND UserID=@UserID;";
            cmd.Parameters.AddWithValue("@ClientID", clientId); cmd.Parameters.AddWithValue("@UserID", userId);
            var val = await cmd.ExecuteScalarAsync();
            return val == null || val == DBNull.Value ? null : Convert.ToString(val);
        }

        private static async Task<bool> IsWorkspaceSavedAsync(SqlConnection connection, int runId)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = @"SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ValidationRuns WHERE RunID=@RunID AND (WorkspaceSavedAt IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.ReviewSignoffs rs WHERE rs.RunID=ValidationRuns.RunID AND rs.SignoffRole='DataAnalyst'))) THEN 1 ELSE 0 END;";
            cmd.Parameters.AddWithValue("@RunID", runId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
        }

        private static async Task<bool> HasSignoffRoleAsync(SqlConnection connection, int runId, string signoffRole)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ReviewSignoffs WHERE RunID=@RunID AND SignoffRole=@SignoffRole) THEN 1 ELSE 0 END;";
            cmd.Parameters.AddWithValue("@RunID", runId); cmd.Parameters.AddWithValue("@SignoffRole", signoffRole);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
        }

        private static async Task<string?> GetValidationRecordHashAsync(SqlConnection connection, int runId)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = "SELECT TOP 1 RecordHash FROM dbo.ValidationRuns WHERE RunID=@RunID;";
            cmd.Parameters.AddWithValue("@RunID", runId);
            var val = await cmd.ExecuteScalarAsync();
            return val == null || val == DBNull.Value ? null : Convert.ToString(val);
        }

        private static async Task<string?> GetLatestValidationHashAsync(SqlConnection connection, int clientId, int ruleNumber)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = "SELECT TOP 1 RecordHash FROM dbo.ValidationRuns WHERE ClientID=@ClientID AND RuleNumber=@RuleNumber AND RecordHash IS NOT NULL ORDER BY RunTimestamp DESC, RunID DESC;";
            cmd.Parameters.AddWithValue("@ClientID", clientId); cmd.Parameters.AddWithValue("@RuleNumber", ruleNumber);
            var val = await cmd.ExecuteScalarAsync();
            return val == null || val == DBNull.Value ? null : Convert.ToString(val);
        }

        private async Task EnsureClientNotArchivedAsync(SqlConnection connection, int clientId)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = "SELECT TOP 1 Status FROM dbo.Clients WHERE ClientID=@ClientID;";
            cmd.Parameters.AddWithValue("@ClientID", clientId);
            if (string.Equals(Convert.ToString(await cmd.ExecuteScalarAsync()), "Archived", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Archived engagements are read-only.");
        }

        private static async Task<int> ClearSignoffsAndFlagForReviewAsync(SqlConnection connection, int runId)
        {
            await using var countCmd = connection.CreateConfiguredCommand();
            countCmd.CommandText = "SELECT COUNT(1) FROM dbo.ReviewSignoffs WHERE RunID=@RunID;";
            countCmd.Parameters.AddWithValue("@RunID", runId);
            var existing = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            await using var delCmd = connection.CreateConfiguredCommand();
            delCmd.CommandText = "DELETE FROM dbo.ReviewSignoffs WHERE RunID=@RunID;";
            delCmd.Parameters.AddWithValue("@RunID", runId);
            await delCmd.ExecuteNonQueryAsync();
            await using var updCmd = connection.CreateConfiguredCommand();
            updCmd.CommandText = "UPDATE dbo.ValidationRuns SET Status='Needs Review' WHERE RunID=@RunID;";
            updCmd.Parameters.AddWithValue("@RunID", runId);
            await updCmd.ExecuteNonQueryAsync();
            return existing;
        }

        private async Task UpdateRunStatusFromSignoffsAsync(SqlConnection connection, int runId)
        {
            var hasAll = await HasAllRequiredSignoffsAsync(connection, runId);
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = "UPDATE dbo.ValidationRuns SET Status=@Status WHERE RunID=@RunID;";
            cmd.Parameters.AddWithValue("@RunID", runId); cmd.Parameters.AddWithValue("@Status", hasAll ? "Reviewed and Completed" : "Needs Review");
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<bool> HasAllRequiredSignoffsAsync(SqlConnection connection, int runId)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = @"SELECT
                CASE WHEN EXISTS (SELECT 1 FROM dbo.ReviewSignoffs WHERE RunID=@RunID AND SignoffRole='DataAnalyst') THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.ReviewSignoffs WHERE RunID=@RunID AND SignoffRole='Manager') THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.ReviewSignoffs WHERE RunID=@RunID AND SignoffRole='Director') THEN 1 ELSE 0 END;";
            cmd.Parameters.AddWithValue("@RunID", runId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return false;
            return reader.GetInt32(0) == 1 && reader.GetInt32(1) == 1 && reader.GetInt32(2) == 1;
        }

        private async Task MarkPreviousRunsHistoricalAsync(SqlConnection connection, int clientId, int ruleNumber)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = "UPDATE dbo.ValidationRuns SET IsCurrent=0 WHERE ClientID=@ClientID AND RuleNumber=@RuleNumber AND IsCurrent=1;";
            cmd.Parameters.AddWithValue("@ClientID", clientId); cmd.Parameters.AddWithValue("@RuleNumber", ruleNumber);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<List<RunSignoffViewModel>> GetRunSignoffsAsync(SqlConnection connection, int runId, int? currentUserId)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = @"
SELECT rs.SignoffID, ISNULL(rs.SignoffRole,'') AS SignoffRole,
       LTRIM(RTRIM(ISNULL(u.FirstName,'')+' '+ISNULL(u.LastName,''))) AS ReviewerName,
       ISNULL(u.Email,'') AS ReviewerEmail, ISNULL(rs.Comment,'') AS Comment,
       rs.SignedOffAt,
       CASE WHEN @CurrentUserID IS NOT NULL AND rs.ReviewerID=@CurrentUserID THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsCurrentUser
FROM dbo.ReviewSignoffs rs
INNER JOIN dbo.Users u ON u.UserID=rs.ReviewerID
WHERE rs.RunID=@RunID
ORDER BY CASE ISNULL(rs.SignoffRole,'') WHEN 'DataAnalyst' THEN 1 WHEN 'Manager' THEN 2 WHEN 'Director' THEN 3 ELSE 4 END, rs.SignedOffAt DESC;";
            cmd.Parameters.AddWithValue("@RunID", runId);
            cmd.Parameters.AddWithValue("@CurrentUserID", currentUserId.HasValue ? currentUserId.Value : DBNull.Value);
            await using var reader = await cmd.ExecuteReaderAsync();
            var signoffs = new List<RunSignoffViewModel>();
            while (await reader.ReadAsync())
                signoffs.Add(new RunSignoffViewModel
                {
                    Id = reader.GetInt32(0), SignoffRole = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    ReviewerName = reader.IsDBNull(2) ? "" : reader.GetString(2), ReviewerEmail = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Comment = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    SignedOffAt = reader.IsDBNull(5) ? DateTime.UtcNow : reader.GetDateTime(5),
                    IsCurrentUser = !reader.IsDBNull(6) && reader.GetBoolean(6)
                });
            return signoffs;
        }

        private static async Task UpdateStoredSummaryAsync(SqlConnection connection, int runId, Rule66ValidationSummary summary)
        {
            await using var cmd = connection.CreateConfiguredCommand();
            cmd.CommandText = "UPDATE dbo.ValidationRuns SET ResultsJSON=@ResultsJSON WHERE RunID=@RunID;";
            cmd.Parameters.AddWithValue("@RunID", runId);
            cmd.Parameters.AddWithValue("@ResultsJSON", ValidationPayloadCodec.Encode(JsonConvert.SerializeObject(summary)));
            await cmd.ExecuteNonQueryAsync();
        }

        private static bool CanSignOffAsRole(string? role) =>
            string.Equals(role, "DataAnalyst", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase);

        private static Rule66ValidationSummary? DeserializeSummary(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { var decoded = ValidationPayloadCodec.Decode(json); return string.IsNullOrWhiteSpace(decoded) ? null : JsonConvert.DeserializeObject<Rule66ValidationSummary>(decoded); }
            catch { return null; }
        }

        private static int GetInt(SqlDataReader reader, int ordinal) =>
            reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));

        private static string ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));
        }
    }
}
