using System.Security.Cryptography;
using System.Text;

namespace PasswordManager
{
    /// <summary>
    /// Encripta y desencripta strings usando AES-256-CBC.
    /// La clave se deriva de la contraseña maestra con PBKDF2 (100,000 iteraciones).
    /// </summary>
    public static class CryptoService
    {
        private const int KeySize = 32;       // 256 bits
        private const int IvSize = 16;        // 128 bits
        private const int SaltSize = 16;      // 128 bits
        private const int Iterations = 100_000;

        /// <summary>
        /// Encripta un texto plano y devuelve Base64: [salt(16)][iv(16)][ciphertext]
        /// </summary>
        public static string Encrypt(string plainText, string masterPassword)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = DeriveKey(masterPassword, salt);
            byte[] iv = RandomNumberGenerator.GetBytes(IvSize);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combinar salt + iv + ciphertext en un solo array
            byte[] result = new byte[SaltSize + IvSize + cipherBytes.Length];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(iv, 0, result, SaltSize, IvSize);
            Buffer.BlockCopy(cipherBytes, 0, result, SaltSize + IvSize, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Desencripta un Base64 generado por Encrypt().
        /// Lanza CryptographicException si la contraseña maestra es incorrecta.
        /// </summary>
        public static string Decrypt(string cipherBase64, string masterPassword)
        {
            byte[] data = Convert.FromBase64String(cipherBase64);

            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];
            byte[] cipherBytes = new byte[data.Length - SaltSize - IvSize];

            Buffer.BlockCopy(data, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(data, SaltSize, iv, 0, IvSize);
            Buffer.BlockCopy(data, SaltSize + IvSize, cipherBytes, 0, cipherBytes.Length);

            byte[] key = DeriveKey(masterPassword, salt);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Genera un hash de verificación de la contraseña maestra (para validar login).
        /// Guarda: Base64(salt + hash)
        /// </summary>
        public static string HashMasterPassword(string masterPassword)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = DeriveKey(masterPassword, salt);
            byte[] combined = new byte[SaltSize + hash.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, combined, SaltSize, hash.Length);
            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Verifica si la contraseña maestra coincide con el hash almacenado.
        /// </summary>
        public static bool VerifyMasterPassword(string masterPassword, string storedHash)
        {
            try
            {
                byte[] combined = Convert.FromBase64String(storedHash);
                byte[] salt = new byte[SaltSize];
                Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
                byte[] expectedHash = DeriveKey(masterPassword, salt);

                // Comparación en tiempo constante para evitar timing attacks
                byte[] storedHashBytes = new byte[combined.Length - SaltSize];
                Buffer.BlockCopy(combined, SaltSize, storedHashBytes, 0, storedHashBytes.Length);
                return CryptographicOperations.FixedTimeEquals(expectedHash, storedHashBytes);
            }
            catch
            {
                return false;
            }
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);
        }
    }
}