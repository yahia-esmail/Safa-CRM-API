namespace Application.Common;

public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = [];
    public PaginationInfo Pagination { get; set; } = new();

    public PagedResult() { }

    public PagedResult(IEnumerable<T> data, int totalCount, int page, int pageSize)
    {
        Data = data;
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        Pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPrevPage = page > 1
        };
    }
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPrevPage { get; set; }
}


public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}
