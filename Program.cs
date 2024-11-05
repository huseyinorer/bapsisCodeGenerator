string GetPluralForm(string singular)
{
    // İngilizce çoğul kuralları
    if (singular.EndsWith("Activity", StringComparison.OrdinalIgnoreCase))
        return singular.Substring(0, singular.Length - 8) + "Activities";
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

try
{
    Console.WriteLine("Model dosyasının yolunu girin:");
    string modelPath = Console.ReadLine();

    if (!File.Exists(modelPath))
    {
        throw new FileNotFoundException("Model dosyası bulunamadı.", modelPath);
    }

    string modelContent = File.ReadAllText(modelPath);
    string modelName = Path.GetFileNameWithoutExtension(modelPath);
    string modelDirectory = Path.GetDirectoryName(modelPath);

    // Beklenen çoğul form
    string expectedPluralName = GetPluralForm(modelName);

    // Klasör adının doğru olup olmadığını kontrol et
    string folderName = new DirectoryInfo(modelDirectory).Name;
    if (folderName != expectedPluralName)
    {
        throw new Exception($"Model dosyası '{expectedPluralName}' adlı bir klasörde olmalıdır. " +
                          $"Mevcut klasör adı: '{folderName}'");
    }

     // Domain sınıflarını oluştur
    var generator = new GenerateAggregateRoot(modelName, modelDirectory);
    generator.ParseModel(modelContent);
    generator.Generate();
    

    // Repository sınıflarını oluştur
    var repoGenerator = new GenerateRepositories(modelPath, modelName, expectedPluralName);
    repoGenerator.Generate();

    // Application katmanını oluştur
    var appGenerator = new GenerateApplication(modelPath, modelName, expectedPluralName); 
    appGenerator.Generate();
}
catch (Exception ex)
{
    Console.WriteLine($"Hata oluştu: {ex.Message}");
}
