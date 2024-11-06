string GetPluralForm(string singular)
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

bool continueProcess = true;

while (continueProcess)
{
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
        var generator = new GenerateAggregateRoot(modelName, modelDirectory, expectedPluralName);
        generator.ParseModel(modelContent);
        generator.Generate();

        // Repository sınıflarını oluştur
        var repoGenerator = new GenerateRepositories(modelPath, modelName, expectedPluralName);
        repoGenerator.Generate();

        // Modül seçimini bir kere al
        var moduleChoice = ModuleChoice.GetUserChoice();

        // Application katmanını oluştur
        var appGenerator = new GenerateApplication(modelPath, modelName, expectedPluralName, moduleChoice);
        appGenerator.Generate();

        // Controller oluştur
        var controllerGenerator = new GenerateController(modelPath, modelName, expectedPluralName,
            appGenerator.GetIdType(), moduleChoice);
        controllerGenerator.Generate();

        // Unit test oluştur
        var testGenerator = new GenerateUnitTest(modelPath, modelName, expectedPluralName);
        testGenerator.Generate();

        Console.WriteLine("\nTüm işlemler başarıyla tamamlandı.");
        Console.Write("\nBaşka bir işlem yapmak istiyor musunuz? (E/H): ");
        var response = Console.ReadLine()?.ToUpper();
        continueProcess = response == "E";

        if (continueProcess)
        {
            Console.Clear(); // Konsolu temizle
            Console.WriteLine("Yeni işlem başlatılıyor...\n");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nHata oluştu: {ex.Message}");
        Console.Write("\nTekrar denemek istiyor musunuz? (E/H): ");
        var response = Console.ReadLine()?.ToUpper();
        continueProcess = response == "E";

        if (continueProcess)
        {
            Console.Clear(); // Konsolu temizle
            Console.WriteLine("Yeni işlem başlatılıyor...\n");
        }
    }
}

Console.WriteLine("\nProgram sonlandırılıyor...");
Console.WriteLine("Çıkmak için herhangi bir tuşa basın...");
Console.ReadKey();
    