namespace BlumBot;

public static class Program
{
    static async Task Main()
    {
        var settings = Settings.ReadSettings();

        if (settings.Length == 0)
        {
            Console.WriteLine("Rechek accounts folder");
            return;
        }

        for (int i = 0; i < settings.Length; i++)
        {
            Console.WriteLine($"Working with account {i + 1}");
            var client = new BlumClient(settings[i]);
            var success = await client.StartBotAsync();
            Console.WriteLine($"Account {i + 1} sucess: {success}");
        }
    }
}