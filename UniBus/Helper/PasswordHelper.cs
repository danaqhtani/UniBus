using System;
using System.Security.Cryptography;
using System.Text;

namespace UniBus.Helpers
{
    public static class PasswordHelper
    {
        // Generate a random 32-byte salt
        public static byte[] GenerateSalt()
        {
            byte[] salt = new byte[32];

            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            return salt;
        }

        // Create a SHA512 hash (64 bytes) using password + salt
        public static byte[] HashPassword(string password, byte[] salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] passwordWithSalt = new byte[passwordBytes.Length + salt.Length];

            Buffer.BlockCopy(passwordBytes, 0, passwordWithSalt, 0, passwordBytes.Length);
            Buffer.BlockCopy(salt, 0, passwordWithSalt, passwordBytes.Length, salt.Length);

            using (var sha512 = SHA512.Create())
            {
                return sha512.ComputeHash(passwordWithSalt);
            }
        }

        // Compare the entered password with the saved hash
        public static bool VerifyPassword(string enteredPassword, byte[] savedSalt, byte[] savedHash)
        {
            byte[] enteredHash = HashPassword(enteredPassword, savedSalt);

            if (enteredHash.Length != savedHash.Length)
                return false;

            for (int i = 0; i < enteredHash.Length; i++)
            {
                if (enteredHash[i] != savedHash[i])
                    return false;
            }

            return true;
        }
    }
}