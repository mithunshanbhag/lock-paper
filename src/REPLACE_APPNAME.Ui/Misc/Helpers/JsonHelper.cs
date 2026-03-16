namespace REPLACE_APPNAME.Ui.Misc.Helpers;

public static class JsonHelper
{
    public static JsonSerializerOptions Opts { get; } = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
}