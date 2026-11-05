using System.Net;
using System.Net.Http.Json;
using Biblioteca.Api.Contracts;
using Xunit;

namespace Biblioteca.IntegrationTests;

public class BooksApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBooks_ReturnsSeededCatalog()
    {
        var response = await _client.GetAsync("/api/books");
        response.EnsureSuccessStatusCode();

        var books = await response.Content.ReadFromJsonAsync<List<BookResponse>>();
        Assert.NotNull(books);
        Assert.True(books.Count >= 3);
        Assert.Contains(books, x => x.Title == "Cien años de soledad");
    }

    [Fact]
    public async Task GetBook_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/books/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBook_WithExistingId_ReturnsBook()
    {
        var response = await _client.GetAsync("/api/books/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateBook_WithValidData_ReturnsCreated()
    {
        var request = new CreateBookRequest
        {
            Title = "El código Da Vinci",
            Author = "Dan Brown",
            Isbn = "9780307474278",
            PublishedYear = 2003,
            AvailableCopies = 4
        };

        var response = await _client.PostAsJsonAsync("/api/books", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateBook_WithDuplicateIsbn_ReturnsConflict()
    {
        var request = new CreateBookRequest
        {
            Title = "Duplicado",
            Author = "Autor",
            Isbn = "9780307474728",
            PublishedYear = 2020,
            AvailableCopies = 1
        };

        var response = await _client.PostAsJsonAsync("/api/books", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/books/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_WithoutActiveLoans_ReturnsNoContent()
    {
        var book = await CreateBookAsync("9780000000001");

        var response = await _client.DeleteAsync($"/api/books/{book.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_WithActiveLoan_ReturnsConflict()
    {
        var book = await CreateBookAsync("9780000000002");
        var loanResponse = await _client.PostAsJsonAsync("/api/loans", new CreateLoanRequest
        {
            BookId = book.Id,
            Borrower = "Test borrower",
            LoanDate = new DateOnly(2026, 1, 1),
            DueDate = new DateOnly(2026, 1, 8)
        });
        Assert.Equal(HttpStatusCode.Created, loanResponse.StatusCode);

        var response = await _client.DeleteAsync($"/api/books/{book.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<BookResponse> CreateBookAsync(string isbn)
    {
        var response = await _client.PostAsJsonAsync("/api/books", new CreateBookRequest
        {
            Title = "Test book",
            Author = "Test author",
            Isbn = isbn,
            PublishedYear = 2020,
            AvailableCopies = 1
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<BookResponse>(await response.Content.ReadFromJsonAsync<BookResponse>());
    }

    private sealed record BookResponse(int Id, string Title, string Author, string Isbn, int PublishedYear, int AvailableCopies);
}
