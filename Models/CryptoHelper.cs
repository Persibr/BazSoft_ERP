using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Bazsoft_ERP.Models
{
    public static class CryptoHelper
    {
        private static readonly string claveSecreta = "@BazServ@Web"; // 16-32 chars

        // Cifrar ID con AES-CBC y IV aleatorio
        public static string CifrarId(string id)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(claveSecreta.PadRight(32));
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV(); // IV aleatorio

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] inputBytes = Encoding.UTF8.GetBytes(id);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

                // Concatenar IV + ciphertext
                byte[] combined = aes.IV.Concat(encryptedBytes).ToArray();

                // Convertir a Base64 y hacer URL-friendly
                return Convert.ToBase64String(combined)
                              .Replace('+', '-')
                              .Replace('/', '_')
                              .TrimEnd('=');
            }
        }

        // Descifrar ID
        public static string DescifrarId(string cifrado)
        {
            // Restaurar Base64 original
            string base64 = cifrado.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            byte[] combined = Convert.FromBase64String(base64);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(claveSecreta.PadRight(32));
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Separar IV y ciphertext
                byte[] iv = combined.Take(16).ToArray();
                byte[] ciphertext = combined.Skip(16).ToArray();

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, iv);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }
}
