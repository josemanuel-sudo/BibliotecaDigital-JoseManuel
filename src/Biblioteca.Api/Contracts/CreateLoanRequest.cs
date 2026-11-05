using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biblioteca.Api.Contracts;

public sealed class CreateLoanRequest
{
    [Range(1, int.MaxValue)]
    public int BookId { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Borrower { get; init; } = "";

    [JsonRequired]
    public DateOnly LoanDate { get; init; }

    [JsonRequired]
    public DateOnly DueDate { get; init; }
}
