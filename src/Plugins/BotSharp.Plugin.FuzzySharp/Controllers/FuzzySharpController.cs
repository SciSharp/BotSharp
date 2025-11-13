using BotSharp.Abstraction.Knowledges;
using BotSharp.Abstraction.Knowledges.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.FuzzySharp.Controllers;

[ApiController]
public class FuzzySharpController : ControllerBase
{
    private readonly IPhraseService _phraseService;
    private readonly ILogger<FuzzySharpController> _logger;

    public FuzzySharpController(
        IPhraseService phraseService,
        ILogger<FuzzySharpController> logger)
    {
        _phraseService = phraseService;
        _logger = logger;
    }

    /// <summary>
    /// Analyze text for typos and entities using domain-specific vocabulary.
    /// 
    /// Returns:
    /// - `original`: Original input text
    /// - `tokens`: Tokenized text (only included if `include_tokens=true`)
    /// - `flagged`: List of flagged items (each with `match_type`):
    ///     - `domain_term_mapping` - Business abbreviations (confidence=1.0)
    ///     - `exact_match` - Exact vocabulary matches (confidence=1.0)
    ///     - `typo_correction` - Spelling corrections (confidence less than 1.0)
    /// - `processing_time_ms`: Processing time in milliseconds
    /// </summary>
    /// <param name="request">Text analysis request</param>
    /// <returns>Text analysis response</returns>
    [HttpPost("fuzzy-sharp/analyze-text")]
    [ProducesResponseType(typeof(List<SearchPhrasesResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnalyzeText([FromBody] string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return BadRequest(new { error = "Text is required" });
            }

            var result = await _phraseService.SearchPhrasesAsync(text);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing and searching entities");
            return StatusCode(500, new { error = $"Error analyzing and searching entities: {ex.Message}" });
        }
    }
}
