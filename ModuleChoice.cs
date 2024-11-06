public class ModuleChoice
{
    public ModuleType SelectedModule { get; private set; }
    public bool IsCommons { get; private set; }

    public static ModuleChoice GetUserChoice()
    {
        Console.WriteLine("Lütfen modül tipini seçin:");
        Console.WriteLine("1- Commons");
        Console.WriteLine("2- Modules");
        
        var choice = Console.ReadLine();
        var moduleChoice = new ModuleChoice
        {
            IsCommons = choice == "1"
        };

        if (!moduleChoice.IsCommons)
        {
            Console.WriteLine("\nLütfen modül seçin:");
            Console.WriteLine("1- Admin");
            Console.WriteLine("2- Commission");
            Console.WriteLine("3- Coordinator");
            Console.WriteLine("4- Management");
            Console.WriteLine("5- OpenApi");
            Console.WriteLine("6- ProjectOffice");
            Console.WriteLine("7- Researcher");
            Console.WriteLine("8- SpendingOffice");
            Console.WriteLine("9- SystemManagement");

            var selectedModule = Console.ReadLine();
            moduleChoice.SelectedModule = (ModuleType)Enum.Parse(typeof(ModuleType), 
                int.Parse(selectedModule).ToString());
        }
        else
        {
            moduleChoice.SelectedModule = ModuleType.Commons;
        }

        return moduleChoice;
    }
}