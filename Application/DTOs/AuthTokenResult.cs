using System;

namespace Rapsodia.Application.DTOs
{
    public sealed record AuthTokenResult(string Token, DateTime ExpiresAtUtc);
}
