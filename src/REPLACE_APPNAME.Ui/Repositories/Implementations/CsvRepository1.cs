namespace REPLACE_APPNAME.Ui.Repositories.Implementations;

public class CsvRepository1() : CsvGenericRepositoryBase(SCsvConfiguration), ICsvRepository1
{
    private static readonly CsvConfiguration SCsvConfiguration = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        TrimOptions = TrimOptions.Trim,
        PrepareHeaderForMatch = args => args.Header.ToLowerInvariant()
    };
}