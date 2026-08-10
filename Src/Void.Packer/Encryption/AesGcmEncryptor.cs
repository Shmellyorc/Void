using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Void.Packer.Encryption;

public static class AesGcmEncryptor
{
    private const int KeySize = 32;     // 256-bit
    private const int NonceSize = 12;   // 96-bit for GCM
    private const int TagSize = 16;     // 128-bit authentication tag

    public static byte[] GenerateKey()
    {
        byte[] key = new byte[KeySize];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    public static byte[] GenerateNonce()
    {
        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);
        return nonce;
    }

    public static byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData = default)
    {
        if (data.Length == 0)
            return Array.Empty<byte>();
        
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));
        if (nonce.Length != NonceSize)
            throw new ArgumentException($"Nonce must be {NonceSize} bytes", nameof(nonce));

        using var aes = new AesGcm(key.ToArray(), TagSize);
        byte[] cipherText = new byte[data.Length];
        byte[] tag = new byte[TagSize];

        aes.Encrypt(nonce.ToArray(), data.ToArray(), cipherText, tag, associatedData.ToArray());

        // Combine ciphertext + tag
        byte[] result = new byte[cipherText.Length + TagSize];
        Buffer.BlockCopy(cipherText, 0, result, 0, cipherText.Length);
        Buffer.BlockCopy(tag, 0, result, cipherText.Length, TagSize);

        return result;
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData = default)
    {
        if (encryptedData.Length <= TagSize)
            throw new ArgumentException("Invalid encrypted data");
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));
        if (nonce.Length != NonceSize)
            throw new ArgumentException($"Nonce must be {NonceSize} bytes", nameof(nonce));

        int cipherTextLength = encryptedData.Length - TagSize;
        byte[] cipherText = new byte[cipherTextLength];
        byte[] tag = new byte[TagSize];

        Buffer.BlockCopy(encryptedData.ToArray(), 0, cipherText, 0, cipherTextLength);
        Buffer.BlockCopy(encryptedData.ToArray(), cipherTextLength, tag, 0, TagSize);

        using var aes = new AesGcm(key.ToArray(), TagSize);
        byte[] plaintext = new byte[cipherTextLength];

        aes.Decrypt(nonce.ToArray(), cipherText, tag, plaintext, associatedData.ToArray());

        return plaintext;
    }

    public static bool Verify(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData = default)
    {
        try
        {
            Decrypt(encryptedData, key, nonce, associatedData);
            return true;
        }
        catch
        {
            return false;
        }
    }
}