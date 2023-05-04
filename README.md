# azure-cognitive-transcribe-translate-demo

# Blazor Starter Application

This template contains an example .NET 7 [Blazor WebAssembly](https://docs.microsoft.com/aspnet/core/blazor/?view=aspnetcore-6.0#blazor-webassembly) client application, a .NET 7 C# [Azure Functions](https://docs.microsoft.com/azure/azure-functions/functions-overview), and a C# class library with shared code.

> Note: Azure Functions only supports .NET 7 in the isolated process execution model

## Getting Started

1. Create a repository from the [GitHub template](https://docs.github.com/en/enterprise/2.22/user/github/creating-cloning-and-archiving-repositories/creating-a-repository-from-a-template) and then clone it locally to your machine.

1. In the **ApiIsolated** folder, copy `local.settings.example.json` to `local.settings.json`

1. Continue using either Visual Studio or Visual Studio Code.

### Visual Studio 2022

Once you clone the project, open the solution in the latest release of [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with the Azure workload installed, and follow these steps:

1. Right-click on the solution and select **Set Startup Projects...**.

1. Select **Multiple startup projects** and set the following actions for each project:
    - *Api* - **Start**
    - *Client* - **Start**
    - *Shared* - None

1. Press **F5** to launch both the client application and the Functions API app.

### Visual Studio Code with Azure Static Web Apps CLI for a better development experience (Optional)

1. Install the [Azure Static Web Apps CLI](https://www.npmjs.com/package/@azure/static-web-apps-cli) and [Azure Functions Core Tools CLI](https://www.npmjs.com/package/azure-functions-core-tools).

1. Open the folder in Visual Studio Code.

1. Delete file `Client/wwwroot/appsettings.Development.json`

1. In the VS Code terminal, run the following command to start the Static Web Apps CLI, along with the Blazor WebAssembly client application and the Functions API app:

    ```bash
    swa start http://localhost:5000 --api-location http://localhost:7071
    ```

    The Static Web Apps CLI (`swa`) starts a proxy on port 4280 that will forward static site requests to the Blazor server on port 5000 and requests to the `/api` endpoint to the Functions server. 

1. Open a browser and navigate to the Static Web Apps CLI's address at `http://localhost:4280`. You'll be able to access both the client application and the Functions API app in this single address. When you navigate to the "Fetch Data" page, you'll see the data returned by the Functions API app.

1. Enter Ctrl-C to stop the Static Web Apps CLI.

## Template Structure

- **Client**: The Blazor WebAssembly sample application
- **Api**: A C# Azure Functions API, which the Blazor application will call
- **Shared**: A C# class library with a shared data model between the Blazor and Functions application

## Deploy to Azure Static Web Apps

This application can be deployed to [Azure Static Web Apps](https://docs.microsoft.com/azure/static-web-apps), to learn how, check out [our quickstart guide](https://aka.ms/blazor-swa/quickstart).
















# cognitive-search-demo

This is anapplication that demonstrates how to use Azure Cognitive Search to build a search experience for a web application.

## Infrastructure as Code

Developer checks in Infrastructure as Code **(*IaC*)** changes for infrastructure definitions in the bicep file this GitHub repository. This will trigger a GitHub Actions to kick off to compile and deploy the infrastructure into Azure.

![Infrastructure as Code DevOps Workflow](./docs/iac-devops-workflow.drawio.png)

## DevOps Flow

Developer checks in code changes for the application into the GitHub repository. This will trigger a GitHub Actions to kick off to compile and deploy the application into Azure.

![DevOps Flow](./docs/devops-flow.drawio.png)

## Cloud Infrastructure

The file [README.md](./bicep/README.md) will explain how to run commands to initiate the deployment of the infrastructure into Azure. Here is an overview of what will be built with this repository:

![Infrastructure](./docs/infrastructure.v2.drawio.png)

## Components

- [GitHub](https://github.com/) is a code-hosting platform for version control and collaboration. A GitHub source-control [repository](https://docs.github.com/github/creating-cloning-and-archiving-repositories/about-repositories) contains all project files and their revision history. Developers can work together to contribute, discuss, and manage code in the repository.
- [GitHub Actions](https://github.com/features/actions) provides a suite of build and release workflows that covers continuous integration (CI), automated testing, and container deployments.
- [Azure Static Web Apps](https://azure.microsoft.com/services/app-service/static) is a fully managed app service that enables you to build and deploy full-stack web apps directly from a GitHub repository. The service builds and deploys your app, and provides a global content delivery network (CDN) for your static content.
- [App Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview) is a feature of Azure Monitor that provides a rich set of analytics tools to help you monitor your application's health and performance. It can automatically detect common problems and includes powerful analytics tools to help you diagnose issues and to understand what users actually do with your app.
- [Azure Functions](https://azure.microsoft.com/services/functions) is a serverless compute service that lets you run code on-demand without having to explicitly provision or manage infrastructure. Azure Functions can be used to extend other Azure services or to build your own back-end services. It enables you to write event-driven serverless code, maintain less infrastructure, and save money.
  - [Service Bus trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus-trigger) enable you to respond to an event sent to an service bus message.
  - [Azure SignalR Service output binding for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-signalr-service-output) enables you to send messages by using Azure SignalR Service.
- [Azure Storage](https://learn.microsoft.com/en-us/azure/storage/common/storage-introduction) is a Microsoft-managed cloud service that provides highly available and secure storage that scales as your needs grow. Storage is a general-purpose storage account that can be used for a variety of data types and scenarios.
- [Azure Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/general/overview/) securely stores and controls access to secrets like API keys, passwords, certificates, and cryptographic keys. Azure Key Vault also lets you easily provision, manage, and deploy public and private Transport Layer Security/Secure Sockets Layer (TLS/SSL) certificates, for use with Azure and your internal connected resources.
- [Azure SignalR Service](https://azure.microsoft.com/services/signalr-service) simplifies the process of adding real-time web functionality to applications over HTTP with minimal effort.
- Azure Cache for Redis is a fully managed, Redis-compatible in-memory data store that can be used as a distributed cache for web apps, mobile apps, gaming, and other scenarios.
- [Azure Cognitive Search](https://azure.microsoft.com/en-us/products/search) is a fully managed cloud search service that provides a rich search experience to custom applications. It is a search-as-a-service solution that allows developers to add a search capability to their applications with little or no prior experience in developing search functionality.
- [Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview) is a fully managed enterprise integration message broker. Service Bus can decouple applications and services. Applications and services can be loosely coupled using asynchronous messaging patterns.

## Data Flow

This repository demostrates two ways of ingesting data into Azure Cognitive Search.

### Option 1

The first option is a minimal infrastructure option where the static web app will directly interact with the search service.

![Option 1](./docs/option1.v2.drawio.png)

### Option 2

The second option **(*highly recommended*)** is to separate the backend concern completely with a microservice exposing apis hosted in Azure Functions to interact with the search service and also use a messaging service and cache results for performance.

![Option 2](./docs/option2.v2.drawio.png)

This option seems to be the best option for a production application. It does have a bit more lines in the infrastructure diagram so here it is broken down by steps:

1. First the key vault serves the keys it needs for that environment and so the apps know where to go for all the backend services. In this step will also connect the front end user interface to the SignalR service.

    ![Option 2.1](./docs/option2.1.v2.drawio.png)
2. Next, this diagrams shows the flow of data from the front end user interface to the backend services to add or update a record in the search service. The front end user interface will send a request to the Azure Functions API. The Azure Functions API will then send a message to be queued into the Azure Service Bus. This is great for performace as it does not block the user interface while process happens in the background.

    ![Option 2.2](./docs/option2.2.v2.drawio.png)
3. As the Azure Function has a binding subscription to the Azure Service Bus queue, a trigger will happen for the message to then flow to the Azure Functions in the background to be processed. The Azure Function  will then send a message to the Azure Cognitive Search service and after it receives a successful message it will send a notification to the user via the Azure SignalR Service.

    ![Option 2.3](./docs/option2.3.v2.drawio.png)
4. To make a simple query to the search service, the front end user interface will send a request to the Azure Functions API. The Azure Functions API will then send a message to be cache service via an input binding. If it is there it returns the result. If it is not there, it queries the search service and caches the result for future queries' performace. It then returns the user the result.

    ![Option 2.4](./docs/option2.4.v2.drawio.png)

## Running the applications

Follow the instructions in this [DEVELOPMENT.md](./DEVELOPMENT.md) file to run the application.































# How to run the application in developer machine

[Back to the main page](/README.md)

These client application was written with .NET 7 [Blazor WebAssembly](https://docs.microsoft.com/aspnet/core/blazor/?view=aspnetcore-6.0#blazor-webassembly), .NET 7 C# [Azure Functions](https://docs.microsoft.com/azure/azure-functions/functions-overview), and a C# class library with shared code.

> Note: Azure Functions only supports .NET 7 in the isolated process execution model

## Pre-requisites

- GigHub account which you can sign up for one free here: <https://github.com/signup>.
- Azure account which you can sign up for a free account here: <https://azure.microsoft.com/en-us/free>. Some of the services in the demo are free (with limits of course) but your new account will give you $200 credit to try out the services that are not free for the first 30 from account creation. That’s more than enough to experiment on all services in the demo.

## Getting Started

1. In the **Api** folders, copy `local.settings.example.json` to `local.settings.json`

1. Continue using either Visual Studio or Visual Studio Code.

### Visual Studio 2022

Open the solution in the latest release of [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with the Azure workload installed, and follow these steps:

1. Right-click on the solution and select **Configure Startup Projects...**.

1. Select **Multiple startup projects** and set the following actions for each project:

    a. For Option 1:

        - *API* - None
        - *Shared* - None
        - *SWA1API* - **Start**
        - *SWA1Client* - **Start**
        - *SWA2API* - None
        - *SWA2Client* - None

    b. For Option 2:

        - *API* - **Start**
        - *Shared* - None
        - *SWA1API* - None
        - *SWA1Client* - None
        - *SWA2API* - **Start**
        - *SWA2Client* - **Start**

1. Press **F5** to launch both the client application and the Functions API apps.

### Visual Studio Code with Azure Static Web Apps CLI for a better development experience (Optional)

1. Install the [Azure Static Web Apps CLI](https://www.npmjs.com/package/@azure/static-web-apps-cli) and [Azure Functions Core Tools CLI](https://www.npmjs.com/package/azure-functions-core-tools).

1. Open the folder in Visual Studio Code.

1. In the VS Code terminal, run the following command to start the Static Web Apps CLI, along with the Blazor WebAssembly client application and the Functions API app:

    ```bash
    swa start http://localhost:5000 --api-location http://localhost:7071
    ```

    The Static Web Apps CLI (`swa`) starts a proxy on port 4280 that will forward static site requests to the Blazor server on port 5000 and requests to the `/api` endpoint to the Functions server. 

1. Open a browser and navigate to the Static Web Apps CLI's address at `http://localhost:4280`. You'll be able to access both the client application and the Functions API app in this single address. When you navigate to the "Fetch Data" page, you'll see the data returned by the Functions API app.

1. Enter Ctrl-C to stop the Static Web Apps CLI.
