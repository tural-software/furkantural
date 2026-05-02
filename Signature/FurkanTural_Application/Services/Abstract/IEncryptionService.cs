using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface IEncryptionService
{
    Result<string> Encrypt(string value);
    Result<string> Decrypt(string value);
}