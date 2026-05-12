using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace UniBusApp.helper
{
    public static class PasswordHelper
    {
        public static byte[] GenerateSalt()
        {
            byte[] salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        public static byte[] HashPassword(string password, byte[] salt)
        {
            return KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 64
            );
        }

        public static bool VerifyPassword(string password, byte[] salt, byte[] storedHash)
        {
            var hash = HashPassword(password, salt);
            return CryptographicOperations.FixedTimeEquals(hash, storedHash);
        }
    }
}