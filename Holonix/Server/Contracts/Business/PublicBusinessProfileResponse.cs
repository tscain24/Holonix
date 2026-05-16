namespace Holonix.Server.Contracts.Business;

public sealed record PublicBusinessProfileResponse(
    int BusinessId,
    string BusinessCode,
    string Name,
    string? Description,
    string? CategoryName,
    string? City,
    string? State,
    string? CountryName,
    string? BusinessEmail,
    string? BusinessPhoneNumber,
    string? Address1,
    string? Address2,
    string? ZipCode,
    string? BusinessIconBase64,
    IReadOnlyList<PublicBusinessServiceResponse> Services);

public sealed record PublicBusinessServiceResponse(
    int ServiceId,
    string Name,
    IReadOnlyList<PublicBusinessSubServiceResponse> SubServices);

public sealed record PublicBusinessSubServiceResponse(
    long BusinessSubServiceId,
    string Name,
    string? Description,
    bool ConsultationNeeded,
    int DurationMinutes,
    decimal Price);
