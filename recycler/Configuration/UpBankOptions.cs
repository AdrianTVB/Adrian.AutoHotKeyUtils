using Microsoft.Extensions.Options;

namespace Recycler.Configuration;

public class UpBankOptions
{
    public const string SectionName = "OrangeThing";

    public string? Token { get; set; }
    public string? BaseUrl { get; set; }
}

public class ValidateUpBankOptions : IValidateOptions<UpBankOptions>
{
    public ValidateOptionsResult Validate(string? name, UpBankOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            return ValidateOptionsResult.Fail("OrangeThing:BaseUrl is missing. Check appsettings.json.");

        if (string.IsNullOrWhiteSpace(options.Token))
            return ValidateOptionsResult.Fail("OrangeThing:Token is missing. Run: dotnet user-secrets set \"OrangeThing:Token\" \"<your-token>\"");

        return ValidateOptionsResult.Success;
    }
}
