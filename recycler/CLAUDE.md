# Recycler

.NET 10 console app for **debt recycling analysis** on an Australian home loan. Connects to the UP Bank API to fetch mortgage transactions, classifies them, and calculates the deductible/non-deductible interest split for tax purposes. Also supports predefined scenarios for testing and demos without API access.

## Project Structure

- `Program.cs` — Entry point: scenario selection (CLI `--scenario A` or interactive menu), then either scenario path or live API path. Shared local functions `DisplayTransactionTable` and `RunDebtRecyclingAnalysis` used by both paths.
- `Configuration/UpBankOptions.cs` — Strongly-typed config bound to the `OrangeThing` section, with `IValidateOptions` validation
- `Configuration/DebtRecyclingOptions.cs` — `DebtRecycling` config section: `InterestDescriptionPatterns` (default: `["Interest"]`) and `RedrawDescriptionPatterns` (default: `["Redraw"]`)
- `Models/UpBank/` — API response models: `UpApiResponse<T>` (JSON:API envelope), `UpAccount`, `UpTransaction`, `MoneyObject`
- `Models/DebtRecycling/` — Calculator models: `ClassifiedTransaction`, `AmortisationEntry`, `FinancialYearSummary`
- `Services/UpBankService.cs` — Typed `HttpClient` for UP API calls: `PingAsync`, `GetAccountsAsync`, `GetTransactionsAsync` (auto-paginates)
- `Services/TransactionClassifier.cs` — Classifies transactions as `Repayment`, `InterestCharge`, `InvestmentRedraw`, or `OtherDebit` based on amount sign and description patterns
- `Services/DebtRecyclingCalculator.cs` — Proportional split calculator: tracks deductible/non-deductible balances, allocates repayments and interest proportionally, produces monthly amortisation and FY tax summaries
- `Scenarios/ScenarioDataProvider.cs` — Predefined scenario data (A, B, C) with hardcoded transactions and extrapolation helper
- `Recycler.Tests/` — xUnit tests for classifier, calculator, and scenario data integrity

## Debt Recycling Model

The calculator uses ATO tracing rules for mixed-purpose loans:

- **Investment redraws** are auto-detected by description pattern matching (default: `["Redraw"]`) and add their full amount to the deductible balance (each redraw is a separate borrowing traced by purpose)
- **Repayments** reduce deductible and non-deductible balances proportionally
- **Interest charges** are split proportionally between deductible and non-deductible based on current balance ratio
- **Other debits** (drawdowns, fees) are allocated entirely to non-deductible
- Lump sum deposits that fund a redraw are classified as repayments (reduce both balances proportionally), then the redraw adds the full amount to deductible
- Australian FY boundaries: July 1 starts a new financial year

## Scenarios

Run with `dotnet run -- --scenario A` or select from the interactive menu (`dotnet run` with no args).

- **A** — Single $200k lump sum deposit + redraw (Feb 2026), core data Mar 2025–Apr 2026, extrapolated May–Dec 2026
- **B** — Simple 50/50 split: $100k loan, lump sum $50k recycled on day one, 36 months at 10% annual
- **C** — Three redraws across financial years: 2× lump sum, 1× built-up (from accumulated overpayments), core data + extrapolation

Core transaction data in scenarios uses `DateTimeOffset` with `TimeSpan.FromHours(11)` (AEST), consistent with UP Bank API.

## Reports

Both live and scenario paths produce three reports when redraws are configured:

1. **Deductible Interest Tracking** — Per-interest-charge breakdown showing total, deductible, non-deductible amounts and percentage
2. **Monthly Amortisation Schedule** — Monthly repayment/interest/redraw totals with running deductible and non-deductible balances
3. **Financial Year Tax Summary** — Aggregated deductible interest per Australian FY for tax returns

## UP Bank API

- Base URL: `https://api.up.com.au/api/v1`
- Auth: Bearer token in `Authorization` header
- JSON:API format — responses wrapped in `{ "data": [...], "links": { "next": ... } }`
- Key endpoints: `GET /util/ping`, `GET /accounts`, `GET /accounts/{id}/transactions`
- Transaction filters: `filter[since]`, `filter[until]`, `filter[status]` (HELD/SETTLED), `filter[category]`, `page[size]`
- Account types: `TRANSACTIONAL`, `SAVER`, `HOME_LOAN`
- Docs: https://developer.up.com.au

## Configuration

- `appsettings.json` — stores `OrangeThing:BaseUrl` and `DebtRecycling` description patterns (copied to output via csproj)
- User secrets — stores `OrangeThing:Token` (the API bearer token)
- User secrets are loaded explicitly via `config.AddUserSecrets()` (not environment-dependent)
- Config sections: `OrangeThing` (UP Bank API), `DebtRecycling` (interest + redraw description patterns)

## Commands

- `dotnet build Recycler.slnx` — build solution
- `dotnet test` — run all tests
- `dotnet run` — interactive menu (Live / Scenario A / B / C)
- `dotnet run -- --scenario A` — run specific scenario directly
- `dotnet user-secrets set "OrangeThing:Token" "<token>"` — set API token
- `dotnet user-secrets list` — verify stored secrets

## Token Refresh

Generate a new token via the UP app (Data sharing > Personal Access Token) or https://api.up.com.au.
