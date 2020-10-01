# File Manager

The system supports several file operations:
- upload
- download
- list
- delete

## Simplifying Assumptions

- Does not support some common file operations:
  - duplicate
  - rename
  - folders/directories
- No authentication or authorization
- Disallow overwriting an existing file

## Design

### File Storage

Files are stored as blobs in an Azure Storage Account.

### REST API

The REST API is implemented in C# using ASP.NET Core 3.1.

### Deployment

Terraform is used to create the Azure resources (storage account, blob container, app service). Azure DevOps pipelines (one build, one release) deploy the web service to the Azure App Service.

### Discussion

There are several possible approaches to fulfilling the project requirements:

- an Azure Function App
- an Azure App Service
- the bare Azure Blob Storage REST API

With regard to storing files, it would be feasible to use various other repositories (e.g. a database, a mounted filesystem, etc.), but none of them match the performance, availability, and cost of Azure Blob Storage.

#### Azure Function App

A function app would be a good implementation since it scales as needed, and only a single endpoint is required. The potentially long connection times to upload or download a file could run up against Azure's hard timeout limits on function apps, so this approach is unsuitable for this particular purpose.

#### Azure App Service

An app service provides more flexibility than a function app, particularly regarding timeouts. It can still be automatically scaled to handle significant concurrency and load. Even though this is an extremely simple, single-controller ASP.NET Core app, the requirements and constraints make this the preferred approach. It is a nice bonus to be able to provide the Swagger UI for the provided REST API.

#### Azure Blob Storage REST API

The Azure Blob Storage REST API satisfies the requirements all on its own. It supports all of the required operations, can be configured to allow unauthenticated usage, and is by far the simplest and most performant solution. That said, it seems like it violates the spirit of the project.
