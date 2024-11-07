using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace bapsisCodeGenerator;

public static class Utility
{
    public static string DetectIdType(string modelContent)
    {
        try
        {
            // Önce IIdentityEntity<T> interface'inden kontrol et
            var interfaceRegex = new Regex(@"IIdentityEntity<([^>]+)>");
            var match = interfaceRegex.Match(modelContent);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Sonra Id property'sini kontrol et
            var propertyRegex = new Regex(@"public\s+([a-zA-Z0-9_<>]+)\s+Id\s*{\s*get;\s*(?:private\s+)?set;\s*}");
            match = propertyRegex.Match(modelContent);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Base class entity generic tipini kontrol et
            var entityRegex = new Regex(@":\s*Entity<([a-zA-Z0-9_<>]+)>");
            match = entityRegex.Match(modelContent);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Tip bulunamazsa varsayılan olarak int döndür
            return "int";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ID tipi belirlenirken hata oluştu: {ex.Message}");
            Console.WriteLine("Varsayılan olarak 'int' kullanılacak.");
            return "int";
        }
    }

    public static string GetPluralForm(string singular)
    {
        // İngilizce çoğul kuralları
        if (singular.EndsWith("y", StringComparison.OrdinalIgnoreCase))
            return singular.Substring(0, singular.Length - 1) + "ies";
        if (singular.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            singular.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            singular.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            singular.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
            return singular + "es";

        // Varsayılan kural
        return singular + "s";
    }

    public static bool HasMultiLanguageSupport(string modelContent)
    {
        try
        {
            // IMultiLanguageEntity interface kontrolü
            var interfaceRegex = new Regex(@"IMultiLanguageEntity<([^>]+)>");
            var match = interfaceRegex.Match(modelContent);
            if (match.Success)
            {
                return true;
            }

            // Interface listesinde kontrol
            var interfacesRegex = new Regex(@":\s*(.*?)\s*{");
            match = interfacesRegex.Match(modelContent);
            if (match.Success)
            {
                var interfaces = match.Groups[1].Value.Split(',')
                    .Select(i => i.Trim());
                return interfaces.Any(i => i.Contains("IMultiLanguageEntity"));
            }

            // Translations property kontrolü
            var translationsRegex = new Regex(@"ICollection<\w+Language>\s+Translations\s*{\s*get;");
            match = translationsRegex.Match(modelContent);
            if (match.Success)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MultiLanguage kontrolünde hata oluştu: {ex.Message}");
            return false;
        }
    }
}
