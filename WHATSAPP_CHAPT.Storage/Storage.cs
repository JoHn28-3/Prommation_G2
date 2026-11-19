using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using WHATSAPP_CHAPT.Storage.Models;

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
                CREATE TABLE IF NOT EXISTS Peers (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    IpAddress TEXT NOT NULL,
                    AddedAt TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ArchivedContacts (
                    Id TEXT PRIMARY KEY,
                    DisplayName TEXT NOT NULL,
                    PublicKey BLOB NOT NULL,
                    AddedAt TEXT NOT NULL,
                    IsArchived INTEGER NOT NULL DEFAULT 0
                );

            ";
            cmd.ExecuteNonQuery();
        }

        private SqliteCommand CreateCommand()
        {
            return _conn.CreateCommand();
        }

        public async Task<Guid> GetLocalIdAsync()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id FROM Identities LIMIT 1";
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && Guid.TryParse(result.ToString(), out var guid))
            {
                return guid;
            }

            return Guid.NewGuid();
        }

        public void InsertContact(Guid id, string displayName, byte[] publicKey)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Contacts (Id, DisplayName, PublicKey, AddedAt) VALUES (@id, @name, @pk, @now);";
            cmd.Parameters.AddWithValue("@id", id.ToString("d"));
            cmd.Parameters.AddWithValue("@name", displayName);
            cmd.Parameters.AddWithValue("@pk", publicKey);
            cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public void ArchiveContact(Guid id)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO ArchivedContacts (Id, DisplayName, PublicKey, AddedAt, IsArchived) SELECT Id, DisplayName, PublicKey, AddedAt, 1 FROM Contacts WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id.ToString("d"));
            cmd.ExecuteNonQuery();
            // Optionally delete from Contacts
            cmd.CommandText = "DELETE FROM Contacts WHERE Id = @id;";
            cmd.ExecuteNonQuery();
        }

        public void UnarchiveContact(Guid id)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Contacts (Id, DisplayName, PublicKey, AddedAt) SELECT Id, DisplayName, PublicKey, AddedAt FROM ArchivedContacts WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id.ToString("d"));
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DELETE FROM ArchivedContacts WHERE Id = @id;";
            cmd.ExecuteNonQuery();
        }


        /// <summary>
        /// Insert or replace a peer record.
        /// </summary>
        public void InsertPeer(Guid id, string name, string ipAddress)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Peers (Id, Name, IpAddress, AddedAt) VALUES (@id, @name, @ip, @now);";
            cmd.Parameters.AddWithValue("@id", id.ToString("d"));
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@ip", ipAddress);
            cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Récupère la liste des pairs enregistrés.
        /// </summary>
        public List<Peer> GetPeers()
        {
            var list = new List<Peer>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, IpAddress, AddedAt FROM Peers";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Peer
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Name = reader.GetString(1),
                    IpAddress = reader.GetString(2),
                    AddedAt = DateTimeOffset.Parse(reader.GetString(3))
                });
            }
            return list;
        }

        /// <summary>
        /// Récupère les noms des contacts archivés.
        /// </summary>
        public List<string> GetArchivedContacts()
        {
            var list = new List<string>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT DisplayName FROM ArchivedContacts";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }
            return list;
        }

        public void Dispose()
        {
            _conn?.Dispose();
        }
    }
}
