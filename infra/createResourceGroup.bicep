// common
targetScope = 'subscription'

// parameters
////////////////////////////////////////////////////////////////////////////////

// common
@description('Rg for storage account, service bus, cosmos db & function app. Value is passed from GHA variable.')
param rgName string

@minLength(3)
@maxLength(6)
@description('A unique environment name (max 6 characters, alphanumeric only).')
param envName string

@description('Commit SHA that triggered this deployment.')
param githubCommitSha string

@description('Github run that triggered this deployment.')
param githubWorkflowRunId string

@description('Github ref (branch, tag) that triggered this deployment.')
param githubRef string

param prefix string = 'lock-paper'

// variables
////////////////////////////////////////////////////////////////////////////////

var rgLocation = 'southindia'

// tags
var rgTags = {
  Product: prefix
  Environment: envName
  GithubCommitSha: githubCommitSha
  GithubWorkflowRunId: githubWorkflowRunId
  GithubRef: githubRef
}

// resource groups
////////////////////////////////////////////////////////////////////////////////

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: rgName
  location: rgLocation
  tags: rgTags
}

// outputs
////////////////////////////////////////////////////////////////////////////////

output outputRgName string = rg.name
