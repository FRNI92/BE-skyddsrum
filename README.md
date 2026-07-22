# Skyddsrumsgruppen Contact API

Azure Functions-backend för webbplatsens kontaktformulär.

## Endpoint

`POST /api/contact`

API:t validerar och normaliserar alla fält, begränsar requeststorlek, stoppar dubbletter och täta upprepningar samt använder ett honeypot-fält mot enklare bottar.

Vid godkänt formulär skickas:

1. Ett internt mejl med kundens förfrågan och kundens adress som `Reply-To`.
2. Ett designat bekräftelsemejl till kunden med referensnummer.

## Lokal konfiguration

Kopiera `local.settings.example.json` till `local.settings.json` och fyll i:

- `Email:ConnectionString`
- `Email:SenderAddress`
- `Email:RecipientAddress`
- `Email:SiteUrl`

`local.settings.json` får inte checkas in.

## Azure

Lägg samma värden som Application settings i Function App. Tillåt endast webbplatsens produktionsdomäner under CORS.

Rate limiting i koden är per Function-instans och skyddar främst mot misstag och enklare spam. Använd Azure Front Door WAF eller API Management om ett distribuerat produktionsskydd krävs.

## Kontroll

```powershell
dotnet build
```
