using bapsisCodeGenerator;

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
    
        // ID tipini bir kere tespit et
        string idType = Utility.DetectIdType(modelContent);
    
        // Beklenen çoğul form
        string expectedPluralName = Utility.GetPluralForm(modelName);

        // MultiLanguage desteği kontrolü
        bool hasMultiLanguageSupport = Utility.HasMultiLanguageSupport(modelContent);
    
        // Klasör adının doğru olup olmadığını kontrol et
        string folderName = new DirectoryInfo(modelDirectory).Name;
        if (folderName != expectedPluralName)
        {
            throw new Exception($"Model dosyası '{expectedPluralName}' adlı bir klasörde olmalıdır. " +
                              $"Mevcut klasör adı: '{folderName}'");
        }
    
        // Domain sınıflarını oluştur - idType'ı parametre olarak gönder
        var generator = new GenerateAggregateRoot(modelName, modelDirectory, expectedPluralName, idType, hasMultiLanguageSupport);
        generator.ParseModel(modelContent);
        generator.Generate();
    
        // Repository sınıflarını oluştur
        var repoGenerator = new GenerateRepositories(modelPath, modelName, expectedPluralName);
        repoGenerator.Generate();

        // Modül seçimini bir kere al
        var moduleChoice = ModuleChoice.GetUserChoice();
    
        // Application katmanını oluştur - idType'ı parametre olarak gönder
        var appGenerator = new GenerateApplication(modelPath, modelName, expectedPluralName, moduleChoice, idType, hasMultiLanguageSupport); 
        appGenerator.Generate();

        // Controller oluştur - idType'ı parametre olarak gönder
        var controllerGenerator = new GenerateController(modelPath, modelName, expectedPluralName, idType, moduleChoice);
        controllerGenerator.Generate();

        // Unit test oluştur - idType'ı parametre olarak gönder
        var testGenerator = new GenerateUnitTest(modelPath, modelName, expectedPluralName, idType, hasMultiLanguageSupport);
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
    