namespace RippleSync.Application.Common.Responses;

public record ListResponse<T>(IEnumerable<T> Data);

