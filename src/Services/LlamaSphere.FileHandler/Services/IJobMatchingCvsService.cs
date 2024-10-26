﻿using LlamaSphere.API.DTOs;

namespace LlamaSphere.API.Services;

public interface IJobMatchingCvsService
{
    Task<List<ReasoningResponse>> GetMatchingCvsForJobAsync(FindDevMatches findDevMatches);
}