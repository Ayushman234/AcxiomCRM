namespace AcxiomCRM.Models;

/// <summary>
/// Application role constants. Used throughout authorization attributes.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";

    public static readonly string[] All = [Admin, Manager, SalesExecutive];
    public static readonly string AdminOrManager = $"{Admin},{Manager}";
}
