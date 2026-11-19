#nullable enable

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WHATSAPP_CHAPT.Core.Security
{
    /// <summary>
    /// Gestion du chiffrement et déchiffrement des messages P2P (Phase I).
    /// Utilise AES-256-CBC avec vecteur d'initialisation aléatoire et dérivation de clé SHA-256.
    /// </summary>
    public static class MessageCrypto
    {
        /// <summary>
        /// Dérive une clé AES 256 bits à partir d'un secret partagé ou d'une clé publique.
        /// </summary>
        public static byte[] DeriveKey(byte[] secretBytes)
        {
            if (secretBytes == null || secretBytes.Length == 0)
                throw new ArgumentException("Le secret ne peut pas être vide.", nameof(secretBytes));

            using var sha = SHA256.Create();
            return sha.ComputeHash(secretBytes);
        }

        /// <summary>
        /// Chiffre un texte clair avec une clé AES 256 bits.
        /// Format retourné en base64 : [16 octets IV] + [données chiffrées].
        /// </summary>
        public static string Encrypt(string plainText, byte[] key)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));
            if (key == null || key.Length != 32)
                throw new ArgumentException("La clé doit faire exactement 32 octets (256 bits).", nameof(key));

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            // Écriture du vecteur d'initialisation IV en début de flux
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cs, Encoding.UTF8))
            {
                writer.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Déchiffre un message au format base64 ([IV] + [chiffré]) avec la clé AES 256 bits.
        /// </summary>
        public static string Decrypt(string cipherTextBase64, byte[] key)
        {
            if (string.IsNullOrEmpty(cipherTextBase64))
                throw new ArgumentNullException(nameof(cipherTextBase64));
            if (key == null || key.Length != 32)
                throw new ArgumentException("La clé doit faire exactement 32 octets (256 bits).", nameof(key));

            byte[] fullCipher = Convert.FromBase64String(cipherTextBase64);
            if (fullCipher.Length < 16)
                throw new CryptographicException("Données chiffrées corrompues ou trop courtes.");

            byte[] iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, 16);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs, Encoding.UTF8);

            return reader.ReadToEnd();
        }
    }
}
