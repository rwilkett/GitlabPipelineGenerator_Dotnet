# GOCD API Client

A .NET client library for connecting to GOCD API version 20.1.0.

## Features

- Bearer token authentication
- Username/password authentication
- Retrieve pipeline objects
- Retrieve template objects
- Async/await support

## Usage

### Authentication with Bearer Token
```csharp
using var client = new GOCDApiClient("https://your-gocd-server.com", "your-bearer-token");
```

### Authentication with Username/Password
```csharp
using var client = new GOCDApiClient("https://your-gocd-server.com", "username", "password");
```

### Get Pipelines
```csharp
var pipelines = await client.GetPipelinesAsync();
var pipeline = await client.GetPipelineAsync("pipeline-name");
var history = await client.GetPipelineHistoryAsync("pipeline-name");
```

### Get Templates
```csharp
var templates = await client.GetTemplatesAsync();
var template = await client.GetTemplateAsync("template-name");
```

### Get Pipeline Groups
```csharp
var groups = await client.GetPipelineGroupsAsync();
var group = await client.GetPipelineGroupAsync("group-name");
```

### Get Pipeline Configuration
```csharp
var config = await client.GetPipelineConfigAsync("pipeline-name");
```