# OData Endpoints for DevMetrics

This document describes how to use the OData endpoints exposed by the MetricsService API to query your metrics data.

## Base URL

The OData endpoints are available at: `{base_url}/{tenant}/odata`

For example, if your service is running on `https://localhost:5001`, the OData base URL for the "tenant-1" tenant would be:
`https://localhost:5001/tenant-1/odata/`

## Available Entity Sets

The following entities are exposed as OData endpoints:

### 1. PRMetricsEx
- **Endpoint**: `/{tenant}/odata/PRMetricsEx`
- **Description**: Pull request metrics including metadata, timelines, and related entities
- **Relationships**: Includes Team, DevExMetrics, Contributors, ReviewerMetrics, and CopilotReviewMetrics
- **Example**: `/{tenant-id}/odata/PRMetricsEx`

### 2. CopilotReviewMetrics
- **Endpoint**: `/{tenant}/odata/CopilotReviewMetrics`
- **Description**: GitHub Copilot review statistics
- **Relationships**: Includes parent PRMetric
- **Additional Methods**:
  - `GET /{tenant}/odata/CopilotReviewMetricsForAllTeams?start={date}&end={date}` - Get Copilot reviewer metrics for all teams within a date range
- **Example**: `/{tenant-id}/odata/CopilotReviewMetrics`

### 3. Teams
- **Endpoint**: `/{tenant}/odata/Teams`
- **Description**: List of all teams with their metadata
- **Query Support**: Full OData query support ($filter, $orderby, $select, etc.)
- **Example**: `/{tenant-id}/odata/Teams`

### 4. Sprints
- **Endpoint**: `/{tenant}/odata/Sprints`
- **Description**: Sprint calendar information including release numbers, sprint numbers, and date ranges
- **Query Support**: Full OData query support
- **Example**: `/{tenant-id}/odata/Sprints`

### 5. Contributors
- **Endpoint**: `/{tenant}/odata/Contributors`
- **Description**: Pull request contributors with their contribution statistics
- **Query Support**: Full OData query support
- **Example**: `/{tenant-id}/odata/Contributors`

### 6. ReviewerSprintMetrics
- **Endpoint**: `/{tenant}/odata/ReviewerSprintMetrics`
- **Description**: Reviewer metrics aggregated by sprint
- **Query Support**: Full OData query support
- **Example**: `/{tenant-id}/odata/ReviewerSprintMetrics`

### 7. ReviewerMonthlyMetrics
- **Endpoint**: `/{tenant}/odata/ReviewerMonthlyMetrics`
- **Description**: Reviewer metrics aggregated by month
- **Query Support**: Full OData query support
- **Example**: `/{tenant-id}/odata/ReviewerMonthlyMetrics`

## Author Metrics Endpoints

The AuthorMetrics controller provides specialized endpoints for querying individual and team-level author metrics:

### Individual Author Metrics

#### Get Author Metrics by Sprint
- **Endpoint**: `GET /{tenant}/odata/AuthorMetrics/Sprint?author={author}&year={year}&sprintNumber={sprintNumber}`
- **Description**: Gets metrics for a specific author during a sprint
- **Parameters**:
  - `author`: Author's login (required)
  - `year`: Year of the sprint (required)
  - `sprintNumber`: Sprint number (required)
- **Example**: 
  ```
  GET /{tenant-id}/odata/AuthorMetrics/Sprint?author=john.doe&year=2025&sprintNumber=20
  ```

#### Get Author Metrics by Month
- **Endpoint**: `GET /{tenant}/odata/AuthorMetrics/Month?author={author}&year={year}&month={month}`
- **Description**: Gets metrics for a specific author during a month
- **Parameters**:
  - `author`: Author's login (required)
  - `year`: Year (required)
  - `month`: Month number 1-12 (required)
- **Example**: 
  ```
  GET /{tenant-id}/odata/AuthorMetrics/Month?author=john.doe&year=2025&month=10
  ```

### Team Author Metrics

#### Get Team Author Metrics by Sprint
- **Endpoint**: `GET /{tenant}/odata/TeamAuthorMetrics/Sprint?team={team}&year={year}&sprintNumber={sprintNumber}`
- **Description**: Gets metrics for all authors in a team during a sprint
- **Parameters**:
  - `team`: Team name (required)
  - `year`: Year of the sprint (required)
  - `sprintNumber`: Sprint number (required)
- **OData Support**: Supports $filter, $orderby, $top, $skip, $select
- **Example**: 
  ```
  GET /{tenant-id}/odata/TeamAuthorMetrics/Sprint?team=Helios&year=2025&sprintNumber=20&$orderby=PrsAuthored desc
  ```

#### Get Team Author Metrics by Month
- **Endpoint**: `GET /{tenant}/odata/TeamAuthorMetrics/Month?team={team}&year={year}&month={month}`
- **Description**: Gets metrics for all authors in a team during a month
- **Parameters**:
  - `team`: Team name (required)
  - `year`: Year (required)
  - `month`: Month number 1-12 (required)
- **OData Support**: Supports $filter, $orderby, $top, $skip, $select
- **Example**: 
  ```
  GET /{tenant-id}/odata/TeamAuthorMetrics/Month?team=Helios&year=2025&month=10&$top=10
  ```

## OData Query Examples

### Basic Queries

#### Get all PR metrics:
```
GET /{tenant-id}/odata/PRMetricsEx
```

#### Get specific PR metric by ID:
```
GET /{tenant-id}/odata/PRMetricsEx(123)
```

#### Get specific PR metric by team:
```
GET /{tenant-id}/odata/PRMetricsEx(Oxygen)
```

#### Get all teams:
```
GET /{tenant-id}/odata/Teams
```

### Filtering ($filter)

#### Get PRs for a specific repository:
```
GET /{tenant-id}/odata/PRMetricsEx?$filter=Repository eq 'your-github-org/repo-1'
```

#### Get PRs created after a specific date:
```
GET /{tenant-id}/odata/PRMetricsEx?$filter=CreatedAt gt 2024-01-01T00:00:00Z
```

#### Get PRs by author:
```
GET /{tenant-id}/odata/PRMetricsEx?$filter=Author eq 'john.doe'
```

#### Get merged PRs:
```
GET /{tenant-id}/odata/PRMetricsEx?$filter=State eq 'merged'
```

#### Get PRs with more than 10 comments:
```
GET /{tenant-id}/odata/PRMetricsEx?$filter=TotalComments gt 10
```

#### Complex filter with multiple conditions:
```
GET /{tenant-id}/odata/PRMetricsEx?$filter=Repository eq 'your-github-org/repo-1' and State eq 'merged' and CreatedAt gt 2024-01-01T00:00:00Z
```

### Selecting Specific Fields ($select)

#### Get only specific fields:
```
GET /{tenant-id}/odata/PRMetricsEx?$select=PrNumber,Author,Repository,CreatedAt,State
```

### Sorting ($orderby)

#### Sort by creation date (newest first):
```
GET /{tenant-id}/odata/PRMetricsEx?$orderby=CreatedAt desc
```

#### Sort by multiple fields:
```
GET /{tenant-id}/odata/PRMetricsEx?$orderby=Repository,CreatedAt desc
```

### Expanding Related Data ($expand)

#### Get PR metrics with team information:
```
GET /{tenant-id}/odata/PRMetricsEx?$expand=Team
```

#### Get PR metrics with all related data:
```
GET /{tenant-id}/odata/PRMetricsEx?$expand=Team,Contributors,ReviewerMetrics($expand=PRReviewer),CopilotReviewMetrics
```

#### Get team with their metrics:
```
GET /{tenant-id}/odata/Teams?$expand=Metrics
```

### Pagination ($top, $skip)

#### Get first 50 records:
```
GET /{tenant-id}/odata/PRMetrics?$top=50
```

#### Get records 51-100 (pagination):
```
GET /{tenant-id}/odata/PRMetrics?$top=50&$skip=50
```

### Counting Records ($count)

#### Get total count of PR metrics:
```
GET /{tenant-id}/odata/PRMetrics/$count
```

#### Get count with filter:
```
GET /{tenant-id}/odata/PRMetrics/$count?$filter=State eq 'merged'
```

#### Include count in response:
```
GET /{tenant-id}/odata/PRMetrics?$count=true&$top=10
```

### Complex Query Examples

#### Get recent merged PRs with team and contributor info:
```
GET /{tenant-id}/odata/PRMetricsEx?$filter=State eq 'merged' and CreatedAt gt 2024-11-01T00:00:00Z&$expand=Team,Contributors&$orderby=CreatedAt desc&$top=20
```

#### Get PR metrics for specific team:
```
GET /{tenant-id}/odata/PRMetricsEx?$filter=Team/Name eq 'Engineering'&$expand=Team
```

#### Get reviewer metrics by sprint for a specific year:
```
GET /{tenant-id}/odata/ReviewerSprintMetrics?$filter=Year eq 2025&$orderby=SprintNumber desc
```

#### Get reviewer monthly metrics for a specific repository:
```
GET /{tenant-id}/odata/ReviewerMonthlyMetrics?$filter=Repository eq 'your-github-org/repo-1' and Year eq 2025&$orderby=Month desc
```

#### Get top contributors by lines of code:
```
GET /{tenant-id}/odata/Contributors?$orderby=LOC desc&$top=10
```

#### Get all sprints for a specific year:
```
GET /{tenant-id}/odata/Sprints?$filter=Year eq 2025&$orderby=SprintNumber
```

#### Get teams by value stream:
```
GET /{tenant-id}/odata/Teams?$filter=ValueStream eq 'Platform'
```

#### Get Copilot review metrics with high file coverage:
```
GET /{tenant-id}/odata/CopilotReviewMetrics?$filter=FilesReviewed gt 5&$orderby=FilesReviewed desc
```

#### Get team author metrics with filtering:
```
GET /{tenant-id}/odata/TeamAuthorMetrics/Sprint?team=Helios&year=2025&sprintNumber=20&$filter=PrsAuthored gt 5&$orderby=AvgPRSize desc
```

#### Get individual author metrics across multiple sprints:
```
GET /{tenant-id}/odata/AuthorMetrics/Sprint?author=john.doe&year=2025&sprintNumber=20
GET /{tenant-id}/odata/AuthorMetrics/Sprint?author=john.doe&year=2025&sprintNumber=21
```

#### Get copilot metrics for all teams in a date range:
```
GET /{tenant-id}/odata/CopilotReviewMetricsForAllTeams?start=2025-09-24T00:00:00Z&end=2025-10-07T23:59:59Z
```

## Advanced Features

### Functions and Actions
OData supports custom functions and actions. These can be added to extend the API with complex operations.

### Metadata
Get the OData metadata document:
```
GET /{tenant-id}/odata/$metadata
```

### Service Document
Get the service document:
```
GET /{tenant-id}/odata/
```

## Response Format

All responses are in JSON format by default. You can request other formats by setting the `Accept` header:

- JSON: `Accept: application/json`
- XML: `Accept: application/xml`

## Error Handling

OData returns standard HTTP status codes:
- `200 OK`: Successful query
- `400 Bad Request`: Invalid query syntax
- `404 Not Found`: Entity not found
- `500 Internal Server Error`: Server error

Error responses include details about what went wrong:

```json
{
  "error": {
    "code": "400",
    "message": "Invalid filter expression"
  }
}
```

## Best Practices

1. **Use filtering**: Always filter data to reduce response size and improve performance
2. **Select only needed fields**: Use `$select` to reduce bandwidth
3. **Implement pagination**: Use `$top` and `$skip` for large datasets
4. **Use expand judiciously**: Only expand related data when necessary
5. **Cache metadata**: The metadata document changes infrequently

## Example Use Cases

### Dashboard Data
Get recent team metrics for a dashboard:
```
GET /{tenant-id}/odata/PRMetricsEx?$filter=CreatedAt gt 2024-11-01T00:00:00Z and Team/Name eq 'Platform'&$expand=Team&$select=PrNumber,Author,CreatedAt,TotalLines,TotalComments&$orderby=CreatedAt desc&$top=10
```

### Report Generation
Get comprehensive data for reporting:
```
GET /{tenant-id}/odata/PRMetricsEx?$filter=CreatedAt ge 2024-10-01T00:00:00Z and CreatedAt le 2024-10-31T00:00:00Z&$expand=Team,Contributors,ReviewerMetrics&$orderby=CreatedAt
```

### Performance Analysis
Analyze review performance by sprint:
```
GET /{tenant-id}/odata/ReviewerSprintMetrics?$filter=Year eq 2025 and SprintNumber eq 20&$orderby=PrsReviewed desc&$top=20
```

Analyze review performance by month:
```
GET /{tenant-id}/odata/ReviewerMonthlyMetrics?$filter=Year eq 2025 and Month eq 10&$orderby=PrsReviewed desc
```

### Author Productivity Tracking
Get individual author metrics for a sprint:
```
GET /{tenant-id}/odata/AuthorMetrics/Sprint?author=john.doe&year=2025&sprintNumber=20
```

Get team productivity metrics for a month:
```
GET /{tenant-id}/odata/TeamAuthorMetrics/Month?team=Helios&year=2025&month=10&$orderby=PrsAuthored desc
```

### Sprint Planning
Get sprint information to align metrics:
```
GET /{tenant-id}/odata/Sprints?$filter=Year eq 2025&$orderby=SprintNumber
```

Get team author metrics for sprint retrospective:
```
GET /{tenant-id}/odata/TeamAuthorMetrics/Sprint?team=Helios&year=2025&sprintNumber=20&$select=Author,PrsAuthored,AvgPRSize,AvgCycleTime,ApprovalRate
```

### Copilot Usage Analysis
Get Copilot review metrics for all teams:
```
GET /{tenant-id}/odata/CopilotReviewMetricsForAllTeams?start=2025-09-01T00:00:00Z&end=2025-09-30T23:59:59Z
```

Get Copilot metrics with PR details:
```
GET /{tenant-id}/odata/CopilotReviewMetrics?$filter=FilesReviewed gt 0&$expand=PRMetric&$orderby=Comments desc
```

### Team Comparison
Compare multiple teams' author metrics:
```
GET /{tenant-id}/odata/TeamAuthorMetrics/Sprint?team=Helios&year=2025&sprintNumber=20
GET /{tenant-id}/odata/TeamAuthorMetrics/Sprint?team=Radon&year=2025&sprintNumber=20
```

### Contributor Analysis
Get top contributors across the organization:
```
GET /{tenant-id}/odata/Contributors?$orderby=LOC desc&$top=50&$select=Author,LOC,CommitCount,PRCount
```

This OData implementation provides a powerful, flexible way to query your metrics data using standard OData conventions.

## Power BI Integration

DevMetrics can be easily integrated with Power BI or Excel Power Query to create interactive dashboards and reports. Use the provided Power Query M language scripts to connect to the OData endpoints and import your metrics data.

### Prerequisites

- Power BI Desktop or Excel with Power Query
- Access to DevMetrics OData API endpoints
- API authentication - Supports Basic Authentication

### Setting Up Data Sources

#### 1. PRMetricsEx Data Connection

To import PR metrics data with team and copilot information:

1. Open Power BI Desktop or Excel Power Query
2. Go to **Get Data** > **Blank Query**
3. Open the **Advanced Editor**
4. Replace the default code with the following Power Query M script:

```m
let
    Tenant = "tenant-1",
    BaseUrl = "https://your-devexmetrics-server/" & Tenant & "/odata/PRMetricsEx",

    PageSize = 1000, 

    GetPage = (skip as number) as table =>
        let
            Url = BaseUrl & "?$top=" & Number.ToText(PageSize) &
                  "&$skip=" & Number.ToText(skip) &
                  "&$expand=Team,CopilotReviewMetrics",
            
            Response = Web.Contents(
                Url,
                [ Headers = [ Accept = "application/json;odata.metadata=minimal" ] ]
            ),
            Json = Json.Document(Response),
            Data = Json[value],
            TableOut = Table.FromList(Data, Splitter.SplitByNothing(), null, null, ExtraValues.Error)
        in
            TableOut,

    Pages = List.Generate(
        () => [Skip = 0, Data = GetPage(0)],
        each Table.RowCount([Data]) > 0,
        each [Skip = [Skip] + PageSize, Data = GetPage([Skip])],
        each [Data]
    ),

    // Combine all pages
    Combined = Table.Combine(Pages),
  #"Expanded Column1" = Table.ExpandRecordColumn(Combined, "Column1", {"Id", "PrNumber", "Author", "MergedBy", "Repository", "BaseBranch", "State", "MergedAt", "CreatedAt", "TotalComments", "TotalLines", "ChangedFiles", "WorkItemId", "WorkItemId2", "IsFeature", "DraftTransitions", "TotalCommits", "ApproveTime", "TotalReviewChangesRequested", "CodeExcellenceRequestedChanges", "TeamRequestedChanges", "OthersRequestedChanges", "CodingTime", "TotalSmallCommits", "TotalSmallCommitComments", "TotalMediumCommits", "TotalMediumCommitComments", "TotalLargeCommits", "TotalLargeCommitComments", "CycleTime", "LeadTime", "MaturityPercentage", "InitialLinesChanged", "SubsequentLinesChanged", "MergeTime", "PickupTime", "TotalReviewCommentsAfterFinalApproval", "ReviewTime", "PRSize", "AvgReviewCommentsPerCommit", "TotalReviewComments", "Team"}, {"Id", "PrNumber", "Author", "MergedBy", "Repository", "BaseBranch", "State", "MergedAt", "CreatedAt", "TotalComments", "TotalLines", "ChangedFiles", "WorkItemId", "WorkItemId2", "IsFeature", "DraftTransitions", "TotalCommits", "ApproveTime", "TotalReviewChangesRequested", "CodeExcellenceRequestedChanges", "TeamRequestedChanges", "OthersRequestedChanges", "CodingTime", "TotalSmallCommits", "TotalSmallCommitComments", "TotalMediumCommits", "TotalMediumCommitComments", "TotalLargeCommits", "TotalLargeCommitComments", "CycleTime", "LeadTime", "MaturityPercentage", "InitialLinesChanged", "SubsequentLinesChanged", "MergeTime", "PickupTime", "TotalReviewCommentsAfterFinalApproval", "ReviewTime", "PRSize", "AvgReviewCommentsPerCommit", "TotalReviewComments", "Team"}),
  #"Expanded Team" = Table.ExpandRecordColumn(#"Expanded Column1", "Team", {"Name", "Region", "ValueStream"}, {"Team.Name", "Team.Region", "Team.ValueStream"}),
  #"Expanded CopilotReviewMetrics" = Table.ExpandRecordColumn(#"Expanded Team", "CopilotReviewMetrics", {"FilesReviewed", "FilesChanged", "Comments"}, {"CopilotReviewMetrics.FilesReviewed","CopilotReviewMetrics.FilesChanged", "CopilotReviewMetrics.Comments"}),
in
    #"Expanded Team"
```
If you need to convert CreatedAt to a sprint number and add as a virtual column, add the below code to the above query.

```
#"Added Sprint" = Table.AddColumn(
        #"Expanded CopilotReviewMetrics", 
        "Sprint", 
        each 
            let
                createdDate = [CreatedAt],
                matchedSprint = Table.SelectRows(Sprints, each createdDate >= [StartDate] and createdDate <= [EndDate]),
                sprintName = if Table.RowCount(matchedSprint) > 0 then matchedSprint{0}[SprintNumber] else null
            in
                sprintName
    ),
```
Replace #"Expanded Team" with #"Added Sprint" and complete the query.

5. Name the query "PRMetricsEx" and click **Close & Apply**, when propmted select `Basic` authentication and use random username and api key as password.

#### 2. Sprint Data Connection

To import sprint information:

1. Create another **Blank Query**
2. Open the **Advanced Editor**
3. Use the following Power Query M script:

```m
let
    Tenant = "tenant-1",
    Source = Web.Contents("http://localhost:5100/" & Tenant & "/odata/Sprints", [
        Headers = [
            Accept = "application/json;odata.metadata=minimal"
        ]
    ]),
    Json = Json.Document(Source),
  Navigation = Json[value],
  #"Converted to table" = Table.FromList(Navigation, Splitter.SplitByNothing(), null, null, ExtraValues.Error),
  #"Expanded Column1" = Table.ExpandRecordColumn(#"Converted to table", "Column1", {"ReleaseNumber", "SprintNumber", "Year", "StartDate", "EndDate"}, {"ReleaseNumber", "SprintNumber", "Year", "StartDate", "EndDate"})
in
    #"Expanded Column1"
```

4. Name the query "Sprints" and click **Close & Apply**, when prompted select `Basic` authentication and use random username and api key as password.

#### 3. Teams Data Connection

To import team information:

1. Create another **Blank Query**
2. Open the **Advanced Editor**
3. Use the following Power Query M script:

```m
let
    Tenant = "tenant-1",
    Source = Web.Contents("http://localhost:5100/" & Tenant & "/odata/Teams", [
        Headers = [
            Accept = "application/json;odata.metadata=minimal"
        ]
    ]),
    Json = Json.Document(Source),
    Navigation = Json[value],
    #"Converted to table" = Table.FromList(Navigation, Splitter.SplitByNothing(), null, null, ExtraValues.Error),
    #"Expanded Column1" = Table.ExpandRecordColumn(#"Converted to table", "Column1", {"Name", "Region", "ValueStream"}, {"Name", "Region", "ValueStream"})
in
    #"Expanded Column1"
```

4. Name the query "Teams" and click **Close & Apply**.

#### 4. Reviewer Sprint Metrics Connection

To import reviewer metrics aggregated by sprint:

1. Create another **Blank Query**
2. Open the **Advanced Editor**
3. Use the following Power Query M script:

```m
let
    Tenant = "tenant-1",
    BaseUrl = "http://localhost:5100/" & Tenant & "/odata/ReviewerSprintMetrics",
    PageSize = 1000,

    GetPage = (skip as number) as table =>
        let
            Url = BaseUrl & "?$top=" & Number.ToText(PageSize) & "&$skip=" & Number.ToText(skip),
            Response = Web.Contents(Url, [ Headers = [ Accept = "application/json;odata.metadata=minimal" ] ]),
            Json = Json.Document(Response),
            Data = Json[value],
            TableOut = Table.FromList(Data, Splitter.SplitByNothing(), null, null, ExtraValues.Error)
        in
            TableOut,

    Pages = List.Generate(
        () => [Skip = 0, Data = GetPage(0)],
        each Table.RowCount([Data]) > 0,
        each [Skip = [Skip] + PageSize, Data = GetPage([Skip])],
        each [Data]
    ),

    Combined = Table.Combine(Pages),
    #"Expanded Column1" = Table.ExpandRecordColumn(Combined, "Column1", 
        {"SprintNumber", "Year", "Repository", "Reviewer", "TotalReviewDays", "PrsRequested", "PrsReviewed", 
         "PrsForApprovalRate", "PrsReviewedWithComments", "PrsCommented", "CommentCountWhenReviewed", 
         "ApprovalRatePercentage", "AverageCommentCount", "ReviewsRequested", "ReviewsSubmitted", 
         "Approved", "CommentCount", "ChangesRequested", "ReviewComments"}, 
        {"SprintNumber", "Year", "Repository", "Reviewer", "TotalReviewDays", "PrsRequested", "PrsReviewed", 
         "PrsForApprovalRate", "PrsReviewedWithComments", "PrsCommented", "CommentCountWhenReviewed", 
         "ApprovalRatePercentage", "AverageCommentCount", "ReviewsRequested", "ReviewsSubmitted", 
         "Approved", "CommentCount", "ChangesRequested", "ReviewComments"})
in
    #"Expanded Column1"
```

4. Name the query "ReviewerSprintMetrics" and click **Close & Apply**.

#### 5. Author Metrics by Team and Sprint

To import team author metrics for specific sprints:

1. Create another **Blank Query**
2. Open the **Advanced Editor**
3. Use the following Power Query M script (modify team, year, and sprint parameters as needed):

```m
let
    Tenant = "tenant-1",
    Team = "Helios",
    Year = 2025,
    SprintNumber = 20,
    
    Url = "http://localhost:5100/" & Tenant & "/odata/TeamAuthorMetrics/Sprint?team=" & Team & 
          "&year=" & Number.ToText(Year) & 
          "&sprintNumber=" & Number.ToText(SprintNumber),
    
    Source = Web.Contents(Url, [
        Headers = [
            Accept = "application/json;odata.metadata=minimal"
        ]
    ]),
    Json = Json.Document(Source),
    Navigation = Json[value],
    #"Converted to table" = Table.FromList(Navigation, Splitter.SplitByNothing(), null, null, ExtraValues.Error),
    #"Expanded Column1" = Table.ExpandRecordColumn(#"Converted to table", "Column1", 
        {"SprintNumber", "Year", "Author", "PrsAuthored", "AvgPRSize", "AvgCommentCount", 
         "AvgCycleTime", "AvgReviewComments", "ApprovalRate"}, 
        {"SprintNumber", "Year", "Author", "PrsAuthored", "AvgPRSize", "AvgCommentCount", 
         "AvgCycleTime", "AvgReviewComments", "ApprovalRate"})
in
    #"Expanded Column1"
```

4. Name the query "TeamAuthorMetrics" and click **Close & Apply**.

### Usage Instructions

#### For Power BI:
1. Follow the data connection steps above
2. Create relationships between tables if needed
3. Build visualizations using the imported metrics data
4. Set up scheduled refresh if connecting to live data

#### For Excel Power Query:
1. Open Excel and go to **Data** > **Get Data** > **From Other Sources** > **Blank Query**
2. Follow the same Power Query steps as Power BI
3. Load data to worksheet or data model
4. Create pivot tables and charts using the imported data

### Configuration Notes

- **Base URL**: Update the `BaseUrl` in the PRMetricsEx query to match your DevMetrics API endpoint
- **Sprint URL**: Update the Sprint endpoint URL to match your environment (dev/prod)
- **Page Size**: Adjust `PageSize` if you need different batch sizes for large datasets
- **Authentication**: Add Basic authentication headers
- **Refresh**: Set up automatic refresh schedules for up-to-date dashboards
