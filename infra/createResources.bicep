// common
targetScope = 'resourceGroup'

// parameters
////////////////////////////////////////////////////////////////////////////////

@allowed([
  'test'
])
@description('Environment for deployment')
param envName string

@description('Commit SHA that triggered this deployment.')
param githubCommitSha string

@description('Github run that triggered this deployment.')
param githubWorkflowRunId string

@description('Github ref (branch, tag) that triggered this deployment.')
param githubRef string

param resourceLocation string = resourceGroup().location

param prefix string = 'lockpaper'

// variables
////////////////////////////////////////////////////////////////////////////////

var suffix = toLower(envName)

// log analytics & app insights
var logAnalyticsWSName = '${prefix}-law-${suffix}'
var appInsightsName = '${prefix}-ai-${suffix}'

// query packs
var incidentManagementQueryPackName = '${prefix}-incident-management-${suffix}'

// tags
var resourceTags = {
  Product: prefix
  Environment: envName
  GithubCommitSha: githubCommitSha
  GithubWorkflowRunId: githubWorkflowRunId
  GithubRef: githubRef
}

// resources
////////////////////////////////////////////////////////////////////////////////

//
// log analytics & app insights
//

// log analytics workspace
resource resLogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWSName
  location: resourceLocation
  tags: resourceTags
  properties: {
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    sku: {
      name: 'PerGB2018' // pay as you go
    }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: 1
    }
  }
}

// app insights
resource resAppInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  tags: resourceTags
  location: resourceLocation
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: resLogAnalyticsWorkspace.id
  }
}

// query pack for incident management KQL queries
resource resIncidentManagementQueryPack 'Microsoft.OperationalInsights/querypacks@2019-09-01' = {
  name: incidentManagementQueryPackName
  location: resourceLocation
  tags: resourceTags
  properties: {}

  // exceptions in last 24 hours
  resource resQueryExceptionsLast24Hours 'queries@2019-09-01' = {
    name: 'a1b2c3d4-e5f6-4789-abcd-123456789abc'
    properties: {
      displayName: 'Exceptions in Last 24 Hours'
      description: 'Lists all exceptions with timestamp ordering in the last 24 hours'
      body: 'exceptions\n| where timestamp > ago(24h)\n| order by timestamp desc'
      related: {
        categories: [
          'applications'
        ]
        resourceTypes: [
          'microsoft.insights/components'
        ]
      }
      tags: {
        labels: [
          'incident-management'
        ]
      }
    }
  }

  // failed requests in last 24 hours
  resource resQueryFailedRequestsLast24Hours 'queries@2019-09-01' = {
    name: 'b2c3d4e5-f6a7-4890-bcde-234567890bcd'
    properties: {
      displayName: 'Failed Requests in Last 24 Hours'
      description: 'Shows failed HTTP requests (success == false) in the last 24 hours'
      body: 'requests\n| where timestamp > ago(24h) and success == false\n| order by timestamp desc'
      related: {
        categories: [
          'applications'
        ]
        resourceTypes: [
          'microsoft.insights/components'
        ]
      }
      tags: {
        labels: [
          'incident-management'
        ]
      }
    }
  }

  // errors in last 24 hours
  resource resQueryErrorsLast24Hours 'queries@2019-09-01' = {
    name: 'c3d4e5f6-a7b8-4901-cdef-345678901cde'
    properties: {
      displayName: 'Errors in Last 24 Hours'
      description: 'Displays traces with severity level 3 (errors) in the last 24 hours'
      body: 'traces\n| where timestamp > ago(24h) and severityLevel == 3\n| order by timestamp desc'
      related: {
        categories: [
          'applications'
        ]
        resourceTypes: [
          'microsoft.insights/components'
        ]
      }
      tags: {
        labels: [
          'incident-management'
        ]
      }
    }
  }

  // warnings in last 24 hours
  resource resQueryWarningsLast24Hours 'queries@2019-09-01' = {
    name: 'd4e5f6a7-b8c9-4012-defa-456789012def'
    properties: {
      displayName: 'Warnings in Last 24 Hours'
      description: 'Shows traces with severity level 2 (warnings) in the last 24 hours'
      body: 'traces\n| where timestamp > ago(24h) and severityLevel == 2\n| order by timestamp desc'
      related: {
        categories: [
          'applications'
        ]
        resourceTypes: [
          'microsoft.insights/components'
        ]
      }
      tags: {
        labels: [
          'incident-management'
        ]
      }
    }
  }

  // failed requests count by cloud role
  resource resQueryFailedRequestsByRole 'queries@2019-09-01' = {
    name: 'e5f6a7b8-c9da-4123-efab-567890123efa'
    properties: {
      displayName: 'Failed Requests Count by Cloud Role'
      description: 'Summarizes failed request counts grouped by cloud_RoleName in the last 24 hours'
      body: 'requests\n| where timestamp > ago(24h) and success == false\n| summarize Count = count() by cloud_RoleName\n| order by Count desc'
      related: {
        categories: [
          'applications'
        ]
        resourceTypes: [
          'microsoft.insights/components'
        ]
      }
      tags: {
        labels: [
          'incident-management'
        ]
      }
    }
  }

  // exceptions count by cloud role
  resource resQueryExceptionsByRole 'queries@2019-09-01' = {
    name: 'f6a7b8c9-daeb-4234-fabc-678901234fab'
    properties: {
      displayName: 'Exceptions Count by Cloud Role'
      description: 'Aggregates exception counts by service role in the last 24 hours'
      body: 'exceptions\n| where timestamp > ago(24h)\n| summarize Count = count() by cloud_RoleName\n| order by Count desc'
      related: {
        categories: [
          'applications'
        ]
        resourceTypes: [
          'microsoft.insights/components'
        ]
      }
      tags: {
        labels: [
          'incident-management'
        ]
      }
    }
  }

  // errors count by cloud role
  resource resQueryErrorsByRole 'queries@2019-09-01' = {
    name: 'a7b8c9da-ebfc-4345-abcd-789012345abc'
    properties: {
      displayName: 'Errors Count by Cloud Role'
      description: 'Groups error traces by cloud role name in the last 24 hours'
      body: 'traces\n| where timestamp > ago(24h) and severityLevel == 3\n| summarize Count = count() by cloud_RoleName\n| order by Count desc'
      related: {
        categories: [
          'applications'
        ]
        resourceTypes: [
          'microsoft.insights/components'
        ]
      }
      tags: {
        labels: [
          'incident-management'
        ]
      }
    }
  }

  // warnings count by cloud role
  resource resQueryWarningsByRole 'queries@2019-09-01' = {
    name: 'b8c9daeb-fcad-4456-bcde-890123456bcd'
    properties: {
      displayName: 'Warnings Count by Cloud Role'
      description: 'Summarizes warning counts per service role in the last 24 hours'
      body: 'traces\n| where timestamp > ago(24h) and severityLevel == 2\n| summarize Count = count() by cloud_RoleName\n| order by Count desc'
      related: {
        categories: [
          'applications'
        ]
        resourceTypes: [
          'microsoft.insights/components'
        ]
      }
      tags: {
        labels: [
          'incident-management'
        ]
      }
    }
  }
}
