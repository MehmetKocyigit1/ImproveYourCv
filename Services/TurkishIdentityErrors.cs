using Microsoft.AspNetCore.Identity;

namespace ImproveYourCv.Services;

// Identity hata mesajlarının Türkçesi (kayıt ve giriş formlarında görünür)
public class TurkishIdentityErrors : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = "Bu e-posta adresiyle zaten bir hesap var." };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = "Bu e-posta adresiyle zaten bir hesap var." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = "Geçerli bir e-posta adresi gir." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = "E-posta adresi geçersiz karakterler içeriyor." };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Şifre en az {length} karakter olmalı." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Şifre en az bir rakam içermeli." };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "Şifre en az bir küçük harf içermeli." };
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "Şifre en az bir büyük harf içermeli." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Şifre en az bir sembol içermeli." };
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Şifre en az {uniqueChars} farklı karakter içermeli." };
}
