using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OnlineGame
{
    internal class AccountService
    {
        private readonly string accountsPath = "Data/accounts.json";
        private readonly Logger logger;

        public AccountService(Logger logger)
        {
            this.logger = logger;
            EnsureAccountsFileExists();
        }

        public bool Register(string username, string password, out string message)
        {
            try
            {
                List<AccountRecord> accounts = LoadAccounts();

                if (accounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    message = "Účet s tímto jménem už existuje.";
                    logger.Log($"Neúspěšná registrace: {username} - účet už existuje");
                    return false;
                }

                AccountRecord account = new AccountRecord
                {
                    Username = username,
                    PasswordHash = ComputeSha256(password)
                };

                accounts.Add(account);
                SaveAccounts(accounts);

                message = "Registrace proběhla úspěšně.";
                logger.Log($"Registrace nového hráče: {username}");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Chyba při registraci uživatele {username}", ex);
                message = "Při registraci nastala chyba.";
                return false;
            }
        }

        public bool Login(string username, string password, out string message)
        {
            try
            {
                List<AccountRecord> accounts = LoadAccounts();

                AccountRecord account = accounts.FirstOrDefault(a =>
                    a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (account == null)
                {
                    message = "Účet neexistuje.";
                    logger.Log($"Neúspěšné přihlášení: {username} - účet neexistuje");
                    return false;
                }

                string hash = ComputeSha256(password);

                if (account.PasswordHash != hash)
                {
                    message = "Nesprávné heslo.";
                    logger.Log($"Neúspěšné přihlášení: {username} - špatné heslo");
                    return false;
                }

                message = "Přihlášení proběhlo úspěšně.";
                logger.Log($"Úspěšné přihlášení: {username}");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Chyba při přihlášení uživatele {username}", ex);
                message = "Při přihlášení nastala chyba.";
                return false;
            }
        }

        private List<AccountRecord> LoadAccounts()
        {
            string json = File.ReadAllText(accountsPath);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<AccountRecord> accounts = JsonSerializer.Deserialize<List<AccountRecord>>(json, options);

            return accounts ?? new List<AccountRecord>();
        }

        private void SaveAccounts(List<AccountRecord> accounts)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(accounts, options);
            File.WriteAllText(accountsPath, json);
        }

        private void EnsureAccountsFileExists()
        {
            string? directory = Path.GetDirectoryName(accountsPath);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(accountsPath))
            {
                File.WriteAllText(accountsPath, "[]");
            }
        }

        private string ComputeSha256(string text)
        {
            using SHA256 sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(bytes);
        }
    }
}