namespace AeroResponse.Data.Mongo.Accounts;

public sealed record CompanyMemberSummary(
    string IdentityUserId,
    string FirstName,
    string Surname,
    string Email,
    string AccountType,
    DateTime CreatedAtUtc)
{
    public string DisplayName
    {
        get
        {
            var fullName =
                string.Join(
                    " ",
                    new[]
                    {
                        FirstName?.Trim(),
                        Surname?.Trim()
                    }
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value)));

            return string.IsNullOrWhiteSpace(fullName)
                ? Email
                : fullName;
        }
    }

    public string AccountTypeLabel =>
        AccountType switch
        {
            "pilot" =>
                "Pilot",

            "trainer" =>
                "Trainer",

            _ =>
                "Member"
        };
}