using System.Collections.Frozen;

namespace NbcCms.Web.Services;

public static class Icons
{
    private static readonly FrozenDictionary<string, string> Paths = new Dictionary<string, string>
    {
        ["grid"] = "<rect x='3' y='3' width='7' height='7' rx='1.6'/><rect x='14' y='3' width='7' height='7' rx='1.6'/><rect x='3' y='14' width='7' height='7' rx='1.6'/><rect x='14' y='14' width='7' height='7' rx='1.6'/>",
        ["file"] = "<path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z'/><path d='M14 2v6h6'/><path d='M8 13h8M8 17h6'/>",
        ["plus"] = "<circle cx='12' cy='12' r='9'/><path d='M12 8v8M8 12h8'/>",
        ["alert"] = "<path d='M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z'/><path d='M12 9v4M12 17h.01'/>",
        ["users"] = "<path d='M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2'/><circle cx='9' cy='7' r='4'/><path d='M23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75'/>",
        ["tv"] = "<rect x='2' y='7' width='20' height='14' rx='2'/><path d='M17 2l-5 5-5-5'/>",
        ["download"] = "<path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4'/><path d='M7 10l5 5 5-5'/><path d='M12 15V3'/>",
        ["activity"] = "<path d='M22 12h-4l-3 9L9 3l-3 9H2'/>",
        ["user"] = "<path d='M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2'/><circle cx='12' cy='7' r='4'/>",
        ["logout"] = "<path d='M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4'/><path d='M16 17l5-5-5-5'/><path d='M21 12H9'/>",
        ["menu"] = "<path d='M3 6h18M3 12h18M3 18h18'/>",
        ["search"] = "<circle cx='11' cy='11' r='7'/><path d='M21 21l-4.3-4.3'/>",
        ["upload"] = "<path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4'/><path d='M17 8l-5-5-5 5'/><path d='M12 3v12'/>",
        ["check"] = "<path d='M20 6L9 17l-5-5'/>",
        ["x"] = "<path d='M18 6L6 18M6 6l12 12'/>",
        ["clock"] = "<circle cx='12' cy='12' r='9'/><path d='M12 7v5l3 2'/>",
        ["arrow-left"] = "<path d='M19 12H5M12 19l-7-7 7-7'/>",
        ["arrow-right"] = "<path d='M5 12h14M12 5l7 7-7 7'/>",
        ["eye"] = "<path d='M1 12s4-7 11-7 11 7 11 7-4 7-11 7S1 12 1 12z'/><circle cx='12' cy='12' r='3'/>",
        ["edit"] = "<path d='M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7'/><path d='M18.5 2.5a2.12 2.12 0 0 1 3 3L12 15l-4 1 1-4z'/>",
        ["trash"] = "<path d='M3 6h18'/><path d='M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2'/><path d='M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6'/><path d='M10 11v6M14 11v6'/>",
        ["shield"] = "<path d='M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z'/>",
        ["send"] = "<path d='M22 2L11 13'/><path d='M22 2l-7 20-4-9-9-4z'/>",
        ["printer"] = "<path d='M6 9V2h12v7'/><path d='M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2'/><rect x='6' y='14' width='12' height='8' rx='1'/>",
        ["bank"] = "<path d='M3 21h18M3 10h18M5 10V6l7-3 7 3v4M6 10v8M10 10v8M14 10v8M18 10v8'/>",
        ["chevron-right"] = "<path d='M9 18l6-6-6-6'/>",
        ["lock"] = "<rect x='4' y='11' width='16' height='10' rx='2'/><path d='M8 11V7a4 4 0 0 1 8 0v4'/>",
        ["inbox"] = "<path d='M22 12h-6l-2 3h-4l-2-3H2'/><path d='M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z'/>",
        ["calendar"] = "<rect x='3' y='4' width='18' height='18' rx='2'/><path d='M16 2v4M8 2v4M3 10h18'/>",
        ["money"] = "<rect x='2' y='6' width='20' height='12' rx='2'/><circle cx='12' cy='12' r='3'/><path d='M6 12h.01M18 12h.01'/>",
        ["refresh"] = "<path d='M23 4v6h-6M1 20v-6h6'/><path d='M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15'/>"
    }.ToFrozenDictionary();

    public static string Svg(string name, int size = 18, string? extraClass = null)
    {
        var body = Paths.TryGetValue(name, out var path)
            ? path
            : Paths["grid"];
        var cssClass = string.IsNullOrEmpty(extraClass) ? "icon" : $"icon {extraClass}";
        return $"<svg class='{cssClass}' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round' aria-hidden='true'>{body}</svg>";
    }
}
