using System.Security.Cryptography;
using System.Text;

namespace Identity.Api.Services;

public sealed class TotpService
{
    private const string Issuer = "Identity Platform";
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encode(bytes);
    }

    public bool ValidateCode(string secret, string? code, DateTimeOffset? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var normalizedCode = new string(code.Where(char.IsDigit).ToArray());
        if (normalizedCode.Length != 6) return false;

        byte[] key;
        try { key = Base32Decode(secret); }
        catch (FormatException) { return false; }

        var currentStep = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / 30;
        // Allow the previous/next 30-second window for small clock drift.
        for (var offset = -1; offset <= 1; offset++)
        {
            var expected = GenerateCode(key, currentStep + offset);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(normalizedCode)))
                return true;
        }

        return false;
    }

    public string CreateProvisioningUri(string email, string secret)
    {
        var label = Uri.EscapeDataString($"{Issuer}:{email}");
        return $"otpauth://totp/{label}?secret={secret}&issuer={Uri.EscapeDataString(Issuer)}&algorithm=SHA1&digits=6&period=30";
    }

    private static string GenerateCode(byte[] key, long timeStep)
    {
        var counter = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian) Array.Reverse(counter);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0f;
        var value = ((hash[offset] & 0x7f) << 24)
                    | (hash[offset + 1] << 16)
                    | (hash[offset + 2] << 8)
                    | hash[offset + 3];
        return (value % 1_000_000).ToString("D6");
    }

    private static string Base32Encode(byte[] bytes)
    {
        var output = new StringBuilder((bytes.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var value in bytes)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                output.Append(Base32Alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }
        if (bitsLeft > 0) output.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        return output.ToString();
    }

    private static byte[] Base32Decode(string value)
    {
        var input = value.Replace(" ", string.Empty).TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>(input.Length * 5 / 8);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var character in input)
        {
            var index = Base32Alphabet.IndexOf(character);
            if (index < 0) throw new FormatException("Invalid Base32 secret.");
            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bytes.Add((byte)(buffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }
        return bytes.ToArray();
    }
}
