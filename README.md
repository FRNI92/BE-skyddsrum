# Skyddsrum Functions

Azure Functions backend for Skyddsrumsgruppen.

## Local settings

Copy `local.settings.example.json` to `local.settings.json` and fill in the values.

## Azure settings

Set these application settings in the Azure Functions app:

- `Cosmos:ConnectionString`
- `Cosmos:DatabaseName`
- `Cosmos:ArticlesContainerName`
- `BlobStorage:ConnectionString`
- `BlobStorage:ImagesContainerName`
- `Email:ConnectionString`
- `Email:SenderAddress`
- `Email:RecipientAddress`

## Cosmos DB

Create database `skyddsrum` and container `articles` with partition key `/id`.

## Auth

Admin endpoints require the `admin` role in `x-ms-client-principal`.
This is easiest when the API is connected to Azure Static Web Apps auth.
