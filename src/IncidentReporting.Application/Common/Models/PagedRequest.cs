namespace IncidentReporting.Application.Common.Models;

public class PagedRequest
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}