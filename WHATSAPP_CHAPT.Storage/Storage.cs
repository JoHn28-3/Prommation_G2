using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace WHATSAPP_CHAPT.Storage
{
    /// <summary>
    /// Gestion du répertoire de données de l'application.
    /// </summary>
    public static class DataFolder
    {
        /// <summary>
        /// Retourne le chemin du dossier de données. Valeur par défaut : %LOCALAPPDATA%\WHATSAPP_CHAPT\data.
        /// L'utilisateur peut le remplacer avec le paramètre de ligne de commande « --data <folder> ».
        /// </summary>
        public static string GetPath(string[] args)
        {
            string defaultSub = "WHATSAPP_CHAPT" + Path.DirectorySeparatorChar + "data";
            string? explicitPath = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals("--data", StringComparison.OrdinalIgnoreCase))
                {
                    explicitPath = args[i + 1];
                    break;
                }
            }
            string path = explicitPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), defaultSub);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// Gestion de l'identité de l'utilisateur (clé ECDSA P‑256 et identifiant dérivé).
    /// </summary>
    public class IdentityManager
    {
        private const string IdentityFileName = "identity.dat"; // stocké chiffré avec DPAPI
        private readonly string _folder;
        private ECDsa? _privateKey;

        /// <summary>
        /// Identifiant de l'utilisateur (guid dérivé de la clé publique).
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Clé publique au format SubjectPublicKeyInfo (DER).
        /// </summary>
        public byte[] PublicKey { get; private set; } = Array.Empty<byte>();

        /// <summary>
        /// Crée ou charge l'identité depuis le répertoire de données.
        /// </summary>
        public IdentityManager(string dataFolder)
        {
            _folder = dataFolder;
            LoadOrCreate();
        }

        private void LoadOrCreate()
        {
            string file = Path.Combine(_folder, IdentityFileName);
            if (File.Exists(file))
            {
                // Lire la clé privée protégée
                byte[] protectedBlob = File.ReadAllBytes(file);
                byte[] pk = ProtectedData.Unprotect(protectedBlob, null, DataProtectionScope.CurrentUser);
                _privateKey = ECDsa.Create();
                _privateKey.ImportPkcs8PrivateKey(pk, out _);
            }
            else
            {
                // Générer une nouvelle paire
                _privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                // Exporter au format PKCS#8 (DER) puis protéger avec DPAPI
                byte[] pk = _privateKey.ExportPkcs8PrivateKey();
                byte[] protectedBlob = ProtectedData.Protect(pk, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(file, protectedBlob);
            }
            // Exporter la clé publique en SubjectPublicKeyInfo (DER)
            PublicKey = _privateKey.ExportSubjectPublicKeyInfo();
            // Calculer l'identifiant (hash SHA‑256, premiers 16 octets → Guid)
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(PublicKey);
            byte[] guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, 16);
            UserId = new Guid(guidBytes);
        }

        /// <summary>
        /// Exportation de la carte de contact (WCARD) au format texte.
        /// </summary>
        public string ExportContactCard(string displayName)
        {
            var json = $"{{\"v\":1,\"n\":\"{displayName}\",\"pk\":\"{Convert.ToBase64String(PublicKey)}\"}}";
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            return $"WCARD1:{b64}";
        }
    }

    /// <summary>
    /// Accès SQLite simple pour le stockage persistant.
    /// </summary>
    public class StorageContext : IDisposable
    {
        private readonly SqliteConnection _conn;
        public StorageContext(string dataFolder)
        {
            string dbPath = Path.Combine(dataFolder, "whatsapp_chapt.db");
            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Default,
                
                ForeignKeys = true
            };
            _conn = new SqliteConnection(csb.ConnectionString);
            _conn.Open();
            // Activer pragma de sécurité
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();
            EnsureSchema();
        }

        private void EnsureSchema()
        {
            // Version simple : créer les tables si elles n'existent pas.
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Identities (
                    Id TEXT PRIMARY KEY,
                    PublicKey BLOB NOT NULL,
                    CreatedAt TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Contacts (
                    Id TEXT PRIMARY KEY,
                    DisplayName TEXT NOT NULL,
                    PublicKey BLOB NOT NULL,
                    AddedAt TEXT NOT NULL
                );
            ";
            cmd.ExecuteNonQuery();
        }

        public void InsertIdentity(Guid id, byte[] publicKey)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Identities (Id, PublicKey, CreatedAt) VALUES (@id, @pk, @now);";
            cmd.Parameters.AddWithValue("@id", id.ToString("d"));
            cmd.Parameters.AddWithValue("@pk", publicKey);
            cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _conn?.Dispose();
        }
    }
}
