using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Recycler.Configuration;
using Recycler.Models.DebtRecycling;
using Recycler.Scenarios;
using Recycler.Services;

// --- Scenario selection ---
string? scenarioKey = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--scenario")
    {
        scenarioKey = args[i + 1].ToUpperInvariant();
        break;
    }
}

if (scenarioKey is null && args.Length == 0)
{
    Console.WriteLine("Select data source:");
    Console.WriteLine("  [L] Live UP Bank data");
    Console.WriteLine("  [A] Scenario A: Single $200k redraw (Feb 2026)");
    Console.WriteLine("  [B] Scenario B: Simple 50/50 split ($100k loan, 3 years)");
    Console.WriteLine("  [C] Scenario C: Three redraws across financial years");
    Console.Write("> ");
    var choice = Console.ReadLine()?.Trim().ToUpperInvariant();
    if (choice is "A" or "B" or "C")
        scenarioKey = choice;
}

// --- Data retrieval (source-specific) ---
AnalysisInput? input;

if (scenarioKey is not null)
{
    var scenario = ScenarioDataProvider.GetScenario(scenarioKey);
    Console.WriteLine($"Scenario {scenarioKey}: {scenario.Description}");
    Console.WriteLine();
    input = new AnalysisInput(scenario.Transactions, 0, new DebtRecyclingOptions());
}
else
{
    input = await FetchLiveData();
}

if (input is null)
    return;

// --- Analysis pipeline (source-agnostic) ---
ReportRenderer.DisplayTransactionTable(input.Transactions);

var classifier = new TransactionClassifier(Options.Create(input.Options));
var (classified, warnings) = classifier.Classify(input.Transactions);

foreach (var warning in warnings)
    Console.WriteLine($"WARNING: {warning}");

var calculator = new DebtRecyclingCalculator();
var result = calculator.Calculate(input.InitialBalanceCents, classified);

ReportRenderer.DisplayDebtRecyclingReports(result);

// --- Live API data retrieval ---

async Task<AnalysisInput?> FetchLiveData()
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            config.AddUserSecrets<UpBankOptions>();
        })
        .ConfigureServices((context, services) =>
        {
            services.Configure<UpBankOptions>(context.Configuration.GetSection(UpBankOptions.SectionName));
            services.AddSingleton<IValidateOptions<UpBankOptions>, ValidateUpBankOptions>();

            services.Configure<DebtRecyclingOptions>(context.Configuration.GetSection(DebtRecyclingOptions.SectionName));

            services.AddHttpClient<UpBankService>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<UpBankOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl!.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Token);
            });
        })
        .Build();

    var upBank = host.Services.GetRequiredService<UpBankService>();

    var ping = await upBank.PingAsync();

    if (!ping.IsSuccessStatusCode)
    {
        if (ping.StatusCode is System.Net.HttpStatusCode.Unauthorized)
        {
            Console.WriteLine("Unauthorized. Your token is invalid or expired.");
            Console.WriteLine();
            Console.WriteLine("To get a fresh token:");
            Console.WriteLine("  1. Open the UP app > swipe right > Data sharing > Personal Access Token");
            Console.WriteLine("     Or visit https://api.up.com.au");
            Console.WriteLine("  2. Generate a new token");
            Console.WriteLine($"  3. Run: dotnet user-secrets set \"{UpBankOptions.SectionName}:Token\" \"<your-token>\"");
        }
        else
        {
            Console.WriteLine($"API error: {(int)ping.StatusCode} ({ping.ReasonPhrase})");
        }
        return null;
    }

    var accounts = await upBank.GetAccountsAsync();
    var homeLoan = accounts.FirstOrDefault(a => a.Attributes.AccountType == "HOME_LOAN");

    if (homeLoan is null)
    {
        Console.WriteLine("No home loan account found.");
        Console.WriteLine("Available accounts:");
        foreach (var a in accounts)
            Console.WriteLine($"  - {a.Attributes.DisplayName} ({a.Attributes.AccountType})");
        return null;
    }

    Console.WriteLine($"Home loan: {homeLoan.Attributes.DisplayName} (balance: {homeLoan.Attributes.Balance.Value})");
    Console.WriteLine();

    var transactions = await upBank.GetTransactionsAsync(homeLoan.Id);

    var debtRecyclingOptions = host.Services.GetRequiredService<IOptions<DebtRecyclingOptions>>().Value;

    long currentBalanceCents = homeLoan.Attributes.Balance.ValueInBaseUnits;
    long sumTransactionCents = transactions.Sum(t => (long)t.Attributes.Amount.ValueInBaseUnits);
    long initialBalanceCents = Math.Abs(currentBalanceCents - sumTransactionCents);

    Console.WriteLine($"Derived initial loan balance: ${initialBalanceCents / 100m:N2}");

    return new AnalysisInput(transactions, initialBalanceCents, debtRecyclingOptions);
}
