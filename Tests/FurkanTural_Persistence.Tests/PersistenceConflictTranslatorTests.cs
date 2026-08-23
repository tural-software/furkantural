using FluentAssertions;
using FurkanTural_Application.Exceptions;
using FurkanTural_Persistence.Repositories.Concrete;

namespace FurkanTural_Persistence.Tests;

public class PersistenceConflictTranslatorTests
{
    private static readonly Exception Inner = new InvalidOperationException("kaynak istisna");

    private const string UniqueIndexMessage =
        "Cannot insert duplicate key row in object 'dbo.Users' with unique index 'IX_Users_Username'. The duplicate key value is (furkan).";

    private const string UniqueConstraintMessage =
        "Violation of UNIQUE KEY constraint 'UQ_Subscribers_Email'. Cannot insert duplicate key in object 'dbo.Subscribers'. The duplicate key value is (a@b.c).";

    private const string PrimaryKeyMessage =
        "Violation of PRIMARY KEY constraint 'PK_Users'. Cannot insert duplicate key in object 'dbo.Users'.";

    private const string ForeignKeyMessage =
        "The INSERT statement conflicted with the FOREIGN KEY constraint \"FK_AccountActivations_Users_UserId\". The conflict occurred in database \"FurkanTural\", table \"dbo.Users\", column 'Id'.";

    [Theory]
    [InlineData(2601, UniqueIndexMessage, "IX_Users_Username")]
    [InlineData(2627, UniqueConstraintMessage, "UQ_Subscribers_Email")]
    [InlineData(2627, PrimaryKeyMessage, "PK_Users")]
    public void Tekil_ihlalleri_DuplicateEntityException_olur(int number, string message, string constraint)
    {
        var result = PersistenceConflictTranslator.Translate(number, message, Inner);

        result.Should().BeOfType<DuplicateEntityException>();
        result!.ConstraintName.Should().Be(constraint);
        result.InnerException.Should().BeSameAs(Inner);
    }

    [Fact]
    public void Yabanci_anahtar_ihlali_RelatedEntityMissingException_olur()
    {
        var result = PersistenceConflictTranslator.Translate(547, ForeignKeyMessage, Inner);

        result.Should().BeOfType<RelatedEntityMissingException>();
        result!.ConstraintName.Should().Be("FK_AccountActivations_Users_UserId");
    }

    [Theory]
    [InlineData(8152)]
    [InlineData(2628)]
    [InlineData(1205)]
    [InlineData(0)]
    public void Cekisme_disindaki_numaralar_cevrilmez(int number)
        => PersistenceConflictTranslator.Translate(number, "herhangi bir metin", Inner).Should().BeNull();

    [Fact]
    public void Kisit_adi_cozulemezse_ceviri_yine_yapilir()
    {
        var result = PersistenceConflictTranslator.Translate(2601, "taninmayan bir metin", Inner);

        result.Should().BeOfType<DuplicateEntityException>();
        result!.ConstraintName.Should().BeNull();
    }

    [Fact]
    public void Mesaj_null_ise_dusmez()
    {
        var result = PersistenceConflictTranslator.Translate(2627, null, Inner);

        result.Should().BeOfType<DuplicateEntityException>();
        result!.ConstraintName.Should().BeNull();
    }

    [Fact]
    public void Tablo_adi_kisit_adi_sanilmaz()
        => PersistenceConflictTranslator.Translate(2601, UniqueIndexMessage, Inner)!
            .ConstraintName.Should().NotBe("dbo.Users");
}
